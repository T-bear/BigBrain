# Media Integration Verification

- Status: Draft
- Scope: Read-only BigBrain/media
- Senast verifierad: 2026-08-03
- Verifierade versioner: Dynamiska versioner verifieras live
- Riskklass: Låg vid read-only
- Kräver godkännande: Ja för mutation

## Syfte

Skilja tjänstehälsa, integration, downloadclient och swarm.

## Förutsättningar

Läsbehörighet, maskering och uttryckligt mål.

## Read-only preflight

Notera Git/containerbaseline; kontrollera health, nätverk, mounts och adapterstatus.

## Stoppvillkor

Stoppa före mutation vid oklar identitet, semantik, path, behörighet eller mediarisk.

## Procedur

Verifiera adapter, tjänst, applikationskoppling och downloadclient; kontrollera kö/tracker/peers innan releasebedömning.

## Verifiering

Separera verifierat, sannolikt och ej verifierat.

## Rollback eller återställning

Ingen för read-only; använd separat godkänd runbook för ändring.

## Förbjudna åtgärder

Ingen restart, konfiguration, grab, blocklist eller filåtgärd.

## Evidens och relaterade incidenter

ARR-slutrapport och post-incidentdiagnos 2026-08-03.
