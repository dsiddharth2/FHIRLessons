# Lesson 15: CarePlan — Applying a Protocol to a Patient

## Overview

In Lesson 14 we built a PlanDefinition — a reusable protocol template with three
actions (survey, lab order, follow-up). But a template sitting on the server doesn't
help any specific patient. This lesson is about **CarePlan** — the patient-specific
plan that says "John Smith needs to do these things by these dates."

---

## What Is a CarePlan?

A CarePlan is a **patient-specific plan of care**. It takes the abstract protocol from
a PlanDefinition and makes it concrete:

| PlanDefinition (template) | CarePlan (patient-specific) |
|---------------------------|----------------------------|
| "Patient should complete PROM survey" | "John Smith must complete PROM survey by May 18" |
| "Order HbA1c lab test" | "HbA1c ordered for John Smith, due by April 25" |
| "Schedule follow-up" | "Follow-up appointment for John Smith, May 11-18" |

### The $apply Operation (Concept)

FHIR defines a `$apply` operation on PlanDefinition:

```
POST /fhir/PlanDefinition/{id}/$apply?subject=Patient/{patientId}
```

This is supposed to read the PlanDefinition, fill in patient-specific details, and
return a CarePlan. However, **HAPI FHIR doesn't support `$apply` by default** — it
requires the Clinical Reasoning module, which is a separate add-on.

This is normal. Most production systems don't rely on `$apply` either — they build
CarePlans programmatically in application code, which gives more control over the
logic.

### What We'll Do Instead

We'll build a `CarePlanService` that:
1. Reads a PlanDefinition from the server
2. Constructs a CarePlan with activities matching each action
3. Sets patient-specific timing and references
4. Creates the CarePlan on the server

This is the **practical approach** used in real systems.

---

## CarePlan Structure

```
CarePlan
  ├── status (draft / active / completed / revoked / entered-in-error)
  ├── intent (proposal / plan / order / option)
  ├── subject → Patient reference
  ├── period (start/end of the plan)
  ├── instantiatesCanonical → PlanDefinition URL (where this plan came from)
  ├── category (what kind of care plan)
  ├── goal[] → Goal references
  └── activity[]
        ├── detail.status (not-started / scheduled / in-progress / completed / cancelled)
        ├── detail.kind (ServiceRequest / Task / etc.)
        ├── detail.description
        ├── detail.scheduledPeriod (when this activity should happen)
        └── detail.instantiatesCanonical → ActivityDefinition/Questionnaire URL
```

### Key CarePlan Fields

| Field | Required? | Description |
|-------|-----------|-------------|
| `status` | YES | draft / active / completed / revoked / entered-in-error |
| `intent` | YES | proposal / plan / order / option |
| `subject` | YES | The patient this plan is for |
| `period` | Recommended | When the plan is active (start/end dates) |
| `instantiatesCanonical` | Recommended | URL of the PlanDefinition this was created from |
| `category` | Recommended | Type of plan (e.g., assess-plan) |
| `activity` | YES | The specific things the patient needs to do |

### Activity Detail Fields

| Field | Description |
|-------|-------------|
| `status` | not-started / scheduled / in-progress / completed / cancelled |
| `kind` | What type of resource this activity represents |
| `description` | Human-readable description of the activity |
| `scheduledPeriod` | When this activity should happen |
| `instantiatesCanonical` | URL of the definition this activity came from |

---

## CarePlan Lifecycle

```
draft  ──>  active  ──>  completed
              │
              ├──>  revoked (cancelled by clinician)
              └──>  entered-in-error (was a mistake)
```

Activity status tracks independently:

```
not-started  ──>  scheduled  ──>  in-progress  ──>  completed
                                       │
                                       └──>  cancelled
```

---

## Our CarePlan

We'll create a CarePlan for a patient based on the PROM Assessment Protocol:

| Activity | Source | Kind | Scheduled Period | Status |
|----------|--------|------|-----------------|--------|
| Complete PROM survey | Questionnaire | — | Full 30 days | not-started |
| Order HbA1c lab | ActivityDefinition | ServiceRequest | First 7 days | not-started |
| Schedule follow-up | ActivityDefinition | Task | Last 7 days | not-started |

The CarePlan links back to the PlanDefinition via `instantiatesCanonical`, so you can
always trace which protocol generated it.

---

## How CarePlan Connects

```
PlanDefinition (template)
   │
   └── instantiatesCanonical (traced by)
         │
         CarePlan (John Smith's plan)
           ├── subject: Patient/1
           ├── period: Apr 18 – May 18
           ├── activity[0]: Complete PROM survey
           │     └── instantiatesCanonical → Questionnaire URL
           ├── activity[1]: Order HbA1c
           │     └── instantiatesCanonical → ActivityDefinition URL
           └── activity[2]: Schedule follow-up
                 └── instantiatesCanonical → ActivityDefinition URL
```

---

## Searching CarePlans

| Search | URL | Use |
|--------|-----|-----|
| By patient | `?subject=Patient/1` | All plans for a patient |
| By status | `?status=active` | Only active plans |
| By category | `?category=assess-plan` | Assessment plans only |
| By date | `?date=ge2026-04-01` | Plans active after a date |
| By definition | `?instantiates-canonical=http://...` | Plans from a specific protocol |
| Combined | `?subject=Patient/1&status=active` | Active plans for a patient |

---

## What Our Code Will Do

We'll create a `CarePlanService` that:

1. **Creates a CarePlan from a PlanDefinition** — reads the PlanDefinition, maps each
   action to a CarePlan activity, sets patient-specific references and timing
2. **Reads a CarePlan** — fetches by ID
3. **Searches CarePlans** — by patient, status, or protocol
4. **Gets the activity schedule** — extracts activities with their descriptions,
   timing, and status in a displayable format

---

## Common Questions

**Q: Why not just use $apply?**
Most FHIR servers (including HAPI without the CDS module) don't support `$apply`.
Building CarePlans in code is the standard approach and gives you more control over
patient-specific logic (e.g., adjusting timing based on patient age or conditions).

**Q: Can a patient have multiple active CarePlans?**
Yes — a patient might have a diabetes care plan, a hypertension care plan, and a
post-surgery recovery plan all active at the same time.

**Q: Who updates activity status?**
Your application code. When a QuestionnaireResponse is submitted, your app updates the
survey activity to `completed`. When a ServiceRequest is created, the lab activity moves
to `in-progress`. FHIR doesn't auto-update these — that's application workflow logic.

**Q: What's the difference between CarePlan.status and activity.detail.status?**
CarePlan.status is the overall plan status. Activity statuses are independent — a plan
can be `active` while some activities are `completed` and others are `not-started`.
The plan moves to `completed` when all activities are done (your app decides this).

**Q: Can I add activities to a CarePlan that aren't in the PlanDefinition?**
Yes — a clinician might add ad-hoc activities. The CarePlan is a living document. The
`instantiatesCanonical` on the activity level tracks which ones came from the protocol
and which were added manually.

---

## Summary

- **CarePlan** is a patient-specific plan created from a PlanDefinition template
- Each PlanDefinition action becomes a CarePlan **activity** with concrete timing
- `instantiatesCanonical` links the CarePlan back to its source PlanDefinition
- Activities have independent status tracking (not-started → completed)
- **$apply** is the FHIR-standard way to generate CarePlans, but most servers don't
  support it — building in code is the practical approach
- CarePlans are living documents — activities can be added, updated, or cancelled

## Next Step

Proceed to **Lesson 16** — display the activity schedule from the CarePlan, showing
what needs to be done, when, and current status.
