# BB-102 — Librarr Provider Security Gate

## Metadata

- Date: 2026-08-23
- Baseline: `8bc49198ef06a332d825c8498b0144e98a8bb660`
- Scope: mandatory upstream, compatibility and security gate for the owner-selected Librarr provider
- Sanitization notice: no keys, private addresses, tracker credentials, magnet links, raw runtime payloads or private library data are published.

## Status

**SECURITY GATE REMEDIATED; RUNTIME COMMISSIONING BLOCKED.** The owner approved option 2, a minimal BigBrain-controlled patch/image. The no-overwrite invariant is automatically verified; deployment has not started because required local credentials are absent.

## Scope and decision

The owner approved [JeremiahM37/librarr](https://github.com/JeremiahM37/librarr) as the first candidate implementation behind BigBrain's existing `IAudiobookAcquisitionProvider`. This report records the mandatory upstream and security gate. No provider implementation, Compose service, secret, runtime setting or acquisition was created.

The reviewed stable release was [v1.2.0](https://github.com/JeremiahM37/librarr/releases/tag/v1.2.0), commit `df1fc5a6951f693a492a38270c261cb863308fc4`. Upstream remained active after that tag, including a version bump and import-mode work on `main`, but those later changes did not remove destination overwrite behavior.

## Evidence

- A non-root multi-stage container image is defined upstream; Docker socket and privileged mode are not required.
- Librarr has API-key authentication and integrations for Prowlarr, qBittorrent and Audiobookshelf scanning.
- qBittorrent supports a dedicated audiobook category and separate incoming path.
- Torrent path mapping constrains reported paths beneath configured roots; later upstream code adds stronger destination containment and symlink-prefix handling.
- Runtime states exist for queued/searching/downloading/importing/completed/error-style workflows.

## Blocking finding: overwrite semantics

Stable `v1.2.0` derives the final audiobook destination as `{AUDIOBOOK_DIR}/{sanitized author}/{sanitized title}`. Its file placement first uses `os.Rename`; the fallback writes the destination with truncation. Directory imports walk into an already-created destination tree. There is no fail-closed preflight when the destination or a contained file already exists.

The current upstream `main` adds move/hardlink/copy modes and better containment, but explicitly preserves replacement behavior for existing destinations. This violates BB-101/102's server-controlled import policy: an existing audiobook must cause conflict/needs-attention, never replacement. Since Librarr itself owns the move into the mounted final library, BigBrain cannot enforce this safely from the provider API boundary.

Deployment therefore stopped before pulling a production image, mounting `/audiobooks`, creating a container or installing credentials.

## Changes

No application, Compose or runtime code was changed. Repository changes are limited to this sanitized decision record plus status, backlog, module and report-catalog references. The deployed provider remains `None / NotConfigured`.

## Security

- With default configuration Librarr loads an external source registry. BigBrain requires Prowlarr to remain the canonical indexer hub, so a future deployment must supply a reviewed empty/local registry rather than silently querying additional sources.
- The audiobook search response preserves individual releases and source details but normally lacks authoritative language, narrator and edition fields. BigBrain may retain deterministic release-title hints only as `Probable`; unknown stays `und` and must remain selectable rather than auto-acquired.
- Librarr exposes torrent/job-specific removal endpoints rather than a provider-neutral audiobook cancel contract. BigBrain must advertise `CanCancel=false` until exact partial-file and client semantics are verified.
- A valid Librarr API key receives administrative API authority. Any future instance must remain internal-only, use a dedicated revocable key, and never expose the key or raw magnet/download URLs to Web.
- Search results include sensitive provider URLs. A future adapter needs a bounded server-side candidate cache so Web receives only opaque BigBrain candidate IDs.

Detta är en sanerad GitHub-version. No credential, private endpoint, tracker identity, source URL, magnet URI or production library item is included.

## Owner decision and continuation

On 2026-08-23 the owner explicitly approved a narrowly scoped BigBrain-maintained image patch whose only purpose is fail-closed import conflict handling. Upstream is now pinned to immutable commit `1208254c20b31fbf217558c0fb987f779fed1cf8`. The patch is independently reviewable at `infrastructure/librarr/patches/0001-audiobook-import-no-overwrite.patch`; build and maintenance instructions are in `infrastructure/librarr/README.md`.

The patched organizer atomically reserves a new final book directory, uses exclusive destination-file creation, refuses existing or partially populated book destinations, rejects unsafe author/title components and source symlinks, and preserves source data when placement fails. Move deletes the source only after all destination writes and syncs succeed; copy and hardlink never replace. The image build executes all 59 organizer tests, including 11 new patch regression tests.

The focused security re-review found no Docker socket, privileged mode, host port or arbitrary Web-controlled path. Prowlarr-only discovery is enforced with a local empty registry. BigBrain retains opaque candidate identifiers and server-only provider payloads. Cancellation remains disabled because upstream deletion semantics are not proven safe.

BigBrain's provider adapter and Compose service are implemented and locally tested. Runtime deployment remains blocked until the appliance owner installs the three required secret values named in the runbook; as of this report all three are absent. No secret value was read or published, no Librarr container was started, and no download was requested.

## Automated verification

- Patched Librarr image build: passed; all 59 organizer tests passed, including 11 patch regressions.
- Focused BigBrain audiobook/provider tests: 21 passed.
- Complete API suite: 528 passed.
- Complete Web suite: 126 passed.
- Sentinel regression suite: 32 passed.
- Release solution build and Vite production build: passed with zero .NET warnings/errors.
- Compose render, documentation verifier (183 Markdown files / 89 unique backlog IDs) and `git diff --check`: passed.
- A redacted gitleaks scan found only the repository's pre-existing documented Finance threat-model example; no BB-102 secret was found.
- Missing-credential entrypoint test: image refused startup with exit code 78 and did not print variable values.

CI, deployment, downstream health, real provider search and browser QA are not claimed: they require publication and then the three local runtime credentials. The first real download remains explicitly prohibited until the owner selects a candidate.

## Remaining work

Install the dedicated Librarr API key and the existing qBittorrent Web API username/password in the ignored appliance `.env`, then follow `docs/operations/runbooks/librarr-provider.md`. Commission only Librarr/API/Web, verify downstream health and perform a sanitized real search. The first live download still requires explicit owner selection in BigBrain.

## Preserved state

- BB-100 Audiobookshelf commissioning: unchanged.
- BB-101 provider: `None / NotConfigured`.
- Existing Prowlarr, qBittorrent and Audiobookshelf services: unchanged.
- Finance: no code or runtime change.
- Sentinel: no code or runtime change.
- BB-099: **TECHNICALLY COMPLETE / OWNER UX REVIEW PENDING**.

## Resumption

Resume BB-102 commissioning after the required secret values are installed locally outside Git/chat. Preserve the provider-neutral BB-101 boundary and do not initiate the first acquisition without the owner's explicit release selection.
