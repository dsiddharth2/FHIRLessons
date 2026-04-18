using FhirLearning.Services;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Xunit.Abstractions;
using Task = System.Threading.Tasks.Task;

namespace FhirLearning.Tests;

public class Step6_CreateConditionTests : IClassFixture<FhirServerFixture>
{
    private readonly PatientService _patientService;
    private readonly EncounterService _encounterService;
    private readonly ConditionService _conditionService;
    private readonly ITestOutputHelper _output;

    public Step6_CreateConditionTests(FhirServerFixture fixture, ITestOutputHelper output)
    {
        _patientService = new PatientService(fixture.Client);
        _encounterService = new EncounterService(fixture.Client);
        _conditionService = new ConditionService(fixture.Client);
        _output = output;
    }

    private void PrintResource(Resource resource, string label)
    {
        _output.WriteLine($"\n=== {label} ===");
        _output.WriteLine(resource.ToJson(pretty: true));
    }

    private async Task<(Patient patient, Encounter encounter, Condition condition)> CreateFullConditionAsync()
    {
        var patient = await _patientService.CreateUsCorePatientAsync(
            "Martinez", "Robert", AdministrativeGender.Male, "1960-03-25", $"MRN-{Guid.NewGuid():N}");

        var encounter = await _encounterService.CreateAmbulatoryEncounterAsync(
            patient.Id,
            new DateTimeOffset(2024, 3, 5, 14, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2024, 3, 5, 14, 30, 0, TimeSpan.Zero),
            "38341003", "Hypertensive disorder");

        var condition = await _conditionService.CreateDiagnosisAsync(
            patient.Id, encounter.Id,
            "38341003", "Hypertensive disorder",
            "I10", "Essential (primary) hypertension",
            "2024-01-01");

        return (patient, encounter, condition);
    }

    [Fact]
    public async Task CreateCondition_ReturnsServerAssignedId()
    {
        var (patient, encounter, condition) = await CreateFullConditionAsync();

        PrintResource(patient, "Patient: Martinez, Robert");
        PrintResource(encounter, "Encounter for Patient: Martinez, Robert");
        PrintResource(condition, "Condition: Hypertension");

        Assert.NotNull(condition.Id);
        Assert.NotEmpty(condition.Id);
    }

    [Fact]
    public async Task CreateCondition_HasDualCoding_SnomedAndIcd10()
    {
        var (_, _, condition) = await CreateFullConditionAsync();

        PrintResource(condition, "Condition (checking dual coding)");

        var codings = condition.Code.Coding;
        Assert.Equal(2, codings.Count);
        Assert.Contains(codings, c => c.System == "http://snomed.info/sct" && c.Code == "38341003");
        Assert.Contains(codings, c => c.System == "http://hl7.org/fhir/sid/icd-10-cm" && c.Code == "I10");
    }

    [Fact]
    public async Task CreateCondition_IsActiveAndConfirmed()
    {
        var (_, _, condition) = await CreateFullConditionAsync();

        PrintResource(condition, "Condition (checking clinical & verification status)");

        Assert.Equal("active", condition.ClinicalStatus.Coding[0].Code);
        Assert.Equal("http://terminology.hl7.org/CodeSystem/condition-clinical",
            condition.ClinicalStatus.Coding[0].System);
        Assert.Equal("confirmed", condition.VerificationStatus.Coding[0].Code);
        Assert.Equal("http://terminology.hl7.org/CodeSystem/condition-ver-status",
            condition.VerificationStatus.Coding[0].System);
    }

    [Fact]
    public async Task CreateCondition_HasEncounterDiagnosisCategory()
    {
        var (_, _, condition) = await CreateFullConditionAsync();

        PrintResource(condition, "Condition (checking category)");

        Assert.Single(condition.Category);
        var categoryCoding = condition.Category[0].Coding[0];
        Assert.Equal("encounter-diagnosis", categoryCoding.Code);
        Assert.Equal("http://terminology.hl7.org/CodeSystem/condition-category", categoryCoding.System);
    }

    [Fact]
    public async Task CreateCondition_ReferencesPatient()
    {
        var (patient, _, condition) = await CreateFullConditionAsync();

        PrintResource(condition, "Condition (checking patient reference)");

        Assert.Equal($"Patient/{patient.Id}", condition.Subject.Reference);
    }

    [Fact]
    public async Task CreateCondition_ReferencesEncounter()
    {
        var (_, encounter, condition) = await CreateFullConditionAsync();

        PrintResource(condition, "Condition (checking encounter reference)");

        Assert.Equal($"Encounter/{encounter.Id}", condition.Encounter.Reference);
    }

    [Fact]
    public async Task CreateCondition_HasOnsetDate()
    {
        var (_, _, condition) = await CreateFullConditionAsync();

        PrintResource(condition, "Condition (checking onset date)");

        var onset = condition.Onset as FhirDateTime;
        Assert.NotNull(onset);
        Assert.Equal("2024-01-01", onset.Value);
    }

    [Fact]
    public async Task CreateCondition_CanBeReadBack()
    {
        var (_, _, condition) = await CreateFullConditionAsync();

        var readBack = await _conditionService.ReadConditionAsync(condition.Id);

        PrintResource(readBack, "Condition (read back from server)");

        Assert.Equal(condition.Id, readBack.Id);
        Assert.Equal("38341003", readBack.Code.Coding[0].Code);
        Assert.Equal("active", readBack.ClinicalStatus.Coding[0].Code);
    }

    [Fact]
    public async Task CreateCondition_WithDiabetes_HasCorrectCodes()
    {
        var patient = await _patientService.CreateUsCorePatientAsync(
            "Chen", "Linda", AdministrativeGender.Female, "1972-08-14", $"MRN-{Guid.NewGuid():N}");

        var encounter = await _encounterService.CreateAmbulatoryEncounterAsync(
            patient.Id,
            new DateTimeOffset(2024, 4, 12, 9, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2024, 4, 12, 9, 45, 0, TimeSpan.Zero),
            "44054006", "Type 2 diabetes mellitus");

        var condition = await _conditionService.CreateDiagnosisAsync(
            patient.Id, encounter.Id,
            "44054006", "Type 2 diabetes mellitus",
            "E11.9", "Type 2 diabetes mellitus without complications",
            "2024-04-12");

        PrintResource(condition, "Condition: Type 2 Diabetes");

        Assert.Contains(condition.Code.Coding, c => c.Code == "44054006");
        Assert.Contains(condition.Code.Coding, c => c.Code == "E11.9");
    }

    [Fact]
    public async Task CreateMultipleConditions_SamePatient_AllPersist()
    {
        var patient = await _patientService.CreateUsCorePatientAsync(
            "Patel", "Arun", AdministrativeGender.Male, "1955-11-30", $"MRN-{Guid.NewGuid():N}");

        var encounter = await _encounterService.CreateAmbulatoryEncounterAsync(
            patient.Id,
            new DateTimeOffset(2024, 5, 1, 8, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2024, 5, 1, 9, 0, 0, TimeSpan.Zero),
            "185349003", "Encounter for check up");

        var hypertension = await _conditionService.CreateDiagnosisAsync(
            patient.Id, encounter.Id,
            "38341003", "Hypertensive disorder",
            "I10", "Essential (primary) hypertension",
            "2024-05-01");

        var diabetes = await _conditionService.CreateDiagnosisAsync(
            patient.Id, encounter.Id,
            "44054006", "Type 2 diabetes mellitus",
            "E11.9", "Type 2 diabetes mellitus without complications",
            "2024-05-01");

        PrintResource(hypertension, "Condition 1: Hypertension");
        PrintResource(diabetes, "Condition 2: Type 2 Diabetes");

        Assert.NotEqual(hypertension.Id, diabetes.Id);

        var readHypertension = await _conditionService.ReadConditionAsync(hypertension.Id);
        var readDiabetes = await _conditionService.ReadConditionAsync(diabetes.Id);

        Assert.Equal("38341003", readHypertension.Code.Coding[0].Code);
        Assert.Equal("44054006", readDiabetes.Code.Coding[0].Code);
    }
}
