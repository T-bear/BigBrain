"""Offline policy tests. No Docker, network, providers or application execution."""
import copy
import importlib.util
import json
import os
from pathlib import Path
import socket
import stat
import subprocess
import sys
import tempfile
import threading
import unittest
from unittest.mock import MagicMock, patch
from types import SimpleNamespace

ROOT = Path(__file__).resolve().parents[2]
SPEC = importlib.util.spec_from_file_location("test_environment", ROOT / "scripts/test-environment.py")
tool = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(tool)
BUILD_SPEC = importlib.util.spec_from_file_location("test_build", ROOT / "scripts/test-candidate-dockerfile.py")
build = importlib.util.module_from_spec(BUILD_SPEC)
BUILD_SPEC.loader.exec_module(build)


def fixture():
    # Synthetic CGNAT address, never a host address discovered from this machine.
    p = tool.parameters("a" * 40, "sha256:" + "b" * 64, "sha256:" + "c" * 64,
                        "100.64.0.1", "d" * 32)
    expected = tool.render(json.loads((ROOT / "compose.test.yaml").read_text()), p)
    images, containers = {}, []
    labels = {tool.SLOT_LABEL: p["slot"], "com.docker.compose.project": tool.PROJECT}
    network = {"Id": "e" * 64, "Name": "bb-test-u1-net", "Driver": "bridge", "Internal": True,
               "EnableIPv6": False, "Options": {"com.docker.network.bridge.gateway_mode_ipv4": "isolated"},
               "Labels": labels, "Containers": {}}
    volumes = [{"Name": "bb-test-u1-" + n, "Driver": "local", "Options": None,
                "Labels": labels | {"com.docker.compose.volume": n}} for n in expected["volumes"]]
    for name, service in expected["services"].items():
        images[name] = {"Id": p[name], "Config": {
            "Labels": {"org.opencontainers.image.revision": p["candidate"], "io.bigbrain.test.kind": name},
            "User": service["user"], "Env": ["PATH=/usr/bin:/bin"],
            "Entrypoint": service["entrypoint"], "Cmd": service["command"]}}
        containers.append({"Id": ("1" if name == "api" else "2") * 64, "Name": "/bb-test-u1-" + name,
            "Image": p[name], "Config": {"Image": p[name], "User": service["user"],
                "Labels": labels | {"com.docker.compose.service": name},
                "Entrypoint": service["entrypoint"], "Cmd": service["command"],
                "Env": ["PATH=/usr/bin:/bin"] + [k + "=" + v for k, v in service.get("environment", {}).items()]},
            "HostConfig": {"ReadonlyRootfs": True, "Privileged": False, "CapDrop": ["ALL"],
                "SecurityOpt": ["no-new-privileges:true"], "NetworkMode": "bb-test-u1-net", "IpcMode": "private",
                "NanoCpus": int(service["cpus"] * 1e9), "Memory": service["mem_limit"],
                "MemorySwap": service["memswap_limit"], "PidsLimit": 64, "RestartPolicy": {"Name": "no"},
                "LogConfig": {"Type": "json-file", "Config": {"max-size": "5m", "max-file": "2"}},
                "Tmpfs": {"/tmp": "rw,noexec,nosuid,size=67108864,mode=1777"},
                "PortBindings": {}},
            "NetworkSettings": {"Networks": {"bb-test-u1-net": {"NetworkID": network["Id"]}}},
            "State": {"Running": False},
            "Mounts": [{"Type": "volume", "Name": "bb-test-u1-" + v["source"], "Destination": v["target"],
                        "Driver": "local", "RW": True} for v in service.get("volumes", [])]})
    return p, expected, images, {"containers": containers, "networks": [network], "volumes": volumes}


class TestPolicy(unittest.TestCase):
    def setUp(self):
        self.p, self.expected, self.images, self.inventory = fixture()

    def slot(self):
        slot = object.__new__(tool.Slot)
        slot.root, slot.p, slot.expected, slot.images = Path("/unused"), self.p, self.expected, self.images
        slot.seal = {"files": {}, "allowed_clients": ["100.64.0.1"]}
        return slot

    def test_valid_inspect_fixture(self):
        for name, image in self.images.items():
            tool.validate_image(image, name, self.p, self.expected["services"][name])
        self.assertEqual(set(self.slot().audit(self.inventory)), {"api", "web"})

    def test_fixed_template_invariants(self):
        services = self.expected["services"]
        self.assertEqual(set(services), {"api", "web"})
        self.assertLessEqual(sum(s["cpus"] for s in services.values()), 1)
        self.assertLessEqual(sum(s["mem_limit"] for s in services.values()), 1536 * 1024**2)
        self.assertLessEqual(sum(s["pids_limit"] for s in services.values()), 128)
        env = services["api"]["environment"]
        self.assertEqual(env["Finance__ResearchOperations__MaintenancePaused"], "true")
        self.assertTrue(all(v == "false" for k, v in env.items() if k.endswith("__Enabled")))
        self.assertFalse(any("env_file" in s or "build" in s for s in services.values()))
        paths = {v["target"] for v in services["api"]["volumes"]}
        self.assertTrue(all(any(value.startswith(p + "/") for p in paths)
                            for key, value in env.items() if key.endswith(("DatabasePath", "Directory"))))

    def test_tags_revisions_and_wildcard_bind_rejected(self):
        for key, values in {"candidate": ["main", "a" * 39, "a" * 40 + "\n"],
                            "api": ["api:latest", "repo@sha256:" + "b" * 64],
                            "bind": ["", "0.0.0.0", "127.0.0.1", "::", "example.org"]}.items():
            for value in values:
                with self.subTest(key=key, value=value), self.assertRaises(tool.Rejected):
                    tool.parameters(**(self.p | {key: value}))

    def test_compose_tampering_rejected(self):
        changes = [
            lambda c: c["services"]["api"].update(privileged=True),
            lambda c: c["services"]["api"].update(env_file=[".env"]),
            lambda c: c["services"]["api"]["environment"].update(Finance__Eodhd__Enabled="true"),
            lambda c: c["services"]["web"].update(ports=[{"host_ip": "0.0.0.0", "target": 8080, "published": "13001"}]),
            lambda c: c["services"]["api"].update(network_mode="host"),
            lambda c: c["services"]["api"].update(cap_add=["SYS_ADMIN"]),
            lambda c: c["services"]["api"].update(image="api:latest"),
            lambda c: c["services"]["api"]["volumes"][0].update(type="bind", source="/var/run/docker.sock"),
            lambda c: c["networks"]["test"].update(internal=False),
            lambda c: c["networks"]["test"]["driver_opts"].clear(),
            lambda c: c["volumes"]["data"].update(external=True),
            lambda c: c["services"].update(worker=copy.deepcopy(c["services"]["api"])),
            lambda c: c["services"]["api"].update(devices=["/dev/sda"]),
            lambda c: c["services"]["api"].update(cpus=2),
            lambda c: c["services"]["api"].update(restart="always"),
        ]
        for index, change in enumerate(changes):
            with self.subTest(index=index), self.assertRaises(tool.Rejected):
                actual = copy.deepcopy(self.expected)
                change(actual)
                tool.validate_effective(actual, self.expected)

    def test_compose_normalization_does_not_hide_extra_settings(self):
        actual = copy.deepcopy(self.expected)
        actual["services"]["api"]["networks"]["test"] = {"priority": 0}
        tool.validate_effective(actual, self.expected)
        actual["services"]["api"]["networks"]["test"]["ipv4_address"] = "100.64.0.2"
        with self.assertRaises(tool.Rejected):
            tool.validate_effective(actual, self.expected)

    def test_compose_531_defaults_and_negative_mutations(self):
        actual = copy.deepcopy(self.expected)
        for service in actual["services"].values():
            for key in ("mem_limit", "memswap_limit"):
                service[key] = str(service[key])
            service["cpus"] = str(service["cpus"])
            service["networks"]["test"] = None
            for mount in service.get("volumes", []):
                mount["volume"] = {}
        actual["networks"]["test"]["ipam"] = {}
        tool.validate_effective(actual, self.expected)
        for value in ({"driver": "default"}, {"config": []}, None, [], {"unknown": True}):
            mutated = copy.deepcopy(actual)
            mutated["networks"]["test"]["ipam"] = value
            with self.subTest(ipam=value), self.assertRaises(tool.Rejected):
                tool.validate_effective(mutated, self.expected)
        for key in ("mem_limit", "memswap_limit"):
            for value in ("0", "1", "1g", "1.0", "+123", "-1", " 123", "１２３", "123\n"):
                mutated = copy.deepcopy(actual)
                mutated["services"]["api"][key] = value
                with self.subTest(key=key, value=value), self.assertRaises(tool.Rejected):
                    tool.validate_effective(mutated, self.expected)
        for section in (actual["networks"]["test"], actual["services"]["api"]):
            section["unknown"] = {}
            with self.assertRaises(tool.Rejected):
                tool.validate_effective(actual, self.expected)
            del section["unknown"]

    def test_test_sdk_derivation_changes_only_build_from(self):
        source = (ROOT / "src/BigBrain.Api/Dockerfile").read_text()
        sdk = json.loads((ROOT / "global.json").read_text())["sdk"]
        derived = build.derive(source, sdk)
        self.assertEqual(derived.splitlines()[1:], source.splitlines()[1:])
        self.assertEqual(derived.splitlines()[0], "FROM mcr.microsoft.com/dotnet/sdk:10.0.300 AS build")
        for changed in (source.replace("sdk:10.0 ", "sdk:10.0.404 "), source + "FROM mcr.microsoft.com/dotnet/sdk:10.0 AS extra\n"):
            with self.assertRaises(ValueError):
                build.derive(changed, sdk)
        with self.assertRaises(ValueError):
            build.derive(source, sdk | {"version": "10.0.400"})

    def test_image_contract_tampering(self):
        for change in [lambda c: c["Config"]["Labels"].update({"org.opencontainers.image.revision": "0" * 40}),
                       lambda c: c["Config"].update(User="0"),
                       lambda c: c["Config"].update(Volumes={"/secret": {}}),
                       lambda c: c["Config"].update(Healthcheck={"Test": ["CMD", "evil"]}),
                       lambda c: c["Config"]["Env"].append("Finance__Eodhd__Enabled=true"),
                       lambda c: c["Config"].update(Entrypoint=["sh", "-c"]),
                       lambda c: c.update(Id="sha256:" + "f" * 64)]:
            with self.subTest(change=change), self.assertRaises(tool.Rejected):
                image = copy.deepcopy(self.images["api"])
                change(image)
                tool.validate_image(image, "api", self.p, self.expected["services"]["api"])

    def test_actual_container_tampering(self):
        changes = [
            lambda c: c["HostConfig"].update(Binds=["/:/host"]),
            lambda c: c["HostConfig"].update(Privileged=True),
            lambda c: c["HostConfig"].update(CapAdd=["NET_ADMIN"]),
            lambda c: c["HostConfig"].update(SecurityOpt=[]),
            lambda c: c["HostConfig"].update(NetworkMode="host"),
            lambda c: c["HostConfig"].update(PidMode="host"),
            lambda c: c["HostConfig"].update(Devices=[{}]),
            lambda c: c["HostConfig"].update(Memory=0),
            lambda c: c["HostConfig"].update(NanoCpus=0),
            lambda c: c["HostConfig"].update(PidsLimit=0),
            lambda c: c["HostConfig"].update(PortBindings={"8080/tcp": [{"HostIp": "0.0.0.0", "HostPort": "18080"}]}),
            lambda c: c["Config"]["Env"].append("SECRET=unexpected"),
            lambda c: c["Mounts"][0].update(Name="production-data"),
            lambda c: c["Mounts"][0].update(Type="bind"),
            lambda c: c["NetworkSettings"]["Networks"].update(production={"NetworkID": "foreign"}),
        ]
        for index, change in enumerate(changes):
            with self.subTest(index=index), self.assertRaises(tool.Rejected):
                actual = copy.deepcopy(self.inventory["containers"][0])
                change(actual)
                tool.validate_container(actual, "api", self.p, self.expected, self.images["api"], "e" * 64)

    def test_network_isolated_flag_not_regular_internal(self):
        for update in [{"Internal": False}, {"EnableIPv6": True}, {"Options": {}}, {"Driver": "host"}]:
            with self.subTest(update=update), self.assertRaises(tool.Rejected):
                tool.validate_network(self.inventory["networks"][0] | update, self.p)

    def test_foreign_resource_collisions_fail_before_reset_mutation(self):
        for kind in ("containers", "networks", "volumes"):
            for label in (tool.SLOT_LABEL, "com.docker.compose.project"):
                inventory = copy.deepcopy(self.inventory)
                labels = inventory[kind][0]["Config"]["Labels"] if kind == "containers" else inventory[kind][0]["Labels"]
                labels[label] = "foreign"
                slot = self.slot()
                with self.subTest(kind=kind, label=label), patch.object(slot, "inventory", return_value=inventory), \
                     patch.object(slot, "receipt"), patch.object(tool, "run") as docker:
                    with self.assertRaises(tool.Rejected):
                        slot.reset("bb-test-u1-fixtures")
                    docker.assert_not_called()

    def test_external_volume_and_foreign_network_attachment_fail(self):
        with self.assertRaises(tool.Rejected):
            tool.validate_volume(self.inventory["volumes"][0] | {"Options": {"device": "/production", "o": "bind"}}, "data", self.p)
        self.inventory["networks"][0]["Containers"] = {"foreign": {}}
        with self.assertRaises(tool.Rejected):
            self.slot().audit(self.inventory)

    def test_reset_confirmation_precedes_every_mutation(self):
        with patch.object(tool, "run") as docker, self.assertRaises(tool.Rejected):
            self.slot().reset(None)
        docker.assert_not_called()

    def test_corrupt_created_container_never_starts(self):
        bad = copy.deepcopy(self.inventory)
        bad["containers"][0]["HostConfig"]["Privileged"] = True
        slot = self.slot()
        empty = {k: [] for k in self.inventory}
        with patch.object(slot, "inventory", side_effect=[empty, bad]), patch.object(tool, "compose") as compose, \
             patch.object(tool, "run") as docker, self.assertRaises(tool.Rejected):
            slot.start()
        compose.assert_called_once_with(slot.root, "create", "--no-build", "--pull", "never")
        docker.assert_not_called()

    def test_failed_http_start_stops_only_validated_ids(self):
        slot = self.slot()
        with patch.object(slot, "inventory", return_value=self.inventory), patch.object(slot, "save_state"), \
             patch.object(slot, "receipt"), patch.object(slot, "internal_health", side_effect=tool.Rejected("probe")), \
             patch.object(tool, "run") as docker, self.assertRaises(tool.Rejected):
            slot.start()
        self.assertEqual(docker.call_args.args, (slot.root, "container", "stop", "--time", "35", "1" * 64, "2" * 64))
        self.assertEqual(docker.call_count, 3)

    def test_start_failure_evidence_precedes_cleanup_even_when_collector_fails(self):
        for stage in ("start-api", "internal-health"):
            with self.subTest(stage=stage):
                slot = self.slot()
                order = []
                def docker(*args):
                    order.append(args[1])
                    if stage == "start-api" and args[1:3] == ("container", "start"):
                        raise tool.DockerRejected(SimpleNamespace(
                            returncode=125, stdout="", stderr="private runtime failure"))
                def retain(*args):
                    order.append("evidence")
                    self.assertEqual(args[1], stage)
                    raise OSError("full disk")
                with patch.object(slot, "inventory", return_value=self.inventory), \
                     patch.object(slot, "save_state"), patch.object(slot, "receipt"), \
                     patch.object(slot, "failure_evidence", side_effect=retain), \
                     patch.object(slot, "internal_health", side_effect=tool.Rejected("probe")), \
                     patch.object(tool, "run", side_effect=docker), self.assertRaises(tool.Rejected):
                    slot.start()
                self.assertLess(order.index("evidence"), len(order) - 1)
                self.assertEqual(order[-1], "container")

    def test_private_failure_evidence_is_bound_to_owned_ids_and_not_public_output(self):
        slot = self.slot()
        slot.health_attempts = [{"attempt": 12, "outcome": "rejected"}]
        calls = []
        def observe(root, *args):
            calls.append(args)
            if args[:2] == ("container", "inspect"):
                info = copy.deepcopy(self.inventory["containers"][0 if args[-1] == "1" * 64 else 1])
                info["State"].update(ExitCode=137, OOMKilled=True, Status="exited",
                                     Error="must never appear in narrowed state")
                return {"status": "complete", "returncode": 0, "output": json.dumps([info])}
            return {"status": "complete", "returncode": 0, "output": "PRIVATE_LOG_CANARY"}
        with tempfile.TemporaryDirectory() as directory:
            slot.root = Path(directory)
            with patch.object(tool, "diagnostic_docker", side_effect=observe), \
                 patch.object(slot, "receipt") as receipt:
                slot.failure_evidence({"api": "1" * 64, "web": "2" * 64},
                    "internal-health", tool.time.monotonic(), tool.Rejected("safe"))
            paths = list(slot.root.glob("start-failure-*.json"))
            self.assertEqual(len(paths), 1)
            self.assertEqual(stat.S_IMODE(paths[0].stat().st_mode), 0o600)
            data = json.loads(paths[0].read_text())
            self.assertEqual(data["containers"]["api"]["state"]["ExitCode"], 137)
            self.assertTrue(data["containers"]["api"]["state"]["OOMKilled"])
            self.assertNotIn("Error", data["containers"]["api"]["state"])
            self.assertIn("PRIVATE_LOG_CANARY", paths[0].read_text())
            self.assertNotIn("PRIVATE_LOG_CANARY", str(receipt.call_args))
            self.assertEqual(receipt.call_args.kwargs["sha256"], tool.digest(paths[0]))
            self.assertEqual(len(calls), 4)

    def test_failure_diagnostic_never_reads_logs_or_cgroups_of_changed_identity(self):
        slot = self.slot()
        slot.seal["engine"] = {"mode": "rootless", "uid": os.getuid(), "id": "fixed"}
        bad = copy.deepcopy(self.inventory["containers"][0])
        bad["Config"]["Labels"][tool.SLOT_LABEL] = "f" * 32
        observed = {"status": "complete", "returncode": 0, "output": json.dumps([bad])}
        with tempfile.TemporaryDirectory() as directory:
            slot.root = Path(directory)
            with patch.object(tool, "diagnostic_docker", return_value=observed) as docker, \
                 patch.object(tool, "diagnostic_cgroup", return_value={}) as counters, \
                 patch.object(slot, "receipt"):
                slot.failure_evidence({"api": "1" * 64, "web": "2" * 64},
                    "internal-health", tool.time.monotonic(), tool.Rejected("safe"))
            self.assertEqual(docker.call_count, 2)
            self.assertTrue(all(c.args[1:3] == ("container", "inspect") for c in docker.call_args_list))
            self.assertEqual(counters.call_count, 2)  # fixed task and engine only
            data = json.loads(next(slot.root.glob("start-failure-*.json")).read_text())
            self.assertTrue(all(row["identity_verified"] is False for row in data["containers"].values()))

    def test_health_failure_records_all_attempts_without_raw_body_and_keeps_original_policy(self):
        slot = self.slot()
        private = tool.DockerRejected(SimpleNamespace(returncode=1, stdout="", stderr="PRIVATE_ERROR_CANARY"))
        with patch.object(tool, "run", side_effect=private), patch.object(tool.time, "sleep") as delay, \
             self.assertRaises(tool.DockerRejected):
            slot.internal_health("2" * 64)
        self.assertEqual(len(slot.health_attempts), 12)
        self.assertEqual(delay.call_count, 11)
        self.assertTrue(all(row["docker_exit"] == 1 for row in slot.health_attempts))
        self.assertNotIn("PRIVATE_ERROR_CANARY", json.dumps(slot.health_attempts))
        self.assertNotIn("PRIVATE_ERROR_CANARY", str(private))

    def test_diagnostic_cli_output_and_time_are_bounded_and_children_reaped(self):
        actual_popen = subprocess.Popen
        for program, expected in (("import os; os.write(1, b'x'*100000)", "truncated"),
                                  ("import time; time.sleep(20)", "timeout")):
            children = []
            def spawn(*args, **kwargs):
                children.append(actual_popen([sys.executable, "-c", program],
                    stdin=kwargs["stdin"], stdout=kwargs["stdout"], stderr=kwargs["stderr"]))
                return children[-1]
            with patch.object(tool.subprocess, "Popen", side_effect=spawn), \
                 patch.object(tool, "clean_environment", return_value={}):
                began = tool.time.monotonic()
                result = tool.diagnostic_docker(Path("/unused"), "logs", "1" * 64)
            self.assertEqual(result["status"], expected)
            self.assertLessEqual(len(result["output"]), 65536)
            self.assertLess(tool.time.monotonic() - began, 5)
            self.assertIsNotNone(children[0].poll())
            self.assertTrue(children[0].stdout.closed)

    def test_missing_cgroup_is_unavailable_not_zero(self):
        self.assertTrue(all(value is None for value in
            tool.diagnostic_cgroup(Path("/nonexistent-bigbrain-cgroup")).values()))

    def test_network_id_can_be_unassigned_only_before_start(self):
        info = self.inventory["containers"][0]
        info["NetworkSettings"]["Networks"]["bb-test-u1-net"]["NetworkID"] = ""
        tool.validate_container(info, "api", self.p, self.expected, self.images["api"], "e" * 64)
        info["State"]["Running"] = True
        with self.assertRaises(tool.Rejected):
            tool.validate_container(info, "api", self.p, self.expected, self.images["api"], "e" * 64)

    def test_installed_clients_reject_wildcards_ranges_ipv6_and_missing_policy(self):
        self.assertEqual(tool.client_addresses(["100.64.0.2", "100.64.0.1"]), ["100.64.0.1", "100.64.0.2"])
        for values in (None, [], ["0.0.0.0"], ["192.0.2.1"], ["100.64.0.0/10"],
                       ["::1"], ["100.64.0.1", "100.64.0.1"], ["100.64.0.1; echo nope"]):
            with self.subTest(values=values), self.assertRaises(tool.Rejected):
                tool.client_addresses(values)

    def test_relay_denies_unlisted_sources_before_target_or_exec(self):
        slot = self.slot()
        for address in ("192.0.2.1", "100.64.0.2", "::1"):
            client = MagicMock()
            with patch.object(slot, "relay_target") as target, patch.object(tool.subprocess, "Popen") as spawn:
                self.assertFalse(slot.accept_client(client, (address, 4567)))
                client.close.assert_called_once()
                target.assert_not_called()
                spawn.assert_not_called()
        client = MagicMock()
        self.assertTrue(slot.accept_client(client, ("100.64.0.1", 4567)))
        client.close.assert_not_called()

    def test_real_engine_named_volume_binds_and_host_bind_rejection(self):
        info = copy.deepcopy(self.inventory["containers"][0])
        info["HostConfig"]["Binds"] = ["bb-test-u1-" + v["source"] + ":" + v["target"] + ":rw"
                                      for v in self.expected["services"]["api"]["volumes"]]
        tool.validate_container(info, "api", self.p, self.expected, self.images["api"], "e" * 64)
        valid = list(info["HostConfig"]["Binds"])
        for binds in (["/var/run/docker.sock:/var/run/docker.sock:rw"], valid + ["/home:/host:ro"],
                      valid[:-1], [valid[0]] * len(valid), [x.replace(":rw", ":ro") for x in valid],
                      [x.replace("bb-test-u1-", "production-") for x in valid]):
            info["HostConfig"]["Binds"] = binds
            with self.subTest(binds=binds), self.assertRaises(tool.Rejected):
                tool.validate_container(info, "api", self.p, self.expected, self.images["api"], "e" * 64)

    def test_relay_command_has_no_network_selected_arguments(self):
        args = tool.relay_argv("2" * 64)
        self.assertEqual(args, ["/usr/bin/docker", "exec", "-i", "2" * 64, "/bin/busybox", "timeout",
                               "-s", "KILL", "35", "/bin/busybox", "nc", "-w", "30", "127.0.0.1", "8080"])
        for target in ("bb-test-u1-web", "foreign", "2" * 64 + "; echo evil"):
            with self.assertRaises(tool.Rejected):
                tool.relay_argv(target)

    def test_relay_pins_image_and_state_before_exec(self):
        slot = self.slot()
        state = {"candidate": self.p["candidate"], "containers": {"web": "2" * 64}, "networks": ["e" * 64]}
        web = copy.deepcopy(self.inventory["containers"][1])
        web["State"]["Running"] = True
        with patch.object(tool, "installation"), patch.object(tool, "read_json", return_value=state), \
             patch.object(tool, "run", return_value=json.dumps([web])):
            slot.relay_target("2" * 64)
            web["Image"] = "sha256:" + "0" * 64
            with patch.object(tool, "run", return_value=json.dumps([web])), self.assertRaises(tool.Rejected):
                slot.relay_target("2" * 64)
            state["containers"]["web"] = "3" * 64
            with self.assertRaises(tool.Rejected):
                slot.relay_target("2" * 64)

    def test_relay_refuses_unverified_target_without_spawning(self):
        slot = self.slot()
        client, capacity = MagicMock(), threading.BoundedSemaphore(1)
        capacity.acquire()
        with patch.object(slot, "relay_target", side_effect=tool.Rejected("changed")), \
             patch.object(tool.subprocess, "Popen") as spawn:
            slot.relay_connection(client, "2" * 64, threading.Event(), capacity)
        spawn.assert_not_called()
        self.assertTrue(capacity.acquire(blocking=False))

    def test_relay_child_cleanup_on_error(self):
        slot = self.slot()
        client, child, capacity = MagicMock(), MagicMock(), threading.BoundedSemaphore(1)
        child.poll.return_value = None
        capacity.acquire()
        with patch.object(slot, "relay_target"), patch.object(tool.subprocess, "Popen", return_value=child), \
             patch.object(tool, "pump", side_effect=OSError("disconnected")):
            slot.relay_connection(client, "2" * 64, threading.Event(), capacity)
        child.terminate.assert_called_once()
        child.wait.assert_called_once_with(timeout=2)
        child.stdin.close.assert_called_once()
        child.stdout.close.assert_called_once()
        self.assertTrue(capacity.acquire(blocking=False))

    def test_relay_pump_copies_only_bytes_over_local_socketpair(self):
        # AF_UNIX socketpair and an owned Python echo child: no listener/network/Docker.
        try:
            caller, relay = socket.socketpair()
        except PermissionError:
            self.skipTest("sandbox denies AF_UNIX socketpair; byte transfer NOT RUN")
        with caller, relay:
            try:
                caller.settimeout(3)
                caller.sendall(b"HTTPdemo")
            except PermissionError:
                self.skipTest("sandbox denies AF_UNIX send; byte transfer NOT RUN")
            with subprocess.Popen(
                    [sys.executable, "-c", "import os; data=os.read(0,8); os.write(1,b'REPLY:'+data)"],
                    stdin=subprocess.PIPE, stdout=subprocess.PIPE) as child:
                tool.pump(relay, child, threading.Event())
                self.assertEqual(caller.recv(64), b"REPLY:HTTPdemo")
                child.wait(timeout=2)

    def test_clean_environment_and_explicit_empty_env_file(self):
        root = Path("/synthetic-installation")
        with patch.dict(os.environ, {"COMPOSE_FILE": "/evil", "DOCKER_HOST": "tcp://evil", "API_KEY": "fake", "HTTP_PROXY": "fake"}):
            clean = tool.clean_environment(root)
        self.assertEqual(set(clean), {"PATH", "HOME", "DOCKER_CONFIG", "DOCKER_HOST", "LANG"})
        self.assertEqual(clean["DOCKER_HOST"], "unix:///var/run/docker.sock")
        with patch.object(tool, "run", return_value="{}") as docker:
            tool.compose(root, "config", "--format", "json")
        args = docker.call_args.args
        self.assertIn("--env-file", args)
        self.assertEqual(args[args.index("--env-file") + 1], str(root / "EMPTY"))
        self.assertEqual(args[args.index("--project-directory") + 1], str(root))

    def test_rootless_metadata_binds_identity_mode_and_cgroups(self):
        profile = {"mode": "rootless", "uid": os.getuid(), "id": "dedicated-daemon"}
        info = {"ID": profile["id"], "SecurityOptions": ["name=seccomp,profile=builtin", "name=rootless"],
                "CgroupVersion": "2", "CgroupDriver": "systemd"}
        tool.validate_engine(info, tool.engine_profile({"engine": profile}))
        for change in ({"ID": "other-daemon"}, {"SecurityOptions": ["name=seccomp"]},
                       {"SecurityOptions": ["name=rootless"]}, {"CgroupVersion": "1"},
                       {"CgroupDriver": "cgroupfs"}):
            with self.subTest(change=change), self.assertRaises(tool.Rejected):
                tool.validate_engine(info | change, profile)
        for change in ({"mode": "tcp://evil"}, {"uid": os.getuid() + 1}, {"id": ""}, {"host": "/evil"}):
            with self.subTest(change=change), self.assertRaises(tool.Rejected):
                tool.engine_profile({"engine": profile | change})
        tool.validate_engine({"SecurityOptions": ["name=seccomp,profile=builtin"]}, tool.engine_profile({}))
        with self.assertRaises(tool.Rejected):
            tool.validate_engine(info, tool.engine_profile({}))
        with self.assertRaises(tool.Rejected):
            tool.engine_profile({"engine": None})

    def test_rootless_socket_rejects_symlink_owner_type_and_public_parent(self):
        uid = os.getuid()
        directory = SimpleNamespace(st_mode=stat.S_IFDIR | 0o700, st_uid=uid)
        endpoint = SimpleNamespace(st_mode=stat.S_IFSOCK | 0o600, st_uid=uid)
        with patch.object(Path, "lstat", side_effect=[directory, directory, endpoint]):
            self.assertEqual(str(tool.rootless_socket()), f"/run/user/{uid}/bigbrain-test-rootless/docker.sock")
        for index, replacement in (
                (0, SimpleNamespace(st_mode=stat.S_IFLNK | 0o777, st_uid=uid)),
                (1, SimpleNamespace(st_mode=stat.S_IFDIR | 0o755, st_uid=uid)),
                (1, SimpleNamespace(st_mode=stat.S_IFDIR | 0o700, st_uid=uid + 1)),
                (2, SimpleNamespace(st_mode=stat.S_IFLNK | 0o777, st_uid=uid)),
                (2, SimpleNamespace(st_mode=stat.S_IFREG | 0o600, st_uid=uid)),
                (2, SimpleNamespace(st_mode=stat.S_IFSOCK | 0o600, st_uid=uid + 1))):
            metadata = [directory, directory, endpoint]
            metadata[index] = replacement
            with self.subTest(index=index, replacement=replacement), \
                 patch.object(Path, "lstat", side_effect=metadata), self.assertRaises(tool.Rejected):
                tool.rootless_socket()

    def test_rootless_snapshot_and_clients_ignore_inherited_engine(self):
        root = Path("/synthetic-installation")
        fixed = Path(f"/run/user/{os.getuid()}/bigbrain-test-rootless/docker.sock")
        info = {"ID": "dedicated-daemon", "SecurityOptions": ["name=seccomp", "name=rootless"],
                "CgroupVersion": "2", "CgroupDriver": "systemd"}
        seal = {}
        contamination = {"DOCKER_HOST": "tcp://evil", "DOCKER_CONTEXT": "evil", "DOCKER_CONFIG": "/evil",
                         "DOCKER_TLS_VERIFY": "1", "DOCKER_CERT_PATH": "/evil", "HOME": "/evil"}
        with patch.dict(os.environ, contamination), patch.object(tool, "rootless_socket", return_value=fixed), \
             patch.object(tool.subprocess, "run", return_value=SimpleNamespace(returncode=0, stdout=json.dumps(info))) as docker:
            tool.bind_rootless_engine(root, seal)
            env = docker.call_args.kwargs["env"]
            self.assertEqual(docker.call_args.args[0], ["/usr/bin/docker", "info", "--format", "{{json .}}"])
            self.assertEqual(seal["engine"], {"mode": "rootless", "uid": os.getuid(), "id": info["ID"]})
            with patch.object(Path, "exists", return_value=True), patch.object(tool, "read_json", return_value=seal):
                self.assertEqual(tool.clean_environment(root), env)
                tool.run(root, "info")
                self.assertEqual(docker.call_args.kwargs["env"], env)
                slot = self.slot()
                slot.root = root
                client, capacity = MagicMock(), threading.BoundedSemaphore(1)
                capacity.acquire()
                with patch.object(slot, "relay_target"), patch.object(tool, "pump"), \
                     patch.object(tool.subprocess, "Popen") as spawn:
                    slot.relay_connection(client, "2" * 64, threading.Event(), capacity)
                self.assertEqual(spawn.call_args.kwargs["env"], env)
            self.assertEqual(env["DOCKER_HOST"], "unix://" + str(fixed))
            self.assertEqual(env["DOCKER_CONFIG"], str(root / "client"))
            self.assertNotIn("DOCKER_CONTEXT", env)
            self.assertNotIn("DOCKER_TLS_VERIFY", env)

    def test_preflight_rejects_changed_daemon_before_compose_or_images(self):
        slot = self.slot()
        slot.seal["engine"] = {"mode": "rootless", "uid": os.getuid(), "id": "installed"}
        info = {"ID": "replacement", "SecurityOptions": ["name=seccomp", "name=rootless"],
                "CgroupVersion": "2", "CgroupDriver": "systemd"}
        with patch.object(tool, "run", return_value=json.dumps(info)) as docker, \
             patch.object(tool, "compose") as compose, self.assertRaises(tool.Rejected):
            slot.preflight()
        docker.assert_called_once_with(slot.root, "info", "--format", "{{json .}}")
        compose.assert_not_called()

    def test_concurrent_lifecycle_fails_without_wait(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            with tool.locked(root), self.assertRaises(tool.Rejected):
                with tool.locked(root):
                    self.fail("concurrent operation acquired lock")

    def test_installed_template_and_validator_drift(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            (root / "client").mkdir()
            for name, data in {"test-environment.py": b"reviewed", "compose.test.yaml": b"{}", "EMPTY": b""}.items():
                (root / name).write_bytes(data)
            seal = {"version": 1, "allowed_clients": ["100.64.0.1"], "files": {p.name: tool.digest(p) for p in root.iterdir() if p.is_file()}}
            (root / "installation.json").write_text(json.dumps(seal))
            tool.installation(root)
            for name in ("test-environment.py", "compose.test.yaml", "EMPTY"):
                original = (root / name).read_bytes()
                (root / name).write_bytes(b"tampered")
                with self.subTest(name=name), self.assertRaises(tool.Rejected):
                    tool.installation(root)
                (root / name).write_bytes(original)

    def test_http_probe_rejects_external_references_before_connecting(self):
        for path in ("https://provider.example", "//provider.example", "relative"):
            with self.subTest(path=path), patch.object(tool.urllib.request, "build_opener") as opener, self.assertRaises(tool.Rejected):
                self.slot().request(path)
            opener.assert_not_called()

    def test_ci_has_independent_package_without_weakening_checks(self):
        text = (ROOT / ".github/workflows/ci.yml").read_text()
        self.assertIn("needs: [backend, frontend, documentation, secrets]", text)
        self.assertIn("always() && !cancelled()", text)
        self.assertIn("github.event.pull_request.head.repo.full_name == github.repository", text)
        self.assertIn("commit.data.author?.login !== owner", text)
        self.assertIn("persist-credentials: false", text)
        self.assertNotIn("continue-on-error", text)
        self.assertNotIn("self-hosted", text)
        package = text.split("  test-candidate-package:", 1)[1]
        self.assertIn("needs.secrets.result == 'success'", package.split("runs-on:", 1)[0])
        self.assertNotRegex(package.replace("needs.secrets.result", "NEEDS_RESULT"), r"\bsecrets\s*(?:\.|\[)")
        for job in ("backend", "frontend", "documentation"):
            self.assertNotIn("needs." + job + ".result == 'success'", package)
        self.assertIn('scripts/test-candidate-dockerfile.py', package)
        self.assertIn("'build': json.loads((p / 'build.json').read_text())", package)


if __name__ == "__main__":
    unittest.main()
