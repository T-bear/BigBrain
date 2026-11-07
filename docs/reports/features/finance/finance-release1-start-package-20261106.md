# Finance Release 1 — startpaket för oberoende granskning

Detta är en sanerad GitHub-version. V = verifierat i denna avstämning;
D = dokumenterat/ägarrapporterat; O = okänt. Paketets föreslagna beteende är
kontrollerat med syntetiska filer utan privilegier. Verklig systemd-isolering är inte
verifierad för den nya kandidaten.

## Metadata

V — 2026-11-07: accepterad GitHub-main
`0f11890dac3ff1529c83d7438658b90e4a61948e`; PR #40:s utgångspunkt
`a9f68f98e9c83baf3b2b9a938238b159ad2e93ac`, rent checkpoint-working-tree före diagnostikändringen.
V — Samma checkpoint används; orelaterade filer i ordinarie checkout lämnas orörda.
D — Ägaren har rapporterat metadataavstämning `problems=0`; råresultatet har inte omverifierats.

## Status

D — Ägaren rapporterar ett försök med kvarvarande `etc`, `var`, `root`, `bin` och
därefter ägarlokal `PASS temporary_resources_removed` för
`0e06762315a143aca87da36e3ab48dc1`. Cleanup är inte omverifierad av Codex.
**D — Det tidigare engångsgodkännandet är förbrukat.**
D — Det efterföljande ägarlokala syntetiska provet rapporterade:

```text
FAIL G06_empty_root
PASS temporary_resources_removed
FAIL P02_normal
PASS synthetic_fixture_removed
```

D — Normalfallet avvisades; båda cleanup-stegen rapporterades PASS. Ingen lyckad
systemd-verifiering påstås. V — Koden når G06 före READY/GO och startar inte andra
provfallet när normalfallet misslyckas. O — kräver mer preciserad diagnostik för att
fastställa det faktiskt brustna G06-delvillkoret; ägarens fyra rader särskiljer det inte.
V — Den nya kandidaten ändrar endast diagnostik, tester, rapport och provets hashbindning.
Ingen Finance-dataåtkomst, administratörsexekvering eller verklig systemd-start har
gjorts av Codex i denna etapp. D — Ingen ny rootexekvering är godkänd.

## Evidence

| Kontroll | Evidens och begränsning |
| --- | --- |
| V — Oförändrad verifierare | SHA-256 `7c065708528a10a0d3f0408e7de5fd4af4b5d93cf43ecae1c5105829de8fb9bb`, samma bytes som den [godkända artefakten](https://github.com/T-bear/BigBrain/blob/029ef0d9d96ddcabff80649138d5f2dd0272c55e/docs/reports/features/finance/finance-release1-verify-readonly-20261106.py). |
| V — Nytt startpaket | SHA-256 `44ad98e08d850d22e994faf3bd08613d0e6d46034695b3010d7fcfd0e9a99997`. Föregående kandidat `959c9f6ee28492ec9f96d8f9e1f74c65e664d1a334842b430b760d8cc170da73` finns kvar i Git-historiken. |
| V — Bevarad cleanup-rättning | Den historiska bristen i `82a73a8` är rättad sedan `a9f68f9`: de fyra tomma katalogerna ingår och okända poster blockerar borttagning. Cleanup-koden ändras inte i denna kandidat. |
| V — Syntetisk regression | [17 tester PASS](finance-release1-start-package-tests-20261107.py), inklusive 40 G06-felfall: tio delvillkor/operationer för vart och ett av fyra katalog-ID:n. Kontrollerar exakt felkod genom förälderns protokoll och avvisning av otillåtna identifierare. Befintliga normal-/felcleanup-tester passerar. Systemd, ägare/UID och mountskydd simuleras; filoperationer sker endast i temp-fixturer. Orelaterad sentinel bevaras. |
| D/O — OS-runtime | Föregående avstämning: Python 3.13.5/systemd 257.13. O — kräver separat godkänt systemd-prov för faktisk namespace-, DAC/ACL- och cgroup-funktion. Syntetiska tester bevisar inte dessa skydd. |
| D/O — Datakopia | Augustikopia 2026-08-17 och manifest-/datafiler från 2026-08-15/16 enligt befintligt underlag. O — kräver verifierarens faktiska hash-, lineage- och internkonsistenskontroller för resultat om innehållet. Ägarens metadatautfall bevisar inte innehållsintegritet. |

## Changes

V — G06 delas upp utan att sänka något villkor. Verifieraren, tjänstens inställningar,
bindningar, resursgränser och cleanup är oförändrade. Provets enda kodändring är hashbindningen.
V — `G00–G11` identifierar vaktens steg, `S01–S14` förälderns steg och `C00–C07`
cleanupsteg. G06-formatet är `G06_empty_<katalog-ID>_<delvillkor>` med fasta ID:n
`etc`, `var`, `root`, `bin`. Suffixen betyder:

| Suffix | Brist |
| --- | --- |
| V — `lstat`, `statvfs`, `listdir` | Respektive metadata-/katalogoperation misslyckades; ingen rå feltext återges. |
| V — `type`, `owner`, `mode` | Inte katalog, inte rootägd eller mode inte exakt 0555. |
| V — `device`, `inode` | Respektive identitetsfält avviker från förskapad katalog. |
| V — `readonly`, `empty` | `ST_RDONLY` saknas respektive katalogen innehåller poster. |

V — `G06_spec` avser ogiltig kontrollspecifikation. Föräldern accepterar endast de fasta
koderna; inga modes, inoder, filnamn, sökvägar eller råa undantag lämnar diagnostiken.

V — Undersökt upstream-kod **systemd v257.13**, inte serverns körande binär:
[namespace.c, protect_system_strict_table](https://github.com/systemd/systemd/blob/v257.13/src/core/namespace.c#L217-L230)
anger `/root` som `MOUNT_READ_WRITE_IMPLICIT` och lämnar hemskydd åt `ProtectHome`.
`ProtectSystem=strict` ensamt styrker därför inte readonly-kravet för `/root`.
[base-filesystem.c](https://github.com/systemd/systemd/blob/v257.13/src/shared/base-filesystem.c#L139-L198)
hoppar över befintliga poster; när `root` saknas skapas den enligt tabellen med 0750.
Den funktionen visar ingen omchmodning av vår redan förskapade 0555-katalog.
Bedömning utifrån koden: readonly är en konkret möjlig förklaring till provutfallet.
O — kräver faktisk delvillkorskod för att fastställa att detta var serverns fel.
V — Kandidaten tar inte bort readonly-, mode-, identitets- eller tomhetskontrollen och
ändrar inte mount-konfiguration på denna bedömning. Explicit readonly för `/root`
är en möjlig separat skyddskorrigering för arkitektens bedömning, inte utförd här.

## Security

V — [Skyddskontraktet från föregående kandidat](https://github.com/T-bear/BigBrain/blob/a9f68f98e9c83baf3b2b9a938238b159ad2e93ac/docs/reports/features/finance/finance-release1-start-package-20261106.md#security)
gäller oförändrat: rootskyddad skriptstaging, UID/GID 1654 utan extra grupper eller
capabilities, privat rot/PID/mount/IPC/nätverk och nekade nätverkssyscalls. Endast fem
utpekade Finance-filer och explicit OS-runtime binds. Originalens sidofiler kontrolleras
före start, före GO och efter avslut; ingen källrättighet ändras. Closure är oexponerad.
V — Gränser: 300 s, en CPU, 512 MiB, ingen swap, åtta tasks, fem sekunders stoppfrist.
Cleanup tar endast egna kända temporära resurser; okänt innehåll bevaras. Sanerad stdout
har fasta statusprefix; felvillkor ger inte extra åtkomst eller automatiskt nytt försök.
O — kräver separat godkänd runtime-verifiering för skyddens faktiska funktion. Original
måste vara statiska; skrivskyddade bindningar förhindrar inte andra värdprocessers skrivning.

## Remaining work

V — Lokal reproduktion utan privilegier:

```sh
python3 -B docs/reports/features/finance/finance-release1-start-package-tests-20261107.py -v
```

V — [Förberett systemd-prov](finance-release1-systemd-probe-20261107.py), SHA-256
`fa73aff7b0b40168dab51e82d5a7ddba693d7b97d4316f068603a2f342d35fb4`, är **inte kört**
för denna kandidat. Endast låsningen till nytt startpaket har ändrats. Det tidigare
provets ägarrapporterade utfall står under Status; tidigare engångsgodkännande är förbrukat.
V — Provet har endast egna syntetiska filer, två villkorade fall och befintlig avgränsad
cleanup. Det kör aldrig Finance-verifieraren. Normalfallet måste passera inklusive
cleanup innan det avsiktliga verifierarfelet provas. Exakt test- och återhämtningskod
finns i den länkade artefakten; ingen ny administratörsexekvering är auktoriserad.
O — kräver arkitektgranskning och nytt separat ägarbeslut före varje nytt administratörsprov.
D — Augustikopian kan endast ge historisk snapshot-evidens, inte dagens drifttillstånd.

## Resumption

V — Nästa steg är granskning av kandidaten och provet. O — faktisk systemd-funktion
för den nya kandidaten är ännu inte verifierad. Ingen D3, merge, retentionåtgärd eller
driftändring är utförd eller auktoriserad här; det förbrukade godkännandet återanvänds inte.
