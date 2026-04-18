using FhirLearning.Services;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Xunit.Abstractions;
using Task = System.Threading.Tasks.Task;

namespace FhirLearning.Tests;

public class Step13_CarePlanTests : IClassFixture<FhirServerFixture>
{
    private readonly FhirServerFixture _fixture;
    private readonly PatientService _patientService;
    private readonly QuestionnaireService _questionnaireService;
    private readonly ActivityDefinitionService _activityDefinitionService;
    private readonly PlanDefinitionService _planDefinitionService;
    private readonly CarePlanService _carePlanService;
    private readonly ITestOutputHelper _output;

    private const string QuestionnaireUrl = "http://example.org/fhir/Questionnaire/prom-test";
    private const string LabOrderUrl = "http://example.org/fhir/ActivityDefinition/order-hba1c";
    private const string FollowUpUrl = "http://example.org/fhir/ActivityDefinition/schedule-followup";
    private const string PlanDefinitionUrl = "http://example.org/fhir/PlanDefinition/prom-assessment-protocol";

    public Step13_CarePlanTests(FhirServerFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _patientService = new PatientService(fixture.Client);
        _questionnaireService = new QuestionnaireService(fixture.Client);
        _activityDefinitionService = new ActivityDefinitionService(fixture.Client);
        _planDefinitionService = new PlanDefinitionService(fixture.Client);
        _carePlanService = new CarePlanService(fixture.Client);
        _output = output;
    }

    private async Task<Patient> CreateTestPatient()
    {
        return await _patientService.CreateUsCorePatientAsync(
            "CarePlanTest", "John",
            AdministrativeGender.Male, "1985-07-15",
            $"MRN-CP-{Guid.NewGuid():N}");
    }

    private async Task EnsureQuestionnaireExists()
    {
        var bundle = await _questionnaireService.SearchQuestionnairesByUrlAsync(QuestionnaireUrl);
        if (bundle.Entry.Count == 0)
            await _questionnaireService.CreatePromQuestionnaireAsync();
    }

    private async Task<PlanDefinition> CreateFullPlanDefinition()
    {
        await EnsureQuestionnaireExists();
        var start = DateTimeOffset.UtcNow;
        var end = start.AddDays(30);

        return await _planDefinitionService.CreatePromAssessmentProtocolAsync(
            QuestionnaireUrl, start, end,
            labOrderActivityUrl: LabOrderUrl,
            followUpActivityUrl: FollowUpUrl);
    }

    private async Task<PlanDefinition> CreateSingleActionPlanDefinition()
    {
        await EnsureQuestionnaireExists();
        var start = DateTimeOffset.UtcNow;
        var end = start.AddDays(30);

        return await _planDefinitionService.CreatePromAssessmentProtocolAsync(
            QuestionnaireUrl, start, end);
    }

    [Fact]
    public async Task CreateCarePlan_FromFullPlanDefinition_ReturnsWithId()
    {
        var patient = await CreateTestPatient();
        var planDef = await CreateFullPlanDefinition();
        var start = DateTimeOffset.UtcNow;
        var end = start.AddDays(30);

        var carePlan = await _carePlanService.CreateFromPlanDefinitionAsync(
            planDef, patient.Id, start, end);

        _output.WriteLine($"Created CarePlan/{carePlan.Id}");
        _output.WriteLine(carePlan.ToJson(pretty: true));

        Assert.NotNull(carePlan.Id);
        Assert.Equal(RequestStatus.Active, carePlan.Status);
        Assert.Equal(CarePlan.CarePlanIntent.Plan, carePlan.Intent);
    }

    [Fact]
    public async Task CreateCarePlan_HasThreeActivities()
    {
        var patient = await CreateTestPatient();
        var planDef = await CreateFullPlanDefinition();
        var start = DateTimeOffset.UtcNow;
        var end = start.AddDays(30);

        var carePlan = await _carePlanService.CreateFromPlanDefinitionAsync(
            planDef, patient.Id, start, end);

        Assert.Equal(3, carePlan.Activity.Count);
    }

    [Fact]
    public async Task CreateCarePlan_ActivitiesHaveDescriptions()
    {
        var patient = await CreateTestPatient();
        var planDef = await CreateFullPlanDefinition();
        var start = DateTimeOffset.UtcNow;
        var end = start.AddDays(30);

        var carePlan = await _carePlanService.CreateFromPlanDefinitionAsync(
            planDef, patient.Id, start, end);

        var descriptions = carePlan.Activity
            .Select(a => a.Detail?.Description)
            .ToList();

        _output.WriteLine("Activities:");
        foreach (var desc in descriptions)
            _output.WriteLine($"  - {desc}");

        Assert.Equal(3, descriptions.Count);
        Assert.All(descriptions, d => Assert.NotNull(d));
    }

    [Fact]
    public async Task CreateCarePlan_AllActivitiesNotStarted()
    {
        var patient = await CreateTestPatient();
        var planDef = await CreateFullPlanDefinition();
        var start = DateTimeOffset.UtcNow;
        var end = start.AddDays(30);

        var carePlan = await _carePlanService.CreateFromPlanDefinitionAsync(
            planDef, patient.Id, start, end);

        Assert.All(carePlan.Activity, activity =>
        {
            Assert.Equal(CarePlan.CarePlanActivityStatus.NotStarted, activity.Detail?.Status);
        });
    }

    [Fact]
    public async Task CreateCarePlan_ActivitiesHaveScheduledPeriods()
    {
        var patient = await CreateTestPatient();
        var planDef = await CreateFullPlanDefinition();
        var start = DateTimeOffset.UtcNow;
        var end = start.AddDays(30);

        var carePlan = await _carePlanService.CreateFromPlanDefinitionAsync(
            planDef, patient.Id, start, end);

        foreach (var activity in carePlan.Activity)
        {
            var scheduled = activity.Detail?.Scheduled as Period;
            Assert.NotNull(scheduled);
            Assert.NotNull(scheduled.Start);
            Assert.NotNull(scheduled.End);

            _output.WriteLine($"  {activity.Detail?.Description}: {scheduled.Start} → {scheduled.End}");
        }
    }

    [Fact]
    public async Task CreateCarePlan_ActivitiesReferenceDefinitions()
    {
        var patient = await CreateTestPatient();
        var planDef = await CreateFullPlanDefinition();
        var start = DateTimeOffset.UtcNow;
        var end = start.AddDays(30);

        var carePlan = await _carePlanService.CreateFromPlanDefinitionAsync(
            planDef, patient.Id, start, end);

        var definitionUrls = carePlan.Activity
            .Select(a => a.Detail?.InstantiatesCanonical?.FirstOrDefault())
            .ToList();

        Assert.Contains(QuestionnaireUrl, definitionUrls);
        Assert.Contains(LabOrderUrl, definitionUrls);
        Assert.Contains(FollowUpUrl, definitionUrls);
    }

    [Fact]
    public async Task CreateCarePlan_LinksToPatient()
    {
        var patient = await CreateTestPatient();
        var planDef = await CreateFullPlanDefinition();
        var start = DateTimeOffset.UtcNow;
        var end = start.AddDays(30);

        var carePlan = await _carePlanService.CreateFromPlanDefinitionAsync(
            planDef, patient.Id, start, end);

        Assert.Equal($"Patient/{patient.Id}", carePlan.Subject.Reference);
    }

    [Fact]
    public async Task CreateCarePlan_InstantiatesPlanDefinition()
    {
        var patient = await CreateTestPatient();
        var planDef = await CreateFullPlanDefinition();
        var start = DateTimeOffset.UtcNow;
        var end = start.AddDays(30);

        var carePlan = await _carePlanService.CreateFromPlanDefinitionAsync(
            planDef, patient.Id, start, end);

        Assert.Contains(PlanDefinitionUrl, carePlan.InstantiatesCanonical);
    }

    [Fact]
    public async Task CreateCarePlan_HasPeriod()
    {
        var patient = await CreateTestPatient();
        var planDef = await CreateFullPlanDefinition();
        var start = DateTimeOffset.UtcNow;
        var end = start.AddDays(30);

        var carePlan = await _carePlanService.CreateFromPlanDefinitionAsync(
            planDef, patient.Id, start, end);

        Assert.NotNull(carePlan.Period);
        Assert.NotNull(carePlan.Period.Start);
        Assert.NotNull(carePlan.Period.End);

        _output.WriteLine($"Plan period: {carePlan.Period.Start} to {carePlan.Period.End}");
    }

    [Fact]
    public async Task CreateCarePlan_HasCategory()
    {
        var patient = await CreateTestPatient();
        var planDef = await CreateFullPlanDefinition();
        var start = DateTimeOffset.UtcNow;
        var end = start.AddDays(30);

        var carePlan = await _carePlanService.CreateFromPlanDefinitionAsync(
            planDef, patient.Id, start, end);

        Assert.Single(carePlan.Category);
        Assert.Equal("assess-plan", carePlan.Category[0].Coding[0].Code);
    }

    [Fact]
    public async Task CreateCarePlan_SingleAction_HasOneActivity()
    {
        var patient = await CreateTestPatient();
        var planDef = await CreateSingleActionPlanDefinition();
        var start = DateTimeOffset.UtcNow;
        var end = start.AddDays(30);

        var carePlan = await _carePlanService.CreateFromPlanDefinitionAsync(
            planDef, patient.Id, start, end);

        Assert.Single(carePlan.Activity);
        Assert.Contains("PRoM", carePlan.Activity[0].Detail?.Description);
    }

    [Fact]
    public async Task ReadCarePlan_ReturnsSameResource()
    {
        var patient = await CreateTestPatient();
        var planDef = await CreateFullPlanDefinition();
        var start = DateTimeOffset.UtcNow;
        var end = start.AddDays(30);

        var created = await _carePlanService.CreateFromPlanDefinitionAsync(
            planDef, patient.Id, start, end);

        var fetched = await _carePlanService.ReadCarePlanAsync(created.Id);

        Assert.Equal(created.Id, fetched.Id);
        Assert.Equal(created.Activity.Count, fetched.Activity.Count);
    }

    [Fact]
    public async Task SearchByPatient_FindsCarePlan()
    {
        var patient = await CreateTestPatient();
        var planDef = await CreateFullPlanDefinition();
        var start = DateTimeOffset.UtcNow;
        var end = start.AddDays(30);

        await _carePlanService.CreateFromPlanDefinitionAsync(
            planDef, patient.Id, start, end);

        var bundle = await _carePlanService.SearchByPatientAsync(patient.Id);

        _output.WriteLine($"Search by patient returned {bundle.Entry.Count} result(s)");
        Assert.NotEmpty(bundle.Entry);
    }

    [Fact]
    public async Task SearchByPatientAndStatus_FindsActiveCarePlan()
    {
        var patient = await CreateTestPatient();
        var planDef = await CreateFullPlanDefinition();
        var start = DateTimeOffset.UtcNow;
        var end = start.AddDays(30);

        await _carePlanService.CreateFromPlanDefinitionAsync(
            planDef, patient.Id, start, end);

        var bundle = await _carePlanService.SearchByPatientAndStatusAsync(patient.Id, "active");

        Assert.NotEmpty(bundle.Entry);
        var found = (CarePlan)bundle.Entry[0].Resource;
        Assert.Equal(RequestStatus.Active, found.Status);
    }

    // --- Activity Schedule Display ---

    [Fact]
    public async Task GetActivitySchedule_ReturnsFormattedItems()
    {
        var patient = await CreateTestPatient();
        var planDef = await CreateFullPlanDefinition();
        var start = DateTimeOffset.UtcNow;
        var end = start.AddDays(30);

        var carePlan = await _carePlanService.CreateFromPlanDefinitionAsync(
            planDef, patient.Id, start, end);

        var schedule = CarePlanService.GetActivitySchedule(carePlan);

        _output.WriteLine($"\n=== Activity Schedule for Patient/{patient.Id} ===");
        _output.WriteLine($"Plan period: {carePlan.Period.Start} to {carePlan.Period.End}");
        _output.WriteLine($"Status: {carePlan.Status}\n");

        foreach (var item in schedule)
        {
            _output.WriteLine($"  {item.Index}. {item.Description}");
            _output.WriteLine($"     Status: {item.Status}");
            _output.WriteLine($"     Scheduled: {item.ScheduledStart} → {item.ScheduledEnd}");
            _output.WriteLine($"     Definition: {item.DefinitionUrl}");
            _output.WriteLine("");
        }

        Assert.Equal(3, schedule.Count);
        Assert.All(schedule, item =>
        {
            Assert.NotEmpty(item.Description);
            Assert.NotNull(item.ScheduledStart);
            Assert.NotNull(item.DefinitionUrl);
        });
    }

    [Fact]
    public async Task GetActivitySchedule_ItemsHaveCorrectIndices()
    {
        var patient = await CreateTestPatient();
        var planDef = await CreateFullPlanDefinition();
        var start = DateTimeOffset.UtcNow;
        var end = start.AddDays(30);

        var carePlan = await _carePlanService.CreateFromPlanDefinitionAsync(
            planDef, patient.Id, start, end);

        var schedule = CarePlanService.GetActivitySchedule(carePlan);

        Assert.Equal(1, schedule[0].Index);
        Assert.Equal(2, schedule[1].Index);
        Assert.Equal(3, schedule[2].Index);
    }

    [Fact]
    public async Task GetActivitySchedule_AllStatusesNotStarted()
    {
        var patient = await CreateTestPatient();
        var planDef = await CreateFullPlanDefinition();
        var start = DateTimeOffset.UtcNow;
        var end = start.AddDays(30);

        var carePlan = await _carePlanService.CreateFromPlanDefinitionAsync(
            planDef, patient.Id, start, end);

        var schedule = CarePlanService.GetActivitySchedule(carePlan);

        Assert.All(schedule, item =>
            Assert.Equal("NotStarted", item.Status));
    }
}
