"""Isolated mountinfo fixtures only. Never start a unit or inspect/mutate production data."""
import hashlib
import importlib.util
from pathlib import Path
import unittest
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[2]


def load(name, filename):
    spec = importlib.util.spec_from_file_location(name, ROOT / "infrastructure/finance" / filename)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


P = load("preflight", "historical-preflight.py")
D = load("diagnostic", "historical-mount-diagnostic.py")


def row(target, filesystem="tmpfs", mount="rw", superblock="rw", identifier=1, parent=0):
    return f"{identifier} {parent} 0:1 / {target} {mount},nosuid shared:7 - {filesystem} source {superblock}"


class MountDiagnosticTests(unittest.TestCase):
    def test_all_unexpected_mounts_retained_without_accepting_any(self):
        result = D.analyze("\n".join([row(D.DIAGNOSTIC_CGROUP, "cgroup2"), row("/proc/kmsg"),
                                       row("/unexpected-private-path", "ext4")]), P)
        self.assertEqual(2, result["unexpectedCount"])
        self.assertEqual("UnexpectedWritableMount", result["policyOutcome"])
        self.assertEqual([False, True, True], [r["unexpectedWritable"] for r in result["mounts"]])

    def test_mount_flags_not_superblock_flags_drive_unchanged_policy(self):
        for mount, superblock, expected in (("ro", "rw", False), ("rw", "ro", True)):
            with self.subTest(mount=mount):
                result = D.analyze(row("/unknown", mount=mount, superblock=superblock), P)
                self.assertEqual(expected, result["mounts"][0]["unexpectedWritable"])
                self.assertIn(superblock, result["mounts"][0]["superblockFlags"])

    def test_escape_decoding_is_one_pass_and_does_not_change_policy_input(self):
        target = r"/private\040name\011tab\012line\134040"
        result = D.analyze(row(target), P)["mounts"][0]
        self.assertEqual("/private name\ttab\nline\\040", result["target"])
        self.assertEqual(target, result["targetEncoded"])
        self.assertTrue(result["unexpectedWritable"])

    def test_stacked_mounts_are_not_discarded_or_guessed_safe(self):
        text = row("/masked", identifier=11) + "\n" + row("/masked", mount="ro", identifier=12, parent=11)
        result = D.analyze(text, P)
        self.assertEqual(2, len(result["mounts"]))
        self.assertEqual(1, result["unexpectedCount"])
        self.assertEqual(11, result["mounts"][1]["parentId"])

    def test_only_exact_diagnostic_cgroup_target_is_substituted(self):
        for path, expected in ((D.DIAGNOSTIC_CGROUP, False), (D.DIAGNOSTIC_CGROUP + "/worker", True),
                               (D.DIAGNOSTIC_CGROUP + "-other", True)):
            result = D.analyze(row(path, "cgroup2"), P)
            self.assertEqual(expected, result["mounts"][0]["unexpectedWritable"])
            self.assertEqual(not expected, result["mounts"][0]["unitNameSubstitutedForComparison"])
            self.assertEqual(path, result["mounts"][0]["target"])

    def test_arbitrary_sources_roots_options_never_exported(self):
        text = "1 0 0:1 /PRIVATE_ROOT /tmp rw,SECRET_MOUNT=canary optional:secret - tmpfs SECRET_SOURCE rw,SECRET_SUPER=canary"
        result = str(D.analyze(text, P))
        for secret in ("PRIVATE_ROOT", "SECRET", "canary", "optional:secret"):
            self.assertNotIn(secret, result)

    def test_malformed_and_oversized_input_refused(self):
        for text in ("", "broken", row(r"/bad\999"), row("/tmp", mount="rw,ro"),
                     "x" * (D.MAX_BYTES + 1), "\n".join([row("/tmp")] * (D.MAX_ROWS + 1))):
            with self.subTest(length=len(text)), self.assertRaises(D.DiagnosticRefused):
                D.analyze(text, P)

    def test_no_fallback_to_terminal_output(self):
        with patch.object(D, "private_output", side_effect=D.DiagnosticRefused("PrivateOutputRequired")), \
             patch.object(D.sys.stdout, "write") as write:
            self.assertEqual(1, D.main())
            write.assert_not_called()

    def test_pinned_policy_and_required_installation_match_repository(self):
        for filename, expected in (("historical-preflight.py", D.ORIGINAL_PROBE_SHA),
                                   ("bigbrain-historical-maintenance.service", D.ORIGINAL_UNIT_SHA)):
            self.assertEqual(expected, hashlib.sha256((ROOT / "infrastructure/finance" / filename).read_bytes()).hexdigest())

    def test_old_installed_unit_is_not_accepted_as_equivalent_to_private_propagation(self):
        current = (ROOT / "infrastructure/finance/bigbrain-historical-maintenance.service").read_bytes()
        old = current.replace(b"MountFlags=private\n", b"", 1)
        self.assertEqual("b643dc93ad1c618753d603072d346af3dfe06d6f3e4e54de05d0a97c6e4b73c4",
                         hashlib.sha256(old).hexdigest())
        with patch.object(D.Path, "resolve", return_value=Path("/")), \
             patch.object(D.Path, "lstat", return_value=type("Metadata", (), {"st_uid": 0, "st_mode": 0o755})()), \
             patch.object(D.Path, "is_file", return_value=True), \
             patch.object(D.Path, "stat", return_value=type("Links", (), {"st_nlink": 1})()), \
             patch.object(D.Path, "read_bytes", return_value=old):
            with self.assertRaisesRegex(D.DiagnosticRefused, "InstalledBaselineMismatch"):
                D.protected_hash(Path("/"), D.ORIGINAL_UNIT_SHA)

    def test_unit_retains_every_isolation_setting_with_only_explicit_deltas(self):
        def settings(filename):
            lines = (ROOT / "infrastructure/finance" / filename).read_text().splitlines()
            return [line for line in lines if line and not line.startswith("#")]
        original = settings("bigbrain-historical-maintenance.service")
        diagnostic = settings("bigbrain-historical-mount-diagnostic.service")
        replacement = {
            "Description=BigBrain historical research isolation preflight (model-free)":
                "Description=BigBrain historical mount namespace diagnostic (model-free)",
            "ExecStart=/usr/bin/python3 -I -B /usr/local/libexec/bigbrain-historical-preflight.py":
                "ExecStart=/usr/bin/python3 -I -B /run/bigbrain-historical-mount-diagnostic/diagnostic.py",
            "ReadWritePaths=" + D.ORIGINAL_CGROUP: "ReadWritePaths=" + D.DIAGNOSTIC_CGROUP,
            "StandardOutput=journal": "StandardOutput=file:/run/bigbrain-historical-mount-diagnostic/result.json",
            "StandardError=journal": "StandardError=null",
        }
        self.assertEqual([replacement.get(line, line) for line in original], diagnostic)


if __name__ == "__main__":
    unittest.main()
