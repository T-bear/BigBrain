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
- Docker Compose and container health checks.
- Backend and frontend tests.

The System and Docker modules are read-only. The Web API does not read host resources, mount the Docker socket, execute shell commands, or control the Docker daemon. Their provider contracts are prepared for a future approved Sentinel integration. Until then, System returns `status: "Unavailable"` with null/empty metrics and Docker returns `available: false` with an empty container list. See [ADR 0001](docs/adr/0001-web-api-must-not-control-docker.md).

Explicitly deferred are Control Plane-to-Sentinel communication, real host metrics, real Docker inventory, every Docker mutation (including start, stop, restart, delete and exec), authentication, database persistence, AI, SignalR, Prometheus, external monitoring, media integrations, home automation and plugin loading.

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
```

Stop the BigBrain stack with:

```bash
docker compose down
```

## Local development

.NET 10 SDK is required for the backend and Node.js is required for the frontend.

```bash
dotnet build BigBrain.slnx
dotnet test BigBrain.slnx
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
