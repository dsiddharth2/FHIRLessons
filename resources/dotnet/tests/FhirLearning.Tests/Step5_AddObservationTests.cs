using FhirLearning.Services;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Xunit.Abstractions;
using Task = System.Threading.Tasks.Task;

namespace FhirLearning.Tests;

public class Step5_AddObservationTests : IClassFixture<FhirServerFixture>
{
    private readonly PatientService _patientService;
    private readonly EncounterService _encounterService;
    private readonly ObservationService _observationService;
    private readonly ITestOutputHelper _output;

    public Step5_AddObservationTests(FhirServerFixture fixture, ITestOutputHelper output)
    {
        _patientService = new PatientService(fixture.Client);
        _encounterService = new EncounterService(fixture.Client);
        _observationService = new ObservationService(fixture.Client);
        _output = output;
    }

    private void PrintResource(Resource resource, string label)
    {
        _output.WriteLine($"\n=== {label} ===");
        _output.WriteLine(resource.ToJson(pretty: true));
    }

    private async Task<(Patient patient, Encounter encounter, Observation bp)> CreateFullBpObservationAsync()
    {
        var patient = await _patientService.CreateUsCorePatientAsync(
            "Lee", "Susan", AdministrativeGender.Female, "1965-12-01", $"MRN-{Guid.NewGuid():N}");

        var encounter = await _encounterService.CreateAmbulatoryEncounterAsync(
            patient.Id,
            new DateTimeOffset(2024, 2, 10, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2024, 2, 10, 10, 30, 0, TimeSpan.Zero),
            "38341003", "Hypertensive disorder");

        var bp = await _observationService.CreateBloodPressureAsync(
            patient.Id, encounter.Id, 140m, 90m,
            new DateTimeOffset(2024, 2, 10, 10, 15, 0, TimeSpan.Zero));

        return (patient, encounter, bp);
    }

    [Fact]
    public async Task CreateBP_HasTwoComponents()
    {
        var (patient, encounter, bp) = await CreateFullBpObservationAsync();

        PrintResource(patient, "Patient: Lee, Susan");
        PrintResource(encounter, "Encounter for Patient: Lee, Susan");
        PrintResource(bp, "Observation: Blood Pressure 140/90");

        Assert.Equal(2, bp.Component.Count);
    }

    [Fact]
    public async Task CreateBP_ReferencesPatientAndEncounter()
    {
        var (_, _, bp) = await CreateFullBpObservationAsync();

        PrintResource(bp, "Observation (checking patient & encounter references)");

        Assert.StartsWith("Patient/", bp.Subject.Reference);
        Assert.StartsWith("Encounter/", bp.Encounter.Reference);
    }

    [Fact]
    public async Task CreateBP_HasCorrectLOINCCode()
    {
        var (_, _, bp) = await CreateFullBpObservationAsync();

        PrintResource(bp, "Observation (checking LOINC codes)");

        Assert.Equal("85354-9", bp.Code.Coding[0].Code);
        Assert.Equal("8480-6", bp.Component[0].Code.Coding[0].Code);
        Assert.Equal("8462-4", bp.Component[1].Code.Coding[0].Code);
    }

    [Fact]
    public async Task CreateBP_HasCorrectUnits()
    {
        var (_, _, bp) = await CreateFullBpObservationAsync();

        PrintResource(bp, "Observation (checking units)");

        var systolic = (Quantity)bp.Component[0].Value;
        var diastolic = (Quantity)bp.Component[1].Value;

        Assert.Equal(140m, systolic.Value);
        Assert.Equal("mm[Hg]", systolic.Unit);
        Assert.Equal(90m, diastolic.Value);
        Assert.Equal("mm[Hg]", diastolic.Unit);
    }
}
