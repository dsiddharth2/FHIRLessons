# Lesson 8: Conditions — Diagnoses, Problems, and Health Concerns

## Overview

You've measured things about a patient (Observations). Now it's time to record what those
measurements *mean* clinically. A blood pressure of 140/90 is an Observation. Saying the
patient **has hypertension** is a Condition.

The **Condition** resource represents a clinical problem, diagnosis, or health concern.
It's the answer to: "What is wrong with this patient?" or "What are we treating?"

In any real FHIR system, Conditions are central to care — they drive treatment plans,
justify procedures, support billing claims, and populate problem lists.

---

## What Is a Condition?

A Condition is a **clinical assertion that a patient has (or had) a problem**. It captures:

```
+-----------------------------------------------+
|                  Condition                     |
|                                                |
|  WHAT is the problem?     -> code (SNOMED/ICD) |
|  WHO has it?              -> subject (Patient)  |
|  HOW certain is it?       -> verificationStatus |
|  IS it still active?      -> clinicalStatus     |
|  WHEN did it start?       -> onset[x]           |
|  WHERE was it diagnosed?  -> encounter           |
|  WHY does it matter?      -> category            |
+-----------------------------------------------+
```

### Conditions vs. Observations vs. Procedures

This is a common source of confusion:

| Resource | Represents | Example |
|----------|-----------|---------|
| **Observation** | A measurement or finding | BP reading: 140/90 mmHg |
| **Condition** | A clinical problem or diagnosis | Hypertension |
| **Procedure** | An action performed on the patient | Blood pressure monitoring |

The flow is: Observe -> Diagnose -> Treat.

- Doctor *observes* elevated blood pressure (Observation)
- Doctor *diagnoses* hypertension (Condition)
- Doctor *orders* antihypertensive medication (MedicationRequest, driven by the Condition)
- Doctor *performs* BP monitoring (Procedure, justified by the Condition)

---

## The Full Condition Resource -- Field by Field

### Identity & Classification

| Field | Type | Required? | What it is |
|-------|------|-----------|-----------|
| `id` | string | Server-assigned | Technical ID |
| `meta` | Meta | Auto | Version, timestamps, profile |
| `identifier` | Identifier[] | No | Business identifiers |
| `clinicalStatus` | CodeableConcept | **Conditional** | Is the problem currently active? |
| `verificationStatus` | CodeableConcept | **Conditional** | How certain is the diagnosis? |
| `category` | CodeableConcept[] | No* | encounter-diagnosis vs. problem-list-item |
| `severity` | CodeableConcept | No | How severe (mild, moderate, severe) |
| `code` | CodeableConcept | No* | The actual diagnosis (SNOMED + ICD-10) |

*US Core requires `category` and `code`.

### Clinical Status

The `clinicalStatus` tracks the lifecycle of the problem:

| Code | Meaning | When to use |
|------|---------|-------------|
| `active` | Currently a problem | Patient has hypertension right now |
| `recurrence` | Problem came back | Cancer returned after remission |
| `relapse` | Problem worsened after improvement | Depression relapse |
| `inactive` | No longer clinically relevant | Resolved seasonal allergy (off-season) |
| `remission` | Under control but not gone | Cancer in remission |
| `resolved` | Problem is over | Broken arm healed |

System: `http://terminology.hl7.org/CodeSystem/condition-clinical`

**Important:** `clinicalStatus` is required UNLESS `verificationStatus` is `entered-in-error`.
If you omit `clinicalStatus` on an active condition, the server may reject it.

### Verification Status

How confident is the clinician in the diagnosis?

| Code | Meaning | When to use |
|------|---------|-------------|
| `unconfirmed` | Suspected but not proven | "Might be diabetes" |
| `provisional` | Working diagnosis, needs more info | "Likely pneumonia, pending X-ray" |
| `differential` | One of several possibilities | "Could be A, B, or C" |
| `confirmed` | Diagnosis is established | "Confirmed Type 2 diabetes" |
| `refuted` | Ruled out | "Not cancer -- it was benign" |
| `entered-in-error` | Should never have been recorded | Wrong patient, duplicate entry |

System: `http://terminology.hl7.org/CodeSystem/condition-ver-status`

### Category -- Why This Matters

| Code | Display | Meaning |
|------|---------|---------|
| `encounter-diagnosis` | Encounter Diagnosis | Diagnosed during a specific visit |
| `problem-list-item` | Problem List Item | Ongoing problem on the patient's chart |

System: `http://terminology.hl7.org/CodeSystem/condition-category`

This distinction is clinically important:

- **Encounter diagnosis**: "During today's visit, we found the patient has X." Often used
  for billing -- the encounter diagnosis justifies the services rendered.
- **Problem list item**: "This patient has X as an ongoing concern." The problem list is a
  living document that follows the patient across visits. A single condition can be BOTH --
  diagnosed at an encounter and added to the problem list.

### The Diagnosis Code

The `code` field identifies what the problem actually is. In practice, you'll almost always
include **two coding systems**:

```json
"code": {
  "coding": [
    {
      "system": "http://snomed.info/sct",
      "code": "73211009",
      "display": "Diabetes mellitus"
    },
    {
      "system": "http://hl7.org/fhir/sid/icd-10-cm",
      "code": "E11.9",
      "display": "Type 2 diabetes mellitus without complications"
    }
  ]
}
```

**Why two codes?**
- **SNOMED CT** is the clinical standard -- rich, granular, designed for clinical reasoning
- **ICD-10-CM** is the billing standard -- required for insurance claims in the US

They serve different audiences. Clinicians think in SNOMED; billing systems think in ICD-10.
Providing both ensures the Condition works for clinical care AND revenue cycle.

### Timing -- When Did It Start?

| Field | Type | What it is |
|-------|------|-----------|
| `onset[x]` | dateTime, Age, Period, Range, string | When the problem started |
| `abatement[x]` | dateTime, Age, Period, Range, string | When it ended or went into remission |
| `recordedDate` | dateTime | When it was entered in the record |

The `[x]` means onset can be expressed different ways:

| Field | Type | Example |
|-------|------|---------|
| `onsetDateTime` | dateTime | "2026-01-15" |
| `onsetAge` | Age | "At age 45" |
| `onsetPeriod` | Period | "Between January and March 2026" |
| `onsetRange` | Range | "Between age 40 and 50" |
| `onsetString` | string | "In childhood" |

For most conditions, `onsetDateTime` is what you'll use. But for historical conditions
("patient had chickenpox as a child"), `onsetString` or `onsetAge` may be all you have.

`abatement[x]` follows the same pattern and indicates when the condition ended. Only set
this when `clinicalStatus` is `inactive`, `remission`, or `resolved`.

### References -- Who and Where

| Field | Type | Required? | What it is |
|-------|------|-----------|-----------|
| `subject` | Reference(Patient) | **YES** | Who has this condition |
| `encounter` | Reference(Encounter) | No | The visit where it was diagnosed |
| `recorder` | Reference | No | Who entered it in the record |
| `asserter` | Reference | No | Who clinically asserted it (may differ from recorder) |

**`recorder` vs. `asserter`:** A nurse (recorder) might enter a diagnosis that the doctor
(asserter) made. This distinction matters for clinical accountability.

### Supporting Information

| Field | Type | What it is |
|-------|------|-----------|
| `evidence` | BackboneElement[] | What evidence supports this diagnosis |
| `evidence.code` | CodeableConcept[] | Coded evidence (symptoms, findings) |
| `evidence.detail` | Reference[] | Links to supporting resources (e.g., Observation) |
| `note` | Annotation[] | Free-text clinical notes |
| `bodySite` | CodeableConcept[] | Where on the body (e.g., left knee) |
| `stage` | BackboneElement | Cancer staging, severity grading |

The `evidence` field is particularly powerful -- it links the Condition back to the
Observations that support it:

```json
"evidence": [
  {
    "code": [
      {
        "coding": [
          {
            "system": "http://snomed.info/sct",
            "code": "271649006",
            "display": "Systolic blood pressure"
          }
        ]
      }
    ],
    "detail": [
      { "reference": "Observation/42" }
    ]
  }
]
```

This closes the clinical loop: the Observation (high BP reading) is evidence for the
Condition (hypertension diagnosis).

---

## Code Systems for Conditions

### SNOMED CT -- Clinical Terminology

System: `http://snomed.info/sct`

SNOMED CT is the primary clinical coding system for conditions. It has over 350,000 concepts
organized in a hierarchy.

#### Common Condition Codes

| SNOMED Code | Display | ICD-10 Equivalent |
|------------|---------|-------------------|
| `38341003` | Hypertensive disorder | I10 |
| `73211009` | Diabetes mellitus | E11.9 |
| `195967001` | Asthma | J45.909 |
| `44054006` | Type 2 diabetes mellitus | E11.9 |
| `13645005` | Chronic obstructive lung disease | J44.9 |
| `49436004` | Atrial fibrillation | I48.91 |
| `84114007` | Heart failure | I50.9 |
| `40055000` | Chronic kidney disease | N18.9 |
| `35489007` | Depressive disorder | F32.9 |
| `230690007` | Cerebrovascular accident (stroke) | I63.9 |

### ICD-10-CM -- Billing Codes

System: `http://hl7.org/fhir/sid/icd-10-cm`

ICD-10-CM (International Classification of Diseases, 10th Revision, Clinical Modification)
is required for billing in the US. Codes follow a structured format:

```
E11.65
 |  ||
 |  |+-- Extension (specific complication)
 |  +--- Category detail
 +------ Chapter letter (E = Endocrine/metabolic)
```

| ICD-10 Code | Display |
|------------|---------|
| `I10` | Essential (primary) hypertension |
| `E11.9` | Type 2 diabetes without complications |
| `E11.65` | Type 2 diabetes with hyperglycemia |
| `J45.909` | Unspecified asthma, uncomplicated |
| `I48.91` | Unspecified atrial fibrillation |
| `I50.9` | Heart failure, unspecified |
| `F32.9` | Major depressive disorder, single episode, unspecified |
| `M54.5` | Low back pain |
| `J06.9` | Acute upper respiratory infection, unspecified |

---

## Real-World Examples

### Example 1: Active Encounter Diagnosis -- Hypertension

This is what our `ConditionService.CreateDiagnosisAsync` creates:

```json
{
  "resourceType": "Condition",
  "clinicalStatus": {
    "coding": [
      {
        "system": "http://terminology.hl7.org/CodeSystem/condition-clinical",
        "code": "active",
        "display": "Active"
      }
    ]
  },
  "verificationStatus": {
    "coding": [
      {
        "system": "http://terminology.hl7.org/CodeSystem/condition-ver-status",
        "code": "confirmed",
        "display": "Confirmed"
      }
    ]
  },
  "category": [
    {
      "coding": [
        {
          "system": "http://terminology.hl7.org/CodeSystem/condition-category",
          "code": "encounter-diagnosis",
          "display": "Encounter Diagnosis"
        }
      ]
    }
  ],
  "code": {
    "coding": [
      {
        "system": "http://snomed.info/sct",
        "code": "38341003",
        "display": "Hypertensive disorder"
      },
      {
        "system": "http://hl7.org/fhir/sid/icd-10-cm",
        "code": "I10",
        "display": "Essential (primary) hypertension"
      }
    ]
  },
  "subject": { "reference": "Patient/1" },
  "encounter": { "reference": "Encounter/2" },
  "onsetDateTime": "2026-04-10"
}
```

### Example 2: Resolved Condition with Abatement

A condition that is no longer active:

```json
{
  "resourceType": "Condition",
  "clinicalStatus": {
    "coding": [
      {
        "system": "http://terminology.hl7.org/CodeSystem/condition-clinical",
        "code": "resolved",
        "display": "Resolved"
      }
    ]
  },
  "verificationStatus": {
    "coding": [
      {
        "system": "http://terminology.hl7.org/CodeSystem/condition-ver-status",
        "code": "confirmed",
        "display": "Confirmed"
      }
    ]
  },
  "category": [
    {
      "coding": [
        {
          "system": "http://terminology.hl7.org/CodeSystem/condition-category",
          "code": "problem-list-item",
          "display": "Problem List Item"
        }
      ]
    }
  ],
  "code": {
    "coding": [
      {
        "system": "http://snomed.info/sct",
        "code": "10509002",
        "display": "Acute bronchitis"
      },
      {
        "system": "http://hl7.org/fhir/sid/icd-10-cm",
        "code": "J20.9",
        "display": "Acute bronchitis, unspecified"
      }
    ]
  },
  "subject": { "reference": "Patient/1" },
  "onsetDateTime": "2026-02-01",
  "abatementDateTime": "2026-02-15",
  "recordedDate": "2026-02-01"
}
```

Notice: `abatementDateTime` is set because the condition is `resolved`. This tells you
the patient had bronchitis for about two weeks.

### Example 3: Problem List Item with Evidence

A chronic condition linked to supporting observations:

```json
{
  "resourceType": "Condition",
  "clinicalStatus": {
    "coding": [
      {
        "system": "http://terminology.hl7.org/CodeSystem/condition-clinical",
        "code": "active",
        "display": "Active"
      }
    ]
  },
  "verificationStatus": {
    "coding": [
      {
        "system": "http://terminology.hl7.org/CodeSystem/condition-ver-status",
        "code": "confirmed",
        "display": "Confirmed"
      }
    ]
  },
  "category": [
    {
      "coding": [
        {
          "system": "http://terminology.hl7.org/CodeSystem/condition-category",
          "code": "problem-list-item",
          "display": "Problem List Item"
        }
      ]
    }
  ],
  "severity": {
    "coding": [
      {
        "system": "http://snomed.info/sct",
        "code": "24484000",
        "display": "Severe"
      }
    ]
  },
  "code": {
    "coding": [
      {
        "system": "http://snomed.info/sct",
        "code": "44054006",
        "display": "Type 2 diabetes mellitus"
      },
      {
        "system": "http://hl7.org/fhir/sid/icd-10-cm",
        "code": "E11.65",
        "display": "Type 2 diabetes mellitus with hyperglycemia"
      }
    ]
  },
  "subject": { "reference": "Patient/1" },
  "onsetDateTime": "2020-06-15",
  "recordedDate": "2020-06-15",
  "evidence": [
    {
      "code": [
        {
          "coding": [
            {
              "system": "http://snomed.info/sct",
              "code": "166922008",
              "display": "Hemoglobin A1c level"
            }
          ]
        }
      ],
      "detail": [
        { "reference": "Observation/55" }
      ]
    }
  ],
  "note": [
    {
      "text": "Patient has struggled with glucose management. A1c consistently above 9%."
    }
  ]
}
```

### Example 4: Provisional Diagnosis

A working diagnosis that hasn't been confirmed yet:

```json
{
  "resourceType": "Condition",
  "clinicalStatus": {
    "coding": [
      {
        "system": "http://terminology.hl7.org/CodeSystem/condition-clinical",
        "code": "active",
        "display": "Active"
      }
    ]
  },
  "verificationStatus": {
    "coding": [
      {
        "system": "http://terminology.hl7.org/CodeSystem/condition-ver-status",
        "code": "provisional",
        "display": "Provisional"
      }
    ]
  },
  "category": [
    {
      "coding": [
        {
          "system": "http://terminology.hl7.org/CodeSystem/condition-category",
          "code": "encounter-diagnosis",
          "display": "Encounter Diagnosis"
        }
      ]
    }
  ],
  "code": {
    "coding": [
      {
        "system": "http://snomed.info/sct",
        "code": "233604007",
        "display": "Pneumonia"
      },
      {
        "system": "http://hl7.org/fhir/sid/icd-10-cm",
        "code": "J18.9",
        "display": "Pneumonia, unspecified organism"
      }
    ]
  },
  "subject": { "reference": "Patient/1" },
  "encounter": { "reference": "Encounter/5" },
  "onsetDateTime": "2026-04-17",
  "note": [
    {
      "text": "Pending chest X-ray to confirm. Patient presenting with cough, fever, and dyspnea."
    }
  ]
}
```

---

## How Conditions Connect to the Clinical Record

```
Patient
  |
  +-- Problem List
  |     +-- Condition: Type 2 Diabetes (active, confirmed, problem-list-item)
  |     +-- Condition: Hypertension (active, confirmed, problem-list-item)
  |     +-- Condition: Acute Bronchitis (resolved, problem-list-item)
  |
  +-- Encounter (visit on Apr 10)
  |     +-- Observation (BP: 140/90)
  |     +-- Condition: Hypertension (encounter-diagnosis)
  |     |     +-- evidence -> Observation (BP: 140/90)
  |     +-- Procedure: BP Monitoring
  |           +-- reasonReference -> Condition: Hypertension
  |
  +-- Encounter (follow-up on Apr 18)
        +-- Observation (BP: 125/82)
        +-- Observation (A1c: 7.2%)
        +-- Condition update: Hypertension (still active)
```

Key relationships:
- **Condition -> Patient** (subject): who has this problem
- **Condition -> Encounter** (encounter): when it was diagnosed
- **Condition -> Observation** (evidence.detail): what supports the diagnosis
- **Procedure -> Condition** (reasonReference): what justified the procedure
- **MedicationRequest -> Condition** (reasonReference): what the medication treats

### Searching for Conditions

```
GET /Condition?subject=Patient/1                         <- all conditions for a patient
GET /Condition?subject=Patient/1&clinical-status=active  <- only active problems
GET /Condition?encounter=Encounter/2                     <- diagnoses from a specific visit
GET /Condition?code=38341003                             <- all hypertension diagnoses
GET /Condition?category=problem-list-item                <- the problem list
GET /Condition?category=encounter-diagnosis              <- encounter-specific diagnoses
GET /Condition?subject=Patient/1&onset-date=ge2026-01-01 <- conditions since Jan 2026
```

---

## US Core Condition Profiles

US Core defines two Condition profiles:

### US Core Condition Encounter Diagnosis

For diagnoses made during a specific encounter.

| Field | Required? | Notes |
|-------|-----------|-------|
| `clinicalStatus` | Conditional | Required unless entered-in-error |
| `verificationStatus` | Conditional | Required unless clinicalStatus is present |
| `category` | YES | Must include `encounter-diagnosis` |
| `code` | YES | Must be from US Core Condition Codes (extensible) |
| `subject` | YES | Reference to US Core Patient |

### US Core Condition Problems and Health Concerns

For the patient's problem list and ongoing health concerns.

| Field | Required? | Notes |
|-------|-----------|-------|
| `clinicalStatus` | Conditional | Required unless entered-in-error |
| `verificationStatus` | Conditional | Required unless clinicalStatus is present |
| `category` | YES | Must include `problem-list-item` or `health-concern` |
| `code` | YES | Must be from US Core Condition Codes (extensible) |
| `subject` | YES | Reference to US Core Patient |

---

## What Our Code Does vs. What's Possible

Our `ConditionService.CreateDiagnosisAsync` creates a condition with:

| Field | What we set | What else is possible |
|-------|------------|----------------------|
| `clinicalStatus` | `active` | resolved, inactive, remission, recurrence |
| `verificationStatus` | `confirmed` | provisional, differential, unconfirmed, refuted |
| `category` | `encounter-diagnosis` | problem-list-item, health-concern |
| `code` | SNOMED + ICD-10 dual coding | Any combination of coding systems |
| `subject` | Patient reference | Same |
| `encounter` | Encounter reference | Can be omitted for problem-list items |
| `onset` | DateTime | Age, Period, Range, or string |
| `severity` | Not set | mild, moderate, severe (SNOMED codes) |
| `abatement` | Not set | When the condition ended |
| `evidence` | Not set | Link to supporting Observations |
| `bodySite` | Not set | Where on the body |
| `stage` | Not set | Cancer staging, grading |
| `note` | Not set | Free-text clinical notes |
| `recorder` | Not set | Who entered the record |
| `asserter` | Not set | Who made the clinical assertion |

---

## Common Questions

**Q: Should the same condition be both an encounter-diagnosis and a problem-list-item?**
Yes, this is common. Hypertension might first appear as an encounter-diagnosis when it's
found during a visit, and then get added to the problem list as a problem-list-item for
ongoing tracking. In FHIR, you can include both categories on the same Condition, or create
two separate Condition resources -- implementations vary.

**Q: When should I set `abatement`?**
Only when `clinicalStatus` is `inactive`, `remission`, or `resolved`. Never set it on an
`active` condition. If you don't know exactly when the condition ended, you can use
`abatementString` (e.g., "sometime in 2025").

**Q: How do I handle a misdiagnosis?**
Set `verificationStatus` to `entered-in-error`. This signals that the condition should be
ignored -- it was never real. Don't delete it (FHIR prefers marking things as entered-in-error
over deleting, so the audit trail is preserved).

**Q: What's the difference between `severity` and `stage`?**
`severity` is a general classification (mild/moderate/severe) that applies to any condition.
`stage` is specifically for conditions that have formal staging systems -- primarily cancer
(e.g., Stage IIA breast cancer). Most conditions use `severity` or neither.

**Q: How do I represent a chronic condition that fluctuates?**
Keep `clinicalStatus` as `active`. Use `note` to document fluctuations, or create dated
Observations that track the condition over time (e.g., periodic pain scores). You can also
use `recurrence` or `relapse` statuses for conditions that resolve and return.

**Q: Is `recordedDate` the same as `onsetDateTime`?**
No. `onsetDateTime` is when the condition *started* clinically. `recordedDate` is when
someone *documented* it in the system. A patient might have had hypertension for years
(`onset: 2020-01-01`) before it was first recorded in this system (`recordedDate: 2026-04-10`).

---

## Summary

- **Conditions** represent diagnoses, problems, and health concerns
- **`clinicalStatus`** tracks the lifecycle: active -> remission -> resolved
- **`verificationStatus`** indicates certainty: provisional -> confirmed (or refuted)
- **`category`** distinguishes encounter-diagnosis from problem-list-item
- **`code`** should include BOTH SNOMED CT (clinical) and ICD-10-CM (billing)
- **`onset[x]`** and **`abatement[x]`** bracket when the condition was active
- **`evidence`** links back to supporting Observations, closing the clinical reasoning loop
- Conditions are referenced BY Procedures (reasonReference) and MedicationRequests

## Next Step

Proceed to **Step 6** in the learning plan -- Create a Condition (Diagnosis) based on the
Encounter. You'll use the `ConditionService` to create a hypertension diagnosis linked to
a Patient and Encounter, then verify the result with the Step6 tests.
