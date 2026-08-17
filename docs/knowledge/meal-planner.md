# Meal Planner

## Syfte

Samla verifierad kunskap om modulen Matlista.

## Verifierade fakta

- Matlista hanterar maträtter, taggar, deterministisk veckogenerering, byte och sparade matsedlar.
- Persistensen är modullokal SQLite med versionssatt, idempotent migrering; en API-instans stöds.

## Viktiga tekniska lärdomar

- Måltider adresseras med schema, datum och måltidstyp så att ett byte är lokalt.
- Modulen degraderar självständigt om dess databas inte kan öppnas.

## Vanliga feltolkningar

- Recept, ingredienskoppling och automatisk inköpslistegenerering är framtidsplaner, inte implementation.
- Backup/restore och fleranvändarkonflikter är inte lösta.
- Externa skolmåltider och skolmedveten hushållsmenygenerering är planerade framtida förbättringar,
  inte implementerade funktioner. De hör till den befintliga veckoplaneraren, inte en separat Home-
  modul, och kräver verifierade officiella källor och automatiseringsrättigheter före implementation.

## Relaterade runbooks

- [Operationsindex](../indexes/operations.md)

## Relaterade dokument och ADR:er

- [Status](../STATUS.md)
- [Arkitektur](../../ARCHITECTURE.md)
- [Future product planning](../reports/documentation/product-ux-auth-school-meals-backlog-capture-20260817.md)

## Senast verifierad

2026-08-03; implementationen runtimeverifierades 2026-08-01.

## Källa och evidens

Kod, tester och verifieringssammanfattningen i `docs/STATUS.md`.
