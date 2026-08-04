# BigBrain

BigBrain is a modular control plane for a Debian-based home server. Sprint 2 adds safe, read-only system monitoring and a Docker inventory contract to the stable modular-monolith foundation.

## Sprint 2 scope

Included:

- ASP.NET Core Web API on .NET 10 LTS.
- React, TypeScript, and Vite web application.
- In-memory module registry with separate System and Docker modules.
- `GET /api/v1/system/health`.
- `GET /api/v1/system/overview` with a stable metrics contract and an explicit unavailable result until Sentinel integration exists.
- `GET /api/v1/docker/containers` with an explicit unavailable result until Sentinel integration exists.
- `GET /api/v1/modules`.
- Responsive dashboard with live system values, provider-driven module status, clear unavailable/error states, and non-overlapping system polling approximately every five seconds.
- Read-only Media dashboard foundation for normalized Jellyfin, Sonarr, Radarr, Prowlarr and qBittorrent status.
- Docker Compose and container health checks.
- Backend and frontend tests.

The System and Docker modules are read-only. The Web API does not read host resources, mount the Docker socket, execute shell commands, or control the Docker daemon. Their provider contracts are prepared for a future approved Sentinel integration. Until then, System returns `status: "Unavailable"` with null/empty metrics and Docker returns `available: false` with an empty container list. See [ADR 0001](docs/adr/0001-web-api-must-not-control-docker.md).

Explicitly deferred are Control Plane-to-Sentinel communication, real host metrics, real Docker inventory, every Docker mutation (including start, stop, restart, delete and exec), every media mutation, authentication, database persistence, AI, SignalR, Prometheus, external monitoring, home automation and plugin loading.

Media Sprint 1 uses only documented application HTTP APIs and exposes `GET /api/v1/modules/media`. Credentials remain runtime configuration and all media mutations are deferred. See [Media Module documentation](docs/modules/media.md).
The verified qBittorrent 5.2+ integration uses its official stateless Bearer API key authentication. Dashboard reads remain read-only; Download Control adds one narrowly bounded, previewed and confirmed removal endpoint with file preservation as the default.

BigBrain Web uses the token-based [theme contract v1](docs/design-system/theme-contract-v1.md). The separately versioned Jellyfin adapter under `themes/jellyfin/` is a manual artifact and is never installed automatically.
The selector offers `Mörkt`, `Ljust` and the graphite-and-brass `Obsidian Gold`; the stored choice is restored on reload.

To connect the module to the existing media stack, follow [TESTING.md](TESTING.md). Runtime credentials belong in an ignored local `.env` copied from `.env.example`.

## Repository layout

```text
src/
├── BigBrain.Api/       Versioned HTTP API and health endpoints
├── BigBrain.Modules/   Module contracts, registry, metrics providers, System and Docker modules
└── BigBrain.Web/       React application and module-driven dashboard
tests/
└── BigBrain.Api.Tests/ API and module registry tests
```

Future logical components documented in `ARCHITECTURE.md` are created only when a sprint gives them concrete responsibility.

## Run with Docker Compose

```bash
docker compose up --build -d
```

The dashboard is available at `http://localhost:13000`. The API is available at `http://localhost:18080`. The non-default loopback ports keep this development stack isolated from existing home-server services.

```bash
curl http://localhost:18080/api/v1/system/health
curl http://localhost:18080/api/v1/system/overview
curl http://localhost:18080/api/v1/docker/containers
curl http://localhost:18080/api/v1/modules
curl http://localhost:18080/api/v1/modules/media
```

Stop the BigBrain stack with:

```bash
docker compose down
```

## Local development

The .NET SDK is not installed on the Debian host. Backend builds, tests and analyzers
run in the pinned .NET 10 SDK container. The temporary artifacts path avoids changing
or depending on host-owned `bin` and `obj` directories.

```bash
docker run --rm --user "$(id -u):$(id -g)" \
  --volume "$PWD:/workspace" \
  --workdir /workspace \
  --env BIGBRAIN_REPOSITORY_ROOT=/workspace \
  mcr.microsoft.com/dotnet/sdk:10.0 \
  dotnet build BigBrain.slnx --configuration Release --artifacts-path /tmp/bigbrain-artifacts

docker run --rm --user "$(id -u):$(id -g)" \
  --volume "$PWD:/workspace" \
  --workdir /workspace \
  --env BIGBRAIN_REPOSITORY_ROOT=/workspace \
  mcr.microsoft.com/dotnet/sdk:10.0 \
  dotnet test BigBrain.slnx --configuration Release --artifacts-path /tmp/bigbrain-artifacts
```

```bash
cd src/BigBrain.Web
npm ci
npm run build
npm test
```

## Architecture and working rules

- `ARCHITECTURE.md` contains the approved architecture baseline and roadmap.
- `AGENTS.md` contains permanent repository working rules.
- Public APIs are versioned and API errors use Problem Details.
- The API and Web containers never mount the Docker socket.
