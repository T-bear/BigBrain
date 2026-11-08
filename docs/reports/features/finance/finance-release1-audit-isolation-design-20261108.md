# Finance Release 1 – förenklad audit-isolering, designcheckpoint

Detta är en sanerad GitHub-version. V = verifierat i denna avgränsade checkpoint;
D = dokumenterat i tidigare underlag; O = ännu inte verifierat på verklig host.
Endast design- och syntetisk testkandidat, inte tillstånd för verklig Finance-dataåtkomst.

## Metadata

V – Datum: 2026-11-08. Risknivå: 3 (Finance); RESEARCH / 0 SEK / NONE.
V – Git-baslinje: `main` `0f11890dac3ff1529c83d7438658b90e4a61948e`.
D – Ägarens [tilläggsmandat v1.1 i draft-PR #42](https://github.com/T-bear/BigBrain/blob/7f36b1f35571b7b0ed72d3d476ee0d1186d49e53/docs/operations/finance-release1-security-simplification-mandate-20261108.md)
tillåter designarbete och syntetiska tester. Det [äldre Release 1-mandatet i draft-PR #40](https://github.com/T-bear/BigBrain/blob/90d45d03f11df8dce946a7fe3ccf19bbc1602309/docs/operations/finance-release1-mandate-20261106.md)
anger villkor för verklig verifiering. PR:erna är ännu inte accepterade i `main`.
[Finance-frysningen](../../../operations/finance-freeze-20261016.md) gäller i övrigt.

## Status

V – **REVIEW ONLY**. Arkitekturval: utred en separat, oprivilegierad
audit-identitet med ägarkontrollerad exponering av exakt tillåtna, redan statiska
underlag. Det är ett **designval**, inte tillstånd att skapa konton, kopiera
data, ändra rättigheter eller köra verklig Finance-verifiering.
O – Hostisolering och faktisk åtkomst är inte styrkta. Någon verklig
Finance-datakälla har inte lästs av denna checkpoint.

## Evidence

V – Den [fristående syntetiska fixturen](finance-release1-audit-isolation-fixture-20261108.py)
kördes den 2026-11-08 på BigBrain med Python 3.13.5: **15 PASS / 0 FAIL**.
Den använder bara tillfälliga påhittade filer, utan CLI-parameter för verklig
databasväg. Den testar explicit allowlist, innehållshash, filtyp, symlänkar,
hårdlänkar, rättighetsbitar, otillåtna extra filer och SQLite-sidofilsnamn.

O – Fixturen bevisar **inte** verklig UID-/ACL-isolering, nätverksgräns,
TOCTOU-/FD-säkerhet, backupens oföränderlighet, retention eller att andra
hostfiler är oläsbara. Dessa egenskaper kräver separat auktoriserad verifiering.

D – [Åtkomstunderlaget i PR #40](https://github.com/T-bear/BigBrain/blob/90d45d03f11df8dce946a7fe3ccf19bbc1602309/docs/reports/features/finance/finance-release1-access-handoff-20261106.md)
anger att befintlig UID 1654 även äger andra originaldatabaser; en vanlig
Pythonprocess som denna UID utan begränsat filsystem är inte acceptabel.
D – Den tidigare specialbyggda systemd-kandidaten omfattar cirka 754
Pythonrader plus prov och har rapporterat `226/NAMESPACE`, med okänd exakt
felorsak och utan verifierad host-PASS.

## Changes

V – Endast denna sanerade designrapport, ett fristående syntetiskt
testskript och en länk i befintligt Finance-rapportindex.
Ingen produktionskod, databasmigration, systemkonfiguration, faktisk UID,
ACL, originalbackup eller tjänst ändras av checkpointen.

## Security

### Hotmodell och arkitekturalternativ

En ny UID är **inte ensam en sandbox**: world-readable-filer, grupper och ACL
kan tillåta annan filåtkomst. SQLite `mode=ro` begränsar inte processens
övriga filåtkomst, nätverk eller skrivmöjligheter. Faktiska hostgränser
måste verifieras; annars krävs separat granskad OS-sandbox.

| Alternativ | Fördel | Olöst krav |
| --- | --- | --- |
| Befintlig specialbyggd systemd-startare | Omfattande avsedd isolering | Tidigare `226/NAMESPACE`; host-PASS saknas; hög komplexitet |
| **A: ny audit-UID + avgränsade läskopior** | Mindre specialkod; separerad identitet | Ägarstyrd provisionering, faktisk rättighetskontroll, nätverksgräns, retention och ny verifierargranskning |
| B: förenklad systemd-RO-sandbox | Kan ge starkare filsystemsgräns | Separat rootbeslut samt bevisade mount-/namespacegränser |

**Val:** A endast för syntetisk design. B är reserv om de obligatoriska
hostskydden inte kan bevisas med A. Inga skyddsmål får försvagas.

### Föreslaget exekveringskontrakt – ej auktoriserat

1. Ägaren fattar separat beslut om ny dedikerad audit-UID utan extra grupper,
   sudo, Docker- eller handelsrättigheter. Den får inte ha läsbehörighet
   till andra originaldatabaser, hemligheter eller modullager.
2. Endast uttryckligen namngivna, rättighetsklarerade och verifierat statiska
   snapshot-/manifest-/datafiler får exponeras. Ny datakopia kräver separat
   beslut om retention och disposition.
3. Verklig hostkontroll måste styrka katalogföräldrar, ACL/DAC, symlänkar,
   hårdlänkar, filtyp, hash, SQLite-sidofiler och integritet före/efter.
   Ingen verklig fil öppnas inom denna checkpoint.
4. Inga inärvda hemligheter, provideranrop eller handelsfunktioner.
   Nätverksisolering ska verifieras faktiskt; frånvaro av HTTP-kod räcker inte.
5. Den tidigare verifieraren har SHA-256
   `7c065708528a10a0d3f0408e7de5fd4af4b5d93cf43ecae1c5105829de8fb9bb`
   och hårdkodar UID 1654. Ny UID kräver ny exakt granskningsartefakt,
   självständigt hashgodkännande och verifiering. Tidigare bytes lämnas orörda.
6. SQLite `mode=ro`, `PRAGMA query_only` och SQL-authorizer ger djupförsvar.
   `immutable=1` är tillåtet endast för styrkt statiska, konsekventa kopior,
   inte aktiv WAL-data.

## Remaining work

O – Före verklig audit krävs separat ägarbeslut om provisionering,
exakta filrättigheter, eventuella nya kopior, nätverksbegränsning och
disposition. Därefter oberoende granskning av exakt exekverbar kandidat,
hash/retention och verklig hostisolering.
D – BB-132K:s dokumenterade dispositionsgranskningsfrist är
2026-11-10 kl. 14.36.45 svensk tid. Den förlängs inte av checkpointen.

Definition of Done för **designcheckpointen**: dokumenterad hotmodell,
alternativval, begränsningar, positiva/negativa syntetiska tester och
en separat granskningsbar PR. Det är inte verifierad verklig isolering.

## Resumption

**STOPP:** Ingen verklig Finance-dataläsning, användar-/ACL-ändring,
rootkörning, D3, Qwen/G3a, merge, deployment eller handel.
Efter godkänd dokumentations- och säkerhetsgranskning kan ägaren fatta
ett separat, exakt avgränsat beslut om eventuell hostförberedelse.
