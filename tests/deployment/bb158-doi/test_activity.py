"""Synthetic DTOs only; no provider, container or production data mutations."""
import copy
import unittest
import tempfile
from pathlib import Path
from unittest.mock import patch
import execute as e
import deploy as d


def job(provider='qBittorrent', status='downloading', progress=0):
    return dict(provider=provider, status=status, progressPercent=progress,
                downloadSpeedBytesPerSecond=None, uploadSpeedBytesPerSecond=None,
                canPlay=status == 'available', playItemId='opaque-fixture' if status == 'available' else None,
                details=[dict(provider=provider, status=status, progressPercent=progress)])


def download(status='active'):
    return dict(status=status, diagnosis={'code': 'waitingForMetadata' if status == 'active' else 'insufficientData'},
                downloadedBytes=0 if status == 'active' else 1000,
                progressPercent=0 if status == 'active' else 100,
                downloadSpeedBytesPerSecond=0, uploadSpeedBytesPerSecond=0)


class ActivityTests(unittest.TestCase):
    def setUp(self):
        # Observed shape: three metadata jobs, one recent library item, completed queue rows.
        self.jobs = dict(status='complete', providers=[dict(provider=p, status='online')
                         for p in ('Sonarr', 'Radarr', 'qBittorrent', 'Jellyfin')],
                         jobs=[job(), job(), job(), job('Sonarr', 'available', 100)])
        self.queue = dict(downloads=[download(), download(), download(), download('completed')])

    def validate(self):
        e.validate_activity(self.jobs, self.queue)

    def test_recent_library_availability_is_not_a_torrent_transfer(self):
        self.validate()
        self.jobs['jobs'][-1] = job('Radarr', 'completed', 100)
        self.validate()

    def test_observed_degraded_snapshot_stays_blocked(self):
        self.jobs['status'] = 'degraded'
        self.jobs['providers'][0]['status'] = 'unavailable'
        with self.assertRaisesRegex(AssertionError, 'ProviderActivityUnknown'): self.validate()

    def test_missing_unknown_duplicate_or_unavailable_provider_stays_blocked(self):
        original = copy.deepcopy(self.jobs['providers'])
        variants = [[], original[:-1], original + [original[0]],
                    [dict(x, status='unavailable') for x in original],
                    [dict(x, provider='unexpected') for x in original]]
        for providers in variants:
            with self.subTest(providers=providers):
                self.jobs['providers'] = providers
                with self.assertRaisesRegex(AssertionError, 'ProviderActivityUnknown'): self.validate()

    def test_completed_seeding_is_not_idle(self):
        for field in ('downloadSpeedBytesPerSecond', 'uploadSpeedBytesPerSecond'):
            with self.subTest(field=field):
                self.queue['downloads'][-1][field] = 1
                with self.assertRaisesRegex(AssertionError, 'QueueTransferActiveOrUnknown'): self.validate()
                self.queue['downloads'][-1][field] = 0

    def test_unknown_or_invalid_numeric_transfer_never_means_zero(self):
        for row in (self.queue['downloads'][0], self.queue['downloads'][-1]):
            for field in ('downloadSpeedBytesPerSecond', 'uploadSpeedBytesPerSecond'):
                for value in (None, -1, False, '0', 0.0):
                    with self.subTest(field=field, value=value):
                        row[field] = value
                        with self.assertRaises(AssertionError): self.validate()
                row[field] = 0

    def test_display_null_does_not_replace_queue_evidence(self):
        self.queue['downloads'] = []
        with self.assertRaisesRegex(AssertionError, 'MissingQueueActivityEvidence'): self.validate()

    def test_unknown_nonterminal_and_other_provider_jobs_block(self):
        for status in ('unknown', 'failed', 'stalled', 'queued', 'searching', 'importing', 'requested'):
            with self.subTest(status=status):
                self.jobs['jobs'][-1] = job('Sonarr', status, 0)
                with self.assertRaisesRegex(AssertionError, 'JobActivityUnsafe'): self.validate()
        self.jobs['jobs'][-1] = job('unexpected', 'available', 100)
        with self.assertRaisesRegex(AssertionError, 'JobProviderUnknown'): self.validate()

    def test_terminal_display_cannot_hide_unsafe_details(self):
        row = self.jobs['jobs'][-1]
        for status in ('downloading', 'importing', 'unknown', 'failed'):
            with self.subTest(status=status):
                row['details'][0]['status'] = status
                with self.assertRaisesRegex(AssertionError, 'JobDetailsUnsafe'): self.validate()

    def test_availability_requires_playback_evidence_and_details(self):
        original = copy.deepcopy(self.jobs['jobs'][-1])
        for key, value in (('canPlay', False), ('playItemId', None), ('progressPercent', 0), ('details', [])):
            with self.subTest(key=key):
                self.jobs['jobs'][-1] = dict(original, **{key: value})
                with self.assertRaises(AssertionError): self.validate()

    def test_active_queue_requires_metadata_zero_payload_and_progress(self):
        original = copy.deepcopy(self.queue['downloads'][0])
        for key, value in (('status', 'unknown'), ('status', 'paused'), ('status', 'queued'),
                           ('status', 'error'), ('diagnosis', {'code': 'insufficientData'}),
                           ('downloadedBytes', 1), ('downloadedBytes', None), ('progressPercent', 1)):
            with self.subTest(key=key, value=value):
                self.queue['downloads'][0] = dict(original, **{key: value})
                with self.assertRaises(AssertionError): self.validate()

    def test_completed_queue_requires_consistent_progress_and_known_payload(self):
        original = copy.deepcopy(self.queue['downloads'][-1])
        for key, value in (('progressPercent', 99), ('downloadedBytes', None), ('downloadedBytes', -1)):
            with self.subTest(key=key):
                self.queue['downloads'][-1] = dict(original, **{key: value})
                with self.assertRaises(AssertionError): self.validate()

    def test_job_transfer_signals_block(self):
        for field in ('downloadSpeedBytesPerSecond', 'uploadSpeedBytesPerSecond'):
            self.jobs['jobs'][-1][field] = 1
            with self.assertRaisesRegex(AssertionError, 'JobTransferActiveOrUnknown'): self.validate()
            self.jobs['jobs'][-1][field] = None

    def test_jobs_limit_is_not_complete_activity_evidence(self):
        self.jobs['jobs'] = [job()] * 50
        with self.assertRaisesRegex(AssertionError, 'JobsTruncatedOrInvalid'): self.validate()

    def test_empty_online_snapshot_is_safe(self):
        self.jobs['jobs'] = []; self.queue['downloads'] = []
        self.validate()

    def test_live_gate_failure_prevents_any_stop_or_state_marker(self):
        self.jobs['status'] = 'degraded'
        with tempfile.TemporaryDirectory() as root, patch.object(d, 'ROOT', Path(root)), \
                patch.object(d, 'unchanged'), patch.object(d, 'imports'), \
                patch.object(e, 'read_api', side_effect=[self.jobs, self.queue]), \
                patch.object(d, 'docker', side_effect=AssertionError('No Docker allowed')) as docker:
            with self.assertRaisesRegex(AssertionError, 'ProviderActivityUnknown'):
                d.work({}, 'hash', 'invocation')
            docker.assert_not_called()
            self.assertEqual(list(Path(root).iterdir()), [])

    def test_activity_uses_only_existing_get_contracts_and_is_deployment_gate(self):
        self.assertIs(d.activity, e.activity)
        with patch.object(e, 'read_api', side_effect=[self.jobs, self.queue]) as read:
            e.activity()
        self.assertEqual([x.args[0] for x in read.call_args_list],
                         ['/api/v1/modules/media/jobs?limit=50', '/api/v1/modules/media/downloads'])
        with patch.object(e, 'read_api', side_effect=TimeoutError):
            with self.assertRaises(TimeoutError): e.activity()


if __name__ == '__main__': unittest.main()
