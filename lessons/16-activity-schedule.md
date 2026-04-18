# Lesson 16: Activity Schedule — Managing CarePlan Progress

## Overview

In Lesson 15 we created a CarePlan from a PlanDefinition — three activities, all with
status `not-started`. But a care plan is a **living document**. As the patient completes
activities, the statuses need to update. When everything is done, the plan itself moves
to `completed`.

This lesson is about **managing the CarePlan lifecycle** — displaying the schedule,
updating activity statuses, and tracking overall completion.

---

## The Activity Schedule

A CarePlan's activities form a schedule — a list of things the patient needs to do,
each with a time window and a status.

```
=== Activity Schedule for Patient John Smith ===
Plan period: 2026-04-18 to 2026-05-18
Status: active

  1. Complete PROM Questionnaire
     Status: not-started
     Due: Apr 18 – May 18
     Definition: Questionnaire/prom-test

  2. Order HbA1c Lab Test
     Status: not-started
     Due: Apr 18 – Apr 25
     Definition: ActivityDefinition/order-hba1c

  3. Schedule Follow-Up Appointment
     Status: not-started
     Due: May 11 – May 18
     Definition: ActivityDefinition/schedule-followup
```

This is what a patient portal or clinician dashboard would display. Each activity has
enough context for action — what to do, when, and what resource defines it.

---

## Updating Activity Status

As things happen in the real world, your application updates the CarePlan:

| Event | Activity Status Change |
|-------|----------------------|
| Lab order placed | `not-started` → `in-progress` |
| Lab results received | `in-progress` → `completed` |
| Patient opens questionnaire | `not-started` → `in-progress` |
| Patient submits answers | `in-progress` → `completed` |
| Appointment booked | `not-started` → `scheduled` |
| Appointment attended | `scheduled` → `completed` |
| Patient declines activity | any → `cancelled` |

### How Updates Work in FHIR

FHIR uses `PUT` for updates — you send the **entire resource** with the modified
fields. There's no "patch just the status" shortcut in standard FHIR R4.

The workflow:
1. `GET /fhir/CarePlan/{id}` — read the current CarePlan
2. Modify the activity status in your code
3. `PUT /fhir/CarePlan/{id}` — send the whole resource back

```
GET CarePlan/123
  → activity[0].detail.status = "not-started"

(patient submits questionnaire response)

PUT CarePlan/123
  → activity[0].detail.status = "completed"    ← changed
  → activity[1].detail.status = "not-started"  ← unchanged
  → activity[2].detail.status = "not-started"  ← unchanged
```

### Version Conflicts

Because `PUT` replaces the whole resource, concurrent updates can collide. FHIR handles
this with **optimistic locking** — the `meta.versionId` increments on each update. If
two systems try to update the same version, the second one gets a `409 Conflict`.

In our learning project this isn't an issue, but in production you'd handle version
conflicts by re-reading and retrying.

---

## Completing the CarePlan

The CarePlan itself has a status separate from its activities:

| CarePlan Status | Meaning |
|----------------|---------|
| `active` | Plan is in progress, activities pending |
| `completed` | All activities done, plan fulfilled |
| `revoked` | Clinician cancelled the plan |
| `entered-in-error` | Plan was created by mistake |

Your application decides when to mark the plan as `completed`. The typical pattern:

```
if (all activities are completed)
    carePlan.status = "completed"
```

FHIR doesn't enforce this automatically — it's application logic.

---

## A Typical Workflow

Here's what a real workflow looks like over time:

```
Day 1:  CarePlan created (active)
        ├── Survey: not-started
        ├── Lab: not-started
        └── Follow-up: not-started

Day 2:  Lab order placed
        ├── Survey: not-started
        ├── Lab: in-progress        ← updated
        └── Follow-up: not-started

Day 5:  Patient completes survey
        ├── Survey: completed        ← updated
        ├── Lab: in-progress
        └── Follow-up: not-started

Day 6:  Lab results back
        ├── Survey: completed
        ├── Lab: completed           ← updated
        └── Follow-up: not-started

Day 20: Follow-up booked
        ├── Survey: completed
        ├── Lab: completed
        └── Follow-up: scheduled     ← updated

Day 25: Follow-up attended
        ├── Survey: completed
        ├── Lab: completed
        └── Follow-up: completed     ← updated

        → All activities completed → CarePlan status: completed
```

---

## What Our Code Does

We extended `CarePlanService` with:

1. **UpdateActivityStatusAsync** — changes a specific activity's status by index
2. **CompleteCarePlanAsync** — marks the overall plan as completed
3. **AreAllActivitiesCompleted** — checks if every activity is done

The tests simulate the full workflow: create a plan, update activities one by one, check
intermediate states, and complete the plan when all activities are done.

---

## Common Questions

**Q: Should I update CarePlan status automatically when all activities complete?**
That's your application's choice. Some systems auto-complete, others require clinician
sign-off. The pattern is: check `AreAllActivitiesCompleted()`, then decide whether to
auto-update or notify the clinician.

**Q: What if an activity needs to be repeated?**
Add a new activity to the CarePlan (FHIR allows this). The original activity stays
`completed`. Or create a new CarePlan for the next cycle.

**Q: Can I track who updated each activity?**
Not directly in the activity detail. You'd use **Provenance** resources linked to the
CarePlan to track who made each change and when. The CarePlan's `meta.lastUpdated` and
`meta.versionId` track when it was modified, but not which field changed.

**Q: What about overdue activities?**
FHIR doesn't have a built-in "overdue" status. Your application compares the
`scheduledPeriod.end` to the current date. If it's past and the status isn't `completed`,
the activity is overdue. Display logic, not FHIR logic.

---

## Summary

- CarePlan activities have **independent statuses** that track progress over time
- Updates use `PUT` (full resource replacement) — read, modify, write back
- Your **application code** drives status transitions based on real-world events
- `AreAllActivitiesCompleted()` checks if the plan is fulfilled
- The CarePlan status moves to `completed` when your app decides all work is done
- Version conflicts are handled via `meta.versionId` (optimistic locking)

## Next Step

Proceed to **Lesson 17** — Bundle transactions for creating multiple resources
atomically in a single request.
