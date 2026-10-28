# Job Magnet backend

.NET 10 solution: `JobMagnet.Api`, `JobMagnet.Worker`, `JobMagnet.Scrapers`, `JobMagnet.Matching`, `JobMagnet.Data`.

## Run locally

```sh
docker compose up --build        # postgres + api (:8080) + worker
curl localhost:8080/health
```

Set `ANTHROPIC_API_KEY` in the shell or a `.env` file for CV parsing and matching.

## Migrations

```sh
dotnet ef migrations add <Name> -p src/JobMagnet.Data -s src/JobMagnet.Api -o Migrations
```

The API applies pending migrations on startup when `Database:MigrateOnStartup` is true.

See [ARCHITECTURE.md](ARCHITECTURE.md) for how the pieces fit together.
