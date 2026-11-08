# Finance Release 1 – syntetisk audit-launcher, kandidat och hostevidens

Detta är en sanerad GitHub-version. V = självständigt verifierat på BigBrain
för oprivilegierat, syntetiskt scope; D = tidigare dokumenterat eller
ägarrapporterat; O = ej verifierat. Inga verkliga datavärden, personuppgifter,
privata Finance-sökvägar eller tokens ingår i rapporten.

## Metadata

- Datum 2026-11-08. Risknivå 3; **FROZEN / RESEARCH / 0 SEK / NONE**.
- GitHub-main vid prov: `93a31d2f31da37e8976feb47012c30eed7384e08`.
- Underlag: [PR #51, syntetisk audit-UID-specifikation](https://github.com/T-bear/BigBrain/pull/51),
  [PR #50, säkerhetskontrakt](https://github.com/T-bear/BigBrain/pull/50),
  [PR #49, SQLite-baslinje](https://github.com/T-bear/BigBrain/pull/49) och
  [PR #47, Bubblewrap-baslinje](https://github.com/T-bear/BigBrain/pull/47).
- Giltigt Release 1-mandat för detta förberedande scope enligt
  [PR #40](https://github.com/T-bear/BigBrain/pull/40) och
  [PR #42](https://github.com/T-bear/BigBrain/pull/42), senast till
  **2026-11-18** eller tidigare återkallelse/acceptans.

## Status

**SYNTHETIC CURRENT-UID HOST PASS / AUDIT-UID HOST BLOCKED.**

V – En komplett kandidat bestående av
[`finance-release1-synthetic-audit-launcher-20261108.py`](finance-release1-synthetic-audit-launcher-20261108.py),
[`finance-release1-synthetic-audit-probe-20261108.py`](finance-release1-synthetic-audit-probe-20261108.py)
och [reproducerbart syntetiskt negativtest](finance-release1-synthetic-audit-launcher-test-20261108.py)
är byggd. Provet kördes på BigBrain som vanlig användare **utan sudo/root**.
Audit-UID:n har **inte** använts, och test under den identiteten får
inte registreras som PASS.

Ingen verklig Finance-fil, originalbackup eller databas har öppnats.
Inga ACL:er, konton, systemd-enheter, Docker-resurser eller tjänster ändrades.

## Evidence

V – Positiv körning med fem nyskapade syntetiska filer gav PASS för
exakt allowlist och filtyp, riktig readonly-bind via `mountinfo`,
SQLite `mode=ro&immutable=1` + `query_only` + `quick_check`,
skrivförbud, minimal filsystemsbild, nätverksnamnrymd, identitet,
`CapEff=CapPrm=0`, `NoNewPrivs=1`, ärvda FD och RLIMIT.
Slutmarkörer: `SYNTHETIC_UID_PROBE=PASS`,
`SYNTHETIC_LAUNCHER=PASS`, `AUDIT_UID_EXECUTION=BLOCKED`.

V – Negativa tester gav **icke-noll exit av rätt orsak utan slut-PASS**:
ogiltiga argument, RW-bind i stället för RO-bind, höjd
`RLIMIT_NOFILE`-gräns, efterhandsförändring av SHA-pinnad probe
och avsiktligt ärvd värd-FD med borttagen `close_fds`.
Kontrollfall med en avsiktligt ärvd FD och **aktiv**
`close_fds=True` passerade.

V – Syntetisk tidsfördröjning användes endast i en tillfällig
probekopia för avbrottstester. SIGTERM, SIGHUP, SIGINT och SIGKILL
mot launchern gav icke-noll exit; både timeout- och Bubblewrap-
barnets PID upphörde vara aktivt. Hård timeout gav icke-noll
exit och vanlig städning. SIGKILL omöjliggör EXIT-städning;
det avsedda syntetiska kvarvarande scratch-objektet
identifierades och rensades separat av den oprivilegierade
testharnessen efter kontroll av ägande och syntetiskt innehåll.
Slutlig kontroll: **noll egna tillfälliga testrester**.

V – Två fel upptäcktes och korrigerades innan slutligt PASS:
en för låg `RLIMIT_NPROC` i förhållande till värdens redan
existerande användartrådar och en SQLite-anslutning som inte
stängdes före FD-grinden. Testharnessens senare felaktiga
hantering av försvunnen `/proc`-post och buffrat stdout
rättades. Ingen av dessa körningar läste verkliga Finance-data.

## Changes

Tre fristående syntetiska Pythonfiler samt denna rapport. Ingen
produktionskod, driftkonfiguration, verklig databas eller annan PR
ändras. Den tidigare hårdkodade verifieraren i PR #40 ändras inte.

## Security

Launchern avvisar root och argument, kontrollerar mandatets sista
datum och kräver en lokalt definierad audit-identitet med nologin.
Själva provet kan bara bygga sina egna syntetiska, tillfälliga filer.
Verifierarens SHA-256 är hårdkodad och kontrolleras före och efter
temporär staging. Ingen extern datakälla eller konfigurerbar väg
accepteras. Den exakta publicerade commit-/tree-identiteten
måste självständigt granskas före godkännande av nya bytes.

Processen använder standardverktygen `setpriv --no-new-privs
--pdeathsig KILL`, `prlimit`, `timeout` och `bwrap --die-with-parent`.
Den vanliga UID:n behålls. Efter explicit framtida godkännande för
en separat identitetsväxling måste samma kontroller testas under
audit-UID. Det sker inte i detta uppdrag.

Begränsningar: `RLIMIT_NPROC` är knuten till användarens samlade
processer/trådar, inte en helt fristående jobbcgroup. Ingen
cgroup-/seccomp-isolering påstås. SIGKILL-städning är en separat
dispositionsåtgärd, inte en garanti från launcherns EXIT-kod.
Det exekverbara underlaget är en syntetisk kandidat, **inte en
verklig Finance-verifierare** och ger inte åtkomst till original.

## Remaining work

O – Negativa hostprov under den dedikerade audit-identiteten är
**BLOCKED**, eftersom eventuell administrativ identitetsväxling
kräver separat avgränsat ägargodkännande. Även inventering av
skyddade sudoers-/polkit-/ACL-/capability-gränser är kvar.
Ingen administrativ eller skyddad inventering har gjorts här.

O – Verklig dataåtkomst, eventuella nya skyddade kopior, aktuell
proveniens, licens/retention, statisk SQLite-konsistens och
datadisposition har separata ägargrindar. Den dokumenterade
BB-132K-granskningen senast 2026-11-10 kl. 14.36.45 svensk
tid påverkas inte.

**Nästa granskningsgrind:** oberoende faktisk nivå 3-review av
exakt commit, tree, kod, negativa tester, hostevidens samt
avgränsningar. CI-PASS är inte självständigt säkerhetsgodkännande.

## Resumption

DoD för denna checkpoint: publicerbar syntetisk launcher/probe,
reproducerbar negativ testmatris, kontrollerade utfall och separat
review-PR. DoD för verklig audit-UID-hostkörning är **inte**
uppnådd. Ingen merge, deployment, admin-/kontoändring,
verklig Finance-läsning, D3 eller modell-/handelskörning ingår.
Frysningen kvarstår.
