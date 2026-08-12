#!/bin/sh
set -eu
repo_dir=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)
test -f "$repo_dir/compose.yaml"
install -d -m 0755 /etc/bigbrain /usr/local/libexec
install -m 0755 "$repo_dir/scripts/bigbrain-host" /usr/local/libexec/bigbrain-host
install -m 0644 "$repo_dir/deploy/systemd/bigbrain.service" /etc/systemd/system/bigbrain.service
tmp=$(mktemp)
trap 'rm -f "$tmp"' EXIT
printf 'BIGBRAIN_PROJECT_DIR=%s\n' "$repo_dir" >"$tmp"
install -m 0644 "$tmp" /etc/bigbrain/bigbrain.conf
systemctl daemon-reload
systemctl enable bigbrain.service
systemd-analyze verify /etc/systemd/system/bigbrain.service
echo "bigbrain.service installed and enabled; run: systemctl start bigbrain.service"
