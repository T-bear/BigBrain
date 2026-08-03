# Sentinel

## Syfte

Beskriva Sentinel som avgränsad capability-process.

## Verifierade fakta

- Sentinel kommunicerar lokalt över Unix-socket med autentiserat protokoll.
- Implementerade capabilities omfattar allowlistad uptime-, CPU-, minnes- och diskläsning.

## Viktiga tekniska lärdomar

- Capability-kontrakt ska vara strukturerade, minsta möjliga och versionssatta.
- Föreslagna ADR:er är inte accepterade beslut förrän status ändras uttryckligen.

## Vanliga feltolkningar

- Sentinel är inte en generell shell- eller Docker-proxy.
- `BigBrain.Brain` är inte ett alternativt privilegierat dumpningslager.

## Relaterade runbooks

- [System baseline capture](../operations/runbooks/system-baseline-capture.md)

## Relaterade dokument och ADR:er

- [ADR 0001](../adr/0001-web-api-must-not-control-docker.md)
- [ADR 0002](../adr/0002-sentinel-exclusive-system-access.md)
- [ADR 0005](../adr/0005-read-only-system-metrics-capability.md)

## Senast verifierad

2026-08-03.

## Källa och evidens

Arkitektur, ADR 0001–0009, säkerhetsdokument och `docs/STATUS.md`.
