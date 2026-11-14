"""Offline U3 boundary tests. No gh/network/Docker/systemd runtime is invoked."""
import copy
import contextlib
import gzip
import hashlib
import importlib.util
import io
import json
import os
from pathlib import Path
import signal
import stat
import subprocess
import tarfile
import tempfile
import threading
import unittest
from unittest.mock import Mock, patch
import warnings
import zipfile

SOURCE = Path(__file__).resolve().parents[2] / "scripts/test-environment-task.py"
spec = importlib.util.spec_from_file_location("test_task_tool", SOURCE)
tool = importlib.util.module_from_spec(spec)
spec.loader.exec_module(tool)

TASK = {"task_id": "a" * 32, "source_sha": "b" * 40, "tree": "c" * 40,
        "run_id": 123, "attempt": 1, "artifact_id": 456, "pr_number": 85, "runtime_seconds": 30}
IDENTITY = {"ref": "refs/heads/checkpoint/demo", "tested_sha": "d" * 40,
            "tests": {"backend": "success", "frontend": "failure",
                      "documentation": "success", "secrets": "success"},
            "artifact_digest": "sha256:" + "e" * 64}


class TestTask(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.root = Path(self.temp.name)
        self.state = self.root / "state"
        self.state.mkdir(mode=0o700)
        (self.state / "tasks").mkdir(mode=0o700)
        self.install = self.root / "install"
        self.install.mkdir(mode=0o700)
        self.controller = self.root / "controller"
        self.controller.mkdir(mode=0o700)
        self.cfg = {"version": 1, "trusted_base": "f" * 40, "workflow_sha256": "1" * 64,
                    "wrapper_sha256": "2" * 64, "controller": str(self.controller),
                    "controller_files": {name: "3" * 64 for name in tool.CONTROL},
                    "bind_ip": "100.64.0.1"}
        self.patches = [patch.object(tool, "STATE", self.state),
                        patch.object(tool, "INSTALL", self.install)]
        for p in self.patches:
            p.start()
        tool.CANCEL.clear()

    def tearDown(self):
        tool.CANCEL.clear()
        for p in reversed(self.patches):
            p.stop()
        self.temp.cleanup()

    def write(self, path, value):
        tool.atomic(path, value)

    def folder(self, task=TASK):
        folder = self.state / "tasks" / task["task_id"]
        folder.mkdir(mode=0o700)
        return folder

    def tar_bytes(self, entries=None):
        out = io.BytesIO()
        with tarfile.open(fileobj=out, mode="w", format=tarfile.USTAR_FORMAT) as archive:
            for name, kind, data in entries or [("manifest.json", tarfile.REGTYPE, b"[]")]:
                info = tarfile.TarInfo(name)
                info.type = kind
                if kind in (tarfile.SYMTYPE, tarfile.LNKTYPE):
                    info.linkname = "/outside"
                info.size = len(data) if kind == tarfile.REGTYPE else 0
                archive.addfile(info, io.BytesIO(data) if info.size else None)
        return out.getvalue()

    def package(self, folder, task=TASK):
        (folder / "images.tar.gz").write_bytes(gzip.compress(self.tar_bytes()))
        images = [{"Id": "sha256:" + str(n) * 64,
                   "Config": {"Labels": {"org.opencontainers.image.revision": task["source_sha"],
                                        "io.bigbrain.test.kind": kind}}}
                  for n, kind in enumerate(("api", "web"), 1)]
        self.write(folder / "images.json", images)
        self.write(folder / "build.json", {"sdk": "synthetic"})
        os.chmod(folder / "images.tar.gz", 0o600)
        package = {"source_sha": task["source_sha"], "tree": task["tree"], "repository": tool.REPO,
                   "trusted_owner": True, "ref": IDENTITY["ref"], "tested_sha": IDENTITY["tested_sha"],
                   "workflow_run": str(task["run_id"]), "workflow_attempt": str(task["attempt"]),
                   "tests": IDENTITY["tests"], "build": {"sdk": "synthetic"},
                   "images": dict(zip(("api", "web"), (x["Id"] for x in images))),
                   "checksums": {n: tool.sha_file(folder / n)
                                 for n in ("images.tar.gz", "images.json", "build.json")}}
        self.write(folder / "package.json", package)
        sums = "".join(tool.sha_file(folder / n) + "  " + n + "\n"
                       for n in sorted(tool.FILES - {"SHA256SUMS"}))
        (folder / "SHA256SUMS").write_text(sums)
        os.chmod(folder / "SHA256SUMS", 0o600)
        return package

    def zip_file(self, items):
        path = self.state / "archive.zip"
        with warnings.catch_warnings():
            warnings.simplefilter("ignore", UserWarning)
            with zipfile.ZipFile(path, "w", compression=zipfile.ZIP_DEFLATED) as archive:
                for name, data in items:
                    archive.writestr(name, data)
        return path

    def test_manifest_exact_schema_rejects_injection_and_wrong_types(self):
        self.assertEqual(tool.manifest(copy.deepcopy(TASK)), TASK)
        for key, value in (("path", "/tmp/evil"), ("env", {"GH_TOKEN": "canary"}),
                           ("hook", "echo evil"), ("source_sha", "$(id)"),
                           ("task_id", "../outside"), ("attempt", True),
                           ("runtime_seconds", 1801), ("run_id", "-XPOST")):
            bad = {**TASK, key: value}
            with self.subTest(key=key), self.assertRaises(tool.Rejected):
                tool.manifest(bad)

    def test_duplicate_json_keys_rejected(self):
        with self.assertRaises(tool.Rejected):
            tool.decode('{"task_id":"a","task_id":"b"}')

    def test_atomic_private_durable_and_symlink_rejected(self):
        path = self.state / "receipt.json"
        self.write(path, {"phase": "claimed"})
        self.assertEqual(tool.document(path)["phase"], "claimed")
        self.assertEqual(stat.S_IMODE(path.stat().st_mode), 0o600)
        target = self.state / "link"
        target.symlink_to(path)
        with self.assertRaises(tool.Rejected):
            self.write(target, {})
        with self.assertRaises(tool.Rejected):
            tool.sha_file(target)

    def test_lock_rejects_overlap(self):
        with tool.lock():
            with self.assertRaises(tool.Rejected):
                with tool.lock():
                    self.fail("second lock admitted")

    def test_environment_credentials_are_fetch_only(self):
        with patch.dict(os.environ, {"GH_TOKEN": "canary", "GITHUB_TOKEN": "canary",
                                    "DOCKER_HOST": "tcp://bad", "HTTP_PROXY": "canary",
                                    "PYTHONPATH": "/candidate", "BASH_ENV": "/candidate"}, clear=False):
            fetch = tool.gh_env()
            client = tool.control_env(self.cfg)
        self.assertEqual(fetch["GH_CONFIG_DIR"], str(tool.HOME / ".config/gh"))
        self.assertNotEqual(fetch["HOME"], client["HOME"])
        for key in ("GH_TOKEN", "GITHUB_TOKEN", "GH_CONFIG_DIR", "HTTP_PROXY", "PYTHONPATH", "BASH_ENV"):
            self.assertNotIn(key, client)
        self.assertNotIn("DOCKER_HOST", fetch)
        self.assertTrue(client["DOCKER_HOST"].endswith("bigbrain-test-rootless/docker.sock"))
        self.assertEqual(client["DOCKER_CONFIG"], str(self.controller / "client"))

    def test_controller_uses_only_installed_isolated_python_and_clean_env(self):
        runner = tool.Runner(TASK, self.cfg, self.folder())
        with patch.object(tool, "config", return_value=self.cfg), patch.object(tool, "process") as process:
            runner.control("validate")
        argv, env, cwd = process.call_args.args
        self.assertEqual(argv, ["/usr/bin/python3", "-I",
                              str(self.controller / "test-environment.py"), "validate"])
        self.assertEqual(env, tool.control_env(self.cfg))
        self.assertEqual(cwd, self.controller)
        with patch.object(tool, "config", return_value={**self.cfg, "bind_ip": "100.64.0.2"}):
            with self.assertRaises(tool.Rejected):
                runner.control("start")

    def test_download_numeric_fixed_endpoint_and_fetch_env(self):
        with patch.object(tool, "process", return_value=b'{"id":456}') as process:
            self.assertEqual(tool.gh("repos/T-bear/BigBrain/actions/artifacts/456"), {"id": 456})
        argv, env, cwd = process.call_args.args
        self.assertEqual(argv[:4], ["/usr/bin/gh", "api", "--hostname", "github.com"])
        self.assertEqual(env, tool.gh_env())
        self.assertEqual(cwd, self.state)
        for endpoint in ("https://evil", "repos/other/repo/actions", "repos/T-bear/BigBrain/../secrets"):
            with self.assertRaises(tool.Rejected):
                tool.gh(endpoint)

    def metadata(self):
        run = {"id": 123, "run_attempt": 1, "status": "completed", "conclusion": "failure",
               "repository": {"full_name": tool.REPO}, "head_repository": {"full_name": tool.REPO},
               "actor": {"login": tool.OWNER}, "triggering_actor": {"login": tool.OWNER},
               "head_branch": "checkpoint/demo", "head_sha": TASK["source_sha"],
               "path": tool.WORKFLOW, "event": "pull_request", "workflow_id": 12,
               "pull_requests": [{"number": 85}]}
        jobs = []
        for name, result in [*IDENTITY["tests"].items(),
                             ("TEST candidate package (not a required acceptance check)", "success")]:
            jobs.append({"name": name, "conclusion": result, "status": "completed",
                         "run_id": 123, "head_sha": TASK["source_sha"],
                         "steps": [{"name": name, "conclusion": "success"} for name in (
                             "Resolve owned exact source before running candidate code",
                             "Build both exact-source images (mandatory even when tests are red)",
                             "Save images and exact provenance")]})
        return {
            "actions/runs/123/attempts/1": run,
            "actions/workflows/12": {"id": 12, "path": tool.WORKFLOW, "state": "active"},
            "git/ref/heads/checkpoint/demo": {"object": {"sha": TASK["source_sha"]}},
            "commits/" + TASK["source_sha"]: {"sha": TASK["source_sha"], "author": {"login": tool.OWNER},
                                             "commit": {"tree": {"sha": TASK["tree"]}}},
            "pulls/85": {"user": {"login": tool.OWNER},
                         "head": {"repo": {"full_name": tool.REPO}, "sha": TASK["source_sha"],
                                  "ref": "checkpoint/demo"}, "merge_commit_sha": IDENTITY["tested_sha"]},
            "actions/runs/123/attempts/1/jobs?per_page=100": {"total_count": 5, "jobs": jobs},
            "actions/artifacts/456": {"id": 456, "expired": False, "size_in_bytes": 1024,
                                     "name": "test-candidate-" + TASK["source_sha"] + "-123-1",
                                     "digest": IDENTITY["artifact_digest"],
                                     "workflow_run": {"id": 123, "head_sha": TASK["source_sha"],
                                                      "head_branch": "checkpoint/demo"}}}

    def identity(self, data):
        def fetch(endpoint, **kwargs):
            return data[endpoint.removeprefix("repos/" + tool.REPO + "/")]
        with patch.object(tool, "gh", side_effect=fetch), patch.object(tool, "workflow_at") as workflow:
            result = tool.github_identity(TASK, self.cfg)
        return result, workflow

    def test_github_owner_identity_preserves_red_and_checks_trusted_workflow(self):
        result, workflow = self.identity(self.metadata())
        self.assertEqual(result, IDENTITY)
        self.assertEqual(workflow.call_args_list[0].args, (self.cfg["trusted_base"], self.cfg["workflow_sha256"]))
        self.assertEqual(workflow.call_count, 3)

    def test_github_mismatches_block_before_download_or_load(self):
        mutations = [
            ("actions/runs/123/attempts/1", "path", ".github/workflows/candidate.yml"),
            ("actions/runs/123/attempts/1", "run_attempt", 2),
            ("actions/runs/123/attempts/1", "head_sha", "0" * 40),
            ("actions/runs/123/attempts/1", "head_branch", "checkpoint/../evil"),
            ("actions/runs/123/attempts/1", "event", "push"),
            ("actions/artifacts/456", "expired", True),
            ("actions/artifacts/456", "digest", ""),
            ("actions/artifacts/456", "name", "wrong-attempt"),
        ]
        for endpoint, key, value in mutations:
            data = self.metadata()
            data[endpoint][key] = value
            with self.subTest(key=key), self.assertRaises(tool.Rejected):
                self.identity(data)
        for badjob in ("secrets", "TEST candidate package (not a required acceptance check)"):
            data = self.metadata()
            for job in data["actions/runs/123/attempts/1/jobs?per_page=100"]["jobs"]:
                if job["name"] == badjob:
                    job["conclusion"] = "failure"
            with self.subTest(job=badjob), self.assertRaises(tool.Rejected):
                self.identity(data)
        for field in ("actor", "triggering_actor"):
            data = self.metadata()
            data["actions/runs/123/attempts/1"][field]["login"] = "other"
            with self.subTest(field=field), self.assertRaises(tool.Rejected):
                self.identity(data)

    def test_github_empty_run_pr_list_uses_explicit_exact_pr_identity(self):
        data = self.metadata()
        data["actions/runs/123/attempts/1"]["pull_requests"] = []
        self.assertEqual(self.identity(data)[0], IDENTITY)
        data["pulls/85"]["head"]["sha"] = "0" * 40
        with self.assertRaises(tool.Rejected):
            self.identity(data)

    def test_workflow_bytes_must_equal_reviewed_base(self):
        with patch.object(tool, "gh", return_value=b"candidate changed workflow"):
            with self.assertRaises(tool.Rejected):
                tool.workflow_at(TASK["source_sha"], self.cfg["workflow_sha256"])

    def test_zip_exact_entries_and_checksum_provenance_pass(self):
        source = self.folder()
        self.package(source)
        archive = self.zip_file([(name, (source / name).read_bytes()) for name in tool.FILES])
        target = self.state / "unzipped"
        target.mkdir(mode=0o700)
        tool.unpack_zip(archive, target)
        package = tool.verify_package(target, TASK, IDENTITY)
        self.assertEqual(package["tests"]["frontend"], "failure")

    def test_zip_traversal_extra_duplicate_and_symlink_rejected_before_extraction(self):
        valid = [(name, b"x") for name in tool.FILES]
        variants = [valid + [("../escape", b"x")], valid[:-1] + [valid[0]],
                    [("../escape" if i == 0 else n, d) for i, (n, d) in enumerate(valid)]]
        for items in variants:
            archive = self.zip_file(items)
            target = self.state / "unzipped"
            target.mkdir(mode=0o700, exist_ok=True)
            with self.assertRaises(tool.Rejected):
                tool.unpack_zip(archive, target)
            self.assertEqual(list(target.iterdir()), [])
        archive = self.state / "archive.zip"
        with zipfile.ZipFile(archive, "w") as bundle:
            for name in tool.FILES:
                entry = zipfile.ZipInfo(name)
                entry.external_attr = (stat.S_IFLNK | 0o777) << 16
                bundle.writestr(entry, "/outside")
        with self.assertRaises(tool.Rejected):
            tool.unpack_zip(archive, target)

    def test_zip_bomb_and_total_limit_rejected(self):
        archive = self.zip_file([(n, b"0" * 100000 if n == "images.tar.gz" else b"x") for n in tool.FILES])
        target = self.state / "unzipped"
        target.mkdir(mode=0o700)
        with self.assertRaises(tool.Rejected):
            tool.unpack_zip(archive, target)
        archive = self.zip_file([(n, b"abcdef") for n in tool.FILES])
        with patch.object(tool, "MAX_EXPANDED", 20), self.assertRaises(tool.Rejected):
            tool.unpack_zip(archive, target)

    def test_tar_links_duplicate_alias_traversal_sparse_and_concatenation_rejected(self):
        path = self.state / "images.tar.gz"
        variants = [
            [("link", tarfile.SYMTYPE, b"")], [("link", tarfile.LNKTYPE, b"")],
            [("../escape", tarfile.REGTYPE, b"x")],
            [("file", tarfile.REGTYPE, b"x"), ("file", tarfile.REGTYPE, b"y")],
            [("./file", tarfile.REGTYPE, b"x")],
            [("sparse", tarfile.GNUTYPE_SPARSE, b"")],
            [("pax", tarfile.XHDTYPE, b"")],
        ]
        for entries in variants:
            path.write_bytes(gzip.compress(self.tar_bytes(entries)))
            with self.subTest(entries=entries), self.assertRaises(tool.Rejected):
                tool.inspect_tar(path)
        path.write_bytes(gzip.compress(self.tar_bytes() + self.tar_bytes([("hidden", tarfile.REGTYPE, b"x")])))
        with self.assertRaises(tool.Rejected):
            tool.inspect_tar(path)

    def test_tar_gzip_expansion_bound(self):
        path = self.state / "images.tar.gz"
        path.write_bytes(gzip.compress(self.tar_bytes()))
        with patch.object(tool, "MAX_TAR", 512), self.assertRaises(tool.Rejected):
            tool.inspect_tar(path)

    def test_checksum_duplicate_and_corruption_rejected(self):
        folder = self.folder()
        self.package(folder)
        sums = folder / "SHA256SUMS"
        sums.write_text(sums.read_text() + sums.read_text().splitlines()[0] + "\n")
        with self.assertRaises(tool.Rejected):
            tool.verify_package(folder, TASK, IDENTITY)
        self.package(folder)
        (folder / "build.json").write_bytes(b"tampered")
        with self.assertRaises(tool.Rejected):
            tool.verify_package(folder, TASK, IDENTITY)

    def test_package_cannot_relabel_red_tests_or_source(self):
        folder = self.folder()
        self.package(folder)
        wrong = copy.deepcopy(IDENTITY)
        wrong["tests"]["frontend"] = "success"
        with self.assertRaises(tool.Rejected):
            tool.verify_package(folder, TASK, wrong)
        with self.assertRaises(tool.Rejected):
            tool.verify_package(folder, {**TASK, "tree": "0" * 40}, IDENTITY)

    def prepared_runner(self):
        folder = self.folder()
        runner = tool.Runner(TASK, self.cfg, folder)
        relay = Mock()
        relay.poll.return_value = None
        runner.control = Mock()
        runner.docker_load = Mock()
        runner.previous_good = Mock(return_value=None)
        runner.start_relay = Mock(side_effect=lambda: setattr(runner, "relay", relay))
        runner.stop_relay = Mock(side_effect=lambda: setattr(runner, "relay", None))
        runner.render_start = Mock(side_effect=lambda *args: setattr(runner, "relay", relay))
        return runner

    @contextlib.contextmanager
    def inputs(self):
        # Identity/transport/archives individually exercised above.
        def download(endpoint, **kwargs):
            if kwargs.get("output"):
                kwargs["output"].write_bytes(b"zip")
                os.chmod(kwargs["output"], 0o600)
        identity = {**IDENTITY, "artifact_digest": "sha256:" + hashlib.sha256(b"zip").hexdigest()}
        with patch.object(tool, "github_identity", return_value=identity), \
             patch.object(tool, "gh", side_effect=download), \
             patch.object(tool, "unpack_zip"), \
             patch.object(tool, "verify_package", return_value={"images": {"api": "a", "web": "b"}}):
            yield

    def test_success_accepts_last_good_only_after_probe_serve_and_stop(self):
        runner = self.prepared_runner()
        self.write(self.controller / "request.json", {"candidate": TASK["source_sha"]})
        with self.inputs(), patch.object(tool.time, "monotonic", side_effect=[0, 31]):
            runner.execute()
        self.assertEqual(tool.document(runner.folder / "receipt.json")["phase"], "succeeded")
        pointer = tool.document(self.state / "last-good.json")
        self.assertEqual(pointer["task_id"], TASK["task_id"])
        self.assertEqual(pointer["receipt_sha256"], tool.sha_file(runner.folder / "receipt.json"))
        runner.control.assert_called_with("stop", cleanup=True)

    def test_unverified_fetch_failure_never_loads_or_mutates(self):
        runner = self.prepared_runner()
        with patch.object(tool, "github_identity", side_effect=tool.Rejected("bad")), self.assertRaises(tool.Rejected):
            runner.execute()
        runner.control.assert_not_called()
        runner.docker_load.assert_not_called()
        self.assertFalse((self.state / "last-good.json").exists())

    def test_failure_rolls_back_once_with_no_retry(self):
        runner = self.prepared_runner()
        runner.render_start.side_effect = tool.Rejected("start failed")
        runner.rollback = Mock(side_effect=lambda: runner.receipt("rolled-back"))
        with self.inputs(), self.assertRaises(tool.Rejected):
            runner.execute()
        runner.rollback.assert_called_once()
        self.assertEqual(tool.document(runner.folder / "receipt.json")["phase"], "rolled-back")
        self.assertFalse((self.state / "last-good.json").exists())

    def test_rollback_failure_persists_pause_and_unfinished_receipt(self):
        runner = self.prepared_runner()
        runner.render_start.side_effect = tool.Rejected("start failed")
        runner.rollback = Mock(side_effect=tool.Rejected("reset failed"))
        with self.inputs(), self.assertRaises(tool.Rejected):
            runner.execute()
        runner.rollback.assert_called_once()
        self.assertTrue((self.state / "PAUSED").exists())
        self.assertEqual(tool.document(runner.folder / "receipt.json")["phase"], "rollback-failed")
        with self.assertRaises(tool.Rejected):
            tool.unfinished()

    def test_pause_during_mutation_stops_without_reset_or_rollback(self):
        runner = self.prepared_runner()
        def pause(*args):
            self.write(self.state / "PAUSED", {"reason": "operator"})
            raise tool.Aborted("pause")
        runner.render_start.side_effect = pause
        runner.rollback = Mock()
        with self.inputs(), self.assertRaises(tool.Aborted):
            runner.execute()
        runner.control.assert_called_once_with("stop", cleanup=True)
        runner.rollback.assert_not_called()
        self.assertEqual(tool.document(runner.folder / "receipt.json")["phase"], "aborted")

    def test_abort_cleanup_failure_blocks_resume(self):
        runner = self.prepared_runner()
        runner.render_start.side_effect = tool.Aborted("signal")
        runner.control.side_effect = tool.Rejected("cleanup failed")
        with self.inputs(), self.assertRaises(tool.Rejected):
            runner.execute()
        self.assertEqual(tool.document(runner.folder / "receipt.json")["phase"], "cleanup-failed")
        self.assertTrue((self.state / "PAUSED").exists())
        with self.assertRaises(tool.Rejected):
            tool.unfinished()

    def test_receipt_write_failure_still_stops_and_pauses_without_loop(self):
        runner = self.prepared_runner()
        real_receipt = runner.receipt
        def receipt(phase, **extra):
            if phase in ("serving", "rolling-back", "rollback-failed"):
                raise OSError("synthetic storage failure")
            return real_receipt(phase, **extra)
        runner.receipt = Mock(side_effect=receipt)
        with self.inputs(), self.assertRaises(OSError):
            runner.execute()
        self.assertTrue((self.state / "PAUSED").exists())
        runner.control.assert_called_once_with("stop", cleanup=True)
        phases = [c.args[0] for c in runner.receipt.call_args_list]
        self.assertEqual(phases.count("rolling-back"), 1)
        self.assertFalse((self.state / "last-good.json").exists())

    def test_crash_after_claim_or_without_receipt_never_replays(self):
        folder = self.folder()
        for phase in (None, "claimed", "replacing", "serving", "succeeded-but-not-recorded"):
            if phase is not None:
                self.write(folder / "receipt.json", {"phase": phase})
            with self.subTest(phase=phase), patch.object(tool.Runner, "execute") as execute:
                with self.assertRaises(tool.Rejected):
                    tool.run_task(self.cfg)
                execute.assert_not_called()
            self.assertTrue((self.state / "PAUSED").exists())

    def test_replay_terminal_task_id_rejected_before_mutation(self):
        folder = self.folder()
        self.write(folder / "receipt.json", {"phase": "succeeded"})
        self.write(self.state / "pending.json", TASK)
        with patch.object(tool.Runner, "execute") as execute, self.assertRaises(tool.Rejected):
            tool.run_task(self.cfg)
        execute.assert_not_called()

    def test_last_good_requires_matching_current_request_and_immutable_receipt(self):
        old = self.folder()
        package = self.package(old)
        self.write(old / "receipt.json", {"phase": "succeeded", "task": TASK,
                                         "github": IDENTITY, "control": self.cfg["controller_files"]})
        self.write(self.controller / "request.json", {"parameters": "record"})
        self.write(self.controller / "state.json", {"containers": ["known"]})
        pointer = {"task_id": TASK["task_id"], "receipt_sha256": tool.sha_file(old / "receipt.json"),
                   "request_sha256": tool.sha_file(self.controller / "request.json")}
        self.write(self.state / "last-good.json", pointer)
        runner = tool.Runner({**TASK, "task_id": "e" * 32}, self.cfg, old)
        runner.control = Mock()
        self.assertEqual(runner.previous_good()[1], package)
        self.write(self.controller / "request.json", {"parameters": "changed"})
        with self.assertRaises(tool.Rejected):
            runner.previous_good()

    def test_no_implicit_adoption_of_existing_unknown_slot(self):
        self.write(self.controller / "state.json", {"containers": ["unknown"]})
        runner = tool.Runner(TASK, self.cfg, self.folder())
        with self.assertRaises(tool.Rejected):
            runner.previous_good()

    def test_rollback_uses_retained_package_once_and_stops_relay(self):
        runner = self.prepared_runner()
        runner.previous = (runner.folder, {"images": {}}, TASK)
        runner.rollback()
        runner.control.assert_any_call("reset", "--confirm", "bb-test-u1-fixtures", cleanup=True)
        runner.control.assert_any_call("stop", cleanup=True)
        runner.render_start.assert_called_once_with({"images": {}}, TASK, runner.folder)
        runner.docker_load.assert_not_called()
        self.assertEqual(tool.document(runner.folder / "receipt.json")["phase"], "rolled-back")

    def test_own_child_cleanup_escalates_and_reaps(self):
        child = Mock(pid=987654321)
        child.poll.return_value = None
        child.wait.side_effect = [subprocess.TimeoutExpired("owned", 5), 0]
        with patch.object(tool.os, "killpg") as kill:
            tool.terminate(child)
        self.assertEqual([c.args for c in kill.call_args_list],
                         [(child.pid, signal.SIGTERM), (child.pid, signal.SIGKILL)])
        self.assertEqual(child.wait.call_count, 2)

    def test_service_has_no_timer_polling_retry_or_candidate_executable(self):
        service = SOURCE.parents[1] / "infrastructure/test-environment/bigbrain-test-task.service"
        text = service.read_text()
        for expected in ("Restart=no", "RuntimeMaxSec=45min", "KillMode=control-group",
                         "Requires=bigbrain-test-rootless.service", "/usr/bin/env -i",
                         "/usr/bin/python3 -I %h/.local/lib/bigbrain-test-task/"):
            self.assertIn(expected, text)
        for forbidden in ("EnvironmentFile=", "GH_TOKEN", "GITHUB_TOKEN", "sudo", "Timer", "ExecStartPre="):
            self.assertNotIn(forbidden, text)


    def installed_config(self):
        (self.controller / "client").mkdir(mode=0o700)
        for name in ("test-environment.py", "compose.test.yaml", "EMPTY"):
            path = self.controller / name
            path.write_bytes(b"" if name == "EMPTY" else b"reviewed control")
            os.chmod(path, 0o400)
        hashes = {n: tool.sha_file(self.controller / n)
                  for n in tool.CONTROL - {"installation.json"}}
        self.write(self.controller / "installation.json", {
            "engine": {"mode": "rootless", "uid": os.getuid(), "id": "fixed-daemon"},
            "files": hashes, "allowed_clients": [self.cfg["bind_ip"]]})
        path = self.install / "test-environment-task.py"
        path.write_bytes(SOURCE.read_bytes())
        os.chmod(path, 0o400)
        self.cfg["wrapper_sha256"] = tool.sha_file(path)
        self.cfg["controller_files"] = {n: tool.sha_file(self.controller / n) for n in tool.CONTROL}
        self.write(self.install / "config.json", self.cfg)
        return path

    def test_config_seals_external_rootless_control_and_rejects_candidate_paths(self):
        installed = self.installed_config()
        with patch.object(tool, "__file__", str(installed)):
            self.assertEqual(tool.config(), self.cfg)
            bad = {**self.cfg, "env": {"GH_TOKEN": "canary"}}
            self.write(self.install / "config.json", bad)
            with self.assertRaises(tool.Rejected):
                tool.config()
            self.write(self.install / "config.json", self.cfg)
            (self.controller / ".git").mkdir()
            with self.assertRaises(tool.Rejected):
                tool.config()

    def test_config_rejects_script_drift_rootful_and_client_credentials(self):
        installed = self.installed_config()
        with patch.object(tool, "__file__", str(installed)):
            path = self.controller / "test-environment.py"
            path.chmod(0o600)
            path.write_text("candidate script")
            with self.assertRaises(tool.Rejected):
                tool.config()
            path.write_bytes(b"reviewed control")
            self.write(self.controller / "client/config.json", {"auths": {"canary": "not-real"}})
            with self.assertRaises(tool.Rejected):
                tool.config()
            (self.controller / "client/config.json").unlink()
            seal = tool.document(self.controller / "installation.json")
            seal["engine"]["mode"] = "rootful"
            self.write(self.controller / "installation.json", seal)
            self.cfg["controller_files"]["installation.json"] = tool.sha_file(self.controller / "installation.json")
            self.write(self.install / "config.json", self.cfg)
            with self.assertRaises(tool.Rejected):
                tool.config()

    def test_process_bounds_output_timeout_and_scrubs_environment(self):
        # Only short stdlib child processes; no app, daemon or remote command.
        with patch.dict(os.environ, {"GH_TOKEN": "synthetic-canary"}):
            data = tool.process(
                ["/usr/bin/python3", "-I", "-c", "import os; print(os.environ.get('GH_TOKEN', 'ABSENT'))"],
                tool.control_env(self.cfg), self.state)
        self.assertEqual(data.strip(), b"ABSENT")
        with self.assertRaises(tool.Rejected):
            tool.process(["/usr/bin/python3", "-I", "-c", "print('x'*100)"],
                         tool.base_env(self.state), self.state, limit=10)
        with self.assertRaises(tool.Rejected):
            tool.process(["/usr/bin/python3", "-I", "-c", "import time; time.sleep(5)"],
                         tool.base_env(self.state), self.state, timeout=0.05)

    def test_submit_atomically_queues_then_triggers_fixed_unit_and_blocks_replay(self):
        stdin = Mock(buffer=io.BytesIO(json.dumps(TASK).encode()))
        with patch.object(tool, "config", return_value=self.cfg), \
             patch.object(tool.sys, "argv", ["installed", "submit"]), \
             patch.object(tool.sys, "stdin", stdin), patch.object(tool, "service_command") as service:
            tool.main()
        self.assertEqual(tool.document(self.state / "pending.json"), TASK)
        service.assert_called_once_with("restart")
        stdin = Mock(buffer=io.BytesIO(json.dumps(TASK).encode()))
        with patch.object(tool, "config", return_value=self.cfg), \
             patch.object(tool.sys, "argv", ["installed", "submit"]), \
             patch.object(tool.sys, "stdin", stdin), patch.object(tool, "service_command") as service:
            with self.assertRaises(tool.Rejected):
                tool.main()
            service.assert_not_called()

    def test_submit_is_bounded_and_pause_prevents_trigger(self):
        for data in (b" " * 4097, json.dumps(TASK).encode()):
            self.write(self.state / "PAUSED", {"reason": "test"})
            with patch.object(tool, "config", return_value=self.cfg), \
                 patch.object(tool.sys, "argv", ["installed", "submit"]), \
                 patch.object(tool.sys, "stdin", Mock(buffer=io.BytesIO(data))), \
                 patch.object(tool, "service_command") as service:
                with self.assertRaises(tool.Rejected):
                    tool.main()
                service.assert_not_called()
        self.assertFalse((self.state / "pending.json").exists())

    def test_load_checks_socket_and_daemon_before_any_image_mutation(self):
        runner = tool.Runner(TASK, self.cfg, self.folder())
        sock = Mock(st_mode=stat.S_IFSOCK | 0o600, st_uid=os.getuid())
        wrong = {"ID": "other", "SecurityOptions": ["name=rootless", "name=seccomp"],
                 "CgroupVersion": "2", "CgroupDriver": "systemd"}
        with patch.object(tool, "config", return_value=self.cfg), \
             patch.object(tool, "private"), patch.object(tool.Path, "lstat", return_value=sock), \
             patch.object(tool, "document", return_value={"engine": {"id": "fixed-daemon"}}), \
             patch.object(tool, "process", return_value=json.dumps(wrong).encode()) as process:
            with self.assertRaises(tool.Rejected):
                runner.docker_load(runner.folder)
        self.assertEqual(process.call_count, 1)
        self.assertEqual(process.call_args.args[0][1], "info")

    def relay_pipe(self):
        read_fd, write_fd = os.pipe()
        child = Mock(pid=987654321)
        child.stdout = os.fdopen(read_fd, "rb", buffering=0)
        child.poll.return_value = None
        self.addCleanup(lambda: child.stdout.close())
        self.addCleanup(os.close, write_fd)
        return child, write_fd

    def test_relay_delayed_partial_listening_line_precedes_probe(self):
        runner = tool.Runner(TASK, self.cfg, self.folder())
        child, writer = self.relay_pipe()
        sent = threading.Event()
        def publish():
            os.write(writer, tool.RELAY_READY[:10])
            sent.wait(0.02)
            sent.set()
            os.write(writer, tool.RELAY_READY[10:])
        thread = threading.Thread(target=publish)
        thread.start()
        def command(name, *args, **kwargs):
            if name == "probe":
                self.assertTrue(sent.is_set())
        runner.control = Mock(side_effect=command)
        with patch.object(tool, "config", return_value=self.cfg), \
             patch.object(tool.subprocess, "Popen", return_value=child) as popen, \
             patch.object(tool, "terminate"):
            runner.render_start({"images": {"api": "a", "web": "w"}}, TASK, runner.folder)
            runner.stop_relay()
        thread.join(timeout=1)
        self.assertFalse(thread.is_alive())
        self.assertEqual(popen.call_args.kwargs["stdout"], subprocess.PIPE)
        self.assertEqual(popen.call_args.kwargs["env"], tool.control_env(self.cfg))
        self.assertTrue(child.stdout.closed)

    def test_relay_death_timeout_and_bad_signal_close_pipe_without_probe(self):
        for failure in ("death", "timeout", "wrong-line"):
            runner = tool.Runner(TASK, self.cfg, self.state)
            child, writer = self.relay_pipe()
            if failure == "death":
                child.poll.return_value = 1
            elif failure == "wrong-line":
                os.write(writer, b"PASS not listening\n")
            ready = runner.relay_ready
            runner.control = Mock()
            with self.subTest(failure=failure), \
                 patch.object(tool, "config", return_value=self.cfg), \
                 patch.object(tool.subprocess, "Popen", return_value=child), \
                 patch.object(tool, "terminate") as terminate, \
                 patch.object(runner, "relay_ready", side_effect=lambda: ready(timeout=0.01)):
                with self.assertRaises(tool.Rejected):
                    runner.start_relay()
                terminate.assert_called_once_with(child)
            self.assertTrue(child.stdout.closed)
            self.assertIsNone(runner.relay)
            runner.control.assert_not_called()

    def test_pause_interrupts_relay_readiness_and_cleans_owned_child(self):
        runner = tool.Runner(TASK, self.cfg, self.folder())
        child, _ = self.relay_pipe()
        with patch.object(tool, "config", return_value=self.cfg), \
             patch.object(tool.subprocess, "Popen", return_value=child), \
             patch.object(tool.select, "select", side_effect=lambda *args:
                          (tool.CANCEL.set() or ([], [], []))), \
             patch.object(tool, "terminate") as terminate:
            with self.assertRaises(tool.Aborted):
                runner.start_relay()
        terminate.assert_called_once_with(child)
        self.assertTrue(child.stdout.closed)
        self.assertIsNone(runner.relay)

    def test_rollback_uses_same_listening_handshake_before_probe(self):
        runner = tool.Runner(TASK, self.cfg, self.folder())
        runner.previous = (runner.folder, {"images": {"api": "a", "web": "w"}}, TASK)
        child, writer = self.relay_pipe()
        os.write(writer, tool.RELAY_READY)
        calls = []
        runner.control = Mock(side_effect=lambda name, *a, **kw: calls.append(name))
        with patch.object(tool, "config", return_value=self.cfg), \
             patch.object(tool.subprocess, "Popen", return_value=child), \
             patch.object(tool, "terminate"), \
             patch.object(runner, "relay_ready", wraps=runner.relay_ready) as ready:
            runner.rollback()
        ready.assert_called_once()
        self.assertEqual(calls, ["reset", "render", "validate", "start", "probe", "stop"])
        self.assertTrue(child.stdout.closed)
        self.assertEqual(tool.document(runner.folder / "receipt.json")["phase"], "rolled-back")

    def layer_archive(self, folder, payload=None):
        layer = self.tar_bytes([("actual-file", tarfile.REGTYPE, payload or b"layer-data"),
                                ("link", tarfile.SYMTYPE, b"")])
        outer = self.tar_bytes([("blobs/sha256/layer", tarfile.REGTYPE, gzip.compress(layer))])
        path = folder / "images.tar.gz"
        path.write_bytes(gzip.compress(outer))
        os.chmod(path, 0o600)
        return path, layer, outer

    def test_oci_gzip_stats_count_actual_logical_payload_and_raw_layer(self):
        folder = self.folder()
        path, layer, outer = self.layer_archive(folder)
        stats = tool.inspect_tar(path)
        self.assertEqual(stats["outer_bytes"], len(outer))
        self.assertEqual(stats["layer_stream_bytes"], len(layer))
        self.assertEqual(stats["unpacked_bytes"], len(b"layer-data"))
        self.assertEqual(stats["members"], 3)
        path.write_bytes(gzip.compress(self.tar_bytes([("layer.tar", tarfile.REGTYPE, layer)])))
        self.assertEqual(tool.inspect_tar(path)["unpacked_bytes"], len(b"layer-data"))

    def test_small_compressed_layer_bomb_rejected_before_load(self):
        folder = self.folder()
        path, _, _ = self.layer_archive(folder, b"0" * 65536)
        self.assertLess(path.stat().st_size, 1024)
        with patch.object(tool, "MAX_UNPACKED", 1024), \
             patch.object(tool, "process") as process, self.assertRaises(tool.Rejected):
            tool.inspect_tar(path)
        process.assert_not_called()
        # Expansion in zero padding is counted too, even with no file headers.
        padding = gzip.compress(bytes(65536))
        path.write_bytes(gzip.compress(self.tar_bytes([("blobs/sha256/pad", tarfile.REGTYPE, padding)])))
        with patch.object(tool, "MAX_LAYER_STREAM", 1024), self.assertRaises(tool.Rejected):
            tool.inspect_tar(path)

    def test_staging_fits_but_actual_engine_space_blocks_load_and_keeps_headroom(self):
        folder = self.folder()
        path, _, _ = self.layer_archive(folder)
        stats = tool.inspect_tar(path)
        reserve = tool.import_reserve(stats)
        daemon_root = self.controller / "docker"
        daemon_root.mkdir(mode=0o700)
        engine = {"ID": "fixed-daemon", "SecurityOptions": ["name=rootless", "name=seccomp"],
                  "CgroupVersion": "2", "CgroupDriver": "systemd", "DockerRootDir": str(daemon_root)}
        runner = tool.Runner(TASK, self.cfg, folder)
        original_lstat = Path.lstat
        original_private = tool.private
        def private(path, directory=False):
            if not str(path).startswith("/run/user/"):
                return original_private(path, directory=directory)
        sock = Mock(st_mode=stat.S_IFSOCK | 0o600, st_uid=os.getuid())
        def lstat(path):
            return sock if path.name == "docker.sock" else original_lstat(path)
        trace = []
        def process(argv, *args, **kwargs):
            trace.append(argv[1])
            return json.dumps(engine).encode()
        def statvfs(path):
            self.assertEqual(path, daemon_root)
            trace.append("engine-space")
            return Mock(f_bavail=reserve + tool.HOST_HEADROOM - 1, f_frsize=1)
        with patch.object(tool, "config", return_value=self.cfg), \
             patch.object(tool, "private", side_effect=private), patch.object(tool.Path, "lstat", new=lstat), \
             patch.object(tool, "document", return_value={"engine": {"id": "fixed-daemon"}}), \
             patch.object(tool, "process", side_effect=process), \
             patch.object(tool.os, "statvfs", side_effect=statvfs), \
             patch.object(tool.shutil, "disk_usage", return_value=Mock(free=16 * 1024**3)):
            tool.budget()  # Staging passes independently.
            with self.assertRaises(tool.Rejected):
                runner.docker_load(folder)
            self.assertEqual(trace, ["info", "engine-space"])
            trace.clear()
            with patch.object(tool.os, "statvfs", side_effect=lambda root:
                              (trace.append("engine-space") or
                               Mock(f_bavail=reserve + tool.HOST_HEADROOM, f_frsize=1))):
                runner.docker_load(folder)
            self.assertEqual(trace, ["info", "engine-space", "load"])
            trace.clear()
            with patch.object(tool, "MAX_UNPACKED", 1), self.assertRaises(tool.Rejected):
                runner.docker_load(folder)
            self.assertEqual(trace, ["info"])  # Bomb stops before space check AND load.


if __name__ == "__main__":
    unittest.main()
