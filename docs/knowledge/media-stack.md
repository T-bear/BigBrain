# Media Stack

## Syfte

Beskriva stabil kunskap om BigBrains medieintegrationer.

## Verifierade fakta

- BigBrain kapslar Jellyfin, Sonarr, Radarr, Prowlarr och qBittorrent bakom media-adapters.
- Publika BigBrain-API:er är versionssatta och kontrollerade mediebegäranden använder preview och explicit bekräftelse.

## Viktiga tekniska lärdomar

- Historisk incidentlärdom: ett fungerande Sonarr–Prowlarr–qBittorrent-flöde kan ändå vänta på swarm eller lokal kö.
- Rekommendation: verifiera integration, köregler, trackerstatus och peers som separata lager.

## Vanliga feltolkningar

- `queuedDL` eller 0/0 peers bevisar inte ensamt att releasen är död.
- Incidentrapporter bevisar ett daterat förlopp, inte aktuell tjänstestatus.

## Relaterade runbooks

- [Media integration verification](../operations/runbooks/media-integration-verification.md)
- [qBittorrent queue and peer diagnosis](../operations/runbooks/qbittorrent-queue-and-peer-diagnosis.md)

## Relaterade dokument och ADR:er

- [Mediamodulen](../modules/media.md)
- [Arkitektur](../../ARCHITECTURE.md)

## Senast verifierad

2026-08-03.

## Källa och evidens

`docs/modules/media.md`, `docs/STATUS.md` och ARR-incidentens slutrapport och post-incidentdiagnos.
