#!/usr/bin/env bash
# BigBrain #101 — bounded Homepage Docker socket removal.
# PR publication never authorizes deployment.
set -Eeuo pipefail
umask 077

MODE="${1:---check}"
[[ $# -le 1 && ( "$MODE" == --check || "$MODE" == --execute ) ]] || {
  echo 'Usage: harden-homepage-socket.sh [--check|--execute]' >&2
  exit 2
}

FILE='/home/enigma/BigBrain/compose.yaml'
CONFIG='/home/enigma/BigBrain/homepage/config/docker.yaml'
NEEDLE='      - /var/run/docker.sock:/var/run/docker.sock:ro'

[[ -f "$FILE" && ! -L "$FILE" && -f "$CONFIG" ]] || {
  echo 'Expected configuration not found; STOP' >&2
  exit 1
}

[[ "$(grep -Fxc -- "$NEEDLE" "$FILE")" == 1 ]] || {
  echo 'Homepage socket configuration differs; STOP' >&2
  exit 1
}

if grep -Eq '^[[:space:]]*[^#[:space:]].*socket:[[:space:]]*' "$CONFIG"; then
  echo 'Homepage Docker integration may be active; STOP' >&2
  exit 1
fi

docker compose -f "$FILE" config --quiet

[[ "$(docker inspect homepage --format '{{.Config.Image}}')" == \
  'ghcr.io/gethomepage/homepage:latest' ]] || {
  echo 'Homepage image differs; STOP' >&2
  exit 1
}

# Verify the current known-good container and the local image before any write.
# --pull never alone does not guarantee that a floating :latest tag is unchanged.
ORIGINAL_IMAGE="$(docker inspect homepage --format '{{.Image}}')"
LOCAL_IMAGE="$(docker image inspect ghcr.io/gethomepage/homepage:latest --format '{{.Id}}')"
[[ "$ORIGINAL_IMAGE" == "$LOCAL_IMAGE" ]] || {
  echo 'Homepage local image tag differs from running image; STOP' >&2
  exit 1
}
[[ "$(docker inspect homepage --format '{{.State.Running}}')" == true &&
   "$(docker inspect homepage --format '{{.State.Health.Status}}')" == healthy ]] || {
  echo 'Homepage baseline is not running and healthy; STOP' >&2
  exit 1
}

if [[ "$MODE" == --check ]]; then
  echo 'PASS: config, image identity and healthy runtime match baseline.'
  echo 'No changes made. Deployment requires separate authorization.'
  exit 0
fi

STATE="$HOME/.local/state/bigbrain-homepage-hardening"
mkdir -p -- "$STATE"
chmod 700 "$STATE"

exec 9>"$STATE/lock"
flock -n 9 || {
  echo 'Another hardening operation is running' >&2
  exit 1
}

STAMP="$(date -u +%Y%m%d-%H%M%S)"
BACKUP="$STATE/compose.$STAMP.backup.yaml"
cp -p -- "$FILE" "$BACKUP"
chmod 600 "$BACKUP"

TMPFILE="$(mktemp "$(dirname "$FILE")/.compose-homepage.XXXXXX")"
trap 'rm -f -- "$TMPFILE"' EXIT

awk -v needle="$NEEDLE" '$0 != needle { print }' "$FILE" > "$TMPFILE"
chmod --reference="$FILE" "$TMPFILE"
docker compose -f "$TMPFILE" config --quiet

# Single-operator runtime action. Refuse unexpected changes since verification.
[[ "$(grep -Fxc -- "$NEEDLE" "$FILE")" == 1 &&
   "$(docker inspect homepage --format '{{.Image}}')" == "$ORIGINAL_IMAGE" ]] || {
  echo 'Baseline changed while preparing; STOP' >&2
  exit 1
}

wait_healthy() {
  local status
  for (( i=0; i<30; i++ )); do
    status="$(docker inspect homepage --format '{{.State.Health.Status}}' 2>/dev/null || true)"
    [[ "$status" == healthy ]] && return 0
    [[ "$status" == unhealthy ]] && return 1
    sleep 2
  done
  return 1
}

restore_baseline() {
  echo 'ERROR: new Homepage failed validation; restoring prior configuration and container.' >&2
  # Explicitly restores the prior, socket-enabled baseline. This is availability
  # recovery, NOT a successful security fix; do not report PASS in that case.
  local restore
  restore="$(mktemp "$(dirname "$FILE")/.compose-homepage-restore.XXXXXX")" || return 1
  cp -- "$BACKUP" "$restore"
  chmod --reference="$FILE" "$restore"
  mv -- "$restore" "$FILE"
  if docker compose -f "$FILE" up -d --no-deps --no-build --pull never --force-recreate homepage &&
     wait_healthy; then
    echo 'RESTORED: prior healthy baseline, Docker socket exposure remains; NO-GO.' >&2
  else
    echo 'CRITICAL: automatic recovery failed. Restore manually from the backup.' >&2
  fi
  echo "Backup: $BACKUP" >&2
  return 1
}

mv -- "$TMPFILE" "$FILE"
trap - EXIT

if ! docker compose -f "$FILE" up -d \
  --no-deps --no-build --pull never --force-recreate homepage; then
  restore_baseline
  exit 1
fi

if ! wait_healthy; then
  restore_baseline
  exit 1
fi

if docker inspect homepage \
  --format '{{range .Mounts}}{{.Destination}}{{"\n"}}{{end}}' |
  grep -Fxq '/var/run/docker.sock'; then
  echo 'FAIL: Docker socket still mounted' >&2
  restore_baseline
  exit 1
fi

[[ "$(docker inspect homepage --format '{{.State.Running}}')" == true ]] || {
  restore_baseline
  exit 1
}

echo 'PASS: Homepage healthy, running and without Docker socket.'
echo "Backup: $BACKUP"
