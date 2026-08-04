# Tidiga sprintar

Detta dokument bevarar historisk kontext som tidigare dominerade README. Det är inte aktuell produktstatus.

## Grundläggande plattform

De första sprintarna etablerade ASP.NET Core-API, React/Vite-Web, modulregister, versionssatta System- och Dockerkontrakt, health checks och Compose. System och Docker returnerade initialt uttryckliga unavailable-resultat tills Sentinelgränsen fanns.

## Read-only Media-baslinje

Den första Media-sprinten normaliserade read-only status från Jellyfin, Sonarr, Radarr, Prowlarr och qBittorrent med partiell felisolering. Senare sprintar lade till kontrollerade request-, playback- och Download Control-mutationer. Tidiga formuleringar som säger att Media helt saknar writes är därför ersatta av [aktuellt modulkontrakt](../modules/media.md).

## Utveckling efter baslinjen

Media Search, Media Jobs, Smart Shuffle, Download Control, Matlista, Inköpslista, designsystem, teman samt Dashboard Views/Widget Framework byggdes ovanpå den modulära monoliten. Daterad evidens finns i [rapportkatalogen](../reports/REPORT-CATALOG.md); aktuell status finns i [STATUS](../STATUS.md).
