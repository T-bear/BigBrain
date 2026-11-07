#!/usr/bin/env python3
"""Unprivileged synthetic regression tests. No systemd, sudo or Finance inputs.

Identity/ownership, mounts and systemd are simulated; filesystem operations use
only TemporaryDirectory. This suite does not certify runtime isolation.
"""
import contextlib
import hashlib
import importlib.util
import io
import os
from pathlib import Path
import stat
import subprocess
import tempfile
from types import SimpleNamespace
import unittest
from unittest.mock import patch

SOURCE = Path(__file__).with_name('finance-release1-start-package-20261106.py')
spec = importlib.util.spec_from_file_location('review_package', SOURCE)
package = importlib.util.module_from_spec(spec)
spec.loader.exec_module(package)
guard = {'__name__': 'synthetic_guard'}
exec(compile(package.GUARD, '<guard>', 'exec'), guard)
SUCCESS = b'PASS synthetic\nNOT CHECKED closure_directory_unavailable\nSAMMANFATTNING: 1/0/1\n'


class SyntheticTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory(prefix='finance-start-test-')
        self.addCleanup(self.tmp.cleanup)
        self.base = Path(self.tmp.name)
        self.outside = self.base / 'unrelated-sentinel'
        self.outside.write_bytes(b'keep-exactly')
        self.before = self.outside.stat()
        self.real_lstat = os.lstat

    def tearDown(self):
        self.assertEqual(self.outside.read_bytes(), b'keep-exactly')
        after = self.outside.stat()
        self.assertEqual((after.st_ino, after.st_mode, after.st_mtime_ns),
                         (self.before.st_ino, self.before.st_mode, self.before.st_mtime_ns))

    def root_stat(self, path, *args, **kwargs):
        s = self.real_lstat(path, *args, **kwargs)
        self.assertTrue(Path(path).is_relative_to(self.base))
        return SimpleNamespace(**{name: (0 if name == 'st_uid' else getattr(s, name))
                                  for name in dir(s) if name.startswith('st_')})

    def make_layout(self):
        stage = self.base / 'stage'
        stage.mkdir(mode=0o700)
        root = stage / 'root'
        root.mkdir()
        files, dirs = package.layout()
        for rel in sorted(dirs - {'/'}, key=lambda s: s.count('/')):
            (root / rel.lstrip('/')).mkdir()
        for rel in files:
            (root / rel.lstrip('/')).write_bytes(b'script' if rel == '/verify.py' else b'')
        return stage, root

    def clean(self, stage):
        with patch.object(Path, 'lstat', lambda p: self.root_stat(p)):
            package.cleanup_tree(stage)

    def test_cleanup_includes_four_reported_directories(self):
        stage, root = self.make_layout()
        for rel in package.EMPTY_SKELETON:
            self.assertTrue((root / rel.lstrip('/')).is_dir())
        self.clean(stage)
        self.assertFalse(stage.exists())

    def test_partial_setup_cleanup(self):
        stage = self.base / 'stage'
        stage.mkdir()
        self.clean(stage)
        self.assertFalse(stage.exists())
        stage.mkdir()
        (stage / 'root').mkdir()
        (stage / 'root/etc').mkdir()
        self.clean(stage)
        self.assertFalse(stage.exists())

    def test_unknown_content_preserved_before_any_unlink(self):
        stage, root = self.make_layout()
        (root / 'var/unexpected').write_bytes(b'keep')
        with self.assertRaises(package.CleanupFailure) as error:
            self.clean(stage)
        self.assertEqual(error.exception.step, 'C05_inventory')
        self.assertEqual((root / 'verify.py').read_bytes(), b'script')
        self.assertEqual((root / 'var/unexpected').read_bytes(), b'keep')

    def test_cleanup_rejects_symlink(self):
        stage, root = self.make_layout()
        (root / 'etc').rmdir()
        (root / 'etc').symlink_to(self.base, target_is_directory=True)
        with self.assertRaises(package.CleanupFailure) as error:
            self.clean(stage)
        self.assertEqual(error.exception.step, 'C05_inventory')
        self.assertTrue((root / 'verify.py').exists())

    def test_cleanup_rejects_nonempty_placeholder(self):
        stage, root = self.make_layout()
        (root / 'finance' / package.DATABASE).write_bytes(b'not-a-mountpoint')
        with self.assertRaises(package.CleanupFailure) as error:
            self.clean(stage)
        self.assertEqual(error.exception.step, 'C05_inventory')
        self.assertTrue((root / 'verify.py').exists())

    def test_active_unit_blocks_all_removal(self):
        stage, root = self.make_layout()
        token = 'a' * 32
        dest = self.base / ('owned-' + token)
        stage.rename(dest)
        with patch.object(package, 'PREFIX', str(self.base / 'owned-')), \
             patch.object(package, 'run_tool') as calls, \
             patch.object(package, 'properties', return_value={
                 'LoadState': 'loaded', 'ActiveState': 'active', 'MainPID': '123'}):
            with self.assertRaises(package.CleanupFailure) as error:
                package.stop_and_cleanup(token)
        self.assertEqual(error.exception.step, 'C02_stopped')
        self.assertTrue((dest / 'root/verify.py').exists())
        self.assertEqual(calls.call_args.args[0], ['/usr/bin/systemctl', 'stop',
                         'finance-release1-' + token + '.service'])

    def test_empty_skeleton_guard(self):
        root = self.base / 'guest'
        root.mkdir()
        identities = {}
        for rel in package.EMPTY_SKELETON:
            p = root / rel.lstrip('/')
            p.mkdir(mode=0o555)
            p.chmod(0o555)
            s = p.stat()
            identities[rel] = [s.st_dev, s.st_ino]
        with patch.object(guard['os'], 'lstat', side_effect=lambda p: self.root_stat(Path(p))), \
             patch.object(guard['os'], 'statvfs', return_value=SimpleNamespace(f_flag=os.ST_RDONLY)):
            guard['check_skeleton']({'empty_skeleton': identities}, str(root))
            for rel in package.EMPTY_SKELETON:
                p = root / rel.lstrip('/')
                p.chmod(0o755)
                (p / 'unexpected').write_bytes(b'synthetic')
                p.chmod(0o555)
                with self.subTest(directory=rel), self.assertRaises(RuntimeError):
                    guard['check_skeleton']({'empty_skeleton': identities}, str(root))
                self.assertEqual(guard['STEP'], 'G06_empty_' + rel[1:] + '_empty')
                p.chmod(0o755)
                (p / 'unexpected').unlink()
                p.chmod(0o555)
            changed = dict(identities)
            changed['/etc'] = [0, 0]
            with self.assertRaises(RuntimeError):
                guard['check_skeleton']({'empty_skeleton': changed}, str(root))
            with patch.object(guard['os'], 'statvfs', return_value=SimpleNamespace(f_flag=0)):
                with self.assertRaises(RuntimeError):
                    guard['check_skeleton']({'empty_skeleton': identities}, str(root))
            p = root / 'bin'
            p.chmod(0o755)
            p.rmdir()
            p.symlink_to(self.outside)
            with self.assertRaises(RuntimeError):
                guard['check_skeleton']({'empty_skeleton': identities}, str(root))
            self.assertEqual(guard['STEP'], 'G06_empty_bin_type')
            p.unlink()
            p.mkdir()
        for p in root.iterdir():
            p.chmod(0o755)

    def test_ready_failure_is_sanitized(self):
        for line, expected in ((b'FAIL G06_empty_var_readonly\n', package.GuardFailure),
                               (b'FAIL /private/example secret\n', RuntimeError)):
            read_fd, write_fd = os.pipe()
            with os.fdopen(read_fd, 'rb', buffering=0) as stream:
                os.write(write_fd, line)
                os.close(write_fd)
                with self.assertRaises(expected) as error:
                    package.wait_ready(stream)
            self.assertNotIn('/private', str(error.exception))
            if isinstance(error.exception, package.GuardFailure):
                self.assertEqual(error.exception.step, 'G06_empty_var_readonly')

    def test_g06_each_subcondition_and_protocol(self):
        # Same protected baseline, one failing predicate/operation at a time.
        root = self.base / 'diagnostic-fixture'
        root.mkdir(mode=0o555)
        root.chmod(0o555)
        baseline = self.root_stat(root)
        expected = [baseline.st_dev, baseline.st_ino]
        conditions = ('lstat', 'type', 'owner', 'mode', 'device', 'inode',
                      'statvfs', 'readonly', 'listdir', 'empty')
        for rel in package.EMPTY_SKELETON:
            identities = {rel: expected, **{p: expected for p in package.EMPTY_SKELETON if p != rel}}
            for condition in conditions:
                with self.subTest(directory=rel, condition=condition):
                    observed = SimpleNamespace(**vars(baseline))
                    if condition == 'type': observed.st_mode = stat.S_IFLNK | 0o555
                    if condition == 'owner': observed.st_uid = 1654
                    if condition == 'mode': observed.st_mode = stat.S_IFDIR | 0o750
                    if condition == 'device': observed.st_dev += 1
                    if condition == 'inode': observed.st_ino += 1
                    private = '/private/synthetic-value-do-not-print'
                    with patch.object(guard['os'], 'lstat', return_value=observed) as metadata, \
                         patch.object(guard['os'], 'statvfs', return_value=SimpleNamespace(
                             f_flag=0 if condition == 'readonly' else os.ST_RDONLY)) as flags, \
                         patch.object(guard['os'], 'listdir', return_value=(
                             [private] if condition == 'empty' else [])) as entries:
                        for name, operation in (('lstat', metadata), ('statvfs', flags), ('listdir', entries)):
                            if condition == name:
                                operation.side_effect = PermissionError(private)
                        with self.assertRaises((RuntimeError, OSError)):
                            guard['check_skeleton']({'empty_skeleton': identities}, str(root))
                    step = 'G06_empty_' + rel[1:] + '_' + condition
                    self.assertEqual(guard['STEP'], step)
                    self.assertIn(step, package.GUARD_STEPS)
                    read_fd, write_fd = os.pipe()
                    with os.fdopen(read_fd, 'rb', buffering=0) as stream:
                        os.write(write_fd, ('FAIL ' + step + '\n').encode('ascii'))
                        os.close(write_fd)
                        with self.assertRaises(package.GuardFailure) as failure:
                            package.wait_ready(stream)
                    self.assertEqual(failure.exception.step, step)
                    self.assertNotIn(private, str(failure.exception))
        root.chmod(0o755)

    def test_g06_invalid_schema_is_sanitized(self):
        with self.assertRaises(RuntimeError):
            guard['check_skeleton']({'empty_skeleton': {'/private/unexpected': [0, 0]}})
        self.assertEqual(guard['STEP'], 'G06_spec')
        self.assertIn('G06_spec', package.GUARD_STEPS)

    def test_prepared_probe_payloads_without_systemd(self):
        path = SOURCE.with_name('finance-release1-systemd-probe-20261107.py')
        probe_spec = importlib.util.spec_from_file_location('review_probe', path)
        probe = importlib.util.module_from_spec(probe_spec)
        probe_spec.loader.exec_module(probe)
        self.assertEqual(probe.PACKAGE_HASH, hashlib.sha256(SOURCE.read_bytes()).hexdigest())
        for name in package.FILES:
            (self.base / name).write_bytes(b'synthetic-only\n')
        for case, expected in (('normal', 0), ('failure', 1)):
            namespace = {'__name__': 'synthetic_payload'}
            exec(compile(probe.probe_source(case, package.FILES), '<synthetic>', 'exec'), namespace)
            output = io.StringIO()
            with contextlib.redirect_stdout(output):
                result = namespace['main'](['--db', str(self.base / package.DATABASE),
                                            '--manifest-dir', str(self.base)])
            self.assertEqual(result, expected)
            self.assertIn('NOT CHECKED closure_directory_unavailable', output.getvalue())

    def exercise_start(self, failure=None):
        inputs = self.base / 'inputs'
        inputs.mkdir()
        for name in package.FILES:
            (inputs / name).write_bytes(b'synthetic-source')
        script = self.base / 'probe.py'
        script.write_bytes(b'# synthetic, not executed\n')
        runtime = self.base / 'runtime'
        runtime.mkdir()
        (runtime / 'python').write_bytes(b'synthetic, not executed')
        original_pin = package.Pins.pin
        real_chmod = os.chmod
        real_unlink = Path.unlink
        command_settings = {}
        observed = {'properties_checked': False, 'go': False}
        fake = SimpleNamespace(returncode=None, stdin=io.BytesIO())
        read_fd, write_fd = os.pipe()
        fake.stdout = os.fdopen(read_fd, 'rb', buffering=0)
        self.addCleanup(fake.stdout.close)
        os.write(write_fd, b'FAIL G06_empty_root_readonly\n' if failure == 'guard' else b'READY identity_hash_isolation\n')
        os.close(write_fd)

        def spawn(command, **kwargs):
            self.assertEqual(command[0], '/usr/bin/systemd-run')
            command_settings.update(x.removeprefix('--property=').split('=', 1)
                                    for x in command if x.startswith('--property='))
            self.assertEqual(command_settings['ReadOnlyPaths'], '/root')
            for binding in command_settings['BindReadOnlyPaths'].split():
                source, destination, option = binding.split(':')
                self.assertTrue(source.startswith('/proc/'))
                self.assertFalse(destination == '/root' or destination.startswith('/root/'))
                self.assertEqual(option, 'norbind')
            return fake

        def communicate(input, timeout):
            self.assertEqual(input, b'GO\n')
            self.assertTrue(observed['properties_checked'])
            observed['go'] = True
            if failure == 'timeout':
                raise subprocess.TimeoutExpired('/private/do-not-print', timeout)
            fake.returncode = 1 if failure == 'verifier' else 0
            if failure == 'source_change':
                (inputs / package.DATABASE).write_bytes(b'changed-synthetic')
            return (b'FAIL synthetic\nSAMMANFATTNING: 0/1/0\n' if failure == 'verifier' else SUCCESS), None

        def properties(unit, keys):
            if keys == ['LoadState'] or keys == ['LoadState', 'ActiveState', 'MainPID']:
                return {'LoadState': 'not-found'}
            result = {key: command_settings.get(key) for key in keys}
            result.update(MemoryMax='536870912', RuntimeMaxUSec='5min', TimeoutStartUSec='30s',
                          TimeoutStopUSec='5s', CPUQuotaPerSecUSec='1s')
            self.assertIn('ReadOnlyPaths', keys)
            observed['properties_checked'] = True
            if failure == 'readonly_missing':
                result.pop('ReadOnlyPaths')
            elif failure == 'readonly_wrong':
                result['ReadOnlyPaths'] = '/elsewhere'
            return result

        def unlink(path, *args, **kwargs):
            if failure == 'unlink' and path.name == 'verify.py':
                raise PermissionError('/private/do-not-print')
            return real_unlink(path, *args, **kwargs)

        fake.communicate = communicate
        fake.poll = lambda: fake.returncode
        fake.terminate = lambda: setattr(fake, 'returncode', -15)
        fake.wait = lambda **kw: fake.returncode
        fake.kill = fake.terminate
        output = io.StringIO()
        with contextlib.ExitStack() as stack:
            for target, name, replacement in (
                (package, 'PREFIX', str(self.base / 'owned-')),
                (package, 'APPROVED', hashlib.sha256(script.read_bytes()).hexdigest()),
                (package, 'RUNTIME', ((str(runtime / 'python'), '/usr/bin/python3.13', False),)),
                (package.Pins, 'pin', lambda obj, p, directory=False, trusted=False:
                 original_pin(obj, str(self.base) if p == '/run' else p, directory, False)),
                (package, 'properties', properties), (package, 'run_tool', lambda *a, **k: None),
                (package, 'check_resource_limits', lambda unit: None),
                (package.subprocess, 'Popen', spawn),
                (os, 'getuid', lambda: 0), (os, 'geteuid', lambda: 0),
                (os, 'fchown', lambda *args: None),
                (os, 'chmod', lambda p, mode: real_chmod(p, 0o755 if mode == 0o555 else mode)),
                (Path, 'lstat', lambda p: self.root_stat(p)), (Path, 'unlink', unlink),
            ):
                stack.enter_context(patch.object(target, name, replacement))
            with contextlib.redirect_stdout(output):
                rc = package.start(str(script), str(inputs))
        text = output.getvalue()
        self.assertNotIn('/private', text)
        self.assertNotIn(str(self.base), text)
        self.assertEqual(sorted(inputs.iterdir()), sorted(inputs / name for name in package.FILES))
        self.assertEqual(observed['go'], failure not in ('guard', 'readonly_missing', 'readonly_wrong'))
        return rc, text, list(self.base.glob('owned-*'))

    def test_start_normal_cleanup(self):
        rc, text, remaining = self.exercise_start()
        self.assertEqual(rc, 0)
        self.assertIn('PASS temporary_resources_removed', text)
        self.assertEqual(remaining, [])

    def test_start_guard_failure_cleanup(self):
        rc, text, remaining = self.exercise_start('guard')
        self.assertEqual(rc, 1)
        self.assertIn('FAIL G06_empty_root_readonly', text)
        self.assertNotIn('PASS identity_hash', text)
        self.assertEqual(remaining, [])

    def test_start_missing_readonly_property_blocks_go_and_cleans_up(self):
        rc, text, remaining = self.exercise_start('readonly_missing')
        self.assertEqual(rc, 1)
        self.assertIn('FAIL S09_service_properties', text)
        self.assertNotIn('PASS identity_hash', text)
        self.assertIn('PASS temporary_resources_removed', text)
        self.assertEqual(remaining, [])

    def test_start_wrong_readonly_property_blocks_go_and_cleans_up(self):
        rc, text, remaining = self.exercise_start('readonly_wrong')
        self.assertEqual(rc, 1)
        self.assertIn('FAIL S09_service_properties', text)
        self.assertNotIn('PASS identity_hash', text)
        self.assertIn('PASS temporary_resources_removed', text)
        self.assertEqual(remaining, [])

    def test_start_timeout_cleanup(self):
        rc, text, remaining = self.exercise_start('timeout')
        self.assertEqual(rc, 1)
        self.assertIn('FAIL S12_execution', text)
        self.assertEqual(remaining, [])

    def test_start_verifier_failure_cleanup(self):
        rc, text, remaining = self.exercise_start('verifier')
        self.assertEqual(rc, 1)
        self.assertIn('FAIL synthetic', text)
        self.assertEqual(remaining, [])

    def test_start_source_change_cleanup(self):
        rc, text, remaining = self.exercise_start('source_change')
        self.assertEqual(rc, 1)
        self.assertIn('FAIL S13_originals_after_execution', text)
        self.assertEqual(remaining, [])

    def test_cleanup_failure_is_reported_and_recoverable(self):
        rc, text, remaining = self.exercise_start('unlink')
        self.assertEqual(rc, 1)
        self.assertIn('FAIL C06_remove_files cleanup_unconfirmed RESOURCE_ID=', text)
        self.assertEqual(len(remaining), 1)
        self.clean(remaining[0])
        self.assertFalse(remaining[0].exists())


if __name__ == '__main__':
    if os.getuid() == 0:
        raise SystemExit('Refusing root: these tests require an ordinary user.')
    unittest.main()
