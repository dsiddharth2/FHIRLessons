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
| 12 | | Step 10 | Create a PROM Questionnaire | Coming up |
| 13 | | Step 11 | Create a QuestionnaireResponse | Coming up |
| 14 | | Step 12 | Create a PlanDefinition with survey activity | Coming up |
| 15 | | Step 13 | Apply PlanDefinition to generate a CarePlan | Coming up |
| 16 | | Step 14 | Display schedule of activities from CarePlan | Coming up |
| 17 | | Step 15 | Bundle transactions (atomic multi-resource operations) | Coming up |
| 18 | | Step 16 | Bulk data export ($export in NDJSON) | Coming up |

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
| | | **59 total** | |

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
