# qBittorrent Queue and Peer Diagnosis

- Status: Verified, Version-specific
- Scope: Passiv diagnos av liveverifierad torrent
- Senast verifierad: 2026-08-03
- Verifierade versioner: qBittorrent 5.2.3
- Riskklass: Låg vid read-only
- Kräver godkännande: Ja för signal/ändring

## Syfte

Skilja kö, tracker, nätverk och swarm utan ersättning.

## Förutsättningar

Exakt liveidentitet och read-only API.

## Read-only preflight

Kontrollera status, metadata, filer, kategori, save path, köposition och kövärden.

## Stoppvillkor

Stoppa mutation vid oklar identitet eller risk för andra torrents.

## Procedur

Läs `queueing_enabled`, `max_active_downloads`, torrentegenskaper, trackers, peers, DHT, PeX och LSD; observera passivt och klassificera.

## Verifiering

`queuedDL` och 0/0 är inte ensamt felbevis; jämför över tid.

## Rollback eller återställning

Ingen för read-only; reannounce/resume/köändring kräver separat godkännande.

## Förbjudna åtgärder

Ingen force start, prioritet, preference, delete, blocklist eller grab.

## Evidens och relaterade incidenter

`qbittorrent-queue-and-peer-availability-diagnosis-20260803-185208.txt`.
