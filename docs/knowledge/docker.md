# Docker

## Syfte

Beskriva BigBrains verifierade containergränser.

## Verifierade fakta

- API och Web ska inte montera Docker-socketen.
- Sentinel är den avsedda nodlokala capability-gränsen; dess detaljer styrs av arkitektur och ADR-status.

## Viktiga tekniska lärdomar

- Loopback är namespace-lokal; använd tjänstens konfigurerade Docker-DNS-namn för container-till-container-trafik.
- Container-ID, `StartedAt` och restart count är användbara före/efter-bevis.

## Vanliga feltolkningar

- Read-only Docker-socket är fortfarande en känslig kontrollkanal.
- En container som är `running` behöver inte ha fungerande applikationsintegration.

## Relaterade runbooks

- [System baseline capture](../operations/runbooks/system-baseline-capture.md)

## Relaterade dokument och ADR:er

- [Arkitektur](../../ARCHITECTURE.md)
- [ADR 0002](../adr/0002-sentinel-exclusive-system-access.md)

## Senast verifierad

2026-08-03.

## Källa och evidens

Compose, arkitekturdokument, säkerhetsdokument och daterade baselines.
