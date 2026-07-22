# BigBrain

BigBrain is a modular control plane for a Debian-based home server. Sprint 1 provides only a minimal, stable foundation: a versioned ASP.NET Core API, an in-memory module registry, a System module, and a React dashboard driven by registered widget metadata.

## Sprint 1 scope

Included:

- ASP.NET Core Web API on .NET 10 LTS.
- React, TypeScript, and Vite web application.
- In-memory module registry and System module.
- `GET /api/v1/system/health`.
- `GET /api/v1/modules`.
- Module-driven placeholder dashboard.
- Docker Compose and container health checks.
- Backend and frontend tests.

Explicitly excluded are database persistence, authentication, Docker Engine integration, Host Agent, AI, media integrations, home automation, and plugin loading.

## Repository layout

```text
src/
├── BigBrain.Api/       Versioned HTTP API and health endpoints
├── BigBrain.Modules/   Module contracts, registry, and System module
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
curl http://localhost:18080/api/v1/modules
```

Stop the Sprint 1 stack with:

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
