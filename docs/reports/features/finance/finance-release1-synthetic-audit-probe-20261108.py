#!/usr/bin/env python3
"""Synthetic-only Finance audit probe, executed inside a restricted bwrap view."""
import errno
import os
import resource
import socket
import sqlite3
import stat
from pathlib import Path

DB = "bb-091-pre-migration-20260817.db"
NAMES = {DB}
for stem in ("3b093584173d74f6", "8b549915b4e4129c"):
    for kind in ("manifest", "data"):
        NAMES.add(f"finance-backup-{stem}.{kind}.json")

def need(ok, reason):
    if not ok:
        raise AssertionError(reason)

need(set(os.listdir("/evidence")) == NAMES, "unexpected evidence allowlist")
for name in NAMES:
    path = Path("/evidence") / name
    st = path.lstat()
    need(stat.S_ISREG(st.st_mode) and st.st_nlink == 1, "invalid evidence type")
    need(stat.S_IMODE(st.st_mode) == 0o440, "invalid evidence mode")
    need(not os.listxattr(path, follow_symlinks=False), "unexpected xattr")
print("SYNTHETIC_ALLOWLIST=PASS", flush=True)

mounts = []
for line in Path("/proc/self/mountinfo").read_text().splitlines():
    fields = line.split(" - ", 1)[0].split()
    if len(fields) >= 6 and fields[4] == "/evidence":
        mounts.append(set(fields[5].split(",")))
need(len(mounts) == 1 and "ro" in mounts[0] and "rw" not in mounts[0],
     "synthetic evidence mount not read-only")
print("SYNTHETIC_RO_BIND=PASS", flush=True)

need(all(not (Path("/evidence") / (DB + ext)).exists()
         for ext in ("-wal", "-shm", "-journal")), "sqlite sidecar present")
uri = "file:/evidence/" + DB + "?mode=ro&immutable=1"
db = sqlite3.connect(uri, uri=True)
try:
    db.execute("PRAGMA query_only=ON")
    need(db.execute("PRAGMA query_only").fetchone()[0] == 1, "SQLite writable mode")
    need(db.execute("PRAGMA quick_check").fetchone()[0] == "ok", "sqlite check failed")
    need(db.execute("SELECT value FROM synthetic_check").fetchone()[0] ==
         "synthetic-only", "sqlite content changed")
finally:
    db.close()
print("SYNTHETIC_SQLITE_RO=PASS", flush=True)

for name in NAMES:
    try:
        with open(Path("/evidence") / name, "wb") as stream:
            stream.write(b"forbidden")
    except OSError:
        continue
    raise AssertionError("synthetic evidence writable")
print("SYNTHETIC_WRITES_DENIED=PASS", flush=True)

need(set(os.listdir("/")) == {
    "bin", "evidence", "lib", "lib64", "probe.py", "proc", "tmp", "usr"
}, "unexpected sandbox root")
for forbidden in ("/home", "/etc", "/var", "/run", "/finance", "/tmp/host-sentinel"):
    need(not Path(forbidden).exists(), "unexpected host path")
need(all(name == "lo" for _, name in socket.if_nameindex()), "network leak")
print("SYNTHETIC_FILESYSTEM_NETWORK=PASS", flush=True)

status = {}
for line in Path("/proc/self/status").read_text().splitlines():
    if ":" in line:
        name, value = line.split(":", 1)
        status[name] = value.strip()
need(int(status["CapEff"], 16) == 0 and
     int(status["CapPrm"], 16) == 0 and
     status["NoNewPrivs"] == "1", "privilege leak")
uid = int(os.environ["EXPECTED_UID"])
gid = int(os.environ["EXPECTED_GID"])
need(all(int(v) == uid for v in status["Uid"].split()), "uid mismatch")
need(all(int(v) == gid for v in status["Gid"].split()), "gid mismatch")
if os.environ["EXPECTED_AUDIT"] == "1":
    need(os.getgroups() == [], "audit account has extra groups")
print("SYNTHETIC_IDENTITY=PASS", flush=True)

# Directory traversal of /proc/self/fd opens a transient directory descriptor.
# Only 0, 1 and 2 may be live after it closes.
leaked = []
for name in os.listdir("/proc/self/fd"):
    n = int(name)
    if n <= 2:
        continue
    try:
        os.fstat(n)
    except OSError as exc:
        if exc.errno == errno.EBADF:
            continue
        raise
    leaked.append(n)
need(not leaked, "inherited host file descriptors")
print("SYNTHETIC_FD_CLOSURE=PASS", flush=True)

for name, ceiling in (("RLIMIT_CPU", 8), ("RLIMIT_AS", 768 * 1024 * 1024),
                       ("RLIMIT_NPROC", 512), ("RLIMIT_NOFILE", 64)):
    current, hard = resource.getrlimit(getattr(resource, name))
    need(0 < current <= ceiling and current <= hard, "resource limit missing")
print("SYNTHETIC_RLIMITS=PASS", flush=True)
print("SYNTHETIC_UID_PROBE=PASS", flush=True)
