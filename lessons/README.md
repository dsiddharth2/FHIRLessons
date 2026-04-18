# FHIR Learning Lessons — Index

## All Lessons

| # | Lesson | Plan Step | Topic | Status |
|---|--------|-----------|-------|--------|
| 01 | [Resources & Encounters](01-resources-and-encounters.md) | Step 1 | FHIR basics, spin up HAPI server, first resources | Done |
| 02 | [FHIR Servers & APIs](02-fhir-servers-and-apis.md) | — | How FHIR servers work, REST API patterns | Done |
| 03 | [FHIR Security & SMART](03-fhir-security-and-smart.md) | — | Authentication, authorization, SMART on FHIR | Done |
| 04 | [Create Patient (US Core)](04-create-patient-us-core.md) | Step 2 | Create a Patient with US Core profile, identifiers, extensions | Done |
| 05 | [Update Patient Demographics](05-update-patient-demographics.md) | Step 3 | PUT updates, version history, marital status | Done |
| 06 | [Encounters Deep Dive](06-encounters-deep-dive.md) | Step 4 | Encounter types, status lifecycle, class codes, period | Done |
| 07 | [Observations Deep Dive](07-observations-deep-dive.md) | Step 5 | Observation value types, LOINC, components, reference ranges | Done |
| 08 | [Conditions Deep Dive](08-conditions-deep-dive.md) | Step 6 | Diagnoses, clinical/verification status, SNOMED + ICD-10 dual coding | Done |
| 09 | [Procedures Deep Dive](09-procedures-deep-dive.md) | Step 7 | Procedures, performed[x], reasonReference, CPT codes | Done |
| 10 | [Implementation Guides](10-implementation-guides.md) | Step 8 | US Core IG, StructureDefinitions, ValueSets, STORE_AND_INSTALL | Done |
| 11 | [Validation Against US Core](11-validation-against-us-core.md) | Step 9 | $validate, OperationOutcome, terminology services, VSAC | Done |
| 12 | [Questionnaires (PROM)](12-questionnaires-prom.md) | Step 10 | Questionnaire resource, item types, answer options, PROMs | Done |
| 13 | [QuestionnaireResponse](13-questionnaire-response.md) | Step 11 | Capturing patient answers, linkId matching, answer types | Done |
| 14 | [PlanDefinition & ActivityDefinition](14-plan-definition.md) | Step 12 | Care protocols, actions, ActivityDefinition kinds, timing | Done |
| 15 | [CarePlan — $apply](15-careplan-apply.md) | Step 13 | Patient-specific plans from templates, activity mapping | Done |
| 16 | [Activity Schedule & Lifecycle](16-activity-schedule.md) | Step 14 | Status updates, workflow simulation, plan completion | Done |
| 17 | [Bundle Transactions](17-bundle-transactions.md) | Step 15 | Atomic multi-resource operations, urn:uuid references, batch | Done |
| 18 | [Bulk Data Export](18-bulk-export.md) | Step 16 | $export concept, $everything, paginated search, NDJSON | Done |

## .NET Services

Each step has a corresponding service in `resources/dotnet/src/FhirLearning.Services/`:

| Service | What it does |
|---------|-------------|
| `FhirClientFactory.cs` | Creates configured FhirClient pointing to localhost:8080 |
| `PatientService.cs` | Create, read, update, search, history for US Core Patient |
| `EncounterService.cs` | Create ambulatory encounters, search by patient |
| `ObservationService.cs` | Create blood pressure observations with LOINC codes |
| `ConditionService.cs` | Create diagnoses with SNOMED + ICD-10 dual coding |
| `ProcedureService.cs` | Create procedures linked to conditions via reasonReference |
| `ImplementationGuideService.cs` | Query StructureDefinitions, ValueSets, CodeSystems |
| `ValidationService.cs` | $validate resources, parse OperationOutcome errors/warnings |
| `TerminologyService.cs` | Upload CodeSystems, check terminology indexing |
| `QuestionnaireService.cs` | Create PROM questionnaires with groups and answer options |
| `QuestionnaireResponseService.cs` | Create completed responses with typed answers |
| `ActivityDefinitionService.cs` | Create ServiceRequest and Task activity definitions |
| `PlanDefinitionService.cs` | Create clinical protocols with multiple actions |
| `CarePlanService.cs` | Create patient-specific plans, update activity statuses, track completion |
| `BundleService.cs` | Transaction and batch bundles, urn:uuid references, response parsing |
| `BulkExportService.cs` | $everything, paginated export, incremental sync, NDJSON conversion |

## .NET Tests

Each step has corresponding tests in `resources/dotnet/tests/FhirLearning.Tests/`:

| Test File | Step | Tests | Covers |
|-----------|------|-------|--------|
| `Step1_ServerConnectivityTests.cs` | 1 | 2 | HAPI server connection, CapabilityStatement |
| `Step2_CreatePatientTests.cs` | 2 | 7 | US Core Patient creation, read, search by name/gender/identifier |
| `Step3_UpdatePatientTests.cs` | 3 | 4 | Demographics update, version history, marital status |
| `Step4_CreateEncounterTests.cs` | 4 | 7 | Encounter creation, patient reference, search |
| `Step5_AddObservationTests.cs` | 5 | 4 | Blood pressure components, LOINC codes, units, references |
| `Step6_CreateConditionTests.cs` | 6 | 10 | Dual coding, status, category, onset, read-back, multiple conditions |
| `Step7_CreateProcedureTests.cs` | 7 | 10 | SNOMED code, references, status, read-back, full clinical story |
| `Step8_ImplementationGuideTests.cs` | 8 | 10 | US Core profiles loaded, Patient/Condition/Procedure/VitalSigns profiles, ValueSets |
| `Step9_ValidationTests.cs` | 9 | 9 | Valid/invalid patients, missing fields, Condition/Observation validation |
| `Step10_QuestionnaireTests.cs` | 10 | 11 | Questionnaire structure, item types, answer options, search |
| `Step11_QuestionnaireResponseTests.cs` | 11 | 14 | Typed answers, linkId matching, patient/encounter linking, search |
| `Step12_PlanDefinitionTests.cs` | 12 | 21 | ActivityDefinitions, multi-action PlanDefinition, search |
| `Step13_CarePlanTests.cs` | 13 | 17 | CarePlan from PlanDefinition, activities, schedule display |
| `Step14_ActivityScheduleTests.cs` | 14 | 12 | Status updates, completion tracking, full workflow simulation |
| `Step15_BundleTransactionTests.cs` | 15 | 10 | Transaction atomicity, urn:uuid resolution, batch, linked resources |
| `Step16_BulkExportTests.cs` | 16 | 12 | $everything, paginated export, _lastUpdated, NDJSON format |
| | | **156 total** | |

## How to Run

```bash
# Start HAPI FHIR server
cd resources
docker compose up -d

# Wait ~2 minutes for first startup (downloads US Core IG)

# Run all tests
cd resources/dotnet
dotnet test

# Run tests for a specific step
dotnet test --filter "Step6"
```
