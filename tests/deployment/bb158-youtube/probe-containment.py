#!/usr/bin/env python3
"""Opt-in, harmless Debian/glibc bubblewrap prerequisite probe. No production mounts.

This tests one proposed containment method, not YouTube functionality or a final
Librarr image. Never changes host/production policy or installs software. Explicit diagnostic
variants alter only the disposable container profile; none is deployment-ready.
Exit 2 = containment blocked; exit 0 = namespace creation only, not full safety.
"""
import argparse
import hashlib
import tempfile
import json
import re
import subprocess
import uuid
from pathlib import Path


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--image', required=True, help='Existing local glibc image sha256 ID')
    parser.add_argument('--seccomp-experiment', choices=['builtin', 'pinned', 'clone', 'clone-slave'], default='builtin')
    parser.add_argument('--profile-source', type=Path, help='Pinned upstream diagnostic profile; never installed')
    args = parser.parse_args()
    profile = None
    if args.seccomp_experiment != 'builtin':
        if args.profile_source is None:
            parser.error('--profile-source is required for isolated seccomp comparison')
        raw = args.profile_source.read_bytes()
        if hashlib.sha256(raw).hexdigest() != '6416b47770785a41ac59073cdc77d9fe98517df2799dc83ef207e622de3053f6':
            parser.error('Unexpected upstream diagnostic profile checksum')
        profile = json.loads(raw)
        if args.seccomp_experiment in ('clone', 'clone-slave'):
            profile['syscalls'].append({'names': ['clone'], 'action': 'SCMP_ACT_ALLOW',
                'args': [{'index': 0, 'value': 0x7e020011, 'op': 'SCMP_CMP_EQ'}]})
        if args.seccomp_experiment == 'clone-slave':
            profile['syscalls'].append({'names': ['mount'], 'action': 'SCMP_ACT_ALLOW',
                'args': [{'index': 3, 'value': 0x8c000, 'op': 'SCMP_CMP_EQ'}]})
    if not re.fullmatch(r'sha256:[0-9a-f]{64}', args.image):
        parser.error('An immutable local image ID is required')
    # Fixed executable/library inputs only. Never /config, library, socket or env mounts.
    paths = [
        '/usr/bin/bwrap', '/lib/x86_64-linux-gnu/libselinux.so.1',
        '/lib/x86_64-linux-gnu/libcap.so.2', '/lib/x86_64-linux-gnu/libc.so.6',
        '/lib/x86_64-linux-gnu/libpcre2-8.so.0', '/lib64/ld-linux-x86-64.so.2',
    ]
    if not all(Path(p).is_file() for p in paths):
        raise SystemExit('BLOCKED: required local Debian/glibc probe inputs missing')
    subprocess.run(['docker', 'image', 'inspect', args.image], check=True,
                   stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, timeout=10)
    name = 'bb158-containment-probe-' + uuid.uuid4().hex
    command = [
        'docker', 'run', '--pull', 'never', '--rm', '--name', name,
        '--network', 'none', '--read-only', '--user', '1000:1000',
        '--cap-drop', 'ALL', '--security-opt', 'no-new-privileges:true',
        '--memory', '64m', '--memory-swap', '64m', '--pids-limit', '16', '--cpus', '0.5',
    ]
    for path in paths:
        command += ['--mount', f'type=bind,src={path},dst={path},readonly']
    command += [
        '--entrypoint', '/usr/bin/bwrap', args.image,
        '--unshare-all', '--die-with-parent', '--new-session',
        '--ro-bind', '/usr', '/usr', '--symlink', 'usr/bin', '/bin',
        '--symlink', 'usr/lib', '/lib', '--symlink', 'usr/lib64', '/lib64',
        '--proc', '/proc', '--dev', '/dev', '/usr/bin/true',
    ]
    scratch = None
    if profile is not None:
        scratch = tempfile.TemporaryDirectory(prefix='bb158-seccomp-probe-', dir=Path(__file__).resolve().parents[3] / 'data')
        policy = Path(scratch.name) / 'diagnostic.json'
        policy.write_text(json.dumps(profile))
        command[2:2] = ['--security-opt', 'seccomp=' + str(policy)]
    try:
        result = subprocess.run(command, capture_output=True, text=True, timeout=20)
        namespace_denied = 'No permissions to create new namespace' in result.stderr
        print(json.dumps({'namespaceCreation': 'PASS' if result.returncode == 0 else 'BLOCKED',
                          'exitCode': result.returncode, 'namespaceDenied': namespace_denied,
                          'youtubeRun': False, 'productionMounts': False,
                          'experiment': args.seccomp_experiment,
                          'mountDenied': 'Failed to make / slave' in result.stderr,
                          'permissionDenied': 'Permission denied' in result.stderr}))
        return 0 if result.returncode == 0 else 2
    finally:
        # Only this UUID-owned disposable test container, never a production service.
        subprocess.run(['docker', 'rm', '-f', name], timeout=10,
                       stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
        if scratch is not None:
            scratch.cleanup()


if __name__ == '__main__':
    raise SystemExit(main())
