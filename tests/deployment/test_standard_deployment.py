"""Offline T2 fault tests: synthetic SQLite/WAL and simulated Docker only."""
import copy
from contextlib import closing
import io
import importlib.util
import json
import os
import runpy
from pathlib import Path
import sqlite3
import shutil
import stat
import subprocess
import sys
import tarfile
import tempfile
import threading
import time
import uuid
import unittest
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location('standard', ROOT / 'scripts/deploy-standard.py')
d = importlib.util.module_from_spec(spec)
spec.loader.exec_module(d)
GIT_GUARD = d.git_guard
REV = 'a' * 40
OLD = 'sha256:' + 'b' * 64
NEW = 'sha256:' + 'c' * 64


def container(role):
    return {'Id': role, 'Image': OLD, 'Config': {'Env': [], 'Labels': {
        'com.docker.compose.config-hash': role + '-hash', 'org.opencontainers.image.revision': REV}},
        'State': {'Running': True, 'OOMKilled': False, 'ExitCode': 0,
                  'Health': {'Status': 'healthy'}, 'StartedAt': 'original'},
        'HostConfig': {'Binds': []}, 'Mounts': [], 'NetworkSettings': {'Networks': {}}}


class StandardTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.root = Path(self.tmp.name)
        self.state = self.root / 'state'
        self.state.mkdir(mode=0o700)
        self.source = self.root / 'compose.json'
        self.source.write_text('{}')
        self.dep = d.Deployment(REV, {r: NEW for r in d.ROLES}, self.source, self.state)
        self.calls = []
        self.runtime = {r: container(r) for r in d.ROLES}
        self.config = {'name': d.PROJECT, 'services': {
            r: {'image': OLD, 'environment': {'LITERAL': 'a$$HOME$${SECRET}b'}, 'networks': {'default': None}}
            for r in d.ROLES}, 'networks': {'default': {'name': 'existing'}}, 'volumes': {}}
        self.roots = {}
        self.databases = []
        for target, names in d.backup.EXPECTED.items():
            folder = self.root / target.lstrip('/')
            folder.mkdir()
            self.roots[target] = (folder, target.lstrip('/'))
            self.runtime['api']['Mounts'].append({'Type': 'volume', 'Destination': target,
                'Name': target.lstrip('/'), 'Source': str(folder), 'RW': True})
            for name in names:
                con = sqlite3.connect(folder / name)
                con.execute('PRAGMA journal_mode=WAL')
                con.execute('CREATE TABLE household(id INTEGER PRIMARY KEY, value TEXT)')
                con.execute("INSERT INTO household VALUES(1, 'before')")
                con.commit()
                self.databases.append(con)
        self.dep.originals = copy.deepcopy(self.runtime)
        self.dep.other_snapshot = {'untouched': 'original'}
        self.dep.config = self.config
        self.dep.release = d.release_config(self.config, {r: NEW for r in d.ROLES})
        self.dep.rollback = d.release_config(self.config, {r: OLD for r in d.ROLES})
        self.dep.roots = self.roots
        self.dep.inventory, _ = d.backup.inventory(self.roots)
        self.dep.schemas = d.schemas(self.roots)
        self.dep.data = self.offline_snapshot()
        self.patches = [
            patch.object(d, 'command', side_effect=AssertionError('No real command')),
            patch.object(d, 'docker', side_effect=self.docker),
            patch.object(d, 'inspect', side_effect=lambda cid: copy.deepcopy(next(c for c in self.runtime.values() if c['Id'] == cid))),
            patch.object(d, 'git_guard', return_value={}), patch.object(d, 'http_health'),
            patch.object(self.dep, 'current', side_effect=lambda role: copy.deepcopy(self.runtime[role])),
            patch.object(self.dep, 'others', return_value=self.dep.other_snapshot),
            patch.object(self.dep, 'compose', side_effect=self.compose),
            patch.object(self.dep, 'wait_healthy', side_effect=self.wait_healthy),
            patch.object(self.dep, 'build_images'),
            patch.object(self.dep, 'prove_restartability'),
            patch.object(self.dep, 'backup_data', side_effect=self.offline_backup),
            patch.object(self.dep, 'helper_data', side_effect=self.offline_snapshot),
            patch.object(d, 'volume_sources', return_value={'Mounts': []}),
        ]
        for p in self.patches:
            p.start()
        self.fail = None

    def offline_snapshot(self):
        inventory, _ = d.backup.inventory(self.roots)
        return json.loads(json.dumps(dict(directories={}, inventory=inventory, schemas=d.schemas(self.roots))))

    def offline_backup(self):
        return d.backup.run_backup(self.roots, self.dep.folder / 'backups', {}, self.dep.unchanged)

    def tearDown(self):
        for p in reversed(self.patches):
            p.stop()
        for con in self.databases:
            con.close()
        self.tmp.cleanup()

    def docker(self, *args, **kwargs):
        self.calls.append(('docker', args))
        if args[0] == 'stop':
            c = next(c for c in self.runtime.values() if c['Id'] == args[-1])
            c['State'].update(Running=False, Status='exited', Restarting=False)
            return b''
        if args[0] == 'diff':
            return b''
        if args[:2] in (('network', 'inspect'), ('volume', 'inspect')):
            return b'[]'
        raise AssertionError(args)

    def compose(self, path, *args, **kwargs):
        self.calls.append(('compose', args))
        if args[:2] == ('config', '--hash'):
            if args[-1] == 'web': return b'web web-hash\n'
            return b'api api-hash\nweb web-hash\n'
        if args[0] == 'config':
            if path == self.source:
                return json.dumps(self.config).encode()
            value = json.loads(path.read_text())
            return json.dumps(value).encode()
        self.assertEqual(args[:-1], ('up', '-d', '--no-deps', '--no-build', '--pull', 'never', '--force-recreate'))
        role = args[-1]
        self.assertIn(role, d.ROLES)
        image = OLD if path.name == 'rollback.json' else NEW
        self.runtime[role]['Image'] = image
        self.runtime[role]['Id'] = role + '-' + str(len(self.calls))
        self.runtime[role]['State'].update(Running=True, Status='running', ExitCode=0)
        if self.fail == ('crash', role) and image == NEW:
            self.runtime[role]['State'].update(Running=False, Status='exited', ExitCode=1)
            self.databases[0].execute("UPDATE household SET value='after-backup'")
            self.databases[0].commit()
        if self.fail == ('start', role) and image == NEW:
            raise d.Blocked('StartFailed')
        if self.fail == ('timeout', role) and image == NEW:
            raise d.Uncertain('CommandInterrupted')
        return b''

    def wait_healthy(self, role, image):
        self.assertEqual(self.runtime[role]['Image'], image)
        if self.fail == ('health', role) and image == NEW:
            raise d.Blocked('HealthDeadline')
        if self.fail == ('rollback', role) and image == OLD:
            raise d.Blocked('RollbackUnhealthy')

    def phases(self):
        return [json.loads(row)['phase'] for row in (self.dep.folder / 'journal.jsonl').read_text().splitlines()]

    def test_success_backup_precedes_only_api_web_switches(self):
        self.dep.execute()
        self.assertEqual(self.phases(), ['PREPARING', 'BUILDING', 'SWITCHING_API', 'SWITCHING_WEB', 'PASS'])
        receipt = json.loads((self.dep.folder / 'receipt.json').read_text())
        folder = self.dep.folder / receipt['backupPath']
        self.assertEqual(d.backup.verify_set(folder), receipt['backupManifestSha256'])
        ups = [args[-1] for cmd, args in self.calls if cmd == 'compose' and args[0] == 'up']
        self.assertEqual(ups, ['api', 'web'])
        self.assertEqual((self.dep.folder / 'receipt.json').stat().st_mode & 0o777, 0o600)

    def test_crashed_api_exit_one_restores_image_without_database_restore(self):
        self.fail = ('crash', 'api')
        self.dep.wait_healthy.side_effect = lambda role, image: d.Deployment.wait_healthy(self.dep, role, image)
        with self.assertRaisesRegex(d.Blocked, 'ReplacementExited'): self.dep.execute()
        self.assertEqual(self.phases()[-1], 'ROLLED_BACK')
        self.assertTrue(all(c['Image'] == OLD for c in self.runtime.values()))
        self.assertEqual(self.databases[0].execute('SELECT value FROM household').fetchone()[0], 'after-backup')
        self.assertEqual([a[-1] for cmd, a in self.calls if cmd == 'compose' and a[0] == 'up'], ['api', 'api'])

    def test_crashed_web_exit_one_restores_both_images(self):
        self.fail = ('crash', 'web')
        self.dep.wait_healthy.side_effect = lambda role, image: d.Deployment.wait_healthy(self.dep, role, image)
        with self.assertRaisesRegex(d.Blocked, 'ReplacementExited'): self.dep.execute()
        self.assertEqual(self.phases()[-1], 'ROLLED_BACK')
        self.assertTrue(all(c['Image'] == OLD for c in self.runtime.values()))
        self.assertEqual(self.databases[0].execute('SELECT value FROM household').fetchone()[0], 'after-backup')

    def test_restarting_failed_candidate_is_stopped_before_rollback(self):
        self.runtime['api'].update(Id='replacement', Image=NEW)
        self.runtime['api']['State'].update(Running=False, Status='restarting', Restarting=True, ExitCode=1)
        self.dep.stop('api', recovery=True)
        self.assertFalse(self.runtime['api']['State']['Restarting'])
        self.assertEqual(self.calls[-1], ('docker', ('stop', '--time', '-1', 'replacement')))

    def test_crashed_original_is_not_treated_as_failed_candidate(self):
        self.runtime['api']['State'].update(Running=False, Status='exited', ExitCode=1)
        with self.assertRaisesRegex(d.Blocked, 'UncleanStop'): self.dep.stop('api', recovery=True)

    def test_foreign_crashed_replacement_is_not_accepted(self):
        self.runtime['api'].update(Id='foreign', Image='sha256:' + 'd' * 64)
        self.runtime['api']['State'].update(Running=False, Status='exited', ExitCode=1)
        with self.assertRaisesRegex(d.Blocked, 'UncleanStop'): self.dep.stop('api', recovery=True)

    def test_build_failure_never_stops_original_services(self):
        self.dep.build_images.side_effect = d.Blocked('BuildFailed')
        with self.assertRaisesRegex(d.Blocked, 'BuildFailed'): self.dep.execute()
        self.assertEqual(self.phases()[-1], 'PRE_SWITCH_FAILED')
        self.assertTrue(all(c['State']['Running'] and c['Image'] == OLD for c in self.runtime.values()))
        self.assertFalse(any(a[0] in ('stop', 'up') for _, a in self.calls))

    def test_failed_restart_proof_blocks_production_switch(self):
        self.dep.prove_restartability.side_effect = d.Blocked('ProbeExited')
        with self.assertRaisesRegex(d.Blocked, 'ProbeExited'): self.dep.execute()
        self.assertEqual(self.phases()[-1], 'PRE_SWITCH_FAILED')
        self.assertTrue(all(c['State']['Running'] and c['Image'] == OLD for c in self.runtime.values()))
        self.assertFalse(any(a[0] in ('stop', 'up') for _, a in self.calls))

    def test_build_backup_proof_and_switch_are_one_ordered_flow(self):
        events = []
        self.dep.build_images.side_effect = lambda: events.append('build')
        def prove(folder):
            self.assertEqual(json.loads((folder / 'manifest.json').read_text())['status'], 'PASS')
            self.assertTrue(all(c['State']['Running'] and c['Image'] == OLD for c in self.runtime.values()))
            events.append('backup-and-proof')
        self.dep.prove_restartability.side_effect = prove
        original = self.compose
        def compose(path, *args, **kw):
            if args[0] == 'up': events.append('switch-' + args[-1])
            return original(path, *args, **kw)
        self.dep.compose.side_effect = compose
        self.dep.execute()
        self.assertEqual(events, ['build', 'backup-and-proof', 'switch-api', 'switch-web'])

    def test_build_uses_git_archive_and_pinned_bases_not_workspace(self):
        self.dep.folder = self.state / 'run-build'
        self.dep.folder.mkdir()
        calls = []
        def command(args, **kwargs):
            calls.append(args)
            if args[1] == 'archive':
                self.assertEqual(args[-1], REV)
                archive = Path(args[3].removeprefix('--output='))
                with tarfile.open(archive, 'w') as bundle:
                    for role in ('Api', 'Web'):
                        info = tarfile.TarInfo('src/BigBrain.' + role + '/Dockerfile')
                        body = b'FROM base:fixed AS runtime\n'
                        info.size = len(body)
                        bundle.addfile(info, io.BytesIO(body))
                return b''
            if args[1] == 'rev-parse': return b'git-tree'
            raise AssertionError(args)
        def docker(*args, **kwargs):
            calls.append(args)
            if args[0] == 'pull': return b''
            if args[:2] == ('image', 'inspect'):
                return json.dumps([{'RepoDigests': ['base@sha256:' + 'e' * 64]}]).encode()
            self.assertEqual(args[0], 'build')
            pinned = Path(args[args.index('--file') + 1])
            self.assertIn('FROM base@sha256:' + 'e' * 64, pinned.read_text())
            self.assertEqual(args[-1], str(self.dep.folder / 'source'))
            self.assertIn('org.opencontainers.image.revision=' + REV, args)
            Path(args[args.index('--iidfile') + 1]).write_text(NEW)
            self.assertTrue(all(c['State']['Running'] and c['Image'] == OLD for c in self.runtime.values()))
            return b''
        with patch.object(d, 'command', side_effect=command), patch.object(d, 'docker', side_effect=docker), \
             patch.object(d, 'image_info'), patch.object(self.dep, 'bind_images'):
            d.Deployment.build_images(self.dep)
        self.assertEqual(len([a for a in calls if a[0] == 'build']), 2)
        self.assertEqual(len([a for a in calls if a[0] == 'pull']), 1)
        receipt = json.loads((self.dep.folder / 'build.json').read_text())
        self.assertEqual(receipt['revision'], REV)
        self.assertEqual(receipt['images'], {'api': NEW, 'web': NEW})
        self.assertEqual(receipt['bases']['base:fixed'], 'base@sha256:' + 'e' * 64)

    def test_compose_inputs_discovered_without_prepared_json(self):
        env_file = self.root / 'private.env'
        env_file.write_text('LOCAL=value')
        for c in self.runtime.values():
            c['Config']['Labels'].update({
                'com.docker.compose.project.environment_file': str(env_file),
                'com.docker.compose.project.config_files': str(self.source) + ',' + str(self.root / 'override.yaml'),
                'com.docker.compose.project.working_dir': str(self.root)})
        self.dep.source = None
        self.dep.discover_compose()
        self.assertEqual(self.dep.source_files, [self.source, self.root / 'override.yaml'])
        self.assertEqual(self.dep.project_directory, self.root)
        self.assertEqual(self.dep.env_files, [env_file])

    def test_mismatched_runtime_compose_inputs_are_rejected(self):
        self.runtime['api']['Config']['Labels']['com.docker.compose.project.config_files'] = '/one'
        self.dep.source = None
        with self.assertRaisesRegex(d.Blocked, 'ComposeProvenanceMissing'): self.dep.discover_compose()

    def mixed_compose(self):
        self.source = self.root / 'compose.yaml'
        self.source.write_text('{}')
        for role, c in self.runtime.items():
            c['Config']['Labels'].update({
                'com.docker.compose.project.config_files': str(self.source) if role == 'web' else
                    '/var/lib/bigbrain-approved-ops/runs/legacy/release.compose.json',
                'com.docker.compose.project.working_dir': str(self.root)})
        self.runtime['api']['Config']['Env'] = ['SECRET=literal$VALUE${name}',
            'Finance__Enabled=false', 'Media__Librarr__DirectAudioImportEnabled=true']
        self.dep.source = None
        self.config['services']['api'].update(user='1654', command=['original'],
            volumes=[{'type': 'bind', 'source': '/original', 'target': '/original', 'read_only': True}],
            environment={'Media__Librarr__DirectAudioImportEnabled': 'false'})

    def model(self, config, *args):
        if args[:2] == ('config', '--hash'): return b'api api-hash\nweb web-hash\n'
        return json.dumps(config).encode()

    def test_mixed_compose_preserves_runtime_environment_and_all_other_inputs(self):
        self.mixed_compose()
        previous = copy.deepcopy(self.config)
        with patch.object(self.dep, 'compose_model', side_effect=self.model):
            result = self.dep.configuration(self.runtime)
        api = result['services']['api']
        self.assertEqual(api['environment'], {'SECRET': 'literal$$VALUE$${name}', 'Finance__Enabled': 'false',
            'Media__Librarr__DirectAudioImportEnabled': 'true'})
        for key in ('user', 'command', 'volumes', 'networks'):
            self.assertEqual(api[key], previous['services']['api'][key])
        self.assertEqual(result['services']['web'], previous['services']['web'])
        self.assertEqual(result['networks'], previous['networks'])
        self.assertFalse(list(self.state.iterdir()))

    def test_reconstruction_does_not_accept_unmatched_runtime_hashes(self):
        for role in d.ROLES:
            with self.subTest(role=role):
                self.mixed_compose()
                self.runtime[role]['Config']['Labels']['com.docker.compose.config-hash'] = 'wrong'
                with patch.object(self.dep, 'compose_model', side_effect=self.model):
                    with self.assertRaisesRegex(d.Blocked, 'ComposeRuntimeMismatch'):
                        self.dep.configuration(self.runtime)
                self.runtime[role]['Config']['Labels']['com.docker.compose.config-hash'] = role + '-hash'

    def test_reconstruction_rejects_missing_duplicate_or_malformed_environment(self):
        for env in ([], ['NO_VALUE'], ['X=one', 'X=two'], [None]):
            with self.subTest(env=env):
                self.mixed_compose()
                self.runtime['api']['Config']['Env'] = env
                with self.assertRaisesRegex(d.Blocked, 'RuntimeEnvironment'):
                    self.dep.configuration(self.runtime)

    def test_reconstruction_rejects_unrecognized_origin_or_changed_snapshot(self):
        self.mixed_compose()
        self.runtime['api']['Config']['Labels']['com.docker.compose.project.config_files'] = '/unknown/compose.yaml'
        with self.assertRaisesRegex(d.Blocked, 'ComposeProvenanceMissing'): self.dep.configuration(self.runtime)
        self.mixed_compose()
        def changed(config, *args):
            if args[:2] == ('config', '--hash'): return b'api api-hash\nweb web-hash\n'
            changed = copy.deepcopy(config)
            changed['services']['api']['user'] = '0'
            return json.dumps(changed).encode()
        with patch.object(self.dep, 'compose_model', side_effect=changed):
            with self.assertRaisesRegex(d.Blocked, 'SnapshotChanged'): self.dep.configuration(self.runtime)

    def test_private_snapshot_mode_does_not_depend_on_callers_umask(self):
        previous = os.umask(0)
        try: d.save(self.state / 'private.json', {'secret': 'synthetic'})
        finally: os.umask(previous)
        self.assertEqual((self.state / 'private.json').stat().st_mode & 0o777, 0o600)

    def test_changed_build_provenance_stops_before_container_switch(self):
        with patch.object(d, 'git_guard', return_value={'web': 'changed'}):
            with self.assertRaisesRegex(d.Blocked, 'BuildProvenanceChanged'): self.dep.execute()
        self.assertEqual(self.phases()[-1], 'PRE_SWITCH_FAILED')
        self.assertFalse(any(cmd == 'compose' and args[0] == 'up' for cmd, args in self.calls))

    def test_timesync_identity_is_rechecked_before_and_after_switch(self):
        self.dep.timesync = (9, 42)
        with patch.object(d, 'timesync_identity', return_value=(9, 43)):
            with self.assertRaisesRegex(d.Blocked, 'TimesyncIdentityMismatch'): self.dep.unchanged()
            with self.assertRaisesRegex(d.Blocked, 'TimesyncIdentityMismatch'):
                d.Deployment.wait_healthy(self.dep, 'api', OLD)

    def test_disappeared_explicit_env_file_is_not_replaced_with_empty_input(self):
        self.dep.env_files = [self.root / 'missing.env']
        with self.assertRaisesRegex(d.Blocked, 'ComposeEnvironmentMissing'):
            d.Deployment.compose(self.dep, self.source, 'config', '--format', 'json')

    def test_readonly_preflight_without_images_does_not_build(self):
        self.dep.images = {}
        self.preflight()
        self.dep.build_images.assert_not_called()
        self.dep.prove_restartability.assert_not_called()

    def test_proof_covers_old_and_new_images_for_both_roles(self):
        self.dep.folder = self.state / 'run-proof'
        self.dep.folder.mkdir()
        with patch.object(self.dep, 'probe_image') as probe, patch.object(d.backup, 'verify_set', return_value='digest'):
            d.Deployment.prove_restartability(self.dep, self.root)
        self.assertEqual([(c.args[0], c.args[1], c.args[3]) for c in probe.call_args_list],
                         [('api', OLD, 'rollback'), ('api', NEW, 'candidate'), ('web', OLD, 'rollback'), ('web', NEW, 'candidate')])
        self.assertTrue((self.dep.folder / 'restartability.json').is_file())

    def probe(self, role='api', crash_on_start=0, stop_timeout=False):
        self.dep.folder = self.state / 'run-probe'
        self.dep.folder.mkdir()
        self.dep.originals[role]['Config']['User'] = '1654'
        self.dep.originals[role]['Config']['Env'] = ['Finance__ObservationRuntime__Enabled=true', 'MealPlanner__SchoolMeals__Enabled=true']
        backup_folder = d.backup.run_backup(self.roots, self.state / 'backups', {}, lambda: None)
        original_hash = d.backup.verify_set(backup_folder)
        cid = 'f' * 64
        probe_state = {'Image': OLD, 'Config': {'User': '1654'},
                       'HostConfig': {'NetworkMode': 'none', 'PortBindings': {}},
                       'State': {'Running': False, 'OOMKilled': False, 'ExitCode': 0}}
        calls = []
        starts = 0
        def docker(*args, **kwargs):
            nonlocal starts
            calls.append(args)
            if args[0] == 'create': return cid.encode()
            self.assertEqual(args[1] if args[0] in ('start', 'exec') else args[-1], cid)
            if args[0] == 'start':
                starts += 1
                probe_state['State']['Running'] = starts != crash_on_start
                probe_state['State']['ExitCode'] = 1 if starts == crash_on_start else 0
            if args[0] == 'stop':
                if stop_timeout: raise d.Uncertain('CommandInterrupted')
                probe_state['State']['Running'] = False
            return b''
        with patch.object(d, 'docker', side_effect=docker), patch.object(d, 'inspect', return_value=probe_state):
            if stop_timeout:
                with self.assertRaises(d.Uncertain):
                    self.dep.probe_image(role, OLD, backup_folder, 'rollback')
            elif crash_on_start:
                with self.assertRaisesRegex(d.Blocked, 'ProbeExited'):
                    self.dep.probe_image(role, OLD, backup_folder, 'rollback')
            else:
                self.dep.probe_image(role, OLD, backup_folder, 'rollback')
        self.assertEqual(d.backup.verify_set(backup_folder), original_hash)
        self.assertEqual(probe_state['State']['Running'], stop_timeout)
        return calls

    def test_restart_probe_starts_real_entrypoint_twice_with_only_copies(self):
        calls = self.probe()
        create = calls[0]
        self.assertNotIn('--entrypoint', create)
        self.assertNotIn('--publish', create)
        self.assertNotIn('--privileged', create)
        self.assertEqual(create[create.index('--network') + 1], 'none')
        self.assertEqual(create[create.index('--user') + 1], '1654')
        mounts = [create[i + 1] for i, arg in enumerate(create) if arg == '--mount']
        self.assertEqual(len(mounts), 5)
        for mount in mounts:
            self.assertIn(str(self.dep.folder / 'probe-api-rollback'), mount)
        env = Path(create[create.index('--env-file') + 1]).read_text()
        self.assertIn('Finance__ObservationRuntime__Enabled=false', env)
        self.assertIn('MealPlanner__SchoolMeals__Enabled=false', env)
        self.assertEqual(len([a for a in calls if a[0] == 'start']), 2)
        self.assertEqual(len([a for a in calls if a[0] == 'stop']), 2)
        self.assertNotIn('rm', [a[0] for a in calls])

    def test_rollback_probe_detects_image_that_fails_on_second_start(self):
        calls = self.probe(crash_on_start=2)
        self.assertEqual(len([a for a in calls if a[0] == 'start']), 2)
        self.assertFalse((self.dep.folder / 'restartability.json').exists())

    def test_uncertain_probe_stop_is_not_retried(self):
        calls = self.probe(stop_timeout=True)
        self.assertEqual(len([a for a in calls if a[0] == 'stop']), 1)

    def test_web_probe_requires_no_network_or_production_mount(self):
        create = self.probe(role='web')[0]
        self.assertNotIn('--mount', create)
        self.assertEqual(create[create.index('--add-host') + 1], 'api:127.0.0.1')

    def test_schema_drift_never_downgrades_or_restores_database(self):
        def check(role, image):
            if role == 'api' and image == NEW:
                self.databases[0].execute('CREATE TABLE unexpected(id INTEGER)')
                self.databases[0].commit()
                raise d.Blocked('HealthDeadline')
        self.dep.wait_healthy.side_effect = check
        with self.assertRaisesRegex(d.Blocked, 'RollbackFailed'): self.dep.execute()
        self.assertEqual(self.phases()[-1], 'MANUAL_REVIEW_REQUIRED')
        self.assertEqual([a[-1] for cmd, a in self.calls if cmd == 'compose' and a[0] == 'up'], ['api'])
        self.assertTrue(self.databases[0].execute("SELECT name FROM sqlite_schema WHERE name='unexpected'").fetchone())

    def test_unclean_stop_cannot_force_recreate(self):
        self.runtime['api']['State']['ExitCode'] = 137
        with self.assertRaises(d.Blocked): self.dep.execute()
        self.assertFalse(any(cmd == 'compose' and a[0] == 'up' for cmd, a in self.calls))
        self.assertEqual(self.phases()[-1], 'MANUAL_REVIEW_REQUIRED')

    def test_snapshot_drift_blocks_before_backup_or_switch(self):
        original = self.compose
        def altered(path, *args, **kwargs):
            result = original(path, *args, **kwargs)
            if path.name == 'release.json':
                obj = json.loads(result)
                obj['services']['api']['environment']['LITERAL'] = 'changed'
                return json.dumps(obj).encode()
            return result
        self.dep.compose.side_effect = altered
        with self.assertRaisesRegex(d.Blocked, 'SnapshotChanged'): self.dep.execute()
        self.assertFalse((self.state / 'backups').exists())
        self.assertEqual(self.phases()[-1], 'PRE_SWITCH_FAILED')

    def test_api_health_failure_rolls_back_without_touching_web(self):
        self.fail = ('health', 'api')
        with self.assertRaises(d.Blocked): self.dep.execute()
        self.assertEqual(self.phases()[-1], 'ROLLED_BACK')
        self.assertTrue(all(c['Image'] == OLD for c in self.runtime.values()))
        self.assertEqual([a[-1] for cmd, a in self.calls if cmd == 'compose' and a[0] == 'up'], ['api', 'api'])

    def test_web_start_failure_restores_both_in_order(self):
        self.fail = ('start', 'web')
        with self.assertRaises(d.Blocked): self.dep.execute()
        self.assertEqual(self.phases()[-1], 'ROLLED_BACK')
        self.assertEqual([a[-1] for cmd, a in self.calls if cmd == 'compose' and a[0] == 'up'], ['api', 'web', 'api', 'web'])

    def test_web_health_failure_preserves_writes_after_backup(self):
        def check(role, image):
            if role == 'web' and image == NEW:
                self.databases[0].execute("UPDATE household SET value='after'")
                self.databases[0].commit()
                raise d.Blocked('HealthDeadline')
        self.dep.wait_healthy.side_effect = check
        with self.assertRaises(d.Blocked): self.dep.execute()
        self.assertEqual(self.phases()[-1], 'ROLLED_BACK')
        self.assertEqual(self.databases[0].execute('SELECT value FROM household').fetchone()[0], 'after')

    def test_uncertain_timeout_never_retries_or_restores(self):
        self.fail = ('timeout', 'api')
        with self.assertRaises(d.Uncertain): self.dep.execute()
        self.assertEqual(self.phases()[-1], 'MANUAL_REVIEW_REQUIRED')
        self.assertEqual([a[-1] for cmd, a in self.calls if cmd == 'compose' and a[0] == 'up'], ['api'])
        with self.assertRaisesRegex(d.Blocked, 'PreviousRunNeedsReview'): d.no_pending_run(self.state)

    def test_backup_failure_blocks_all_mutations_and_retains_evidence(self):
        with patch.object(d.backup, 'verify_set', side_effect=d.backup.Blocked('ChecksumMismatch')):
            with self.assertRaises(d.backup.Blocked): self.dep.execute()
        self.assertEqual(self.phases()[-1], 'PRE_SWITCH_FAILED')
        self.assertFalse(any(a[0] in ('stop', 'up') for _, a in self.calls))
        self.assertTrue(list((self.dep.folder / 'backups').glob('*/manifest.json')))

    def test_failed_rollback_requires_manual_review(self):
        def check(role, image):
            raise d.Blocked('HealthDeadline')
        self.dep.wait_healthy.side_effect = check
        with self.assertRaisesRegex(d.Blocked, 'RollbackFailed'): self.dep.execute()
        self.assertEqual(self.phases()[-1], 'MANUAL_REVIEW_REQUIRED')

    def test_sigterm_during_switch_never_blindly_rolls_back(self):
        self.dep.wait_healthy.side_effect = KeyboardInterrupt()
        with self.assertRaises(KeyboardInterrupt): self.dep.execute()
        self.assertEqual(self.phases()[-1], 'MANUAL_REVIEW_REQUIRED')

    def test_other_service_drift_blocks_before_backup(self):
        self.dep.others.return_value = {'untouched': 'changed'}
        with self.assertRaisesRegex(d.Blocked, 'OtherServiceChanged'): self.dep.execute()
        self.assertFalse(any(a[0] in ('stop', 'up') for _, a in self.calls))

    def test_compose_literal_dollars_are_preserved(self):
        self.dep.execute()
        self.assertEqual(json.loads((self.dep.folder / 'release.json').read_text())['services']['api']['environment']['LITERAL'],
                         'a$$HOME$${SECRET}b')

    def test_incomplete_run_blocks_another_attempt(self):
        (self.state / 'run-interrupted').mkdir()
        with self.assertRaisesRegex(d.Blocked, 'PreviousRunNeedsReview'): self.dep.execute()
        self.assertEqual(self.calls, [])

    def test_existing_artifact_is_not_overwritten(self):
        path = self.state / 'saved.json'
        d.save(path, {'original': True})
        with self.assertRaises(FileExistsError): d.save(path, {'original': False})
        self.assertEqual(json.loads(path.read_text()), {'original': True})

    def preflight(self, bad=None):
        def images(image):
            return {'Id': image, 'Config': {'Labels': {'org.opencontainers.image.revision': REV}}}
        with patch.object(d, 'image_info', side_effect=images):
            if bad:
                bad()
            self.dep.preflight()

    def test_preflight_is_read_only(self):
        self.preflight()
        self.assertFalse(list(self.state.iterdir()))
        self.assertFalse(any(a[0] in ('stop', 'up') for _, a in self.calls))

    def test_mismatched_compose_is_rejected(self):
        self.runtime['api']['Config']['Labels']['com.docker.compose.config-hash'] = 'different'
        with self.assertRaisesRegex(d.Blocked, 'ComposeRuntimeMismatch'): self.preflight()

    def test_unproven_runtime_command_override_blocks_preflight(self):
        self.runtime['api']['Config']['Cmd'] = ['custom-runtime-mode']
        with self.assertRaisesRegex(d.Blocked, 'RuntimeEntrypointOverride'): self.preflight()

    def test_repaired_container_cannot_be_rollback_image(self):
        with patch.object(d, 'docker', return_value=b'C /app/appsettings.json\n'):
            with self.assertRaisesRegex(d.Blocked, 'RollbackImageHasRuntimeRepairs'): self.preflight()

    def test_old_bootstrap_is_not_silently_carried_forward(self):
        self.runtime['api']['Config']['Env'] = ['MealPlanner__SchoolMeals__RosenfeldtVerifiedInitialNotBeforeUtc=old']
        with self.assertRaisesRegex(d.Blocked, 'LegacyBootstrapPresent'): self.preflight()

    def test_source_symlink_rejected(self):
        link = self.root / 'alias.json'
        link.symlink_to(self.source)
        self.dep.source = link
        self.dep.source_files = [link]
        with self.assertRaises(d.backup.Blocked): self.dep.preflight()

    def test_candidate_revision_is_checked(self):
        def images(image):
            return {'Config': {'Labels': {'org.opencontainers.image.revision': REV} if image == OLD else {}}}
        with patch.object(d, 'image_info', side_effect=images):
            with self.assertRaisesRegex(d.Blocked, 'ImageRevisionMismatch'): self.dep.preflight()

    def test_runtime_label_cannot_replace_rollback_image_revision(self):
        for role in d.ROLES:
            with self.subTest(role=role):
                self.runtime[role]['Config']['Labels']['org.opencontainers.image.revision'] = 'd' * 40
                with self.assertRaisesRegex(d.Blocked, 'OldImageRevisionMismatch'): self.preflight()
                self.runtime[role]['Config']['Labels']['org.opencontainers.image.revision'] = REV

    def test_image_entrypoint_change_rejected(self):
        def images(image):
            return {'Config': {'Labels': {'org.opencontainers.image.revision': REV},
                    'Entrypoint': ['different'] if image == NEW else None}}
        with patch.object(d, 'image_info', side_effect=images):
            with self.assertRaisesRegex(d.Blocked, 'ImageRuntimeContractChanged'): self.dep.preflight()

    def test_resource_snapshot_is_external_and_only_api_web(self):
        cfg = copy.deepcopy(self.config)
        cfg['services']['finance'] = {'image': 'unrelated'}
        cfg['services']['api']['build'] = '.'
        cfg['services']['api']['depends_on'] = {'finance': {}}
        cfg['volumes']['data'] = {'name': 'existing-data', 'driver': 'local'}
        result = d.release_config(cfg, {r: NEW for r in d.ROLES})
        self.assertEqual(set(result['services']), {'api', 'web'})
        self.assertEqual(result['volumes'], {'data': {'name': 'existing-data', 'external': True}})
        self.assertNotIn('build', result['services']['api'])
        self.assertNotIn('depends_on', result['services']['api'])
        self.assertEqual(cfg['services']['api']['build'], '.')

    def test_unsupported_configuration_cannot_expand_scope(self):
        for key in ('profiles', 'extends', 'configs', 'secrets', 'develop', 'links', 'volumes_from'):
            cfg = copy.deepcopy(self.config)
            cfg['services']['api'][key] = []
            with self.assertRaisesRegex(d.Blocked, 'UnsupportedCompose'): d.release_config(cfg, {r: NEW for r in d.ROLES})

    def test_unpinned_images_rejected_without_docker(self):
        for image in ('latest', 'repo:v1', 'sha256:short'):
            with self.assertRaisesRegex(d.Blocked, 'UnpinnedImage'): d.image_info(image)
        self.assertEqual(self.calls, [])

    def test_ci_requires_all_four_successful_checks(self):
        good = {'total_count': 4, 'check_runs': [
            {'name': n, 'app': {'slug': 'github-actions'}, 'status': 'completed', 'conclusion': 'success'} for n in d.CHECKS]}
        with patch.object(d, 'command', return_value=json.dumps(good).encode()): d.ci_green(REV)
        for fault in ('missing', 'failure', 'pending', 'wrong-app', 'pagination'):
            value = copy.deepcopy(good)
            if fault == 'missing': value['check_runs'].pop()
            if fault == 'failure': value['check_runs'][0]['conclusion'] = 'failure'
            if fault == 'pending': value['check_runs'][0]['status'] = 'in_progress'
            if fault == 'wrong-app': value['check_runs'][0]['app']['slug'] = 'other'
            if fault == 'pagination': value['total_count'] = 101
            with patch.object(d, 'command', return_value=json.dumps(value).encode()):
                with self.assertRaises(d.Blocked): d.ci_green(REV)

    def test_git_guard_rejects_finance_schema_network_privilege_changes(self):
        def fake(args, *a):
            if args[1] == 'ls-remote': return (REV + '\trefs/heads/main').encode()
            if args[1] == 'rev-parse': return REV.encode()
            if args[1] == 'cat-file': return b'commit\n'
            if args[1] == 'diff': return (self.path + '\0').encode()
            return b''
        with patch.object(d, 'command', side_effect=fake), patch.object(d, 'ci_green'):
            for self.path in ('src/BigBrain.Finance.Hosting/Program.cs', 'compose.yaml', 'src/BigBrain.Api/MealPlanner/MealPlannerStore.cs', 'infrastructure/sudoers'):
                with self.assertRaisesRegex(d.Blocked, 'OutsideStandardScope'): GIT_GUARD(REV, self.dep.originals)
            self.path = 'src/BigBrain.Api/MealPlanner/SchoolMealService.cs'
            GIT_GUARD(REV, self.dep.originals)

    def test_archived_gateway_and_worker_reject_formerly_valid_entrypoints(self):
        for filename, args in (('bigbrain-approved-ops', ['start-approved']),
                               ('school_lunch_worker.py', ['work', '/unused', '0' * 64])):
            namespace = runpy.run_path(str(ROOT / 'scripts/approved-ops' / filename))
            with patch.object(sys, 'argv', [filename, *args]), patch.object(os, 'geteuid', return_value=0):
                with self.assertRaisesRegex(Exception, 'FROZEN'):
                    namespace['main']()

    def test_archive_entrypoints_fail_before_any_operation(self):
        for filename in ('bigbrain-approved-ops', 'school_lunch_worker.py', 'prepare_school_lunch_package.py'):
            # Only these local Python entrypoints; guards precede argument/root/host checks.
            result = subprocess.run([sys.executable, str(ROOT / 'scripts/approved-ops' / filename)], capture_output=True)
            self.assertNotEqual(result.returncode, 0)


class GitGuardTests(unittest.TestCase):
    """Actual local Git ancestry/deltas; no Docker or network requests."""

    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self.tmp.cleanup)
        self.root = Path(self.tmp.name)
        self.git('init', '-q')
        self.git('config', 'user.name', 'Deployment test')
        self.git('config', 'user.email', 'test@example.invalid')
        self.git('config', 'commit.gpgsign', 'false')
        self.git('config', 'core.hooksPath', '/dev/null')
        self.api_file = 'src/BigBrain.Api/MealPlanner/SchoolMealService.cs'
        self.web_file = 'src/BigBrain.Web/src/MediaDashboard.tsx'
        self.write(self.api_file, 'old API')
        self.write(self.web_file, 'old Web')
        self.api_base = self.commit()
        self.write(self.web_file, 'accepted Web-only text change')
        self.web_base = self.commit()
        self.write(self.api_file, 'memory fetch')
        self.revision = self.commit()
        self.originals = {r: container(r) for r in d.ROLES}
        for role, base in (('api', self.api_base), ('web', self.web_base)):
            self.originals[role]['Config']['Labels']['org.opencontainers.image.revision'] = base

    def git(self, *args):
        return subprocess.run(['git', *args], cwd=self.root, check=True, capture_output=True).stdout.decode().strip()

    def write(self, path, value):
        file = self.root / path
        file.parent.mkdir(parents=True, exist_ok=True)
        file.write_text(value)

    def commit(self):
        self.git('add', '.')
        self.git('commit', '-qm', 'fixture')
        return self.git('rev-parse', 'HEAD')

    def guard(self):
        real_command = d.command
        def command(args, *a, **kw):
            if args[:2] == ['git', 'ls-remote']:
                return (self.revision + '\trefs/heads/main').encode()
            return real_command(args, *a, **kw)
        with patch.object(d, 'ROOT', self.root), patch.object(d, 'command', side_effect=command), \
                patch.object(d, 'MEMORY_FETCH', self.api_base), patch.object(d, 'ci_green') as ci:
            GIT_GUARD(self.revision, self.originals)
            ci.assert_called_once_with(self.revision)

    def test_accepted_web_delta_is_compared_to_web_baseline_not_older_api(self):
        self.assertIn(self.web_file, self.git('diff', '--name-only', self.api_base, self.revision))
        self.assertNotIn(self.web_file, self.git('diff', '--name-only', self.web_base, self.revision))
        self.guard()

    def test_new_web_delta_is_not_exempted_by_role_filter(self):
        for path in (self.web_file, 'src/BigBrain.Web/src/FinanceDashboard.tsx',
                     'src/BigBrain.Web/src/MediaImport.tsx', 'src/BigBrain.Web/Dockerfile',
                     'src/BigBrain.Web/nginx.conf', 'src/BigBrain.Web/package-lock.json'):
            with self.subTest(path=path):
                self.write(path, 'unapproved new Web change')
                self.revision = self.commit()
                with self.assertRaisesRegex(d.Blocked, 'OutsideStandardScope'): self.guard()
                # Restore the fixture's accepted contents before the next independent delta.
                if path == self.web_file:
                    self.write(path, 'accepted Web-only text change')
                else:
                    (self.root / path).unlink()

    def test_api_finance_schema_privilege_bb158_and_build_changes_still_block(self):
        for path in ('src/BigBrain.Api/MealPlanner/MealPlannerStore.cs',
                     'src/BigBrain.Api/Finance/FinanceSchemaMigrations.cs',
                     'src/BigBrain.Finance.Hosting/Program.cs', 'src/BigBrain.Api/Media/MediaImport.cs',
                     'src/BigBrain.Api/Program.cs', 'src/BigBrain.Api/Dockerfile',
                     'src/BigBrain.Api/appsettings.json', 'src/BigBrain.Modules/Finance/Authority.cs'):
            with self.subTest(path=path):
                self.write(path, 'out of scope')
                self.revision = self.commit()
                with self.assertRaisesRegex(d.Blocked, 'OutsideStandardScope'): self.guard()
                (self.root / path).unlink()

    def test_shared_inputs_and_infrastructure_remain_blocked(self):
        for path in ('compose.yaml', '.dockerignore', 'Directory.Build.props', 'global.json',
                     'infrastructure/sudoers', 'infrastructure/network.yaml'):
            with self.subTest(path=path):
                self.write(path, 'out of scope')
                self.revision = self.commit()
                with self.assertRaisesRegex(d.Blocked, 'OutsideStandardScope'): self.guard()
                (self.root / path).unlink()

    def test_missing_or_noncommit_web_revision_is_not_replaced_with_api_baseline(self):
        for revision in ('c4edb2fc1a693a7bdae4ab2bbc1c7fe63622290f',
                         self.git('rev-parse', 'HEAD:' + self.web_file)):
            with self.subTest(revision=revision):
                self.originals['web']['Config']['Labels']['org.opencontainers.image.revision'] = revision
                with self.assertRaisesRegex(d.Blocked, 'OldRevisionUnavailable'): self.guard()

    def test_local_unaccepted_revision_is_not_a_runtime_baseline(self):
        branch = self.git('commit-tree', 'HEAD^{tree}', '-p', self.api_base, '-m', 'unaccepted')
        self.originals['web']['Config']['Labels']['org.opencontainers.image.revision'] = branch
        with self.assertRaisesRegex(d.Blocked, 'OldRevisionNotAccepted'): self.guard()

    def test_both_runtime_baselines_are_required(self):
        del self.originals['web']
        with self.assertRaisesRegex(d.Blocked, 'MissingRuntimeBaseline'): self.guard()

    def test_proven_legacy_web_revision_is_checked_against_accepted_ancestry(self):
        labels = self.originals['web']['Config']['Labels']
        labels.update({'org.opencontainers.image.source': d.NGINX_SOURCE,
                       'org.opencontainers.image.revision': 'c4edb2fc1a693a7bdae4ab2bbc1c7fe63622290f'})
        with patch.object(d, 'legacy_web_provenance', return_value={'image': OLD, 'revision': self.web_base}) as proof:
            self.guard()
            proof.assert_called_once_with(OLD)
        branch = self.git('commit-tree', 'HEAD^{tree}', '-p', self.api_base, '-m', 'unaccepted')
        with patch.object(d, 'legacy_web_provenance', return_value={'image': OLD, 'revision': branch}):
            with self.assertRaisesRegex(d.Blocked, 'OldRevisionNotAccepted'): self.guard()
        with patch.object(d, 'legacy_web_provenance', side_effect=d.Blocked('BuildProvenanceAmbiguousOrMissing')):
            with self.assertRaisesRegex(d.Blocked, 'BuildProvenanceAmbiguousOrMissing'): self.guard()


class CompatibilityEvidenceTests(unittest.TestCase):
    def setUp(self):
        self.ref = 'a' * 25
        self.rows = [{'status': 'Completed', 'name': 'src/BigBrain.Web', 'ref': 'default/default/' + self.ref}]
        self.record = {'Ref': self.ref, 'Status': 'completed', 'Dockerfile': 'src/BigBrain.Web/Dockerfile',
            'VCSRevision': REV, 'VCSRepository': 'git@github.com:T-bear/BigBrain.git', 'Attachments': [
                {'Digest': OLD, 'Type': 'application/vnd.oci.image.index.v1+json'},
                {'Digest': NEW, 'Type': 'https://slsa.dev/provenance/v1'}]}
        self.proof = {'buildDefinition': {
            'buildType': 'https://github.com/moby/buildkit/blob/master/docs/attestations/slsa-definitions.md',
            'externalParameters': {'configSource': {'path': 'Dockerfile'}, 'request': {'root': {'request': {'args': {
            'vcs:revision': REV, 'vcs:source': self.record['VCSRepository']}}}}}}, 'runDetails': {'metadata': {
            'buildkit_metadata': {'vcs': {'revision': REV, 'source': self.record['VCSRepository']}},
            'buildkit_completeness': {'request': True}}}}
        self.calls = []

    def docker(self, *args, **kwargs):
        self.calls.append(args)
        self.assertEqual(args[:2], ('buildx', 'history'))
        self.assertIn('default', args)
        if args[2] == 'ls': return b'\n'.join(json.dumps(x).encode() for x in self.rows)
        if args[3] == 'attachment':
            self.assertEqual(args[-1], NEW)  # digest selection, never a movable type/tag
            return json.dumps(self.proof).encode()
        return json.dumps(self.record).encode()

    def prove(self):
        with patch.object(d, 'image_info', return_value={'Config': {'Labels': {'org.opencontainers.image.source': d.NGINX_SOURCE}}}), \
                patch.object(d, 'docker', side_effect=self.docker):
            return d.legacy_web_provenance(OLD)

    def test_buildkit_binds_exact_image_record_proof_and_revision(self):
        result = self.prove()
        self.assertEqual((result['image'], result['revision'], result['provenanceDigest']), (OLD, REV, NEW))
        self.assertEqual(len(self.calls), 3)

    def test_tag_label_or_other_image_cannot_substitute_for_build_result(self):
        for image in ('repo:latest', NEW):
            self.record['Attachments'][0]['Digest'] = image
            with self.assertRaisesRegex(d.Blocked, 'BuildProvenanceAmbiguousOrMissing'): self.prove()

    def test_missing_or_duplicate_matching_records_are_rejected(self):
        rows = self.rows
        for changed in ([], rows + rows):
            self.rows = changed
            with self.assertRaisesRegex(d.Blocked, 'BuildProvenanceAmbiguousOrMissing'): self.prove()

    def test_wrong_repository_revision_dockerfile_or_build_state_blocks(self):
        for key, bad in (('VCSRepository', 'https://example.invalid/repo'), ('VCSRevision', REV + '-dirty'),
                         ('Dockerfile', 'unknown/Dockerfile'), ('Status', 'error'), ('Ref', 'b' * 25)):
            with self.subTest(key=key):
                previous = self.record[key]
                self.record[key] = bad
                with self.assertRaises(d.Blocked): self.prove()
                self.record[key] = previous

    def test_missing_or_conflicting_attestation_blocks(self):
        attestation = self.record['Attachments'].pop()
        with self.assertRaisesRegex(d.Blocked, 'BuildProvenanceMissing'): self.prove()
        self.record['Attachments'].append(attestation)
        vcs = self.proof['runDetails']['metadata']['buildkit_metadata']['vcs']
        for key in ('revision', 'source'):
            original = vcs[key]
            vcs[key] = 'contradiction'
            with self.assertRaisesRegex(d.Blocked, 'BuildProvenanceMismatch'): self.prove()
            vcs[key] = original
        self.proof['runDetails']['metadata']['buildkit_completeness']['request'] = False
        with self.assertRaisesRegex(d.Blocked, 'BuildProvenanceMismatch'): self.prove()

    def test_unknown_provenance_format_or_build_definition_blocks(self):
        for key, value in (('buildType', 'unknown'), ('externalParameters', {})):
            prior = self.proof['buildDefinition'][key]
            self.proof['buildDefinition'][key] = value
            with self.assertRaisesRegex(d.Blocked, 'BuildProvenanceMismatch'): self.prove()
            self.proof['buildDefinition'][key] = prior

    def mount_fixture(self):
        c = container('api')
        c['Mounts'] = [{'Type': 'bind', 'Source': '/run/systemd/timesync', 'Destination': '/host-timesync',
                        'RW': False, 'Propagation': 'rprivate'}]
        service = {'volumes': [{'type': 'bind', 'source': '/run/systemd/timesync', 'target': '/host-timesync', 'read_only': True}]}
        return c, service

    def layer(self, c, service, diff=b'A /host-timesync\n', identity=b'9:42:41ed', host_stats=None, role='api'):
        info = os.stat_result((stat.S_IFDIR | 0o755, 42, 9, 1, 0, 0, 0, 0, 0, 0))
        def docker(*args, **kw):
            if args[0] == 'diff': return diff
            self.assertEqual(args, ('exec', c['Id'], 'stat', '-c', '%d:%i:%f', '/host-timesync'))
            return identity
        with patch.object(d, 'docker', side_effect=docker), patch.object(d.backup, 'no_symlinks'), \
                patch.object(Path, 'stat', side_effect=host_stats or [info, info]):
            return d.writable_layer(role, c, service)

    def test_only_declared_readonly_timesync_mount_with_matching_identity_passes(self):
        c, service = self.mount_fixture()
        self.assertEqual(self.layer(c, service), (9, 42))

    def test_missing_writable_wrong_source_or_propagated_mount_blocks(self):
        for target, key, bad in (('live', 'RW', True), ('live', 'Source', '/elsewhere'),
                                 ('live', 'Type', 'volume'), ('live', 'Propagation', 'shared'),
                                 ('declared', 'read_only', False), ('declared', 'source', '/elsewhere')):
            c, service = self.mount_fixture()
            value = c['Mounts'][0] if target == 'live' else service['volumes'][0]
            value[key] = bad
            with self.assertRaisesRegex(d.Blocked, 'TimesyncMountMismatch'): self.layer(c, service)
        c, service = self.mount_fixture()
        service['volumes'] = []
        with self.assertRaisesRegex(d.Blocked, 'TimesyncMountMissing'): self.layer(c, service)

    def test_changed_inode_device_non_directory_or_missing_stat_blocks(self):
        c, service = self.mount_fixture()
        for value in (b'9:43:41ed', b'10:42:41ed', b'9:42:81a4', b'9:42:a1ff', b''):
            with self.assertRaises(d.Blocked): self.layer(c, service, identity=value)

    def test_timesync_host_replacement_during_verification_blocks(self):
        c, service = self.mount_fixture()
        before = os.stat_result((stat.S_IFDIR | 0o755, 42, 9, 1, 0, 0, 0, 0, 0, 0))
        after = os.stat_result((stat.S_IFDIR | 0o755, 43, 9, 1, 0, 0, 0, 0, 0, 0))
        with self.assertRaisesRegex(d.Blocked, 'TimesyncIdentityMismatch'):
            self.layer(c, service, host_stats=[before, after])

    def test_timesync_symlink_source_is_rejected(self):
        c, service = self.mount_fixture()
        with patch.object(d.backup, 'no_symlinks', side_effect=d.backup.Blocked('Symlink')):
            with self.assertRaises(d.backup.Blocked): d.timesync_identity(c, service)

    def test_all_other_nonvolatile_changes_still_block(self):
        c, service = self.mount_fixture()
        for diff in (b'C /host-timesync', b'D /host-timesync', b'A /host-timesync/extra',
                     b'A /host-timesync-other', b'C /app/appsettings.json', b'A /var/cache/nginx-other',
                     b'A /host-timesync\nC /etc/passwd'):
            with self.subTest(diff=diff):
                with self.assertRaisesRegex(d.Blocked, 'RollbackImageHasRuntimeRepairs'):
                    self.layer(c, service, diff=diff)
        with self.assertRaisesRegex(d.Blocked, 'RollbackImageHasRuntimeRepairs'):
            self.layer(c, service, role='web')


class BackupHelperTests(unittest.TestCase):
    def setUp(self):
        self.api = container('api')
        self.api['Config']['User'] = '1654'
        self.api['Mounts'] = [dict(Type='volume', Name='synthetic-' + str(i),
            Source='/var/lib/docker/volumes/synthetic-' + str(i) + '/_data', Destination=t, RW=True)
            for i, t in enumerate(d.backup.EXPECTED)]

    def volume(self, *args, **kwargs):
        self.assertEqual(args[:2], ('volume', 'inspect'))
        m = next(m for m in self.api['Mounts'] if m.get('Name') == args[-1])
        return json.dumps([dict(Name=m['Name'], Mountpoint=m['Source'], Driver='local', Options=None)]).encode()

    def test_volume_validation_never_traverses_production_paths(self):
        with patch.object(d, 'docker', side_effect=self.volume), \
             patch.object(Path, 'stat', side_effect=AssertionError('host database access')):
            result = d.volume_sources(self.api)
        self.assertEqual(len(result['Mounts']), 5)
        self.assertNotIn('/finance-data', [m['Destination'] for m in result['Mounts']])

    def test_wrong_volume_name_mountpoint_driver_and_options_rejected(self):
        for field, bad in [('Name', 'foreign'), ('Mountpoint', '/foreign'), ('Driver', 'nfs'), ('Options', {'device': '/finance'})]:
            def wrong(*args):
                value = json.loads(self.volume(*args)); value[0][field] = bad
                return json.dumps(value).encode()
            with self.subTest(field=field), patch.object(d, 'docker', side_effect=wrong):
                with self.assertRaisesRegex(d.Blocked, 'BackupVolumeIdentity'): d.volume_sources(self.api)

    def test_alias_finance_nested_missing_and_duplicate_mounts_rejected(self):
        original = copy.deepcopy(self.api)
        for fault in ('alias', 'nested', 'missing', 'duplicate', 'bind', 'root-user'):
            self.api = copy.deepcopy(original)
            if fault == 'alias': self.api['Mounts'].append(dict(self.api['Mounts'][0], Destination='/finance-data'))
            if fault == 'nested': self.api['Mounts'].append(dict(Type='bind', Source='/elsewhere', Destination='/data/nested'))
            if fault == 'missing': self.api['Mounts'].pop()
            if fault == 'duplicate': self.api['Mounts'].append(copy.deepcopy(self.api['Mounts'][0]))
            if fault == 'bind': self.api['Mounts'][0]['Type'] = 'bind'
            if fault == 'root-user': self.api['Config']['User'] = '0'
            with self.subTest(fault=fault), patch.object(d, 'docker', side_effect=self.volume):
                with self.assertRaises(d.Blocked): d.volume_sources(self.api)

    def test_same_data_allows_live_writes_but_rejects_identity_schema_or_directory_drift(self):
        before = dict(directories={'/data': [1, 2, 1654, 0, 493]}, schemas={'db': ['table']},
                      inventory={'db': dict(device=1, inode=2, uid=1654, gid=1654, mode=420, sourceBytes=100)})
        after = copy.deepcopy(before); after['inventory']['db']['sourceBytes'] = 200
        d.same_data(before, after)
        for field in d.IDENTITY_FIELDS:
            bad = copy.deepcopy(after); bad['inventory']['db'][field] += 1
            with self.assertRaisesRegex(d.Blocked, 'DatabaseIdentityChanged'): d.same_data(before, bad)
        for field in ('directories', 'schemas', 'inventory'):
            bad = copy.deepcopy(before); bad[field] = {}
            with self.assertRaises(d.Blocked): d.same_data(before, bad)

    def test_default_cli_reports_deferred_without_any_mutation_or_host_data_read(self):
        with patch.object(sys, 'argv', ['deploy-standard.py', '--revision', REV]), \
             patch.object(d.Deployment, 'preflight') as preflight, \
             patch.object(d.Deployment, 'execute', side_effect=AssertionError('execute')), \
             patch.object(Path, 'mkdir', side_effect=AssertionError('mkdir')), \
             patch.object(d, 'docker', side_effect=AssertionError('Docker mutation')), \
             patch.object(d.backup, 'sources', side_effect=AssertionError('host data')), \
             patch('sys.stdout', new_callable=io.StringIO) as output:
            with self.assertRaises(SystemExit) as error: d.main()
            self.assertEqual(error.exception.code, 2)
            self.assertIn('DEFERRED', output.getvalue())
            preflight.assert_called_once()

    def test_copy_validation_rejects_symlink_wide_permissions_and_wrong_owner(self):
        with tempfile.TemporaryDirectory() as name:
            folder = Path(name); file = folder / 'data'; file.write_text('synthetic'); file.chmod(0o600)
            d.verify_private_copy(folder)
            file.chmod(0o644)
            with self.assertRaisesRegex(d.Blocked, 'BackupCopyMode'): d.verify_private_copy(folder)
            file.unlink(); file.symlink_to('/does-not-exist')
            with self.assertRaisesRegex(d.Blocked, 'BackupCopyOwner'): d.verify_private_copy(folder)

    def test_helper_isolation_faults_block_before_start_and_preserve_intent(self):
        for fault in ('user', 'image', 'capability', 'privileged', 'network', 'extra-mount',
                      'writable-bind', 'groups', 'memory', 'autoremove', 'entrypoint'):
            with self.subTest(fault=fault), tempfile.TemporaryDirectory() as name:
                dep = d.Deployment(REV, {}, None, Path(name))
                dep.folder = Path(name) / 'run-test'; dep.folder.mkdir(mode=0o700)
                dep.originals = {'api': self.api}
                dep.sources = {'Mounts': [{k: m[k] for k in ('Type','Name','Source','Destination')} for m in self.api['Mounts']]}
                calls = []
                cid = 'f' * 64
                c = {'Image': OLD, 'Config': {'User': '1654:1654', 'Entrypoint': ['python3']},
                     'State': {'Running': False}, 'HostConfig': {
                     'NetworkMode': 'none', 'CapDrop': ['ALL'], 'SecurityOpt': ['no-new-privileges'],
                     'Memory': 512*1024*1024, 'MemorySwap': 512*1024*1024, 'NanoCpus': 1000000000,
                     'PidsLimit': 32, 'RestartPolicy': {'Name': 'no'}},
                     'Mounts': [dict(m, Destination=m['Source'], RW=True) for m in self.api['Mounts']]}
                def docker(*args, **kwargs):
                    calls.append(args)
                    if args[:2] == ('image', 'inspect'):
                        return json.dumps([{'Id': OLD, 'RepoDigests': [d.BACKUP_IMAGE], 'Config': {}}]).encode()
                    self.assertEqual(args[0], 'create')
                    self.assertEqual(args[args.index('--pull')+1], 'never')
                    self.assertEqual(args[args.index('--user')+1], '1654:1654')
                    self.assertNotIn('--rm', args)
                    mounts = [args[i+1] for i,v in enumerate(args) if v == '--mount']
                    self.assertEqual(len(mounts), 5)
                    self.assertTrue(all('volume-nocopy' in v for v in mounts))
                    c['Config']['Cmd'] = list(args[-4:])
                    if fault == 'user': c['Config']['User'] = '0'
                    if fault == 'image': c['Image'] = NEW
                    if fault == 'capability': c['HostConfig']['CapAdd'] = ['SYS_ADMIN']
                    if fault == 'privileged': c['HostConfig']['Privileged'] = True
                    if fault == 'network': c['HostConfig']['NetworkMode'] = 'host'
                    if fault == 'extra-mount': c['Mounts'].append(dict(Type='bind', Name='', Source='/finance', Destination='/finance', RW=True))
                    if fault == 'writable-bind': c['HostConfig']['Binds'] = ['/var/run/docker.sock:/var/run/docker.sock']
                    if fault == 'groups': c['HostConfig']['GroupAdd'] = ['0']
                    if fault == 'memory': c['HostConfig']['Memory'] = 0
                    if fault == 'autoremove': c['HostConfig']['AutoRemove'] = True
                    if fault == 'entrypoint': c['Config']['Entrypoint'] = ['sh']
                    return cid.encode()
                with patch.object(d, 'docker', side_effect=docker), patch.object(d, 'inspect', return_value=c), \
                     patch.object(d, 'volume_sources', return_value=dep.sources), \
                     patch.object(dep, 'current', return_value=self.api), patch.object(os, 'geteuid', return_value=1000):
                    with self.assertRaisesRegex(d.Blocked, 'BackupHelper'): dep.helper_data(True)
                self.assertTrue(dep.helper_incomplete)
                self.assertTrue(list(dep.folder.glob('*/intent.json')))
                self.assertFalse(any(v[0] in ('start','cp') for v in calls))

    def test_helper_failure_requires_manual_review_and_blocks_retry(self):
        with tempfile.TemporaryDirectory() as name:
            state = Path(name)
            dep = d.Deployment(REV, {}, None, state)
            dep.config = dep.provenance = dep.release = dep.rollback = {}
            def fail():
                dep.helper_incomplete = True
                raise d.Blocked('BackupHelperFailed')
            with patch.object(dep, 'build_images'), patch.object(dep, 'compose', return_value=b'{}'), \
                 patch.object(dep, 'backup_data', side_effect=fail), patch.object(dep, 'switch') as switch:
                with self.assertRaisesRegex(d.Blocked, 'BackupHelperFailed'): dep.execute()
                switch.assert_not_called()
            with self.assertRaisesRegex(d.Blocked, 'PreviousRunNeedsReview'): d.no_pending_run(state)


def synthetic_in_container(request):
    """Explicit opt-in Docker test, exclusively newly created synthetic volumes."""
    import multiprocessing
    import signal
    assert os.getuid() == os.getgid() == 1654
    assert sqlite3.sqlite_version == '3.53.4'
    os.umask(0o077)
    roots = d.backup.sources(request['sources'])
    for target, (root, _) in roots.items():
        for name in d.backup.EXPECTED[target]:
            with closing(sqlite3.connect(root / name)) as db:
                db.execute('CREATE TABLE household(id INTEGER PRIMARY KEY, value BLOB)')
                db.execute("INSERT INTO household VALUES(1, 'initial')")
                db.commit()
                db.execute('PRAGMA journal_mode=WAL')  # no data writes after this switch
            # Do not assume that close must remove WAL/SHM. Prove the requested
            # fixture using closed synthetic DBs with no committed WAL frames; only
            # their optional empty sidecars are removed.
            for suffix in ('-wal', '-shm'):
                (root / (name + suffix)).unlink(missing_ok=True)
            assert not (root / (name + '-wal')).exists()
            assert not (root / (name + '-shm')).exists()
    for root, _ in roots.values():
        s = root.stat()
        assert (s.st_uid, s.st_gid, stat.S_IMODE(s.st_mode)) == (1654, 0, 0o755), (s.st_uid, s.st_gid, oct(stat.S_IMODE(s.st_mode)))
    baseline = d.data_snapshot(roots)
    live = roots['/data'][0] / 'meal-planner.db'
    # Enough pages to force incremental backup callbacks and deterministic overlap.
    with closing(sqlite3.connect(live)) as db:
        db.executemany('INSERT INTO household(value) VALUES(?)', [(b'x' * 4096,)] * 512)
        db.commit()
    before = d.data_snapshot(roots)
    ctx = multiprocessing.get_context('fork')
    ready, finish = ctx.Event(), ctx.Event()
    def writer():
        with closing(sqlite3.connect(live, timeout=3)) as db:
            db.execute("UPDATE household SET value='concurrent' WHERE id=1"); db.commit()
            ready.set()
            assert finish.wait(20)
    child = ctx.Process(target=writer)
    original = d.backup.copy_database
    concurrent = []
    def copy_with_writer(source, destination, deadline):
        if source != live:
            return original(source, destination, deadline)
        # Exercise the unchanged copy_database and SQLite backup API: its normal
        # monotonic deadline callback synchronizes one real concurrent writer.
        real_time = time.monotonic
        def progress_time():
            if not concurrent:
                concurrent.append(True); child.start(); assert ready.wait(10)
            return real_time()
        with patch.object(d.backup.time, 'monotonic', side_effect=progress_time):
            original(source, destination, deadline)
    home = Path('/tmp/synthetic-results'); home.mkdir(mode=0o700)
    try:
        with patch.object(d.backup, 'copy_database', side_effect=copy_with_writer):
            folder = d.backup.run_backup(roots, home / 'concurrent', {}, lambda: d.same_data(before, d.data_snapshot(roots)))
    finally:
        finish.set()
        if child.pid is not None: child.join(10)
    assert concurrent and child.exitcode == 0
    digest = d.backup.verify_set(folder)
    with closing(sqlite3.connect(folder / 'data/meal-planner.db')) as db:
        assert db.execute('SELECT value FROM household WHERE id=1').fetchone()[0] == 'concurrent'
    # Kill a process during an actual incremental backup, with live WAL and an
    # INCOMPLETE manifest already durable. No restore, journal-mode or checkpoint.
    entered = ctx.Event()
    def crashing_backup():
        os.umask(0o077)
        crash = home / 'crash'; crash.mkdir(mode=0o700)
        d.record(crash, 'STARTED')
        real_copy = d.backup.copy_database
        def slow(source, destination, deadline):
            if source != live: return real_copy(source, destination, deadline)
            def pause():
                entered.set(); signal.pause()
            with patch.object(d.backup.time, 'monotonic', side_effect=pause):
                real_copy(source, destination, deadline)
        with patch.object(d.backup, 'copy_database', side_effect=slow):
            d.backup.run_backup(roots, crash / 'backups', {}, lambda: None)
    crash = ctx.Process(target=crashing_backup)
    crash.start(); assert entered.wait(10)
    os.kill(crash.pid, signal.SIGKILL); crash.join(10)
    assert crash.exitcode == -signal.SIGKILL
    manifest = next((home / 'crash/backups').glob('*/manifest.json'))
    assert json.loads(manifest.read_text())['status'] == 'INCOMPLETE'
    assert (home / 'crash/journal.jsonl').is_file()
    try: d.backup.verify_set(manifest.parent)
    except d.backup.Blocked: pass
    else: raise AssertionError('Incomplete backup accepted')
    d.same_data(baseline, d.data_snapshot(roots))
    # API-equivalent UID/GID can still read and write every database. Remaining
    # sidecars may exist or disappear; permissions/ownership are what matters.
    for target, (root, _) in roots.items():
        for name in d.backup.EXPECTED[target]:
            with closing(sqlite3.connect(root / name)) as db:
                assert db.execute('PRAGMA integrity_check').fetchone() == ('ok',)
                db.execute("UPDATE household SET value='api-after-crash' WHERE id=1"); db.commit()
                for suffix in ('-wal', '-shm'):
                    p = root / (name + suffix)
                    if p.exists():
                        s = p.stat()
                        assert s.st_uid == s.st_gid == 1654 and stat.S_IMODE(s.st_mode) == 0o600
    assert d.backup.verify_set(folder) == digest
    d.backup_helper(request)  # real unchanged run_backup/verify_set + durable result
    print('SYNTHETIC WAL/concurrency/SIGKILL/permissions/backup PASS', flush=True)


@unittest.skipUnless(os.environ.get('BIGBRAIN_STANDARD_SYNTHETIC_DOCKER') == '1', 'explicit synthetic Docker opt-in required')
class SyntheticDockerTests(unittest.TestCase):
    def test_real_image_wal_crash_durability_and_host_copy(self):
        token = uuid.uuid4().hex
        names, containers = [], []
        api = container('api'); api['Config']['User'] = '1654'; api['Mounts'] = []
        # Only objects created by this test are eligible for cleanup.
        def docker(*args, **kw): return d.docker(*args, **kw)
        with tempfile.TemporaryDirectory() as temporary:
            state = Path(temporary)
            try:
                for i, target in enumerate(d.backup.EXPECTED):
                    name = 'bigbrain-standard-test-' + token + '-' + str(i)
                    docker('volume', 'create', '--label', 'bigbrain.standard.test=' + token, name, mutation=True)
                    names.append(name)
                    v = json.loads(docker('volume', 'inspect', name))[0]
                    api['Mounts'].append(dict(Type='volume', Name=name, Source=v['Mountpoint'], Destination=target, RW=True))
                sources = d.volume_sources(api)
                request = dict(sources=sources, baseline=None, backup=True, identity={})
                code = ("import importlib.util,json; from pathlib import Path; "
                        "s=importlib.util.spec_from_file_location('suite','/standard-code/tests/deployment/test_standard_deployment.py'); "
                        "m=importlib.util.module_from_spec(s); s.loader.exec_module(m); "
                        "m.synthetic_in_container(json.loads(Path('/standard-code/request.json').read_text()))")
                args = ['create', '--name', 'bigbrain-standard-test-' + token, '--pull', 'never',
                        '--user', '1654:1654', '--network', 'none', '--cap-drop', 'ALL',
                        '--security-opt', 'no-new-privileges', '--restart', 'no', '--memory', '512m',
                        '--memory-swap', '512m', '--cpus', '1', '--pids-limit', '32', '--entrypoint', 'python3']
                for m in sources['Mounts']:
                    args += ['--mount', 'type=volume,source=' + m['Name'] + ',target=' + m['Source'] + ',volume-nocopy']
                cid = docker(*args, d.BACKUP_IMAGE, '-I', '-B', '-c', code, mutation=True).decode().strip()
                containers.append(cid)
                stream = io.BytesIO(d.helper_archive(request))
                with tarfile.open(fileobj=stream, mode='a') as archive:
                    body = Path(__file__).read_bytes()
                    info = tarfile.TarInfo('standard-code/tests/deployment/test_standard_deployment.py')
                    info.size, info.mode = len(body), 0o444
                    archive.addfile(info, io.BytesIO(body))
                    # Ownership only of newly created, empty synthetic volume roots;
                    # stdin tar ownership, no root process, no production paths/volumes.
                    for m in sources['Mounts']:
                        info = tarfile.TarInfo(m['Source'].lstrip('/'))
                        info.type, info.uid, info.gid, info.mode = tarfile.DIRTYPE, 1654, 0, 0o755
                        archive.addfile(info)
                docker('cp', '-', cid + ':/', input_data=stream.getvalue(), mutation=True)
                try:
                    output = docker('start', '--attach', cid, timeout=90, mutation=True)
                except d.Blocked:
                    logs = subprocess.run(['docker', '--host', 'unix:///var/run/docker.sock', 'logs', cid], capture_output=True)
                    raise AssertionError((logs.stdout + logs.stderr).decode()) from None
                self.assertIn(b'SYNTHETIC WAL/concurrency/SIGKILL/permissions/backup PASS', output)
                self.assertEqual(d.inspect(cid)['State']['ExitCode'], 0)
                copied = state / 'copied'; copied.mkdir(mode=0o700)
                docker('cp', cid + ':' + str(d.HELPER_HOME) + '/.', str(copied), mutation=True)
                d.verify_private_copy(copied)
                interrupted = state / 'interrupted'; interrupted.mkdir(mode=0o700)
                docker('cp', cid + ':/tmp/synthetic-results/crash/.', str(interrupted), mutation=True)
                d.verify_private_copy(interrupted)
                incomplete = next((interrupted / 'backups').iterdir())
                with self.assertRaises(d.backup.Blocked): d.backup.verify_set(incomplete)
                self.assertTrue((interrupted / 'journal.jsonl').exists())
                result = json.loads((copied / 'result.json').read_text())
                self.assertEqual(d.backup.verify_set(copied / 'backups' / result['backupId']), result['manifestSha256'])
                # Use the real orchestration path with ONLY the synthetic API mounts.
                dep = d.Deployment(REV, {}, None, state)
                dep.folder = state / 'run-synthetic'; dep.folder.mkdir(mode=0o700)
                dep.sources, dep.originals = sources, {'api': api}
                with patch.object(dep, 'current', return_value=api):
                    try:
                        folder = dep.helper_data(make_backup=True)
                        self.assertTrue(folder.is_relative_to(dep.folder))
                        d.backup.verify_set(folder)
                        dep.data_identity()
                    finally:
                        containers.extend(dep.probes)
                # Corrupt only a separate host copy: hash/integrity gate must reject it.
                damaged = state / 'damaged'; shutil.copytree(folder, damaged)
                with (damaged / 'data/meal-planner.db').open('r+b') as stream:
                    stream.write(b'corrupt')
                with self.assertRaises(d.backup.Blocked): d.backup.verify_set(damaged)
                for item in containers:
                    self.assertFalse(d.inspect(item)['State']['Running'])
                # Separate copied data persists and verifies after helper deletion.
                for item in list(containers):
                    docker('rm', item, mutation=True); containers.remove(item)
                self.assertEqual(d.backup.verify_set(copied / 'backups' / result['backupId']), result['manifestSha256'])
                self.assertTrue((interrupted / 'journal.jsonl').exists())
                with self.assertRaises(d.backup.Blocked): d.backup.verify_set(incomplete)
            finally:
                for item in containers:
                    # Only test-owned objects; never global prune or other services.
                    if d.inspect(item)['State']['Running']: docker('stop', '--time', '5', item, mutation=True)
                    docker('rm', item, mutation=True)
                for name in names: docker('volume', 'rm', name, mutation=True)


if __name__ == '__main__':
    unittest.main()
