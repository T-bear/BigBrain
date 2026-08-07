# Shopping List

## Syfte

Samla verifierad kunskap om modulen Inköpslista.

## Verifierade fakta

- Modulen har en permanent aktiv lista, lokal autocomplete, köphistorik, handlingsläge och deterministisk inlärd butiksordning.
- Persistensen är modullokal SQLite i separat volym; version 1 stöder en API-instans.

## Viktiga tekniska lärdomar

- Dubblettinvarianten försvaras i UI, API och databasindex.
- Exakt normalisering behåller sin databasunikhet. En separat konservativ jämförelsenyckel tar bort whitespace, bindestreck och diakritiska markörer för att föreslå sannolika skrivvarianter utan edit distance eller substringmatchning; användaren kan välja befintlig vara, lägga till ändå eller avbryta.
- Lokal, deterministisk och förklarbar modell valdes framför extern AI.

## Vanliga feltolkningar

- Backup/restore, flerinstanssamordning och generell användaridentitet är ännu inte lösta.
- Redan öppna klienter får inte automatiskt server push när en annan klient ändrar listan. Ett gemensamt realtidslager är planerat för arkitekturutredning i BB-036 och kräver separat ADR före implementation.
- Kapselknapparna under ”Ofta köpt” använder semantiska text-, surface-, primary-, focus- och disabled-tokens i alla tre teman.
- Grundlisteförslag blir inte historik förrän användaren lägger till varan.

## Relaterade runbooks

- [Operationsindex](../indexes/operations.md) (backup/restore är planerad, inte godkänd).

## Relaterade dokument och ADR:er

- [Status](../STATUS.md)
- [Backlog](../BACKLOG.md)

## Senast verifierad

2026-08-07; grundimplementationen runtimeverifierades 2026-08-02. Sprint 1-fixarna är automatiskt verifierade och production-byggda men inte deployade eller manuellt verifierade.

## Källa och evidens

Kod, tester och verifieringssammanfattningen i `docs/STATUS.md`.
