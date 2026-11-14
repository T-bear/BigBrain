#!/usr/bin/env python3
"""Manual U1 operator tool, outside all product processes. Python stdlib only.

No install/start is implicit. Snapshot the reviewed control files separately; later
candidates supply image IDs and provenance, never Compose or executable helpers.
This is an accident boundary for trusted workers, not isolation from Docker admins.
"""
import argparse
import contextlib
from concurrent.futures import ThreadPoolExecutor
import copy
import fcntl
import hashlib
from html.parser import HTMLParser
import ipaddress
import json
import os
from pathlib import Path
import re
import select
import shutil
import socket
import stat
import subprocess
import sys
import time
import threading
import urllib.request
import uuid

PROJECT = "bb-test-u1"
RELAY_CONNECTIONS = 8
SLOT_LABEL = "io.bigbrain.test.slot"
SHA = re.compile(r"[0-9a-f]{40}\Z")
IMAGE = re.compile(r"sha256:[0-9a-f]{64}\Z")
ROOT = Path(__file__).resolve().parent
BASE_ENV_KEYS = {
    "PATH", "APP_UID", "ASPNETCORE_HTTP_PORTS", "DOTNET_RUNNING_IN_CONTAINER",
    "DOTNET_VERSION", "ASPNET_VERSION", "NGINX_VERSION", "NJS_VERSION",
    "NJS_RELEASE", "PKG_RELEASE", "DYNPKG_RELEASE", "ACME_VERSION",
}


class Rejected(Exception):
    pass


class DockerRejected(Rejected):
    def __init__(self, result):
        super().__init__("Docker command failed; no raw output published")
        self.returncode = result.returncode
        # Private failure evidence only; never included in str(error)/public receipt.
        self.private_output = (result.stdout + result.stderr)[-8192:]


def require(condition, reason):
    if not condition:
        raise Rejected(reason)


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def read_json(path):
    require(not path.is_symlink(), "symlink rejected")
    return json.loads(path.read_text())


def write_json(path, value):
    # All callers hold the installation lock; never follow an existing symlink.
    require(not path.is_symlink(), "symlink rejected")
    temp = path.with_suffix(".tmp")
    with temp.open("x", encoding="utf-8") as out:
        os.chmod(temp, 0o600)
        json.dump(value, out, indent=2)
        out.write("\n")
    temp.replace(path)


def template_path():
    installed = ROOT / "compose.test.yaml"
    return installed if installed.exists() else ROOT.parent / "compose.test.yaml"


def engine_profile(seal):
    # Missing profile is the original U1 rootful installation, never auto-detect.
    profile = seal.get("engine")
    if profile is None:
        require("engine" not in seal, "invalid installed engine")
        return {"mode": "rootful"}
    require(isinstance(profile, dict) and set(profile) == {"mode", "uid", "id"},
            "invalid installed engine")
    require(profile["mode"] == "rootless" and type(profile["uid"]) is int and
            profile["uid"] == os.getuid(), "installed engine mode/owner mismatch")
    require(isinstance(profile["id"], str) and bool(profile["id"].strip()), "installed daemon identity missing")
    return profile


def rootless_socket():
    path = Path(f"/run/user/{os.getuid()}/bigbrain-test-rootless/docker.sock")
    # lstat rejects symlinks at every runtime component, including the socket.
    for parent in (path.parent.parent, path.parent):
        info = parent.lstat()
        require(stat.S_ISDIR(info.st_mode) and info.st_uid == os.getuid() and
                stat.S_IMODE(info.st_mode) == 0o700, "rootless runtime must be private and owned")
    info = path.lstat()
    require(stat.S_ISSOCK(info.st_mode) and info.st_uid == os.getuid(),
            "rootless socket must be an owned non-symlink socket")
    return path


def clean_environment(root):
    # Only operator-installed policy selects an engine; never candidate or env.
    seal_path = root / "installation.json"
    profile = engine_profile(read_json(seal_path)) if seal_path.exists() else {"mode": "rootful"}
    host = "unix://" + str(rootless_socket()) if profile["mode"] == "rootless" else "unix:///var/run/docker.sock"
    return {"PATH": "/usr/bin:/bin", "HOME": str(root / "client"),
            "DOCKER_CONFIG": str(root / "client"), "DOCKER_HOST": host, "LANG": "C.UTF-8"}


def validate_engine(info, profile):
    security = info.get("SecurityOptions") or []
    require(isinstance(security, list) and all(isinstance(entry, str) for entry in security),
            "invalid daemon security metadata")
    require(any(entry.split(",", 1)[0] == "name=seccomp" for entry in security), "default seccomp required")
    rootless = "name=rootless" in security
    require(rootless == (profile["mode"] == "rootless"), "daemon engine mode mismatch")
    if profile["mode"] == "rootless":
        require(info.get("ID") == profile["id"], "daemon identity mismatch")
        require(info.get("CgroupVersion") == "2" and info.get("CgroupDriver") == "systemd",
                "rootless requires cgroup v2/systemd")


def bind_rootless_engine(root, seal):
    # Snapshot alone may query an unsealed daemon, using the same private client
    # environment and fixed socket as all later commands. No context is consulted.
    env = {"PATH": "/usr/bin:/bin", "HOME": str(root / "client"),
           "DOCKER_CONFIG": str(root / "client"), "LANG": "C.UTF-8",
           "DOCKER_HOST": "unix://" + str(rootless_socket())}
    try:
        result = subprocess.run(["/usr/bin/docker", "info", "--format", "{{json .}}"],
                                cwd=root, env=env, text=True, capture_output=True, timeout=120, check=False)
    except (OSError, subprocess.TimeoutExpired):
        raise Rejected("Docker unavailable during rootless snapshot") from None
    require(result.returncode == 0, "Docker snapshot metadata unavailable")
    info = json.loads(result.stdout)
    profile = {"mode": "rootless", "uid": os.getuid(), "id": info.get("ID")}
    engine_profile({"engine": profile})
    validate_engine(info, profile)
    seal["engine"] = profile


def run(root, *args):
    try:
        result = subprocess.run(["/usr/bin/docker", *args], cwd=root,
                                env=clean_environment(root), text=True,
                                capture_output=True, timeout=120, check=False)
    except (OSError, subprocess.TimeoutExpired):
        raise Rejected("Docker unavailable or command timed out; inspect private slot state") from None
    # Docker output can contain private paths, addresses or application messages.
    if result.returncode != 0:
        raise DockerRejected(result)
    return result.stdout



def diagnostic_docker(root, *args):
    """Read-only, exact-ID diagnostic: 2s/64KiB, always reap our own CLI."""
    child = subprocess.Popen(["/usr/bin/docker", *args], cwd=root,
        env=clean_environment(root), stdin=subprocess.DEVNULL,
        stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    data = bytearray()
    status = "complete"
    deadline = time.monotonic() + 2
    try:
        while True:
            remaining = deadline - time.monotonic()
            if remaining <= 0:
                status = "timeout"
                break
            ready, _, _ = select.select([child.stdout], [], [], remaining)
            if not ready:
                status = "timeout"
                break
            chunk = os.read(child.stdout.fileno(), min(4096, 65537 - len(data)))
            if not chunk:
                try:
                    child.wait(timeout=max(0.01, deadline - time.monotonic()))
                except subprocess.TimeoutExpired:
                    status = "timeout"
                break
            data.extend(chunk)
            if len(data) > 65536:
                status = "truncated"
                break
    finally:
        if child.poll() is None:
            child.kill()
        child.wait(timeout=2)
        child.stdout.close()
    return {"status": status, "returncode": child.returncode,
            "output": bytes(data[:65536]).decode("utf-8", errors="replace")}


def diagnostic_cgroup(path):
    values = {}
    for name in ("pids.current", "pids.events", "memory.current", "memory.peak",
                 "memory.events", "cpu.stat"):
        try:
            with (path / name).open("r", encoding="ascii") as source:
                values[name] = source.read(4096).strip()
        except (OSError, UnicodeError):
            values[name] = None
    return values


def compose(root, *args):
    return run(root, "compose", "--project-name", PROJECT, "--project-directory", str(root),
               "--env-file", str(root / "EMPTY"), "--file", str(root / "candidate.json"), *args)


def parameters(candidate, api, web, bind, slot):
    require(bool(SHA.fullmatch(candidate or "")), "exact candidate SHA required")
    require(bool(IMAGE.fullmatch(api or "") and IMAGE.fullmatch(web or "")), "exact local image IDs required")
    require(api != web, "distinct API/Web images required")
    try:
        address = ipaddress.IPv4Address(bind)
    except ipaddress.AddressValueError:
        raise Rejected("user-selected Tailscale IPv4 required") from None
    require(address in ipaddress.IPv4Network("100.64.0.0/10"), "Tailscale IPv4 range required")
    require(bool(re.fullmatch(r"[a-f0-9]{32}", slot)), "invalid slot identity")
    return {"candidate": candidate, "api": api, "web": web, "bind": str(address), "slot": slot}


def render(template, p):
    substitutions = {"TEST_API_IMAGE": p["api"], "TEST_WEB_IMAGE": p["web"],
                     "TEST_BIND_IP": p["bind"], "TEST_SLOT_ID": p["slot"]}
    def substitute(value):
        if isinstance(value, str):
            return re.sub(r"\$\{([A-Z_]+):\?[^}]*\}", lambda m: substitutions[m[1]], value)
        if isinstance(value, dict):
            return {k: substitute(v) for k, v in value.items()}
        if isinstance(value, list):
            return [substitute(v) for v in value]
        return value
    return substitute(template)


def normalized(config):
    """Only harmless Compose canonical defaults are accepted; unknown keys fail."""
    value = copy.deepcopy(config)
    for service in value.get("services", {}).values():
        service.setdefault("command", [])
        if "cpus" in service:
            service["cpus"] = float(service["cpus"])
        for key in ("mem_limit", "memswap_limit"):
            if isinstance(service.get(key), str) and re.fullmatch(r"[0-9]+", service[key]):
                service[key] = int(service[key])
        for network, options in service.get("networks", {}).items():
            if options in (None, {}, {"priority": 0}):
                service["networks"][network] = None
        for port in service.get("ports", []):
            if port.get("mode") == "ingress":
                del port["mode"]
            port["published"] = str(port["published"])
        for mount in service.get("volumes", []):
            if mount.get("read_only") is False:
                del mount["read_only"]
            if mount.get("volume") == {}:
                del mount["volume"]
    for network in value.get("networks", {}).values():
        if network.get("ipam") == {}:
            del network["ipam"]
    return value


def validate_effective(actual, expected):
    require(normalized(actual) == normalized(expected), "effective Compose differs from installed template")


def validate_image(info, name, p, service):
    require(info["Id"] == p[name], "image identity mismatch")
    config = info["Config"]
    labels = config.get("Labels") or {}
    require(labels.get("org.opencontainers.image.revision") == p["candidate"], "OCI revision mismatch")
    require(labels.get("io.bigbrain.test.kind") == name, "TEST image kind missing")
    require(config.get("User") == service["user"], "image user mismatch")
    require(not config.get("Volumes"), "image-declared volumes rejected")
    require(not config.get("OnBuild"), "image OnBuild rejected")
    require(not config.get("Healthcheck"), "image healthcheck rejected")
    require(config.get("Entrypoint") == service["entrypoint"], "image entrypoint mismatch")
    require((config.get("Cmd") or []) == service["command"], "image command mismatch")
    env = config.get("Env") or []
    keys = [entry.split("=", 1)[0] for entry in env]
    require(len(keys) == len(set(keys)), "duplicate image environment")
    require(set(keys) <= BASE_ENV_KEYS, "unexpected image environment")


def owned(labels, slot):
    return (labels or {}).get(SLOT_LABEL) == slot and (labels or {}).get("com.docker.compose.project") == PROJECT


def validate_network(info, p):
    require(info["Name"] == PROJECT + "-net" and owned(info.get("Labels"), p["slot"]), "foreign network")
    require(info.get("Driver") == "bridge" and info.get("Internal") is True and
            info.get("EnableIPv6") is False, "network isolation mismatch")
    require(info.get("Options") == {"com.docker.network.bridge.gateway_mode_ipv4": "isolated"},
            "isolated bridge option missing or extra options")


def validate_volume(info, name, p):
    require(info["Name"] == PROJECT + "-" + name and owned(info.get("Labels"), p["slot"]), "foreign volume")
    require(info.get("Driver") == "local" and not info.get("Options"), "external/bind-backed volume rejected")
    require((info.get("Labels") or {}).get("com.docker.compose.volume") == name, "volume role mismatch")


def validate_container(info, name, p, expected, image_info, network_id):
    service = expected["services"][name]
    config, host = info["Config"], info["HostConfig"]
    require(info["Name"] == "/" + PROJECT + "-" + name, "container name mismatch")
    require(info["Image"] == p[name] and config["Image"] == p[name], "container image mismatch")
    require(owned(config.get("Labels"), p["slot"]) and
            config["Labels"].get("com.docker.compose.service") == name, "foreign container")
    require(config.get("User") == service["user"], "container user mismatch")
    require(config.get("Entrypoint") == service["entrypoint"] and
            (config.get("Cmd") or []) == service["command"], "container command mismatch")
    require(not config.get("Healthcheck"), "container healthcheck rejected")
    image_env = dict(entry.split("=", 1) for entry in image_info["Config"].get("Env", []))
    expected_env = image_env | service.get("environment", {})
    actual_env = config.get("Env") or []
    require(len(actual_env) == len(expected_env) and
            dict(entry.split("=", 1) for entry in actual_env) == expected_env, "container environment mismatch")
    require(host.get("ReadonlyRootfs") is True and not host.get("Privileged"), "container privilege mismatch")
    require(host.get("CapDrop") == ["ALL"] and not host.get("CapAdd"), "capabilities mismatch")
    require(host.get("SecurityOpt") == ["no-new-privileges:true"], "security options mismatch")
    # Engine also uses Binds strings for named volumes; permit only exact own mappings.
    binds = host.get("Binds") or []
    expected_binds = [PROJECT + "-" + v["source"] + ":" + v["target"] + ":rw"
                      for v in service.get("volumes", [])]
    require(not binds or sorted(binds) == sorted(expected_binds), "unapproved named-volume or host bind")
    for key in ("Devices", "DeviceRequests", "VolumesFrom", "ExtraHosts", "Links",
                "Dns", "DnsSearch", "DnsOptions", "GroupAdd", "Sysctls", "DeviceCgroupRules"):
        require(not host.get(key), "unexpected host access option")
    require(host.get("PidMode", "") == "" and host.get("IpcMode") in ("private", "") and
            host.get("UTSMode", "") == "" and host.get("UsernsMode", "") == "", "namespace sharing rejected")
    require(host.get("NetworkMode") == PROJECT + "-net", "container network mismatch")
    nets = info["NetworkSettings"]["Networks"]
    allowed_network_ids = {network_id} if info["State"].get("Running") else {"", network_id}
    require(set(nets) == {PROJECT + "-net"} and nets[PROJECT + "-net"]["NetworkID"] in allowed_network_ids,
            "additional or foreign network")
    require(host.get("NanoCpus") == int(service["cpus"] * 1_000_000_000) and
            host.get("Memory") == service["mem_limit"] and host.get("MemorySwap") == service["memswap_limit"] and
            host.get("PidsLimit") == 64, "resource limits mismatch")
    require(host.get("RestartPolicy", {}).get("Name") == "no", "autostart rejected")
    require(host.get("LogConfig") == {"Type": "json-file", "Config": service["logging"]["options"]}, "log limits mismatch")
    require(host.get("Tmpfs") == {"/tmp": "rw,noexec,nosuid,size=67108864,mode=1777"}, "tmpfs mismatch")
    require(not host.get("PortBindings") and not host.get("PublishAllPorts"), "container published ports rejected")
    mounts = info.get("Mounts") or []
    wanted = service.get("volumes", [])
    # Engine versions may include the separately checked tmpfs in Mounts.
    mounts = [m for m in mounts if not (m.get("Type") == "tmpfs" and m.get("Destination") == "/tmp")]
    require(len(mounts) == len(wanted), "extra/missing mounts")
    for mount in mounts:
        require(mount.get("Type") == "volume" and mount.get("Driver") == "local" and mount.get("RW") is True,
                "bind/device/read-only mount rejected")
        require(any(mount.get("Name") == PROJECT + "-" + v["source"] and mount.get("Destination") == v["target"]
                    for v in wanted), "foreign volume mount")


@contextlib.contextmanager
def locked(root, name="lock"):
    path = root / name
    require(not path.is_symlink(), "lock symlink rejected")
    with path.open("a") as handle:
        os.chmod(path, 0o600)
        try:
            fcntl.flock(handle, fcntl.LOCK_EX | fcntl.LOCK_NB)
        except BlockingIOError:
            raise Rejected("slot busy; no concurrent lifecycle operation") from None
        yield


def client_addresses(values):
    require(isinstance(values, list) and 1 <= len(values) <= 16, "explicit installed client allowlist required")
    result = []
    for value in values:
        try:
            address = ipaddress.IPv4Address(value)
        except (ipaddress.AddressValueError, TypeError):
            raise Rejected("exact Tailscale IPv4 client required") from None
        require(address in ipaddress.IPv4Network("100.64.0.0/10"), "client outside Tailscale range")
        result.append(str(address))
    require(len(result) == len(set(result)), "duplicate allowed client")
    return sorted(result)


def installation(root):
    seal = read_json(root / "installation.json")
    require(seal.get("version") == 1, "installation version mismatch")
    client_addresses(seal.get("allowed_clients"))
    engine_profile(seal)
    for name in ("test-environment.py", "compose.test.yaml", "EMPTY"):
        path = root / name
        require(not path.is_symlink() and digest(path) == seal["files"][name], "installed control file drift")
    require((root / "EMPTY").read_bytes() == b"", "env file must be empty")
    require(not (root / "client" / "config.json").exists(), "Docker credentials/config rejected")
    return seal


def snapshot(destination, revision, clients=None, engine="rootful"):
    require(engine in ("rootful", "rootless"), "invalid snapshot engine")
    allowed_clients = client_addresses(clients)
    require(bool(SHA.fullmatch(revision or "")), "reviewed control SHA required")
    destination = Path(destination).absolute()
    require(not destination.exists(), "installation destination already exists")
    require(not any((parent / ".git").exists() for parent in [destination, *destination.parents]),
            "installation must be outside candidate worktrees")
    # No install/service/mutation/hook; only rootless snapshots query Docker info.
    destination.mkdir(mode=0o700)
    shutil.copyfile(Path(__file__), destination / "test-environment.py")
    shutil.copyfile(template_path(), destination / "compose.test.yaml")
    (destination / "EMPTY").write_bytes(b"")
    (destination / "client").mkdir(mode=0o700)
    files = {n: digest(destination / n) for n in ("test-environment.py", "compose.test.yaml", "EMPTY")}
    seal = {"version": 1, "control_revision": revision, "slot": uuid.uuid4().hex,
            "files": files, "allowed_clients": allowed_clients}
    if engine == "rootless":
        bind_rootless_engine(destination, seal)
    write_json(destination / "installation.json", seal)
    for name in [*files, "installation.json"]:
        (destination / name).chmod(0o400)


class Slot:
    def __init__(self, root, seal):
        self.root, self.seal = root, seal
        record = read_json(root / "request.json")
        self.p = parameters(**record["parameters"])
        require(self.p["slot"] == seal["slot"], "slot identity mismatch")
        self.expected = render(read_json(root / "compose.test.yaml"), self.p)
        validate_effective(read_json(root / "candidate.json"), self.expected)
        self.images = {}

    def receipt(self, event, **extra):
        path = self.root / "events.jsonl"
        require(not path.is_symlink(), "receipt symlink rejected")
        with path.open("a", encoding="utf-8") as output:
            os.chmod(path, 0o600)
            output.write(json.dumps({"at": time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime()),
                "event": event, "candidate": self.p["candidate"], "images": {k: self.p[k] for k in ("api", "web")},
                "control_files": self.seal["files"], **extra}) + "\n")

    def preflight(self):
        profile = engine_profile(self.seal)
        validate_engine(json.loads(run(self.root, "info", "--format", "{{json .}}")), profile)
        validate_effective(json.loads(compose(self.root, "config", "--format", "json")), self.expected)
        for name in ("api", "web"):
            info = json.loads(run(self.root, "image", "inspect", self.p[name]))[0]
            validate_image(info, name, self.p, self.expected["services"][name])
            self.images[name] = info

    def inventory(self):
        # Read only filtered resources, including collisions with fixed names.
        result = {}
        for kind, noun in (("containers", "container"), ("networks", "network"), ("volumes", "volume")):
            ids = set()
            for filter_value in ("label=com.docker.compose.project=" + PROJECT,
                                 "label=" + SLOT_LABEL + "=" + self.p["slot"], "name=" + PROJECT):
                args = [noun, "ls", "--quiet", "--filter", filter_value]
                if noun == "container":
                    args.append("--all")
                ids.update(run(self.root, *args).split())
            result[kind] = json.loads(run(self.root, noun, "inspect", *sorted(ids))) if ids else []
        return result

    def audit(self, inventory, complete=True):
        networks = inventory["networks"]
        require(len(networks) <= 1, "unexpected network collision")
        for network in networks:
            validate_network(network, self.p)
        names = set()
        for volume in inventory["volumes"]:
            name = (volume.get("Labels") or {}).get("com.docker.compose.volume")
            require(name in self.expected["volumes"] and name not in names, "unexpected volume collision")
            validate_volume(volume, name, self.p)
            names.add(name)
        seen = set()
        for info in inventory["containers"]:
            name = (info["Config"].get("Labels") or {}).get("com.docker.compose.service")
            require(name in ("api", "web") and name not in seen and networks, "unexpected container collision")
            validate_container(info, name, self.p, self.expected, self.images[name], networks[0]["Id"])
            seen.add(name)
        allowed_ids = {c["Id"] for c in inventory["containers"]}
        for network in networks:
            require(set((network.get("Containers") or {})) <= allowed_ids, "foreign network attachment")
        if complete:
            require(seen == {"api", "web"} and names == set(self.expected["volumes"]), "incomplete slot")
        return {c["Config"]["Labels"]["com.docker.compose.service"]: c["Id"] for c in inventory["containers"]}

    def save_state(self, inventory):
        write_json(self.root / "state.json", {"candidate": self.p["candidate"],
                   "containers": {c["Config"]["Labels"]["com.docker.compose.service"]: c["Id"] for c in inventory["containers"]},
                   "networks": [n["Id"] for n in inventory["networks"]],
                   "volumes": [v["Name"] for v in inventory["volumes"]]})

    def start(self):
        inventory = self.inventory()
        self.audit(inventory, complete=False)
        if not inventory["containers"]:
            compose(self.root, "create", "--no-build", "--pull", "never")
        inventory = self.inventory()
        ids = self.audit(inventory)
        self.save_state(inventory)
        self.receipt("validated-before-start", containers=ids)
        self.health_attempts = []
        began = time.monotonic()
        stage = "start-api"
        try:
            for name in ("api", "web"):
                stage = "start-" + name
                run(self.root, "container", "start", ids[name])
            stage = "post-start-audit"
            self.audit(self.inventory())
            stage = "internal-health"
            self.internal_health(ids["web"])
        except BaseException as error:
            # Diagnostic failure must never suppress exact-owned cleanup.
            try:
                self.failure_evidence(ids, stage, began, error)
            except Exception:
                pass
            finally:
                # Only exact IDs which passed pre-start validation; keep fixture data.
                run(self.root, "container", "stop", "--time", "35", *ids.values())
                self.receipt("start-failed-stopped")
            raise
        self.receipt("started-internal-health-pass", external_http="NOT RUN", rootless="NOT RUN", client_acl="NOT RUN", isolation="PENDING")

    def failure_evidence(self, ids, stage, began, error):
        # Installed operations tool, outside API/Web/Brain. No candidate-selected paths.
        profile = engine_profile(self.seal)
        base = Path("/sys/fs/cgroup/user.slice") / f"user-{os.getuid()}.slice" / f"user@{os.getuid()}.service"
        evidence = {"version": 1, "candidate": self.p["candidate"],
            "stage": stage, "elapsed_seconds": round(time.monotonic() - began, 3),
            "failure_type": type(error).__name__,
            "docker_exit": getattr(error, "returncode", None),
            "private_error": getattr(error, "private_output", None),
            "health_attempts": getattr(self, "health_attempts", []), "containers": {}}
        if profile["mode"] == "rootless":
            evidence["task_cgroup"] = diagnostic_cgroup(base / "app.slice/bigbrain-test-task.service")
            evidence["engine_cgroup"] = diagnostic_cgroup(base / "app.slice/bigbrain-test-rootless.service")
        for name in ("api", "web"):
            cid = ids[name]
            require(re.fullmatch(r"[0-9a-f]{64}", cid) is not None, "invalid diagnostic target")
            observed = diagnostic_docker(self.root, "container", "inspect", cid)
            row = {"id": cid, "inspect_status": observed["status"]}
            evidence["containers"][name] = row
            try:
                require(observed["status"] == "complete" and observed["returncode"] == 0,
                        "diagnostic inspect unavailable")
                info = json.loads(observed["output"])[0]
                require(info["Id"] == cid and info["Image"] == self.p[name] and
                        owned(info["Config"].get("Labels"), self.p["slot"]) and
                        info["Config"]["Labels"].get("com.docker.compose.service") == name,
                        "diagnostic identity mismatch")
                row["state"] = {key: info["State"].get(key) for key in
                    ("Status", "Running", "ExitCode", "OOMKilled", "StartedAt", "FinishedAt")}
                row["logs"] = diagnostic_docker(self.root, "logs", "--timestamps", "--tail", "80", cid)
                if profile["mode"] == "rootless":
                    row["cgroup"] = diagnostic_cgroup(base / "user.slice" / ("docker-" + cid + ".scope"))
            except (Rejected, ValueError, KeyError, TypeError, IndexError):
                row["identity_verified"] = False
                continue
            row["identity_verified"] = True
        path = self.root / ("start-failure-" + uuid.uuid4().hex + ".json")
        write_json(path, evidence)
        # Only the opaque filename/hash enters the general receipt.
        self.receipt("start-failure-evidence", file=path.name, sha256=digest(path))

    def internal_health(self, web_id):
        attempts = []
        self.health_attempts = attempts
        began = time.monotonic()
        for attempt in range(12):
            try:
                body = run(self.root, "exec", web_id, "/bin/busybox", "wget", "-T", "3", "-q", "-O", "-", "http://127.0.0.1:8080/health")
                require(body.strip() == "Healthy", "internal Web/API health failed")
                attempts.append({"attempt": attempt + 1, "elapsed_seconds": round(time.monotonic() - began, 3),
                                 "outcome": "healthy"})
                return
            except Rejected as error:
                attempts.append({"attempt": attempt + 1, "elapsed_seconds": round(time.monotonic() - began, 3),
                    "outcome": "rejected", "failure_type": type(error).__name__,
                    "docker_exit": getattr(error, "returncode", None)})
                if attempt == 11:
                    raise
                time.sleep(1)

    def stop(self):
        ids = self.audit(self.inventory(), complete=False)
        if ids:
            run(self.root, "container", "stop", "--time", "35", *ids.values())
        self.receipt("stopped")

    def reset(self, confirmation):
        require(confirmation == PROJECT + "-fixtures", "explicit own-fixture reset confirmation required")
        inventory = self.inventory()
        ids = self.audit(inventory, complete=False)
        # Prevalidate every target before the first mutation, recheck before removal.
        self.receipt("reset-authorized", containers=ids)
        if ids:
            run(self.root, "container", "stop", "--time", "35", *ids.values())
            self.audit(self.inventory(), complete=False)
            run(self.root, "container", "rm", *ids.values())
        remaining = self.inventory()
        self.audit(remaining, complete=False)
        require(not remaining["containers"], "container appeared during reset")
        for volume in remaining["volumes"]:
            current = json.loads(run(self.root, "volume", "inspect", volume["Name"]))[0]
            validate_volume(current, current["Labels"]["com.docker.compose.volume"], self.p)
            run(self.root, "volume", "rm", volume["Name"])
        for network in remaining["networks"]:
            current = json.loads(run(self.root, "network", "inspect", network["Id"]))[0]
            validate_network(current, self.p)
            require(not current.get("Containers"), "network acquired foreign attachment")
            run(self.root, "network", "rm", network["Id"])
        self.save_state({"containers": [], "networks": [], "volumes": []})
        self.receipt("reset-complete")

    def relay_target(self, expected_id):
        # Pin target before every connection; network input cannot select a target.
        installation(self.root)
        state = read_json(self.root / "state.json")
        require(state.get("candidate") == self.p["candidate"] and
                state.get("containers", {}).get("web") == expected_id, "relay slot changed")
        info = json.loads(run(self.root, "container", "inspect", expected_id))[0]
        require(len(state.get("networks", [])) == 1, "relay network state missing")
        validate_container(info, "web", self.p, self.expected, self.images["web"], state["networks"][0])
        require(info["State"].get("Running") is True, "relay Web is not running")

    def relay(self):
        # Foreground, manually owned process only. No systemd/timer/background installer.
        with locked(self.root, "relay.lock"):
            with locked(self.root):
                self.preflight()
                ids = self.audit(self.inventory())
                self.relay_target(ids["web"])
                self.internal_health(ids["web"])
            cancelled = threading.Event()
            capacity = threading.BoundedSemaphore(RELAY_CONNECTIONS)
            capacity_notice_recorded = False
            with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as listener:
                # No wildcard, IPv6, SO_REUSEPORT or fallback bind.
                listener.bind((self.p["bind"], 13001))
                listener.listen(RELAY_CONNECTIONS)
                listener.settimeout(0.5)
                self.receipt("relay-listening", web_id=ids["web"], max_connections=RELAY_CONNECTIONS, timeout_seconds=30)
                print("RELAY listening on selected interface; client ACL acceptance remains separate", flush=True)
                with ThreadPoolExecutor(max_workers=RELAY_CONNECTIONS) as workers:
                    try:
                        while not cancelled.is_set():
                            try:
                                client, peer = listener.accept()
                            except socket.timeout:
                                # A reset/new candidate retires this relay. Stop alone may
                                # preserve it across a deliberate same-ID restart test.
                                state = read_json(self.root / "state.json")
                                if state.get("containers", {}).get("web") != ids["web"]:
                                    break
                                continue
                            if not self.accept_client(client, peer):
                                continue
                            if not capacity.acquire(blocking=False):
                                client.close()
                                if not capacity_notice_recorded:
                                    # One sanitized observation per relay, never per request.
                                    self.receipt("relay-capacity-rejected", max_connections=RELAY_CONNECTIONS)
                                    capacity_notice_recorded = True
                                continue
                            workers.submit(self.relay_connection, client, ids["web"], cancelled, capacity)
                    except KeyboardInterrupt:
                        pass
                    finally:
                        cancelled.set()
                self.receipt("relay-stopped")

    def accept_client(self, client, peer):
        # Destination binding is insufficient for traffic routed over another interface.
        # The operator installs this policy once; candidate render cannot change it.
        if peer[0] not in self.seal["allowed_clients"]:
            client.close()
            return False
        return True

    def relay_connection(self, client, web_id, cancelled, capacity):
        child = None
        try:
            with client:
                # Read-only validation intentionally does not acquire lifecycle lock:
                # probe/restart holds it while making HTTP requests through this relay.
                # The immutable container ID prevents a concurrent reset redirecting exec.
                self.relay_target(web_id)
                child = subprocess.Popen(relay_argv(web_id), cwd=self.root,
                    env=clean_environment(self.root), stdin=subprocess.PIPE, stdout=subprocess.PIPE,
                    stderr=subprocess.DEVNULL)
                pump(client, child, cancelled)
        except (Rejected, OSError, ValueError, KeyError):
            pass  # Fail closed, never forward diagnostics or private Docker output.
        finally:
            if child is not None:
                # Only this connection's owned Docker client, never an arbitrary PID.
                if child.poll() is None:
                    child.terminate()
                    try:
                        child.wait(timeout=2)
                    except subprocess.TimeoutExpired:
                        child.kill()
                        child.wait(timeout=2)
                for stream in (child.stdin, child.stdout):
                    if stream is not None:
                        stream.close()
            capacity.release()

    def request(self, path, payload=None):
        require(path.startswith("/") and not path.startswith("//") and "://" not in path, "nonlocal HTTP path")
        class NoRedirect(urllib.request.HTTPRedirectHandler):
            def redirect_request(self, *args, **kwargs):
                return None
        # Never inherit http_proxy; refuse redirects to providers/production.
        client = urllib.request.build_opener(urllib.request.ProxyHandler({}), NoRedirect())
        data = None if payload is None else json.dumps(payload).encode()
        req = urllib.request.Request("http://" + self.p["bind"] + ":13001" + path, data=data,
                                     headers={"Content-Type": "application/json"} if data else {})
        with client.open(req, timeout=5) as response:
            require(response.status in (200, 201), "HTTP status failure")
            body = response.read(8 * 1024 * 1024 + 1)
            require(len(body) <= 8 * 1024 * 1024, "HTTP response too large")
            return body, response.headers.get_content_type()

    def probe(self, restart=False):
        self.audit(self.inventory())
        # Bounded startup readiness, not a background monitoring/wait loop.
        for attempt in range(12):
            try:
                health, _ = self.request("/health")
                require(health.strip() == b"Healthy", "API health body mismatch")
                break
            except (OSError, Rejected):
                if attempt == 11:
                    raise Rejected("API through Web health failed") from None
                time.sleep(1)
        body, mime = self.request("/")
        require(mime == "text/html" and b' id="bb-test-banner"' in body and
                self.p["candidate"][:12].encode() in body, "TEST banner missing")
        revision, _ = self.request("/test-revision.txt")
        require(revision.decode().strip() == self.p["candidate"], "served revision mismatch")
        parser = Assets()
        parser.feed(body.decode())
        require(parser.js and parser.css and parser.icons, "built JS/CSS/icon references missing")
        for path, types in [(p, ("application/javascript", "text/javascript")) for p in parser.js] + \
                           [(p, ("text/css",)) for p in parser.css] + [(p, ("image/png", "image/svg+xml", "image/x-icon")) for p in parser.icons]:
            asset, content_type = self.request(path)
            require(asset and content_type in types and not asset.lstrip().startswith(b"<!doctype"), "asset response invalid")
        manifest, content_type = self.request("/manifest.webmanifest")
        require(content_type in ("application/manifest+json", "application/json", "application/octet-stream"), "manifest MIME invalid")
        manifest = json.loads(manifest)
        require(manifest["short_name"] == "BB TEST" and manifest.get("icons"), "TEST manifest missing")
        for icon in manifest["icons"]:
            body, content_type = self.request(icon["src"])
            require(content_type == "image/png" and body.startswith(b"\x89PNG\r\n\x1a\n"), "manifest icon invalid")
        if restart:
            name = "TEST fixture " + uuid.uuid4().hex
            self.request("/api/v1/modules/shopping-list/items", {"name": name, "quantity": 1})
            ids = self.audit(self.inventory())
            self.stop()
            for service in ("api", "web"):
                run(self.root, "container", "start", ids[service])
            self.probe()
            body, _ = self.request("/api/v1/modules/shopping-list/items")
            require(any(item["name"] == name for item in json.loads(body)["items"]), "synthetic fixture lost after restart")
        self.receipt("http-assets-pass", restart_fixture="PASS" if restart else "NOT RUN")


class Assets(HTMLParser):
    def __init__(self):
        super().__init__()
        self.js, self.css, self.icons = [], [], []

    def handle_starttag(self, tag, attrs):
        attrs = dict(attrs)
        if tag == "script" and "src" in attrs:
            self.js.append(attrs["src"])
        if tag == "link" and "href" in attrs:
            if attrs.get("rel") == "stylesheet":
                self.css.append(attrs["href"])
            if attrs.get("rel") in ("icon", "apple-touch-icon"):
                self.icons.append(attrs["href"])


def relay_argv(web_id):
    require(bool(re.fullmatch(r"[0-9a-f]{64}", web_id)), "exact verified Web container ID required")
    # BusyBox timeout bounds the in-container exec even if its Docker client is lost.
    return ["/usr/bin/docker", "exec", "-i", web_id, "/bin/busybox", "timeout", "-s", "KILL", "35",
            "/bin/busybox", "nc", "-w", "30", "127.0.0.1", "8080"]


def pump(client, child, cancelled):
    """Bounded bidirectional byte copy. No HTTP parsing, shell or caller-selected args."""
    limit, deadline = 65536, time.monotonic() + 30
    inbound, outbound = bytearray(), bytearray()
    client.setblocking(False)
    os.set_blocking(child.stdin.fileno(), False)
    os.set_blocking(child.stdout.fileno(), False)
    input_open = output_open = True
    while not cancelled.is_set() and time.monotonic() < deadline:
        readers, writers = [], []
        if input_open and len(inbound) < limit:
            readers.append(client)
        if output_open and len(outbound) < limit:
            readers.append(child.stdout)
        if inbound:
            writers.append(child.stdin)
        elif not input_open and not child.stdin.closed:
            child.stdin.close()
        if outbound:
            writers.append(client)
        elif not output_open:
            break
        readable, writable, _ = select.select(readers, writers, [], 0.25)
        for stream in readable:
            if stream is client:
                block = client.recv(limit - len(inbound))
                if block:
                    inbound.extend(block)
                else:
                    input_open = False
            else:
                block = os.read(child.stdout.fileno(), limit - len(outbound))
                if block:
                    outbound.extend(block)
                else:
                    output_open = False
        for stream in writable:
            if stream is client:
                del outbound[:client.send(outbound)]
            else:
                del inbound[:os.write(child.stdin.fileno(), inbound)]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("command", choices=["snapshot", "validate", "render", "start", "status", "stop", "reset", "probe", "relay"])
    parser.add_argument("--destination")
    parser.add_argument("--engine", choices=["rootful", "rootless"], help="operator snapshot only; default rootful")
    parser.add_argument("--control-sha")
    parser.add_argument("--allow-client", action="append", help="snapshot only: exact approved Tailscale IPv4, include local probe")
    parser.add_argument("--candidate-sha")
    parser.add_argument("--api-image")
    parser.add_argument("--web-image")
    parser.add_argument("--bind-ip")
    parser.add_argument("--package", type=Path, help="private package provenance JSON")
    parser.add_argument("--confirm")
    parser.add_argument("--restart", action="store_true")
    args = parser.parse_args()
    require(args.engine is None or args.command == "snapshot", "engine selection is snapshot only")
    os.umask(0o077)
    if args.command == "snapshot":
        require(args.destination is not None, "destination required")
        snapshot(args.destination, args.control_sha, args.allow_client, args.engine or "rootful")
        print("SNAPSHOT created; review installed control hashes before runtime")
        return
    if args.command == "validate" and not (ROOT / "installation.json").exists():
        p = parameters(args.candidate_sha, args.api_image, args.web_image, args.bind_ip, "0" * 32)
        expected = render(read_json(template_path()), p)
        validate_effective(expected, expected)
        print("PASS template render only; Docker/image/network/runtime NOT RUN")
        return
    seal = installation(ROOT)
    if args.command == "relay":
        Slot(ROOT, seal).relay()
        return
    with locked(ROOT):
        if args.command == "render":
            state_path = ROOT / "state.json"
            if state_path.exists():
                state = read_json(state_path)
                require(not any(state.get(key) for key in ("containers", "volumes", "networks")), "reset existing slot before replacing candidate")
            p = parameters(args.candidate_sha, args.api_image, args.web_image, args.bind_ip, seal["slot"])
            require(args.package is not None, "hosted package provenance required")
            package = read_json(args.package)
            require(package.get("source_sha") == p["candidate"] and SHA.fullmatch(package.get("tree", "")), "package source/tree mismatch")
            require(package.get("images") == {k: p[k] for k in ("api", "web")}, "package image mismatch")
            require(package.get("repository") == "T-bear/BigBrain" and package.get("trusted_owner") is True and
                    package.get("ref", "").startswith("refs/heads/checkpoint/"), "untrusted candidate package")
            require(package.get("workflow_run") and package.get("tests") and package.get("checksums"), "package provenance incomplete")
            write_json(ROOT / "candidate.json", render(read_json(ROOT / "compose.test.yaml"), p))
            write_json(ROOT / "request.json", {"parameters": {"candidate": p["candidate"], "api": p["api"],
                       "web": p["web"], "bind": p["bind"], "slot": p["slot"]}, "package": package})
            print("RENDERED private candidate; runtime NOT RUN")
            return
        slot = Slot(ROOT, seal)
        slot.preflight()
        if args.command == "start":
            slot.start()
        elif args.command == "stop":
            slot.stop()
        elif args.command == "reset":
            slot.reset(args.confirm)
        elif args.command == "probe":
            slot.probe(args.restart)
        else:
            inventory = slot.inventory()
            slot.audit(inventory, complete=args.command == "status")
            slot.receipt(args.command + "-inspect-pass", running={c["Config"]["Labels"]["com.docker.compose.service"]:
                         c["State"]["Running"] for c in inventory["containers"]})
        print("PASS " + args.command + "; private receipts retained; client ACL/isolation acceptance remains separate")


if __name__ == "__main__":
    try:
        main()
    except (Rejected, OSError, ValueError, KeyError, TypeError) as error:
        # Never echo raw Docker, HTTP, JSON, paths, addresses or secret values.
        print("REJECTED: " + (str(error) if isinstance(error, Rejected) else type(error).__name__), file=sys.stderr)
        sys.exit(1)
