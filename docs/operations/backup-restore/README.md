# Backup and Restore

## Nivå 2-grind – evidens och återstående verifiering

Bedömning 2026-10-27 mot accepterad main `82fd7bba0dfe41196902bba200d05426f1af2bde`:
**BLOCKED för självständig deployment av databasändringar** enligt den föreslagna
[riskpolicyn](../../../AGENTS.md#3-riskbaserad-arbetsinsats). Detta är dokumentationsgranskning,
inte en ny backup, restore eller produktionsinspektion. Separat granskning krävs tills både
daglig backup och isolerat återställningsprov från verklig backup är verifierade.

| Krav/evidens | Faktiskt underlag | Bedömning |
| --- | --- | --- |
| Viktiga hushålls-/Media-databaser | Befintlig [sexdatabasrutin](../runbooks/approved-operations.md#reused-mechanisms-and-boundaries) omfattar Meal Planner, Media acquisition, Shopping List, Calendar, Settings och Lifecycle i fem mounts. SQLite onlinebackup och per-databas-konsistens; inte atomär backup över alla databaser. | Implementerad omfattning, inte bevis för daglig drift. Finance och övriga volymer omfattas inte. |
| Verklig backup och isolerad återläsning | [Operationsrapport vid 7971fa2](https://github.com/T-bear/BigBrain/blob/7971fa2c8cb2345d6bcd71627923a7d2e33417b9/docs/reports/features/family/school-meals-preparation-20261023.md#skollunch-v3--sealed-operator-deployment-2026-10-25) dokumenterar operatörens PASS för sex databaser, integritet, foreign keys, schema, SHA-256 och isolerad readback 2026-10-25. Agentens manifestläsning nekades. | Ägarrapporterad evidens, inte självständigt verifierad här. Befintlig [backupkod](../../../scripts/approved-ops/backup_support.py) återöppnar backupkopian för integritet/schema/hash; separat återställningsförfarande är inte verifierat. Ett lyckat isolerat återställningsprov enligt den nya grinden är därför UNVERIFIED. Full produktionsrestore krävs inte. |
| Daglig backup | Det granskade underlaget visar engångsbackuper och en godkännandestyrd rutin; ingen verifierad daglig körserie för viktiga databaser. | UNVERIFIED, därmed grinden BLOCKED. Ingen slutsats att servern saknar andra backuper. |
| Senaste avgränsade backupförsök | [Publicerad operationsuppföljning aa5db2a](https://github.com/T-bear/BigBrain/blob/aa5db2a418654ad3dd8c0e3c6c8355fcea974e73/docs/operations/runbooks/approved-operations.md#approved-merges-completed-root-backup-gate-blocked--2026-10-27): verify-backup och backup-approved nekades av gatewayen; exakt intern orsak okänd. | Inget nytt PASS; ingen omkörning eller åtkomst till skyddade filer i detta uppdrag. |

Minsta kvarstående bevis: sanerade körkvitton som styrker faktisk daglig backup och täckning
för berörda viktiga databaser, samt ett verifierbart isolerat återställningsprov från en verklig
backup med revision, tidpunkt, manifestintegritet och läsbart/användbart återställt datalager.
Återanvänd befintlig evidens om den kan styrkas; skapa inte en extra backup av processvana.
Syntetiska tester ersätter inte verkligt backupunderlag. Återställningsprovet får inte använda
eller skriva till produktionsdatabaser. Ingen ny backuplösning, scheduler eller körning beställs här.
Databasändringen behöver fortfarande sin egen riskmotiverade recovery och granskning.

## Befintligt modulunderlag

Nedan bevaras tidigare modulbeskrivningar och deras begränsningar. De är inte körorder;
[frysningsregistret](../freezes.md) och senare avgränsade godkännanden har företräde.

BB-088 cadence rows are operational timestamps/status and add no copied market payload. Prospective
predictions/outcomes retain their existing EODHD source-dependent classification from BB-087;
the overview is derived on read and creates no independent backup artifact.

BB-085 implements Finance provider-tagged backup. WIKI public-domain canonical memory plus
exact WIKI-only feature/backtest/robustness lineage is eligible for local indefinite backup.
EODHD stays outside that class and remains in its subscription/deletion inventory.

Maintenance commands (no provider calls):

```bash
docker compose run --rm --no-deps --entrypoint dotnet api BigBrain.Api.dll finance-backup-create
docker compose run --rm --no-deps --entrypoint dotnet api BigBrain.Api.dll finance-backup-inventory
docker compose run --rm --no-deps --entrypoint dotnet api BigBrain.Api.dll finance-backup-verify <backup-id>
docker compose run --rm --no-deps --entrypoint dotnet api BigBrain.Api.dll finance-backup-restore-drill <backup-id>
docker compose run --rm --no-deps --entrypoint dotnet api BigBrain.Api.dll finance-backup-corruption-drill <backup-id>
docker compose run --rm --no-deps --entrypoint dotnet api BigBrain.Api.dll finance-quarantine-cleanup-drill
```

Backup writes to `.staging-*`, hashes the deterministic data artifact, then publishes data
and a COMPLETE manifest. Incomplete staging is never inventoried and is removed on restart.
Restore drills copy into isolated staging, verify SHA-256 and compare revision/count/lineage,
then remove staging; they never overwrite canonical state. Low disk fails before publication.
Cleanup affects only retention-aged rejected raw payloads and retains manifests/audit evidence.

BB-090 schema closure uses a separate full SQLite pre-migration snapshot only for migration recovery. Stop API/Web first, retain the snapshot inside the protected Finance backup inventory, hash it, and label every provider class: EODHD copies remain deletion-controlled, WIKI is public-domain, and Macro follows its revision rights. Run `finance-evidence-counts` on the source and an isolated copy, then `finance-schema-status` only on the copy. Production migration is allowed only when versions and all evidence counts match. `finance-schema-status` applies missing ordered migrations transactionally and is restart-safe; it never resets the database. The closure snapshot is not a new indefinite entitlement for provider-restricted payloads.

Meal planner, shopping list, calendar, settings and lifecycle have named volumes but no
approved restore drill. Sentinel socket state is ephemeral. Any operator-created EODHD copy
must still join provider deletion inventory/deadline; BB-085 does not authorize one.

Grundstruktur. Ingen procedur är godkänd ännu; framtida runbooks ska skilja onlinebackup, restoretest, retention och moduldata.
