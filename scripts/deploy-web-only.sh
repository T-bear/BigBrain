#!/usr/bin/env bash
# Owner-authorized Web-only PR merge/deploy. T2/T7 review required before use.
set -Eeuo pipefail
umask 077
REPO='T-bear/BigBrain'
PR="${1:-}"
ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd -P)"
[[ $# -eq 1 && "$PR" =~ ^[1-9][0-9]*$ ]] || { echo 'Usage: deploy-web-only.sh PR_NUMBER' >&2; exit 2; }
for cmd in git gh jq docker curl flock mktemp grep od tr head; do
  command -v "$cmd" >/dev/null || { echo "Missing: $cmd" >&2; exit 2; }
done
cd "$ROOT"
[[ "$(git rev-parse --show-toplevel)" == "$ROOT" ]] || { echo 'Not repository root' >&2; exit 2; }
gh auth status >/dev/null 2>&1 || { echo 'Authenticate with gh auth login' >&2; exit 2; }
[[ "$(gh repo view --json nameWithOwner --jq '.nameWithOwner')" == "$REPO" ]] || { echo 'Wrong repository' >&2; exit 2; }
STATE="$HOME/.local/state/bigbrain-web-deploy"
mkdir -p -- "$STATE"
chmod 700 "$STATE"
exec 9>"$STATE/lock"
flock -n 9 || { echo 'Another Web deploy is running' >&2; exit 2; }
TMP="$(mktemp -d)"
WORKTREE=''
SWITCH_STARTED=0
DEPLOY_OK=0
OLD_IMAGE=''
OLD_CID=''
MERGE_SHA=''
compose() { docker compose --project-directory "$ROOT" -f "$ROOT/compose.yaml" "$@"; }
http_ok() {
  local html_code icon_code
  html_code="$(curl --noproxy '*' -sS --max-time 3 -o "$TMP/index" -w '%{http_code}' 'http://127.0.0.1:13000/')" || return 1
  icon_code="$(curl --noproxy '*' -sS --max-time 3 -o "$TMP/icon" -w '%{http_code}' 'http://127.0.0.1:13000/icons/bigbrain-32.png')" || return 1
  [[ "$html_code" == 200 && "$icon_code" == 200 ]] || return 1
  grep -Eiq '<html([[:space:]>])' "$TMP/index" || return 1
  [[ "$(head -c 8 "$TMP/icon" | od -An -tx1 | tr -d '[:space:]')" == '89504e470d0a1a0a' ]]
}
wait_healthy() {
  local expected="$1" n cid
  for ((n=0;n<18;n++)); do
    cid="$(compose ps -q web 2>/dev/null || true)"
    if [[ -n "$cid" ]] &&
      [[ "$(docker inspect --format '{{.Image}}' "$cid" 2>/dev/null || true)" == "$expected" ]] &&
      [[ "$(docker inspect --format '{{.State.Health.Status}}' "$cid" 2>/dev/null || true)" == healthy ]] &&
      http_ok; then return 0; fi
    sleep 5
  done
  return 1
}
rollback() {
  echo 'WEB DEPLOY FAILED: restoring previous image' >&2
  docker image tag "$OLD_IMAGE" 'bigbrain-sprint1-web:latest' || return 1
  compose up -d --no-deps --no-build --force-recreate web || return 1
  wait_healthy "$OLD_IMAGE" || return 1
  echo 'ROLLBACK PASS: prior Web image restored; Git main remains merged' >&2
}
cleanup() {
  local code="$?"
  trap - EXIT
  if ((SWITCH_STARTED==1 && DEPLOY_OK==0)); then
    rollback || { echo 'CRITICAL: rollback failed; operator intervention required' >&2; code=1; }
  fi
  [[ -z "$WORKTREE" ]] || git worktree remove --force -- "$WORKTREE" >/dev/null 2>&1 || true
  rm -rf -- "$TMP"
  exit "$code"
}
trap cleanup EXIT
trap 'exit 129' HUP
trap 'exit 130' INT
trap 'exit 143' TERM

# Both an open PR and its already-merged SHA are resumable using the same PR number.
meta="$(gh api "repos/$REPO/pulls/$PR")"
[[ "$(jq -r '.base.ref' <<< "$meta")" == main &&
    "$(jq -r '.head.repo.full_name' <<< "$meta")" == "$REPO" ]] || { echo 'STRIKT: PR source/base mismatch' >&2; exit 3; }
HEAD="$(jq -r '.head.sha' <<< "$meta")"
[[ "$HEAD" =~ ^[0-9a-f]{40}$ ]] || exit 3
status="$(jq -r '.state' <<< "$meta")"
merged="$(jq -r '.merged' <<< "$meta")"
if [[ "$status" == open && "$merged" == false && "$(jq -r '.draft' <<< "$meta")" == false ]]; then
  MODE=open
elif [[ "$status" == closed && "$merged" == true ]]; then
  MODE=resume
  MERGE_SHA="$(jq -r '.merge_commit_sha' <<< "$meta")"
  [[ "$MERGE_SHA" =~ ^[0-9a-f]{40}$ ]] || exit 3
else
  echo 'STRIKT: neither an open ready PR nor an already-merged PR' >&2; exit 3
fi

git fetch --no-tags origin main >/dev/null
git fetch --no-tags origin "refs/pull/$PR/head" >/dev/null
[[ "$(git rev-parse FETCH_HEAD)" == "$HEAD" ]] || { echo 'PR head changed' >&2; exit 3; }
CURRENT="$(git rev-parse refs/remotes/origin/main)"
[[ "$(git branch --show-current)" == main ]] || { echo 'Local checkout must be main' >&2; exit 3; }
git diff --quiet && git diff --cached --quiet || { echo 'Local tracked changes present' >&2; exit 3; }
if [[ "$MODE" == open ]]; then
  BASE="$CURRENT"
  [[ "$(git rev-parse HEAD)" == "$BASE" ]] || { echo 'Local main must match origin/main' >&2; exit 3; }
  [[ "$(git merge-base "$BASE" "$HEAD")" == "$BASE" ]] || { echo 'STRIKT: outdated PR branch' >&2; exit 3; }
else
  [[ "$CURRENT" == "$MERGE_SHA" ]] || { echo 'Resumption only allowed when main is this exact merge SHA' >&2; exit 3; }
  BASE="$(git rev-parse "$MERGE_SHA^1")"
  [[ "$(git rev-parse HEAD)" == "$BASE" || "$(git rev-parse HEAD)" == "$MERGE_SHA" ]] || { echo 'Local main not at merge base or merged SHA' >&2; exit 3; }
  [[ "$(git rev-parse "$MERGE_SHA^{tree}")" == "$(git rev-parse "$HEAD^{tree}")" ]] || { echo 'STRIKT: merged tree differs from reviewed PR head' >&2; exit 3; }
fi

mapfile -d '' -t FILES < <(git diff --no-renames --name-only -z "$BASE" "$HEAD")
((${#FILES[@]})) || { echo 'STRIKT: no changed files' >&2; exit 3; }
for path in "${FILES[@]}"; do
  case "$path" in
    src/BigBrain.Web/src/*.ts|src/BigBrain.Web/src/*.tsx|src/BigBrain.Web/src/*.css|src/BigBrain.Web/src/*.svg|src/BigBrain.Web/src/*.json|src/BigBrain.Web/index.html|src/BigBrain.Web/public/icons/*.png|src/BigBrain.Web/public/manifest.webmanifest) ;;
    *) echo "STRIKT: $path" >&2; exit 3 ;;
  esac
  case "$path" in
    src/BigBrain.Web/src/api.ts|src/BigBrain.Web/src/*Api.ts|src/BigBrain.Web/src/*Client.ts|src/BigBrain.Web/src/*/api/*)
      echo "STRIKT: API adapter: $path" >&2; exit 3 ;;
  esac
done
# Reject symlinks/submodules and executable changes in the candidate tree.
if git ls-tree -r "$HEAD" -- src/BigBrain.Web/src src/BigBrain.Web/public src/BigBrain.Web/index.html | awk '$1 != "100644" { bad=1 } END { exit !bad }'; then
  echo 'STRIKT: Web tree includes non-regular files' >&2; exit 3
fi
git diff --check "$BASE" "$HEAD"

ci_green() {
  local sha="$1" n
  for ((n=0;n<120;n++)); do
    gh api "repos/$REPO/commits/$sha/check-runs?per_page=100" > "$TMP/checks"
    if jq -e '
      . as $root |
      ($root.total_count <= 100) and
      all(["backend","frontend","documentation","secrets"][];
        . as $name |
        [$root.check_runs[] | select(.name == $name and .app.slug == "github-actions")] as $matches |
        ($matches | length > 0) and
        all($matches[]; .status == "completed" and .conclusion == "success"))
    ' "$TMP/checks" >/dev/null; then return 0; fi
    if jq -e '
      any(.check_runs[];
        .app.slug == "github-actions" and
        (.name as $n | ["backend","frontend","documentation","secrets"] | index($n) != null) and
        .status == "completed" and
        (.conclusion == "failure" or .conclusion == "cancelled" or .conclusion == "timed_out"))
    ' "$TMP/checks" >/dev/null; then
      echo "CI failed for $sha" >&2; return 1
    fi
    sleep 6
  done
  echo "CI pending after 12 minutes for $sha; rerun this command with PR #$PR to resume" >&2
  return 1
}

# All known pre-merge checks, including resolved Compose, precede gh pr merge.
[[ -f "$ROOT/compose.yaml" ]] || exit 3
compose config --quiet >/dev/null || { echo 'STRIKT: Compose preflight failed' >&2; exit 3; }
compose config --format json | jq -e '
  .name == "bigbrain-sprint1" and .services.web.build != null and
  (.services.web.image == null or .services.web.image == "bigbrain-sprint1-web" or .services.web.image == "bigbrain-sprint1-web:latest") and
  ([.services.web.ports[] | select((.published|tostring)=="13000" and (.target|tostring)=="8080")] | length) == 1
' >/dev/null || { echo 'STRIKT: Web Compose contract changed' >&2; exit 3; }
OLD_CID="$(compose ps -q web)"
[[ -n "$OLD_CID" && "$(docker inspect --format '{{.State.Health.Status}}' "$OLD_CID")" == healthy ]] || { echo 'Old Web not healthy' >&2; exit 3; }
OLD_IMAGE="$(docker inspect --format '{{.Image}}' "$OLD_CID")"
http_ok || { echo 'Old Web HTTP check failed' >&2; exit 3; }
ci_green "$HEAD" || exit 3
if [[ "$MODE" == resume ]]; then ci_green "$MERGE_SHA" || exit 3; fi

WORKTREE="$TMP/source"
git worktree add --detach "$WORKTREE" "$HEAD" >/dev/null
CANDIDATE="bigbrain-web:candidate-$HEAD"
echo 'Building candidate while old Web stays running...'
docker build --file "$WORKTREE/src/BigBrain.Web/Dockerfile" --tag "$CANDIDATE" "$WORKTREE"
NEW_IMAGE="$(docker image inspect --format '{{.Id}}' "$CANDIDATE")"
[[ "$(compose ps -q web)" == "$OLD_CID" ]] || { echo 'Web changed concurrently' >&2; exit 3; }

if [[ "$MODE" == open ]]; then
  [[ "$(gh api "repos/$REPO/branches/main" --jq '.commit.sha')" == "$BASE" ]] || { echo 'main changed; STOP' >&2; exit 3; }
  [[ "$(gh api "repos/$REPO/pulls/$PR" --jq '.head.sha')" == "$HEAD" ]] || { echo 'PR changed; STOP' >&2; exit 3; }
  gh pr merge "$PR" --repo "$REPO" --merge --match-head-commit "$HEAD"
  MERGE_SHA="$(gh api "repos/$REPO/pulls/$PR" --jq '.merge_commit_sha')"
  [[ "$MERGE_SHA" =~ ^[0-9a-f]{40}$ ]] || { echo 'Merge SHA unknown; STOP' >&2; exit 3; }
  git fetch --no-tags origin main >/dev/null
  [[ "$(git rev-parse refs/remotes/origin/main)" == "$MERGE_SHA" ]] || { echo 'Main differs from merged PR; STOP' >&2; exit 3; }
  [[ "$(git rev-parse "$MERGE_SHA^{tree}")" == "$(git rev-parse "$HEAD^{tree}")" ]] || { echo 'Merge tree differs; STOP' >&2; exit 3; }
  echo "Merged $MERGE_SHA; waiting for main CI (safe to rerun PR #$PR if CI is delayed)..."
  ci_green "$MERGE_SHA" || { echo "Merged but NOT deployed. Rerun PR #$PR after CI is green." >&2; exit 3; }
  git merge --ff-only "$MERGE_SHA" >/dev/null || { echo 'Merged but local main cannot fast-forward; STOP' >&2; exit 3; }
fi
if [[ "$MODE" == resume && "$(git rev-parse HEAD)" != "$MERGE_SHA" ]]; then
  git merge --ff-only "$MERGE_SHA" >/dev/null || { echo 'Resume could not sync local main; STOP' >&2; exit 3; }
fi
[[ "$(gh api "repos/$REPO/branches/main" --jq '.commit.sha')" == "$MERGE_SHA" ]] || { echo 'Remote main changed before deployment' >&2; exit 3; }
[[ "$(compose ps -q web)" == "$OLD_CID" ]] || { echo 'Old Web changed before switch' >&2; exit 3; }
if [[ "$OLD_IMAGE" == "$NEW_IMAGE" ]]; then
  echo 'ALREADY DEPLOYED: Web image matches candidate; no recreation'
  exit 0
fi
# Exact one-command owner authorization already occurred by invoking this script.
docker image tag "$OLD_IMAGE" 'bigbrain-sprint1-web:previous'
SWITCH_STARTED=1
docker image tag "$NEW_IMAGE" 'bigbrain-sprint1-web:latest'
compose up -d --no-deps --no-build --force-recreate web
wait_healthy "$NEW_IMAGE" || { echo 'New Web failed HTTP/container health checks' >&2; exit 1; }
DEPLOY_OK=1
printf 'PASS: PR #%s, main %s, only Web recreated.\n' "$PR" "$MERGE_SHA"
echo 'Previous Web image: bigbrain-sprint1-web:previous'
echo 'Owner iPhone/PWA acceptance: PENDING'
