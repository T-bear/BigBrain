#!/usr/bin/env bash
# REVIEW ONLY: run after approval of this exact package, access exception and retention.
# Owner-local administrative copy step, then verification as ordinary UID 1000.
set -Eeuo pipefail

script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
checkpoint="$(git -C "$script_dir" rev-parse --show-toplevel)"
branch="$(git -C "$checkpoint" branch --show-current)"
test "$(id -u)" = 1000
case "$branch" in review/*) ;; *) exit 1 ;; esac
test -z "$(git -C "$checkpoint" status --porcelain)"

sudo -- /usr/bin/python3 -I -S -B - \
  "$checkpoint/docs/reports/features/finance/finance-release1-h1-export-20261107.py" <<'PY'
import hashlib
import sys

path = sys.argv[1]
try:
    with open(path, 'rb') as stream:
        source = stream.read(131073)
    if len(source) > 131072 or hashlib.sha256(source).hexdigest() != '27794b59a29dec8380fddff455f56ec23504e4649cc2dbf3c6cdf8ac810e9613':
        raise ValueError()
except Exception:
    print('FAIL H1_export_script_hash; no_retry')
    raise SystemExit(1)
sys.argv = [path, '--owner-approved-read-until', '2026-11-09T20:00:00+01:00']
exec(compile(source, path, 'exec'), {'__name__': '__main__', '__file__': path})
PY

/usr/bin/python3 -I -S -B - \
  "$checkpoint/docs/reports/features/finance/finance-release1-h1-verify-20261107.py" <<'PY'
import hashlib
import os
import sys

path = sys.argv[1]
try:
    if os.getuid() != 1000 or os.geteuid() != 1000:
        raise ValueError()
    with open(path, 'rb') as stream:
        source = stream.read(131073)
    if len(source) > 131072 or hashlib.sha256(source).hexdigest() != '4e6395e152567e83380f42e65885d3a80f485af992379a8dbc8b29b8418a7eb4':
        raise ValueError()
except Exception:
    print('FAIL H1_verifier_identity_or_hash; no_retry')
    raise SystemExit(1)
root = '/var/lib/bigbrain-finance-h1-20261107'
sys.argv = [path, '--db', root + '/bb-091-pre-migration-20260817.db',
            '--manifest-dir', root]
exec(compile(source, path, 'exec'), {'__name__': '__main__', '__file__': path})
PY
