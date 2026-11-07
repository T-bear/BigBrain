#!/usr/bin/env python3
"""REVIEW ONLY: two root-local systemd probes, synthetic files only.

Not executed by Codex. Requires a NEW exact-artifact owner approval.
No Finance input argument exists. The approved Finance verifier is never run.
"""
import contextlib
import hashlib
import io
import os
from pathlib import Path
import re
import signal
import stat
import sys
from types import SimpleNamespace
import uuid

PACKAGE_HASH = '277201476976f10dad22ac483ce430aa289dbce6184530910091545d34f35029'
PREFIX = '/run/finance-release1-probe-'


def need(ok):
    if not ok:
        raise RuntimeError('probe')


def child_id(token, case):
    return hashlib.sha256((token + ':' + case).encode('ascii')).hexdigest()[:32]


def probe_source(case, files):
    need(case in ('normal', 'failure'))
    return (
        'from pathlib import Path\n'
        'def main(argv):\n'
        '    assert len(argv) == 4\n'
        '    root = Path(argv[3])\n'
        '    for name in ' + repr(files) + ':\n'
        '        assert (root / name).read_bytes() == b"synthetic-only\\n"\n'
        '    print("' + ('PASS' if case == 'normal' else 'FAIL') + ' synthetic_probe")\n'
        '    print("NOT CHECKED closure_directory_unavailable")\n'
        '    print("SAMMANFATTNING: ' + ('1/0/1' if case == 'normal' else '0/1/1') + '")\n'
        '    return ' + ('0' if case == 'normal' else '1') + '\n'
    ).encode('ascii')


def cleanup_fixture(package, token):
    need(re.fullmatch('[0-9a-f]{32}', token) is not None)
    # No retry of a failed child cleanup. Owner must use package --cleanup first.
    for case in ('normal', 'failure'):
        child = child_id(token, case)
        need(package.properties('finance-release1-' + child + '.service',
                                ['LoadState']).get('LoadState') == 'not-found')
        need(not os.path.lexists(package.PREFIX + child))
    root = Path(PREFIX + token)
    if not os.path.lexists(root):
        return
    inputs = root / 'inputs'
    for p in (root, inputs):
        if os.path.lexists(p):
            s = p.lstat()
            need(stat.S_ISDIR(s.st_mode) and s.st_uid == 0 and not s.st_mode & 0o022)
    need(set(os.listdir(root)) <= {'inputs', 'probe.py'})
    if inputs.exists():
        need(set(os.listdir(inputs)) <= set(package.FILES))
    paths = [root / 'probe.py', *(inputs / name for name in package.FILES)]
    for p in paths:
        if os.path.lexists(p):
            s = p.lstat()
            need(stat.S_ISREG(s.st_mode) and s.st_uid == 0 and s.st_nlink == 1)
    for p in paths:
        if os.path.lexists(p):
            p.unlink()
    if inputs.exists():
        inputs.rmdir()
    root.rmdir()


def main(argv):
    need(os.getuid() == os.geteuid() == 0)
    os.umask(0o077)
    def interrupted(signum, frame):
        raise RuntimeError('interrupted')
    for signum in (signal.SIGINT, signal.SIGTERM, signal.SIGHUP):
        signal.signal(signum, interrupted)
    need(len(argv) in (2, 4) and argv[0] == '--package')
    if len(argv) == 4:
        need(argv[2] == '--cleanup-fixture')
    with open(argv[1], 'rb') as stream:
        source = stream.read(131073)
    need(len(source) <= 131072 and hashlib.sha256(source).hexdigest() == PACKAGE_HASH)
    namespace = {'__name__': 'reviewed_package'}
    exec(compile(source, '<reviewed-package>', 'exec'), namespace)
    package = SimpleNamespace(**namespace)
    if len(argv) == 4:
        cleanup_fixture(package, argv[3])
        print('PASS synthetic_fixture_removed')
        return 0
    token = uuid.uuid4().hex
    root = Path(PREFIX + token)
    root.mkdir(mode=0o700)  # No reused source, directory or existing copy.
    print('PROBE_RESOURCE_ID ' + token, flush=True)
    for case in ('normal', 'failure'):
        print('PROBE_CHILD_' + case.upper() + ' ' + child_id(token, case), flush=True)
    rc = 1
    step = 'P01_fixture'
    try:
        inputs = root / 'inputs'
        inputs.mkdir(mode=0o700)
        for name in package.FILES:
            with open(inputs / name, 'xb') as stream:
                stream.write(b'synthetic-only\n')
            os.chmod(inputs / name, 0o444)
        script = root / 'probe.py'
        for case in ('normal', 'failure'):
            step = 'P02_' + case
            # This probe replaces only the in-memory expected hash and test payload.
            # The package file and the approved Finance verifier remain unchanged.
            probe = probe_source(case, package.FILES)
            script.write_bytes(probe)
            os.chmod(script, 0o400)
            namespace['APPROVED'] = hashlib.sha256(probe).hexdigest()
            child = child_id(token, case)
            namespace['uuid'] = SimpleNamespace(uuid4=lambda: SimpleNamespace(hex=child))
            output = io.StringIO()
            with contextlib.redirect_stdout(output):
                result = package.start(str(script), str(inputs))
            text = output.getvalue()
            print(text, end='', flush=True)
            expected = 0 if case == 'normal' else 1
            need(result == expected and 'PASS temporary_resources_removed\n' in text)
            need(('PASS' if case == 'normal' else 'FAIL') + ' synthetic_probe\n' in text)
            print('PASS P03_' + case + '_and_cleanup', flush=True)
        rc = 0
    except BaseException:
        print('FAIL ' + step, flush=True)
    finally:
        try:
            cleanup_fixture(package, token)
            print('PASS synthetic_fixture_removed', flush=True)
        except BaseException:
            print('FAIL P04_fixture_cleanup_unconfirmed PROBE_RESOURCE_ID=' + token, flush=True)
            rc = 1
    return rc


if __name__ == '__main__':
    try:
        raise SystemExit(main(sys.argv[1:]))
    except BaseException as error:
        if isinstance(error, SystemExit):
            raise
        print('FAIL P00_arguments_or_fixture_cleanup')
        raise SystemExit(1)
