#!/usr/bin/env python3
"""Opt-in synthetic Landlock/seccomp proof. No product mounts, downloads or policy changes."""
import argparse
import pathlib
import re
import subprocess
import tempfile
import uuid

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--image', required=True, help='Existing immutable local glibc image')
args = parser.parse_args()
if not re.fullmatch(r'sha256:[0-9a-f]{64}', args.image):
    parser.error('immutable image required')
root = pathlib.Path(__file__).resolve().parents[3]
subprocess.run(['docker', 'image', 'inspect', args.image], check=True,
               stdout=subprocess.DEVNULL, timeout=10)
with tempfile.TemporaryDirectory(prefix='bb158-landlock-', dir=root / 'data') as temp:
    temp = pathlib.Path(temp)
    temp.chmod(0o755)
    binary = temp / 'probe'
    subprocess.run(['cc', '-static', '-O2', '-Wall', '-Wextra', '-Werror',
                    str(pathlib.Path(__file__).with_suffix('.c')), '-o', str(binary)],
                   check=True, timeout=30)
    private = temp / 'private'
    private.mkdir(mode=0o755)
    for name in ('library', 'credentials'):
        path = private / name
        path.write_text('synthetic fixture only\n')
        path.chmod(0o666)
    name = 'bb158-landlock-proof-' + uuid.uuid4().hex
    try:
        result = subprocess.run([
            'docker', 'run', '--rm', '--pull', 'never', '--name', name,
            '--network', 'none', '--read-only', '--user', '1000:1000',
            '--cap-drop', 'ALL', '--security-opt', 'no-new-privileges:true',
            '--memory', '96m', '--memory-swap', '96m', '--pids-limit', '8', '--cpus', '0.5',
            '--tmpfs', '/work:rw,nosuid,nodev,noexec,size=4m,uid=1000,gid=1000,mode=0700',
            '--mount', f'type=bind,src={binary},dst=/probe,readonly',
            '--mount', f'type=bind,src={private},dst=/private',
            '--entrypoint', '/bin/sh', args.image, '-c',
            'echo STAGE:container-started; ln -s /private/library /work/library && exec /probe'
        ], capture_output=True, text=True, timeout=45)
        print(result.stdout, end='')
        print(result.stderr, end='')
        if result.returncode:
            raise SystemExit(result.returncode)
        for path in private.iterdir():
            assert path.read_text() == 'synthetic fixture only\n'
        print('PASS: synthetic private files unchanged; no production inputs; NOT a downloader acceptance test')
    except subprocess.TimeoutExpired as error:
        print('FAIL: bounded probe timeout; container cleanup follows')
        print((error.stdout or b'').decode() if isinstance(error.stdout, bytes) else (error.stdout or ''))
        raise SystemExit(2)
    finally:
        subprocess.run(['docker', 'rm', '-f', name], timeout=10,
                       stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
