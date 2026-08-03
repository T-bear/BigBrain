# System Baseline Capture

- Status: Draft
- Scope: Daterad read-only inventering
- Senast verifierad: 2026-08-03
- Verifierade versioner: Ej versionsbunden
- Riskklass: Låg, med sekretessrisk
- Kräver godkännande: Ja för extern publicering

## Syfte

Skapa reproducerbar evidens inom namngivet scope.

## Förutsättningar

Definierat scope, destination, retention och secret-scan.

## Read-only preflight

Registrera Git, containers, tidzon och mål utan överskrivning.

## Stoppvillkor

Stoppa vid hemlighet, överskrivning, runtimepåverkan eller oklart scope.

## Procedur

Samla minsta data; maskera; skriv temporärt; verifiera; publicera atomiskt; checksumma och indexera.

## Verifiering

Kontrollera innehåll, länkar, secret scan, Git och containerbaseline.

## Rollback eller återställning

Skriv aldrig över; markera fel additivt efter review.

## Förbjudna åtgärder

Ingen installation, restart, konfiguration eller credentialinsamling.

## Evidens och relaterade incidenter

Systembaseline och dokumentationsinventering 2026-08-03.
