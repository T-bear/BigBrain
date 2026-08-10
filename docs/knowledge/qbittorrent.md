# qBittorrent

## Syfte

Samla säker kunskap om kö, peers och torrentborttagning.

## Verifierade fakta

- Versionsspecifik incidentobservation gäller qBittorrent 5.2.3.
- Installerad Web API-version är 2.15.1. Officiellt delete-kontrakt är
  `POST /api/v2/torrents/delete` med formfälten `hashes` och `deleteFiles`.
- `hashes` kan upstream vara flera `|`-separerade värden eller `all`; BigBrains Download
  Control-adapter tillåter därför endast en internt liveverifierad hash per anrop.
- qBittorrent 5.2.3/Web API 2.15.1 använder `torrents/stop`, `torrents/start` och
  `torrents/reannounce` för Sprint 2:s paus, återuppta och konservativa retry.
- Även batch behandlas som separata, liveverifierade enmålsanrop; `hashes=all` och
  hashlistor används aldrig.
- `deleteFiles=true` raderar även nedladdad data; annars påverkas endast torrentjobbet.
- `queuedDL` med metadata och 0/0 peers betyder inte automatiskt en död torrent.

## Viktiga tekniska lärdomar

- Kontrollera `queueing_enabled`, `max_active_downloads`, köposition, trackers, DHT, PeX och LSD före peerbedömning.
- Separat borttagning i den verifierade recoveryrutinen använder endast exakt liveverifierad torrent och `deleteFiles=false`.
- Save path delas normalt av flera torrents och är inte en säker datagräns. Content path
  måste vara icke-tom och unik, och färdiga/importosäkra jobb blockeras från destruktiv borttagning.
- Den deployade MVP:n har användarverifierat en filbevarande UI-borttagning av ett fastnat
  jobb. Detta är inte evidens för fullständig säkerhet i `deleteFiles=true`-flödet.

## Vanliga feltolkningar

- Indexerns seederantal är inte en garanti för aktiva peers vid senare announce.
- Metadatahämtning och dataöverföring är olika tillstånd.

## Relaterade runbooks

- [Queue and peer diagnosis](../operations/runbooks/qbittorrent-queue-and-peer-diagnosis.md)
- [Dead Download Recovery v1](../operations/recovery/dead-download-recovery-v1.md)

## Relaterade dokument och ADR:er

- [Media stack](media-stack.md)

## Senast verifierad

2026-08-04; versionsobservationen gäller qBittorrent 5.2.3 och Web API 2.15.1.

## Källa och evidens

Read-only liveverifiering 2026-08-04, officiell qBittorrent WebUI API-dokumentation,
post-incidentdiagnosen 2026-08-03 och ARR-incidentens slutrapport.
