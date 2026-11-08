#!/usr/bin/env python3
"""Synthetic-only bundle invariants, not a real UID/ACL/network attestation.

No CLI parameters accept Finance paths or real data. UID/GID and exact modes
refer only to files created in the temporary synthetic test directory.
"""
import hashlib
import os
from pathlib import Path
import re
import shutil
import subprocess
import stat
import tempfile
import unittest
from unittest.mock import patch


FILE_MODE = 0o440
DIR_MODE = 0o700
ACL_XATTRS = frozenset({"system.posix_acl_access", "system.posix_acl_default"})


class Rejected(ValueError):
    pass


def require(condition):
    if not condition:
        raise Rejected("synthetic bundle rejected")


def signature(info):
    return (info.st_dev, info.st_ino, info.st_size, info.st_mtime_ns,
            info.st_ctime_ns, info.st_mode, info.st_uid, info.st_gid,
            info.st_nlink)


def no_posix_acl(path):
    try:
        attrs = os.listxattr(path, follow_symlinks=False)
    except (OSError, NotImplementedError) as exc:
        raise Rejected("cannot inspect synthetic POSIX ACL") from exc
    require(not ACL_XATTRS.intersection(attrs))


def ownership_and_mode(info, uid, gid, mode):
    require(info.st_uid == uid and info.st_gid == gid)
    require(stat.S_IMODE(info.st_mode) == mode)


def inspect_synthetic_bundle(root: Path, expected: dict[str, str],
                             expected_uid: int, expected_gid: int):
    require(root.is_absolute())
    directory = root.lstat()
    require(stat.S_ISDIR(directory.st_mode))
    ownership_and_mode(directory, expected_uid, expected_gid, DIR_MODE)
    no_posix_acl(root)
    require(bool(expected))

    for name, digest in expected.items():
        require(isinstance(name, str) and
                re.fullmatch(r"[A-Za-z0-9_.-]{1,80}", name) is not None and
                name not in (".", "..") and not name.startswith("."))
        require(isinstance(digest, str) and
                re.fullmatch(r"[0-9a-f]{64}", digest) is not None)

    require(set(os.listdir(root)) == set(expected))
    for name, digest in expected.items():
        path = root / name
        before = path.lstat()
        require(stat.S_ISREG(before.st_mode) and before.st_nlink == 1)
        ownership_and_mode(before, expected_uid, expected_gid, FILE_MODE)
        no_posix_acl(path)

        # O_NOFOLLOW and fstat bind hashing to the opened object, not
        # to a pathname that may have been replaced after lstat().
        flags = os.O_RDONLY | os.O_CLOEXEC | os.O_NOFOLLOW
        try:
            fd = os.open(path, flags)
        except OSError as exc:
            raise Rejected("cannot open synthetic fixture") from exc
        try:
            opened = os.fstat(fd)
            require(signature(opened) == signature(before))
            digest_state = hashlib.sha256()
            for chunk in iter(lambda: os.read(fd, 8192), b""):
                digest_state.update(chunk)
            require(signature(os.fstat(fd)) == signature(before))
        finally:
            os.close(fd)
        require(signature(path.lstat()) == signature(before))
        no_posix_acl(path)
        require(digest_state.hexdigest() == digest)

    require(signature(root.lstat()) == signature(directory))
    no_posix_acl(root)


class SyntheticBundleTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix="bb-audit-synthetic-")
        self.addCleanup(self.temp.cleanup)
        self.base = Path(self.temp.name)
        self.root = self.base / "bundle"
        self.root.mkdir(mode=DIR_MODE)
        self.uid, self.gid = os.getuid(), os.getgid()
        self.hashes = {}
        for name, payload in (
            ("synthetic.db", b"not-a-real-sqlite-db"),
            ("manifest.json", b'{"synthetic":true}'),
            ("payload.bin", b"fictional-payload"),
        ):
            p = self.root / name
            p.write_bytes(payload)
            p.chmod(FILE_MODE)
            self.hashes[name] = hashlib.sha256(payload).hexdigest()

    def inspect(self, root=None, hashes=None, uid=None, gid=None):
        return inspect_synthetic_bundle(
            self.root if root is None else root,
            self.hashes if hashes is None else hashes,
            self.uid if uid is None else uid,
            self.gid if gid is None else gid,
        )

    def rejects(self, **kwargs):
        with self.assertRaises(Rejected):
            self.inspect(**kwargs)

    def test_01_approved_fixture(self):
        self.inspect()

    def test_02_missing_file(self):
        (self.root / "payload.bin").unlink()
        self.rejects()

    def test_03_unexpected_file(self):
        (self.root / "extra").write_bytes(b"x")
        self.rejects()

    def test_04_sqlite_sidecar(self):
        (self.root / "synthetic.db-wal").write_bytes(b"fake")
        self.rejects()

    def test_05_file_symlink(self):
        (self.root / "manifest.json").unlink()
        (self.root / "manifest.json").symlink_to(self.root / "synthetic.db")
        self.rejects()

    def test_06_directory_symlink(self):
        alias = self.base / "alias"
        alias.symlink_to(self.root, target_is_directory=True)
        self.rejects(root=alias)

    def test_07_hardlink(self):
        os.link(self.root / "synthetic.db", self.base / "duplicate")
        self.rejects()

    def test_08_owner_writable_file(self):
        (self.root / "synthetic.db").chmod(0o640)
        self.rejects()

    def test_09_world_writable_file(self):
        (self.root / "synthetic.db").chmod(0o446)
        self.rejects()

    def test_10_group_writable_directory(self):
        self.root.chmod(0o770)
        self.rejects()

    def test_11_changed_hash(self):
        p = self.root / "payload.bin"
        p.chmod(0o600)
        p.write_bytes(b"changed")
        p.chmod(FILE_MODE)
        self.rejects()

    def test_12_directory_disguised_as_file(self):
        (self.root / "manifest.json").unlink()
        (self.root / "manifest.json").mkdir()
        self.rejects()

    def test_13_bad_allowlist_name(self):
        h = dict(self.hashes)
        h["../outside"] = "0" * 64
        self.rejects(hashes=h)

    def test_14_invalid_digest(self):
        h = dict(self.hashes)
        h["payload.bin"] = "not-a-sha256"
        self.rejects(hashes=h)

    def test_15_non_directory_root(self):
        self.rejects(root=self.root / "synthetic.db")

    def test_16_wrong_file_or_directory_owner(self):
        self.rejects(uid=self.uid + 1)

    def test_17_wrong_file_or_directory_group(self):
        self.rejects(gid=self.gid + 1)

    def test_18_world_readable_file(self):
        (self.root / "synthetic.db").chmod(0o444)
        self.rejects()

    def test_19_setuid_file(self):
        (self.root / "synthetic.db").chmod(0o4440)
        self.rejects()

    def test_20_setgid_directory(self):
        self.root.chmod(0o2700)
        self.rejects()

    def test_21_sticky_directory(self):
        self.root.chmod(0o1700)
        self.rejects()

    def test_22_world_readable_directory(self):
        self.root.chmod(0o755)
        self.rejects()

    def test_23_file_with_posix_acl_xattr(self):
        original = os.listxattr
        affected = self.root / "synthetic.db"

        def fake(path, **kwargs):
            attrs = original(path, **kwargs)
            if Path(path) == affected:
                return list(attrs) + ["system.posix_acl_access"]
            return attrs

        with patch("os.listxattr", side_effect=fake):
            self.rejects()

    def test_24_directory_with_posix_acl_xattr(self):
        original = os.listxattr

        def fake(path, **kwargs):
            attrs = original(path, **kwargs)
            if Path(path) == self.root:
                return list(attrs) + ["system.posix_acl_default"]
            return attrs

        with patch("os.listxattr", side_effect=fake):
            self.rejects()

    def test_25_acl_inspection_failure_is_rejected(self):
        with patch("os.listxattr", side_effect=OSError("synthetic EACCES")):
            self.rejects()

    def test_26_wrong_file_mode_even_if_narrower(self):
        (self.root / "synthetic.db").chmod(0o400)
        self.rejects()

    def test_27_sqlite_shm_name_is_extra(self):
        (self.root / "synthetic.db-shm").write_bytes(b"fake")
        self.rejects()

    def test_28_sqlite_journal_name_is_extra(self):
        (self.root / "synthetic.db-journal").write_bytes(b"fake")
        self.rejects()


    def test_29_real_posix_acl_if_supported(self):
        tool = shutil.which("setfacl")
        if tool is None:
            self.skipTest("setfacl not installed")
        target = self.root / "synthetic.db"
        other_uid = 65534 if self.uid != 65534 else 65533
        result = subprocess.run(
            [tool, "-m", f"u:{other_uid}:r--", str(target)],
            capture_output=True, text=True, check=False,
        )
        if result.returncode != 0:
            self.skipTest("synthetic temp filesystem has no POSIX ACL support")
        self.assertEqual(stat.S_IMODE(target.lstat().st_mode), FILE_MODE)
        self.assertIn("system.posix_acl_access",
                      os.listxattr(target, follow_symlinks=False))
        self.rejects()


if __name__ == "__main__":
    unittest.main(verbosity=2)
