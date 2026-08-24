# BigBrain-maintained Librarr image

BB-102/103 builds Librarr from immutable upstream commit `1208254c20b31fbf217558c0fb987f779fed1cf8` and applies four independently reviewable patches: `patches/0001-audiobook-import-no-overwrite.patch`, `patches/0002-explicit-source-policy.patch`, `patches/0003-durable-import-outcome.patch` and `patches/0004-author-aware-discovery.patch`.

## Why the patch exists

Upstream audiobook organization replaces or merges existing destinations. BigBrain requires fail-closed import: an existing or partially populated `{author}/{title}` destination is a conflict and must remain unchanged.

The patch is intentionally limited to `internal/organize/pipeline.go` plus isolated regression tests. It atomically reserves the final book directory, uses exclusive file creation, never replaces hardlinks, keeps a move source until the complete destination tree has been placed, rejects unsafe destination components and refuses symlink escape through the author destination.

The second patch requires `LIBRARR_ALLOWED_SOURCES`, preserves constructor order and rejects missing, empty, duplicate or unavailable identifiers. Compose sets `prowlarr_audiobooks,audiobookbay`: Prowlarr is preferred and AudioBookBay is the only approved native source. The checked-in `approved-audiobook-sources.json` supplies only AudioBookBay's reviewed public mirror/tracker metadata; it does not load upstream's mutable external registry. Unlisted current or future sources cannot run.

## Build and verification

```bash
docker build -f infrastructure/librarr/Dockerfile -t bigbrain-librarr:1208254-bb4 .
```

The third BB-103 patch records a sanitized `torrent_import_failed` activity event keyed by the torrent hash whenever final import fails. It does not alter placement: the no-overwrite patch remains authoritative, the source is retained and the existing destination stays untouched. This durable evidence lets BigBrain distinguish a real conflict/failure from asynchronous import/indexing without reading provider logs.

The fourth discovery-quality patch keeps title-only search unchanged. For an audiobook title plus author it issues at most three normalized, unique source queries: title, title plus author and—only for leading English articles—the article-free title plus author. Variants run within the existing bounded/cancellable source search, partial source success is retained, and author remains a post-result scoring signal. Prowlarr no longer appends a second audiobook term when the query already contains `audiobook`, `audio book` or `ljudbok`. Provider/source architecture and acquisition/import behavior are unchanged.

The Docker build applies all patches with `git apply --check`, verifies formatting, runs the complete upstream `internal/organize`, `internal/search` and `internal/download` packages plus the focused API regression and then builds the binary. The runtime stage remains non-root and contains neither Git nor the Go toolchain. Its entrypoint fails closed before Librarr starts if any required server-side credential, Audiobookshelf library ID or source allowlist is absent; it never prints values.

## Upgrade procedure

1. Review upstream import, path mapping, authentication and download-client changes.
2. Pin the new immutable commit and base-image digests.
3. Rebase the two small patches; do not weaken any no-overwrite or source-policy test. Inventory every new upstream source before adding its exact identifier to the allowlist.
4. Run the patched package tests, full BigBrain validation and an import dry run using temporary storage.
5. Re-run the security review before deployment.

Remove the import patch when a reviewed upstream release provides equivalent fail-closed file, directory, partial-destination and symlink-conflict behavior with tests for move, copy and hardlink modes. Remove the source patch only when upstream has an equivalent explicit, default-deny allowlist.
