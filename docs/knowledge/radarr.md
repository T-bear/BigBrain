# Radarr

## Syfte

Beskriva verifierad projektkunskap om Radarr-integrationen.

## Verifierade fakta

- Radarr ingår i mediamodulen och kapslas bakom en adapter.
- BigBrains kontrollerade filmrequestflöde använder preview, explicit bekräftelse och idempotens.

## Viktiga tekniska lärdomar

- Read-only health och applikationstest är evidens för nåbarhet, inte för alla framtida releaser.
- Mutationer ska följa liveverifierat versionskontrakt.

## Vanliga feltolkningar

- Sonarr-specifika incidentlärdomar får inte antas gälla Radarr utan verifiering.

## Relaterade runbooks

- [Media integration verification](../operations/runbooks/media-integration-verification.md)

## Relaterade dokument och ADR:er

- [Mediamodulen](../modules/media.md)
- [Arkitektur](../../ARCHITECTURE.md)

## Senast verifierad

2026-08-03.

## Källa och evidens

`docs/modules/media.md`, `docs/STATUS.md` och daterade media-baselines.
