#!/usr/bin/python3
"""One inert owned child in one systemd-delegated subtree. No Finance/model commands.

Requires the exact accompanying unit, root installation and an explicit local start.
Results are private, invocation-bound and persistent. No arbitrary path/command options.
"""

import hashlib
import json
import os
from pathlib import Path
import re
import select
import signal
import stat
import sys
from datetime import datetime, timezone

UNIT = "bigbrain-historical-maintenance.service"
RELATIVE = "/system.slice/" + UNIT
CGROUP = Path("/sys/fs/cgroup" + RELATIVE)
STATE = Path("/var/lib/bigbrain-historical-preflight")
LIMITS = {
    "cpu.max": "200000 100000",
    "memory.max": "4294967296",
    "memory.swap.max": "0",
    "pids.max": "32",
    "memory.oom.group": "1",
}


class Refused(Exception):
    pass


def require(condition, category):
    if not condition:
        raise Refused(category)


def read(path):
    with open(path, encoding="ascii") as stream:
        result = stream.read(65537)
    require(len(result) <= 65536, "OversizedEvidence")
    return result.strip()


def write_control(path, value):
    fd = os.open(path, os.O_WRONLY | os.O_CLOEXEC | os.O_NOFOLLOW)
    try:
        data = value.encode("ascii")
        require(os.write(fd, data) == len(data), "ControlWriteIncomplete")
    finally:
        os.close(fd)


def identity(status):
    fields = dict(line.split(":", 1) for line in status.splitlines() if ":" in line)
    require(fields.get("NoNewPrivs", "").strip() == "1", "NoNewPrivilegesMissing")
    for field in ("CapInh", "CapPrm", "CapEff", "CapBnd", "CapAmb"):
        require(fields.get(field, "").strip() == "0000000000000000", "CapabilitiesPresent")
    for field in ("Uid", "Gid"):
        require(fields.get(field, "").split() == ["1654"] * 4, "WrongIdentity")
    require(set(fields.get("Groups", "").split()) <= {"1654"}, "UnexpectedSupplementaryGroup")


def private_directory(path):
    require(path.resolve() == path, "SymlinkDirectory")
    info = path.lstat()
    require(stat.S_ISDIR(info.st_mode) and info.st_uid == 1654 and info.st_gid == 1654
            and stat.S_IMODE(info.st_mode) == 0o700, "PrivateDirectoryInvalid")


def protected_file(path):
    require(path.resolve() == path, "SymlinkArtifact")
    for parent in (path, *path.parents):
        info = parent.lstat()
        require(info.st_uid == 0 and not info.st_mode & 0o022, "InstallationNotProtected")
    require(path.is_file(), "InstallationNotRegular")


def mount_policy(text):
    """Reject every writable filesystem except this unit's explicit disposable/private scopes."""
    found_cgroup = False
    for line in text.splitlines():
        left, right = line.split(" - ", 1)
        fields = left.split()
        mount = fields[4]
        options = fields[5].split(",")
        filesystem = right.split()[0]
        if "rw" not in options:
            continue
        allowed = (
            filesystem == "cgroup2" and mount == str(CGROUP)
            or mount == str(STATE)
            or filesystem == "tmpfs" and mount in ("/tmp", "/var/tmp", "/dev", "/dev/shm")
            or filesystem == "devpts" and mount == "/dev/pts"
            or filesystem == "mqueue" and mount == "/dev/mqueue"
            or filesystem == "proc" and mount == "/proc"
        )
        require(allowed, "UnexpectedWritableMount")
        found_cgroup |= filesystem == "cgroup2" and mount == str(CGROUP)
    require(found_cgroup, "DelegatedMountMissing")


def own_child(move, verify):
    """Identity-bound termination/reap even when verification throws; no external executable."""
    reader, writer = os.pipe2(os.O_CLOEXEC)
    pid = None
    pidfd = None
    try:
        pid = os.fork()
        if pid == 0:
            os.close(reader)
            signal.signal(signal.SIGTERM, signal.SIG_DFL)
            signal.signal(signal.SIGINT, signal.SIG_DFL)
            try:
                move()
                os.write(writer, b"R")
                os.close(writer)
                while True:
                    signal.pause()  # inert child only; parent/systemd impose finite deadlines
            except BaseException:
                os._exit(1)
        os.close(writer)
        writer = None
        # No SIGCHLD handler/reaper: an exited child stays owned (zombie) until waitpid below.
        pidfd = os.pidfd_open(pid)
        ready, _, _ = select.select([reader], [], [], 2)
        require(ready and os.read(reader, 1) == b"R", "ChildMigrationFailed")
        verify(pid)
    finally:
        os.close(reader)
        if writer is not None:
            os.close(writer)
        if pid is not None and pid > 0:
            if pidfd is None:
                # No PID fallback. The dedicated unit's KillMode=control-group contains this failure.
                raise Refused("ChildIdentityUnavailable")
            else:
                try:
                    signal.pidfd_send_signal(pidfd, signal.SIGKILL)
                    done, _, _ = select.select([pidfd], [], [], 2)
                    require(done, "ChildReapUncertain")
                    require(os.waitpid(pid, 0)[0] == pid, "ChildReapUncertain")
                finally:
                    os.close(pidfd)


def probe():
    require(os.getuid() == os.geteuid() == os.getgid() == os.getegid() == 1654, "WrongIdentity")
    identity(read("/proc/self/status"))
    require(read("/proc/self/cgroup") == "0::" + RELATIVE + "/controller", "WrongDelegationTopology")
    mount_policy(read("/proc/self/mountinfo"))
    require(CGROUP.resolve() == CGROUP, "SymlinkDelegation")
    require(CGROUP.stat().st_uid == 1654 and CGROUP.stat().st_gid == 1654, "DelegationNotOwned")
    require(read(CGROUP / "cgroup.type") == "domain", "WrongCgroupType")
    require(read(CGROUP / "cgroup.procs") == "", "DelegatedParentPopulated")
    controller = CGROUP / "controller"
    require(read(controller / "cgroup.procs") == str(os.getpid()), "UnexpectedControllerProcess")
    children = {p.name for p in CGROUP.iterdir() if p.is_dir()}
    require(children == {"controller"}, "ExistingOrUnexpectedChildGroup")
    require({"cpu", "memory", "pids"} <= set(read(CGROUP / "cgroup.controllers").split()),
            "ControllersUnavailable")
    write_control(CGROUP / "cgroup.subtree_control", "+cpu +memory +pids")
    worker = CGROUP / "worker"
    worker.mkdir(mode=0o700)  # exclusive; never adopt or empty another worker group
    try:
        private_directory(worker)
        require(read(worker / "cgroup.procs") == "", "WorkerPopulated")
        for name, value in LIMITS.items():
            write_control(worker / name, value)
            require(read(worker / name) == value, "ResourceLimitMismatch")

        def move():
            write_control(worker / "cgroup.procs", str(os.getpid()))

        def verify(pid):
            require(read(controller / "cgroup.procs") == str(os.getpid()), "ParentMoved")
            require(read(worker / "cgroup.procs") == str(pid), "ChildNotInWorker")
            require(read(f"/proc/{pid}/cgroup") == "0::" + RELATIVE + "/worker", "ChildTopologyMismatch")
            identity(read(f"/proc/{pid}/status"))

        own_child(move, verify)
        require(read(worker / "cgroup.procs") == "", "WorkerNotEmptyAfterReap")
        require(read(controller / "cgroup.procs") == str(os.getpid()), "ParentMoved")
    finally:
        # Never kill/relocate unknown processes. On uncertainty systemd owns whole-unit cleanup.
        if read(worker / "cgroup.procs") == "":
            worker.rmdir()
    return {"topology": RELATIVE + "/{controller,worker}", "limits": LIMITS,
            "ownedChildMigration": "PASS", "killReapEmpty": "PASS", "mountPolicy": "PASS"}


def main():
    result = {"contract": "FIN-OPS-G3-20261016/preflight-v1", "status": "FAIL"}
    pending = None
    stream = None
    try:
        require(len(sys.argv) == 1, "ArgumentsForbidden")
        invocation = os.environ.get("INVOCATION_ID", "")
        require(re.fullmatch("[0-9a-f]{32}", invocation) is not None, "InvocationIdentityMissing")
        private_directory(STATE)
        protected_file(Path(__file__))
        protected_file(Path("/etc/systemd/system") / UNIT)
        result.update(invocation=invocation, startedUtc=datetime.now(timezone.utc).isoformat(),
                      probeSha256=hashlib.sha256(Path(__file__).read_bytes()).hexdigest(),
                      unitSha256=hashlib.sha256((Path("/etc/systemd/system") / UNIT).read_bytes()).hexdigest())
        destination = STATE / (invocation + ".pending")
        fd = os.open(destination, os.O_WRONLY | os.O_CREAT | os.O_EXCL | os.O_NOFOLLOW, 0o600)
        stream = os.fdopen(fd, "w")
        pending = destination  # only our newly created file may be updated
        json.dump(result, stream); stream.flush(); os.fsync(stream.fileno())
        result.update(probe(), status="PASS")
    except Refused as error:
        result["category"] = str(error)
    except Exception:
        result["category"] = "PreflightSystemFailure"  # no paths, exception text or environment
    finally:
        if stream is not None:
            try:
                info = os.fstat(stream.fileno())
                actual = pending.lstat()
                require((info.st_dev, info.st_ino) == (actual.st_dev, actual.st_ino)
                        and info.st_nlink == 1, "ResultReplaced")
                result["completedUtc"] = datetime.now(timezone.utc).isoformat()
                stream.seek(0); stream.truncate()
                json.dump(result, stream, sort_keys=True); stream.write("\n")
                stream.flush(); os.fsync(stream.fileno())
                # Unique invocation; hard link fails instead of replacing any previous result.
                os.link(pending, pending.with_suffix(".json"), follow_symlinks=False)
                pending.unlink()
                fd = os.open(STATE, os.O_RDONLY | os.O_DIRECTORY)
                try:
                    os.fsync(fd)
                finally:
                    os.close(fd)
            except Exception:
                result.update(status="FAIL", category="ResultPersistenceFailed")
            finally:
                stream.close()
    print("HistoricalIsolationPreflight:" + result["status"] + ":" + result.get("category", "Verified"))
    return 0 if result["status"] == "PASS" else 1


if __name__ == "__main__":
    def interrupted(_number, _frame):
        raise Refused("Interrupted")
    signal.signal(signal.SIGTERM, interrupted)
    signal.signal(signal.SIGINT, interrupted)
    sys.exit(main())
