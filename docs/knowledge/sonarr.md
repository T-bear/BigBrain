# Sonarr

## Syfte

Samla verifierade Sonarr-lärdomar utan återanvändbara historiska identiteter.

## Verifierade fakta

- BigBrain använder Sonarr genom en adapter; kontrollerade begäranden kräver preview och bekräftelse.
- Versionsspecifik observation: Sonarr 4.0.19.2979:s queue-delete exponerade inte en explicit `deleteFiles`-parameter.

## Viktiga tekniska lärdomar

- Incidentverifierat tvåstegsflöde: Sonarr blocklistar och tar bort exakt liveverifierad köpost med `removeFromClient=false`; därefter tas exakt torrentpost bort separat med `deleteFiles=false`.
- Liveidentitet och API-kontrakt måste verifieras före varje mutation.

## Vanliga feltolkningar

- Historiska queue-ID:n och hashprefix är inte framtida instruktioner.
- Ett incidentverifierat flöde är inte automatiskt säkert för andra versioner.

## Relaterade runbooks

- [Dead Download Recovery v1](../operations/recovery/dead-download-recovery-v1.md)

## Relaterade dokument och ADR:er

- [Mediamodulen](../modules/media.md)
- [Dokumentauktoritet](../indexes/documentation.md)

## Senast verifierad

2026-08-03; versionsobservationen gäller Sonarr 4.0.19.2979.

## Källa och evidens

ARR-incidentens slutrapport 2026-08-03 och installerat API-kontrakt som dokumenterades där.
