# Deployment

## Finance EODHD Free

EODHD is disabled by default. The named `finance-market-data` volume is mounted only into
API at `/finance-data`; it is not part of backup automation. Configure secrets and lifecycle
through `FINANCE__EODHD__APITOKEN`, `FINANCE__EODHD__ENABLED`,
`FINANCE__EODHD__ACCOUNTACTIVE` and, only after verified termination,
`FINANCE__EODHD__ENTITLEMENTENDSATUTC`. Never render resolved Compose configuration when a
token is present. Use the [retention/deletion runbook](../runbooks/finance-eodhd-retention-deletion.md)
for enablement, expiry and destructive maintenance.

BB-078 deployed the enabled read-only path on 2026-08-11. The persistent volume contains
real EODHD Free memory and must be preserved on recreation. Use the runbook's sanitized
runtime-evidence command to verify counts/replay without printing the token or raw payloads.

Grundstruktur. Ingen ny normativ deploymentprocedur införs i denna fas.
