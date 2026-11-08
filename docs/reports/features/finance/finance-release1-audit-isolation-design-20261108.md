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
kördes ursprungligen på BigBrain med Python 3.13.5: **15 PASS / 0 FAIL**.
Efter Claudes oberoende granskning av den ursprungliga kandidaten har denna
samordnade kandidat utökats: **29 PASS / 0 FAIL** på BigBrain 2026-11-08.
Fixturen skapar enbart påhittade filer i en temporär katalog och accepterar
inga verkliga databasvägar. Exakt ägare, grupp och mode (0700 på katalog,
0440 på filer), avsaknad av POSIX ACL-xattr, filtyp, allowlist, hash,
symlänkar, hårdlänkar, SQLite-sidofilsnamn och specialbitar provas.
Filen öppnas med O_NOFOLLOW; hash läses från samma FD och metadata kontrolleras
före och efter. Detta eliminerar inte alla path-/mount-race på en verklig host.

V – Det [reproducerbara syntetiska Bubblewrap-provet](finance-release1-bwrap-synthetic-replay-20261108.sh)
kördes som vanlig oprivilegierad användare på BigBrain: PASS för begränsad
filsystemsvy, nätverksnamnrymd samt CapEff/CapPrm = 0 och NoNewPrivs = 1.
Efter Claudes oberoende granskning av kandidat PR #46 har replay-provet
utökats: det läser per-mount-alternativen för exakt /evidence i
/proc/self/mountinfo och kräver ro. Filskrivningsförbudet rapporteras
separat som DAC-/mount-effekt och används inte ensamt som bevis för RO-bind.
Ett negativt syntetiskt mutationstest ersätter --ro-bind med --bind för
evidence-katalogen och ska då stoppa utan slut-PASS. Scriptet är en vidareutvecklad
reproduktionsvariant av det tidigare ägarövervakade provet: det använder
dessutom Python -I -S -B. Läget --audit kräver en separat ägarövervakad
administrativ start och **har inte körts på nytt för denna kandidat**.
Scriptet skapar inga konton och accepterar inte verkliga Finance-data.

D – Claude granskade ursprungliga PR #44/#45 den 2026-11-08, reproducerade
15 syntetiska tester och gav *GODKÄND FÖR SYNTETISKT SCOPE*, med två P2-villkor:
utökade fil-/ACL-kontroller samt reproducerbart bwrap-prov. Båda åtgärdas i
denna kandidat. Claudes tidigare godkännande av de äldre bytesen är **inte**
ett godkännande av den här nya kandidatens exakta SHA/tree.

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

V – Den samordnade patchen innehåller designrapporten, den skärpta
syntetiska Python-fixturen, ett reproducerbart syntetiskt bwrap-skript,
den oförändrade sanerade hostrapporten från draft-PR #45 samt **båda**
länkarna i befintligt Finance-rapportindex. Det löser den gemensamma
README-hunk som Claude identifierade för PR #44 och #45.
Ingen produktionskod, databas, systemkonfiguration, faktisk UID, ACL,
originalbackup eller tjänst ändras av Git-checkpointen.

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

O – Före verklig audit krävs separat ägarbeslut om verklig filåtkomst,
eventuella nya kopior, nätverksbegränsning och disposition. R1–R6 i Claudes
granskning kvarstår för verklig Finance-dataåtkomst: startväg utan permanent
sudo, negativa hosttester under audit-UID, sudoers-/ACL-inventering, ny exakt
verifierarartefakt, FD/seccomp/resursgränser och statisk SQLite-konsistens.
Den syntetiska fixturens textfil är **inte** ett SQLite-integrationstest.

D – Claude granskade även exakt PR #46-kandidat, commit 72a60ef1ee8e07efb8c66071fd7efc98092bb299,
tree 6bc8499d198139a4ed397a55c092ebb052e4c3a4, och gav
GODKÄND FÖR SYNTETISKT SCOPE med ett icke-blockerande P2-fynd:
skrivtestets PASS skilde inte en skrivbar bind från DAC-begränsning.
Denna nya kandidat adresserar just det fyndet.

O – Den här kandidatens ändrade bytes måste genomgå ny oberoende
nivå 3-review av exakt SHA/tree mot rätt main-baslinje.
Ingen tidigare granskning får överföras automatiskt till nya bytes.

Definition of Done för **designcheckpointen**: dokumenterad hotmodell,
alternativval, begränsningar, positiva/negativa syntetiska tester och
en separat granskningsbar PR. Det är inte verifierad verklig isolering.

## Resumption

**STOPP:** Ingen verklig Finance-dataläsning, användar-/ACL-ändring,
rootkörning, D3, Qwen/G3a, merge, deployment eller handel.
Efter godkänd dokumentations- och säkerhetsgranskning kan ägaren fatta
ett separat, exakt avgränsat beslut om eventuell hostförberedelse.
