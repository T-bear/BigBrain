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

## Skollunch – HTTP-färskhet skild från anropsspärr (T2-review, 2026-11-03)

Denna korrigering ersätter cache-/stale-beteendet ovan; minnesmodell och anropsspärr bevaras.

- Ett giltigt svar behöver inte ha sex timmars `max-age`. En separat absolut färskhetsgräns
  beräknas enligt [RFC 9111 §§4.2–4.3](https://www.rfc-editor.org/rfc/rfc9111.html#section-4.2):
  `s-maxage` före `max-age` före `Expires`, med `Date`, `Age` och anropets fördröjning.
  Även läsning av svarskroppen förbrukar färskhet. Lokal livslängd begränsas till 24 timmar.
- API:t återanvänder aldrig ett utgånget svar. Frånvaro av `must-revalidate` innebär inte
  verifierad stale-rätt. Stale-utökningar aktiveras inte. `no-store`, `no-cache` (även med
  fältnamn), `private`, `Vary: *`, ogiltig/tvetydig cachemetadata och saknad explicit färskhet
  avvisas konservativt. `must-revalidate`/`proxy-revalidate` uppfylls genom förbudet mot stale.
- Giltig 304 kräver sparad meny och skickad validator; motsägande ETag avvisas. Utelämnade
  cachefält ärvs, nya fält ersätter motsvarande metadata. Färskheten räknas om, ursprunglig
  hämtningstid bevaras. Utgången data kan hållas enbart för validering inom befintlig 24 h-/
  ISO-veckogräns; den får inte visas. Cacheförbud rensar även meny och validatorer.
- `NextAttempt` förblir minst sex timmar efter avslutat anrop, senare vid `Retry-After` på
  vilket HTTP-svar som helst. Osäkra headers/ogiltig Retry-After spärrar resten av processen.
  Cacheutgång, UI-timer och samtidiga läsningar ger inga extra källanrop.
- Additivt `validUntilUtc` i befintligt API låter en redan öppen vy dölja utgångna menyer med
  samma feltext. Timer samt fokus-/synlighetskontroll hämtar inget. Historiska fixtures påverkas
  inte. Webbläsarens tidskontroll förutsätter korrekt enhetsklocka; fysisk PWA-acceptans återstår.
- Omstart tömmer fortfarande cache och spärr enligt accepterad main och ägarbeslutet 2026-11-01.
  **Ingen beständig spärr över omstarter finns.** Detta uppdrag tillför inga databas-, fil- eller
  driftsändringar. Omstart får inte användas för att kringgå källgränser; framtida drift måste
  verifiera tidigare åtkomst/Retry-After innan nya anrop. Nuvarande uppdrag gör inga anrop.
- Ägarrapporterad produktionsdiagnostik: Rosenfeldt `200/max-age=60/must-revalidate` ger högst
  60 sekunders visning per sex timmar, minus redan förbrukad ålder. **Kontinuerlig visning är
  omöjlig med dessa villkor.** Musikugglan `200/max-age=3600` ger högst en timme; inte heller
  där används utgångna svar utan verifierad rätt. Ingen ökad hämtningsfrekvens föreslås här.

[Scope, testresultat, underlagsproveniens och kvarstående leverantörshinder](../reports/features/family/school-meals-preparation-20261023.md#cache-policy-correction--t2-review-checkpoint-2026-11-03).
Implementation/offline-test är inte deployment, aktuell menyverifiering eller ägaracceptans.

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
