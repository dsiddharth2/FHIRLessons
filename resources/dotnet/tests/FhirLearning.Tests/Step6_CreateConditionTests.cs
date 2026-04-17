using FhirLearning.Services;
using Hl7.Fhir.Model;
using Task = System.Threading.Tasks.Task;

namespace FhirLearning.Tests;

public class Step6_CreateConditionTests : IClassFixture<FhirServerFixture>
{
    private readonly PatientService _patientService;
    private readonly EncounterService _encounterService;
    private readonly ConditionService _conditionService;

    public Step6_CreateConditionTests(FhirServerFixture fixture)
    {
        _patientService = new PatientService(fixture.Client);
        _encounterService = new EncounterService(fixture.Client);
        _conditionService = new ConditionService(fixture.Client);
    }

    private async Task<Condition> CreateFullConditionAsync()
    {
        var patient = await _patientService.CreateUsCorePatientAsync(
            "Martinez", "Robert", AdministrativeGender.Male, "1960-03-25", $"MRN-{Guid.NewGuid():N}");

        var encounter = await _encounterService.CreateAmbulatoryEncounterAsync(
            patient.Id,
            new DateTimeOffset(2024, 3, 5, 14, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2024, 3, 5, 14, 30, 0, TimeSpan.Zero),
            "38341003", "Hypertensive disorder");

        return await _conditionService.CreateDiagnosisAsync(
            patient.Id, encounter.Id,
            "38341003", "Hypertensive disorder",
            "I10", "Essential (primary) hypertension",
            "2024-01-01");
    }

    [Fact]
    public async Task CreateCondition_HasDualCoding()
    {
        var condition = await CreateFullConditionAsync();

        var codings = condition.Code.Coding;
        Assert.Equal(2, codings.Count);
        Assert.Contains(codings, c => c.System == "http://snomed.info/sct" && c.Code == "38341003");
        Assert.Contains(codings, c => c.System == "http://hl7.org/fhir/sid/icd-10-cm" && c.Code == "I10");
    }

    [Fact]
    public async Task CreateCondition_IsActiveAndConfirmed()
    {
        var condition = await CreateFullConditionAsync();

        Assert.Equal("active", condition.ClinicalStatus.Coding[0].Code);
        Assert.Equal("confirmed", condition.VerificationStatus.Coding[0].Code);
    }

    [Fact]
    public async Task CreateCondition_ReferencesEncounter()
    {
        var condition = await CreateFullConditionAsync();

        Assert.StartsWith("Encounter/", condition.Encounter.Reference);
    }

    [Fact]
    public async Task CreateCondition_HasOnsetDate()
    {
        var condition = await CreateFullConditionAsync();

        var onset = condition.Onset as FhirDateTime;
        Assert.NotNull(onset);
        Assert.Equal("2024-01-01", onset.Value);
    }
}
