#!/usr/bin/env bash
# Finance Release 1: rootless synthetic SQLite launch experiment.
# NO real paths, NO --audit/uid/root mode, NO credentials, NO Finance data.
set -Eeuo pipefail
umask 077

[[ "$#" -eq 0 ]] || { echo 'STOPP: this synthetic probe accepts no arguments' >&2; exit 2; }
[[ "$EUID" -ne 0 ]] || { echo 'STOPP: do not run as root' >&2; exit 1; }
for binary in /usr/bin/python3.13 /usr/bin/bwrap /usr/bin/timeout; do
  [[ -x "$binary" ]] || { echo 'STOPP: missing synthetic-test runtime' >&2; exit 1; }
done

scratch="$(mktemp -d /tmp/bb-finance-synth-start.XXXXXXXX)"
cleanup() {
  local rc=$?
  if [[ "$scratch" == /tmp/bb-finance-synth-start.* && -d "$scratch" && ! -L "$scratch" ]]; then
    chmod u+rwx -- "$scratch/evidence"
    rm -rf -- "$scratch"
  fi
  exit "$rc"
}
trap cleanup EXIT
mkdir "$scratch/evidence"
printf 'hidden-synthetic-sentinel\n' > "$scratch/host-sentinel.txt"

/usr/bin/python3.13 -I -S -B - "$scratch/evidence" <<'PY'
import pathlib
import sqlite3
import sys

root = pathlib.Path(sys.argv[1])
db = root / "bb-091-pre-migration-20260817.db"
connection = sqlite3.connect(db)
try:
    connection.execute("CREATE TABLE synthetic_check (value TEXT NOT NULL)")
    connection.execute("INSERT INTO synthetic_check VALUES ('synthetic-only')")
    connection.commit()
finally:
    connection.close()
for ident in ("3b093584173d74f6", "8b549915b4e4129c"):
    (root / ("finance-backup-" + ident + ".manifest.json")).write_text(
        '{"synthetic":true}\n')
    (root / ("finance-backup-" + ident + ".data.json")).write_text(
        '{"synthetic":"data"}\n')
PY

cat > "$scratch/probe.py" <<'PY'
import os
import socket
import sqlite3
import stat
from pathlib import Path

root = Path("/evidence")
db = "bb-091-pre-migration-20260817.db"
names = {db}
for ident in ("3b093584173d74f6", "8b549915b4e4129c"):
    names.add("finance-backup-" + ident + ".manifest.json")
    names.add("finance-backup-" + ident + ".data.json")

assert set(os.listdir(root)) == names, "unexpected or missing evidence"
for name in names:
    p = root / name
    st = p.lstat()
    assert stat.S_ISREG(st.st_mode) and st.st_nlink == 1, name
    assert stat.S_IMODE(st.st_mode) == 0o440, name
print("SYNTHETIC_FIVE_FILE_ALLOWLIST=PASS")

mounts = []
for record in Path("/proc/self/mountinfo").read_text().splitlines():
    mount_fields = record.split(" - ", 1)[0].split()
    if len(mount_fields) >= 6 and mount_fields[4] == "/evidence":
        mounts.append(set(mount_fields[5].split(",")))
assert len(mounts) == 1
assert "ro" in mounts[0] and "rw" not in mounts[0], "evidence mount not read-only"
print("SYNTHETIC_MOUNT_RO=PASS")

for suffix in ("-wal", "-shm", "-journal"):
    assert not (root / (db + suffix)).exists(), suffix
print("SYNTHETIC_NO_SQLITE_SIDECARS=PASS")

uri = "file:/evidence/" + db + "?mode=ro&immutable=1"
with sqlite3.connect(uri, uri=True) as conn:
    conn.execute("PRAGMA query_only=ON")
    assert conn.execute("PRAGMA quick_check").fetchone()[0] == "ok"
    assert conn.execute("SELECT value FROM synthetic_check").fetchone()[0] == "synthetic-only"
print("SYNTHETIC_SQLITE_READ_ONLY=PASS")

for name in names:
    try:
        with open(root / name, "wb") as stream:
            stream.write(b"forbidden")
    except OSError:
        continue
    raise AssertionError("synthetic evidence writable")
print("SYNTHETIC_WRITES_DENIED=PASS")

for hidden in ("/home", "/etc", "/run", "/var", "/finance",
               "/tmp/host-sentinel.txt", "/evidence/host-sentinel.txt"):
    assert not Path(hidden).exists(), hidden
assert set(os.listdir("/")) == {
    "bin", "evidence", "lib", "lib64", "probe.py", "proc", "tmp", "usr"
}
print("SYNTHETIC_MINIMAL_FILESYSTEM=PASS")

assert all(name == "lo" for _, name in socket.if_nameindex())
print("SYNTHETIC_NETWORK_NAMESPACE=PASS")
status = dict(line.strip().split(":", 1)
              for line in Path("/proc/self/status").read_text().splitlines()
              if line.startswith(("CapEff:", "CapPrm:", "NoNewPrivs:", "Uid:")))
assert int(status["CapEff"], 16) == 0
assert int(status["CapPrm"], 16) == 0
assert status["NoNewPrivs"].strip() == "1"
uid_values = [int(part) for part in status["Uid"].split()]
assert all(uid == uid_values[0] for uid in uid_values)
assert uid_values[0] == int(os.environ["EXPECTED_UID"])
print("SYNTHETIC_UNPRIVILEGED_IDENTITY=PASS")
PY

chmod 0500 "$scratch/evidence"
chmod 0440 "$scratch/evidence/"*
chmod 0400 "$scratch/probe.py"
chmod 0600 "$scratch/host-sentinel.txt"

echo 'SCOPE=SYNTHETIC_SQLITE_ONLY'
/usr/bin/timeout 20s /usr/bin/bwrap \
  --die-with-parent --new-session \
  --unshare-user --unshare-pid --unshare-ipc --unshare-net --cap-drop ALL \
  --ro-bind /usr /usr \
  --symlink usr/bin /bin --symlink usr/lib /lib \
  --symlink usr/lib64 /lib64 \
  --ro-bind "$scratch/evidence" /evidence \
  --ro-bind "$scratch/probe.py" /probe.py \
  --tmpfs /tmp --proc /proc \
  --clearenv --setenv PATH /usr/bin --setenv TMPDIR /tmp \
  --setenv EXPECTED_UID "$(id -u)" \
  --chdir / \
  -- /usr/bin/python3.13 -I -S -B /probe.py

echo 'SYNTHETIC_AUDIT_START=PASS'
