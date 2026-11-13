# Verktygskatalog – beslutsstöd för BigBrain

**Status:** pilot från 2026-11-08. Katalogen beskriver möjligheter och begränsningar,
inte tillstånd att använda ett verktyg eller bevis på åtkomst i en viss session.

## Syfte och användning

ChatGPT (arkitekt) och Codex (Lead Developer) väljer vid behov minsta lämpliga
verktygskombination för ett redan auktoriserat uppdrag. Gör valet utifrån uppgift,
risknivå, tillgängliga åtkomster, sekretess, kostnad/krediter och krav på oberoende
granskning. Vid rutinuppgifter med uppenbart verktygsval behövs ingen separat
genomgång eller dokumentation.

Kontrollera aktuell åtkomst och rättigheter när verktyget ska användas.
En tidigare fungerande integration innebär inte att den fortfarande är ansluten.
Avsaknad av tillgång ger inte rätt att kringgå behörigheter eller skyddsregler.

## Verktyg och lämpliga användningar

| Verktyg | Lämplig användning | Begränsning eller valprincip |
| --- | --- | --- |
| ChatGPT | Arkitektur, avgränsad kod-/dokumentationspatch, granskning och teknisk handoff | Egna ändringar är inte en oberoende nivå 3-granskning. |
| Codex | Större implementation, tester och felsökning som kräver utvecklararbetsflöde | Lead Developer enligt AGENTS; använd inte rutinmässigt för små patchar som passar befintligt Termius-flöde. Tillgång/krediter kontrolleras per uppdrag. |
| Claude (extern) | Fristående, kritisk säkerhets- och kandidatgranskning | Åtkomst till repo/host antas aldrig. Granska exakt SHA/tree och ange vad som faktiskt har lästs eller körts. Kopiera aldrig hemligheter. |
| GitHub och ansluten GitHub-integration | Verifiera main, commit/tree, PR/diff och CI; publicera review inom auktoriserat scope | Push/PR ger varken merge-, deployment- eller utökat uppdragstillstånd. |
| Remote Desktop Commander | Avgränsad serverdiagnostik, läsning och oprivilegierade syntetiska tester | Åtkomst styrs av faktisk session och hostbehörighet. Ingen generell sudo/root eller påverkan på produktion utan uttryckligt tillstånd. |
| Termius och `/home/enigma/bigbrain-pr.sh` | Ägarstyrd publicering av små, isolerade patchar med diffgranskning och `PUBLICERA` | Använd befintligt patch-/checkpointflöde; ingen automatisk merge/deployment och inga manuella filöverföringar. |
| Context7 | Versionsnära dokumentation för bibliotek och API:er | Använd när aktuell primärdokumentation kan påverka lösningen; verifiera sedan mot projektets faktiska versioner. |
| Wolfram (ChatGPT-plugin) | Matematiska och statistiska kontrollberäkningar för Finance, t.ex. avkastning, Sharpe-kvot, drawdown och backtestmått | Ansluten till ChatGPT, inte direkt till BigBrain. Ersätter inte dataproveniens, rättighetskontroll eller oberoende nivå 3-granskning och ger ingen handels- eller deploymentauktoritet. |
| Acumen | Förberedande kontroll av ändrad extern kunskap inför research | Externt underlag ersätter inte projektets accepterade kontrakt eller egen verifiering. |
| Webbsökning och officiell dokumentation | Aktuella beroenden, leverantörskrav, standarder och källor | Bedöm källtrovärdighet och dataskydd; publicera inte interna uppgifter i sökningar. |

Tabellen är ett urval av kända arbetsvägar, inte en uttömmande inventering av
installerade appar, plugins, konton eller priser. Föränderlig åtkomst, behörighet,
version och kostnad ska verifieras inför faktisk användning. Claude är här en
separat granskningsväg, inte en bekräftad direkt integration i ChatGPT/Codex.

## Snabbt verktygsval

1. Fastställ redan godkänt scope, risk, DoD och stoppvillkor enligt
   [AGENTS.md](../../AGENTS.md) och [START-HERE](../START-HERE.md).
2. Välj enklaste fungerande väg. Små ändringar: ChatGPT + verifiering +
   Termius/PR-skript; större implementation: Codex enligt dess uppdrag.
3. Använd Remote Desktop endast när hostfakta behövs; GitHub för käll- och
   revisionsfakta. Hämta aktuell extern kunskap bara när det påverkar beslutet.
4. För risknivå 3: säkerställ faktisk oberoende granskning av exakt kandidat.
   Claude kan anlitas som separat granskare, men rapportens verifierade scope
   ska vara tydligt.
5. Om ett verktyg saknas eller är olämpligt: välj en redan tillåten alternativ
   väg eller stoppa och begär beslut. Skapa inte ny infrastruktur av vana.

## Gränser och utvärdering

[AGENTS.md](../../AGENTS.md), [checkpoint-arbetsflödet](checkpoint-workflow.md),
[projektets frysningar](freezes.md) och konkreta ägarbeslut gäller före denna
katalog. Verktygstillgång ändrar inte risknivå, kostnadsgränser, godkännanden,
säkerhetskrav eller dokumentauktoritet. Inga tokens, nycklar, privata adresser
eller interna hostidentiteter ska antecknas här.

Piloten utvärderas vid nästa relevanta arbetsuppdrag: hjälpte verktygsvalet att
minska onödig arbetsinsats utan att försämra kontrollen? Om inte, förenkla eller
ta bort katalogen i separat granskad ändring. Ingen automatisk ny sprint.
