# Backup and Restore

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

Meal planner, shopping list, calendar, settings and lifecycle have named volumes but no
approved restore drill. Sentinel socket state is ephemeral. Any operator-created EODHD copy
must still join provider deletion inventory/deadline; BB-085 does not authorize one.

Grundstruktur. Ingen procedur är godkänd ännu; framtida runbooks ska skilja onlinebackup, restoretest, retention och moduldata.
