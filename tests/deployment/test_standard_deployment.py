"""Offline T2 fault tests: synthetic SQLite/WAL and simulated Docker only."""
import copy
import importlib.util
import json
import os
import runpy
from pathlib import Path
import sqlite3
import subprocess
import sys
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
            patch.object(d, 'inspect', side_effect=lambda cid: copy.deepcopy(self.runtime[cid])),
            patch.object(d, 'git_guard'), patch.object(d, 'http_health'),
            patch.object(self.dep, 'current', side_effect=lambda role: copy.deepcopy(self.runtime[role])),
            patch.object(self.dep, 'others', return_value=self.dep.other_snapshot),
            patch.object(self.dep, 'compose', side_effect=self.compose),
            patch.object(self.dep, 'wait_healthy', side_effect=self.wait_healthy),
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
            self.runtime[args[-1]]['State']['Running'] = False
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
        self.runtime[role]['State']['Running'] = True
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
        self.assertEqual(self.phases(), ['PREPARING', 'SWITCHING_API', 'SWITCHING_WEB', 'PASS'])
        receipt = json.loads((self.dep.folder / 'receipt.json').read_text())
        folder = self.state / 'backups' / receipt['backupId']
        self.assertEqual(d.backup.verify_set(folder), receipt['backupManifestSha256'])
        ups = [args[-1] for cmd, args in self.calls if cmd == 'compose' and args[0] == 'up']
        self.assertEqual(ups, ['api', 'web'])
        self.assertEqual((self.dep.folder / 'receipt.json').stat().st_mode & 0o777, 0o600)

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
        with self.assertRaises(d.backup.Blocked): self.dep.preflight()

    def test_candidate_revision_is_checked(self):
        with patch.object(d, 'image_info', return_value={'Config': {'Labels': {}}}):
            with self.assertRaisesRegex(d.Blocked, 'ImageRevisionMismatch'): self.dep.preflight()

    def test_image_entrypoint_change_rejected(self):
        with patch.object(d, 'image_info', side_effect=[{'Config': {}}, {'Config': {
                'Entrypoint': ['different'], 'Labels': {'org.opencontainers.image.revision': REV}}}]):
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


if __name__ == '__main__':
    unittest.main()
