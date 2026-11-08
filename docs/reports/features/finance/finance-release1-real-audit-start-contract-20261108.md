# Finance Release 1 – kontrakt för verklig audit-start (förslag)

Detta är en sanerad GitHub-version. V = självständigt verifierat i denna
designetapp; D = dokumenterat eller ägarrapporterat; O = ännu inte verifierat.
Dokumentet är en **review-kandidat**, inte ett godkänt körkommando.

## Metadata

- Datum: 2026-11-08. Risknivå 3, Finance **FROZEN / RESEARCH / 0 SEK / NONE**.
- Baslinje: `main` `93a31d2f31da37e8976feb47012c30eed7384e08`.
- Beslutsgrund: [Finance-frysningen](../../../operations/finance-freeze-20261016.md),
  [AGENTS](../../../../AGENTS.md), [Release 1-mandatet i PR #40](https://github.com/T-bear/BigBrain/pull/40),
  [säkerhetstillägget i PR #42](https://github.com/T-bear/BigBrain/pull/42),
  [syntetisk isolering PR #47](https://github.com/T-bear/BigBrain/pull/47)
  och [syntetiskt SQLite-startprov PR #49](https://github.com/T-bear/BigBrain/pull/49).
  PR #47 och #49 förblir separata, ännu inte mergade kandidater.
- Ägarbeslut i ChatGPT 2026-11-08: gå vidare med nästa **planeringsfas** för
  säkerhetskontraktet. Ingen ny auktorisering av verklig Finance-filläsning,
  adminstart, kopiering, ändrade rättigheter, merge eller deployment.

## Status

**DESIGN READY FOR REVIEW / REAL AUDIT BLOCKED.**
V – GitHub-main och ren arbetskopia kontrollerade; ingen kod eller värdresurs
har ändrats inom denna etapp. Den separata audit-identiteten existerar enligt
oprivilegierad hostkontroll, med nologin och endast sin primärgrupp.

D – Ägarens avgränsade metadatautskrift visade fem vanliga, enkellänkade
filer: en äldre databas med root som ägare, fyra backup-/manifestfiler
ägda av det historiska Finance-kontot, samtliga 0644, och inga
SQLite-sidofiler för den utpekade databasen vid kontrolltillfället.
Föräldrarna till backupfilerna är delvis skrivbara av den historiska
identiteten. Katalogkedjan kan inte traverseras direkt av audit-identiteten.
Dessa fakta visar **inte** att materialet är oföränderligt.

O – Faktiskt hash-/SQLite-innehåll, provider-/retentionsstatus,
samtidig källmodifiering och säker verklig audit-start har inte verifierats.

## Evidence

V – PR #47:s syntetiska bwrap-prover och PR #49:s syntetiska
SQLite-prover har oberoende granskats för **endast syntetiskt scope**.
PR #49 har verifierat negativa mutationer och har fyra gröna CI-jobb
för exakt kandidat enligt separat GitHub-kontroll; detta är inte host-PASS
för en verklig audit-process.

D – Den äldre verifieraren i [PR #40](https://github.com/T-bear/BigBrain/pull/40)
har en hårdkodad *annan* systemidentitet. Den får inte återanvändas
oförändrad för den separata audit-identiteten. Dess verifierade bytes och
tidigare hashbeslut ska bevaras, inte tyst patchas.

## Changes

Endast detta sanerade säkerhetskontrakt föreslås som en ny dokumentationsfil.
Inga ändringar i syntetiska PR #47/#49, produktionskod, databas,
användare, ACL, tjänster, systemd eller Docker.

## Security

### Säkerhetsmål och vald minsta arkitektur

**Välj ett enda kortlivat offlinejobb med befintlig dedikerad audit-UID,
Bubblewrap och en enda exakt avgränsad, verifierat statisk filuppsättning.**
Ingen permanent daemon, nytt konto, global Docker-/sudo-behörighet,
återaktiverad specialbyggd systemd-startare eller ny köinfrastruktur.

1. **Ägarkontrollerad indata:** exakt fem utpekade artefakter, utan andra
   databasvolymer, tokens eller hemligheter. Källvägarnas innehåll läses
   inte under designfasen. En `--ro-bind` gör den monterade vyn skrivskyddad
   men gör **inte** den underliggande källfilen statisk om dess ägare
   fortfarande kan ändra eller byta ut den.
2. **Statisk proveniens före `immutable=1`:** före en verklig körning krävs
   rättighets- och retentionklarerad, konsekvent och **ändringsskyddad**
   uppsättning, med godkända källhashar, manifestrelation, filidentiteter,
   sidofilskontroll, verifierat SQLite-innehåll och ny kontroll efteråt.
   Nuvarande 0644-filer under skrivbara källföräldrar räcker inte.
   Om befintlig immutabilitet inte kan bevisas är alternativet en särskilt
   auktoriserad, tidsbegränsad **skyddad kopia/staging** med explicit
   ägarbeslut om retention, plats och disposition, eller stopp.
3. **Säker staging – endast design:** om godkänd ska administratören
   begränsa kopieringskällor och symlink-/hardlink-/mount-race, staga exakt
   allowlist till privat, icke-värdskrivbar plats, därefter sätta
   root-kontrollerade katalog- och filrättigheter där enbart audit-gruppen
   får läsa. Stage får inga sidofiler och inget skrivbart bind för audit.
   Kopior får **aldrig** automatiskt tas från eller återskrivas till drift-DB.
4. **Minsta startväg:** före start säkras exakt granskad launcher och en
   ny, exakt granskad verifierare med SHA-256 och versionsbundna bytes.
   En eventuell administrativ setup är ett ägarövervakat engångssteg;
   den exekverbara verifieraren måste köras utan root, sudo, extra grupper,
   Docker-socket, miljöhemligheter, provideranslutningar eller handelsrätt.
   Verifiera verklig/effectiv UID/GID och avslagna capabilities **inne**
   i den faktiska sandboxen. Ingen interaktiv shell-åtkomst behövs.
5. **Processegenskaper:** minimalt filsystem med endast verifierade
   artefakter och kända runtimebinarier; faktiskt `ro` i mountinfo,
   isolerad nätverksnamnrymd, `NoNewPrivs=1` och kontrollerat FD-arv.
   Sätt tids-, minnes-, process- och CPU-gränser samt definiera tydlig
   signal-/timeout-/processgruppsterminering inklusive SIGKILL-städning.
   Negativa hosttester under audit-identiteten måste visa att andra
   originaldatabaser, hemligheter och icke-allowlist-filer inte är läsbara.
6. **Verifiering:** verifiera byteidentitet för startskript och kontrollera
   rättigheter och målfilernas identitet före och efter bind/start.
   Öppna SQLite först när statiskt underlag styrkts; använd därefter
   `mode=ro`, `query_only` och lämplig SQL-authorizer.
   `immutable=1` kräver separat motiverad statisk konsistens.
   Avvikelse, oväntad sidofil eller otillgänglig evidens stoppar utan
   nytt automatiskt försök.
7. **Sanerad rapport och återkallelse:** registrera identiteter,
   käll-/skriptfingeravtryck, kontroller, tydliga PASS/FAIL/NOT CHECKED
   och eventuell disposition utan datavärden eller privata sökvägar.
   Vid avbrott ska endast audit-processen och dess privata, uttryckligen
   godkända staging beröras. Ingen städning av original, befintliga
   backuper, tjänster eller orelaterat material.

### Öppna avvägningar / stoppgrindar

| Grind | Status | Kräver före verklig körning |
| --- | --- | --- |
| Syntetisk bwrap och SQLite | V: PASS i PR #47/#49 | Återanvänd evidens; inga fler rutinvarv |
| Audit-identitet | V: konto finns | O: kör-/isoleringsprov med exakt identitet |
| Befintliga källfiler | D: metadata, ingen sidofil då | O: konsistent immutabilitet, hash, proveniens |
| Läsbar statisk staging | O: saknas | Separat beslut vid ny kopia/retention |
| Launcher + verifierare | O: ingen verklig kandidat | Exakt SHA/tree, negativ hostreview, R1–R6 |
| Privilegierad setup | Ej auktoriserad | Nytt avgränsat ägarbeslut |
| BB-132K-disposition | Ägarreserverad | Beslut senast 2026-11-10 kl. 14.36.45 svensk tid |

## Remaining work

Nästa utvecklingsuppgift, **efter review av detta kontrakt**, är att
specificera exakt verifierar-/launcher-kandidat och tester med enbart
syntetisk filuppsättning under separat audit-identitet; inget tidigare
godkännande av PR #47/#49 gäller automatiskt nya bytes.

**Nödvändiga separata beslut före verklig Finance-data:**
om skyddad kopia alls får skapas, rättighets-/retentionsvillkor och
disposition, eventuell avgränsad administrativ engångssetup och till sist
exakt auktoriserad data-/verifieringskörning efter oberoende nivå 3-review.
Kapacitet/kostnad och faktisk proveniens är ännu inte verifierade.

## Resumption

Detta kontrakt är **review only**; ingen exekverbar verklig startväg skapas.
Definition of Done för planeringsfasen är begripliga verifierbara grindar,
uttryckliga stoppvillkor, oförändrade säkerhetskrav och en separat draft-PR.
Ingen automatisk fortsättning när dokumentationen är granskad.
Finance-frysningen, RESEARCH / 0 SEK / NONE och BB-132K-fristen kvarstår.
