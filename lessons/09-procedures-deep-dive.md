# Lesson 9: Procedures — Actions Performed on the Patient

## Overview

You've measured things (Observations), diagnosed problems (Conditions). Now it's time for
what was actually **done** about those problems: **Procedures**.

A Procedure is any action performed on, with, or for a patient. Surgeries, injections,
counseling sessions, imaging studies, physical therapy — if a clinician *did something*
to or for a patient, it's a Procedure.

---

## What Is a Procedure?

A Procedure answers the question: **"What was done to this patient, when, and why?"**

```
+-----------------------------------------------+
|                  Procedure                     |
|                                                |
|  WHAT was done?           -> code (SNOMED/CPT) |
|  WHO was it done to?      -> subject (Patient)  |
|  WHEN was it done?        -> performed[x]       |
|  WHERE (visit context)?   -> encounter           |
|  WHY was it done?         -> reasonReference     |
|  HOW did it go?           -> status              |
|  WHO did it?              -> performer            |
+-----------------------------------------------+
```

### Procedures vs. Observations vs. Conditions

| Resource | Question it answers | Example |
|----------|-------------------|---------|
| **Observation** | What was measured? | BP reading: 160/100 |
| **Condition** | What's the problem? | Hypertension |
| **Procedure** | What was done about it? | BP monitoring, prescribed medication |

The clinical flow: **Observe -> Diagnose -> Treat**

```
Observation (BP: 160/100)
     |
     v
Condition (Hypertension)
     |
     +---> Procedure (BP monitoring)
     +---> MedicationRequest (antihypertensive prescription)
```

---

## The Full Procedure Resource — Field by Field

### Identity & Status

| Field | Type | Required? | What it is |
|-------|------|-----------|-----------|
| `id` | string | Server-assigned | Technical ID |
| `meta` | Meta | Auto | Version, timestamps, profile |
| `identifier` | Identifier[] | No | Business identifiers (e.g., surgical case number) |
| `status` | code | **YES** | Current state of the procedure |
| `statusReason` | CodeableConcept | No | Why the procedure has this status (especially for not-done) |

**Procedure statuses:**

| Status | Meaning | When to use |
|--------|---------|-------------|
| `preparation` | Being set up | OR is being prepped, instruments laid out |
| `in-progress` | Currently happening | Surgery underway, therapy session ongoing |
| `not-done` | Was planned but didn't happen | Patient refused, contraindication found |
| `on-hold` | Paused | Waiting for anesthesia, complication paused the surgery |
| `stopped` | Started but ended early | Procedure aborted due to complication |
| `completed` | Done successfully | Normal case — procedure finished |
| `entered-in-error` | Should never have existed | Wrong patient, duplicate entry |
| `unknown` | Status not known | Legacy data import |

In practice, most procedures you create will be `completed`. But `not-done` is important —
it documents that something was *supposed* to happen but didn't, and `statusReason`
explains why.

```json
{
  "status": "not-done",
  "statusReason": {
    "coding": [
      {
        "system": "http://snomed.info/sct",
        "code": "183944003",
        "display": "Procedure refused"
      }
    ]
  }
}
```

### What Was Done

| Field | Type | Required? | What it is |
|-------|------|-----------|-----------|
| `code` | CodeableConcept | No* | The procedure performed (SNOMED or CPT) |
| `category` | CodeableConcept | No | Broad classification |

*US Core requires `code`.

**Categories** (system: `http://snomed.info/sct`):

| Code | Display | Examples |
|------|---------|---------|
| `387713003` | Surgical procedure | Appendectomy, joint replacement |
| `103693007` | Diagnostic procedure | Biopsy, MRI, endoscopy |
| `46947000` | Counseling | Smoking cessation, dietary counseling |
| `225358003` | Physiotherapy | Physical therapy, occupational therapy |

### When Was It Done — performed[x]

Like Observation's `effective[x]`, this is polymorphic:

| Field | Type | Use for |
|-------|------|---------|
| `performedDateTime` | dateTime | Quick procedures (injection, blood draw) |
| `performedPeriod` | Period | Procedures that take time (surgery with start/end) |
| `performedString` | string | Vague timing ("last year", "in childhood") |
| `performedAge` | Age | Historical procedures ("at age 12") |
| `performedRange` | Range | Approximate age range ("between 10 and 15") |

For most procedures, you'll use either `performedDateTime` (quick ones) or `performedPeriod`
(longer ones like surgeries).

```json
"performedPeriod": {
  "start": "2026-04-10T08:00:00Z",
  "end": "2026-04-10T10:30:00Z"
}
```

### Who, Where, and Why

| Field | Type | Required? | What it is |
|-------|------|-----------|-----------|
| `subject` | Reference(Patient) | **YES** | Who the procedure was performed on |
| `encounter` | Reference(Encounter) | No | The visit during which it happened |
| `performer` | BackboneElement[] | No | Who performed it |
| `performer.actor` | Reference | No | The practitioner, organization, or device |
| `performer.function` | CodeableConcept | No | Their role (surgeon, assistant, anesthetist) |
| `reasonCode` | CodeableConcept[] | No | Why it was done (coded) |
| `reasonReference` | Reference[] | No | Why it was done (link to Condition, Observation, etc.) |

**`reasonReference` is the key relationship.** It links the Procedure back to the Condition
that justified it:

```json
"reasonReference": [
  { "reference": "Condition/99" }
]
```

This answers "why was this done?" with a traceable link. It's important for:
- **Clinical reasoning** — understanding the treatment rationale
- **Billing** — procedures must be justified by a diagnosis
- **Auditing** — was this procedure medically necessary?

#### Performer roles

A complex procedure like surgery can have multiple performers:

```json
"performer": [
  {
    "function": {
      "coding": [
        {
          "system": "http://snomed.info/sct",
          "code": "304292004",
          "display": "Surgeon"
        }
      ]
    },
    "actor": { "reference": "Practitioner/10" }
  },
  {
    "function": {
      "coding": [
        {
          "system": "http://snomed.info/sct",
          "code": "158970007",
          "display": "Anesthetist"
        }
      ]
    },
    "actor": { "reference": "Practitioner/11" }
  }
]
```

### Body Site and Method

| Field | Type | What it is |
|-------|------|-----------|
| `bodySite` | CodeableConcept[] | Where on the body (e.g., left knee, right arm) |
| `location` | Reference(Location) | Physical location (OR room, clinic) |
| `outcome` | CodeableConcept | Overall result (successful, unsuccessful, partially successful) |
| `report` | Reference[] | Links to DiagnosticReport or DocumentReference with details |
| `complication` | CodeableConcept[] | What went wrong (if anything) |
| `complicationDetail` | Reference(Condition)[] | Links to Conditions created by complications |
| `followUp` | CodeableConcept[] | Follow-up instructions |
| `note` | Annotation[] | Free-text notes |

**`complicationDetail` is interesting** — if a surgery causes a complication (e.g., infection),
you create a new Condition for the infection and link it here. This tracks that the
procedure *caused* the condition.

### Related Resources

| Field | Type | What it is |
|-------|------|-----------|
| `basedOn` | Reference[] | The order that requested this (ServiceRequest, CarePlan) |
| `partOf` | Reference[] | Parent procedure (e.g., a biopsy that's part of a larger surgery) |
| `focalDevice` | BackboneElement[] | Devices implanted or removed during the procedure |
| `usedReference` | Reference[] | Devices, medications, or substances used |
| `usedCode` | CodeableConcept[] | Coded items used (when you don't have a specific resource) |

---

## Code Systems for Procedures

### SNOMED CT — Clinical Procedure Codes

System: `http://snomed.info/sct`

SNOMED is the primary coding system for procedures in clinical systems.

#### Common Procedure Codes

| SNOMED Code | Display | What it is |
|------------|---------|-----------|
| `46973005` | Blood pressure taking | Measuring BP |
| `413153004` | Blood pressure monitoring | Ongoing BP monitoring |
| `104001` | Excision of lesion of patella | Knee surgery |
| `80146002` | Appendectomy | Removing the appendix |
| `73761001` | Colonoscopy | Examining the colon |
| `71388002` | Procedure (generic) | Non-specific procedure |
| `36969009` | Placement of stent | Cardiac/vascular stent |
| `18286008` | Catheterization of heart | Cardiac catheterization |
| `392021009` | Lumpectomy of breast | Breast cancer surgery |
| `44608003` | Joint replacement | Hip/knee replacement |
| `225358003` | Physiotherapy | Physical therapy |
| `370995009` | Health risk assessment | Risk screening |
| `710824005` | Assessment of health and social care needs | Social needs evaluation |
| `225323000` | Smoking cessation education | Tobacco counseling |

### CPT — Billing Procedure Codes (US)

System: `http://www.ama-assn.org/go/cpt`

CPT (Current Procedural Terminology) is required for billing in the US. Like ICD-10 for
diagnoses, CPT is the billing language for procedures.

| CPT Code | Display | What it is |
|---------|---------|-----------|
| `99213` | Office visit, established patient, low complexity | Standard follow-up |
| `99214` | Office visit, established patient, moderate complexity | Extended visit |
| `99203` | Office visit, new patient, low complexity | New patient visit |
| `43239` | Upper GI endoscopy with biopsy | Diagnostic procedure |
| `27447` | Total knee arthroplasty | Knee replacement |
| `33533` | Coronary artery bypass, single graft | Heart surgery |
| `93000` | Electrocardiogram, complete | EKG |

**Why dual coding matters (same as Conditions):**
- **SNOMED** describes what was done clinically
- **CPT** describes what to bill for

```json
"code": {
  "coding": [
    {
      "system": "http://snomed.info/sct",
      "code": "73761001",
      "display": "Colonoscopy"
    },
    {
      "system": "http://www.ama-assn.org/go/cpt",
      "code": "45378",
      "display": "Colonoscopy, diagnostic"
    }
  ]
}
```

---

## Real-World Examples

### Example 1: Simple Procedure — Blood Pressure Taking

This is what our `ProcedureService.CreateProcedureAsync` creates:

```json
{
  "resourceType": "Procedure",
  "status": "completed",
  "code": {
    "coding": [
      {
        "system": "http://snomed.info/sct",
        "code": "46973005",
        "display": "Blood pressure taking"
      }
    ]
  },
  "subject": { "reference": "Patient/1" },
  "encounter": { "reference": "Encounter/2" },
  "performedDateTime": "2026-04-10T09:30:00Z",
  "reasonReference": [
    { "reference": "Condition/4" }
  ]
}
```

### Example 2: Surgery with Period, Performer, and Body Site

A knee replacement surgery with full details:

```json
{
  "resourceType": "Procedure",
  "status": "completed",
  "category": {
    "coding": [
      {
        "system": "http://snomed.info/sct",
        "code": "387713003",
        "display": "Surgical procedure"
      }
    ]
  },
  "code": {
    "coding": [
      {
        "system": "http://snomed.info/sct",
        "code": "44608003",
        "display": "Total knee replacement"
      },
      {
        "system": "http://www.ama-assn.org/go/cpt",
        "code": "27447",
        "display": "Total knee arthroplasty"
      }
    ]
  },
  "subject": { "reference": "Patient/1" },
  "encounter": { "reference": "Encounter/10" },
  "performedPeriod": {
    "start": "2026-04-15T07:30:00Z",
    "end": "2026-04-15T10:00:00Z"
  },
  "performer": [
    {
      "function": {
        "coding": [
          {
            "system": "http://snomed.info/sct",
            "code": "304292004",
            "display": "Surgeon"
          }
        ]
      },
      "actor": { "reference": "Practitioner/5" }
    }
  ],
  "reasonReference": [
    { "reference": "Condition/50" }
  ],
  "bodySite": [
    {
      "coding": [
        {
          "system": "http://snomed.info/sct",
          "code": "72696002",
          "display": "Left knee"
        }
      ]
    }
  ],
  "outcome": {
    "coding": [
      {
        "system": "http://snomed.info/sct",
        "code": "385669000",
        "display": "Successful"
      }
    ]
  },
  "followUp": [
    {
      "coding": [
        {
          "system": "http://snomed.info/sct",
          "code": "225358003",
          "display": "Physiotherapy"
        }
      ]
    }
  ]
}
```

### Example 3: Procedure Not Done — Patient Refused

A planned colonoscopy that the patient refused:

```json
{
  "resourceType": "Procedure",
  "status": "not-done",
  "statusReason": {
    "coding": [
      {
        "system": "http://snomed.info/sct",
        "code": "183944003",
        "display": "Procedure refused by patient"
      }
    ]
  },
  "code": {
    "coding": [
      {
        "system": "http://snomed.info/sct",
        "code": "73761001",
        "display": "Colonoscopy"
      }
    ]
  },
  "subject": { "reference": "Patient/1" },
  "encounter": { "reference": "Encounter/12" },
  "performedDateTime": "2026-04-10",
  "note": [
    {
      "text": "Patient declined colonoscopy. Discussed risks of not screening. Will revisit at next annual visit."
    }
  ]
}
```

Recording `not-done` matters — it documents that screening was offered and refused, which
is important for quality metrics and liability.

### Example 4: Diagnostic Procedure with Complications

A cardiac catheterization that caused a complication:

```json
{
  "resourceType": "Procedure",
  "status": "completed",
  "code": {
    "coding": [
      {
        "system": "http://snomed.info/sct",
        "code": "18286008",
        "display": "Catheterization of heart"
      }
    ]
  },
  "subject": { "reference": "Patient/1" },
  "encounter": { "reference": "Encounter/15" },
  "performedPeriod": {
    "start": "2026-04-12T13:00:00Z",
    "end": "2026-04-12T14:30:00Z"
  },
  "reasonReference": [
    { "reference": "Condition/60" }
  ],
  "complication": [
    {
      "coding": [
        {
          "system": "http://snomed.info/sct",
          "code": "131148009",
          "display": "Bleeding"
        }
      ]
    }
  ],
  "complicationDetail": [
    { "reference": "Condition/65" }
  ]
}
```

Here, `complicationDetail` points to a new Condition resource that was created because the
procedure caused bleeding. This links cause (Procedure) to effect (new Condition).

---

## How Procedures Connect to Everything

```
Patient
  |
  +-- Encounter (visit)
  |     |
  |     +-- Observation (BP: 160/100)        <- measurement
  |     |
  |     +-- Condition (Hypertension)          <- diagnosis
  |     |     |
  |     |     +-- evidence -> Observation     <- proof
  |     |
  |     +-- Procedure (BP monitoring)         <- action taken
  |           |
  |           +-- reasonReference -> Condition <- why it was done
  |
  +-- Encounter (surgery)
        |
        +-- Procedure (Knee replacement)
        |     |
        |     +-- reasonReference -> Condition (osteoarthritis)
        |     +-- complicationDetail -> Condition (post-op infection)
        |     +-- performer -> Practitioner (surgeon)
        |     +-- bodySite -> Left knee
        |
        +-- Procedure (Anesthesia)
              +-- partOf -> Procedure (Knee replacement)
```

### Searching for Procedures

```
GET /Procedure?subject=Patient/1                          <- all procedures for a patient
GET /Procedure?encounter=Encounter/2                      <- procedures from a visit
GET /Procedure?code=46973005                              <- all BP monitoring procedures
GET /Procedure?reason-reference=Condition/4               <- procedures justified by a condition
GET /Procedure?date=ge2026-01-01                          <- procedures since Jan 2026
GET /Procedure?status=not-done                            <- procedures that were skipped
GET /Procedure?subject=Patient/1&status=completed         <- completed procedures for a patient
```

---

## US Core Procedure Profile

US Core defines one Procedure profile.

| Field | Required? | Notes |
|-------|-----------|-------|
| `status` | YES | Must be a valid status code |
| `code` | YES | Must be from US Core Procedure Codes (extensible binding to SNOMED CT) |
| `subject` | YES | Reference to US Core Patient |
| `performed[x]` | YES | When the procedure was done |

US Core requires `performed[x]` — you must always say *when* the procedure happened. The
base FHIR Procedure doesn't require this, but US Core does.

---

## What Our Code Does vs. What's Possible

Our `ProcedureService.CreateProcedureAsync` creates a procedure with:

| Field | What we set | What else is possible |
|-------|------------|----------------------|
| `status` | `completed` | not-done, in-progress, stopped, preparation |
| `code` | SNOMED only | SNOMED + CPT dual coding |
| `subject` | Patient reference | Same |
| `encounter` | Encounter reference | Can be omitted |
| `performed` | DateTime | Period (for surgeries), Age, Range, string |
| `reasonReference` | Condition link | Can also link to Observation, DiagnosticReport |
| `performer` | Not set | Practitioner with role (surgeon, anesthetist) |
| `bodySite` | Not set | Where on the body (SNOMED coded) |
| `outcome` | Not set | Successful, unsuccessful, partially successful |
| `complication` | Not set | What went wrong |
| `complicationDetail` | Not set | Link to Conditions caused by the procedure |
| `followUp` | Not set | Follow-up instructions |
| `report` | Not set | Link to DiagnosticReport with full results |
| `note` | Not set | Free-text notes |
| `category` | Not set | Surgical, diagnostic, counseling |
| `statusReason` | Not set | Why it wasn't done (for not-done status) |

---

## The Complete Clinical Story — All Resources Connected

Now that you know Patient, Encounter, Observation, Condition, and Procedure, here's how
a full clinical visit looks as FHIR resources:

```
Patient/1 (John Smith)
  |
  +-- Encounter/2 (Outpatient visit, Apr 10, 2026)
        |
        |  1. MEASURE
        +-- Observation/3  (BP: 160/100 mmHg)
        +-- Observation/4  (Heart Rate: 88 /min)
        +-- Observation/5  (Temperature: 98.6 °F)
        +-- Observation/6  (Blood Glucose: 142 mg/dL)
        |
        |  2. DIAGNOSE
        +-- Condition/7   (Hypertension)
        |     +-- evidence -> Observation/3  (BP was the proof)
        +-- Condition/8   (Pre-diabetes)
        |     +-- evidence -> Observation/6  (Glucose was the proof)
        |
        |  3. TREAT
        +-- Procedure/9   (BP monitoring)
        |     +-- reasonReference -> Condition/7
        +-- Procedure/10  (Dietary counseling)
              +-- reasonReference -> Condition/8
```

Every link is traceable. You can follow the chain in any direction:
- "Why was dietary counseling done?" -> reasonReference -> Condition/8 (pre-diabetes)
- "What evidence supports pre-diabetes?" -> evidence.detail -> Observation/6 (glucose: 142)
- "What happened during this visit?" -> search by encounter -> all resources above

---

## Common Questions

**Q: What's the difference between a Procedure and a MedicationRequest?**
A Procedure is something *done to* the patient (surgery, therapy, monitoring). A
MedicationRequest is something *prescribed for* the patient (take this pill twice daily).
Both can have `reasonReference` pointing to a Condition.

**Q: Should I use `performedDateTime` or `performedPeriod`?**
Use `performedDateTime` for procedures that happen at a point in time (injection, blood
draw, quick measurement). Use `performedPeriod` for procedures that take meaningful time
(surgery, therapy session, infusion). If the procedure took 2+ hours, a Period with
start/end is more informative.

**Q: How do I record that a procedure was attempted but failed?**
Use `status: stopped` with a `note` explaining what happened. If the procedure was
completed but the outcome was unsuccessful, use `status: completed` with `outcome` set
to unsuccessful.

**Q: Can a Procedure reference multiple Conditions?**
Yes. `reasonReference` is an array. A surgery might address multiple problems:
```json
"reasonReference": [
  { "reference": "Condition/7" },
  { "reference": "Condition/8" }
]
```

**Q: What about procedures done outside this system?**
Use `performedString` for vague timing ("had appendectomy as a child") and omit
`encounter` since it didn't happen here. This is common when recording surgical history.

**Q: How does `partOf` work?**
It creates a parent-child relationship. Anesthesia is part of a surgery:
```json
{
  "resourceType": "Procedure",
  "code": { "text": "General anesthesia" },
  "partOf": [{ "reference": "Procedure/20" }]
}
```
Procedure/20 is the surgery; this anesthesia procedure is a sub-step of it.

---

## Summary

- **Procedures** represent actions performed on or for a patient
- **`status`** tracks the lifecycle — `not-done` is important for documenting refused/skipped procedures
- **`code`** uses SNOMED CT (clinical) and CPT (billing), similar to dual coding on Conditions
- **`performed[x]`** is polymorphic — DateTime for quick procedures, Period for surgeries
- **`reasonReference`** links back to the Condition that justified the procedure
- **`complicationDetail`** links forward to any Conditions the procedure caused
- **`performer`** records who did it and their role
- Procedures complete the Observe -> Diagnose -> Treat chain

## Next Step

Proceed to **Step 7** in the learning plan — Create a Procedure based on the Condition.
You'll use the `ProcedureService` to create a blood pressure monitoring procedure linked
to a Patient, Encounter, and Condition, then verify the full clinical story with the
Step7 tests.
