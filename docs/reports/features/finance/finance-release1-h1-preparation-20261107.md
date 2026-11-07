# H1: historisk evidensintegritet – granskningspaket 2026-11-07

**V – Förberett och syntetiskt testat, inte kört på servern.** H1 avser endast
augustikopians sex BB-084-resultat. **O – kräver:** faktisk verifiering för att
bedöma evidensintegriteten. Release 1, dagens DB och D3 är inte godkända genom detta paket.

## Beslut och avgränsning

**D – Ägaren beställde förberedelse av den förenklade vägen efter Claudes granskning.**
Arkitektbedömning: systemd-vägen lämnas som misslyckat historiskt experiment; inga fler
serverprov beställs för den. `226/NAMESPACE` har ingen styrkt exakt orsak.
Tidigare utfall och cleanup-uppgifter bevaras i [startpaketsrapporten i PR #40](https://github.com/T-bear/BigBrain/blob/90d45d03f11df8dce946a7fe3ccf19bbc1602309/docs/reports/features/finance/finance-release1-start-package-20261106.md).

**V – Baslinje vid förberedelsen:** main `0f11890dac3ff1529c83d7438658b90e4a61948e`,
historisk checkpoint i PR #40: `90d45d03f11df8dce946a7fe3ccf19bbc1602309`. GitHub-integrationen
nekade publicering med HTTP 403. H1-patchen förbereds därför för ägarens uttryckliga
PUBLICERA-flöde på en separat dokumentationsgren; PR #40 bevaras. Endast nya reviewartefakter
under denna rapportkatalog förbereds; ingen produktionskod eller godkänd mandattext ändras.
Serverns working tree har inte inspekterats här.

## Föreslaget avgränsat ägarbeslut – ännu inte godkänt

> För H1 tillåter jag en enda ägarlokal administrativ export av de fem angivna,
> befintliga backupfilerna till den nya evidenskatalogen. Ordinarie UID 1000 får
> ersätta mandatets dedikerade läsidentitet endast mot dessa rootägda kopior,
> utan sudo/root för verifieringen. Katalog och filer får endast läsas av root
> samt den befintliga privata gruppen GID 1000, vars lokala kontomedlemskap
> kontrolleras före export och verifiering. Inga konton, grupper eller ursprungsrättigheter ändras.
> Kopiorna, även ofullständiga, ingår i min dispositionsinventering. Min beslutspunkt
> för fortsatt disposition är senast 2026-11-09 kl. 20.00 svensk tid, eller tidigare
> om källvillkoren kräver det. Ingen radering är godkänd. Ett fel stoppar utan nytt försök.

**Bedömning –** detta är ett föreslaget undantag till mandatets punkt 5 och ett nytt
kopierings-/retentionsbeslut enligt punkt 6. Befintligt mandat ändras inte retroaktivt.
Läsgränsen ovan är ett stopp i verifieraren; Unix-rättigheter upphör inte automatiskt vid ett klockslag.
Ägaren måste hantera kopiornas disposition separat, även vid misslyckad export.

## Exakt beteende och evidensgräns

| Del | Implementerat beteende (V: länkad kod) |
| --- | --- |
| [Export](finance-release1-h1-export-20261107.py) | Fast lista: BB-091:s augusti-DB och de två BB-085-manifest-/dataparen. Kontrollerar historisk DB-hash och fullständiga manifestbundna datahashar före kopiering, samt kopians hash efteråt. Vägrar sidofiler, symlänkar, befintlig destination och otillräckligt utrymme. |
| Åtkomst | Ny rootägd katalog, slutläge 0550 root:GID1000; filer 0440. Kontrollerar skyddade föräldrar och avsaknad av utökade ACL:er. Fel före publicering lämnar eventuell delkopia rootprivat 0700. Inget systemd-/namespace-/stagingmaskineri. |
| [Verifierare v1.1](finance-release1-h1-verify-20261107.py) | UID1000, exakt evidenskatalog, rättigheter, lokalt gruppmedlemskap och inventering kontrolleras. Befintliga datakontroller bevaras: full DB-hash, `mode=ro&immutable=1`, `query_only`, SQL-authorizer, konfiguration, resultatchecksumma och underposter. |
| [Samlat ägarblock](finance-release1-h1-owner-20261107.sh) | Kräver ren review-arbetskopia och UID1000; hashkontrollerar export/verifierare och exekverar samma kontrollerade bytes. Ett sudo-steg för kopiorna, därefter vanlig UID1000. Stoppar vid fel. Endast för granskning tills exakt paket och undantag godkänts. |

**V –** Högst cirka 2,36 GB nytt filinnehåll plus inventeringsnot; ytterligare 256 MiB
utrymmesmarginal krävs. Inga provideranrop, modell-/forskningskörningar, migrationer,
driftåtgärder, originalskrivningar eller raderingar finns i den nya vägen.
Källfiler öppnas med `O_NOATIME`. Metadata kontrolleras före/efter; detta är inte en atomär snapshot
eller ett skydd mot samtidig privilegierad ändring. Skrivskydd ersätter inte en generell sandbox.
Gruppkontrollen avser lokala kontofiler; den certifierar inte externa identitetstjänster.

**O – kräver:** oberoende kandidatgranskning samt exakt ägarbeslut ovan före serverexekvering.
Faktiskt grupp-/filåtkomstläge och källintegritet på servern är inte verifierade här.

## Tester och kvarstående krav

**D –** Ägarens första publiceringsförsök stoppade i payloadkontrollen, före repoändring.
Det tidigare chattblocket innehöll en skadad kodsträng. Ett filbaserat, hashkontrollerat
terminalblock ersätter den leveransen; ingen ny Finance-exekvering har skett.

**V –** [Kärnfixturen](finance-release1-h1-core-tests-20261107.py): 71 PASS / 0 FAIL /
6 NOT CHECKED; 26 negativa fall avvisade. [Åtkomst-/exporttester](finance-release1-h1-access-tests-20261107.py):
21 PASS, inklusive kopiering, hashfel, källändring, delkopia, extra filer, rättigheter,
utgången lästid och stopp utan återförsök. Identitet, chown och skyddade föräldrar simuleras
i dessa lokala tester. Faktiska serverrättigheter är inte testade. Syntaxkontroll PASS.
AST-jämförelse: alla ursprungliga datafunktioner/modeller oförändrade; endast körningens miljögrind ändrad.

**Bedömning –** H1 kan styrka augustikopians lagrade integritet och lineage för sex körningar.
Det bevisar inte dagens tillstånd, engine-replay, statistisk fördel eller självlärande.
Source-/feature-checksummornas numeriska beräkning, fills ordningsordinal och closure förblir
deklarerade begränsningar. Hashfel stoppar denna kopieprövning; det avgör inte automatiskt D3.

**D/O – BB-132K:** senast rapporterad frist 2026-11-10 kl. 14.36.45 svensk tid, eventuellt tidigare.
Aktuell disposition är okänd och ska beslutas oberoende av H1. Detta paket förlänger ingen frist.

## Parkerat

- **O –** exakt orsak till systemd-felet; inga fler serverprov inom H1.
- **O –** dagens schema/backup/driftläge, full Release 1-DoD och eventuell separat D3-prövning.
