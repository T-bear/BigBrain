"""Offline worker fault tests: no Docker, network, systemd or production data access."""
import copy
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import tempfile
import time
import unittest
from unittest.mock import patch

spec = importlib.util.spec_from_file_location('worker', Path(__file__).resolve().parents[2] / 'scripts/approved-ops/school_lunch_worker.py')
d = importlib.util.module_from_spec(spec)
spec.loader.exec_module(d)


def container(role, running=True):
    return {'Id': role, 'Name': role, 'Image': 'old-' + role,
            'State': {'Running': running, 'ExitCode': 0, 'OOMKilled': False,
                      'Health': {'Status': 'healthy'}, 'StartedAt': 'before'},
            'Config': {'Env': [k + '=false' for k in d.FLAGS] if role == 'api' else [],
                       'Labels': {'org.opencontainers.image.revision': 'a' * 40}},
            'HostConfig': {'Binds': ['b', 'a'], 'Memory': 123},
            'Mounts': [{'Type': 'volume', 'Name': 'v', 'Source': '/synthetic',
                        'Destination': '/data', 'Driver': 'local', 'RW': True}],
            'NetworkSettings': {'Networks': {'synthetic': {}}}}


class WorkerTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.root = Path(self.temp.name)
        now = int(time.time())
        self.p = {'run': '1' * 32, 'project': 'synthetic', 'revision': 'b' * 40,
                  'targets': {r: container(r) for r in d.ROLES},
                  'candidates': {r: 'new-' + r for r in d.ROLES},
                  'backupManifestSha256': 'c' * 64,
                  'sourceGate': {'containerId': 'api', 'mode': 'meal-planner-sqlite-v1',
                    'revision': 'b' * 40, 'apiImage': 'new-api', 'evidenceSha256': 'e' * 64}}
        d.save(self.root / 'backup-verified.json', {'status': 'PASS', 'manifestSha256': 'c' * 64, 'runtimeMatches': True})
        self.guards = [patch.object(d.subprocess, 'Popen', side_effect=AssertionError('No real process')),
                       patch.object(d.urllib.request, 'build_opener', side_effect=AssertionError('No real HTTP'))]
        for guard in self.guards: guard.start()

    def tearDown(self):
        for guard in reversed(self.guards): guard.stop()
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
             patch.object(d, 'docker') as docker, patch.object(d, 'inspect', side_effect=[container('api'), container('api', False), container('web'), container('web', False)]), \
             patch.object(d, 'compose', side_effect=compose), patch.object(d, 'wait_healthy', return_value={'Image': 'new'}), \
             patch.object(d, 'verify_service'), patch.object(d, 'functional'):
            d.work(self.root, self.p)
        self.assertEqual(calls, ['api', 'web'])
        self.assertEqual([c.args for c in docker.call_args_list], [('stop', '--time', '-1', 'api'), ('stop', '--time', '-1', 'web')])
        self.assertEqual(json.loads((self.root/'complete.json').read_text())['schoolSources'], 'UNCHANGED')

    def test_api_health_failure_does_not_replace_web(self):
        calls = []
        with patch.object(d, 'preflight'), patch.object(d, 'quiescent'), patch.object(d, 'check_other'), \
             patch.object(d, 'docker'), patch.object(d, 'inspect', side_effect=[container('api'), container('api', False)]), \
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

    def test_durable_gate_needs_exact_reviewed_image_revision_and_evidence_not_client_pause(self):
        d.source_gate(self.p)
        for key in ('containerId', 'mode', 'revision', 'apiImage', 'evidenceSha256'):
            bad = copy.deepcopy(self.p)
            bad['sourceGate'][key] = 'wrong'
            with self.subTest(key=key), self.assertRaises(d.Blocked): d.source_gate(bad)
        for field in ('quietSince', 'retryAfterUntil', 'usersPaused'):
            bad = copy.deepcopy(self.p); bad['sourceGate'][field] = True
            with self.subTest(field=field), self.assertRaises(d.Blocked): d.source_gate(bad)
        # A future supplier retry is handled by the DB guard, never a household outage.
        self.assertNotIn('quietSince', self.p['sourceGate'])

    def test_recovery_rejects_unverified_durable_gate(self):
        self.state('old-stopped')
        self.p['sourceGate']['evidenceSha256'] = ''
        with patch.object(d, 'inspect', return_value=container('api', False)), patch.object(d, 'docker') as docker:
            with self.assertRaisesRegex(d.Blocked, 'SourceGuardNotVerified'): d.recover(self.root, self.p)
        docker.assert_not_called()

    def test_rollback_allows_only_three_sources_off_with_original_flags_on(self):
        p = self.p
        p['targets']['api']['Config']['Env'] = [key + '=true' for key in d.FLAGS] + ['PRESERVE=value']
        release = {'services': {}, 'volumes': {}, 'networks': {}}
        rollback = copy.deepcopy(release)
        for role in d.ROLES:
            release['services'][role] = {'image': p['candidates'][role],
                'environment': d.service_config(p['targets'][role])['env'],
                'labels': {'org.opencontainers.image.revision': p['revision']}}
            old = copy.deepcopy(release['services'][role])
            old['image'] = p['targets'][role]['Image']
            old['labels']['org.opencontainers.image.revision'] = 'a' * 40
            if role == 'api': old['environment'].update({key: 'false' for key in d.FLAGS})
            rollback['services'][role] = old
        d.save(self.root/'release.compose.json', release)
        d.save(self.root/'rollback.compose.json', rollback)
        with patch.object(d, 'docker', side_effect=lambda *args: json.dumps([{'Id': args[-1]}]).encode()):
            d.validate_rollback(self.root, p)
            for key in (*d.FLAGS, 'PRESERVE'):
                bad = copy.deepcopy(rollback)
                bad['services']['api']['environment'][key] = 'true'
                d.save(self.root/'rollback.compose.json', bad)
                with self.subTest(key=key), self.assertRaises(d.Blocked): d.validate_rollback(self.root, p)

    def test_legacy_api_is_recreated_with_sources_off_never_started_on(self):
        old = self.p['targets']['api']
        old['Config']['Env'] = [k + '=true' for k in d.FLAGS]
        stopped = copy.deepcopy(old); stopped['State']['Running'] = False
        restored = container('api'); restored['Id'] = 'replacement'
        self.p['databases'] = ['same']
        self.p['mealsHash'] = hashlib.sha256(b'[]').hexdigest()
        self.state('old-stopped')
        with patch.object(d, 'inspect', return_value=stopped), patch.object(d, 'check_other'), \
             patch.object(d, 'validate_rollback'), patch.object(d, 'compose', return_value=b'replacement') as compose, \
             patch.object(d, 'wait_healthy', return_value=restored), patch.object(d, 'db_identity', return_value=['same']), \
             patch.object(d, 'endpoint', return_value=b'[]'), patch.object(d, 'docker') as docker:
            d.recover(self.root, self.p)
        docker.assert_not_called()
        self.assertTrue(all(c.kwargs.get('recovery') for c in compose.call_args_list))
        self.assertEqual(json.loads((self.root/'recovery.json').read_text())['status'], 'OLD_API_RESTORED_SOURCES_OFF')
        self.assertEqual(json.loads((self.root/'state.json').read_text())['phase'], 'recovery-replacement-possible')
        with patch.object(d, 'docker') as docker, patch.object(d, 'compose') as compose:
            d.recover(self.root, self.p)
        docker.assert_not_called(); compose.assert_not_called()

    def test_legacy_recovery_uncertainty_never_retries_or_restores_data(self):
        self.p['targets']['api']['Config']['Env'] = [k + '=true' for k in d.FLAGS]
        old = copy.deepcopy(self.p['targets']['api']); old['State']['Running'] = False
        self.state('old-stopped')
        with patch.object(d, 'inspect', return_value=old), patch.object(d, 'check_other'), \
             patch.object(d, 'validate_rollback'), patch.object(d, 'compose', side_effect=d.Blocked('timeout')), \
             patch.object(d, 'docker') as docker:
            with self.assertRaises(d.Blocked): d.recover(self.root, self.p)
        docker.assert_not_called()
        with patch.object(d, 'compose') as compose, patch.object(d, 'docker') as docker:
            d.recover(self.root, self.p)
        compose.assert_not_called(); docker.assert_not_called()

    def test_release_preserves_all_environment_and_flags(self):
        self.p['targets']['api']['Config']['Env'].append('UNCHANGED=value')
        c = copy.deepcopy(self.p['targets']['api'])
        c['Image'] = self.p['candidates']['api']
        c['Config']['Labels'].update({'org.opencontainers.image.revision': self.p['revision'],
                                     'com.docker.compose.service': 'api', 'com.docker.compose.project': self.p['project']})
        self.p['databases'] = ['same']
        with patch.object(d, 'db_identity', return_value=['same']): d.verify_service(self.p, 'api', c)
        for key in (*d.FLAGS, 'UNCHANGED'):
            bad = copy.deepcopy(c)
            bad['Config']['Env'] = [k + '=changed' if k.startswith(key+'=') else k for k in bad['Config']['Env']]
            with self.subTest(key=key), self.assertRaisesRegex(d.Blocked, 'CandidateConfigDrift'):
                d.verify_service(self.p, 'api', bad)

    def test_ordered_arguments_remain_part_of_identity(self):
        a = container('api'); a['Config']['Cmd'] = ['first', 'second']
        b = copy.deepcopy(a); b['Config']['Cmd'].reverse()
        self.assertNotEqual(d.fingerprint(a), d.fingerprint(b))
        a['HostConfig']['UnknownList'] = ['first', 'second']
        b = copy.deepcopy(a); b['HostConfig']['UnknownList'].reverse()
        self.assertNotEqual(d.fingerprint(a), d.fingerprint(b))

    def test_functional_checks_never_request_school_sources(self):
        self.p['mealsHash'] = self.p['webIndexHash'] = hashlib.sha256(b'[]').hexdigest()
        with patch.object(d, 'endpoint', return_value=b'[]') as endpoint:
            d.functional('api', self.p); d.functional('web', self.p)
        self.assertEqual(endpoint.call_args_list[0].args, ('api', '/health'))
        self.assertEqual(len(endpoint.call_args_list), 5)
        self.assertFalse(any('school-meals' in str(c) for c in endpoint.call_args_list))
        for path in ('/api/v1/modules/meal-planner/school-meals?schoolId=rosenfeldt', 'https://example.invalid', '/api/v1/modules/media/jobs'):
            with self.subTest(path=path), self.assertRaisesRegex(d.Blocked, 'HttpScope'): d.endpoint('api', path)

    def test_compose_uses_fixed_plugin_and_closed_environment(self):
        with patch.object(d, 'command', return_value=b'{}') as command:
            d.compose(self.root, self.p, 'config', '--format', 'json')
        self.assertEqual(command.call_args.args[0][0], '/usr/libexec/docker/cli-plugins/docker-compose')
        self.assertEqual(d.ENV['DOCKER_CONFIG'], '/etc/bigbrain-approved-ops/docker')
        self.assertEqual(d.ENV['HOME'], '/var/empty')
        self.assertNotIn('PYTHONPATH', d.ENV)

    def test_additional_or_missing_other_service_blocks(self):
        other = container('unrelated')
        self.p['others'] = [{'id': other['Id'], 'image': other['Image'], 'fingerprint': d.fingerprint(other), 'started': 'before'}]
        def inspect(cid): return other if cid == 'unrelated' else container(cid)
        with patch.object(d, 'inspect', side_effect=inspect), patch.object(d, 'docker', return_value=b'api web unrelated'):
            d.check_other(self.p)
        for actual in (b'api web unrelated new', b'api web'):
            with self.subTest(actual=actual), patch.object(d, 'inspect', side_effect=inspect), patch.object(d, 'docker', return_value=actual), self.assertRaisesRegex(d.Blocked, 'OtherServiceSetChanged'):
                d.check_other(self.p)

    def test_post_replacement_recovery_does_not_require_removed_container(self):
        self.state('replacement-possible')
        with patch.object(d, 'inspect', side_effect=AssertionError('Old may be removed')), patch.object(d, 'docker') as docker:
            d.recover(self.root, self.p)
        docker.assert_not_called()
        self.assertEqual(json.loads((self.root/'recovery.json').read_text())['status'], 'MANUAL_REVIEW_NO_RESTORE')

    def test_backup_receipt_hash_must_match_reviewed_plan(self):
        self.p['backupManifestSha256'] = '0' * 64
        with patch.object(d, 'preflight') as preflight, self.assertRaisesRegex(d.Blocked, 'BackupReceiptMissing'):
            d.work(self.root, self.p)
        preflight.assert_not_called()

    def test_direct_worker_entry_rejected_before_any_operation(self):
        for action in ('launch', 'prepare', 'rollback-work', 'unknown'):
            with self.subTest(action=action), patch.object(d.os, 'geteuid', return_value=0), patch.object(d.sys, 'argv', ['runner', action, '/private', 'hash']), self.assertRaisesRegex(d.Blocked, 'InvalidInvocation'):
                d.main()

    def test_preflight_real_checks_with_offline_dependencies(self):
        p = self.p
        p.update({'others': [], 'databases': ['same'], 'originalConfigHashes': {},
                  'mealsHash': hashlib.sha256(b'[]').hexdigest()})
        release = {'services': {}, 'networks': {'n': {'external': True}}, 'volumes': {}}
        rollback = copy.deepcopy(release)
        for role in d.ROLES:
            release['services'][role] = {'image': p['candidates'][role],
                'environment': d.expected_config(p, role)['env'],
                'labels': {'org.opencontainers.image.revision': p['revision']}}
            rollback['services'][role] = copy.deepcopy(release['services'][role])
            rollback['services'][role]['image'] = p['targets'][role]['Image']
            rollback['services'][role]['labels']['org.opencontainers.image.revision'] = 'a' * 40
        p['expectedServices'] = copy.deepcopy(release['services'])
        for destination in ('/shopping-data', '/calendar-data', '/settings-data', '/lifecycle-data'):
            p['targets']['api']['Mounts'].append({**p['targets']['api']['Mounts'][0], 'Destination': destination, 'Name': destination[1:], 'Source': '/synthetic' + destination})
        backup = {'runtime': {'containerId': 'api', 'image': 'old-api', 'volumes': {
            m['Destination']: {k:v for k,v in m.items() if k != 'Destination'} for m in p['targets']['api']['Mounts']}}}
        for name, data in [('release.compose.json', release), ('rollback.compose.json', rollback), ('backup-runtime.json', backup)]:
            d.save(self.root/name, data)
        def docker(*args, **kwargs):
            if args == ('ps', '-q', '--no-trunc'): return b'api web'
            if args[:2] == ('image', 'inspect'):
                return json.dumps([{'Id': args[2], 'Config': {'Labels': {'org.opencontainers.image.revision': p['revision']}}}]).encode()
            raise AssertionError(args)
        with patch.object(d, 'inspect', side_effect=lambda cid: p['targets'][cid]), patch.object(d, 'docker', side_effect=docker), \
             patch.object(d, 'db_identity', return_value=['same']), patch.object(d, 'endpoint', return_value=b'[]'), \
             patch.object(d, 'compose', return_value=json.dumps(release).encode()):
            d.preflight(self.root, p)
            for problem in ('image', 'mount', 'backup', 'environment', 'revision', 'scope'):
                bad = copy.deepcopy(p)
                if problem == 'image': bad['targets']['api']['Image'] = 'different'
                if problem == 'mount': bad['targets']['api']['Mounts'][0]['Source'] = '/different'
                if problem == 'backup': bad['targets']['api']['Id'] = 'different'
                if problem == 'environment': bad['targets']['api']['Config']['Env'][0] += 'changed'
                if problem == 'revision': bad['revision'] = 'd' * 40
                if problem == 'scope': bad['expectedServices']['api']['command'] = ['extra']
                with self.subTest(problem=problem), self.assertRaises(d.Blocked): d.preflight(self.root, bad)

    def test_offline_package_is_complete_without_approval_and_never_overwrites(self):
        spec = importlib.util.spec_from_file_location('prepare', Path(d.__file__).with_name('prepare_school_lunch_package.py'))
        prepare = importlib.util.module_from_spec(spec); spec.loader.exec_module(prepare)
        source = self.root/'inputs'; source.mkdir()
        p = {**self.p, 'contract': d.CONTRACT}
        d.save(source/'plan.json', p)
        for name in ('release.compose.json', 'rollback.compose.json'):
            d.save(source/name, {'services': {'api': {}, 'web': {}}})
        d.save(source/'backup-runtime.json', {})
        output = self.root/'package'
        digest = prepare.assemble(source, output)
        self.assertEqual(digest, d.hash_file(output/'package.json'))
        hashes = json.loads((output/'package.json').read_text())
        self.assertEqual(set(hashes), set(d.PACKAGE_FILES))
        self.assertTrue(all(d.hash_file(output/n) == h for n,h in hashes.items()))
        self.assertEqual(set(x.name for x in output.iterdir()), set(d.PACKAGE_FILES)|{'package.json'})
        self.assertEqual(output.stat().st_mode & 0o777, 0o700)
        self.assertTrue(all(f.stat().st_mode & 0o777 == 0o600 for f in output.iterdir()))
        with self.assertRaises(FileExistsError): prepare.assemble(source, output)
        for contract in ('api-only-diagnostics-v1', 'unknown'):
            p['contract'] = contract; d.save(source/'plan.json', p)
            with self.subTest(contract=contract), self.assertRaises(ValueError): prepare.assemble(source, self.root/'denied')
        with patch.object(prepare.os, 'geteuid', return_value=0), self.assertRaises(ValueError):
            prepare.assemble(source, self.root/'denied-root')

    def test_package_loader_rejects_wrong_contract_hash_and_symlink(self):
        root = self.root/(d.PREFIX+self.p['run']); root.mkdir(mode=0o700)
        plan = {**self.p, 'contract': d.CONTRACT}
        for name in d.PACKAGE_FILES: d.save(root/name, plan if name == 'plan.json' else {})
        d.save(root/'package.json', {n: d.hash_file(root/n) for n in d.PACKAGE_FILES})
        digest = d.hash_file(root/'package.json')
        # Filesystem ownership is tested separately; no root files needed offline.
        with patch.object(d, 'RUNS', self.root), patch.object(d, 'protected'):
            self.assertEqual(d.load(root, digest), plan)
            with self.assertRaisesRegex(d.Blocked, 'PackageChanged'): d.load(root, '0'*64)
            (root/'runner.py').write_text('changed')
            with self.assertRaisesRegex(d.Blocked, 'InputChanged'): d.load(root, digest)
            d.save(root/'package.json', {n: d.hash_file(root/n) for n in d.PACKAGE_FILES})
            plan['contract'] = 'api-only-diagnostics-v1'; d.save(root/'plan.json', plan)
            d.save(root/'package.json', {n: d.hash_file(root/n) for n in d.PACKAGE_FILES})
            with self.assertRaisesRegex(d.Blocked, 'WrongPlan'): d.load(root, d.hash_file(root/'package.json'))
        link = self.root/'link'; link.symlink_to(root/'plan.json')
        with self.assertRaisesRegex(d.Blocked, 'UnprotectedPath'): d.protected(link)
        with self.assertRaisesRegex(d.Blocked, 'UnprotectedPath'): d.protected(root/'plan.json')  # agent owned

    def test_backup_plan_requires_all_five_volumes(self):
        # Positive full preflight is covered above. Verify incomplete plan cannot
        # reach DB inspection even when all other read-only guards are successful.
        d.save(self.root/'release.compose.json', {})
        d.save(self.root/'backup-runtime.json', {'runtime': {'containerId': 'api', 'image': 'old-api', 'volumes': {}}})
        def docker(*args, **kwargs):
            return json.dumps([{'Id': args[-1], 'Config': {'Labels': {'org.opencontainers.image.revision': self.p['revision']}}}]).encode()
        with patch.object(d, 'validate_release'), patch.object(d, 'validate_rollback'), patch.object(d, 'check_other'), \
             patch.object(d, 'inspect', side_effect=lambda cid: self.p['targets'][cid]), patch.object(d, 'docker', side_effect=docker), \
             patch.object(d, 'db_identity') as stat, self.assertRaisesRegex(d.Blocked, 'BackupVolumeCoverage'):
            d.preflight(self.root, self.p)
        stat.assert_not_called()

    def test_real_worker_bytes_fit_unchanged_gateway_contract(self):
        # Reuse accepted gateway's synthetic WAL, protected-files and fake-systemd fixture.
        spec = importlib.util.spec_from_file_location('gateway_tests', Path(__file__).with_name('test_approved_operations.py'))
        module = importlib.util.module_from_spec(spec); spec.loader.exec_module(module)
        fixture = module.OperationsTests()
        fixture.setUp()
        try:
            approval, root = fixture.prepare_package()
            (root/'runner.py').write_bytes(Path(d.__file__).read_bytes())
            fixture.reseal(approval, root)
            result = fixture.engine.dispatch('start-approved')
            self.assertEqual(result['status'], 'SUBMITTED')
            systemd = fixture.calls[-1]
            self.assertEqual(systemd[0], module.ops.SYSTEMD)
            self.assertIn('--property=TimeoutStartSec=480', systemd)
            self.assertIn('work', systemd)
            self.assertTrue(any(' recover ' in a for a in systemd))
            self.assertFalse(module.ops.CONTRACTS['api-only-diagnostics-v1']['enabled'])
        finally:
            fixture.doCleanups()
            fixture.tearDown()

    def test_interruption_after_stop_marker_preserves_recovery_evidence(self):
        with patch.object(d, 'preflight'), patch.object(d, 'quiescent'), patch.object(d, 'check_other'), \
             patch.object(d, 'inspect', return_value=container('api')), patch.object(d, 'docker', side_effect=KeyboardInterrupt), \
             patch.object(d, 'compose') as compose:
            with self.assertRaises(KeyboardInterrupt): d.work(self.root, self.p)
        self.assertEqual(json.loads((self.root/'state.json').read_text())['phase'], 'stopping-old')
        self.assertFalse((self.root/'complete.json').exists())
        compose.assert_not_called()


    def recovery_candidate(self):
        p = self.p
        p['apiRecovery'] = {'image': p['candidates']['api'], 'revision': p['revision'],
            'backupManifestSha256': p['backupManifestSha256'], 'evidenceSha256': 'f' * 64}
        p['databases'] = ['same']; p['mealsHash'] = hashlib.sha256(b'[]').hexdigest()
        release = {'services': {}, 'volumes': {}, 'networks': {}}
        for role in d.ROLES:
            release['services'][role] = {'image': p['candidates'][role],
                'environment': d.service_config(p['targets'][role])['env'],
                'labels': {'org.opencontainers.image.revision': p['revision']}}
        rollback = copy.deepcopy(release)
        rollback['services']['web']['image'] = p['targets']['web']['Image']
        rollback['services']['web']['labels']['org.opencontainers.image.revision'] = 'a' * 40
        rollback['services']['api']['environment'].update({k: 'false' for k in d.FLAGS})
        d.save(self.root / 'release.compose.json', release)
        d.save(self.root / 'rollback.compose.json', rollback)
        return rollback

    def test_explicit_recovery_accepts_only_candidate_and_backup_bound_evidence(self):
        self.recovery_candidate()
        with patch.object(d, 'docker', side_effect=lambda *a: json.dumps([{'Id': a[-1],
                'Config': {'Labels': {'org.opencontainers.image.revision': self.p['revision']}}}]).encode()):
            d.validate_rollback(self.root, self.p)
            for key in ('image', 'revision', 'backupManifestSha256', 'evidenceSha256', 'extra'):
                bad = copy.deepcopy(self.p); bad['apiRecovery'][key] = 'wrong'
                with self.subTest(key=key), self.assertRaisesRegex(d.Blocked, 'ApiRecoveryScope'):
                    d.validate_rollback(self.root, bad)
        with patch.object(d, 'docker', return_value=json.dumps([{'Id': 'new-api',
                'Config': {'Labels': {'org.opencontainers.image.revision': 'wrong'}}}]).encode()), \
             self.assertRaisesRegex(d.Blocked, 'RecoveryRevisionDrift'):
            d.validate_rollback(self.root, self.p)

    def test_explicit_recovery_keeps_config_mounts_and_sources_off(self):
        cfg = self.recovery_candidate()
        for kind in ('environment', 'mount', 'command', 'source', 'old-image'):
            bad = copy.deepcopy(cfg)
            if kind == 'environment': bad['services']['api']['environment']['EXTRA'] = 'bad'
            if kind == 'mount': bad['volumes']['other'] = {'external': True}
            if kind == 'command': bad['services']['api']['command'] = ['unexpected']
            if kind == 'source': bad['services']['api']['environment'][d.FLAGS[0]] = 'true'
            if kind == 'old-image': bad['services']['api']['image'] = 'old-api'
            d.save(self.root / 'rollback.compose.json', bad)
            with patch.object(d, 'docker', side_effect=lambda *a: json.dumps([{'Id': a[-1],
                    'Config': {'Labels': {'org.opencontainers.image.revision': self.p['revision']}}}]).encode()):
                with self.subTest(kind=kind), self.assertRaises(d.Blocked): d.validate_rollback(self.root, self.p)

    def test_explicit_recovery_uses_fixed_image_even_if_original_flags_off(self):
        self.recovery_candidate(); self.state('old-stopped')
        restored = container('api'); restored['Image'] = 'new-api'
        restored['Config']['Labels']['org.opencontainers.image.revision'] = self.p['revision']
        with patch.object(d, 'inspect', return_value=container('api', False)), patch.object(d, 'check_other'), \
             patch.object(d, 'validate_rollback'), patch.object(d, 'compose', return_value=b'new-api') as compose, \
             patch.object(d, 'wait_healthy', return_value=restored), patch.object(d, 'db_identity', return_value=['same']), \
             patch.object(d, 'endpoint', return_value=b'[]'), patch.object(d, 'docker') as docker:
            d.recover(self.root, self.p)
        docker.assert_not_called()
        self.assertTrue(all(c.kwargs.get('recovery') for c in compose.call_args_list))
        self.assertEqual(json.loads((self.root / 'recovery.json').read_text())['status'], 'APPROVED_API_RESTORED_SOURCES_OFF')
        with patch.object(d, 'docker') as docker, patch.object(d, 'compose') as compose:
            d.recover(self.root, self.p)
        docker.assert_not_called(); compose.assert_not_called()

    def test_recovery_failure_never_retries_or_reports_success(self):
        self.recovery_candidate()
        for fault in ('health', 'image', 'revision', 'database', 'household', 'other-service'):
            self.state('old-stopped')
            restored = container('api'); restored['Image'] = 'new-api'
            restored['Config']['Labels']['org.opencontainers.image.revision'] = self.p['revision']
            if fault == 'image': restored['Image'] = 'old-api'
            if fault == 'revision': restored['Config']['Labels']['org.opencontainers.image.revision'] = 'wrong'
            other = [None, d.Blocked('OtherServiceChanged')] if fault == 'other-service' else None
            with patch.object(d, 'inspect', return_value=container('api', False)), \
                 patch.object(d, 'check_other', side_effect=other), patch.object(d, 'validate_rollback'), \
                 patch.object(d, 'compose', return_value=b'new-api'), \
                 patch.object(d, 'wait_healthy', return_value=restored, side_effect=d.Blocked('HealthDeadline') if fault == 'health' else None), \
                 patch.object(d, 'db_identity', return_value=['changed'] if fault == 'database' else ['same']), \
                 patch.object(d, 'endpoint', return_value=b'changed' if fault == 'household' else b'[]'):
                with self.subTest(fault=fault), self.assertRaises(d.Blocked): d.recover(self.root, self.p)
            self.assertEqual(json.loads((self.root / 'state.json').read_text())['phase'], 'recovery-replacement-possible')
            with patch.object(d, 'docker') as docker, patch.object(d, 'compose') as compose:
                d.recover(self.root, self.p)
            docker.assert_not_called(); compose.assert_not_called()
            self.assertEqual(json.loads((self.root / 'recovery.json').read_text())['status'], 'MANUAL_REVIEW_NO_RESTORE')

    def test_explicit_recovery_does_not_allow_blind_partial_replacement_retry(self):
        self.recovery_candidate()
        for role in ('api', 'web'):
            for phase in ('replacement-possible', 'verified', 'recovery-replacement-possible'):
                self.state(phase, role)
                with patch.object(d, 'inspect', side_effect=AssertionError('No inspection of removed target')), \
                     patch.object(d, 'docker') as docker, patch.object(d, 'compose') as compose:
                    d.recover(self.root, self.p)
                docker.assert_not_called(); compose.assert_not_called()
                self.assertEqual(json.loads((self.root / 'recovery.json').read_text())['status'], 'MANUAL_REVIEW_NO_RESTORE')
