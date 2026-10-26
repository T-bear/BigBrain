#!/usr/bin/python3.13 -IS
"""One-attempt PhaseADrift recovery. Prepare only; operator uses pinned launcher."""
import base64
import hashlib
import json
import os
from pathlib import Path
import sys
import types

SOURCE = 'b23e00d20a24bdeb459949ab335c8b9cd3599bd3'
ORIGINAL_COMMIT = 'be47b4732711653c72a723f3bcb054deb3825735'
ORIGINAL_STAGE = Path('/root/bigbrain-wrapper-install-djodxe_9')
ORIGINAL_MANIFEST = '1dcc25d5bd07279760d5c6f621d596c5090b159a1bcf08155ca691863c8412fa'
HELPER_HASH = '07797634a5b1b4a380c070929720490a699db0092fb9f47dde838a850af8b12b'
NAMES = {'recover.py', 'install.py'}
MAX_BYTES = 1024 * 1024

# Verified by the operator-pinned launcher before sudo; input is data, not paths/code arguments.
BOOTSTRAP = r'''
import base64, hashlib, json, os, pathlib, stat, sys, tempfile
try:
    assert os.geteuid() == 0 and len(sys.argv) == 2
    os.umask(0o077)
    for p in ('/', '/root'):
        s=os.lstat(p)
        assert stat.S_ISDIR(s.st_mode) and s.st_uid == s.st_gid == 0 and not s.st_mode & 0o022
    raw=sys.stdin.buffer.read(1048577)
    assert len(raw) <= 1048576
    b=json.loads(raw)
    assert set(b) == {'manifest','files'}
    manifest=base64.b64decode(b['manifest'],validate=True)
    assert hashlib.sha256(manifest).hexdigest() == sys.argv[1]
    m=json.loads(manifest)
    assert set(m) == {'schema','sourceRevision','originalCommit','files'} and m['schema'] == 1
    assert m['sourceRevision'] == 'b23e00d20a24bdeb459949ab335c8b9cd3599bd3'
    assert m['originalCommit'] == 'be47b4732711653c72a723f3bcb054deb3825735'
    names={'recover.py','install.py'}
    assert set(m['files']) == set(b['files']) == names
    blobs={n:base64.b64decode(b['files'][n],validate=True) for n in names}
    assert all(hashlib.sha256(v).hexdigest() == m['files'][n] for n,v in blobs.items())
    assert m['files']['install.py'] == '07797634a5b1b4a380c070929720490a699db0092fb9f47dde838a850af8b12b'
    stage=pathlib.Path(tempfile.mkdtemp(prefix='bigbrain-wrapper-recover-',dir='/root'))
    for n,v in {**blobs,'recovery-manifest.json':manifest}.items():
        fd=os.open(stage/n,os.O_WRONLY|os.O_CREAT|os.O_EXCL|os.O_NOFOLLOW,0o600)
        with os.fdopen(fd,'wb') as f: f.write(v); f.flush(); os.fsync(f.fileno())
    fd=os.open(stage,os.O_RDONLY|os.O_DIRECTORY); os.fsync(fd); os.close(fd)
    os.execve('/usr/bin/python3.13',['/usr/bin/python3.13','-ISB',str(stage/'recover.py'),'--root-stage',sys.argv[1]],
              {'PATH':'/usr/bin:/usr/sbin','HOME':'/var/empty','LANG':'C.UTF-8'})
except BaseException:
    print('BLOCKED: protected recovery staging failed',file=sys.stderr)
    sys.exit(1)
'''


def load_helper(directory):
    # Read no-follow and bounded before executing even this pinned helper.
    import stat
    fd = os.open(Path(directory)/'install.py', os.O_RDONLY | os.O_NOFOLLOW | os.O_NONBLOCK)
    with os.fdopen(fd, 'rb') as f:
        s = os.fstat(f.fileno())
        if not stat.S_ISREG(s.st_mode) or s.st_nlink != 1 or s.st_size > MAX_BYTES:
            raise ValueError('HelperFile')
        data = f.read(MAX_BYTES + 1)
    if hashlib.sha256(data).hexdigest() != HELPER_HASH:
        raise ValueError('HelperHash')
    helper = types.ModuleType('verified_installer')
    exec(compile(data, 'verified_installer', 'exec'), helper.__dict__)
    return helper


def verify_bundle(h, manifest, blobs, expected):
    h.require(h.digest(manifest) == expected, 'RecoveryManifestHash')
    m = json.loads(manifest)
    h.require(set(m) == {'schema', 'sourceRevision', 'originalCommit', 'files'}
              and m['schema'] == 1 and m['sourceRevision'] == SOURCE
              and m['originalCommit'] == ORIGINAL_COMMIT, 'RecoveryIdentity')
    h.require(set(m['files']) == set(blobs) == NAMES, 'RecoveryScope')
    h.require(all(h.digest(b) == m['files'][n] for n, b in blobs.items())
              and m['files']['install.py'] == HELPER_HASH, 'RecoveryHash')


def receipt(h, path):
    def unique(pairs):
        result = {}
        for key, value in pairs:
            h.require(key not in result, 'DuplicateReceiptField')
            result[key] = value
        return result
    return json.loads(h.read_file(path), object_pairs_hook=unique)


def historical_evidence(h):
    h.secure(ORIGINAL_STAGE, 0o700)
    h.require({p.name for p in ORIGINAL_STAGE.iterdir()} ==
              h.FILES | {'manifest.json', 'progress.json', 'result.json'}, 'HistoricalScopeOrAttemptExists')
    for p in ORIGINAL_STAGE.iterdir(): h.secure(p, 0o600)
    blobs = {n: h.read_file(ORIGINAL_STAGE/n) for n in h.FILES}
    h.verify_bundle(h.read_file(ORIGINAL_STAGE/'manifest.json'), blobs, ORIGINAL_MANIFEST)
    progress = {'status': 'BLOCKED', 'phase': 'A-installing', 'sourceRevision': SOURCE,
                'systemdAcceptance': 'NOT RUN', 'productionOperations': 'NOT RUN'}
    h.require(receipt(h, ORIGINAL_STAGE/'progress.json') == progress, 'HistoricalProgress')
    h.require(receipt(h, ORIGINAL_STAGE/'result.json') ==
              {**progress, 'reason': 'PhaseADrift'}, 'HistoricalResult')
    return blobs['sudoers.example']  # Hash verified; old installer is never executed.


def installed_state(h):
    for p in (h.LIB, h.CONFIG, h.CONFIG/'packages', h.CONFIG/'docker',
              h.STATE, h.STATE/'claims', h.STATE/'runs'):
        h.secure(p, 0o700)
        h.require(p.is_dir(), 'DirectoryType')
    h.require(set(h.LIB.iterdir()) == {h.LIB/'backup_support.py'}, 'LibraryScope')
    for p, name, mode in ((h.WRAPPER, 'bigbrain-approved-ops', 0o755),
                          (h.LIB/'backup_support.py', 'backup_support.py', 0o644)):
        h.secure(p, mode)
        h.require(h.digest(h.read_file(p)) == h.PINNED[name], 'InstalledHash')
    h.no_authority()
    lock = h.STATE/'lock'
    h.secure(lock, 0o600)
    h.require(h.read_file(lock) == b'', 'UnexpectedLockContent')
    fd = os.open(lock, os.O_RDONLY | os.O_NOFOLLOW | os.O_NONBLOCK)
    try: h.fcntl.flock(fd, h.fcntl.LOCK_EX | h.fcntl.LOCK_NB)
    finally: os.close(fd)


def policy_acceptance(h, doc):
    """Conservative current-state acceptance, not reconstruction of historical policy.

    Only standard password-authorized root/sudo/enigma ALL rules and four benign
    global Defaults are accepted. Unknown local rules/defaults require manual review.
    """
    h.require(set(doc) <= {'Defaults', 'User_Specs'}, 'PolicyStructure')
    defaults = doc.get('Defaults', [])
    h.require(isinstance(defaults, list), 'PolicyDefaults')
    for entry in defaults:
        h.require(set(entry) == {'Options'}, 'PolicyDefaultsScope')
        for option in entry['Options']:
            h.require(len(option) == 1, 'PolicyDefaults')
            key, value = next(iter(option.items()))
            if key == 'secure_path':
                h.require(isinstance(value, str) and value, 'PolicyPath')
                for p in value.split(':'):
                    h.require(p.startswith('/') and '..' not in Path(p).parts, 'PolicyPath')
                    # Debian's /bin and /sbin may be root-owned aliases to /usr.
                    path = Path(p)
                    h.secure(path.parent)
                    s = path.lstat()
                    h.require(s.st_uid == s.st_gid == 0 and
                              (path.is_symlink() or not s.st_mode & 0o022), 'PolicyPath')
                    h.secure(path.resolve(strict=True))
            else:
                h.require(key in ('env_reset', 'mail_badpass', 'use_pty') and value is True,
                          'PolicyDefaults')
    specs = doc.get('User_Specs', [])
    h.require(bool(specs), 'PolicyMissingRules')
    enigma_access = False
    for entry in specs:
        h.require(set(entry) == {'User_List', 'Host_List', 'Cmnd_Specs'}, 'PolicyRuleScope')
        users = entry['User_List']
        h.require(users in ([{'username': 'root'}], [{'username': 'enigma'}], [{'usergroup': 'sudo'}])
                  and entry['Host_List'] == [{'hostname': 'ALL'}], 'PolicyUsers')
        enigma_access |= users in ([{'username': 'enigma'}], [{'usergroup': 'sudo'}])
        h.require(len(entry['Cmnd_Specs']) == 1, 'PolicyCommands')
        cmd = entry['Cmnd_Specs'][0]
        h.require(set(cmd) <= {'runasusers', 'runasgroups', 'Options', 'Commands'}
                  and cmd.get('runasusers') == [{'username': 'ALL'}]
                  and cmd.get('runasgroups') == [{'usergroup': 'ALL'}]
                  and cmd.get('Commands') == [{'command': 'ALL'}]
                  and cmd.get('Options', []) in ([], [{'setenv': True}]), 'PolicyCommands')
    h.require(enigma_access, 'PolicyUserAccess')


def environment_checks(h):
    h.require(sys.version_info[:2] == (3, 13) and sys.flags.isolated and sys.flags.no_site, 'PythonIsolation')
    for p in h.DEPENDENCIES: h.secure(p)
    import sqlite3, selectors, shutil, datetime, uuid, ssl
    for mod in list(sys.modules.values()):
        p = getattr(mod, '__file__', None)
        if p and p.startswith('/usr/'): h.secure(Path(p))
    for line in Path('/proc/self/maps').read_text().splitlines():
        p = line.split()[-1]
        if p.startswith('/') and '.so' in p: h.secure(Path(p).resolve(strict=True))
    h.require(h.pwd.getpwnam('enigma').pw_uid == 1000, 'UserIdentity')
    h.secure(h.BACKUPS, 0o700)  # Metadata only; never read backup content.


def require_recovery_stage(h, stage):
    h.require(os.geteuid() == 0 and stage.parent == Path('/root')
              and stage.name.startswith('bigbrain-wrapper-recover-'), 'ProtectedStageRequired')


def recover(h, stage, manifest_hash):
    report = {'status': 'BLOCKED', 'phase': 'recovery-preflight', 'sourceRevision': SOURCE,
              'historicalDrift': 'UNKNOWN', 'systemdAcceptance': 'NOT RUN', 'productionOperations': 'NOT RUN'}
    lock_fd = None
    safe_stage = False
    try:
        require_recovery_stage(h, stage)
        h.secure(stage, 0o700)
        h.require({p.name for p in stage.iterdir()} == NAMES | {'recovery-manifest.json'}, 'RecoveryStageScope')
        for p in stage.iterdir(): h.secure(p, 0o600)
        verify_bundle(h, h.read_file(stage/'recovery-manifest.json'),
                      {n: h.read_file(stage/n) for n in NAMES}, manifest_hash)
        safe_stage = True
        environment_checks(h)
        h.secure(h.WRAPPER.parent)
        lock_fd = os.open(h.WRAPPER.parent, os.O_RDONLY | os.O_DIRECTORY | os.O_NOFOLLOW)
        h.fcntl.flock(lock_fd, h.fcntl.LOCK_EX | h.fcntl.LOCK_NB)
        rule = historical_evidence(h)
        h.absent(h.SUDOERS)
        h.absent(h.SUDOERS.parent/'.bigbrain-approved-ops.installing')
        installed_state(h)
        h.run(['/usr/sbin/visudo', '-c'])
        policy = h.policy_snapshot()
        parsed = json.loads(h.run(['/usr/bin/cvtsudoers', '-c', '/dev/null', '-e', '-f', 'json', '/etc/sudoers']).stdout)
        policy_acceptance(h, parsed)
        runtime = h.runtime_snapshot()
        h.require(bool(runtime), 'RuntimeEmpty')
        h.run(['/usr/bin/docker', 'compose', 'version'])
        # Bind a single attempt to the original evidence; never delete/overwrite this marker.
        h.write_new(ORIGINAL_STAGE/'recovery-attempt.json', json.dumps({
            'recoveryManifest': manifest_hash, 'stage': str(stage)}).encode(), 0o600)
        report['phase'] = 'A-revalidating'
        h.write_new(stage/'progress.json', json.dumps(report).encode(), 0o600)
        h.wrapper_tests()
        for args in (['unknown'], ['status', 'extra']):
            p = h.run([str(h.WRAPPER), *args], ok=False)
            h.require(p.returncode == 1 and json.loads(p.stderr).get('status') == 'BLOCKED', 'InvocationAcceptance')
        # Before activation all four exact commands must be denied by sudo, not executed.
        prefix = ['/usr/sbin/runuser', '-u', 'enigma', '--', '/usr/bin/sudo', '-k', '-n']
        for op in h.OPERATIONS:
            p = h.run(prefix + [str(h.WRAPPER), op], ok=False)
            h.require(p.returncode != 0 and not p.stdout and b'"status"' not in p.stderr,
                      'ExistingPasswordlessWrapper')
        h.sudo_denials()
        installed_state(h)
        report['phaseAChecks'] = {'policyUnchanged': h.policy_snapshot() == policy,
                                 'runtimeUnchanged': h.runtime_snapshot() == runtime}
        h.require(all(report['phaseAChecks'].values()), 'PhaseADrift')
        h.absent(h.SUDOERS)
        h.absent(h.SUDOERS.parent/'.bigbrain-approved-ops.installing')
        h.run(['/usr/sbin/visudo', '-c', '-f', str(ORIGINAL_STAGE/'sudoers.example')])
        report['phase'] = 'B-activating'
        h.write_new(stage/'phase-a-pass.json', json.dumps({
            'phaseA': 'PASS', 'acceptance': 'CURRENT_STATE_ONLY',
            'policyFingerprint': h.digest(json.dumps(policy, sort_keys=True).encode()),
            'runtimeFingerprint': h.digest(json.dumps(runtime, sort_keys=True).encode())}).encode(), 0o600)
        h.install_sudoers(rule)
        h.run(['/usr/sbin/visudo', '-c'])
        h.run(['/usr/bin/sudo', '-l', '-U', 'enigma'])
        expected = {**policy, str(h.SUDOERS): (h.PINNED['sudoers.example'], 0o440, 0, 0)}
        h.require(h.policy_snapshot() == expected, 'SudoPolicyDrift')
        h.wrapper_tests(as_user=True)
        h.sudo_denials()
        installed_state(h)
        h.require(h.runtime_snapshot() == runtime, 'RuntimeDrift')
        report.update(status='PASS', phase='recovery-complete', phaseA='PASS', phaseB='PASS',
                      acceptance='CURRENT_STATE_ONLY', sudoersDelta='EXACT_FOUR_OPERATIONS')
    except BaseException as error:
        report['reason'] = str(error) if isinstance(error, h.Blocked) else type(error).__name__
    finally:
        # Keep the lock through durable reporting. No cleanup or automatic rollback.
        try:
            if safe_stage: h.write_new(stage/'result.json', json.dumps(report, indent=2).encode(), 0o600)
        finally:
            if lock_fd is not None: os.close(lock_fd)
    print(json.dumps(report, sort_keys=True))
    print('Retained recovery evidence:', stage)
    return 0 if report['status'] == 'PASS' else 1


def launch(directory, expected):
    h = load_helper(directory)
    h.require(os.geteuid() != 0 and len(expected) == 64, 'LaunchAsUnprivilegedUser')
    directory = Path(directory)
    manifest = h.read_file(directory/'recovery-manifest.json')
    blobs = {n: h.read_file(directory/n) for n in NAMES}
    verify_bundle(h, manifest, blobs, expected)
    bundle = json.dumps({'manifest': base64.b64encode(manifest).decode(),
                         'files': {n: base64.b64encode(b).decode() for n, b in blobs.items()}}).encode()
    h.require(len(bundle) <= MAX_BYTES, 'BundleSize')
    with open('/dev/tty', 'rb'): pass
    return h.subprocess.run(['/usr/bin/sudo', '-k', '--', h.PYTHON, '-ISB', '-c', BOOTSTRAP, expected],
                            input=bundle, env={'PATH': '/usr/bin:/usr/sbin', 'LANG': 'C.UTF-8'}, cwd='/').returncode


if __name__ == '__main__':
    try:
        if len(sys.argv) != 3 or sys.argv[1] != '--root-stage': raise ValueError('EntryPoint')
        stage = Path(__file__).parent
        sys.exit(recover(load_helper(stage), stage, sys.argv[2]))
    except Exception:
        print('BLOCKED: recovery entrypoint or evidence write failed; inspect retained staging', file=sys.stderr)
        sys.exit(1)
