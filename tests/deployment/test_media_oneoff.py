"""Offline fail-closed tests for the one-off Media-only STANDARD release."""
import copy
import hashlib
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location('media_standard', ROOT / 'scripts/deploy-standard.py')
d = importlib.util.module_from_spec(spec)
spec.loader.exec_module(d)
REV = 'a' * 40
OLD = 'sha256:' + 'b' * 64
NEW = 'sha256:' + 'c' * 64


def blob(body):
    return hashlib.sha1(b'blob ' + str(len(body)).encode() + b'\0' + body).hexdigest()


def original(base):
    return {'Image': OLD, 'Config': {'Labels': {'org.opencontainers.image.revision': base},
                                    'Env': ['DOTNET_VERSION=10.0.10', 'ASPNET_VERSION=10.0.10']}}


class MediaOneOffTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self.tmp.cleanup)
        self.root = Path(self.tmp.name)
        self.originals = {'api': original(d.MEDIA_RUNNING_API),
                          'web': original(d.MEDIA_RUNNING_WEB)}
        self.dep = d.Deployment(REV, {}, None, self.root, media_release=True)

    def fake_git(self, args, *a, **kw):
        if args[:2] == ['git', 'ls-remote']:
            return (REV + '\trefs/heads/main').encode()
        if args[:2] == ['git', 'rev-parse']:
            return REV.encode()
        if args[:2] == ['git', 'status']:
            return b''
        if args[:2] == ['git', 'merge-base']:
            return b''
        if args[:2] == ['git', 'cat-file']:
            return b'commit'
        if args[:2] == ['git', 'diff']:
            if d.MEDIA_ACCEPTED_SOURCE in args:
                return getattr(self, 'new_source_delta', b'')
            if d.MEDIA_RUNNING_API in args:
                paths = sorted(d.MEDIA_API | d.MEDIA_KEEP_FINANCE)
                if getattr(self, 'extra_root_path', None):
                    paths.append(self.extra_root_path)
                return b'\0'.join(x.encode() for x in paths) + b'\0'
            if d.MEDIA_RUNNING_WEB in args:
                paths = sorted(d.MEDIA_WEB)
                if getattr(self, 'extra_root_path', None):
                    paths.append(self.extra_root_path)
                return b'\0'.join(x.encode() for x in paths) + b'\0'
        raise AssertionError(args)

    def guard(self, media=True):
        with patch.object(d, 'command', side_effect=self.fake_git), \
             patch.object(d, 'ci_green') as checks, \
             patch.object(d, 'MEMORY_FETCH', d.MEDIA_ACCEPTED_SOURCE):
            result = d.git_guard(REV, self.originals, media_release=media)
            checks.assert_called_once_with(REV)
            return result

    def test_exact_old_revisions_and_paths_pass_only_in_media_mode(self):
        self.assertEqual(self.guard(), {})
        with self.assertRaisesRegex(d.Blocked, 'OutsideStandardScope'):
            self.guard(media=False)

    def test_gitleaks_review_metadata_does_not_block_media_or_expand_scope(self):
        self.extra_root_path = '.gitleaksignore'
        self.guard()
        self.extra_root_path = '.env'
        with self.assertRaisesRegex(d.Blocked, 'OutsideMediaScope'):
            self.guard()

    def test_other_source_changes_block_before_ci(self):
        self.new_source_delta = b'src/BigBrain.Api/Finance/Unrelated.cs\0'
        with self.assertRaisesRegex(d.Blocked, 'MediaAcceptedSourceChanged'):
            self.guard()

    def test_wrong_api_or_web_runtime_revision_blocks(self):
        for role in d.ROLES:
            with self.subTest(role=role):
                self.originals[role]['Config']['Labels']['org.opencontainers.image.revision'] = 'f' * 40
                with self.assertRaisesRegex(d.Blocked, 'MediaRuntimeBaselineChanged'):
                    self.guard()
                self.originals[role]['Config']['Labels']['org.opencontainers.image.revision'] = (
                    d.MEDIA_RUNNING_API if role == 'api' else d.MEDIA_RUNNING_WEB)

    def test_previous_composite_runtime_never_acts_as_normal_git_baseline(self):
        self.originals['api']['Config']['Labels'][d.MEDIA_COMPOSITE_LABEL] = 'sha256:' + 'f' * 64
        with self.assertRaisesRegex(d.Blocked, 'CompositeRuntimeRequiresReview'):
            self.guard(media=True)
        with self.assertRaisesRegex(d.Blocked, 'CompositeRuntimeRequiresReview'):
            self.guard(media=False)

    def prepare(self, initial=b'new-finance', prior=b'old-finance'):
        source = self.root / 'source'
        for path in d.MEDIA_KEEP_FINANCE:
            file = source / path
            file.parent.mkdir(parents=True, exist_ok=True)
            file.write_bytes(initial)
        self.dep.folder = self.root / 'run'
        self.dep.folder.mkdir()
        self.dep.originals = copy.deepcopy(self.originals)
        def git(args, *a, **kw):
            if args[:2] == ['git', 'rev-parse']:
                return (blob(prior) if args[2].startswith(d.MEDIA_RUNNING_API + ':')
                        else blob(initial)).encode()
            if args[:2] == ['git', 'show']:
                return prior
            raise AssertionError(args)
        return source, git

    def test_overlay_preserves_only_two_exact_finance_blobs_and_records_manifest(self):
        source, git = self.prepare()
        with patch.object(d, 'command', side_effect=git):
            manifest = self.dep.media_source_overlay(source)
        self.assertEqual(set(manifest['overlaidFinanceBlobs']), d.MEDIA_KEEP_FINANCE)
        for path in d.MEDIA_KEEP_FINANCE:
            self.assertEqual((source / path).read_bytes(), b'old-finance')
        receipt = self.dep.folder / 'media-composition.json'
        self.assertEqual(self.dep.media_digest, 'sha256:' + d.backup.digest(receipt))
        self.assertEqual(json.loads(receipt.read_text()), manifest)
        self.assertEqual(receipt.stat().st_mode & 0o777, 0o600)

    def test_overlay_rejects_wrong_archive_blob_and_wrong_prior_blob(self):
        source, git = self.prepare()
        file = source / sorted(d.MEDIA_KEEP_FINANCE)[0]
        file.write_bytes(b'tampered')
        with patch.object(d, 'command', side_effect=git):
            with self.assertRaisesRegex(d.Blocked, 'MediaArchiveMismatch'):
                self.dep.media_source_overlay(source)
        file.write_bytes(b'new-finance')
        def forged(args, *a, **kw):
            if args[:2] == ['git', 'rev-parse'] and args[2].startswith(d.MEDIA_RUNNING_API):
                return ('0' * 40).encode()
            return git(args, *a, **kw)
        with patch.object(d, 'command', side_effect=forged):
            with self.assertRaisesRegex(d.Blocked, 'MediaFinanceBlobMismatch'):
                self.dep.media_source_overlay(source)

    def test_overlay_rejects_wrong_runtime_and_symlink(self):
        source, git = self.prepare()
        self.dep.originals['api']['Config']['Labels']['org.opencontainers.image.revision'] = 'f' * 40
        with patch.object(d, 'command', side_effect=git):
            with self.assertRaisesRegex(d.Blocked, 'MediaRuntimeBaselineChanged'):
                self.dep.media_source_overlay(source)
        self.dep.originals['api']['Config']['Labels']['org.opencontainers.image.revision'] = d.MEDIA_RUNNING_API
        file = source / sorted(d.MEDIA_KEEP_FINANCE)[0]
        file.unlink()
        file.symlink_to(self.root / 'not-source')
        with patch.object(d, 'command', side_effect=git):
            with self.assertRaisesRegex(d.Blocked, 'MediaSourceMissing'):
                self.dep.media_source_overlay(source)

    def test_replacement_api_container_must_report_composite_label(self):
        self.dep.originals = copy.deepcopy(self.originals)
        self.dep.images = {'api': NEW}
        self.dep.media_digest = 'sha256:' + 'e' * 64
        replacement = copy.deepcopy(self.originals['api'])
        replacement['Image'] = NEW
        with patch.object(d, 'fingerprint', return_value='same'):
            self.assertFalse(self.dep.runtime_matches('api', replacement, NEW))
            replacement['Config']['Labels'][d.MEDIA_COMPOSITE_LABEL] = self.dep.media_digest
            self.assertTrue(self.dep.runtime_matches('api', replacement, NEW))

    def test_built_api_image_must_bind_the_exact_composition_digest(self):
        self.dep.originals = copy.deepcopy(self.originals)
        self.dep.images = dict.fromkeys(d.ROLES, NEW)
        self.dep.config = {'name': d.PROJECT, 'services': {
            role: {'image': OLD, 'environment': {}, 'networks': {}} for role in d.ROLES},
            'networks': {}, 'volumes': {}}
        self.dep.media_digest = 'sha256:' + 'e' * 64
        config = {'Env': self.originals['api']['Config']['Env']}
        old_image = {'Config': {**config, 'Labels': {'org.opencontainers.image.revision': d.MEDIA_RUNNING_API}}}
        new_image = {'Config': {**config, 'Labels': {'org.opencontainers.image.revision': REV,
                                                   d.MEDIA_COMPOSITE_LABEL: self.dep.media_digest}}}
        def info(image):
            return copy.deepcopy(old_image if image == OLD else new_image)
        with patch.object(d, 'image_info', side_effect=info):
            self.dep.bind_images()
        self.assertEqual(self.dep.release['services']['api']['labels'][d.MEDIA_COMPOSITE_LABEL],
                         self.dep.media_digest)
        new_image['Config']['Labels'][d.MEDIA_COMPOSITE_LABEL] = 'sha256:' + 'f' * 64
        with patch.object(d, 'image_info', side_effect=info):
            with self.assertRaisesRegex(d.Blocked, 'MediaImageCompositionMismatch'):
                self.dep.bind_images()


if __name__ == '__main__':
    unittest.main()
