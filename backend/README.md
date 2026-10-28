# Job Magnet backend

.NET 10 solution: `JobMagnet.Api`, `JobMagnet.Worker`, `JobMagnet.Web` (Blazor UI), `JobMagnet.Scrapers`, `JobMagnet.Matching`, `JobMagnet.Data`.

## Run locally

```sh
docker compose up --build        # postgres + api (:8080) + worker + web (:8081)
curl localhost:8080/health
```

Open http://localhost:8081 for the web app.

Without Docker, run the API and the web app side by side (Postgres still needs to be running):

```sh
dotnet run --project src/JobMagnet.Api     # http://localhost:5034
cd src/JobMagnet.Web && dotnet run         # web, talks to :5034
```

Set `ANTHROPIC_API_KEY` in the shell or a `.env` file for CV parsing and matching.

## Migrations

```sh
dotnet ef migrations add <Name> -p src/JobMagnet.Data -s src/JobMagnet.Api -o Migrations
```

The API applies pending migrations on startup when `Database:MigrateOnStartup` is true.

See [ARCHITECTURE.md](ARCHITECTURE.md) for how the pieces fit together.
