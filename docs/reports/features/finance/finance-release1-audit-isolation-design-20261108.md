# Finance Release 1 – förenklad audit-isolering, designcheckpoint

**Datum:** 2026-11-08
**Status:** REVIEW ONLY, inte säkerhetsgodkänd för verkliga Finance-data
**Risk:** nivå 3, RESEARCH / 0 SEK / NONE
**Baslinje:** `main` `0f11890dac3ff1529c83d7438658b90e4a61948e`

## Beslut och gräns

Ägarens [tilläggsmandat v1.1 i PR #42](https://github.com/T-bear/BigBrain/blob/7f36b1f35571b7b0ed72d3d476ee0d1186d49e53/docs/operations/finance-release1-security-simplification-mandate-20261108.md) tillåter designarbete och syntetiska tester. Det [tidigare Release 1-mandatet i PR #40](https://github.com/T-bear/BigBrain/blob/90d45d03f11df8dce946a7fe3ccf19bbc1602309/docs/operations/finance-release1-mandate-20261106.md) anger villkor för verklig verifiering. Dessa PR:er är ännu inte accepterade i `main`. [Finance-frysningen](../../../operations/finance-freeze-20261016.md) gäller i övrigt.

**Arkitekturval:** utred en separat, opriviligierad audit-identitet med ägarkontrollerad exponering av exakt tillåtna, redan statiska underlag. Detta är ett **designval**, inte tillstånd att skapa konton, kopiera data, ändra rättigheter eller exekvera mot Finance.

## Konkret hotmodell

Enligt [åtkomstunderlaget i PR #40](https://github.com/T-bear/BigBrain/blob/90d45d03f11df8dce946a7fe3ccf19bbc1602309/docs/reports/features/finance/finance-release1-access-handoff-20261106.md) äger befintlig UID 1654 också andra originaldatabaser. Direkt körning som UID 1654 utan filsystemsisolering är därför inte acceptabel.

En *ny* UID är inte i sig en fullständig sandbox: world-readable-, grupp- och ACL-rättigheter kan fortfarande medge otillåten läsning. SQLite `mode=ro` begränsar inte processens övriga filåtkomst, nätverk eller skrivmöjligheter. Faktiska åtkomstgränser måste granskas på host; om de inte kan säkerställas behövs en separat godkänd OS-sandbox.

## Jämförda alternativ

| Alternativ | Fördel | Olöst krav |
| --- | --- | --- |
| Nuvarande specialbyggda systemd-startare | Omfattande avsedd isolering | Cirka 754 Pythonrader plus prob; tidigare `226/NAMESPACE`; host-PASS saknas |
| **A: ny audit-UID + avgränsade läskopior** | Mindre specialkod och separerad identitet | Ägarstyrd provisionering, verklig rättighetskontroll, nätverksgräns, retention och ny verifierargranskning |
| B: förenklad systemd-RO-sandbox | Kan ge starkare filsystemsgräns | Särskilt rootbeslut, exakt mount-/namespacebevis och oberoende granskning |

**Val:** A för syntetisk design; B är reserv om de obligatoriska hostskydden inte kan bevisas med A. Ingen svagare säkerhetsgräns accepteras.

## Föreslaget exekveringskontrakt – ännu inte auktoriserat

1. Ägaren ska separat besluta om ny dedikerad UID utan extra grupper, sudo, Docker- eller handelsrättigheter. Kontot ska inte ha åtkomst till andra originaldatabaser, hemligheter eller modullager.
2. Exakt namngivna, rättighetsklarerade och verifierat stillastående snapshot-/manifest-/datafiler exponeras via ägarstyrd åtkomst. Eventuell ny datakopia kräver särskilt retention- och dispositionsbeslut.
3. Kontroll av katalogföräldrar, ACL/DAC, symlänkar, hårdlänkar, filtyp, hash, SQLite-sidofiler samt före/efter-integritet ska ske på verklig host. Några verkliga filer öppnas inte i detta checkpoint.
4. Auditprocessen får inga inärvda hemligheter, provideranrop eller handelsfunktioner. Verifiera faktisk nätverksbegränsning; frånvaro av HTTP-kod är inte likvärdigt med OS-isolering.
5. Den tidigare verifieraren har SHA-256 `7c065708528a10a0d3f0408e7de5fd4af4b5d93cf43ecae1c5105829de8fb9bb` och hårdkodar UID 1654. Ändrad identitet innebär ny exakt artefakt, granskning och hashgodkännande. Ändra inte tidigare godkända bytes.
6. `mode=ro`, `PRAGMA query_only` och SQL-authorizer ger djupförsvar. `immutable=1` gäller endast verifierat statiska och konsekventa kopior, aldrig aktiv WAL-data.

## Syntetisk evidens och DoD

Den [fristående fixturen](finance-release1-audit-isolation-fixture-20261108.py) skapar endast påhittade filer i en egen tillfällig katalog. Inga parametrar accepterar en verklig databasväg. Den testar allowlist, hash, filtyp, symlänkar, hårdlänkar, rättighetsbitar och otillåtna sidofiler.

Fixturen **bevisar inte** andra UID:ers verkliga läsrättigheter, ACL, nätverksisolering, TOCTOU-/FD-säkerhet, retention eller frånvaro av hoståtkomst. De frågorna kräver senare separat ägarbeslut och oberoende kandidatgranskning.

**Definition of Done för denna checkpoint:** dokumenterat alternativval och skyddsgräns, positiva/negativa syntetiska tester och en separat granskningsbar PR. Detta innebär inte godkänd verklig isolering.

**STOPP:** Ingen verklig Finance-dataläsning, användar-/ACL-ändring, rootkörning, D3, Qwen/G3a, merge, deployment eller handel. BB-132K:s dokumenterade dispositionsfrist 2026-11-10 kl. 14.36.45 svensk tid påverkas inte.
