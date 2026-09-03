# Trainfree Screen Mockups -- v0.1

---

## 1. Program Select (entry point)

```
┌─────────────────────────────┐
│  Trainfree          [Hist]  │
├─────────────────────────────┤
│                             │
│  Select a Program           │
│                             │
│  ┌─────────────────────┐   │
│  │  Workout A        › │   │
│  └─────────────────────┘   │
│  ┌─────────────────────┐   │
│  │  Workout B        › │   │
│  └─────────────────────┘   │
│  ┌─────────────────────┐   │
│  │  Workout C        › │   │
│  └─────────────────────┘   │
│  ┌─────────────────────┐   │
│  │  Workout D        › │   │
│  └─────────────────────┘   │
│                             │
└─────────────────────────────┘
```

- Program names are admin-defined.
- [Hist] navigates to Workout History (UC6.0).

---

## 2. Session Select

```
┌─────────────────────────────┐
│  ‹  Workout A               │
├─────────────────────────────┤
│                             │
│  Select a Session           │
│                             │
│  ┌─────────────────────┐   │
│  │  Monday Lower Body › │   │
│  └─────────────────────┘   │
│  ┌─────────────────────┐   │
│  │  Tuesday Upper Body › │  │
│  └─────────────────────┘   │
│  ┌─────────────────────┐   │
│  │  Thursday Lower Body › │ │
│  └─────────────────────┘   │
│  ┌─────────────────────┐   │
│  │  Friday Upper Body  › │  │
│  └─────────────────────┘   │
│                             │
└─────────────────────────────┘
```

- Session names are admin-defined.
- Any session can be run on any calendar day; backend records date/time of execution.

---

## 3. Exercise -- Ready to Start

```
┌─────────────────────────────┐
│  ‹  Monday Lower Body  0:00 │
├─────────────────────────────┤
│  Warm Up          Set 1 / 1 │
├─────────────────────────────┤
│                             │
│  ┌─────────────────────┐   │
│  │                     │   │
│  │    [exercise img]   │   │
│  │                     │   │
│  └─────────────────────┘   │
│                             │
│  Skater Jump                │
│  30 sec                     │
│                             │
│       ┌───────────────┐     │
│       │   START SET   │     │
│       └───────────────┘     │
│                             │
│  Next: Bodyweight Squat     │
└─────────────────────────────┘
```

- Top-right: global workout timer (counts up, subtle).
- Phase name and set counter below header.
- Exercise image uploaded via admin UI, stored in Cloudflare R2.
- "Next" preview shows upcoming exercise.

---

## 4. Exercise -- Timed Set in Progress

```
┌─────────────────────────────┐
│  ‹  Monday Lower Body  0:47 │
├─────────────────────────────┤
│  Warm Up          Set 1 / 1 │
├─────────────────────────────┤
│                             │
│  ┌─────────────────────┐   │
│  │                     │   │
│  │    [exercise img]   │   │
│  │                     │   │
│  └─────────────────────┘   │
│                             │
│  Skater Jump                │
│                             │
│         ╔═══════╗           │
│         ║  0:24 ║           │
│         ╚═══════╝           │
│                             │
│  Next: Bodyweight Squat     │
└─────────────────────────────┘
```

- Timer counts down; auto-completes at 0:00, no button needed.
- Log screen appears automatically when timer hits zero.

---

## 5. Log Set -- Timed Exercise

```
┌─────────────────────────────┐
│  ‹  Monday Lower Body  0:47 │
├─────────────────────────────┤
│  Warm Up          Set 1 / 1 │
├─────────────────────────────┤
│                             │
│  Skater Jump                │
│  Set 1 complete             │
│                             │
│  Duration                   │
│  ┌─────────────────────┐   │
│  │  30 sec          ── │   │
│  └─────────────────────┘   │
│                             │
│  Weight (optional)          │
│  ┌─────────────────────┐   │
│  │  -- lbs             │   │
│  └─────────────────────┘   │
│                             │
│       ┌───────────────┐     │
│       │     DONE      │     │
│       └───────────────┘     │
└─────────────────────────────┘
```

---

## 6. Log Set -- Untimed Exercise

```
┌─────────────────────────────┐
│  ‹  Monday Lower Body  1:15 │
├─────────────────────────────┤
│  A                Set 1 / 3 │
├─────────────────────────────┤
│                             │
│  Dumbbell Goblet Squat      │
│  Set 1 complete             │
│                             │
│  Reps                       │
│  ┌─────────────────────┐   │
│  │  12                 │   │
│  └─────────────────────┘   │
│                             │
│  Weight                     │
│  ┌─────────────────────┐   │
│  │  50 lbs             │   │
│  └─────────────────────┘   │
│                             │
│       ┌───────────────┐     │
│       │     DONE      │     │
│       └───────────────┘     │
└─────────────────────────────┘
```

- Pre-filled with prescribed reps/weight from admin; editable before confirming.
- DONE starts the rest timer.

---

## 7. Rest Timer

```
┌─────────────────────────────┐
│  ‹  Monday Lower Body  1:18 │
├─────────────────────────────┤
│  A                Set 1 / 3 │
├─────────────────────────────┤
│                             │
│  Rest                       │
│                             │
│         ╔═══════╗           │
│         ║  0:45 ║           │
│         ╚═══════╝           │
│                             │
│  Up next                    │
│  Dumbbell Goblet Squat      │
│  Set 2 -- 12 reps, 50 lbs  │
│                             │
│                             │
│       ┌───────────────┐     │
│       │   SKIP REST   │     │
│       └───────────────┘     │
│                             │
└─────────────────────────────┘
```

- Rest duration is per-exercise, set in admin UI.
- Timer hits zero: auto-advance to next set, exercise screen shown with START SET.
- SKIP REST: immediately advances.

---

## 8. Workout Complete

```
┌─────────────────────────────┐
│  Monday Lower Body          │
├─────────────────────────────┤
│                             │
│         Workout             │
│         Complete            │
│                             │
│         Total time          │
│         ╔═══════╗           │
│         ║ 47:32 ║           │
│         ╚═══════╝           │
│                             │
│      Exercises    Sets      │
│           8        24       │
│                             │
│                             │
│       ┌───────────────┐     │
│       │  END WORKOUT  │     │
│       └───────────────┘     │
│                             │
└─────────────────────────────┘
```

- END WORKOUT saves history to D1 and returns to Program Select.

---

## 9. History -- List

```
┌─────────────────────────────┐
│  ‹  History                 │
├─────────────────────────────┤
│                             │
│  ┌─────────────────────┐   │
│  │  Mon Lower Body     │   │
│  │  Workout A          │   │
│  │  Aug 3, 2026  47:32 │   │
│  └─────────────────────┘   │
│  ┌─────────────────────┐   │
│  │  Fri Upper Body     │   │
│  │  Workout A          │   │
│  │  Aug 1, 2026  52:14 │   │
│  └─────────────────────┘   │
│  ┌─────────────────────┐   │
│  │  Thu Lower Body     │   │
│  │  Workout A          │   │
│  │  Jul 30, 2026 44:07 │   │
│  └─────────────────────┘   │
│                             │
└─────────────────────────────┘
```

- Reverse-chronological order.
- Tap a row to see per-exercise detail.

---

## 10. History -- Detail

```
┌─────────────────────────────┐
│  ‹  Aug 3, 2026             │
├─────────────────────────────┤
│  Workout A -- Mon Lower     │
│  Duration: 47:32            │
├─────────────────────────────┤
│  Dumbbell Goblet Squat      │
│  Set 1: 12 reps, 50 lbs    │
│  Set 2: 12 reps, 50 lbs    │
│  Set 3: 10 reps, 50 lbs    │
│                             │
│  Dumbbell Sumo Deadlift     │
│  Set 1: 12 reps, 50 lbs    │
│  Set 2: 12 reps, 50 lbs    │
│  Set 3: 12 reps, 50 lbs    │
│                             │
│  ...                        │
└─────────────────────────────┘
```

- Actual reps/weight as logged during the workout (may differ from prescribed).

---

## 11. Admin -- Single-Page Spreadsheet

```
┌──────────────────────────────────────────────────────────────────────────────────┐
│  Trainfree Admin                                                      [+ Program] │
├──────────────────┬───────┬──────┬────────┬──────┬─────────┬───────┬──────┬──────┤
│  Name            │ Type  │ Reps │ Weight │ Sets │ Rest(s) │ Side  │ Note │Image │
├──────────────────┼───────┼──────┼────────┼──────┼─────────┼───────┼──────┼──────┤
│ ▼ Workout A  [x] │       │      │        │      │         │       │      │      │
│  ▼ Mon Lower [x] │       │      │        │      │         │       │      │      │
│   ▼ Warm Up  [x] │       │      │        │      │         │       │      │      │
│    Skater Jmp[x] │ Timed │  --  │   --   │  1   │   30    │ Both  │      │[Brws]│
│    BW Squat  [x] │ Reps  │  20  │   --   │  1   │   30    │ Both  │      │[Brws]│
│              [+ Exercise]│      │        │      │         │       │      │      │
│   ▼ A        [x] │       │      │        │      │         │       │      │      │
│    Goblet Sq [x] │ Reps  │  12  │   50   │  3   │   60    │ Both  │      │[Brws]│
│    Sumo DL   [x] │ Reps  │  12  │   50   │  3   │   60    │ Both  │      │[Brws]│
│              [+ Exercise]│      │        │      │         │       │      │      │
│              [+ Phase]   │      │        │      │         │       │      │      │
│  ▼ Tue Upper [x] │       │      │        │      │         │       │      │      │
│   ...            │       │      │        │      │         │       │      │      │
│              [+ Session] │      │        │      │         │       │      │      │
│ ▼ Workout B  [x] │       │      │        │      │         │       │      │      │
│   ...            │       │      │        │      │         │       │      │      │
└──────────────────┴───────┴──────┴────────┴──────┴─────────┴───────┴──────┴──────┘
```

- Click any cell to edit inline; changes save on blur/Enter.
- [x] deletes the row (and its children).
- [Brws] opens a file picker; filename shown in cell after upload; stored in R2.
- Side column: dropdown -- Both (default) / Left / Right.
- Rows collapse/expand via the ▼ toggle.
- Prescribed reps/weight are the template; actuals are logged per-set at workout time.
- Sets = number of prescribed sets (all share the same reps/weight prescription).
