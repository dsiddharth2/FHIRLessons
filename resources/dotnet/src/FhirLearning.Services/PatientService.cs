using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;

namespace FhirLearning.Services;

public class PatientService
{
    private readonly FhirClient _client;

    public PatientService(FhirClient client)
    {
        _client = client;
    }

    public async Task<Patient> CreateUsCorePatientAsync(
        string family,
        string given,
        AdministrativeGender gender,
        string birthDate,
        string mrn)
    {
        var patient = new Patient
        {
            Meta = new Meta
            {
                Profile = ["http://hl7.org/fhir/us/core/StructureDefinition/us-core-patient"]
            },
            Identifier =
            [
                new Identifier("http://hospital.example.org/mrn", mrn)
                {
                    Type = new CodeableConcept("http://terminology.hl7.org/CodeSystem/v2-0203", "MR")
                }
            ],
            Name =
            [
                new HumanName { Family = family, Given = [given] }
            ],
            Gender = gender,
            BirthDate = birthDate
        };

        // US Core race extension
        var raceExtension = new Extension { Url = "http://hl7.org/fhir/us/core/StructureDefinition/us-core-race" };
        raceExtension.Extension.Add(new Extension("ombCategory",
            (DataType)new Coding("urn:oid:2.16.840.1.113883.6.238", "2106-3", "White")));
        raceExtension.Extension.Add(new Extension("text", (DataType)new FhirString("White")));
        patient.Extension.Add(raceExtension);

        // US Core ethnicity extension
        var ethnicityExtension = new Extension { Url = "http://hl7.org/fhir/us/core/StructureDefinition/us-core-ethnicity" };
        ethnicityExtension.Extension.Add(new Extension("ombCategory",
            (DataType)new Coding("urn:oid:2.16.840.1.113883.6.238", "2186-5", "Not Hispanic or Latino")));
        ethnicityExtension.Extension.Add(new Extension("text", (DataType)new FhirString("Not Hispanic or Latino")));
        patient.Extension.Add(ethnicityExtension);

        return await _client.CreateAsync(patient);
    }

    public async Task<Patient> ReadPatientAsync(string id)
    {
        return await _client.ReadAsync<Patient>($"Patient/{id}");
    }

    public async Task<Patient> UpdateDemographicsAsync(
        string patientId,
        Address? address = null,
        ContactPoint? phone = null,
        CodeableConcept? maritalStatus = null)
    {
        var patient = await ReadPatientAsync(patientId);

        if (address != null)
        {
            patient.Address = [address];
        }

        if (phone != null)
        {
            patient.Telecom = [phone];
        }

        if (maritalStatus != null)
        {
            patient.MaritalStatus = maritalStatus;
        }

        return await _client.UpdateAsync(patient);
    }

    public async Task<Bundle> GetPatientHistoryAsync(string patientId)
    {
        return await _client.HistoryAsync($"Patient/{patientId}");
    }

    public async Task<Bundle> SearchPatientsAsync(
        string? family = null,
        string? given = null,
        string? birthDate = null,
        string? identifier = null,
        AdministrativeGender? gender = null,
        int? count = null)
    {
        var searchParams = new SearchParams();

        if (family != null)
            searchParams.Add("family", family);

        if (given != null)
            searchParams.Add("given", given);

        if (birthDate != null)
            searchParams.Add("birthdate", birthDate);

        if (identifier != null)
            searchParams.Add("identifier", identifier);

        if (gender != null)
            searchParams.Add("gender", gender.Value.ToString().ToLowerInvariant());

        if (count.HasValue)
            searchParams.Count = count.Value;

        return await _client.SearchAsync<Patient>(searchParams);
    }

    public async Task<Bundle> GetAllPatientsAsync(int? count = null)
    {
        var searchParams = new SearchParams();
        if (count.HasValue)
            searchParams.Count = count.Value;

        return await _client.SearchAsync<Patient>(searchParams);
    }
}
