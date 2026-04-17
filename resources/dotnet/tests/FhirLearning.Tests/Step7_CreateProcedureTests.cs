using FhirLearning.Services;
using Hl7.Fhir.Model;
using Task = System.Threading.Tasks.Task;

namespace FhirLearning.Tests;

public class Step7_CreateProcedureTests : IClassFixture<FhirServerFixture>
{
    private readonly FhirServerFixture _fixture;

    public Step7_CreateProcedureTests(FhirServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CreateProcedure_ReferencesConditionAsReason()
    {
        var patientService = new PatientService(_fixture.Client);
        var encounterService = new EncounterService(_fixture.Client);
        var conditionService = new ConditionService(_fixture.Client);
        var procedureService = new ProcedureService(_fixture.Client);

        var patient = await patientService.CreateUsCorePatientAsync(
            "Clark", "Emily", AdministrativeGender.Female, "1978-09-10", $"MRN-{Guid.NewGuid():N}");

        var encounter = await encounterService.CreateAmbulatoryEncounterAsync(
            patient.Id,
            new DateTimeOffset(2024, 4, 1, 11, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2024, 4, 1, 12, 0, 0, TimeSpan.Zero),
            "38341003", "Hypertensive disorder");

        var condition = await conditionService.CreateDiagnosisAsync(
            patient.Id, encounter.Id,
            "38341003", "Hypertensive disorder",
            "I10", "Essential (primary) hypertension",
            "2024-01-01");

        var procedure = await procedureService.CreateProcedureAsync(
            patient.Id, encounter.Id, condition.Id,
            "46973005", "Blood pressure taking",
            new DateTimeOffset(2024, 4, 1, 11, 15, 0, TimeSpan.Zero));

        Assert.Contains(procedure.ReasonReference,
            r => r.Reference == $"Condition/{condition.Id}");
    }

    [Fact]
    public async Task FullClinicalStory_AllResourcesLinked()
    {
        var patientService = new PatientService(_fixture.Client);
        var encounterService = new EncounterService(_fixture.Client);
        var observationService = new ObservationService(_fixture.Client);
        var conditionService = new ConditionService(_fixture.Client);
        var procedureService = new ProcedureService(_fixture.Client);

        // 1. Create patient
        var patient = await patientService.CreateUsCorePatientAsync(
            "Johnson", "Michael", AdministrativeGender.Male, "1955-05-20", $"MRN-{Guid.NewGuid():N}");
        Assert.NotNull(patient.Id);

        // 2. Create encounter
        var encounter = await encounterService.CreateAmbulatoryEncounterAsync(
            patient.Id,
            new DateTimeOffset(2024, 5, 1, 9, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2024, 5, 1, 10, 0, 0, TimeSpan.Zero),
            "38341003", "Hypertensive disorder");
        Assert.Equal($"Patient/{patient.Id}", encounter.Subject.Reference);

        // 3. Record blood pressure observation
        var bp = await observationService.CreateBloodPressureAsync(
            patient.Id, encounter.Id, 150m, 95m,
            new DateTimeOffset(2024, 5, 1, 9, 15, 0, TimeSpan.Zero));
        Assert.Equal($"Patient/{patient.Id}", bp.Subject.Reference);
        Assert.Equal($"Encounter/{encounter.Id}", bp.Encounter.Reference);

        // 4. Create condition (diagnosis)
        var condition = await conditionService.CreateDiagnosisAsync(
            patient.Id, encounter.Id,
            "38341003", "Hypertensive disorder",
            "I10", "Essential (primary) hypertension",
            "2024-05-01");
        Assert.Equal($"Patient/{patient.Id}", condition.Subject.Reference);
        Assert.Equal($"Encounter/{encounter.Id}", condition.Encounter.Reference);

        // 5. Create procedure linked to condition
        var procedure = await procedureService.CreateProcedureAsync(
            patient.Id, encounter.Id, condition.Id,
            "46973005", "Blood pressure taking",
            new DateTimeOffset(2024, 5, 1, 9, 30, 0, TimeSpan.Zero));
        Assert.Equal($"Patient/{patient.Id}", procedure.Subject.Reference);
        Assert.Equal($"Encounter/{encounter.Id}", procedure.Encounter.Reference);
        Assert.Contains(procedure.ReasonReference,
            r => r.Reference == $"Condition/{condition.Id}");
    }
}
