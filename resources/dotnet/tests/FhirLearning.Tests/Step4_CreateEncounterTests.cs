using FhirLearning.Services;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Xunit.Abstractions;
using Task = System.Threading.Tasks.Task;

namespace FhirLearning.Tests;

public class Step4_CreateEncounterTests : IClassFixture<FhirServerFixture>
{
    private readonly PatientService _patientService;
    private readonly EncounterService _encounterService;
    private readonly ITestOutputHelper _output;

    public Step4_CreateEncounterTests(FhirServerFixture fixture, ITestOutputHelper output)
    {
        _patientService = new PatientService(fixture.Client);
        _encounterService = new EncounterService(fixture.Client);
        _output = output;
    }

    private void PrintResource(Resource resource, string label)
    {
        _output.WriteLine($"\n=== {label} ===");
        _output.WriteLine(resource.ToJson(pretty: true));
    }

    private async Task<(Patient patient, Encounter encounter)> CreatePatientWithEncounterAsync()
    {
        var patient = await _patientService.CreateUsCorePatientAsync(
            "Adams", "David", AdministrativeGender.Male, "1982-07-15", $"MRN-{Guid.NewGuid():N}");

        var encounter = await _encounterService.CreateAmbulatoryEncounterAsync(
            patient.Id,
            new DateTimeOffset(2024, 1, 15, 9, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2024, 1, 15, 9, 30, 0, TimeSpan.Zero),
            "38341003", "Hypertensive disorder");

        return (patient, encounter);
    }

    [Fact]
    public async Task CreateEncounter_ReferencesPatient()
    {
        var (patient, encounter) = await CreatePatientWithEncounterAsync();

        PrintResource(patient, "Patient: Adams, David");
        PrintResource(encounter, "Encounter for Patient: Adams, David");

        Assert.Equal($"Patient/{patient.Id}", encounter.Subject.Reference);
    }

    [Fact]
    public async Task CreateEncounter_HasCorrectClass()
    {
        var (_, encounter) = await CreatePatientWithEncounterAsync();

        PrintResource(encounter, "Encounter (checking class=AMB)");

        Assert.Equal("AMB", encounter.Class.Code);
    }

    [Fact]
    public async Task CreateEncounter_HasPeriod()
    {
        var (_, encounter) = await CreatePatientWithEncounterAsync();

        PrintResource(encounter, "Encounter (checking period)");

        Assert.NotNull(encounter.Period);
        Assert.NotNull(encounter.Period.Start);
        Assert.NotNull(encounter.Period.End);
    }

    [Fact]
    public async Task SearchEncountersByPatient_ReturnsEncountersForPatient()
    {
        var patient = await _patientService.CreateUsCorePatientAsync(
            "Walker", "Emma", AdministrativeGender.Female, "1990-03-22", $"MRN-{Guid.NewGuid():N}");

        PrintResource(patient, "Patient: Walker, Emma");

        var encounter1 = await _encounterService.CreateAmbulatoryEncounterAsync(
            patient.Id,
            new DateTimeOffset(2024, 2, 10, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2024, 2, 10, 10, 30, 0, TimeSpan.Zero),
            "185349003", "Encounter for check up");

        PrintResource(encounter1, "Encounter 1: Check up");

        var encounter2 = await _encounterService.CreateAmbulatoryEncounterAsync(
            patient.Id,
            new DateTimeOffset(2024, 3, 15, 14, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2024, 3, 15, 14, 45, 0, TimeSpan.Zero),
            "38341003", "Hypertensive disorder");

        PrintResource(encounter2, "Encounter 2: Hypertensive disorder follow-up");

        var bundle = await _encounterService.SearchEncountersByPatientAsync(patient.Id);

        Assert.NotNull(bundle);
        Assert.NotNull(bundle.Entry);
        Assert.True(bundle.Entry.Count >= 2);

        _output.WriteLine($"\n=== All Encounters for Patient/{patient.Id} (Walker, Emma) ===");
        _output.WriteLine($"Total: {bundle.Entry.Count}");
        _output.WriteLine(new string('-', 80));

        foreach (var entry in bundle.Entry)
        {
            if (entry.Resource is Encounter enc)
            {
                var reason = enc.ReasonCode.Count > 0
                    ? enc.ReasonCode[0].Coding[0].Display
                    : "(none)";
                _output.WriteLine($"  ID: {enc.Id,-8} Class: {enc.Class.Code,-5} Status: {enc.Status,-10} " +
                                  $"Period: {enc.Period.Start} to {enc.Period.End}  Reason: {reason}");
            }
        }
    }
}
