# Finance Release 1 — åtkomst-handoff

Detta är en sanerad GitHub-version. V = verifierat; D = dokumenterat/ägarrapporterat;
O = okänt. Förslag nedan är inte körorder eller utförda åtgärder.

## Metadata

V — Avstämning 2026-11-06. Main `0f11890dac3ff1529c83d7438658b90e4a61948e`;
PR #40:s utgångspunkt `11fb26a1110c239c9ff3b3254149231fbbeb85ef`, rent working tree.
[Mandatet](../../../operations/finance-release1-mandate-20261106.md) §§5/7 och
[åtkomststoppet](finance-release1-readonly-preparation-20261106.md) gäller fortsatt.
D — Ägaren har förmedlat arkitektgodkännande av exakt verifieringsskript för augustikopian.

## Status

V — **Ingen befintlig tillåten körväg är styrkt; inget exekverbart terminalblock lämnas.**
Ingen Finance-fil öppnades och inget tidigare nekat läsförsök upprepades.

## Evidence

| Underlag | Slutsats |
| --- | --- |
| V — `/etc/passwd` och `/etc/group`: befintligt konto `bigbrain-historical`, UID/GID 1654, hem `/nonexistent`, skal `/usr/sbin/nologin`; ingen annan lokal gruppmedlemskapspost hittad | Kontot är inte förberett för vanlig interaktiv inloggning. Existens bevisar inte tillåten exekvering eller datarättigheter. |
| V — `os.getuid()/geteuid()`: sessionen använder UID 1000 | Denna session uppfyller inte verifierarens identitetskrav. |
| V — `lstat` av skriptet och dess föräldrar: skript 0600, privata föräldrar 0700, ägare annan än 1654 | Den nuvarande checkout-sökvägen är inte tillgänglig för en vanlig process med UID 1654. |
| V — Metadata visar att maintenance-unitfilen finns; D — [historisk hosting-runbook](https://github.com/T-bear/BigBrain/blob/378d43e9a33b217fef30de5d4f74ef58a0a01926/docs/operations/runbooks/finance-historical-hosting.md) beskriver en fryst preflight | Ingen styrkt Release 1-startväg. Den gamla enheten ska inte startas, ändras eller återanvändas för verifieraren. |
| D — Backupkandidaten och tidigare PermissionError är dokumenterade | O — kräver ägarlokal metadata om exakt kopia, föräldrar, filrättigheter/ACL och sidofiler; inga nya försök gjordes här. |

V — Skriptets bytes matchar fortsatt godkänd SHA-256:
`7c065708528a10a0d3f0408e7de5fd4af4b5d93cf43ecae1c5105829de8fb9bb`.
[Exakt godkänd artefakt](https://github.com/T-bear/BigBrain/blob/029ef0d9d96ddcabff80649138d5f2dd0272c55e/docs/reports/features/finance/finance-release1-verify-readonly-20261106.py).
V — [Befintlig driftpolicy](../../../policy/lanes.md) dokumenterar att UID 1654 också äger
andra originaldatabaser. Att öppna traversal till hela lagringsroten är därför ingen bevisat
avgränsad läsrätt till augustikopian.

## Changes

V — Endast denna handoff och kataloglänk tillkommer. Ingen startmekanism eller rättighet ändras.

## Security

**Förslag för separat ägarbeslut:** en ägarstyrd, tidsbegränsad engångsprocess under befintlig
UID/GID 1654, i en separat skrivskyddad filsystemsbild. Ingen interaktiv inloggning, ändrat
login-skal, ny användare, sudoers-/Docker-gruppåtkomst eller generell ACL-öppning behövs i förslaget.

1. **Ägarlokal metadataavstämning:** befintlig administratör verifierar endast de utpekade
   kopiornas och föräldrarnas ägare/mode/ACL, riktiga sökvägar och frånvaro av sidofiler.
   Det ger Codex inga nya rättigheter. O — kräver detta för att avgöra om befintliga
   filrättigheter räcker även efter avgränsad exponering; bindning ändrar inte filernas DAC/ACL.
2. **Tillgänglig verifierarartefakt:** gör en tillfällig, administratörsskyddad och för 1654
   läsbar kopia av enbart skriptet, byteidentisk med länken ovan. Ingen Finance-DB kopieras.
   Föreslagen skyddsgräns: katalog 0:1654/0750, skript 0:1654/0440; inga skrivbara föräldrar.
3. **Avgränsad start:** en ny separat engångskontext, inte den historiska maintenance-enheten,
   exponerar enbart nedanstående befintliga filer skrivskyddat. Inga extra grupper,
   capabilities, nätverk, Docker-socket, driftDB eller andra datavolymer. Originalets
   WAL/SHM/journal måste kontrolleras före exponering; filtreringen får inte dölja sidofiler.
   Inga källfilsrättigheter ändras automatiskt om läsning fortfarande saknas.
4. **Kontroll före dataläsning:** faktisk real/effective UID och GID 1654, avgränsade rättigheter
   och exakt skripthash kontrolleras inne i körkontexten. Kör samma kontrollerade bytes
   med isolerad Pythonmiljö; undvik byte mellan hashkontroll och exekvering. Därefter får
   originalskriptets oförändrade fil-/hash-/SQLite-grindar köras. Avvikelse stoppar utan nytt försök.

V — Skriptets obligatoriska filåtkomst omfattar `bb-091-pre-migration-20260817.db` och
både `.manifest.json` och `.data.json` för `finance-backup-3b093584173d74f6` respektive
`finance-backup-8b549915b4e4129c`. Manifestfilerna läses; datafilerna SHA-256-hashas blockvis.
`bb090-closure-20260816` används endast för katalogmetadata i skriptet. O — dess innehåll
är okänt; föreslagen minsta åtkomst undantar den katalogen, vilket ska rapporteras
NOT CHECKED av det oförändrade skriptet, inte PASS. Övriga backupfiler exponeras inte.

**Återkallelse:** stoppa endast den nya engångsprocessen och dess processgrupp; den privata
filsystemsbilden och bindningarna upphör. Ta bort enbart ny temporär startdefinition och
skriptstaging efter avslut, enligt det separata beslutet. Inga konton, originalfiler, backuper
eller produktionsenheter berörs; inga bestående datarättigheter ska finnas att återställa.

## Remaining work

O — kräver ägarens separata beslut om ovanstående avgränsade administratörssteg, metadata
som fyller åtkomstluckorna och granskning av exakt startpaket före exekvering. Skriptgodkännandet
är inte ett godkännande av en ännu obefintlig startmekanism. Inget körklart paket påstås finnas.

## Resumption

V — Åtkomststoppet kvarstår. Ingen driftDB, D3, migration, omstart eller merge utförd.
Nästa underlag är den ägarlokala metadataavstämningen, inte ett nytt Codex-läsförsök.
