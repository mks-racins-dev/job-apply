# Job Magnet: Implementation Plan

> Fill in your profile once, and jobs from the Latvian market come to you.
> The idea: you don't apply to everything. You're at the center, and relevant jobs "pop up" to get your attention.

## 1. Confirmed decisions

| Area | Decision |
|---|---|
| Mobile app | React Native + Expo SDK 57 (TypeScript) |
| Backend | C# / ASP.NET Core (.NET), Docker, any host |
| AI | Claude (Anthropic API) |
| Accounts | Yes, free (no monetization in v1) |
| Market | Latvia: visidarbi.lv, cv.lv, ss.lv, plus more TBD |

## 2. Architecture

```
┌────────────────────┐      HTTPS/JSON       ┌──────────────────────────────┐
│  Expo app (iOS/And)│ ───────────────────▶  │  ASP.NET Core Web API         │
│  - Profile form    │ ◀───────────────────  │  - Auth, profile, matches     │
│  - Profile form    │      Push (Expo)      │                               │
│  - "Job orbit" UI  │ ◀──────────────────── │  Worker service (scrapers)    │
└────────────────────┘                       │  - per-site scraper jobs      │
                                             │  - normalize + dedupe         │
                                             │  - Claude matching pipeline   │
                                             └──────────┬───────────────────┘
                                                        │
                                          ┌─────────────┴─────────────┐
                                          │ PostgreSQL (+ pgvector?)  │
                                          │ Profile + match data only │
                                          └───────────────────────────┘
```

### Backend projects (one .NET solution)
- `JobMagnet.Api`: ASP.NET Core minimal API or controllers
- `JobMagnet.Worker`: hosted background service that runs scrapers on a schedule (Hangfire or Quartz.NET)
- `JobMagnet.Scrapers`: one class per source behind an `IJobSource` interface
- `JobMagnet.Matching`: Claude integration (job scoring against the user's profile)
- `JobMagnet.Data`: EF Core + PostgreSQL

## 3. Core flows

### 3.1 Profile form (no CV upload)
1. The user fills in a short profile form in the app. No CV is uploaded, and no CV text is stored.
2. The form collects only what matching needs: desired roles, skills (chosen from a list plus optional tags), seniority, languages (LV/EN/RU), city or region, salary floor, and remote preference.
3. The form avoids free-text fields where possible. It collects no name, photo, address, phone number, date of birth, or employer history. The account needs only the login email.
4. The user can edit the profile at any time. Each save creates a new profile version, and the user can delete the profile and all matches (see GDPR in Phase 3).
5. Matching starts after the user saves the profile once.

### 3.2 Job ingestion
1. The worker runs each `IJobSource` on a schedule (for example, every 1–3 hours).
2. Scraped listings are normalized to a common `Job` model: title, company, city, salary range, language, description, URL, source, posted date.
3. Listings are deduplicated across sources (the same job is often posted on several sites).
4. Expired jobs are marked inactive.

### 3.3 Matching (cost-aware)
1. **Cheap prefilter.** Run keyword/location/language filters (or embeddings) for each user and keep the top N candidates. We shouldn't send every job to the LLM.
2. **Claude scoring.** Send the profile and candidate jobs to Claude. It returns a 0–100 score, a short reason ("why this job wants you"), and which skills matched or are missing.
3. Store `Match` rows. A new match above the threshold triggers a push notification through Expo Push.

### 3.4 The "jobs come to you" UX
- The home screen shows a neutral center graphic (not a photo or CV) with job "bubbles" orbiting or pulling in, sized and positioned by match score.
- New matches animate in with a light haptic (`react-native-reanimated`, `expo-haptics`).
- Tap a bubble to see the job card: why it matched, salary, and source link.
- Actions: **Interested** (pull closer/save), **Dismiss** (fade out; also feeds back into scoring), **Open original**.
- The app does **not** auto-apply. "Apply" opens the original listing.

## 4. Data model (initial)
- `User` (id, email, auth provider, push token, created)
- `Profile` (user, structured form fields, version, updated). No file or extracted text is stored.
- `Job` (source, external id, url, title, company, city, salary, language, description, posted, active, hash for dedupe)
- `Match` (user, job, score, reason, status: new/seen/interested/dismissed)

## 5. Phases

**Phase 0: Validation (before writing app code)**
- Check each source's robots.txt and terms of service, and whether it has an API or RSS feed (see open questions).
- Build a spike that scrapes ~50 jobs from each source in a console app.
- Run a Claude prompt test on 3–5 sample profiles (synthetic or consented, never real personal data) to tune the scoring prompt and measure the cost per user.

**Phase 1: Backend MVP**
- Solution skeleton, Postgres, EF Core migrations, Docker Compose
- Scrapers for visidarbi.lv, cv.lv, ss.lv
- Profile form API and the profile endpoint (validation, versioning)
- Matching pipeline and matches endpoint

**Phase 2: App MVP**
- Expo app: auth, profile form, the orbit home screen, job details
- Push notifications

**Phase 3: Store release**
- EAS Build and Submit, App Store Connect and Google Play listings
- Privacy policy, GDPR (profile = personal data): explicit consent, data minimisation, data export and deletion, EU hosting
- Apple Sign-In (required by Apple if we offer other social logins)

**Phase 4: Improvements**
- More sources, feedback-driven ranking, filters (remote, salary floor, language)

## 6. Open questions (need your answers)

1. **Other sources:** besides visidarbi.lv, cv.lv, and ss.lv, which sites should we include? Candidates I know of that need your confirmation: likeit.lv, prakse.lv, LinkedIn (scraping it isn't realistic or allowed). Do you have a list?
2. **Legal and scraping:** are you OK with scraping HTML where there's no API? cv.lv in particular may prohibit it in its ToS. Should we contact the sites for partnerships or feeds?
3. **Login methods:** email, Google, Apple? (If Google is offered, Apple is required on iOS.)
4. **Expo Go vs dev build:** Expo Go is fine for development, but store builds and some modules (push, haptics are fine; any custom native code isn't) need EAS dev/production builds. Is that OK?
5. **Languages:** app UI in LV, EN, and/or RU? Job descriptions are mixed, and Claude handles all three.
6. **Hosting:** which provider or region (for example, Hetzner in an EU region for GDPR)?
7. **Profile fields:** which fields are required and which are optional? Is a salary floor needed at signup, or later?
8. **Notifications:** how often and how aggressive? Instant for each match, or a daily digest?
9. **Budget:** is there a monthly limit for Claude API spend? It determines how aggressive the prefilter needs to be.
10. **App name and branding:** is "Job Magnet" just a placeholder?
