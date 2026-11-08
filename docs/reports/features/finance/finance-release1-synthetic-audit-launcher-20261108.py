#!/usr/bin/env python3
"""Run the Finance audit isolation candidate against NEW SYNTHETIC FILES ONLY."""
import datetime
import hashlib
import os
import pathlib
import pwd
import shutil
import signal
import sqlite3
import stat
import subprocess
import sys
import tempfile

PROBE_FILE = "finance-release1-synthetic-audit-probe-20261108.py"
PROBE_SHA256 = "c60aadde627992b00bbc316d154d707d595dda37d5701bb427172c0b23b465ae"
AUDIT_ACCOUNT = "bb-finance-audit"
MANDATE_DEADLINE_UTC = datetime.date(2026, 11, 18)
DB = "bb-091-pre-migration-20260817.db"
BINARY = ("/usr/bin/python3.13", "/usr/bin/bwrap",
          "/usr/bin/setpriv", "/usr/bin/prlimit", "/usr/bin/timeout")

class Interrupted(Exception):
    pass

def stop(signum, frame):
    raise Interrupted(signum)

def check(condition, reason):
    if not condition:
        raise RuntimeError("STOPP: " + reason)

def digest(path):
    h = hashlib.sha256()
    with open(path, "rb") as file:
        while block := file.read(1024 * 1024):
            h.update(block)
    return h.hexdigest()

def file_identity(path):
    meta = os.lstat(path)
    check(stat.S_ISREG(meta.st_mode) and meta.st_nlink == 1,
          "invalid synthetic file type")
    return (meta.st_dev, meta.st_ino, meta.st_size, meta.st_mtime_ns,
            stat.S_IMODE(meta.st_mode), digest(path))

def main():
    check(len(sys.argv) == 1, "no arguments accepted")
    check(os.geteuid() == os.getuid() != 0, "root or setuid not allowed")
    check(datetime.datetime.now(datetime.timezone.utc).date() <= MANDATE_DEADLINE_UTC,
          "Finance Release 1 mandate expired")
    for binary in BINARY:
        check(os.path.isfile(binary) and os.access(binary, os.X_OK),
              "missing runtime")
    account = pwd.getpwnam(AUDIT_ACCOUNT)
    check(account.pw_uid not in (0, 1654) and account.pw_gid != 0 and
          account.pw_shell == "/usr/sbin/nologin" and
          account.pw_dir == "/nonexistent", "unsafe audit account metadata")
    is_audit = (os.getuid(), os.getgid()) == (account.pw_uid, account.pw_gid)
    if is_audit:
        check(os.getgroups() == [], "audit account has supplementary groups")

    verified_probe = pathlib.Path(__file__).with_name(PROBE_FILE)
    check(verified_probe.is_file() and not verified_probe.is_symlink() and
          digest(verified_probe) == PROBE_SHA256, "unreviewed probe bytes")

    scratch = pathlib.Path(tempfile.mkdtemp(
        prefix="bb-finance-uid-synthetic.", dir="/tmp"))
    proc = None
    try:
        evidence = scratch / "evidence"
        evidence.mkdir(mode=0o700)
        conn = sqlite3.connect(evidence / DB)
        try:
            conn.execute("CREATE TABLE synthetic_check (value TEXT NOT NULL)")
            conn.execute("INSERT INTO synthetic_check VALUES ('synthetic-only')")
            conn.commit()
        finally:
            conn.close()
        for stem in ("3b093584173d74f6", "8b549915b4e4129c"):
            for kind in ("manifest", "data"):
                (evidence / f"finance-backup-{stem}.{kind}.json").write_text(
                    '{"synthetic":true}\n', encoding="ascii")
        copied_probe = scratch / "probe.py"
        copied_probe.write_bytes(verified_probe.read_bytes())
        check(digest(copied_probe) == PROBE_SHA256, "probe changed during staging")
        for file in evidence.iterdir():
            file.chmod(0o440)
        copied_probe.chmod(0o440)
        evidence.chmod(0o500)
        baseline = {p.name: file_identity(p) for p in evidence.iterdir()}
        check(len(baseline) == 5, "synthetic file count incorrect")

        args = [
            "/usr/bin/setpriv", "--no-new-privs", "--pdeathsig", "KILL",
            "/usr/bin/prlimit", "--cpu=8:8", "--as=805306368:805306368",
            "--nproc=512:512", "--nofile=64:64", "--",
            "/usr/bin/timeout", "--signal=TERM", "--kill-after=2s", "12s",
            "/usr/bin/bwrap", "--die-with-parent", "--new-session",
            "--unshare-user", "--unshare-pid", "--unshare-ipc",
            "--unshare-net", "--cap-drop", "ALL",
            "--ro-bind", "/usr", "/usr",
            "--symlink", "usr/bin", "/bin",
            "--symlink", "usr/lib", "/lib",
            "--symlink", "usr/lib64", "/lib64",
            "--ro-bind", str(evidence), "/evidence",
            "--ro-bind", str(copied_probe), "/probe.py",
            "--tmpfs", "/tmp", "--proc", "/proc", "--clearenv",
            "--setenv", "PATH", "/usr/bin",
            "--setenv", "EXPECTED_UID", str(os.getuid()),
            "--setenv", "EXPECTED_GID", str(os.getgid()),
            "--setenv", "EXPECTED_AUDIT", "1" if is_audit else "0",
            "--chdir", "/",
            "--", "/usr/bin/python3.13", "-I", "-S", "-B", "/probe.py",
        ]
        print("SCOPE=SYNTHETIC_ONLY", flush=True)
        print("AUDIT_UID_EXECUTION=" + ("RUNNING" if is_audit else "BLOCKED"),
              flush=True)
        proc = subprocess.Popen(
            args, stdin=subprocess.DEVNULL, close_fds=True, pass_fds=(),
            start_new_session=True, env={"PATH": "/usr/bin"},
        )
        result = proc.wait()
        check(result == 0, "synthetic sandbox failed")
        check({p.name: file_identity(p) for p in evidence.iterdir()} == baseline,
              "synthetic source changed during sandbox run")
        print("SYNTHETIC_LAUNCHER=PASS", flush=True)
    finally:
        # EXIT cleanup is intentionally NOT claimed for SIGKILL: a separate
        # owner-supervised recovery procedure would dispose of such leftovers.
        if proc is not None and proc.poll() is None:
            try:
                os.killpg(proc.pid, signal.SIGTERM)
            except ProcessLookupError:
                pass
            try:
                proc.wait(timeout=2)
            except subprocess.TimeoutExpired:
                try:
                    os.killpg(proc.pid, signal.SIGKILL)
                except ProcessLookupError:
                    pass
                proc.wait(timeout=2)
        evidence = scratch / "evidence"
        if evidence.is_dir() and not evidence.is_symlink():
            evidence.chmod(0o700)
        shutil.rmtree(scratch)

if __name__ == "__main__":
    for sig in (signal.SIGTERM, signal.SIGHUP, signal.SIGINT):
        signal.signal(sig, stop)
    try:
        main()
    except (Interrupted, RuntimeError, AssertionError) as exc:
        print(f"STOPP: {type(exc).__name__}: {exc}", file=sys.stderr, flush=True)
        sys.exit(1)
