#!/usr/bin/env python3
"""Synthetic CI only. No input paths/commands, host data or privileged fallback.

Requires an already installed non-setuid bubblewrap with unprivileged user
namespaces. Unsupported environments exit nonzero, never skip to an unsafe run.
Build/restore happens separately. Only selected test output is staged.
"""
import os
from pathlib import Path
import shutil
import sqlite3
import stat
import subprocess
import sys
import tempfile

ROOT = Path(__file__).resolve().parents[1]
ASSEMBLY = "BigBrain.Finance.OutcomeAudit.Tests"
OUTPUT = ROOT / "tests" / ASSEMBLY / "bin/Release/net10.0"


def run():
    if sys.platform != "linux" or os.getuid() == 0 or os.geteuid() == 0:
        raise RuntimeError("requires unprivileged Linux")
    bwrap = shutil.which("bwrap")
    dotnet = shutil.which("dotnet")
    if not bwrap or not dotnet or Path(bwrap).stat().st_mode & (stat.S_ISUID | stat.S_ISGID):
        raise RuntimeError("requires non-setuid bubblewrap and dotnet")
    runtime = Path(dotnet).resolve().parent
    if not (OUTPUT / (ASSEMBLY + ".runtimeconfig.json")).is_file():
        raise RuntimeError("build the Release fixture executable first")
    with tempfile.TemporaryDirectory(prefix="bb-research-isolated-", dir="/tmp") as name:
        fixture = Path(name)
        app = fixture / "app"
        source = fixture / "source"
        app.mkdir()
        source.mkdir()
        for path in OUTPUT.rglob("*"):
            relative = path.relative_to(OUTPUT)
            if path.is_symlink():
                raise RuntimeError("linked build output is not permitted")
            if not path.is_file():
                continue
            # Never stage .env, runner configuration, credentials, data or Git.
            if not (path.suffix in {".dll", ".so"} or
                    path.name in {ASSEMBLY + ".deps.json", ASSEMBLY + ".runtimeconfig.json"} or
                    (relative.parts[0] == "schema" and path.name in {
                        "EodhdFinanceMarketData.cs", "FinanceShadowResearch.cs", "FinanceFeatureStore.cs"})):
                continue
            destination = app / relative
            destination.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(path, destination)
        # .NET network-interface enumeration requires this file; never expose host DNS.
        resolv = fixture / "resolv.conf"
        resolv.write_text("# Empty synthetic DNS configuration; no nameservers.\n")
        (source / "identity").write_text("synthetic-only")
        with sqlite3.connect(source / "production-surrogate.db") as database:
            database.execute("CREATE TABLE sentinel(value TEXT)")
            database.execute("INSERT INTO sentinel VALUES ('synthetic-only')")
        command = [bwrap, "--unshare-all", "--die-with-parent", "--new-session",
                   "--cap-drop", "ALL", "--clearenv", "--tmpfs", "/",
                   "--proc", "/proc", "--dev", "/dev", "--tmpfs", "/tmp",
                   "--dir", "/home", "--dir", "/dotnet", "--dir", "/etc",
                   "--ro-bind", str(resolv), "/etc/resolv.conf",
                   "--ro-bind", str(runtime / "dotnet"), "/dotnet/dotnet",
                   "--ro-bind", str(runtime / "host"), "/dotnet/host",
                   "--ro-bind", str(runtime / "shared"), "/dotnet/shared",
                   "--ro-bind", "/usr/share/zoneinfo/America/New_York",
                   "/usr/share/zoneinfo/America/New_York",
                   "--ro-bind", "/usr/lib", "/usr/lib", "--symlink", "usr/lib", "/lib"]
        if Path("/usr/lib64").is_dir():
            command += ["--ro-bind", "/usr/lib64", "/usr/lib64", "--symlink", "usr/lib64", "/lib64"]
        command += ["--ro-bind", str(app), "/app", "--ro-bind", str(source), "/source",
                    "--remount-ro", "/", "--chdir", "/app"]
        for key, value in {
            "BB_RESEARCH_ISOLATED": "1", "HOME": "/tmp", "TMPDIR": "/tmp", "DOTNET_ROOT": "/dotnet",
            "DOTNET_EnableDiagnostics": "0", "DOTNET_CLI_TELEMETRY_OPTOUT": "1", "LANG": "C.UTF-8",
        }.items():
            command += ["--setenv", key, value]
        command += ["/dotnet/dotnet", "/app/" + ASSEMBLY + ".dll", "-failSkips", "-noLogo"]
        # Empty launcher environment too; close inherited descriptors. Never shell=True.
        result = subprocess.run(command, env={}, close_fds=True, timeout=180, check=False)
        if result.returncode:
            raise RuntimeError("isolated fixture failed or isolation unavailable; no fallback")
        print("PASS: isolated synthetic research fixture; host qualification remains PENDING")


if __name__ == "__main__":
    try:
        run()
    except (RuntimeError, OSError, subprocess.TimeoutExpired) as error:
        # Fixed messages only; do not print host paths or inherited environment.
        print("FAIL: isolated synthetic research fixture (no fallback)", file=sys.stderr)
        sys.exit(1)
