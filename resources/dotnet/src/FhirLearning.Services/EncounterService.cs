using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;

namespace FhirLearning.Services;

public class EncounterService
{
    private readonly FhirClient _client;

    public EncounterService(FhirClient client)
    {
        _client = client;
    }

    public async Task<Encounter> CreateAmbulatoryEncounterAsync(
        string patientId,
        DateTimeOffset start,
        DateTimeOffset end,
        string reasonCode,
        string reasonDisplay)
    {
        var encounter = new Encounter
        {
            Text = new Narrative
            {
                Status = Narrative.NarrativeStatus.Generated,
                Div = $"<div xmlns=\"http://www.w3.org/1999/xhtml\">Ambulatory encounter for Patient/{patientId}: {reasonDisplay} ({start:yyyy-MM-dd})</div>"
            },
            Status = Encounter.EncounterStatus.Finished,
            Class = new Coding("http://terminology.hl7.org/CodeSystem/v3-ActCode", "AMB", "ambulatory"),
            Type =
            [
                new CodeableConcept("http://snomed.info/sct", "185349003", "Encounter for check up")
            ],
            Subject = new ResourceReference($"Patient/{patientId}"),
            Period = new Period
            {
                StartElement = new FhirDateTime(start),
                EndElement = new FhirDateTime(end)
            },
            ReasonCode =
            [
                new CodeableConcept("http://snomed.info/sct", reasonCode, reasonDisplay)
            ]
        };

        return await _client.CreateAsync(encounter);
    }

    public async Task<Encounter> ReadEncounterAsync(string id)
    {
        return await _client.ReadAsync<Encounter>($"Encounter/{id}");
    }

    public async Task<Bundle> SearchEncountersByPatientAsync(string patientId, int? count = null)
    {
        var searchParams = new SearchParams();
        searchParams.Add("subject", $"Patient/{patientId}");

        if (count.HasValue)
            searchParams.Count = count.Value;

        return await _client.SearchAsync<Encounter>(searchParams);
    }
}
