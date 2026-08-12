#!/bin/sh
set -eu
systemctl disable --now bigbrain.service || true
rm -f /etc/systemd/system/bigbrain.service /etc/bigbrain/bigbrain.conf /usr/local/libexec/bigbrain-host
systemctl daemon-reload
echo "bigbrain.service removed; repository data and Docker volumes were preserved"
