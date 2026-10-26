"""Offline only: synthetic filesystem and mocked privileged commands; never install on host."""
import base64
import contextlib
import copy
import io
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import subprocess
import tempfile
import unittest
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[2]
PACKAGE = ROOT/'scripts/approved-ops-install'
spec = importlib.util.spec_from_file_location('installer', PACKAGE/'install.py')
m = importlib.util.module_from_spec(spec)
spec.loader.exec_module(m)


class PackageTests(unittest.TestCase):
    def setUp(self):
        self.blobs = {n: (PACKAGE/n).read_bytes() for n in m.FILES}
        self.manifest = (PACKAGE/'manifest.json').read_bytes()
        self.hash = m.digest(self.manifest)

    def test_manifest_and_accepted_git_bytes(self):
        m.verify_bundle(self.manifest, self.blobs, self.hash)
        for n in m.PINNED:
            original = subprocess.check_output(['git', 'show', m.REVISION+':scripts/approved-ops/'+n], cwd=ROOT)
            self.assertEqual(original, self.blobs[n])

    def test_tamper_every_payload_and_manifest(self):
        for n in m.FILES:
            with self.subTest(n=n), self.assertRaises(m.Blocked):
                m.verify_bundle(self.manifest, {**self.blobs, n:self.blobs[n]+b'x'}, self.hash)
        with self.assertRaises(m.Blocked):
            m.verify_bundle(self.manifest+b' ', self.blobs, self.hash)

    def test_resealed_wrapper_and_source_revision_rejected(self):
        doc = json.loads(self.manifest)
        altered = {**self.blobs, 'bigbrain-approved-ops':b'changed'}
        doc['files']['bigbrain-approved-ops'] = m.digest(b'changed')
        raw=json.dumps(doc).encode()
        with self.assertRaises(m.Blocked): m.verify_bundle(raw, altered, m.digest(raw))
        doc=json.loads(self.manifest); doc['sourceRevision']='a'*40
        raw=json.dumps(doc).encode()
        with self.assertRaises(m.Blocked): m.verify_bundle(raw,self.blobs,m.digest(raw))

    def test_traversal_extra_missing_package_files_rejected(self):
        for name in ('../escape','/tmp/escape','other.py'):
            with self.subTest(name=name), self.assertRaises(m.Blocked):
                m.verify_bundle(self.manifest,{**self.blobs,name:b'bad'},self.hash)
        with self.assertRaises(m.Blocked): m.verify_bundle(self.manifest,{},self.hash)

    def test_symlink_hardlink_and_existing_target_rejected(self):
        with tempfile.TemporaryDirectory() as td:
            p=Path(td)/'source'; p.write_bytes(b'file')
            link=Path(td)/'symlink'; link.symlink_to(p)
            with self.assertRaises(OSError): m.read_file(link)
            os.link(p,Path(td)/'hardlink')
            with self.assertRaises(m.Blocked): m.read_file(p)
            with self.assertRaises(m.Blocked): m.absent(link)
            self.assertEqual(p.read_bytes(),b'file')

    def test_exclusive_install_never_overwrites(self):
        with tempfile.TemporaryDirectory() as td, patch.object(m,'secure'):
            p=Path(td)/'target'
            m.write_new(p,b'first',0o644)
            with self.assertRaises(FileExistsError): m.write_new(p,b'second',0o644)
            self.assertEqual(p.read_bytes(),b'first')
            self.assertEqual(p.stat().st_mode & 0o777,0o644)

    def test_sudoers_exact_four_literal_rules(self):
        raw=json.loads(subprocess.check_output(['/usr/bin/cvtsudoers','-f','json',str(PACKAGE/'sudoers.example')]))
        specs=raw['User_Specs']; self.assertEqual(len(specs),4)
        commands=[]
        for s in specs:
            self.assertEqual(s['User_List'],[{'username':'enigma'}])
            self.assertEqual(s['Host_List'],[{'hostname':'ALL'}])
            cs=s['Cmnd_Specs']; self.assertEqual(len(cs),1)
            self.assertEqual(cs[0]['runasusers'],[{'username':'root'}])
            self.assertEqual(cs[0]['Options'],[{'authenticate':False},{'setenv':False}])
            commands.extend(x['command'] for x in cs[0]['Commands'])
        self.assertEqual(commands,[str(m.WRAPPER)+' '+op for op in m.OPERATIONS])

    def test_root_bootstrap_rechecks_hash_before_staging(self):
        # Execute actual bootstrap with fake uid only; invalid input must fail before mkdtemp.
        for doc in ({'manifest':base64.b64encode(self.manifest).decode(),'files':{}},
                    {'manifest':base64.b64encode(b'wrong').decode(),'files':{}}):
            source='import os\nos.geteuid=lambda:0\n'+m.BOOTSTRAP
            p=subprocess.run(['/usr/bin/python3.13','-ISB','-c',source,self.hash],
                             input=json.dumps(doc).encode(),capture_output=True)
            self.assertNotEqual(p.returncode,0)
            self.assertIn(b'protected staging failed',p.stderr)

    def test_launch_sends_only_verified_bytes_and_sudo_uses_tty(self):
        fake=subprocess.CompletedProcess([],0)
        with patch('builtins.open',unittest.mock.mock_open()), patch.object(m,'absent'), \
             patch.object(m.os,'geteuid',return_value=1000), patch.object(m.subprocess,'run',return_value=fake) as run:
            self.assertEqual(m.launch(PACKAGE,self.hash),0)
            args=run.call_args.args[0]
            self.assertEqual(args[:5],['/usr/bin/sudo','-k','--',m.PYTHON,'-ISB'])
            self.assertNotIn('-S',args)
            self.assertNotIn(str(PACKAGE),args)
            bundle=json.loads(run.call_args.kwargs['input'])
            self.assertEqual(base64.b64decode(bundle['files']['install.py']),self.blobs['install.py'])

    def test_launch_invalid_manifest_never_calls_sudo(self):
        with patch.object(m.os,'geteuid',return_value=1000), patch.object(m.subprocess,'run') as run:
            with self.assertRaises(m.Blocked): m.launch(PACKAGE,'0'*64)
            run.assert_not_called()

    def test_cached_sudo_cannot_mask_negative_tests(self):
        calls=[]
        def denied(args,**kwargs):
            calls.append(args)
            return subprocess.CompletedProcess(args,1,b'',b'sudo: a password is required')
        with patch.object(m,'run',side_effect=denied): m.sudo_denials()
        self.assertEqual(len(calls),5)
        for args in calls:
            self.assertEqual(args[:7],['/usr/sbin/runuser','-u','enigma','--','/usr/bin/sudo','-k','-n'])
        for p in (subprocess.CompletedProcess([],0,b'',b''),
                  subprocess.CompletedProcess([],1,b'',b'{"status": "BLOCKED"}')):
            with patch.object(m,'run',return_value=p), self.assertRaises(m.Blocked): m.sudo_denials()

    def test_missing_approval_and_status_tests_no_production(self):
        def answer(args,**kwargs):
            if args[-1]=='status':
                return subprocess.CompletedProcess(args,0,b'{"operation":"status","deployment":"NO_WRAPPER_RUN"}',b'')
            return subprocess.CompletedProcess(args,1,b'',b'{"status":"BLOCKED"}')
        with patch.object(m,'no_authority'),patch.object(m,'run',side_effect=answer) as run:
            m.wrapper_tests(); m.wrapper_tests(as_user=True)
            self.assertEqual(run.call_count,8)
            for c in run.call_args_list: self.assertIn(str(m.WRAPPER),c.args[0])
        with patch.object(m,'no_authority'),patch.object(m,'run',return_value=subprocess.CompletedProcess([],1,b'',b'sudo: denied')):
            with self.assertRaises(json.JSONDecodeError):m.wrapper_tests(as_user=True)

    def test_acl_and_writable_or_symlink_chain_rejected(self):
        # /tmp writable ancestor is forbidden, regardless of leaf permissions.
        with tempfile.TemporaryDirectory() as td:
            with patch.object(m,'run',return_value=subprocess.CompletedProcess([],0,b'user::rwx\ngroup::r-x\nother::r-x\n',b'')):
                with self.assertRaises(m.Blocked):m.secure(Path(td))
        with patch.object(m,'run',return_value=subprocess.CompletedProcess([],0,b'user:enigma:rwx\n',b'')):
            with self.assertRaises(m.Blocked):m.secure(Path('/usr'))

    def test_no_approvals_or_claims_allowed(self):
        with tempfile.TemporaryDirectory() as td:
            cfg=Path(td)/'config'; state=Path(td)/'state'; cfg.mkdir(); state.mkdir()
            for d in (cfg/'docker',cfg/'packages',state/'claims',state/'runs'):d.mkdir()
            with patch.object(m,'CONFIG',cfg),patch.object(m,'STATE',state):
                m.no_authority()
                (cfg/'start-approved.json').write_text('{}')
                with self.assertRaises(m.Blocked):m.no_authority()

    def test_install_cannot_run_as_user_and_preserves_existing_host(self):
        with contextlib.redirect_stdout(io.StringIO()),patch.object(m.os,'geteuid',return_value=1000),patch.object(m,'write_new') as write:
            self.assertEqual(m.install(PACKAGE,self.hash),1)
            write.assert_not_called()


class RuntimeSnapshotTests(unittest.TestCase):
    def setUp(self):
        self.container = {'Id': 'synthetic-container', 'Image': 'synthetic-image',
                          'Config': {'Env': ['ONE=1', 'TWO=2']},
                          'HostConfig': {'Binds': ['one:/one', 'two:/two']},
                          'Mounts': [{'Type': 'volume', 'Name': 'one', 'Source': '/vol/one',
                                      'Destination': '/one', 'RW': True},
                                     {'Type': 'bind', 'Source': '/vol/two',
                                      'Destination': '/two', 'RW': False}],
                          'State': {'StartedAt': 'synthetic-start', 'Running': True},
                          'RestartCount': 0}

    def snapshot(self, containers):
        responses = [subprocess.CompletedProcess([], 0,
                     ' '.join(c['Id'] for c in containers).encode(), b'')]
        if containers:
            responses.append(subprocess.CompletedProcess([], 0, json.dumps(containers).encode(), b''))
        with patch.object(m, 'run', side_effect=responses) as run:
            result = m.runtime_snapshot()
            self.assertEqual(run.call_args_list[0].args[0],
                             ['/usr/bin/docker', 'ps', '-aq', '--no-trunc'])
            if containers:
                self.assertEqual(run.call_args_list[1].args[0],
                                 ['/usr/bin/docker', 'inspect', *[c['Id'] for c in containers]])
            return result

    def test_mount_and_container_order_do_not_change_identity(self):
        other = copy.deepcopy(self.container); other['Id'] = 'second'
        before = self.snapshot([self.container, other])
        changed = copy.deepcopy(self.container); changed['Mounts'].reverse()
        self.assertEqual(before, self.snapshot([other, changed]))

    def test_every_mount_field_and_membership_remain_protected(self):
        before = self.snapshot([self.container])
        for field, value in [('Type', 'bind'), ('Name', 'different'), ('Source', '/different'),
                             ('Destination', '/different'), ('RW', False), ('NewField', 'value')]:
            changed = copy.deepcopy(self.container); changed['Mounts'][0][field] = value
            with self.subTest(field=field): self.assertNotEqual(before, self.snapshot([changed]))
        for operation in ('remove', 'duplicate', 'field-remove'):
            changed = copy.deepcopy(self.container)
            if operation == 'remove': changed['Mounts'].pop()
            elif operation == 'duplicate': changed['Mounts'].append(changed['Mounts'][0])
            else: del changed['Mounts'][0]['Name']
            with self.subTest(operation=operation): self.assertNotEqual(before, self.snapshot([changed]))

    def test_runtime_identity_restart_and_configuration_drift_detected(self):
        before = self.snapshot([self.container])
        for path, value in [(('Id',), 'other'), (('Image',), 'other'), (('RestartCount',), 1),
                            (('State', 'Running'), False), (('State', 'StartedAt'), 'later'),
                            (('Config', 'Env'), ['TWO=2', 'ONE=1']),
                            (('HostConfig', 'Binds'), ['two:/two', 'one:/one'])]:
            changed = copy.deepcopy(self.container); target = changed
            for key in path[:-1]: target = target[key]
            target[path[-1]] = value
            with self.subTest(path=path): self.assertNotEqual(before, self.snapshot([changed]))
        self.assertNotEqual(before, self.snapshot([]))

    def test_volatile_health_log_is_not_configuration_drift(self):
        before = self.snapshot([self.container])
        changed = copy.deepcopy(self.container)
        changed['State']['Health'] = {'Status': 'healthy', 'Log': ['new probe']}
        self.assertEqual(before, self.snapshot([changed]))

    def test_malformed_inspection_does_not_produce_valid_snapshot(self):
        changed = copy.deepcopy(self.container); del changed['Mounts']
        with self.assertRaises(KeyError): self.snapshot([changed])


class PhaseTests(unittest.TestCase):
    def run_install(self, fail=None, policy_drift=False, runtime_drift=False):
        with tempfile.TemporaryDirectory() as td, contextlib.ExitStack() as stack:
            root=Path(td); stage=root/'stage'; stage.mkdir(mode=0o700)
            for name in m.FILES | {'manifest.json'}:
                (stage/name).write_bytes((PACKAGE/name).read_bytes())
            destinations={'WRAPPER':root/'sbin'/'wrapper','LIB':root/'lib'/'wrapper',
                          'CONFIG':root/'etc'/'wrapper','STATE':root/'state'/'wrapper',
                          'SUDOERS':root/'sudoers.d'/'wrapper','BACKUPS':root/'backups'}
            for p in destinations.values():p.parent.mkdir(parents=True,exist_ok=True)
            destinations['BACKUPS'].mkdir()
            (destinations['BACKUPS']/'retain').write_bytes(b'old backup sentinel')
            for name,path in destinations.items():stack.enter_context(patch.object(m,name,path))
            stack.enter_context(patch.object(m,'secure'))
            stack.enter_context(patch.object(m,'require_root_stage'))
            stack.enter_context(patch.object(m.pwd,'getpwnam',return_value=type('User',(),{'pw_uid':1000})()))
            policy={'old':('hash',0o440,0,0)}
            expected={**policy,str(m.SUDOERS):(m.PINNED['sudoers.example'],0o440,0,0)}
            stack.enter_context(patch.object(m,'policy_snapshot',side_effect=[
                policy, {'changed': 'private-policy'} if policy_drift else policy, expected]))
            runtime = {'container': 'identity'}
            stack.enter_context(patch.object(m,'runtime_snapshot',side_effect=[
                runtime, {'container': 'private-runtime'} if runtime_drift else runtime, runtime]))
            calls=[]
            def answer(args,**kw):
                calls.append(args)
                if str(m.WRAPPER) in args:
                    if fail=='A' and args[-1]=='status':return subprocess.CompletedProcess(args,1,b'',b'bad')
                    if args[-1]=='status' and ('-E' not in args and 'PYTHONPATH=/tmp' not in args):
                        return subprocess.CompletedProcess(args,0,b'{"operation":"status","deployment":"NO_WRAPPER_RUN"}',b'')
                    if '/usr/bin/sudo' in args and (args[-1]=='extra' or args[-1]=='unknown' or '-E' in args or 'PYTHONPATH=/tmp' in args):
                        return subprocess.CompletedProcess(args,1,b'',b'sudo: a password is required')
                    return subprocess.CompletedProcess(args,1,b'',b'{"status":"BLOCKED"}')
                if args[-1]=='/usr/bin/true':return subprocess.CompletedProcess(args,1,b'',b'sudo: a password is required')
                if fail=='B' and args==['/usr/bin/sudo','-l','-U','enigma']:raise m.Blocked('PolicyListingFailed')
                return subprocess.CompletedProcess(args,0,b'',b'')
            stack.enter_context(patch.object(m,'run',side_effect=answer))
            out=io.StringIO()
            with contextlib.redirect_stdout(out):rc=m.install(stage,m.digest((stage/'manifest.json').read_bytes()))
            report=json.loads(out.getvalue().splitlines()[0])
            self.assertEqual((m.BACKUPS/'retain').read_bytes(),b'old backup sentinel')
            self.assertFalse((m.STATE/'active.json').exists())
            self.assertTrue((m.CONFIG/'packages').exists(),report)
            self.assertFalse(list((m.CONFIG/'packages').iterdir()))
            self.assertFalse(any('systemd-run' in arg for c in calls for arg in c))
            return rc,report,m.SUDOERS.exists(),(stage/'phase-a-pass.json').exists()

    def test_complete_two_phase_install_offline(self):
        rc,report,rule,phaseA=self.run_install()
        self.assertEqual(rc,0); self.assertEqual(report['status'],'PASS')
        self.assertTrue(rule);self.assertTrue(phaseA)
        self.assertEqual(report['systemdAcceptance'],'NOT RUN')

    def test_phase_a_failure_never_activates_sudoers(self):
        rc,report,rule,phaseA=self.run_install('A')
        self.assertEqual(rc,1); self.assertEqual(report['phase'],'A-installing')
        self.assertFalse(rule);self.assertFalse(phaseA)

    def test_phase_a_drift_reports_both_checks_without_secrets_or_activation(self):
        for policy_drift, runtime_drift in ((True, False), (False, True), (True, True)):
            with self.subTest(policy=policy_drift, runtime=runtime_drift):
                rc, report, rule, phaseA = self.run_install(
                    policy_drift=policy_drift, runtime_drift=runtime_drift)
                self.assertEqual(rc, 1)
                self.assertEqual(report['reason'], 'PhaseADrift')
                self.assertEqual(report['phaseAChecks'], {
                    'policyUnchanged': not policy_drift, 'runtimeUnchanged': not runtime_drift})
                self.assertFalse(rule); self.assertFalse(phaseA)
                self.assertNotIn('private-', json.dumps(report))

    def test_phase_b_failure_retains_partial_install_no_blind_rollback(self):
        rc,report,rule,phaseA=self.run_install('B')
        self.assertEqual(rc,1); self.assertEqual(report['phase'],'B-activating')
        self.assertTrue(rule);self.assertTrue(phaseA)

    def test_atomic_sudoers_refuses_existing_destination(self):
        with tempfile.TemporaryDirectory() as td,patch.object(m,'secure'),patch.object(m,'run'):
            p=Path(td)/'rule';p.write_bytes(b'old rule')
            with patch.object(m,'SUDOERS',p),self.assertRaises(FileExistsError):
                m.install_sudoers((PACKAGE/'sudoers.example').read_bytes())
            self.assertEqual(p.read_bytes(),b'old rule')

    def test_directory_lock_excludes_concurrent_installer_without_lock_file(self):
        with tempfile.TemporaryDirectory() as td:
            a=os.open(td,os.O_RDONLY|os.O_DIRECTORY);b=os.open(td,os.O_RDONLY|os.O_DIRECTORY)
            try:
                m.fcntl.flock(a,m.fcntl.LOCK_EX|m.fcntl.LOCK_NB)
                with self.assertRaises(BlockingIOError):m.fcntl.flock(b,m.fcntl.LOCK_EX|m.fcntl.LOCK_NB)
                self.assertEqual(list(Path(td).iterdir()),[])
            finally:os.close(a);os.close(b)


if __name__=='__main__': unittest.main()
