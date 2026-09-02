# Attendly Unified

Attendly Unified is a university attendance solution containing a Windows Forms desktop client and an ASP.NET Core web client. Both clients use one ASP.NET Core API, ensuring that authorization, validation, paging, and attendance rules remain consistent.

This solution is separate from the original `StudentAttendanceSystem` project. It does not reference or modify the original application.

## Documentation

- [Entity Relationship Diagram](docs/ER-DIAGRAM.md)
- [Architecture and Process Flow Charts](docs/FLOWCHARTS.md)

## Solution architecture

```text
AttendlyUnified
├── docs
│   ├── ER-DIAGRAM.md
│   └── FLOWCHARTS.md
└── src
    ├── Attendly.Domain
    ├── Attendly.Application
    ├── Attendly.Infrastructure
    ├── Attendly.Api
    ├── Attendly.Web
    └── Attendly.WinForms
```

| Project | Responsibility |
|---|---|
| `Attendly.Domain` | Core entities with no UI or database dependency |
| `Attendly.Application` | Request/response contracts and repository interfaces |
| `Attendly.Infrastructure` | SQL Server queries and PBKDF2 password verification |
| `Attendly.Api` | HTTP endpoints and the database security boundary |
| `Attendly.Web` | ASP.NET Core Razor Pages client |
| `Attendly.WinForms` | Windows desktop client |

## Implemented starter features

- API health check
- Active-user login using the existing PBKDF2-SHA256 password format
- SQL Server student retrieval
- Primary-key ordering by `StudentId`
- Server-side search
- Server-side paging with 10 students per page
- Web API-status dashboard
- WinForms sign-in and student-list starter screen

The new solution is an extensible foundation. The remaining administration, timetable, attendance-entry, and reporting screens from the original project should be migrated through the same API pattern before this replaces the original application.

## Requirements

- Windows 10 or newer for the WinForms client
- .NET 8 SDK
- SQL Server or SQL Server Express
- The `StudentAttendanceSystem` database schema
- A valid administrator or lecturer record in the `Users` table

## Database configuration

Only `Attendly.Api` connects directly to SQL Server. Configure the connection string in:

`src/Attendly.Api/appsettings.json`

The default development configuration uses Windows authentication:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=.;Database=StudentAttendanceSystem;Integrated Security=True;TrustServerCertificate=True;"
}
```

For a local SQL username and password, use user secrets or an environment variable instead of committing the password:

```powershell
dotnet user-secrets init --project src/Attendly.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=.;Database=StudentAttendanceSystem;User Id=YOUR_USER;Password=YOUR_PASSWORD;TrustServerCertificate=True;" --project src/Attendly.Api
```

Production deployments should use a secret manager and a restricted database account.

## Build

From the `AttendlyUnified` directory:

```powershell
dot restore
dot build --configuration Release
```

## Run locally

Start the projects in this order.

### 1. Start the API

```powershell
dotnet run --project src/Attendly.Api
```

The development HTTPS address is `https://localhost:7049`. Verify it at:

```text
https://localhost:7049/api/health
```

### 2. Start the web client

```powershell
dotnet run --project src/Attendly.Web
```

The web client is configured to call the API at `https://localhost:7049`.

### 3. Start the WinForms client

```powershell
dotnet run --project src/Attendly.WinForms
```

In Visual Studio, configure multiple startup projects when you want to start the API and one client together.

## Current API endpoints

| Method | Endpoint | Purpose |
|---|---|---|
| `GET` | `/api/health` | Check API availability |
| `POST` | `/api/auth/login` | Validate an active user |
| `GET` | `/api/rooms/{roomId}/students?page=1&pageSize=10&search=` | Retrieve a paged student list |

Example login request:

```json
{
  "username": "admin",
  "password": "your-password"
}
```

## Security model

- Client applications never receive database credentials.
- SQL parameters are used for user input.
- Passwords are verified against PBKDF2-SHA256 hashes.
- Inactive users cannot sign in.
- HTTPS is enabled for local API profiles.
- Full production use still requires token or secure-cookie authentication and endpoint role authorization.

## Paging convention

List endpoints should use these rules:

- Default page size: 10
- Maximum page size: 100
- Ordering: ascending primary key
- Response: items, current page, page size, total count, and total pages

## Recommended next implementation order

1. Add JWT or secure-cookie authentication and role authorization.
2. Add room, subject, lecturer, and assignment endpoints.
3. Add timetable management and overlap validation.
4. Add generated class sessions and lecturer attendance entry.
5. Add home-room and monthly reports.
6. Complete matching responsive web and WinForms interfaces.
7. Add unit, integration, and authorization tests.

## Design documentation

The ER diagram documents all database tables and relationships. The flow-chart document covers system architecture, login, attendance entry, weighted calculation, and paging. Both use Mermaid and render in GitHub, compatible Markdown viewers, and supported Visual Studio extensions.
