#!/bin/sh
set -eu

for name in API_KEY PROWLARR_API_KEY QB_USER QB_PASS ABS_TOKEN ABS_LIBRARY_ID; do
  eval "value=\${$name:-}"
  if [ -z "$value" ]; then
    echo "Librarr startup refused: required server-side configuration is missing." >&2
    exit 78
  fi
done

exec /usr/local/bin/librarr
