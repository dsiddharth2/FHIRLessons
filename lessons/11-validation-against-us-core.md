# Lesson 11: Validation — Enforcing US Core Rules

## Overview

In the last lesson you verified that the US Core IG is loaded in HAPI. The profiles,
value sets, and code systems are all there — but the server isn't using them to reject
bad data. It's like having a rulebook on the shelf that nobody reads.

This lesson is about **turning on validation** so the server actually enforces US Core
constraints, and then testing it by submitting both valid and invalid resources to see
what passes and what gets rejected.

---

## What Is FHIR Validation?

Validation checks whether a resource conforms to a profile's rules:

- **Required fields present?** — US Core Patient needs name, gender, identifier
- **Correct data types?** — Is `birthDate` actually a date, not a string?
- **Valid codes?** — Is `gender` one of male/female/other/unknown?
- **Cardinality respected?** — Does the resource have at least one identifier?
- **Extensions correct?** — Is the race extension structured properly?

### Two Ways to Validate

#### 1. Server-Side Validation (Reject on Create/Update)

When `validation.requests_enabled` is `true`, HAPI validates every incoming resource
against the profile declared in `meta.profile`. If it fails, the server returns a
`400 Bad Request` with an OperationOutcome explaining what's wrong.

```
POST /fhir/Patient  (with meta.profile = us-core-patient)
  |
  +-- Server checks resource against us-core-patient profile
  |
  +-- If valid:   201 Created
  +-- If invalid: 400 Bad Request + OperationOutcome with error details
```

#### 2. The $validate Operation (Check Without Saving)

You can ask the server to validate a resource without actually creating it:

```
POST /fhir/Patient/$validate
Content-Type: application/fhir+json

{ ... resource body ... }
```

This returns an **OperationOutcome** with issues found — errors, warnings, and
informational messages — without saving anything to the database.

This is useful for:
- Testing your resources before submitting them
- Validating data during migration (check thousands of records without storing them)
- Debugging why a resource is being rejected

---

## Enabling Validation in HAPI

To turn on validation, change this line in `docker-compose.yml`:

```yaml
# Before
hapi.fhir.validation.requests_enabled: "false"

# After
hapi.fhir.validation.requests_enabled: "true"
```

Then restart the server:

```bash
docker compose down && docker compose up -d
```

### What Changes

| Before (false) | After (true) |
|----------------|-------------|
| Server accepts any valid FHIR JSON | Server validates against declared profile |
| `meta.profile` is just a label | `meta.profile` triggers validation |
| No OperationOutcome on create | Returns OperationOutcome on validation failure |
| `$validate` still works | `$validate` still works (always available) |

**Important:** Validation only kicks in for resources that declare a `meta.profile`. If
you POST a Patient with no profile, the server only checks base FHIR rules (which are
very loose). The profile declaration is what triggers US Core validation.

---

## The OperationOutcome Resource

When validation fails (or when you use `$validate`), the server returns an
**OperationOutcome** — a FHIR resource that describes what went wrong.

```json
{
  "resourceType": "OperationOutcome",
  "issue": [
    {
      "severity": "error",
      "code": "processing",
      "diagnostics": "Patient.identifier: minimum required = 1, but only found 0",
      "location": ["Patient.identifier"]
    },
    {
      "severity": "error",
      "code": "processing",
      "diagnostics": "Patient.name: minimum required = 1, but only found 0",
      "location": ["Patient.name"]
    },
    {
      "severity": "warning",
      "code": "processing",
      "diagnostics": "Patient.birthDate: US Core recommends this field be present",
      "location": ["Patient.birthDate"]
    }
  ]
}
```

### Issue Severity Levels

| Severity | Meaning | Blocks create? |
|----------|---------|---------------|
| `fatal` | Cannot process at all | Yes |
| `error` | Violates a constraint | Yes |
| `warning` | Doesn't meet a recommendation (SHOULD) | No |
| `information` | Just FYI | No |

Only `fatal` and `error` block resource creation. Warnings and informational messages
are returned but the resource is still saved.

### Issue Codes

| Code | Meaning |
|------|---------|
| `required` | A required field is missing |
| `value` | A value doesn't match the expected format or binding |
| `processing` | General validation error |
| `code-invalid` | A coded value isn't in the required ValueSet |
| `structure` | The resource structure is wrong |

---

## What US Core Validates

### Patient Profile Checks

| Rule | What it checks | Severity |
|------|---------------|----------|
| `identifier` min 1 | At least one identifier | error |
| `name` min 1 | At least one name | error |
| `gender` min 1 | Gender must be present | error |
| `gender` binding | Must be male/female/other/unknown | error |
| `birthDate` must-support | Should be present | warning |
| Race extension structure | Correct URL, ombCategory sub-extension | error (if present but malformed) |

### Condition Profile Checks

| Rule | What it checks | Severity |
|------|---------------|----------|
| `category` min 1 | At least one category | error |
| `code` min 1 | Must have a diagnosis code | error |
| `subject` min 1 | Must reference a patient | error |
| `clinicalStatus` conditional | Required unless entered-in-error | error |

### Observation Vital Signs Checks

| Rule | What it checks | Severity |
|------|---------------|----------|
| `status` required | Must have a status | error |
| `category` must include vital-signs | Category code required | error |
| `code` required | Must have a LOINC code | error |
| `subject` required | Must reference a patient | error |
| `effective[x]` required | Must have a date/time | error |

---

## Testing Validation — Valid vs. Invalid Resources

### Valid Patient (passes US Core)

```json
{
  "resourceType": "Patient",
  "meta": {
    "profile": ["http://hl7.org/fhir/us/core/StructureDefinition/us-core-patient"]
  },
  "identifier": [{ "system": "http://hospital.example.org/mrn", "value": "MRN-001" }],
  "name": [{ "family": "Smith", "given": ["John"] }],
  "gender": "male"
}
```

Result: `201 Created` — has identifier, name, and gender.

### Invalid Patient — Missing Name

```json
{
  "resourceType": "Patient",
  "meta": {
    "profile": ["http://hl7.org/fhir/us/core/StructureDefinition/us-core-patient"]
  },
  "identifier": [{ "system": "http://hospital.example.org/mrn", "value": "MRN-001" }],
  "gender": "male"
}
```

Result: `400 Bad Request` — OperationOutcome says `Patient.name: minimum required = 1`.

### Invalid Patient — Missing Everything

```json
{
  "resourceType": "Patient",
  "meta": {
    "profile": ["http://hl7.org/fhir/us/core/StructureDefinition/us-core-patient"]
  }
}
```

Result: `400 Bad Request` — multiple errors for missing identifier, name, and gender.

---

## Using $validate Without Saving

The `$validate` operation is available even with validation disabled. It's a dry run.

```bash
# Validate a Patient without creating it
curl -X POST http://localhost:8080/fhir/Patient/\$validate \
  -H "Content-Type: application/fhir+json" \
  -d '{
    "resourceType": "Patient",
    "meta": {
      "profile": ["http://hl7.org/fhir/us/core/StructureDefinition/us-core-patient"]
    },
    "gender": "male"
  }'
```

Returns an OperationOutcome listing all issues — without storing anything.

You can also validate a specific resource already on the server:

```bash
# Validate an existing resource
GET /fhir/Patient/1/$validate
```

And you can validate against a specific profile (even if the resource doesn't declare it):

```bash
POST /fhir/Patient/$validate?profile=http://hl7.org/fhir/us/core/StructureDefinition/us-core-patient
```

---

## What Our Code Will Do

We'll create a `ValidationService` that:
- Uses `$validate` to check resources without saving them
- Parses the OperationOutcome to extract errors and warnings
- Tests both valid and invalid resources to see what HAPI catches

This is useful regardless of whether server-side validation is enabled — `$validate` always
works as a dry-run check.

---

## Terminology Validation and External Code Systems

When you enable validation, HAPI checks two things:

1. **Structural validation** — are required fields present, correct types, right cardinality?
2. **Terminology validation** — are coded values from the right CodeSystems and ValueSets?

Structural validation works out of the box. Terminology validation is trickier because
some code systems live on **external terminology servers** that HAPI can't reach locally.

### The Race/Ethnicity Problem

US Core's race and ethnicity extensions use codes from the CDC Race & Ethnicity CodeSystem
(`urn:oid:2.16.840.1.113883.6.238`). The ValueSets that define valid race/ethnicity codes
(`omb-race-category`, `omb-ethnicity-category`) reference **NLM VSAC** — the US National
Library of Medicine's Value Set Authority Center.

```
US Core Patient
  -> race extension
    -> ombCategory Coding
      -> must be from ValueSet: omb-race-category
        -> references: http://cts.nlm.nih.gov/fhir/ValueSet/2.16.840.1.114222.4.11.836
          -> HAPI can't reach this external server!
```

Without VSAC access, HAPI can't expand these ValueSets and rejects the codes as "unknown."

### What Is a Terminology Service?

A terminology service is a server that answers questions about code systems:
- "Is code `2106-3` valid in system `urn:oid:2.16.840.1.113883.6.238`?"
- "What codes are in the ValueSet `omb-race-category`?"
- "What does code `I10` mean in ICD-10?"

FHIR defines standard terminology operations:
- `CodeSystem/$validate-code` — is this code valid?
- `ValueSet/$expand` — give me all codes in this ValueSet
- `ValueSet/$validate-code` — is this code in this ValueSet?
- `ConceptMap/$translate` — translate a code from one system to another

HAPI has a built-in terminology service, but it only knows about code systems that are
loaded in its database. For external code systems (like NLM VSAC), you need to either:

1. **Configure HAPI with VSAC credentials** — HAPI can act as a proxy to NLM VSAC
   (requires a free UMLS account at https://uts.nlm.nih.gov/)
2. **Pre-load the codes** — Upload the CodeSystem resource with all concepts directly
   to HAPI so it has them locally
3. **Skip the extensions** — Race and ethnicity are SHOULD (not required) in US Core,
   so omitting them is still conformant

### Our TerminologyService

We created a `TerminologyService` class that can:
- Check if a CodeSystem's codes are indexed (`IsCdcRaceEthnicityIndexedAsync`)
- Upload the CDC Race & Ethnicity CodeSystem to HAPI (`EnsureCdcRaceEthnicityLoadedAsync`)

When you upload a CodeSystem with `content: complete` and a list of concepts, HAPI indexes
those codes into its terminology tables. After indexing (which happens asynchronously),
`$validate-code` operations work for those codes.

However, even with the CodeSystem indexed, the **ValueSets** from US Core still reference
NLM VSAC for expansion. HAPI caches ValueSet expansions in memory when it loads the IG,
and updating the stored ValueSet resource doesn't change the in-memory cache.

### What We Do in Our Code

For our learning project, we omit race/ethnicity extensions from `PatientService` because
we don't have VSAC access. This is a **pragmatic trade-off** — the Patient is still fully
US Core conformant (race and ethnicity are SHOULD, not MUST).

In a production system with VSAC credentials, you'd configure HAPI to use the external
terminology server and the extensions would validate correctly:

```yaml
# Production HAPI config (not in our docker-compose)
hapi.fhir.terminology_server.url: "https://cts.nlm.nih.gov/fhir/"
hapi.fhir.terminology_server.apikey: "your-umls-api-key"
```

### Key Takeaway

Terminology validation is the hardest part of FHIR validation. Structural validation
(required fields, types, cardinality) works locally. But code validation often depends
on external terminology servers that may not be available in development. Real production
systems solve this by:
- Connecting to NLM VSAC or a local terminology server
- Pre-loading commonly used CodeSystems
- Running terminology validation as a separate pipeline step

---

## Common Questions

**Q: Should I enable validation in production?**
Usually yes, but with care. Enabling validation means bad data gets rejected at the door —
which is good for data quality. But it can also block legitimate data that doesn't perfectly
conform (e.g., legacy data missing optional-but-expected fields). Many teams enable
validation gradually: start with `$validate` in a testing pipeline, then enable server-side
validation once the data is clean.

**Q: What happens to existing resources when I enable validation?**
Nothing — validation only applies to new creates and updates. Existing resources in the
database are not re-validated. This means you can have non-conformant data already stored.

**Q: Can I validate against multiple profiles?**
Yes. A resource can declare multiple profiles in `meta.profile`, and the server checks
against all of them. The `$validate` operation also accepts a `profile` parameter to
validate against a specific profile.

**Q: Why do some valid resources still get warnings?**
Warnings come from SHOULD rules (must-support fields that are recommended but not required).
A Patient without `birthDate` is technically valid but gets a warning because US Core says
you SHOULD include it. Warnings don't block creation.

**Q: What if HAPI validation is too strict for my data?**
You can configure HAPI's validation severity levels, or keep server-side validation off
and use `$validate` selectively. Some teams validate in a pre-processing pipeline rather
than at the server level.

---

## Summary

- **Validation** checks resources against profile constraints before accepting them
- Enable with `hapi.fhir.validation.requests_enabled: "true"` in docker-compose
- **`$validate`** is a dry-run operation — checks without saving, always available
- Failed validation returns an **OperationOutcome** with severity, code, and diagnostics
- Only `error` and `fatal` block resource creation; `warning` and `information` don't
- Validation only triggers for resources that declare `meta.profile`
- Existing resources are NOT retroactively validated

## Next Step

Proceed to **Step 9** in the learning plan — use the `ValidationService` to validate
resources against US Core profiles using `$validate`, test with both conformant and
non-conformant resources, and examine the OperationOutcome responses.
