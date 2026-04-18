# Lesson 10: Implementation Guides — Loading US Core into HAPI

## Overview

Up to now, your HAPI server has accepted anything you threw at it. You declared
`meta.profile: us-core-patient` on your Patients, but the server never actually *checked*
whether your Patients conform to US Core. It was a label with no enforcement.

This lesson is about **Implementation Guides (IGs)** — what they are, what they contain,
and how to load one (US Core) into HAPI so the server actually knows the rules.

---

## What Is an Implementation Guide?

An Implementation Guide is a **package of rules** that tells a FHIR server (and developers)
how to use FHIR for a specific purpose. Think of base FHIR as the language and an IG as
the dialect.

Base FHIR is deliberately loose — a Patient resource has very few required fields. An IG
tightens that up for a specific context:

```
Base FHIR Patient         US Core Patient (IG adds constraints)
-------------------       -----------------------------------
name: optional       -->  name: REQUIRED (at least one)
gender: optional     -->  gender: REQUIRED
identifier: optional -->  identifier: REQUIRED (at least one)
race: doesn't exist  -->  race extension: SHOULD have
ethnicity: doesn't   -->  ethnicity extension: SHOULD have
  exist
```

### What's Inside an IG Package?

An IG is distributed as an NPM-style package (`.tgz`) containing FHIR resources that
define the rules:

| Resource Type | What It Does | Example |
|--------------|-------------|---------|
| **StructureDefinition** | Defines profiles — what fields are required, what types are allowed, what extensions exist | `us-core-patient`, `us-core-vital-signs` |
| **ValueSet** | Defines allowed sets of codes | "US Core Birth Sex" (M, F, UNK) |
| **CodeSystem** | Defines the codes themselves | CDC Race & Ethnicity code system |
| **SearchParameter** | Defines custom search parameters | Search patients by race |
| **CapabilityStatement** | Describes what a compliant server must support | Required search parameters, operations |
| **OperationDefinition** | Defines custom operations | `$docref` for document references |

### The US Core IG

US Core (`hl7.fhir.us.core`) is THE implementation guide for healthcare in the United
States. It's required by the ONC for certified health IT systems.

**Version we're using:** 6.1.0

**What it defines:**
- 23+ profiles (Patient, Encounter, Condition, Observation, Procedure, etc.)
- Required and must-support elements for each profile
- Search parameters that servers must implement
- US-specific extensions (race, ethnicity, birth sex)
- Value sets for US healthcare (ICD-10-CM, CPT, etc.)

**Profile naming pattern:** `http://hl7.org/fhir/us/core/StructureDefinition/us-core-{resource}`

| Profile URL | For Resource |
|------------|-------------|
| `.../us-core-patient` | Patient |
| `.../us-core-encounter` | Encounter |
| `.../us-core-condition-encounter-diagnosis` | Condition (encounter diagnosis) |
| `.../us-core-condition-problems-health-concerns` | Condition (problem list) |
| `.../us-core-observation-lab` | Observation (lab results) |
| `.../us-core-vital-signs` | Observation (vitals) |
| `.../us-core-blood-pressure` | Observation (blood pressure specifically) |
| `.../us-core-smokingstatus` | Observation (smoking status) |
| `.../us-core-procedure` | Procedure |

---

## How HAPI Loads an IG

Our `docker-compose.yml` already configures HAPI to load US Core at startup:

```yaml
hapi.fhir.implementationguides.uscore.packageUrl: "https://packages.simplifier.net/hl7.fhir.us.core/6.1.0"
hapi.fhir.implementationguides.uscore.name: "hl7.fhir.us.core"
hapi.fhir.implementationguides.uscore.version: "6.1.0"
hapi.fhir.implementationguides.uscore.installMode: STORE_AND_INSTALL

hapi.fhir.implementationguides.cdcrec.packageUrl: "https://packages.simplifier.net/hl7.terminology.r4/6.2.0"
hapi.fhir.implementationguides.cdcrec.name: "hl7.terminology.r4"
hapi.fhir.implementationguides.cdcrec.version: "6.2.0"
hapi.fhir.implementationguides.cdcrec.installMode: STORE_AND_INSTALL
```

**Important:** The `installMode: STORE_AND_INSTALL` setting is critical. Without it, HAPI
downloads the packages into its internal cache but does NOT save the StructureDefinitions,
ValueSets, and CodeSystems as searchable resources in the database. With `STORE_AND_INSTALL`,
the resources are both stored (searchable via REST) and installed (available for validation).

Two packages are loaded:
1. **hl7.fhir.us.core** — the US Core profiles, extensions, and search parameters
2. **hl7.terminology.r4** — the terminology (CodeSystems and ValueSets) that US Core
   depends on, including the CDC Race & Ethnicity codes

When HAPI starts, it downloads these packages from Simplifier (the FHIR package registry),
unpacks them, and loads all the contained resources (StructureDefinitions, ValueSets, etc.)
into its database. This takes a while on first boot — you may have noticed the server
takes longer to start the first time.

### What Validation Is Currently Disabled

Notice this line in docker-compose:

```yaml
hapi.fhir.validation.requests_enabled: "false"
```

This means the IG is **loaded** (the server knows about the profiles) but **not enforced**
(the server won't reject resources that violate the profiles). We'll enable validation
in the next lesson.

---

## Verifying the IG Is Loaded

Once the IG is loaded, all its resources are queryable like any other FHIR resource.

### Query StructureDefinitions (Profiles)

```bash
# Get a specific profile by exact URL
curl "http://localhost:8080/fhir/StructureDefinition?url=http://hl7.org/fhir/us/core/StructureDefinition/us-core-patient"

# List all StructureDefinitions (then filter client-side for "us-core")
# Note: HAPI does NOT support url:contains — you must use exact URL match or fetch all
curl "http://localhost:8080/fhir/StructureDefinition?_count=100&_elements=url"

# Count how many StructureDefinitions exist
curl "http://localhost:8080/fhir/StructureDefinition?_summary=count"
```

### Query ValueSets

```bash
# Get a specific value set by exact URL
curl "http://localhost:8080/fhir/ValueSet?url=http://hl7.org/fhir/us/core/ValueSet/birthsex"

# Check if a specific value set is loaded (birth sex)
curl "http://localhost:8080/fhir/ValueSet?url=http://hl7.org/fhir/us/core/ValueSet/birthsex"
```

### Query CodeSystems

```bash
# Check if the CDC Race & Ethnicity code system is loaded
curl "http://localhost:8080/fhir/CodeSystem?url=urn:oid:2.16.840.1.113883.6.238"
```

### Using the $validate-code Operation

Once the terminology is loaded, you can ask the server if a code is valid:

```bash
# Is "2106-3" a valid code in the CDC race system?
curl "http://localhost:8080/fhir/ValueSet/$validate-code?url=http://hl7.org/fhir/us/core/ValueSet/omb-race-category&code=2106-3&system=urn:oid:2.16.840.1.113883.6.238"
```

---

## StructureDefinitions — The Heart of an IG

A StructureDefinition is the formal definition of a profile. It specifies:

- **Which fields are required** (`min: 1`)
- **Which fields are must-support** (must be capable of storing/returning them)
- **What data types are allowed** (e.g., value[x] can only be Quantity for vitals)
- **What value sets bind to coded fields** (e.g., gender must come from AdministrativeGender)
- **What extensions are defined** (e.g., us-core-race)
- **Cardinality constraints** (e.g., exactly one name, at most one birth date)

### Reading a StructureDefinition

When you fetch a profile, you get a large resource. The key parts are:

```json
{
  "resourceType": "StructureDefinition",
  "url": "http://hl7.org/fhir/us/core/StructureDefinition/us-core-patient",
  "name": "USCorePatientProfile",
  "status": "active",
  "kind": "resource",
  "type": "Patient",
  "baseDefinition": "http://hl7.org/fhir/StructureDefinition/Patient",
  "differential": {
    "element": [
      {
        "id": "Patient.identifier",
        "path": "Patient.identifier",
        "min": 1,
        "mustSupport": true
      },
      {
        "id": "Patient.name",
        "path": "Patient.name",
        "min": 1,
        "mustSupport": true
      },
      {
        "id": "Patient.gender",
        "path": "Patient.gender",
        "min": 1,
        "mustSupport": true
      }
    ]
  }
}
```

The `differential` section shows what US Core *changes* from base FHIR:
- `Patient.identifier` goes from `min: 0` (optional) to `min: 1` (required)
- `Patient.name` goes from `min: 0` to `min: 1`
- `Patient.gender` goes from `min: 0` to `min: 1`
- Fields marked `mustSupport: true` means a compliant system must handle them

---

## What Our Code Will Do

We'll create an `ImplementationGuideService` that queries the HAPI server to verify
the US Core IG is loaded. This doesn't create any clinical data — it inspects the
server's configuration.

Key operations:
- Search for StructureDefinitions by URL
- Search for ValueSets by URL
- Check that specific US Core profiles exist
- Read a StructureDefinition to inspect its constraints

---

## Key Concepts

### Must-Support vs. Required

These are different in FHIR:

| Concept | Meaning | Example |
|---------|---------|---------|
| **Required** (`min: 1`) | The field MUST be present, or the resource is invalid | Patient.name in US Core |
| **Must-Support** | The system must be *capable* of storing and returning this field — but it can be empty if the data isn't available | Patient.birthDate in US Core |

A field can be must-support but not required. This means: "If you have a birth date, you
must store it and return it. But if you don't have one, that's okay."

### Binding Strength

When a coded field is bound to a ValueSet, the binding has a strength:

| Strength | Meaning |
|----------|---------|
| **required** | You MUST use a code from this ValueSet. No exceptions. |
| **extensible** | You SHOULD use a code from this ValueSet. If none fits, you can use another. |
| **preferred** | This ValueSet is recommended but not enforced. |
| **example** | Just an example — use whatever codes you want. |

US Core uses `extensible` for most coded fields (like Condition.code) — use the suggested
codes when possible, but you can bring your own if needed.

### Snapshots vs. Differentials

A StructureDefinition has two views:

- **Differential** — only shows what changed from the base definition (compact, easier to read)
- **Snapshot** — shows the complete definition with all inherited fields (verbose, complete)

When you fetch a profile from HAPI, you typically get both. The differential is what you
want to look at to understand "what does US Core add?"

---

## Common Questions

**Q: Does loading the IG automatically validate incoming resources?**
No. Loading the IG puts the rules on the server. You must separately enable validation
(`hapi.fhir.validation.requests_enabled: "true"`) to enforce them. We'll do that in
the next lesson.

**Q: Can I load multiple IGs?**
Yes. HAPI supports loading multiple IGs simultaneously. IGs can depend on each other —
US Core depends on the core FHIR spec and the HL7 terminology package (which is why we
load `hl7.terminology.r4` too).

**Q: What happens if I create a resource without the IG loaded?**
The server accepts it without validation. The `meta.profile` tag is just a claim — without
the IG loaded, the server can't verify it.

**Q: Where do IGs come from?**
Most are published on the [FHIR IG Registry](https://registry.fhir.org/) and distributed
via [Simplifier](https://simplifier.net/) (packages.simplifier.net). HL7 publishes the
official ones. Organizations can create their own IGs for specific use cases.

**Q: What's the difference between hl7.fhir.us.core and hl7.terminology.r4?**
`hl7.fhir.us.core` contains the profiles and extensions (the rules about resource
structure). `hl7.terminology.r4` contains the code systems and value sets (the vocabulary).
US Core profiles reference value sets from the terminology package.

---

## Summary

- **Implementation Guides** are packages of rules that constrain base FHIR for specific contexts
- **US Core** is the US healthcare IG — required for ONC certification
- An IG contains **StructureDefinitions** (profiles), **ValueSets**, **CodeSystems**, and more
- Our HAPI server already loads US Core via docker-compose configuration
- The IG is loaded but **validation is not yet enabled** — that's the next lesson
- **Must-support** means the system must handle the field; **required** means it must be present
- You can query loaded profiles with `GET /StructureDefinition?url=<exact-profile-url>`

## Next Step

Proceed to **Step 8** in the learning plan — verify the US Core IG is loaded in your HAPI
server. Use the `ImplementationGuideService` to query for StructureDefinitions, ValueSets,
and check that the US Core profiles are present, then run the Step8 tests to confirm.
