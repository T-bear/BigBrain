# Finance Release 1 – specifikation för syntetisk audit-UID-start

Detta är en sanerad GitHub-version. V = tekniskt verifierat i befintligt
underlag; D = rapporterat av extern granskare eller ägare; O = inte verifierat
på BigBrain. Dokumentet är **review only** och ger ingen körbehörighet.

## Metadata

- Datum: 2026-11-08. Risknivå 3; **FROZEN / RESEARCH / 0 SEK / NONE**.
- Main-baslinje: `93a31d2f31da37e8976feb47012c30eed7384e08`.
- Underlag: [PR #50, säkerhetskontrakt](https://github.com/T-bear/BigBrain/pull/50),
  [PR #47, syntetisk isolering](https://github.com/T-bear/BigBrain/pull/47),
  [PR #49, syntetisk SQLite](https://github.com/T-bear/BigBrain/pull/49)
  och [Finance-frysningen](../../../operations/finance-freeze-20261016.md).
- [Release 1-mandat PR #40](https://github.com/T-bear/BigBrain/pull/40)
  och [tillägg PR #42](https://github.com/T-bear/BigBrain/pull/42)
  upphör senast **2026-11-18**, eller tidigare vid återkallelse eller
  accepterad Release 1. Efter det krävs förnyat uttryckligt mandat.
- D – Ägarförmedlad extern granskning av exakt PR #50:
  **GODKÄND MED ICKE-BLOCKERANDE ANMÄRKNINGAR**, inga P0/P1,
  två P2 och tre P3. Granskarens off-host-tester är inte BigBrain-host-PASS.
  PR #50 ska inte ändras på grund av dessa iakttagelser.

## Status

**SPECIFICATION ONLY / AUDIT-UID EXECUTION NOT RUN.**
Det avsedda nästa uppdraget är en enda exakt bunden, syntetisk
exekverbar launcher-/verifierarkandidat som testas under den redan
befintliga icke-inloggningsbara audit-identiteten. Endast påhittade
temporärfiler; inga Finance-källvägar, backupdata eller körargument
som kan peka på verkligt innehåll.

O – Att använda en annan UID kan kräva administrativ start som **inte
är auktoriserad av denna specifikation**. Inget `sudo`, `setpriv` med
privilegieökning, ACL-/kontoändring eller verkligt hostprov görs här.
En design kan vara klar även om själva UID-provet är BLOCKED.

## Evidence

V – PR #47/#49 har syntetiska tester för minimal Bubblewrap-vy,
read-only-mount, nätverksnamnrymd och SQLite-läsning. Dessa tester får
återanvändas som daterad evidens inom sina ursprungliga scope.

D – Granskaren av PR #50 reproducerade off-host att Bubblewrap kan ärva
värdens öppna filbeskrivare och att SIGTERM/SIGHUP i ett bash-skript
kan lämna sandlådan aktiv till timeout. Ett direkt `exec`-baserat
privilegietapp under kontrollerad processkedja gav bättre resultat
i granskarens andra miljö. Dessa observationer måste bekräftas för
den nya exakta implementationen på BigBrain innan någon verklig audit.

## Changes

Detta är endast en ny sanerad implementationsspecifikation. PR #47,
#49 och #50, den äldre verifieraren, produktionskod och serverns
faktiska rättigheter lämnas oförändrade.

## Security

### Bindande scope och mätbar Definition of Done

1. **Identitet och behörighet (P2-a).** Första faktisk auktoriserade
   hostgrind ska inventera audit-kontots primär-/tilläggsgrupper,
   nologin, konto- och filkapabiliteter, relevanta sudoers-/polkitvägar
   samt access/default-ACL på den framtida stagingens hela föräldra-
   och filkedja. Negativa tester ska bevisa att andra databaser,
   Docker-socket, secrets och icke-allowlist-vägar inte är läsbara.
   Sudoers-/skyddad metadata-inspektion sker först efter ett separat
   godkännande; allmän läs-/traverseringsrätt får inte öppnas.
2. **Exakt exekverbart kontrakt.** En ny fristående launcher och
   verifierare; ingen tyst omskrivning av PR #40:s UID-bundna
   kontrollartefakt. Båda identifieras med commit, tree, SHA-256 och
   byteidentitet före körning. Endast syntetisk femfils-allowlist.
   Avvikande argument eller omgivningsvariabler avvisas fail-closed.
3. **FD-arv (P3-b).** Alla ärvda deskriptorer över 2 ska stängas
   före `exec` om inte en viss deskriptor uttryckligen motiveras
   och verifieras. Inspektera `/proc/self/fd` inne i sandboxen med
   hänsyn till probens tillfälliga katalog-FD. Negativt test ska
   avvisa en avsiktligt ärvd host-FD.
4. **Processlivscykel.** Efter eventuellt separat godkänd
   administrativ setup ska den exekverbara processkedjan tappa
   privilegier och `exec`:a till kortlivad timeout/bwrap/probe.
   Kontrollera hela process-trädet; ingen kvarvarande privilegierad
   launcher eller behörig bakgrundsprocess får bli kvar. Kontrollera
   TERM, HUP, INT, KILL, hård timeout och saknat barn: sandlådan ska
   upphöra, utan automatisk omkörning. `--die-with-parent` måste
   knytas till faktisk direktförälder, inte antas transitivt.
   Temporär staging-disposition får **inte** förutsätta en EXIT-trap
   eftersom SIGKILL kringgår den.
5. **Resursgränser (P3-c).** Välj enkla kernel-RLIMIT-gränser
   (`prlimit` eller motsvarande) för CPU-tid, minne,
   process-/trådskapande, öppna FD och maximal runtime; verifiera
   faktiskt resultat och negativt överskridandetest. Inga påstådda
   effektiva cgroup-/seccomp-gränser utan verifiering. Ett inbyggt
   prov ska inte resultera i resursutarmning av andra tjänster.
6. **Syntetisk datagräns.** Hela testmängden skapas från scratch
   med syntetiska värden. Faktiskt `ro` kontrolleras via
   mountinfo, SQLite är read-only och `immutable=1` gäller bara
   verifierat statiska, stängda syntetiska kopior. Inga nätverks-
   eller provideranrop, inga produktionsvolymer.
7. **Staging/TOCTOU (P3-a), endast för framtida ägarbeslut.**
   Innan verkliga filer används ska hela stagingkedjan vara
   root-kontrollerad och inte värdskrivbar av andra principaler,
   och den stagade kopian måste hashkontrolleras före/efter.
   Källans filidentitet, storlek och mtime ska kontrolleras
   före/efter kopiering; det är upptäcktskontroller, inte bevis
   mot samtliga race. Öppning via betrodd FD-/inode-bindning och
   konsekvent SQLite-snapshot kräver separat riskgranskning.
   Ingen verklig kopia får skapas med stöd av denna specifikation.

**DoD:** fullständig exekverbar kandidat och negativ testmatris;
inga hemligheter eller verkliga Finance-filer; dokumenterade
PASS/FAIL/NOT CHECKED; noll oavsiktliga process-/testrester;
nytt oberoende nivå 3-review av exakt SHA/tree och korrekt
main-baslinje. Syntetisk CI eller tidigare Claude-review är inte
automatiskt godkännande av nya bytes.

### Stoppvillkor

STOPP vid saknad UID-startauktorisering, otillräcklig behörighet,
oväntat FD-arv, skrivbar evidence-bind, extra filer, kvarvarande
privilegierad process, gränsöverträdelse, ogiltigt/utgånget mandat,
saknad bevisning eller varje försök att öppna verkliga Finance-filer.

## Remaining work

Föreslå och granska en **separat, syntetisk launcher-kandidat** enligt
kraven ovan. Om ingen godkänd väg för körning under audit-UID finns
kan kod och oprivilegierade tester förberedas, men hostidentitetstestet
ska markeras **BLOCKED**, inte PASS.

Varje skyddad staging, retention/disposition eller verklig databasinläsning
kräver därefter ett eget, uttryckligt ägarbeslut. Särskilt gäller
BB-132K:s dispositionsgranskning senast 2026-11-10 kl. 14.36.45
svensk tid, eller tidigare enligt tillämpliga villkor.

## Resumption

Denna checkpoint är ett avslutat specifikationsuppdrag, inte
implementations-, merge-, admin-, drift- eller D3-auktorisering.
Finance-frysningen förblir verksam. Fortsatt arbete kräver att
relevant mandat ännu är giltigt och att nästa exakta uppdrag
auktoriserats inom dess gränser.
