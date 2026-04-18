using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;

namespace FhirLearning.Services;

public class ActivityDefinitionService
{
    private readonly FhirClient _client;

    public ActivityDefinitionService(FhirClient client)
    {
        _client = client;
    }

    public async Task<ActivityDefinition> CreateLabOrderActivityAsync()
    {
        var activityDefinition = new ActivityDefinition
        {
            Text = new Narrative
            {
                Status = Narrative.NarrativeStatus.Generated,
                Div = "<div xmlns=\"http://www.w3.org/1999/xhtml\">Activity: Order HbA1c lab test</div>"
            },
            Url = "http://example.org/fhir/ActivityDefinition/order-hba1c",
            Version = "1.0.0",
            Name = "OrderHbA1c",
            Title = "Order HbA1c Lab Test",
            Status = PublicationStatus.Active,
            Date = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd"),
            Publisher = "FHIR Learning Project",
            Description = new Markdown(
                "Activity definition for ordering a Hemoglobin A1c lab test. When applied, generates a ServiceRequest."),
            Kind = ActivityDefinition.RequestResourceType.ServiceRequest,
            Intent = RequestIntent.Order,
            Priority = RequestPriority.Routine,
            Code = new CodeableConcept("http://loinc.org", "4548-4", "Hemoglobin A1c/Hemoglobin.total in Blood"),
            DoNotPerform = false
        };

        return await _client.CreateAsync(activityDefinition);
    }

    public async Task<ActivityDefinition> CreateFollowUpTaskActivityAsync()
    {
        var activityDefinition = new ActivityDefinition
        {
            Text = new Narrative
            {
                Status = Narrative.NarrativeStatus.Generated,
                Div = "<div xmlns=\"http://www.w3.org/1999/xhtml\">Activity: Schedule follow-up appointment in 2 weeks</div>"
            },
            Url = "http://example.org/fhir/ActivityDefinition/schedule-followup",
            Version = "1.0.0",
            Name = "ScheduleFollowUp",
            Title = "Schedule Follow-Up Appointment",
            Status = PublicationStatus.Active,
            Date = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd"),
            Publisher = "FHIR Learning Project",
            Description = new Markdown(
                "Activity definition for scheduling a follow-up appointment within 2 weeks. When applied, generates a Task resource."),
            Kind = ActivityDefinition.RequestResourceType.Task,
            Intent = RequestIntent.Proposal,
            Priority = RequestPriority.Routine,
            Code = new CodeableConcept
            {
                Text = "Schedule follow-up encounter within 2 weeks"
            },
            DoNotPerform = false
        };

        return await _client.CreateAsync(activityDefinition);
    }

    public async Task<ActivityDefinition> ReadActivityDefinitionAsync(string id)
    {
        return await _client.ReadAsync<ActivityDefinition>($"ActivityDefinition/{id}");
    }

    public async Task<Bundle> SearchByUrlAsync(string url)
    {
        var searchParams = new SearchParams();
        searchParams.Add("url", url);
        return await _client.SearchAsync<ActivityDefinition>(searchParams);
    }

    public async Task<Bundle> SearchByStatusAsync(string status)
    {
        var searchParams = new SearchParams();
        searchParams.Add("status", status);
        return await _client.SearchAsync<ActivityDefinition>(searchParams);
    }
}
