# STÅENDE MANDAT: FINANCE RESEARCH RELEASE 1 (version 1)

## 1. SYFTE OCH GILTIGHET

Detta mandat gör det möjligt för arkitekten (ChatGPT) och lead developer (Codex) att slutföra Finance Research Release 1 (RESEARCH-läge, modellfri) utan att ägaren behöver följa eller förmedla varje steg. Det är ett uttryckligt ägarbeslut enligt finance-freeze-20261016.md (avsnitten om ägarbeslut för att häva frysning) och AGENTS.md (ägaren beslutar om scope och acceptans). Det gäller från ägarens godkännande till 2026-11-18, eller tills Release 1 är accepterad eller mandatet återkallas. Det avgör vad som får göras inom den tidsramen, inte att frysningen i övrigt är hävd.

## 2. ARKITEKTEN (GPT) FÅR BESLUTA OM

- Att befintlig, skrivskyddat verifierad evidens räcker för Release 1, eller att ett reproducerbarhetskrav återstår.
- DoD för Release 1 (förslag finns i Claude-/Codex-rapporterna), samt vilka fynd som är P0/P1 (blockerar) respektive parkeras.
- Ett D3-beslut om en enda modellfri historisk körning, men bara om samtliga villkor i punkt 4 är uppfyllda och dokumenterade i förväg.
- Att reviewvarv avslutas (högst två ordinarie; därefter redovisas kvarstående hinder).

Arkitekten får fastställa att Release 1 uppfylls genom befintlig evidens utan D3. Godkänd evidens ska identifieras med kodversion, käll-/feature-revisioner, run-ID:n, fullständiga resultat och verifierad intern konsistens. Enbart matchande lagrade checksummefält räcker inte.

## 3. CODEX FÅR GÖRA

- Läsa repot, skrivskyddade backuper och kopior enligt punkt 5 samt skapa rapporter och dokumentationsändringar i en egen gren. Rapporter lagras i docs/reports/features/finance/ och sammanfattas i högst 2 sidor.
- Köra de skrivskyddade verifieringsskript som arkitekten godkänt.
- Köra högst EN godkänd modellfri körning enligt punkt 4, om arkitekten beslutat det.
- INGEN kodändring i produktionskod, ingen migration, ingen deploy eller omstart i drift, och ingen merge av annat än rena dokumentationsändringar. Kodändringar kräver separat ägargodkännande.

## 4. VILLKOR FÖR EN D3-KÖRNING (alla ska vara uppfyllda)

- Indata: exakt WIKI-revision wiki-5713d7dccfa38f56 och feature-3833eb92bb641e51. Inga nya providers, inga anrop mot EODHD, Alpaca eller andra tjänster.
- Ingen språkmodell, ingen Qwen-väg, inget PAPER/LIVE/AUTO.
- Före körning: verifierad, aktuell backup och en dokumenterad återställningsplan, plus en lista över de tabeller som får skrivas (resultat- och lease-tabeller).
- Efter körning: återläsning och integritetskontroll av resultatet.
- Stopp utan automatiskt nytt försök vid varje oväntat utfall, FAIL eller skrivning utanför listan.
- Ingen strategi- eller periodsökning. Parametrar fastställs av arkitekten före körning och ändras inte efteråt.

## 5. ÅTKOMST

- Codex kör som en dedikerad, skrivskyddad systemanvändare. Ingen sudo, inget root, inga hemligheter.
- Verifiering sker endast mot befintliga, verifierade kopior, öppnade skrivskyddat med `mode=ro`. `immutable=1` används endast för dokumenterat statiska, konsekventa kopior. En beslutad D3-körning får endast skriva till en separat, uttryckligen utpekad arbetskopia genom en i förväg granskad exekveringsväg. Driftdatabasen öppnas aldrig. Saknas arbetskopia eller nödvändig avgränsad skrivbehörighet stoppas D3.
- Inga nätverksanrop mot dataleverantörer.

## 6. FÖRBEHÅLLET ÄGAREN (kan inte delegeras av detta mandat)

- Retention, disposition och radering av data (BB-132K, EODHD, WIKI-härledd data), eftersom det rör avtal med tredje part. BB-132K:s dokumenterade senaste disposal review deadline är 2026-11-10 kl. 14.36.45 svensk tid, eller tidigare enligt tillämpliga villkor. Ägaren beslutar om disposition utifrån aktuell inventering. Mandatet förlänger inga retentionfrister och auktoriserar ingen radering.
- Allt som rör riktiga pengar eller handel (PAPER, manuell handel, LIVE, AUTO).
- Qwen/G3a-isolering, nya providers, UI-arbete och annat utanför Release 1.
- Ändring av detta mandat och beslut om att frysningen i övrigt hävs.
- Utgifter eller kostnader utöver befintliga abonnemang.

## 7. STOPPVILLKOR (arbetet pausas och ägaren meddelas)

- Fynd som är P0/P1 och inte kan åtgärdas inom mandatet.
- Oförklarad skillnad mellan lagrad evidens och dokumenterade värden (checksummor, antal, lineage).
- Varje oväntad skrivning eller förändring i en kopia eller databas.
- Åtkomst som saknas och inte kan lösas utan förhöjda rättigheter.

## 8. RAPPORTERING OCH ACCEPTANS

- Efter varje etapp en rapport på högst en sida till ägaren (status, utfall, beslut som tagits, vad som återstår).
- När Release 1 är klar skickas en slutrapport. Ägaren accepterar eller avvisar den. Slutacceptans är det enda planerade ytterligare ägarbeslutet för Release 1. Frågor reserverade i punkt 6 och stopp enligt punkt 7 kan kräva ett separat ägarbeslut. Befintliga obligatoriska gransknings-, Git- och mergekrav kvarstår.

## 9. ÅTERKALLELSE

Ägaren kan återkalla mandatet när som helst med ett enda meddelande. Arkitekten och Codex ska då avsluta pågående arbete och rapportera status.

Godkänt av ägaren: 2026-11-06 — Jag godkänner mandat v1 med de fyra preciseringarna.
