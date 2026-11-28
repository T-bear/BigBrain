"""Synthetic custody protocol qualification, NOT an OS seal or Finance reader.

Only fresh private fixtures, a fixed child writer and fixed aggregate SQLite read.
Same-UID checks cannot exclude hidden writers, mounts or ABA between observations.
The explicit breach event below models trusted custody notification, not detection.
No production imports, caller paths, credentials, namespaces or privilege changes.
"""

from contextlib import closing
from dataclasses import dataclass, replace
import hashlib
import os
from pathlib import Path
import shutil
import sqlite3
import stat
import subprocess
import sys
import tempfile
import unittest


# Fixed code, no shell, descendants, descriptor transfer or caller-selected source.
# The connection and the deliberately retained writable fd both close before ACK.
WRITER = """
import errno, os, sqlite3, sys
db = sqlite3.connect('custody/image.sqlite')
try:
    db.execute('CREATE TABLE samples (value INTEGER NOT NULL)')
    db.executemany('INSERT INTO samples VALUES (?)', [(1,), (2,), (3,)])
    db.commit()
finally:
    db.close()
fd = os.open('custody/image.sqlite', os.O_RDWR)
if sys.stdin.readline().strip() != 'close':
    os._exit(17)
os.close(fd)
try:
    os.fstat(fd)
except OSError as error:
    if error.errno != errno.EBADF:
        raise
else:
    raise RuntimeError('descriptor still open')
print('CLOSED', flush=True)
"""


class Stopped(RuntimeError):
    """Contains a fixed code only; no path, SQL, row or underlying exception."""


@dataclass(frozen=True)
class SimulatedReceipt:
    invocation: str
    fingerprint: tuple


class SimulatedCustody:
    """Test-only, in-memory protocol. Neither authenticated nor crash durable."""

    def __init__(self, root, writer):
        self.root = root
        self.image = root / 'custody' / 'image.sqlite'
        self.writer = writer
        self.state = 'CAPTURING'
        self.stop_code = None
        self.ready = False
        self.result = None
        self.receipt = None
        self.used = False

    def stop(self, code):
        if self.stop_code is None:
            self.stop_code = code
        self.state = 'STOP'
        self.ready = False
        self.result = None
        raise Stopped(self.stop_code) from None

    def require(self, state):
        if self.stop_code is not None:
            self.stop(self.stop_code)
        if self.state != state:
            self.stop('protocol.invalid')

    def complete(self, acknowledgement):
        self.require('CAPTURING')
        if self.writer.poll() != 0 or acknowledgement != 'CLOSED\n':
            self.stop('writer.unclosed')
        self.state = 'COMPLETE'

    def fingerprint(self):
        # Check the fixture parent and leaf without following links. This is an
        # observation under cooperative test custody, NOT race-proof resolution.
        try:
            with closing_fd(os.open(self.image.parent, os.O_RDONLY | os.O_DIRECTORY |
                                    os.O_NOFOLLOW)) as parent:
                if os.listdir(parent) != ['image.sqlite']:
                    self.stop('custody.changed')
                with closing_fd(os.open('image.sqlite', os.O_RDONLY | os.O_NOFOLLOW |
                                        os.O_NONBLOCK, dir_fd=parent)) as fd:
                    info = os.fstat(fd)
                    if (not stat.S_ISREG(info.st_mode) or info.st_nlink != 1 or
                            not 100 <= info.st_size <= 65536):
                        self.stop('custody.changed')
                    data = os.read(fd, 65537)
                    if len(data) != info.st_size:
                        self.stop('custody.changed')
                    parent_info = os.fstat(parent)
                    return (parent_info.st_dev, parent_info.st_ino, info.st_dev,
                            info.st_ino, info.st_size, info.st_mtime_ns, info.st_ctime_ns,
                            hashlib.sha256(data).digest())
        except OSError:
            self.stop('custody.changed')

    def seal(self):
        self.require('COMPLETE')
        self.receipt = SimulatedReceipt(self.root.name, self.fingerprint())
        self.state = 'SEALED'
        self.ready = True  # In-memory simulated READY only; never a disk marker.
        return self.receipt

    def verify(self, receipt):
        if receipt != self.receipt:
            self.stop('receipt.invalid')
        if self.fingerprint() != receipt.fingerprint:
            self.stop('custody.changed')

    def breach(self):
        # Explicit trusted simulation event, including transient ABA / fd alias.
        self.stop('custody.changed')

    def crash(self):
        self.stop('operation.incomplete')

    def audit(self, receipt, boundary=lambda stage: None):
        if self.used:
            self.stop('receipt.replay')
        self.require('SEALED')
        self.used = True  # No retry/refund, including failed/interrupted reads.
        self.state = 'AUDITING'
        try:
            boundary('before_open')
            self.require('AUDITING')
            self.verify(receipt)
            # Fixed tiny synthetic query, no Finance schema or library claim.
            with closing(sqlite3.connect(self.image.as_uri() + '?mode=ro&immutable=1',
                                         uri=True)) as db:
                db.execute('PRAGMA query_only=ON')
                db.execute('PRAGMA trusted_schema=OFF')
                db.execute('PRAGMA temp_store=MEMORY')
                count = db.execute('SELECT count(*) FROM samples').fetchone()[0]
            boundary('after_read')  # Connection is closed before release checks.
            self.require('AUDITING')
            self.verify(receipt)
        except Stopped:
            raise
        except (OSError, sqlite3.Error, RuntimeError):
            self.stop('operation.incomplete')
        self.state = 'CLOSED'
        self.ready = False
        self.result = {'synthetic_rows': count}
        return self.result


class closing_fd:
    def __init__(self, fd):
        self.fd = fd

    def __enter__(self):
        return self.fd

    def __exit__(self, *args):
        os.close(self.fd)


class FinancePositiveSealTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        if sys.platform != 'linux' or os.getuid() == 0 or os.geteuid() == 0:
            raise unittest.SkipTest('UNVERIFIED/BLOCKED: requires unprivileged Linux')
        # Only own-process metadata; no host mount/handle scan or protected probe.
        caps = dict(line.split(':', 1) for line in Path('/proc/self/status').read_text().splitlines()
                    if ':' in line)
        if any(int(caps[key].strip(), 16) for key in ('CapEff', 'CapPrm', 'CapAmb')):
            raise unittest.SkipTest('UNVERIFIED/BLOCKED: process has capabilities')
        if not shutil.rmtree.avoids_symlink_attacks:
            raise unittest.SkipTest('UNVERIFIED/BLOCKED: requires fd-based cleanup')

    def setUp(self):
        fixture = tempfile.TemporaryDirectory(prefix='bb-finance-positive-synthetic-', dir='/tmp')
        self.addCleanup(fixture.cleanup)
        self.root = Path(fixture.name)
        self.assertEqual(self.root.stat().st_mode & 0o777, 0o700)
        self.assertEqual(self.root.stat().st_uid, os.geteuid())
        (self.root / 'custody').mkdir(mode=0o700)
        self.writer = subprocess.Popen(
            [sys.executable, '-I', '-B', '-c', WRITER], cwd=self.root, env={},
            stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.PIPE,
            text=True, close_fds=True)
        self.addCleanup(self.cleanup_writer)  # Reap child before fixture removal.
        self.custody = SimulatedCustody(self.root, self.writer)

    def cleanup_writer(self):
        if self.writer.poll() is None:
            self.writer.kill()  # Only the fixed child owned by this test.
        self.writer.communicate(timeout=5)

    def complete(self):
        acknowledgement, errors = self.writer.communicate('close\n', timeout=5)
        self.assertEqual(self.writer.returncode, 0)
        self.assertEqual(errors, '')
        self.custody.complete(acknowledgement)

    def sealed(self):
        self.complete()
        return self.custody.seal()

    def assert_latched(self, action, code):
        with self.assertRaisesRegex(Stopped, '^' + code.replace('.', r'\.') + '$'):
            action()
        self.assertEqual(self.custody.state, 'STOP')
        self.assertEqual(self.custody.stop_code, code)
        self.assertFalse(self.custody.ready)
        self.assertIsNone(self.custody.result)
        # Repair, a new ACK or a valid old receipt cannot reset STOP.
        for retry in (lambda: self.custody.complete('CLOSED\n'), self.custody.seal,
                      lambda: self.custody.audit(self.custody.receipt)):
            with self.assertRaises(Stopped) as error:
                retry()
            self.assertEqual(str(error.exception), code)
        self.assertFalse((self.root / 'READY').exists())

    def test_positive_closed_writer_seal_and_safe_aggregate_read(self):
        self.assertEqual(self.custody.state, 'CAPTURING')
        self.complete()
        self.assertEqual(self.custody.state, 'COMPLETE')
        receipt = self.custody.seal()
        self.assertTrue(self.custody.ready)
        before = self.custody.image.read_bytes()
        stages = []
        self.assertEqual(self.custody.audit(receipt, stages.append), {'synthetic_rows': 3})
        self.assertEqual(stages, ['before_open', 'after_read'])
        self.assertEqual(self.custody.state, 'CLOSED')
        self.assertFalse(self.custody.ready)
        self.assertEqual(self.custody.image.read_bytes(), before)
        self.assertEqual(list(self.custody.image.parent.iterdir()), [self.custody.image])
        self.assertEqual(self.custody.fingerprint(), receipt.fingerprint)

    def test_live_writer_cannot_be_completed_by_forged_ack(self):
        self.assertIsNone(self.writer.poll())  # Child awaits input; no timing sleep.
        self.assert_latched(lambda: self.custody.complete('CLOSED\n'), 'writer.unclosed')

    def test_writer_crash_closes_process_but_never_completes_capture(self):
        acknowledgement, _ = self.writer.communicate('crash\n', timeout=5)
        self.assertEqual(self.writer.returncode, 17)
        self.assert_latched(lambda: self.custody.complete(acknowledgement), 'writer.unclosed')

    def test_zero_exit_without_descriptor_ack_is_rejected(self):
        self.writer.communicate('close\n', timeout=5)
        self.assertEqual(self.writer.returncode, 0)
        self.assert_latched(lambda: self.custody.complete(''), 'writer.unclosed')

    def test_seal_before_completion_is_stop(self):
        self.assert_latched(self.custody.seal, 'protocol.invalid')

    def test_mutation_after_read_discards_aggregate_and_latches(self):
        receipt = self.sealed()
        original = self.custody.image.read_bytes()

        def mutate(stage):
            if stage == 'after_read':
                with self.custody.image.open('r+b') as writer:
                    writer.seek(-1, os.SEEK_END)
                    writer.write(b'X')

        self.assert_latched(lambda: self.custody.audit(receipt, mutate), 'custody.changed')
        self.custody.image.write_bytes(original)
        self.assert_latched(lambda: self.custody.audit(receipt), 'custody.changed')

    def test_hardlink_outside_custody_directory_is_stop(self):
        receipt = self.sealed()
        alias = self.root / 'alias'
        os.link(self.custody.image, alias)
        self.assert_latched(lambda: self.custody.audit(receipt), 'custody.changed')
        alias.unlink()
        self.assert_latched(lambda: self.custody.audit(receipt), 'custody.changed')

    def test_same_byte_substitution_before_open_is_stop(self):
        receipt = self.sealed()
        replacement = self.root / 'replacement'
        replacement.write_bytes(self.custody.image.read_bytes())
        original_inode = self.custody.image.stat().st_ino

        def substitute(stage):
            if stage == 'before_open':
                replacement.replace(self.custody.image)

        self.assert_latched(lambda: self.custody.audit(receipt, substitute), 'custody.changed')
        self.assertNotEqual(self.custody.image.stat().st_ino, original_inode)

    def test_parent_symlink_alias_is_stop(self):
        receipt = self.sealed()
        parent = self.custody.image.parent
        saved = self.root / 'saved'
        parent.rename(saved)
        parent.symlink_to(saved, target_is_directory=True)
        self.assert_latched(lambda: self.custody.audit(receipt), 'custody.changed')

    def test_leaf_symlink_alias_is_stop(self):
        receipt = self.sealed()
        saved = self.root / 'saved'
        self.custody.image.rename(saved)
        self.custody.image.symlink_to(saved)
        self.assert_latched(lambda: self.custody.audit(receipt), 'custody.changed')

    def test_simulated_hidden_writer_alias_event_latches_even_without_mutation(self):
        receipt = self.sealed()
        with self.custody.image.open('r+b'):
            # No hash/link-count claim: the simulation explicitly signals custody loss.
            self.assert_latched(self.custody.breach, 'custody.changed')
        self.assertEqual(self.custody.fingerprint(), receipt.fingerprint)
        self.assert_latched(lambda: self.custody.audit(receipt), 'custody.changed')

    def test_wrong_invocation_receipt_is_stop(self):
        receipt = self.sealed()
        self.assert_latched(lambda: self.custody.audit(replace(receipt, invocation='old')),
                            'receipt.invalid')

    def test_consumed_receipt_replay_clears_result_and_latches(self):
        receipt = self.sealed()
        self.custody.audit(receipt)
        self.assert_latched(lambda: self.custody.audit(receipt), 'receipt.replay')

    def test_capture_crash_is_stop(self):
        self.assert_latched(self.custody.crash, 'operation.incomplete')

    def test_complete_before_seal_crash_is_stop(self):
        self.complete()
        self.assert_latched(self.custody.crash, 'operation.incomplete')

    def test_sealed_before_audit_crash_is_stop(self):
        self.sealed()
        self.assert_latched(self.custody.crash, 'operation.incomplete')

    def test_crash_after_read_releases_no_partial_result(self):
        receipt = self.sealed()

        def interrupt(stage):
            if stage == 'after_read':
                raise RuntimeError('synthetic crash')

        self.assert_latched(lambda: self.custody.audit(receipt, interrupt), 'operation.incomplete')

    @unittest.skip('UNVERIFIED/BLOCKED: true OS mount/backing seal and separate principals; '
                   'no mount or privilege operation attempted')
    def test_true_os_mount_and_backing_seal(self):
        pass


if __name__ == '__main__':
    unittest.main()
