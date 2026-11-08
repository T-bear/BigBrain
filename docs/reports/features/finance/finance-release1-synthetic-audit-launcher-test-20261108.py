#!/usr/bin/env python3
"""Bounded, owner-UID, synthetic-only negative tests for the audit launcher."""
import hashlib
import os
import re
import select
import shutil
import signal
import stat
import subprocess
import tempfile
import time
from pathlib import Path

ROOT = Path(__file__).resolve().parent
LAUNCH = ROOT / "finance-release1-synthetic-audit-launcher-20261108.py"
PROBE = ROOT / "finance-release1-synthetic-audit-probe-20261108.py"
PYTHON = "/usr/bin/python3.13"
PREFIX = "bb-finance-uid-synthetic."

def check(test, reason):
    if not test:
        raise AssertionError(reason)

def run(script, pass_fd=None, seconds=16):
    kw = {"stdout": subprocess.PIPE, "stderr": subprocess.PIPE,
          "text": True, "timeout": seconds}
    if pass_fd is not None:
        kw["pass_fds"] = (pass_fd,)
    return subprocess.run([PYTHON, "-I", "-S", "-B", str(script)], **kw)

def candidate(probe_edit=None, launch_edit=None):
    workspace = Path(tempfile.mkdtemp(prefix="bb-finance-test-mut.", dir="/tmp"))
    probe = PROBE.read_text()
    launcher = LAUNCH.read_text()
    if probe_edit is not None:
        probe = probe_edit(probe)
    current = re.search(r'PROBE_SHA256 = "([0-9a-f]{64})"', launcher)
    check(current is not None, "probe pin missing")
    launcher = launcher.replace(current.group(1),
                                hashlib.sha256(probe.encode()).hexdigest(), 1)
    if launch_edit is not None:
        launcher = launch_edit(launcher)
    (workspace / PROBE.name).write_text(probe)
    target = workspace / LAUNCH.name
    target.write_text(launcher)
    return workspace, target

def negative(label, expected_error, probe_edit=None, launch_edit=None, fd=None):
    workspace, script = candidate(probe_edit, launch_edit)
    try:
        result = run(script, fd)
        check(result.returncode != 0 and "SYNTHETIC_LAUNCHER=PASS" not in
              result.stdout and expected_error in result.stderr,
              label + " did not fail closed for expected reason")
        print(label + "=PASS", flush=True)
    finally:
        shutil.rmtree(workspace)

def tamper_test():
    workspace, script = candidate()
    try:
        with (workspace / PROBE.name).open("a") as f:
            f.write("# mutated AFTER pinning\\n")
        result = run(script)
        check(result.returncode != 0 and
              "unreviewed probe bytes" in result.stderr and
              "SYNTHETIC_LAUNCHER=PASS" not in result.stdout,
              "probe hash check failed")
        print("REJECTS_TAMPERED_PROBE=PASS", flush=True)
    finally:
        shutil.rmtree(workspace)

def live(pid):
    try:
        line = Path(f"/proc/{pid}/stat").read_text()
    except (FileNotFoundError, ProcessLookupError):
        return False
    return line.rsplit(")", 1)[1].split()[0] not in ("Z", "X")

def assert_child_gone(pid):
    until = time.monotonic() + 3
    while live(pid) and time.monotonic() < until:
        time.sleep(.05)
    check(not live(pid), "timeout child survived signal")

def own_synthetic_dirs():
    return {p for p in Path("/tmp").glob(PREFIX + "*")
            if p.is_dir() and not p.is_symlink() and p.stat().st_uid == os.getuid()}

def reclaim_killed_synthetic(before):
    new = own_synthetic_dirs() - before
    check(len(new) == 1, "unexpected SIGKILL scratch residue count")
    for path in new:
        check(path.name.startswith(PREFIX) and
              (path / "evidence" / "bb-091-pre-migration-20260817.db").is_file()
              and (path / "probe.py").is_file(),
              "not a verified synthetic fixture")
        (path / "evidence").chmod(0o700)
        shutil.rmtree(path)
    check(own_synthetic_dirs() == before, "SIGKILL cleanup incomplete")
    print("OUT_OF_BAND_SYNTHETIC_SIGKILL_CLEANUP=PASS", flush=True)

def hold_probe(source):
    marker='print("SYNTHETIC_UID_PROBE=PASS", flush=True)'
    check(source.count(marker)==1, "hold marker unavailable")
    return source.replace(marker,
        'import time\nprint("SYNTHETIC_HOLD_READY", flush=True)\ntime.sleep(30)\n'
        +marker)

def signal_test(sig):
    before = own_synthetic_dirs()
    workspace, script = candidate(probe_edit=hold_probe)
    proc = None
    try:
        proc = subprocess.Popen([PYTHON, "-I", "-S", "-B", str(script)],
                                stdout=subprocess.PIPE, stderr=subprocess.PIPE,
                                bufsize=0, start_new_session=True)
        ready=False
        seen=b""
        limit=time.monotonic()+5
        while time.monotonic()<limit:
            readable,_,_=select.select([proc.stdout],[],[],.5)
            if readable:
                chunk=os.read(proc.stdout.fileno(),4096)
                seen += chunk
                if b"SYNTHETIC_HOLD_READY" in seen:
                    ready=True
                    break
            if proc.poll() is not None: break
        check(ready, "hold probe never ready")
        children=Path(f"/proc/{proc.pid}/task/{proc.pid}/children").read_text().split()
        check(len(children)==1, "unexpected launcher process tree")
        timeout_pid=int(children[0])
        child_path=Path(f"/proc/{timeout_pid}/task/{timeout_pid}/children")
        bwrap_children = [int(p) for p in child_path.read_text().split()]
        check(bwrap_children, "timeout has no Bubblewrap child")
        os.kill(proc.pid, sig)
        proc.wait(timeout=4)
        assert_child_gone(timeout_pid)
        for child in bwrap_children:
            assert_child_gone(child)
        check(proc.returncode != 0, "signal test falsely succeeded")
        print(f"SIGNAL_{signal.Signals(sig).name}=PASS", flush=True)
        if sig == signal.SIGKILL:
            reclaim_killed_synthetic(before)
        else:
            check(own_synthetic_dirs() == before, "signal leaked scratch")
    finally:
        if proc is not None and proc.poll() is None:
            proc.kill()
            proc.wait(timeout=2)
        shutil.rmtree(workspace)

def timeout_test():
    before = own_synthetic_dirs()
    workspace, script = candidate(probe_edit=hold_probe)
    try:
        result = run(script, seconds=18)
        check(result.returncode != 0 and
              "SYNTHETIC_HOLD_READY" in result.stdout and
              "SYNTHETIC_LAUNCHER=PASS" not in result.stdout,
              "timeout failed closed")
        check(own_synthetic_dirs() == before, "timeout left synthetic scratch")
        print("HARD_TIMEOUT_AND_CLEANUP=PASS", flush=True)
    finally:
        shutil.rmtree(workspace)

if __name__ == "__main__":
    check(os.geteuid() != 0, "root forbidden")
    initial=own_synthetic_dirs()
    positive=run(LAUNCH)
    check(positive.returncode == 0 and "SYNTHETIC_LAUNCHER=PASS" in positive.stdout
          and "AUDIT_UID_EXECUTION=BLOCKED" in positive.stdout,
          "normal synthetic run failed")
    print("SYNTHETIC_CURRENT_UID=PASS", flush=True)

    bad_args=subprocess.run([PYTHON,"-I","-S","-B",str(LAUNCH),"--audit"],
                            capture_output=True,text=True,timeout=4)
    check(bad_args.returncode != 0, "unexpected arguments accepted")
    print("REJECTS_ARGUMENTS=PASS", flush=True)

    negative("REJECTS_RW_BIND", "synthetic evidence mount not read-only",
             launch_edit=lambda s:
             s.replace('"--ro-bind", str(evidence), "/evidence"',
                       '"--bind", str(evidence), "/evidence"'))
    negative("REJECTS_RLIMIT_OVERRIDE", "resource limit missing",
             launch_edit=lambda s:
             s.replace('"--nofile=64:64"', '"--nofile=128:128"'))
    tamper_test()
    fd=os.open("/dev/null", os.O_RDONLY)
    try:
        if fd != 8:
            os.dup2(fd, 8)
            os.close(fd)
            fd=8
        os.set_inheritable(fd, True)
        good=run(LAUNCH, fd)
        check(good.returncode==0 and "SYNTHETIC_FD_CLOSURE=PASS" in good.stdout,
              "intentional host FD not closed")
        print("INHERITED_FD_CLOSED=PASS", flush=True)
        negative("REJECTS_UNCLOSED_FD", "inherited host file descriptors",
                 launch_edit=lambda s:
                 s.replace("close_fds=True", "close_fds=False"), fd=fd)
    finally:
        os.close(fd)

    for sig in (signal.SIGTERM, signal.SIGHUP, signal.SIGINT, signal.SIGKILL):
        signal_test(sig)
    timeout_test()
    check(own_synthetic_dirs() == initial, "unexpected leftover synthetic directories")
    print("SYNTHETIC_NEGATIVE_MATRIX=PASS", flush=True)
