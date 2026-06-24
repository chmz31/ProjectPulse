# ProjectPulse API

[![.NET CI](https://github.com/chmz31/ProjectPulse/actions/workflows/dotnet-ci.yml/badge.svg?branch=master)](https://github.com/chmz31/ProjectPulse/actions/workflows/dotnet-ci.yml)

ProjectPulse is a .NET 8 REST API for managing user-owned projects. It is a backend portfolio and learning project focused on practical API design, authentication, resource-level authorization, persistence, automated testing, and repeatable builds.

The project is intentionally compact. It demonstrates production-minded security and engineering practices without presenting itself as a production-ready enterprise platform.

## Features

- User registration and login with JWT bearer authentication.
- PBKDF2-SHA256 password hashing with per-password salts.
- Opaque refresh tokens stored only as SHA-256 hashes.
- Refresh-token rotation, expiry checks, revocation, and reuse protection.
- CRUD endpoints for projects with pagination, filtering, and sorting.
- Resource-level authorization: users can access only their own projects.
- Swagger/OpenAPI documentation.
- EF Core migrations with SQLite persistence.
- Startup validation for JWT and database configuration.
- Integration tests using `WebApplicationFactory` and isolated SQLite databases.
- Reproducible SDK/package resolution and GitHub Actions CI.

## Tech stack

| Area | Technology |
| --- | --- |
| Runtime | .NET 8 / ASP.NET Core Web API |
| Persistence | Entity Framework Core 8 and SQLite |
| Authentication | JWT bearer tokens and opaque refresh tokens |
| Validation | FluentValidation |
| API documentation | Swagger / Swashbuckle |
| Testing | xUnit and `Microsoft.AspNetCore.Mvc.Testing` |
| Build reproducibility | `global.json` and NuGet lock files |
| CI | GitHub Actions |

SQLite keeps local setup simple and makes the repository easy to evaluate. It is not intended here as a recommendation for a larger multi-instance production system.

## Security model

- JWT issuer, audience, and signing key are validated during startup.
- The signing key must be at least 32 UTF-8 bytes and cannot be a known placeholder or demo value.
- Critical configuration is validated before migrations or seed operations can modify the database.
- Raw refresh tokens are returned to clients but never stored; only their SHA-256 hashes are persisted.
- Refresh tokens are one-time credentials. Rotation revokes the old token, and reuse is rejected.
- Project ownership comes from the authenticated JWT subject, never from client input.
- Cross-user project lookups return `404 Not Found` to avoid revealing resource existence.

Secrets must be supplied through environment variables, development secret storage, or a deployment secret manager. Do not commit real credentials or signing keys.

## Requirements

- [.NET SDK 8.0.422](https://dotnet.microsoft.com/download/dotnet/8.0), or a compatible patch selected by `global.json`.
- Optional: `dotnet-ef` 8.0.28 for manual migration commands.

Confirm the selected SDK:

```bash
dotnet --version
```

## Configuration

ASP.NET Core maps double underscores in environment-variable names to configuration sections.

| Environment variable | Required | Purpose |
| --- | --- | --- |
| `Jwt__Issuer` | Yes | Expected JWT issuer |
| `Jwt__Audience` | Yes | Expected JWT audience |
| `Jwt__Key` | Yes | Random signing key of at least 32 UTF-8 bytes |
| `ConnectionStrings__Default` | Outside Development | SQLite connection string |
| `EnableSwagger` | No | Enables Swagger outside Development when `true` |
| `SEED` | No | Enables optional development seed data when `true` |
| `SEED_ADMIN_PASSWORD` | When seeding | Password for the seeded development admin |

Development settings provide local issuer, audience, and SQLite defaults. A real development signing key must still be supplied because tracked placeholder values are rejected.

## Run locally

Restore locked dependencies:

```bash
dotnet restore --locked-mode
```

Generate a temporary development signing key and run the API.

PowerShell:

```powershell
$env:Jwt__Key = [Convert]::ToBase64String(
  [Security.Cryptography.RandomNumberGenerator]::GetBytes(32)
)
dotnet run --project .\ProjectPulse.Api\ProjectPulse.Api.csproj
```

Bash:

```bash
export Jwt__Key="$(openssl rand -base64 32)"
dotnet run --project ./ProjectPulse.Api/ProjectPulse.Api.csproj
```

With the default launch profile, the API listens on `http://localhost:5241`. Swagger is available in Development at:

```text
http://localhost:5241/swagger/index.html
```

For a non-Development environment, also provide explicit issuer, audience, and connection-string values:

```text
Jwt__Issuer=<expected-issuer>
Jwt__Audience=<expected-audience>
Jwt__Key=<random-secret-at-least-32-bytes>
ConnectionStrings__Default=Data Source=<database-path>
```

## Docker

The image uses a locked multi-stage .NET 8 build and runs the published API as a non-root user. Build it from the repository root so `global.json` and NuGet lock files are available:

```bash
docker build -t projectpulse-api -f ProjectPulse.Api/Dockerfile .
```

Generate a signing key in the host shell before starting a container.

PowerShell:

```powershell
$env:PROJECTPULSE_JWT_KEY = [Convert]::ToBase64String(
  [Security.Cryptography.RandomNumberGenerator]::GetBytes(32)
)
$env:PROJECTPULSE_CONNECTION_STRING = "Data Source=/data/projectpulse.db"
```

Bash:

```bash
export PROJECTPULSE_JWT_KEY="$(openssl rand -base64 32)"
export PROJECTPULSE_CONNECTION_STRING="Data Source=/data/projectpulse.db"
```

Run the image with configuration supplied through environment variables and a named volume mounted at `/data`:

```bash
docker run --rm --name projectpulse-api -p 8080:8080 \
  -e Jwt__Issuer=ProjectPulse \
  -e Jwt__Audience=ProjectPulse \
  -e Jwt__Key="$PROJECTPULSE_JWT_KEY" \
  -e "ConnectionStrings__Default=Data Source=/data/projectpulse.db" \
  -e EnableSwagger=true \
  -v projectpulse-data:/data \
  projectpulse-api
```

Alternatively, Compose uses the same host signing-key variable:

```bash
docker compose up --build
```

The API is then available on `http://localhost:8080`; `/hello` is an unauthenticated smoke-check endpoint and Swagger is at `/swagger/index.html` for this local Compose configuration.

The SQLite database is stored at `/data/projectpulse.db`. Mount `/data` to retain it when containers are replaced. This container setup is intended for local/demo evaluation; SQLite and automatic startup migrations are not designed here for horizontally scaled deployment.

## Database migrations

The application currently applies pending EF Core migrations during startup. For manual migration management, install the matching CLI tool if needed and run:

```bash
dotnet tool install --global dotnet-ef --version 8.0.28
dotnet ef database update \
  --project ./ProjectPulse.Api/ProjectPulse.Api.csproj \
  --startup-project ./ProjectPulse.Api/ProjectPulse.Api.csproj
```

The required JWT and database configuration must be available when invoking the startup project.

Important migration behavior:

- The ownership migration deletes legacy projects that have no owner. Those rows predate ownership and cannot be assigned safely to a real user.
- The refresh-token hashing migration deletes legacy raw refresh-token rows. Raw values cannot be safely converted while preserving active client sessions.

## Run tests

The integration suite starts the API with safe test configuration and uses isolated temporary SQLite databases.

```bash
dotnet restore --locked-mode
dotnet build ./ProjectPulse.Api/ProjectPulse.Api.csproj --configuration Release --no-restore
dotnet test --configuration Release --no-restore
```

Current coverage focuses on startup, project ownership boundaries, and refresh-token storage and rotation.

## CI

The [`.NET CI` workflow](.github/workflows/dotnet-ci.yml) runs on pushes and pull requests targeting `master`. It performs a locked restore, builds the API in Release mode, and runs the Release test suite with least-privilege repository permissions.

## API overview

| Method | Route | Authentication | Purpose |
| --- | --- | --- | --- |
| `POST` | `/auth/register` | No | Register a user |
| `POST` | `/auth/login` | No | Issue access and refresh tokens |
| `POST` | `/auth/refresh` | No | Rotate a refresh token |
| `POST` | `/auth/logout` | No | Revoke a refresh token |
| `GET` | `/users/me` | JWT | Return the current user claims |
| `GET` | `/projects` | JWT | List the current user's projects |
| `POST` | `/projects` | JWT | Create an owned project |
| `GET` | `/projects/{id}` | JWT | Get an owned project |
| `PUT` | `/projects/{id}` | JWT | Update an owned project |
| `DELETE` | `/projects/{id}` | JWT | Delete an owned project |

## Project structure

```text
ProjectPulse/
├── .github/workflows/          # GitHub Actions CI
├── ProjectPulse.Api/
│   ├── Controllers/            # HTTP endpoints
│   ├── Domain/                 # EF Core entities
│   ├── DTOs/                   # Request/response contracts and validators
│   ├── Migrations/             # Database schema history
│   ├── Persistence/            # DbContext and startup seeding
│   ├── Security/               # Claims helpers
│   ├── Services/               # Password and token services
│   └── Program.cs              # Dependency registration and HTTP pipeline
├── ProjectPulse.Api.Tests/     # Integration tests and test application factory
├── global.json                 # Pinned .NET SDK
└── ProjectPulse.sln
```

## Known limitations and roadmap

- SQLite and automatic startup migrations favor local development over multi-instance deployment.
- Authentication does not yet include email verification, password reset, MFA, or account lockout.
- Projects have a single owner; collaboration and project-specific roles are not implemented.
- Refresh-token state is database-backed and designed for this single-service scope.
- Test coverage is intentionally focused rather than exhaustive.
- Docker support is suitable for local/demo evaluation; production deployment would require additional infrastructure hardening.

Possible next steps include broader validation/error-path tests, structured observability, PostgreSQL deployment support, and collaborative project membership.

## Author

Christian Manrique Zanetti — [LinkedIn](https://www.linkedin.com/in/camz31/)
