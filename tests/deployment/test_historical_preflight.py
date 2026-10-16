"""Model-free checks; no systemd start, cgroup writes, Finance access or provider calls."""
import importlib.util
import os
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[2]
SPEC = importlib.util.spec_from_file_location("preflight", ROOT / "infrastructure/finance/historical-preflight.py")
P = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(P)

STATUS = "\n".join(["Uid:\t1654 1654 1654 1654", "Gid:\t1654 1654 1654 1654", "Groups:\t1654",
                    "NoNewPrivs:\t1"] + [f"{name}:\t0000000000000000" for name in
                                         ("CapInh", "CapPrm", "CapEff", "CapBnd", "CapAmb")])


def mount(path, kind, access="rw"):
    return f"1 0 0:1 / {path} {access},nosuid - {kind} source rw"


class PreflightTests(unittest.TestCase):
    def test_identity_rejects_each_capability_and_missing_nnp(self):
        P.identity(STATUS)
        for name in ("CapInh", "CapPrm", "CapEff", "CapBnd", "CapAmb"):
            with self.subTest(name=name), self.assertRaises(P.Refused):
                P.identity(STATUS.replace(name + ":\t0000000000000000", name + ":\t0000000000000001"))
        with self.assertRaises(P.Refused):
            P.identity(STATUS.replace("NoNewPrivs:\t1", "NoNewPrivs:\t0"))

    def test_wrong_or_supplementary_identity_refused(self):
        for data in (STATUS.replace("1654", "0"), STATUS.replace("Groups:\t1654", "Groups:\t1654 999")):
            with self.assertRaises(P.Refused):
                P.identity(data)

    def test_mount_policy_allows_only_scoped_rw_mounts(self):
        P.mount_policy("\n".join([mount("/", "ext4", "ro"), mount(str(P.CGROUP), "cgroup2"),
                                 mount(str(P.STATE), "ext4"), mount("/proc", "proc"),
                                 mount("/tmp", "tmpfs"), mount("/dev", "tmpfs")]))

    def test_mount_policy_rejects_broad_or_other_writable_host_mounts(self):
        for path, kind in (("/", "ext4"), ("/sys/fs/cgroup", "cgroup2"), ("/var/lib/finance", "ext4"),
                           ("/run/docker.sock", "ext4"), ("/proc/sys", "proc"), ("/tmp", "ext4")):
            with self.subTest(path=path), self.assertRaises(P.Refused):
                P.mount_policy(mount(str(P.CGROUP), "cgroup2") + "\n" + mount(path, kind))

    def test_delegation_must_be_writable_and_present(self):
        with self.assertRaises(P.Refused):
            P.mount_policy(mount(str(P.CGROUP), "cgroup2", "ro"))

    def test_topology_rejected_before_any_cgroup_write(self):
        with patch.object(P.os, "getuid", return_value=1654), patch.object(P.os, "geteuid", return_value=1654), \
             patch.object(P.os, "getgid", return_value=1654), patch.object(P.os, "getegid", return_value=1654), \
             patch.object(P, "read", side_effect=[STATUS, "0::/unexpected"]), patch.object(P, "write_control") as write:
            with self.assertRaisesRegex(P.Refused, "WrongDelegationTopology"):
                P.probe()
            write.assert_not_called()

    def test_real_owned_inert_child_is_reaped_on_success_and_verification_exception(self):
        # Exercises pidfd/kill/reap on harmless forked children; does NOT claim cgroup proof.
        for fail in (False, True):
            seen = []

            def verify(pid):
                seen.append(pid)
                if fail:
                    raise P.Refused("TestVerificationFailed")

            if fail:
                with self.assertRaisesRegex(P.Refused, "TestVerificationFailed"):
                    P.own_child(lambda: None, verify)
            else:
                P.own_child(lambda: None, verify)
            self.assertEqual(1, len(seen))
            with self.assertRaises(ChildProcessError):
                os.waitpid(seen[0], os.WNOHANG)

    def test_child_migration_error_is_not_success(self):
        def refused():
            raise P.Refused("TestMoveFailure")
        with self.assertRaisesRegex(P.Refused, "ChildMigrationFailed"):
            P.own_child(refused, lambda _pid: self.fail("must not verify failed migration"))

    def test_symlink_control_write_refused(self):
        with tempfile.TemporaryDirectory() as temporary:
            directory = Path(temporary)
            target = directory / "target"
            target.write_text("unchanged")
            link = directory / "link"
            link.symlink_to(target)
            with self.assertRaises(OSError):
                P.write_control(link, "replace")
            self.assertEqual("unchanged", target.read_text())

    def test_existing_invocation_record_never_rewritten(self):
        with tempfile.TemporaryDirectory() as temporary:
            directory = Path(temporary)
            invocation = "a" * 32
            retained = directory / (invocation + ".pending")
            retained.write_text("previous evidence")
            with patch.object(P, "STATE", directory), patch.object(P, "private_directory"), \
                 patch.object(P, "protected_file"), patch.object(P.sys, "argv", ["preflight"]), \
                 patch.dict(os.environ, {"INVOCATION_ID": invocation}), \
                 patch.object(P.Path, "read_bytes", return_value=b"fixture"), patch.object(P, "probe") as probe:
                self.assertEqual(1, P.main())
                probe.assert_not_called()
            self.assertEqual("previous evidence", retained.read_text())

    def test_unit_keeps_probe_only_and_systemd_cleanup_boundary(self):
        unit = (ROOT / "infrastructure/finance/bigbrain-historical-maintenance.service").read_text()
        for setting in ("Delegate=cpu memory pids", "DelegateSubgroup=controller", "User=1654", "Group=1654",
                        "NoNewPrivileges=yes", "CapabilityBoundingSet=\n", "AmbientCapabilities=\n",
                        "Restart=no", "KillMode=control-group", "TimeoutStartSec=45s", "TimeoutStopSec=5s"):
            self.assertIn(setting, unit)
        self.assertNotIn("[Install]", unit.splitlines())
        self.assertNotIn("finance-historical-run", unit)
        self.assertNotIn("ExecStartPre=", unit)  # subgroup applies to main ExecStart, not ancillary commands
        writes = [line for line in unit.splitlines() if line.startswith("ReadWritePaths=")]
        self.assertEqual(["ReadWritePaths=" + str(P.CGROUP)], writes)


if __name__ == "__main__":
    unittest.main()
