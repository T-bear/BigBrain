# Finance Release 1 — startpaket för oberoende granskning

Detta är en sanerad GitHub-version. V = verifierat i kod/test/Git nu;
D = dokumenterat/ägarrapporterat; O = okänt. Endast granskningskandidat, inte körtillstånd.

## Metadata

V — 2026-11-07: main `0f11890dac3ff1529c83d7438658b90e4a61948e`; utgångspunkt i PR #40
`af5390c80744ae49f7fd12f93e795a8638e7cbcc`, tree `04de780292a9d95f874f8f68a45216363a51a1b8`.
Rent checkpoint-working-tree före ändringen; samma branch och orelaterat arbete bevaras.
D — Finance-risknivå 3; ägarens uppdrag tillåter diagnostik/rättningsförberedelse men ingen
rootexekvering, Finance-dataåtkomst, driftändring eller merge. Tidigare engångstillstånd är förbrukat.

## Status

D — Det senaste ägarlokala syntetiska provet stoppade vid `S08_wait_ready`; ägarens
journalutdrag anger `status=226/NAMESPACE`. Både `temporary_resources_removed` och
`synthetic_fixture_removed` rapporterades PASS. Ingen rå journal har lästs av Codex.
D — Uppgiften avser enligt ägaren senaste provet, vars levererade hash var
`4180b911b2f828ff5fe0b87a43486ae026d812f565563fc53e407022e7151623`; faktisk serverhash
är inte omverifierad. O — kräver operation och errno från exakt tillfällig tjänst för säker rotorsak.
V — S08 ligger före READY/GO; namespacefel före exec innebär att gästvakten inte startar.
Normalfallets fel förhindrar även det avsiktliga felprovet. Cleanup-PASS är D, inte egen hostverifiering.

D — Tidigare försök: kvarvarande etc/var/root/bin, därefter cleanup-PASS för
`0e06762315a143aca87da36e3ab48dc1`; senare G06-fel och sedan `G06_empty_root_readonly`
med båda cleanup-PASS. Det senare provets ID var `fe5c198e991442cd89928593bda72289`,
normalbarn `5cc242d331ec6d614c2fa3dbc3434d8d`; andra barnets utskrivna ID bevisade inte körning.
[Bevarad full tidigare evidens](https://github.com/T-bear/BigBrain/blob/af5390c80744ae49f7fd12f93e795a8638e7cbcc/docs/reports/features/finance/finance-release1-start-package-20261106.md).

## Evidence

| Kontroll | Resultat och gräns |
| --- | --- |
| V — Verifierare oförändrad | SHA-256 `7c065708528a10a0d3f0408e7de5fd4af4b5d93cf43ecae1c5105829de8fb9bb`. |
| V — Startpaket | SHA-256 `042a4b04e8dd2558deb4512b88545b3f4ceb01dcd419cb1107112b7426208481`. |
| V — Syntetiskt systemd-prov | SHA-256 `eff9feb969673e0cf71977813d707b87b31e6c500e5a3f064210d750d7731db0`; endast paketets hashbindning ändrad; inte kört. |
| V — [35 lokala tester](finance-release1-start-package-tests-20261107.py) | PASS: 2 MiB stderr dräneras utan pipeblockering, högst 64 KiB sparas; avslut med öppen skribent; journalgräns, timeout/kill, exakt unit-filter, sanering, namespacefel före GO och cleanup. Tidigare maskerings-/G06-tester kvar, inklusive 40 G06-felfall. |
| V — Testavvikelse | Första stora journalfixturen slog i operativsystemets argv-gräns (E2BIG); fixturen genererar nu data inne i det syntetiska barnet. Slutkörningen ovan passerar. Ingen systemd/journalctl körs i testerna. |
| O — Runtime | Kräver separat godkänt ägarlokalt prov av exakt kandidat; lokala tester använder vanlig användare, syntetiska filer och simulerad systemd. |

## Changes

V — stderr från systemd-run fångas nu av en separat lästråd under hela processens livstid,
även under READY-väntan, propertykontroller och cleanup. Icke-blockerande läsning med
selector, högst 64 KiB i RAM; fortsatt dränering kastar överskott. Ingen rå logg sparas
eller skrivs ut. `communicate` får inte samtidigt läsa stderr. Trådens avslut är tidsbegränsat.
V — `--pipe` skickar stderr-FD vidare; i
[exec-invoke.c](https://github.com/systemd/systemd/blob/v257.13/src/core/exec-invoke.c#L490-L505)
har en explicit FD företräde framför `StandardError`. Tidigare DEVNULL var därför en faktisk informationsförlust.

V — Endast vid S07/S08-startfel, före befintlig cleanup, förbereds `journalctl --boot`
för just den nyss skapade UUID-tjänsten, högst 16 JSON-poster och fälten MESSAGE/ERRNO/EXIT_STATUS.
Den fångsten har också 64 KiB-gräns, tre sekunders väntan och kill/wait vid timeout.
Ingen bred journalinventering eller egen beständig logg tillkommer. Journalens stderr kastas;
dess stdout dräneras samtidigt. Inga verkliga journalanrop har utförts här.

V — Parsern släpper endast igenom fasta D01/D02-koder, operation-ID och errno-namn.
Journalens ERRNO binds till samma post som operationen. Enbart stderr-text eller exitstatus
fyller inte okänt errno; frånvaro/fel/gränsöverskridande ger NOT CHECKED/UNKNOWN.
Högst sex unika diagnosrader plus två fångststatusrader. Exempelform, inte observerad runtime:

```text
FAIL D01_NAMESPACE operation=MOUNT_NAMESPACE errno=EACCES
NOT CHECKED D02_JOURNAL operation=JOURNAL_READ errno=UNKNOWN
```

## Security

V — Gästrelativa `ReadOnlyPaths=+/root` och `InaccessiblePaths=+/sys +/run` behålls,
liksom G05:s faktiska nekade åtkomst, G06:s ägare/mode/device/inode/ST_RDONLY/tomhet,
UID/GID 1654, inga extra grupper/capabilities, privat PID/mount/IPC/nätverk och syscallfilter.
Verifierare, källbindningar, source-sidecar-kontroller och cleanup av egna resurser är oförändrade.
V — Gästgränserna kvar: 300 s, en CPU, 512 MiB, ingen swap, åtta tasks, fem sekunders stoppfrist.
Diagnostiken körs i den redan ägarlokala supervisorn; journalavläsningen tillför högst tre sekunders
väntan plus två sekunders kill/wait och begränsat trådavslut. Ingen ny beständig behörighet.

V — Enda ändrade mountinställningen är explicit `MountAPIVFS=no`, som även krävs i S09
före GO. Detta stänger av automatiskt os-release-länkskapande; nödvändiga API-mountar begärs
fortfarande av befintliga PrivatePIDs/ProtectProc/ProcSubset enligt koden nedan. Ingen
återgång till värdrelativa skydd, ingen borttagning av gästvakter eller utökad dataläsning.

## Mountordning och orsaksbedömning

V — Granskad upstream **systemd v257.13**, inte serverns binär. Bedömning: automatisk
os-release-länk efter maskering av `/run` är den starkaste kodbaserade förklaringen.
O — kräver exakt runtime-operation/errno för att fastställa att detta orsakade ägarens 226.

| Berörd konfiguration | Kodgranskning och konsekvens |
| --- | --- |
| V — RootDirectory/MountAPIVFS | [execute.c:1542](https://github.com/systemd/systemd/blob/v257.13/src/core/execute.c#L1542-L1553): RootDirectory ger implicit MountAPIVFS=yes; explicit no vinner. [exec-invoke.c:3315](https://github.com/systemd/systemd/blob/v257.13/src/core/exec-invoke.c#L3315-L3319) kopplar os-release-länken till detta värde. |
| V — Sökvägar, prioritet | `+` prefixar gästrot; bindkällor är värdrelativa, destinationer gästrelativa. [namespace.c:828](https://github.com/systemd/systemd/blob/v257.13/src/core/namespace.c#L828-L872) sorterar föräldrar först och därefter mode. MOUNT_INACCESSIBLE vinner över andra mounts på samma mål. |
| V — Maskering sys/run | [drop_inaccessible](https://github.com/systemd/systemd/blob/v257.13/src/core/namespace.c#L942-L968) tar bort underordnade mounts, inklusive `/run/host/.os-release-stage`. Maskeringen av sys har företräde framför PrivateNetwork:s sysfs-mount. |
| V — Symlink-konflikt | [compile_symlinks](https://github.com/systemd/systemd/blob/v257.13/src/core/exec-invoke.c#L2966-L2977) lägger ändå in os-release-länken. [apply_mounts](https://github.com/systemd/systemd/blob/v257.13/src/core/namespace.c#L2204-L2269) försöker skapa länkar efter mounts men före readonly-ommonteringen. Den listan rensas inte av drop_inaccessible. Försök att skapa föräldern under maskerad run kan därför stoppa namespace-setup. Exakt errno är O. |
| V — Proc/dev/IPC/nätverk | [namespace_parameters_mount_apivfs](https://github.com/systemd/systemd/blob/v257.13/src/core/namespace.c#L2074-L2088) kräver API-mountar även med MountAPIVFS=no när PrivatePIDs, ProtectProc eller ProcSubset är satta. PrivateDevices och PrivateIPC:s mqueue samt privata namespaces behålls. |
| V — ProtectSystem/readonly | Mounts etableras först; readonly-flaggor sätts i nästa pass. Explicit +/root överstyr dess implicita skrivbara undantag; G06 kontrollerar slutligt resultat. BindLogSockets=no stoppar loggsockets men inte os-release-länken. |
| V — Filbindningar | [apply_one_mount](https://github.com/systemd/systemd/blob/v257.13/src/core/namespace.c#L1801-L1830) löser källor från värdrot; befintliga pinnade FD-källor/norbind ändras inte. Ingen databindning ligger under de maskerade målen. |
| V — Övriga krav | Identitet/caps/NNP/seccomp och resursgränser lämnas orörda. [exec-invoke.c](https://github.com/systemd/systemd/blob/v257.13/src/core/exec-invoke.c) utför namespace-setup före slutliga syscallfilter. 226 identifierar ett namespace-steg, inte en viss syscall eller errno. |

## Remaining work

V — Reproduktion av lokala tester utan privilegier:

```sh
python3 -B docs/reports/features/finance/finance-release1-start-package-tests-20261107.py -v
```

V — Följande provblock är **endast för granskning**, inte exekverat. Körs från checkpointens
reporot först efter oberoende granskning och separat ägargodkännande. Provhash kontrolleras
före exec av samma bytes; provet kontrollerar paketets hash före staging. Det använder bara
egna syntetiska filer, aldrig Finance-verifieraren eller Finance-data.

```sh
sudo -- /usr/bin/python3 -I -S -B - \
  docs/reports/features/finance/finance-release1-systemd-probe-20261107.py \
  docs/reports/features/finance/finance-release1-start-package-20261106.py <<'PYCODE'
import hashlib
import sys

expected = "eff9feb969673e0cf71977813d707b87b31e6c500e5a3f064210d750d7731db0"
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
PYCODE
```

## Resumption

V — Nästa steg är granskning av den konkreta rättningen och diagnostiken, inte automatisk
omkörning. O — Kräver separat godkänt systemd-prov för slutlig isolering och felinsamling.
D — Båda senaste cleanup-stegen är ägarrapporterade PASS; inget nytt återställningsbehov har
rapporterats. Ingen D3, merge, retentionåtgärd eller ny administratörsexekvering ingår.
