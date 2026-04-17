# FHIR Learning Plan — Hands-On Guide

## What is FHIR?

FHIR (Fast Healthcare Interoperability Resources) is an HL7 standard for exchanging healthcare
data via RESTful APIs. Think of it as a standardized way to represent and share medical records
— patients, encounters, diagnoses, procedures — as JSON resources over HTTP.

Every piece of clinical data is a **Resource** (Patient, Encounter, Observation, etc.).
Resources link to each other via **references** (e.g., an Encounter references a Patient).
A **Profile** adds constraints on top of a base resource (e.g., US Core Patient requires
name, gender, identifier, and race/ethnicity extensions).

---

## Step 1: Spin Up a FHIR Server in Docker with Postgres

### What you're doing
Running a local HAPI FHIR server (the most popular open-source FHIR server, Java-based)
backed by a PostgreSQL database. This gives you a fully functional FHIR R4 API at
`http://localhost:8080/fhir`.

### Why
You need a server to accept and store FHIR resources. HAPI FHIR supports the full
FHIR R4 spec and has a built-in web UI for browsing data.

### How

1. Make sure Docker Desktop is installed and running on your machine.

2. A `docker-compose.yml` file is already created in this folder. It defines two services:
   - **fhir-db** — Postgres 15 database (exposed on port 5433 so it doesn't clash with
     any existing Postgres on 5432)
   - **fhir-server** — HAPI FHIR server (exposed on port 8080), configured to use the
     Postgres database instead of the default embedded H2 database

3. Open a terminal in this folder and run:
   ```bash
   docker compose up -d
   ```

4. Wait ~30 seconds for the server to start, then verify:
   ```bash
   curl http://localhost:8080/fhir/metadata
   ```
   This returns the **CapabilityStatement** — a FHIR resource that describes what the
   server supports. If you get JSON back, your server is alive.

5. You can also open `http://localhost:8080` in a browser to see the HAPI FHIR web UI.

### Key concepts
- Every FHIR server exposes `/metadata` (the CapabilityStatement) — clients use it to
  discover what the server can do.
- FHIR R4 is the current widely-adopted version. R5 exists but R4 is the standard for
  US Core and most production systems.

---

## Step 2: Create a FHIR Patient with US Core Profile

### What you're doing
Creating your first FHIR resource — a Patient that conforms to the US Core Patient profile.

### Why
The Patient is the central resource in FHIR. Almost everything else references a Patient.
US Core is the US-specific implementation guide that adds requirements like race, ethnicity,
and birth sex extensions.

### How

Send a `POST` request to `http://localhost:8080/fhir/Patient` with Content-Type
`application/fhir+json`. The body is a JSON object with:

```json
{
  "resourceType": "Patient",
  "meta": {
    "profile": [
      "http://hl7.org/fhir/us/core/StructureDefinition/us-core-patient"
    ]
  },
  "identifier": [
    {
      "system": "http://hospital.example.org/mrn",
      "value": "MRN-001"
    }
  ],
  "name": [
    {
      "use": "official",
      "family": "Smith",
      "given": ["John", "Michael"]
    }
  ],
  "gender": "male",
  "birthDate": "1985-07-15",
  "address": [
    {
      "use": "home",
      "line": ["123 Main St"],
      "city": "Anytown",
      "state": "CA",
      "postalCode": "90210",
      "country": "US"
    }
  ],
  "telecom": [
    {
      "system": "phone",
      "value": "555-123-4567",
      "use": "mobile"
    },
    {
      "system": "email",
      "value": "john.smith@example.com"
    }
  ],
  "extension": [
    {
      "url": "http://hl7.org/fhir/us/core/StructureDefinition/us-core-race",
      "extension": [
        {
          "url": "ombCategory",
          "valueCoding": {
            "system": "urn:oid:2.16.840.1.113883.6.238",
            "code": "2106-3",
            "display": "White"
          }
        },
        {
          "url": "text",
          "valueString": "White"
        }
      ]
    },
    {
      "url": "http://hl7.org/fhir/us/core/StructureDefinition/us-core-ethnicity",
      "extension": [
        {
          "url": "ombCategory",
          "valueCoding": {
            "system": "urn:oid:2.16.840.1.113883.6.238",
            "code": "2186-5",
            "display": "Not Hispanic or Latino"
          }
        },
        {
          "url": "text",
          "valueString": "Not Hispanic or Latino"
        }
      ]
    }
  ]
}
```

### What to expect
The server returns `201 Created` with the patient assigned an `id` (e.g., `"id": "1"`).
**Save this ID** — you'll reference it as `Patient/1` in every subsequent step.

### Verify
```bash
curl http://localhost:8080/fhir/Patient/1
```

### Key concepts
- **resourceType** — every FHIR JSON object starts with this. It tells the server what
  kind of resource you're sending.
- **meta.profile** — declares which profile this resource claims to conform to. The server
  can validate against it.
- **identifier** — a business identifier (like MRN). Different from `id` which is the
  server-assigned technical ID.
- **extensions** — FHIR's extensibility mechanism. US Core adds race and ethnicity as
  extensions because they're not part of the base Patient resource.
- **coding systems** — `system` + `code` pairs identify concepts unambiguously.
  `urn:oid:2.16.840.1.113883.6.238` is the CDC Race & Ethnicity code system.

### US Core Patient requirements
| Field | Required? | Notes |
|-------|-----------|-------|
| identifier | YES | At least one |
| name | YES | At least one |
| gender | YES | male / female / other / unknown |
| race extension | SHOULD | US-specific |
| ethnicity extension | SHOULD | US-specific |
| birthDate | SHOULD | |

---

## Step 3: Update FHIR Patient Demographics

### What you're doing
Modifying the patient's address, phone number, and adding marital status using a `PUT` request.

### Why
Demographics change — patients move, change phone numbers, get married. You need to know
how to update existing resources.

### How

Send a `PUT` request to `http://localhost:8080/fhir/Patient/1` (note: the ID is in the URL).

The body must be the **complete** resource with your changes applied. FHIR `PUT` does a
full replacement — if you leave out a field, it gets removed.

Changes to make in the JSON body:
- Update `address` to a new address
- Update `telecom` phone number
- Add a `maritalStatus` field:
  ```json
  "maritalStatus": {
    "coding": [
      {
        "system": "http://terminology.hl7.org/CodeSystem/v3-MaritalStatus",
        "code": "M",
        "display": "Married"
      }
    ]
  }
  ```
- **Important:** Include `"id": "1"` in the body — it must match the URL

### What to expect
The server returns `200 OK` with the updated resource. The `meta.versionId` increments.

### Verify version history
```bash
curl http://localhost:8080/fhir/Patient/1/_history
```
This shows all versions of the patient — FHIR servers keep history automatically.

### Key concepts
- **PUT = full replacement.** If you omit the race/ethnicity extensions, they're deleted.
  This is different from PATCH (partial update), which FHIR also supports but is less common.
- **Versioning** — FHIR servers track versions automatically. Each update creates a new
  version. `meta.versionId` and `meta.lastUpdated` reflect this.
- **_history** — a special FHIR operation that returns all versions of a resource.

---

## Step 4: Create an Encounter for the Patient

### What you're doing
Recording that the patient had a visit (ambulatory/outpatient check-up).

### Why
An **Encounter** is the clinical context — the "visit" or "admission." Everything that
happens during a visit (observations, diagnoses, procedures) links back to the Encounter.
Without it, clinical data has no temporal/contextual anchor.

### How

Send a `POST` request to `http://localhost:8080/fhir/Encounter`:

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
    "reference": "Patient/1"
  },
  "period": {
    "start": "2026-04-10T09:00:00Z",
    "end": "2026-04-10T10:30:00Z"
  },
  "reasonCode": [
    {
      "coding": [
        {
          "system": "http://snomed.info/sct",
          "code": "185349003",
          "display": "Routine health check"
        }
      ]
    }
  ]
}
```

### What to expect
Server returns `201 Created`. **Save the Encounter ID** (likely `2`) — you'll reference
it as `Encounter/2` in steps 5, 6, and 7.

### Key concepts
- **References** — `"subject": {"reference": "Patient/1"}` is how FHIR links resources
  together. This is the fundamental relationship mechanism in FHIR.
- **class** — the broad category of the encounter:
  - `AMB` = ambulatory (outpatient)
  - `IMP` = inpatient
  - `EMER` = emergency
- **status** — the lifecycle: planned -> in-progress -> finished (or cancelled)
- **period** — when the encounter started and ended
- **SNOMED CT** (`http://snomed.info/sct`) — a comprehensive clinical terminology system
  used worldwide for clinical terms

---

## Step 5: Add an Observation Collected During the Encounter

### What you're doing
Recording a blood pressure measurement (130/85 mmHg) taken during the encounter.

### Why
**Observations** are the workhorse of clinical data. They represent measurements, test
results, vital signs, social history, and more. In your migration, most clinical data
points will become Observations.

### How

Send a `POST` request to `http://localhost:8080/fhir/Observation`:

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
        "code": "85354-9",
        "display": "Blood pressure panel"
      }
    ]
  },
  "subject": {
    "reference": "Patient/1"
  },
  "encounter": {
    "reference": "Encounter/2"
  },
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
        "value": 130,
        "unit": "mmHg",
        "system": "http://unitsofmeasure.org",
        "code": "mm[Hg]"
      }
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
        "value": 85,
        "unit": "mmHg",
        "system": "http://unitsofmeasure.org",
        "code": "mm[Hg]"
      }
    }
  ]
}
```

### Key concepts
- **LOINC** (`http://loinc.org`) — the standard coding system for lab tests and clinical
  observations. Every observation type has a LOINC code.
- **component** — used for multi-part observations. Blood pressure has two components:
  systolic and diastolic. A simple observation (like body temperature) would use
  `valueQuantity` directly instead of components.
- **category** — classifies the observation type:
  - `vital-signs` — BP, heart rate, temperature, etc.
  - `laboratory` — lab test results
  - `social-history` — smoking status, alcohol use, etc.
  - `survey` — questionnaire responses
- **valueQuantity** — a numeric value with a unit. Uses UCUM (`http://unitsofmeasure.org`)
  for standardized unit codes.
- **effectiveDateTime** — when the observation was actually taken (not when it was recorded)
- The Observation links to BOTH `Patient/1` (subject) and `Encounter/2` (encounter context)

---

## Step 6: Create a Condition (Diagnosis) Based on the Encounter

### What you're doing
Recording that the patient was diagnosed with hypertension during the encounter
(based on the elevated BP reading from Step 5).

### Why
A **Condition** represents a clinical problem, diagnosis, or health concern. It's how FHIR
tracks what's wrong with a patient. In your migration, diagnoses from the old database
will map to Condition resources.

### How

Send a `POST` request to `http://localhost:8080/fhir/Condition`:

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
        "display": "Hypertension"
      },
      {
        "system": "http://hl7.org/fhir/sid/icd-10-cm",
        "code": "I10",
        "display": "Essential (primary) hypertension"
      }
    ]
  },
  "subject": {
    "reference": "Patient/1"
  },
  "encounter": {
    "reference": "Encounter/2"
  },
  "onsetDateTime": "2026-04-10",
  "recordedDate": "2026-04-10"
}
```

### What to expect
Server returns `201 Created`. **Save the Condition ID** (likely `4`) — you'll reference
it as `Condition/4` in Step 7.

### Key concepts
- **Multiple codings** — the `code` field has BOTH a SNOMED CT code and an ICD-10-CM code
  for the same concept. This is common and encouraged in FHIR. SNOMED is for clinical use,
  ICD-10 is for billing. Real systems often need both.
- **clinicalStatus** — is the condition currently active?
  - `active` — ongoing problem
  - `resolved` — no longer an issue
  - `inactive` — not currently relevant
- **verificationStatus** — how certain is the diagnosis?
  - `confirmed` — definitely has it
  - `unconfirmed` — suspected but not proven
  - `differential` — one of several possibilities
  - `entered-in-error` — mistake, should be ignored
- **category** — `encounter-diagnosis` (diagnosed during a visit) vs `problem-list-item`
  (an ongoing problem on the patient's problem list)
- **ICD-10-CM** (`http://hl7.org/fhir/sid/icd-10-cm`) — the US billing code system.
  `I10` = Essential hypertension.

---

## Step 7: Create a Procedure Based on that Condition

### What you're doing
Recording that a blood pressure monitoring procedure was performed because of the
hypertension diagnosis.

### Why
A **Procedure** represents an action performed on or for a patient — surgeries, therapies,
diagnostic procedures, counseling. It can reference the Condition that justified it,
closing the clinical loop.

### How

Send a `POST` request to `http://localhost:8080/fhir/Procedure`:

```json
{
  "resourceType": "Procedure",
  "status": "completed",
  "code": {
    "coding": [
      {
        "system": "http://snomed.info/sct",
        "code": "413153004",
        "display": "Blood pressure monitoring"
      }
    ]
  },
  "subject": {
    "reference": "Patient/1"
  },
  "encounter": {
    "reference": "Encounter/2"
  },
  "performedDateTime": "2026-04-10T09:30:00Z",
  "reasonReference": [
    {
      "reference": "Condition/4"
    }
  ]
}
```

### Key concepts
- **reasonReference** — links the Procedure to the Condition that justified it. This is
  clinically important ("why was this done?") and also matters for billing.
- **status** — the lifecycle:
  - `completed` — done
  - `in-progress` — currently happening
  - `not-done` — was planned but didn't happen (e.g., patient refused)
  - `preparation` — being set up
- **performedDateTime** vs **performedPeriod** — use DateTime for a point-in-time procedure,
  use Period (with start/end) for procedures that take time (like a surgery).

---

## How Everything Connects

```
Patient/1  (John Smith)
   |
   +-- Encounter/2  (checkup visit, Apr 10 2026)
          |
          +-- Observation/3  (BP reading: 130/85 mmHg)
          |
          +-- Condition/4  (Hypertension diagnosis)
          |       |
          |       +-- Procedure/5  (BP monitoring, justified by the hypertension)
```

This is the **clinical story** told through linked FHIR resources. The reference chain is:
- Procedure -> references Condition (reason) + Encounter + Patient
- Condition -> references Encounter + Patient
- Observation -> references Encounter + Patient
- Encounter -> references Patient
- Patient is the root

---

## Tools You'll Want

| Tool | Purpose |
|------|---------|
| **Postman** | GUI for sending HTTP requests — great for exploring APIs |
| **VS Code REST Client** | `.http` files with Send Request buttons — keeps requests in version control |
| **curl** | Quick command-line testing |
| **Browser** | `http://localhost:8080` for the HAPI FHIR web UI (browse/search resources) |
| **DBeaver or pgAdmin** | Connect to Postgres on port 5433 to see how FHIR stores data internally |

---

## Code Systems Quick Reference

| System URL | Name | Used For | Example |
|-----------|------|----------|---------|
| `http://snomed.info/sct` | SNOMED CT | Clinical terms (diagnoses, procedures, findings) | Hypertension = 38341003 |
| `http://loinc.org` | LOINC | Lab tests and observation types | Blood pressure panel = 85354-9 |
| `http://hl7.org/fhir/sid/icd-10-cm` | ICD-10-CM | Billing diagnosis codes (US) | Hypertension = I10 |
| `http://www.ama-assn.org/go/cpt` | CPT | Billing procedure codes (US) | Office visit = 99213 |
| `http://unitsofmeasure.org` | UCUM | Standardized units of measure | mmHg = mm[Hg] |
| `urn:oid:2.16.840.1.113883.6.238` | CDC Race/Ethnicity | US race and ethnicity codes | White = 2106-3 |

---

## FHIR REST API Cheat Sheet

| Operation | HTTP Method | URL Pattern | Example |
|-----------|-------------|-------------|---------|
| Create | POST | `/fhir/{ResourceType}` | `POST /fhir/Patient` |
| Read | GET | `/fhir/{ResourceType}/{id}` | `GET /fhir/Patient/1` |
| Update | PUT | `/fhir/{ResourceType}/{id}` | `PUT /fhir/Patient/1` |
| Delete | DELETE | `/fhir/{ResourceType}/{id}` | `DELETE /fhir/Patient/1` |
| Search | GET | `/fhir/{ResourceType}?params` | `GET /fhir/Patient?name=Smith` |
| History | GET | `/fhir/{ResourceType}/{id}/_history` | `GET /fhir/Patient/1/_history` |
| Capabilities | GET | `/fhir/metadata` | `GET /fhir/metadata` |

---

## Useful Search Queries to Try After You Complete All Steps

```bash
# Find all resources for Patient 1
curl "http://localhost:8080/fhir/Patient/1/$everything"

# Find all encounters for a patient
curl "http://localhost:8080/fhir/Encounter?subject=Patient/1"

# Find all observations from a specific encounter
curl "http://localhost:8080/fhir/Observation?encounter=Encounter/2"

# Find all active conditions for a patient
curl "http://localhost:8080/fhir/Condition?subject=Patient/1&clinical-status=active"

# Find procedures linked to a condition
curl "http://localhost:8080/fhir/Procedure?reason-reference=Condition/4"
```

These search queries demonstrate how FHIR's RESTful search lets you navigate the
clinical data graph from any direction.

---

## Step 8: Load the US Core Implementation Guide in the FHIR Server

### What you're doing
Loading the US Core Implementation Guide (IG) package into the HAPI FHIR server so that
the server knows about US Core profiles, value sets, and validation rules.

### Why
Without the IG loaded, the server accepts any JSON that looks like FHIR — it can't validate
that a Patient actually conforms to US Core. Loading the IG gives the server the
StructureDefinitions, CodeSystems, and ValueSets it needs to enforce US Core constraints.

---

## Step 9: Validate All Resources Against the US Core Implementation Guide

### What you're doing
Configuring the FHIR server to validate incoming resources against US Core profiles, and
testing validation by submitting both valid and invalid resources.

### Why
In production, you want the server to reject resources that don't meet US Core requirements.
This ensures data quality at the point of entry — catching missing required fields,
incorrect code systems, or missing extensions before data lands in the database.

---

## Step 10: Add a Patient Reported Outcome Questionnaire

### What you're doing
Creating a Questionnaire resource titled "PRoM Test Questionnaire" — a structured form
for capturing Patient Reported Outcome Measures (PROMs).

### Why
PROMs are standardized surveys that patients fill out to report their own health status,
symptoms, and quality of life. In FHIR, the **Questionnaire** resource defines the form
structure (questions, answer types, validation rules), while **QuestionnaireResponse**
captures the patient's actual answers.

---

## Step 11: Add a QuestionnaireResponse for the PROM Questionnaire

### What you're doing
Creating a QuestionnaireResponse that captures Patient/1's answers to the PRoM Test
Questionnaire from Step 10.

### Why
The Questionnaire defines the form; the QuestionnaireResponse records what the patient
actually answered. This separation lets you reuse the same questionnaire across many
patients while keeping each patient's responses as a distinct resource linked to their
record.

---

## Step 12: Add a PlanDefinition with a Survey Activity

### What you're doing
Creating a PlanDefinition that defines a care protocol with one activity: the patient
must complete a survey (the PRoM Questionnaire) within a given time period.

### Why
A **PlanDefinition** is a reusable clinical protocol or guideline — it describes *what
should happen* without being tied to a specific patient. Think of it as a template for
care plans. By defining the survey activity here, you can later apply it to any patient
to generate a personalized CarePlan.

---

## Step 13: Apply the PlanDefinition and Validate the CarePlan

### What you're doing
Running the `$apply` operation on the PlanDefinition for Patient/1 to auto-generate a
CarePlan, then validating that the generated CarePlan is correct and complete.

### Why
The `$apply` operation is how FHIR turns a generic protocol (PlanDefinition) into a
patient-specific plan (CarePlan). This is a key workflow in clinical decision support —
the server takes the template, fills in patient-specific details, and produces actionable
tasks. Validating the output ensures the generated plan is structurally sound.

---

## Step 14: Display a Schedule of Activities from the CarePlan

### What you're doing
Using the generated CarePlan to display a schedule of activities for the patient —
showing what needs to be done, when, and the current status of each activity.

### Why
A CarePlan is only useful if it drives action. This step closes the loop from protocol
definition to patient-facing schedule. In a real application, this is what a patient
portal or clinician dashboard would display.

---

## Step 15: Explore Bundle Transactions

### What you're doing
Creating and submitting FHIR Bundle resources of type `transaction` to perform multiple
operations (create, update, delete) in a single atomic request.

### Why
In real-world integrations, you rarely send one resource at a time. Bundle transactions
let you submit a batch of interdependent resources atomically — either all succeed or all
fail. This is essential for data migrations, system integrations, and any workflow where
resources reference each other (e.g., creating a Patient and Encounter together).

---

## Step 16: Explore FHIR Bulk Operations

### What you're doing
Using FHIR Bulk Data Access (the `$export` operation) to export large volumes of data
from the server in NDJSON format.

### Why
Bulk operations are how FHIR handles population-level data exchange — exporting all
patients, all conditions, or all data for a specific group. This is critical for
analytics, reporting, data warehousing, and large-scale migrations where resource-by-resource
REST calls would be too slow.

---

## Next Steps After Completing This Plan

1. **Try searching** — use the queries above to retrieve data in different ways
2. **Try $everything** — `GET /fhir/Patient/1/$everything` returns all resources related
   to a patient in one call (a FHIR "operation")
3. **Add more resources** — try MedicationRequest, AllergyIntolerance, DiagnosticReport
4. **Look at US Core profiles** — https://www.hl7.org/fhir/us/core/ for the full list
5. **Think about your migration** — map columns in your old database to FHIR resource
   fields and identify which resource type each table maps to
