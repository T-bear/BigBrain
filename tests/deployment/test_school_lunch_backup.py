"""Offline regression tests; no Docker, network or production database access."""
from contextlib import closing
import copy
import importlib.util
import json
import os
from pathlib import Path
import sqlite3
import tempfile
import time
import unittest
from unittest.mock import patch

SPEC = importlib.util.spec_from_file_location('backup', Path(__file__).resolve().parents[2] / 'scripts/backup-school-lunch-api.py')
b = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(b)


class BackupTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.root = Path(self.temp.name)
        self.roots = {}
        self.live = []
        self.umask = os.umask(0o077)
        self.no_docker = patch.object(b.subprocess, 'run', side_effect=AssertionError('No production calls'))
        self.no_docker.start()
        for target, names in b.EXPECTED.items():
            folder = self.root / target.lstrip('/')
            folder.mkdir()
            self.roots[target] = (folder, 'synthetic-' + target.lstrip('/'))
            for name in names:
                db = sqlite3.connect(folder / name)
                self.live.append(db)
                db.executescript('PRAGMA journal_mode=WAL; PRAGMA wal_autocheckpoint=0; CREATE TABLE parent(id INTEGER PRIMARY KEY); CREATE TABLE child(id INTEGER REFERENCES parent(id)); INSERT INTO parent VALUES(1); INSERT INTO child VALUES(1);')
                db.commit()
        self.base = self.root / 'backups'

    def tearDown(self):
        for db in self.live:
            db.close()
        self.no_docker.stop()
        os.umask(self.umask)
        self.temp.cleanup()

    def run_backup(self, check=lambda: None):
        return b.run_backup(self.roots, self.base, {'test': True}, check)

    def incomplete(self):
        manifests = list(self.base.glob('*/manifest.json'))
        self.assertEqual(len(manifests), 1)
        m = json.loads(manifests[0].read_text())
        self.assertEqual(m['status'], 'INCOMPLETE')
        return m

    def test_six_wal_snapshots_and_isolated_readback(self):
        source = self.roots['/lifecycle-data'][0] / 'lifecycle.db'
        self.assertTrue(Path(str(source) + '-wal').exists())
        # A committed row resides in WAL, an uncommitted row must not be captured.
        self.live[-1].execute('INSERT INTO parent VALUES(2)')
        self.live[-1].commit()
        self.live[-1].execute('INSERT INTO parent VALUES(3)')
        before = {p: b.digest(p) for root, _ in self.roots.values() for p in root.iterdir() if not p.name.endswith('-shm')}
        folder = self.run_backup()
        self.assertEqual(before, {p: b.digest(p) for p in before})
        m = json.loads((folder / 'manifest.json').read_text())
        self.assertEqual(m['status'], 'PASS')
        self.assertEqual(len(m['files']), 6)
        with closing(sqlite3.connect(folder / 'lifecycle-data/lifecycle.db')) as restored:
            self.assertEqual(restored.execute('SELECT id FROM parent ORDER BY id').fetchall(), [(1,), (2,)])
        for f in m['files']:
            p = folder / f['relativePath']
            self.assertEqual(p.stat().st_mode & 0o777, 0o600)
            self.assertEqual(b.digest(p), f['sha256'])
            self.assertEqual(f['isolatedReadback'], 'PASS')
        self.assertEqual(folder.stat().st_mode & 0o777, 0o700)

    def test_source_connections_readonly(self):
        original = b.sqlite3.connect
        seen = []
        def connect(value, *args, **kwargs):
            if str(value).endswith('?mode=ro'):
                seen.append(value)
                db = original(value, *args, **kwargs)
                with self.assertRaises(sqlite3.OperationalError):
                    db.execute('CREATE TABLE forbidden(x)')
                return db
            return original(value, *args, **kwargs)
        with patch.object(b.sqlite3, 'connect', side_effect=connect):
            self.run_backup()
        self.assertEqual(len(seen), 6)

    def test_unexpected_file_blocks_before_copy(self):
        (self.roots['/data'][0] / 'unknown.txt').write_text('synthetic')
        with self.assertRaisesRegex(b.Blocked, 'UnexpectedEntry'):
            self.run_backup()
        self.assertEqual(self.incomplete()['files'], [])

    def test_symlink_blocks(self):
        (self.roots['/data'][0] / 'unknown').symlink_to('/etc/passwd')
        with self.assertRaises(b.Blocked):
            self.run_backup()
        self.incomplete()

    def test_missing_database_blocks(self):
        self.live[-1].close()
        (self.roots['/lifecycle-data'][0] / 'lifecycle.db').unlink()
        with self.assertRaises(b.Blocked):
            self.run_backup()
        self.incomplete()

    def test_foreign_key_violation_blocks(self):
        self.live[0].execute('INSERT INTO child VALUES(999)')
        self.live[0].commit()
        with self.assertRaisesRegex(b.Blocked, 'ForeignKeysFailed'):
            self.run_backup()
        self.incomplete()

    def test_corruption_blocks(self):
        self.live[0].close()
        (self.roots['/data'][0] / 'meal-planner.db').write_bytes(b'not a database')
        with self.assertRaises(b.Blocked):
            self.run_backup()
        self.incomplete()

    def test_runtime_drift_blocks(self):
        def fail():
            raise b.Blocked('RuntimeChanged')
        with self.assertRaises(b.Blocked):
            self.run_backup(fail)
        self.incomplete()

    def test_file_appearing_during_backup_blocks(self):
        original = b.copy_database
        def copy(*args):
            original(*args)
            (self.roots['/settings-data'][0] / 'new-file').write_text('synthetic')
        with patch.object(b, 'copy_database', side_effect=copy):
            with self.assertRaises(b.Blocked):
                self.run_backup()
        self.incomplete()

    def test_space_gate(self):
        with patch.object(b.shutil, 'disk_usage', return_value=type('Space', (), {'free': 0})()):
            with self.assertRaisesRegex(b.Blocked, 'InsufficientSpace'):
                self.run_backup()
        self.incomplete()

    def test_interruption_keeps_incomplete(self):
        with patch.object(b, 'copy_database', side_effect=KeyboardInterrupt):
            with self.assertRaises(KeyboardInterrupt):
                self.run_backup()
        self.incomplete()

    def test_deadline(self):
        with self.assertRaisesRegex(b.Blocked, 'BackupDeadline'):
            b.copy_database(self.roots['/data'][0] / 'meal-planner.db', self.root / 'expired.db', time.monotonic() - 1)

    def test_no_overwrite_or_cleanup(self):
        first = self.run_backup()
        old = b.digest(first / 'manifest.json')
        second = self.run_backup()
        self.assertNotEqual(first, second)
        self.assertEqual(old, b.digest(first / 'manifest.json'))

    def test_reverification_needs_no_sources(self):
        folder = self.run_backup()
        with patch.object(b, 'inventory', side_effect=AssertionError('No source access')):
            self.assertEqual(b.verify_set(folder), b.digest(folder / 'manifest.json'))

    def test_checksum_tampering_rejected(self):
        folder = self.run_backup()
        p = folder / 'settings-data/settings.db'
        data = bytearray(p.read_bytes())
        data[-1] ^= 1
        p.write_bytes(data)
        with self.assertRaisesRegex(b.Blocked, 'ChecksumMismatch'):
            b.verify_set(folder)

    def test_incomplete_cannot_be_verified(self):
        with patch.object(b, 'copy_database', side_effect=KeyboardInterrupt):
            with self.assertRaises(KeyboardInterrupt):
                self.run_backup()
        folder = next(self.base.iterdir())
        with self.assertRaisesRegex(b.Blocked, 'IncompleteManifest'):
            b.verify_set(folder)

    def test_abrupt_process_exit_leaves_incomplete(self):
        pid = os.fork()
        if pid == 0:
            with patch.object(b, 'copy_database', side_effect=lambda *args: os._exit(33)):
                self.run_backup()
            os._exit(99)
        _, status = os.waitpid(pid, 0)
        self.assertEqual(os.waitstatus_to_exitcode(status), 33)
        self.incomplete()

    def test_excluded_mount_alias_rejected(self):
        mounts = [{'Destination': target, 'Source': str(root), 'Name': volume, 'Type': 'volume'}
                  for target, (root, volume) in self.roots.items()]
        self.assertEqual(len(b.sources({'Mounts': mounts})), 5)
        mounts.append({'Destination': '/finance-data', 'Source': mounts[0]['Source'], 'Type': 'volume'})
        with self.assertRaisesRegex(b.Blocked, 'OverlappingSources'):
            b.sources({'Mounts': mounts})

    def test_destination_symlink_rejected(self):
        self.base.symlink_to(self.roots['/data'][0], target_is_directory=True)
        with self.assertRaisesRegex(b.Blocked, 'SymlinkRejected'):
            self.run_backup()

    def runtime(self):
        return {
            'Id': 'synthetic-api', 'Image': b.IMAGE,
            'State': {'Running': True, 'Health': {'Status': 'healthy'}},
            'Mounts': [
                {'Destination': target, 'Source': str(root), 'Name': volume, 'Type': 'volume',
                 'Driver': 'local', 'RW': True, 'Mode': 'rw', 'Propagation': ''}
                for target, (root, volume) in self.roots.items()
            ] + [{'Destination': '/unrelated', 'Source': str(self.root / 'unrelated'),
                  'Type': 'bind', 'Mode': 'ro', 'RW': False}],
        }

    def test_mount_order_and_irrelevant_metadata_do_not_block(self):
        before = self.runtime()
        after = copy.deepcopy(before)
        after['Mounts'].reverse()
        after['Mounts'][0]['Source'] = str(self.root / 'other-unrelated')
        after['Mounts'][1]['Mode'] = 'rw,z'
        after['State']['Health']['Log'] = [{'Output': 'synthetic'}]
        self.assertNotEqual(before['Mounts'], after['Mounts'])  # Old check rejects this.
        b.verify_runtime(before, after)

    def test_required_runtime_fields_block(self):
        before = self.runtime()
        for field, value in [('Id', 'replacement'), ('Image', 'different-image')]:
            after = copy.deepcopy(before)
            after[field] = value
            with self.subTest(field=field), self.assertRaises(b.Blocked):
                b.verify_runtime(before, after)
        for state in [{'Running': False, 'Health': {'Status': 'healthy'}},
                      {'Running': True, 'Health': {'Status': 'unhealthy'}},
                      {'Running': True}]:
            after = copy.deepcopy(before)
            after['State'] = state
            with self.subTest(state=state), self.assertRaises(b.Blocked):
                b.verify_runtime(before, after)

    def test_selected_volume_identity_changes_block(self):
        before = self.runtime()
        replacement = self.root / 'replacement-source'
        replacement.mkdir()
        for index in range(5):
            for field, value in [('Name', 'different-volume'), ('Source', str(replacement)),
                                 ('Destination', '/different-target'), ('Type', 'bind'),
                                 ('Driver', 'different-driver'), ('RW', False)]:
                after = copy.deepcopy(before)
                after['Mounts'][index][field] = value
                with self.subTest(index=index, field=field), self.assertRaises(b.Blocked):
                    b.verify_runtime(before, after)

    def test_missing_duplicate_or_nested_selected_mount_blocks(self):
        before = self.runtime()
        for kind in ('missing', 'duplicate', 'nested'):
            after = copy.deepcopy(before)
            if kind == 'missing':
                after['Mounts'].pop(0)
            elif kind == 'duplicate':
                after['Mounts'].append(copy.deepcopy(after['Mounts'][0]))
            else:
                after['Mounts'].append({'Destination': '/data/nested', 'Source': str(self.root / 'nested'), 'Type': 'bind'})
            with self.subTest(kind=kind), self.assertRaises(b.Blocked):
                b.verify_runtime(before, after)

    def test_real_final_runtime_failure_keeps_set_incomplete(self):
        before = self.runtime()
        after = copy.deepcopy(before)
        after['Mounts'][0]['Name'] = 'replacement'
        with self.assertRaisesRegex(b.Blocked, 'RuntimeChanged'):
            self.run_backup(lambda: b.verify_runtime(before, after))
        self.incomplete()


if __name__ == '__main__':
    unittest.main()
