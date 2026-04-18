# FHIR Lessons

Hands-on lessons for learning HL7 FHIR (Fast Healthcare Interoperability Resources) — from
spinning up a local server to creating, linking, and validating clinical resources.

## What's Inside

```
FHIRLessons/
├── FHIR-Learning-Plan.md                 # Full 16-step execution plan
├── lessons/                              # Deep-dive lesson files
│   ├── README.md                         # Lesson index with progress tracking
│   ├── 01 - 05                           # Foundations (server, patients, demographics)
│   ├── 06 - 09                           # Clinical resources (encounters, observations, conditions, procedures)
│   └── 10 - 11                           # Implementation guides & validation
└── resources/
    ├── docker-compose.yml                # HAPI FHIR server + Postgres + US Core IG
    └── dotnet/                           # .NET Firely SDK project
        ├── src/FhirLearning.Services/    # 9 service classes (Patient, Encounter, Observation, etc.)
        └── tests/FhirLearning.Tests/     # 59 tests across 9 test files (Steps 1-9)
```

## Getting Started

1. Start the FHIR server:
   ```bash
   cd resources
   docker compose up -d
   ```
   First startup takes ~2 minutes (downloads US Core IG from Simplifier).

2. Verify the server is running:
   ```bash
   curl http://localhost:8080/fhir/metadata
   ```

3. Run the .NET tests:
   ```bash
   cd resources/dotnet
   dotnet test
   ```

4. Read `FHIR-Learning-Plan.md` for the full walkthrough, and study `lessons/` for deep dives.

## Lessons

| # | Lesson | Topic |
|---|--------|-------|
| 01 | [Resources & Encounters](lessons/01-resources-and-encounters.md) | FHIR resource types, Encounters, how resources link together |
| 02 | [FHIR Servers & APIs](lessons/02-fhir-servers-and-apis.md) | What a FHIR server provides, REST operations, search |
| 03 | [FHIR Security & SMART](lessons/03-fhir-security-and-smart.md) | OAuth, SMART on FHIR, production security layers |
| 04 | [Create Patient (US Core)](lessons/04-create-patient-us-core.md) | Patient resource, US Core profile, extensions, identifiers |
| 05 | [Update Demographics](lessons/05-update-patient-demographics.md) | PUT vs PATCH, versioning, _history, CodeableConcept |
| 06 | [Encounters Deep Dive](lessons/06-encounters-deep-dive.md) | Encounter types, status lifecycle, class codes, period |
| 07 | [Observations Deep Dive](lessons/07-observations-deep-dive.md) | value[x] types, LOINC, components, reference ranges, categories |
| 08 | [Conditions Deep Dive](lessons/08-conditions-deep-dive.md) | Diagnoses, clinical/verification status, SNOMED + ICD-10, evidence |
| 09 | [Procedures Deep Dive](lessons/09-procedures-deep-dive.md) | Procedures, performed[x], reasonReference, CPT, complications |
| 10 | [Implementation Guides](lessons/10-implementation-guides.md) | US Core IG, StructureDefinitions, ValueSets, STORE_AND_INSTALL |
| 11 | [Validation Against US Core](lessons/11-validation-against-us-core.md) | $validate, OperationOutcome, terminology services, VSAC |

See [lessons/README.md](lessons/README.md) for full progress tracking, .NET services, and test details.

## Stack

| Component | Technology |
|-----------|-----------|
| FHIR Server | HAPI FHIR (Docker) with Postgres |
| FHIR Version | R4 |
| Implementation Guide | US Core 6.1.0 |
| SDK | .NET with Firely SDK |
| Tests | xUnit |

## Prerequisites

- Docker Desktop installed and running
- .NET 10 SDK (or later)
- A REST client (Postman, VS Code REST Client, or curl)
