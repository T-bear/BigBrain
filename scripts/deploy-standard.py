#!/usr/bin/env python3
"""T2: bounded API+Web image replacement. Independent review required before use.

Build from accepted Git, prove isolated restartability, then replace API+Web.
No merge, schema operation, privilege grant or database restore.
Default is read-only preflight. See docs/policy/lanes.md for authority and limits.
"""
import argparse
import copy
from contextlib import closing
import fcntl
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import re
import signal
import stat
import shutil
import sqlite3
import subprocess
import sys
import tarfile
import time
import urllib.request
import uuid

ROOT = Path(__file__).resolve().parents[1]
ROLES = ('api', 'web')
PROJECT = 'bigbrain-sprint1'
MEMORY_FETCH = '283060d41f4494d2e5606f7c857fb82972e86094'
CHECKS = {'backend', 'frontend', 'documentation', 'secrets'}
SHA = r'[0-9a-f]{40}'
IMAGE = r'sha256:[0-9a-f]{64}'
NGINX_SOURCE = 'https://github.com/nginx/docker-nginx-unprivileged'
REPOSITORIES = {'git@github.com:T-bear/BigBrain.git', 'https://github.com/T-bear/BigBrain.git',
                'https://github.com/T-bear/BigBrain'}
# Intentionally narrow initial lane. Expansion requires review, never a path-only safety claim.
APP_FILES = {
    'src/BigBrain.Api/MealPlanner/SchoolMealService.cs',
    'src/BigBrain.Api/MealPlanner/MealPlannerStore.SchoolMeals.cs',
}
# These disjoint source trees are copied only by their respective Dockerfiles.
# Other inputs (including root build/config files) remain subject to the shared gate.
# Dockerfile changes themselves are NOT allowed by APP_FILES.
SOURCE_TREES = {
    'api': ('src/BigBrain.Api/', 'src/BigBrain.Modules/', 'src/BigBrain.Sentinel.Contracts/',
            'src/BigBrain.Brain/', 'src/BigBrain.Finance.Hosting/'),
    'web': ('src/BigBrain.Web/',),
}
CONFIG_FIELDS = ('User', 'Cmd', 'Entrypoint', 'WorkingDir', 'Healthcheck', 'StopTimeout', 'StopSignal')
IMAGE_FIELDS = ('User', 'Cmd', 'Entrypoint', 'WorkingDir', 'Env', 'Healthcheck',
                'StopSignal', 'Volumes', 'ExposedPorts', 'Shell', 'OnBuild')
spec = importlib.util.spec_from_file_location('standard_backup', ROOT / 'scripts/approved-ops/backup_support.py')
backup = importlib.util.module_from_spec(spec)
spec.loader.exec_module(backup)


class Blocked(Exception):
    pass


class Uncertain(Blocked):
    """An interrupted Docker mutation may still be running in the daemon."""


def need(value, reason):
    if not value:
        raise Blocked(reason)


def command(args, timeout=30, mutation=False, input_data=None):
    # Retain no raw stdout/stderr on failure: Compose/inspect may contain secrets.
    try:
        stream = {'stdin': subprocess.DEVNULL} if input_data is None else {'input': input_data}
        result = subprocess.run(args, cwd=ROOT, **stream,
                                capture_output=True, timeout=timeout, check=False)
    except (subprocess.TimeoutExpired, KeyboardInterrupt):
        raise Uncertain('CommandInterrupted') if mutation else Blocked('CommandInterrupted') from None
    need(result.returncode == 0, 'CommandFailed')
    return result.stdout


def docker(*args, timeout=30, mutation=False, input_data=None):
    # Fixed local daemon, never a caller-selected context/remote host.
    extra = {} if input_data is None else {'input_data': input_data}
    return command(['docker', '--host', 'unix:///var/run/docker.sock', *args], timeout, mutation, **extra)


def inspect(identifier):
    return json.loads(docker('inspect', identifier))[0]


def image_info(identifier):
    need(re.fullmatch(IMAGE, identifier), 'UnpinnedImage')
    value = json.loads(docker('image', 'inspect', identifier))[0]
    need(value['Id'] == identifier, 'ImageIdentityChanged')
    return value


def fingerprint(c):
    host = copy.deepcopy(c['HostConfig'])
    for key in ('Binds', 'CapAdd', 'CapDrop', 'SecurityOpt', 'GroupAdd'):
        if isinstance(host.get(key), list):
            host[key].sort()
    value = {'config': {k: c['Config'].get(k) for k in CONFIG_FIELDS},
             'env': sorted(c['Config']['Env']), 'host': host,
             'mounts': sorted((m['Type'], m.get('Name'), m['Source'], m['Destination'], m['RW']) for m in c['Mounts']),
             'networks': sorted(c['NetworkSettings']['Networks'])}
    return hashlib.sha256(json.dumps(value, sort_keys=True).encode()).hexdigest()


def healthy(c):
    need(c['State']['Running'] and not c['State']['OOMKilled']
         and c['State'].get('Health', {}).get('Status') == 'healthy', 'NotHealthy')


def http(role, path):
    need((role, path) in {('api', '/health'), ('web', '/'), ('web', '/icons/bigbrain-32.png')}, 'HttpScope')
    class NoRedirect(urllib.request.HTTPRedirectHandler):
        def redirect_request(self, *args, **kwargs):
            raise Blocked('HttpRedirect')
    opener = urllib.request.build_opener(urllib.request.ProxyHandler({}), NoRedirect())
    port = 18080 if role == 'api' else 13000
    with opener.open(f'http://127.0.0.1:{port}{path}', timeout=3) as response:
        need(response.status == 200, 'HttpStatus')
        value = response.read(2 * 1024 * 1024 + 1)
        need(len(value) <= 2 * 1024 * 1024, 'HttpSize')
        return value


def http_health(role):
    if role == 'api':
        http(role, '/health')
    else:
        need(re.search(br'<html[\s>]', http(role, '/'), re.I), 'WebHtml')
        need(http(role, '/icons/bigbrain-32.png').startswith(b'\x89PNG\r\n\x1a\n'), 'WebIcon')


def ci_green(revision):
    value = json.loads(command(['gh', 'api', f'repos/T-bear/BigBrain/commits/{revision}/check-runs?per_page=100']))
    need(value['total_count'] <= 100, 'CiPagination')
    for name in CHECKS:
        runs = [c for c in value['check_runs'] if c['name'] == name and c['app']['slug'] == 'github-actions']
        need(runs and all(c['status'] == 'completed' and c['conclusion'] == 'success' for c in runs), 'CiNotGreen')


def legacy_web_provenance(image):
    """Read only the existing local builder; no tag, supplied receipt or label fallback."""
    prior = image_info(image)
    need(prior['Config'].get('Labels', {}).get('org.opencontainers.image.source') == NGINX_SOURCE,
         'LegacyWebSourceUnknown')
    rows = [json.loads(line) for line in docker('buildx', 'history', 'ls', '--builder', 'default',
            '--no-trunc', '--format', 'json').splitlines()]
    matches = []
    for row in rows:
        if row.get('status') != 'Completed' or 'bigbrain.web' not in row.get('name', '').lower():
            continue
        need(re.fullmatch(r'default/default/[a-z0-9]{25}', row.get('ref', '')), 'BuildRecordLocationUnknown')
        ref = row['ref'].split('/')[-1]
        raw = docker('buildx', 'history', 'inspect', '--builder', 'default', '--format', 'json', ref)
        record = json.loads(raw)
        attachments = record.get('Attachments', [])
        images = [a for a in attachments if a.get('Digest') == image and a.get('Type') in
                  {'application/vnd.oci.image.index.v1+json', 'application/vnd.oci.image.manifest.v1+json'}]
        if not images:
            continue
        need(len(images) == 1 and record.get('Status') == 'completed'
             and record.get('Ref') == ref and record.get('Dockerfile') == 'src/BigBrain.Web/Dockerfile'
             and record.get('VCSRepository') in REPOSITORIES, 'BuildRecordMismatch')
        revision = record.get('VCSRevision', '')
        need(re.fullmatch(SHA, revision), 'BuildRevisionUnknown')
        attestations = [a for a in attachments if a.get('Type') == 'https://slsa.dev/provenance/v1']
        need(len(attestations) == 1 and re.fullmatch(IMAGE, attestations[0].get('Digest', '')), 'BuildProvenanceMissing')
        raw_proof = docker('buildx', 'history', 'inspect', 'attachment', '--builder', 'default',
                           ref, attestations[0]['Digest'])
        proof = json.loads(raw_proof)
        definition, details = proof.get('buildDefinition', {}), proof.get('runDetails', {})
        args = definition.get('externalParameters', {}).get('request', {}).get('root', {}).get('request', {}).get('args', {})
        metadata = details.get('metadata', {})
        vcs = metadata.get('buildkit_metadata', {}).get('vcs', {})
        need(definition.get('buildType') == 'https://github.com/moby/buildkit/blob/master/docs/attestations/slsa-definitions.md'
             and definition.get('externalParameters', {}).get('configSource', {}).get('path') == 'Dockerfile'
             and args.get('vcs:revision') == vcs.get('revision') == revision
             and args.get('vcs:source') == vcs.get('source') == record['VCSRepository']
             and metadata.get('buildkit_completeness', {}).get('request') is True,
             'BuildProvenanceMismatch')
        # Preserve raw builder evidence privately on execute, never print it.
        matches.append({'image': image, 'revision': revision, 'ref': ref,
                        'record': record, 'provenanceDigest': attestations[0]['Digest'], 'provenance': proof})
    need(len(matches) == 1, 'BuildProvenanceAmbiguousOrMissing')
    return matches[0]


def git_guard(revision, originals):
    need(re.fullmatch(SHA, revision), 'InvalidRevision')
    remote = command(['git', 'ls-remote', 'origin', 'refs/heads/main']).decode().split()
    need(remote and remote[0] == revision, 'NotCurrentRemoteMain')
    need(command(['git', 'rev-parse', 'HEAD']).decode().strip() == revision, 'CheckoutNotAcceptedMain')
    need(not command(['git', 'status', '--porcelain', '--untracked-files=no']).strip(), 'TrackedChanges')
    command(['git', 'merge-base', '--is-ancestor', MEMORY_FETCH, revision])
    need(set(originals) == set(ROLES), 'MissingRuntimeBaseline')
    evidence = {}
    for role, old in originals.items():
        base = old['Config']['Labels'].get('org.opencontainers.image.revision', '')
        need(re.fullmatch(SHA, base), 'OldRevisionUnknown')
        try:
            need(command(['git', 'cat-file', '-t', base]).strip() == b'commit', 'OldRevisionUnavailable')
        except Blocked:
            if role != 'web' or old['Config']['Labels'].get('org.opencontainers.image.source') != NGINX_SOURCE:
                raise Blocked('OldRevisionUnavailable') from None
            evidence[role] = legacy_web_provenance(old['Image'])
            base = evidence[role]['revision']
            need(command(['git', 'cat-file', '-t', base]).strip() == b'commit', 'OldRevisionUnavailable')
        try:
            command(['git', 'merge-base', '--is-ancestor', base, revision])
        except Blocked:
            raise Blocked('OldRevisionNotAccepted') from None
        paths = command(['git', 'diff', '--no-renames', '--name-only', '-z', base, revision]).decode().split('\0')
        for path in filter(None, paths):
            # API and Web may have been deployed from different accepted commits.
            # The other application's delta is checked against ITS own baseline below.
            other_role = 'web' if role == 'api' else 'api'
            if path.startswith(SOURCE_TREES[other_role]):
                continue
            need(path in APP_FILES or path.startswith(('docs/', 'tests/', 'scripts/'))
                 or path in {'AGENTS.md', '.github/workflows/ci.yml'}, 'OutsideStandardScope')
    ci_green(revision)
    return evidence


def timesync_identity(container, service):
    declared = [m for m in service.get('volumes', []) if m.get('target') == '/host-timesync']
    actual = [m for m in container['Mounts'] if m['Destination'] == '/host-timesync']
    need(len(declared) == len(actual) == 1, 'TimesyncMountMissing')
    mount, live = declared[0], actual[0]
    source = Path('/run/systemd/timesync')
    need(mount.get('type') == live.get('Type') == 'bind'
         and mount.get('source') == live.get('Source') == str(source)
         and mount.get('read_only') is True and live.get('RW') is False
         and live.get('Propagation') == 'rprivate'
         and mount.get('bind', {}).get('propagation', 'rprivate') == 'rprivate', 'TimesyncMountMismatch')
    backup.no_symlinks(source)
    before = source.stat()
    need(stat.S_ISDIR(before.st_mode), 'TimesyncNotDirectory')
    # stat runs as the existing container user; no writes, shell, root override or restart.
    fields = docker('exec', container['Id'], 'stat', '-c', '%d:%i:%f', '/host-timesync').decode().strip().split(':')
    need(len(fields) == 3 and all(re.fullmatch(r'[0-9a-f]+', x) for x in fields), 'TimesyncIdentityUnknown')
    after = source.stat()
    identity = (before.st_dev, before.st_ino)
    need(identity == (after.st_dev, after.st_ino) == (int(fields[0]), int(fields[1]))
         and stat.S_ISDIR(int(fields[2], 16)), 'TimesyncIdentityMismatch')
    return identity


def writable_layer(role, container, service):
    identity = None
    for row in docker('diff', container['Id']).decode().splitlines():
        if role == 'api' and row == 'A /host-timesync':
            identity = timesync_identity(container, service)
            continue
        need(re.fullmatch(r'[ACD] /.+', row) and any(row[2:] == p or row[2:].startswith(p + '/')
             for p in ('/tmp', '/var/cache/nginx', '/var/run', '/run')), 'RollbackImageHasRuntimeRepairs')
    return identity


def release_config(config, images):
    need(config.get('name') == PROJECT, 'WrongComposeProject')
    result = copy.deepcopy(config)
    result['services'] = {r: result['services'][r] for r in ROLES}
    for role, service in result['services'].items():
        need(not any(k in service for k in ('profiles', 'extends', 'env_file', 'label_file', 'configs', 'secrets', 'develop', 'volumes_from', 'links')), 'UnsupportedCompose')
        service.pop('build', None)
        service.pop('depends_on', None)
        service['image'] = images[role]
        service['pull_policy'] = 'never'
    # Existing volumes/networks only. Never let Compose create a network or volume.
    for kind in ('volumes', 'networks'):
        result[kind] = {key: {'name': entry['name'], 'external': True}
                        for key, entry in result.get(kind, {}).items()}
    result.pop('configs', None)
    result.pop('secrets', None)
    return result


def schemas(roots):
    result = {}
    for target, names in backup.EXPECTED.items():
        for name in names:
            path = roots[target][0] / name
            with closing(sqlite3.connect(path.as_uri() + '?mode=ro', uri=True, timeout=3)) as db:
                db.execute('PRAGMA query_only=ON')
                result[target + '/' + name] = db.execute(
                    'SELECT type,name,tbl_name,sql FROM sqlite_schema ORDER BY type,name').fetchall()
    return result


def snapshot_matches(rendered, expected):
    # Compose's canonical output is already dollar-escaped for safe re-use.
    # Re-rendering may add empty IPAM metadata or omit empty top-level maps.
    for value in (rendered, expected):
        for network in value.get('networks', {}).values():
            if network.get('ipam') == {}:
                network.pop('ipam')
        for key in ('volumes', 'networks'):
            if value.get(key) == {}:
                value.pop(key)
    return rendered == expected


def save(path, value):
    # Exclusive artifacts: no existing file or recovery evidence is overwritten.
    fd = os.open(path, os.O_WRONLY | os.O_CREAT | os.O_EXCL | os.O_NOFOLLOW, 0o600)
    with os.fdopen(fd, 'w', encoding='utf8') as stream:
        json.dump(value, stream, sort_keys=True, indent=2)
        stream.flush()
        os.fsync(stream.fileno())
    sync_dir(path.parent)


def sync_dir(path):
    fd = os.open(path, os.O_RDONLY | os.O_DIRECTORY)
    try:
        os.fsync(fd)
    finally:
        os.close(fd)


def record(folder, phase):
    with (folder / 'journal.jsonl').open('a', encoding='utf8') as stream:
        stream.write(json.dumps({'phase': phase, 'at': backup.utc()}) + '\n')
        stream.flush()
        os.fsync(stream.fileno())
    sync_dir(folder)


def no_pending_run(state):
    for folder in state.glob('run-*'):
        journal = folder / 'journal.jsonl'
        rows = journal.read_text().splitlines() if journal.exists() else []
        need(rows and json.loads(rows[-1])['phase'] in {'PASS', 'ROLLED_BACK', 'PRE_SWITCH_FAILED'}, 'PreviousRunNeedsReview')


class Deployment:
    def __init__(self, revision, images, compose_file, state):
        self.revision, self.images, self.source, self.state = revision, images, compose_file, state
        self.originals = {}
        self.changed = []
        self.folder = None
        self.source_files = [compose_file] if compose_file else []
        self.project_directory = compose_file.parent if compose_file else None
        self.probes = set()
        self.env_files = []
        self.reconstruct = False
        self.provenance = {}
        self.timesync = None

    def compose_model(self, config, *args):
        return docker('compose', '--project-name', PROJECT, '--project-directory', str(self.project_directory),
                      '--env-file', '/dev/null', '-f', '-', *args, timeout=120,
                      input_data=json.dumps(config).encode())

    def compose(self, path, *args, mutation=False):
        files = self.source_files if path == self.source else [path]
        # Resolve ordinary YAML/overrides and .env ourselves; no prepared JSON required.
        env_files = self.env_files or [self.project_directory / '.env']
        if path == self.source and self.env_files:
            need(all(env.is_file() for env in self.env_files), 'ComposeEnvironmentMissing')
        if path != self.source:
            env_files = [Path('/dev/null')]
        env_args = [arg for env in env_files for arg in ('--env-file', str(env if env.exists() else Path('/dev/null')))]
        return docker('compose', '--project-name', PROJECT, '--project-directory', str(self.project_directory),
                      *env_args,
                      *[arg for source in files for arg in ('-f', str(source))],
                      *args, timeout=120, mutation=mutation)

    def discover_compose(self):
        if self.source is not None:
            return
        labels = [self.current(r)['Config']['Labels'] for r in ROLES]
        keys = ('com.docker.compose.project.config_files', 'com.docker.compose.project.working_dir')
        need(all(l.get(k) for l in labels for k in keys), 'ComposeProvenanceMissing')
        self.reconstruct = any(labels[0][k] != labels[1][k] for k in keys)
        if self.reconstruct:
            # Only the diagnosed legacy API + ordinary Web combination is supported.
            need(re.fullmatch(r'/var/lib/bigbrain-approved-ops/runs/[a-zA-Z0-9_-]+/release\.compose\.json', labels[0][keys[0]])
                 and Path(labels[1][keys[0]]).name == 'compose.yaml'
                 and ',' not in labels[1][keys[0]], 'ComposeProvenanceMissing')
        selected = labels[1] if self.reconstruct else labels[0]
        self.source_files = [Path(x) for x in selected[keys[0]].split(',')]
        self.source = self.source_files[0]
        env_key = 'com.docker.compose.project.environment_file'
        need(self.reconstruct or labels[0].get(env_key, '') == labels[1].get(env_key, ''), 'ComposeEnvironmentMismatch')
        self.env_files = [Path(x) for x in selected.get(env_key, '').split(',') if x]
        for path in self.env_files:
            need(path.is_absolute() and path.is_file(), 'ComposeEnvironmentMissing')
            backup.no_symlinks(path)
        self.project_directory = Path(selected[keys[1]])
        need(self.project_directory.is_absolute(), 'ComposeDirectoryInvalid')
        backup.no_symlinks(self.project_directory)

    def configuration(self, originals):
        self.discover_compose()
        for path in self.source_files:
            need(path.is_absolute() and path.is_file(), 'ComposeMissing')
            backup.no_symlinks(path)
        config = json.loads(self.compose(self.source, 'config', '--format', 'json'))
        if self.reconstruct:
            web_hash = self.compose(self.source, 'config', '--hash', 'web').decode().split()
            need(web_hash == ['web', originals['web']['Config']['Labels'].get('com.docker.compose.config-hash')], 'ComposeRuntimeMismatch')
            api = config['services']['api']
            api['image'] = originals['api']['Image']
            api.setdefault('labels', {})['org.opencontainers.image.revision'] = originals['api']['Config']['Labels']['org.opencontainers.image.revision']
            env = originals['api']['Config']['Env']
            need(env and all(isinstance(x, str) and re.match(r'^[A-Za-z_][A-Za-z0-9_]*=', x) for x in env), 'RuntimeEnvironmentUnknown')
            pairs = [x.split('=', 1) for x in env]
            need(len({k for k, v in pairs}) == len(pairs), 'RuntimeEnvironmentAmbiguous')
            api['environment'] = {k: v.replace('$', '$$') for k, v in pairs}
            hashes = dict(line.split() for line in self.compose_model(config, 'config', '--hash', '*').decode().splitlines())
            rendered = json.loads(self.compose_model(config, 'config', '--format', 'json'))
            need(snapshot_matches(rendered, copy.deepcopy(config)), 'SnapshotChanged')
        else:
            hashes = dict(line.split() for line in self.compose(self.source, 'config', '--hash', '*').decode().splitlines())
        need(all(hashes.get(r) and hashes[r] == originals[r]['Config']['Labels'].get('com.docker.compose.config-hash') for r in ROLES), 'ComposeRuntimeMismatch')
        return config

    def current(self, role):
        ids = docker('ps', '-aq', '--no-trunc', '--filter', f'label=com.docker.compose.project={PROJECT}',
                     '--filter', f'label=com.docker.compose.service={role}').decode().split()
        need(len(ids) == 1, 'ServiceIdentityAmbiguous')
        return inspect(ids[0])

    def others(self):
        target_ids = {self.current(r)['Id'] for r in ROLES}
        result = {}
        for cid in docker('ps', '-aq', '--no-trunc').decode().split():
            if cid not in target_ids and cid not in self.probes:
                c = inspect(cid)
                result[cid] = (c['Image'], fingerprint(c), c['State']['Running'], c['State']['StartedAt'])
        return result

    def unchanged(self):
        need(all(not inspect(cid)['State']['Running'] for cid in self.probes), 'ProbeStillRunning')
        need(self.others() == self.other_snapshot, 'OtherServiceChanged')
        for role, old in self.originals.items():
            current = self.current(role)
            need(current['Id'] == old['Id'] and current['Image'] == old['Image']
                 and fingerprint(current) == fingerprint(old)
                 and current['State']['StartedAt'] == old['State']['StartedAt'], 'RuntimeChanged')
            healthy(current)
        if self.timesync is not None:
            need(timesync_identity(self.current('api'), self.config['services']['api']) == self.timesync, 'TimesyncIdentityMismatch')

    def preflight(self):
        originals = {r: self.current(r) for r in ROLES}
        self.config = self.configuration(originals)
        for role in ROLES:
            old = originals[role]
            healthy(old)
            prior = image_info(old['Image'])
            need(prior['Config'].get('Labels', {}).get('org.opencontainers.image.revision')
                 == old['Config']['Labels'].get('org.opencontainers.image.revision'), 'OldImageRevisionMismatch')
            need(all(old['Config'].get(k) == prior['Config'].get(k)
                     for k in ('User', 'Cmd', 'Entrypoint', 'WorkingDir')), 'RuntimeEntrypointOverride')
            # A healthy container repaired in its writable layer is not a safe rollback image.
            identity = writable_layer(role, old, self.config['services'][role])
            if identity is not None:
                self.timesync = identity
            for name in old['Config']['Env']:
                need('VerifiedInitialNotBeforeUtc=' not in name, 'LegacyBootstrapPresent')
            self.originals[role] = old
            http_health(role)
        self.provenance = git_guard(self.revision, self.originals)
        self.other_snapshot = self.others()
        self.rollback = release_config(self.config, {r: c['Image'] for r, c in self.originals.items()})
        self.release = copy.deepcopy(self.rollback)
        if self.images:
            self.bind_images()
        # Resolve existing resource IDs; no creation is permitted by the execution snapshot.
        for kind, noun in (('networks', 'network'), ('volumes', 'volume')):
            for value in self.release.get(kind, {}).values():
                docker(noun, 'inspect', value['name'])
        self.roots = backup.sources(self.originals['api'])
        self.inventory, _ = backup.inventory(self.roots)
        self.schemas = schemas(self.roots)
        self.unchanged()

    def bind_images(self):
        for role, old in self.originals.items():
            prior, candidate = image_info(old['Image']), image_info(self.images[role])
            need(candidate['Config'].get('Labels', {}).get('org.opencontainers.image.revision') == self.revision, 'ImageRevisionMismatch')
            need(all(candidate['Config'].get(k) == prior['Config'].get(k) for k in IMAGE_FIELDS), 'ImageRuntimeContractChanged')
        self.release = release_config(self.config, self.images)
        for service in self.release['services'].values():
            service.setdefault('labels', {})['org.opencontainers.image.revision'] = self.revision

    def build_images(self):
        # No workspace/untracked file enters the context. Keep archive + base bindings.
        need(git_guard(self.revision, self.originals) == self.provenance, 'BuildProvenanceChanged')
        archive = self.folder / 'source.tar'
        command(['git', 'archive', '--format=tar', '--output=' + str(archive), self.revision], timeout=120)
        source = self.folder / 'source'
        source.mkdir()
        with tarfile.open(archive) as bundle:
            need(all(m.isfile() or m.isdir() for m in bundle.getmembers()), 'UnsupportedBuildTree')
            bundle.extractall(source, filter='data')
        bases = {}
        for role in ROLES:
            original = source / ('src/BigBrain.' + ('Api' if role == 'api' else 'Web') + '/Dockerfile')
            lines = original.read_text().splitlines()
            for index, line in enumerate(lines):
                if not line.upper().startswith('FROM '):
                    continue
                fields = line.split()
                ref = fields[1]
                need(re.fullmatch(r'[a-z0-9./_-]+:[A-Za-z0-9._-]+', ref), 'UnsupportedBuildBase')
                if ref not in bases:
                    docker('pull', ref, timeout=600, mutation=True)
                    info = json.loads(docker('image', 'inspect', ref))[0]
                    repo = ref.rsplit(':', 1)[0]
                    matches = [x for x in info.get('RepoDigests', []) if x.startswith(repo + '@sha256:')]
                    need(len(matches) == 1 and re.fullmatch(r'.+@sha256:[0-9a-f]{64}', matches[0]), 'UnpinnedBuildBase')
                    bases[ref] = matches[0]
                fields[1] = bases[ref]
                lines[index] = ' '.join(fields)
            pinned = self.folder / (role + '.Dockerfile')
            pinned.write_text('\n'.join(lines) + '\n')
            iid = self.folder / (role + '.iid')
            docker('build', '--pull=false', '--file', str(pinned), '--iidfile', str(iid),
                   '--tag', f'bigbrain-standard-{role}:{self.revision}-{self.folder.name}',
                   '--label', 'org.opencontainers.image.revision=' + self.revision,
                   '--build-arg', 'BIGBRAIN_REVISION=' + self.revision, str(source), timeout=1200, mutation=True)
            self.images[role] = iid.read_text().strip()
            image_info(self.images[role])
            self.unchanged()
        self.bind_images()
        save(self.folder / 'build.json', {'revision': self.revision,
             'tree': command(['git', 'rev-parse', self.revision + '^{tree}']).decode().strip(),
             'archiveSha256': backup.digest(archive), 'bases': bases, 'images': self.images})

    def prove_restartability(self, backup_folder):
        results = []
        for role in ROLES:
            for purpose, image in (('rollback', self.originals[role]['Image']), ('candidate', self.images[role])):
                self.probe_image(role, image, backup_folder, purpose)
                results.append({'role': role, 'purpose': purpose, 'image': image, 'starts': 2, 'status': 'PASS'})
                self.unchanged()
        save(self.folder / 'restartability.json', {'backupManifestSha256': backup.verify_set(backup_folder),
             'isolation': 'network-none; no production mounts or ports; disposable backup copies', 'results': results})

    def probe_image(self, role, image, backup_folder, purpose):
        # Actual immutable image, entrypoint and non-root user. Never use the old
        # container's writable layer, production mounts, provider network or Docker socket.
        old = self.originals[role]
        uid = old['Config'].get('User', '')
        need(re.fullmatch(r'[1-9][0-9]*(?::[1-9][0-9]*)?', uid), 'ProbeUserUnknown')
        uid_number = uid.split(':')[0]
        gid = uid.split(':')[-1]
        folder = self.folder / ('probe-' + role + '-' + purpose)
        folder.mkdir(mode=0o700)
        args = ['create', '--name', 'bigbrain-standard-' + uuid.uuid4().hex,
                '--label', 'bigbrain.standard.probe=' + self.folder.name,
                '--network', 'none', '--read-only', '--cap-drop', 'ALL',
                '--security-opt', 'no-new-privileges', '--restart', 'no',
                '--memory', '768m', '--cpus', '1', '--pids-limit', '256', '--user', uid,
                '--tmpfs', f'/tmp:rw,nosuid,nodev,uid={uid_number},gid={gid},mode=0700']
        env = dict(entry.split('=', 1) for entry in old['Config']['Env'])
        roots = {}
        if role == 'api':
            # Disable external work only in the probe; production snapshot is unchanged.
            for key in list(env):
                if key.lower().endswith('__enabled') or key.lower().endswith('__directaudioimportenabled'):
                    env[key] = 'false'
            for key in ('MealPlanner__SchoolMeals__Enabled', 'MealPlanner__SchoolMeals__RosenfeldtEnabled',
                        'MealPlanner__SchoolMeals__MusikugglanEnabled', 'Sentinel__Enabled',
                        'Finance__RecurringLocalReasoner__Enabled', 'Finance__ResearchScheduler__Enabled',
                        'Finance__ObservationRuntime__Enabled', 'Media__Requests__DirectAudioImportEnabled',
                        'Media__SmartShuffle__Enabled'):
                env[key] = 'false'
            env['Finance__ResearchOperations__MaintenancePaused'] = 'true'
            for target, names in backup.EXPECTED.items():
                copies = folder / target.lstrip('/')
                copies.mkdir()
                # Accessible to the image UID only through its bind mount. The host
                # parent remains 0700; original backups and production stay untouched.
                copies.chmod(0o777)
                for name in names:
                    shutil.copyfile(backup_folder / target.lstrip('/') / name, copies / name)
                    (copies / name).chmod(0o666)
                roots[target] = (copies, 'isolated-copy')
                args += ['--mount', f'type=bind,source={copies},target={target}']
            for target in ('/finance-data', '/app/data'):
                args += ['--tmpfs', f'{target}:rw,nosuid,nodev,uid={uid_number},gid={gid},mode=0700']
        else:
            # nginx resolves its upstream at startup, but no upstream request is made.
            args += ['--add-host', 'api:127.0.0.1', '--tmpfs', f'/var/cache/nginx:uid={uid_number},gid={gid}',
                     '--tmpfs', f'/run:uid={uid_number},gid={gid}']
        need(all('\n' not in k + v and '\r' not in k + v for k, v in env.items()), 'ProbeEnvironmentInvalid')
        env_file = folder / 'environment'
        env_file.write_text(''.join(k + '=' + v + '\n' for k, v in env.items()))
        env_file.chmod(0o600)
        args += ['--env-file', str(env_file), image]
        save(folder / 'intent.json', {'name': args[2], 'image': image})
        cid = docker(*args, mutation=True).decode().strip()
        need(re.fullmatch(r'[0-9a-f]{64}', cid), 'ProbeIdentityUnknown')
        self.probes.add(cid)
        save(folder / 'identity.json', {'containerId': cid, 'image': image})
        initial_schema = schemas(roots) if roots else None
        try:
            for cycle in range(2):
                docker('start', cid, mutation=True)
                deadline = time.monotonic() + 60
                while True:
                    c = inspect(cid)
                    need(c['Image'] == image and c['State']['Running'] and not c['State']['OOMKilled'], 'ProbeExited')
                    need(c['HostConfig']['NetworkMode'] == 'none' and not c['HostConfig'].get('PortBindings')
                         and c['Config']['User'] == uid, 'ProbeIsolationChanged')
                    try:
                        if role == 'api':
                            check = "exec 3<>/dev/tcp/127.0.0.1/8080; printf 'GET /health HTTP/1.1\\r\\nHost: localhost\\r\\nConnection: close\\r\\n\\r\\n' >&3; IFS= read -r line <&3; [[ $line == *' 200 '* ]]"
                            docker('exec', cid, '/bin/bash', '-ec', check)
                        else:
                            docker('exec', cid, 'wget', '--spider', '-q', 'http://127.0.0.1:8080/')
                        break
                    except Blocked:
                        need(time.monotonic() < deadline, 'ProbeHealthDeadline')
                        time.sleep(1)
                docker('stop', '--time', '-1', cid, timeout=90, mutation=True)
                stopped = inspect(cid)
                need(not stopped['State']['Running'] and stopped['State']['ExitCode'] == 0
                     and not stopped['State']['OOMKilled'], 'ProbeUncleanStop')
                if roots:
                    need(schemas(roots) == initial_schema, 'ProbeSchemaChanged')
                record(self.folder, 'PROBE_' + role.upper() + '_' + purpose.upper() + '_' + str(cycle + 1))
        finally:
            # Retain the uniquely owned stopped probe and its private evidence. No rm,
            # production stop or force-kill; uncertain cleanup blocks the release.
            if not isinstance(sys.exc_info()[1], (Uncertain, KeyboardInterrupt, SystemExit)):
                try:
                    c = inspect(cid)
                    if c['State']['Running']:
                        docker('stop', '--time', '-1', cid, timeout=90, mutation=True)
                    need(not inspect(cid)['State']['Running'], 'ProbeStillRunning')
                except BaseException:
                    raise Uncertain('ProbeCleanupUncertain') from None

    def data_identity(self):
        need(schemas(self.roots) == self.schemas, 'SchemaChangedNoAutomaticRollback')
        after, _ = backup.inventory(self.roots)
        need(after.keys() == self.inventory.keys(), 'DatabaseCoverageChanged')
        for key, old in self.inventory.items():
            need(all(after[key][k] == old[k] for k in ('device', 'inode', 'uid', 'gid', 'mode')), 'DatabaseIdentityChanged')

    def wait_healthy(self, role, image):
        deadline = time.monotonic() + 100
        while time.monotonic() < deadline:
            c = self.current(role)
            need(c['Image'] == image and fingerprint(c) == fingerprint(self.originals[role]), 'ReplacementConfigChanged')
            need(c['State']['Running'] or c['State'].get('Restarting'), 'ReplacementExited')
            if role == 'api' and self.timesync is not None and c['State']['Running']:
                need(timesync_identity(c, self.config['services']['api']) == self.timesync, 'TimesyncIdentityMismatch')
            try:
                healthy(c)
                http_health(role)
                return
            except (Blocked, OSError):
                time.sleep(2)
        raise Blocked('HealthDeadline')

    def stop(self, role, recovery=False):
        c = self.current(role)
        if c['State']['Running'] or c['State'].get('Restarting'):
            docker('stop', '--time', '-1', c['Id'], timeout=90, mutation=True)
        stopped = inspect(c['Id'])
        # A failed candidate is not the original healthy service. Its known exit=1
        # must not prevent image rollback, but never waive the original's clean stop.
        failed_candidate = (recovery and stopped['Id'] != self.originals[role]['Id']
                            and stopped['Image'] == self.images[role]
                            and fingerprint(stopped) == fingerprint(self.originals[role])
                            and stopped['State'].get('Status') == 'exited')
        need(not stopped['State']['Running'] and not stopped['State'].get('Restarting')
             and not stopped['State'].get('Dead') and not stopped['State']['OOMKilled']
             and (stopped['State']['ExitCode'] == 0 or failed_candidate), 'UncleanStop')

    def switch(self, role, recovery=False):
        self.stop(role, recovery=recovery)
        path = self.folder / ('rollback.json' if recovery else 'release.json')
        self.compose(path, 'up', '-d', '--no-deps', '--no-build', '--pull', 'never', '--force-recreate', role, mutation=True)
        self.wait_healthy(role, self.originals[role]['Image'] if recovery else self.images[role])

    def execute(self):
        no_pending_run(self.state)
        self.folder = self.state / ('run-' + uuid.uuid4().hex)
        self.folder.mkdir(mode=0o700)
        sync_dir(self.state)
        record(self.folder, 'PREPARING')
        try:
            save(self.folder / 'baseline.json', self.config)
            save(self.folder / 'provenance.json', self.provenance)
            record(self.folder, 'BUILDING')
            self.build_images()
            save(self.folder / 'release.json', self.release)
            save(self.folder / 'rollback.json', self.rollback)
            # Preserve canonical Compose escaping; verify the frozen execution model.
            for name, expected in (('release', self.release), ('rollback', self.rollback)):
                rendered = json.loads(self.compose(self.folder / (name + '.json'), 'config', '--format', 'json'))
                need(snapshot_matches(rendered, copy.deepcopy(expected)), 'SnapshotChanged')
            identity = {'containerId': self.originals['api']['Id'], 'image': self.originals['api']['Image'],
                        'revision': self.originals['api']['Config']['Labels']['org.opencontainers.image.revision']}
            folder = backup.run_backup(self.roots, self.state / 'backups', identity, self.unchanged)
            digest = backup.verify_set(folder)
            self.prove_restartability(folder)
            need(backup.verify_set(folder) == digest, 'BackupChangedDuringProbe')
            save(self.folder / 'receipt.json', {'revision': self.revision, 'images': self.images,
                 'previous': {r: c['Image'] for r, c in self.originals.items()},
                 'backupId': folder.name, 'backupManifestSha256': digest})
            need(git_guard(self.revision, self.originals) == self.provenance, 'BuildProvenanceChanged')
            self.unchanged()
            self.data_identity()
            for role in ROLES:
                # Persist intent before every mutation; abrupt death blocks automatic retries.
                record(self.folder, 'SWITCHING_' + role.upper())
                self.changed.append(role)
                self.switch(role)
            self.data_identity()
            need(self.others() == self.other_snapshot, 'OtherServiceChanged')
            record(self.folder, 'PASS')
        except BaseException as error:
            if not self.changed:
                record(self.folder, 'MANUAL_REVIEW_REQUIRED' if isinstance(error, (Uncertain, KeyboardInterrupt, SystemExit)) else 'PRE_SWITCH_FAILED')
            elif isinstance(error, (Uncertain, KeyboardInterrupt, SystemExit)):
                record(self.folder, 'MANUAL_REVIEW_REQUIRED')
            else:
                try:
                    # No DB restore: preserve writes made after the backup.
                    self.data_identity()
                    need(self.others() == self.other_snapshot, 'OtherServiceChanged')
                    record(self.folder, 'ROLLING_BACK')
                    for role in self.changed:  # API before Web, also on recovery.
                        self.switch(role, recovery=True)
                    for role in ROLES:
                        self.wait_healthy(role, self.originals[role]['Image'])
                    self.data_identity()
                    need(self.others() == self.other_snapshot, 'OtherServiceChanged')
                    record(self.folder, 'ROLLED_BACK')
                except BaseException:
                    record(self.folder, 'MANUAL_REVIEW_REQUIRED')
                    raise Blocked('RollbackFailedManualReview') from None
            raise


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--revision', required=True)
    parser.add_argument('--compose', type=Path, help='Optional ordinary Compose file; default discovers active files from runtime labels')
    parser.add_argument('--execute', action='store_true', help='Separately authorized deployment; default is read-only preflight')
    args = parser.parse_args()
    os.umask(0o077)
    # Compose interpolation must not inherit alternative files, projects or orphan removal.
    for name in list(os.environ):
        if name.startswith(('COMPOSE_', 'DOCKER_')):
            del os.environ[name]
    state = Path.home() / '.local/state/bigbrain-standard'
    deployment = Deployment(args.revision, {}, args.compose, state)
    if not args.execute:
        deployment.preflight()
        print('PREFLIGHT PASS; no backup, deployment or acceptance performed')
        return
    for folder in (state, Path.home() / '.local/state/bigbrain-web-deploy'):
        backup.no_symlinks(folder)
        folder.mkdir(mode=0o700, parents=True, exist_ok=True)
        need(folder.stat().st_uid == os.geteuid() and folder.stat().st_mode & 0o777 == 0o700, 'UnsafeState')
    # Same lock as the existing Web-only script; no concurrent STANDARD replacement.
    lock = Path.home() / '.local/state/bigbrain-web-deploy/lock'
    fd = os.open(lock, os.O_CREAT | os.O_WRONLY | os.O_NOFOLLOW, 0o600)
    with os.fdopen(fd, 'w') as stream:
        fcntl.flock(stream, fcntl.LOCK_EX | fcntl.LOCK_NB)
        no_pending_run(state)
        deployment.preflight()
        deployment.execute()
    print('PASS: API+Web replaced; iPhone/PWA and school source verification PENDING')


if __name__ == '__main__':
    try:
        for sig in (signal.SIGHUP, signal.SIGTERM):
            signal.signal(sig, lambda *_: (_ for _ in ()).throw(KeyboardInterrupt()))
        main()
    except SystemExit:
        raise
    except BaseException:
        print('STANDARD BLOCKED: inspect private journal; no automatic retry or database restore', file=sys.stderr)
        sys.exit(1)
