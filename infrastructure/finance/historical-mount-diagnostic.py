#!/usr/bin/python3
"""Read this process's mount namespace; no mount/cgroup mutation, child or Finance call.

stdout MUST be the precreated private root-owned result file selected by the companion unit.
Never run interactively or log stdout. Targets can contain private paths. Sources, filesystem
roots, arbitrary mount options, environment and credential material are deliberately omitted.
"""
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import re
import stat
import sys
from datetime import datetime, timezone

ORIGINAL_UNIT = Path("/etc/systemd/system/bigbrain-historical-maintenance.service")
ORIGINAL_PROBE = Path("/usr/local/libexec/bigbrain-historical-preflight.py")
ORIGINAL_UNIT_SHA = "212558051adef7b1d42afa3e825fed5fe15a97ef006e0bf0676bfa302f1967f3"
ORIGINAL_PROBE_SHA = "97ecacefdeed8e95e7056557b0871384d5e4ab0b9194e7846679f4cffe04bbf6"
DIAGNOSTIC_UNIT = "bigbrain-historical-mount-diagnostic.service"
DIAGNOSTIC_CGROUP = "/sys/fs/cgroup/system.slice/" + DIAGNOSTIC_UNIT
ORIGINAL_CGROUP = "/sys/fs/cgroup/system.slice/bigbrain-historical-maintenance.service"
RESULT = Path("/run/bigbrain-historical-mount-diagnostic/result.json")
MAX_BYTES = 262144
MAX_ROWS = 1024
FLAGS = {"rw", "ro", "nosuid", "nodev", "noexec", "relatime", "noatime", "strictatime"}


class DiagnosticRefused(Exception):
    pass


def require(condition, category):
    if not condition:
        raise DiagnosticRefused(category)


def bounded_read(path):
    with open(path, "rb") as stream:
        data = stream.read(MAX_BYTES + 1)
    require(len(data) <= MAX_BYTES, "InputTooLarge")
    return data.decode("utf-8", errors="surrogateescape")


def decode_target(value):
    replacements = {"040": " ", "011": "\t", "012": "\n", "134": "\\"}
    require(re.search(r"\\(?!040|011|012|134)", value) is None, "InvalidMountEscape")
    # One pass: a decoded literal backslash must not turn a following '040' into a space.
    return re.sub(r"\\(040|011|012|134)", lambda match: replacements[match[1]], value)


def parse_row(line):
    left, separator, right = line.partition(" - ")
    fields, tail = left.split(), right.split()
    require(separator and len(fields) >= 6 and len(tail) == 3, "MalformedMountInfo")
    require(fields[0].isdigit() and fields[1].isdigit() and
            re.fullmatch(r"\d+:\d+", fields[2]) is not None, "MalformedMountInfo")
    target = decode_target(fields[4])
    require(target.startswith("/"), "MalformedMountInfo")
    for options in (fields[5], tail[2]):
        require(len(set(options.split(",")) & {"ro", "rw"}) == 1, "MalformedMountInfo")
    # Production policy is not modified: only the separate unit's exact delegated mount name
    # is substituted for comparison. No prefix, child, mount permission or filesystem alias.
    normalized = line
    renamed = fields[4] == DIAGNOSTIC_CGROUP
    if renamed:
        fields[4] = ORIGINAL_CGROUP
        normalized = " ".join(fields) + " - " + right
    return {
        "mountId": int(fields[0]), "parentId": int(fields[1]), "target": target,
        "targetEncoded": left.split()[4], "filesystem": tail[0],
        "mountFlags": sorted(set(fields[5].split(",")) & FLAGS),
        "superblockFlags": sorted(set(tail[2].split(",")) & FLAGS),
        "unitNameSubstitutedForComparison": renamed,
    }, normalized


def policy_result(policy, text):
    try:
        policy.mount_policy(text)
        return "Accepted"
    except policy.Refused as error:
        category = str(error)
        require(category in {"UnexpectedWritableMount", "DelegatedMountMissing"}, "UnknownPolicyFailure")
        return category


def analyze(text, policy):
    require(len(text.encode("utf-8", errors="surrogateescape")) <= MAX_BYTES, "InputTooLarge")
    lines = text.splitlines()
    require(0 < len(lines) <= MAX_ROWS, "InvalidMountCount")
    records, normalized = [], []
    for line in lines:
        record, comparable = parse_row(line)
        decision = policy_result(policy, comparable)
        record["unexpectedWritable"] = decision == "UnexpectedWritableMount"
        records.append(record)
        normalized.append(comparable)
    # Keep readonly/stacked records too: a hidden or masked mount cannot be diagnosed from one row.
    return {"mounts": records, "unexpectedCount": sum(r["unexpectedWritable"] for r in records),
            "policyOutcome": policy_result(policy, "\n".join(normalized)),
            "exceedsOriginalReadLimit": len(text) > 65536}


def protected_hash(path, expected=None):
    require(path.resolve() == path, "UnprotectedDiagnosticInput")
    for item in (path, *path.parents):
        metadata = item.lstat()
        require(metadata.st_uid == 0 and not metadata.st_mode & 0o022, "UnprotectedDiagnosticInput")
    require(path.is_file() and path.stat().st_nlink == 1, "UnprotectedDiagnosticInput")
    digest = hashlib.sha256(path.read_bytes()).hexdigest()
    require(expected is None or digest == expected, "InstalledBaselineMismatch")
    return digest


def private_output():
    info, path_info = os.fstat(1), RESULT.lstat()
    require(RESULT.resolve() == RESULT and stat.S_ISREG(info.st_mode) and
            info.st_uid == 0 and stat.S_IMODE(info.st_mode) == 0o600 and info.st_nlink == 1 and
            info.st_size == 0 and (info.st_dev, info.st_ino) == (path_info.st_dev, path_info.st_ino),
            "PrivateOutputRequired")
    for parent in RESULT.parents:
        metadata = parent.lstat()
        require(metadata.st_uid == 0 and not metadata.st_mode & 0o022, "PrivateOutputRequired")


def main():
    # No error, path or mount data may reach an interactive terminal/journal.
    try:
        private_output()
    except Exception:
        return 1
    result = {"contract": "FIN-OPS-G3-20261016/mount-diagnostic-v1", "status": "DIAGNOSTIC_FAILED"}
    try:
        require(len(sys.argv) == 1, "ArgumentsForbidden")
        invocation = os.environ.get("INVOCATION_ID", "")
        require(re.fullmatch("[a-f0-9]{32}", invocation) is not None, "InvocationIdentityMissing")
        result.update(invocation=invocation, atUtc=datetime.now(timezone.utc).isoformat(),
                      probeSha256=protected_hash(ORIGINAL_PROBE, ORIGINAL_PROBE_SHA),
                      originalUnitSha256=protected_hash(ORIGINAL_UNIT, ORIGINAL_UNIT_SHA),
                      diagnosticSha256=protected_hash(Path(__file__)),
                      diagnosticUnitSha256=protected_hash(Path("/run/systemd/system") / DIAGNOSTIC_UNIT))
        spec = importlib.util.spec_from_file_location("installed_preflight", ORIGINAL_PROBE)
        policy = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(policy)  # trusted hash-pinned module; never call main()/probe()
        policy.identity(bounded_read("/proc/self/status"))
        topology = bounded_read("/proc/self/cgroup").strip()
        require(topology == "0::/system.slice/" + DIAGNOSTIC_UNIT + "/controller", "WrongDiagnosticTopology")
        result.update(analyze(bounded_read("/proc/self/mountinfo"), policy),
                      status="DIAGNOSTIC_ONLY", cgroup=topology,
                      mountNamespace=os.readlink("/proc/self/ns/mnt"))
    except DiagnosticRefused as error:
        result["category"] = str(error)
    except Exception:
        result["category"] = "DiagnosticSystemFailure"
    sys.stdout.write(json.dumps(result, sort_keys=True, ensure_ascii=True) + "\n")
    sys.stdout.flush()
    os.fsync(1)
    return 0 if result["status"] == "DIAGNOSTIC_ONLY" else 1


if __name__ == "__main__":
    sys.exit(main())
