#!/usr/bin/env python3
"""Bounded synthetic TEST tasks; install reviewed code outside Git before use.

Fixed ops layout (owner-created, never candidate-controlled):
  ~/.local/lib/bigbrain-test-task/{test-environment-task.py,config.json}
  ~/.local/state/bigbrain-test-task/ (0700; pending.json, tasks/, PAUSED)
config.json exact keys: version=1, trusted_base=<accepted commit SHA>,
workflow_sha256=<SHA256 of that base's .github/workflows/ci.yml>,
wrapper_sha256=<reviewed wrapper hash>, controller=<absolute sealed rootless
installation>, controller_files={test-environment.py,compose.test.yaml,EMPTY,
installation.json: SHA256}, bind_ip=<installed allowlisted Tailscale IPv4>.

submit reads one manifest from stdin, then starts the fixed user unit. Manifest:
task_id (32 lowercase hex), source_sha/tree (40 hex), run_id/attempt/artifact_id
(positive integers), pr_number (positive for PR, zero for dispatch), runtime_seconds (30..1800). No paths, commands or env.
Run is one bounded task, never a poller. A crash requires inspected recovery;
resume cannot clear an unfinished receipt. First task requires an empty slot.
Successful stopped tasks become last-known-good; rollback restores code/package
with FRESH synthetic fixtures, never a database undo. Rootful is never selected.
"""
import contextlib
import fcntl
import gzip
import hashlib
import ipaddress
import json
import os
from pathlib import Path, PurePosixPath
import pwd
import tempfile
import re
import shutil
import signal
import stat
import subprocess
import sys
import tarfile
import threading
import time
import uuid
import zipfile

HOME = Path(pwd.getpwuid(os.getuid()).pw_dir)
INSTALL = HOME / ".local/lib/bigbrain-test-task"
STATE = HOME / ".local/state/bigbrain-test-task"
UNIT = "bigbrain-test-task.service"
REPO = "T-bear/BigBrain"
OWNER = "T-bear"
WORKFLOW = ".github/workflows/ci.yml"
SHA = re.compile(r"[0-9a-f]{40}\Z")
HASH = re.compile(r"[0-9a-f]{64}\Z")
IMAGE = re.compile(r"sha256:[0-9a-f]{64}\Z")
TASK = re.compile(r"[0-9a-f]{32}\Z")
BRANCH = re.compile(r"checkpoint/[A-Za-z0-9][A-Za-z0-9._/-]*\Z")
FILES = {"images.tar.gz", "images.json", "build.json", "package.json", "SHA256SUMS"}
CONTROL = {"test-environment.py", "compose.test.yaml", "EMPTY", "installation.json"}
MAX_ZIP = 2 * 1024**3
MAX_EXPANDED = 4 * 1024**3
MAX_TAR = 12 * 1024**3
MAX_STATE = 16 * 1024**3
TERMINAL = {"succeeded", "rejected", "aborted", "rolled-back"}
CANCEL = threading.Event()


class Rejected(Exception):
    pass


class Aborted(Rejected):
    pass


def require(ok, reason):
    if not ok:
        raise Rejected(reason)


def pairs(items):
    result = {}
    for key, value in items:
        require(key not in result, "duplicate JSON key")
        result[key] = value
    return result


def decode(raw):
    return json.loads(raw, object_pairs_hook=pairs)


def private(path, directory=False):
    info = path.lstat()
    require(info.st_uid == os.getuid() and not stat.S_IMODE(info.st_mode) & 0o077,
            "ops path must be private and owned")
    require(stat.S_ISDIR(info.st_mode) if directory else stat.S_ISREG(info.st_mode),
            "ops path type rejected")
    return info


def outside_git(path):
    require(path.is_absolute() and path == path.resolve(), "absolute nonsymlink ops path required")
    require(not any((p / ".git").exists() for p in (path, *path.parents)),
            "control/state inside Git rejected")


def read(path, limit=1024 * 1024):
    info = private(path)
    require(info.st_size <= limit, "metadata too large")
    return path.read_bytes()


def document(path):
    return decode(read(path))


def sha_file(path):
    private(path)
    with path.open("rb") as stream:
        return hashlib.file_digest(stream, "sha256").hexdigest()


def atomic(path, value):
    # Only private operator directories; caller holds task lock except PAUSED.
    private(path.parent, directory=True)
    if path.exists() or path.is_symlink():
        private(path)
    temp = path.parent / (".atomic-" + uuid.uuid4().hex)
    try:
        fd = os.open(temp, os.O_WRONLY | os.O_CREAT | os.O_EXCL | os.O_NOFOLLOW, 0o600)
        with os.fdopen(fd, "w") as out:
            json.dump(value, out, sort_keys=True)
            out.write("\n")
            out.flush()
            os.fsync(out.fileno())
        os.replace(temp, path)
        syncdir(path.parent)
    finally:
        if temp.exists():
            temp.unlink()


def syncdir(path):
    fd = os.open(path, os.O_RDONLY | os.O_DIRECTORY | os.O_NOFOLLOW)
    try:
        os.fsync(fd)
    finally:
        os.close(fd)


@contextlib.contextmanager
def lock():
    private(STATE, directory=True)
    fd = os.open(STATE / "lock", os.O_WRONLY | os.O_CREAT | os.O_NOFOLLOW, 0o600)
    try:
        info = os.fstat(fd)
        require(stat.S_ISREG(info.st_mode) and info.st_uid == os.getuid() and
                not stat.S_IMODE(info.st_mode) & 0o077, "unsafe task lock")
        try:
            fcntl.flock(fd, fcntl.LOCK_EX | fcntl.LOCK_NB)
        except BlockingIOError:
            raise Rejected("task busy") from None
        yield
    finally:
        os.close(fd)


def manifest(value):
    keys = {"task_id", "source_sha", "tree", "run_id", "attempt", "artifact_id", "pr_number", "runtime_seconds"}
    require(isinstance(value, dict) and set(value) == keys, "manifest fields rejected")
    require(isinstance(value["task_id"], str) and TASK.fullmatch(value["task_id"]), "task ID rejected")
    for key in ("source_sha", "tree"):
        require(isinstance(value[key], str) and SHA.fullmatch(value[key]), "exact source/tree required")
    for key in ("run_id", "attempt", "artifact_id"):
        require(type(value[key]) is int and 1 <= value[key] <= 10**15, "numeric GitHub identity required")
    require(type(value["pr_number"]) is int and 0 <= value["pr_number"] <= 10**9,
            "numeric PR identity required")
    require(type(value["runtime_seconds"]) is int and 30 <= value["runtime_seconds"] <= 1800,
            "runtime outside fixed bounds")
    return value


def config():
    require(Path(__file__).absolute() == INSTALL / "test-environment-task.py",
            "run only installed ops wrapper")
    for path in (INSTALL, STATE, STATE / "tasks"):
        outside_git(path)
        private(path, directory=True)
    cfg = document(INSTALL / "config.json")
    require(set(cfg) == {"version", "trusted_base", "workflow_sha256", "wrapper_sha256",
                        "controller", "controller_files", "bind_ip"} and cfg["version"] == 1,
            "installed config fields rejected")
    require(SHA.fullmatch(cfg["trusted_base"]) and HASH.fullmatch(cfg["workflow_sha256"]) and
            HASH.fullmatch(cfg["wrapper_sha256"]), "installed identities rejected")
    require(sha_file(INSTALL / "test-environment-task.py") == cfg["wrapper_sha256"],
            "wrapper drift")
    private(INSTALL / "test-environment-task.py")
    root = Path(cfg["controller"])
    outside_git(root)
    require(root != INSTALL and root != STATE and STATE not in root.parents and root not in STATE.parents,
            "separate controller installation required")
    private(root, directory=True)
    require(set(cfg["controller_files"]) == CONTROL, "controller seal incomplete")
    for name, expected in cfg["controller_files"].items():
        require(isinstance(expected, str) and HASH.fullmatch(expected), "invalid control digest")
        private(root / name)
        require(sha_file(root / name) == expected, "installed controller drift")
    seal = document(root / "installation.json")
    engine = seal.get("engine", {})
    require(set(engine) == {"mode", "uid", "id"} and engine["mode"] == "rootless" and
            engine["uid"] == os.getuid() and isinstance(engine["id"], str) and engine["id"],
            "sealed rootless engine required")
    require(seal.get("files") == {k: cfg["controller_files"][k] for k in CONTROL - {"installation.json"}},
            "controller seal hash mismatch")
    address = str(ipaddress.IPv4Address(cfg["bind_ip"]))
    require(ipaddress.IPv4Address(address) in ipaddress.IPv4Network("100.64.0.0/10") and
            address in seal["allowed_clients"], "installed bind/client policy mismatch")
    private(root / "client", directory=True)
    require((root / "EMPTY").read_bytes() == b"" and not list((root / "client").iterdir()),
            "controller credential/config rejected")
    return cfg


def base_env(home):
    return {"PATH": "/usr/bin:/bin", "HOME": str(home), "LANG": "C.UTF-8"}


def gh_env():
    # Existing private gh auth only. No inherited GH_TOKEN/GITHUB_TOKEN/proxy/SSH.
    env = base_env(HOME)
    env.update(GH_CONFIG_DIR=str(HOME / ".config/gh"), GH_PROMPT_DISABLED="1", GH_PAGER="cat")
    return env


def control_env(cfg):
    root = Path(cfg["controller"])
    env = base_env(root / "client")
    env.update(DOCKER_CONFIG=str(root / "client"),
               DOCKER_HOST=f"unix:///run/user/{os.getuid()}/bigbrain-test-rootless/docker.sock")
    return env


def terminate(child):
    if child.poll() is None:
        try:
            os.killpg(child.pid, signal.SIGTERM)
        except ProcessLookupError:
            pass
        try:
            child.wait(timeout=5)
        except subprocess.TimeoutExpired:
            try:
                os.killpg(child.pid, signal.SIGKILL)
            except ProcessLookupError:
                pass
            child.wait(timeout=5)


def process(argv, env, cwd, timeout=120, output=None, limit=1024 * 1024, interruptible=True):
    # Regular private file avoids unbounded PIPE/capture_output and credential logs.
    with tempfile.TemporaryFile(dir=STATE) as temp:
        child = subprocess.Popen(argv, cwd=cwd, env=env, stdin=subprocess.DEVNULL,
                                 stdout=temp, stderr=subprocess.DEVNULL, start_new_session=True)
        deadline = time.monotonic() + timeout
        try:
            while child.poll() is None:
                if interruptible:
                    checkpoint()
                require(time.monotonic() < deadline, "bounded command deadline")
                require(os.fstat(temp.fileno()).st_size <= limit, "command output limit")
                CANCEL.wait(0.1) if interruptible else time.sleep(0.1)
            require(child.returncode == 0, "external command rejected; raw output private")
            size = os.fstat(temp.fileno()).st_size
            require(size <= limit, "command output limit")
            temp.seek(0)
            if output:
                with output.open("xb") as target:
                    os.chmod(output, 0o600)
                    shutil.copyfileobj(temp, target, 1024 * 1024)
                    target.flush()
                    os.fsync(target.fileno())
                return None
            return temp.read(limit + 1)
        finally:
            terminate(child)


def gh(endpoint, raw=False, output=None):
    require(endpoint.startswith("repos/" + REPO + "/") and not any(x in endpoint for x in ("..", "\\", "\n")),
            "GitHub endpoint rejected")
    argv = ["/usr/bin/gh", "api", "--hostname", "github.com", "-H",
            "Accept: application/vnd.github.raw+json" if raw else "Accept: application/vnd.github+json", endpoint]
    data = process(argv, gh_env(), STATE, timeout=300 if output else 60,
                   output=output, limit=MAX_ZIP if output else 2 * 1024 * 1024)
    return data if raw or output else decode(data)


def workflow_at(sha, expected):
    require(SHA.fullmatch(sha), "workflow revision rejected")
    data = gh(f"repos/{REPO}/contents/{WORKFLOW}?ref={sha}", raw=True)
    require(hashlib.sha256(data).hexdigest() == expected, "workflow differs from reviewed trusted base")


def github_identity(task, cfg):
    run = gh(f"repos/{REPO}/actions/runs/{task['run_id']}/attempts/{task['attempt']}")
    require(run["id"] == task["run_id"] and run["run_attempt"] == task["attempt"] and
            run["status"] == "completed" and run["conclusion"] in ("success", "failure"),
            "incomplete or wrong GitHub run")
    require(run["repository"]["full_name"] == REPO and run["head_repository"]["full_name"] == REPO and
            run["actor"]["login"] == OWNER and run["triggering_actor"]["login"] == OWNER,
            "GitHub owner/repository rejected")
    branch = run["head_branch"]
    require(BRANCH.fullmatch(branch) and ".." not in branch and "//" not in branch and
            run["head_sha"] == task["source_sha"] and run["path"] == WORKFLOW,
            "run source/workflow mismatch")
    require(run["event"] in ("pull_request", "workflow_dispatch"), "run event rejected")
    workflow = gh(f"repos/{REPO}/actions/workflows/{run['workflow_id']}")
    require(workflow["path"] == WORKFLOW and workflow["id"] == run["workflow_id"] and
            workflow["state"] == "active", "workflow registration mismatch")
    ref = gh(f"repos/{REPO}/git/ref/heads/{branch}")
    require(ref["object"]["sha"] == task["source_sha"], "checkpoint branch moved")
    commit = gh(f"repos/{REPO}/commits/{task['source_sha']}")
    require(commit["sha"] == task["source_sha"] and commit["author"]["login"] == OWNER and
            commit["commit"]["tree"]["sha"] == task["tree"], "source author/tree mismatch")
    if run["event"] == "pull_request":
        require(task["pr_number"] > 0, "explicit PR identity required")
        # GitHub may omit closed/otherwise unassociated PRs from run.pull_requests.
        # Bind the explicit numeric PR against the run's exact owned branch/head.
        prs = run["pull_requests"]
        require(not prs or (len(prs) == 1 and prs[0]["number"] == task["pr_number"]),
                "run PR identity mismatch")
        pr = gh(f"repos/{REPO}/pulls/{task['pr_number']}")
        require(pr["user"]["login"] == OWNER and pr["head"]["repo"]["full_name"] == REPO and
                pr["head"]["sha"] == task["source_sha"] and pr["head"]["ref"] == branch,
                "PR owner/head mismatch")
        tested = pr["merge_commit_sha"]
        require(isinstance(tested, str) and SHA.fullmatch(tested), "tested merge SHA unavailable")
    else:
        require(task["pr_number"] == 0, "dispatch cannot select a PR")
        # Dispatch must itself execute on this candidate, not a different workflow ref.
        tested = task["source_sha"]
    workflow_at(cfg["trusted_base"], cfg["workflow_sha256"])
    workflow_at(task["source_sha"], cfg["workflow_sha256"])
    workflow_at(tested, cfg["workflow_sha256"])
    jobs = gh(f"repos/{REPO}/actions/runs/{task['run_id']}/attempts/{task['attempt']}/jobs?per_page=100")
    require(jobs["total_count"] == len(jobs["jobs"]) == 5, "unexpected or paginated jobs")
    outcomes = {}
    package_job = None
    for job in jobs["jobs"]:
        name = job["name"]
        require(name not in outcomes and job["status"] == "completed" and job["run_id"] == task["run_id"] and
                job["head_sha"] == task["source_sha"], "job identity/status mismatch")
        outcomes[name] = job["conclusion"]
        if name == "TEST candidate package (not a required acceptance check)":
            package_job = job
    require(set(outcomes) == {"backend", "frontend", "documentation", "secrets",
                             "TEST candidate package (not a required acceptance check)"},
            "job names mismatch")
    require(outcomes["secrets"] == "success" and package_job["conclusion"] == "success",
            "secrets and package must pass")
    require(all(outcomes[k] in ("success", "failure") for k in ("backend", "frontend", "documentation")),
            "functional jobs must finish truthfully")
    for name in ("Resolve owned exact source before running candidate code",
                 "Build both exact-source images (mandatory even when tests are red)",
                 "Save images and exact provenance"):
        matches = [s for s in package_job["steps"] if s["name"] == name]
        require(len(matches) == 1 and matches[0]["conclusion"] == "success", "package step not successful")
    artifact = gh(f"repos/{REPO}/actions/artifacts/{task['artifact_id']}")
    require(artifact["id"] == task["artifact_id"] and artifact["expired"] is False and
            type(artifact["size_in_bytes"]) is int and 0 < artifact["size_in_bytes"] <= MAX_ZIP and
            artifact["name"] == f"test-candidate-{task['source_sha']}-{task['run_id']}-{task['attempt']}",
            "artifact identity/size mismatch")
    arun = artifact["workflow_run"]
    require(arun["id"] == task["run_id"] and arun["head_sha"] == task["source_sha"] and
            arun["head_branch"] == branch, "artifact run mismatch")
    require(IMAGE.fullmatch(artifact.get("digest", "")), "GitHub artifact digest required")
    return {"ref": "refs/heads/" + branch, "tested_sha": tested,
            "tests": {k: outcomes[k] for k in ("backend", "frontend", "documentation", "secrets")},
            "artifact_digest": artifact["digest"]}


def unpack_zip(archive, target):
    require(archive.stat().st_size <= MAX_ZIP, "ZIP too large")
    with zipfile.ZipFile(archive) as bundle:
        entries = bundle.infolist()
        require(len(entries) == 5 and {e.filename for e in entries} == FILES,
                "ZIP entry allowlist/duplicates")
        total = 0
        for item in entries:
            mode = item.external_attr >> 16
            require(not item.is_dir() and not item.flag_bits & 1 and
                    (stat.S_IFMT(mode) in (0, stat.S_IFREG)), "ZIP type/encryption rejected")
            require(0 <= item.file_size <= MAX_EXPANDED and
                    item.file_size <= max(1, item.compress_size) * 200, "ZIP expansion ratio")
            total += item.file_size
        require(total <= MAX_EXPANDED, "ZIP expanded limit")
        for item in entries:
            limit = MAX_EXPANDED if item.filename == "images.tar.gz" else 1024 * 1024
            require(item.file_size <= limit, "ZIP metadata limit")
            with bundle.open(item) as source, (target / item.filename).open("xb") as out:
                os.chmod(target / item.filename, 0o600)
                copy_bounded(source, out, min(limit, item.file_size))
                out.flush()
                os.fsync(out.fileno())


def copy_bounded(source, out, limit):
    total = 0
    while True:
        checkpoint()
        chunk = source.read(min(1024 * 1024, limit - total + 1))
        if not chunk:
            return total
        total += len(chunk)
        require(total <= limit, "expanded stream limit")
        out.write(chunk)


class LimitedReader:
    def __init__(self, source, maximum):
        self.source, self.remaining = source, maximum

    def read(self, size=-1):
        checkpoint()
        size = min(size if size >= 0 else self.remaining + 1, self.remaining + 1)
        data = self.source.read(size)
        self.remaining -= len(data)
        require(self.remaining >= 0, "tar expanded limit")
        return data


class SafeTarInfo(tarfile.TarInfo):
    def _proc_member(self, archive):
        # Reject extended headers BEFORE tarfile allocates their declared payload.
        require(self.type in (tarfile.REGTYPE, tarfile.AREGTYPE, tarfile.DIRTYPE),
                "tar extended/link/sparse header rejected")
        return super()._proc_member(archive)


def inspect_tar(path):
    # Never extract Docker's archive. Stream through bounded gzip INCLUDING padding
    # and trailing bytes, rejecting links, sparse files, duplicate paths and bombs.
    seen = set()
    with gzip.open(path, "rb") as raw:
        stream = LimitedReader(raw, MAX_TAR)
        with tarfile.open(fileobj=stream, mode="r|", tarinfo=SafeTarInfo) as archive:
            total = 0
            for item in archive:
                name = item.name
                parts = PurePosixPath(name).parts
                require(name and not name.startswith("/") and "\\" not in name and
                        ".." not in parts and "." not in name.split("/") and name.rstrip("/") == str(PurePosixPath(name)) and
                        str(PurePosixPath(name)) not in seen and
                        (item.isfile() or item.isdir()) and not item.issparse(),
                        "tar member type/path/duplicate rejected")
                require(item.size >= 0 and item.size <= MAX_TAR, "tar member size")
                seen.add(str(PurePosixPath(name)))
                total += item.size
                require(len(seen) <= 100000 and total <= MAX_TAR, "tar count/size limit")
            # Read through tarfile's buffered stream, not around its read-ahead.
            while True:
                tail = archive.fileobj.read(1024 * 1024)
                if not tail:
                    break
                require(not tail.strip(bytes([0])), "nonzero trailing/concatenated tar rejected")


def verify_package(folder, task, identity):
    names = FILES - {"SHA256SUMS"}
    lines = read(folder / "SHA256SUMS", 1024).decode("ascii").splitlines()
    checks = {}
    for line in lines:
        require(re.fullmatch(r"[0-9a-f]{64}  [A-Za-z0-9.]+", line), "checksum syntax")
        value, name = line.split("  ")
        require(name in names and name not in checks, "checksum entry/duplicate")
        checks[name] = value
    require(set(checks) == names, "checksum coverage")
    for name, value in checks.items():
        require(sha_file(folder / name) == value, "package checksum mismatch")
    package = document(folder / "package.json")
    require(package["source_sha"] == task["source_sha"] and package["tree"] == task["tree"] and
            package["repository"] == REPO and package["trusted_owner"] is True and
            package["ref"] == identity["ref"] and package["tested_sha"] == identity["tested_sha"] and
            package["workflow_run"] == str(task["run_id"]) and
            package["workflow_attempt"] == str(task["attempt"]) and
            package["tests"] == identity["tests"], "package provenance mismatch")
    require(package["checksums"] == {k: checks[k] for k in names - {"package.json"}},
            "nested checksum coverage/mismatch")
    require(package["build"] == document(folder / "build.json"), "build metadata mismatch")
    images = document(folder / "images.json")
    require(isinstance(images, list) and len(images) == 2 and set(package["images"]) == {"api", "web"},
            "two exact images required")
    for kind, image in zip(("api", "web"), images):
        value = package["images"][kind]
        require(isinstance(value, str) and IMAGE.fullmatch(value) and image["Id"] == value and
                image["Config"]["Labels"]["org.opencontainers.image.revision"] == task["source_sha"] and
                image["Config"]["Labels"]["io.bigbrain.test.kind"] == kind, "image identity mismatch")
    require(package["images"]["api"] != package["images"]["web"], "distinct images required")
    inspect_tar(folder / "images.tar.gz")
    return package


def checkpoint():
    if CANCEL.is_set() or (STATE / "PAUSED").exists() or (STATE / "PAUSED").is_symlink():
        raise Aborted("task paused or terminated")


def unfinished():
    tasks = STATE / "tasks"
    children = list(tasks.iterdir())
    require(len(children) <= 64, "task retention cap; operator maintenance required")
    for path in children:
        require(TASK.fullmatch(path.name), "unknown task directory")
        private(path, directory=True)
        receipt = path / "receipt.json"
        require(receipt.exists(), "interrupted claim; manual recovery required")
        require(document(receipt)["phase"] in TERMINAL, "unfinished task; manual recovery required")


def budget():
    total = 0
    for root, dirs, files in os.walk(STATE, followlinks=False):
        for name in dirs:
            private(Path(root) / name, directory=True)
        for name in files:
            info = private(Path(root) / name)
            total += info.st_size
    require(total + MAX_EXPANDED + MAX_ZIP <= MAX_STATE, "ops storage cap; operator maintenance required")
    require(shutil.disk_usage(STATE).free >= MAX_EXPANDED + MAX_ZIP + 1024**3, "insufficient staging space")


class Runner:
    def __init__(self, task, cfg, folder):
        self.task, self.cfg, self.folder = task, cfg, folder
        self.relay = None
        self.mutating = False
        self.previous = None
        self.package = None
        self.identity = None

    def receipt(self, phase, **extra):
        value = {"task": self.task, "phase": phase, "at": time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime()),
                 "control": self.cfg["controller_files"], **extra}
        if self.identity is not None:
            value["github"] = self.identity
        atomic(self.folder / "receipt.json", value)

    def control(self, command, *args, cleanup=False):
        # Recheck fixed installation and private credential-free environment each time.
        require(config() == self.cfg, "ops config changed")
        return process(["/usr/bin/python3", "-I", str(Path(self.cfg["controller"]) / "test-environment.py"),
                        command, *args], control_env(self.cfg), Path(self.cfg["controller"]),
                       timeout=300, interruptible=not cleanup)

    def docker_load(self, package_dir):
        require(config() == self.cfg, "ops config changed")
        socket = Path(f"/run/user/{os.getuid()}/bigbrain-test-rootless/docker.sock")
        for parent in (socket.parent.parent, socket.parent):
            private(parent, directory=True)
        info = socket.lstat()
        require(stat.S_ISSOCK(info.st_mode) and info.st_uid == os.getuid(), "unsafe rootless socket")
        engine = decode(process(["/usr/bin/docker", "info", "--format", "{{json .}}"],
                                control_env(self.cfg), Path(self.cfg["controller"])))
        seal = document(Path(self.cfg["controller"]) / "installation.json")["engine"]
        require(engine["ID"] == seal["id"] and "name=rootless" in engine["SecurityOptions"] and
                any(x.split(",", 1)[0] == "name=seccomp" for x in engine["SecurityOptions"]) and
                engine["CgroupVersion"] == "2" and engine["CgroupDriver"] == "systemd",
                "rootless daemon identity/security mismatch")
        process(["/usr/bin/docker", "load", "--input", str(package_dir / "images.tar.gz")],
                control_env(self.cfg), Path(self.cfg["controller"]), timeout=300)

    def start_relay(self):
        require(config() == self.cfg, "ops config changed")
        self.relay = subprocess.Popen(
            ["/usr/bin/python3", "-I", str(Path(self.cfg["controller"]) / "test-environment.py"), "relay"],
            cwd=self.cfg["controller"], env=control_env(self.cfg), stdin=subprocess.DEVNULL,
            stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, start_new_session=True)

    def stop_relay(self):
        if self.relay is not None:
            terminate(self.relay)
            self.relay = None

    def previous_good(self):
        root = Path(self.cfg["controller"])
        last = STATE / "last-good.json"
        state = document(root / "state.json") if (root / "state.json").exists() else {}
        if not last.exists():
            require(not any(state.get(k) for k in ("containers", "networks", "volumes")),
                    "initial slot must be empty; no implicit adoption")
            if (root / "request.json").exists():
                require(set(state) == {"candidate", "containers", "networks", "volumes"},
                        "empty-slot state required")
                self.control("validate")
            return None
        pointer = document(last)
        require(set(pointer) == {"task_id", "receipt_sha256", "request_sha256"} and
                TASK.fullmatch(pointer["task_id"]), "last-good pointer rejected")
        folder = STATE / "tasks" / pointer["task_id"]
        private(folder, directory=True)
        require(sha_file(folder / "receipt.json") == pointer["receipt_sha256"], "last-good receipt drift")
        receipt = document(folder / "receipt.json")
        require(receipt["phase"] == "succeeded" and receipt["control"] == self.cfg["controller_files"],
                "last-good control/result mismatch")
        require(sha_file(root / "request.json") == pointer["request_sha256"], "current slot differs from last-good")
        oldtask = manifest(receipt["task"])
        pkg = verify_package(folder, oldtask, receipt["github"])
        self.control("validate")
        return folder, pkg, oldtask

    def render_start(self, package, task, folder):
        self.control("render", "--candidate-sha", task["source_sha"],
                     "--api-image", package["images"]["api"], "--web-image", package["images"]["web"],
                     "--bind-ip", self.cfg["bind_ip"], "--package", str(folder / "package.json"))
        self.control("validate")
        self.control("start")
        self.start_relay()
        self.control("probe")
        require(self.relay.poll() is None, "relay exited")

    def rollback(self):
        self.receipt("rolling-back")
        self.stop_relay()
        self.control("reset", "--confirm", "bb-test-u1-fixtures", cleanup=True)
        if self.previous is not None:
            folder, package, task = self.previous
            # Existing verified images are retained; never fetch or rebuild on rollback.
            # Cancellation does not suppress necessary cleanup, but pause never starts rollback.
            self.render_start(package, task, folder)
            self.stop_relay()
            self.control("stop", cleanup=True)
        self.receipt("rolled-back", fixtures="fresh" if self.previous else "empty")

    def execute(self):
        try:
            checkpoint()
            self.identity = github_identity(self.task, self.cfg)
            self.receipt("fetching")
            archive = self.folder / "artifact.zip"
            gh(f"repos/{REPO}/actions/artifacts/{self.task['artifact_id']}/zip", raw=True, output=archive)
            require("sha256:" + sha_file(archive) == self.identity["artifact_digest"], "artifact ZIP digest mismatch")
            unpack_zip(archive, self.folder)
            self.package = verify_package(self.folder, self.task, self.identity)
            self.receipt("verified")
            self.previous = self.previous_good()
            checkpoint()
            self.docker_load(self.folder)
            checkpoint()
            self.receipt("replacing")
            self.mutating = True
            if self.previous is not None:
                self.control("reset", "--confirm", "bb-test-u1-fixtures")
            self.render_start(self.package, self.task, self.folder)
            self.receipt("serving")
            deadline = time.monotonic() + self.task["runtime_seconds"]
            while time.monotonic() < deadline:
                checkpoint()
                require(self.relay.poll() is None, "relay exited during task")
                CANCEL.wait(min(0.25, deadline - time.monotonic()))
            self.stop_relay()
            self.control("stop", cleanup=True)
            self.receipt("succeeded")
            atomic(STATE / "last-good.json", {
                "task_id": self.task["task_id"],
                "receipt_sha256": sha_file(self.folder / "receipt.json"),
                "request_sha256": sha_file(Path(self.cfg["controller"]) / "request.json")})
        except Aborted:
            self.stop_relay()
            if self.mutating:
                try:
                    self.control("stop", cleanup=True)
                except Exception:
                    atomic(STATE / "PAUSED", {"reason": "cleanup-failed"})
                    self.receipt("cleanup-failed")
                    raise
            # A paused replacement differs from last-good. No resume/adoption is implicit.
            atomic(STATE / "PAUSED", {"reason": "aborted; inspect slot before resubmission"})
            self.receipt("aborted")
            raise
        except Exception:
            self.stop_relay()
            if self.mutating:
                try:
                    checkpoint()
                    self.rollback()
                except Exception:
                    # Receipt/storage failures must not bypass safe stop and pause.
                    stopped = False
                    try:
                        self.control("stop", cleanup=True)
                        stopped = True
                    except Exception:
                        pass
                    atomic(STATE / "PAUSED", {"reason": "rollback-failed; manual recovery required"})
                    self.receipt("rollback-failed", cleanup_stop="PASS" if stopped else "FAIL")
                    raise Rejected("rollback failed; paused for manual recovery") from None
            else:
                self.receipt("rejected")
            raise
        finally:
            self.stop_relay()


def run_task(cfg):
    with lock():
        try:
            unfinished()
        except Exception:
            atomic(STATE / "PAUSED", {"reason": "unfinished task; manual recovery required"})
            raise
        checkpoint()
        pending = STATE / "pending.json"
        if not pending.exists():
            return
        task = manifest(document(pending))
        folder = STATE / "tasks" / task["task_id"]
        require(not folder.exists() and not folder.is_symlink(), "replayed task ID rejected")
        budget()
        folder.mkdir(mode=0o700)
        syncdir(folder.parent)
        os.replace(pending, folder / "manifest.json")
        syncdir(STATE)
        syncdir(folder)
        runner = Runner(task, cfg, folder)
        runner.receipt("claimed")
        runner.execute()


def service_command(*args):
    env = base_env(HOME)
    env.update(XDG_RUNTIME_DIR=f"/run/user/{os.getuid()}",
               DBUS_SESSION_BUS_ADDRESS=f"unix:path=/run/user/{os.getuid()}/bus")
    process(["/usr/bin/systemctl", "--user", *args, UNIT], env, STATE, timeout=20,
            interruptible=False)


def main():
    os.umask(0o077)
    require(len(sys.argv) == 2 and sys.argv[1] in ("run", "submit", "pause", "resume", "status"),
            "fixed command required")
    cfg = config()
    command = sys.argv[1]
    if command == "pause":
        atomic(STATE / "PAUSED", {"reason": "operator pause"})
        # The running process also checks PAUSED; kill wakes a blocked subprocess.
        service_command("kill", "--signal=SIGTERM", "--kill-whom=main")
    elif command == "resume":
        with lock():
            unfinished()
            if (STATE / "PAUSED").exists():
                private(STATE / "PAUSED")
                (STATE / "PAUSED").unlink()
                syncdir(STATE)
    elif command == "status":
        print("PAUSED" if (STATE / "PAUSED").exists() else "IDLE/SEE PRIVATE RECEIPTS")
    elif command == "submit":
        raw = sys.stdin.buffer.read(4097)
        require(0 < len(raw) <= 4096, "manifest byte limit")
        task = manifest(decode(raw))
        with lock():
            unfinished()
            checkpoint()
            require(not (STATE / "pending.json").exists() and
                    not (STATE / "tasks" / task["task_id"]).exists() and
                    not (STATE / "tasks" / task["task_id"]).is_symlink(), "pending/replayed task rejected")
            atomic(STATE / "pending.json", task)
        # Restart closes the small active-but-already-unlocked service exit race.
        # Submission held the same lock used for the entire preceding task.
        service_command("restart")
    else:
        for sig in (signal.SIGTERM, signal.SIGINT):
            signal.signal(sig, lambda *_: CANCEL.set())
        run_task(cfg)


if __name__ == "__main__":
    try:
        main()
    except Exception:
        # No raw GitHub/Docker output, credentials, addresses or candidate error text.
        print("REJECTED: inspect private task receipt and pause state", file=sys.stderr)
        sys.exit(1)
