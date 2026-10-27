#!/usr/bin/python3
"""Offline assembly only; never installs, approves, launches, fetches or builds anything.
Inputs are private, independently reviewed deployment facts, not defaults or credentials in Git.
"""
import hashlib
import json
import os
from pathlib import Path
import re
import stat
import sys

INPUTS = ('plan.json', 'release.compose.json', 'rollback.compose.json', 'backup-runtime.json')
SUPPORT_SHA = '2e4ffda2c26a3932f0722b145e4083dfd0dd71a343c5da1a03448f239016fdef'
# Existing seven-file interface retains this slot. Verification is performed by the
# installed gateway before submission, never by executing an unapproved helper CLI.
VERIFY = b'"""Verification belongs to the installed approved-operations gateway."""\nif __name__ == "__main__":\n    raise SystemExit("BLOCKED: use separately approved verify-backup operation")\n'


def assemble(source, destination):
    if os.geteuid() == 0:
        raise ValueError('UnprivilegedPreparationOnly')
    os.umask(0o077)
    blobs = {}
    for name in INPUTS:
        path = source / name
        info = path.lstat()
        if not stat.S_ISREG(info.st_mode) or info.st_nlink != 1 or info.st_size > 4 * 1024 * 1024:
            raise ValueError('InvalidInput')
        blobs[name] = path.read_bytes()
    p = json.loads(blobs['plan.json'])
    if p['contract'] != 'school-lunch-v3-systemd-v1' or not re.fullmatch('[0-9a-f]{32}', p['run']) or not re.fullmatch('[0-9a-f]{40}', p['revision']):
        raise ValueError('WrongContractOrIdentity')
    if set(p['targets']) != {'api', 'web'} or set(p['candidates']) != {'api', 'web'}:
        raise ValueError('ServiceScope')
    for name in ('release.compose.json', 'rollback.compose.json'):
        cfg = json.loads(blobs[name])
        if set(cfg['services']) != {'api', 'web'}:
            raise ValueError('ServiceScope')
    parent = Path(__file__).resolve().parent
    blobs['runner.py'] = (parent / 'school_lunch_worker.py').read_bytes()
    blobs['backup.py'] = (parent / 'backup_support.py').read_bytes()
    if hashlib.sha256(blobs['backup.py']).hexdigest() != SUPPORT_SHA:
        raise ValueError('SupportChanged')
    blobs['verify-backup.py'] = VERIFY
    blobs['package.json'] = (json.dumps({n: hashlib.sha256(b).hexdigest() for n, b in blobs.items()}, sort_keys=True, indent=2) + '\n').encode()
    # New exclusive destination, no overwrite or cleanup of partial/previous packages.
    destination.mkdir(mode=0o700)
    for name, data in blobs.items():
        with (destination / name).open('xb') as f:
            f.write(data)
    return hashlib.sha256(blobs['package.json']).hexdigest()


if __name__ == '__main__':
    try:
        if len(sys.argv) != 3:
            raise ValueError('ExpectedPrivateInputDirectoryAndNewOutputDirectory')
        digest = assemble(Path(sys.argv[1]), Path(sys.argv[2]))
        print('ASSEMBLED_NOT_APPROVED packageSha256=' + digest)
    except Exception:
        print('BLOCKED: private package assembly failed; preserve partial files for review', file=sys.stderr)
        sys.exit(1)
