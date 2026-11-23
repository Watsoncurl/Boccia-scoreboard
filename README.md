# Boccia-scoreboard
Boccia is a Paralympic sport designed for people with severe disabilities. It is a game of precision and strategy, similar to bowling or bocce

## Proposed Solution
### 1. Project Structure
#### Backend:

A .NET Core project that acts as a server.

It contains the game logic, handles the database, and uses SignalR to notify clients of changes.

#### Frontend:

Two desktop applications (Windows Forms or WPF):

Control Application: To enter the values of the game.

Display Application: To display the scoreboard in real time.

### 2. Workflow
Start of Game:

From the Control Application, the names of the players/teams are entered and the game starts.

This information is sent to the server, which stores it in the database.

Play Log:

During the game, from the Control Application, the thrown balls and their position relative to the jack are recorded.

The server calculates the score and notifies the Display Application to update the scoreboard.

### 3. Real-time Visualization:

The Display Application receives updates from the server using SignalR and displays the updated dialer.

### 4. End of Game:

At the end of the game, the server stores the result in the database and notifies both applications.


## Current Implementation

| Project | Description |
|---|---|
| `BocciaServer` | ASP.NET Core API + SignalR hub (`/hubs/scoreboard`). Game rules, scoring and persistence (EF Core, SQL Server). |
| `BocciaShared` | DTOs, enums, scoring rules and SignalR hub contract shared by the server and the Blazor apps. |
| `BocciaClient` | API client and SignalR connection (auto-reconnect) used by both Blazor apps. |
| `BocciaControlApp` | Blazor web app to operate a match: athletes, new matches, throws, measurements, penalties, ends and tie-breaks. |
| `BocciaViewApp` | Blazor web app that shows the live scoreboard (TV/projector or phone). |
| `BocciaDatabase` | SQL Server database project (SDK-style, builds on Linux). |
| `BocciaServer.Tests` | xUnit tests: scoring rules and full match flow (SQLite in-memory). |

All apps listen on every network interface so any device on the LAN can open them:
server `:5123`, control app `:5201`, scoreboard `:5202`.

### Quick start without SQL Server (development)

The server can use a local SQLite file instead of SQL Server. Tables and divisions are created automatically:

```bash
dotnet run --project BocciaServer --launch-profile sqlite
dotnet run --project BocciaControlApp
dotnet run --project BocciaViewApp
```

Then open the control app at `http://<host>:5201` and the scoreboard at `http://<host>:5202`.
If the apps run on another machine than the server, set `BocciaServer:BaseUrl` in their `appsettings.json`.

### Running locally with SQL Server

```bash
# 1. SQL Server (e.g. Docker) and deploy the schema
dotnet build BocciaDatabase          # produces BocciaDatabase/bin/Debug/BocciaDatabase.dacpac
# deploy the .dacpac with SqlPackage or the SQL Database Projects extension

# 2. Connection string (password kept out of the repo)
cd BocciaServer
dotnet user-secrets set "ConnectionStrings:BocciaDb" "Server=localhost,1433;Database=BocciaDb;User Id=sa;Password=<password>;TrustServerCertificate=True;Encrypt=False"

# 3. Run
dotnet run --project BocciaServer
dotnet run --project BocciaControlApp
dotnet run --project BocciaViewApp

# Tests
dotnet test
```

See `BocciaServer/BocciaServer.http` for example API calls.
