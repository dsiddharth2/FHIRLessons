# Lesson 17: Bundle Transactions — Atomic Multi-Resource Operations

## Overview

So far we've created resources one at a time — POST a Patient, POST an Encounter, POST
an Observation. Each call is independent. But in real-world systems, you often need to
create multiple interdependent resources **atomically** — either all succeed or all fail.

That's what **Bundle transactions** do. A Bundle of type `transaction` wraps multiple
FHIR operations into a single HTTP request. The server processes them as one atomic
unit.

---

## Why Bundles?

### The Problem with Individual Requests

Imagine creating a Patient and an Encounter that references that Patient:

```
POST /fhir/Patient → 201 Created (Patient/123)
POST /fhir/Encounter (subject: Patient/123) → 500 Server Error
```

Now you have an orphaned Patient with no Encounter. You'd need cleanup logic to delete
the Patient, handle retries, etc.

### The Bundle Solution

```
POST /fhir (Bundle transaction)
  ├── Entry 1: Create Patient
  └── Entry 2: Create Encounter (references Patient from Entry 1)
  → Either both succeed or neither does
```

No orphaned resources. No cleanup. One request, one response.

---

## Bundle Types

| Type | Who Creates It | Atomicity | Description |
|------|---------------|-----------|-------------|
| `transaction` | You | All or nothing | Multiple operations in one atomic request |
| `batch` | You | Each entry independent | Multiple independent operations in one request |
| `searchset` | Server | N/A | Returned when you search (e.g., `GET /fhir/Patient?name=Smith`) |
| `history` | Server | N/A | Returned when you call `_history` on a resource |
| `collection` | Either | N/A | Group resources for transfer (like a zip file) |
| `document` | You | N/A | Clinical document with a Composition as first entry |

For data operations, `transaction` and `batch` are the ones you actively create. The key difference:

- **transaction** — if any entry fails, the **entire bundle** is rolled back
- **batch** — each entry is independent; some can succeed while others fail

### Real-World Examples for Each Type

**Transaction** — when resources depend on each other and partial creation would leave bad data:
- **Admitting a patient**: create Patient + Encounter + Coverage (insurance) in one shot.
  If the insurance lookup fails, you don't want a half-admitted patient
- **Lab results**: create DiagnosticReport + multiple Observation results together. A
  report without its observations is useless
- **Data migration**: moving a patient from an old system — Patient + all their Conditions
  + Medications must arrive together or not at all

**Batch** — when you have many independent operations and don't need atomicity:
- **Nightly bulk import**: load 500 lab results from an external lab system. If one fails,
  the other 499 should still go through
- **Preloading reference data**: create 200 Practitioner resources for your hospital
  directory. Each is independent
- **Monthly reporting**: read 50 different patients' data in one request instead of 50
  separate calls

**Searchset** — you use this every day without thinking:
- "Show me all active patients" → `GET /fhir/Patient?active=true`
- "Find all lab results for this patient" →
  `GET /fhir/Observation?subject=Patient/123&category=laboratory`

**History** — auditing and tracking changes:
- "What did this patient record look like before the address change?" →
  `GET /fhir/Patient/123/_history`
- Compliance review: proving what data existed at a specific point in time

**Document** — packaging a clinical narrative:
- **Discharge summary**: a Composition (narrative) + referenced Conditions, Medications,
  and follow-up instructions, all bundled as one shareable document
- **Referral letter**: a structured document sent from a GP to a specialist with all
  relevant clinical context

**Collection** — general-purpose grouping:
- Exporting a patient's complete record to hand to another provider
- Packaging a set of Questionnaires for distribution to clinics

---

## Transaction Bundle Structure

```json
{
  "resourceType": "Bundle",
  "type": "transaction",
  "entry": [
    {
      "fullUrl": "urn:uuid:unique-id-1",
      "resource": { ... the resource to create ... },
      "request": {
        "method": "POST",
        "url": "Patient"
      }
    },
    {
      "fullUrl": "urn:uuid:unique-id-2",
      "resource": { ... references urn:uuid:unique-id-1 ... },
      "request": {
        "method": "POST",
        "url": "Encounter"
      }
    }
  ]
}
```

### Entry Fields

| Field | Description |
|-------|-------------|
| `fullUrl` | Temporary ID for referencing within the bundle (use `urn:uuid:...`) |
| `resource` | The FHIR resource to create/update |
| `request.method` | HTTP method: POST (create), PUT (update), DELETE |
| `request.url` | The FHIR endpoint (e.g., `Patient`, `Patient/123`) |

### Internal References with urn:uuid

The key feature of transactions is **internal references**. Before the bundle is
processed, no resource has a server-assigned ID. So you use temporary UUIDs:

1. Assign `fullUrl: "urn:uuid:abc-123"` to Entry 1 (Patient)
2. In Entry 2 (Encounter), reference it as `"subject": {"reference": "urn:uuid:abc-123"}`
3. The server resolves `urn:uuid:abc-123` to the actual Patient ID after creation

This is how you create resources that reference each other in a single request.

---

## Supported Operations in a Transaction

| Method | URL Pattern | What It Does |
|--------|-------------|-------------|
| `POST` | `ResourceType` | Create a new resource |
| `PUT` | `ResourceType/id` | Update an existing resource |
| `DELETE` | `ResourceType/id` | Delete a resource |
| `GET` | `ResourceType/id` | Read a resource (less common in transactions) |
| `PUT` | `ResourceType?identifier=...` | Conditional update (upsert) |

You can mix operations — create some resources, update others, delete some — all in
one atomic request.

---

## Transaction Response

The server returns a Bundle with one entry per request, containing the outcome:

```json
{
  "resourceType": "Bundle",
  "type": "transaction-response",
  "entry": [
    {
      "response": {
        "status": "201 Created",
        "location": "Patient/456/_history/1"
      }
    },
    {
      "response": {
        "status": "201 Created",
        "location": "Encounter/457/_history/1"
      }
    }
  ]
}
```

If the transaction fails, you get an **OperationOutcome** instead — nothing was created.

---

## Conditional Creates (Upserts)

A powerful pattern is the **conditional create** — create only if the resource doesn't
already exist:

```json
{
  "request": {
    "method": "POST",
    "url": "Patient",
    "ifNoneExist": "identifier=http://hospital.example.org/mrn|MRN-001"
  }
}
```

If a Patient with that MRN already exists, the server returns the existing one instead
of creating a duplicate. This makes transactions **idempotent** — safe to retry.

---

## Our Bundle Scenarios

We'll build three transaction types:

### 1. Clinical Encounter Bundle
Create a Patient + Encounter + Observation in one atomic request, with internal
references linking them together.

### 2. Multi-Resource Update Bundle
Update multiple existing resources in a single request (e.g., update a CarePlan
activity status and create the corresponding QuestionnaireResponse).

### 3. Mixed Operations Bundle
Combine creates and deletes in one transaction.

---

## What Our Code Will Do

We'll create a `BundleService` that:

1. **CreateClinicalEncounterBundleAsync** — builds and submits a transaction with
   Patient + Encounter + Observation using urn:uuid references
2. **CreateBatchBundleAsync** — builds a batch (non-atomic) with multiple creates
3. Parses the transaction response to extract created resource IDs

---

## Common Questions

**Q: What's the size limit on a Bundle?**
HAPI's default limit is 100 entries per transaction. This is configurable. For large
data loads, you'd split into multiple bundles or use batch mode.

**Q: What happens if one entry in a transaction fails?**
The entire transaction is rolled back. Nothing is created, updated, or deleted. The
server returns an OperationOutcome explaining what failed.

**Q: What about batch mode — what if one entry fails?**
In batch mode, other entries still succeed. The response contains individual status
codes for each entry — you check each one for errors.

**Q: Can I use transactions for migrations?**
Yes — this is one of the primary use cases. Create all resources for a patient in one
transaction to ensure referential integrity. If anything fails, nothing is partially
migrated.

**Q: Do the entries execute in order?**
For transactions, the server may reorder entries to resolve references. For batches,
entries execute in the order given. In practice, put referenced resources (Patient)
before referencing resources (Encounter) for clarity.

---

## Summary

- **Bundle transaction** wraps multiple operations into one atomic request
- Use `urn:uuid:` temporary IDs for internal references between entries
- **transaction** = all or nothing; **batch** = each entry independent
- Supported operations: POST (create), PUT (update), DELETE, GET
- **Conditional creates** (`ifNoneExist`) make transactions idempotent
- Transaction responses contain the status and location for each entry
- Key use cases: interdependent resource creation, data migration, atomic updates

## Next Step

Proceed to **Lesson 18** — Bulk data export using the `$export` operation for
population-level data extraction in NDJSON format.
