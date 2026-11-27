# Synthetic journal mapping — review preparation

Local implementation for the six owner-approved DoD items, risk level 3. Independent
review is pending. No runtime registration, API dependency, SQLite access, provider,
model, trading, persistence or policy changes are introduced by this bridge.
The existing offline ledger auditor is not called by it.

`JournalForwardBridge.EvaluateSynthetic` accepts in-memory synthetic mirrors of
`FinanceShadowPrediction` and `FinanceShadowOutcome` in
`src/BigBrain.Api/Finance/FinanceShadowResearch.cs`. A source contract test pins every
constructor field, its order and type; the state enum is represented by its exact
name as a string. No production record is read. The existing `ForwardEvaluation`
validates the supplied manifest and boundary. The supplied manifest is not evidence
that the journal committed to a cohort before predictions were made.

| Journal field | Partial forward field |
| --- | --- |
| prediction `PredictionId`; outcome `OutcomeId`, `PredictionId` | Retained identities; join only on exact `PredictionId` |
| prediction `StrategyId`, `StrategyVersion`, `ParameterFingerprint` | `Strategy`, `StrategyVersion`, `ParametersHash`, unchanged |
| prediction `InstrumentId`, `Horizon`, `SessionDate` | `Instrument`, `Horizon`, `SourceSession` |
| prediction `SourceRevisionId`, `ObservationKnowledgeUtc` | `SourceRevision`, `SourceKnownAt` (claimed journal knowledge instant) |
| prediction `FeatureRevisionId`, `KnowledgeCutoffUtc`, `CreatedAtUtc` | `FeatureRevision`, `Cutoff`, `CreatedAt` |
| prediction `Signal` | `TargetLong` → true, `TargetFlat` → false; other values → null and diagnostic |
| outcome `TargetSession`, `SourceRevisionId`, `KnowledgeUtc`, `EvaluatedAtUtc` | `TargetSession`, `TargetRevision`, `TargetKnownAt`, `EvaluatedAt` |
| outcome `ReferenceClose`, `RealizedClose` | Same names and values |
| outcome `RealizedReturn`, `DirectionResult` | Retained and checked for consistency; never treated as equity |
| prediction `State`, `OperatingMode` | Require `Evaluated`, `RESEARCH`; invalidation is never overridden |
| prediction `Symbol`, `Provider`, `ReasonCodes` | Retained only; provider is not a verified source identity |

The journal read models cannot supply source/target checksums, acquisition instants
or verified event instants; feature checksum, complete input revisions and their
checksums/times, feature knowledge time, definition hash or engine version; a verified
source identity; an independently bound outcome version key; or a frozen journal
manifest/cohort commitment. Calendar boundaries are used only for chronology checks,
never as fabricated evidence events. Parsed timestamps cannot attest that the stored
strings had explicit zones. Retrospective status, revision ambiguity and complete
population provenance also cannot be established by these read models alone.

Probability, verified regime/knowledge time and net unitized equity/costs are absent.
Direction and close-to-close return cannot substitute for those fields. All metrics
remain `UNKNOWN`; every bridge result is `SYNTHETIC_ONLY / NOT_EVALUABLE`, with zero
genuine future sessions, no groups and no claimed evidence hash. No `ForwardEvidence`
is constructed. This is a useful partial mapping and diagnostic boundary, not an
admission path. No enrichment or relaxation is included.

Review should check ADR 0029 causality, exact source-model mapping, unchanged version
values, duplicate joins, invalidation, missing outcomes and full declared panel
coverage. Synthetic integration tests project the existing 120-session evaluator
fixture into the actual journal shape, prove the lost evidence stays missing, and
exercise chronology, version, duplicate and incomplete-cohort defects. The positive
evaluator tests remain separate and do not establish journal evaluability.

Verification commands:

```sh
dotnet format tests/BigBrain.Finance.OutcomeAudit.Tests/BigBrain.Finance.OutcomeAudit.Tests.csproj --no-restore
dotnet test tests/BigBrain.Finance.OutcomeAudit.Tests/BigBrain.Finance.OutcomeAudit.Tests.csproj --no-restore -p:UseSharedCompilation=false -m:1
```

Local verification, 2026-11-27: test suite **PASS**, 132 passed, zero failed or
skipped. Project-based `dotnet format` was **BLOCKED** by denied named-pipe access;
`dotnet format whitespace --folder --include` on the three affected C# files
**PASS**. Full semantic formatting remains unverified. Remote Git verification was
**BLOCKED** by local SSH configuration permissions. Local HEAD remains `6951df2`;
the bridge and tests are uncommitted. No independent review, publication or deployment
has been performed.

## Prospective commitment waterfall — scoped implementation, 2026-11-27

Owner-authorized DoD: exact identities and hashes; immutable fail-closed commitment;
complete >=120 named target sessions and causal knowledge boundary; synthetic positive
and negative verification; isolated internal architecture; no real integration; traceable
requirements and no fabricated rights. Risk remains **3**. This local change does not
resolve “no hypotheses” through a model run or grant: it implements only the specified
synthetic commitment prerequisite. Independent candidate review remains pending.

Design: `ProspectiveCommitment.EvaluateSynthetic` wraps the unchanged
`ForwardEvaluation`. `FrozenCommitment` snapshots serialized manifest and receipt bytes
into private arrays, returns fresh manifest copies, and computes SHA256 integrity hashes.
The existing contract hash (including every forward identity/definition/session) and the
exact serialized-byte hash are separate claims bound to a distinct receipt identity,
issuer, clock source, witnessed instant and validity boundary. None is authentication.
`ICommitmentReceiptVerifier` checks exact receipt bytes and claims under its configured
verification contract; its result does not establish independent trust. The default
`RejectCommitmentReceiptVerifier` always returns false. The original test verifier compares
isolated fixture bytes and claims. Stage 2 below adds offline signature authentication
relative to an explicitly supplied public key, without attesting clock provenance.

All logical checks and receipt verification precede evaluator acceptance. A witness must
be at or after the claimed freeze and no later than any prediction cutoff/creation and
`asOf`; expired receipts fail. The fixture's validity interval is supplied evidence,
not a newly invented real issuer freshness policy. Missing, duplicate or mismatched
receipt identities fail; this is one in-memory commitment with one receipt, not a durable
cross-process replay registry. Retrospective flags are never cleared, and backdating
claims cannot bypass the default verifier. Independent custody, clock trust, signed
claims and cutoff witnesses cannot be established by application-supplied strings.

| Requirement / authority | Implementation and verification trace | Fact versus external constraint |
| --- | --- | --- |
| Exact identity, immutable bytes and SHA256 (DoD; ADR 0023) | `FrozenCommitment`; `SnapshotDoesNotExposeMutableManifestOrReceiptBytes`; wrong-manifest, wrong-bytes-hash, tampered, altered-key cases | Byte integrity and copying are testable; a hash authenticates neither author nor time |
| Witnessed prospective boundary (DoD; ADR 0029) | `ICommitmentReceiptVerifier`, default rejection; forged, default-verifier, wrong-clock, wrong-issuer, wrong-receipt-id, missing cases | Independent issuer/clock authentication remains external and unavailable |
| No retrospective promotion (ADR 0029) | Freeze <= witness <= cutoff/creation; future-commit, stale, retroactive and retrospective-row cases, even with agreeing synthetic verifier for bad chronology | Fixture chronology only; no claim that historical rows were actually witnessed |
| Full predeclared cohort (DoD) | Unchanged `ForwardEvaluation`: >=120 named consecutive eligible sessions, exact complete series/session panel; positive and incomplete-cohort cases | Synthetic future-relative-to-commit sequence only; no actual future observation |
| Causal lineage (ADR 0023/0029) | Unchanged revision/checksum/source/feature/knowledge/cutoff gates; late-feature and evidence-tampered cases plus existing evaluator suite | Payload truth and actual acquisition/cutoff clocks remain unproven |
| No partial acceptance (DoD) | Failures return NOT_EVALUABLE, UNKNOWN metrics, zero sessions and no groups; all negative cases assert these | Optional unavailable metrics retain existing UNKNOWN semantics |
| Architecture and truthful reporting (DoD) | Separate internal tool component; no public/production contract edits; every case asserts SYNTHETIC_ONLY, independent NOT_VERIFIED and real admission false | Positive status is only SYNTHETIC_FIXTURE_PROVEN; no real prospective admission |

### Open external gate register

These are existing prerequisites, not newly authorized features or closed gaps:

| External gate | Status / evidence needed |
| --- | --- |
| Issuer trusted timestamp or independently signed external receipt | OPEN: verify issuer, receipt identity, exact bound bytes/hash, clock provenance, validity and independent custody; default verifier rejects |
| Provider entitlement and retention | OPEN: current authoritative permission and artifact lifecycle evidence; this code grants none |
| Source payload verification | OPEN: actual source revision/checksum, acquisition and knowledge-time evidence, complete feature lineage |
| Verified host OS isolation G3a/G3b | OPEN: effective host isolation and separately authorized runtime verification; no runtime executed here |
| Equity, regime and probability capture | OPEN: independently captured, versioned values and regime knowledge time; never infer from direction/returns |
| Future sessions and cutoff witnesses | OPEN: actual complete precommitted cohort and independent prediction/cutoff witnesses; synthetic dates do not count as real observations |

Local test/format results for this delta are recorded below after execution. The earlier
bridge verification paragraph above is historical and does not identify this baseline.
No provider/model/data access, grant, migration, trading, deployment or merge
is authorized. A review commit and draft PR do not constitute acceptance.

Verification for this delta, 2026-11-27:

- **PASS:** compilation and complete OutcomeAudit unit suite, 187 passed, zero failed/skipped
  (`dotnet test` command above, after package restore with `--ignore-failed-sources -p:NuGetAudit=false`).
- **PASS:** `dotnet format whitespace --folder --include` both new C# files with
  `--verify-no-changes`; `git diff --check`; no changes to production files or `ForwardEvaluation`.
- **BLOCKED:** project-based `dotnet format --no-restore --verify-no-changes` for the new
  files: named-pipe connection denied. Semantic formatting is not verified.
- **BLOCKED:** `node scripts/verify-documentation.mjs`: child-process `spawnSync git EPERM`.
- **PASS (local only):** initially clean required branch and HEAD/local `origin/main`
  `02122a088a38a478e8acffdb94f1b60e92349854`, baseline tree
  `6c822ecf2ef62aa4d428713723dd24a82bf78df1`.
- **BLOCKED:** remote main verification: SSH configuration permission error. No remote
  candidate SHA exists for these uncommitted changes. Independent risk-3 review is pending.


## Signed receipt waterfall stage 2 — offline authenticity, 2026-11-27

Risk **3**, owner-authorized offline implementation only, based on main
`139171d93e039adcbdb6dc791ba1679ddd7f7cfb` after PR #122. Independent review is pending;
changes remain uncommitted. No runtime registration, network, file I/O, persistence,
new dependency, credential enrollment, provider/model or trading capability is added.

`SignedCommitmentReceiptVerifier` implements the existing interface using .NET ECDSA
P-256 / SHA256 and a fixed 64-byte IEEE P1363 signature. Configuration requires an
explicit 65-byte SEC1 uncompressed P-256 public point (`04 || X[32] || Y[32]`), exact
issuer, key ID and clock-source identity. It copies the public point and validates it
on construction; invalid configuration throws. It accepts no private key and generates
no trust root. Only tests generate ephemeral synthetic signing keys in memory.

The receipt is exactly `canonical payload || signature[64]`. The signed payload is:

1. ASCII `BB073/receipt/v1/ECDSA-P256-SHA256/P1363` followed by one zero byte.
2. Key ID, receipt ID, issuer, clock source, in that order. Each is a two-byte unsigned
   big-endian byte length followed by 1–256 visible ASCII bytes (`0x21`–`0x7e`).
   There is no whitespace, Unicode normalization, replacement encoding or case folding.
3. Manifest hash and manifest-byte hash, in that order, each 32 raw bytes. Input claims
   must use exactly `sha256:` plus 64 lowercase hexadecimal characters.
4. Witnessed instant and valid-until instant, in that order, each an eight-byte signed
   big-endian .NET UTC tick count (100 ns since 0001-01-01). Nonzero UTC values are
   mandatory; valid-until must be at least witnessed-at.

Verification reconstructs the canonical payload from all claims and configured key ID,
requires byte equality and exact total length, then verifies the signature. Receipts
above 2048 bytes fail before payload allocation; identity/hash lengths are bounded before
encoding. Malformed signatures, substitutions, alternate layouts and trailing bytes fail
closed. The supplied UTC `asOf` must fall inclusively within the signed interval. Neither
`asOf` nor the claimed witnessed instant is an independently trusted clock assertion.
SHA256 integrity hashes alone are never accepted as authentication.

A valid signature establishes authenticity only relative to the explicitly supplied key.
It does **not** establish the issuer's independent identity, custody, clock accuracy,
actual signing time or truth of the signed claims. Real issuer trust, independently
verified clock/timestamp provenance and key custody remain **OPEN** external gates.
`EvaluateSynthetic` still unconditionally reports independent boundary `NOT_VERIFIED`
and `RealProspectiveAdmission=false`; default rejection is unchanged. Duplicate or
mismatched receipt identities fail at the existing evaluator boundary. Offline signature
verification is deliberately repeatable and has no durable/cross-call replay registry.

`SignedCommitmentReceiptVerifierTests` pins the binary layout independently and tests
positive authentication, all seven altered claims, substituted key/key ID/issuer/clock,
malformed signatures, a digest masquerading as a signature, signed noncanonical payloads,
invalid encodings and sizes, UTC/expiry boundaries, configuration validation, copied public
key bytes, duplicate handling and the unchanged synthetic-only admission boundary.

Stage 2 verification, 2026-11-27:

- **PASS:** package restore (`--ignore-failed-sources -p:NuGetAudit=false`); compilation
  of the full affected test project with analyzers. Initial missing restore assets and
  CA1870 were resolved before the successful build.
- **BLOCKED:** requested `dotnet test ... --no-restore -p:UseSharedCompilation=false -m:1`
  compiled successfully, but VSTest could not open its communication socket (permission
  denied); no tests ran through that runner.
- **PASS:** complete affected suite via the built xUnit v3 executable:
  `dotnet tests/BigBrain.Finance.OutcomeAudit.Tests/bin/Debug/net10.0/BigBrain.Finance.OutcomeAudit.Tests.dll -noLogo`:
  **230 passed, zero errors/failed/skipped/not-run**, including 43 new signed-receipt cases.
- **PASS:** whitespace formatted and then verified on all three affected C# files using
  `dotnet format whitespace --folder --include ... --verify-no-changes`; `git diff --check`.
- **BLOCKED:** project-based `dotnet format --no-restore --verify-no-changes --include ...`:
  denied named-pipe connection. Full semantic formatting remains unverified.
- **BLOCKED:** documentation checker: `spawnSync git EPERM`; remote `git ls-remote`
  verification: SSH configuration permission error. No permissions were changed.
- **PASS (local only):** initially clean checkpoint `checkpoint/finance-signed-receipt-20261127`,
  HEAD/local `origin/main` matching the owner-supplied verified main SHA above; baseline
  tree `6cf02e5ad0c9b794e1a30ad8b734ffbf16f49332`. There is no published candidate SHA/tree.
  Four changed files only: verifier, new tests, interface comments and this document.
  No commit, push, merge or deployment; independent risk-3 review remains **PENDING**.

## Trust qualification waterfall stage 3 — STOP without code, 2026-11-27

Owner-authorized risk-3 assessment at accepted baseline
`4b4c9f66895980a52bb3a90d6a8c1362db03d82c` (PR #123 per owner).
Local HEAD and local `origin/main` match; the initially clean branch is
`checkpoint/finance-trust-qualification-20261127`, baseline tree
`edba4954805e2eccc403ca9728abe53c6833dee1`. Remote verification was blocked
by SSH configuration permissions. Prior stage review/publication statements above
are historical; this assessment introduces no new accepted candidate.

### Requirements and design decision before implementation

The supplied DoD requires exact source evidence, a protected invariant, negative
tests if implemented, regression/format/diff checks, no real admission claims,
explicit external blockers and uncommitted independent-review material.
ADR [0023](../../docs/adr/0023-finance-immutable-feature-revisions-and-causal-lineage.md)
requires immutable causal lineage; ADR
[0029](../../docs/adr/0029-finance-prospective-knowledge-time-evidence.md)
requires fail-closed clock integrity and forbids retrospective promotion.
The existing [custody contract](../../docs/reports/features/finance/finance-bb-087-prospective-shadow-foundation-20260815.md#trust-and-custody-contract--design-only)
is explicitly design-only. Its later OS qualification design requires a trust
anchor outside auditor control and says no key or receipt issuer is added.
Neither document supplies a real authority or independently verifiable timestamp.

Protected invariant: every `ProspectiveCommitment.EvaluateSynthetic` result retains
`IndependentAttestedBoundary=NOT_VERIFIED` and `RealProspectiveAdmission=false`,
including a valid signature under a caller-supplied key. The default verifier rejects.
No code delta is warranted by the available evidence. Qualification ends at this
documented preflight; no new trust API, registry, timestamp profile or policy is defined.

| Gap | Exact existing evidence | Missing authoritative input / disposition |
| --- | --- | --- |
| Receipt authenticity and key binding | `SignedCommitmentReceiptVerifier` reconstructs the canonical payload including key ID, issuer, clock and both manifest hashes; .NET verifies P-256 signatures with the explicit copied public point | Already implemented relative to supplied key only. A self-computed fingerprint would identify bytes, not independently authorize them |
| Independently trusted issuer/key | Constructor accepts caller-selected public point and identities; no authority pin is supplied by this task or the accepted contract | Independently authenticated issuer-to-key binding and owner-approved authority, with an independently protected pin and provenance; OPEN |
| Key custody | Custody design explicitly treats unknown custody as STOP; no real custody attestation is supplied | Independently verifiable evidence identifying the key, custodian and enforced protection boundary over the relevant interval; custody text or signatures by the same untrusted key are insufficient; OPEN |
| Key validity, rotation and revocation | Receipt `WitnessedAt`/`ValidUntil` are signed claims, not key lifecycle records; no accepted lifecycle evidence format or authority is specified | Authenticated lifecycle evidence bound to the pinned authority/key, with accepted interval, effective revocation, rotation/conflict and freshness semantics; no locally invented lifetime or revoked flag can close this; OPEN |
| Independent timestamp | Signed clock-source string and UTC ticks; caller supplies `asOf`; chronology checks only | Actual independently signed timestamp evidence binding the exact commitment, independently pinned timestamp authority and accepted verification/freshness semantics; local time and signer assertions do not establish when signing occurred; OPEN |
| Genuine prospective admission | `ProspectiveCommitment` hardcodes independent NOT_VERIFIED and real admission false; bridge cannot construct complete `ForwardEvidence` | External gates above plus the existing complete source/cohort/cutoff evidence; no status upgrade or real observation is justified; OPEN |

Rejected alternative: a local lifecycle registry with caller-supplied trust/custody
flags, or a new fingerprint-only qualification API. Neither could authenticate its
own inputs. It would add maintenance and review cost and risk being mistaken for
accepted authority without improving the existing unconditional real-admission
rejection. An external timestamp implementation also lacks supplied signed material
and accepted authority semantics; choosing a provider or format here would invent
scope. No external witness was contacted and no real keys were created.

Existing negative coverage is in `ProspectiveCommitmentTests` (missing, forged,
duplicate/ambiguous receipt identity and stale claims) and
`SignedCommitmentReceiptVerifierTests` (substituted key/identity, forged signatures,
noncanonical payloads and exact expiry boundaries). The signed positive fixture
also asserts no real admission. **Revoked-key handling is not implemented or tested**:
there is no authoritative lifecycle input to verify. This is an external blocker,
not a successful revocation check. No new tests are claimed for this no-code decision.

Stage 3 verification results are recorded below. No runtime, API, provider, database,
security policy, grants or production keys change. BB-073 remains open; independent
risk-3 review of this uncommitted assessment is pending. No commit, PR, merge or deployment.

- **PASS:** offline restore from the existing local NuGet cache, after the initial
  `--no-restore` attempt failed with NETSDK1004 (missing assets).
- **PASS:** full OutcomeAudit regression suite with the `dotnet test` command above:
  **230 passed, zero failed/skipped**; affected projects compiled successfully.
- **PASS:** baseline `node scripts/verify-documentation.mjs` (297 Markdown files,
  57 unique backlog IDs); `git diff --check`; reviewed diff contains this Markdown file only.
- **PASS (independent host check):** final `node scripts/verify-documentation.mjs`
  verified 297 Markdown files and 57 unique backlog IDs after the completed delta;
  `git diff --check` PASS. Earlier sandbox `spawnSync git EPERM` is resolved by
  this independent host run.
- **NOT APPLICABLE:** C# formatting and new lifecycle tests, because no C# changes
  or lifecycle implementation were made.
- **PASS (independent host check):** remote GitHub `main` remains
  `4b4c9f66895980a52bb3a90d6a8c1362db03d82c`. Earlier sandbox SSH error
  does not affect this verified host result.
- **PENDING:** independent risk-3 review/publication; all external trust gates above
  remain OPEN. Synthetic tests do not close them.
