# Start here — BigBrain continuity

BigBrain is a modular home-server control plane, with first-party React/TypeScript
views, an ASP.NET Core API, external-service adapters and a separate Sentinel
boundary. The owner decides product priority and UX/behavior acceptance; ChatGPT
reviews architecture independently; Codex implements and maintains evidence.

## Mandatory read order and authority

1. Read [AGENTS.md](../AGENTS.md) fully: working rules, safety, documentation and publication.
2. Fetch GitHub, inspect `origin/main`, HEAD and the working tree, then read the
   [canonical recovery note](operations/codex-recovery.md). GitHub main is the
   technical source of truth between completed sessions. Preserve unpublished work;
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

Completed work belongs in published history and authoritative documents. Interrupted
or unpublished work belongs temporarily in `docs/operations/codex-recovery.md`, the
only recovery-note location. Preserve valid work and record exact remaining steps.
Once a checkpoint is complete and published, remove its temporary state; if later
work is interrupted, record only that outstanding work against the new source SHA.
Publication never implies deployment authorization.

The [BB-130 plan](architecture/bb-130-stabilization.md) applies this contract to the
current stabilization sprint; current progress belongs in STATUS and BACKLOG.
