using FhirLearning.Services;
using Hl7.Fhir.Model;
using Xunit.Abstractions;
using Task = System.Threading.Tasks.Task;

namespace FhirLearning.Tests;

public class Step16_BulkExportTests : IClassFixture<FhirServerFixture>
{
    private readonly FhirServerFixture _fixture;
    private readonly BulkExportService _bulkExportService;
    private readonly PatientService _patientService;
    private readonly ITestOutputHelper _output;

    public Step16_BulkExportTests(FhirServerFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _bulkExportService = new BulkExportService(fixture.Client);
        _patientService = new PatientService(fixture.Client);
        _output = output;
    }

    private async Task<Patient> CreateTestPatientWithData()
    {
        var bundleService = new BundleService(_fixture.Client);

        var response = await bundleService.CreateClinicalEncounterBundleAsync(
            "ExportTest", "Bulk",
            AdministrativeGender.Female, "1990-01-01",
            $"MRN-EXP-{Guid.NewGuid():N}",
            DateTimeOffset.UtcNow.AddHours(-2),
            DateTimeOffset.UtcNow.AddHours(-1),
            systolicBp: 120, diastolicBp: 80);

        var entries = BundleService.ParseTransactionResponse(response);
        return await _fixture.Client.ReadAsync<Patient>($"Patient/{entries[0].ResourceId}");
    }

    // --- $everything ---

    [Fact]
    public async Task PatientEverything_ReturnsBundleWithResources()
    {
        var patient = await CreateTestPatientWithData();

        var bundle = await _bulkExportService.PatientEverythingAsync(patient.Id);

        _output.WriteLine($"$everything for Patient/{patient.Id}:");
        _output.WriteLine($"  Total entries: {bundle.Entry.Count}");

        var types = bundle.Entry
            .Select(e => e.Resource.TypeName)
            .Distinct()
            .OrderBy(t => t)
            .ToList();

        _output.WriteLine($"  Resource types: {string.Join(", ", types)}");

        Assert.NotEmpty(bundle.Entry);
        Assert.Contains(bundle.Entry, e => e.Resource is Patient);
    }

    [Fact]
    public async Task PatientEverything_IncludesEncounterAndObservation()
    {
        var patient = await CreateTestPatientWithData();

        var bundle = await _bulkExportService.PatientEverythingAsync(patient.Id);

        var types = bundle.Entry.Select(e => e.Resource.TypeName).ToList();

        Assert.Contains("Patient", types);
        Assert.Contains("Encounter", types);
        Assert.Contains("Observation", types);
    }

    [Fact]
    public async Task PatientEverything_AllResourcesReferencePatient()
    {
        var patient = await CreateTestPatientWithData();

        var bundle = await _bulkExportService.PatientEverythingAsync(patient.Id);

        foreach (var entry in bundle.Entry)
        {
            if (entry.Resource is Patient)
                continue;

            if (entry.Resource is Encounter enc)
                Assert.Equal($"Patient/{patient.Id}", enc.Subject.Reference);

            if (entry.Resource is Observation obs)
                Assert.Equal($"Patient/{patient.Id}", obs.Subject.Reference);
        }
    }

    // --- Paginated Export ---

    [Fact]
    public async Task ExportResourceType_ReturnsPatients()
    {
        await CreateTestPatientWithData();

        var patients = await _bulkExportService.ExportResourceTypeAsync<Patient>(maxPages: 1);

        _output.WriteLine($"Exported {patients.Count} Patient resource(s) (1 page)");

        Assert.NotEmpty(patients);
        Assert.All(patients, p => Assert.NotNull(p.Id));
    }

    [Fact]
    public async Task ExportResourceType_ReturnsObservations()
    {
        await CreateTestPatientWithData();

        var observations = await _bulkExportService.ExportResourceTypeAsync<Observation>(maxPages: 1);

        _output.WriteLine($"Exported {observations.Count} Observation resource(s) (1 page)");

        Assert.NotEmpty(observations);
    }

    [Fact]
    public async Task ExportResourceType_PaginatesCorrectly()
    {
        var patients1Page = await _bulkExportService.ExportResourceTypeAsync<Patient>(maxPages: 1);
        var patients2Pages = await _bulkExportService.ExportResourceTypeAsync<Patient>(maxPages: 2);

        _output.WriteLine($"1 page: {patients1Page.Count} patients");
        _output.WriteLine($"2 pages: {patients2Pages.Count} patients");

        Assert.True(patients2Pages.Count >= patients1Page.Count);
    }

    // --- Export Since ---

    [Fact]
    public async Task ExportSince_ReturnsRecentResources()
    {
        var since = DateTimeOffset.UtcNow.AddHours(-1);

        await CreateTestPatientWithData();

        var bundle = await _bulkExportService.ExportSinceAsync<Patient>(since);

        _output.WriteLine($"Patients modified since {since:yyyy-MM-dd HH:mm}: {bundle.Entry.Count}");

        Assert.NotEmpty(bundle.Entry);
    }

    [Fact]
    public async Task ExportSince_FutureDateReturnsEmpty()
    {
        var futureDate = DateTimeOffset.UtcNow.AddDays(1);

        var bundle = await _bulkExportService.ExportSinceAsync<Patient>(futureDate);

        _output.WriteLine($"Patients modified since tomorrow: {bundle.Entry.Count}");

        Assert.Empty(bundle.Entry);
    }

    // --- NDJSON Conversion ---

    [Fact]
    public async Task ToNdjson_ProducesOneLinePerResource()
    {
        var patients = await _bulkExportService.ExportResourceTypeAsync<Patient>(maxPages: 1);

        var ndjson = BulkExportService.ToNdjson(patients);
        var lines = ndjson.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        _output.WriteLine($"NDJSON: {lines.Length} lines for {patients.Count} patients");
        if (lines.Length > 0)
            _output.WriteLine($"First line (truncated): {lines[0][..Math.Min(120, lines[0].Length)]}...");

        Assert.Equal(patients.Count, lines.Length);
    }

    [Fact]
    public async Task ToNdjson_EachLineIsValidJson()
    {
        var patients = await _bulkExportService.ExportResourceTypeAsync<Patient>(maxPages: 1);

        var ndjson = BulkExportService.ToNdjson(patients);
        var lines = ndjson.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines.Take(5))
        {
            Assert.Contains("\"resourceType\"", line);
            Assert.Contains("\"Patient\"", line);
            Assert.DoesNotContain("\n", line);
        }
    }

    [Fact]
    public async Task ToNdjson_NoCommasBetweenLines()
    {
        var patients = await _bulkExportService.ExportResourceTypeAsync<Patient>(maxPages: 1);

        var ndjson = BulkExportService.ToNdjson(patients);

        Assert.DoesNotContain("}\n,", ndjson);
        Assert.DoesNotContain("},\n", ndjson);
    }

    // --- Summary Display ---

    [Fact]
    public async Task DisplayExportSummary()
    {
        var patient = await CreateTestPatientWithData();
        var everything = await _bulkExportService.PatientEverythingAsync(patient.Id);

        _output.WriteLine($"\n=== Bulk Export Summary ===\n");

        _output.WriteLine($"Patient/{patient.Id} $everything:");
        var typeCounts = everything.Entry
            .GroupBy(e => e.Resource.TypeName)
            .OrderBy(g => g.Key)
            .Select(g => $"  {g.Key}: {g.Count()}")
            .ToList();

        foreach (var line in typeCounts)
            _output.WriteLine(line);

        _output.WriteLine($"\n  Total resources: {everything.Entry.Count}");

        Assert.NotEmpty(everything.Entry);
    }
}
