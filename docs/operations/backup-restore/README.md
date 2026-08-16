# Backup and Restore

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
