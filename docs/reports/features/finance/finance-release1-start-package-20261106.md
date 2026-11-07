# Finance Release 1 — startpaket för oberoende granskning

Detta är en sanerad GitHub-version. V = verifierat i denna avstämning;
D = dokumenterat/ägarrapporterat; O = okänt. Paketets föreslagna beteende är
kontrollerat med syntetiska filer utan privilegier. Verklig systemd-isolering är inte
verifierad för den nya kandidaten.

## Metadata

V — 2026-11-07: accepterad GitHub-main
`0f11890dac3ff1529c83d7438658b90e4a61948e`; PR #40:s utgångspunkt
`82a73a88204fa308b518de7592dd6f93c5183616`, rent checkpoint-working-tree före rättningen.
V — Samma checkpoint används; orelaterade filer i ordinarie checkout lämnas orörda.
D — Ägaren har rapporterat metadataavstämning `problems=0`; råresultatet har inte omverifierats.

## Status

D — Ägaren rapporterar ett försök med kvarvarande `etc`, `var`, `root`, `bin` och
därefter ägarlokal `PASS temporary_resources_removed` för
`0e06762315a143aca87da36e3ab48dc1`. Cleanup är inte omverifierad av Codex.
**D — Det tidigare engångsgodkännandet är förbrukat.**
V — [Startpaketet](finance-release1-start-package-20261106.py) är rättat och syntetiskt
testat. Ingen Finance-dataåtkomst, administratörsexekvering eller verklig systemd-start
har gjorts i denna rättningsetapp. O — kräver ny oberoende kandidatgranskning och därefter
separat ägargodkännande; tidigare terminalblock är inte ett nytt körtillstånd.

## Evidence

| Kontroll | Evidens och begränsning |
| --- | --- |
| V — Oförändrad verifierare | SHA-256 `7c065708528a10a0d3f0408e7de5fd4af4b5d93cf43ecae1c5105829de8fb9bb`, samma bytes som den [godkända artefakten](https://github.com/T-bear/BigBrain/blob/029ef0d9d96ddcabff80649138d5f2dd0272c55e/docs/reports/features/finance/finance-release1-verify-readonly-20261106.py). |
| V — Nytt startpaket | SHA-256 `959c9f6ee28492ec9f96d8f9e1f74c65e664d1a334842b430b760d8cc170da73`. Ersätter granskningskandidaten `e0f5cd99071e8266a2ef0055b1ab5f6ca71cad85ef95a924a57a35447ad9c45b`; historiken är bevarad i föregående commit. |
| V — Cleanup-felet | I föregående `layout()` saknades alla fyra namnen. `_cleanup_tree` omfattar nu förskapade, tomma kataloger och kontrollerar hela den begränsade listan före borttagning. Okända poster ger stopp utan borttagning. |
| V/O — Startvakten | Föregående vakt avvisar befintliga `var`/`root`, även tomma. `etc`/`bin` omfattades inte av den kontrollen. O — kräver det tidigare försökets steglogg för dess faktiska första avvisningspunkt och metadata för att avgöra vem som skapade katalogerna; inget sådant antas. |
| V — Syntetisk regression | [15 tester PASS](finance-release1-start-package-tests-20261107.py): normal avslutning, guard-fel, timeout, verifierarfel, källändring, delvis staging, blockerad cleanup/återhämtning, aktiv tjänst, okända poster/symlänkar, skydd mot osanerad feltext och båda provpayloads. Systemd, ägare/UID och mountskydd simuleras; filoperationer sker endast i temp-fixturer. Orelaterad sentinel bevaras. |
| D/O — OS-runtime | Föregående avstämning: Python 3.13.5/systemd 257.13. O — kräver separat godkänt systemd-prov för faktisk namespace-, DAC/ACL- och cgroup-funktion. Syntetiska tester bevisar inte dessa skydd. |
| D/O — Datakopia | Augustikopia 2026-08-17 och manifest-/datafiler från 2026-08-15/16 enligt befintligt underlag. O — kräver verifierarens faktiska hash-, lineage- och internkonsistenskontroller för resultat om innehållet. Ägarens metadatautfall bevisar inte innehållsintegritet. |

## Changes

V — Startpaket, denna rapport, syntetiska tester och ett separat förberett systemd-prov. Verifieraren,
produktionskod, säkerhetsundantag och driftkonfiguration ändras inte.
V — `G00–G11` identifierar vaktens steg, `S01–S14` förälderns steg och `C00–C07`
cleanupsteg. Exempel: `G06_empty_var`, `S08_wait_ready`, `C05_inventory`,
`C06_remove_files`. Endast fasta tillåtna identifierare återges; råa undantag, paths,
konfiguration och journalutdrag skrivs inte ut. Ett vaktfel före READY bevarar sitt G-ID.

## Security

V — Följande skydd är uttryckta i paketets kod; **runtime-PASS påstås inte**:

1. **Staging:** administratören läser endast verifierarens innehåll och kontrollerar
   dess godkända hash. En exklusivt skapad katalog `/run/finance-release1-<RESOURCE_ID>`
   är rootägd 0700. Endast skriptet kopieras, root:1654/0440; övriga skapade filer är
   tomma monteringspunkter. Källornas rättigheter ändras inte.
   V — `etc`, `var`, `root`, `bin` förskapas tomma 0555. Vakten kräver rootägarskap,
   samma device/inode som före start, skrivskydd och tomhet. Symlänkar, innehåll och
   utbytta kataloger avvisas; inga bindningar av värdens motsvarande kataloger tillkommer.
2. **Exakta Finance-bindningar:** endast följande fem reguljära originalfiler binds
   skrivskyddat och icke-rekursivt till `/finance/<samma filnamn>`:
   `bb-091-pre-migration-20260817.db`,
   `finance-backup-3b093584173d74f6.manifest.json`,
   `finance-backup-3b093584173d74f6.data.json`,
   `finance-backup-8b549915b4e4129c.manifest.json`,
   `finance-backup-8b549915b4e4129c.data.json`.
   Källor och föräldrar hålls med `O_PATH`/`O_NOFOLLOW`; bindningar använder dessa
   fildeskriptorer. Ingen backupkatalog eller lagringsvolym binds i sin helhet.
3. **Kontext:** ny, unik transient systemtjänst med tom rot, privat PID-, mount-,
   IPC- och nätverksnamnrymd; UID/GID 1654, inga grupper utöver primärgruppen,
   samtliga capability-mängder tomma och `NoNewPrivileges`. Nätverkssyscalls nekas;
   värdens journal-sockets binds inte. Endast explicit angiven OS-runtime tillkommer.
   Värdens hem, datakataloger, Docker-socket, driftDB och closure-katalog exponeras inte.
4. **Före läsning:** vakten kontrollerar faktisk identitet, capabilities, namnrymder,
   skrivskydd, exakta filidentiteter och skripthash. Skriptets kontrollerade bytes körs
   senare direkt ur minnet. Före `GO` återläser föräldern tjänstens skyddsinställningar
   och dess fyra cgroup v2-gränser för CPU, minne, swap och antal processer/trådar,
   och kontrollerar originalens metadata och frånvaro av `-wal`, `-shm`, `-journal` igen.
   Kontrollen sker även före start och efter avslut. Avvikelse ger stopp utan nytt försök.
5. **Gränser och resultat:** CPUQuota 100 % (en CPU), MemoryMax 512 MiB,
   MemorySwapMax 0, TasksMax 8, RuntimeMax 300 s, startspärr 30 s, stoppfrist 5 s,
   NOFILE 64, CORE/FSIZE 0. Förälderns resultatväntan är högst 330 s.
   Resultat buffras, begränsas till 256 KiB/80 verifierarrader och tillåtna statusprefix;
   råa undantag och stderr återges inte. Inga resultatfiler skapas. Enbart sanerad stdout,
   exitkod 0/1 och temporärt RESOURCE_ID lämnas till ägaren. Closure ska ge
   `NOT CHECKED closure_directory_unavailable`; det ersätts aldrig med PASS.
6. **Avslut/återkallelse:** normal avslutning, fel eller Ctrl-C går via cleanup av endast
   den nya tjänsten och stagingkatalogen. `KillMode=control-group`, 5 s stoppfrist och
   SIGKILL stoppar kvarvarande barn. Borttagning sker först efter bekräftat avslut;
   endast kända nytillkomna filer/tomma kataloger tas bort, aldrig rekursiv radering.
   Vid obekräftad cleanup behålls resurserna och RESOURCE_ID rapporteras. Efter dödad
   supervisor kan exakt paket anropas med `--cleanup <rapporterat RESOURCE_ID>`;
   inga original, konton, ACL eller produktionsenheter återkallas/ändras.

V — Paketets administrativa del kräver **ägarens befintliga rootbehörighet för just
denna exekvering**: skapa egen staging, läsa/pinna utpekad metadata och skript, skapa
en transient systemtjänst med bindningar/namnrymder/cgroup-gränser, byta till 1654 samt
stoppa och städa egna temporära resurser. Verifieraren kör inte som root. Inga nya
sudoersregler, grupper, användare, ACL eller Docker-privilegier föreslås.

O — kräver att originalen förblir statiska under exekveringen; metadata jämförs före/efter,
men en skrivskyddad bindning hindrar inte en separat värdprocess från att skriva originalet.
O — kräver godkänd exekvering för att fastställa att runtime-bindningarna och alla
systemd-grindar fungerar tillsammans. Paketet stoppar vid oförenlighet; automatisk
fallback, utökad åtkomst eller ändring av verifieraren ingår inte.

## Remaining work

V — Lokal reproduktion utan privilegier:

```sh
python3 -B docs/reports/features/finance/finance-release1-start-package-tests-20261107.py -v
```

V — [Exakt förberett systemd-prov](finance-release1-systemd-probe-20261107.py), SHA-256
`846a385fa9ec9f7d0d0f1e03024f3e53d08e875bbf1bf9a04f5a0c223f948d79`, är **inte kört**.
Det kräver befintlig rootbehörighet för samma isolerade systemtjänst som kandidaten.
Provet låser startpaketets hash och skapar endast egna syntetiska filer i en ny privat
`/run/finance-release1-probe-<id>`; inget argument för Finance-källor finns.
Två fall, högst 300 s vardera: normal avslutning och avsiktligt verifierarfel efter GO.
Andra fallet startar endast om första passerar inklusive cleanup. Den riktiga
Finance-verifieraren körs inte; en syntetisk payload och dess förväntade hash används
endast i testprocessens minne. Kandidatens identitets-/isolerings-/cleanupkod återanvänds.

V — Förväntat: `PASS P03_normal_and_cleanup`, `PASS P03_failure_and_cleanup`,
`PASS synthetic_fixture_removed`, exit 0. Det avsiktliga `FAIL synthetic_probe` i andra
fallet är testindata, inte Finance-evidens. Vid oväntat fel stoppas provet utan nytt försök.
Eventuella kvarvarande barn städas med startpaketets `--cleanup <PROBE_CHILD-id>`;
därefter provets `--package <exakt kandidat> --cleanup-fixture <PROBE_RESOURCE_ID>`.
Fixture-cleanup vägrar om något av de två barnens tjänst/staging återstår; endast kända
nyskapade syntetiska filer och tomma kataloger tas bort.

O — kräver arkitektgranskning av båda exakta hasharna och ett **nytt separat ägarbeslut**
för just det syntetiska administratörsprovet. Det ger ingen Finance-exekveringsauktoritet.
D — Augustikopian kan endast ge historisk snapshot-evidens; den avgör inte dagens
Finance-evidens, driftdatabas, workerhistorik eller reproducerbarhetskrav.

## Resumption

V — Nästa steg är granskning av kandidaten och provet. O — faktisk systemd-funktion
för den nya kandidaten är ännu inte verifierad. Ingen D3, merge, retentionåtgärd eller
driftändring är utförd eller auktoriserad här; det förbrukade godkännandet återanvänds inte.
