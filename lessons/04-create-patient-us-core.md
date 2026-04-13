# Lesson 4: Create a FHIR Patient with US Core Profile

## Overview

The Patient resource is the foundation of everything in FHIR. Almost every other resource
(Encounter, Observation, Condition, Procedure) references a Patient. In this lesson, you'll
create your first Patient that conforms to the US Core Patient profile.

---

## What is a Patient Resource?

A Patient resource represents the person receiving healthcare. It holds demographics — name,
date of birth, gender, address, phone, identifiers — but NOT clinical data. Clinical data
lives in other resources (Observations, Conditions, etc.) that reference back to the Patient.

Think of it as the cover page of a medical chart.

### Patient resource structure

```json
{
  "resourceType": "Patient",       // Always "Patient"
  "id": "1",                       // Server-assigned ID (you don't set this on create)
  "meta": { ... },                 // Profile, version, last updated
  "identifier": [ ... ],           // Business identifiers (MRN, SSN, etc.)
  "name": [ ... ],                 // Legal name, nickname, maiden name
  "gender": "male",                // male | female | other | unknown
  "birthDate": "1985-07-15",       // Date of birth
  "address": [ ... ],              // Home, work, billing addresses
  "telecom": [ ... ],              // Phone, email, fax
  "maritalStatus": { ... },        // Single, married, divorced, etc.
  "extension": [ ... ]             // US Core adds race, ethnicity, birth sex here
}
```

---

## What is US Core?

**US Core** is a set of rules (called an Implementation Guide) that says: "If you're
building a FHIR system in the United States, HERE is the minimum data you must support."

It's maintained by HL7 and required by the **ONC (Office of the National Coordinator for
Health IT)** for health data interoperability in the US.

### Why does US Core exist?

Without US Core, two FHIR servers could store Patient data completely differently:
- Server A might store race as a text field
- Server B might not store race at all
- Server C might use a custom code system

US Core says: "Everyone must store race using THIS specific extension, with THESE specific
codes from THIS code system." This makes systems interoperable — data from Server A can
be understood by Server B.

### What US Core adds to the base Patient

The base FHIR Patient has very few required fields. US Core adds requirements:

| Field | Base FHIR | US Core | Notes |
|-------|-----------|---------|-------|
| identifier | Optional | **REQUIRED** | At least one (e.g., MRN) |
| name | Optional | **REQUIRED** | At least one |
| gender | Optional | **REQUIRED** | male / female / other / unknown |
| birthDate | Optional | **SHOULD** | Strongly recommended |
| race | Not in base | **SHOULD** (extension) | US-specific, uses CDC codes |
| ethnicity | Not in base | **SHOULD** (extension) | US-specific, uses CDC codes |
| birth sex | Not in base | Optional (extension) | Male / Female / Unknown |

**REQUIRED** = must be present or the resource is invalid.
**SHOULD** = strongly recommended; systems are expected to support it.

---

## Understanding Each Field

### meta.profile — Declaring the profile

```json
"meta": {
  "profile": [
    "http://hl7.org/fhir/us/core/StructureDefinition/us-core-patient"
  ]
}
```

This tells the server: "This Patient claims to follow the US Core Patient rules."
The server can then validate the resource against those rules.

### identifier — Business identifiers

```json
"identifier": [
  {
    "system": "http://hospital.example.org/mrn",
    "value": "MRN-001"
  }
]
```

This is NOT the same as `id`. Here's the difference:

| | `id` | `identifier` |
|-|------|-------------|
| **Set by** | Server (auto-generated) | You (business value) |
| **Example** | "1", "abc-123" | MRN-001, SSN, driver's license |
| **Purpose** | Technical — used in URLs | Business — used by humans and systems |
| **Unique in** | This FHIR server | The system that issued it |

A patient can have MULTIPLE identifiers:
```json
"identifier": [
  { "system": "http://hospital.example.org/mrn", "value": "MRN-001" },
  { "system": "http://hl7.org/fhir/sid/us-ssn", "value": "999-99-9999" },
  { "system": "http://hospital.example.org/insurance", "value": "INS-12345" }
]
```

The `system` is a URI that identifies WHO issued the identifier. It doesn't need to be
a real URL — it's a namespace.

### name — Patient names

```json
"name": [
  {
    "use": "official",
    "family": "Smith",
    "given": ["John", "Michael"]
  }
]
```

- `use` — official, usual, nickname, maiden, old, anonymous
- `family` — last name (single string)
- `given` — first and middle names (array — first element is first name, rest are middle)
- A patient can have multiple names (maiden name, nickname, legal name change)

### gender — Administrative gender

```json
"gender": "male"
```

Allowed values: `male`, `female`, `other`, `unknown`.

**Important:** This is administrative gender (what's on file for administrative purposes),
NOT clinical sex or gender identity. FHIR has separate extensions for those.

### birthDate

```json
"birthDate": "1985-07-15"
```

Format: `YYYY-MM-DD`. Can be partial: `1985-07` (month only) or `1985` (year only).

### address

```json
"address": [
  {
    "use": "home",
    "line": ["123 Main St", "Apt 4B"],
    "city": "Anytown",
    "state": "CA",
    "postalCode": "90210",
    "country": "US"
  }
]
```

- `use` — home, work, temp, billing
- `line` — array of address lines (street, apartment, suite)
- A patient can have multiple addresses

### telecom — Contact information

```json
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
]
```

- `system` — phone, fax, email, pager, url, sms
- `use` — home, work, mobile, temp
- A patient can have multiple contact methods

### extension — US Core Race and Ethnicity

Extensions are how FHIR handles data that's not in the base resource. Race and ethnicity
are US-specific requirements, so they're added as extensions rather than base fields.

**Race extension:**
```json
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
}
```

- `ombCategory` — the OMB (Office of Management and Budget) race category. These are
  the standard US federal race categories.
- `text` — a human-readable text representation (required by US Core)

**Available OMB race codes:**

| Code | Display |
|------|---------|
| 1002-5 | American Indian or Alaska Native |
| 2028-9 | Asian |
| 2054-5 | Black or African American |
| 2076-8 | Native Hawaiian or Other Pacific Islander |
| 2106-3 | White |

**Ethnicity extension:**
```json
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
```

**Available OMB ethnicity codes:**

| Code | Display |
|------|---------|
| 2135-2 | Hispanic or Latino |
| 2186-5 | Not Hispanic or Latino |

### Why are extensions structured this way?

You might wonder why extensions are so verbose. The reason: **extensibility without
breaking the standard.** Any country or organization can add their own extensions without
modifying the base Patient resource. The `url` field uniquely identifies each extension
globally, preventing conflicts.

---

## The Complete Patient JSON

Here's the full JSON you'll send to create the patient. Every field is explained above.

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

---

## How to Execute This Step

### Using Postman

1. Open Postman
2. Create a new request
3. Set method to **POST**
4. Set URL to `http://localhost:8080/fhir/Patient`
5. Go to **Headers** tab, add:
   - Key: `Content-Type`
   - Value: `application/fhir+json`
6. Go to **Body** tab, select **raw**, choose **JSON** from the dropdown
7. Paste the complete Patient JSON above
8. Click **Send**

### Using curl

Save the JSON above to a file called `patient.json` in the `resources/` folder, then run:

```bash
curl -X POST http://localhost:8080/fhir/Patient \
  -H "Content-Type: application/fhir+json" \
  -d @resources/patient.json
```

### Using VS Code REST Client

Create a file called `requests.http` in the `resources/` folder with:

```http
### Create Patient with US Core Profile
POST http://localhost:8080/fhir/Patient
Content-Type: application/fhir+json

{
  ...paste the JSON here...
}
```

Then click "Send Request" above the `POST` line.

---

## What to Expect

### Successful response (201 Created)

The server returns the Patient resource back with server-assigned fields:

```json
{
  "resourceType": "Patient",
  "id": "1",                              // <-- Server assigned this
  "meta": {
    "versionId": "1",                     // <-- First version
    "lastUpdated": "2026-04-12T...",      // <-- Timestamp
    "profile": [
      "http://hl7.org/fhir/us/core/StructureDefinition/us-core-patient"
    ]
  },
  ...rest of your data...
}
```

**SAVE THE ID** (e.g., `1`). You'll reference this patient as `Patient/1` in all
subsequent steps (Encounter, Observation, Condition, Procedure).

### Verify by reading it back

```bash
curl http://localhost:8080/fhir/Patient/1
```

Or in browser: `http://localhost:8080/fhir/Patient/1`

### Common errors

| Error | Cause | Fix |
|-------|-------|-----|
| 400 Bad Request | Malformed JSON | Check for missing commas, brackets, quotes |
| 415 Unsupported Media Type | Wrong Content-Type header | Must be `application/fhir+json` |
| 422 Unprocessable Entity | Validation error | Check the error message — usually a required field is missing |

---

## What Happens Inside the Server

When you POST a Patient:

1. Server receives the JSON
2. Parses it and validates the structure (is it valid FHIR?)
3. If `validation.requests_enabled` is true, validates against the declared profile
4. Assigns an `id` (auto-incrementing or UUID)
5. Sets `meta.versionId` to "1" and `meta.lastUpdated` to now
6. Stores it in PostgreSQL (in the `hfj_resource` and related tables)
7. Returns 201 Created with the complete resource including server-assigned fields

---

## Mapping to Your Old Database

When you migrate, you'll need to map your existing patient table columns to FHIR fields:

| Your old DB column | FHIR field | Notes |
|-------------------|------------|-------|
| patient_id / MRN | `identifier[].value` | Use your system as the `identifier[].system` |
| first_name | `name[].given[0]` | |
| middle_name | `name[].given[1]` | |
| last_name | `name[].family` | |
| date_of_birth | `birthDate` | Convert to YYYY-MM-DD |
| sex / gender | `gender` | Map to: male, female, other, unknown |
| street_address | `address[].line[0]` | |
| city | `address[].city` | |
| state | `address[].state` | |
| zip | `address[].postalCode` | |
| phone | `telecom[] (system=phone)` | |
| email | `telecom[] (system=email)` | |
| race | `extension (us-core-race)` | Map to OMB codes |
| ethnicity | `extension (us-core-ethnicity)` | Map to OMB codes |

---

## Next Step

After creating the Patient, proceed to **Step 3** in the learning plan — update the
patient's demographics using a PUT request. This teaches you how FHIR handles updates
and versioning.
