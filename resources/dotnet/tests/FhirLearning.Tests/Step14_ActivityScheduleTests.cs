using FhirLearning.Services;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Xunit.Abstractions;
using Task = System.Threading.Tasks.Task;

namespace FhirLearning.Tests;

public class Step14_ActivityScheduleTests : IClassFixture<FhirServerFixture>
{
    private readonly PatientService _patientService;
    private readonly QuestionnaireService _questionnaireService;
    private readonly PlanDefinitionService _planDefinitionService;
    private readonly CarePlanService _carePlanService;
    private readonly ITestOutputHelper _output;

    private const string QuestionnaireUrl = "http://example.org/fhir/Questionnaire/prom-test";
    private const string LabOrderUrl = "http://example.org/fhir/ActivityDefinition/order-hba1c";
    private const string FollowUpUrl = "http://example.org/fhir/ActivityDefinition/schedule-followup";

    public Step14_ActivityScheduleTests(FhirServerFixture fixture, ITestOutputHelper output)
    {
        _patientService = new PatientService(fixture.Client);
        _questionnaireService = new QuestionnaireService(fixture.Client);
        _planDefinitionService = new PlanDefinitionService(fixture.Client);
        _carePlanService = new CarePlanService(fixture.Client);
        _output = output;
    }

    private async Task<Patient> CreateTestPatient()
    {
        return await _patientService.CreateUsCorePatientAsync(
            "ScheduleTest", "Jane",
            AdministrativeGender.Female, "1992-11-03",
            $"MRN-SCH-{Guid.NewGuid():N}");
    }

    private async Task<CarePlan> CreateTestCarePlan(string patientId)
    {
        var bundle = await _questionnaireService.SearchQuestionnairesByUrlAsync(QuestionnaireUrl);
        if (bundle.Entry.Count == 0)
            await _questionnaireService.CreatePromQuestionnaireAsync();

        var start = DateTimeOffset.UtcNow;
        var end = start.AddDays(30);

        var planDef = await _planDefinitionService.CreatePromAssessmentProtocolAsync(
            QuestionnaireUrl, start, end,
            labOrderActivityUrl: LabOrderUrl,
            followUpActivityUrl: FollowUpUrl);

        return await _carePlanService.CreateFromPlanDefinitionAsync(
            planDef, patientId, start, end);
    }

    // --- Display Schedule ---

    [Fact]
    public async Task DisplaySchedule_ShowsAllActivities()
    {
        var patient = await CreateTestPatient();
        var carePlan = await CreateTestCarePlan(patient.Id);

        var schedule = CarePlanService.GetActivitySchedule(carePlan);

        _output.WriteLine($"\n=== Activity Schedule for Patient/{patient.Id} ===");
        _output.WriteLine($"Plan: {carePlan.Status} | Period: {carePlan.Period.Start} → {carePlan.Period.End}\n");

        foreach (var item in schedule)
        {
            _output.WriteLine($"  {item.Index}. {item.Description}");
            _output.WriteLine($"     Status:    {item.Status}");
            _output.WriteLine($"     Due:       {item.ScheduledStart} → {item.ScheduledEnd}");
            _output.WriteLine($"     Source:    {item.DefinitionUrl}\n");
        }

        Assert.Equal(3, schedule.Count);
    }

    // --- Update Activity Status ---

    [Fact]
    public async Task UpdateActivityStatus_ChangesToInProgress()
    {
        var patient = await CreateTestPatient();
        var carePlan = await CreateTestCarePlan(patient.Id);

        var updated = await _carePlanService.UpdateActivityStatusAsync(
            carePlan.Id, 0, CarePlan.CarePlanActivityStatus.InProgress);

        Assert.Equal(CarePlan.CarePlanActivityStatus.InProgress,
            updated.Activity[0].Detail.Status);

        Assert.Equal(CarePlan.CarePlanActivityStatus.NotStarted,
            updated.Activity[1].Detail.Status);

        _output.WriteLine($"Activity 0: {updated.Activity[0].Detail.Status}");
        _output.WriteLine($"Activity 1: {updated.Activity[1].Detail.Status}");
    }

    [Fact]
    public async Task UpdateActivityStatus_ChangesToCompleted()
    {
        var patient = await CreateTestPatient();
        var carePlan = await CreateTestCarePlan(patient.Id);

        await _carePlanService.UpdateActivityStatusAsync(
            carePlan.Id, 0, CarePlan.CarePlanActivityStatus.InProgress);

        var updated = await _carePlanService.UpdateActivityStatusAsync(
            carePlan.Id, 0, CarePlan.CarePlanActivityStatus.Completed);

        Assert.Equal(CarePlan.CarePlanActivityStatus.Completed,
            updated.Activity[0].Detail.Status);
    }

    [Fact]
    public async Task UpdateActivityStatus_ChangesToCancelled()
    {
        var patient = await CreateTestPatient();
        var carePlan = await CreateTestCarePlan(patient.Id);

        var updated = await _carePlanService.UpdateActivityStatusAsync(
            carePlan.Id, 2, CarePlan.CarePlanActivityStatus.Cancelled);

        Assert.Equal(CarePlan.CarePlanActivityStatus.Cancelled,
            updated.Activity[2].Detail.Status);
    }

    [Fact]
    public async Task UpdateActivityStatus_OtherActivitiesUnchanged()
    {
        var patient = await CreateTestPatient();
        var carePlan = await CreateTestCarePlan(patient.Id);

        var updated = await _carePlanService.UpdateActivityStatusAsync(
            carePlan.Id, 1, CarePlan.CarePlanActivityStatus.InProgress);

        Assert.Equal(CarePlan.CarePlanActivityStatus.NotStarted,
            updated.Activity[0].Detail.Status);
        Assert.Equal(CarePlan.CarePlanActivityStatus.InProgress,
            updated.Activity[1].Detail.Status);
        Assert.Equal(CarePlan.CarePlanActivityStatus.NotStarted,
            updated.Activity[2].Detail.Status);
    }

    // --- Check Completion ---

    [Fact]
    public async Task AreAllActivitiesCompleted_FalseWhenNotStarted()
    {
        var patient = await CreateTestPatient();
        var carePlan = await CreateTestCarePlan(patient.Id);

        Assert.False(CarePlanService.AreAllActivitiesCompleted(carePlan));
    }

    [Fact]
    public async Task AreAllActivitiesCompleted_FalseWhenPartiallyDone()
    {
        var patient = await CreateTestPatient();
        var carePlan = await CreateTestCarePlan(patient.Id);

        await _carePlanService.UpdateActivityStatusAsync(
            carePlan.Id, 0, CarePlan.CarePlanActivityStatus.Completed);

        var current = await _carePlanService.ReadCarePlanAsync(carePlan.Id);
        Assert.False(CarePlanService.AreAllActivitiesCompleted(current));
    }

    [Fact]
    public async Task AreAllActivitiesCompleted_TrueWhenAllDone()
    {
        var patient = await CreateTestPatient();
        var carePlan = await CreateTestCarePlan(patient.Id);

        await _carePlanService.UpdateActivityStatusAsync(
            carePlan.Id, 0, CarePlan.CarePlanActivityStatus.Completed);
        await _carePlanService.UpdateActivityStatusAsync(
            carePlan.Id, 1, CarePlan.CarePlanActivityStatus.Completed);
        await _carePlanService.UpdateActivityStatusAsync(
            carePlan.Id, 2, CarePlan.CarePlanActivityStatus.Completed);

        var current = await _carePlanService.ReadCarePlanAsync(carePlan.Id);
        Assert.True(CarePlanService.AreAllActivitiesCompleted(current));
    }

    // --- Complete CarePlan ---

    [Fact]
    public async Task CompleteCarePlan_StatusChangesToCompleted()
    {
        var patient = await CreateTestPatient();
        var carePlan = await CreateTestCarePlan(patient.Id);

        await _carePlanService.UpdateActivityStatusAsync(
            carePlan.Id, 0, CarePlan.CarePlanActivityStatus.Completed);
        await _carePlanService.UpdateActivityStatusAsync(
            carePlan.Id, 1, CarePlan.CarePlanActivityStatus.Completed);
        await _carePlanService.UpdateActivityStatusAsync(
            carePlan.Id, 2, CarePlan.CarePlanActivityStatus.Completed);

        var completed = await _carePlanService.CompleteCarePlanAsync(carePlan.Id);

        Assert.Equal(RequestStatus.Completed, completed.Status);

        _output.WriteLine($"CarePlan/{completed.Id} status: {completed.Status}");
    }

    // --- Full Workflow Simulation ---

    [Fact]
    public async Task FullWorkflow_SimulatePatientJourney()
    {
        var patient = await CreateTestPatient();
        var carePlan = await CreateTestCarePlan(patient.Id);

        _output.WriteLine($"=== Patient Journey for {patient.Name[0].Given.First()} {patient.Name[0].Family} ===\n");

        // Day 1: Plan created
        var schedule = CarePlanService.GetActivitySchedule(carePlan);
        PrintSchedule(schedule, "Day 1: Plan created");
        Assert.Equal(RequestStatus.Active, carePlan.Status);

        // Day 2: Lab ordered
        carePlan = await _carePlanService.UpdateActivityStatusAsync(
            carePlan.Id, 1, CarePlan.CarePlanActivityStatus.InProgress);
        schedule = CarePlanService.GetActivitySchedule(carePlan);
        PrintSchedule(schedule, "Day 2: Lab ordered");

        // Day 5: Patient completes survey
        carePlan = await _carePlanService.UpdateActivityStatusAsync(
            carePlan.Id, 0, CarePlan.CarePlanActivityStatus.Completed);
        schedule = CarePlanService.GetActivitySchedule(carePlan);
        PrintSchedule(schedule, "Day 5: Survey completed");

        // Day 6: Lab results back
        carePlan = await _carePlanService.UpdateActivityStatusAsync(
            carePlan.Id, 1, CarePlan.CarePlanActivityStatus.Completed);
        schedule = CarePlanService.GetActivitySchedule(carePlan);
        PrintSchedule(schedule, "Day 6: Lab results received");
        Assert.False(CarePlanService.AreAllActivitiesCompleted(carePlan));

        // Day 25: Follow-up attended
        carePlan = await _carePlanService.UpdateActivityStatusAsync(
            carePlan.Id, 2, CarePlan.CarePlanActivityStatus.Completed);
        schedule = CarePlanService.GetActivitySchedule(carePlan);
        PrintSchedule(schedule, "Day 25: Follow-up completed");
        Assert.True(CarePlanService.AreAllActivitiesCompleted(carePlan));

        // All done — complete the plan
        carePlan = await _carePlanService.CompleteCarePlanAsync(carePlan.Id);
        _output.WriteLine($"  → CarePlan status: {carePlan.Status}\n");
        Assert.Equal(RequestStatus.Completed, carePlan.Status);
    }

    private void PrintSchedule(List<ActivityScheduleItem> schedule, string label)
    {
        _output.WriteLine($"  [{label}]");
        foreach (var item in schedule)
            _output.WriteLine($"    {item.Index}. {item.Description} — {item.Status}");
        _output.WriteLine("");
    }

    // --- Schedule After Updates ---

    [Fact]
    public async Task GetActivitySchedule_ReflectsUpdatedStatuses()
    {
        var patient = await CreateTestPatient();
        var carePlan = await CreateTestCarePlan(patient.Id);

        await _carePlanService.UpdateActivityStatusAsync(
            carePlan.Id, 0, CarePlan.CarePlanActivityStatus.Completed);
        await _carePlanService.UpdateActivityStatusAsync(
            carePlan.Id, 1, CarePlan.CarePlanActivityStatus.InProgress);

        var current = await _carePlanService.ReadCarePlanAsync(carePlan.Id);
        var schedule = CarePlanService.GetActivitySchedule(current);

        Assert.Equal("Completed", schedule[0].Status);
        Assert.Equal("InProgress", schedule[1].Status);
        Assert.Equal("NotStarted", schedule[2].Status);
    }

    [Fact]
    public async Task VersionIncrementsOnUpdate()
    {
        var patient = await CreateTestPatient();
        var carePlan = await CreateTestCarePlan(patient.Id);
        var initialVersion = carePlan.Meta.VersionId;

        var updated = await _carePlanService.UpdateActivityStatusAsync(
            carePlan.Id, 0, CarePlan.CarePlanActivityStatus.InProgress);

        _output.WriteLine($"Initial version: {initialVersion}");
        _output.WriteLine($"Updated version: {updated.Meta.VersionId}");

        Assert.NotEqual(initialVersion, updated.Meta.VersionId);
    }
}
