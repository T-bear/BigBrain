# BB-158 bounded Audiobookshelf probe

Review infrastructure, **not installed**. Use the existing `FFPROBE_PATH` hook in ABS 2.36.0;
no replacement server, scanner, downloader or database. The reviewed image is
`sha256:180acad33d69c99ed208676465d8edcb268fa46967735579a7810859885b1a8e` (FFmpeg8.0.1,
Node20.20.2, BusyBox1.37.0, existing tini PID1). Verify the same binaries before installation.
The Docker source path skips ABS's generic BinaryManager downloader/version replacement;
do not set `SKIP_BINARIES_CHECK` or falsify the native version to make other images work.

## Contract

`bounded-ffprobe.cjs` is an executable Node hook, not a shell command. It launches only fixed
`/usr/bin/timeout -s KILL 60 /usr/bin/ffprobe` with the received argument array, preserving stdin
and ABS's JSON/INI/version output modes. No credentials or inherited environment reach the native
process. Child environment contains only fixed PATH and locale. Limits are compiled into this
reviewed file; CLI input and environment cannot increase them.

- 60-second wall-clock budget per invocation, including startup. JS deadline and an independent
  BusyBox watchdog apply; the latter bounds native lifetime even on supervisor SIGKILL.
- stdout ≤8MiB, stderr ≤256KiB. Count bytes before retention; stderr is counted/discarded.
  Retain stdout only up to the limit and forward it only after a successful native close.
- Timeout, byte overflow, cancellation, missing executable or nonzero exit kills the dedicated
  process group, discards partial output and returns small sanitized JSON error + nonzero exit.
  Wait for close/reap before responding; existing tini reaps adopted watchdog/descendant processes.
  ABS's JSON prober understands `error.string`; fluent-ffmpeg receives a nonzero controlled error.
- Both SIGTERM and SIGINT use cleanup. The fixed ffprobe is not a hostile process runner:
  process-group cleanup does not claim containment of arbitrary code that deliberately escapes
  a session. Uninterruptible kernel I/O and a broken host kernel cannot be bounded by userspace.
- Successful metadata remains unchanged. No probe results are logged by the wrapper. This is
  resource containment, not a decoder/malware validator or an independent filesystem sandbox.

## Resource sizing and permissions

`compose.bounded-probe.yaml` affects ABS only: 2 CPUs, 1GiB RAM, total memory+swap1GiB (zero
additional swap), 128 tasks, no capabilities and no-new-privileges. Existing root UID is retained
for compatibility with root-owned config/metadata; no chown, migration or permission expansion.
The wrapper is mounted read-only and all existing mounts/network/image/entrypoint remain intact.

Measured host: 4 CPUs/7.68GiB RAM; observed ABS peak489MiB/81 tasks, idle~73MiB. RAM limit gives
about2.1× observed peak, task limit about1.6× peak; CPU cap reserves at least half host CPU capacity
from ABS consumption. These are bounded operational ceilings, not measured worst-case demand.
The synthetic real-codec/consumer tests pass under them. Full-library scan/transcoding peak load
is not certified; do not silently increase ceilings if a later workload fails. The 60-second/8MiB
probe envelope deliberately rejects extreme workloads and needs review to change. An aggregate
OOM remains possible under exceptional concurrency; this is not a promise of unlimited service
availability. The tested timeout/output failures return errors without killing the consumer.

## Isolated verification (no production volumes or network)

From repository root, using the existing local image (no pull required):

```bash
docker run --rm --network none --read-only --cpus 2 --memory 1g --memory-swap 1g \
  --pids-limit 128 --cap-drop ALL --security-opt no-new-privileges:true \
  --tmpfs /tmp:rw,exec,nosuid,nodev,size=64m \
  --mount type=bind,src="$PWD/infrastructure/audiobookshelf",dst=/review,readonly \
  sha256:180acad33d69c99ed208676465d8edcb268fa46967735579a7810859885b1a8e \
  node --test /review/bounded-ffprobe.test.cjs
```

The executable tmpfs is **test-only** for synthetic fake-probe drivers; it is not added to the
production override. Tests generate two seconds of synthetic MP3/M4A locally, use the image's
real ffprobe and ABS JSON/fluent consumers, and exercise timeout, both production byte ceilings,
SIGTERM, supervisor SIGKILL, group descendants and error recovery. No library/server is started.

## Future installation sequence — exact-SHA approval required

1. Review/approve this code SHA first. Reverify production identity, current use/import activity,
   recoverable consistent ABS config/metadata and relevant Media/Librarr state. Preserve old image
   IDs, full private Compose inputs and protected backup checksums. Do not copy active SQLite
   main files without a consistency method; no backup/restore is executed by these files.
2. Install the exact approved wrapper to a private root-owned host location, root-owned parents,
   file mode0555, no symlink substitution. Compare SHA256 to the approved Git blob bytes. Set
   `BB158_FFPROBE_WRAPPER` to that existing absolute file in private Compose interpolation.
   The long bind syntax refuses to create a missing source. Do not point it at a mutable checkout.
3. Combine the existing project Compose files with this override. Compare the effective ABS
   service privately: only limits/capabilities/security option/hook/one read-only mount may differ.
   No secrets in rendered config/logs. Preserve original ABS image and all existing volume sources.
4. Under approved downtime, recreate only ABS with `up -d --no-deps --no-build audiobookshelf`.
   Recheck cgroup limits, zero CapEff/NoNewPrivs1, hook checksum, native paths/version, health,
   unchanged library count and metadata. A running production probe was not tested here.
5. Only after these gates continue approved Librarr → API(opt-in false) → Web deployment, then
   activation gates and at most one explicitly permitted HTTPS source with truthful DOI relation.
   Verify exact indexed identity and retained outcome; iPhone/PWA and actual playback remain manual.

On failure, stop rollout. Revert only the added override using preserved original config/image,
recreating ABS only if safe and approved. Never restore an older database or delete uncertain
imports to retry. No schema migration is introduced by this hook, but normal ABS startup can
write state; preserve all new state and stop if rollback compatibility is uncertain.
