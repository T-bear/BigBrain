# BigBrain appliance startup, shutdown and recovery

## Normal operation

Start with one physical power-button press. Stop with one brief press and wait for poweroff.
Debian 13/systemd defaults `HandlePowerKey=poweroff`. A forced long hold bypasses software.

## Install and verify

```bash
sudo ./scripts/install-bigbrain-host-service.sh && sudo systemctl start bigbrain.service
./scripts/verify-bigbrain-recovery.sh
```

The idempotent installer writes only the repository path, validates/enables the unit and
does not copy secrets. Diagnose with `systemctl status bigbrain.service`,
`journalctl -u bigbrain.service -b` and `docker compose ps`. Never render resolved Compose
environment where credentials exist.

## Degraded/recovery

Read `/api/v1/system/recovery`. Never delete/recreate a failed database. Preserve volumes
and use read-only filesystem/ownership/SQLite diagnostics. Missing external media may be
degraded while local modules remain available. Interrupted EODHD acquisition publishes no
partial revision and is not retried for that symbol the same UTC day.

## Rollback

```bash
sudo ./scripts/uninstall-bigbrain-host-service.sh
```

This preserves repository data, images and volumes.

## Physical owner gate

Run the verifier, short-press power, wait for full poweroff, press once, wait, and run the
verifier again. Expect enabled/active service and `previousShutdown=clean`. BIOS `Restore on
AC Power Loss` may optionally be Last State/Power On; software cannot attest BIOS. Future UPS
support may use NUT/apcupsd after hardware selection.
