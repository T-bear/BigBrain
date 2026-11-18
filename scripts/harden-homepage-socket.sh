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

if [[ "$MODE" == --check ]]; then
  echo 'PASS: configuration matches reviewed baseline.'
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

[[ "$(grep -Fxc -- "$NEEDLE" "$FILE")" == 1 ]] || {
  echo 'Configuration changed concurrently; STOP' >&2
  exit 1
}

mv -- "$TMPFILE" "$FILE"
trap - EXIT

if ! docker compose -f "$FILE" up -d \
  --no-deps --no-build --pull never \
  --force-recreate homepage; then
  echo 'Recreation failed. Safe config retained; inspect runtime.' >&2
  exit 1
fi

if docker inspect homepage \
  --format '{{range .Mounts}}{{.Destination}}{{"\n"}}{{end}}' |
  grep -Fxq '/var/run/docker.sock'; then
  echo 'FAIL: Docker socket still mounted' >&2
  exit 1
fi

[[ "$(docker inspect homepage --format '{{.State.Running}}')" == true ]] || {
  echo 'FAIL: Homepage is not running' >&2
  exit 1
}

echo "PASS: Homepage running without Docker socket."
echo "Backup: $BACKUP"
echo 'Verify Homepage health and functionality separately.'
