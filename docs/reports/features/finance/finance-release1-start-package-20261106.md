# Finance Release 1 — startpaket för oberoende granskning

Detta är en sanerad GitHub-version. V = verifierat i denna avstämning;
D = dokumenterat/ägarrapporterat; O = okänt. Paketets föreslagna beteende är
kontrollerat med syntetiska filer utan privilegier. Ägarlokalt systemd-prov av
kandidaten `013125d` har rapporterat FAIL. Den nya kandidaten är endast för granskning;
`InaccessiblePaths`-luckan har en förberedd kodkorrigering och gästkontroll, ännu inte
verifierad i systemd.

## Metadata

V — 2026-11-07: accepterad GitHub-main
`0f11890dac3ff1529c83d7438658b90e4a61948e`; PR #40:s utgångspunkt
`ad2fd99a728a373db2be0aba99a98d3bd7db7623`, tree
`cbcba2d3bfe981c83ea6c307a91f5ae28ebdc0a8`, rent checkpoint-working-tree före
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
D — Föregående beslut korrigerade ReadOnlyPaths till `+/root`. Det nya uppdraget
auktoriserar `InaccessiblePaths=+/sys +/run`, återläsning före GO och faktisk kontroll
av otillgänglighet i gästen, med oförändrad övrig isolering.
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
exekveringsauktorisering följer av resultatrapporten. Ingen ny kandidat har körts av ägaren
enligt tillgängligt underlag; faktisk isolering är inte godkänd.

## Evidence

| Kontroll | Evidens och begränsning |
| --- | --- |
| V — Oförändrad verifierare | SHA-256 `7c065708528a10a0d3f0408e7de5fd4af4b5d93cf43ecae1c5105829de8fb9bb`, samma bytes som den [godkända artefakten](https://github.com/T-bear/BigBrain/blob/029ef0d9d96ddcabff80649138d5f2dd0272c55e/docs/reports/features/finance/finance-release1-verify-readonly-20261106.py). |
| V — Nytt startpaket | SHA-256 `277201476976f10dad22ac483ce430aa289dbce6184530910091545d34f35029`. Föregående kandidat `449b5045555df72430771d65c3434800e8d0cb53d5a11a14e10a9947a3fa9201` finns kvar i Git-historiken. |
| V — Bevarad cleanup-rättning | Den historiska bristen i `82a73a8` är rättad sedan `a9f68f9`: de fyra tomma katalogerna ingår och okända poster blockerar borttagning. Cleanup-koden ändras inte i denna kandidat. |
| V — Syntetisk regression | [27 tester PASS](finance-release1-start-package-tests-20261107.py), inklusive 40 oförändrade G06-felfall. Gamla, blandade eller saknade maskeringsvärden stoppar S09 före GO. Åtkomlig sys/run stoppas genom den riktiga kontrollfunktionen och förälderns felprotokoll före GO, med lyckad cleanup. Riktiga kernel-DAC-fel provas i temp-kataloger som vanlig testanvändare; varken root, UID 1654 eller systemd används. Öppning/traversering måste nekas även om `os.access` simuleras falskt. ENOENT, EPERM, EIO och symlänk avvisas. Orelaterad sentinel bevaras. |
| D/O — OS-runtime | Föregående avstämning: Python 3.13.5/systemd 257.13. O — kräver separat godkänt systemd-prov för faktisk namespace-, DAC/ACL- och cgroup-funktion. Syntetiska tester bevisar inte dessa skydd. |
| D/O — Datakopia | Augustikopia 2026-08-17 och manifest-/datafiler från 2026-08-15/16 enligt befintligt underlag. O — kräver verifierarens faktiska hash-, lineage- och internkonsistenskontroller för resultat om innehållet. Ägarens metadatautfall bevisar inte innehållsintegritet. |

## Changes

V — `InaccessiblePaths=+/sys +/run` sätts i den tillfälliga tjänsten och krävs exakt
i S09:s återläsning före GO. Befintliga `ReadOnlyPaths=+/root` och dess återläsning
behålls. Verifieraren, övrig isolering, bindningar, resursgränser och cleanup är oförändrade.
Provets enda kodändring är hashbindningen. Inga nya källrättigheter eller databindningar.

V — Ny `check_inaccessible()` körs efter G01/G04:s verifiering av UID/GID 1654 och
noll capabilities, före READY/GO. För vardera sys/run krävs en verklig katalog utan
symlänk, nekad R/W/X-åtkomst via `os.access(..., effective_ids=True)`, samt exakt
`EACCES` från både skrivskyddad katalogöppning och `stat` av `/.` för traversering.
Oväntat lyckad öppning stängs omedelbart och stoppar; inga katalogposter eller
filinnehåll läses. Inget skrivförsök görs. Saknade mål eller andra fel godtas inte.
Sanerade fel är `G05_mask_<sys|run>_<type|access|open|traverse>` och tillåts uttryckligen
i förälderns felprotokoll. G06:s ST_RDONLY, ägare, mode, device/inode och tomhet behålls.

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
`4180b911b2f828ff5fe0b87a43486ae026d812f565563fc53e407022e7151623`, är inte kört.
D — Föregående provs rapporterade körning står under Status; resultatet är FAIL, inte
godkänd isolering. Tidigare engångsgodkännande får inte återanvändas.
V — Provet har endast egna syntetiska filer, två villkorade fall och befintlig avgränsad
cleanup. Det kör aldrig Finance-verifieraren. Normalfallet måste passera inklusive
cleanup innan det avsiktliga verifierarfelet provas. Exakt test- och återhämtningskod
finns i den länkade artefakten; ingen ny administratörsexekvering är auktoriserad.
O — kräver arkitektgranskning och nytt separat ägarbeslut före varje nytt administratörsprov.
D — Augustikopian kan endast ge historisk snapshot-evidens, inte dagens drifttillstånd.

V — Följande uppdaterade terminalblock är **endast för granskning**, inte exekverat.
Oberoende granskning och separat ägargodkännande krävs före exekvering. Det förutsätter
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

expected = "4180b911b2f828ff5fe0b87a43486ae026d812f565563fc53e407022e7151623"
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

D — Föregående kandidat hade `InaccessiblePaths=/sys /run`, alltså värdrelativa mål
enligt [systemd v257.13](https://github.com/systemd/systemd/blob/v257.13/man/systemd.exec.xml#L1495-L1537).
V — Den nu auktoriserade korrigeringen riktar maskeringen mot gästen och kontrollerar
både inställning och faktiskt nekad åtkomst. Den tidigare P1-luckan är hanterad i kod,
inte stängd genom runtime-evidens. O — kräver separat godkänt syntetiskt systemd-prov
för faktisk maskering som UID 1654; lokala tester bevisar inte monteringsmekanismen.

V — Nästa steg är oberoende granskning av samlad kandidat och provblock. D — Tidigare
engångsgodkännande är förbrukat. Ingen rootexekvering, Finance-dataåtkomst, D3 eller
merge ingår nu. Föregående prov stoppade före GO; båda cleanup-stegen är dokumenterade
som ägarrapporterade PASS. Ingen automatisk omkörning.
