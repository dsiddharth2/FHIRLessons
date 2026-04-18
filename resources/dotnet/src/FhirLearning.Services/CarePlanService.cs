using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;

namespace FhirLearning.Services;

public class CarePlanService
{
    private readonly FhirClient _client;

    public CarePlanService(FhirClient client)
    {
        _client = client;
    }

    public async Task<CarePlan> CreateFromPlanDefinitionAsync(
        PlanDefinition planDefinition,
        string patientId,
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd)
    {
        var activities = new List<CarePlan.ActivityComponent>();

        foreach (var action in planDefinition.Action)
        {
            var activity = new CarePlan.ActivityComponent
            {
                Detail = new CarePlan.DetailComponent
                {
                    Status = CarePlan.CarePlanActivityStatus.NotStarted,
                    Description = action.Description
                }
            };

            if (action.Definition is Canonical canonical)
                activity.Detail.InstantiatesCanonical = [canonical.Value];

            if (action.Timing is Period actionPeriod)
            {
                activity.Detail.Scheduled = new Period
                {
                    StartElement = actionPeriod.StartElement,
                    EndElement = actionPeriod.EndElement
                };
            }

            activities.Add(activity);
        }

        var carePlan = new CarePlan
        {
            Text = new Narrative
            {
                Status = Narrative.NarrativeStatus.Generated,
                Div = $"<div xmlns=\"http://www.w3.org/1999/xhtml\">Care plan for Patient/{patientId} based on {planDefinition.Title}</div>"
            },
            Status = RequestStatus.Active,
            Intent = CarePlan.CarePlanIntent.Plan,
            Subject = new ResourceReference($"Patient/{patientId}"),
            Period = new Period
            {
                StartElement = new FhirDateTime(periodStart),
                EndElement = new FhirDateTime(periodEnd)
            },
            Category =
            [
                new CodeableConcept(
                    "http://hl7.org/fhir/us/core/CodeSystem/careplan-category",
                    "assess-plan", "Assessment and Plan of Treatment")
            ],
            Activity = activities
        };

        if (planDefinition.Url != null)
            carePlan.InstantiatesCanonical = [planDefinition.Url];

        return await _client.CreateAsync(carePlan);
    }

    public async Task<CarePlan> ReadCarePlanAsync(string id)
    {
        return await _client.ReadAsync<CarePlan>($"CarePlan/{id}");
    }

    public async Task<Bundle> SearchByPatientAsync(string patientId, int? count = null)
    {
        var searchParams = new SearchParams();
        searchParams.Add("subject", $"Patient/{patientId}");
        if (count.HasValue)
            searchParams.Count = count.Value;
        return await _client.SearchAsync<CarePlan>(searchParams);
    }

    public async Task<Bundle> SearchByStatusAsync(string status)
    {
        var searchParams = new SearchParams();
        searchParams.Add("status", status);
        return await _client.SearchAsync<CarePlan>(searchParams);
    }

    public async Task<Bundle> SearchByPatientAndStatusAsync(string patientId, string status)
    {
        var searchParams = new SearchParams();
        searchParams.Add("subject", $"Patient/{patientId}");
        searchParams.Add("status", status);
        return await _client.SearchAsync<CarePlan>(searchParams);
    }

    public async Task<CarePlan> UpdateActivityStatusAsync(
        string carePlanId,
        int activityIndex,
        CarePlan.CarePlanActivityStatus newStatus)
    {
        var carePlan = await ReadCarePlanAsync(carePlanId);

        carePlan.Activity[activityIndex].Detail.Status = newStatus;

        return await _client.UpdateAsync(carePlan);
    }

    public async Task<CarePlan> CompleteCarePlanAsync(string carePlanId)
    {
        var carePlan = await ReadCarePlanAsync(carePlanId);

        carePlan.Status = RequestStatus.Completed;

        return await _client.UpdateAsync(carePlan);
    }

    public static bool AreAllActivitiesCompleted(CarePlan carePlan)
    {
        return carePlan.Activity.All(a =>
            a.Detail?.Status == CarePlan.CarePlanActivityStatus.Completed);
    }

    public static List<ActivityScheduleItem> GetActivitySchedule(CarePlan carePlan)
    {
        var items = new List<ActivityScheduleItem>();

        for (var i = 0; i < carePlan.Activity.Count; i++)
        {
            var activity = carePlan.Activity[i];
            var detail = activity.Detail;

            string? scheduledStart = null;
            string? scheduledEnd = null;

            if (detail?.Scheduled is Period period)
            {
                scheduledStart = period.Start;
                scheduledEnd = period.End;
            }

            items.Add(new ActivityScheduleItem
            {
                Index = i + 1,
                Description = detail?.Description ?? "(no description)",
                Status = detail?.Status?.ToString() ?? "unknown",
                ScheduledStart = scheduledStart,
                ScheduledEnd = scheduledEnd,
                DefinitionUrl = detail?.InstantiatesCanonical?.FirstOrDefault()
            });
        }

        return items;
    }
}

public class ActivityScheduleItem
{
    public int Index { get; set; }
    public string Description { get; set; } = "";
    public string Status { get; set; } = "";
    public string? ScheduledStart { get; set; }
    public string? ScheduledEnd { get; set; }
    public string? DefinitionUrl { get; set; }
}
