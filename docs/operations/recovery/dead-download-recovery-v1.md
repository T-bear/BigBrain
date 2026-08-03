# Dead Download Recovery v1

- Status: Draft, incident-verified, Version-specific, Requires explicit approval
- Scope: En exakt liveverifierad Sonarr-download i taget
- Senast verifierad: 2026-08-03
- Verifierade versioner: Sonarr 4.0.19.2979; qBittorrent 5.2.3
- Riskklass: Hög
- Kräver godkännande: Ja, för varje muterande fas

## Syfte

Återställa en verifierat död Arr-download utan att radera data eller påverka andra objekt.

## Förutsättningar

Liveverifierad identitet, filinventering, säker backup, dynamiskt verifierad ersättningsrelease och versionsspecifika API-kontrakt.

## Read-only preflight

Kontrollera health, liveidentitet, filer, köregler, metadata, faktisk progress, trackers och peers. `queuedDL` med 0/0 peers är inte automatiskt död. Historiska ID:n eller hashprefix får aldrig återanvändas.

## Stoppvillkor

Stoppa vid identitetsavvikelse, faktisk progress, filer i riskzonen, oklar API-semantik, otillräcklig kandidat, oväntad sidoeffekt eller integrationsfel.

## Procedur

1. Verifiera identitet, filsystem och qBittorrents köregler live.
2. Sök isolerat mot avsedd indexer och verifiera kandidaten.
3. Låt Sonarr blocklista och ta bort exakt köpost med `removeFromClient=false`, `blocklist=true`, `skipRedownload=true` och versionsverifierade övriga parametrar.
4. Verifiera kö, blocklist, historik, filer och att torrenten finns kvar.
5. Ta separat bort exakt liveverifierad qBittorrent-post med `deleteFiles=false`.
6. Verifiera att inga filer eller andra torrents ändrats och sök kandidaten igen.
7. En loopback-host får korrigeras endast efter bevis för identisk path, byte-identisk query och identisk resurs, till Sonarrs redan konfigurerade Prowlarr-host.
8. Låt Sonarr göra exakt en grab; verifiera och observera.

## Verifiering

Bevisa att endast målobjektet ändrats, gamla releasen blocklistats, filer bevarats och ny torrent är tekniskt frisk. Dynamiska indexer- och peeruppgifter hämtas på nytt.

## Rollback eller återställning

Ingen automatisk kompensationsmutation. Stoppa och rapportera förväntad och faktisk effekt samt risk.

## Förbjudna åtgärder

Aldrig `deleteFiles=true` eller `removeFromClient=true` i detta tvåstegsflöde. Ingen historisk identitet, rå magnet, andra grab, massoperation eller konfigurationsändring.

## Evidens och relaterade incidenter

ARR-slutrapporten `sonarr-e03-e10-e12-recovery-and-workflow-validation-20260803-173958.txt` och post-incidentdiagnosen `qbittorrent-queue-and-peer-availability-diagnosis-20260803-185208.txt` i extern rapportyta.
