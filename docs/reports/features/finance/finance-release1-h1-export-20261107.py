#!/usr/bin/env python3
"""REVIEW ONLY. Owner-local copy preparation, never a Finance/research run.

Requires separate approval of new copies, private reader-group access and retention.
No systemd, namespaces, source permission changes, subprocesses or deletion.
"""
import datetime as dt
import errno
import hashlib
import json
import os
from pathlib import Path
import stat
import sys

SOURCE = Path('/var/lib/docker/volumes/bigbrain-sprint1_finance-market-data/_data/backups')
DEST = Path('/var/lib/bigbrain-finance-h1-20261107')
DB = 'bb-091-pre-migration-20260817.db'
DB_HASH = '84ef36437bdcd8b762b0a3243c6af705def029e036749ecce701264c28405986'
BACKUPS = ('finance-backup-3b093584173d74f6', 'finance-backup-8b549915b4e4129c')
READER = 1000
READ_UNTIL = '2026-11-09T20:00:00+01:00'
MAX_BYTES = 3 * 1024**3


def need(ok):
    if not ok:
        raise ValueError('precondition')


def pairs(rows):
    result = {}
    for k, v in rows:
        need(k not in result)
        result[k] = v
    return result


def reader_group():
    # Local account files only, no NSS/network lookup and no account changes.
    with open('/etc/passwd', encoding='utf-8') as stream:
        users = [line.rstrip('\n').split(':') for line in stream if line.strip()]
    with open('/etc/group', encoding='utf-8') as stream:
        groups = [line.rstrip('\n').split(':') for line in stream if line.strip()]
    need(all(len(row) == 7 for row in users) and all(len(row) == 4 for row in groups))
    reader = [row for row in users if row[2] == '1000']
    need(len(reader) == 1 and reader[0][3] == '1000')
    name = reader[0][0]
    group = [row for row in groups if row[2] == '1000']
    need(len(group) == 1 and set(filter(None, group[0][3].split(','))) <= {name})
    need({row[0] for row in users if row[3] == '1000'} == {name})


def no_acl(path):
    for attribute in ('system.posix_acl_access', 'system.posix_acl_default'):
        try:
            os.getxattr(path, attribute)
        except OSError as exc:
            need(exc.errno == 61)  # Linux ENODATA; fail on unsupported metadata.
        else:
            need(False)


def sig(s):
    return (s.st_dev, s.st_ino, s.st_mode, s.st_uid, s.st_gid,
            s.st_nlink, s.st_size, s.st_mtime_ns, s.st_ctime_ns)


def no_sidecars():
    for suffix in ('-wal', '-shm', '-journal'):
        need(not os.path.lexists(SOURCE / (DB + suffix)))


def ancestors(path):
    for p in (path, *path.parents):
        s = p.lstat()
        need(stat.S_ISDIR(s.st_mode) and s.st_uid == 0 and not s.st_mode & 0o022)
        no_acl(p)


def hash_fd(fd, output=None):
    os.lseek(fd, 0, os.SEEK_SET)
    h = hashlib.sha256()
    count = 0
    while True:
        block = os.read(fd, 1024 * 1024)
        if not block:
            break
        count += len(block)
        need(count <= MAX_BYTES)
        h.update(block)
        if output is not None:
            view = memoryview(block)
            while view:
                written = os.write(output, view)
                need(written > 0)
                view = view[written:]
    return count, h.hexdigest()


def prepare():
    need(os.getuid() == os.geteuid() == 0)
    need(sys.argv[1:] == ['--owner-approved-read-until', READ_UNTIL])
    need(dt.datetime.now(dt.timezone.utc) < dt.datetime.fromisoformat(READ_UNTIL))
    need(not os.path.lexists(DEST))  # Never reuse or overwrite a previous attempt.
    ancestors(DEST.parent)
    reader_group()
    no_sidecars()
    names = [DB] + [bid + suffix for bid in BACKUPS
                    for suffix in ('.manifest.json', '.data.json')]
    fds, before, parents = {}, {}, {}
    created = False
    complete = False
    try:
        # Fixed paths only. Refuse symlink components; no directory listing.
        for p in (SOURCE, *SOURCE.parents):
            s = p.lstat()
            need(stat.S_ISDIR(s.st_mode) and s.st_uid in (0, 1654))
            parents[p] = sig(s)[:5]
        for name in names:
            fd = os.open(SOURCE / name, os.O_RDONLY | os.O_NOFOLLOW |
                         os.O_CLOEXEC | os.O_NOATIME)
            fds[name] = fd
            s = os.fstat(fd)
            need(stat.S_ISREG(s.st_mode) and s.st_nlink == 1 and 0 < s.st_size <= MAX_BYTES)
            need(s.st_uid in (0, 1654))
            before[name] = sig(s)
            need(sig((SOURCE / name).lstat()) == before[name])
        inventory = {}
        for name, fd in fds.items():
            size, digest = hash_fd(fd)
            need(sig(os.fstat(fd)) == before[name])
            inventory[name] = {'bytes': size, 'sha256': digest}
        need(inventory[DB]['sha256'] == DB_HASH)
        for bid in BACKUPS:
            fd = fds[bid + '.manifest.json']
            need(inventory[bid + '.manifest.json']['bytes'] <= 1024 * 1024)
            os.lseek(fd, 0, os.SEEK_SET)
            m = json.loads(os.read(fd, 1024 * 1024 + 1), object_pairs_hook=pairs)
            need(m['backupId'] == bid and m['status'] == 'Complete')
            need(len(m['artifacts']) == 1)
            a = m['artifacts'][0]
            name = bid + '.data.json'
            expected = 'sha256:' + inventory[name]['sha256']
            need(a['path'] == name and a['bytes'] == inventory[name]['bytes'])
            need(a['sha256'] == m['contentFingerprint'] == expected)
            need(bid == 'finance-backup-' + expected[7:23])
        no_sidecars()
        total = sum(x['bytes'] for x in inventory.values())
        need(total <= MAX_BYTES)
        space = os.statvfs(DEST.parent)
        need(space.f_bavail * space.f_frsize >= total + 256 * 1024**2)
        os.mkdir(DEST, 0o700)
        created = True
        os.chown(DEST, 0, 1000)
        # Remove inherited ACLs before any content is staged.
        for attribute in ('system.posix_acl_access', 'system.posix_acl_default'):
            try:
                os.removexattr(DEST, attribute)
            except OSError as exc:
                need(exc.errno == errno.ENODATA)
        os.chmod(DEST, 0o700)
        for name in names:
            out = os.open(DEST / name, os.O_RDWR | os.O_CREAT | os.O_EXCL |
                          os.O_NOFOLLOW | os.O_CLOEXEC, 0o600)
            try:
                size, digest = hash_fd(fds[name], out)
                os.fsync(out)
                need({'bytes': size, 'sha256': digest} == inventory[name])
                need(hash_fd(out) == (size, digest))
                need(sig(os.fstat(fds[name])) == before[name])
            finally:
                os.close(out)
        for name, fd in fds.items():
            need(sig(os.fstat(fd)) == before[name])
            need(sig((SOURCE / name).lstat()) == before[name])
        for p, original in parents.items():
            need(sig(p.lstat())[:5] == original)
        no_sidecars()
        marker = {'status': 'complete', 'readerUid': READER, 'readUntil': READ_UNTIL,
                  'createdUtc': dt.datetime.now(dt.timezone.utc).isoformat(),
                  'scope': 'H1 historical evidence only; not current runtime or D3',
                  'files': inventory}
        fd = os.open(DEST / 'h1-custody.json', os.O_WRONLY | os.O_CREAT |
                     os.O_EXCL | os.O_NOFOLLOW, 0o600)
        with os.fdopen(fd, 'wb') as stream:
            stream.write((json.dumps(marker, sort_keys=True) + '\n').encode('ascii'))
            stream.flush()
            os.fsync(stream.fileno())
        for p in (DEST / n for n in names + ['h1-custody.json']):
            os.chown(p, 0, 1000)
            os.chmod(p, 0o440)
            no_acl(p)
        # Publish the private reader group last; partial files remain root-only.
        no_acl(DEST)
        os.chmod(DEST, 0o550)
        complete = True
        print('PASS H1_sources_and_copies_hash_verified')
        print('PASS H1_reader_1000_readonly; read_until=' + READ_UNTIL)
        print('NOT CHECKED H1_database_evidence; run_nonroot_verifier_separately')
    finally:
        for fd in fds.values():
            os.close(fd)
        if created and not complete:
            # Preserve financial copies for owner disposition; never delete/retry.
            os.chmod(DEST, 0o700)
            try:
                os.removexattr(DEST, 'system.posix_acl_access')
            except OSError as exc:
                if exc.errno != errno.ENODATA:
                    raise
            print('NOT CHECKED H1_partial_copy_private; owner_disposition_required')


def main():
    try:
        prepare()
        return 0
    except (Exception, KeyboardInterrupt):
        print('FAIL H1_export_stopped; no_retry; no_source_change_or_deletion_requested')
        return 1


if __name__ == '__main__':
    raise SystemExit(main())
