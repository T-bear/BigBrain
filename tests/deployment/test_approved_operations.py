"""Offline only: synthetic SQLite/WAL, fake Docker and systemd. No production commands."""
import contextlib
import copy
import hashlib
import importlib.machinery
import importlib.util
import io
import json
import os
from pathlib import Path
import sqlite3
import subprocess
import sys
import tempfile
import time
import unittest
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[2]


def load(name, path):
    loader = importlib.machinery.SourceFileLoader(name, str(path))
    spec = importlib.util.spec_from_loader(name, loader)
    module = importlib.util.module_from_spec(spec)
    loader.exec_module(module)
    return module


ops = load('approved_operations', ROOT / 'scripts/approved-ops/bigbrain-approved-ops')
support = load('backup_support', ROOT / 'scripts/approved-ops/backup_support.py')
REV = 'a' * 40
IMAGE = 'sha256:' + 'b' * 64
RUN = 'c' * 32
CONTRACT = 'school-lunch-v3-systemd-v1'
FILES = ops.CONTRACTS[CONTRACT]['files']


class OperationsTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.base = Path(self.temp.name)
        self.old_umask = os.umask(0o077)
        self.fs = ops.ProtectedFiles(self.base, os.getuid())
        self.config = self.base / 'config'
        self.state = self.base / 'state'
        self.backups = self.base / 'backups'
        for path in (self.config, self.config / 'packages', self.state,
                     self.state / 'claims', self.state / 'runs', self.backups):
            path.mkdir(mode=0o700)
        self.roots = {}
        self.live = []
        mounts = []
        for target, names in support.EXPECTED.items():
            root = self.base / ('source-' + target.lstrip('/'))
            root.mkdir(mode=0o700)
            self.roots[target] = (root, target.lstrip('/') + '-volume')
            mounts.append({'Destination': target, 'Type': 'volume', 'Name': self.roots[target][1],
                           'Source': str(root), 'Driver': 'local', 'RW': True})
            for name in names:
                db = sqlite3.connect(root / name)
                db.execute('PRAGMA journal_mode=WAL')
                db.execute('CREATE TABLE parent(id INTEGER PRIMARY KEY)')
                db.execute('CREATE TABLE child(id INTEGER PRIMARY KEY, parent_id INTEGER REFERENCES parent(id))')
                db.execute('INSERT INTO parent VALUES(1)')
                db.execute('INSERT INTO child VALUES(1,1)')
                db.commit()
                self.live.append(db)
        self.container = {'Id': 'd' * 64, 'Image': IMAGE,
                          'State': {'Running': True, 'Health': {'Status': 'healthy'}, 'OOMKilled': False},
                          'Config': {'Labels': {'org.opencontainers.image.revision': REV}}, 'Mounts': mounts}
        self.calls = []
        def fake_run(args, timeout=20):
            self.calls.append(args)
            if args[0] == ops.DOCKER:
                self.assertEqual(args, [ops.DOCKER, '--host', ops.ENV['DOCKER_HOST'], 'inspect', 'bigbrain-sprint1-api-1'])
                return json.dumps([self.container]).encode()
            self.assertEqual(args[0], ops.SYSTEMD)
            return b''
        self.engine = ops.Operations(self.fs, support, self.config, self.state, self.backups, fake_run)

    def tearDown(self):
        for db in self.live:
            db.close()
        os.umask(self.old_umask)
        self.temp.cleanup()

    def write(self, path, value):
        path.write_bytes(json.dumps(value).encode() if not isinstance(value, bytes) else value)
        path.chmod(0o600)

    def approval(self, op, **values):
        a = {'schema': 1, 'operation': op, 'id': RUN, 'expiresAt': int(time.time()) + 600, **values}
        self.write(self.config / (op + '.json'), a)
        return a

    def backup(self):
        self.approval('backup-approved', runtime=self.engine.identity(self.container), revision=REV)
        result = self.engine.dispatch('backup-approved')
        self.assertEqual(result, {'operation': 'backup-approved', 'status': 'PASS', 'databases': 6})
        folder = next(self.backups.iterdir())
        return folder, support.digest(folder / 'manifest.json')

    def prepare_package(self):
        folder, digest = self.backup()
        # New independently approved operation, never reuse backup claim.
        run = 'e' * 32
        plan = {'contract': CONTRACT, 'revision': REV, 'run': run, 'targets': {'api': self.container, 'web': {'Id': 'f' * 64, 'Image': IMAGE}},
                'candidates': {'api': IMAGE, 'web': IMAGE}}
        cfg = {'services': {'api': {'image': IMAGE}, 'web': {'image': IMAGE}},
               'networks': {'default': {'external': True}}, 'volumes': {'data': {'external': True}}}
        root = self.config / 'packages' / run
        root.mkdir(mode=0o700)
        files = {name: b'# synthetic, never executed\n' for name in FILES}
        files.update({'plan.json': json.dumps(plan).encode(),
                      'release.compose.json': json.dumps(cfg).encode(),
                      'rollback.compose.json': json.dumps(cfg).encode(),
                      'backup-runtime.json': b'{}'})
        for name, value in files.items():
            self.write(root / name, value)
        manifest = {name: ops.sha(value) for name, value in files.items()}
        self.write(root / 'package.json', manifest)
        self.dependencies = []
        for i in range(4):
            path = self.base / ('system-executable-' + str(i))
            self.write(path, b'trusted synthetic executable')
            self.dependencies.append(str(path))
        a = self.approval('start-approved', revision=REV, services=['api', 'web'],
                          packageSha256=ops.sha((root / 'package.json').read_bytes()),
                          backupId=folder.name, manifestSha256=digest,
                          dependencies={p: ops.sha(Path(p).read_bytes()) for p in self.dependencies},
                          contract='school-lunch-v3-systemd-v1')
        a['id'] = run
        self.write(self.config / 'start-approved.json', a)
        self.dep_patch = patch.object(ops, 'DEPENDENCIES', tuple(self.dependencies))
        self.dep_patch.start()
        self.addCleanup(self.dep_patch.stop)
        return a, root

    def reseal(self, a, root):
        self.write(root / 'package.json', {name: ops.sha((root / name).read_bytes()) for name in FILES})
        a['packageSha256'] = ops.sha((root / 'package.json').read_bytes())
        self.write(self.config / 'start-approved.json', a)

    def test_old_client_pause_approval_is_rejected_without_operation(self):
        a, root = self.prepare_package()
        a['usersPaused'] = True
        self.write(self.config / 'start-approved.json', a)
        with self.assertRaises(ops.Denied): self.engine.approval('start-approved')
        self.assertFalse((self.state / ('claim-' + a['id'] + '.json')).exists())

    def test_backup_all_six_live_wal_readback_and_no_source_changes(self):
        before = [(db.execute('SELECT * FROM parent').fetchall(), db.execute('PRAGMA schema_version').fetchone()) for db in self.live]
        folder, digest = self.backup()
        self.assertEqual(support.verify_set(folder), digest)
        self.assertEqual(before, [(db.execute('SELECT * FROM parent').fetchall(), db.execute('PRAGMA schema_version').fetchone()) for db in self.live])
        for path in folder.rglob('*'):
            self.assertEqual(path.stat().st_mode & 0o077, 0)
        self.assertTrue(all(args[3] == 'inspect' for args in self.calls))

    def test_verify_backup_uses_only_retained_copies(self):
        folder, digest = self.backup()
        self.approval('verify-backup', backupId=folder.name, manifestSha256=digest)
        self.calls.clear()
        self.engine.run = lambda *_a, **_k: self.fail('external process during retained verification')
        self.assertEqual(self.engine.dispatch('verify-backup')['status'], 'PASS')
        self.assertEqual(self.calls, [])

    def test_status_is_allowlisted_no_private_payload(self):
        self.assertEqual(self.engine.dispatch('status')['deployment'], 'NO_WRAPPER_RUN')
        a, _ = self.prepare_package()
        self.engine.dispatch('start-approved')
        root = self.state / 'runs' / ('school-lunch-v3-' + a['id'])
        self.write(root / 'complete.json', {'status': 'COMPLETED', 'run': a['id'], 'password': 'never-output', 'privatePath': '/secret/backup'})
        status = self.engine.dispatch('status')
        self.assertEqual(status['deployment'], 'COMPLETION_RECORDED')
        self.assertNotIn('never-output', json.dumps(status))
        self.assertNotIn('/secret', json.dumps(status))
        self.assertEqual(set(status), {'operation', 'deployment'})

    def test_start_fixed_systemd_arguments_and_snapshot(self):
        a, source = self.prepare_package()
        result = self.engine.dispatch('start-approved')
        self.assertEqual(result['status'], 'SUBMITTED')
        command = self.calls[-1]
        self.assertIn('--no-block', command)
        self.assertIn('--property=KillMode=control-group', command)
        self.assertIn('--property=Restart=no', command)
        self.assertIn('--property=TimeoutStopSec=180', command)
        self.assertTrue(any(x.startswith('--property=ExecStopPost=') and ' recover ' in x for x in command))
        root = self.state / 'runs' / ('school-lunch-v3-' + a['id'])
        self.assertTrue((root / 'submitted.json').exists())
        for name in (*FILES, 'package.json'):
            self.assertEqual((root / name).read_bytes(), (source / name).read_bytes())
        self.assertEqual(command[-5:], ['-IS', str(root / 'runner.py'), 'work', str(root), a['packageSha256']])

    def test_unknown_operation_and_arguments_fail_without_process(self):
        for op in ('stop', 'status --all', '../start-approved', 'start-approved;id', '--help', ''):
            with self.subTest(op=op), self.assertRaises(ops.Denied):
                self.engine.dispatch(op)
        self.assertFalse(self.calls)
        for args in (['status', 'anything'], ['--env=X'], ['status;id']):
            with patch.object(sys, 'argv', ['wrapper', *args]), patch.object(os, 'geteuid', return_value=0), self.assertRaises(ops.Denied):
                ops.main()

    def test_approval_schema_expiry_wrong_operation_and_unknown_key(self):
        for change in ({'expiresAt': 1}, {'operation': 'status'}, {'id': '../x'}, {'shell': 'id'}, {'schema': 2}):
            with self.subTest(change=change):
                self.approval('backup-approved', runtime=self.engine.identity(self.container), revision=REV)
                path = self.config / 'backup-approved.json'
                data = json.loads(path.read_text()); data.update(change); self.write(path, data)
                with self.assertRaises(ops.Denied):
                    self.engine.dispatch('backup-approved')
        self.assertFalse(self.calls)

    def test_duplicate_json_keys_rejected(self):
        with self.assertRaises(ops.Denied):
            ops.decode(b'{"id":1,"id":2}')

    def test_paths_symlink_parents_hardlinks_and_permissions(self):
        good = self.config / 'safe.json'; self.write(good, b'{}')
        link = self.config / 'link.json'; link.symlink_to(good)
        with self.assertRaises(OSError): self.fs.read(link)
        parent = self.base / 'link-parent'; parent.symlink_to(self.config, target_is_directory=True)
        with self.assertRaises(OSError): self.fs.read(parent / 'safe.json')
        hard = self.config / 'hard.json'; os.link(good, hard)
        with self.assertRaises(ops.Denied): self.fs.read(hard)
        hard.unlink(); good.chmod(0o666)
        with self.assertRaises(ops.Denied): self.fs.read(good)
        good.chmod(0o600); self.config.chmod(0o777)
        with self.assertRaises(ops.Denied): self.fs.read(good)
        self.config.chmod(0o700)
        with self.assertRaises(ops.Denied): self.fs.read(self.base / '..' / self.base.name / 'config/safe.json')
        foreign = ops.ProtectedFiles(self.base, os.getuid() + 1)
        with self.assertRaises(ops.Denied): foreign.read(good)

    def test_file_size_limit(self):
        path = self.config / 'oversized'; self.write(path, b'abc')
        with self.assertRaises(ops.Denied): self.fs.read(path, limit=2)

    def test_concurrent_wrapper_rejected(self):
        with self.fs.lock(self.state / 'lock'):
            with self.assertRaisesRegex(ops.Denied, 'Busy'):
                self.engine.dispatch('status')

    def test_backup_approval_single_use(self):
        self.backup()
        with self.assertRaises(FileExistsError): self.engine.dispatch('backup-approved')
        self.assertEqual(len(list(self.backups.iterdir())), 1)

    def test_runtime_identity_image_health_or_volume_drift_rejected(self):
        original = copy.deepcopy(self.container)
        for mutate in (lambda c: c.update(Id='x'), lambda c: c.update(Image='sha256:' + 'f'*64),
                       lambda c: c['State'].update(Running=False),
                       lambda c: c['State']['Health'].update(Status='unhealthy'),
                       lambda c: c['Mounts'][0].update(Name='wrong')):
            self.container = copy.deepcopy(original)
            self.approval('backup-approved', runtime=self.engine.identity(self.container), revision=REV)
            mutate(self.container)
            with self.assertRaises(ops.Denied): self.engine.dispatch('backup-approved')
        self.assertFalse(list(self.backups.iterdir()))

    def test_unrelated_mount_order_does_not_invalidate_identity(self):
        identity = self.engine.identity(self.container)
        self.container['Mounts'].reverse()
        self.assertEqual(identity, self.engine.identity(self.container))

    def test_runtime_change_after_backup_marks_incomplete(self):
        self.approval('backup-approved', runtime=self.engine.identity(self.container), revision=REV)
        count = 0
        def changed():
            nonlocal count
            count += 1
            if count == 2: self.container['Id'] = 'changed'
            return self.container
        with patch.object(self.engine, 'inspect', side_effect=changed), self.assertRaises(ops.Denied):
            self.engine.dispatch('backup-approved')
        manifest = json.loads(next(self.backups.iterdir()).joinpath('manifest.json').read_text())
        self.assertEqual(manifest['status'], 'INCOMPLETE')

    def test_unexpected_source_file_incomplete(self):
        (self.roots['/data'][0] / 'unexpected').write_bytes(b'x')
        self.approval('backup-approved', runtime=self.engine.identity(self.container), revision=REV)
        with self.assertRaises(support.Blocked): self.engine.dispatch('backup-approved')
        self.assertEqual(json.loads(next(self.backups.iterdir()).joinpath('manifest.json').read_text())['status'], 'INCOMPLETE')

    def test_tampered_manifest_or_backup_rejected(self):
        folder, digest = self.backup()
        self.approval('verify-backup', backupId=folder.name, manifestSha256=digest)
        data = folder / 'data/meal-planner.db'; data.write_bytes(b'corrupted')
        with self.assertRaises(support.Blocked): self.engine.dispatch('verify-backup')
        (folder / 'manifest.json').write_bytes(b'{}')
        with self.assertRaises(ops.Denied): self.engine.dispatch('verify-backup')

    def test_backup_traversal_rejected(self):
        self.approval('verify-backup', backupId='../../etc', manifestSha256='a'*64)
        with self.assertRaises(ops.Denied): self.engine.dispatch('verify-backup')

    def test_package_tampering_and_extra_files(self):
        a, root = self.prepare_package()
        runner = root / 'runner.py'; original = runner.read_bytes(); runner.write_bytes(b'changed')
        with self.assertRaises(ops.Denied): self.engine.dispatch('start-approved')
        runner.write_bytes(original)
        (root / 'extra.py').write_bytes(b'extra')
        with self.assertRaises(ops.Denied): self.engine.dispatch('start-approved')
        self.assertFalse(any(c[0] == ops.SYSTEMD for c in self.calls))

    def test_package_symlink_rejected(self):
        _, root = self.prepare_package()
        runner = root / 'runner.py'; runner.unlink(); runner.symlink_to(root / 'backup.py')
        with self.assertRaises(OSError): self.engine.dispatch('start-approved')

    def test_unauthorized_service_and_revision_rejected(self):
        a, root = self.prepare_package()
        a['services'] = ['api', 'finance']; self.write(self.config / 'start-approved.json', a)
        with self.assertRaises(ops.Denied): self.engine.dispatch('start-approved')
        a['services'] = ['api', 'web']; a['revision'] = 'f'*40; self.write(self.config / 'start-approved.json', a)
        with self.assertRaises(ops.Denied): self.engine.dispatch('start-approved')
        self.assertFalse(any(c[0] == ops.SYSTEMD for c in self.calls))

    def test_compose_inputs_and_third_service_rejected_even_if_resealed(self):
        a, root = self.prepare_package()
        path = root / 'release.compose.json'; original = path.read_bytes()
        for change in ('third-service', 'env_file', 'depends_on', 'build'):
            cfg = json.loads(original)
            if change == 'third-service': cfg['services']['other'] = {}
            else: cfg['services']['api'][change] = 'untrusted'
            self.write(path, cfg); self.reseal(a, root)
            with self.subTest(change=change), self.assertRaises(ops.Denied): self.engine.dispatch('start-approved')

    def test_wrong_compose_image_rejected_even_if_resealed(self):
        a, root = self.prepare_package()
        path = root / 'release.compose.json'
        cfg = json.loads(path.read_text()); cfg['services']['api']['image'] = 'unapproved:latest'
        self.write(path, cfg); self.reseal(a, root)
        with self.assertRaisesRegex(ops.Denied, 'ComposeImageChanged'):
            self.engine.dispatch('start-approved')

    def test_unknown_and_blocked_contracts_rejected_before_privileged_calls(self):
        a, _ = self.prepare_package()
        for name in ('unknown', '../school-lunch-v3-systemd-v1', 'api-only-diagnostics-v1'):
            a['contract'] = name
            self.write(self.config / 'start-approved.json', a)
            self.calls.clear()
            with self.subTest(contract=name), self.assertRaises(ops.Denied):
                self.engine.dispatch('start-approved')
            self.assertEqual(self.calls, [])
            self.assertFalse((self.state / 'active.json').exists())

    def test_api_web_package_cannot_be_used_for_api_only(self):
        a, _ = self.prepare_package()
        a.update(contract='api-only-diagnostics-v1', services=['api'])
        self.write(self.config / 'start-approved.json', a)
        self.calls.clear()
        with self.assertRaisesRegex(ops.Denied, 'ContractUnavailable'):
            self.engine.dispatch('start-approved')
        self.assertEqual(self.calls, [])
        self.assertFalse((self.state / 'claims' / (a['id'] + '.json')).exists())

    def test_api_web_contract_cannot_be_narrowed_or_expanded_by_approval(self):
        a, _ = self.prepare_package()
        for services in (['api'], ['web'], ['api', 'web', 'sonarr'], ['web', 'api']):
            a['services'] = services
            self.write(self.config / 'start-approved.json', a)
            with self.subTest(services=services), self.assertRaisesRegex(ops.Denied, 'DeploymentScope'):
                self.engine.dispatch('start-approved')

    def test_plan_contract_confusion_rejected_even_if_resealed(self):
        a, root = self.prepare_package()
        for identifier in ('api-only-diagnostics-v1', 'unknown', None):
            path = root / 'plan.json'
            plan = json.loads(path.read_text())
            plan['contract'] = identifier
            self.write(path, plan); self.reseal(a, root)
            with self.subTest(identifier=identifier), self.assertRaisesRegex(ops.Denied, 'ContractMismatch'):
                self.engine.dispatch('start-approved')

    def test_api_only_plan_cannot_masquerade_as_api_web_contract(self):
        a, root = self.prepare_package()
        path = root / 'plan.json'; plan = json.loads(path.read_text())
        del plan['targets']['web']; del plan['candidates']['web']
        self.write(path, plan); self.reseal(a, root)
        with self.assertRaisesRegex(ops.Denied, 'PlanScope'):
            self.engine.dispatch('start-approved')

    def test_contract_registry_cannot_be_supplied_by_approval_or_package(self):
        a, root = self.prepare_package()
        a['contracts'] = {'api-only-diagnostics-v1': {'enabled': True}}
        self.write(self.config / 'start-approved.json', a)
        with self.assertRaisesRegex(ops.Denied, 'InvalidSchema'):
            self.engine.dispatch('start-approved')
        del a['contracts']; self.write(self.config / 'start-approved.json', a)
        self.write(root / 'contracts.json', {'enabled': True})
        with self.assertRaisesRegex(ops.Denied, 'PackageFiles'):
            self.engine.dispatch('start-approved')
        with self.assertRaises(TypeError):
            ops.CONTRACTS['api-only-diagnostics-v1']['enabled'] = True
        with self.assertRaises(TypeError):
            ops.CONTRACTS['new'] = {}

    def test_status_rejects_unknown_or_missing_contract_without_fallback(self):
        for value in ({'id': RUN}, {'id': RUN, 'contract': 'unknown'},
                      {'id': RUN, 'contract': 'api-only-diagnostics-v1'}):
            self.write(self.state / 'active.json', value)
            with self.subTest(value=value), self.assertRaises(ops.Denied):
                self.engine.dispatch('status')
        self.assertEqual(self.calls, [])

    def test_dependency_changed_rejected(self):
        _, _ = self.prepare_package()
        Path(self.dependencies[0]).write_bytes(b'changed')
        with self.assertRaises(ops.Denied): self.engine.dispatch('start-approved')

    def test_launch_failure_is_consumed_and_interlocked(self):
        a, _ = self.prepare_package()
        real = self.engine.run
        def failure(args, **kwargs):
            if args[0] == ops.SYSTEMD: raise ops.Denied('CommandTimeout')
            return real(args, **kwargs)
        self.engine.run = failure
        with self.assertRaises(ops.Denied): self.engine.dispatch('start-approved')
        self.assertTrue((self.state / 'claims' / (a['id'] + '.json')).exists())
        self.assertTrue((self.state / 'active.json').exists())
        with self.assertRaisesRegex(ops.Denied, 'DeploymentInterlock'): self.engine.dispatch('start-approved')
        self.assertEqual(self.engine.dispatch('status')['deployment'], 'CLAIMED_MANUAL_REVIEW_REQUIRED')

    def test_parent_package_replacement_cannot_change_executed_snapshot(self):
        a, source = self.prepare_package()
        verified = (source / 'runner.py').read_bytes()
        original = self.engine.verify
        def swap(approval):
            result = original(approval)
            (source / 'runner.py').write_bytes(b'malicious replacement after hash check')
            return result
        with patch.object(self.engine, 'verify', side_effect=swap):
            self.engine.dispatch('start-approved')
        snapshot = self.state / 'runs' / ('school-lunch-v3-' + a['id']) / 'runner.py'
        self.assertEqual(snapshot.read_bytes(), verified)

    def test_expiry_between_verification_and_claim_blocks_start(self):
        a, _ = self.prepare_package()
        original = self.engine.verify
        def expires(approval):
            result = original(approval)
            approval['expiresAt'] = 1
            return result
        with patch.object(self.engine, 'verify', side_effect=expires), self.assertRaises(ops.Denied):
            self.engine.dispatch('start-approved')
        self.assertFalse((self.state / 'active.json').exists())

    def test_recovery_status_preserves_manual_boundary(self):
        a, _ = self.prepare_package()
        self.engine.dispatch('start-approved')
        root = self.state / 'runs' / ('school-lunch-v3-' + a['id'])
        self.write(root / 'complete.json', {'status': 'COMPLETED', 'run': 'wrong'})
        self.assertEqual(self.engine.dispatch('status')['deployment'], 'SUBMITTED')
        self.write(root / 'recovery.json', {'status': 'MANUAL_REVIEW_NO_RESTORE', 'run': a['id']})
        self.assertEqual(self.engine.dispatch('status')['deployment'], 'MANUAL_REVIEW_REQUIRED')

    def test_claimed_run_blocks_different_approval_and_backup(self):
        a, _ = self.prepare_package()
        self.engine.dispatch('start-approved')
        a['id'] = '9' * 32
        self.write(self.config / 'start-approved.json', a)
        with self.assertRaisesRegex(ops.Denied, 'DeploymentInterlock'): self.engine.dispatch('start-approved')
        with self.assertRaisesRegex(ops.Denied, 'DeploymentInterlock'): self.engine.dispatch('backup-approved')

    def test_backup_foreign_key_error_is_incomplete(self):
        self.live[0].execute('INSERT INTO child VALUES(2,999)')
        self.live[0].commit()
        self.approval('backup-approved', runtime=self.engine.identity(self.container), revision=REV)
        with self.assertRaisesRegex(support.Blocked, 'ForeignKeysFailed'): self.engine.dispatch('backup-approved')
        manifest = json.loads(next(self.backups.iterdir()).joinpath('manifest.json').read_text())
        self.assertEqual(manifest['status'], 'INCOMPLETE')

    def test_nonregular_file_rejected_without_blocking(self):
        path = self.config / 'pipe'
        os.mkfifo(path, 0o600)
        with self.assertRaises(ops.Denied): self.fs.read(path)

    def test_support_hash_matches_checked_in_core(self):
        self.assertEqual(ops.SUPPORT_SHA256, ops.sha((ROOT / 'scripts/approved-ops/backup_support.py').read_bytes()))

    def test_subprocess_cleans_environment_and_bounds_output_time(self):
        with patch.dict(os.environ, {'EVIL_CALLER_VALUE': 'must-not-inherit'}):
            result = ops.command([sys.executable, '-I', '-c', 'import os; print(os.environ.get("EVIL_CALLER_VALUE", "clean"))'])
        self.assertEqual(result.strip(), b'clean')
        with self.assertRaisesRegex(ops.Denied, 'CommandOutputLimit'):
            ops.command([sys.executable, '-I', '-c', 'import os; os.write(1,b"x"*5000000)'])
        with self.assertRaisesRegex(ops.Denied, 'CommandTimeout'):
            ops.command([sys.executable, '-I', '-c', 'import time; time.sleep(5)'], timeout=0.1)

    def test_cli_error_has_no_paths_or_traceback(self):
        result = subprocess.run([sys.executable, '-I', str(ROOT / 'scripts/approved-ops/bigbrain-approved-ops'), 'status', 'secret'],
                                capture_output=True, text=True)
        self.assertEqual(result.returncode, 1)
        self.assertNotIn('secret', result.stderr)
        self.assertNotIn('Traceback', result.stderr)
        self.assertEqual(json.loads(result.stderr)['status'], 'BLOCKED')


if __name__ == '__main__':
    unittest.main()
