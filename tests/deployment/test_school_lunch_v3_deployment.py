"""Offline fault tests. Docker/systemd/HTTP calls are mocked, never production."""
import copy
import importlib.util
import json
import os
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch, Mock

spec = importlib.util.spec_from_file_location('deploy', Path(__file__).resolve().parents[2] / 'scripts/deploy-school-lunch-v3.py')
d = importlib.util.module_from_spec(spec)
spec.loader.exec_module(d)


def container(role, running=True):
    return {'Id': role, 'Image': d.OLD[role], 'State': {'Running': running, 'ExitCode': 0, 'OOMKilled': False,
            'Health': {'Status': 'healthy'}, 'StartedAt': 'before'},
            'Config': {'Env': [], 'Labels': {}}, 'HostConfig': {'Binds': ['b', 'a'], 'Memory': 123},
            'Mounts': [{'Type': 'volume', 'Name': 'v', 'Source': '/synthetic', 'Destination': '/data', 'RW': True}],
            'NetworkSettings': {'Networks': {'synthetic': {}}}}


class DeploymentTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.root = Path(self.temp.name)
        self.p = {'run': '1' * 32, 'project': 'synthetic', 'revision': d.REV,
                  'targets': {r: container(r) for r in d.ROLES}, 'candidates': {r: 'new-' + r for r in d.ROLES}}
        d.save(self.root/'backup-verified.json', {'status':'PASS','manifestSha256':d.BACKUP_SHA,'runtimeMatches':True})
        self.guard = patch.object(d.subprocess, 'run', side_effect=AssertionError('No real command'))
        self.guard.start()

    def tearDown(self):
        self.guard.stop()
        self.temp.cleanup()

    def state(self, phase, role='api'):
        d.record(self.root, self.p, role, phase)

    def test_scope_rejects_extra_services_or_dependencies(self):
        cfg = {'services': {r: {'image': self.p['candidates'][r]} for r in d.ROLES},
               'networks': {'n': {'external': True}}, 'volumes': {'v': {'external': True}}}
        self.p['expectedServices'] = copy.deepcopy(cfg['services'])
        d.validate_release(self.p, cfg)
        for kind in ('service', 'depends', 'build', 'volume', 'command'):
            bad = copy.deepcopy(cfg)
            if kind == 'service': bad['services']['librarr'] = {}
            if kind == 'depends': bad['services']['api']['depends_on'] = ['librarr']
            if kind == 'build': bad['services']['api']['build'] = '.'
            if kind == 'volume': bad['volumes']['v']['external'] = False
            if kind == 'command': bad['services']['api']['command'] = ['unexpected']
            with self.subTest(kind=kind), self.assertRaises(d.Blocked): d.validate_release(self.p, bad)

    def test_mount_order_not_identity_but_changes_are(self):
        a = container('api'); b = copy.deepcopy(a)
        b['HostConfig']['Binds'].reverse()
        self.assertEqual(d.fingerprint(a), d.fingerprint(b))
        b['Mounts'][0]['Source'] = '/different'
        self.assertNotEqual(d.fingerprint(a), d.fingerprint(b))
        b = copy.deepcopy(a); b['HostConfig']['ReadonlyRootfs'] = True
        self.assertNotEqual(d.fingerprint(a), d.fingerprint(b))

    def test_flags_default_off_and_explicit_activation_blocks(self):
        c = container('api'); d.flags_off(c)
        c['Config']['Env'] = ['MealPlanner__SchoolMeals__Enabled=false']; d.flags_off(c)
        for flag in ('Enabled', 'RosenfeldtEnabled', 'MusikugglanEnabled'):
            c['Config']['Env'] = ['MealPlanner__SchoolMeals__' + flag + '=true']
            with self.subTest(flag=flag), self.assertRaises(d.Blocked): d.flags_off(c)

    def test_socket_gate_ipv4_ipv6_and_bad_input(self):
        for address in ('00000000', '0' * 32):
            self.assertEqual(d.inbound_connections('sl local_address rem_address st\n0: '+address+':1F90 00000000:0000 0A'), 0)
            self.assertEqual(d.inbound_connections('0: '+address+':1F90 00000000:1234 01'), 1)
            self.assertEqual(d.inbound_connections('0: '+address+':1234 00000000:1F90 01'), 0)
        with self.assertRaises(d.Blocked): d.inbound_connections('invalid')

    def test_active_connection_prevents_stop(self):
        with patch.object(d, 'preflight'), patch.object(d, 'quiescent', side_effect=d.Blocked('ApiConnectionsPresent')), patch.object(d, 'docker') as docker:
            with self.assertRaises(d.Blocked): d.work(self.root, self.p)
            docker.assert_not_called()
        self.assertFalse((self.root / 'state.json').exists())

    def test_missing_backup_receipt_blocks_worker(self):
        (self.root/'backup-verified.json').unlink()
        with patch.object(d, 'docker') as docker, self.assertRaisesRegex(d.Blocked, 'BackupReceiptMissing'):
            d.work(self.root,self.p)
        docker.assert_not_called()

    def test_failed_preflight_prevents_stop(self):
        with patch.object(d, 'preflight', side_effect=d.Blocked('CandidateChanged')), patch.object(d, 'docker') as docker:
            with self.assertRaises(d.Blocked): d.work(self.root, self.p)
            docker.assert_not_called()

    def test_normal_order_and_durable_pre_replacement_marker(self):
        calls = []
        def compose(root, p, *args, **kwargs):
            role = args[-1]
            if args[0] == 'up':
                self.assertEqual(json.loads((root / 'state.json').read_text())['phase'], 'replacement-possible')
                self.assertEqual(args, ('up', '-d', '--no-deps', '--no-build', '--pull', 'never', role))
                calls.append(role)
            return ('new-' + role).encode()
        with patch.object(d, 'preflight'), patch.object(d, 'quiescent'), patch.object(d, 'check_other'), \
             patch.object(d, 'docker') as docker, patch.object(d, 'inspect', side_effect=lambda cid: container(cid, False)), \
             patch.object(d, 'compose', side_effect=compose), patch.object(d, 'wait_healthy', return_value={'Image': 'new'}), \
             patch.object(d, 'verify_service'), patch.object(d, 'functional'):
            d.work(self.root, self.p)
        self.assertEqual(calls, ['api', 'web'])
        self.assertEqual([c.args for c in docker.call_args_list], [('stop', '--time', '-1', 'api'), ('stop', '--time', '-1', 'web')])
        self.assertEqual(json.loads((self.root/'complete.json').read_text())['schoolSources'], 'ON')

    def test_api_health_failure_does_not_replace_web(self):
        calls = []
        with patch.object(d, 'preflight'), patch.object(d, 'quiescent'), patch.object(d, 'check_other'), \
             patch.object(d, 'docker'), patch.object(d, 'inspect', return_value=container('api', False)), \
             patch.object(d, 'compose', side_effect=lambda *a, **k: calls.append(a[2:]) or b'new-api'), \
             patch.object(d, 'wait_healthy', side_effect=d.Blocked('HealthDeadline')):
            with self.assertRaises(d.Blocked): d.work(self.root, self.p)
        self.assertTrue(all(c[-1] == 'api' for c in calls))
        self.assertFalse((self.root/'complete.json').exists())
        self.assertEqual(json.loads((self.root/'state.json').read_text())['phase'], 'replacement-possible')

    def test_stop_failure_restarts_only_exact_clean_old(self):
        self.state('stopping-old')
        with patch.object(d, 'inspect', return_value=container('api', False)), patch.object(d, 'check_other'), \
             patch.object(d, 'docker') as docker, patch.object(d, 'wait_healthy'):
            d.recover(self.root, self.p)
        docker.assert_called_once_with('start', 'api')
        self.assertEqual(json.loads((self.root/'recovery.json').read_text())['status'], 'EXACT_OLD_RESTARTED')

    def test_pending_stop_waits_before_restart(self):
        self.state('stopping-old')
        with patch.object(d, 'inspect', side_effect=[container('api'), container('api', False)]), \
             patch.object(d, 'check_other'), patch.object(d, 'docker') as docker, patch.object(d, 'wait_healthy'):
            d.recover(self.root, self.p)
        self.assertEqual([x.args for x in docker.call_args_list], [('wait', 'api'), ('start', 'api')])

    def test_repeated_completed_recovery_does_not_stop_or_wait_again(self):
        self.state('old-stopped')
        with patch.object(d, 'inspect', side_effect=[container('api', False), container('api')]), \
             patch.object(d, 'check_other'), patch.object(d, 'docker') as docker, patch.object(d, 'wait_healthy'):
            d.recover(self.root, self.p)
            d.recover(self.root, self.p)
        docker.assert_called_once_with('start', 'api')

    def test_replacement_uncertainty_never_restarts(self):
        for phase in ('replacement-possible', 'verified'):
            self.state(phase)
            with patch.object(d, 'inspect', return_value=container('api', False)), patch.object(d, 'docker') as docker:
                d.recover(self.root, self.p)
            docker.assert_not_called()
            self.assertEqual(json.loads((self.root/'recovery.json').read_text())['status'], 'MANUAL_REVIEW_NO_RESTORE')

    def test_wrong_identity_or_unclean_exit_never_restarts(self):
        for kind in ('image', 'mount', 'unclean', 'oom'):
            self.state('old-stopped'); c = container('api', False)
            if kind == 'image': c['Image'] = 'other'
            if kind == 'mount': c['Mounts'][0]['Source'] = '/other'
            if kind == 'unclean': c['State']['ExitCode'] = 137
            if kind == 'oom': c['State']['OOMKilled'] = True
            with patch.object(d, 'inspect', return_value=c), patch.object(d, 'docker') as docker:
                d.recover(self.root, self.p)
            docker.assert_not_called()

    def test_stale_invocation_rejected(self):
        with patch.dict(os.environ, {'INVOCATION_ID': 'old'}): self.state('old-stopped')
        with patch.dict(os.environ, {'INVOCATION_ID': 'new'}), self.assertRaisesRegex(d.Blocked, 'StaleRun'):
            d.recover(self.root, self.p)

    def test_no_state_means_no_stop(self):
        with patch.object(d, 'docker') as docker: d.recover(self.root, self.p)
        docker.assert_not_called()
        self.assertEqual(json.loads((self.root/'recovery.json').read_text())['status'], 'NO_STOP_RECORDED')

    def test_wrong_reviewed_package_hash_rejected(self):
        os.chmod(self.root, 0o700)
        (self.root/'package.json').write_text('{}')
        with self.assertRaisesRegex(d.Blocked, 'PackageChanged'): d.load(self.root, '0'*64)
        with self.assertRaisesRegex(d.Blocked, 'PackageCoverage'): d.load(self.root, d.hash_file(self.root/'package.json'))

    def test_main_failure_is_closed_before_build_artifacts(self):
        with patch.object(d, 'command', return_value=b'wrong-main'), patch.object(d, 'docker') as docker:
            with self.assertRaisesRegex(d.Blocked, 'MainChanged'): d.prepare(self.root)
        docker.assert_not_called()

    def test_launch_queues_system_manager_worker_and_recovery_without_deploying(self):
        h = 'a' * 64
        calls = []
        def command(args, **kwargs):
            calls.append(args)
            if args[0] == 'git': return (d.REV + ' refs/heads/main').encode()
            if args[:4] == ['/usr/bin/sudo', '-n', '/usr/bin/python3', '-I']: return json.dumps({'status': 'PASS', 'manifestSha256': d.BACKUP_SHA, 'runtimeMatches': True}).encode()
            self.assertTrue((self.root/'queued.json').exists())
            return b''
        with patch.object(d.sys, 'argv', ['runner', 'launch', str(self.root), h, '--users-paused']), \
             patch.object(d, 'load', return_value=self.p), patch.object(d, 'preflight'), \
             patch.object(d, 'quiescent'), patch.object(d, 'command', side_effect=command), \
             patch.object(d.subprocess, 'run') as run, patch.object(d, 'work') as work, patch('builtins.print'):
            run.return_value.returncode = 0
            d.launch(self.root)
        work.assert_not_called()
        args = calls[-1]
        self.assertEqual(args[:3], ['/usr/bin/sudo', '-n', '/usr/bin/systemd-run'])
        for value in ('--service-type=oneshot', '--property=TimeoutStartSec=480',
                      '--property=TimeoutStopSec=180', '--property=KillMode=control-group', '--property=Restart=no'):
            self.assertIn(value, args)
        self.assertTrue(any(x.startswith('--property=ExecStopPost=') and ' recover ' in x and h in x for x in args))
        self.assertEqual(args[-3:], ['work', str(self.root), h])

    def test_failed_unit_launch_has_receipt_and_cannot_be_requeued(self):
        def command(args, **kwargs):
            if args[0] == 'git': return (d.REV + ' refs/heads/main').encode()
            if args[:4] == ['/usr/bin/sudo', '-n', '/usr/bin/python3', '-I']: return json.dumps({'status': 'PASS', 'manifestSha256': d.BACKUP_SHA, 'runtimeMatches': True}).encode()
            raise d.Blocked('CommandFailed')
        with patch.object(d.sys, 'argv', ['runner', 'launch', str(self.root), 'a'*64, '--users-paused']), \
             patch.object(d, 'load', return_value=self.p), patch.object(d, 'preflight'), patch.object(d, 'quiescent'), \
             patch.object(d, 'command', side_effect=command), patch.object(d.subprocess, 'run') as run:
            run.return_value.returncode = 0
            with self.assertRaisesRegex(d.Blocked, 'CommandFailed'): d.launch(self.root)
            with self.assertRaisesRegex(d.Blocked, 'RunAlreadyQueued'): d.launch(self.root)
        self.assertEqual(json.loads((self.root/'failure.json').read_text())['status'], 'LAUNCH_FAILED_CHECK_UNIT')

    def test_expected_environment_changes_only_three_api_flags(self):
        self.p['targets']['api']['Config']['Env'] = ['KEEP=original']
        self.p['targets']['web']['Config']['Env'] = ['WEB=original']
        expected = d.expected_config(self.p, 'api')
        self.assertEqual(expected['env'], {'KEEP': 'original', **d.FLAGS})
        self.assertEqual(d.expected_config(self.p, 'web'), d.service_config(self.p['targets']['web']))
        self.assertEqual(self.p['targets']['api']['Config']['Env'], ['KEEP=original'])

    def test_enabled_flags_require_all_three(self):
        c = container('api')
        c['Config']['Env'] = [k + '=' + v for k, v in d.FLAGS.items()]
        d.flags_on(c)
        for key in d.FLAGS:
            bad = copy.deepcopy(c)
            bad['Config']['Env'] = [s for s in bad['Config']['Env'] if not s.startswith(key + '=')]
            with self.assertRaisesRegex(d.Blocked, 'SchoolFlagsNotEnabled'): d.flags_on(bad)

    def test_candidate_config_preserves_non_school_environment(self):
        self.p['targets']['api']['Config']['Env'] = ['KEEP=original']
        c = copy.deepcopy(self.p['targets']['api'])
        c['Image'] = self.p['candidates']['api']
        c['Config']['Env'] = ['KEEP=original'] + [k + '=' + v for k, v in d.FLAGS.items()]
        c['Config']['Labels'] = {'org.opencontainers.image.revision': d.REV, 'com.docker.compose.service': 'api', 'com.docker.compose.project': 'synthetic'}
        self.p['databases'] = ['same']
        with patch.object(d, 'db_identity', return_value=['same']): d.verify_service(self.p, 'api', c)
        c['Config']['Env'][0] = 'KEEP=changed'
        with self.assertRaisesRegex(d.Blocked, 'CandidateConfigDrift'): d.verify_service(self.p, 'api', c)

    def test_source_errors_recorded_without_claiming_published(self):
        import hashlib
        self.p['mealsHash'] = hashlib.sha256(b'[]').hexdigest()
        for status in ('published', 'unavailable', 'invalid', 'notPublished', 'cachePolicy'):
            def endpoint(role, path):
                if 'school-meals?' in path:
                    return json.dumps({'schoolId': path.split('=')[-1], 'status': status, 'isFixture': False, 'year': 2026, 'week': 43}).encode()
                return b'[]'
            with patch.object(d, 'endpoint', side_effect=endpoint): d.functional('api', self.p)
            self.assertEqual(self.p['sourceResults']['api:rosenfeldt']['status'], status)

    def test_offline_or_fixture_cannot_pass_live_check(self):
        import hashlib
        self.p['mealsHash'] = hashlib.sha256(b'[]').hexdigest()
        for status, fixture in [('offline', False), ('published', True)]:
            def endpoint(role, path):
                return json.dumps({'schoolId': 'rosenfeldt', 'status': status, 'isFixture': fixture}).encode() if 'school-meals?' in path else b'[]'
            with patch.object(d, 'endpoint', side_effect=endpoint), self.assertRaises(d.Blocked): d.functional('api', self.p)

    def test_bad_privileged_backup_result_prevents_systemd_submission(self):
        calls = []
        def command(args, **kwargs):
            calls.append(args)
            if args[0] == 'git': return (d.REV + ' refs/heads/main').encode()
            return json.dumps({'status': 'INCOMPLETE'}).encode()
        with patch.object(d.sys, 'argv', ['runner', 'launch', str(self.root), 'a'*64, '--users-paused']), patch.object(d, 'load', return_value=self.p), patch.object(d, 'preflight'), patch.object(d, 'quiescent'), patch.object(d, 'command', side_effect=command), patch.object(d.subprocess, 'run') as run:
            run.return_value.returncode = 0
            with self.assertRaisesRegex(d.Blocked, 'BackupVerificationFailed'): d.launch(self.root)
        self.assertFalse(any('/usr/bin/systemd-run' in x for x in calls))
        self.assertFalse((self.root/'queued.json').exists())

    def test_quiescence_rechecked_after_privileged_verification(self):
        def command(args, **kwargs):
            if args[0] == 'git': return (d.REV + ' refs/heads/main').encode()
            return json.dumps({'status': 'PASS', 'manifestSha256': d.BACKUP_SHA, 'runtimeMatches': True}).encode()
        with patch.object(d.sys, 'argv', ['runner', 'launch', str(self.root), 'a'*64, '--users-paused']), patch.object(d, 'load', return_value=self.p), patch.object(d, 'preflight'), patch.object(d, 'quiescent', side_effect=[None, d.Blocked('ApiConnectionsPresent')]), patch.object(d, 'command', side_effect=command), patch.object(d.subprocess, 'run') as run:
            run.return_value.returncode = 0
            with self.assertRaisesRegex(d.Blocked, 'ApiConnectionsPresent'): d.launch(self.root)
        self.assertFalse((self.root/'queued.json').exists())

    def test_explicit_rollback_launch_has_separate_unit_and_receipts(self):
        self.state('verified')
        calls=[]
        def command(args, **kwargs):
            calls.append(args)
            if args[0]=='git': return (d.REV+' refs/heads/main').encode()
            if args[:4]==['/usr/bin/sudo','-n','/usr/bin/python3','-I']:
                self.assertEqual(args[-1],'--retained-only')
                return json.dumps({'status':'PASS','manifestSha256':d.BACKUP_SHA,'runtimeMatches':False}).encode()
            return b''
        with patch.object(d.sys,'argv',['runner','launch-rollback',str(self.root),'a'*64,'--users-paused']), patch.object(d,'load',return_value=self.p), patch.object(d,'rollback_preflight'), patch.object(d,'command',side_effect=command), patch.object(d.subprocess,'run') as run, patch('builtins.print'):
            run.return_value.returncode=0
            d.launch(self.root)
        self.assertTrue((self.root/'rollback-queued.json').exists())
        self.assertFalse((self.root/'queued.json').exists())
        self.assertEqual(calls[-1][-3:],['rollback-work',str(self.root),'a'*64])
        self.assertIn('--unit=school-lunch-v3-'+self.p['run']+'-rollback',calls[-1])
        self.assertTrue(any(' rollback-recover ' in x for x in calls[-1]))
        self.assertTrue(json.loads((self.root/'backup-verified.json').read_text())['runtimeMatches'])

    def test_rollback_rejects_unknown_state_before_stopping(self):
        with patch.object(d, 'rollback_preflight', side_effect=d.Blocked('DatabaseIdentityDrift')), patch.object(d, 'docker') as docker:
            with self.assertRaises(d.Blocked): d.rollback_work(self.root, self.p)
            docker.assert_not_called()

    def test_explicit_rollback_scopes_api_then_web_without_restore(self):
        import hashlib
        self.p['mealsHash'] = self.p['oldWebIndexHash'] = hashlib.sha256(b'[]').hexdigest()
        current = {r: container(r) for r in d.ROLES}; calls=[]
        def endpoint(role, path): return b'{"status":"offline"}' if 'school-meals?' in path else b'[]'
        def docker(*args, **kwargs):
            calls.append(args)
            if args[0] == 'compose':
                self.assertIn('rollback.compose.json', ' '.join(args))
                self.assertIn('--no-deps', args)
                self.assertEqual(json.loads((self.root/'rollback-state.json').read_text())['phase'], 'replacement-possible')
        with patch.object(d, 'rollback_preflight', return_value=current), patch.object(d, 'check_other'), patch.object(d, 'inspect', side_effect=[container('api'),container('api',False),container('web'),container('web',False)]), patch.object(d, 'docker', side_effect=docker), patch.object(d, 'compose', return_value=b'new'), patch.object(d, 'wait_healthy', return_value={}), patch.object(d, 'verify_old'), patch.object(d, 'endpoint', side_effect=endpoint):
            d.rollback_work(self.root,self.p)
        self.assertEqual([c[-1] for c in calls if c[0]=='compose'], ['api','web'])
        self.assertEqual(json.loads((self.root/'rollback-complete.json').read_text())['status'],'ROLLED_BACK')
        self.assertTrue(all(c[0] in ('stop','compose') for c in calls))

    def test_rollback_recovery_after_possible_replacement_never_mutates(self):
        d.save(self.root/'rollback-state.json', {'run': self.p['run'], 'invocation': os.environ.get('INVOCATION_ID'), 'phase':'replacement-possible'})
        with patch.object(d, 'docker') as docker: d.rollback_recover(self.root, self.p)
        docker.assert_not_called()
        self.assertEqual(json.loads((self.root/'rollback-recovery.json').read_text())['status'], 'MANUAL_REVIEW_NO_RESTORE')

    def test_rollback_recovery_before_replacement_restarts_exact_clean_target(self):
        c=container('api',False)
        d.save(self.root/'rollback-state.json', {'run':self.p['run'], 'invocation':os.environ.get('INVOCATION_ID'), 'phase':'stopping-old', 'container':c['Id'], 'image':c['Image'], 'fingerprint':d.fingerprint(c)})
        with patch.object(d, 'inspect', return_value=c), patch.object(d, 'check_other'), patch.object(d, 'wait_healthy'), patch.object(d, 'docker') as docker: d.rollback_recover(self.root,self.p)
        docker.assert_called_once_with('start','api')


class BackupBindingTests(unittest.TestCase):
    def setUp(self):
        spec = importlib.util.spec_from_file_location('verify_backup', Path(__file__).resolve().parents[2] / 'scripts/verify-school-lunch-v3-backup.py')
        self.v = importlib.util.module_from_spec(spec); spec.loader.exec_module(self.v)
        self.temp = tempfile.TemporaryDirectory(); self.root = Path(self.temp.name)
        self.v.BACKUP = self.root
        (self.root/'manifest.json').write_text(json.dumps({'scriptSha256': self.v.SCRIPT_SHA, 'runtime': {'test': 'same'}}))
        self.v.MANIFEST_SHA = d.hash_file(self.root/'manifest.json')
        self.b=Mock()
        self.b.digest.side_effect=d.hash_file
        self.b.require.side_effect=lambda value, code: None if value else (_ for _ in ()).throw(d.Blocked(code))
        self.b.runtime_identity.return_value={'test':'same'}
        self.b.verify_set.return_value=self.v.MANIFEST_SHA

    def tearDown(self): self.temp.cleanup()

    def test_existing_verifier_and_runtime_binding_both_required(self):
        result=self.v.verify(self.root,self.b)
        self.assertEqual(result,{'status':'PASS','manifestSha256':self.v.MANIFEST_SHA,'runtimeMatches':True})
        self.b.verify_set.assert_called_once_with(self.root)
        self.b.verify_runtime_plan.assert_called_once()
        self.b.verify_runtime.assert_called_once()

    def test_changed_manifest_blocks(self):
        (self.root/'manifest.json').write_text('{}')
        with self.assertRaisesRegex(d.Blocked,'BackupManifestChanged'): self.v.verify(self.root,self.b)
        self.b.inspect.assert_not_called()

    def test_runtime_mismatch_blocks(self):
        self.b.runtime_identity.return_value={'test':'different'}
        with self.assertRaisesRegex(d.Blocked,'BackupRuntimeMismatch'): self.v.verify(self.root,self.b)
        self.b.verify_set.assert_not_called()

    def test_failed_retained_integrity_never_returns_pass(self):
        self.b.verify_set.side_effect=d.Blocked('IntegrityFailed')
        with self.assertRaisesRegex(d.Blocked,'IntegrityFailed'): self.v.verify(self.root,self.b)

    def test_rollback_rechecks_retained_backup_without_claiming_original_container(self):
        result=self.v.verify(self.root,self.b,True)
        self.assertFalse(result['runtimeMatches'])
        self.b.verify_set.assert_called_once_with(self.root)
        self.b.inspect.assert_not_called()



if __name__ == '__main__': unittest.main()
