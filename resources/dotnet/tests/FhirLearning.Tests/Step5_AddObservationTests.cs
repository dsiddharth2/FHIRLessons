using FhirLearning.Services;
using Hl7.Fhir.Model;
using Task = System.Threading.Tasks.Task;

namespace FhirLearning.Tests;

public class Step5_AddObservationTests : IClassFixture<FhirServerFixture>
{
    private readonly PatientService _patientService;
    private readonly EncounterService _encounterService;
    private readonly ObservationService _observationService;

    public Step5_AddObservationTests(FhirServerFixture fixture)
    {
        _patientService = new PatientService(fixture.Client);
        _encounterService = new EncounterService(fixture.Client);
        _observationService = new ObservationService(fixture.Client);
    }

    private async Task<Observation> CreateFullBpObservationAsync()
    {
        var patient = await _patientService.CreateUsCorePatientAsync(
            "Lee", "Susan", AdministrativeGender.Female, "1965-12-01", $"MRN-{Guid.NewGuid():N}");

        var encounter = await _encounterService.CreateAmbulatoryEncounterAsync(
            patient.Id,
            new DateTimeOffset(2024, 2, 10, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2024, 2, 10, 10, 30, 0, TimeSpan.Zero),
            "38341003", "Hypertensive disorder");

        return await _observationService.CreateBloodPressureAsync(
            patient.Id, encounter.Id, 140m, 90m,
            new DateTimeOffset(2024, 2, 10, 10, 15, 0, TimeSpan.Zero));
    }

    [Fact]
    public async Task CreateBP_HasTwoComponents()
    {
        var bp = await CreateFullBpObservationAsync();

        Assert.Equal(2, bp.Component.Count);
    }

    [Fact]
    public async Task CreateBP_ReferencesPatientAndEncounter()
    {
        var bp = await CreateFullBpObservationAsync();

        Assert.StartsWith("Patient/", bp.Subject.Reference);
        Assert.StartsWith("Encounter/", bp.Encounter.Reference);
    }

    [Fact]
    public async Task CreateBP_HasCorrectLOINCCode()
    {
        var bp = await CreateFullBpObservationAsync();

        Assert.Equal("85354-9", bp.Code.Coding[0].Code);
        Assert.Equal("8480-6", bp.Component[0].Code.Coding[0].Code);
        Assert.Equal("8462-4", bp.Component[1].Code.Coding[0].Code);
    }

    [Fact]
    public async Task CreateBP_HasCorrectUnits()
    {
        var bp = await CreateFullBpObservationAsync();

        var systolic = (Quantity)bp.Component[0].Value;
        var diastolic = (Quantity)bp.Component[1].Value;

        Assert.Equal(140m, systolic.Value);
        Assert.Equal("mm[Hg]", systolic.Unit);
        Assert.Equal(90m, diastolic.Value);
        Assert.Equal("mm[Hg]", diastolic.Unit);
    }
}
