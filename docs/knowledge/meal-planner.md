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
- Skolmedveten hushållsmenygenerering är fortsatt framtida scope.
- Skollunchs tidigare offline- och källverifieringar är daterad evidens, inte aktuell driftstatus:
  [kontrakt och historisk evidens](../reports/features/family/school-meals-preparation-20261023.md#skollunch-v1--offline-implementation-2026-10-23).

## Skollunch – minnesbaserad hämtning (review, 2026-11-01)

- Ägarens steg (c) återför hämtningen till processminne utan `MealPlannerStore`,
  bootstrap-inställningar eller skapande/användning av `SchoolMealFetchGate`. En redan befintlig
  tabell lämnas orörd; ingen migration eller dataradering ingår.
- Varje skola har egen låsning och cache. Högst ett källanrop pågår per skola och API-process;
  nästa försök tillåts tidigast sex timmar efter avslutat anrop, eller senare enligt Retry-After.
  Ogiltig Retry-After eller osäkert mottagna headers spärrar den skolan resten av processens livstid.
- Omstart tömmer historiken och tillåter första anropet utan verifierad tidigare historik enligt
  det uttryckliga ägarbeslutet. Inga schemalagda hämtningar eller automatiska omförsök införs.
- Källklient, parser, API/UI-kontrakt och default-off-flaggor behålls. Cacheförbud respekteras;
  stale begränsas till 24 timmar efter giltig validering och samma ISO-vecka. Utgångna menyer och
  validatorer rensas; 304 utan skickad validator får inte förnya innehåll.
- Offlineverifiering: 117 fokuserade SchoolMeal-, MealPlanner- och ShoppingList-tester passerar,
  inklusive samtidighet, omstart, Retry-After, cache/304, timeout och oförändrade databasbytes.
  Ingen verklig källhämtning, merge eller deployment ingår i denna checkpoint.
- Befintligt privilegierat deploymentkontrakt ändras inte här. Dess krav på beständig gate och
  bootstrap är inte bevis för denna minnesbaserade revision; drift kräver separat tillämpligt beslut.

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
