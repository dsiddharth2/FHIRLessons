using FhirLearning.Services;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Xunit.Abstractions;
using Task = System.Threading.Tasks.Task;

namespace FhirLearning.Tests;

public class Step3_UpdatePatientTests : IClassFixture<FhirServerFixture>
{
    private readonly PatientService _patientService;
    private readonly ITestOutputHelper _output;

    public Step3_UpdatePatientTests(FhirServerFixture fixture, ITestOutputHelper output)
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
    public async Task UpdatePatient_AddressIsUpdated()
    {
        var patient = await _patientService.CreateUsCorePatientAsync(
            "Brown", "Alice", AdministrativeGender.Female, "1988-11-05", "MRN-100");

        var address = new Address
        {
            Line = ["123 Main St"],
            City = "Springfield",
            State = "IL",
            PostalCode = "62701",
            Country = "US"
        };

        PrintPatient(patient, "Before Update: Brown, Alice");

        var updated = await _patientService.UpdateDemographicsAsync(patient.Id, address: address);

        PrintPatient(updated, "After Update: Brown, Alice (address added)");

        Assert.Single(updated.Address);
        Assert.Equal("Springfield", updated.Address[0].City);
        Assert.Equal("IL", updated.Address[0].State);
    }

    [Fact]
    public async Task UpdatePatient_VersionIncremented()
    {
        var patient = await _patientService.CreateUsCorePatientAsync(
            "Wilson", "Bob", AdministrativeGender.Male, "1970-04-22", "MRN-101");

        var phone = new ContactPoint(ContactPoint.ContactPointSystem.Phone,
            ContactPoint.ContactPointUse.Home, "555-0100");

        PrintPatient(patient, "Before Update: Wilson, Bob");

        var updated = await _patientService.UpdateDemographicsAsync(patient.Id, phone: phone);

        PrintPatient(updated, "After Update: Wilson, Bob (phone added)");

        Assert.NotEqual(patient.Meta.VersionId, updated.Meta.VersionId);
    }

    [Fact]
    public async Task UpdatePatient_HistoryShowsBothVersions()
    {
        var patient = await _patientService.CreateUsCorePatientAsync(
            "Taylor", "Carol", AdministrativeGender.Female, "1995-08-12", "MRN-102");

        var maritalStatus = new CodeableConcept(
            "http://terminology.hl7.org/CodeSystem/v3-MaritalStatus", "M", "Married");

        PrintPatient(patient, "Before Update: Taylor, Carol");

        var updatedPatient = await _patientService.UpdateDemographicsAsync(patient.Id, maritalStatus: maritalStatus);

        PrintPatient(updatedPatient, "After Update: Taylor, Carol (marital status added)");

        var history = await _patientService.GetPatientHistoryAsync(patient.Id);

        Assert.True(history.Entry.Count >= 2);
    }
}
