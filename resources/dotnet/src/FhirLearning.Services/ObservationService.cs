using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;

namespace FhirLearning.Services;

public class ObservationService
{
    private readonly FhirClient _client;

    public ObservationService(FhirClient client)
    {
        _client = client;
    }

    public async Task<Observation> CreateBloodPressureAsync(
        string patientId,
        string encounterId,
        decimal systolic,
        decimal diastolic,
        DateTimeOffset effectiveDateTime)
    {
        var observation = new Observation
        {
            Status = ObservationStatus.Final,
            Category =
            [
                new CodeableConcept("http://terminology.hl7.org/CodeSystem/observation-category",
                    "vital-signs", "Vital Signs")
            ],
            Code = new CodeableConcept("http://loinc.org", "85354-9",
                "Blood pressure panel with all children optional"),
            Subject = new ResourceReference($"Patient/{patientId}"),
            Encounter = new ResourceReference($"Encounter/{encounterId}"),
            Effective = new FhirDateTime(effectiveDateTime),
            Component =
            [
                new Observation.ComponentComponent
                {
                    Code = new CodeableConcept("http://loinc.org", "8480-6", "Systolic blood pressure"),
                    Value = new Quantity(systolic, "mm[Hg]", "http://unitsofmeasure.org")
                },
                new Observation.ComponentComponent
                {
                    Code = new CodeableConcept("http://loinc.org", "8462-4", "Diastolic blood pressure"),
                    Value = new Quantity(diastolic, "mm[Hg]", "http://unitsofmeasure.org")
                }
            ]
        };

        return await _client.CreateAsync(observation);
    }

    public async Task<Observation> ReadObservationAsync(string id)
    {
        return await _client.ReadAsync<Observation>($"Observation/{id}");
    }
}
