using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;

namespace FhirLearning.Services;

public class PlanDefinitionService
{
    private readonly FhirClient _client;

    public PlanDefinitionService(FhirClient client)
    {
        _client = client;
    }

    public async Task<PlanDefinition> CreatePromAssessmentProtocolAsync(
        string questionnaireUrl,
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        string? labOrderActivityUrl = null,
        string? followUpActivityUrl = null)
    {
        var actions = new List<PlanDefinition.ActionComponent>
        {
            new()
            {
                Title = "Complete PROM Questionnaire",
                Description = "Patient completes the PRoM Test Questionnaire to report health outcomes",
                Priority = RequestPriority.Routine,
                Timing = new Period
                {
                    StartElement = new FhirDateTime(periodStart),
                    EndElement = new FhirDateTime(periodEnd)
                },
                Definition = new Canonical(questionnaireUrl)
            }
        };

        if (labOrderActivityUrl != null)
        {
            actions.Add(new PlanDefinition.ActionComponent
            {
                Title = "Order HbA1c Lab Test",
                Description = "Order a Hemoglobin A1c test to assess blood sugar control",
                Priority = RequestPriority.Routine,
                Timing = new Period
                {
                    StartElement = new FhirDateTime(periodStart),
                    EndElement = new FhirDateTime(periodStart.AddDays(7))
                },
                Definition = new Canonical(labOrderActivityUrl)
            });
        }

        if (followUpActivityUrl != null)
        {
            actions.Add(new PlanDefinition.ActionComponent
            {
                Title = "Schedule Follow-Up Appointment",
                Description = "Schedule a follow-up visit to review results and patient-reported outcomes",
                Priority = RequestPriority.Routine,
                Timing = new Period
                {
                    StartElement = new FhirDateTime(periodEnd.AddDays(-7)),
                    EndElement = new FhirDateTime(periodEnd)
                },
                Definition = new Canonical(followUpActivityUrl)
            });
        }

        var planDefinition = new PlanDefinition
        {
            Text = new Narrative
            {
                Status = Narrative.NarrativeStatus.Generated,
                Div = "<div xmlns=\"http://www.w3.org/1999/xhtml\">PROM Assessment Protocol — survey, lab order, and follow-up</div>"
            },
            Url = "http://example.org/fhir/PlanDefinition/prom-assessment-protocol",
            Version = "1.0.0",
            Name = "PromAssessmentProtocol",
            Title = "PROM Assessment Protocol",
            Status = PublicationStatus.Active,
            Type = new CodeableConcept(
                "http://terminology.hl7.org/CodeSystem/plan-definition-type",
                "clinical-protocol", "Clinical Protocol"),
            Date = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd"),
            Publisher = "FHIR Learning Project",
            Description = new Markdown(
                "A care protocol requiring the patient to complete a PROM questionnaire, get a lab test, and schedule a follow-up appointment."),
            Goal =
            [
                new PlanDefinition.GoalComponent
                {
                    Description = new CodeableConcept { Text = "Assess patient-reported health outcomes" },
                    Priority = new CodeableConcept(
                        "http://terminology.hl7.org/CodeSystem/goal-priority",
                        "medium-priority", "Medium Priority")
                }
            ],
            Action = actions
        };

        return await _client.CreateAsync(planDefinition);
    }

    public async Task<PlanDefinition> ReadPlanDefinitionAsync(string id)
    {
        return await _client.ReadAsync<PlanDefinition>($"PlanDefinition/{id}");
    }

    public async Task<Bundle> SearchByTitleAsync(string title)
    {
        var searchParams = new SearchParams();
        searchParams.Add("title", title);
        return await _client.SearchAsync<PlanDefinition>(searchParams);
    }

    public async Task<Bundle> SearchByUrlAsync(string url)
    {
        var searchParams = new SearchParams();
        searchParams.Add("url", url);
        return await _client.SearchAsync<PlanDefinition>(searchParams);
    }

    public async Task<Bundle> SearchByStatusAsync(string status)
    {
        var searchParams = new SearchParams();
        searchParams.Add("status", status);
        return await _client.SearchAsync<PlanDefinition>(searchParams);
    }
}
