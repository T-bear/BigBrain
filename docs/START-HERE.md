# Start here — BigBrain continuity

BigBrain is a modular home-server control plane, with first-party React/TypeScript
views, an ASP.NET Core API, external-service adapters and a separate Sentinel
boundary. The owner decides product priority and UX/behavior acceptance; ChatGPT
reviews architecture independently; Codex implements and maintains evidence.

## Mandatory read order and authority

1. Read [AGENTS.md](../AGENTS.md) fully: working rules, safety, documentation and publication.
2. Fetch GitHub, inspect `origin/main`, HEAD and the working tree, then read the
   [canonical recovery note](operations/codex-recovery.md). GitHub main is the
   source of truth for accepted work; the active checkpoint branch is source of truth
   for published, ongoing unaccepted work. Preserve unpublished work;
   compare it with the note before resuming. If an expected baseline moved, assess
   the delta before using an old plan. Never reset another person's work.
3. Read [ARCHITECTURE](../ARCHITECTURE.md) and applicable [ADRs](indexes/adr.md):
   accepted requirements and reasons, with current, historical and proposed scope distinguished.
4. Read [STATUS](STATUS.md) for current documented reality and dated verification,
   then [BACKLOG](BACKLOG.md) for incomplete work and Definition of Done.
5. Read [ROADMAP](../ROADMAP.md) for owner-owned long-term direction and links to
   domain plans. It does not replace backlog priority or current runtime evidence.
6. Read [report conventions](reports/README.md), [report catalog](reports/REPORT-CATALOG.md),
   relevant reports, module contracts, runbooks, [TESTING](../TESTING.md), and affected
   code/tests. Follow the [documentation index](indexes/documentation.md) for discovery.

Authority is question-specific: accepted ADRs constrain permitted architecture;
code establishes implemented behavior; dated evidence establishes what was actually
tested or deployed. Code does not retroactively accept a proposal, and an ADR does
not prove deployment. Apply the existing documentation-index hierarchy within the
same scope. Report contradictions explicitly; stop for architectural conflicts.
Owner UX approval requires explicit owner evidence and cannot be inferred from CI.

## Continuity contract for significant work

Record these fields in the relevant sprint/report/status entry, linking rather than
copying whole documents. This entry point must never become another STATUS or BACKLOG.

| Field | Required meaning |
| --- | --- |
| WHY | Concrete problem, intended outcome and reason to do it now |
| BASELINE | Exact published source SHA and any preserved unpublished work |
| SCOPE | Included work, exclusions and safety boundaries |
| STATE | Planned, in progress, implemented, automatically verified, CI verified, deployed, runtime verified, owner verified, blocked, known limited or superseded — independently evidenced |
| EVIDENCE | Exact commands/results, CI commit/run, dated runtime scope and explicit owner/manual evidence |
| DECISIONS | Accepted decisions, owner/architect authority, rationale and applicable ADRs |
| REMAINING | Exact unfinished work, limitations and missing verification/publication |
| NEXT | Smallest safe next action and any real prerequisite |

## NO UNDOCUMENTED SIGNIFICANT WORK

Significant work materially changes architecture, status, backlog, roadmap,
deployment/runtime, security, operational recovery, or important technical findings
and lessons. It must not exist only in chat or terminal history. Publish sanitized
durable knowledge with the relevant code/tests; no credentials, private identities,
private addresses or raw sensitive logs belong in reports.

Follow the [permanent Review Checkpoint workflow](../AGENTS.md#permanent-checkpoint-branch-workflow):
publish coherent in-progress work, failed experiments and blockers as **REVIEW CHECKPOINT**,
then stop for independent review. **MERGE CANDIDATE** additionally requires completed acceptance
and full verification. Only an independently reviewed, explicitly owner-approved exact SHA may
merge after pre-merge identity/check verification. Publication never implies acceptance or deployment.
Continue on the same branch with additive commits; preserve prior review SHAs without force push.

The checkpoint report carries durable evidence/history and an unambiguous GitHub commit/tree
identity resolution. The owner need not transfer terminal output. Recovery remains the single
location for interruption state and unpublished delta; preserve valid work and exact remaining
steps. A published Review Checkpoint can be incomplete without being an interrupted local session.
When publication completes, reconcile the active recovery entry against that review state; do not
leave a false active interruption or claim the entire checkpoint is accepted.

The [BB-130 plan](architecture/bb-130-stabilization.md) applies this contract to the
current stabilization sprint; current progress belongs in STATUS and BACKLOG.
