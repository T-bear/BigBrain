# Shopping List

## Syfte

Samla verifierad kunskap om modulen Inköpslista.

## Verifierade fakta

- Modulen har en permanent aktiv lista, lokal autocomplete, köphistorik, handlingsläge och deterministisk inlärd butiksordning.
- Persistensen är modullokal SQLite i separat volym; version 1 stöder en API-instans.

## Viktiga tekniska lärdomar

- Dubblettinvarianten försvaras i UI, API och databasindex.
- Lokal, deterministisk och förklarbar modell valdes framför extern AI.

## Vanliga feltolkningar

- Backup/restore, flerinstanssamordning och generell användaridentitet är ännu inte lösta.
- Grundlisteförslag blir inte historik förrän användaren lägger till varan.

## Relaterade runbooks

- [Operationsindex](../indexes/operations.md) (backup/restore är planerad, inte godkänd).

## Relaterade dokument och ADR:er

- [Status](../STATUS.md)
- [Backlog](../BACKLOG.md)

## Senast verifierad

2026-08-03; implementationen runtimeverifierades 2026-08-02.

## Källa och evidens

Kod, tester och verifieringssammanfattningen i `docs/STATUS.md`.
