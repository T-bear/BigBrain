#!/usr/bin/env python3
"""Synthetic-only audit invariants; cannot verify actual UID/ACL/network isolation.

The script has no option to point it at Finance or any other real data.
"""
import hashlib
import os
from pathlib import Path
import re
import stat
import tempfile
import unittest


class Rejected(ValueError):
    pass


def require(condition):
    if not condition:
        raise Rejected("synthetic bundle rejected")


def signature(info):
    return (info.st_dev, info.st_ino, info.st_size,
            info.st_mtime_ns, info.st_ctime_ns)


def inspect_synthetic_bundle(root: Path, expected: dict[str, str]):
    require(root.is_absolute())
    directory = root.lstat()
    require(stat.S_ISDIR(directory.st_mode))
    require(not directory.st_mode & (stat.S_IWGRP | stat.S_IWOTH))
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
        require(not before.st_mode & 0o222)
        h = hashlib.sha256()
        with path.open("rb") as stream:
            for chunk in iter(lambda: stream.read(8192), b""):
                h.update(chunk)
        require(signature(path.lstat()) == signature(before))
        require(h.hexdigest() == digest)


class SyntheticBundleTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix="bb-audit-synthetic-")
        self.addCleanup(self.temp.cleanup)
        self.base = Path(self.temp.name)
        self.root = self.base / "bundle"
        self.root.mkdir(mode=0o700)
        self.hashes = {}
        for name, payload in (
            ("synthetic.db", b"not-a-real-sqlite-db"),
            ("manifest.json", b'{"synthetic":true}'),
            ("payload.bin", b"fictional-payload"),
        ):
            p = self.root / name
            p.write_bytes(payload)
            p.chmod(0o440)
            self.hashes[name] = hashlib.sha256(payload).hexdigest()

    def rejects(self, root=None, hashes=None):
        with self.assertRaises(Rejected):
            inspect_synthetic_bundle(
                self.root if root is None else root,
                self.hashes if hashes is None else hashes,
            )

    def test_01_approved_fixture(self):
        inspect_synthetic_bundle(self.root, self.hashes)

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
        p.chmod(0o440)
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


if __name__ == "__main__":
    unittest.main(verbosity=2)
