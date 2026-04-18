using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;

namespace FhirLearning.Services;

public class ProcedureService
{
    private readonly FhirClient _client;

    public ProcedureService(FhirClient client)
    {
        _client = client;
    }

    public async Task<Procedure> CreateProcedureAsync(
        string patientId,
        string encounterId,
        string conditionId,
        string snomedCode,
        string snomedDisplay,
        DateTimeOffset performedDateTime)
    {
        var procedure = new Procedure
        {
            Text = new Narrative
            {
                Status = Narrative.NarrativeStatus.Generated,
                Div = $"<div xmlns=\"http://www.w3.org/1999/xhtml\">{snomedDisplay} for Patient/{patientId} on {performedDateTime:yyyy-MM-dd}</div>"
            },
            Status = EventStatus.Completed,
            Code = new CodeableConcept("http://snomed.info/sct", snomedCode, snomedDisplay),
            Subject = new ResourceReference($"Patient/{patientId}"),
            Encounter = new ResourceReference($"Encounter/{encounterId}"),
            Performed = new FhirDateTime(performedDateTime),
            ReasonReference = [new ResourceReference($"Condition/{conditionId}")]
        };

        return await _client.CreateAsync(procedure);
    }

    public async Task<Procedure> ReadProcedureAsync(string id)
    {
        return await _client.ReadAsync<Procedure>($"Procedure/{id}");
    }
}
