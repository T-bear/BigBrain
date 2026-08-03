# qBittorrent

## Syfte

Samla säker kunskap om kö, peers och torrentborttagning.

## Verifierade fakta

- Versionsspecifik incidentobservation gäller qBittorrent 5.2.3.
- `queuedDL` med metadata och 0/0 peers betyder inte automatiskt en död torrent.

## Viktiga tekniska lärdomar

- Kontrollera `queueing_enabled`, `max_active_downloads`, köposition, trackers, DHT, PeX och LSD före peerbedömning.
- Separat borttagning i den verifierade recoveryrutinen använder endast exakt liveverifierad torrent och `deleteFiles=false`.

## Vanliga feltolkningar

- Indexerns seederantal är inte en garanti för aktiva peers vid senare announce.
- Metadatahämtning och dataöverföring är olika tillstånd.

## Relaterade runbooks

- [Queue and peer diagnosis](../operations/runbooks/qbittorrent-queue-and-peer-diagnosis.md)
- [Dead Download Recovery v1](../operations/recovery/dead-download-recovery-v1.md)

## Relaterade dokument och ADR:er

- [Media stack](media-stack.md)

## Senast verifierad

2026-08-03; versionsobservationen gäller qBittorrent 5.2.3.

## Källa och evidens

Post-incidentdiagnosen 2026-08-03 och ARR-incidentens slutrapport.
