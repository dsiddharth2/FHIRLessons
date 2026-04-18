# Lesson 7: Observations — The Workhorse of Clinical Data

## Overview

You've created Patients and learned about Encounters. Now it's time for the resource you'll
encounter most often in any FHIR system: the **Observation**.

Observations are everywhere. Blood pressure, heart rate, lab results, smoking status, body
weight, pregnancy status, social history — if it was measured, assessed, or reported about a
patient, it's probably an Observation.

This lesson explains what an Observation is, all the ways data can be stored in one, the
coding systems that make them interoperable, and the patterns you'll see in real systems.

---

## What Is an Observation?

An Observation is a **measurement or assertion about a patient**. It answers the question:
"What was observed, when, and what was the result?"

Every Observation has three core parts:

```
┌──────────────────────────────────────────────┐
│                 Observation                   │
│                                               │
│  WHAT was measured?     → code (LOINC)        │
│  WHO was it about?      → subject (Patient)   │
│  WHAT was the result?   → value[x]            │
│  WHEN was it taken?     → effective[x]        │
│  WHERE did it happen?   → encounter           │
│  HOW reliable is it?    → status              │
└──────────────────────────────────────────────┘
```

### Types of things stored as Observations

| Category | Examples |
|----------|---------|
| **Vital signs** | Blood pressure, heart rate, temperature, respiratory rate, O2 saturation, height, weight, BMI |
| **Laboratory** | Blood glucose, cholesterol panel, CBC, hemoglobin A1c, urinalysis |
| **Social history** | Smoking status, alcohol use, drug use, exercise habits |
| **Imaging** | Bone density score, tumor size measurement |
| **Survey / Assessment** | Pain score, PHQ-9 depression score, fall risk assessment |
| **Activity** | Step count, sleep duration (from devices) |
| **Exam findings** | Heart murmur detected, lung sounds clear |

If you're unsure whether something is an Observation vs. a Condition or Procedure:
- **Observation** = a measurement or finding (what was seen/measured)
- **Condition** = a clinical problem that persists (diagnosis, ongoing illness)
- **Procedure** = an action performed on the patient (surgery, injection)

A doctor might *observe* high blood pressure (Observation), *diagnose* hypertension
(Condition), and *perform* a catheterization (Procedure).

---

## The Full Observation Resource — Field by Field

### Identity & Status

| Field | Type | Required? | What it is |
|-------|------|-----------|-----------|
| `id` | string | Server-assigned | Technical ID |
| `meta` | Meta | Auto | Version, timestamps, profile |
| `identifier` | Identifier[] | No | Business identifiers (e.g., lab accession number) |
| `status` | code | **YES** | Reliability of the result |
| `basedOn` | Reference[] | No | The order that requested this observation (e.g., ServiceRequest) |

**Observation statuses:**

| Status | Meaning | When to use |
|--------|---------|-------------|
| `registered` | Observation exists but has no result yet | Lab ordered, sample collected, waiting for result |
| `preliminary` | Early/incomplete result | Partial lab result, initial reading |
| `final` | Complete and verified | Normal completed observation |
| `amended` | Changed after being final | Corrected lab value |
| `corrected` | Same as amended, but error was identified | Lab re-ran the test |
| `cancelled` | Observation was not completed | Sample was unusable |
| `entered-in-error` | Should never have existed | Wrong patient, duplicate |
| `unknown` | Status not known | Legacy data import |

In practice, most observations you create will be `final`.

### What Was Measured

| Field | Type | Required? | What it is |
|-------|------|-----------|-----------|
| `category` | CodeableConcept[] | No* | Broad classification (vital-signs, laboratory, etc.) |
| `code` | CodeableConcept | **YES** | What specifically was measured (LOINC code) |

*US Core requires `category` for vital signs and lab results.

**Observation categories:**

| Code | Display | Use for |
|------|---------|---------|
| `vital-signs` | Vital Signs | BP, HR, temp, weight, height, BMI, O2 sat |
| `laboratory` | Laboratory | All lab test results |
| `social-history` | Social History | Smoking, alcohol, exercise |
| `survey` | Survey | Questionnaire scores, assessments |
| `imaging` | Imaging | Radiology measurements |
| `procedure` | Procedure | Observations made during a procedure |
| `exam` | Exam | Physical exam findings |
| `therapy` | Therapy | Observations during treatment |
| `activity` | Activity | Physical activity, device data |

Category system: `http://terminology.hl7.org/CodeSystem/observation-category`

### The Result — value[x]

This is the most important part. The `[x]` means the value can be one of several types.
FHIR uses this polymorphic pattern: the field name changes based on the type.

| Field name | Type | Use for | Example |
|-----------|------|---------|---------|
| `valueQuantity` | Quantity | Numeric measurements | BP: 120 mmHg, Temp: 98.6 °F |
| `valueCodeableConcept` | CodeableConcept | Coded values | Smoking status: "Current smoker" |
| `valueString` | string | Free text | "Heart sounds normal" |
| `valueBoolean` | boolean | Yes/no | Pregnant: true |
| `valueInteger` | integer | Whole numbers | Apgar score: 9 |
| `valueRange` | Range | A range | Reference range: 70-100 |
| `valueRatio` | Ratio | Ratios | Albumin/Creatinine ratio: 30 mg/g |
| `valueDateTime` | dateTime | Timestamps | Last menstrual period: 2026-01-15 |
| `valuePeriod` | Period | Time ranges | Fasting period: 8pm to 8am |
| `valueTime` | time | Time of day | Medication taken at: 08:00 |
| `valueSampledData` | SampledData | Waveform data | EKG trace |

**You can only use ONE value type per Observation.** If the observation has no value (e.g.,
it was cancelled), you should include `dataAbsentReason` instead.

#### valueQuantity — the most common

```json
"valueQuantity": {
  "value": 98.6,
  "unit": "°F",
  "system": "http://unitsofmeasure.org",
  "code": "degF"
}
```

The `system` is always UCUM (`http://unitsofmeasure.org`) for standardized units.

Common UCUM codes:

| Unit | UCUM code | Used for |
|------|-----------|---------|
| mmHg | `mm[Hg]` | Blood pressure |
| °F | `[degF]` | Temperature (Fahrenheit) |
| °C | `Cel` | Temperature (Celsius) |
| kg | `kg` | Weight |
| cm | `cm` | Height |
| kg/m² | `kg/m2` | BMI |
| % | `%` | O2 saturation, percentages |
| /min | `/min` | Heart rate, respiratory rate |
| mg/dL | `mg/dL` | Blood glucose, cholesterol |
| mmol/L | `mmol/L` | Blood glucose (international) |
| g/dL | `g/dL` | Hemoglobin |

#### valueCodeableConcept — for coded results

Used when the result is a category, not a number:

```json
"valueCodeableConcept": {
  "coding": [
    {
      "system": "http://snomed.info/sct",
      "code": "449868002",
      "display": "Current every day smoker"
    }
  ]
}
```

### Who, When, Where

| Field | Type | Required? | What it is |
|-------|------|-----------|-----------|
| `subject` | Reference(Patient) | No* | Who this observation is about |
| `encounter` | Reference(Encounter) | No | The visit during which it was taken |
| `effective[x]` | dateTime, Period, Timing, instant | No | When the observation was made |
| `issued` | instant | No | When the result was made available |
| `performer` | Reference[] | No | Who made the observation (Practitioner, Organization) |

*US Core requires `subject`.

**effective[x]** — when the observation was clinically relevant:

| Field | Type | Use for |
|-------|------|---------|
| `effectiveDateTime` | dateTime | Single point in time (most common) |
| `effectivePeriod` | Period | Over a time range (e.g., 24-hour urine collection) |
| `effectiveInstant` | instant | Precise timestamp (device readings) |
| `effectiveTiming` | Timing | Recurring pattern |

**Important:** `effectiveDateTime` is when the measurement was *taken*. `issued` is when the
result was *available*. For a BP taken at the bedside, they're the same. For a lab test,
`effective` is when the sample was collected and `issued` is when the lab reported the result.

### Components — Multi-Part Observations

Some observations have multiple related values that belong together. Instead of creating
separate Observation resources, you use **components**.

| Field | Type | What it is |
|-------|------|-----------|
| `component` | BackboneElement[] | Sub-observations within this observation |
| `component.code` | CodeableConcept | What this component measures |
| `component.value[x]` | (same types as value[x]) | The component's result |
| `component.referenceRange` | (same as referenceRange) | Normal range for this component |

**When to use components vs. separate Observations:**

| Scenario | Use |
|----------|-----|
| Blood pressure (systolic + diastolic) | Components — they're always measured together |
| Cholesterol panel (total, LDL, HDL, triglycerides) | Components — one panel, multiple results |
| Heart rate and temperature | Separate Observations — measured independently |
| Height and weight | Separate Observations — different measurement types |

The rule: if the sub-values don't make sense independently, use components. If they can
stand alone, use separate Observations.

### Reference Ranges & Interpretation

| Field | Type | What it is |
|-------|------|-----------|
| `referenceRange` | BackboneElement[] | Normal range for the result |
| `referenceRange.low` | Quantity | Lower bound of normal |
| `referenceRange.high` | Quantity | Upper bound of normal |
| `referenceRange.type` | CodeableConcept | Type of range (normal, recommended, therapeutic) |
| `referenceRange.text` | string | Human-readable range description |
| `interpretation` | CodeableConcept[] | Whether the result is normal, high, low, critical |

**Interpretation codes** (system: `http://terminology.hl7.org/CodeSystem/v3-ObservationInterpretation`):

| Code | Display | Meaning |
|------|---------|---------|
| `N` | Normal | Within normal range |
| `H` | High | Above normal |
| `L` | Low | Below normal |
| `HH` | Critical high | Dangerously above normal |
| `LL` | Critical low | Dangerously below normal |
| `A` | Abnormal | Outside normal (direction unspecified) |

```json
"interpretation": [
  {
    "coding": [
      {
        "system": "http://terminology.hl7.org/CodeSystem/v3-ObservationInterpretation",
        "code": "H",
        "display": "High"
      }
    ]
  }
],
"referenceRange": [
  {
    "low": { "value": 90, "unit": "mmHg", "system": "http://unitsofmeasure.org", "code": "mm[Hg]" },
    "high": { "value": 120, "unit": "mmHg", "system": "http://unitsofmeasure.org", "code": "mm[Hg]" },
    "text": "Normal systolic blood pressure"
  }
]
```

### Relationships

| Field | Type | What it is |
|-------|------|-----------|
| `hasMember` | Reference(Observation)[] | Groups related observations (e.g., a panel pointing to individual results) |
| `derivedFrom` | Reference[] | Source data this was derived from (e.g., calculated from other observations) |
| `focus` | Reference[] | What the observation is about if not the subject (rare) |
| `partOf` | Reference[] | Parent event (e.g., Procedure this observation was part of) |

**hasMember** is used for grouping. For example, a "Metabolic Panel" observation might have
`hasMember` references pointing to individual glucose, sodium, potassium observations.

---

## LOINC — The Coding System for Observations

LOINC (Logical Observation Identifiers Names and Codes) is THE standard for coding
observations. Nearly every Observation you create will have a LOINC code.

System URL: `http://loinc.org`

### Common LOINC Codes

#### Vital Signs

| LOINC Code | Display | Value type |
|-----------|---------|-----------|
| `85354-9` | Blood pressure panel | Components (systolic + diastolic) |
| `8480-6` | Systolic blood pressure | Quantity (mmHg) |
| `8462-4` | Diastolic blood pressure | Quantity (mmHg) |
| `8867-4` | Heart rate | Quantity (/min) |
| `8310-5` | Body temperature | Quantity (°F or °C) |
| `9279-1` | Respiratory rate | Quantity (/min) |
| `2708-6` | Oxygen saturation | Quantity (%) |
| `8302-2` | Body height | Quantity (cm or in) |
| `29463-7` | Body weight | Quantity (kg or lb) |
| `39156-5` | Body mass index (BMI) | Quantity (kg/m²) |

#### Laboratory

| LOINC Code | Display | Value type |
|-----------|---------|-----------|
| `2339-0` | Glucose [Mass/volume] in Blood | Quantity (mg/dL) |
| `4548-4` | Hemoglobin A1c | Quantity (%) |
| `2093-3` | Cholesterol [Mass/volume] in Serum | Quantity (mg/dL) |
| `18262-6` | Cholesterol in LDL | Quantity (mg/dL) |
| `2085-9` | Cholesterol in HDL | Quantity (mg/dL) |
| `2571-8` | Triglycerides | Quantity (mg/dL) |
| `718-7` | Hemoglobin [Mass/volume] in Blood | Quantity (g/dL) |
| `6690-2` | White blood cell count | Quantity (10*3/uL) |
| `789-8` | Red blood cell count | Quantity (10*6/uL) |
| `777-3` | Platelet count | Quantity (10*3/uL) |

#### Social History

| LOINC Code | Display | Value type |
|-----------|---------|-----------|
| `72166-2` | Tobacco smoking status | CodeableConcept |
| `11331-6` | History of alcohol use | CodeableConcept |

### How to find LOINC codes

In practice, you look up codes at [https://loinc.org/search/](https://loinc.org/search/).
Each code has a fully specified name following the pattern:

```
Component : Property : Time : System : Scale : Method
```

For example, `2339-0` (Blood glucose):
- **Component:** Glucose
- **Property:** Mass concentration (MCnc)
- **Time:** Point in time (Pt)
- **System:** Blood (Bld)
- **Scale:** Quantitative (Qn)

This structure ensures two different labs measuring the same thing use the same code.

---

## Real-World Examples

### Example 1: Simple Vital Sign — Body Temperature

A single measurement with one value:

```json
{
  "resourceType": "Observation",
  "status": "final",
  "category": [
    {
      "coding": [
        {
          "system": "http://terminology.hl7.org/CodeSystem/observation-category",
          "code": "vital-signs",
          "display": "Vital Signs"
        }
      ]
    }
  ],
  "code": {
    "coding": [
      {
        "system": "http://loinc.org",
        "code": "8310-5",
        "display": "Body temperature"
      }
    ]
  },
  "subject": { "reference": "Patient/1" },
  "encounter": { "reference": "Encounter/2" },
  "effectiveDateTime": "2026-04-10T09:10:00Z",
  "valueQuantity": {
    "value": 98.6,
    "unit": "°F",
    "system": "http://unitsofmeasure.org",
    "code": "[degF]"
  }
}
```

Notice: no `component` — simple observations use `valueQuantity` directly.

### Example 2: Blood Pressure — Component Observation

Two values that belong together (this is what our code does):

```json
{
  "resourceType": "Observation",
  "status": "final",
  "category": [
    {
      "coding": [
        {
          "system": "http://terminology.hl7.org/CodeSystem/observation-category",
          "code": "vital-signs"
        }
      ]
    }
  ],
  "code": {
    "coding": [
      {
        "system": "http://loinc.org",
        "code": "85354-9",
        "display": "Blood pressure panel with all children optional"
      }
    ]
  },
  "subject": { "reference": "Patient/1" },
  "encounter": { "reference": "Encounter/2" },
  "effectiveDateTime": "2026-04-10T09:15:00Z",
  "component": [
    {
      "code": {
        "coding": [
          {
            "system": "http://loinc.org",
            "code": "8480-6",
            "display": "Systolic blood pressure"
          }
        ]
      },
      "valueQuantity": {
        "value": 140,
        "unit": "mmHg",
        "system": "http://unitsofmeasure.org",
        "code": "mm[Hg]"
      },
      "interpretation": [
        {
          "coding": [
            {
              "system": "http://terminology.hl7.org/CodeSystem/v3-ObservationInterpretation",
              "code": "H",
              "display": "High"
            }
          ]
        }
      ],
      "referenceRange": [
        {
          "high": { "value": 120, "unit": "mmHg", "system": "http://unitsofmeasure.org", "code": "mm[Hg]" },
          "text": "Normal: < 120 mmHg"
        }
      ]
    },
    {
      "code": {
        "coding": [
          {
            "system": "http://loinc.org",
            "code": "8462-4",
            "display": "Diastolic blood pressure"
          }
        ]
      },
      "valueQuantity": {
        "value": 90,
        "unit": "mmHg",
        "system": "http://unitsofmeasure.org",
        "code": "mm[Hg]"
      },
      "interpretation": [
        {
          "coding": [
            {
              "system": "http://terminology.hl7.org/CodeSystem/v3-ObservationInterpretation",
              "code": "H",
              "display": "High"
            }
          ]
        }
      ],
      "referenceRange": [
        {
          "high": { "value": 80, "unit": "mmHg", "system": "http://unitsofmeasure.org", "code": "mm[Hg]" },
          "text": "Normal: < 80 mmHg"
        }
      ]
    }
  ]
}
```

This is a richer version of what our `ObservationService.CreateBloodPressureAsync` produces —
with interpretation and reference ranges added.

### Example 3: Lab Result — Blood Glucose

```json
{
  "resourceType": "Observation",
  "status": "final",
  "category": [
    {
      "coding": [
        {
          "system": "http://terminology.hl7.org/CodeSystem/observation-category",
          "code": "laboratory",
          "display": "Laboratory"
        }
      ]
    }
  ],
  "code": {
    "coding": [
      {
        "system": "http://loinc.org",
        "code": "2339-0",
        "display": "Glucose [Mass/volume] in Blood"
      }
    ]
  },
  "subject": { "reference": "Patient/1" },
  "encounter": { "reference": "Encounter/2" },
  "effectiveDateTime": "2026-04-10T08:30:00Z",
  "issued": "2026-04-10T11:45:00Z",
  "valueQuantity": {
    "value": 95,
    "unit": "mg/dL",
    "system": "http://unitsofmeasure.org",
    "code": "mg/dL"
  },
  "interpretation": [
    {
      "coding": [
        {
          "system": "http://terminology.hl7.org/CodeSystem/v3-ObservationInterpretation",
          "code": "N",
          "display": "Normal"
        }
      ]
    }
  ],
  "referenceRange": [
    {
      "low": { "value": 70, "unit": "mg/dL", "system": "http://unitsofmeasure.org", "code": "mg/dL" },
      "high": { "value": 100, "unit": "mg/dL", "system": "http://unitsofmeasure.org", "code": "mg/dL" },
      "text": "Fasting normal: 70-100 mg/dL"
    }
  ]
}
```

Notice: `effectiveDateTime` (when blood was drawn) is different from `issued` (when the lab
reported the result). This distinction matters in lab workflows.

### Example 4: Social History — Smoking Status

A coded value instead of a quantity:

```json
{
  "resourceType": "Observation",
  "status": "final",
  "category": [
    {
      "coding": [
        {
          "system": "http://terminology.hl7.org/CodeSystem/observation-category",
          "code": "social-history",
          "display": "Social History"
        }
      ]
    }
  ],
  "code": {
    "coding": [
      {
        "system": "http://loinc.org",
        "code": "72166-2",
        "display": "Tobacco smoking status"
      }
    ]
  },
  "subject": { "reference": "Patient/1" },
  "effectiveDateTime": "2026-04-10T09:00:00Z",
  "valueCodeableConcept": {
    "coding": [
      {
        "system": "http://snomed.info/sct",
        "code": "266919005",
        "display": "Never smoker"
      }
    ]
  }
}
```

Notice: no `encounter` — social history observations don't always happen during a visit.
And `valueCodeableConcept` instead of `valueQuantity` — the result is a category, not a
number.

Smoking status SNOMED codes:

| Code | Display |
|------|---------|
| `449868002` | Current every day smoker |
| `428041000124106` | Current some day smoker |
| `8517006` | Former smoker |
| `266919005` | Never smoker |
| `77176002` | Smoker, current status unknown |
| `266927001` | Tobacco smoking consumption unknown |

---

## How Observations Connect to the Clinical Record

```
Patient
  │
  ├── Encounter (visit)
  │     │
  │     ├── Observation (BP: 140/90)      ← vital-signs
  │     ├── Observation (HR: 78)           ← vital-signs
  │     ├── Observation (Temp: 98.6°F)     ← vital-signs
  │     ├── Observation (Glucose: 95)      ← laboratory
  │     └── Condition (Hypertension)       ← diagnosis based on BP observation
  │
  ├── Observation (Smoking: never)         ← social-history (no encounter)
  │
  └── Encounter (follow-up)
        │
        ├── Observation (BP: 125/82)       ← improvement noted
        └── Observation (A1c: 5.4%)        ← lab result
```

You can query observations in several ways:

```
GET /Observation?subject=Patient/1                          ← all observations for a patient
GET /Observation?encounter=Encounter/2                      ← all observations from a visit
GET /Observation?code=85354-9                               ← all blood pressure observations
GET /Observation?subject=Patient/1&category=vital-signs     ← all vitals for a patient
GET /Observation?subject=Patient/1&code=85354-9&date=ge2026-01-01  ← BP readings since Jan 2026
```

---

## US Core Observation Profiles

US Core defines several profiles that constrain Observation for specific use cases:

| Profile | Required fields | Use for |
|---------|----------------|---------|
| **US Core Vital Signs** | status, category (vital-signs), code, subject, effective, value or dataAbsentReason | BP, HR, temp, etc. |
| **US Core Laboratory Result** | status, category (laboratory), code, subject | All lab results |
| **US Core Smoking Status** | status, code (72166-2), subject, effective, value | Tobacco use |
| **US Core Pediatric BMI for Age** | Same as vitals + age-specific percentile | BMI for children |
| **US Core Pulse Oximetry** | Same as vitals + flow rate component | O2 saturation |

The vital signs profile follows the HL7 FHIR Vital Signs profile, which mandates specific
LOINC codes for each type of vital sign.

---

## What Our Code Does vs. What's Possible

Our `ObservationService.CreateBloodPressureAsync` creates a blood pressure with:

| Field | What we set | What else is possible |
|-------|------------|----------------------|
| `status` | `final` | Any observation status |
| `category` | `vital-signs` | `laboratory`, `social-history`, `survey`, etc. |
| `code` | LOINC 85354-9 (BP panel) | Any LOINC code |
| `subject` | Patient reference | Same |
| `encounter` | Encounter reference | Can be omitted for out-of-visit observations |
| `effective` | dateTime | Period, Timing, or instant |
| `component` | Systolic + Diastolic | Any number of components |
| `value[x]` | Not set (uses components) | Quantity, CodeableConcept, string, boolean, etc. |
| `interpretation` | Not set | Normal, High, Low, Critical |
| `referenceRange` | Not set | Normal ranges with low/high bounds |
| `performer` | Not set | Who took the measurement |
| `note` | Not set | Additional text comments |
| `bodySite` | Not set | Where on the body (e.g., left arm) |
| `method` | Not set | How it was measured (e.g., automated cuff) |
| `device` | Not set | Reference to the Device used |

---

## Common Questions

**Q: Can an Observation have both value[x] and components?**
Technically yes, but it's unusual. Blood pressure uses components with no top-level value.
A complete blood count might have a top-level interpretation with component details. Most
observations use one or the other.

**Q: What's the difference between `effectiveDateTime` and `issued`?**
`effectiveDateTime` is when the measurement was *clinically relevant* — when the blood was
drawn, when the BP cuff was on the arm. `issued` is when the result was *available* — when
the lab report came back. For bedside vitals, they're the same moment. For lab tests, there
can be hours or days between them.

**Q: How do I record that a result is missing?**
Use `dataAbsentReason` instead of `value[x]`:
```json
"dataAbsentReason": {
  "coding": [
    {
      "system": "http://terminology.hl7.org/CodeSystem/data-absent-reason",
      "code": "not-performed",
      "display": "Not Performed"
    }
  ]
}
```

**Q: Should I create one Observation per vital sign or group them?**
One Observation per measurement type. BP is one Observation (with systolic/diastolic
components). Heart rate is a separate Observation. Temperature is another. Don't try to
create a "vitals panel" observation that contains everything — that's not how FHIR works.

**Q: What about device data (wearables, monitors)?**
Observations from devices use the `device` field to reference a Device resource.
`effectiveDateTime` is when the device took the reading. The `method` field can indicate
it was an automated measurement. For continuous data (like EKG), use `valueSampledData`.

**Q: How do I represent a trend (multiple readings over time)?**
Create separate Observation resources, one per reading, all with the same `code` but
different `effectiveDateTime` values. Then query with date ranges to get the trend:
```
GET /Observation?subject=Patient/1&code=8480-6&date=ge2026-01-01&_sort=date
```

---

## Summary

- **Observations** are the most common resource — anything measured or assessed about a patient
- **`code`** uses LOINC for interoperability — every measurement type has a LOINC code
- **`value[x]`** is polymorphic — Quantity for numbers, CodeableConcept for categories, etc.
- **`component`** groups related values that belong together (BP = systolic + diastolic)
- **`category`** classifies observations: vital-signs, laboratory, social-history, survey
- **`effectiveDateTime`** is when it was measured; **`issued`** is when the result was available
- **`interpretation`** and **`referenceRange`** indicate whether results are normal or abnormal
- **UCUM** (`http://unitsofmeasure.org`) standardizes units across all observations
- Every Observation links to a **Patient** (subject) and usually an **Encounter** (context)

## Next Step

Proceed to **Step 5** in the learning plan — Add an Observation collected during the
Encounter. You'll use the `ObservationService` to create a blood pressure reading linked
to both a Patient and an Encounter, then verify the result with the Step5 tests.
