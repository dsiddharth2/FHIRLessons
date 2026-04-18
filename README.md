# FHIR Lessons

Hands-on lessons for learning HL7 FHIR (Fast Healthcare Interoperability Resources) — from
spinning up a local server to creating, linking, and validating clinical resources,
building care plans, and working with bulk data operations.

## What's Inside

```
FHIRLessons/
├── FHIR-Learning-Plan.md                 # Full 16-step execution plan
├── lessons/                              # 18 deep-dive lesson files
│   ├── README.md                         # Lesson index with progress tracking
│   ├── 01 - 03                           # Foundations (server, APIs, security)
│   ├── 04 - 05                           # Patient (create, update demographics)
│   ├── 06 - 09                           # Clinical resources (encounters, observations, conditions, procedures)
│   ├── 10 - 11                           # Implementation guides & validation
│   ├── 12 - 13                           # Questionnaires & patient-reported outcomes
│   ├── 14 - 16                           # Care planning (PlanDefinition, CarePlan, lifecycle)
│   └── 17 - 18                           # Data operations (bundles, bulk export)
└── resources/
    ├── docker-compose.yml                # HAPI FHIR server + Postgres + US Core IG
    └── dotnet/                           # .NET Firely SDK project
        ├── src/FhirLearning.Services/    # 16 service classes
        └── tests/FhirLearning.Tests/     # 156 tests across 16 test files (Steps 1-16)
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
| 12 | [Questionnaires (PROM)](lessons/12-questionnaires-prom.md) | Questionnaire resource, item types, answer options, PROMs |
| 13 | [QuestionnaireResponse](lessons/13-questionnaire-response.md) | Capturing patient answers, linkId matching, answer types |
| 14 | [PlanDefinition & ActivityDefinition](lessons/14-plan-definition.md) | Care protocols, actions, ActivityDefinition kinds, timing |
| 15 | [CarePlan — $apply](lessons/15-careplan-apply.md) | Patient-specific plans from templates, activity mapping |
| 16 | [Activity Schedule & Lifecycle](lessons/16-activity-schedule.md) | Status updates, workflow simulation, plan completion |
| 17 | [Bundle Transactions](lessons/17-bundle-transactions.md) | Atomic multi-resource operations, urn:uuid references, batch |
| 18 | [Bulk Data Export](lessons/18-bulk-export.md) | $export concept, $everything, paginated search, NDJSON |

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
