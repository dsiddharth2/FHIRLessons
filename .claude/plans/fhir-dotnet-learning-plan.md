# FHIR Learning with .NET 10 + Firely SDK

## Context

Siddharth is learning FHIR hands-on. He has 5 markdown lessons and a 7-step learning plan (FHIR-Learning-Plan.md) that walks through creating clinical resources against a local HAPI FHIR server. He originally started with Python but is switching to .NET with the Firely SDK on a colleague's recommendation. The goal is a service-layer + integration-tests approach so he can learn one FHIR concept at a time by writing code and running tests.

---

## Solution Structure

All code lives under `resources/dotnet/`:

```
resources/dotnet/
  FhirLearning.sln
  src/FhirLearning.Services/
    FhirLearning.Services.csproj    (Hl7.Fhir.R4, Hl7.Fhir.Client)
    FhirClientFactory.cs
    PatientService.cs               (Steps 2-3)
    EncounterService.cs             (Step 4)
    ObservationService.cs           (Step 5)
    ConditionService.cs             (Step 6)
    ProcedureService.cs             (Step 7)
  tests/FhirLearning.Tests/
    FhirLearning.Tests.csproj       (xUnit, references Services)
    FhirServerFixture.cs
    Step1_ServerConnectivityTests.cs
    Step2_CreatePatientTests.cs
    Step3_UpdatePatientTests.cs
    Step4_CreateEncounterTests.cs
    Step5_AddObservationTests.cs
    Step6_CreateConditionTests.cs
    Step7_CreateProcedureTests.cs
```

---

## Step 0: Scaffold the .NET Solution

**Do once before starting any steps.**

1. Create solution + two projects (.NET 10):
   - `FhirLearning.Services` (classlib) - service layer
   - `FhirLearning.Tests` (xunit) - integration tests
2. Add NuGet packages to Services: `Hl7.Fhir.R4`, `Hl7.Fhir.Client`
3. Add project reference from Tests -> Services
4. Delete auto-generated Class1.cs and UnitTest1.cs

**Commands:**
```bash
cd "C:\2_WorkSpace\Learn FHIR\resources\dotnet"
dotnet new sln -n FhirLearning
dotnet new classlib -n FhirLearning.Services -o src/FhirLearning.Services -f net10.0
dotnet new xunit -n FhirLearning.Tests -o tests/FhirLearning.Tests -f net10.0
dotnet sln add src/FhirLearning.Services/FhirLearning.Services.csproj
dotnet sln add tests/FhirLearning.Tests/FhirLearning.Tests.csproj
dotnet add tests/FhirLearning.Tests reference src/FhirLearning.Services
dotnet add src/FhirLearning.Services package Hl7.Fhir.R4
dotnet add src/FhirLearning.Services package Hl7.Fhir.Client
```

**Teaches:** .NET solution structure, NuGet packages, project references.

---

## Step 0.5: Shared Infrastructure

### `FhirClientFactory.cs` (Services)
Static factory that creates a `FhirClient` pointed at `http://localhost:8080/fhir` with JSON format and `Prefer: return=representation`.

### `FhirServerFixture.cs` (Tests)
xUnit `IClassFixture` + `IAsyncLifetime`. On init, creates a `FhirClient` and calls `CapabilityStatementAsync()` to verify the server is running. All test classes share this fixture.

**Teaches:** FhirClient configuration, xUnit fixtures, CapabilityStatement.

---

## Step 1: Verify Server Connectivity

**Files:** `Step1_ServerConnectivityTests.cs` only (no service class needed)

**Tests:**
- `Server_ReturnsCapabilityStatement` - fetch metadata, assert FHIR version is 4.0.1
- `Server_SupportsPatientResource` - check the CapabilityStatement lists Patient in its supported resources

**Run:** `dotnet test --filter "Step1"`

**Teaches:** How FhirClient connects, what CapabilityStatement contains, verifying server capabilities.

---

## Step 2: Create a FHIR Patient (US Core)

**Files:** `PatientService.cs` + `Step2_CreatePatientTests.cs`

**Service methods:**
- `CreateUsCorePatientAsync(family, given, gender, birthDate, mrn)` - builds a Patient with US Core profile, MRN identifier, name, gender, birthDate, race/ethnicity extensions. Calls `client.CreateAsync(patient)`.
- `ReadPatientAsync(id)` - calls `client.ReadAsync<Patient>()`.

**Tests:**
- `CreatePatient_ReturnsPatientWithId` - verify server assigns an ID, name and gender are correct
- `CreatePatient_CanBeReadBack` - create then read, verify round-trip
- `CreatePatient_HasUsCoreProfile` - verify the profile URL is in meta.profile

**Run:** `dotnet test --filter "Step2"`

**Teaches:** Firely strongly-typed model (Patient, HumanName, Identifier), US Core extensions (race/ethnicity), `CreateAsync`/`ReadAsync`, difference between `id` and `identifier`.

---

## Step 3: Update Patient Demographics

**Files:** Extend `PatientService.cs` + `Step3_UpdatePatientTests.cs`

**New service methods:**
- `UpdateDemographicsAsync(patientId, address, phone, maritalStatus)` - reads existing patient, updates address/phone/maritalStatus, calls `client.UpdateAsync(patient)`.
- `GetPatientHistoryAsync(patientId)` - calls `client.HistoryAsync()`.

**Tests:**
- `UpdatePatient_AddressIsUpdated` - create, update address, verify new address
- `UpdatePatient_VersionIncremented` - check `meta.versionId` is "2" after update
- `UpdatePatient_HistoryShowsBothVersions` - verify `_history` returns both versions

**Run:** `dotnet test --filter "Step3"`

**Teaches:** FHIR PUT semantics (full replacement), read-modify-write pattern, `UpdateAsync`, version history, CodeableConcept (maritalStatus).

---

## Step 4: Create an Encounter

**Files:** `EncounterService.cs` + `Step4_CreateEncounterTests.cs`

**Service methods:**
- `CreateAmbulatoryEncounterAsync(patientId, start, end, reasonCode, reasonDisplay)` - builds an Encounter with status=finished, class=AMB, type=checkup (SNOMED), subject=Patient reference, period, reasonCode (SNOMED).
- `ReadEncounterAsync(id)`

**Tests:**
- `CreateEncounter_ReferencesPatient` - verify subject reference is `Patient/{id}`
- `CreateEncounter_HasCorrectClass` - verify class code is "AMB"
- `CreateEncounter_HasPeriod` - verify start and end times

**Run:** `dotnet test --filter "Step4"`

**Teaches:** ResourceReference (FHIR linking), Encounter structure (status, class, type, period), SNOMED CT coding, how encounters connect to patients.

---

## Step 5: Add an Observation (Blood Pressure)

**Files:** `ObservationService.cs` + `Step5_AddObservationTests.cs`

**Service methods:**
- `CreateBloodPressureAsync(patientId, encounterId, systolic, diastolic, effectiveDateTime)` - builds an Observation with category=vital-signs, code=LOINC 85354-9 (BP panel), two components: systolic (8480-6) and diastolic (8462-4) with Quantity values in mmHg.
- `ReadObservationAsync(id)`

**Tests:**
- `CreateBP_HasTwoComponents` - verify systolic + diastolic components
- `CreateBP_ReferencesPatientAndEncounter` - verify both references
- `CreateBP_HasCorrectLOINCCode` - verify panel code and component codes
- `CreateBP_HasCorrectUnits` - verify mmHg quantities

**Run:** `dotnet test --filter "Step5"`

**Teaches:** Multi-component observations, LOINC codes, UCUM units (Quantity), observation categories, linking to both Patient and Encounter.

---

## Step 6: Create a Condition (Hypertension)

**Files:** `ConditionService.cs` + `Step6_CreateConditionTests.cs`

**Service methods:**
- `CreateDiagnosisAsync(patientId, encounterId, snomedCode, snomedDisplay, icd10Code, icd10Display, onsetDate)` - builds a Condition with clinicalStatus=active, verificationStatus=confirmed, category=encounter-diagnosis, dual coding (SNOMED + ICD-10-CM), onset date.
- `ReadConditionAsync(id)`

**Tests:**
- `CreateCondition_HasDualCoding` - verify both SNOMED and ICD-10 codings on `.Code`
- `CreateCondition_IsActiveAndConfirmed` - verify clinical and verification status
- `CreateCondition_ReferencesEncounter` - verify encounter link
- `CreateCondition_HasOnsetDate` - verify onset

**Run:** `dotnet test --filter "Step6"`

**Teaches:** Dual coding systems (SNOMED for clinical, ICD-10 for billing), clinicalStatus vs verificationStatus, condition categories, Onset[x] polymorphism.

---

## Step 7: Create a Procedure (Capstone)

**Files:** `ProcedureService.cs` + `Step7_CreateProcedureTests.cs`

**Service methods:**
- `CreateProcedureAsync(patientId, encounterId, conditionId, snomedCode, snomedDisplay, performedDateTime)` - builds a Procedure with status=completed, SNOMED code, subject/encounter references, `reasonReference` pointing to the Condition.
- `ReadProcedureAsync(id)`

**Tests:**
- `CreateProcedure_ReferencesConditionAsReason` - verify reasonReference
- `FullClinicalStory_AllResourcesLinked` - **capstone test**: creates Patient -> Encounter -> Observation + Condition -> Procedure, verifies the entire reference chain

**Run:** `dotnet test --filter "Step7"`

**Teaches:** Procedure resource, `reasonReference` (linking procedure to justifying condition), the full FHIR reference graph, how all 7 steps form a coherent clinical story.

---

## How to Work Through This

1. **Start HAPI:** `docker compose up -d` from `resources/`
2. **One step at a time:** For each step N:
   - Create the service class (copy from plan, adjust as needed)
   - Create the test class
   - Run: `dotnet test --filter "StepN"`
   - Debug failures using HAPI web UI at `http://localhost:8080`
3. **After Step 7:** Run all tests: `dotnet test`

Each test class creates its own data from scratch -- tests are independent and can run in any order.

---

## Verification

After all steps are complete:
```bash
cd "C:\2_WorkSpace\Learn FHIR\resources\dotnet"
dotnet test
```
All 7 test classes should pass. The HAPI server at `http://localhost:8080` will contain the created resources, browsable via its web UI.
