# Finance Release 1 — startpaket för oberoende granskning

Detta är en sanerad GitHub-version. V = verifierat i denna avstämning;
D = dokumenterat/ägarrapporterat; O = okänt. Paketets föreslagna beteende är
statiskt granskat i källtext, inte verifierat genom exekvering.

## Metadata

V — 2026-11-06: accepterad GitHub-main
`0f11890dac3ff1529c83d7438658b90e4a61948e`; PR #40:s utgångspunkt
`bf3174173e661555dd40881045b152ae29e9e25b`, rent checkpoint-working-tree före arbetet.
V — Samma checkpoint används; orelaterade filer i ordinarie checkout lämnas orörda.
D — Ägaren har rapporterat metadataavstämning `problems=0`; råresultatet har inte omverifierats.

## Status

V — [Komplett startpaket](finance-release1-start-package-20261106.py) är förberett,
**inte kört**. Ingen administratörsexekvering, Finance-dataläsning eller verifierarkörning
har gjorts i denna etapp. D — Arkitekten har godkänt principen för
[åtkomst-handoffen](finance-release1-access-handoff-20261106.md), inte detta nya paket.
O — kräver oberoende granskning av exakt paket och därefter ägarens separata
exekveringsgodkännande. Mandatets åtkomststopp kvarstår för Codex.

## Evidence

| Kontroll | Evidens och begränsning |
| --- | --- |
| V — Oförändrad verifierare | SHA-256 `7c065708528a10a0d3f0408e7de5fd4af4b5d93cf43ecae1c5105829de8fb9bb`, samma bytes som den [godkända artefakten](https://github.com/T-bear/BigBrain/blob/029ef0d9d96ddcabff80649138d5f2dd0272c55e/docs/reports/features/finance/finance-release1-verify-readonly-20261106.py). |
| V — Startpaketets granskningsfingeravtryck | SHA-256 `e0f5cd99071e8266a2ef0055b1ab5f6ca71cad85ef95a924a57a35447ad9c45b`. Detta är en ny artefakt och omfattas inte av verifierarens tidigare godkännande. |
| V — Statisk kontroll | Python AST för paket och inbäddad identitets-/isoleringsvakt går att tolka. Ingen import eller exekvering av paketet gjordes. |
| V — Befintlig OS-runtime | Python 3.13.5, systemd 257.13; exakta runtime-bindningar anges i konstanten `RUNTIME`. Lokala manualsidor beskriver `PrivatePIDs`, `BindReadOnlyPaths` och övriga systemd-gränser. O — kräver en separat godkänd exekvering för att verifiera kombinationen med kernel, systemd och befintliga DAC/ACL. |
| D/O — Datakopia | Augustikopia 2026-08-17 och manifest-/datafiler från 2026-08-15/16 enligt befintligt underlag. O — kräver verifierarens faktiska hash-, lineage- och internkonsistenskontroller för resultat om innehållet. Ägarens metadatautfall bevisar inte innehållsintegritet. |

## Changes

V — Endast nytt granskningspaket, denna rapport och kataloglänk. Verifieraren,
produktionskod, säkerhetsundantag och driftkonfiguration ändras inte.

## Security

V — Följande skydd är uttryckta i paketets kod; **runtime-PASS påstås inte**:

1. **Staging:** administratören läser endast verifierarens innehåll och kontrollerar
   dess godkända hash. En exklusivt skapad katalog `/run/finance-release1-<RESOURCE_ID>`
   är rootägd 0700. Endast skriptet kopieras, root:1654/0440; övriga skapade filer är
   tomma monteringspunkter. Källornas rättigheter ändras inte.
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

O — kräver arkitektgranskning av paketets ovanstående hash och ägarens separata
godkännande av administratörsexekveringen, inklusive cleanup av egna temporära resurser.
D — Augustikopian kan endast ge historisk snapshot-evidens; den avgör inte dagens
Finance-evidens, driftdatabas, workerhistorik eller reproducerbarhetskrav.

## Resumption

V — Paketet lämnas för granskning; inga runtime-resultat är tillgängliga. Exakta privata
källsökvägar lämnas endast i ägarens terminalblock, inte i det publicerade paketet.
Ingen D3, merge, retentionåtgärd eller driftändring är utförd eller auktoriserad här.
