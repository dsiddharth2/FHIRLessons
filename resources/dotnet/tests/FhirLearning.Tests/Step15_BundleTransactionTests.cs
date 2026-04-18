using FhirLearning.Services;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Serialization;
using Xunit.Abstractions;
using Task = System.Threading.Tasks.Task;

namespace FhirLearning.Tests;

public class Step15_BundleTransactionTests : IClassFixture<FhirServerFixture>
{
    private readonly FhirServerFixture _fixture;
    private readonly BundleService _bundleService;
    private readonly PatientService _patientService;
    private readonly ITestOutputHelper _output;

    public Step15_BundleTransactionTests(FhirServerFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _bundleService = new BundleService(fixture.Client);
        _patientService = new PatientService(fixture.Client);
        _output = output;
    }

    // --- Transaction Bundle: Clinical Encounter ---

    [Fact]
    public async Task ClinicalEncounterBundle_CreatesAllResources()
    {
        var response = await _bundleService.CreateClinicalEncounterBundleAsync(
            "BundleTest", "Alice",
            AdministrativeGender.Female, "1988-06-20",
            $"MRN-BND-{Guid.NewGuid():N}",
            DateTimeOffset.UtcNow.AddHours(-2),
            DateTimeOffset.UtcNow.AddHours(-1),
            systolicBp: 125, diastolicBp: 80);

        _output.WriteLine($"Transaction response type: {response.Type}");
        _output.WriteLine($"Entries: {response.Entry.Count}");

        Assert.Equal(Bundle.BundleType.TransactionResponse, response.Type);
        Assert.Equal(3, response.Entry.Count);
    }

    [Fact]
    public async Task ClinicalEncounterBundle_AllEntriesReturn201()
    {
        var response = await _bundleService.CreateClinicalEncounterBundleAsync(
            "BundleTest", "Bob",
            AdministrativeGender.Male, "1975-03-10",
            $"MRN-BND-{Guid.NewGuid():N}",
            DateTimeOffset.UtcNow.AddHours(-2),
            DateTimeOffset.UtcNow.AddHours(-1),
            systolicBp: 140, diastolicBp: 90);

        foreach (var entry in response.Entry)
        {
            _output.WriteLine($"  {entry.Response?.Location} — {entry.Response?.Status}");
            Assert.Contains("201", entry.Response?.Status);
        }
    }

    [Fact]
    public async Task ClinicalEncounterBundle_ParsesResponseCorrectly()
    {
        var response = await _bundleService.CreateClinicalEncounterBundleAsync(
            "BundleTest", "Carol",
            AdministrativeGender.Female, "1995-12-01",
            $"MRN-BND-{Guid.NewGuid():N}",
            DateTimeOffset.UtcNow.AddHours(-2),
            DateTimeOffset.UtcNow.AddHours(-1),
            systolicBp: 118, diastolicBp: 75);

        var entries = BundleService.ParseTransactionResponse(response);

        _output.WriteLine("Parsed response entries:");
        foreach (var entry in entries)
            _output.WriteLine($"  {entry.ResourceType}/{entry.ResourceId} — {entry.Status}");

        Assert.Equal(3, entries.Count);
        Assert.Equal("Patient", entries[0].ResourceType);
        Assert.Equal("Encounter", entries[1].ResourceType);
        Assert.Equal("Observation", entries[2].ResourceType);
    }

    [Fact]
    public async Task ClinicalEncounterBundle_ResourcesAreLinked()
    {
        var response = await _bundleService.CreateClinicalEncounterBundleAsync(
            "BundleTest", "Dave",
            AdministrativeGender.Male, "1960-08-25",
            $"MRN-BND-{Guid.NewGuid():N}",
            DateTimeOffset.UtcNow.AddHours(-2),
            DateTimeOffset.UtcNow.AddHours(-1),
            systolicBp: 150, diastolicBp: 95);

        var entries = BundleService.ParseTransactionResponse(response);
        var patientId = entries[0].ResourceId;
        var encounterId = entries[1].ResourceId;

        var encounter = await _fixture.Client.ReadAsync<Encounter>($"Encounter/{encounterId}");
        Assert.Equal($"Patient/{patientId}", encounter.Subject.Reference);

        var observationBundle = await _fixture.Client.SearchAsync<Observation>(
            new SearchParams().Add("encounter", $"Encounter/{encounterId}"));
        Assert.NotEmpty(observationBundle.Entry);

        var observation = (Observation)observationBundle.Entry[0].Resource;
        Assert.Equal($"Patient/{patientId}", observation.Subject.Reference);
        Assert.Equal($"Encounter/{encounterId}", observation.Encounter.Reference);

        _output.WriteLine($"Patient/{patientId} → Encounter/{encounterId} → Observation linked");
    }

    [Fact]
    public async Task ClinicalEncounterBundle_ObservationHasCorrectBp()
    {
        var response = await _bundleService.CreateClinicalEncounterBundleAsync(
            "BundleTest", "Eve",
            AdministrativeGender.Female, "2000-01-15",
            $"MRN-BND-{Guid.NewGuid():N}",
            DateTimeOffset.UtcNow.AddHours(-2),
            DateTimeOffset.UtcNow.AddHours(-1),
            systolicBp: 132, diastolicBp: 88);

        var entries = BundleService.ParseTransactionResponse(response);
        var encounterId = entries[1].ResourceId;

        var observationBundle = await _fixture.Client.SearchAsync<Observation>(
            new SearchParams().Add("encounter", $"Encounter/{encounterId}"));
        var observation = (Observation)observationBundle.Entry[0].Resource;

        var systolic = observation.Component
            .First(c => c.Code.Coding[0].Code == "8480-6");
        Assert.Equal(132, ((Quantity)systolic.Value).Value);

        var diastolic = observation.Component
            .First(c => c.Code.Coding[0].Code == "8462-4");
        Assert.Equal(88, ((Quantity)diastolic.Value).Value);
    }

    // --- Batch Bundle ---

    [Fact]
    public async Task BatchBundle_CreatesMultiplePatients()
    {
        var patients = new List<Resource>();
        for (var i = 0; i < 3; i++)
        {
            patients.Add(new Patient
            {
                Meta = new Meta
                {
                    Profile = ["http://hl7.org/fhir/us/core/StructureDefinition/us-core-patient"]
                },
                Identifier = [new Identifier("http://hospital.example.org/mrn", $"MRN-BATCH-{Guid.NewGuid():N}")],
                Name = [new HumanName { Family = "BatchTest", Given = [$"Patient{i + 1}"] }],
                Gender = AdministrativeGender.Unknown
            });
        }

        var response = await _bundleService.CreateBatchBundleAsync(patients);

        _output.WriteLine($"Batch response type: {response.Type}");
        _output.WriteLine($"Entries: {response.Entry.Count}");

        Assert.Equal(Bundle.BundleType.BatchResponse, response.Type);
        Assert.Equal(3, response.Entry.Count);

        foreach (var entry in response.Entry)
        {
            _output.WriteLine($"  {entry.Response?.Location} — {entry.Response?.Status}");
            Assert.Contains("201", entry.Response?.Status);
        }
    }

    // --- Transaction vs Batch ---

    [Fact]
    public async Task TransactionResponse_HasCorrectType()
    {
        var response = await _bundleService.CreateClinicalEncounterBundleAsync(
            "TypeTest", "Frank",
            AdministrativeGender.Male, "1990-04-18",
            $"MRN-BND-{Guid.NewGuid():N}",
            DateTimeOffset.UtcNow.AddHours(-2),
            DateTimeOffset.UtcNow.AddHours(-1),
            systolicBp: 120, diastolicBp: 80);

        Assert.Equal(Bundle.BundleType.TransactionResponse, response.Type);
    }

    [Fact]
    public async Task BatchResponse_HasCorrectType()
    {
        var resources = new List<Resource>
        {
            new Patient
            {
                Meta = new Meta
                {
                    Profile = ["http://hl7.org/fhir/us/core/StructureDefinition/us-core-patient"]
                },
                Identifier = [new Identifier("http://hospital.example.org/mrn", $"MRN-BTYPE-{Guid.NewGuid():N}")],
                Name = [new HumanName { Family = "TypeTest", Given = ["BatchCheck"] }],
                Gender = AdministrativeGender.Female
            }
        };

        var response = await _bundleService.CreateBatchBundleAsync(resources);

        Assert.Equal(Bundle.BundleType.BatchResponse, response.Type);
    }

    // --- Verify urn:uuid References Resolved ---

    [Fact]
    public async Task Transaction_UrnUuidReferencesResolvedToServerIds()
    {
        var response = await _bundleService.CreateClinicalEncounterBundleAsync(
            "RefTest", "Grace",
            AdministrativeGender.Female, "1985-09-30",
            $"MRN-BND-{Guid.NewGuid():N}",
            DateTimeOffset.UtcNow.AddHours(-2),
            DateTimeOffset.UtcNow.AddHours(-1),
            systolicBp: 128, diastolicBp: 82);

        var entries = BundleService.ParseTransactionResponse(response);

        foreach (var entry in entries)
        {
            Assert.NotNull(entry.ResourceId);
            Assert.DoesNotContain("urn:uuid", entry.ResourceId);
            _output.WriteLine($"  {entry.ResourceType}/{entry.ResourceId}");
        }
    }

    [Fact]
    public async Task Transaction_CreatedResourcesAreReadable()
    {
        var response = await _bundleService.CreateClinicalEncounterBundleAsync(
            "ReadTest", "Heidi",
            AdministrativeGender.Female, "1992-02-14",
            $"MRN-BND-{Guid.NewGuid():N}",
            DateTimeOffset.UtcNow.AddHours(-2),
            DateTimeOffset.UtcNow.AddHours(-1),
            systolicBp: 115, diastolicBp: 72);

        var entries = BundleService.ParseTransactionResponse(response);

        var patient = await _fixture.Client.ReadAsync<Patient>($"Patient/{entries[0].ResourceId}");
        Assert.Equal("ReadTest", patient.Name[0].Family);

        var encounter = await _fixture.Client.ReadAsync<Encounter>($"Encounter/{entries[1].ResourceId}");
        Assert.Equal(Encounter.EncounterStatus.Finished, encounter.Status);

        _output.WriteLine("All created resources are individually readable");
    }
}
