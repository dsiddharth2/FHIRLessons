# Lesson 5: Update FHIR Patient Demographics

## Overview

You've created a Patient. Now you need to update it — patients move, change phone numbers,
get married. This lesson teaches you how FHIR handles updates, the difference between PUT
and PATCH, and how versioning works automatically.

---

## How Updates Work in FHIR

FHIR supports two ways to update a resource:

| Method | HTTP | What it does | When to use |
|--------|------|-------------|-------------|
| **Full Update** | `PUT` | Replaces the ENTIRE resource | You have all the data (most common) |
| **Partial Update** | `PATCH` | Changes only specific fields | You only want to change one field |

**We'll use PUT** — it's the standard approach and what most FHIR systems use.

### The critical rule of PUT

**PUT replaces the entire resource.** If you send a Patient with name and gender but
forget to include the address, the address is DELETED. You must send the complete
resource every time.

```
Original Patient:
  name: John Smith
  gender: male
  address: 123 Main St          ← exists
  phone: 555-123-4567           ← exists

PUT with only name + gender:
  name: John Smith
  gender: male
  address: ???                   ← GONE (you didn't include it)
  phone: ???                     ← GONE (you didn't include it)
```

This is why the typical workflow is:
1. `GET` the current resource
2. Modify the fields you want to change
3. `PUT` the complete modified resource back

---

## What We're Changing

Starting from the Patient created in Lesson 04, we'll update:

| Field | Old Value | New Value |
|-------|-----------|-----------|
| Address | 123 Main St, Anytown, CA 90210 | 456 Oak Avenue, Springfield, IL 62704 |
| Phone | 555-123-4567 | 555-987-6543 |
| Email | john.smith@example.com | john.smith.new@example.com |
| Marital Status | (not set) | Married |

---

## Step-by-Step

### Step 1: Read the current Patient

First, get the current state of the patient so you don't lose any data:

```http
GET http://localhost:8080/fhir/Patient/1
```

This returns the full Patient JSON including the server-assigned `id`, `meta.versionId`,
and all your original data.

### Step 2: Modify the JSON

Take the JSON you got back and change the fields listed above. The complete updated
JSON should look like this:

```json
{
  "resourceType": "Patient",
  "id": "1",
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
      "line": ["456 Oak Avenue"],
      "city": "Springfield",
      "state": "IL",
      "postalCode": "62704",
      "country": "US"
    }
  ],
  "telecom": [
    {
      "system": "phone",
      "value": "555-987-6543",
      "use": "mobile"
    },
    {
      "system": "email",
      "value": "john.smith.new@example.com"
    }
  ],
  "maritalStatus": {
    "coding": [
      {
        "system": "http://terminology.hl7.org/CodeSystem/v3-MaritalStatus",
        "code": "M",
        "display": "Married"
      }
    ]
  },
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

**Important things to notice:**
- `"id": "1"` is included in the body — it MUST match the URL
- We kept ALL original fields (name, gender, birthDate, identifier, extensions)
- We changed address, telecom, and added maritalStatus
- The race/ethnicity extensions are still there — if we left them out, they'd be deleted

### Step 3: Send the PUT request

```http
PUT http://localhost:8080/fhir/Patient/1
Content-Type: application/fhir+json
```

With the complete JSON body above.

**Using curl:**
```bash
curl -X PUT http://localhost:8080/fhir/Patient/1 \
  -H "Content-Type: application/fhir+json" \
  -d @resources/update-patient.json
```

---

## What to Expect

### Successful response (200 OK)

```json
{
  "resourceType": "Patient",
  "id": "1",
  "meta": {
    "versionId": "2",                        // <-- Was "1", now "2"
    "lastUpdated": "2026-04-12T15:30:00Z"    // <-- Updated timestamp
  },
  ...your updated data...
}
```

Key changes in the response:
- `versionId` incremented from `"1"` to `"2"`
- `lastUpdated` is the time of this update
- All your changes are reflected

### Common errors

| Error | Cause | Fix |
|-------|-------|-----|
| 400 Bad Request | `id` in body doesn't match URL | Ensure `"id": "1"` matches `/Patient/1` |
| 400 Bad Request | Missing `resourceType` | Must include `"resourceType": "Patient"` |
| 404 Not Found | Patient doesn't exist | Check the ID — did you create it first? |

---

## Understanding Versioning

Every time you update a resource, FHIR automatically:
1. Saves the current version as history
2. Creates a new version with your changes
3. Increments `meta.versionId`
4. Updates `meta.lastUpdated`

### View version history

```http
GET http://localhost:8080/fhir/Patient/1/_history
```

This returns a **Bundle** containing every version of Patient/1:

```json
{
  "resourceType": "Bundle",
  "type": "history",
  "total": 2,
  "entry": [
    {
      "resource": {
        "resourceType": "Patient",
        "id": "1",
        "meta": { "versionId": "2" },
        "address": [{ "line": ["456 Oak Avenue"], "city": "Springfield" }]
      }
    },
    {
      "resource": {
        "resourceType": "Patient",
        "id": "1",
        "meta": { "versionId": "1" },
        "address": [{ "line": ["123 Main St"], "city": "Anytown" }]
      }
    }
  ]
}
```

Version 2 (current) has the new address. Version 1 (historical) has the old address.
Both are preserved.

### Read a specific version

```http
GET http://localhost:8080/fhir/Patient/1/_history/1
```

This returns version 1 specifically — the original patient before your update.

---

## New Field: maritalStatus

This is a **CodeableConcept** — one of the most common data types in FHIR. It represents
a concept using codes from a standard system.

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

**Available marital status codes:**

| Code | Display |
|------|---------|
| A | Annulled |
| D | Divorced |
| I | Interlocutory |
| L | Legally Separated |
| M | Married |
| P | Polygamous |
| S | Never Married (Single) |
| T | Domestic Partner |
| U | Unmarried |
| W | Widowed |

### What is a CodeableConcept?

You'll see this pattern everywhere in FHIR. A CodeableConcept has:
- `coding[]` — one or more code+system pairs (machine-readable)
- `text` — optional human-readable text

```
CodeableConcept
├── coding[]
│   ├── system   ← which code system (a URL)
│   ├── code     ← the code value
│   └── display  ← human-readable label for the code
└── text         ← free-text description (optional)
```

You'll encounter CodeableConcept in nearly every FHIR resource:
- Condition.code (diagnosis)
- Observation.code (what was measured)
- Procedure.code (what was done)
- Encounter.type (type of visit)
- Medication.code (which drug)

Understanding CodeableConcept now will help with every subsequent lesson.

---

## PUT vs PATCH — When to Use Which

### PUT (what we used)

```http
PUT /fhir/Patient/1
Body: { complete resource }
```

- **Replaces everything** — you send the full resource
- **Simple** — no special syntax
- **Safe** — you know exactly what the resource will look like after
- **Risk** — forget a field and it's deleted
- **Most common** in FHIR implementations

### PATCH (alternative)

```http
PATCH /fhir/Patient/1
Content-Type: application/json-patch+json
Body: [
  { "op": "replace", "path": "/address/0/city", "value": "Springfield" }
]
```

- **Modifies only specified fields** — everything else stays
- **Complex** — uses JSON Patch syntax (RFC 6902)
- **Efficient** — smaller request body
- **Risk** — harder to reason about the final state
- **Less common** — not all FHIR servers support it

### Recommendation

Stick with **PUT** for learning. It's simpler, more widely supported, and makes it
explicit what the resource contains after the update.

---

## How This Applies to Migration

In your migration, you'll likely need to update patients after initial creation:
- Batch imports might create patients with partial data, then fill in details later
- You might need to correct errors found during data validation
- Demographic updates from the source system need to be synced

The pattern will be:
1. Search for the patient by MRN: `GET /fhir/Patient?identifier=system|MRN-001`
2. Get the full resource: `GET /fhir/Patient/{id}`
3. Merge in the new data
4. PUT the updated resource back

---

## Summary

- **PUT replaces the entire resource** — always send the complete Patient
- **Include the `id` in the body** — it must match the URL
- **Don't forget existing fields** — anything omitted is deleted
- **Versioning is automatic** — `versionId` increments, `_history` preserves all versions
- **CodeableConcept** — the pattern of system+code+display appears everywhere in FHIR
- **maritalStatus** — uses the v3-MaritalStatus code system

## Next Step

Proceed to **Step 4** in the learning plan — Create an Encounter for the patient. This
links the patient to a specific visit, which becomes the context for observations,
diagnoses, and procedures.
