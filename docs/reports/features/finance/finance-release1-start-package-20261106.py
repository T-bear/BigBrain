#!/usr/bin/env python3
"""REVIEW ONLY. Owner-local root launcher; no execution before separate approval.

Only the approved verifier is copied. Finance sources are metadata-pinned and
bound read-only by the transient service. This file is NOT the approved verifier.
"""
import hashlib
import json
import os
from pathlib import Path
import re
import selectors
import signal
import stat
import subprocess
import sys
import time
import uuid

APPROVED = '7c065708528a10a0d3f0408e7de5fd4af4b5d93cf43ecae1c5105829de8fb9bb'
DATABASE = 'bb-091-pre-migration-20260817.db'
FILES = (DATABASE,) + tuple(
    'finance-backup-' + identity + suffix
    for identity in ('3b093584173d74f6', '8b549915b4e4129c')
    for suffix in ('.manifest.json', '.data.json'))
SIDECARS = tuple(DATABASE + suffix for suffix in ('-wal', '-shm', '-journal'))
PREFIX = '/run/finance-release1-'
EMPTY_SKELETON = ('/etc', '/var', '/root', '/bin')
ENV = {'PATH': '/usr/sbin:/usr/bin:/sbin:/bin', 'LC_ALL': 'C'}
# Existing Debian runtime, read-only; no /usr, /lib, host /etc or data volume bind.
RUNTIME = (
    ('/usr/bin/python3.13', '/usr/bin/python3.13', False),
    ('/usr/lib/python3.13', '/usr/lib/python3.13', True),
    ('/usr/lib/x86_64-linux-gnu/ld-linux-x86-64.so.2', '/lib64/ld-linux-x86-64.so.2', False),
    ('/usr/lib/x86_64-linux-gnu/libc.so.6', '/lib/x86_64-linux-gnu/libc.so.6', False),
    ('/usr/lib/x86_64-linux-gnu/libm.so.6', '/lib/x86_64-linux-gnu/libm.so.6', False),
    ('/usr/lib/x86_64-linux-gnu/libcrypto.so.3', '/lib/x86_64-linux-gnu/libcrypto.so.3', False),
    ('/usr/lib/x86_64-linux-gnu/libexpat.so.1.10.2', '/lib/x86_64-linux-gnu/libexpat.so.1', False),
    ('/usr/lib/x86_64-linux-gnu/libsqlite3.so.0.8.6', '/lib/x86_64-linux-gnu/libsqlite3.so.0', False),
    ('/usr/lib/x86_64-linux-gnu/libz.so.1.3.1', '/lib/x86_64-linux-gnu/libz.so.1', False),
    ('/usr/lib/x86_64-linux-gnu/libzstd.so.1.5.7', '/lib/x86_64-linux-gnu/libzstd.so.1', False),
    ('/usr/lib/x86_64-linux-gnu/libffi.so.8.1.4', '/lib/x86_64-linux-gnu/libffi.so.8', False),
)

# Inline guard: not staged. It runs unprivileged in the final namespace.
GUARD = r'''
import ctypes, errno, hashlib, io, json, os, stat, sys

STEP = 'G00_spec'

def need(ok):
    if not ok:
        raise RuntimeError('guard')

def check_inaccessible(root='/'):
    global STEP
    # G01/G04 already established UID/GID 1654 and no capabilities.
    # No directory entries or file contents are read, and no write is attempted.
    for name in ('sys', 'run'):
        path = root.rstrip('/') + '/' + name
        prefix = 'G05_mask_' + name
        STEP = prefix + '_type'
        need(stat.S_ISDIR(os.lstat(path).st_mode))
        STEP = prefix + '_access'
        need(not any(os.access(path, mode, effective_ids=True)
                     for mode in (os.R_OK, os.W_OK, os.X_OK)))
        STEP = prefix + '_open'
        try:
            fd = os.open(path, os.O_RDONLY | os.O_DIRECTORY | os.O_NOFOLLOW | os.O_CLOEXEC)
        except PermissionError as error:
            need(error.errno == errno.EACCES)
        else:
            os.close(fd)
            need(False)
        STEP = prefix + '_traverse'
        try:
            os.stat(path + '/.', follow_symlinks=False)
        except PermissionError as error:
            need(error.errno == errno.EACCES)
        else:
            need(False)

def check_skeleton(spec, root='/'):
    global STEP
    STEP = 'G06_spec'
    need(set(spec['empty_skeleton']) == {'/etc', '/var', '/root', '/bin'})
    need(all(isinstance(value, list) and len(value) == 2 for value in spec['empty_skeleton'].values()))
    for rel, identity in spec['empty_skeleton'].items():
        prefix = 'G06_empty_' + rel[1:]
        path = root.rstrip('/') + rel
        STEP = prefix + '_lstat'
        s = os.lstat(path)
        STEP = prefix + '_type'
        need(stat.S_ISDIR(s.st_mode))
        STEP = prefix + '_owner'
        need(s.st_uid == 0)
        STEP = prefix + '_mode'
        need(stat.S_IMODE(s.st_mode) == 0o555)
        STEP = prefix + '_device'
        need(s.st_dev == identity[0])
        STEP = prefix + '_inode'
        need(s.st_ino == identity[1])
        STEP = prefix + '_statvfs'
        flags = os.statvfs(path).f_flag
        STEP = prefix + '_readonly'
        need(flags & os.ST_RDONLY)
        STEP = prefix + '_listdir'
        entries = os.listdir(path)
        STEP = prefix + '_empty'
        need(not entries)

class BoundedOutput(io.StringIO):
    def write(self, text):
        need(self.tell() + len(text) <= 262144)
        return super().write(text)

def main():
    global STEP
    spec = json.loads(sys.argv[1])
    os.environ.clear()
    STEP = 'G01_identity'
    need(os.getresuid() == (1654, 1654, 1654))
    need(os.getresgid() == (1654, 1654, 1654))
    need(set(os.getgroups()) <= {1654})
    STEP = 'G02_dumpable'
    # Shared host UID: forbid peer processes from inspecting this process.
    libc = ctypes.CDLL(None, use_errno=True)
    need(libc.prctl(4, 0, 0, 0, 0) == 0)  # PR_SET_DUMPABLE=0
    STEP = 'G03_namespaces'
    need(os.getpid() == 1 and os.getppid() == 0)
    for kind in ('pid', 'net', 'mnt', 'ipc'):
        need(os.stat('/proc/self/ns/' + kind).st_ino != spec['host_ns'][kind])
    STEP = 'G04_process_protection'
    with open('/proc/self/status', encoding='ascii') as stream:
        status = dict(line.rstrip().split(':', 1) for line in stream if ':' in line)
    for key in ('CapInh', 'CapPrm', 'CapEff', 'CapBnd', 'CapAmb'):
        need(int(status[key].strip(), 16) == 0)
    need(status['NoNewPrivs'].strip() == '1' and status['Seccomp'].strip() == '2')
    need([int(x) for x in status['Uid'].split()] == [1654]*4)
    need([int(x) for x in status['Gid'].split()] == [1654]*4)
    STEP = 'G05_exposure'
    need(set(os.listdir('/finance')) == set(spec['files']))
    need(not os.path.lexists('/finance/bb090-closure-20260816'))
    need(not any(os.path.lexists(p) for p in ('/home', '/opt', '/srv', '/mnt', '/media')))
    check_inaccessible()
    check_skeleton(spec)
    STEP = 'G07_input_metadata'
    for name, expected in spec['files'].items():
        path = '/finance/' + name
        s = os.lstat(path)
        need(stat.S_ISREG(s.st_mode) and not stat.S_ISLNK(s.st_mode))
        need([s.st_dev, s.st_ino, s.st_size, s.st_mtime_ns, s.st_ctime_ns] == expected)
        need(os.statvfs(path).f_flag & os.ST_RDONLY)
        need(os.access(path, os.R_OK) and not os.access(path, os.W_OK))
    STEP = 'G08_verifier_hash'
    s = os.lstat('/verify.py')
    need(stat.S_ISREG(s.st_mode) and s.st_uid == 0 and s.st_gid == 1654)
    need(stat.S_IMODE(s.st_mode) == 0o440 and os.statvfs('/verify.py').f_flag & os.ST_RDONLY)
    with open('/verify.py', 'rb') as stream:
        source = stream.read(131073)
    need(len(source) <= 131072 and hashlib.sha256(source).hexdigest() == spec['hash'])
    # No Finance content has been read. Parent must recheck originals before GO.
    sys.stdout.write('READY identity_hash_isolation\n')
    sys.stdout.flush()
    STEP = 'G09_wait_go'
    need(sys.stdin.readline(16) == 'GO\n')
    STEP = 'G10_verifier'
    saved = sys.stdout
    output = BoundedOutput()
    sys.stdout = output
    try:
        namespace = {'__name__': 'approved_verifier', '__file__': '/verify.py'}
        exec(compile(source, '/verify.py', 'exec'), namespace)
        result = namespace['main'](['--db', '/finance/' + spec['database'], '--manifest-dir', '/finance'])
    finally:
        sys.stdout = saved
    STEP = 'G11_output_contract'
    text = output.getvalue()
    need(len(text.encode('utf-8')) <= 262144 and len(text.splitlines()) <= 80)
    need(all(line.startswith(('PASS ', 'FAIL ', 'NOT CHECKED ', 'SAMMANFATTNING: ')) for line in text.splitlines()))
    need(all(ch == '\n' or ch.isprintable() for ch in text))
    need(result in (0, 1) and text.splitlines()[-1].startswith('SAMMANFATTNING: '))
    if result == 0:
        need('NOT CHECKED closure_directory_unavailable\n' in text)
    sys.stdout.write(text)
    sys.stdout.flush()
    return result

if __name__ == '__main__':
    try:
        rc = main()
    except BaseException:
        sys.stdout = sys.__stdout__
        print('FAIL ' + STEP)
        rc = 1
    raise SystemExit(rc)
'''

GUARD_STEPS = frozenset(('G00_spec', 'G01_identity', 'G02_dumpable', 'G03_namespaces',
    'G04_process_protection', 'G05_exposure', 'G06_spec', 'G07_input_metadata', 'G08_verifier_hash',
    'G09_wait_go', 'G10_verifier', 'G11_output_contract',
    *('G05_mask_' + name + '_' + condition for name in ('sys', 'run')
      for condition in ('type', 'access', 'open', 'traverse')),
    *('G06_empty_' + p[1:] + '_' + condition for p in EMPTY_SKELETON
      for condition in ('lstat', 'type', 'owner', 'mode', 'device', 'inode',
                        'statvfs', 'readonly', 'listdir', 'empty'))))


class GuardFailure(RuntimeError):
    def __init__(self, step):
        self.step = step


class CleanupFailure(RuntimeError):
    def __init__(self, step):
        self.step = step


def need(ok):
    if not ok:
        raise RuntimeError('precondition')


def run_tool(args, timeout=15):
    return subprocess.run(args, stdin=subprocess.DEVNULL, stdout=subprocess.PIPE,
                          stderr=subprocess.DEVNULL, env=ENV, timeout=timeout, check=False)


def properties(unit, keys):
    result = run_tool(['/usr/bin/systemctl', 'show', unit] +
                      ['--property=' + key for key in keys])
    need(result.returncode in (0, 1, 4))
    return dict(line.split('=', 1) for line in result.stdout.decode('ascii').splitlines() if '=' in line)


def check_resource_limits(unit):
    # Read only these four kernel-control files; never enumerate other cgroups.
    group = properties(unit, ['ControlGroup']).get('ControlGroup')
    need(group == '/system.slice/' + unit)
    base = Path('/sys/fs/cgroup' + group)
    for name, value in (('memory.max', '536870912'), ('memory.swap.max', '0'), ('pids.max', '8')):
        with open(base / name, encoding='ascii') as stream:
            need(stream.read(128).strip() == value)
    with open(base / 'cpu.max', encoding='ascii') as stream:
        quota, period = map(int, stream.read(128).split())
    need(0 < quota <= period)


def sig(s):
    return (s.st_dev, s.st_ino, s.st_mode, s.st_uid, s.st_gid,
            s.st_size, s.st_mtime_ns, s.st_ctime_ns)


class Pins:
    def __init__(self):
        self.items = {}

    def pin(self, path, directory=False, trusted=False):
        need(path.startswith('/') and os.path.normpath(path) == path)
        parent = None
        current = ''
        parts = ['/'] + path.strip('/').split('/') if path != '/' else ['/']
        for index, name in enumerate(parts):
            current = '/' if name == '/' else current.rstrip('/') + '/' + name
            if current not in self.items:
                fd = os.open(name, os.O_PATH | os.O_NOFOLLOW | os.O_CLOEXEC, dir_fd=parent)
                s = os.fstat(fd)
                self.items[current] = (fd, parent, name, sig(s))
            fd, _, _, original = self.items[current]
            s = os.fstat(fd)
            need(not stat.S_ISLNK(s.st_mode))
            need(stat.S_ISDIR(s.st_mode) if index < len(parts)-1 or directory else stat.S_ISREG(s.st_mode))
            if trusted:
                need(s.st_uid == 0 and not s.st_mode & 0o022)
            parent = fd
        return self.items[path][0]

    def unchanged(self):
        for fd, parent, name, original in self.items.values():
            s = os.stat(name, dir_fd=parent, follow_symlinks=False)
            need((s.st_dev, s.st_ino, s.st_mode, s.st_uid, s.st_gid) == original[:5])
            if stat.S_ISREG(s.st_mode):
                need(sig(s) == original and sig(os.fstat(fd)) == original)

    def close(self):
        for fd, _, _, _ in reversed(list(self.items.values())):
            os.close(fd)


def no_sidecars(directory_fd):
    for name in SIDECARS:
        try:
            os.stat(name, dir_fd=directory_fd, follow_symlinks=False)
        except FileNotFoundError:
            continue
        raise RuntimeError('sidecar')


def wait_ready(stream):
    deadline = time.monotonic() + 30
    line = b''
    with selectors.DefaultSelector() as selector:
        selector.register(stream, selectors.EVENT_READ)
        while not line.endswith(b'\n'):
            remaining = deadline - time.monotonic()
            need(remaining > 0 and selector.select(timeout=remaining))
            byte = os.read(stream.fileno(), 1)
            need(byte and len(line) < 128)
            line += byte
    failure = line.decode('ascii', errors='replace').removeprefix('FAIL ').rstrip('\n')
    if line.startswith(b'FAIL ') and failure in GUARD_STEPS:
        raise GuardFailure(failure)
    need(line == b'READY identity_hash_isolation\n')


def layout():
    files = {'/verify.py', *(destination for _, destination, is_dir in RUNTIME if not is_dir),
             *('/finance/' + name for name in FILES)}
    dirs = {'/', '/finance', '/proc', '/sys', '/dev', '/run', '/usr/lib/python3.13', *EMPTY_SKELETON}
    for path in files | dirs.copy():
        parent = Path(path).parent
        while str(parent) != '/':
            dirs.add(str(parent))
            parent = parent.parent
    return files, dirs


def stop_and_cleanup(token):
    step = 'C00_token'
    try:
        need(re.fullmatch('[0-9a-f]{32}', token) is not None)
        stage = Path(PREFIX + token)
        unit = 'finance-release1-' + token + '.service'
        step = 'C01_stop'
        # Stop only this exact transient unit. Never touch an existing Finance service.
        run_tool(['/usr/bin/systemctl', 'stop', unit], timeout=20)
        step = 'C02_stopped'
        state = properties(unit, ['LoadState', 'ActiveState', 'MainPID'])
        need(state.get('LoadState') == 'not-found' or
             (state.get('ActiveState') in ('inactive', 'failed') and state.get('MainPID') == '0'))
        step = 'C03_unloaded'
        # KillMode=control-group/SendSIGKILL were fixed at creation.
        if state.get('LoadState') != 'not-found':
            run_tool(['/usr/bin/systemctl', 'reset-failed', unit])
            need(properties(unit, ['LoadState']).get('LoadState') == 'not-found')
    except BaseException:
        raise CleanupFailure(step) from None
    cleanup_tree(stage)


def cleanup_tree(stage):
    step = 'C04_stage'
    try:
        _cleanup_tree(stage)
    except CleanupFailure:
        raise
    except BaseException:
        raise CleanupFailure(step) from None


def _cleanup_tree(stage):
    if not os.path.lexists(stage):
        return
    files, dirs = layout()
    root = stage / 'root'
    for path in (stage, root):
        if path == root and not os.path.lexists(root):
            stage.rmdir()  # Partial setup: only an empty, newly created directory.
            return
        s = path.lstat()
        need(stat.S_ISDIR(s.st_mode) and s.st_uid == 0 and not s.st_mode & 0o022)
    # Preflight the bounded, known skeleton before deleting anything. No recursive walk.
    step = 'C05_inventory'
    try:
        need(set(os.listdir(stage)) == {'root'})
        for rel in sorted(dirs, key=lambda x: x.count('/')):
            p = root / rel.lstrip('/')
            if not os.path.lexists(p):
                continue
            s = p.lstat()
            need(stat.S_ISDIR(s.st_mode) and s.st_uid == 0 and not s.st_mode & 0o022)
            children = {Path(item).name for item in files | (dirs - {'/'})
                        if str(Path(item).parent) == rel}
            need(set(os.listdir(p)) <= children)
        _remove_known_tree(stage, root, files, dirs)
    except CleanupFailure:
        raise
    except BaseException:
        raise CleanupFailure(step) from None


def _remove_known_tree(stage, root, files, dirs):
    # Validate every known target before any unlink. No symlink or source contents allowed.
    for rel in sorted(dirs - {'/'}, key=lambda x: x.count('/')):
        p = root / rel.lstrip('/')
        if os.path.lexists(p):
            s = p.lstat()
            need(stat.S_ISDIR(s.st_mode) and s.st_uid == 0 and not s.st_mode & 0o022)
    for rel in files:
        p = root / rel.lstrip('/')
        if os.path.lexists(p):
            s = p.lstat()
            need(stat.S_ISREG(s.st_mode) and s.st_uid == 0 and s.st_nlink == 1)
            if rel != '/verify.py':
                need(s.st_size == 0)
    step = 'C06_remove_files'
    try:
        for rel in sorted(files):
            p = root / rel.lstrip('/')
            if os.path.lexists(p):
                p.unlink()
        step = 'C07_remove_dirs'
        for rel in sorted(dirs - {'/'}, key=lambda x: x.count('/'), reverse=True):
            p = root / rel.lstrip('/')
            if os.path.lexists(p):
                p.rmdir()
        root.rmdir()
        stage.rmdir()
    except BaseException:
        raise CleanupFailure(step) from None


def start(verifier, backup_dir):
    pins = Pins()
    token = None
    process = None
    rc = 1
    step = 'S01_identity'
    try:
        need(os.getuid() == os.geteuid() == 0)
        pins.pin('/run', directory=True, trusted=True)
        step = 'S02_verifier_hash'
        # No source content is read here except the approved Python script.
        script_fd = pins.pin(verifier)
        with open('/proc/self/fd/' + str(script_fd), 'rb') as stream:
            source = stream.read(131073)
        need(len(source) <= 131072 and hashlib.sha256(source).hexdigest() == APPROVED)
        step = 'S03_source_metadata'
        directory_fd = pins.pin(backup_dir, directory=True)
        directory_sig = sig(os.fstat(directory_fd))
        no_sidecars(directory_fd)
        sources = {name: pins.pin(backup_dir + '/' + name) for name in FILES}
        step = 'S04_runtime_metadata'
        runtime = [(pins.pin(src, directory=directory, trusted=True), dst)
                   for src, dst, directory in RUNTIME]
        step = 'S05_stage'
        candidate = uuid.uuid4().hex
        unit = 'finance-release1-' + candidate + '.service'
        need(properties(unit, ['LoadState']).get('LoadState') == 'not-found')
        stage = Path(PREFIX + candidate)
        stage.mkdir(mode=0o700)  # Exclusive creation; no reused stage.
        token = candidate  # Cleanup ownership starts only after exclusive creation.
        print('RESOURCE_ID ' + token, flush=True)
        root = stage / 'root'
        root.mkdir(mode=0o755)
        targets, dirs = layout()
        for rel in sorted(dirs - {'/'}, key=lambda x: x.count('/')):
            (root / rel.lstrip('/')).mkdir(mode=0o755)
        for rel in sorted(targets):
            p = root / rel.lstrip('/')
            fd = os.open(p, os.O_WRONLY | os.O_CREAT | os.O_EXCL | os.O_NOFOLLOW, 0o400)
            try:
                if rel == '/verify.py':
                    with os.fdopen(os.dup(fd), 'wb') as stream:
                        stream.write(source)
                    os.fchown(fd, 0, 1654)
                    os.fchmod(fd, 0o440)
                else:
                    os.fchmod(fd, 0o444)  # Empty mountpoint, never a data copy.
            finally:
                os.close(fd)
        for rel in dirs:
            os.chmod(root / rel.lstrip('/'), 0o555)
        spec = {'hash': APPROVED, 'database': DATABASE,
                'empty_skeleton': {rel: [os.lstat(root / rel.lstrip('/')).st_dev,
                                         os.lstat(root / rel.lstrip('/')).st_ino]
                                   for rel in EMPTY_SKELETON},
                'host_ns': {name: os.stat('/proc/self/ns/' + name).st_ino for name in ('pid', 'net', 'mnt', 'ipc')},
                'files': {name: [os.fstat(fd).st_dev, os.fstat(fd).st_ino,
                                 os.fstat(fd).st_size, os.fstat(fd).st_mtime_ns,
                                 os.fstat(fd).st_ctime_ns] for name, fd in sources.items()}}
        settings = {
            'RootDirectory': str(root), 'User': '1654', 'Group': '1654',
            'Slice': 'system.slice',
            'SupplementaryGroups': '', 'CapabilityBoundingSet': '', 'AmbientCapabilities': '',
            'NoNewPrivileges': 'yes', 'PrivatePIDs': 'yes', 'PrivateNetwork': 'yes',
            'PrivateDevices': 'yes', 'PrivateIPC': 'yes', 'ProtectSystem': 'strict',
            'ReadOnlyPaths': '+/root',
            'BindLogSockets': 'no',
            'ProcSubset': 'pid', 'ProtectProc': 'invisible', 'RestrictNamespaces': 'yes',
            'RestrictSUIDSGID': 'yes', 'LockPersonality': 'yes', 'MemoryDenyWriteExecute': 'yes',
            'SystemCallArchitectures': 'native', 'SystemCallFilter': '~@network-io @mount @privileged @reboot @swap',
            'InaccessiblePaths': '+/sys +/run', 'UnsetEnvironment': 'LD_PRELOAD LD_LIBRARY_PATH PYTHONPATH PYTHONHOME',
            'Environment': 'LC_ALL=C', 'WorkingDirectory': '/', 'UMask': '0077',
            'MemoryMax': '512M', 'MemorySwapMax': '0', 'CPUQuota': '100%', 'TasksMax': '8',
            'RuntimeMaxSec': '300', 'TimeoutStartSec': '30', 'TimeoutStopSec': '5',
            'KillMode': 'control-group', 'SendSIGKILL': 'yes', 'OOMPolicy': 'kill',
            'LimitCORE': '0', 'LimitNOFILE': '64', 'LimitFSIZE': '0', 'StandardError': 'null',
        }
        bind = [(fd, '/finance/' + name) for name, fd in sources.items()] + runtime
        # Pinned O_PATH descriptors belong to this supervisor, never to the worker.
        settings['BindReadOnlyPaths'] = ' '.join(
            '/proc/' + str(os.getpid()) + '/fd/' + str(fd) + ':' + dst + ':norbind' for fd, dst in bind)
        command = ['/usr/bin/systemd-run', '--quiet', '--wait', '--pipe', '--collect',
                   '--service-type=exec', '--unit=' + unit]
        command += ['--property=' + key + '=' + value for key, value in settings.items()]
        command += ['/usr/bin/python3.13', '-I', '-S', '-B', '-X', 'utf8=1', '-c', GUARD,
                    json.dumps(spec, separators=(',', ':'))]
        step = 'S06_originals_before_start'
        pins.unchanged()
        need(sig(os.fstat(directory_fd)) == directory_sig)
        no_sidecars(directory_fd)  # Original names, immediately before process start.
        step = 'S07_service_start'
        process = subprocess.Popen(command, stdin=subprocess.PIPE, stdout=subprocess.PIPE,
                                   stderr=subprocess.DEVNULL, env=ENV, bufsize=0)
        step = 'S08_wait_ready'
        wait_ready(process.stdout)
        step = 'S09_service_properties'
        expected = {
            'InaccessiblePaths': '+/sys +/run',
            'User': '1654', 'Group': '1654', 'SupplementaryGroups': '',
            'CapabilityBoundingSet': '', 'AmbientCapabilities': '', 'NoNewPrivileges': 'yes',
            'PrivatePIDs': 'yes', 'PrivateNetwork': 'yes', 'RootDirectory': str(root),
            'PrivateIPC': 'yes', 'BindLogSockets': 'no', 'ProtectSystem': 'strict',
            'ReadOnlyPaths': '+/root',
            'MemoryMax': '536870912', 'MemorySwapMax': '0', 'TasksMax': '8',
            'RuntimeMaxUSec': '5min', 'TimeoutStartUSec': '30s', 'TimeoutStopUSec': '5s',
            'CPUQuotaPerSecUSec': '1s', 'SendSIGKILL': 'yes',
            'KillMode': 'control-group', 'LimitCORE': '0', 'LimitNOFILE': '64', 'LimitFSIZE': '0',
        }
        actual = properties(unit, list(expected))
        need(all(actual.get(key) == value for key, value in expected.items()))
        step = 'S10_resource_limits'
        check_resource_limits(unit)
        step = 'S11_originals_before_go'
        pins.unchanged()
        need(sig(os.fstat(directory_fd)) == directory_sig)
        no_sidecars(directory_fd)  # Recheck originals after guard, before first content read.
        print('PASS identity_hash_isolation_and_original_sidecars', flush=True)
        step = 'S12_execution'
        output, _ = process.communicate(input=b'GO\n', timeout=330)
        need(len(output) <= 262144)
        step = 'S13_originals_after_execution'
        pins.unchanged()
        need(sig(os.fstat(directory_fd)) == directory_sig)
        no_sidecars(directory_fd)
        step = 'S14_output_contract'
        text = output.decode('utf-8', errors='strict')
        need(all(ch == '\n' or ch.isprintable() for ch in text))
        need(all(line.startswith(('PASS ', 'FAIL ', 'NOT CHECKED ', 'SAMMANFATTNING: ')) for line in text.splitlines()))
        if process.returncode == 0:
            need('NOT CHECKED closure_directory_unavailable\n' in text)
            need(re.search(r'SAMMANFATTNING: \d+/0/\d+\n$', text) is not None)
        sys.stdout.write(text)
        rc = 0 if process.returncode == 0 else 1
    except GuardFailure as failure:
        print('FAIL ' + failure.step, flush=True)
    except BaseException:
        print('FAIL ' + step, flush=True)
    finally:
        if token is not None:
            try:
                stop_and_cleanup(token)
                print('PASS temporary_resources_removed', flush=True)
            except CleanupFailure as failure:
                print('FAIL ' + failure.step + ' cleanup_unconfirmed RESOURCE_ID=' + token, flush=True)
                rc = 1
            except BaseException:
                print('FAIL C99_cleanup_unconfirmed RESOURCE_ID=' + token, flush=True)
                rc = 1
        if process is not None:
            if process.poll() is None:
                process.terminate()  # Only our systemd-run client; unit stop happened above.
                try:
                    process.wait(timeout=5)
                except subprocess.TimeoutExpired:
                    process.kill()
                    process.wait(timeout=5)
            for pipe in (process.stdin, process.stdout):
                if pipe is not None:
                    pipe.close()
        pins.close()
    return rc


def main(argv):
    need(os.getuid() == os.geteuid() == 0)
    os.umask(0o077)
    def interrupted(signum, frame):
        raise RuntimeError('interrupted')
    for signum in (signal.SIGINT, signal.SIGTERM, signal.SIGHUP):
        signal.signal(signum, interrupted)
    # No root terminal path or secret is printed on failure.
    if len(argv) == 2 and argv[0] == '--cleanup':
        stop_and_cleanup(argv[1])
        print('PASS temporary_resources_removed')
        return 0
    need(len(argv) == 4 and set(argv[::2]) == {'--verifier', '--backup-dir'})
    args = dict(zip(argv[::2], argv[1::2]))
    return start(args['--verifier'], args['--backup-dir'])


if __name__ == '__main__':
    try:
        raise SystemExit(main(sys.argv[1:]))
    except CleanupFailure as failure:
        print('FAIL ' + failure.step + ' cleanup_unconfirmed')
        raise SystemExit(1)
    except Exception:
        print('FAIL A00_arguments_or_cleanup')
        raise SystemExit(1)
