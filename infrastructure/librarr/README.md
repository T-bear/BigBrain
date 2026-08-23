# BigBrain-maintained Librarr image

BB-102 builds Librarr from immutable upstream commit `1208254c20b31fbf217558c0fb987f779fed1cf8` and applies one reviewable patch: `patches/0001-audiobook-import-no-overwrite.patch`.

## Why the patch exists

Upstream audiobook organization replaces or merges existing destinations. BigBrain requires fail-closed import: an existing or partially populated `{author}/{title}` destination is a conflict and must remain unchanged.

The patch is intentionally limited to `internal/organize/pipeline.go` plus isolated regression tests. It atomically reserves the final book directory, uses exclusive file creation, never replaces hardlinks, keeps a move source until the complete destination tree has been placed, rejects unsafe destination components and refuses symlink escape through the author destination.

The build also installs `prowlarr-only-sources.json` as runtime configuration. It prevents Librarr from loading its external source registry; Prowlarr remains the only audiobook indexer hub.

## Build and verification

```bash
docker build -f infrastructure/librarr/Dockerfile -t bigbrain-librarr:1208254-bb1 .
```

The Docker build applies the patch with `git apply --check`, verifies formatting, runs the complete upstream `internal/organize` package tests and then builds the binary. The runtime stage remains non-root and contains neither Git nor the Go toolchain. Its entrypoint fails closed before Librarr starts if any required server-side credential or the Audiobookshelf library ID is absent; it never prints values.

## Upgrade procedure

1. Review upstream import, path mapping, authentication and download-client changes.
2. Pin the new immutable commit and base-image digests.
3. Rebase the small patch; do not weaken any no-overwrite test.
4. Run the patched package tests, full BigBrain validation and an import dry run using temporary storage.
5. Re-run the security review before deployment.

Remove this patch when a reviewed upstream release provides equivalent fail-closed file, directory, partial-destination and symlink-conflict behavior with tests for move, copy and hardlink modes.
