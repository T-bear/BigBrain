#!/usr/bin/python3
"""One-off, operator-run SQLite backup. No container actions or production restore."""
import datetime
from contextlib import closing
import hashlib
import json
import os
from pathlib import Path
import shutil
import signal
import sqlite3
import stat
import subprocess
import sys
import time
import uuid

EXPECTED = {
    '/data': ('meal-planner.db', 'audiobook-acquisition.db'),
    '/shopping-data': ('shopping-list.db',),
    '/calendar-data': ('calendar.db',),
    '/settings-data': ('settings.db',),
    '/lifecycle-data': ('lifecycle.db',),
}
IMAGE = 'sha256:8b92e5ea802f215427456e670875df07760e2eff5694bd4ef24d6ad6ed32aa5c'
BASE = Path('/var/backups/bigbrain-school-lunch')
MAX_BYTES = 256 * 1024 * 1024


class Blocked(Exception):
    pass


def require(value, code):
    if not value:
        raise Blocked(code)


def utc():
    return datetime.datetime.now(datetime.timezone.utc).isoformat()


def no_symlinks(path):
    require(path.is_absolute(), 'AbsolutePathRequired')
    for part in (*reversed(path.parents), path):
        require(not part.is_symlink(), 'SymlinkRejected')


def inspect():
    result = subprocess.run(['/usr/bin/docker', 'inspect', 'bigbrain-sprint1-api-1'],
                            capture_output=True, timeout=15, check=True,
                            env={'PATH': '/usr/bin:/bin', 'DOCKER_HOST': 'unix:///var/run/docker.sock'})
    c = json.loads(result.stdout)[0]
    require(c['Image'] == IMAGE and c['State']['Running']
            and c['State'].get('Health', {}).get('Status') == 'healthy', 'RuntimeDrift')
    return c


def sources(c):
    selected = {}
    for target in EXPECTED:
        mounts = [m for m in c['Mounts'] if m['Destination'] == target]
        require(len(mounts) == 1 and mounts[0]['Type'] == 'volume', 'MountMismatch')
        m = mounts[0]
        root = Path(m['Source'])
        no_symlinks(root)
        require(root.is_dir(), 'SourceDirectoryMissing')
        # No nested mounts, aliased sources or accidental inclusion of excluded storage.
        for other in c['Mounts']:
            if other is m:
                continue
            p = Path(other['Source'])
            require(not root.is_relative_to(p) and not p.is_relative_to(root), 'OverlappingSources')
            require(not other['Destination'].startswith(target + '/'), 'NestedMount')
        selected[target] = (root, m['Name'])
    return selected


def inventory(roots):
    result = {}
    total = 0
    for target, (root, volume) in roots.items():
        allowed = {n + suffix for n in EXPECTED[target] for suffix in ('', '-wal', '-shm')}
        names = set()
        for entry in root.iterdir():
            s = entry.lstat()
            require(entry.name in allowed and stat.S_ISREG(s.st_mode) and s.st_nlink == 1,
                    'UnexpectedEntryOrFileType')
            require(s.st_size <= MAX_BYTES, 'SourceTooLarge')
            names.add(entry.name)
            total += s.st_size
        require(set(EXPECTED[target]) <= names, 'MissingDatabase')
        for name in EXPECTED[target]:
            p = root / name
            s = p.stat()
            with p.open('rb') as f:
                require(f.read(16) == b'SQLite format 3\x00', 'NotSQLite')
            result[target + '/' + name] = {
                'volume': volume, 'hostSource': str(p), 'relativePath': target.lstrip('/') + '/' + name,
                'device': s.st_dev, 'inode': s.st_ino, 'uid': s.st_uid, 'gid': s.st_gid,
                'mode': stat.S_IMODE(s.st_mode), 'sourceBytes': s.st_size,
                'sourceMtimeNs': s.st_mtime_ns,
            }
    return result, total


def digest(path):
    with path.open('rb') as f:
        return hashlib.file_digest(f, 'sha256').hexdigest()


def verify_database(path):
    # Immutable is safe ONLY here: the closed, standalone backup has no writer/WAL.
    with closing(sqlite3.connect(path.as_uri() + '?mode=ro&immutable=1', uri=True)) as db:
        db.execute('PRAGMA query_only=ON')
        require(db.execute('PRAGMA integrity_check').fetchall() == [('ok',)], 'IntegrityFailed')
        require(db.execute('PRAGMA foreign_key_check').fetchone() is None, 'ForeignKeysFailed')
        count = db.execute('SELECT COUNT(*) FROM sqlite_schema').fetchone()[0]
        require(count > 0, 'EmptySchema')
        db.execute('SELECT type,name,tbl_name,rootpage,sql FROM sqlite_schema').fetchall()
    return {'integrity': 'PASS', 'foreignKeys': 'PASS', 'schema': 'PASS', 'schemaObjects': count}


def copy_database(source, destination, deadline):
    require(not destination.exists(), 'DestinationExists')
    with closing(sqlite3.connect(source.as_uri() + '?mode=ro', uri=True, timeout=3)) as src:
        src.execute('PRAGMA query_only=ON')
        # Never use immutable on a live source or issue a WAL checkpoint.
        with closing(sqlite3.connect(destination)) as dst:
            def progress(status, remaining, pages):
                require(time.monotonic() < deadline, 'BackupDeadline')
                require(pages * src.execute('PRAGMA page_size').fetchone()[0] <= MAX_BYTES,
                        'SnapshotTooLarge')
            src.backup(dst, pages=64, progress=progress, sleep=0.05)
    os.chmod(destination, 0o600)
    with destination.open('rb') as f:
        os.fsync(f.fileno())


def save_manifest(folder, value):
    temp = folder / 'manifest.pending'
    with temp.open('w', encoding='utf8') as f:
        json.dump(value, f, indent=2, sort_keys=True)
        f.write('\n')
        f.flush()
        os.fsync(f.fileno())
    os.replace(temp, folder / 'manifest.json')
    fd = os.open(folder, os.O_RDONLY | os.O_DIRECTORY)
    try:
        os.fsync(fd)
    finally:
        os.close(fd)


def run_backup(roots, base, identity, final_check):
    """Also used by offline tests with synthetic roots, never a production test flag."""
    os.umask(0o077)
    no_symlinks(base)
    base.mkdir(mode=0o700, exist_ok=True)
    require(base.stat().st_uid == os.geteuid() and stat.S_IMODE(base.stat().st_mode) == 0o700,
            'UnsafeDestinationPermissions')
    for root, _ in roots.values():
        require(not base.is_relative_to(root) and not root.is_relative_to(base), 'DestinationOverlap')
    folder = base / (datetime.datetime.now(datetime.timezone.utc).strftime('%Y%m%dT%H%M%SZ-') + uuid.uuid4().hex)
    folder.mkdir(mode=0o700)
    manifest = {'status': 'INCOMPLETE', 'id': folder.name, 'startedAt': utc(), 'runtime': identity,
                'method': 'sqlite-online-backup-readonly-source', 'consistency': 'per-database-not-cross-database',
                'scriptSha256': digest(Path(__file__).resolve()), 'files': [], 'excluded': ['Finance', 'Sentinel', 'identity-secrets', 'other-volumes']}
    save_manifest(folder, manifest)
    try:
        original, size = inventory(roots)
        require(shutil.disk_usage(base).free > max(64 * 1024 * 1024, size * 3), 'InsufficientSpace')
        deadline = time.monotonic() + 120
        for source_key, entry in original.items():
            dst = folder / entry['relativePath']
            dst.parent.mkdir(mode=0o700, exist_ok=True)
            start = utc()
            copy_database(Path(entry['hostSource']), dst, deadline)
            checks = verify_database(dst)
            sha = digest(dst)
            fd = os.open(dst.parent, os.O_RDONLY | os.O_DIRECTORY)
            try:
                os.fsync(fd)
            finally:
                os.close(fd)
            # Reopen only the backup; no restore into any production location.
            require(verify_database(dst) == checks and digest(dst) == sha, 'ReadbackMismatch')
            manifest['files'].append({**entry, 'source': source_key, 'snapshotStartedAt': start,
                                      'snapshotFinishedAt': utc(), 'bytes': dst.stat().st_size,
                                      'sha256': sha, 'checks': checks, 'isolatedReadback': 'PASS'})
            save_manifest(folder, manifest)
        after, _ = inventory(roots)
        require(original.keys() == after.keys(), 'InventoryDrift')
        for key in original:
            require(all(original[key][k] == after[key][k] for k in ('device', 'inode', 'uid', 'gid', 'mode')),
                    'SourceIdentityDrift')
        final_check()
        require(len(manifest['files']) == 6, 'IncompleteCoverage')
        manifest.update(status='PASS', finishedAt=utc())
        save_manifest(folder, manifest)
        return folder
    except BaseException as error:
        manifest.update(status='INCOMPLETE', failure= str(error) if isinstance(error, Blocked) else type(error).__name__)
        save_manifest(folder, manifest)
        print('INCOMPLETE: ' + str(folder), flush=True)
        raise


def verify_set(folder):
    """Verify retained artifacts only: never resolve/read original sources or call Docker."""
    no_symlinks(folder)
    no_symlinks(folder / 'manifest.json')
    m = json.loads((folder / 'manifest.json').read_text())
    require(m['status'] == 'PASS' and m['method'] == 'sqlite-online-backup-readonly-source', 'IncompleteManifest')
    paths = {target.lstrip('/') + '/' + name for target, names in EXPECTED.items() for name in names}
    require(len(m['files']) == 6 and {f['relativePath'] for f in m['files']} == paths, 'ManifestCoverage')
    for entry in m['files']:
        p = folder / entry['relativePath']
        no_symlinks(p)
        require(p.is_file() and p.stat().st_size <= MAX_BYTES and p.stat().st_size == entry['bytes'], 'BackupFileMismatch')
        require(not Path(str(p) + '-wal').exists() and not Path(str(p) + '-journal').exists(), 'BackupSidecarUnexpected')
        require(digest(p) == entry['sha256'], 'ChecksumMismatch')
        require(verify_database(p) == entry['checks'] and entry['isolatedReadback'] == 'PASS', 'VerificationMismatch')
    return digest(folder / 'manifest.json')


def main():
    require(os.geteuid() == 0, 'LocalSudoRequired')
    require(len(sys.argv) == 1 or (len(sys.argv) == 3 and sys.argv[1] == '--verify'), 'InvalidArguments')
    def timeout(signum, frame):
        raise Blocked('OperationInterrupted')
    for sig in (signal.SIGTERM, signal.SIGINT, signal.SIGALRM):
        signal.signal(sig, timeout)
    signal.alarm(180)
    # Root-owned destination chain; refuse symlink/writable-parent redirection.
    for p in (Path('/'), Path('/var'), Path('/var/backups')):
        s = p.lstat()
        require(stat.S_ISDIR(s.st_mode) and s.st_uid == 0 and not s.st_mode & 0o022, 'UnsafeBackupParent')
    if len(sys.argv) == 3:
        folder = Path(sys.argv[2])
        require(folder.parent == BASE and folder.name not in ('.', '..'), 'BackupLocationRequired')
        print('PASS: retained backup reverified; manifest SHA-256 ' + verify_set(folder))
        return
    before = inspect()
    roots = sources(before)
    def final_check():
        after = inspect()
        require(after['Id'] == before['Id'] and after['Mounts'] == before['Mounts'], 'RuntimeChanged')
    folder = run_backup(roots, BASE, {'containerId': before['Id'], 'image': before['Image']}, final_check)
    signal.alarm(0)
    print('PASS: 6/6 databases; integrity, foreign keys, schema, SHA-256 and isolated readback PASS')
    print('Backup: ' + str(folder))
    print('Manifest: ' + str(folder / 'manifest.json'))
    print('Manifest SHA-256: ' + digest(folder / 'manifest.json'))


if __name__ == '__main__':
    try:
        main()
    except BaseException as error:
        print('BLOCKED: ' + (str(error) if isinstance(error, Blocked) else type(error).__name__), file=sys.stderr)
        sys.exit(1)
