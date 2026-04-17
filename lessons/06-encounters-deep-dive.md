# Lesson 6: Encounters — The Clinical Context for Everything

## Overview

In Lesson 01 you learned that an Encounter represents a "visit." You've created and updated
Patients — now before you create your first Encounter, let's understand what it actually IS,
what you can store in it, and how it connects to the rest of the clinical record.

This lesson is the theory. After this, you'll create your first Encounter in Step 4 of the
learning plan.

---

## What Is an Encounter?

An Encounter represents **any interaction between a patient and a healthcare provider**. That
includes obvious things like a doctor visit, but also:

- A phone call with a nurse
- An emergency room visit
- A hospital admission (which can last days)
- A telemedicine video call
- A home health visit
- A lab walk-in for blood work

**The key idea:** An Encounter is the **container** that groups everything that happened during
an interaction. Observations, diagnoses, procedures, notes, orders — they all point back to
the Encounter that triggered them.

```
Patient comes in for a checkup
        |
        v
  ┌─────────────────────────────────────┐
  │         Encounter (the visit)       │
  │                                     │
  │  ┌─────────────┐  ┌──────────────┐  │
  │  │ Observation  │  │ Observation  │  │
  │  │ (BP reading) │  │ (heart rate) │  │
  │  └─────────────┘  └──────────────┘  │
  │                                     │
  │  ┌─────────────┐  ┌──────────────┐  │
  │  │  Condition   │  │  Procedure   │  │
  │  │ (diagnosis)  │  │ (blood draw) │  │
  │  └─────────────┘  └──────────────┘  │
  │                                     │
  └─────────────────────────────────────┘
```

Without the Encounter, you'd have a bag of disconnected observations and diagnoses with no
way to know they happened at the same visit.

---

## The Full Encounter Resource — Field by Field

Here is every field available on the FHIR R4 Encounter resource, grouped by purpose.

### Identity & Status

| Field | Type | Required? | What it is |
|-------|------|-----------|-----------|
| `id` | string | Server-assigned | The technical ID (e.g., `"42"`) |
| `meta` | Meta | Auto | Version, last updated, profile, tags |
| `identifier` | Identifier[] | No | Business identifiers (e.g., visit number `"VN-2026-0042"`) |
| `status` | code | **YES** | Current state of the encounter (see status list below) |
| `statusHistory` | BackboneElement[] | No | Past statuses with time periods — tracks the encounter's lifecycle |

**Encounter statuses** — these represent the lifecycle of a visit:

```
planned → arrived → triaged → in-progress → onleave → finished
                                                   \→ cancelled
                                    entered-in-error (can happen anytime)
```

| Status | Meaning | Example |
|--------|---------|---------|
| `planned` | Scheduled but hasn't started | Appointment booked for next Tuesday |
| `arrived` | Patient has checked in | Patient at the front desk |
| `triaged` | Patient assessed for urgency | ER nurse assessed the patient |
| `in-progress` | Visit is actively happening | Doctor is with the patient |
| `onleave` | Patient temporarily away | Patient went to radiology |
| `finished` | Visit is complete | Patient has been discharged |
| `cancelled` | Visit was cancelled before it started | Patient no-showed |
| `entered-in-error` | Should not have been created | Created on the wrong patient |
| `unknown` | Status not known | Legacy data import |

### Classification

| Field | Type | Required? | What it is |
|-------|------|-----------|-----------|
| `class` | Coding | **YES** | Broad category of the encounter |
| `classHistory` | BackboneElement[] | No | Past classes with time periods (e.g., patient moved from ER to inpatient) |
| `type` | CodeableConcept[] | No | More specific type of encounter |
| `serviceType` | CodeableConcept | No | Specific service within the encounter type |
| `priority` | CodeableConcept | No | Urgency (e.g., emergency, elective, routine) |

**Encounter class** — this is the most important classification:

| Code | Display | Meaning |
|------|---------|---------|
| `AMB` | Ambulatory | Outpatient visit — patient walks in, sees the doctor, walks out |
| `IMP` | Inpatient | Admitted to the hospital — stays overnight |
| `EMER` | Emergency | Emergency room visit |
| `SS` | Short Stay | Observation/day case — patient stays but less than inpatient |
| `HH` | Home Health | Provider visits the patient at home |
| `VR` | Virtual | Telemedicine / video call |
| `FLD` | Field | Mobile / community encounter |

The class uses the code system `http://terminology.hl7.org/CodeSystem/v3-ActCode`.

**Encounter type** uses SNOMED CT codes. Common examples:

| SNOMED Code | Display |
|-------------|---------|
| `185349003` | Encounter for check up |
| `270427003` | Patient-initiated encounter |
| `308335008` | Patient encounter procedure |
| `11429006` | Consultation |
| `50849002` | Emergency room admission |
| `32485007` | Hospital admission |

### Who and Where

| Field | Type | Required? | What it is |
|-------|------|-----------|-----------|
| `subject` | Reference(Patient) | No* | The patient this encounter is for |
| `participant` | BackboneElement[] | No | Providers involved (doctors, nurses, etc.) |
| `appointment` | Reference(Appointment)[] | No | The appointment that led to this encounter |
| `episodeOfCare` | Reference(EpisodeOfCare)[] | No | Groups related encounters (e.g., all visits for a pregnancy) |
| `basedOn` | Reference(ServiceRequest)[] | No | The order/referral that triggered this encounter |
| `location` | BackboneElement[] | No | Physical location(s) where the encounter happened |
| `serviceProvider` | Reference(Organization) | No | The organization responsible |

*`subject` is not technically required by the base spec but is required by US Core and is
always populated in practice.

**participant** — this is where you record who was involved:

```json
"participant": [
  {
    "type": [
      {
        "coding": [
          {
            "system": "http://terminology.hl7.org/CodeSystem/v3-ParticipationType",
            "code": "ATND",
            "display": "attender"
          }
        ]
      }
    ],
    "individual": {
      "reference": "Practitioner/dr-jones",
      "display": "Dr. Sarah Jones"
    },
    "period": {
      "start": "2026-04-10T09:00:00Z",
      "end": "2026-04-10T09:30:00Z"
    }
  }
]
```

Participant type codes:

| Code | Meaning |
|------|---------|
| `ATND` | Attender — the primary provider |
| `ADM` | Admitter — who admitted the patient |
| `DIS` | Discharger — who discharged the patient |
| `CON` | Consultant — consulting provider |
| `REF` | Referrer — who referred the patient |
| `PPRF` | Primary performer |
| `SPRF` | Secondary performer |

**location** — an encounter can span multiple locations:

```json
"location": [
  {
    "location": {
      "reference": "Location/er-bay-3",
      "display": "ER Bay 3"
    },
    "status": "completed",
    "physicalType": {
      "coding": [
        {
          "system": "http://terminology.hl7.org/CodeSystem/location-physical-type",
          "code": "bd",
          "display": "Bed"
        }
      ]
    },
    "period": {
      "start": "2026-04-10T09:00:00Z",
      "end": "2026-04-10T11:00:00Z"
    }
  }
]
```

### When and Why

| Field | Type | Required? | What it is |
|-------|------|-----------|-----------|
| `period` | Period | No | Start and end time of the encounter |
| `length` | Duration | No | How long it lasted (calculated or explicit) |
| `reasonCode` | CodeableConcept[] | No | Why the visit happened (chief complaint) |
| `reasonReference` | Reference[] | No | Links to Condition/Observation/Procedure that triggered the visit |

**period** — most encounters have both start and end:

```json
"period": {
  "start": "2026-04-10T09:00:00-05:00",
  "end": "2026-04-10T09:45:00-05:00"
}
```

For in-progress encounters, `end` is omitted until the visit completes.

**reasonCode** vs **reasonReference**:
- `reasonCode` — a coded reason like "Annual physical exam" (SNOMED `185349003`)
- `reasonReference` — points to an existing Condition, e.g., the patient came in because of
  their diabetes (`Condition/123`)

You can use both simultaneously.

### Clinical Details

| Field | Type | Required? | What it is |
|-------|------|-----------|-----------|
| `diagnosis` | BackboneElement[] | No | Diagnoses relevant to this encounter |
| `account` | Reference(Account)[] | No | Billing accounts for charges |
| `hospitalization` | BackboneElement | No | Admission/discharge details (inpatient only) |
| `partOf` | Reference(Encounter) | No | Parent encounter (for sub-encounters) |

**diagnosis** — links conditions to the encounter with a role:

```json
"diagnosis": [
  {
    "condition": {
      "reference": "Condition/hypertension-123"
    },
    "use": {
      "coding": [
        {
          "system": "http://terminology.hl7.org/CodeSystem/diagnosis-role",
          "code": "AD",
          "display": "Admission diagnosis"
        }
      ]
    },
    "rank": 1
  }
]
```

Diagnosis role codes:

| Code | Meaning |
|------|---------|
| `AD` | Admission diagnosis — why the patient was admitted |
| `DD` | Discharge diagnosis — what was confirmed at discharge |
| `CC` | Chief complaint — what the patient said was wrong |
| `CM` | Comorbidity — existing condition relevant to this visit |
| `pre-op` | Pre-op diagnosis |
| `post-op` | Post-op diagnosis |
| `billing` | Diagnosis used for billing |

**hospitalization** — only used for inpatient admissions:

```json
"hospitalization": {
  "admitSource": {
    "coding": [
      {
        "system": "http://terminology.hl7.org/CodeSystem/admit-source",
        "code": "emd",
        "display": "From accident/emergency department"
      }
    ]
  },
  "dischargeDisposition": {
    "coding": [
      {
        "system": "http://terminology.hl7.org/CodeSystem/discharge-disposition",
        "code": "home",
        "display": "Home"
      }
    ]
  }
}
```

---

## Real-World Examples

### Example 1: Simple Outpatient Visit

A patient comes in for a routine checkup. The doctor measures blood pressure, diagnoses
mild hypertension, and prescribes medication.

```json
{
  "resourceType": "Encounter",
  "status": "finished",
  "class": {
    "system": "http://terminology.hl7.org/CodeSystem/v3-ActCode",
    "code": "AMB",
    "display": "ambulatory"
  },
  "type": [
    {
      "coding": [
        {
          "system": "http://snomed.info/sct",
          "code": "185349003",
          "display": "Encounter for check up"
        }
      ]
    }
  ],
  "subject": {
    "reference": "Patient/1",
    "display": "John Smith"
  },
  "participant": [
    {
      "type": [
        {
          "coding": [
            {
              "system": "http://terminology.hl7.org/CodeSystem/v3-ParticipationType",
              "code": "ATND",
              "display": "attender"
            }
          ]
        }
      ],
      "individual": {
        "reference": "Practitioner/dr-jones"
      }
    }
  ],
  "period": {
    "start": "2026-04-10T09:00:00-05:00",
    "end": "2026-04-10T09:45:00-05:00"
  },
  "reasonCode": [
    {
      "coding": [
        {
          "system": "http://snomed.info/sct",
          "code": "185349003",
          "display": "Encounter for check up"
        }
      ]
    }
  ]
}
```

### Example 2: Emergency Room Visit

A patient arrives with chest pain. They are triaged, seen by an ER doctor, diagnosed with
angina, and discharged home.

```json
{
  "resourceType": "Encounter",
  "status": "finished",
  "class": {
    "system": "http://terminology.hl7.org/CodeSystem/v3-ActCode",
    "code": "EMER",
    "display": "emergency"
  },
  "type": [
    {
      "coding": [
        {
          "system": "http://snomed.info/sct",
          "code": "50849002",
          "display": "Emergency room admission"
        }
      ]
    }
  ],
  "priority": {
    "coding": [
      {
        "system": "http://terminology.hl7.org/CodeSystem/v3-ActPriority",
        "code": "EM",
        "display": "emergency"
      }
    ]
  },
  "subject": {
    "reference": "Patient/1"
  },
  "period": {
    "start": "2026-04-10T14:30:00-05:00",
    "end": "2026-04-10T20:15:00-05:00"
  },
  "reasonCode": [
    {
      "coding": [
        {
          "system": "http://snomed.info/sct",
          "code": "29857009",
          "display": "Chest pain"
        }
      ]
    }
  ],
  "diagnosis": [
    {
      "condition": {
        "reference": "Condition/angina-456"
      },
      "use": {
        "coding": [
          {
            "system": "http://terminology.hl7.org/CodeSystem/diagnosis-role",
            "code": "DD",
            "display": "Discharge diagnosis"
          }
        ]
      },
      "rank": 1
    }
  ],
  "hospitalization": {
    "admitSource": {
      "coding": [
        {
          "system": "http://terminology.hl7.org/CodeSystem/admit-source",
          "code": "other",
          "display": "Other"
        }
      ]
    },
    "dischargeDisposition": {
      "coding": [
        {
          "system": "http://terminology.hl7.org/CodeSystem/discharge-disposition",
          "code": "home",
          "display": "Home"
        }
      ]
    }
  }
}
```

### Example 3: Telemedicine Visit

A patient has a video call follow-up for a previously diagnosed condition.

```json
{
  "resourceType": "Encounter",
  "status": "finished",
  "class": {
    "system": "http://terminology.hl7.org/CodeSystem/v3-ActCode",
    "code": "VR",
    "display": "virtual"
  },
  "type": [
    {
      "coding": [
        {
          "system": "http://snomed.info/sct",
          "code": "11429006",
          "display": "Consultation"
        }
      ]
    }
  ],
  "subject": {
    "reference": "Patient/1"
  },
  "period": {
    "start": "2026-04-15T10:00:00-05:00",
    "end": "2026-04-15T10:20:00-05:00"
  },
  "reasonReference": [
    {
      "reference": "Condition/hypertension-123"
    }
  ]
}
```

---

## How Encounters Connect to Everything Else

The Encounter is the central hub of a clinical visit. Other resources point TO it:

```
                    ┌──────────────┐
                    │   Patient    │
                    └──────┬───────┘
                           │ subject
                    ┌──────▼───────┐
    appointment ──▶ │  Encounter   │ ◀── episodeOfCare
                    └──────┬───────┘
                           │ encounter (context)
              ┌────────────┼────────────┬────────────┐
              ▼            ▼            ▼            ▼
        Observation    Condition    Procedure   MedicationRequest
        (vitals)       (diagnosis)  (surgery)   (prescription)
```

These resources reference the encounter like this:

```json
// In an Observation
"encounter": { "reference": "Encounter/42" }

// In a Condition
"encounter": { "reference": "Encounter/42" }

// In a Procedure
"encounter": { "reference": "Encounter/42" }
```

This lets you query: "Give me everything that happened during Encounter/42."

---

## US Core Encounter Profile

US Core adds requirements on top of the base Encounter:

| Field | US Core Requirement |
|-------|-------------------|
| `status` | **Required** |
| `class` | **Required** |
| `type` | **Required** (must have at least one) |
| `subject` | **Required** (must reference a US Core Patient) |
| `identifier` | Must Support |
| `participant` | Must Support |
| `period` | Must Support |
| `reasonCode` | Must Support |
| `hospitalization.dischargeDisposition` | Must Support |
| `location` | Must Support |

Profile URL: `http://hl7.org/fhir/us/core/StructureDefinition/us-core-encounter`

---

## What Our Code Does vs. What's Possible

Our current `EncounterService.CreateAmbulatoryEncounterAsync` creates a minimal ambulatory
encounter with:

| Field | What we set | What else is possible |
|-------|------------|----------------------|
| `status` | `finished` | Any status from the lifecycle |
| `class` | `AMB` (ambulatory) | `IMP`, `EMER`, `VR`, `HH`, etc. |
| `type` | SNOMED checkup code | Any encounter type code |
| `subject` | Patient reference | Same — always needed |
| `period` | Start and end time | Same — can omit end if in-progress |
| `reasonCode` | SNOMED coded reason | Multiple reasons, or use `reasonReference` |
| `participant` | Not set | Doctors, nurses, their roles and times |
| `location` | Not set | Room, bed, ward with time periods |
| `diagnosis` | Not set | Linked conditions with roles and ranking |
| `hospitalization` | Not set | Admit source, discharge disposition |
| `identifier` | Not set | Visit number, account number |
| `priority` | Not set | Emergency, urgent, routine, elective |
| `serviceType` | Not set | Specific department or service line |

The service covers the basics for learning. In a real system, you'd populate more fields
depending on the encounter type.

---

## Common Questions

**Q: Does every Observation need an Encounter?**
No, but most clinical observations happen during an encounter. Lab results ordered during
a visit link back to that encounter. A patient-reported observation (like a home BP reading)
might not have one.

**Q: Can an Encounter have multiple diagnoses?**
Yes. The `diagnosis` array can hold multiple conditions, each with a role (admission,
discharge, billing) and a rank (primary = 1, secondary = 2, etc.).

**Q: What's the difference between `reasonCode` and `diagnosis`?**
`reasonCode` is *why the patient came in* (chief complaint). `diagnosis` is *what was found*.
A patient might come in for "chest pain" (reasonCode) and be diagnosed with "angina"
(diagnosis).

**Q: When would I use `partOf`?**
For sub-encounters within a larger encounter. Example: a hospital admission (`partOf` = null)
might contain a surgical encounter (`partOf` = the admission) and a recovery encounter
(`partOf` = the admission).

**Q: What about appointments vs. encounters?**
An Appointment is scheduled future time. When the patient arrives, an Encounter is created
and linked back to the Appointment via `encounter.appointment`. Think of it as:
Appointment = planned, Encounter = happened.

---

## Summary

- An **Encounter** is any interaction between a patient and a healthcare provider
- **`status`** and **`class`** are required — they tell you the lifecycle stage and type of visit
- **`participant`** records who was involved (doctors, nurses) with their roles
- **`diagnosis`** links conditions to the encounter with roles (admission, discharge, billing)
- **`hospitalization`** captures admit/discharge details for inpatient stays
- **`location`** tracks where the patient was during the encounter
- **`reasonCode`** is why they came; **`diagnosis`** is what was found
- Other resources (Observation, Condition, Procedure) point back to the Encounter as context
- US Core requires status, class, type, and subject at minimum

## Next Step

Proceed to **Step 4** in the learning plan — Create an Encounter for the Patient. You'll
use the `EncounterService` to create an ambulatory encounter linked to the Patient you
created in earlier lessons, then verify it with the tests in Step4.
