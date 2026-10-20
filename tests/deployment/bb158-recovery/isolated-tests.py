#!/usr/bin/python3
"""Owner-local systemd fault tests, or agent-local guard logic tests. No production targets."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import signal
import subprocess
import sys
import time
import uuid

from guard import fingerprint, save

IMAGE = 'sha256:2431e564d944b048450ab99c94722cdbd10ca95af5e2e24a120cb0ec1bf33b6a'
ROOT = Path(__file__).resolve().parent
GUARD = ROOT / 'guard.py'
CASES = ('normal', 'disconnect', 'stop-failure', 'kill-after-stop',
         'timeout-after-stop', 'stale-marker', 'replacement-possible')


def run(*args, timeout=45, check=True, env=None):
    return subprocess.run(args, capture_output=True, timeout=timeout, check=check, env=env)


def inspect(cid):
    return json.loads(run('/usr/bin/docker', 'inspect', cid).stdout)[0]


def production_snapshot():
    ids = run('/usr/bin/docker', 'ps', '-q').stdout.decode().split()
    infos = json.loads(run('/usr/bin/docker', 'inspect', *ids).stdout)
    return {i['Id']: (fingerprint(i), i['State']['StartedAt']) for i in infos
            if not (i['Config'].get('Labels') or {}).get('bb158.recovery-test')}


def verify_detached_process(unit, folder, timeout=5):
    """Bounded startup observation after --no-block; completed work is not proof."""
    deadline = time.monotonic() + timeout
    while True:
        remaining = deadline - time.monotonic()
        if remaining <= 0:
            raise TimeoutError('Manager-owned process not observed within startup deadline')
        assert not (folder / 'recovery.json').exists(), 'Work completed before disconnect observation'
        pid = int(run('systemctl', 'show', unit, '-p', 'MainPID', '--value',
                      timeout=remaining).stdout)
        if pid > 1:
            process = Path('/proc') / str(pid)
            # A disappearing process fails closed; never infer parentage from a stale PID.
            assert 'PPid:\t1' in (process / 'status').read_text()
            assert unit in (process / 'cgroup').read_text()
            save(folder / 'disconnect-observation.json',
                 {'unit': unit, 'pid': pid, 'managerParent': True, 'unitCgroup': True})
            return
        time.sleep(0.05)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--systemd', action='store_true')
    parser.add_argument('--local', action='store_true')
    parser.add_argument('--case', choices=CASES, action='append',
                        help='Run only selected cases, in supplied order; defaults to all')
    args = parser.parse_args()
    assert args.systemd != args.local
    cases = args.case or CASES
    assert len(set(cases)) == len(cases), 'Duplicate case selection'
    assert os.getuid() == 1000 and os.getgid() == 1000, 'Run as existing operator, not root'
    assert ROOT.stat().st_mode & 0o077 == 0
    # Validate exact private package before any test; no environment-supplied code/targets.
    expected = json.loads((ROOT / 'package-sha256.json').read_text())
    for name in ('guard.py', 'isolated-tests.py'):
        assert hashlib.sha256((ROOT / name).read_bytes()).hexdigest() == expected[name]
    if args.systemd:
        subprocess.run(['sudo', '-v'], check=True, timeout=120)
    before = production_snapshot()
    batch = uuid.uuid4().hex
    output = ROOT / (('systemd-' if args.systemd else 'local-') + batch)
    output.mkdir(mode=0o700)
    results = []
    save(ROOT / 'latest-test.json', {'directory': output.name, 'mode': 'systemd' if args.systemd else 'local'})
    for case in cases:
        run_id = uuid.uuid4().hex
        folder = output / case
        folder.mkdir(mode=0o700)
        unit = 'bb158-recovery-test-' + run_id + '.service'
        cid = run('/usr/bin/docker', 'create', '--name', 'bb158-recovery-test-' + run_id,
                  '--label', 'bb158.recovery-test=' + run_id, '--network', 'none',
                  '--read-only', '--cap-drop=ALL', '--security-opt=no-new-privileges',
                  '--memory=32m', '--memory-swap=32m', '--pids-limit=16', '--cpus=0.25',
                  '--user=1000:1000', '--entrypoint=/bin/sh', IMAGE, '-c',
                  'trap "exit 0" TERM; for n in $(seq 1 120); do sleep 1 & wait $!; done').stdout.decode().strip()
        try:
            run('/usr/bin/docker', 'start', cid)
            original = inspect(cid)
            assert not original['Mounts'] and original['HostConfig']['NetworkMode'] == 'none'
            manifest = {'purpose': 'bb158-isolated-recovery-test', 'run': run_id,
                        'container': cid, 'fingerprint': fingerprint(original), 'case': case,
                        'unit': unit, 'localLogicOnly': args.local}
            manifest_path = folder / 'manifest.json'
            save(manifest_path, manifest)
            if args.systemd:
                # Authentication, if required, is exclusively the owner's local sudo prompt.
                command = ['sudo', 'systemd-run', '--unit=' + unit, '--collect', '--no-block',
                           '--property=Type=oneshot', '--property=User=1000', '--property=Group=1000',
                           '--property=SupplementaryGroups=docker', '--property=UMask=0077',
                           '--property=NoNewPrivileges=yes', '--property=CapabilityBoundingSet=',
                           '--property=TimeoutStartSec=10s', '--property=TimeoutStopSec=30s',
                           '--property=KillMode=control-group', '--property=Restart=no',
                           '--property=ExecStopPost=/usr/bin/python3 ' + str(GUARD) + ' recover ' + str(manifest_path),
                           '/usr/bin/python3', str(GUARD), 'work', str(manifest_path)]
                # Oneshot startup completes only after ExecStart exits. --no-block is
                # required even without --wait; exit 0 proves enqueueing, not execution.
                result = subprocess.run(command, timeout=120, check=False)
                assert result.returncode == 0, 'Local system-manager authorization/start failed'
                deadline = time.monotonic() + 45
                if case == 'disconnect':
                    verify_detached_process(unit, folder)
                while not (folder / 'recovery.json').exists() and time.monotonic() < deadline:
                    time.sleep(0.1)
                assert (folder / 'recovery.json').exists(), 'Bounded recovery result absent'
                properties = run('systemctl', 'show', unit, '-p', 'Result', '-p', 'ExecMainCode',
                                 '-p', 'ExecMainStatus', check=False).stdout.decode()
                (folder / 'unit-result.txt').write_text(properties)
            else:
                env = dict(os.environ, INVOCATION_ID=uuid.uuid4().hex)
                worker = subprocess.Popen(['/usr/bin/python3', str(GUARD), 'work', str(manifest_path)],
                                          env=env, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL,
                                          start_new_session=True)
                try:
                    worker.wait(timeout=8)
                except subprocess.TimeoutExpired:
                    os.killpg(worker.pid, signal.SIGKILL)
                    worker.wait(timeout=5)
                run('/usr/bin/python3', str(GUARD), 'recover', str(manifest_path), env=env, check=False)
                # A repeat cleanup must not restart a running container or grant new authority.
                started = inspect(cid)['State']['StartedAt']
                run('/usr/bin/python3', str(GUARD), 'recover', str(manifest_path), env=env, check=False)
                assert inspect(cid)['State']['StartedAt'] == started
            result = json.loads((folder / 'recovery.json').read_text())
            final = inspect(cid)
            assert fingerprint(final) == manifest['fingerprint']
            if case in ('stale-marker', 'replacement-possible'):
                assert result['outcome'] == 'BLOCKED_NO_RESTORE' and not final['State']['Running']
            else:
                assert final['State']['Running'] and result['outcome'] != 'BLOCKED_NO_RESTORE'
            results.append({'case': case, 'status': 'PASS', 'recovery': result['outcome']})
            save(output / 'results.json', {'mode': 'systemd' if args.systemd else 'local', 'results': results})
            print(case + ': PASS', flush=True)
        finally:
            # Only this invocation's unmounted scratch container may be removed.
            current = inspect(cid)
            assert current['Config']['Labels'].get('bb158.recovery-test') == run_id and not current['Mounts']
            run('/usr/bin/docker', 'rm', '-f', cid)
    assert production_snapshot() == before, 'Production identity/start-time changed'
    save(output / 'complete.json', {'status': 'PASS', 'mode': 'systemd' if args.systemd else 'local',
                                  'cases': len(results), 'selectedCases': list(cases),
                                  'productionUnchanged': True})
    print(f'PASS: {len(results)} selected isolated cases; production unchanged. ' +
          ('System-manager execution tested.' if args.systemd else 'Local logic only; NOT a systemd PASS.'))


if __name__ == '__main__':
    if sys.flags.optimize:
        raise RuntimeError('OptimizedPythonNotAllowed')
    main()
