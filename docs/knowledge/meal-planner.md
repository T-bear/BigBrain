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
- Skollunch v1 har en offline RSS-parser och separat read-only skolmåltidskontrakt för review.
  Family visar offline-status och utvecklingsfixtur; ingen hushållsplan eller inköpslista skrivs.
  Live-hämtning är inte implementerad/aktiverad. Båda originalfilerna har parsats offline med fem lunchdagar; HTML anger vecka men inte årtal.
  Skolmedveten hushållsmenygenerering är fortsatt framtida scope.
  [Kontrakt och evidens](../reports/features/family/school-meals-preparation-20261023.md#skollunch-v1--offline-implementation-2026-10-23).

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
