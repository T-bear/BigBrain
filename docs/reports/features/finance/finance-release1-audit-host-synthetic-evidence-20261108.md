# Finance Release 1 – syntetisk hostkontroll av separat auditidentitet

Detta är en sanerad GitHub-version. Systemkontonamn, numeriska identiteter,
privata sökvägar och råa terminalutdrag utelämnas. V = självständigt verifierat
med skrivskyddad hostkontroll; D = ägarrapporterat terminalutfall eller tidigare
dokumenterat; O = ej verifierat. Detta är en risknivå 3-checkpoint, inte
auktorisering att använda verkliga Finance-data.

## Metadata

- Datum: 2026-11-08.
- Avgränsat ägargodkännande: skapa en dedikerad icke-inloggningsbar
  auditidentitet utan sudo/Docker/extra grupper, följt av ett syntetiskt
  Bubblewrap-test. Inte godkännande av verklig dataåtkomst eller driftändring.
- Git-baslinje: `main` `0f11890dac3ff1529c83d7438658b90e4a61948e`.
- Tidigare designcheckpoint: [draft-PR #44](https://github.com/T-bear/BigBrain/pull/44),
  kandidat `88db75521997382b61fc1a991a2a85954d957bc3`.
  Den är inte mergad. [Finance-frysningen](../../../operations/finance-freeze-20261016.md)
  gäller i övrigt.

## Status

D – Ägaren genomförde en avgränsad systemkontoskapelse i Termius efter lokalt
sudo-godkännande. Ett syntetiskt Bubblewrap-prov under den nya identiteten
avslutades utan fel. Systemidentiteten kvarstår, men ingen tjänst eller
Finance-jobb har aktiverats.

V – Remote Desktop verifierade sedan att identiteten finns, har
`nologin`-skal, ingen skapad hemkatalog, enbart sin primärgrupp, inget
medlemskap i sudo- eller Docker-grupp och inga aktiva processer.
V – Den granskade Git-arbetskopian var ren och `main` oförändrad.

## Evidence

D – Ägarens terminal rapporterade **PASS** för identitet och tillåten
syntetisk läsning, skrivskydd av syntetisk indata, filsystemsallowlist,
separat nätverksnamnrymd samt `CapEff=0` / `CapPrm=0` /
`NoNewPrivs=1`. Testet använde bara tillfälligt genererade
påhittade filer, inte Finance-snapshot eller original.

D – Låst lösenord rapporterades av den administrativa kontrollen i
Termius. Lösenordsstatus har **inte** självständigt avlästs i den skyddade
hostdatabasen av Remote Desktop, eftersom den anslutningen saknar
icke-interaktiv administratörsåtkomst.

V – Självständig skrivskyddad hostkontroll av kontometadata, grupptillhörighet
och frånvaro av kvarvarande kontoprocesser gav förväntat resultat. Inget
audit-konto eller filrättighet skapades eller ändrades av denna efterkontroll.

## Changes

Den enda auktoriserade bestående hoständringen är en dedikerad, låst
systemidentitet utan hemkatalog. Kontoskapandet uppdaterar operativsystemets
kontodatabas. Det syntetiska testet använde och rensade en tillfällig fixture
enligt ägarens terminalutfall.

Denna Git-checkpoint tillför endast denna sanerade evidensrapport och en
länk i Finance-rapportindexet. Inga produktionsskript, databasfiler,
scheman, tjänster, behörigheter eller handelsfunktioner ändras av patchen.

## Security

Den separata identiteten reducerar risk jämfört med att använda det äldre
historiska kontot med bredare originaldataåtkomst. Rootless Bubblewrap
begränsade testprocessens synliga filsystem och nätverksnamnrymd till den
syntetiska allowlisten.

**Bevisets gräns:** Ett lyckat syntetiskt test är inte bevis för att den
framtida exekverbara Finance-verifieraren, verkliga fil- och ACL-rättigheter,
ärvda filbeskrivare, providerretention och alla nätverksvägar är korrekt
avgränsade. Separat identitet utan sandbox är inte tillräcklig. Avsaknad av
sudo/Docker-grupp bevisar inte ensam att alternativa privilegievägar saknas;
ingen fullständig sudoers-/host-ACL-inventering har gjorts.

Ingen verifierad avvikelse mot det avgränsade syntetiska testets krav.
Inget tillstånd till D3, modellkörning, provideråtkomst eller handel följer.

## Remaining work

- Oberoende nivå 3-granskning av den exakta framtida exekverbara kandidaten,
  användar- och sandboxkontraktet.
- Separat ägarbeslut före verklig Finance-dataåtkomst, ändrade datarättigheter,
  eventuella ytterligare kopior, retention/disposition eller aktivering.
- Verifiera verkliga källhashar, fil- och katalogbehörigheter, ACL, statisk
  SQLite-konsistens, inga sidofiler och kontrollerad exekveringsmiljö.
- Den dokumenterade BB-132K-fristen för dispositionsgranskning den
  2026-11-10 kl. 14.36.45 svensk tid påverkas inte.

## Resumption

Finance förblir **FROZEN / RESEARCH / 0 SEK / NONE** utanför den uttryckligen
auktoriserade syntetiska checkpointen. PR #44 och denna evidens är endast
granskningskandidater; varken merge eller deployment utförs här.
Nästa verkliga data- eller behörighetsåtgärd kräver sitt eget avgränsade
godkännande och verifierade skyddsgränser.
