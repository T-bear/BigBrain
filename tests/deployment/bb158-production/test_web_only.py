"""No production access: command spies, synthetic Docker records and private temp files."""
import copy
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import sys
import tempfile
import unittest
from unittest.mock import patch

sys.path.insert(0, str(Path(__file__).parent))
spec = importlib.util.spec_from_file_location('web_only', Path(__file__).with_name('web_only.py'))
m = importlib.util.module_from_spec(spec); spec.loader.exec_module(m)


def container(role, key):
    return {'Id': key * 64, 'Image': 'sha256:' + key * 64,
            'Config': {'Env': [], 'Labels': {'com.docker.compose.service': role, 'com.docker.compose.project': 'fixture'},
                       'User': '101', 'Cmd': ['nginx']},
            'HostConfig': {}, 'Mounts': [],
            'NetworkSettings': {'Networks': {'fixture_default': {}}, 'Ports': {'8080/tcp': [{'HostPort': '12345'}]}},
            'State': {'Running': True, 'StartedAt': 'before', 'Health': {'Status': 'healthy'}, 'OOMKilled': False, 'ExitCode': 0}}


class WebOnly(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory(); self.addCleanup(self.tmp.cleanup)
        self.root = Path(self.tmp.name)
        self.p = patch.object(m, 'ROOT', self.root); self.p.start(); self.addCleanup(self.p.stop)
        self.old = container('web', 'a'); self.api = container('api', 'b')
        self.new = copy.deepcopy(self.old); self.new.update(Id='c' * 64, Image='sha256:' + 'd' * 64)
        self.plan = {'run': '1' * 32, 'unit': 'bb158-web-' + '1' * 32 + '.service', 'project': 'fixture',
            'originals': [self.old, self.api], 'candidate': self.new['Image'],
            'acceptedMain': 'approved', 'runtimeOverride': str(self.root / 'override.json'),
            'indexSha256': hashlib.sha256(b'<html>fixture</html>').hexdigest()}
        m.save(self.root / 'override.json', {'services': {'api': {'image': self.api['Image']}, 'web': {'image': self.old['Image']}}})
        self.plan['inputHashes'] = {self.plan['runtimeOverride']: m.digest(self.root / 'override.json')}
        self.calls = []

    def compose(self, plan, *args, **kwargs):
        self.calls.append(args)
        return b''

    def perform(self):
        class Response:
            status = 200
            def __enter__(s): return s
            def __exit__(s, *unused): pass
            def read(s, n): return b'<html>fixture</html>'
        with patch.object(m, 'preflight'), patch.object(m, 'compose', side_effect=self.compose), \
             patch.object(m, 'current_web', return_value=self.new), patch.object(m, 'unchanged'), \
             patch.object(m.urllib.request, 'urlopen', return_value=Response()):
            m.execute(self.plan, 'invocation', 'planhash')

    def test_success_only_replaces_web_preserves_api_pin(self):
        self.perform()
        self.assertEqual(self.calls, [('up', '-d', '--no-deps', '--no-build', '--pull', 'never', '--wait', '--wait-timeout', '100', 'web')])
        pins = json.loads((self.root / 'override.json').read_text())
        self.assertEqual(pins['services']['api']['image'], self.api['Image'])
        self.assertEqual(pins['services']['web']['image'], self.new['Image'])
        self.assertEqual(json.loads((self.root / 'complete.json').read_text())['run'], self.plan['run'])

    def test_retry_has_no_second_command(self):
        self.perform()
        with self.assertRaises(AssertionError): self.perform()
        self.assertEqual(len(self.calls), 1)

    def test_preflight_failure_cannot_replace(self):
        with patch.object(m, 'preflight', side_effect=AssertionError), patch.object(m, 'compose') as c:
            with self.assertRaises(AssertionError): m.execute(self.plan, 'i', 'h')
            c.assert_not_called()
        self.assertFalse((self.root / 'state.json').exists())

    def test_compose_failure_retains_write_ahead_no_success(self):
        with patch.object(m, 'preflight'), patch.object(m, 'compose', side_effect=RuntimeError):
            with self.assertRaises(RuntimeError): m.execute(self.plan, 'i', 'h')
        self.assertEqual(json.loads((self.root / 'state.json').read_text())['phase'], 'web-replacement-possible')
        self.assertFalse((self.root / 'complete.json').exists())

    def test_other_service_drift_blocks_before_action(self):
        changed = copy.deepcopy(self.api); changed['Image'] = 'unexpected'
        with patch.object(m, 'inspect', return_value=changed):
            with self.assertRaises(AssertionError): m.unchanged(self.plan)

    def test_candidate_wrong_image_privilege_or_network_rejected(self):
        for key, value in [('Image', 'wrong'), ('HostConfig', {'Privileged': True}),
                           ('NetworkSettings', {'Networks': {'other': {}}})]:
            with self.subTest(key=key):
                item = copy.deepcopy(self.new); item[key] = value
                with self.assertRaises(AssertionError): m.verify_web(self.plan, item, self.plan['candidate'])

    def mark(self, invocation='i', phase='web-replacement-possible'):
        m.record(self.plan, invocation, 'h', phase)

    def test_recovery_without_state_has_no_start(self):
        with patch.object(m, 'unchanged'), patch.object(m, 'docker') as d:
            m.recover(self.plan, 'i', 'h'); d.assert_not_called()

    def test_recovery_stale_invocation_rejected(self):
        self.mark('old')
        with patch.object(m, 'unchanged'), patch.object(m, 'docker') as d:
            with self.assertRaises(AssertionError): m.recover(self.plan, 'i', 'h')
            d.assert_not_called()

    def test_disconnect_recovery_retains_healthy_candidate_no_restore(self):
        self.mark()
        with patch.object(m, 'unchanged'), patch.object(m, 'current_web', return_value=self.new), patch.object(m, 'docker') as d:
            m.recover(self.plan, 'i', 'h'); d.assert_not_called()
        self.assertEqual(json.loads((self.root / 'recovery.json').read_text())['outcome'], 'CANDIDATE_HEALTHY_RETAINED')
        self.assertFalse((self.root / 'complete.json').exists())

    def test_recovery_can_only_start_exact_old_clean_web(self):
        self.mark(); stopped = copy.deepcopy(self.old); stopped['State']['Running'] = False
        with patch.object(m, 'unchanged'), patch.object(m, 'current_web', return_value=stopped), \
             patch.object(m, 'inspect', return_value=self.old), patch.object(m, 'docker') as d:
            m.recover(self.plan, 'i', 'h'); d.assert_called_once_with('start', self.old['Id'])

    def test_uncertain_old_exit_or_changed_configuration_never_started(self):
        self.mark()
        for drift in ('exit', 'config'):
            old = copy.deepcopy(self.old); old['State']['Running'] = False
            if drift == 'exit': old['State']['ExitCode'] = 137
            else: old['HostConfig']['Privileged'] = True
            with self.subTest(drift=drift), patch.object(m, 'unchanged'), patch.object(m, 'current_web', return_value=old), patch.object(m, 'docker') as d:
                with self.assertRaises(AssertionError): m.recover(self.plan, 'i', 'h')
                d.assert_not_called()

    def test_unhealthy_candidate_never_recreated_or_downgraded(self):
        self.mark(); new = copy.deepcopy(self.new); new['State']['Health']['Status'] = 'unhealthy'
        with patch.object(m, 'unchanged'), patch.object(m, 'current_web', return_value=new), patch.object(m, 'docker') as d:
            with self.assertRaises(AssertionError): m.recover(self.plan, 'i', 'h')
            d.assert_not_called()

    def test_systemd_detached_finite_and_cleanup_same_script(self):
        args = m.unit_arguments(self.plan)
        for flag in ('--no-block', '--collect', 'TimeoutStartSec=300s', 'TimeoutStopSec=90s', 'Restart=no', 'KillMode=control-group', 'NoNewPrivileges=yes', 'CapabilityBoundingSet='):
            self.assertIn(flag, args)
        self.assertTrue(any(x.startswith('ExecStopPost=') and x.endswith('web_only.py recover') for x in args))
        self.assertEqual(args[-1], 'work')

    def make_bundle(self):
        cfg = {'name': 'fixture', 'services': {'web': {'image': self.plan['candidate'], 'networks': {'default': None}}},
               'networks': {'default': {'name': 'fixture_default', 'external': True, 'ipam': {}}}}
        for name in ('execute.py', 'web_only.py', 'empty.env'): (self.root / name).write_bytes(b'')
        m.save(self.root / 'plan.json', self.plan); m.save(self.root / 'web-compose.json', cfg)
        self.seal()
        return cfg

    def seal(self):
        names = ('execute.py', 'web_only.py', 'empty.env', 'plan.json', 'web-compose.json')
        for name in names: (self.root / name).chmod(0o600)
        m.save(self.root / 'package.json', {name: m.digest(self.root / name) for name in names})

    def test_bundle_rejects_other_services_dependencies_mounts_or_managed_network(self):
        cfg = self.make_bundle(); m.bundle()
        for change in ('api', 'depends_on', 'volumes', 'external'):
            altered = copy.deepcopy(cfg)
            if change == 'api': altered['services']['api'] = {}
            elif change == 'external': altered['networks']['default']['external'] = False
            else: altered['services']['web'][change] = []
            m.save(self.root / 'web-compose.json', altered); self.seal()
            with self.subTest(change=change), self.assertRaises(AssertionError): m.bundle()

    def test_package_change_rejected(self):
        self.make_bundle(); (self.root / 'web_only.py').write_text('changed')
        with self.assertRaises(AssertionError): m.bundle()

    def test_launch_queues_once_does_not_call_docker_mutation(self):
        with patch.object(m, 'bundle', return_value=(self.plan, 'h')), patch.object(m, 'preflight'), \
             patch.object(m.sys, 'argv', ['web_only.py', 'launch']), patch.object(m.subprocess, 'run') as run:
            m.main()
            self.assertEqual(run.call_count, 2)
            self.assertEqual(run.call_args_list[0].args[0], ['sudo', '-v'])
            self.assertEqual(run.call_args_list[1].args[0], m.unit_arguments(self.plan))
            with self.assertRaises(AssertionError): m.main()
            self.assertEqual(run.call_count, 2)
        self.assertFalse((self.root / 'complete.json').exists())

    def test_override_drift_cannot_be_overwritten_after_replacement(self):
        # Another operator change must survive even if Web itself becomes healthy.
        (self.root / 'override.json').write_text('{"services":{"api":{"image":"changed"}}}')
        with self.assertRaises(AssertionError): self.perform()
        self.assertIn('changed', (self.root / 'override.json').read_text())
        self.assertFalse((self.root / 'complete.json').exists())

    def test_wrong_served_index_prevents_pin_and_completion(self):
        self.plan['indexSha256'] = 'incorrect'
        before = (self.root / 'override.json').read_bytes()
        with self.assertRaises(AssertionError): self.perform()
        self.assertEqual((self.root / 'override.json').read_bytes(), before)
        self.assertFalse((self.root / 'complete.json').exists())

    def test_optimized_python_refused_before_any_preflight(self):
        with patch.object(m.sys, 'flags', type('Flags', (), {'optimize': 1})()), patch.object(m, 'bundle') as b:
            with self.assertRaisesRegex(RuntimeError, 'OptimizedPythonNotAllowed'): m.main()
            b.assert_not_called()

    def test_compose_prefix_is_local_explicit_and_not_plan_controlled(self):
        self.plan['composeCommand'] = ['dangerous']
        with patch.object(m, 'docker', return_value=b'') as d:
            m.compose(self.plan, 'ps', '-q', 'web')
            args = d.call_args.args
            self.assertEqual(args[0], 'compose'); self.assertNotIn('dangerous', args)
            self.assertEqual(args[-3:], ('ps', '-q', 'web'))


if __name__ == '__main__': unittest.main()
