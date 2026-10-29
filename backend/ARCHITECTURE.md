# How the backend works

Two backend processes share one Postgres database, and a web app sits in front of them:

- **Api**: what clients talk to (auth, CV upload, profile, matches).
- **Worker**: a background loop that scrapes jobs and scores them for users.
- **Web**: a Blazor Server UI that only talks to the Api over HTTP, just like the future mobile app will.

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
| `JobMagnet.Web` | Blazor UI: sign-in, CV upload, profile review, the job orbit |

## Data model

- `User`: email, password hash, avatar (index into the web app's 16 pixel avatars).
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

## Jobs pasted in by the user (LinkedIn)

LinkedIn forbids scraping, so users bring those jobs in themselves: on `/add-job` they paste a posting's text, plus an optional link.

`POST /jobs` → `SubmittedJobService`:
1. Validates the link (only `http`/`https`, tracking query removed) and requires a confirmed profile.
2. If this user already pasted the same link (or, with no link, the same text), returns that match and un-dismisses it if needed.
3. `JobPostingExtractor` (Claude) pulls title, company, city, monthly salary and a clean description out of the page noise.
4. Scores the job right away with `JobScorer` and saves the `Job` plus a `Match`. The web app then opens the orbit focused on it.

Pasted jobs have `Job.SubmittedByUserId` set. They are **private to that user**: the worker never matches them for anyone else, never deactivates them, and they never count as a cross-site duplicate of a scraped listing. Source is `linkedin.com` for LinkedIn links, `manual` otherwise.

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
| `GET /me`, `PUT /me/avatar` | ✓ | Account email and chosen avatar (0–15) |
| `GET /profile`, `PUT /profile` | ✓ | `PUT` body is the profile JSON and confirms it |
| `GET /matches?minScore=50&page=1&pageSize=25&sort=score&dir=desc` | ✓ | Active, not-dismissed matches, one page at a time: `{ items, total, newCount, page, pageSize }`. `sort` is one of score, title, company, city, salary, source or added; jobs without a salary always sort last |
| `PATCH /matches/{id}` | ✓ | `{ "status": "Interested" }` |
| `POST /jobs` | ✓ | `{ "url"?, "text" }`: adds a pasted job, returns its match |

Authenticated endpoints need the header `Authorization: Bearer <token>`.

## Web app

Blazor Web App with interactive server rendering and **prerendering turned off**, because the session token lives in browser storage, which isn't reachable during a prerender.

- `AuthSession` (one per circuit) keeps the JWT and stores it in `ProtectedLocalStorage`, which is encrypted with ASP.NET Data Protection, so a reload stays signed in. In Docker the keys live in the `webkeys` volume (`DataProtection:KeysPath=/keys`). If the keys are lost, users are simply signed out.
- `JobMagnetApiClient` wraps every API call and returns an `ApiResult<T>` instead of throwing. A 401 signs the user out, and `MainLayout` then redirects to `/login`.
- `MainLayout` loads the session before rendering any page and sends signed-out users to `/login`.

| Page | Does |
|---|---|
| `/login` | Sign in or register |
| `/cv` | Upload a PDF/DOCX; on success, goes to `/profile` |
| `/add-job` | Paste a job (e.g. from LinkedIn); it's scored and opened in the orbit |
| `/profile` | Edits the Claude-built profile; the first save confirms it and turns matching on |
| Account menu | Top right, behind your avatar: profile, CV, avatar picker, sign out |
| `/` | Orbit or table, switched at the top and kept in the URL (`?view=table&page=2&size=50&sort=salary&dir=asc&minScore=60`), so any view can be bookmarked or shared. The table pages and sorts on the server. The orbit: you in the center, matches as bubbles. A higher score puts the bubble closer and makes it bigger (`OrbitLayout`), and *Interested* pulls it further in. Clicking a bubble opens `JobDetails` and marks the match as seen. The page refreshes every minute; this stands in for push notifications |

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
| `DataProtection:KeysPath` | web | unset (local dev keeps keys in the user profile) |
| `Api:BaseUrl` | web | `http://localhost:8080/` (dev: `:5034`) |
| `Claude:Model`, `Claude:MaxTokens` | api, worker | `claude-opus-5-5`, 16000 |

You can override any key with an environment variable, using `__` as the separator (e.g. `Scrapers__MaxPagesPerSource=1`).

## Adding a job source

1. Write `Parsing/XyzParser.cs` with `Parse(string html) → IReadOnlyList<ScrapedJob>`, plus tests.
2. Write `Sources/XyzSource.cs : PagedJobSource` that overrides `Name`, `GetPageUrl` and `Parse`.
3. Register it with `AddSource<XyzSource>(services)` in `Scrapers/ServiceCollectionExtensions.cs`.
4. Check the site's robots.txt and terms of service first.

## Not built yet

Push notifications (the score threshold exists, but nothing is sent), Google/Apple login, OCR for scanned CVs, using dismissals to improve scoring, and city names for cv.lv.
