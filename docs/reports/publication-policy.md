# Report Publication Policy

Each local report is classified as one of: sanitized full report, safe summary,
catalog metadata only, local only or superseded. Publication is deliberate, not a
recursive copy of the local report tree.

Sanitized reports preserve decisions, verification level, commands and limitations.
They omit secrets, authorization material, raw external IDs and hashes, private URLs,
internal addresses, raw logs and sensitive filesystem or media details. The committed
report is immutable evidence; corrections use a new report or an explicit amendment.

The repository catalog is the shared discovery surface. Local reports remain the
full internal evidence source and may have stricter access controls.

## Review Checkpoint publication

Within an authorized bounded checkpoint, coherent code/tests and sanitized report/handoff may be
committed and pushed even when acceptance fails or work is blocked. Label **REVIEW CHECKPOINT**;
use **MERGE CANDIDATE** only after full verification and completed acceptance criteria. Neither label
is acceptance. Follow [AGENTS](../../AGENTS.md#permanent-checkpoint-branch-workflow), then STOP.

The report must enable independent GitHub review without terminal copying. Record current outcomes,
exact commands/results, missing verification, known failures, provenance, invariants and next work.
Identify each review publication unambiguously; its exact SHA/tree and changed-file list come from
GitHub/Git commit metadata. Later review commits append evidence without rewriting prior SHA history.
Dated failures remain failures; amendments must not silently replace scientific/audit records.
Main remains the accepted source of truth; the active branch records unaccepted ongoing work.
Only an independently reviewed and owner-approved exact Merge Candidate SHA can enter the merge path.
