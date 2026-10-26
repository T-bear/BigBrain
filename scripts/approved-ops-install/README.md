# Approved operations — one-time installation package

Status: **REVIEW CHECKPOINT — CORRECTION OFFLINE-VERIFIED, NOT INSTALLED**. Source baseline:
`b23e00d20a24bdeb459949ab335c8b9cd3599bd3`. Installer branch:
`ops/approved-wrapper-installation`; exact publication commit/tree are Git metadata.
The accepted [wrapper runbook](../../docs/operations/runbooks/approved-operations.md)
and [freezes](../../docs/operations/freezes.md) remain authoritative and unchanged.

This package prepares one administrator-run installation with two phases. Publication is not
installation or production authorization. No operator command was executed by Codex.
It copies the accepted wrapper, backup support and sudoers example byte-for-byte. It creates
no approvals, deployment packages or systemd units and never opens a production database.
API-only remains disabled. Finance, BB-158 and SchoolMeals are unchanged.

## Exact operator command — fresh installation only, after approval

**Do not run this command on the existing partial installation.** The administrator's earlier
attempt stopped during phase A; retain its files and protected staging. This corrected package
does not resume, adopt, overwrite or clean up that installation. Recovery requires inspection
of the protected result and installed bytes, followed by a separately authorized bounded action.

Run as **enigma**, in the local BigBrain terminal, with this reviewed branch checked out and
package files present at the stated repository location. Do not prefix this command with sudo.
Obtain this command and its pins from the independently reviewed GitHub checkpoint, not from
an untrusted edited local README. The OS Python interpreter/stdlib are trusted prerequisites.

```bash
/usr/bin/python3.13 -ISB -c 'import hashlib,pathlib; p=pathlib.Path("/home/enigma/BigBrain/apps/bigbrain/scripts/approved-ops-install"); b=(p/"install.py").read_bytes(); assert hashlib.sha256(b).hexdigest()=="07797634a5b1b4a380c070929720490a699db0092fb9f47dde838a850af8b12b", "Installer hash mismatch"; n={"__name__":"verified_installer"}; exec(compile(b,"verified_installer","exec"),n); f=n["launch"]; raise SystemExit(f(p,"e85676c6d7cd85881eb89b86b3e092c199abf3c6dc2c60294f123f1862f4b95f"))'
```

The command pins installer bytes **before executing even unprivileged installer code**.
The installer checks the manifest, the accepted source revision and three fixed upstream hashes;
all payload bytes are captured before sudo. `sudo -k` requests local authentication even if a
credential timestamp exists. Sudo reads the password from the controlling terminal; no `-S`,
password argument, file, log, askpass helper or chat input is used.

A small pinned bootstrap runs under OS Python `-ISB`. It checks the bounded in-memory bundle
again against the manifest digest, creates a unique `/root/bigbrain-wrapper-install-*` directory
0700 and writes only verified files 0600. Only then does it execute the staged installer.
It never executes a script, Git hook or Python import from the user's checkout as root.
No dependency installation, network fetch or Git command runs in the privileged phase.

## Artifact identities

- Accepted source revision: `b23e00d20a24bdeb459949ab335c8b9cd3599bd3`.
- Installer SHA-256: `07797634a5b1b4a380c070929720490a699db0092fb9f47dde838a850af8b12b`.
- Manifest SHA-256: `e85676c6d7cd85881eb89b86b3e092c199abf3c6dc2c60294f123f1862f4b95f`.
- `manifest.json` covers all four executable/configuration payload files: installer, wrapper,
  support and sudoers example. The manifest cannot hash itself; its digest is anchored in the
  command above. README and offline tests are review documentation, not installation payloads.
- Wrapper SHA-256: `1a512f21c1cfa1ca868aabe473874bad2f463341bfd14e1bde4ae8beee124055`.
- Support SHA-256: `2e4ffda2c26a3932f0722b145e4083dfd0dd71a343c5da1a03448f239016fdef`.
- Sudoers SHA-256: `1158cf3fec2b78abf559f53f20bedde807c317871970c4e2b789ce001423fac1`.

Changing any installer/payload bytes requires new manifest/pins and independent review.
Hashes establish identity, not semantic safety. Root, trusted OS dependencies and administrator
custody remain the trust boundary. Enigma's existing Docker access is root-equivalent: this
wrapper does not isolate enigma from the operating system or a malicious Docker user.

## Phase A — protected installation

Preflight requires absent wrapper/config/state/library/sudoers destinations, no leftover sudoers
staging file, existing protected backup root, Python 3.13 isolated/no-site mode, root-owned
executable paths, protected loaded stdlib/extensions/libraries and paths without writable or
symlink components/extended ACLs. Standard OS library aliases resolve to protected canonical
files. Unknown policy includes/nonlocal sudoers sources stop installation. Existing complete
sudoers must pass `visudo -c`. A generic passwordless `sudo true` must fail before proceeding.

An advisory flock on the existing `/usr/local/sbin` directory serializes this installer without
creating a global lock file. Other administrators must not change files/policy or run deployment
concurrently. Fresh directories are created exclusively; there is no recursive chmod/chown,
no adoption of an existing installation, no overwrite and no automatic retry.

Install only:

| Destination | Owner and mode |
| --- | --- |
| `/usr/local/sbin/bigbrain-approved-ops` | root:root 0755 |
| `/usr/local/libexec/bigbrain-approved-ops/backup_support.py` | root:root 0644 |
| Library directory; `/etc/bigbrain-approved-ops`, `packages`, `docker`; `/var/lib/bigbrain-approved-ops`, `claims`, `runs` | root:root 0700 |

The existing backup directory/sets are untouched; only backup-root metadata is checked.
Verify installed hashes/modes, `status = NO_WRAPPER_RUN`, three missing-approval denials,
unknown-operation and extra-argument denial. Status/testing creates the wrapper's private
empty lock file; it creates no claim, active marker or approval. Docker inspection compares
container identity/configuration/restart/start-time fingerprints, never printing raw inspection.
Mount records are compared as an order-independent multiset of their complete contents; no
mount field or duplicate is discarded. Other configuration arrays retain their ordering.
Phase A records separate `phaseAChecks.policyUnchanged` and `runtimeUnchanged` booleans in
its result, evaluating both checks before deciding `PhaseADrift`. Neither snapshot is published.

## Phase B — four literal NOPASSWD operations

Only after phase A passes, validate the unchanged sudoers example using `visudo -c -f`.
Create a new dot-prefixed 0440 temporary file inside `/etc/sudoers.d` (ignored by sudo), validate
it, then hard-link the complete bytes to the absent final name and remove only that temporary
link. No existing rule is overwritten. Verify final hash/mode, `visudo -c`, `sudo -l -U enigma`,
and that the local sudoers files differ only by the exact reviewed file.

The rule is only `/etc/sudoers.d/bigbrain-approved-ops`, root:root 0440, with four literal
commands and `NOPASSWD: NOSETENV`. No shell, interpreter, Docker, systemd or wildcard grants.
Existing password-authorized `(ALL : ALL) ALL` is not removed or interpreted as wrapper authority.

Behavioral tests run as enigma using `runuser` then `sudo -k -n COMMAND`: cached authentication
is ignored and password prompting disabled. Status must succeed, while the other three exact
commands must reach the wrapper and return its missing-approval BLOCKED JSON. Unknown operation,
extra argument, `-E`, `PYTHONPATH=...` and generic `/usr/bin/true` must fail at sudo, without wrapper
output. This distinguishes a sudo denial from a wrapper denial and from existing cached/admin
rights. It does not prove enigma lacks general root access (password sudo and Docker remain).

## Results, boundaries and recovery

Success prints JSON `status: PASS`, `phase: A+B-complete`, phaseA/B PASS and runtime identity
UNCHANGED. `systemdAcceptance: NOT RUN` and `productionOperations: NOT RUN` are explicit.
Private staging retains `progress.json`, `phase-a-pass.json` when applicable, and `result.json`.
It reports its local location to the operator; do not publish private host artifacts.

Any failure stops with BLOCKED and the last phase. A failure before staging may produce only a
fixed error. An interrupted process may leave progress without a final result: treat that as
UNKNOWN/BLOCKED, never retry blindly. Files, staging and partial policy remain for inspection;
no automatic deletion, database restore, image rollback or systemd action occurs.

An administrator recovering a phase-B failure first revokes **only this newly installed sudoers
entry**, validates `visudo -c` and confirms no wrapper operation is active. Preserve all backup
sets, claims and results; do not kill units or delete recovery inputs. Phase-A-only failures have
no activated sudoers rule. Remove/retire only proven new installer files after inspection, or
correct the concrete prerequisite under a separately reviewed continuation. Existing destinations
make subsequent runs fail closed. This is intentionally a one-time installer, not reconciliation.

Container fingerprints are compared before/after; databases are not read or checksummed.
No claim of production restore or database-content verification follows. Logs/policy/inspect
contents are captured privately in memory, not printed. Existing services are not started/stopped.

## Systemd acceptance — NOT RUN, separate gate

The supported production worker requires a real API+Web package and runtime/backup gates.
Manufacturing an approval for a fake package would not be a valid test of that contract.
Therefore this installer does not start a systemd unit or claim host deployment acceptance.
For a later explicitly authorized isolated host smoke test, the minimal fixture is OS
`/usr/bin/true` followed by `/usr/bin/false`, each in its own fresh transient
`bigbrain-wrapper-acceptance-<UUID>` oneshot with `--wait --collect`, 10-second start/stop limits,
`Restart=no`, `UMask=0077`, no mounts, no credentials, no Docker and no production dependencies.
Verify success versus failure and terminal state. That tests host systemd only; it does not
replace a separately reviewed worker/recovery acceptance test. No such command runs here.

## Offline verification

```sh
/usr/bin/python3.13 -ISB -m unittest discover -s tests/deployment -p test_approved_ops_install.py -q
PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests/deployment -p test_approved_operations.py -q
node scripts/verify-documentation.mjs
git diff --check
```

Installer suite: **26 PASS**. Synthetic two-phase success, A failure preventing B, B failure
retaining partial state, exclusive destinations, concurrency lock, malformed/tampered/resealed
sources, traversal/symlinks/hardlinks/ACLs, exact sudoers scope, bootstrap hash denial, sanitized
acceptance and cache-independent sudo commands. Privileged commands and host paths are mocked
in phase tests; actual filesystem tests use temporary directories. No root installation occurred.
The first full-flow offline run exposed a local `fcntl` import shadowing the lock implementation;
this was corrected before the original passing run. That original offline result did not
predict or validate the later administrator-run installation.

Accepted wrapper suite rerun unchanged: **40 PASS**. Documentation validator (286 Markdown files,
53 unique backlog IDs), `git diff --check`, sudoers syntax and exact accepted payload checks:
**PASS**. Scope and targeted secrets inspection cover only the four changed files; this is not
a full repository secrets audit. Host root/sudo acceptance is **NOT RUN** until
the administrator completes a separately reviewed installation/recovery. No installer test
authorizes production work.

## PhaseADrift follow-up — 2026-10-26

The administrator reported `A-installing / PhaseADrift / BLOCKED` from installer checkpoint
`be47b4732711653c72a723f3bcb054deb3825735`; systemd acceptance and production operations were
NOT RUN. Read-only inspection confirmed the installed wrapper's accepted hash and root:root
0755, protected library/config/state directories, and absence of the final and temporary
installer sudoers entries. Protected support bytes, directory contents and staging receipts
could not be read by the agent. `sudo -n -l` still required a password. These observations
do not prove that the complete partial installation or prior sudo policy is unchanged.

**Confirmed defect:** two read-only Docker inspections of the same 15 containers, two seconds
apart, differed only in mount-list ordering for three containers. The original fingerprint used
`sort_keys=True`, which canonicalizes dictionary keys but not list order. It therefore reported
runtime drift for semantically identical mount records. The new offline ordering regression
also fails against the original function and passes with the correction. An initial ad-hoc
historical-function comparison failed because its copied globals escaped the Docker mock and
performed a read-only inspection. The harness binding was corrected and the comparison repeated
entirely with mocked Docker. No privileged or mutating operation was executed. The committed
regression tests use the module globals with mocked command execution throughout.

**Historical cause remains uncertain:** the original `policy_snapshot() == policy and
runtime_snapshot() == runtime` short-circuited and emitted only `PhaseADrift`; before/after
snapshots were not retained. Mount ordering is a demonstrated explanation, not proof that the
administrator's specific failure involved runtime only. Policy drift, or both, cannot be
excluded from available evidence. The correction retains the fail-closed condition and makes
future completed checks distinguish policy-only, runtime-only and simultaneous drift using
booleans in the protected result. If a snapshot cannot be read, installation still stops.

Regression coverage includes mount order, all mount fields, additions/removals/duplicates,
container identity, image, restart count, start time, running state and ordered configuration
arrays. Volatile health-probe log changes remain outside the configuration fingerprint, as
before; this is not a new container-health acceptance test. Policy-only, runtime-only and both
changes prevent phase B without logging policy/configuration values. Accepted wrapper/support/
sudoers payloads, privilege boundaries and absent-destination checks are unchanged.

Remaining operational gate: local administrator inspection of retained protected evidence and
installed identities, then a separately reviewed recovery decision. No reinstall, sudoers
activation, deletion, database access or deployment was performed by this correction. Systemd
acceptance and completed host installation remain **NOT RUN / BLOCKED**; offline PASS does not
satisfy those host gates. Do not use the fresh-install command as a recovery command.
