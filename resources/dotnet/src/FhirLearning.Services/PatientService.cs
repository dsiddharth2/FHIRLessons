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
            Text = new Narrative
            {
                Status = Narrative.NarrativeStatus.Generated,
                Div = $"<div xmlns=\"http://www.w3.org/1999/xhtml\">{given} {family}, {gender}, DOB: {birthDate}</div>"
            },
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

        // Race and ethnicity extensions are SHOULD (not required) in US Core.
        // They require NLM VSAC terminology access for validation, which isn't
        // available locally. In production, configure HAPI with VSAC credentials.

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
