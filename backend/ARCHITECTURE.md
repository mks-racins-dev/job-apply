# How the backend works

Two processes share one Postgres database:

- **Api**: what the app talks to (auth, CV upload, profile, matches).
- **Worker**: a background loop that scrapes jobs and scores them for users.

```
Worker (every 2h):  scrape 3 sites ─▶ Jobs table ─▶ prefilter per user ─▶ Claude scores ─▶ Matches table
Api:                register/login ─▶ upload CV ─▶ Claude builds profile ─▶ user confirms ─▶ GET /matches
```

## Projects

| Project | Responsibility |
|---|---|
| `JobMagnet.Data` | EF Core entities, `JobMagnetDbContext`, migrations, `TextNormalizer` |
| `JobMagnet.Scrapers` | One `IJobSource` per site, HTML/JSON parsers, `JobIngestionService` |
| `JobMagnet.Matching` | `ClaudeJsonClient`, CV → profile, prefilter, scoring, `MatchingService` |
| `JobMagnet.Api` | Minimal API endpoints, JWT auth, PDF/DOCX text extraction |
| `JobMagnet.Worker` | `PipelineBackgroundService`: runs ingestion, then matching, on a timer |

## Data model

- `User`: email, password hash.
- `CvDocument`: the uploaded file path and its extracted text.
- `Profile`: one per user. The structured profile is stored as `jsonb` so its schema can change without migrations. `IsConfirmed` becomes true once the user saves it.
- `Job`: unique per `(Source, ExternalId)`. `DedupeHash` identifies the same vacancy posted on several sites. `IsActive` turns false when a job hasn't been seen for 3 days.
- `Match`: one per `(User, Job)`, with a score from 0 to 100, a reason, matched and missing skills, and a status (`New`/`Seen`/`Interested`/`Dismissed`).

## Scraping

`PagedJobSource` fetches pages 1…`MaxPagesPerSource` and waits `DelayBetweenRequests` between them. It stops early on an HTTP error or an empty page. Each subclass only provides a page URL and a parser:

| Source | What we read | Known gaps |
|---|---|---|
| visidarbi.lv | `div.item` cards, `?page=N` | ~17 listings per page in plain HTML; no description |
| cv.lv | `__NEXT_DATA__` JSON embedded in the page | city is only a numeric id, so it's left empty |
| ss.lv | `tr[id^=tr_]` table rows, `pageN.html` | no company or salary; the ad text is used as the description |

Parsers are static and work without HTTP (`Parse(html)`), so they can be tested with saved HTML. When a site changes its markup, the fix goes in its parser and in `ParserTests`.

`JobIngestionService.UpsertAsync`:
1. Known `(Source, ExternalId)`: updates `LastSeenAt` and keeps the job active.
2. Same `DedupeHash` already active on another source: skips the listing, because the first source seen stays canonical.
3. Otherwise: inserts a new `Job`.

After all sources run, it deactivates jobs older than `InactiveAfter`. A failing site is logged and skipped.

## CV → profile

`POST /cv` takes a PDF or DOCX of up to 10 MB. PdfPig or OpenXML extracts the text, then `CvProfileExtractor` sends it to Claude with a JSON schema (structured outputs), so the reply is always valid profile JSON. The profile is saved unconfirmed. `PUT /profile` stores the user's edited version and marks it confirmed. **Only confirmed profiles get matched.**

## Matching (cost control)

For each confirmed profile, `MatchingService`:
1. Loads active jobs the user hasn't been matched with yet.
2. **Prefilter** (`CandidatePrefilter`, no LLM): counts how many profile skills and roles appear in the job title and description, adds a bonus for a matching city, and keeps the top `MaxCandidatesPerUser` (20).
3. **Scores** them with Claude in batches of `JobsPerScoringRequest` (10). `JobScorer` drops unknown job ids and clamps scores to 0–100.
4. Saves every score, low ones included, so no job is ever sent to Claude twice for the same user.

So the cost per user per run is at most `20 / 10 = 2` Claude calls.

All Claude calls go through `IClaudeJsonClient` (model `claude-opus-5-5`, low effort, JSON schema output). If Claude declines a request, the client returns `null` and the caller skips that CV or batch.

## API

| Endpoint | Auth | |
|---|---|---|
| `GET /health` | – | Checks that the database is reachable |
| `POST /auth/register`, `POST /auth/login` | – | Returns `{ token }` (a JWT, valid 30 days) |
| `POST /cv` | ✓ | multipart `file`; returns the draft profile |
| `GET /profile`, `PUT /profile` | ✓ | `PUT` body is the profile JSON and confirms it |
| `GET /matches?minScore=50` | ✓ | Active, not-dismissed matches, best first (max 100) |
| `PATCH /matches/{id}` | ✓ | `{ "status": "Interested" }` |

Authenticated endpoints need the header `Authorization: Bearer <token>`.

## Configuration

| Key | Where | Default |
|---|---|---|
| `ConnectionStrings:Postgres` | both | local docker Postgres |
| `ANTHROPIC_API_KEY` (env) | both | required for CV parsing and matching |
| `Jwt:SigningKey` | api | required, at least 32 chars (dev key in `appsettings.Development.json`) |
| `Storage:UploadsPath` | api | `uploads` |
| `Database:MigrateOnStartup` | api | `true` |
| `Scrapers:*` | worker | 3 pages, 10s delay, every 2h, inactive after 3 days |
| `Matching:*` | worker | 20 candidates, batches of 10, notify threshold 70 |
| `Claude:Model`, `Claude:MaxTokens` | both | `claude-opus-5-5`, 16000 |

You can override any key with an environment variable, using `__` as the separator (e.g. `Scrapers__MaxPagesPerSource=1`).

## Adding a job source

1. Write `Parsing/XyzParser.cs` with `Parse(string html) → IReadOnlyList<ScrapedJob>`, plus tests.
2. Write `Sources/XyzSource.cs : PagedJobSource` that overrides `Name`, `GetPageUrl` and `Parse`.
3. Register it with `AddSource<XyzSource>(services)` in `Scrapers/ServiceCollectionExtensions.cs`.
4. Check the site's robots.txt and terms of service first.

## Not built yet

Push notifications (the score threshold exists, but nothing is sent), Google/Apple login, OCR for scanned CVs, using dismissals to improve scoring, and city names for cv.lv.
