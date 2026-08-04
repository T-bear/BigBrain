# Media Stack

## Syfte

Beskriva stabil kunskap om BigBrains medieintegrationer.

## Verifierade fakta

- BigBrain kapslar Jellyfin, Sonarr, Radarr, Prowlarr och qBittorrent bakom media-adapters.
- Publika BigBrain-API:er är versionssatta och kontrollerade mediebegäranden använder preview och explicit bekräftelse.
- Jellyfin Server 10.11.11 exponerar det verifierade remote-play-kontraktet
  `POST /Sessions/{sessionId}/Playing`; Smart Shuffle confines it to a selected live
  remote-control-session och ett servervaliderat avsnitt för den konfigurerade användaren.
- En Samsung Smart TV med Jellyfin for Tizen har verifierats som entydig aktiv session
  med `SupportsRemoteControl=true`. UI-styrd start och hopp har verifierats end-to-end:
  Jellyfin accepterade kommandot, rätt `NowPlayingItem` bekräftades och sessionen blev aktiv.

## Viktiga tekniska lärdomar

- Historisk incidentlärdom: ett fungerande Sonarr–Prowlarr–qBittorrent-flöde kan ändå vänta på swarm eller lokal kö.
- Rekommendation: verifiera integration, köregler, trackerstatus och peers som separata lager.
- Smart Shuffle behöver en separat, avgränsad Jellyfin-timeout: en generell tresekundersgräns
  kan avbryta episodvalet innan PlayNow når servern.
- Ett accepterat remote-play-kommando och bekräftad TV-uppspelning är skilda tillstånd.
  Tizen kan behöva en kort verifieringsperiod; skrivkommandot får inte skickas igen under den.

## Vanliga feltolkningar

- `queuedDL` eller 0/0 peers bevisar inte ensamt att releasen är död.
- Incidentrapporter bevisar ett daterat förlopp, inte aktuell tjänstestatus.

## Relaterade runbooks

- [Media integration verification](../operations/runbooks/media-integration-verification.md)
- [qBittorrent queue and peer diagnosis](../operations/runbooks/qbittorrent-queue-and-peer-diagnosis.md)

## Relaterade dokument och ADR:er

- [Mediamodulen](../modules/media.md)
- [Arkitektur](../../ARCHITECTURE.md)
- [Proposed ADR 0011](../adr/0011-smart-shuffle-jellyfin-remote-playback-boundary.md)

## Senast verifierad

2026-08-04.

## Källa och evidens

`docs/modules/media.md`, `docs/STATUS.md`, de maskerade Smart Shuffle-rapporterna under
`/home/enigma/BigBrain/reports/features/smart-shuffle/` samt ARR-incidentens slutrapport
och post-incidentdiagnos.
