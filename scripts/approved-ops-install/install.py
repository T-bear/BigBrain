#!/usr/bin/python3.13 -IS
"""One-shot installer. Launch only using the independently pinned command in README.
No root execution from a checkout, no operation approvals, no production worker.
"""
import ast
import base64
import hashlib
import fcntl
import json
import os
from pathlib import Path
import pwd
import stat
import subprocess
import sys

REVISION = 'b23e00d20a24bdeb459949ab335c8b9cd3599bd3'
PINNED = {
    'bigbrain-approved-ops': '1a512f21c1cfa1ca868aabe473874bad2f463341bfd14e1bde4ae8beee124055',
    'backup_support.py': '2e4ffda2c26a3932f0722b145e4083dfd0dd71a343c5da1a03448f239016fdef',
    'sudoers.example': '1158cf3fec2b78abf559f53f20bedde807c317871970c4e2b789ce001423fac1',
}
FILES = {'install.py', *PINNED}
PYTHON = '/usr/bin/python3.13'
WRAPPER = Path('/usr/local/sbin/bigbrain-approved-ops')
LIB = Path('/usr/local/libexec/bigbrain-approved-ops')
CONFIG = Path('/etc/bigbrain-approved-ops')
STATE = Path('/var/lib/bigbrain-approved-ops')
BACKUPS = Path('/var/backups/bigbrain-school-lunch')
SUDOERS = Path('/etc/sudoers.d/bigbrain-approved-ops')
ENV = {'PATH': '/usr/bin:/usr/sbin', 'HOME': '/var/empty', 'LANG': 'C.UTF-8',
       'DOCKER_CONFIG': str(CONFIG / 'docker'), 'DOCKER_HOST': 'unix:///var/run/docker.sock'}
OPERATIONS = ('verify-backup', 'backup-approved', 'start-approved', 'status')
DEPENDENCIES = (PYTHON, '/usr/bin/docker', '/usr/bin/systemd-run',
                '/usr/libexec/docker/cli-plugins/docker-compose', '/usr/bin/getfacl',
                '/usr/sbin/visudo', '/usr/sbin/runuser', '/usr/bin/sudo', '/usr/bin/cvtsudoers')
MAX_BYTES = 1024 * 1024

# Executed by trusted OS Python, not by an installer file in the user's checkout.
# The operator-pinned outer launcher establishes this bootstrap's trust. Input is data only.
BOOTSTRAP = r'''
import base64, hashlib, json, os, pathlib, stat, sys, tempfile
try:
    assert os.geteuid() == 0 and len(sys.argv) == 2
    os.umask(0o077)
    for p in ('/', '/root'):
        s=os.lstat(p)
        assert stat.S_ISDIR(s.st_mode) and s.st_uid == 0 and not s.st_mode & 0o022
    raw=sys.stdin.buffer.read(1048577)
    assert len(raw) <= 1048576
    bundle=json.loads(raw)
    assert set(bundle) == {'manifest', 'files'}
    manifest=base64.b64decode(bundle['manifest'], validate=True)
    assert hashlib.sha256(manifest).hexdigest() == sys.argv[1]
    m=json.loads(manifest)
    names={'install.py','bigbrain-approved-ops','backup_support.py','sudoers.example'}
    assert set(m) == {'schema','sourceRevision','files'} and m['schema'] == 1
    assert m['sourceRevision'] == 'b23e00d20a24bdeb459949ab335c8b9cd3599bd3'
    assert set(m['files']) == names and set(bundle['files']) == names
    blobs={n:base64.b64decode(bundle['files'][n],validate=True) for n in names}
    assert all(hashlib.sha256(b).hexdigest() == m['files'][n] for n,b in blobs.items())
    stage=pathlib.Path(tempfile.mkdtemp(prefix='bigbrain-wrapper-install-',dir='/root'))
    for n,b in {**blobs,'manifest.json':manifest}.items():
        fd=os.open(stage/n,os.O_WRONLY|os.O_CREAT|os.O_EXCL|os.O_NOFOLLOW,0o600)
        with os.fdopen(fd,'wb') as f: f.write(b); f.flush(); os.fsync(f.fileno())
    fd=os.open(stage,os.O_RDONLY|os.O_DIRECTORY); os.fsync(fd); os.close(fd)
    env={'PATH':'/usr/bin:/usr/sbin','HOME':'/var/empty','LANG':'C.UTF-8'}
    os.execve('/usr/bin/python3.13',['/usr/bin/python3.13','-ISB',str(stage/'install.py'),'--root-stage',sys.argv[1]],env)
except BaseException:
    print('BLOCKED: protected staging failed; no installation completed',file=sys.stderr)
    sys.exit(1)
'''


class Blocked(Exception):
    pass


def require(value, code):
    if not value:
        raise Blocked(code)


def digest(data):
    return hashlib.sha256(data).hexdigest()


def verify_bundle(manifest, blobs, expected):
    require(digest(manifest) == expected, 'ManifestHash')
    m = json.loads(manifest)
    require(set(m) == {'schema', 'sourceRevision', 'files'} and m['schema'] == 1
            and m['sourceRevision'] == REVISION, 'SourceRevision')
    require(set(m['files']) == FILES and set(blobs) == FILES, 'PackageScope')
    require(all(digest(b) == m['files'][n] for n, b in blobs.items()), 'PackageHash')
    require(all(digest(blobs[n]) == h for n, h in PINNED.items()), 'AcceptedSourceHash')
    assignments = ast.parse(blobs['bigbrain-approved-ops']).body
    pin = next(ast.literal_eval(n.value) for n in assignments if isinstance(n, ast.Assign)
               and any(isinstance(t, ast.Name) and t.id == 'SUPPORT_SHA256' for t in n.targets))
    require(pin == PINNED['backup_support.py'], 'EmbeddedSupportHash')


def read_file(path):
    fd = os.open(path, os.O_RDONLY | os.O_NOFOLLOW | os.O_NONBLOCK)
    with os.fdopen(fd, 'rb') as f:
        s = os.fstat(f.fileno())
        require(stat.S_ISREG(s.st_mode) and s.st_nlink == 1 and s.st_size <= MAX_BYTES, 'InputFile')
        data = f.read(MAX_BYTES + 1)
        require(len(data) <= MAX_BYTES, 'InputSize')
        return data


def run(args, ok=True, env=None):
    p = subprocess.run(args, stdin=subprocess.DEVNULL, capture_output=True, timeout=30,
                       env=env or ENV, cwd='/')
    if ok:
        require(p.returncode == 0, 'CommandFailed:' + Path(args[0]).name)
    return p


def secure(path, mode=None):
    """Reject symlink chains and any extra/default ACL; no filesystem mutation."""
    path = Path(path)
    for p in [*reversed(path.parents), path]:
        s = p.lstat()
        require(s.st_uid == 0 and s.st_gid == 0 and not s.st_mode & 0o022
                and (stat.S_ISDIR(s.st_mode) or (p == path and stat.S_ISREG(s.st_mode)
                     and s.st_nlink == 1)), 'UnsafePath')
        acl = run(['/usr/bin/getfacl', '-cp', str(p)]).stdout.decode()
        require(all(not l or l.startswith(('user::', 'group::', 'other::'))
                    for l in acl.splitlines()), 'ExtendedACL')
    if mode is not None:
        require(stat.S_IMODE(path.stat().st_mode) == mode, 'FileMode')


def absent(path):
    require(not os.path.lexists(path), 'DestinationExists')


def write_new(path, data, mode):
    secure(path.parent)
    fd = os.open(path, os.O_WRONLY | os.O_CREAT | os.O_EXCL | os.O_NOFOLLOW, 0o600)
    with os.fdopen(fd, 'wb') as f:
        f.write(data)
        os.fchmod(f.fileno(), mode)
        f.flush()
        os.fsync(f.fileno())
    fd = os.open(path.parent, os.O_RDONLY | os.O_DIRECTORY)
    try:
        os.fsync(fd)
    finally:
        os.close(fd)
    secure(path, mode)
    require(digest(read_file(path)) == digest(data), 'InstalledHash')


def policy_snapshot():
    # Never publish policy contents. Refuse unsupported remote/alternate policy sources.
    for line in Path('/etc/nsswitch.conf').read_text().splitlines():
        if line.split('#')[0].strip().startswith('sudoers:'):
            require(line.split('#')[0].split(':', 1)[1].split() == ['files'], 'NonlocalSudoPolicy')
    files = [Path('/etc/sudoers'), *sorted(Path('/etc/sudoers.d').iterdir())]
    result = {}
    for p in files:
        secure(p)
        data = read_file(p)
        if p.name != 'README':
            # Accept only the conventional includedir; other includes need separate review.
            includes = re_includes(data.decode())
            require(all(x in ('@includedir /etc/sudoers.d', '#includedir /etc/sudoers.d')
                        for x in includes), 'AlternatePolicyInclude')
        s = p.stat()
        result[str(p)] = (digest(data), stat.S_IMODE(s.st_mode), s.st_uid, s.st_gid)
    return result


def re_includes(text):
    return [l.strip() for l in text.splitlines()
            if l.strip().startswith(('@include', '#include'))]


def runtime_snapshot():
    ids = run(['/usr/bin/docker', 'ps', '-aq', '--no-trunc']).stdout.decode().split()
    if not ids:
        return {}
    values = json.loads(run(['/usr/bin/docker', 'inspect', *ids]).stdout)
    # Read-only identity/config evidence; never print env, mounts, names or raw inspect.
    return {c['Id']: digest(json.dumps([c['Image'], c['Config'], c['HostConfig'], c['Mounts'],
             c['State']['StartedAt'], c['RestartCount'], c['State']['Running']], sort_keys=True).encode())
            for c in values}


def no_authority():
    require(set(CONFIG.iterdir()) == {CONFIG/'docker', CONFIG/'packages'}, 'UnexpectedAuthority')
    require(not list((CONFIG/'docker').iterdir()) and not list((CONFIG/'packages').iterdir()), 'UnexpectedAuthority')
    require(not list((STATE/'claims').iterdir()) and not list((STATE/'runs').iterdir()), 'UnexpectedOperation')
    require(set(STATE.iterdir()) <= {STATE/'claims', STATE/'runs', STATE/'lock'}, 'UnexpectedOperation')


def wrapper_tests(as_user=False):
    no_authority()
    prefix = ['/usr/sbin/runuser', '-u', 'enigma', '--', '/usr/bin/sudo', '-k', '-n'] if as_user else []
    for op in OPERATIONS:
        p = run([*prefix, str(WRAPPER), op], ok=False)
        if op == 'status':
            require(p.returncode == 0 and json.loads(p.stdout) == {
                'operation': 'status', 'deployment': 'NO_WRAPPER_RUN'}, 'StatusAcceptance')
        else:
            require(p.returncode == 1 and json.loads(p.stderr).get('status') == 'BLOCKED', 'MissingApprovalAcceptance')
    no_authority()


def sudo_denials():
    # -k WITH a command ignores cached credentials; -n never prompts. Do not use sudo -K.
    prefix = ['/usr/sbin/runuser', '-u', 'enigma', '--', '/usr/bin/sudo', '-k', '-n']
    cases = [[str(WRAPPER), 'unknown'], [str(WRAPPER), 'status', 'extra'],
             ['-E', str(WRAPPER), 'status'], ['PYTHONPATH=/tmp', str(WRAPPER), 'status'],
             ['/usr/bin/true']]
    for args in cases:
        p = run(prefix + args, ok=False)
        require(p.returncode != 0 and b'"status": "BLOCKED"' not in p.stderr
                and b'NO_WRAPPER_RUN' not in p.stdout, 'UnexpectedPasswordlessAuthority')


def require_root_stage(stage):
    require(os.geteuid() == 0 and stage.parent == Path('/root')
            and stage.name.startswith('bigbrain-wrapper-install-'), 'ProtectedStageRequired')


def install_sudoers(data):
    # sudo ignores names containing a dot. Publish the complete checked rule atomically,
    # without replacing an existing destination, then remove only our temporary link.
    pending = SUDOERS.parent / '.bigbrain-approved-ops.installing'
    write_new(pending, data, 0o440)
    run(['/usr/sbin/visudo', '-c', '-f', str(pending)])
    os.link(pending, SUDOERS, follow_symlinks=False)
    pending.unlink()
    fd = os.open(SUDOERS.parent, os.O_RDONLY | os.O_DIRECTORY)
    try:
        os.fsync(fd)
    finally:
        os.close(fd)
    secure(SUDOERS, 0o440)
    require(digest(read_file(SUDOERS)) == PINNED['sudoers.example'], 'SudoersInstalledHash')


def install(stage, manifest_hash):
    report = {'status': 'BLOCKED', 'phase': 'preflight', 'sourceRevision': REVISION,
              'systemdAcceptance': 'NOT RUN', 'productionOperations': 'NOT RUN'}
    lock_fd = None
    try:
        require_root_stage(stage)
        secure(stage, 0o700)
        require(set(p.name for p in stage.iterdir()) == FILES | {'manifest.json'}, 'StageScope')
        for p in stage.iterdir(): secure(p, 0o600)
        blobs = {n: read_file(stage/n) for n in FILES}
        verify_bundle(read_file(stage/'manifest.json'), blobs, manifest_hash)
        require(sys.version_info[:2] == (3, 13) and sys.flags.isolated and sys.flags.no_site, 'PythonIsolation')
        for p in DEPENDENCIES: secure(p)
        secure(WRAPPER.parent)
        lock_fd = os.open(WRAPPER.parent, os.O_RDONLY | os.O_DIRECTORY | os.O_NOFOLLOW)
        fcntl.flock(lock_fd, fcntl.LOCK_EX | fcntl.LOCK_NB)
        # Verify actual loaded stdlib/extensions and their protected canonical OS locations.
        import sqlite3, selectors, shutil, datetime, uuid, ssl
        for mod in list(sys.modules.values()):
            p = getattr(mod, '__file__', None)
            if p and p.startswith('/usr/'):
                secure(Path(p))
        for line in Path('/proc/self/maps').read_text().splitlines():
            p = line.split()[-1]
            if p.startswith('/') and '.so' in p: secure(Path(p).resolve(strict=True))
        require(pwd.getpwnam('enigma').pw_uid == 1000, 'UserIdentity')
        for p in (WRAPPER, LIB, CONFIG, STATE, SUDOERS):
            secure(p.parent)
            absent(p)
        absent(SUDOERS.parent / '.bigbrain-approved-ops.installing')
        secure(BACKUPS, 0o700)  # Preserve existing backups; no enumeration or creation.
        run(['/usr/sbin/visudo', '-c'])
        policy = policy_snapshot()
        runtime = runtime_snapshot()
        run(['/usr/bin/docker', 'compose', 'version'])
        # Before installation, no passwordless generic sudo should already mask the tests.
        p = run(['/usr/sbin/runuser', '-u', 'enigma', '--', '/usr/bin/sudo', '-k', '-n', '/usr/bin/true'], ok=False)
        require(p.returncode != 0, 'ExistingBroadPasswordlessSudo')
        report['phase'] = 'A-installing'
        write_new(stage/'progress.json', json.dumps(report).encode(), 0o600)
        for p in (LIB, CONFIG, CONFIG/'packages', CONFIG/'docker', STATE, STATE/'claims', STATE/'runs'):
            secure(p.parent)
            p.mkdir(mode=0o700)
            secure(p, 0o700)
        write_new(LIB/'backup_support.py', blobs['backup_support.py'], 0o644)
        write_new(WRAPPER, blobs['bigbrain-approved-ops'], 0o755)
        wrapper_tests()
        for args in (['unknown'], ['status', 'extra']):
            p = run([str(WRAPPER), *args], ok=False)
            require(p.returncode == 1 and json.loads(p.stderr).get('status') == 'BLOCKED', 'InvocationAcceptance')
        require(policy_snapshot() == policy and runtime_snapshot() == runtime, 'PhaseADrift')
        report['phase'] = 'B-activating'
        write_new(stage/'phase-a-pass.json', json.dumps({'phaseA': 'PASS'}).encode(), 0o600)
        run(['/usr/sbin/visudo', '-c', '-f', str(stage/'sudoers.example')])
        require(digest(blobs['sudoers.example']) == PINNED['sudoers.example'], 'SudoersHash')
        install_sudoers(blobs['sudoers.example'])
        run(['/usr/sbin/visudo', '-c'])
        run(['/usr/bin/sudo', '-l', '-U', 'enigma'])  # Captured, not published; not sufficient alone.
        expected = dict(policy)
        expected[str(SUDOERS)] = (PINNED['sudoers.example'], 0o440, 0, 0)
        require(policy_snapshot() == expected, 'SudoPolicyDrift')
        wrapper_tests(as_user=True)
        sudo_denials()
        require(runtime_snapshot() == runtime, 'RuntimeDrift')
        no_authority()
        report.update(status='PASS', phase='A+B-complete', phaseA='PASS', phaseB='PASS',
                      runtimeIdentity='UNCHANGED', sudoersDelta='EXACT_FOUR_OPERATIONS',
                      databaseVerification='NOT RUN; no database access')
    except BaseException as error:
        report['reason'] = str(error) if isinstance(error, Blocked) else type(error).__name__
    finally:
        if lock_fd is not None:
            os.close(lock_fd)
    # Retain partial install; no cleanup, retries, rollback or operation approval creation.
    if os.geteuid() == 0 and stage.parent == Path('/root'):
        write_new(stage/'result.json', json.dumps(report, indent=2).encode(), 0o600)
    print(json.dumps(report, sort_keys=True))
    print('Retained staging/result:', stage)
    return 0 if report['status'] == 'PASS' else 1


def launch(directory, manifest_hash):
    require(os.geteuid() != 0 and len(manifest_hash) == 64, 'LaunchAsUnprivilegedUser')
    directory = Path(directory)
    blobs = {n: read_file(directory/n) for n in FILES}
    manifest = read_file(directory/'manifest.json')
    verify_bundle(manifest, blobs, manifest_hash)
    for p in (WRAPPER, LIB, CONFIG, STATE, SUDOERS): absent(p)
    # Only data is carried from the checkout. No git, shell script or imports run as root there.
    bundle = json.dumps({'manifest': base64.b64encode(manifest).decode(),
                         'files': {n: base64.b64encode(b).decode() for n,b in blobs.items()}}).encode()
    require(len(bundle) <= MAX_BYTES, 'BundleSize')
    with open('/dev/tty', 'rb'):
        pass  # Require a local controlling terminal for sudo's own password prompt.
    p = subprocess.run(['/usr/bin/sudo', '-k', '--', PYTHON, '-ISB', '-c', BOOTSTRAP, manifest_hash],
                       input=bundle, env={'PATH': '/usr/bin:/usr/sbin', 'LANG': 'C.UTF-8'}, cwd='/')
    return p.returncode


if __name__ == '__main__':
    try:
        require(len(sys.argv) == 3 and sys.argv[1] == '--root-stage', 'UsePinnedOperatorCommand')
        sys.exit(install(Path(__file__).parent, sys.argv[2]))
    except Exception:
        print('BLOCKED: installation entrypoint/preflight failed', file=sys.stderr)
        sys.exit(1)
