# Lesson 14: PlanDefinition — Defining a Care Protocol

## Overview

So far we've built individual resources: Patient, Encounter, Observation, Condition,
Procedure, Questionnaire, QuestionnaireResponse. Each one captures a piece of the
clinical picture. But in real healthcare, these pieces are part of a **plan** — a
protocol that says "for this type of patient, do these things in this order."

That's what **PlanDefinition** is for. It's a reusable template that describes a care
protocol — what activities should happen, when, and in what order. Think of it as a
recipe that can be applied to any patient.

---

## What Is a PlanDefinition?

A PlanDefinition is a **protocol definition** — not tied to any specific patient. It
describes:

- What actions should be taken (e.g., "complete a survey", "schedule a follow-up")
- When they should happen (timing, triggers)
- What conditions must be met (applicability)
- What resources are needed (e.g., which Questionnaire to use)

### Real-World Examples

| PlanDefinition | Purpose |
|----------------|---------|
| Hypertension management protocol | BP monitoring schedule, medication steps, lifestyle counseling |
| Post-surgery recovery plan | Pain assessment schedule, wound care, physical therapy |
| Depression screening workflow | PHQ-9 at intake, repeat every 2 weeks, escalate if score > 15 |
| Diabetes care plan | HbA1c every 3 months, foot exam yearly, diet counseling |

### PlanDefinition vs. CarePlan

| Resource | What it is | Analogy |
|----------|-----------|---------|
| **PlanDefinition** | A reusable protocol template | A recipe in a cookbook |
| **CarePlan** | A patient-specific plan | Tonight's dinner, following the recipe |

A PlanDefinition says "patients with hypertension should get BP monitoring."
A CarePlan says "John Smith will get BP monitoring on April 25th."

---

## PlanDefinition Structure

```
PlanDefinition
  ├── metadata (title, status, description, type)
  ├── goal[] (what the protocol aims to achieve)
  └── action[] (what to do)
        ├── title, description
        ├── trigger[] (when to start)
        ├── condition[] (when this applies)
        ├── timing (schedule/period)
        ├── definitionCanonical (what resource defines the activity)
        └── action[] (nested sub-actions)
```

### Key Fields

| Field | Required? | Description |
|-------|-----------|-------------|
| `url` | YES (canonical) | Globally unique identifier |
| `status` | YES | draft / active / retired / unknown |
| `title` | Recommended | Human-readable name |
| `type` | Recommended | The type of protocol (e.g., clinical-protocol, eca-rule, workflow-definition) |
| `description` | Recommended | What this protocol does |
| `goal` | No | Desired outcomes |
| `action` | YES | The activities in the protocol |

### Action Fields

| Field | Description |
|-------|-------------|
| `title` | Name of the activity |
| `description` | What to do |
| `priority` | routine / urgent / asap / stat |
| `trigger` | What starts this action (e.g., named-event, periodic) |
| `timingPeriod` | When the action should happen (start/end dates) |
| `timingTiming` | Repeating schedule (e.g., every 2 weeks) |
| `definitionCanonical` | URL of the resource that defines this activity (e.g., a Questionnaire URL) |
| `type` | create / update / remove / fire-event |

---

## PlanDefinition Types

| Type Code | Description | Example |
|-----------|-------------|---------|
| `clinical-protocol` | A clinical care protocol | Hypertension management |
| `eca-rule` | Event-Condition-Action rule | "If BP > 140, alert provider" |
| `workflow-definition` | A multi-step workflow | Referral process |
| `order-set` | A set of orders to place together | Admission order set |

For our PROM use case, `clinical-protocol` fits best — it defines a care protocol
that includes completing a survey.

---

## ActivityDefinition — What an Action Actually Does

Before we build the PlanDefinition, we need to understand **ActivityDefinition**. While
a PlanDefinition says "do these things," an ActivityDefinition says "here's exactly what
one of those things looks like."

An ActivityDefinition is a template for a specific request resource. When applied, it
generates a concrete resource. The `kind` field determines what type of resource gets
created:

| ActivityDefinition `kind` | Generates | Use Case | Example |
|---------------------------|-----------|----------|---------|
| `ServiceRequest` | A lab/procedure order | Ordering tests, imaging, referrals | "Order HbA1c test", "Request chest X-ray" |
| `Task` | A generic to-do item | Scheduling, reminders, admin work | "Schedule follow-up in 2 weeks" |
| `MedicationRequest` | A prescription | Prescribing drugs | "Prescribe lisinopril 10mg daily" |
| `CommunicationRequest` | A message to send | Notifications, patient outreach | "Send appointment reminder via SMS" |
| `NutritionOrder` | A diet order | Dietary management | "Low sodium diet for hypertension" |
| `DeviceRequest` | An equipment request | Medical device orders | "Provide home BP monitor" |
| `Appointment` | A scheduled visit | Booking appointments | "Cardiology consult in 30 days" |
| `SupplyRequest` | A supply order | Ordering materials | "Order glucose test strips" |
| `VisionPrescription` | An optical prescription | Eye care | "Prescribe corrective lenses" |
| `ImmunizationRecommendation` | A vaccine recommendation | Vaccination schedules | "Recommend flu vaccine" |

The most common kinds you'll encounter in practice are **ServiceRequest** (labs,
procedures, referrals), **MedicationRequest** (prescriptions), **Task** (generic
to-dos), and **CommunicationRequest** (messages/notifications).

### Key ActivityDefinition Fields

| Field | Required? | Description |
|-------|-----------|-------------|
| `url` | YES (canonical) | Globally unique identifier |
| `status` | YES | draft / active / retired |
| `kind` | YES | What type of request this generates (ServiceRequest, Task, etc.) |
| `intent` | Recommended | order / proposal / plan |
| `priority` | No | routine / urgent / asap / stat |
| `code` | Recommended | What specifically to do (e.g., LOINC code for a lab test) |
| `doNotPerform` | No | If true, this is a "don't do" instruction |

### Our Two ActivityDefinitions

1. **Order HbA1c Lab Test** (`kind: ServiceRequest`)
   - Code: LOINC `4548-4` (Hemoglobin A1c)
   - Intent: `order`
   - When applied → generates a ServiceRequest to order the lab

2. **Schedule Follow-Up Appointment** (`kind: Task`)
   - Code: text description (follow-up within 2 weeks)
   - Intent: `proposal`
   - When applied → generates a Task for scheduling

---

## Our PlanDefinition

We'll create a PlanDefinition called "PROM Assessment Protocol" with **three actions**:

1. **Complete PROM Questionnaire** → references the Questionnaire (30-day window)
2. **Order HbA1c Lab Test** → references the lab order ActivityDefinition (first 7 days)
3. **Schedule Follow-Up Appointment** → references the follow-up ActivityDefinition (last 7 days)

Each action has its own timing period within the overall 30-day protocol window:

```
Day 0                    Day 7              Day 23              Day 30
|========================|                   |==================|
  Complete PROM survey (full 30 days)
|========|
  Order HbA1c (first 7 days)
                                             |==================|
                                               Schedule follow-up (last 7 days)
```

The PlanDefinition is backward-compatible — the ActivityDefinition URLs are optional
parameters, so calling it with just the Questionnaire URL still works (single action).

---

## How PlanDefinition Connects

```
PlanDefinition (protocol template)
   │
   ├── action[0].definitionCanonical → Questionnaire (fill out PROM survey)
   ├── action[1].definitionCanonical → ActivityDefinition (order HbA1c lab)
   ├── action[2].definitionCanonical → ActivityDefinition (schedule follow-up)
   │
   └── $apply (operation) → CarePlan (patient-specific plan)
         ├── subject: Patient/1
         ├── activity[0] → complete questionnaire
         ├── activity[1] → lab order (ServiceRequest)
         ├── activity[2] → follow-up task (Task)
         └── period: 30-day window
```

The `$apply` operation is what turns a PlanDefinition into a CarePlan for a specific
patient. We'll explore that in the next lesson.

---

## Goals

A PlanDefinition can define goals — the desired outcomes of following the protocol.

```json
"goal": [{
  "description": {
    "text": "Assess patient-reported health outcomes"
  },
  "priority": {
    "coding": [{
      "system": "http://terminology.hl7.org/CodeSystem/goal-priority",
      "code": "medium-priority"
    }]
  }
}]
```

Goals are informational — they describe what the protocol aims to achieve but don't
enforce anything. They're useful for documentation and clinical decision support.

---

## FHIR Operations on PlanDefinition

| Operation | Description |
|-----------|-------------|
| `POST /fhir/PlanDefinition` | Create a new protocol |
| `GET /fhir/PlanDefinition/{id}` | Read a specific protocol |
| `GET /fhir/PlanDefinition?title=PROM` | Search by title |
| `GET /fhir/PlanDefinition?status=active` | Search by status |
| `POST /fhir/PlanDefinition/{id}/$apply` | Apply to a patient → generates a CarePlan |

The `$apply` operation is the most interesting — it takes a PlanDefinition and a
subject (Patient), and generates a CarePlan. Not all FHIR servers support it;
HAPI has partial support.

---

## PlanDefinition Lifecycle

```
draft  ──>  active  ──>  retired
              │
              └── ($apply to patients to generate CarePlans)
```

Same lifecycle as Questionnaire. Only `active` PlanDefinitions should be applied to
patients. Retired ones are kept for historical reference.

---

## Common Questions

**Q: Can a PlanDefinition have multiple actions?**
Yes — and actions can be nested. A top-level action might be "Hypertension Management"
with sub-actions for "Monitor BP", "Prescribe medication", and "Lifestyle counseling."

**Q: What's the difference between trigger and timing?**
A **trigger** starts the action (e.g., "when the patient is admitted"). **Timing**
says when/how often to do it (e.g., "every 2 weeks for 3 months"). You can have both:
"trigger on admission, then repeat every 2 weeks."

**Q: Does HAPI support $apply?**
HAPI has partial support for `$apply`. It can generate a basic CarePlan from a
PlanDefinition, but complex logic (conditions, dynamic values) may not be fully
supported. For our simple case with one survey action, it works.

**Q: Can I reference activities other than Questionnaires?**
Yes — and we do exactly that in this lesson. `definitionCanonical` can point to any
ActivityDefinition, Questionnaire, or PlanDefinition. Our protocol references a
Questionnaire (survey), and two ActivityDefinitions (lab order and follow-up task).

---

## What Our Code Will Do

We'll create two services:

### ActivityDefinitionService
- `CreateLabOrderActivityAsync()` — builds an ActivityDefinition of kind `ServiceRequest`
  with a LOINC code for HbA1c lab test
- `CreateFollowUpTaskActivityAsync()` — builds an ActivityDefinition of kind `Task` for
  scheduling a follow-up appointment
- Read and search operations

### PlanDefinitionService
- `CreatePromAssessmentProtocolAsync()` — builds a PlanDefinition with up to 3 actions:
  1. Complete PROM Questionnaire (always included)
  2. Order HbA1c lab test (optional, via ActivityDefinition URL)
  3. Schedule follow-up (optional, via ActivityDefinition URL)
- Read and search operations

The PlanDefinition method accepts optional ActivityDefinition URLs — pass none for a
single-action plan, or pass both for the full 3-action protocol.

---

## Summary

- **PlanDefinition** is a reusable care protocol template — not tied to any patient
- **ActivityDefinition** is a template for a single action (generates ServiceRequest, Task, etc.)
- PlanDefinition actions reference Questionnaires or ActivityDefinitions via **definitionCanonical**
- Each action has its own **timing** (when to do it within the protocol window)
- The **$apply** operation turns a PlanDefinition into a patient-specific **CarePlan**
- Types include clinical-protocol, eca-rule, workflow-definition, and order-set
- Same lifecycle as other definitional resources: draft → active → retired

## Next Step

Proceed to **Lesson 15** — apply the PlanDefinition to Patient/1 using `$apply` to
generate a CarePlan, then validate the generated plan.
