# Download Control safe removal

- Status: Implemented and deployed MVP; file-preserving live removal user-verified, destructive live verification pending
- Scope: One live-verified qBittorrent job per user-confirmed request
- Verified versions: qBittorrent 5.2.3, Web API 2.15.1
- Risk: Medium for file-preserving removal; high for destructive removal
- Approval: Explicit UI confirmation for every target; additional active acknowledgement for data deletion

## Safe procedure

1. Open Media → Nedladdningar and refresh the list.
2. Verify name, status, progress, category and Arr ownership warning.
3. Open Hantera for exactly one job.
4. Prefer Avbryt nedladdning. Preview must say that downloaded files are preserved.
5. Confirm once. Verify the row disappears only after server success.
6. Verify the same job disappeared from qBittorrent, its data remains and all other jobs are unchanged.

At least one file-preserving removal of a stuck job has been confirmed by the user through BigBrain UI. This evidence does not approve or prove the destructive procedure below.

## Destructive procedure

Use only a separately designated harmless test job. The destructive button must be enabled by server risk assessment, preview must match the intended item, and the user must actively check the acknowledgement. Stop if the path/import scope is uncertain. After confirmation, verify exactly the designated data disappeared and no imported media or other torrent changed.

## Stop conditions

Stop on expired/changed identity, ambiguous or shared content path, completed/import-uncertain job, provider/version uncertainty, unexpected Arr behavior, or any indication that another torrent/media could be affected. Use the file-preserving action instead where appropriate.

## Invariants

No terminal-based live delete, mass operation, Arr history/blocklist/search mutation, media deletion, qBittorrent configuration change or raw identity in reports. A new list and preview are required after API restart.

Sprint 2:s batch omfattar endast pause, resume och retry enligt ADR 0016. Batch-delete
ingår inte; all borttagning följer fortsatt objektspecifik preview och bekräftelse.

Produktägaren godkände 2026-08-10 pause/resume, den säkra batchhanteringen och
diagnostiken i deployad runtime. Retry är implementerad och automatiskt verifierad men
väntar på manuell kontroll när en naturligt felande nedladdning finns; skapa eller
mutera inte ett riktigt jobb enbart för att framtvinga kontrollen. Detta är ingen känd
defekt och blockerade inte Sprint 2-stängningen.
