#!/usr/bin/python3
"""Owner-local, read-only active Media/Librarr verification; no deployment actions."""
import datetime
import hashlib
import json
import os
from pathlib import Path
import sqlite3
import stat
import subprocess
import sys
import time

TARGETS = {'api': ('/data', 'audiobook-acquisition.db'), 'librarr': ('/config', 'librarr.db')}
PHASE = 'plan'


def fingerprint(item):
    return hashlib.sha256(json.dumps({k: sorted(item[k], key=lambda v: v['Destination']) if k == 'Mounts' else item[k]
        for k in ('Id', 'Image', 'Config', 'HostConfig', 'Mounts')}, sort_keys=True).encode()).hexdigest()


def inspect(cid):
    assert len(cid) == 64 and all(c in '0123456789abcdef' for c in cid)
    return json.loads(subprocess.check_output(['/usr/bin/docker', '--host', 'unix:///var/run/docker.sock',
        'inspect', cid], stderr=subprocess.DEVNULL, timeout=3))[0]


def active_path(item, role):
    destination, filename = TARGETS[role]
    mount = next(m for m in item['Mounts'] if m['Destination'] == destination)
    assert mount['Type'] in ('bind', 'volume')
    path = Path(mount['Source']) / filename
    assert path.is_absolute() and path.resolve() == path, 'SymlinkOrPathMismatch'
    return path


def check_database(path, expected_identity, require_readonly=True, seconds=4):
    info = path.stat()
    assert stat.S_ISREG(info.st_mode) and [info.st_dev, info.st_ino] == expected_identity, 'DatabaseIdentityMismatch'
    assert 0 < info.st_size <= 32 * 1024**2, 'DatabaseSizeOutsideBound'
    if require_readonly:
        assert os.statvfs(path).f_flag & os.ST_RDONLY, 'ReadOnlyMountRequired'
    deadline = time.monotonic() + seconds
    def deadline_check(*unused):
        return int(time.monotonic() >= deadline)
    def progress(status, remaining, total):
        if status in (sqlite3.SQLITE_BUSY, sqlite3.SQLITE_LOCKED):
            raise RuntimeError('DatabaseBusyNoRetry')
        if deadline_check():
            raise TimeoutError('DatabaseCheckDeadline')
    # Online backup reads an actual current SQLite snapshot, including committed WAL pages.
    # Only the transient in-memory destination is writable. No immutable/nolock source flags.
    source = sqlite3.connect(path.as_uri() + '?mode=ro', uri=True, timeout=0)
    memory = sqlite3.connect(':memory:')
    try:
        source.set_progress_handler(deadline_check, 1000)
        memory.set_progress_handler(deadline_check, 1000)
        source.backup(memory, pages=32, progress=progress, sleep=0)
        assert memory.execute('PRAGMA integrity_check').fetchmany(2) == [('ok',)], 'IntegrityFailed'
        assert memory.execute('PRAGMA foreign_key_check').fetchone() is None, 'ForeignKeyFailed'
        assert not deadline_check(), 'DatabaseCheckDeadline'
    finally:
        memory.close()
        source.close()
    after = path.stat()
    assert [after.st_dev, after.st_ino] == expected_identity, 'DatabaseReplacedDuringCheck'
    return {'integrity': 'PASS', 'foreignKeys': 'PASS', 'method': 'active-online-memory-snapshot'}


def main():
    global PHASE
    assert not sys.flags.optimize and len(sys.argv) == 3
    manifest = Path(sys.argv[1]); raw = manifest.read_bytes()
    assert hashlib.sha256(raw).hexdigest() == sys.argv[2], 'PlanHashMismatch'
    plan = json.loads(raw)
    assert set(plan['databases']) == set(TARGETS), 'UnexpectedScope'
    results = {}
    for role, expected in plan['databases'].items():
        PHASE = 'identify-' + role
        item = inspect(expected['container'])
        assert item['Config']['Labels'].get('com.docker.compose.service') == role, 'UnexpectedScope'
        assert fingerprint(item) == expected['fingerprint'] and item['State']['Running']
        assert item['State']['StartedAt'] == expected['startedAt']
        assert item['State'].get('Health', {}).get('Status') == 'healthy'
        path = active_path(item, role)
        PHASE = 'snapshot-and-check-' + role
        results[role] = check_database(path, expected['fileIdentity'])
    for role, expected in plan['databases'].items():
        PHASE = 'recheck-' + role
        item = inspect(expected['container'])
        assert fingerprint(item) == expected['fingerprint'] and item['State']['Running']
        assert item['State']['StartedAt'] == expected['startedAt']
    print(json.dumps({'status': 'PASS', 'verificationId': plan['verificationId'],
        'checkedAtUtc': datetime.datetime.now(datetime.timezone.utc).isoformat(), 'databases': results}))


if __name__ == '__main__':
    try:
        if sys.flags.optimize:
            raise RuntimeError('OptimizedPythonNotAllowed')
        main()
    except Exception as error:
        safe_codes = {'DatabaseIdentityMismatch', 'DatabaseSizeOutsideBound', 'ReadOnlyMountRequired',
                      'DatabaseBusyNoRetry', 'DatabaseCheckDeadline', 'IntegrityFailed', 'ForeignKeyFailed',
                      'DatabaseReplacedDuringCheck', 'SymlinkOrPathMismatch', 'PlanHashMismatch',
                      'UnexpectedScope', 'OptimizedPythonNotAllowed'}
        print(json.dumps({'status': 'FAIL', 'phase': PHASE, 'category': type(error).__name__,
                          'code': str(error) if str(error) in safe_codes else None,
                          'sqliteCode': getattr(error, 'sqlite_errorname', None)}))
        sys.exit(1)
