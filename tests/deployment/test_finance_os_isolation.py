"""SYNTHETIC ONLY: Linux semantics, never a protected Finance qualification.

No input paths, child processes, namespace changes, mounts or privilege changes.
All mutated objects belong to fresh private fixtures. CI discovers this module.
Successful counterexamples prove limitations, not an operator security boundary.
"""

import errno
import hashlib
import os
from pathlib import Path
import shutil
import sys
import tempfile
import unittest


class FinanceOsIsolationTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        if sys.platform != "linux" or os.geteuid() == 0 or os.getuid() == 0:
            raise unittest.SkipTest("UNVERIFIED: requires unprivileged Linux")
        status = Path("/proc/self/status").read_text()
        caps = dict(line.split(":", 1) for line in status.splitlines() if ":" in line)
        if any(int(caps[key].strip(), 16) for key in ("CapEff", "CapPrm", "CapAmb")):
            raise unittest.SkipTest("BLOCKED: test process has capabilities")
        if not shutil.rmtree.avoids_symlink_attacks:
            raise unittest.SkipTest("BLOCKED: requires fd-based fixture cleanup")

    def setUp(self):
        # Explicit /tmp avoids a caller-supplied TMPDIR selecting a protected area.
        self.fixture = tempfile.TemporaryDirectory(prefix="bb-finance-os-synthetic-", dir="/tmp")
        self.addCleanup(self.fixture.cleanup)
        self.root = Path(self.fixture.name)
        self.assertEqual(self.root.stat().st_uid, os.geteuid())
        self.assertEqual(self.root.stat().st_mode & 0o777, 0o700)

    def image(self, directory=None, content=b"synthetic-A"):
        path = (directory or self.root) / "image"
        path.write_bytes(content)
        return path

    def denied(self, action):
        with self.assertRaises(OSError) as error:
            action()
        self.assertEqual(error.exception.errno, errno.EACCES)

    def test_mode_bits_deny_new_writes_but_owner_can_restore_them(self):
        image = self.image()
        image.chmod(0o400)
        self.denied(lambda: image.write_bytes(b"forbidden"))
        self.assertEqual(image.read_bytes(), b"synthetic-A")
        image.chmod(0o600)  # Same owner is NOT a separate security principal.
        image.write_bytes(b"synthetic-B")
        self.assertEqual(image.read_bytes(), b"synthetic-B")

    def test_directory_mode_denies_create_unlink_and_rename_only_while_enforced(self):
        custody = self.root / "custody"
        custody.mkdir()
        image = self.image(custody)
        custody.chmod(0o500)
        try:
            self.denied(lambda: (custody / "image-wal").write_bytes(b"sidecar"))
            self.denied(image.unlink)
            self.denied(lambda: image.rename(custody / "replacement"))
            self.assertEqual(list(custody.iterdir()), [image])
            self.assertEqual(image.read_bytes(), b"synthetic-A")
        finally:
            custody.chmod(0o700)  # Restore only this test-created directory.

    def test_leaf_nofollow_rejects_symlink_but_does_not_protect_parent(self):
        real = self.root / "real"
        real.mkdir()
        image = self.image(real)
        leaf = self.root / "leaf"
        leaf.symlink_to(image)
        with self.assertRaises(OSError) as error:
            os.open(leaf, os.O_RDONLY | os.O_NOFOLLOW)
        self.assertEqual(error.exception.errno, errno.ELOOP)
        parent = self.root / "parent"
        parent.symlink_to(real, target_is_directory=True)
        fd = os.open(parent / "image", os.O_RDONLY | os.O_NOFOLLOW)
        try:
            self.assertEqual(os.read(fd, 32), b"synthetic-A")
        finally:
            os.close(fd)
        with self.assertRaises(OSError) as error:
            os.open(parent, os.O_RDONLY | os.O_DIRECTORY | os.O_NOFOLLOW)
        self.assertEqual(error.exception.errno, errno.ENOTDIR)
        self.assertEqual(image.read_bytes(), b"synthetic-A")

    def test_readonly_ancestor_can_be_swapped_via_its_writable_parent(self):
        ancestor = self.root / "ancestor"
        ancestor.mkdir()
        image = self.image(ancestor)
        image.chmod(0o400)
        ancestor.chmod(0o500)
        saved = self.root / "saved"
        try:
            ancestor.rename(saved)  # Permission is on root, not ancestor itself.
            ancestor.mkdir()
            replacement = self.image(ancestor, b"synthetic-B")
            self.assertEqual(image.read_bytes(), b"synthetic-B")
            self.assertEqual((saved / "image").read_bytes(), b"synthetic-A")
            self.assertNotEqual(replacement.stat().st_ino, (saved / "image").stat().st_ino)
        finally:
            (saved if saved.exists() else ancestor).chmod(0o700)

    def test_hardlink_is_same_inode_and_mode_not_independent_copy(self):
        image = self.image()
        alias = self.root / "alias"
        os.link(image, alias)
        self.assertEqual(image.stat().st_ino, alias.stat().st_ino)
        self.assertEqual(image.stat().st_nlink, 2)
        image.chmod(0o400)
        self.denied(lambda: alias.write_bytes(b"forbidden"))
        alias.chmod(0o600)
        alias.write_bytes(b"synthetic-B")
        self.assertEqual(image.read_bytes(), b"synthetic-B")

    def test_single_link_and_chmod_do_not_revoke_already_open_writer(self):
        image = self.image()
        alias = self.root / "alias"
        os.link(image, alias)
        with alias.open("r+b", buffering=0) as writer:
            alias.unlink()
            self.assertEqual(image.stat().st_nlink, 1)
            image.chmod(0o400)
            self.denied(lambda: image.write_bytes(b"forbidden"))
            writer.write(b"synthetic-B")
        self.assertEqual(image.read_bytes(), b"synthetic-B")

    def test_deterministic_aba_between_hash_and_open_evades_stream_rehash(self):
        image = self.image()
        other = self.root / "other"
        other.write_bytes(b"synthetic-B")
        original = self.root / "original"
        before = {p.name: p.read_bytes() for p in self.root.iterdir()}
        with image.open("rb") as hash_stream:
            digest = hashlib.sha256(hash_stream.read()).digest()
            image.rename(original)
            other.rename(image)
            try:
                with image.open("rb") as reader:
                    self.assertNotEqual(os.fstat(reader.fileno()).st_ino,
                                        os.fstat(hash_stream.fileno()).st_ino)
                    self.assertEqual(reader.read(), b"synthetic-B")
            finally:
                image.rename(other)
                original.rename(image)
            hash_stream.seek(0)
            self.assertEqual(hashlib.sha256(hash_stream.read()).digest(), digest)
        self.assertEqual({p.name: p.read_bytes() for p in self.root.iterdir()}, before)
        # Deterministic interleaving, not a stress race or a modified library hook.
        # Reader writes no bytes; unchanged pre/post bytes cannot establish custody.

    def test_failure_rollback_and_cleanup_do_not_follow_fixture_symlink(self):
        witness = self.root / "witness"
        witness.mkdir()
        image = self.image(witness)
        sandbox = None
        with self.assertRaisesRegex(RuntimeError, "synthetic interruption"):
            with tempfile.TemporaryDirectory(prefix="partial-", dir=self.root) as name:
                sandbox = Path(name)
                (sandbox / "alias").symlink_to(witness, target_is_directory=True)
                pending = sandbox / "pending"
                pending.write_bytes(b"incomplete synthetic image")
                sandbox.chmod(0o500)
                try:
                    raise RuntimeError("synthetic interruption")
                finally:
                    sandbox.chmod(0o700)
        self.assertFalse(sandbox.exists())
        self.assertEqual(list(self.root.iterdir()), [witness])
        self.assertEqual(image.read_bytes(), b"synthetic-A")
        self.assertFalse((self.root / "READY").exists())

    @unittest.skip("UNVERIFIED/BLOCKED: no separate UID; same-owner tests are not principal isolation")
    def test_future_separate_principal_enforcement(self):
        pass

    @unittest.skip("UNVERIFIED/BLOCKED: no private read-only mount or backing seal; no mount attempted")
    def test_future_mount_namespace_and_no_writable_alias_enforcement(self):
        pass


if __name__ == "__main__":
    unittest.main()
