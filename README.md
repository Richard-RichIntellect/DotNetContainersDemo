# TaskBoard — .NET + Docker containers demo

A small ASP.NET Core 10 Web API (EF Core, SQL Server) demonstrating three
things about containers:

1. **The whole dependency graph lives in containers.** SQL Server isn't
   installed on your machine or emulated — it's a second container, wired
   up through `docker-compose.yml`, that the API talks to over the network
   Docker creates between them.
2. **One command builds and runs everything.** `docker compose up --build`
   builds the API image, starts SQL Server, waits for it to be healthy,
   then starts the API — no manual setup beyond having Docker.
3. **The whole thing is shareable as a file.** `docker save` turns both
   images into one `.tar` a colleague can `docker load` and run — no
   registry account, no "works on my machine."

This repo does **not** set up Docker itself — no Docker Desktop install, no
WSL2/Ubuntu configuration. That's a one-time, per-person setup step; this
repo assumes `docker` and `docker compose` already work wherever you run
it, the same way it assumes the .NET SDK is already installed.

## Prerequisites

- Docker (Docker Desktop, Docker Engine inside WSL2 Ubuntu, or any other
  install) with `docker compose`
- .NET 10 SDK — only needed if you're building/running outside Docker

## Run everything with Docker Compose

```bash
docker compose up --build
```

This one command:

- builds the API image from `src/TaskBoard.Api/Dockerfile`
- pulls the SQL Server image (`mcr.microsoft.com/mssql/server:2022-latest`
  — a public registry, no login needed)
- starts SQL Server and waits for its healthcheck to pass
- starts the API once SQL Server is healthy; the API creates its own
  database schema on first boot (see `Program.cs`)

The API is now running at `http://localhost:8080`.

Stop everything with:

```bash
docker compose down       # keeps the SQL Server data
docker compose down -v    # also deletes the SQL Server data volume
```

## Test it

```bash
./scripts/smoke-test.sh
```

Creates a task, lists it, marks it complete, fetches it, deletes it,
confirms it's gone — end to end through the real containerized API and SQL
Server. Or hit endpoints yourself:

```bash
curl http://localhost:8080/health
curl http://localhost:8080/tasks
curl -X POST http://localhost:8080/tasks \
  -H "Content-Type: application/json" \
  -d '{"title":"Ship it"}'
```

Or browse the API interactively at
[http://localhost:8080/scalar/v1](http://localhost:8080/scalar/v1) — a
Scalar UI generated from the API's own OpenAPI document
(`/openapi/v1.json`), available whenever the API runs in Development (the
default for every workflow in this README).

## Share the image with a colleague

```bash
./scripts/build-and-share.sh
```

Builds the API image, makes sure the SQL Server image is pulled locally
too, and bundles both into `taskboard-images.tar`. Send that file (and this
repo, for the compose files) to a colleague any way you like — a shared
drive, a USB stick, Slack.

Then, on their machine, with Docker running and nothing else set up:

```bash
./scripts/load-and-run.sh
```

This loads both images from the `.tar` and runs `docker compose up -d`.
Because the images already exist locally under the exact names and tags
`docker-compose.yml` expects, compose doesn't try to build or pull
anything — it just starts them. Your colleague never touches NuGet, never
installs SQL Server, and doesn't need internet access at that point. That
`.tar` is deliberately the whole dependency graph, not just your code.

## Running it from Visual Studio

Open `TaskBoardDemo.sln`, select the `docker-compose` startup item, and
press F5 — Visual Studio builds the image, starts both containers, and
attaches the debugger, with breakpoints in `Program.cs` or
`Endpoints/TaskEndpoints.cs` working exactly as they would running locally.

**This needs Docker Desktop specifically.** If you have Docker Engine via
WSL2 but not Docker Desktop (common on machines without a Docker Desktop
license), Visual Studio's Container Tools will refuse to run the
`docker-compose` target. In that case:

1. `docker compose up -d db` from a terminal — starts just SQL Server
2. In Visual Studio, select the plain `https` launch profile (not
   `docker-compose`) and press F5 — it already points at
   `Server=127.0.0.1,14330` via `appsettings.Development.json`, so
   breakpoints work normally against the containerized database
3. When you want to confirm the API itself still works fully
   containerized, run `docker compose up --build` from a terminal and
   `./scripts/smoke-test.sh` — no Visual Studio needed for that check

Two WSL2-specific things baked into that connection string, both found by
actually hitting them:

- The SQL Server container publishes to host port **14330**, not the
  standard 1433 — deliberately, since 1433 is very likely already owned by
  a locally-installed SQL Server/Express instance on a machine that also
  has Visual Studio (a native Windows process bound to that port silently
  intercepts anything meant for the container, with no obvious error to
  say so). If you connect to this database with a tool like SSMS or Azure
  Data Studio, use `127.0.0.1,14330`.
- It's `127.0.0.1`, not `localhost`. On this setup, `localhost` resolved to
  the IPv6 loopback (`::1`), and the SQL client connecting from a native
  Windows process (Visual Studio) to that address intermittently got
  "actively refused" even though the exact same port tested fine with a
  raw TCP check seconds earlier — a WSL2 localhost-forwarding quirk.
  Forcing the literal IPv4 address sidesteps it entirely.

## What's in the box

```
TaskBoardDemo.sln
src/TaskBoard.Api/
  Program.cs                  DI wiring, schema creation on startup, health endpoint
  Data/AppDbContext.cs        EF Core DbContext
  Models/TaskItem.cs          the one table
  Endpoints/TaskEndpoints.cs  minimal API endpoints (list/get/create/complete/delete)
  Dockerfile                  multi-stage build, the shape Visual Studio itself generates
  .dockerignore
docker-compose.yml            image/build definition — what you'd keep if these images
                               were ever pushed to a real registry for a team
docker-compose.override.yml   local-only overrides: ports, the demo SA password, ordering
docker-compose.dcproj         wires the compose files into Visual Studio's Container Tools
scripts/
  build-and-share.sh
  load-and-run.sh
  smoke-test.sh
```

## Notes on the SQL Server password

`Your_password123` is a placeholder used throughout this repo purely to
satisfy SQL Server's complexity rules for a local demo — it is not a
secret worth protecting and it's fine to see it in plain text in
`docker-compose.override.yml`. Don't reuse this pattern for anything that
isn't disposable local dev data; a real deployment would pull the SA
password from an environment-specific secret store instead of a file
that's checked into source control.

## License

MIT — see [LICENSE](LICENSE).
