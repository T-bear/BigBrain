#!/usr/bin/python3
"""BB-158 isolated stop/start recovery proof. NEVER admits production containers."""
import hashlib
import json
import os
from pathlib import Path
import re
import signal
import subprocess
import sys
import time
import uuid


def call(*args, timeout=40):
    return subprocess.run(args, check=True, capture_output=True, timeout=timeout).stdout


def digest(value):
    return hashlib.sha256(json.dumps(value, sort_keys=True, separators=(",", ":")).encode()).hexdigest()


def fingerprint(info):
    return digest({k: sorted(info[k], key=lambda x: x['Destination']) if k == 'Mounts' else info[k]
                   for k in ('Id', 'Image', 'Config', 'HostConfig', 'Mounts')})


def save(path, value):
    temporary = path.with_name(path.name + '.' + uuid.uuid4().hex + '.new')
    fd = os.open(temporary, os.O_WRONLY | os.O_CREAT | os.O_EXCL | os.O_NOFOLLOW, 0o600)
    with os.fdopen(fd, 'w') as stream:
        json.dump(value, stream, sort_keys=True)
        stream.flush()
        os.fsync(stream.fileno())
    os.replace(temporary, path)
    fd = os.open(path.parent, os.O_RDONLY | os.O_DIRECTORY)
    try:
        os.fsync(fd)
    finally:
        os.close(fd)


def main():
    action, raw = sys.argv[1:]
    path = Path(raw)
    assert path.is_absolute() and not path.is_symlink()
    assert path.parent.stat().st_uid == os.getuid() and path.parent.stat().st_mode & 0o077 == 0
    manifest = json.loads(path.read_text())
    assert manifest['purpose'] == 'bb158-isolated-recovery-test'
    assert re.fullmatch('[a-f0-9]{64}', manifest['container'])
    assert re.fullmatch('[a-f0-9]{32}', manifest['run'])
    invocation = os.environ.get('INVOCATION_ID', '')
    assert re.fullmatch('[a-f0-9]{32}', invocation)
    if not manifest['localLogicOnly']:
        assert manifest['unit'] in Path('/proc/self/cgroup').read_text()
    state_path = path.parent / 'state.json'
    result_path = path.parent / 'recovery.json'

    def inspect():
        info = json.loads(call('/usr/bin/docker', 'inspect', manifest['container']))[0]
        assert info['Config']['Labels'].get('bb158.recovery-test') == manifest['run']
        assert info['HostConfig']['NetworkMode'] == 'none' and not info['Mounts']
        assert fingerprint(info) == manifest['fingerprint']
        return info

    def record(phase):
        save(state_path, {'run': manifest['run'], 'manifest': digest(manifest),
                          'invocation': invocation, 'phase': phase})

    inspect()
    if action == 'work':
        assert not state_path.exists() and inspect()['State']['Running']
        record('prepared')
        if manifest['case'] == 'stop-failure':
            raise RuntimeError('InjectedStopFailureBeforeRequest')
        record('stopping-old')
        call('/usr/bin/docker', 'stop', '--time', '5', manifest['container'])
        info = inspect()
        assert not info['State']['Running'] and info['State']['ExitCode'] == 0
        record('stopped-clean')
        if manifest['case'] == 'kill-after-stop':
            os.kill(os.getpid(), signal.SIGKILL)
        if manifest['case'] == 'timeout-after-stop':
            time.sleep(60)
        if manifest['case'] == 'stale-marker':
            state = json.loads(state_path.read_text())
            state['invocation'] = '0' * 32
            save(state_path, state)
            raise RuntimeError('InjectedStaleMarker')
        if manifest['case'] == 'replacement-possible':
            record('replacement-possible')
            raise RuntimeError('InjectedUncertainReplacement')
        if manifest['case'] == 'disconnect':
            time.sleep(3)
        # No backup/restore or library operation in this isolated test.
        call('/usr/bin/docker', 'start', manifest['container'])
        assert inspect()['State']['Running']
        record('old-running')
    elif action == 'recover':
        try:
            state = json.loads(state_path.read_text())
            assert state['run'] == manifest['run'] and state['manifest'] == digest(manifest)
            assert state['invocation'] == invocation
            assert state['phase'] in ('prepared', 'stopping-old', 'stopped-clean', 'old-running')
            info = inspect()
            if state['phase'] == 'stopping-old' and info['State']['Running']:
                # A lost Docker client may leave a daemon request in flight. Never assume
                # a running snapshot means the original stop request has finished.
                call('/usr/bin/docker', 'wait', manifest['container'], timeout=12)
                info = inspect()
            if not info['State']['Running']:
                assert state['phase'] in ('stopping-old', 'stopped-clean', 'old-running')
                assert info['State']['ExitCode'] == 0 and not info['State']['OOMKilled']
                call('/usr/bin/docker', 'start', manifest['container'])
                assert inspect()['State']['Running']
                outcome = 'restarted-exact-clean-container'
            else:
                outcome = 'already-running'
            record('old-running')
            save(result_path, {'run': manifest['run'], 'outcome': outcome})
        except Exception:
            save(result_path, {'run': manifest['run'], 'outcome': 'BLOCKED_NO_RESTORE'})
            raise
    else:
        raise ValueError('UnknownOperation')


if __name__ == '__main__':
    if sys.flags.optimize:
        raise RuntimeError('OptimizedPythonNotAllowed')
    try:
        main()
    except Exception as error:
        # Never print Docker responses, paths or manifests.
        print(type(error).__name__, file=sys.stderr)
        sys.exit(1)
