using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;

namespace FhirLearning.Services;

public class ConditionService
{
    private readonly FhirClient _client;

    public ConditionService(FhirClient client)
    {
        _client = client;
    }

    public async Task<Condition> CreateDiagnosisAsync(
        string patientId,
        string encounterId,
        string snomedCode,
        string snomedDisplay,
        string icd10Code,
        string icd10Display,
        string onsetDate)
    {
        var condition = new Condition
        {
            Text = new Narrative
            {
                Status = Narrative.NarrativeStatus.Generated,
                Div = $"<div xmlns=\"http://www.w3.org/1999/xhtml\">{snomedDisplay} (ICD-10: {icd10Code}) for Patient/{patientId}, onset {onsetDate}</div>"
            },
            ClinicalStatus = new CodeableConcept(
                "http://terminology.hl7.org/CodeSystem/condition-clinical", "active", "Active"),
            VerificationStatus = new CodeableConcept(
                "http://terminology.hl7.org/CodeSystem/condition-ver-status", "confirmed", "Confirmed"),
            Category =
            [
                new CodeableConcept(
                    "http://terminology.hl7.org/CodeSystem/condition-category",
                    "encounter-diagnosis", "Encounter Diagnosis")
            ],
            Code = new CodeableConcept
            {
                Coding =
                [
                    new Coding("http://snomed.info/sct", snomedCode, snomedDisplay),
                    new Coding("http://hl7.org/fhir/sid/icd-10-cm", icd10Code, icd10Display)
                ]
            },
            Subject = new ResourceReference($"Patient/{patientId}"),
            Encounter = new ResourceReference($"Encounter/{encounterId}"),
            Onset = new FhirDateTime(onsetDate)
        };

        return await _client.CreateAsync(condition);
    }

    public async Task<Condition> ReadConditionAsync(string id)
    {
        return await _client.ReadAsync<Condition>($"Condition/{id}");
    }
}
