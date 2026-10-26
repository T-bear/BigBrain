"""Synthetic partial installations only; all privileged/host commands are mocked."""
import base64
import contextlib
import copy
import importlib.util
import io
import json
import os
from pathlib import Path
import subprocess
import tempfile
import unittest
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[2]
PACKAGE = ROOT/'scripts/approved-ops-install'
spec = importlib.util.spec_from_file_location('recovery', PACKAGE/'recover.py')
r = importlib.util.module_from_spec(spec); spec.loader.exec_module(r)
h = r.load_helper(PACKAGE)


def basic_policy():
    return {'Defaults': [{'Options': [{'env_reset': True}]}], 'User_Specs': [{
        'User_List': [{'usergroup': 'sudo'}], 'Host_List': [{'hostname': 'ALL'}],
        'Cmnd_Specs': [{'runasusers': [{'username': 'ALL'}], 'runasgroups': [{'usergroup': 'ALL'}],
                        'Options': [{'setenv': True}], 'Commands': [{'command': 'ALL'}]}]}]}


class RecoveryTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.original = {name: subprocess.check_output([
            'git', 'show', r.ORIGINAL_COMMIT+':scripts/approved-ops-install/'+name], cwd=ROOT)
                        for name in h.FILES | {'manifest.json'}}

    def run_case(self, change=None, drift=None, interruption=None):
        with tempfile.TemporaryDirectory() as td, contextlib.ExitStack() as stack:
            root = Path(td); stage = root/'recovery'; stage.mkdir(mode=0o700)
            old = root/'original'; old.mkdir(mode=0o700)
            for n in r.NAMES | {'recovery-manifest.json'}: (stage/n).write_bytes((PACKAGE/n).read_bytes())
            for n, b in self.original.items(): (old/n).write_bytes(b)
            progress = {'status': 'BLOCKED', 'phase': 'A-installing', 'sourceRevision': r.SOURCE,
                        'systemdAcceptance': 'NOT RUN', 'productionOperations': 'NOT RUN'}
            (old/'progress.json').write_text(json.dumps(progress))
            (old/'result.json').write_text(json.dumps({**progress, 'reason': 'PhaseADrift'}))
            paths = {'WRAPPER': root/'sbin'/'wrapper', 'LIB': root/'lib'/'wrapper',
                     'CONFIG': root/'etc'/'wrapper', 'STATE': root/'state'/'wrapper',
                     'SUDOERS': root/'sudoers.d'/'wrapper', 'BACKUPS': root/'backups'}
            for p in paths.values(): p.parent.mkdir(parents=True, exist_ok=True)
            for name, p in paths.items(): stack.enter_context(patch.object(h, name, p))
            for p in (h.LIB, h.CONFIG, h.CONFIG/'packages', h.CONFIG/'docker',
                      h.STATE, h.STATE/'claims', h.STATE/'runs', h.BACKUPS): p.mkdir(mode=0o700)
            h.WRAPPER.write_bytes((PACKAGE/'bigbrain-approved-ops').read_bytes())
            (h.LIB/'backup_support.py').write_bytes((PACKAGE/'backup_support.py').read_bytes())
            (h.STATE/'lock').write_bytes(b'')
            (h.BACKUPS/'sentinel').write_bytes(b'untouched')
            stack.enter_context(patch.object(r, 'ORIGINAL_STAGE', old))
            stack.enter_context(patch.object(r, 'require_recovery_stage'))
            stack.enter_context(patch.object(r, 'environment_checks'))
            stack.enter_context(patch.object(h, 'secure'))
            policy = {'existing': ('digest', 0o440, 0, 0)}
            expected = {**policy, str(h.SUDOERS): (h.PINNED['sudoers.example'], 0o440, 0, 0)}
            stack.enter_context(patch.object(h, 'policy_snapshot', side_effect=[
                policy, {} if drift == 'policy' else policy, {} if drift == 'policy-after' else expected]))
            container = {'Id': 'synthetic', 'Image': 'image', 'Config': {}, 'HostConfig': {},
                         'Mounts': [{'Source': '/a', 'Destination': '/a', 'RW': True},
                                    {'Source': '/b', 'Destination': '/b', 'RW': False}],
                         'State': {'StartedAt': 'start', 'Running': True}, 'RestartCount': 0}
            calls = []; inspections = 0
            def answer(args, **kwargs):
                nonlocal inspections
                calls.append(args)
                if args[:2] == ['/usr/bin/docker', 'ps']:
                    return subprocess.CompletedProcess(args, 0, b'synthetic', b'')
                if args[:2] == ['/usr/bin/docker', 'inspect']:
                    c = copy.deepcopy(container); inspections += 1
                    if inspections > 1:
                        c['Mounts'].reverse()
                        if drift == 'runtime' or (drift == 'runtime-after' and inspections == 3):
                            c['Mounts'][0]['RW'] = True
                    return subprocess.CompletedProcess(args, 0, json.dumps([c]).encode(), b'')
                if args[0] == '/usr/bin/cvtsudoers':
                    doc = basic_policy()
                    if drift == 'unknown-policy': doc['Defaults'][0]['Options'] = [{'authenticate': False}]
                    return subprocess.CompletedProcess(args, 0, json.dumps(doc).encode(), b'')
                if args == ['/usr/bin/sudo', '-l', '-U', 'enigma'] and interruption == 'after-rule':
                    raise KeyboardInterrupt()
                if str(h.WRAPPER) in args:
                    is_sudo = '/usr/bin/sudo' in args
                    if is_sudo and (not h.SUDOERS.exists() or args[-1] in ('unknown', 'extra')
                                    or '-E' in args or 'PYTHONPATH=/tmp' in args):
                        return subprocess.CompletedProcess(args, 1, b'', b'sudo: a password is required')
                    if args[-1] == 'status':
                        return subprocess.CompletedProcess(args, 0,
                            b'{"operation":"status","deployment":"NO_WRAPPER_RUN"}', b'')
                    return subprocess.CompletedProcess(args, 1, b'', b'{"status": "BLOCKED"}')
                if args[-1] == '/usr/bin/true':
                    return subprocess.CompletedProcess(args, 1, b'', b'sudo: a password is required')
                return subprocess.CompletedProcess(args, 0, b'', b'')
            stack.enter_context(patch.object(h, 'run', side_effect=answer))
            if change: change(old, stage, h)
            original_write = h.write_new
            def write(path, data, mode):
                if path.name == 'phase-a-pass.json' and interruption == 'before-rule': raise KeyboardInterrupt()
                original_write(path, data, mode)
            stack.enter_context(patch.object(h, 'write_new', side_effect=write))
            output = io.StringIO()
            with contextlib.redirect_stdout(output):
                rc = r.recover(h, stage, h.digest((PACKAGE/'recovery-manifest.json').read_bytes()))
            report = json.loads(output.getvalue().splitlines()[0])
            if (stage/'result.json').exists(): self.assertEqual(report, json.loads((stage/'result.json').read_bytes()))
            self.assertEqual((h.BACKUPS/'sentinel').read_bytes(), b'untouched')
            self.assertEqual((old/'install.py').read_bytes(), self.original['install.py'])
            self.assertFalse(any('systemd-run' in arg for args in calls for arg in args))
            self.assertTrue(all(args[1] in ('ps', 'inspect', 'compose') for args in calls if args[0] == '/usr/bin/docker'))
            return rc, report, h.SUDOERS.exists(), (stage/'phase-a-pass.json').exists(), (old/'recovery-attempt.json').exists()

    def test_correct_partial_install_resumes_with_reordered_mounts(self):
        rc, report, rule, phase_a, attempted = self.run_case()
        self.assertEqual(rc, 0); self.assertEqual(report['status'], 'PASS')
        self.assertEqual(report['historicalDrift'], 'UNKNOWN')
        self.assertEqual(report['acceptance'], 'CURRENT_STATE_ONLY')
        self.assertTrue(rule and phase_a and attempted)
        self.assertEqual(report['productionOperations'], 'NOT RUN')

    def test_installed_hashes_rejected(self):
        for name in ('wrapper', 'support'):
            def change(old, stage, m):
                (m.WRAPPER if name == 'wrapper' else m.LIB/'backup_support.py').write_bytes(b'tampered')
            with self.subTest(name=name):
                rc, report, rule, phase_a, _ = self.run_case(change)
                self.assertEqual(report['reason'], 'InstalledHash'); self.assertEqual(rc, 1)
                self.assertFalse(rule or phase_a)

    def test_missing_altered_or_inconsistent_history_rejected(self):
        changes = [lambda o,s,m: (o/'result.json').unlink(),
                   lambda o,s,m: (o/'manifest.json').write_bytes(b'changed'),
                   lambda o,s,m: (o/'sudoers.example').write_bytes(b'changed'),
                   lambda o,s,m: (o/'result.json').write_text('{}'),
                   lambda o,s,m: (o/'result.json').write_text('{"status":"PASS","status":"BLOCKED"}'),
                   lambda o,s,m: (o/'progress.json').write_text('{}'),
                   lambda o,s,m: (o/'phase-a-pass.json').write_text('{}'),
                   lambda o,s,m: (o/'recovery-attempt.json').write_text('{}')]
        for index, change in enumerate(changes):
            with self.subTest(index=index):
                rc, _, rule, phase_a, _ = self.run_case(change)
                self.assertEqual(rc, 1); self.assertFalse(rule or phase_a)

    def test_existing_sudoers_or_pending_rule_rejected_without_overwrite(self):
        for pending in (False, True):
            def change(o,s,m):
                (m.SUDOERS.parent/'.bigbrain-approved-ops.installing' if pending else m.SUDOERS).write_bytes(b'existing')
            rc, report, rule, phase_a, _ = self.run_case(change)
            self.assertEqual(rc, 1); self.assertEqual(report['reason'], 'DestinationExists')
            self.assertFalse(phase_a); self.assertEqual(rule, not pending)

    def test_approvals_claims_runs_active_and_unexpected_state_rejected(self):
        for location in ('approval', 'claim', 'run', 'active', 'package', 'docker', 'library', 'lock'):
            def change(o,s,m):
                dest = {'approval': m.CONFIG/'start-approved.json', 'claim': m.STATE/'claims'/'claim',
                        'run': m.STATE/'runs'/'run', 'active': m.STATE/'active.json',
                        'package': m.CONFIG/'packages'/'package', 'docker': m.CONFIG/'docker'/'config.json',
                        'library': m.LIB/'extra.py', 'lock': m.STATE/'lock'}[location]
                dest.write_bytes(b'not allowed')
            with self.subTest(location=location):
                rc, _, rule, phase_a, _ = self.run_case(change)
                self.assertEqual(rc, 1); self.assertFalse(rule or phase_a)

    def test_current_policy_and_runtime_changes_block_before_b(self):
        for drift in ('policy', 'runtime', 'unknown-policy'):
            with self.subTest(drift=drift):
                rc, _, rule, phase_a, _ = self.run_case(drift=drift)
                self.assertEqual(rc, 1); self.assertFalse(rule or phase_a)

    def test_post_activation_drift_is_blocked_without_rollback(self):
        for drift in ('policy-after', 'runtime-after'):
            rc, report, rule, phase_a, _ = self.run_case(drift=drift)
            self.assertEqual(rc, 1); self.assertEqual(report['status'], 'BLOCKED')
            self.assertTrue(rule and phase_a)

    def test_interruptions_preserve_attempt_and_partial_state(self):
        for interruption in ('before-rule', 'after-rule'):
            rc, report, rule, phase_a, attempted = self.run_case(interruption=interruption)
            self.assertEqual(rc, 1); self.assertEqual(report['reason'], 'KeyboardInterrupt')
            self.assertTrue(attempted); self.assertEqual(rule, interruption == 'after-rule')
            self.assertEqual(phase_a, interruption == 'after-rule')

    def test_concurrent_recovery_cannot_acquire_installer_lock(self):
        fd = None
        def change(o,s,m):
            nonlocal fd
            fd = os.open(m.WRAPPER.parent, os.O_RDONLY | os.O_DIRECTORY)
            h.fcntl.flock(fd, h.fcntl.LOCK_EX | h.fcntl.LOCK_NB)
        try:
            rc, report, rule, phase_a, attempted = self.run_case(change)
            self.assertEqual(rc, 1); self.assertEqual(report['reason'], 'BlockingIOError')
            self.assertFalse(rule or phase_a or attempted)
        finally:
            if fd is not None: os.close(fd)

    def test_wrapper_operation_lock_blocks_recovery(self):
        fd = None
        def change(o,s,m):
            nonlocal fd
            fd = os.open(m.STATE/'lock', os.O_RDONLY)
            h.fcntl.flock(fd, h.fcntl.LOCK_EX | h.fcntl.LOCK_NB)
        try:
            rc, report, rule, phase_a, _ = self.run_case(change)
            self.assertEqual(rc, 1); self.assertEqual(report['reason'], 'BlockingIOError')
            self.assertFalse(rule or phase_a)
        finally:
            if fd is not None: os.close(fd)

    def test_secure_path_denial_prevents_phase_b(self):
        def change(o,s,m):
            def secure(path, mode=None):
                if Path(path) == m.WRAPPER: raise m.Blocked('UnsafePath')
            m.secure.side_effect = secure
        rc, report, rule, phase_a, _ = self.run_case(change)
        self.assertEqual(report['reason'], 'UnsafePath'); self.assertEqual(rc, 1)
        self.assertFalse(rule or phase_a)


class PackageAndPolicyTests(unittest.TestCase):
    def test_recovery_manifest_and_pinned_helper(self):
        manifest = (PACKAGE/'recovery-manifest.json').read_bytes()
        r.verify_bundle(h, manifest, {n: (PACKAGE/n).read_bytes() for n in r.NAMES}, h.digest(manifest))
        self.assertEqual(h.digest((PACKAGE/'install.py').read_bytes()), r.HELPER_HASH)

    def test_invalid_bundle_or_traversal_cannot_launch_sudo(self):
        manifest = (PACKAGE/'recovery-manifest.json').read_bytes()
        blobs = {n: (PACKAGE/n).read_bytes() for n in r.NAMES}
        for bad in ({**blobs, '../escape': b'x'}, {**blobs, 'recover.py': b'bad'}, {}):
            with self.assertRaises(h.Blocked): r.verify_bundle(h, manifest, bad, h.digest(manifest))
        with patch.object(r.os, 'geteuid', return_value=1000), patch.object(h.subprocess, 'run') as run:
            with self.assertRaises(Exception): r.launch(PACKAGE, '0'*64)
            run.assert_not_called()

    def test_launch_captures_verified_bytes_and_uses_interactive_sudo_only(self):
        manifest = (PACKAGE/'recovery-manifest.json').read_bytes()
        with patch('builtins.open', unittest.mock.mock_open()), patch.object(r.os, 'geteuid', return_value=1000), \
             patch.object(h.subprocess, 'run', return_value=subprocess.CompletedProcess([], 0)) as run:
            self.assertEqual(r.launch(PACKAGE, h.digest(manifest)), 0)
            args = run.call_args.args[0]
            self.assertEqual(args[:5], ['/usr/bin/sudo', '-k', '--', '/usr/bin/python3.13', '-ISB'])
            self.assertNotIn('-S', args); self.assertNotIn(str(PACKAGE), args)
            bundle = json.loads(run.call_args.kwargs['input'])
            self.assertEqual(base64.b64decode(bundle['files']['recover.py']), (PACKAGE/'recover.py').read_bytes())

    def test_bad_root_bootstrap_bundle_stops_before_staging(self):
        p = subprocess.run(['/usr/bin/python3.13', '-ISB', '-c',
                            'import os\nos.geteuid=lambda:0\n'+r.BOOTSTRAP, '0'*64],
                           input=b'{"manifest":"eA==","files":{}}', capture_output=True)
        self.assertNotEqual(p.returncode, 0)
        self.assertIn(b'protected recovery staging failed', p.stderr)

    def test_root_entry_and_helper_symlink_denied(self):
        with patch.object(r.os, 'geteuid', return_value=1000), self.assertRaises(h.Blocked):
            r.require_recovery_stage(h, Path('/root/bigbrain-wrapper-recover-example'))
        with tempfile.TemporaryDirectory() as td:
            (Path(td)/'install.py').symlink_to(PACKAGE/'install.py')
            with self.assertRaises(OSError): r.load_helper(td)

    def test_conservative_policy_rejects_unknown_permissions_and_defaults(self):
        r.policy_acceptance(h, basic_policy())
        for option in ({'authenticate': False}, {'setenv': False}, {'exempt_group': 'sudo'}):
            doc = basic_policy(); doc['Defaults'][0]['Options'] = [option]
            with self.assertRaises(h.Blocked): r.policy_acceptance(h, doc)
        for field, value in [('Commands', [{'command': '/bin/sh'}]),
                             ('Options', [{'authenticate': False}]), ('unknown', True)]:
            doc = basic_policy(); doc['User_Specs'][0]['Cmnd_Specs'][0][field] = value
            with self.assertRaises(h.Blocked): r.policy_acceptance(h, doc)

    def test_standard_debian_policy_paths_accept_protected_usr_aliases(self):
        doc = basic_policy()
        doc['Defaults'].append({'Options': [{'secure_path': '/usr/bin:/bin:/sbin'}]})
        with patch.object(h, 'secure') as secure:
            r.policy_acceptance(h, doc)
            for p in ('/usr/bin', '/bin', '/sbin'):
                self.assertIn(unittest.mock.call(Path(p).resolve(strict=True)), secure.call_args_list)

    def test_real_converter_reads_included_policy_and_rejects_nopasswd(self):
        with tempfile.TemporaryDirectory() as td:
            p = Path(td)
            (p/'extra').write_text('enigma ALL=(root) NOPASSWD: /usr/bin/true\n')
            (p/'sudoers').write_text('%sudo ALL=(ALL:ALL) ALL\n@include '+str(p/'extra')+'\n')
            doc = json.loads(subprocess.check_output(['/usr/bin/cvtsudoers', '-c', '/dev/null', '-e', '-f', 'json', str(p/'sudoers')]))
            self.assertEqual(len(doc['User_Specs']), 2)
            with self.assertRaises(h.Blocked): r.policy_acceptance(h, doc)


if __name__ == '__main__': unittest.main()
