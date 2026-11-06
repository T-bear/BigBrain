# Finance Release 1 — förberedelse för skrivskyddad evidensgranskning

Detta är en sanerad GitHub-version. V = verifierat nu; D = dokumenterat/ägarrapporterat;
O = okänt. Inga Finance-data har öppnats eller verifierats i denna etapp.

## Metadata

V — 2026-11-06, risknivå 3. Verifierad GitHub-main, commit: `0f11890dac3ff1529c83d7438658b90e4a61948e`.
[Ägarmandatet](https://github.com/T-bear/BigBrain/blob/28e08469f248921922ece380523a0ac7f0a0f6db/docs/operations/finance-release1-mandate-20261106.md)
är läst på exakt SHA; PR #40 var öppen/omergad.
V — Huvudarbetskopian: HEAD `e946793707a2a2c46c1d67d8a61ae1f06a9df992`, tree
`021f00fb4d221806c39b6a43b978694b05eb1df2`; inga tracked ändringar, sju orelaterade untracked poster.
`git diff` visar identisk Finance-kod mot main. PR #40:s rena arbetskopia återanvänds. AGENTS, START-HERE och frysningsbeslutet är lästa.

## Status

V — **REVIEW CHECKPOINT; dataverifiering stoppad vid saknad åtkomst.** `/etc/passwd` bekräftar det dedikerade kontot; `os.geteuid()` visar annan sessionsanvändare.
Förnyad metadatakontroll (`os.stat`) gav PermissionError; sessionens UID/EUID motsvarar inte
det dedikerade kontot. Ingen Finance-fil öppnades eller åtkomst höjdes.
D — Ägaren har nu förmedlat arkitektens godkännande av exakt skripthash nedan för augustikopian.
V — Filhashen är omverifierad och oförändrad; verifieraren har inte körts mot Finance-data.
O — kräver: befintlig tillåten körkontext med dedikerad skrivskyddad identitet och verifierade
läsrättigheter; kontots faktiska ACL:er och kopians aktuella metadata är inte styrkta.

## Evidence

D — [BB-091](finance-bb-091-european-macro-fx-20260817.md) identifierar
`bb-091-pre-migration-20260817.db`, daterad 2026-08-17: SHA-256
`84ef36437bdcd8b762b0a3243c6af705def029e036749ecce701264c28405986`, lika stoppad driftkälla,
samt isolerad migrationskontroll med oförändrade antal. Ägaren uppger cirka 1,3 GB och inga WAL/SHM.
O — kräver: aktuell hash-/metadata-/sidecarkontroll före val som verifierad kopia.
D — Kandidatens augustidatum begränsar evidensen; senare förändringar och dagens drift förblir O.

| V — kontroll i granskningsskriptet | Bevisets gräns |
| --- | --- |
| Dedikerad UID, ej root, ej skrivbar fil; exakt dokumenterad filhash före SQLite; inga symlänkar eller WAL/SHM/journal; stabil metadata | Daterat kopieinnehåll, inte nuvarande drift eller full ACL-inventering. `immutable=1` används först efter denna bindning. |
| Sex BB-084-ID:n och full historisk konfiguration, inklusive parametrar, alla kostnadsfält, kapital, period, seed och evaluationContext | Konfiguration v1 från BB-084-koden; inga nya strategikörningar. |
| Resultathash omräknad med tomt root-checksumfält; alla child-JSON jämförs med Decimal, identitet, antal och ordning | Oberoende jämförelse av lagrade representationer; ingen ekonomisk omberäkning. Fills har ingen lagrad insättningsordinal. |
| WIKI-/feature-relation, feature-radernas källreferenser, datum, tillstånd/antal; fem instrument och AAPL i varje run | Inga numeriska käll-/feature-checksummor räknas om; ingen källfil återtolkas. |
| 1 172 fills, 27 992 events, 748 sessioner; inventering av övriga ID:n/tider | Endast snapshoten; skapande worker/manuell körning och faktisk byggrevision är okända. |
| Manifestdeklarationer och strömmad datafilhash; closure-katalog endast metadata | Manifestkontraktet saknar tabellvisa radantal; inga datafiler återställs. |

V — Kontrakt: main:s backtest-/feature-persistenskod samt BB-084:s `DeterministicBacktesting.cs`
på `d9eadbb3f3533e70e57cd12ad6890b06e36e4772`. Hashing av konfigurationskonstanter återskapar alla sex ID:n.
V — [Syntetisk fixture](finance-release1-verify-fixture-20261106.py): 72 PASS/0 FAIL/5 NOT CHECKED,
78 utdatarader; 26 negativa fall avvisades. UID/skrivbarhet/filhash och manifest-ID:n ersätts
endast i testets minnesnamnrymd. Revisionsskriptet ändras inte. Audit och före/efter-inventering
visar inga verifierarskrivningar, process-/nätverksanrop eller ändrade fixturfiler; alla fixturer skapades i temp.

## Changes

V — Endast denna rapport, kataloglänk och två fristående granskningsartefakter; ingen produktionskod.
[Exakt verifieringsskript](finance-release1-verify-readonly-20261106.py), SHA-256:
`7c065708528a10a0d3f0408e7de5fd4af4b5d93cf43ecae1c5105829de8fb9bb`.

## Security

V — Endast standardbibliotek, `mode=ro&immutable=1`, `PRAGMA query_only=ON`, SQL-authorizer
med SELECT/READ/count. Ingen EodhdMarketMemory, appstart, lease, migration eller driftåtkomst.
Första FAIL stoppar skriptet; endast sanerad stdout och exit 0 utan FAIL, annars 1.
V — Text-/AST-kontroll: INSERT/DELETE/ALTER/DROP/VACUUM/REINDEX/ATTACH/CREATE/subprocess/socket: 0;
UPDATE: 2, båda `hashlib.update` i minnet. Tre filöppningar, samtliga `rb`; inga skrivlägen.

### Sekretessjobbets verifierade falsklarm och avgränsat förslag

V — [CI 42219758708](https://github.com/T-bear/BigBrain/actions/runs/42219758708), kandidat
`029ef0d9d96ddcabff80649138d5f2dd0272c55e`: backend/frontend/documentation PASS;
[secrets 127898580555](https://github.com/T-bear/BigBrain/actions/runs/42219758708/job/127898580555)
FAIL. Gitleaks 8.24.3 rapporterade exakt ett fynd, `generic-api-key`, rapportens historiska rad 8.
Den utpekade strängen är main-commitidentiteten i Metadata, verifierad både via GitHub och
`git cat-file -t` (commit), inte en credential. Jobbets maskerade Finding-rad binder fyndet till
formuleringen före denna SHA. Raden har nu förtydligats; det tar inte bort det historiska fyndet.

V — Befintlig praxis: [BB-124](finance-bb-124-anti-overfitting-oos-governance-20260901.md),
commit `91247b81be5b55b1200e7c39233106ef3f2ecd3e` och repots `.gitleaksignore` använder ett
fyndspecifikt undantag. Förslag, **inte tillämpat**: lägg efter avgränsad granskning till exakt
följande enda rad i den befintliga filen, med en vanlig framåtriktad commit:

```text
029ef0d9d96ddcabff80649138d5f2dd0272c55e:docs/reports/features/finance/finance-release1-readonly-preparation-20261106.md:generic-api-key:8
```

Förslaget undantar bara kombinationen commit/fil/regel/rad. Behåll övriga regler, workflow,
kravet på godkänt sekretessjobb och befintlig historik. Verifiera därefter sekretessjobbet på
ny exakt kandidat; andra fynd måste bedömas separat. V — `.gitleaksignore`, workflow och det
godkända skriptet är oförändrade i denna dokumentationsetapp; inget nytt sekretess-PASS påstås.

## Remaining work

O — kräver: en redan tillåten dedikerad körkontext med befintlig läs-/traverseringsrätt till
backupkatalogen och den utpekade kopian. Ingen sådan körkontext är styrkt i denna session;
kontots faktiska rättigheter är inte härledbara ur dess existens. Inga nya rättigheter beställs.
Evidensintegritet, Release 1-DoD och separat reproducerbarhetskrav är
inte avgjorda av syntetiska PASS. D — retentionbeslut förblir ägarens enligt mandat §6;
senaste dokumenterade granskningsfrist är 2026-11-10 14.36.45 svensk tid, eller tidigare.

## Resumption

V — STOP enligt mandat §7: ingen körning utan styrkt befintlig åtkomst; ingen merge, D3
eller driftåtgärd utförd. D — Exakt skript är godkänt enligt ägarens besked. Först i tillåten kontext: `python3 -B finance-release1-verify-readonly-20261106.py
--db <godkänd-kopia> --manifest-dir <backupkatalog>` (en rad). Exit 0 innebär inte Release 1-acceptans.

Parkerat: O — numerisk omräkning av käll-/feature-checksummor och driftens senare evidens.
