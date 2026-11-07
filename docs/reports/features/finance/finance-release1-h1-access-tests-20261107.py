"""Synthetic tests, no Finance paths. Identity/chown are simulated explicitly."""
import contextlib
import datetime
import hashlib
import io
import json
import os
from pathlib import Path
import sys
import tempfile
import types
import unittest
from unittest.mock import patch

BASE = Path(__file__).parent


def module(name):
    namespace = {'__name__': 'synthetic', '__file__': str(BASE / name)}
    exec(compile((BASE / name).read_bytes(), name, 'exec'), namespace)
    namespace['os'] = types.SimpleNamespace(**{k: getattr(os, k) for k in dir(os)})
    return namespace


class AccessTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix='h1-synthetic-')
        self.root = Path(self.temp.name)
        self.n = module('finance-release1-h1-export-20261107.py')
        self.src = self.root / 'source'
        self.src.mkdir()
        self.dest = self.root / 'evidence'
        self.n.update(SOURCE=self.src, DEST=self.dest,
                      READ_UNTIL='2099-01-01T00:00:00+00:00')
        self.n['ancestors'] = lambda _: None  # /tmp is intentionally not trusted.
        self.n['reader_group'] = lambda: None
        self.n['os'].getuid = self.n['os'].geteuid = lambda: 0
        self.n['os'].chown = lambda *args: None  # No privilege claimed by fixtures.
        self.db = self.src / self.n['DB']
        self.db.write_bytes(b'SYNTHETIC database, not SQLite')
        self.n['DB_HASH'] = hashlib.sha256(self.db.read_bytes()).hexdigest()
        bids = []
        for i in range(2):
            data = ('synthetic payload ' + str(i)).encode()
            digest = hashlib.sha256(data).hexdigest()
            bid = 'finance-backup-' + digest[:16]
            bids.append(bid)
            (self.src / (bid + '.data.json')).write_bytes(data)
            manifest = {'backupId': bid, 'status': 'Complete',
                        'artifacts': [{'path': bid + '.data.json', 'bytes': len(data),
                                       'sha256': 'sha256:' + digest}],
                        'contentFingerprint': 'sha256:' + digest}
            (self.src / (bid + '.manifest.json')).write_text(json.dumps(manifest))
        self.n['BACKUPS'] = tuple(bids)
        self.before = {p.name: (p.read_bytes(), p.stat().st_mtime_ns)
                       for p in self.src.iterdir()}

    def tearDown(self):
        # Fixture data only, never a production cleanup path.
        self.temp.cleanup()

    def run_export(self):
        out = io.StringIO()
        with patch.object(sys, 'argv', ['test', '--owner-approved-read-until',
                                        self.n['READ_UNTIL']]):
            with contextlib.redirect_stdout(out):
                result = self.n['main']()
        return result, out.getvalue()

    def test_copy_hashes_and_private_group_modes(self):
        result, out = self.run_export()
        self.assertEqual(result, 0, out)
        for name, (data, when) in self.before.items():
            self.assertEqual((self.dest / name).read_bytes(), data)
            self.assertEqual((self.src / name).stat().st_mtime_ns, when)
            self.assertEqual((self.dest / name).stat().st_mode & 0o777, 0o440)
        self.assertEqual(self.dest.stat().st_mode & 0o777, 0o550)
        self.assertEqual(len(list(self.dest.iterdir())), 6)

    def test_hash_mismatch_before_copy(self):
        self.n['DB_HASH'] = '0' * 64
        self.assertEqual(self.run_export()[0], 1)
        self.assertFalse(self.dest.exists())

    def test_sidecar_before_copy(self):
        Path(str(self.db) + '-wal').touch()
        self.assertEqual(self.run_export()[0], 1)
        self.assertFalse(self.dest.exists())

    def test_existing_destination_never_overwritten(self):
        self.dest.mkdir()
        marker = self.dest / 'keep'
        marker.write_text('keep')
        self.assertEqual(self.run_export()[0], 1)
        self.assertEqual(marker.read_text(), 'keep')

    def test_manifest_escape_before_copy(self):
        p = self.src / (self.n['BACKUPS'][0] + '.manifest.json')
        m = json.loads(p.read_bytes())
        m['artifacts'][0]['path'] = '../escape'
        p.write_text(json.dumps(m))
        self.assertEqual(self.run_export()[0], 1)
        self.assertFalse(self.dest.exists())

    def test_copy_failure_preserves_private_partial(self):
        original = self.n['hash_fd']
        def fail(fd, output=None):
            if output is not None:
                os.write(output, b'synthetic partial')
                raise OSError('synthetic')
            return original(fd, output)
        self.n['hash_fd'] = fail
        result, out = self.run_export()
        self.assertEqual(result, 1)
        self.assertIn('owner_disposition_required', out)
        self.assertEqual(self.dest.stat().st_mode & 0o777, 0o700)
        self.assertTrue(any(self.dest.iterdir()))

    def test_expired_approval_before_copy(self):
        self.n['READ_UNTIL'] = '2000-01-01T00:00:00+00:00'
        self.assertEqual(self.run_export()[0], 1)
        self.assertFalse(self.dest.exists())

    def test_untrusted_writable_ancestor(self):
        real = module('finance-release1-h1-export-20261107.py')
        with self.assertRaises(ValueError):
            real['ancestors'](Path('/tmp'))

    def test_account_group_rejects_additional_reader(self):
        real = module('finance-release1-h1-export-20261107.py')
        def account(path, **kwargs):
            return io.StringIO('reader:x:1000:1000::/:/bin/sh\n' if path.endswith('passwd')
                               else 'reader:x:1000:other\n')
        real['open'] = account
        with self.assertRaises(ValueError):
            real['reader_group']()

    def test_account_group_accepts_single_reader(self):
        real = module('finance-release1-h1-export-20261107.py')
        def account(path, **kwargs):
            return io.StringIO('reader:x:1000:1000::/:/bin/sh\n' if path.endswith('passwd')
                               else 'reader:x:1000:reader\n')
        real['open'] = account
        real['reader_group']()

    def test_verifier_wrong_location(self):
        n = module('finance-release1-h1-verify-20261107.py')
        with self.assertRaises(ValueError):
            n['evidence_access'](self.db, self.src)

    def test_no_retry_after_success(self):
        self.assertEqual(self.run_export()[0], 0)
        marker = (self.dest / 'h1-custody.json').read_bytes()
        self.assertEqual(self.run_export()[0], 1)
        self.assertEqual((self.dest / 'h1-custody.json').read_bytes(), marker)

    def test_source_changed_during_copy_rejected(self):
        original = self.n['hash_fd']
        def changed(fd, output=None):
            result = original(fd, output)
            if output is not None:
                with self.db.open('ab') as stream:
                    stream.write(b'synthetic change')
            return result
        self.n['hash_fd'] = changed
        self.assertEqual(self.run_export()[0], 1)
        self.assertEqual(self.dest.stat().st_mode & 0o777, 0o700)

    def test_manifest_hash_mismatch_rejected(self):
        data = self.src / (self.n['BACKUPS'][0] + '.data.json')
        data.write_bytes(b'synthetic mismatch')
        self.assertEqual(self.run_export()[0], 1)
        self.assertFalse(self.dest.exists())

    def test_no_space_before_copy(self):
        self.n['os'].statvfs = lambda _: types.SimpleNamespace(f_bavail=0, f_frsize=4096)
        self.assertEqual(self.run_export()[0], 1)
        self.assertFalse(self.dest.exists())

    def verifier_guard(self, fault=None):
        self.assertEqual(self.run_export()[0], 0)
        marker = self.dest / 'h1-custody.json'
        os.chmod(marker, 0o600)
        custody = json.loads(marker.read_bytes())
        custody['readUntil'] = '2026-11-09T20:00:00+01:00'
        if fault == 'expired':
            custody['readUntil'] = '2000-01-01T00:00:00+00:00'
        marker.write_text(json.dumps(custody))
        os.chmod(marker, 0o440)
        if fault == 'extra_file':
            os.chmod(self.dest, 0o700)
            (self.dest / 'unapproved').touch()
            os.chmod(self.dest, 0o550)
        n = module('finance-release1-h1-verify-20261107.py')
        n.update(EVIDENCE_ROOT=self.dest, SNAPSHOT_SHA256=self.n['DB_HASH'],
                 BACKUPS=self.n['BACKUPS'], reader_group=lambda: None)
        n['os'].access = lambda *args: False
        class Clock(datetime.datetime):
            @classmethod
            def now(cls, tz=None):
                return cls(2026, 11, 7, tzinfo=datetime.timezone.utc)
        n['dt'] = types.SimpleNamespace(datetime=Clock, timezone=datetime.timezone)
        old = Path.lstat
        def metadata(p, *args, **kwargs):
            s = list(old(p, *args, **kwargs))
            # Simulated protected ancestor/group; real file modes/content retained.
            if p == Path('/tmp'):
                s[0] &= ~0o022
            if p == self.dest or p.parent == self.dest:
                s[5] = 1000
            if p == self.dest / self.n['DB']:
                if fault == 'writable':
                    s[0] |= 0o020
                if fault == 'wrong_owner':
                    s[4] = 1000
                if fault == 'hardlink':
                    s[3] = 2
            return os.stat_result(s)
        with patch.object(Path, 'lstat', metadata):
            n['evidence_access'](self.dest / self.n['DB'], self.dest)

    def test_verifier_guard_positive_synthetic(self):
        self.verifier_guard()

    def test_verifier_guard_writable_rejected(self):
        with self.assertRaises(ValueError):
            self.verifier_guard('writable')

    def test_verifier_guard_wrong_owner_rejected(self):
        with self.assertRaises(ValueError):
            self.verifier_guard('wrong_owner')

    def test_verifier_guard_hardlink_rejected(self):
        with self.assertRaises(ValueError):
            self.verifier_guard('hardlink')

    def test_verifier_guard_expired_rejected(self):
        with self.assertRaises(ValueError):
            self.verifier_guard('expired')

    def test_verifier_guard_extra_file_rejected(self):
        with self.assertRaises(ValueError):
            self.verifier_guard('extra_file')


if __name__ == '__main__':
    unittest.main()
