# Finance Release 1 – syntetiskt SQLite-startprov

Detta är en sanerad GitHub-version: inga privata hostvägar, konton,
hemligheter eller verkliga Finance-data publiceras. V = verifierat på
BigBrain i denna etapp; O = fortfarande obevisat för verklig audit.

## Metadata

- Datum 2026-11-08; risknivå 3; **FROZEN / RESEARCH / 0 SEK / NONE**.
- Verifierad GitHub-main: `93a31d2f31da37e8976feb47012c30eed7384e08`.
- Den [tidigare godkända syntetiska PR #47](https://github.com/T-bear/BigBrain/pull/47)
  förblir separat och draft; dess exakta granskade bytes ändras inte här.
- Scope: ny fristående syntetisk startkandidat, inte Finance-produktionskod.

## Status

V – En oprivilegierad rootless Bubblewrap-process har startats med exakt fem
**nyskapade syntetiska** filer: en liten verklig SQLite-databas och fyra
påhittade manifest-/datafiler. Skriptet accepterar inga argument och avvisar
root. Det kan inte ta emot verkliga Finance-sökvägar.

O – Ingen start under den dedikerade audit-UID:n; inga verkliga databaser har
öppnats. Detta är **inte** en färdig drift- eller granskningsstartare.

## Evidence

V – [Skriptet](finance-release1-synthetic-sqlite-start-20261108.sh)
passerade följande vid körning på BigBrain:
`SYNTHETIC_FIVE_FILE_ALLOWLIST`, `SYNTHETIC_MOUNT_RO`,
`SYNTHETIC_NO_SQLITE_SIDECARS`, `SYNTHETIC_SQLITE_READ_ONLY`,
`SYNTHETIC_WRITES_DENIED`, `SYNTHETIC_MINIMAL_FILESYSTEM`,
`SYNTHETIC_NETWORK_NAMESPACE`, `SYNTHETIC_UNPRIVILEGED_IDENTITY`.
Slutrad `SYNTHETIC_AUDIT_START=PASS`, exit 0.

V – Negativa mutationer gav icke-noll exit **utan** slut-PASS:
- `--ro-bind` ersattes med `--bind`: skrivbar montering avvisades.
- En extra syntetisk fil skapades: strikt allowlist avvisade den.
- SQLite-testvärdet ändrades: innehållskontrollen avvisade det.
- Godtyckligt argument `--db` avvisades innan körningen.

V – Den första provkörningen hittade en felskriven `Uid`-nyckel vid
läsning av `/proc/self/status` och ett problem med att städa en
0500-katalog. Båda korrigerades före slutligt PASS; de enda berörda
filerna var självskapade temporära provfiler. Kontroll efter körning
visade noll kvarvarande testkataloger.

V – Skriptets SHA-256:
`faef7b259bdceb9db75b78b532f09638d6d7f779e6988b907c2e6cfdd4f66655`.

## Changes

Denna separata kandidat innehåller endast ovanstående syntetiska skript
och denna sanerade rapport. Inga ändringar i PR #47 eller i Finance-kod,
rättigheter, konton, verklig data, tjänster eller serverkonfiguration.

## Security

Provet använder `python3.13 -I -S -B` under Bubblewrap med
`--unshare-user`, `--unshare-pid`, `--unshare-ipc`, `--unshare-net`,
`--cap-drop ALL`, `--clearenv` och 20 sekunders timeout.
En skräddarsydd, minimal filsystemsbild visar endast det syntetiska
underlaget. Skrivskyddad bindmontering verifieras med
`/proc/self/mountinfo`, separat från DAC-baserat skrivförbud.
SQLite öppnas med `mode=ro&immutable=1`, `PRAGMA query_only=ON`
och `PRAGMA quick_check`. `immutable=1` gäller endast den nyskapade,
stängda, syntetiska databasen.

V – Testet styrker detta snäva syntetiska scope, inte faktisk hostisolering
vid privilegierad start eller tillgång till verkliga Finance-kopior.

## Remaining work

O – Sudoers-/ACL-inventering, exakt audit-UID-start, verifierad
skriptintegritet före eventuell privilegierad start, FD-/seccomp-/
resursgränser, verklig kopiehash och SQLite-konsistens, frånvaro av
faktiska sidofiler samt retention/disposition och fullständig R1–R6-
granskning återstår. En dedikerad audit-UID ger inte automatiskt
en säker isolerad startväg.

En separat faktisk nivå 3-granskning av exakt ny kandidat-SHA/tree krävs
innan godkännande för ens denna nya syntetiska checkpoint.
BB-132K:s reserverade dispositionsfrist 2026-11-10 kl. 14.36.45
svensk tid påverkas inte.

## Resumption

DoD för denna checkpoint: positivt syntetiskt SQLite-/monteringsprov,
negativa mutationer, sanerad dokumentation och separat draft-PR för review.
Ingen root-/sudo-körning, ingen verklig Finance-läsning, ingen ny kopia,
ingen D3, merge, deployment eller handel ingår. Uppfylld DoD startar inte
automatiskt nästa fas. Frysningen kvarstår.
