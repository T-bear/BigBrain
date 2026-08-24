# BigBrain-maintained Librarr image

BB-102/103 builds Librarr from immutable upstream commit `1208254c20b31fbf217558c0fb987f779fed1cf8` and applies five independently reviewable patches: `patches/0001-audiobook-import-no-overwrite.patch`, `patches/0002-explicit-source-policy.patch`, `patches/0003-durable-import-outcome.patch`, `patches/0004-author-aware-discovery.patch` and `patches/0005-pinned-native-source-policy.patch`.

## Why the patch exists

Upstream audiobook organization replaces or merges existing destinations. BigBrain requires fail-closed import: an existing or partially populated `{author}/{title}` destination is a conflict and must remain unchanged.

The patch is intentionally limited to `internal/organize/pipeline.go` plus isolated regression tests. It atomically reserves the final book directory, uses exclusive file creation, never replaces hardlinks, keeps a move source until the complete destination tree has been placed, rejects unsafe destination components and refuses symlink escape through the author destination.

The fifth patch supersedes the earlier per-source allowlist after an explicit owner decision. It requires the exact `LIBRARR_TRUSTED_SOURCE_REVISION`, preserves constructor order and admits only the five audiobook source identifiers reviewed in the pinned source tree: Prowlarr plus AudioBookBay, LibriVox, The Pirate Bay audiobook and BookTracker audiobook. It fails startup on a revision mismatch, a missing expected source, duplicate registration, malformed ID or any injected/future audiobook source. Prowlarr therefore remains first/preferred. The checked-in `pinned-audiobook-sources.json` is tied to the same immutable revision and supplies the public configuration needed by AudioBookBay, LibriVox and The Pirate Bay; BookTracker remains disabled unless its separate server-side credentials/configuration exist.

## Build and verification

```bash
docker build -f infrastructure/librarr/Dockerfile -t bigbrain-librarr:1208254-bb5 .
```

The third BB-103 patch records a sanitized `torrent_import_failed` activity event keyed by the torrent hash whenever final import fails. It does not alter placement: the no-overwrite patch remains authoritative, the source is retained and the existing destination stays untouched. This durable evidence lets BigBrain distinguish a real conflict/failure from asynchronous import/indexing without reading provider logs.

The fourth discovery-quality patch keeps title-only search unchanged. For an audiobook title plus author it issues at most three normalized, unique source queries: title, title plus author and—only for leading English articles—the article-free title plus author. Variants run within the existing bounded/cancellable source search, partial source success is retained, and author remains a post-result scoring signal. Prowlarr no longer appends a second audiobook term when the query already contains `audiobook`, `audio book` or `ljudbok`. Provider/source architecture and acquisition/import behavior are unchanged.

The fifth patch also makes the diagnostic streaming endpoint use those same bounded author-aware variants and exposes only a per-source result count alongside the existing stream data. This permits sanitized coverage verification without logging provider payloads. BigBrain accepts only the exact pinned source IDs; direct-download candidates that cannot satisfy the existing hash-backed job lifecycle are visible for discovery but rejected before any provider request.

The Docker build applies all patches with `git apply --check`, verifies formatting, runs the complete upstream `internal/organize`, `internal/search` and `internal/download` packages plus the focused API regression and then builds the binary. The runtime stage remains non-root and contains neither Git nor the Go toolchain. Its entrypoint fails closed before Librarr starts if any required server-side credential, Audiobookshelf library ID or pinned trusted revision is absent; it never prints values.

## Upgrade procedure

1. Review upstream import, path mapping, authentication and download-client changes.
2. Pin the new immutable commit and base-image digests.
3. Rebase the narrowly scoped patches; do not weaken any no-overwrite or source-policy test. Inventory every audiobook source in the proposed revision. Any added, removed or renamed source requires explicit review and a deliberate update to the revision-bound source set.
4. Run the patched package tests, full BigBrain validation and an import dry run using temporary storage.
5. Re-run the security review before deployment.

Remove the import patch when a reviewed upstream release provides equivalent fail-closed file, directory, partial-destination and symlink-conflict behavior with tests for move, copy and hardlink modes. Remove the source patch only when upstream can bind discovery-source trust to an immutable reviewed revision while rejecting unexpected runtime registrations.
