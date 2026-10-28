#!/usr/bin/env python3
"""Offline image regression: real image USER, config permissions and HTTP startup.

Usage: python3 tests/deployment/verify_api_image.py sha256:<image-id>
Only disposable, network-isolated containers with tmpfs data are created. No host
mounts, ports, production configuration, supplier requests or Finance grants.
"""
import json
import re
import subprocess
import sys
import time
import uuid


def docker(*args):
    return subprocess.check_output(['/usr/bin/docker', *args], stderr=subprocess.PIPE,
                                   timeout=30, text=True).strip()


def verify(image):
    if not re.fullmatch(r'sha256:[0-9a-f]{64}', image):
        raise ValueError('Use an immutable local image ID')
    config = json.loads(docker('image', 'inspect', image))[0]
    assert config['Id'] == image and config['Config']['User'] == '1654', 'Unexpected image user'
    isolation = ['--network', 'none', '--read-only', '--cap-drop', 'ALL',
                 '--security-opt', 'no-new-privileges']
    check = '''set -eu
[ "$(id -u)" = 1654 ] && [ "$(id -g)" = 1654 ]
test -f /app/appsettings.json
for f in /app/appsettings*.json /app/*.runtimeconfig.json /app/*.deps.json; do
    [ -f "$f" ] || continue
    [ "$(stat -c '%u:%g:%a' "$f")" = '0:1654:440' ]
    test -r "$f" && ! test -w "$f"
    cat "$f" >/dev/null
done
'''
    docker('run', '--rm', *isolation, '--entrypoint', '/bin/sh', image, '-ec', check)
    print('PASS: actual non-root UID reads root-owned 0440 configuration; no write access')
    name = 'bigbrain-image-regression-' + uuid.uuid4().hex
    cid = None
    try:
        cid = docker('run', '-d', '--name', name, *isolation,
                     '--tmpfs', '/tmp:rw,nosuid,nodev,uid=1654,gid=1654,mode=0700',
                     '--tmpfs', '/app/data:rw,nosuid,nodev,uid=1654,gid=1654,mode=0700',
                     '--tmpfs', '/data:rw,nosuid,nodev,uid=1654,gid=1654,mode=0700',
                     '-e', 'ASPNETCORE_ENVIRONMENT=Production',
                     '-e', 'ASPNETCORE_URLS=http://+:8080',
                     '-e', 'MealPlanner__SchoolMeals__Enabled=false',
                     '-e', 'MealPlanner__SchoolMeals__RosenfeldtEnabled=false',
                     '-e', 'MealPlanner__SchoolMeals__MusikugglanEnabled=false',
                     '-e', 'Finance__RecurringLocalReasoner__Enabled=false',
                     '-e', 'Finance__ResearchScheduler__Enabled=false', image)
        health = "exec 3<>/dev/tcp/127.0.0.1/8080; printf 'GET /health HTTP/1.1\\r\\nHost: localhost\\r\\nConnection: close\\r\\n\\r\\n' >&3; IFS= read -r line <&3; [[ $line == *' 200 '* ]]"
        deadline = time.monotonic() + 60
        while True:
            state = json.loads(docker('inspect', cid))[0]
            assert state['State']['Running'], 'Isolated API exited before health passed'
            try:
                docker('exec', cid, '/bin/bash', '-ec', health)
                break
            except subprocess.CalledProcessError:
                if time.monotonic() >= deadline:
                    raise RuntimeError('Isolated API health deadline exceeded') from None
                time.sleep(1)
        assert state['RestartCount'] == 0
        assert state['HostConfig']['NetworkMode'] == 'none' and not state['HostConfig']['Binds']
        assert not state['HostConfig']['PortBindings']
        assert all(m['Type'] == 'tmpfs' for m in state['Mounts'])
        print('PASS: normal API entrypoint starts and /health returns 200, no restarts')
        print('PASS: no host volumes, published ports or external network; synthetic tmpfs data only')
    finally:
        if cid:
            # Only this newly created, uniquely named test container; never production.
            docker('rm', '-f', cid)


if __name__ == '__main__':
    if len(sys.argv) != 2:
        raise SystemExit('Usage: verify_api_image.py sha256:<image-id>')
    verify(sys.argv[1])
