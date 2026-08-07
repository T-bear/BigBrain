# Shopping List

## Syfte

Samla verifierad kunskap om modulen Inköpslista.

## Verifierade fakta

- Modulen har en permanent aktiv lista, lokal autocomplete, köphistorik, handlingsläge och deterministisk inlärd butiksordning.
- Persistensen är modullokal SQLite i separat volym; version 1 stöder en API-instans.

## Viktiga tekniska lärdomar

- Dubblettinvarianten försvaras i UI, API och databasindex.
- Nuvarande namnnormalisering stoppar exakta normaliserade träffar men är inte verifierad för snarlika namn som skiljer sig genom sammanskrivning eller andra mindre skrivvariationer; se BB-034.
- Lokal, deterministisk och förklarbar modell valdes framför extern AI.

## Vanliga feltolkningar

- Backup/restore, flerinstanssamordning och generell användaridentitet är ännu inte lösta.
- Redan öppna klienter får inte automatiskt server push när en annan klient ändrar listan. Ett gemensamt realtidslager är planerat för arkitekturutredning i BB-036 och kräver separat ADR före implementation.
- Läsbarheten för kapselknapparna under ”Ofta köpt” är ett bekräftat tillgänglighetsbehov i BB-035.
- Grundlisteförslag blir inte historik förrän användaren lägger till varan.

## Relaterade runbooks

- [Operationsindex](../indexes/operations.md) (backup/restore är planerad, inte godkänd).

## Relaterade dokument och ADR:er

- [Status](../STATUS.md)
- [Backlog](../BACKLOG.md)

## Senast verifierad

2026-08-07; implementationen runtimeverifierades 2026-08-02 och de senare användarfynden är dokumenterade, inte implementerade.

## Källa och evidens

Kod, tester och verifieringssammanfattningen i `docs/STATUS.md`.
