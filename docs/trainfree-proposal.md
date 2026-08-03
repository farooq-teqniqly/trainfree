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

## Open Questions

Persistence? Leaning Sqllite. Is this possible with Cloudfare?

## Cost Summary

| Item                                                              | Cost      |
| ----------------------------------------------------------------- | --------- |
| Cloudflare Workers (static assets)                                | $0        |
| Cloudflare Access (1 user)                                        | $0        |
| Domain (optional — `*.workers.dev` works, and Access protects it) | ~$10/yr   |
| **Total**                                                         | **$0/mo** |
