using FhirLearning.Services;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Xunit.Abstractions;
using Task = System.Threading.Tasks.Task;

namespace FhirLearning.Tests;

public class Step2_CreatePatientTests : IClassFixture<FhirServerFixture>
{
    private readonly PatientService _patientService;
    private readonly ITestOutputHelper _output;

    public Step2_CreatePatientTests(FhirServerFixture fixture, ITestOutputHelper output)
    {
        _patientService = new PatientService(fixture.Client);
        _output = output;
    }

    private void PrintPatient(Patient patient, string label)
    {
        _output.WriteLine($"\n=== {label} ===");
        _output.WriteLine(patient.ToJson(pretty: true));
    }

    [Fact]
    public async Task CreatePatient_ReturnsPatientWithId()
    {
        var patient = await _patientService.CreateUsCorePatientAsync(
            "Smith", "John", AdministrativeGender.Male, "1990-01-15", "MRN-001");

        PrintPatient(patient, "Created Patient: Smith, John");

        Assert.NotNull(patient.Id);
        Assert.Equal("Smith", patient.Name[0].Family);
        Assert.Equal(AdministrativeGender.Male, patient.Gender);
    }

    [Fact]
    public async Task CreatePatient_CanBeReadBack()
    {
        var created = await _patientService.CreateUsCorePatientAsync(
            "Doe", "Jane", AdministrativeGender.Female, "1985-06-20", "MRN-002");

        PrintPatient(created, "Created Patient: Doe, Jane");

        var readBack = await _patientService.ReadPatientAsync(created.Id);

        PrintPatient(readBack, "Read Back Patient: Doe, Jane");

        Assert.Equal(created.Id, readBack.Id);
        Assert.Equal("Doe", readBack.Name[0].Family);
        Assert.Equal("1985-06-20", readBack.BirthDate);
    }

    [Fact]
    public async Task CreatePatient_HasUsCoreProfile()
    {
        var patient = await _patientService.CreateUsCorePatientAsync(
            "Garcia", "Maria", AdministrativeGender.Female, "1975-03-10", "MRN-003");

        PrintPatient(patient, "Created Patient: Garcia, Maria (US Core)");

        Assert.Contains("http://hl7.org/fhir/us/core/StructureDefinition/us-core-patient",
            patient.Meta.Profile);
    }

    [Fact]
    public async Task GetAllPatients_ReturnsBundleWithEntries()
    {
        await _patientService.CreateUsCorePatientAsync(
            "ListTest", "Pat", AdministrativeGender.Other, "2000-01-01", $"MRN-{Guid.NewGuid():N}");

        var bundle = await _patientService.GetAllPatientsAsync(count: 50);

        Assert.NotNull(bundle);
        Assert.NotNull(bundle.Entry);
        Assert.NotEmpty(bundle.Entry);

        _output.WriteLine($"Total patients on server: {bundle.Total ?? bundle.Entry.Count}");
        _output.WriteLine(new string('-', 80));

        foreach (var entry in bundle.Entry)
        {
            if (entry.Resource is Patient patient)
            {
                var name = patient.Name.Count > 0
                    ? $"{patient.Name[0].Family}, {string.Join(" ", patient.Name[0].Given)}"
                    : "(no name)";
                var gender = patient.Gender?.ToString() ?? "Unknown";
                var dob = patient.BirthDate ?? "Unknown";

                _output.WriteLine($"  ID: {patient.Id,-12} Name: {name,-25} Gender: {gender,-10} DOB: {dob}");
            }
        }
    }

    [Fact]
    public async Task SearchByFamilyName_ReturnsMatchingPatients()
    {
        await _patientService.CreateUsCorePatientAsync(
            "TestSearch", "Alice", AdministrativeGender.Female, "1992-04-10", "MRN-SEARCH-001");

        var bundle = await _patientService.SearchPatientsAsync(family: "TestSearch");

        Assert.NotNull(bundle);
        Assert.NotEmpty(bundle.Entry);

        _output.WriteLine($"Patients matching family='TestSearch': {bundle.Entry.Count}");
        foreach (var entry in bundle.Entry)
        {
            if (entry.Resource is Patient patient)
            {
                _output.WriteLine($"  ID: {patient.Id} — {patient.Name[0].Family}, {string.Join(" ", patient.Name[0].Given)}");
            }
        }

        Assert.All(bundle.Entry, entry =>
        {
            var patient = Assert.IsType<Patient>(entry.Resource);
            Assert.Equal("TestSearch", patient.Name[0].Family);
        });
    }

    [Fact]
    public async Task SearchByGender_ReturnsMatchingPatients()
    {
        var bundle = await _patientService.SearchPatientsAsync(gender: AdministrativeGender.Female, count: 10);

        Assert.NotNull(bundle);

        _output.WriteLine($"Female patients (up to 10): {bundle.Entry?.Count ?? 0}");
        if (bundle.Entry != null)
        {
            foreach (var entry in bundle.Entry)
            {
                if (entry.Resource is Patient patient)
                {
                    var name = patient.Name.Count > 0
                        ? $"{patient.Name[0].Family}, {string.Join(" ", patient.Name[0].Given)}"
                        : "(no name)";
                    _output.WriteLine($"  ID: {patient.Id,-12} Name: {name,-25} Gender: {patient.Gender}");
                }
            }
        }
    }

    [Fact]
    public async Task SearchByIdentifier_ReturnsSinglePatient()
    {
        var created = await _patientService.CreateUsCorePatientAsync(
            "TestById", "Bob", AdministrativeGender.Male, "1988-11-30", "MRN-SEARCH-002");

        var bundle = await _patientService.SearchPatientsAsync(identifier: "MRN-SEARCH-002");

        Assert.NotNull(bundle);
        Assert.NotEmpty(bundle.Entry);

        var patient = Assert.IsType<Patient>(bundle.Entry[0].Resource);
        _output.WriteLine($"Found by identifier MRN-SEARCH-002:");
        _output.WriteLine($"  ID: {patient.Id}  Name: {patient.Name[0].Family}, {string.Join(" ", patient.Name[0].Given)}  DOB: {patient.BirthDate}");

        Assert.Equal("TestById", patient.Name[0].Family);
    }
}
