# Start here — uppgiftsstyrd läsguide

BigBrain är ett modulärt hemserversystem med React/TypeScript, ASP.NET API, externa adapters
och separat Sentinel-gräns. Läs minsta kontext som uppgiften kräver; historiska rapporter är
inte rutinmässigt obligatorisk läsning.

## Alltid före arbete

Läs [AGENTS.md](../AGENTS.md), fetch/verifiera GitHub main, aktiv branch, exakt SHA och working tree.
Bevara opublicerat och orelaterat arbete. Identifiera uppdragets scope, risk och verifierbara DoD,
samt kontrollera [relevanta frysningar](operations/freezes.md). Om main har ändrats, bedöm relevant
delta innan äldre underlag används. Saknas DoD: undersök vid behov och föreslå den, men invänta
ägarens eller arkitektens godkännande inom auktoriserat scope före implementation.

## Välj kontext efter uppgift

| Uppgiftstyp | Minsta relevanta dokumentation |
| --- | --- |
| Lokal dokumentation eller text | Berört kanoniskt dokument och dess länk-/publiceringskrav; ingen rutinmässig kod- eller historikläsning. |
| Implementation eller felsökning | Berörd kod/tester, modulkontrakt, relevanta STATUS/BACKLOG-poster, tillämplig arkitektur/ADR och vid UI designsystemet. |
| Arkitektur eller integrationsgräns | ARCHITECTURE, berörda accepterade ADR:er och modul-/API-kontrakt; relevanta beslut och beroenden. |
| Finance | Kanoniskt frysningsbeslut först; endast vid auktoriserat scope berörda science-, lineage-, säkerhets-, rättighets- och testkontrakt. |
| Review eller merge | Exakt checkpoint och main-baseline, diff, relevant rapport/evidens, status/backlog och tester/required checks enligt [reviewreglerna](../AGENTS.md#7-review-och-blockerare). |
| Deployment eller runtime | Separat ägarauktorisering, accepterad revision, berörd runbook, verifierad runtimeidentitet, säkerhets-/backup-/återhämtningskrav. |
| Recovery eller fortsättning | [Enda recovery-noten](operations/codex-recovery.md), Git/working tree, checkpointens evidens och exakt återstående scope. Konflikt med repo kräver stopp. |
| Fryst eller återupptaget projekt | [Frysningsregistret](operations/freezes.md), kanoniskt beslut och återupptagningsprotokoll; nytt uttryckligt ägarbeslut och auktoriserad scope/DoD före teknisk fortsättning. |

Utöka läsning vid konkret risk, osäkerhet, beroende eller konflikt. Tabellen är en urvalsguide,
inte ett nytt fullständigt läskrav. [Dokumentationsindexet](indexes/documentation.md) hjälper till
att hitta källor; [TESTING](../TESTING.md) hjälper till att välja riskmotiverad verifiering.

Vid breda sökningar ska historiska dokument normalt exkluderas. Exempel från repositoryroten:

```sh
rg --glob '!docs/history/**' 'sökterm'
```

Läs historiken vid riktad spårbarhetskontroll, felsökning eller när ett aktuellt dokument
hänvisar till den. Detta ändrar inte dokumentauktoriteten.

## Dokumentauktoritet

Behåll [indexets auktoritetsordning](indexes/documentation.md#authority-order): accepterad ADR →
normativ arkitektur → kod och commitbunden baseline → modulkontrakt → godkänd runbook → aktuell
STATUS → sanerad verifieringsrapport → diagnostik → föreslagen ADR → historiskt dokument.

Auktoritet är frågespecifik: accepterade kontrakt begränsar tillåtna lösningar, kod visar
implementerat beteende och daterad evidens visar vad som faktiskt verifierats eller driftsatts.
Kod accepterar inte ett förslag retroaktivt; en ADR bevisar inte deployment. Ägarbeslut styr om
arbete är auktoriserat och om UX accepterats. Rapportera konflikter uttryckligen. Publication av
reviewarbete innebär inte acceptans; main och review har olika status.

## Kontinuitet och avslut

Följ [AGENTS avsnitt 11](../AGENTS.md#11-kort-sluthandoff) för kort handoff och
[avsnitt 6](../AGENTS.md#6-dokumentation--minsta-beständiga-sanning) för beständig dokumentation.
För betydande arbete ska nödvändiga kontinuitetsfakta normalt rymmas i cirka tio rader med länkar
till fördjupad evidens; ingen separat åttafältsmall krävs. Chatt ersätter inte kanonisk dokumentation.
Recovery används för avbrott/opublicerad delta, inte som en parallell statusrapport.

Uppfylld DoD avslutar uppdraget. Ingen automatisk fortsättning, ny sprint eller aktivering.
Review och P0/P1 följer [AGENTS avsnitt 7](../AGENTS.md#7-review-och-blockerare);
merge kräver exakt SHA-godkännande och deployment separat auktorisering enligt
[avsnitt 8](../AGENTS.md#8-git-och-review-checkpoints).
