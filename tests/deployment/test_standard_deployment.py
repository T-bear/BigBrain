"""Offline T2 fault tests: synthetic SQLite/WAL and simulated Docker only."""
import copy
import io
import importlib.util
import json
import os
import runpy
from pathlib import Path
import sqlite3
import subprocess
import sys
import tarfile
import tempfile
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
        self.patches = [
            patch.object(d, 'command', side_effect=AssertionError('No real command')),
            patch.object(d, 'docker', side_effect=self.docker),
            patch.object(d, 'inspect', side_effect=lambda cid: copy.deepcopy(next(c for c in self.runtime.values() if c['Id'] == cid))),
            patch.object(d, 'git_guard'), patch.object(d, 'http_health'),
            patch.object(self.dep, 'current', side_effect=lambda role: copy.deepcopy(self.runtime[role])),
            patch.object(self.dep, 'others', return_value=self.dep.other_snapshot),
            patch.object(self.dep, 'compose', side_effect=self.compose),
            patch.object(self.dep, 'wait_healthy', side_effect=self.wait_healthy),
            patch.object(self.dep, 'build_images'),
            patch.object(self.dep, 'prove_restartability'),
        ]
        for p in self.patches:
            p.start()
        self.fail = None

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
        folder = self.state / 'backups' / receipt['backupId']
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
        self.assertTrue(list((self.state / 'backups').glob('*/manifest.json')))

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


if __name__ == '__main__':
    unittest.main()
