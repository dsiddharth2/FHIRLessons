using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;

namespace FhirLearning.Services;

public class BundleService
{
    private readonly FhirClient _client;

    public BundleService(FhirClient client)
    {
        _client = client;
    }

    public async Task<Bundle> CreateClinicalEncounterBundleAsync(
        string familyName,
        string givenName,
        AdministrativeGender gender,
        string birthDate,
        string mrn,
        DateTimeOffset encounterStart,
        DateTimeOffset encounterEnd,
        int systolicBp,
        int diastolicBp)
    {
        var patientId = Guid.NewGuid().ToString();
        var encounterId = Guid.NewGuid().ToString();
        var observationId = Guid.NewGuid().ToString();

        var patient = new Patient
        {
            Text = new Narrative
            {
                Status = Narrative.NarrativeStatus.Generated,
                Div = $"<div xmlns=\"http://www.w3.org/1999/xhtml\">{givenName} {familyName}, {gender}, DOB: {birthDate}</div>"
            },
            Meta = new Meta
            {
                Profile = ["http://hl7.org/fhir/us/core/StructureDefinition/us-core-patient"]
            },
            Identifier = [new Identifier("http://hospital.example.org/mrn", mrn)],
            Name = [new HumanName { Family = familyName, Given = [givenName] }],
            Gender = gender,
            BirthDate = birthDate
        };

        var encounter = new Encounter
        {
            Text = new Narrative
            {
                Status = Narrative.NarrativeStatus.Generated,
                Div = $"<div xmlns=\"http://www.w3.org/1999/xhtml\">Ambulatory encounter for {givenName} {familyName}</div>"
            },
            Status = Encounter.EncounterStatus.Finished,
            Class = new Coding(
                "http://terminology.hl7.org/CodeSystem/v3-ActCode", "AMB", "ambulatory"),
            Type =
            [
                new CodeableConcept("http://snomed.info/sct", "185349003", "Encounter for check up")
            ],
            Subject = new ResourceReference($"urn:uuid:{patientId}"),
            Period = new Period
            {
                StartElement = new FhirDateTime(encounterStart),
                EndElement = new FhirDateTime(encounterEnd)
            }
        };

        var observation = new Observation
        {
            Text = new Narrative
            {
                Status = Narrative.NarrativeStatus.Generated,
                Div = $"<div xmlns=\"http://www.w3.org/1999/xhtml\">Blood pressure: {systolicBp}/{diastolicBp} mmHg</div>"
            },
            Status = ObservationStatus.Final,
            Category =
            [
                new CodeableConcept(
                    "http://terminology.hl7.org/CodeSystem/observation-category",
                    "vital-signs", "Vital Signs")
            ],
            Code = new CodeableConcept("http://loinc.org", "85354-9", "Blood pressure panel"),
            Subject = new ResourceReference($"urn:uuid:{patientId}"),
            Encounter = new ResourceReference($"urn:uuid:{encounterId}"),
            Effective = new FhirDateTime(encounterStart.AddMinutes(15)),
            Component =
            [
                new Observation.ComponentComponent
                {
                    Code = new CodeableConcept("http://loinc.org", "8480-6", "Systolic blood pressure"),
                    Value = new Quantity(systolicBp, "mmHg", "http://unitsofmeasure.org")
                },
                new Observation.ComponentComponent
                {
                    Code = new CodeableConcept("http://loinc.org", "8462-4", "Diastolic blood pressure"),
                    Value = new Quantity(diastolicBp, "mmHg", "http://unitsofmeasure.org")
                }
            ]
        };

        var bundle = new Bundle
        {
            Type = Bundle.BundleType.Transaction,
            Entry =
            [
                new Bundle.EntryComponent
                {
                    FullUrl = $"urn:uuid:{patientId}",
                    Resource = patient,
                    Request = new Bundle.RequestComponent
                    {
                        Method = Bundle.HTTPVerb.POST,
                        Url = "Patient"
                    }
                },
                new Bundle.EntryComponent
                {
                    FullUrl = $"urn:uuid:{encounterId}",
                    Resource = encounter,
                    Request = new Bundle.RequestComponent
                    {
                        Method = Bundle.HTTPVerb.POST,
                        Url = "Encounter"
                    }
                },
                new Bundle.EntryComponent
                {
                    FullUrl = $"urn:uuid:{observationId}",
                    Resource = observation,
                    Request = new Bundle.RequestComponent
                    {
                        Method = Bundle.HTTPVerb.POST,
                        Url = "Observation"
                    }
                }
            ]
        };

        return await _client.TransactionAsync(bundle);
    }

    public async Task<Bundle> CreateBatchBundleAsync(List<Resource> resources)
    {
        var bundle = new Bundle
        {
            Type = Bundle.BundleType.Batch,
            Entry = resources.Select(r => new Bundle.EntryComponent
            {
                Resource = r,
                Request = new Bundle.RequestComponent
                {
                    Method = Bundle.HTTPVerb.POST,
                    Url = r.TypeName
                }
            }).ToList()
        };

        return await _client.TransactionAsync(bundle);
    }

    public static List<BundleResponseEntry> ParseTransactionResponse(Bundle response)
    {
        var entries = new List<BundleResponseEntry>();

        foreach (var entry in response.Entry)
        {
            var location = entry.Response?.Location;
            string? resourceType = null;
            string? resourceId = null;

            if (location != null)
            {
                var parts = location.Split('/');
                if (parts.Length >= 2)
                {
                    resourceType = parts[0];
                    resourceId = parts[1];
                }
            }

            entries.Add(new BundleResponseEntry
            {
                Status = entry.Response?.Status ?? "unknown",
                Location = location,
                ResourceType = resourceType,
                ResourceId = resourceId
            });
        }

        return entries;
    }
}

public class BundleResponseEntry
{
    public string Status { get; set; } = "";
    public string? Location { get; set; }
    public string? ResourceType { get; set; }
    public string? ResourceId { get; set; }
}
