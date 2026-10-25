#!/usr/bin/python3
"""Operator-only retained-backup verification and current runtime binding; no DB writes."""
import hashlib
import json
import os
from pathlib import Path
import signal
import sys

BACKUP = Path('/var/backups/bigbrain-school-lunch/20261025T182326Z-9479fced116f4ab58ba97219372faaa1')
MANIFEST_SHA = '09ccfdd17da297d307f8a76b0af69a2dfafaf16c77fcf56ce1eab167f8aec209'
SCRIPT_SHA = '88ccb8ec063d0bcaad6adabefc2e2e4250bd1f7bf28e7e7c3891143d0807b1da'
PLAN_SHA = '04115e6031b2df6334690bbe15f44282a5f5fa17f053b4995931662154ca2625'


def verify(root, b, retained_only=False):
    b.no_symlinks(BACKUP / 'manifest.json')
    b.require(b.digest(BACKUP / 'manifest.json') == MANIFEST_SHA, 'BackupManifestChanged')
    m = json.loads((BACKUP / 'manifest.json').read_text())
    b.require(m['scriptSha256'] == SCRIPT_SHA, 'BackupMethodChanged')
    if retained_only:
        b.require(b.verify_set(BACKUP) == MANIFEST_SHA, 'BackupVerificationFailed')
        return {'status': 'PASS', 'manifestSha256': MANIFEST_SHA, 'runtimeMatches': False}
    c = b.inspect()
    b.verify_runtime_plan(c, root / 'backup-runtime.json', PLAN_SHA)
    b.require(m['runtime'] == b.runtime_identity(c), 'BackupRuntimeMismatch')
    b.require(b.verify_set(BACKUP) == MANIFEST_SHA, 'BackupVerificationFailed')
    after = b.inspect()
    b.verify_runtime(c, after)
    return {'status': 'PASS', 'manifestSha256': MANIFEST_SHA, 'runtimeMatches': True}


def main():
    if os.geteuid() != 0:
        raise PermissionError('LocalSudoRequired')
    def timeout(signum, frame):
        raise TimeoutError()
    signal.signal(signal.SIGALRM, timeout)
    signal.alarm(170)
    root = Path(__file__).resolve().parent
    script = root / 'backup.py'
    data = script.read_bytes()
    if hashlib.sha256(data).hexdigest() != SCRIPT_SHA:
        raise ValueError('BackupScriptChanged')
    # Execute exactly the verified bytes, not a second path lookup.
    import types
    b = types.ModuleType('backup_checks')
    b.__file__ = str(script)
    exec(compile(data, str(script), 'exec'), b.__dict__)
    b.require(sys.argv[1:] in ([], ['--retained-only']), 'InvalidArguments')
    print(json.dumps(verify(root, b, bool(sys.argv[1:])), sort_keys=True))


if __name__ == '__main__':
    try:
        main()
    except BaseException as error:
        print('BLOCKED: backup verification ' + type(error).__name__, file=sys.stderr)
        sys.exit(1)
