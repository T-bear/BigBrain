#!/usr/bin/env bash
# Reproducible synthetic-only Bubblewrap probe, without real Finance data.
# Default: bash SCRIPT --current
# Owner-supervised separate audit-UID test, only after explicit authorization:
#   sudo bash SCRIPT --audit EXISTING_AUDIT_ACCOUNT
# Neither mode creates accounts, changes persistent rights or starts services.
set -Eeuo pipefail
umask 077

mode='--current'
if [[ "$#" -gt 0 ]]; then mode="$1"; fi
if [[ "$mode" == --current && "$#" -le 1 ]]; then
  uid="$(id -u)"
  gid="$(id -g)"
  require_empty_groups=0
elif [[ "$mode" == --audit && "$#" -eq 2 ]]; then
  [[ "$EUID" -eq 0 ]] || { echo 'STOPP: --audit requires owner-supervised root' >&2; exit 1; }
  account="$2"
  [[ "$account" =~ ^[a-z_][a-z0-9_-]*$ ]] || { echo 'STOPP: invalid audit account' >&2; exit 1; }
  entry="$(getent passwd "$account")" || { echo 'STOPP: account not present' >&2; exit 1; }
  IFS=: read -r account_name _ uid gid comment home shell <<< "$entry"
  [[ "$account_name" == "$account" && "$uid" -ne 0 &&
     "$uid" -ne 1654 && "$gid" -ne 0 ]] || { echo 'STOPP: unsafe identity' >&2; exit 1; }
  [[ "$shell" == /usr/sbin/nologin && "$home" == /nonexistent ]] || {
    echo 'STOPP: not an approved non-login identity' >&2; exit 1;
  }
  [[ "$(id -G "$account")" == "$gid" ]] || {
    echo 'STOPP: account has supplementary groups' >&2; exit 1;
  }
  require_empty_groups=1
else
  echo 'Usage: bash SCRIPT --current | sudo bash SCRIPT --audit EXISTING_AUDIT_ACCOUNT' >&2
  exit 2
fi

for command in /usr/bin/setpriv /usr/bin/bwrap /usr/bin/timeout /usr/bin/python3.13; do
  [[ -x "$command" ]] || { echo 'STOPP: missing runtime' >&2; exit 1; }
done

scratch="$(mktemp -d /tmp/bb-audit-bwrap-replay.XXXXXX)"
cleanup() {
  local status="$?"
  if [[ "$scratch" == /tmp/bb-audit-bwrap-replay.* &&
        -d "$scratch" && ! -L "$scratch" ]]; then
    rm -rf -- "$scratch"
  fi
  exit "$status"
}
trap cleanup EXIT

mkdir "$scratch/evidence"
printf 'synthetic-only\n' > "$scratch/evidence/fixture.txt"
printf 'not-mounted-synthetic-sentinel\n' > "$scratch/host-sentinel.txt"

cat > "$scratch/probe.py" <<'PY'
import os
import socket
from pathlib import Path

expected_uid = int(os.environ["EXPECTED_UID"])
require_empty_groups = os.environ["EMPTY_GROUPS"] == "1"
assert os.getuid() == os.geteuid() == expected_uid
if require_empty_groups:
    assert os.getgroups() == []
print("IDENTITY=PASS")

assert Path("/evidence/fixture.txt").read_bytes() == b"synthetic-only\n"
print("SYNTHETIC_ALLOWLIST_READ=PASS")

# Inspect the bind mount itself; DAC denial alone can be a false positive.
# In mountinfo the fifth field is mountpoint, sixth is per-mount options.
evidence_mounts = []
for record in Path("/proc/self/mountinfo").read_text().splitlines():
    mount_fields = record.split(" - ", 1)[0].split()
    if len(mount_fields) >= 6 and mount_fields[4] == "/evidence":
        evidence_mounts.append(set(mount_fields[5].split(",")))
assert len(evidence_mounts) == 1, "missing or ambiguous /evidence mount"
assert "ro" in evidence_mounts[0] and "rw" not in evidence_mounts[0], (
    "synthetic evidence bind is not mounted read-only"
)
print("SYNTHETIC_BIND_MOUNT_RO=PASS")

try:
    with open("/evidence/fixture.txt", "wb") as stream:
        stream.write(b"forbidden")
except OSError:
    print("SYNTHETIC_FILE_WRITE_DENIED=PASS")
else:
    raise AssertionError("writable synthetic bind mount")

for hidden in ("/home", "/etc", "/run", "/var", "/finance",
               "/evidence/host-sentinel.txt", "/tmp/host-sentinel.txt"):
    assert not Path(hidden).exists(), hidden
assert set(os.listdir("/")) == {
    "bin", "evidence", "lib", "lib64", "probe.py", "proc", "tmp", "usr"
}
print("MINIMAL_ROOT_FILESYSTEM=PASS")

assert all(name == "lo" for _, name in socket.if_nameindex())
print("ISOLATED_NETWORK_NAMESPACE=PASS")

info = dict(row.strip().split(":", 1) for row in
            Path("/proc/self/status").read_text().splitlines()
            if row.startswith(("CapEff:", "CapPrm:", "NoNewPrivs:")))
assert int(info["CapEff"], 16) == 0
assert int(info["CapPrm"], 16) == 0
assert info["NoNewPrivs"].strip() == "1"
print("NO_CAPABILITIES_AND_NO_NEW_PRIVS=PASS")
PY

if [[ "$mode" == --audit ]]; then
  chgrp "$gid" "$scratch" "$scratch/evidence" \
        "$scratch/evidence/fixture.txt" "$scratch/probe.py"
fi
chmod 0750 "$scratch" "$scratch/evidence"
chmod 0440 "$scratch/evidence/fixture.txt" "$scratch/probe.py"
chmod 0600 "$scratch/host-sentinel.txt"

set -- /usr/bin/setpriv --no-new-privs
if [[ "$mode" == --audit ]]; then
  set -- "$@" --reuid "$uid" --regid "$gid" --clear-groups
fi

echo 'REPRODUCIBLE_SCOPE=SYNTHETIC_ONLY'
echo "MODE=$mode"
# -I -S -B hardens interpreter initialization but does not prove FD/seccomp limits.
/usr/bin/timeout 20s "$@" /usr/bin/bwrap \
  --die-with-parent --new-session \
  --unshare-user --unshare-pid --unshare-ipc --unshare-net --cap-drop ALL \
  --ro-bind /usr /usr \
  --symlink usr/bin /bin --symlink usr/lib /lib \
  --symlink usr/lib64 /lib64 \
  --ro-bind "$scratch/evidence" /evidence \
  --ro-bind "$scratch/probe.py" /probe.py \
  --tmpfs /tmp --proc /proc \
  --clearenv --setenv PATH /usr/bin --setenv TMPDIR /tmp \
  --setenv EXPECTED_UID "$uid" \
  --setenv EMPTY_GROUPS "$require_empty_groups" \
  --chdir / \
  -- /usr/bin/python3.13 -I -S -B /probe.py

echo 'SYNTHETIC_BWRAP_REPLAY=PASS'
