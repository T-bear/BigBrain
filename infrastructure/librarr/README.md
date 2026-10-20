# BigBrain-maintained Librarr image

BB-102/103/105 builds Librarr from immutable upstream commit `1208254c20b31fbf217558c0fb987f779fed1cf8` and applies seven independently reviewable patches: `patches/0001-audiobook-import-no-overwrite.patch`, `patches/0002-explicit-source-policy.patch`, `patches/0003-durable-import-outcome.patch`, `patches/0004-author-aware-discovery.patch`, `patches/0005-pinned-native-source-policy.patch`, `patches/0006-audiobookbay-parser-boundaries.patch` and `patches/0007-audiobookbay-query-case-normalization.patch`.

## Why the patch exists

Upstream audiobook organization replaces or merges existing destinations. BigBrain requires fail-closed import: an existing or partially populated `{author}/{title}` destination is a conflict and must remain unchanged.

The patch is intentionally limited to `internal/organize/pipeline.go` plus isolated regression tests. It atomically reserves the final book directory, uses exclusive file creation, never replaces hardlinks, keeps a move source until the complete destination tree has been placed, rejects unsafe destination components and refuses symlink escape through the author destination.

The fifth patch supersedes the earlier per-source allowlist after an explicit owner decision. It requires the exact `LIBRARR_TRUSTED_SOURCE_REVISION`, preserves constructor order and admits only the five audiobook source identifiers reviewed in the pinned source tree: Prowlarr plus AudioBookBay, LibriVox, The Pirate Bay audiobook and BookTracker audiobook. It fails startup on a revision mismatch, a missing expected source, duplicate registration, malformed ID or any injected/future audiobook source. Prowlarr therefore remains first/preferred. The checked-in `pinned-audiobook-sources.json` is tied to the same immutable revision and supplies the public configuration needed by AudioBookBay, LibriVox and The Pirate Bay; BookTracker remains disabled unless its separate server-side credentials/configuration exist.

## Build and verification

```bash
docker build -f infrastructure/librarr/Dockerfile -t bigbrain-librarr:<unique-reviewed-revision> .
```

The third BB-103 patch records a sanitized `torrent_import_failed` activity event keyed by the torrent hash whenever final import fails. It does not alter placement: the no-overwrite patch remains authoritative, the source is retained and the existing destination stays untouched. This durable evidence lets BigBrain distinguish a real conflict/failure from asynchronous import/indexing without reading provider logs.

The fourth discovery-quality patch keeps title-only search unchanged. For an audiobook title plus author it issues at most three normalized, unique source queries: title, title plus author and—only for leading English articles—the article-free title plus author. Variants run within the existing bounded/cancellable source search, partial source success is retained, and author remains a post-result scoring signal. Prowlarr no longer appends a second audiobook term when the query already contains `audiobook`, `audio book` or `ljudbok`. Provider/source architecture and acquisition/import behavior are unchanged.

The fifth patch also makes the diagnostic streaming endpoint use those same bounded author-aware variants and exposes only a per-source result count alongside the existing stream data. This permits sanitized coverage verification without logging provider payloads. BigBrain accepts only the exact pinned source IDs; direct-download candidates that cannot satisfy the existing hash-backed job lifecycle are visible for discovery but rejected before any provider request.

The sixth patch fixes only AudioBookBay search parsing. Adjacent DOM text nodes are separated before reading `Language:` so current `English` plus nested `Keywords:` markup is not misread as one language token. A sanitized network-free fixture reproduces that boundary. Debug logging contains only parser stage counts; source URLs, queries, titles and download data are excluded.

The seventh patch fixes AudioBookBay's proven case-dependent search behavior by lowercasing only its outgoing `s` query parameter with Go's deterministic Unicode lowercase operation. The owner input remains unchanged in BigBrain, Librarr relevance filtering, scoring and every other source. A network-free table regression covers `Pirateaba`, `pirateaba`, `PIRATEABA`, `The Wandering Inn` and `the wandering inn`.

The Docker build applies all patches with `git apply --check`, verifies formatting, runs the complete upstream `internal/organize`, `internal/search`, `internal/download`, `internal/api` and `internal/db` packages and then builds the binary. Tests and compilation run as builder UID1000 with an explicit UID assertion, an owned build/GOPATH and a writable builder-owned HOME; root is used only for image preparation. In particular, the existing 0444 settings-write failure test must exercise actual permission denial, with its 500/no-path-disclosure assertions unchanged. The runtime stage remains non-root and contains neither Git nor the Go toolchain. Its entrypoint fails closed before Librarr starts if any required server-side credential, Audiobookshelf library ID or pinned trusted revision is absent; it never prints values.

## Upgrade procedure

1. Review upstream import, path mapping, authentication and download-client changes.
2. Pin the new immutable commit and base-image digests.
3. Rebase the narrowly scoped patches; do not weaken any no-overwrite or source-policy test. Inventory every audiobook source in the proposed revision. Any added, removed or renamed source requires explicit review and a deliberate update to the revision-bound source set.
4. Run the patched package tests, full BigBrain validation and an import dry run using temporary storage.
5. Re-run the security review before deployment.

Remove the import patch when a reviewed upstream release provides equivalent fail-closed file, directory, partial-destination and symlink-conflict behavior with tests for move, copy and hardlink modes. Remove the source patch only when upstream can bind discovery-source trust to an immutable reviewed revision while rejecting unexpected runtime registrations.

## BB-158 direct audio import — review extension, 2026-10-18

Patch `0008-bounded-direct-audio-import.patch` adds an authenticated, bounded raw MP3/M4A stream API
and an additive table in the existing Librarr DB. The first seven patches/torrent behavior remain
unchanged. The Dockerfile applies eight patches and now tests the API and DB packages as well.
No provider/container is replaced by publishing this source.

[Canonical protocol, limits, failure handling, verification and deployment gates](../../docs/reports/features/media/bb-158-url-import-20261017.md#mp3-and-m4a-format-completion--2026-10-18).
MPEG-1 Layer III MP3 with bounded ID3/APEv2 text tags and progressive AAC-LC M4A are admitted
within the report’s explicit format subset. mp4ff v0.59.0 is pinned in patch8; its MIT notice ships
in the runtime image. No codec execution or transcoding is added. Final content is published
with an exclusive link only after bounded structural validation/hash/fsync. The existing reservation
owner is reused; interrupted reservations never become retry authority. Roots/parents must be
trusted service-owned local directories supporting hardlinks/fsync. Unknown leftovers require manual
review, not automatic recursive removal. ABS must see identical files/inodes and folder layout.
The capability endpoint identifies `bb158-direct-audio-v2`; BigBrain's new opt-in defaults false.
This is not a decoder sandbox or a production-verified scanner safety claim.

## BB-158 deployment and session interruption — proposed operational sequence

This is the minimal correction to the existing Compose procedure, **not an installed deployment
runner or permission to deploy**. Keep the current hardened ABS and original Librarr/API/Web
running during preparation. The prior detached build was independent of the agent, but service
stop/capture/start were separate agent operations: losing the session left services stopped.
A shell EXIT trap alone does not cover SIGKILL or disappearance of the terminal/session.
The proposed cleanup uses documented [systemd 257 service semantics](https://www.freedesktop.org/software/systemd/man/257/systemd.service.html#ExecStopPost=):
`ExecStopPost` also runs on startup failure; oneshot needs an explicit finite start timeout.

1. **Build before interruption.** From the exact owner-approved clean source, build Librarr,
   API (existing `scripts/build-bigbrain-api.sh`) and Web into unique candidate tags. For the API
   script, preserve/tag the running rollback image first because its build updates a local image
   tag; building must never replace a running container. Require exit 0, immutable image IDs,
   source revision/build-context identity, passing build tests and isolated compatibility checks
   for **all** candidates. A failed/incomplete build means STOP with production still running.
   No stop, backup quiescence, config switch or recreation while any required build is pending.
2. **Prepare a bounded execution envelope before any stop.** Use the host's existing systemd
   system-manager transient-service mechanism (not an unverified login-bound user unit) to execute
   the already reviewed Compose/capture commands, never
   the interactive agent shell. A named `Type=oneshot` unit with finite `TimeoutStartSec`,
   `TimeoutStopSec`, `KillMode=control-group`, `Restart=no` and a bounded `ExecStopPost` recovery
   command survives agent disconnect and handles failed/terminated execution. No timer, daemon,
   new platform, automatic retry or deployment at boot. Run with the existing authorized Docker
   operator identity; do not use Docker to obtain host administrative privileges. Local systemd
   authorization is an execution prerequisite, not a reason to weaken the boundary.
3. **Fix the scope and recovery decision in private durable state.** Before stopping, save the
   exact old container ID/image/config/mount fingerprint, candidate image ID, approved service
   name, finite interruption deadline and one unique execution ID. Protect inputs/results,
   exclude secrets from logs, take an exclusive operation lock and fsync phase records plus their
   directory. Never let a previous run marker authorize a new operation. Verify the unit is
   actually registered/running, its owner/timeout/recovery command and result location before
   the agent relinquishes control. Do not apply this proposal until the concrete local commands
   and recovery predicates are prepared and failure-tested in an isolated, nonproduction context.
4. **Capture and restart old service as one operation.** Revalidate activity, writers, free space,
   protected mounts and original identities; record `stopping-old` **before** a graceful stop.
   After confirmed clean exit, copy the approved durable scope including any WAL/SHM, record
   checksums, then start the same unchanged old container immediately within that unit. Perform
   deeper integrity/restore-readability verification on the copies while production runs again.
   A failed copy is not permission to delete source data or leave a safe old container stopped.
   Account for shared writers/dependencies explicitly; do not assume a stopped reader quiesces
   its producer. No Finance restore or database operation is introduced by the shared API stop.
5. **Replace one service at a time, only after the capture gate.** Use the existing scoped Compose
   `up -d --no-deps --no-build` flow and pinned candidate image IDs. ABS is already hardened and
   needs no routine recreation. Remaining order is Librarr → API(import false) → Web. Before any
   replacement command, durably mark `replacement-possible` so interruption cannot look like the
   harmless backup-only phase. Verify actual identity, health and required version contract;
   only then mark completion. Never restore backup databases to obtain a green health check.
   Keep BB-158 disabled until the existing activation gates and explicit authorization are met.

**Independent recovery rules:** `ExecStopPost` re-reads live Docker state and the same run's
write-ahead record; it never trusts a stale phase alone. During the capture-only phase it may
start only the exact old, still-existing container with unchanged image/config/mount identity,
clean exit and no evidence of replacement or unsafe source mutation. It must be idempotent:
an already-running matching container needs no action only after any in-flight Docker daemon
stop/start request has resolved; killing a Docker CLI does not cancel the daemon operation.
If pending operations cannot be reconciled within the deadline, preserve state and report failure.
If replacement may have happened,
identity is ambiguous, a write/migration/import may have occurred, or data safety is uncertain,
it must preserve data, record failure and alert the operator via existing systemd/journal
facilities; **no automatic downgrade, recreation, import retry or database/volume restore**.
Candidate-to-old-image rollback is permitted only with separately verified compatibility of
current durable state and explicit approval; never infer compatibility from image health.

The operator must select an explicit per-service deadline from measured stop/capture/start
cost before authorizing execution. No build, test suite or human-review wait belongs inside that
window. Manager-owned execution/recovery removes dependence on Codex remaining connected; it
cannot promise availability after host/power/kernel failure or unknown durable-state changes.
After host reboot, Docker's existing restart policy and the persisted run record must be checked;
no new automatic deployment is scheduled. If the envelope cannot be provisioned and demonstrated,
STOP **before stopping production**, rather than relying on a trap or a future agent session.

Before future execution, demonstrate with isolated containers: failed build causes zero stops;
agent disconnect leaves the unit running; worker failure during capture restarts only the exact
safe old container; repeated cleanup is harmless; stale IDs/mounts or `replacement-possible`
never trigger automatic rollback; all original data/volume hashes remain unchanged. These
operational fault tests and the concrete transient unit have **not been run/installed by this
build-correction checkpoint**. This section is the requested proposal for review, not runtime
verification or an expansion into general disaster recovery.

### Isolated recovery proof — prepared 2026-10-20

The [bounded test package](../../tests/deployment/bb158-recovery/isolated-tests.py) exercises
only labelled, networkless, unmounted scratch containers; it rejects production targets.
The [current report](../../docs/reports/features/media/bb-158-url-import-20261017.md#candidate-builds-and-isolated-recovery-preparation--2026-10-20)
records private package hashes, seven local PASS results and the pending owner-local
`--systemd` test. Local mode is not system-manager evidence. Run only from the prepared private
package with its matching checksum manifest; do not execute the entire driver as root.
The test does not implement capture, Compose replacement or production rollback. The concrete
production envelope, lock, deadlines and fresh data/activity gates above still require verification
before any real stop. Do not weaken test target restrictions to operate production.

The later [production-procedure checkpoint](../../docs/reports/features/media/bb-158-url-import-20261017.md#verified-systemd-proof-and-prepared-production-procedure--2026-10-20)
verifies all seven actual isolated systemd cases and prepares a distinct, exact-plan-bound operator
procedure. It has not run against production; owner-local administrative launch and all in-procedure
fresh capture/integrity gates remain required. Existing test-only restrictions are unchanged.
