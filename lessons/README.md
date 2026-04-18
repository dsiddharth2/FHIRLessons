# FHIR Learning Lessons — Index

## Progress

| Step | Topic | Lesson | Status |
|------|-------|--------|--------|
| ~~Step 1~~ | Spin up FHIR server | [01 — Resources & Encounters](01-resources-and-encounters.md) | Done |
| ~~Step 2~~ | Create Patient (US Core) | [04 — Create Patient US Core](04-create-patient-us-core.md) | Done |
| ~~Step 3~~ | Update Patient demographics | [05 — Update Patient Demographics](05-update-patient-demographics.md) | Done |
| ~~Step 4~~ | Create Encounter | [06 — Encounters Deep Dive](06-encounters-deep-dive.md) | Done |
| ~~Step 5~~ | Add Observation | [07 — Observations Deep Dive](07-observations-deep-dive.md) | Done |
| ~~Step 6~~ | Create Condition (Diagnosis) | [08 — Conditions Deep Dive](08-conditions-deep-dive.md) | Done |
| ~~Step 7~~ | Create Procedure | [09 — Procedures Deep Dive](09-procedures-deep-dive.md) | Done |
| ~~Step 8~~ | Load US Core IG into HAPI | [10 — Implementation Guides](10-implementation-guides.md) | Done |
| ~~Step 9~~ | Validate resources against US Core | [11 — Validation Against US Core](11-validation-against-us-core.md) | Done |
| Step 10 | Create a PROM Questionnaire | Coming up | |
| Step 11 | Create a QuestionnaireResponse | Coming up | |
| Step 12 | Create a PlanDefinition with survey activity | Coming up | |
| Step 13 | Apply PlanDefinition to generate a CarePlan | Coming up | |
| Step 14 | Display schedule of activities from CarePlan | Coming up | |
| Step 15 | Bundle transactions (atomic multi-resource operations) | Coming up | |
| Step 16 | Bulk data export ($export in NDJSON) | Coming up | |

## Background Lessons

These lessons cover FHIR concepts that don't map to a specific step but provide context:

| Lesson | Topic |
|--------|-------|
| [02 — FHIR Servers & APIs](02-fhir-servers-and-apis.md) | How FHIR servers work, REST API patterns |
| [03 — FHIR Security & SMART](03-fhir-security-and-smart.md) | Authentication, authorization, SMART on FHIR |

## .NET Tests

Each step has corresponding tests in `resources/dotnet/tests/FhirLearning.Tests/`:

| Test File | Covers |
|-----------|--------|
| `Step1_ServerConnectivityTests.cs` | HAPI server connection, CapabilityStatement |
| `Step2_CreatePatientTests.cs` | US Core Patient creation, search |
| `Step3_UpdatePatientTests.cs` | Patient demographics update, version history |
| `Step4_CreateEncounterTests.cs` | Encounter creation, patient reference |
| `Step5_AddObservationTests.cs` | Blood pressure observation, LOINC codes, components |
| `Step6_CreateConditionTests.cs` | Condition creation, dual coding (SNOMED + ICD-10) |
| `Step7_CreateProcedureTests.cs` | Procedure creation, reasonReference, full clinical story |
| `Step8_ImplementationGuideTests.cs` | US Core IG loaded, profiles and value sets present |
| `Step9_ValidationTests.cs` | $validate operation, valid/invalid resource checks |
