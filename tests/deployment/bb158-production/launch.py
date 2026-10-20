#!/usr/bin/python3
"""Owner-local launch of the approved BB-158 deployment; password stays in sudo."""
import hashlib
import json
import os
from pathlib import Path
import subprocess
import sys

if sys.flags.optimize or len(sys.argv) != 1:
    raise RuntimeError('UseUnoptimizedPythonWithoutArguments')

root = Path(__file__).resolve().parent
assert os.getuid() == 1000 and root.stat().st_mode & 0o077 == 0
expected = json.loads((root / 'package.json').read_text())
for name, digest in expected.items():
    assert hashlib.sha256((root / name).read_bytes()).hexdigest() == digest
assert not (root / 'state.json').exists(), 'Prior attempt exists; inspect, never retry blindly'
plan = json.loads((root / 'plan.json').read_text())
subprocess.run(['sudo', '-v'], check=True, timeout=120)
unit = plan['unit']
subprocess.run(['sudo', 'systemd-run', '--no-block', '--collect', '--unit=' + unit,
               '-p', 'Type=oneshot', '-p', 'User=1000', '-p', 'Group=1000',
               '-p', 'SupplementaryGroups=docker', '-p', 'UMask=0077',
               '-p', 'NoNewPrivileges=yes', '-p', 'CapabilityBoundingSet=',
               '-p', 'TimeoutStartSec=900s', '-p', 'TimeoutStopSec=90s',
               '-p', 'KillMode=control-group', '-p', 'Restart=no',
               '-p', 'ExecStopPost=/usr/bin/python3 ' + str(root / 'execute.py') + ' recover',
               '/usr/bin/python3', str(root / 'execute.py'), 'work'], check=True, timeout=120)
print('QUEUED: ' + unit)
print('This deploys Librarr, API and Web. Import stays disabled; no automatic data restore.')
print('Read complete.json/recovery.json in this private directory; queued is not success.')
