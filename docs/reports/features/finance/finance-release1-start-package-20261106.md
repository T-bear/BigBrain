# Finance Release 1 — startpaket för oberoende granskning

Detta är en sanerad GitHub-version. V = verifierat i denna avstämning;
D = dokumenterat/ägarrapporterat; O = okänt. Paketets föreslagna beteende är
kontrollerat med syntetiska filer utan privilegier. Ägarlokalt systemd-prov av
kandidaten `013125d` har rapporterat FAIL. Den nya kandidaten är endast för granskning;
`InaccessiblePaths` har en kvarstående målsemantisk lucka.

## Metadata

V — 2026-11-07: accepterad GitHub-main
`0f11890dac3ff1529c83d7438658b90e4a61948e`; PR #40:s utgångspunkt
`3a72ef05dfeacac9441fce272d3bd907d5452dfc`, tree
`0efbdea95b67ea2cace3a75532b753205f3ed6a7`, rent checkpoint-working-tree före
denna korrigering. Finance-risknivå 3; inget exekverings- eller mergegodkännande.
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
provfallet när normalfallet misslyckas. D — Det äldre utfallet särskilde inte
delvillkoret; följande nya prov rapporterar det explicit.
D — Föregående beslut gav `ReadOnlyPaths=/root`. Det nya uppdraget preciserar
`+/root`, systemds återläsningsrepresentation, regression mot det gamla värdet samt
granskning/redovisning av målsemantiken för `InaccessiblePaths`.
V — Ingen Finance-dataåtkomst, administratörsexekvering eller verklig systemd-start har
gjorts av Codex i denna etapp. D — Ägaren rapporterar 2026-11-07 följande nya utfall
för provets SHA-256 `b9092c6a3bf39600087f11e4eee1450a183b4bdb2c30a8bb5c2be8ebb8d73679`:

```text
PROBE_RESOURCE_ID fe5c198e991442cd89928593bda72289
PROBE_CHILD_NORMAL 5cc242d331ec6d614c2fa3dbc3434d8d
PROBE_CHILD_FAILURE 6d05d7f7b5d15009e98fb29743afe158
RESOURCE_ID 5cc242d331ec6d614c2fa3dbc3434d8d
FAIL G06_empty_root_readonly
PASS temporary_resources_removed
FAIL P02_normal
PASS synthetic_fixture_removed
```

D — Normalfallet misslyckades och båda cleanup-stegen rapporterades PASS; det är inte
oberoende hostverifiering. V — I oförändrad kod betyder felsteget att `statvfs`
returnerade utan `ST_RDONLY` för gästens rootkatalog. Typ, ägare, mode, device/inode
kontrolleras dessförinnan; tomhet kontrolleras efteråt och nåddes inte för katalogen.
V — Stoppet sker före READY, S09:s återläsning och GO. Det avsiktliga felprovet startas
inte efter detta normalfallsfel; dess utskrivna ID bevisar inte körning. D — Ingen ny
exekveringsauktorisering följer av resultatrapporten. Startpaketet kvarstår blockerat.

## Evidence

| Kontroll | Evidens och begränsning |
| --- | --- |
| V — Oförändrad verifierare | SHA-256 `7c065708528a10a0d3f0408e7de5fd4af4b5d93cf43ecae1c5105829de8fb9bb`, samma bytes som den [godkända artefakten](https://github.com/T-bear/BigBrain/blob/029ef0d9d96ddcabff80649138d5f2dd0272c55e/docs/reports/features/finance/finance-release1-verify-readonly-20261106.py). |
| V — Nytt startpaket | SHA-256 `449b5045555df72430771d65c3434800e8d0cb53d5a11a14e10a9947a3fa9201`. Föregående kandidat `bf2815aa2260a33cceea43033d4028fa730312d7864c1ac08c842ef361227aef` finns kvar i Git-historiken. |
| V — Bevarad cleanup-rättning | Den historiska bristen i `82a73a8` är rättad sedan `a9f68f9`: de fyra tomma katalogerna ingår och okända poster blockerar borttagning. Cleanup-koden ändras inte i denna kandidat. |
| V — Syntetisk regression | [20 tester PASS](finance-release1-start-package-tests-20261107.py), inklusive 40 G06-felfall: tio delvillkor/operationer för vart och ett av fyra katalog-ID:n. Kontrollerar exakt felkod genom förälderns protokoll och avvisning av otillåtna identifierare. Det värdrelativa `/root` samt saknad/felaktig återläst `ReadOnlyPaths` ger `FAIL S09_service_properties`, inget GO och lyckad cleanup. Normalfallet kräver återläsning före GO; bindningarna får inte exponera värdens rootkatalog. Befintliga normal-/felcleanup-tester passerar. Systemd, ägare/UID och mountskydd simuleras; filoperationer sker endast i temp-fixturer. Orelaterad sentinel bevaras. |
| D/O — OS-runtime | Föregående avstämning: Python 3.13.5/systemd 257.13. O — kräver separat godkänt systemd-prov för faktisk namespace-, DAC/ACL- och cgroup-funktion. Syntetiska tester bevisar inte dessa skydd. |
| D/O — Datakopia | Augustikopia 2026-08-17 och manifest-/datafiler från 2026-08-15/16 enligt befintligt underlag. O — kräver verifierarens faktiska hash-, lineage- och internkonsistenskontroller för resultat om innehållet. Ägarens metadatautfall bevisar inte innehållsintegritet. |

## Changes

V — Två värden ändras från `/root` till `+/root`: tjänsteinställningen och S09:s
exakta återläsningskrav före GO. G06:s `ST_RDONLY`, ägare, mode, device/inode och tomhet
är oförändrade, liksom verifieraren, bindningarna, resursgränserna och cleanup.
Provets enda kodändring är hashbindningen. Inga nya bindningar eller källrättigheter.

V — I upstream **systemd v257.13** bevarar
[dbus-execute.c](https://github.com/systemd/systemd/blob/v257.13/src/core/dbus-execute.c#L3103-L3149)
`+` när sökvägen förenklas och lagras; ReadOnlyPaths exponeras som stränglista.
[bus-print-properties.c](https://github.com/systemd/systemd/blob/v257.13/src/shared/bus-print-properties.c#L224-L257)
skriver dessa värden via shell-citering;
[escape.h](https://github.com/systemd/systemd/blob/v257.13/src/basic/escape.h#L11-L20)
kräver inga citattecken för `+/root`. Kravet är därför exakt `+/root`, inte `/root`
eller en värdsökväg med stagingprefix. Ingen tolerant fallback tas in.
O — kräver separat godkänt prov för faktisk representation på servern; avvikelse stoppar S09.

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

D — Tidigare undersökt upstream-kod **systemd v257.13**, inte serverns körande binär:
[namespace.c, protect_system_strict_table](https://github.com/systemd/systemd/blob/v257.13/src/core/namespace.c#L217-L230)
anger `/root` som `MOUNT_READ_WRITE_IMPLICIT` och lämnar hemskydd åt `ProtectHome`.
`ProtectSystem=strict` ensamt styrker därför inte readonly-kravet för `/root`.
[base-filesystem.c](https://github.com/systemd/systemd/blob/v257.13/src/shared/base-filesystem.c#L139-L198)
hoppar över befintliga poster; när `root` saknas skapas den enligt tabellen med 0750.
Den funktionen visar ingen omchmodning av vår redan förskapade 0555-katalog.
D — Tidigare kodbedömning: readonly är en konkret möjlig förklaring till provutfallet.
D — Det nya ägarrapporterade provet identifierar readonly-delvillkoret.
O — kräver separat tillåten runtime-evidens för exakt orsak i serverns mountuppsättning.
V — Kandidaten behåller ägare, mode, device/inode, readonly och tomhet som separata
vaktkrav. Den nu uttryckligen auktoriserade skyddskorrigeringen är förberedd i kod.
O — kräver separat godkänt systemd-prov för att styrka det faktiska mountskyddet;
återläst tjänsteinställning ersätter inte G06:s kontroll av `ST_RDONLY`.

## Security

V — [Skyddskontraktet från föregående kandidat](https://github.com/T-bear/BigBrain/blob/a9f68f98e9c83baf3b2b9a938238b159ad2e93ac/docs/reports/features/finance/finance-release1-start-package-20261106.md#security)
beskriver de kvarstående kodvillkoren: rootskyddad skriptstaging, UID/GID 1654 utan extra grupper eller
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
`06aa3fb32327fd71220560269067898f621c2fea0aa4c1b0aeaa03e580b60268`, är inte kört.
D — Föregående provs rapporterade körning står under Status; resultatet är FAIL, inte
godkänd isolering. Tidigare engångsgodkännande får inte återanvändas.
V — Provet har endast egna syntetiska filer, två villkorade fall och befintlig avgränsad
cleanup. Det kör aldrig Finance-verifieraren. Normalfallet måste passera inklusive
cleanup innan det avsiktliga verifierarfelet provas. Exakt test- och återhämtningskod
finns i den länkade artefakten; ingen ny administratörsexekvering är auktoriserad.
O — kräver arkitektgranskning och nytt separat ägarbeslut före varje nytt administratörsprov.
D — Augustikopian kan endast ge historisk snapshot-evidens, inte dagens drifttillstånd.

V — Följande uppdaterade terminalblock är **endast för granskning**, inte exekverat.
Kvarstående lucka nedan måste hanteras före godkännande. Det förutsätter
checkpointens reporot som arbetskatalog. Blocket kontrollerar provets hash och kör samma
inlästa bytes; provet kontrollerar i sin tur startpaketets hash ovan före staging.
Administratörsprivilegiet är värdens root för temporär systemd-tjänst, isolering,
cgroups och egna staging-/cleanup-resurser. Inga bestående behörigheter tillkommer.

```sh
sudo -- /usr/bin/python3 -I -S -B - \
  docs/reports/features/finance/finance-release1-systemd-probe-20261107.py \
  docs/reports/features/finance/finance-release1-start-package-20261106.py <<'PY'
import hashlib
import sys

expected = "06aa3fb32327fd71220560269067898f621c2fea0aa4c1b0aeaa03e580b60268"
try:
    probe_path, package_path = sys.argv[1:]
    with open(probe_path, "rb") as stream:
        source = stream.read(262145)
    if len(source) > 262144 or hashlib.sha256(source).hexdigest() != expected:
        raise ValueError()
except Exception:
    print("FAIL probe_hash_or_arguments")
    raise SystemExit(1)

sys.argv = [probe_path, "--package", package_path]
exec(compile(source, probe_path, "exec"),
     {"__name__": "__main__", "__file__": probe_path})
PY
```

## Resumption

V — **Kvarstående skyddslucka:** befintliga `InaccessiblePaths=/sys /run` är värdrelativa,
inte relativa till gästens `RootDirectory`. D — Samma prefixregel gäller enligt
[systemd v257.13:s dokumentation](https://github.com/systemd/systemd/blob/v257.13/man/systemd.exec.xml#L1495-L1537).
V — [namespace.c](https://github.com/systemd/systemd/blob/v257.13/src/core/namespace.c#L364-L395)
skiljer dessa fall; `drop_outside_root` tar bort mål utanför gästrot. S09 återläser inte
InaccessiblePaths och G05 kontrollerar inte åtkomst till dessa två kataloger.
D — Detta avviker från startpaketets avsedda maskering av gästens `/sys` och `/run`.
Bedömning: P1 mot det befintliga isoleringskravet; värdrelativa mål kan lämna gästens
kataloger omaskerade trots att egenskapen är satt. Ingen faktisk innehållsexponering
på servern påstås. O — kräver tillåten runtime-evidens för det faktiska tillståndet.

V — InaccessiblePaths lämnas oförändrat inom uppdragets granskning/redovisning; inga
åtkomster utökas. D — Förslag för separat beslut: gästrelativa `+/sys +/run`, kontroll
av återläst representation samt faktiskt nekad åtkomst, utan att ändra källrättigheter
eller lägga till bindningar. Det förslaget är inte implementerat eller provat.
O — kräver arkitektens hantering av luckan och granskning av exakt kandidat samt separat
ägarbeslut före nytt administratörsprov. Ingen D3, merge, retentionåtgärd eller driftändring
är utförd här. Ingen automatisk omkörning; det tidigare provet stoppade före GO och
båda cleanup-stegen är endast ägarrapporterade PASS.
