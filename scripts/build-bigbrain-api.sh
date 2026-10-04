#!/usr/bin/env bash
# Build only. Never deploy, start a host, or configure providers.
set -euo pipefail
cd "$(dirname "$0")/.."
if [[ $# != 1 || ! $1 =~ ^[0-9a-f]{40}$ ]]; then
    echo 'Usage: scripts/build-bigbrain-api.sh <exact accepted main SHA>' >&2
    exit 1
fi
revision=$(git rev-parse HEAD)
if [[ $revision != "$1" || $(git rev-parse origin/main) != "$1" ]]; then
    echo 'Accepted revision mismatch; fetch and review first.' >&2
    exit 1
fi
git diff --quiet
git diff --cached --quiet
# Build an immutable archive, not ignored/untracked/mutating local build inputs.
build_context=$(mktemp -d)
trap 'rm -rf -- "$build_context"' EXIT
git archive "$revision" | tar -x -C "$build_context"
# No local secret file enters the build context or Compose interpolation.
docker compose --project-directory "$build_context" --env-file /dev/null -f "$build_context/compose.yaml" \
    build --build-arg "BIGBRAIN_REVISION=$revision" api
