# Prowlarr

## Syfte

Samla nätverks- och indexerlärdomar för Prowlarr.

## Verifierade fakta

- Prowlarr förmedlar indexerresultat till Arr-applikationerna.
- Historisk incidentobservation: 1337x fungerade via den då verifierade basadressen; indexerstatus är dynamisk och ska testas live.

## Viktiga tekniska lärdomar

- `127.0.0.1` inuti en container avser containern själv.
- En download-URL får bara hostkorrigeras efter bevis att path, query och resurs är identiska och att den konfigurerade Docker-hosten används.

## Vanliga feltolkningar

- En generell kontrollsökning är inte ett universellt indexerhälsokrav.
- Incidentens hostkorrigering är versions- och nätverksspecifik, inte en generell rewrite-regel.

## Relaterade runbooks

- [Media integration verification](../operations/runbooks/media-integration-verification.md)
- [Dead Download Recovery v1](../operations/recovery/dead-download-recovery-v1.md)

## Relaterade dokument och ADR:er

- [Mediamodulen](../modules/media.md)

## Senast verifierad

2026-08-03; indexerstatus måste verifieras på nytt.

## Källa och evidens

ARR-incidentens slutrapport och post-incidentdiagnos.
