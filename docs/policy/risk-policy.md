# Riskpolicy och arbetsinsats

Bindande detaljer till [AGENTS.md](../../AGENTS.md); avsnittsnummer avser AGENTS.md. Backupgrinden ligger kvar i [AGENTS §3](../../AGENTS.md#3-riskbaserad-arbetsinsats).

Princip: skydda pengar och behörighetsgränser hårt. Låt resten av BigBrain vara ett experiment som kan rullas tillbaka.

## 3. Riskbaserad arbetsinsats

### Risknivå för uppdraget

Risknivå anges i uppdraget utifrån vad ändringen kan göra och vilken åtkomst koden faktiskt har, inte vilken modul den ligger i. Datans värde räcker inte: en oviktig modul som kan nå Docker-socketen, Finance-nycklar eller ta emot godtyckliga kommandon är nivå 3. Vid verklig osäkerhet om en nivå 3-trigger används den högre nivån. Codex får inte själv sänka nivån för att undvika granskning, och ett bekvämlighetsmål gör inte en databas- eller privilegieändring till nivå 1.

| Risknivå | Utlöses av | Verifiering och granskning |
| --- | --- | --- |
| 1 – experiment och bekvämlighet | UI, media, prototyper, testdata, dokumentation, isolerad forskning och läsbaserade bekvämlighetsfunktioner utan ändrad datalagring, åtkomstkontroll eller privilegieökning. Befintliga leverantörs- och driftvillkor gäller. | Fokuserade tester och CI. |
| 2 – normal drift | Beständiga data, databasstruktur, API-beteende, schemaläggning och åtkomst till hushållsdata utan nivå 3-konsekvens. Finance RESEARCH enligt nedan. | CI, egenkontroll och relevant backup. Dataintegritet, migration och avgränsad åtkomst verifieras. |
| 3 – ekonomisk säkerhet | Order, PAPER/LIVE, handels- eller kapitalauktoritet; ekonomiska nycklar eller konton; root, sudo, Docker-root, Docker-socket, administratörsåtkomst eller nätverksexponering; ändringar som kan öppna en väg dit; ändringar av skyddsreglerna själva. | Oberoende granskning, negativa tester och specifika godkännanden. |

Risknivå är inte i sig ett merge-, deployment-, root- eller upptiningsgodkännande. Befintliga giltiga godkännanden återanvänds inom sin omfattning; installerade skydd och aktuella kontrakt får inte kringgås utom enligt följande.

Leveransspår:

- Web-only STANDARD (nivå 1, endast Web-filer enligt [lanes.md](lanes.md)) deployas med `scripts/deploy-web-only.sh` utan oktober-gatewayen. Ägarens körning av kommandot är merge- och deploymentgodkännandet, när båda filerna har accepterats i `main`.
- API+Web STANDARD följer [T2-regeln i lanes.md](lanes.md#standard--apiweb-t2-review-checkpoint-2026-11-01) efter oberoende kandidatgranskning och acceptans i main.
- Gamla paket, approvals och worker är frysta/arkiverade, inte en alternativ STRIKT-väg. STRIKT, schemaändringar och frysningar behåller sina bindande gransknings- och driftgränser. Ingen ny generell exekveringsväg, inga nya admin-ACL:er, betaltjänster eller handelsrättigheter följer.

### Finance RESEARCH och isoleringsvillkor

Handelsförmåga är alltid nivå 3. Finance RESEARCH på historisk data är nivå 2 när alla villkor nedan är verifierade och ägaren har noterat det i `docs/STATUS.md`. Tills dess är Finance nivå 3.

Isoleringsvillkor (engångsverifiering, nivå 3):

1. Docker-socketen är inte monterad i API, Web eller Media.
2. Media-stacken körs utan privilegier och utan åtkomst till Finance-volymer eller Finance-nätverk.
3. Inga broker- eller handelsnycklar finns på servern.
4. Servern har inte nätverksåtkomst (Tailscale-regler) till ägarens personliga enheter eller konton utöver vad som är nödvändigt.
5. Backupkopior kan inte raderas av en komprometterad container.

RESEARCH-garanti (automatiska tester i CI):

- RESEARCH-koden importerar inga broker-, order- eller handelsklienter.
- Databasen öppnas skrivskyddad och ett skrivförsök misslyckas.
- Testmiljön har varken nätverk eller nycklar.
- Ett negativt test som försöker lägga en order eller skriva till produktionsdata gör CI rött.

Ändringar i order-, rättighets-, nyckel- eller skyddskod är nivå 3 oavsett modul. Den tunga vetenskapliga granskningen (framtidsläckage, backtestvalidering, reproducerbarhet) görs vid övergången från RESEARCH till simulerad eller riktig handel, inte vid varje ändring. Finance-ADR:erna gäller i övrigt som tekniska krav. Före simulerad eller riktig handel krävs separat ägarbeslut och förlustgränser som inte enbart beror på strategikod, helst hos brokern (nyckel utan uttagsrätt, litet konto, förlustgräns).

### Proportionell arbetsinsats och verifiering

Använd lägsta lämpliga arbetsinsats och eskalera vid konkret behov. LOW/MEDIUM/HIGH beskriver arbetsinsats, inte de tre risknivåerna eller rätt att sänka ett uppdrags granskning.

| Nivå | Typisk omfattning |
| --- | --- |
| LOW | Mekanisk dokumentation/formatering, isolerad UI/text, små mekaniska teständringar. |
| MEDIUM | Normal implementation, flerfilsrefaktor, integration och vanlig felsökning. |
| HIGH | Finance-korrekthet, beräkningar/modeller och vetenskaplig integritet; marknadsdatasemantik/lineage; arkitekturbeslut, migrationer, säkerhetsgränser, samtidighet, svår felsökning och kritisk kodreview; PAPER/LIVE/trading/kapitalauktoritet. |

Inom ägarens scope beslutar arkitekten verifieringsnivå efter risk; Codex gör samma bedömning när den inte anges. Ansvarskedjan är ägare → arkitekt → Codex.
Nya funktioner ska ha relevanta tester. Kör fokuserade tester först och utöka vid konkret risk eller obligatoriskt krav. Merge Candidates får den bredaste verifiering som faktisk risk motiverar, inte rutinmässigt hela testsviten.
Återanvänd giltig evidens när relevant tree/kod, beroenden och antaganden är oförändrade. Ny commit/push kräver inte i sig omtest; vid oförändrat godkänt kandidatträd och förväntad main räcker normalt SHA/tree-verifiering och relevant CI. Rena dokumentändringar behöver inte applikationstester. Redovisa körda kommandon och resultat, återanvänd evidens och varför kontroller saknas eller inte gäller.
