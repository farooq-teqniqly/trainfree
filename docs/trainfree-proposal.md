# Trainfree -- a personal workout app — v0.1 Project Proposal

**Purpose:** Replace a paid Trainwell subscription with a self-hosted, single-user workout tracker. For v0.1, I will be the only user.
**Stack:** Blazor WebAssembly (.NET 10) → Cloudflare Workers static assets + Cloudflare Access
**Estimated cost:** $0/month
**Date:** August 2026

---

## 1. Goals

### In scope

- Display a list of workouts organized by day of the week
- Per-exercise timer for timed exercises
- Total elapsed workout time, tracked across the whole session
- Pause / resume for both the exercise timer and the total timer
- Private access — only the owner can load the site. Access to domain will be restricted to my email via Cloudfare portal.
- Usable on a phone, mid-workout, with poor signal

### Explicitly out of scope (v1)

- Multiple users, accounts, or sharing
- Workout history, analytics, or progress charts
- Editing workouts in the UI (workout data is edited in source and redeployed)
- Video demonstrations or exercise images
- Any server-side component or database

### Secondary goal

This project doubles as a Blazor learning exercise. Where a choice exists between "fastest to ship" and "teaches more of the framework," the proposal leans toward the latter as long as the cost is small.

---

## 2. Why Blazor WebAssembly (not Blazor Server)

|                       | Blazor WASM        | Blazor Server                  |
| --------------------- | ------------------ | ------------------------------ |
| Output                | Static files       | Live .NET process              |
| Hosting cost          | $0 on static hosts | ~$5–15/mo minimum              |
| Works offline         | Yes (with PWA)     | No — needs constant connection |
| Gym wifi / dead zones | Fine               | Timer stalls on disconnect     |
| First load            | Several MB         | Fast                           |

WASM is the right call here. The one real drawback — a multi-megabyte initial download of the .NET runtime — is a one-time cost that's cached afterward, and it's irrelevant for a single user who installs the app once.

The offline point is worth emphasizing: a Blazor Server timer is driven by a SignalR round-trip. If the connection drops mid-set, the UI freezes. For a gym app, that alone disqualifies it.

---

## 3. Hosting & Access

### Cloudflare Workers with static assets

**Changed from v1, which specified Cloudflare Pages.** Workers has reached feature parity with Pages for static assets and custom domains, and Cloudflare now recommends Workers for new projects — new platform features ship there first, and the `wrangler pages` commands already prompt users to migrate. Pages is not being discontinued and existing projects are fine, but there's no reason to start a new project on the older path.

Practically, the two are nearly identical for this use case: upload a folder of files, get a URL. Static asset requests are free on both. The difference is which one still gets attention in two years.

Configuration lives in `wrangler.jsonc` at the project root:

```jsonc
{
  "name": "workout-app",
  "compatibility_date": "2026-08-01",
  "assets": {
    "directory": "./bin/Release/net10.0/publish/wwwroot",
    "not_found_handling": "single-page-application",
  },
}
```

No `main` field — omitting it means pure static hosting with no Worker script. That's correct for this app.

### Cloudflare Access (Zero Trust)

Free for up to 50 users.

**Changed from v1:** Access can now be enabled on a `workers.dev` URL with a single button — Worker → Settings → Domains & Routes → **Enable Cloudflare Access**. v1 implied you might need a custom domain for this. You don't, and no domain purchase is required.

The button provisions an Access application defaulting to your account email. Reviewing and adjusting the policy in the Zero Trust dashboard is optional. As of late 2025 these are _reusable_ policies rather than per-resource duplicates, so protecting multiple projects later means editing one policy.

Access authenticates at the edge before any file is served — `.wasm`, `.dll`, workout JSON, everything. Set the session duration to roughly a month; the default is short, and re-authenticating by email code in a gym with bad signal is precisely the friction that kills adoption of a personal tool.

### Deployment flow

Cloudflare's build environment has no .NET SDK, so the app compiles locally and the published output is uploaded:

```
dotnet publish -c Release
wrangler deploy
```

`wrangler deploy` replaces the older `wrangler pages deploy`. This can move to a GitHub Action later — the Action runs `dotnet publish` on a runner that does have the SDK, then calls Wrangler. Worth doing once the app stabilizes; manual deploys are fine while iterating.

### Required configuration files

**`wwwroot/_redirects`** — without this, refreshing on any route other than `/` returns a 404, because the server looks for a physical file:

```
/* /index.html 200
```

`_redirects` and `_headers` are supported natively by Workers static assets. This overlaps with `not_found_handling` above; keeping both is harmless and means the app still routes correctly if it ever moves to a different static host.

### Rejected alternatives

- **Cloudflare Pages** — works today, but it's the legacy path for new projects
- **Vercel / Netlify password protection** — clean, but gated behind a ~$20/mo tier
- **GitHub Pages** — free, but private repos don't get private sites; the site is public regardless
- **Azure Static Web Apps** — free tier exists and has built-in auth; a reasonable fallback, but Access gives finer-grained control and no Azure account overhead
- **Home-built login** — requires a backend, a user store, and password handling. Strictly worse than delegating to Cloudflare
- **Local-only (`dotnet run`)** — zero cost and zero setup, but no phone access, which defeats the purpose

---

## 4. Data Model

Workouts are transcribed from the source images into a static JSON file shipped with the app. No database, no API.

```csharp
public record Workout(
    DayOfWeek Day,
    string Name,
    List<Exercise> Exercises);

public record Exercise(
    string Name,
    ExerciseKind Kind,
    int? DurationSeconds,   // set when Kind == Timed
    int? Sets,              // set when Kind == Reps
    int? Reps,
    string? Notes);

public enum ExerciseKind { Timed, Reps }
```

Loaded once at startup via `HttpClient.GetFromJsonAsync<List<Workout>>("data/workouts.json")`.

**Open question:** the workout images haven't been reviewed yet. Transcribing them may surface structures this model doesn't capture — supersets, circuits with round counts, rest intervals between sets, or weight/load fields. The model should be treated as provisional until the images are in hand.

---

## 5. Application Structure

```
WorkoutApp/
├── Models/
│   ├── Workout.cs
│   ├── Exercise.cs
│   └── SessionState.cs
├── Services/
│   ├── IWorkoutService.cs        // loads workout data
│   ├── WorkoutService.cs
│   └── TimerService.cs           // owns all timing logic
├── Pages/
│   ├── Week.razor                // "/" — days of the week
│   ├── WorkoutDetail.razor       // "/workout/{day}" — exercise list
│   └── ActiveSession.razor       // "/session/{day}" — the running workout
├── Components/
│   ├── ExerciseTimer.razor
│   ├── TotalTimeDisplay.razor
│   └── ExerciseRow.razor
└── wwwroot/
    ├── data/workouts.json
    └── _redirects
```

Three screens: pick a day → review the exercises → run the session.

> **.NET 10 note:** `blazor.boot.json` no longer exists — boot configuration is embedded in `dotnet.js`. Older tutorials that tell you to configure MIME types or cache headers for that file are describing a version that's been superseded. .NET 10 also enables Hot Reload for WASM by default in Debug builds, which makes phases 3–4 considerably less tedious.

---

## 6. Timer Design

This is the part of the app with real design decisions in it, and the part most worth getting right.

### Two independent timers

1. **Total session timer** — starts when the session starts, runs until the workout ends
2. **Exercise timer** — counts down for the current timed exercise only

Pausing pauses both. Total time reflects actual working time, not wall-clock time since starting.

### Implementation: don't accumulate ticks

The naive approach increments a counter every tick. This drifts, because timer callbacks are never exactly on schedule, and it breaks entirely if the browser throttles background tabs — which mobile browsers do aggressively when the screen locks.

Instead, store timestamps and compute elapsed time on demand:

```csharp
private DateTime _startedAt;
private TimeSpan _accumulatedBeforePause;
private bool _isRunning;

public TimeSpan Elapsed => _isRunning
    ? _accumulatedBeforePause + (DateTime.UtcNow - _startedAt)
    : _accumulatedBeforePause;

public void Pause()
{
    if (!_isRunning) return;
    _accumulatedBeforePause += DateTime.UtcNow - _startedAt;
    _isRunning = false;
}

public void Resume()
{
    if (_isRunning) return;
    _startedAt = DateTime.UtcNow;
    _isRunning = true;
}
```

The `PeriodicTimer` then exists only to trigger re-renders, not to measure anything. If a tick is late or skipped, the displayed time is still correct.

### Re-rendering from a background thread

Timer callbacks don't run on Blazor's synchronization context, so calling `StateHasChanged()` directly will throw or silently fail. It must be marshalled:

```csharp
await InvokeAsync(StateHasChanged);
```

This is one of the more common Blazor stumbling blocks and a genuinely useful thing to hit while learning.

### Cleanup

Components owning a timer implement `IAsyncDisposable` and dispose the `PeriodicTimer` and cancel its token. Skipping this leaks a running loop every time you navigate away.

### Completion signal

When a timed exercise hits zero: an audible tone via the Web Audio API (a short JS interop call) plus a visual state change. Vibration via `navigator.vibrate` is worth adding but has inconsistent iOS support — treat it as a nice-to-have.

---

## 7. Progressive Web App

Created with `dotnet new blazorwasm --pwa`. This gets:

- Installable to the phone home screen, launching without browser chrome
- Service worker caching the runtime and assets, so the app loads offline
- No app store, no signing, no review

The Access cookie persists in the installed app the same as in the browser.

**Caveat:** the service worker aggressively caches. During development this causes confusing stale-content behavior. Standard practice is to develop against `service-worker.js` in its non-published form and hard-refresh often.

---

## 8. Screen Wake Lock

Phone screens sleep during a workout, which is actively annoying when a timer is running. The Screen Wake Lock API prevents this:

```javascript
navigator.wakeLock.request("screen");
```

Called via JS interop when a session starts, released when it ends. Supported in Chrome and Safari 16.4+. Small addition, disproportionate quality-of-life improvement.

---

## 9. Build Sequence

| Phase | Deliverable                                       | Learning focus                                       |
| ----- | ------------------------------------------------- | ---------------------------------------------------- |
| 1     | Project scaffold, workout JSON, day list renders  | Blazor project structure, DI, `HttpClient`           |
| 2     | Workout detail screen, routing between days       | Routing, route parameters, `@page`                   |
| 3     | Total session timer with pause/resume             | `PeriodicTimer`, `InvokeAsync`, `IAsyncDisposable`   |
| 4     | Per-exercise countdown, advance between exercises | Component state, `EventCallback`, parent/child comms |
| 5     | Audio cue, wake lock                              | JS interop                                           |
| 6     | Deploy to Workers, configure Access               | Publishing, static hosting, edge auth                |
| 7     | PWA install, offline verification                 | Service workers, caching                             |

Phases 1–2 produce something usable immediately. Each phase after that is independently shippable.

---

## 10. Risks & Open Items

| Item                                | Notes                                                                                                                                                     |
| ----------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Workout images not yet reviewed** | The data model is a guess until the images are transcribed. Structures like supersets or circuits may require model changes. This is the largest unknown. |
| **First-load size**                 | Several MB of runtime. Mitigated by PWA caching and trimming (`<PublishTrimmed>true</PublishTrimmed>`), but the first visit will be slow.                 |
| iOS PWA quirks                      | Background timer behavior and vibration are inconsistent on iOS. The timestamp-based timer design is what makes this survivable.                          |
| Access session expiry               | Set a long session duration to avoid re-authenticating at the gym.                                                                                        |
| Service worker staleness            | Expect confusing caching behavior during development.                                                                                                     |

---

## 11. Next Steps

1. **Provide the workout images** — everything downstream depends on the transcription
2. Confirm .NET 10 SDK is installed and whether a Cloudflare account already exists
3. Build phase 1 and confirm the data model survives contact with the real workouts
4. Deploy early — get the Workers + Access setup working while the app is still trivial, so hosting problems and app problems don't arrive at the same time

---

## Cost Summary

| Item                                                              | Cost      |
| ----------------------------------------------------------------- | --------- |
| Cloudflare Workers (static assets)                                | $0        |
| Cloudflare Access (1 user)                                        | $0        |
| Domain (optional — `*.workers.dev` works, and Access protects it) | ~$10/yr   |
| **Total**                                                         | **$0/mo** |
