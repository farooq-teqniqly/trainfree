# Trainfree -- a personal workout app — v0.1 Project Proposal

**Purpose:** Replace a paid Trainwell subscription with a self-hosted, single-user workout tracker. For v0.1, I will be the only user.
**Stack:** Blazor WebAssembly (.NET 10) → Cloudflare Workers static assets + Cloudflare Access
**Estimated cost:** $0/month
**Date:** August 2026

## Use cases

### Workout use cases

UC1.0 - Select workout
When app loads, it displays the list of workouts. Currently there are four (see `.\workouts` folder).

I select the workout. The first exercise in the workout is shown. Global workout timer starts.

UC2.0 - Start workout
Prerequisites: UC1.0
I click the Start Set button. Start Set button becomes End Set button.

UC3.0 - Exercise started
Prerequisites: UC2.0
The current set / total sets counter is displayed. For timed exercise, the exercise timer counts down.

UC3.1 - Set completed

For untimed exercises:
When I complete a set, I click the End Set button. The rest set starts. 

For timed exercises:
When exercise timer reaches zero, the rest set starts

UC3.2 - Rest set
The rest timer counts down. When it hits zero, the next set starts. Set counter incremented by one.

UC4.0 - Workout ends
All exercises are completed. The End Set button becomes End Workout. I press End Workout and the workout is complete.
The Select Workout screen loads.

### Administration use cases

UC5.0 - Administer Workouts
I am able to create, edit, delete workouts.

UC6.0 - Workout History
I am able to see my workout history.

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

## 3. Hosting, Access, and Deployment

### Cloudflare Workers with static assets

Follow patterns in [](https://github.com/farooq-teqniqly/blazor-cloudfare-throwaway)
---

## 4. Object Model
[Object Model Diagram (Draft)](https://lucid.app/lucidchart/bc369bf0-b3b5-4861-807f-efb3698c0827/edit?view_items=S-FhqfmfzDD-&page=0_0&invitationId=inv_05565094-678a-431a-85cc-2a9b0d7f74d3)

---

## Key Decisions

- **Persistence: Cloudflare D1 (SQLite).** Data lives in a Cloudflare Worker API backed
  by D1, not in the browser. Enables real sync and durable history across phone (workout)
  and desktop (admin). Stays ~$0 on the free tier. Trade-off: breaks "static assets only" --
  adds an API layer to build and secure.
  - **API runtime: vanilla JavaScript Cloudflare Worker with native D1 binding.** No
    TypeScript, no .NET on the server side. The Worker is a thin JSON API (routes +
    D1 queries). The .NET-everywhere convention applies to the Blazor client only.

- **Rest timer: per-exercise.** Each exercise carries its own `restSeconds`. Timed
  exercises carry a `durationSeconds` (work interval, e.g. Skater Jump 30s). Untimed
  exercises carry `reps`. Draft exercise shape:
  `Exercise { name, reps? | durationSeconds?, weightLbs?, side?, note?, restSeconds }`.

- **Workout selection: 2-level navigation.** Select screen shows 4 programs (A/B/C/D).
  Tapping a program shows its day-sessions (e.g. Monday Lower Body, Tuesday Upper Body).
  Any session can be run on any calendar day -- the backend records date/time of execution,
  not the labeled day.
- **Data model clarification:** "four workouts" in the spec means four *programs*, each
  containing four day-sessions = 16 distinct sessions total.

- **Seeding: manual via CRUD UI.** No migration script. The 16 sessions will be entered
  by hand using UC5.0, which doubles as a smoke-test of the admin UI. UC5.0 must therefore
  support creating programs, day-sessions, and exercises from scratch, not just editing
  existing data.

- **History: completion + per-exercise log.** Each completed workout writes: program,
  day-session, startedAt, endedAt, and for each exercise: sets with actual reps and
  weight used.
- **Set logging flow (untimed exercises).** After End Set: show a log screen pre-filled
  with prescribed reps/weight (editable). Confirming the log starts the rest timer.
  Updated UC flow: Start Set -> [exercise] -> End Set -> Log Set screen -> rest timer ->
  next set.
- **Timed exercises** auto-complete when timer hits zero; log screen appears at that point
  (duration is known, only weight is editable if applicable) before rest timer starts.

- **Exercise advance: auto.** When the rest timer for the last set of an exercise hits
  zero, the next exercise loads automatically and shows the Start Set button. No tap
  required between exercises.
- **"Categories" is the correct term** for what the workout images call sections (Warm Up,
  A, B). Already modeled in the object model diagram.

- **Exercise images: Cloudflare R2.** Uploaded per-exercise in the admin UI, stored in R2
  (free tier). URL stored in D1 alongside the exercise record. Blazor WASM loads the image
  directly from R2 during a workout.
- **Side field: dropdown.** Both (default) / Left / Right. Set per-exercise in admin UI.

## Open Questions

## Cost Summary

| Item                                                                                                 | Cost      |
| ------------------------------------------------------------------------------------------------------ | --------- |
| Cloudflare Workers (static assets)                                                                     | $0        |
| Cloudflare D1                                                                                           | $0        |
| Cloudflare R2 ([free tier](https://developers.cloudflare.com/r2/pricing/): 10 GB storage/mo, well within a single-user exercise-image library) | $0        |
| Cloudflare Access (1 user)                                                                              | $0        |
| Domain (optional — `*.workers.dev` works, and Access protects it)                                       | ~$10/yr   |
| **Total**                                                                                                | **$0/mo** |
