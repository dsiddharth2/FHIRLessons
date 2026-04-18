using FhirLearning.Services;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Xunit.Abstractions;
using Task = System.Threading.Tasks.Task;

namespace FhirLearning.Tests;

public class Step7_CreateProcedureTests : IClassFixture<FhirServerFixture>
{
    private readonly PatientService _patientService;
    private readonly EncounterService _encounterService;
    private readonly ConditionService _conditionService;
    private readonly ProcedureService _procedureService;
    private readonly ObservationService _observationService;
    private readonly ITestOutputHelper _output;

    public Step7_CreateProcedureTests(FhirServerFixture fixture, ITestOutputHelper output)
    {
        _patientService = new PatientService(fixture.Client);
        _encounterService = new EncounterService(fixture.Client);
        _conditionService = new ConditionService(fixture.Client);
        _procedureService = new ProcedureService(fixture.Client);
        _observationService = new ObservationService(fixture.Client);
        _output = output;
    }

    private void PrintResource(Resource resource, string label)
    {
        _output.WriteLine($"\n=== {label} ===");
        _output.WriteLine(resource.ToJson(pretty: true));
    }

    private async Task<(Patient patient, Encounter encounter, Condition condition, Procedure procedure)>
        CreateFullProcedureAsync()
    {
        var patient = await _patientService.CreateUsCorePatientAsync(
            "Clark", "Emily", AdministrativeGender.Female, "1978-09-10", $"MRN-{Guid.NewGuid():N}");

        var encounter = await _encounterService.CreateAmbulatoryEncounterAsync(
            patient.Id,
            new DateTimeOffset(2024, 4, 1, 11, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2024, 4, 1, 12, 0, 0, TimeSpan.Zero),
            "38341003", "Hypertensive disorder");

        var condition = await _conditionService.CreateDiagnosisAsync(
            patient.Id, encounter.Id,
            "38341003", "Hypertensive disorder",
            "I10", "Essential (primary) hypertension",
            "2024-01-01");

        var procedure = await _procedureService.CreateProcedureAsync(
            patient.Id, encounter.Id, condition.Id,
            "46973005", "Blood pressure taking",
            new DateTimeOffset(2024, 4, 1, 11, 15, 0, TimeSpan.Zero));

        return (patient, encounter, condition, procedure);
    }

    [Fact]
    public async Task CreateProcedure_ReturnsServerAssignedId()
    {
        var (patient, encounter, condition, procedure) = await CreateFullProcedureAsync();

        PrintResource(patient, "Patient: Clark, Emily");
        PrintResource(encounter, "Encounter for Patient: Clark, Emily");
        PrintResource(condition, "Condition: Hypertension");
        PrintResource(procedure, "Procedure: Blood pressure taking");

        Assert.NotNull(procedure.Id);
        Assert.NotEmpty(procedure.Id);
    }

    [Fact]
    public async Task CreateProcedure_StatusIsCompleted()
    {
        var (_, _, _, procedure) = await CreateFullProcedureAsync();

        PrintResource(procedure, "Procedure (checking status)");

        Assert.Equal(EventStatus.Completed, procedure.Status);
    }

    [Fact]
    public async Task CreateProcedure_HasCorrectSnomedCode()
    {
        var (_, _, _, procedure) = await CreateFullProcedureAsync();

        PrintResource(procedure, "Procedure (checking SNOMED code)");

        var coding = procedure.Code.Coding[0];
        Assert.Equal("http://snomed.info/sct", coding.System);
        Assert.Equal("46973005", coding.Code);
        Assert.Equal("Blood pressure taking", procedure.Code.Text);
    }

    [Fact]
    public async Task CreateProcedure_ReferencesPatient()
    {
        var (patient, _, _, procedure) = await CreateFullProcedureAsync();

        PrintResource(procedure, "Procedure (checking patient reference)");

        Assert.Equal($"Patient/{patient.Id}", procedure.Subject.Reference);
    }

    [Fact]
    public async Task CreateProcedure_ReferencesEncounter()
    {
        var (_, encounter, _, procedure) = await CreateFullProcedureAsync();

        PrintResource(procedure, "Procedure (checking encounter reference)");

        Assert.Equal($"Encounter/{encounter.Id}", procedure.Encounter.Reference);
    }

    [Fact]
    public async Task CreateProcedure_ReferencesConditionAsReason()
    {
        var (_, _, condition, procedure) = await CreateFullProcedureAsync();

        PrintResource(procedure, "Procedure (checking reason reference)");

        Assert.Single(procedure.ReasonReference);
        Assert.Equal($"Condition/{condition.Id}", procedure.ReasonReference[0].Reference);
    }

    [Fact]
    public async Task CreateProcedure_HasPerformedDateTime()
    {
        var (_, _, _, procedure) = await CreateFullProcedureAsync();

        PrintResource(procedure, "Procedure (checking performed date)");

        var performed = procedure.Performed as FhirDateTime;
        Assert.NotNull(performed);
    }

    [Fact]
    public async Task CreateProcedure_CanBeReadBack()
    {
        var (_, _, condition, procedure) = await CreateFullProcedureAsync();

        var readBack = await _procedureService.ReadProcedureAsync(procedure.Id);

        PrintResource(readBack, "Procedure (read back from server)");

        Assert.Equal(procedure.Id, readBack.Id);
        Assert.Equal("46973005", readBack.Code.Coding[0].Code);
        Assert.Equal(EventStatus.Completed, readBack.Status);
        Assert.Contains(readBack.ReasonReference,
            r => r.Reference == $"Condition/{condition.Id}");
    }

    [Fact]
    public async Task CreateProcedure_WithDifferentCode_Works()
    {
        var patient = await _patientService.CreateUsCorePatientAsync(
            "Williams", "Sarah", AdministrativeGender.Female, "1985-03-22", $"MRN-{Guid.NewGuid():N}");

        var encounter = await _encounterService.CreateAmbulatoryEncounterAsync(
            patient.Id,
            new DateTimeOffset(2024, 6, 15, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2024, 6, 15, 10, 45, 0, TimeSpan.Zero),
            "44054006", "Type 2 diabetes mellitus");

        var condition = await _conditionService.CreateDiagnosisAsync(
            patient.Id, encounter.Id,
            "44054006", "Type 2 diabetes mellitus",
            "E11.9", "Type 2 diabetes mellitus without complications",
            "2024-06-15");

        var procedure = await _procedureService.CreateProcedureAsync(
            patient.Id, encounter.Id, condition.Id,
            "225358003", "Physiotherapy",
            new DateTimeOffset(2024, 6, 15, 10, 20, 0, TimeSpan.Zero));

        PrintResource(procedure, "Procedure: Physiotherapy for Diabetes");

        Assert.Equal("225358003", procedure.Code.Coding[0].Code);
        Assert.Equal($"Condition/{condition.Id}", procedure.ReasonReference[0].Reference);
    }

    [Fact]
    public async Task FullClinicalStory_AllResourcesLinked()
    {
        var patient = await _patientService.CreateUsCorePatientAsync(
            "Johnson", "Michael", AdministrativeGender.Male, "1955-05-20", $"MRN-{Guid.NewGuid():N}");

        var encounter = await _encounterService.CreateAmbulatoryEncounterAsync(
            patient.Id,
            new DateTimeOffset(2024, 5, 1, 9, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2024, 5, 1, 10, 0, 0, TimeSpan.Zero),
            "38341003", "Hypertensive disorder");

        var bp = await _observationService.CreateBloodPressureAsync(
            patient.Id, encounter.Id, 150m, 95m,
            new DateTimeOffset(2024, 5, 1, 9, 15, 0, TimeSpan.Zero));

        var condition = await _conditionService.CreateDiagnosisAsync(
            patient.Id, encounter.Id,
            "38341003", "Hypertensive disorder",
            "I10", "Essential (primary) hypertension",
            "2024-05-01");

        var procedure = await _procedureService.CreateProcedureAsync(
            patient.Id, encounter.Id, condition.Id,
            "46973005", "Blood pressure taking",
            new DateTimeOffset(2024, 5, 1, 9, 30, 0, TimeSpan.Zero));

        PrintResource(patient, "1. Patient: Johnson, Michael");
        PrintResource(encounter, "2. Encounter: Outpatient visit");
        PrintResource(bp, "3. Observation: BP 150/95");
        PrintResource(condition, "4. Condition: Hypertension");
        PrintResource(procedure, "5. Procedure: BP monitoring");

        Assert.Equal($"Patient/{patient.Id}", encounter.Subject.Reference);
        Assert.Equal($"Patient/{patient.Id}", bp.Subject.Reference);
        Assert.Equal($"Encounter/{encounter.Id}", bp.Encounter.Reference);
        Assert.Equal($"Patient/{patient.Id}", condition.Subject.Reference);
        Assert.Equal($"Encounter/{encounter.Id}", condition.Encounter.Reference);
        Assert.Equal($"Patient/{patient.Id}", procedure.Subject.Reference);
        Assert.Equal($"Encounter/{encounter.Id}", procedure.Encounter.Reference);
        Assert.Equal($"Condition/{condition.Id}", procedure.ReasonReference[0].Reference);
    }
}
