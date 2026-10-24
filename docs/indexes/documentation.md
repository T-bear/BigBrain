# Documentation Index

- [Frysningsregister](../operations/freezes.md): index till kanoniska ägarbeslut och återupptagningsprotokoll.

- [BB-158 — canonical freeze / primary entry](../operations/bb158-freeze-20261023.md)
- [Family school meals — source preparation only](../reports/features/family/school-meals-preparation-20261023.md)


- [Finance dataset intake and quarantine](../architecture/finance/dataset-intake-quarantine.md)
- [Finance owner market-data drop runbook](../operations/runbooks/finance-owner-market-data-drop.md)

## Authority order

When sources disagree, use this order and resolve the inconsistency explicitly:

1. Accepted ADR
2. Normative architecture
3. Code and commit-bound baseline
4. Module contract
5. Approved runbook
6. Current [STATUS](../STATUS.md)
7. Sanitized verification report
8. Diagnostics
9. Proposed ADR
10. Historical document

## Entry points

- [Start here / continuity](../START-HERE.md): uppgiftsstyrd läsguide och hänvisning till beständig handoff.
- [Product roadmap](../../ROADMAP.md): direction and domain-roadmap delegation.
- [BB-130 ordered stabilization plan](../architecture/bb-130-stabilization.md): phase scope and DoD.

- [README](../../README.md): product overview and development entry point.
- [STATUS](../STATUS.md): compact current reality and verification level.
- [BACKLOG](../BACKLOG.md): remaining work and acceptance criteria.
- [AGENTS](../../AGENTS.md): working, safety and completion rules.
- [ARCHITECTURE](../../ARCHITECTURE.md): normative system boundaries.
- [TESTING](../../TESTING.md): testing map.
- [ADR index](adr.md), [knowledge index](knowledge.md), [operations index](operations.md) and [reports index](reports.md).

Local reports under `/home/enigma/BigBrain/reports/` are fuller internal evidence and
are not repository authority. Sanitized knowledge is published under
`docs/reports/`, including incomplete Review Checkpoints awaiting independent review. Main records
accepted work; the active checkpoint branch records ongoing unaccepted work. Publication never
promotes branch code or a proposed decision above accepted architecture. See the
[permanent workflow](../../AGENTS.md#8-git-och-review-checkpoints).

## Product and architecture

- [BB-131F owned-worker lifecycle — Accepted ADR 0040](../adr/0040-local-reasoner-owned-worker-lifecycle.md)

- [BB-131D local-first reasoner/security contract — accepted design](../architecture/finance/local-first-reasoner-boundary.md)

- [Family View & Family Coordination epic](../architecture/family-view-family-coordination.md)
- [Dashboard and Widget Framework](../architecture/dashboard-widget-framework.md)
- [Media module](../modules/media.md)
- [Calendar module](../modules/calendar.md)
- [Finance master roadmap](../architecture/finance/master-roadmap.md)
- [Finance module](../modules/finance.md)
- [Finance threat model](../security/finance-threat-model.md)
- [Finance market-data provider selection](../architecture/finance/market-data-provider-selection.md)
- [Meal Planner knowledge](../knowledge/meal-planner.md)
- [Shopping List knowledge](../knowledge/shopping-list.md)
- [Design system](../design-system/theme-contract-v1.md)
- [Early sprint history](../history/early-sprints.md)

## Historiska status- och recoveryförlopp

- [Status före DOC-2A](../history/status-before-doc-2a-20261024.md) och
  [Recovery före DOC-2A](../history/codex-recovery-before-doc-2a-20261024.md): historisk evidens, inte aktuella körorder.
- [DOC-2A inventering och destinationer](../reports/documentation/doc-2a-recovery-status-inventory-20261024.md#fas-b--godkänd-konsolidering-2026-10-24).
  Aktuella ingångar är fortsatt STATUS och codex-recovery; auktoritetsordningen ovan är oförändrad.
