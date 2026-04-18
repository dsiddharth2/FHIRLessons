using FhirLearning.Services;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Xunit.Abstractions;
using Task = System.Threading.Tasks.Task;

namespace FhirLearning.Tests;

public class Step12_PlanDefinitionTests : IClassFixture<FhirServerFixture>
{
    private readonly PlanDefinitionService _planDefinitionService;
    private readonly ActivityDefinitionService _activityDefinitionService;
    private readonly QuestionnaireService _questionnaireService;
    private readonly ITestOutputHelper _output;

    private const string QuestionnaireUrl = "http://example.org/fhir/Questionnaire/prom-test";
    private const string PlanDefinitionUrl = "http://example.org/fhir/PlanDefinition/prom-assessment-protocol";
    private const string LabOrderUrl = "http://example.org/fhir/ActivityDefinition/order-hba1c";
    private const string FollowUpUrl = "http://example.org/fhir/ActivityDefinition/schedule-followup";

    public Step12_PlanDefinitionTests(FhirServerFixture fixture, ITestOutputHelper output)
    {
        _planDefinitionService = new PlanDefinitionService(fixture.Client);
        _activityDefinitionService = new ActivityDefinitionService(fixture.Client);
        _questionnaireService = new QuestionnaireService(fixture.Client);
        _output = output;
    }

    private async Task EnsureQuestionnaireExists()
    {
        var bundle = await _questionnaireService.SearchQuestionnairesByUrlAsync(QuestionnaireUrl);
        if (bundle.Entry.Count == 0)
            await _questionnaireService.CreatePromQuestionnaireAsync();
    }

    private (DateTimeOffset start, DateTimeOffset end) GetDefaultPeriod()
    {
        var start = DateTimeOffset.UtcNow;
        var end = start.AddDays(30);
        return (start, end);
    }

    // --- ActivityDefinition Tests ---

    [Fact]
    public async Task CreateLabOrderActivity_ReturnsWithId()
    {
        var activity = await _activityDefinitionService.CreateLabOrderActivityAsync();

        _output.WriteLine($"Created ActivityDefinition/{activity.Id}");
        _output.WriteLine(activity.ToJson(pretty: true));

        Assert.NotNull(activity.Id);
        Assert.Equal("Order HbA1c Lab Test", activity.Title);
        Assert.Equal(PublicationStatus.Active, activity.Status);
    }

    [Fact]
    public async Task CreateLabOrderActivity_IsServiceRequestKind()
    {
        var activity = await _activityDefinitionService.CreateLabOrderActivityAsync();

        Assert.Equal(ActivityDefinition.RequestResourceType.ServiceRequest, activity.Kind);
        Assert.Equal(RequestIntent.Order, activity.Intent);
        Assert.Equal(RequestPriority.Routine, activity.Priority);
    }

    [Fact]
    public async Task CreateLabOrderActivity_HasLoincCode()
    {
        var activity = await _activityDefinitionService.CreateLabOrderActivityAsync();

        Assert.NotNull(activity.Code);
        var coding = activity.Code.Coding[0];
        Assert.Equal("http://loinc.org", coding.System);
        Assert.Equal("4548-4", coding.Code);
    }

    [Fact]
    public async Task CreateFollowUpActivity_ReturnsWithId()
    {
        var activity = await _activityDefinitionService.CreateFollowUpTaskActivityAsync();

        _output.WriteLine($"Created ActivityDefinition/{activity.Id}");
        _output.WriteLine(activity.ToJson(pretty: true));

        Assert.NotNull(activity.Id);
        Assert.Equal("Schedule Follow-Up Appointment", activity.Title);
        Assert.Equal(PublicationStatus.Active, activity.Status);
    }

    [Fact]
    public async Task CreateFollowUpActivity_IsTaskKind()
    {
        var activity = await _activityDefinitionService.CreateFollowUpTaskActivityAsync();

        Assert.Equal(ActivityDefinition.RequestResourceType.Task, activity.Kind);
        Assert.Equal(RequestIntent.Proposal, activity.Intent);
    }

    [Fact]
    public async Task CreateFollowUpActivity_HasTextCode()
    {
        var activity = await _activityDefinitionService.CreateFollowUpTaskActivityAsync();

        Assert.NotNull(activity.Code);
        Assert.Contains("follow-up", activity.Code.Text);
    }

    [Fact]
    public async Task ReadActivityDefinition_ReturnsSameResource()
    {
        var created = await _activityDefinitionService.CreateLabOrderActivityAsync();

        var fetched = await _activityDefinitionService.ReadActivityDefinitionAsync(created.Id);

        Assert.Equal(created.Id, fetched.Id);
        Assert.Equal(created.Title, fetched.Title);
        Assert.Equal(created.Url, fetched.Url);
    }

    [Fact]
    public async Task SearchActivityByUrl_FindsLabOrder()
    {
        await _activityDefinitionService.CreateLabOrderActivityAsync();

        var bundle = await _activityDefinitionService.SearchByUrlAsync(LabOrderUrl);

        _output.WriteLine($"Search by URL returned {bundle.Entry.Count} result(s)");

        Assert.NotEmpty(bundle.Entry);
        var found = (ActivityDefinition)bundle.Entry[0].Resource;
        Assert.Equal(LabOrderUrl, found.Url);
    }

    // --- PlanDefinition Tests (questionnaire-only, backward compatible) ---

    [Fact]
    public async Task CreatePromProtocol_QuestionnaireOnly_ReturnsWithId()
    {
        await EnsureQuestionnaireExists();
        var (start, end) = GetDefaultPeriod();

        var plan = await _planDefinitionService.CreatePromAssessmentProtocolAsync(
            QuestionnaireUrl, start, end);

        _output.WriteLine($"Created PlanDefinition/{plan.Id}");

        Assert.NotNull(plan.Id);
        Assert.Equal("PROM Assessment Protocol", plan.Title);
        Assert.Single(plan.Action);
    }

    [Fact]
    public async Task CreatePromProtocol_HasCorrectType()
    {
        await EnsureQuestionnaireExists();
        var (start, end) = GetDefaultPeriod();

        var plan = await _planDefinitionService.CreatePromAssessmentProtocolAsync(
            QuestionnaireUrl, start, end);

        var typeCoding = plan.Type.Coding[0];
        Assert.Equal("clinical-protocol", typeCoding.Code);
    }

    [Fact]
    public async Task CreatePromProtocol_HasGoal()
    {
        await EnsureQuestionnaireExists();
        var (start, end) = GetDefaultPeriod();

        var plan = await _planDefinitionService.CreatePromAssessmentProtocolAsync(
            QuestionnaireUrl, start, end);

        Assert.Single(plan.Goal);
        Assert.Equal("Assess patient-reported health outcomes", plan.Goal[0].Description.Text);
    }

    // --- PlanDefinition with all three actions ---

    [Fact]
    public async Task CreateFullProtocol_HasThreeActions()
    {
        await EnsureQuestionnaireExists();
        var (start, end) = GetDefaultPeriod();

        var plan = await _planDefinitionService.CreatePromAssessmentProtocolAsync(
            QuestionnaireUrl, start, end,
            labOrderActivityUrl: LabOrderUrl,
            followUpActivityUrl: FollowUpUrl);

        _output.WriteLine($"Created PlanDefinition/{plan.Id} with {plan.Action.Count} actions");
        _output.WriteLine(plan.ToJson(pretty: true));

        Assert.Equal(3, plan.Action.Count);
    }

    [Fact]
    public async Task CreateFullProtocol_FirstActionIsQuestionnaire()
    {
        await EnsureQuestionnaireExists();
        var (start, end) = GetDefaultPeriod();

        var plan = await _planDefinitionService.CreatePromAssessmentProtocolAsync(
            QuestionnaireUrl, start, end,
            labOrderActivityUrl: LabOrderUrl,
            followUpActivityUrl: FollowUpUrl);

        var action = plan.Action[0];
        Assert.Equal("Complete PROM Questionnaire", action.Title);
        Assert.Equal(QuestionnaireUrl, ((Canonical)action.Definition).Value);
    }

    [Fact]
    public async Task CreateFullProtocol_SecondActionIsLabOrder()
    {
        await EnsureQuestionnaireExists();
        var (start, end) = GetDefaultPeriod();

        var plan = await _planDefinitionService.CreatePromAssessmentProtocolAsync(
            QuestionnaireUrl, start, end,
            labOrderActivityUrl: LabOrderUrl,
            followUpActivityUrl: FollowUpUrl);

        var action = plan.Action[1];
        Assert.Equal("Order HbA1c Lab Test", action.Title);
        Assert.Equal(LabOrderUrl, ((Canonical)action.Definition).Value);
    }

    [Fact]
    public async Task CreateFullProtocol_ThirdActionIsFollowUp()
    {
        await EnsureQuestionnaireExists();
        var (start, end) = GetDefaultPeriod();

        var plan = await _planDefinitionService.CreatePromAssessmentProtocolAsync(
            QuestionnaireUrl, start, end,
            labOrderActivityUrl: LabOrderUrl,
            followUpActivityUrl: FollowUpUrl);

        var action = plan.Action[2];
        Assert.Equal("Schedule Follow-Up Appointment", action.Title);
        Assert.Equal(FollowUpUrl, ((Canonical)action.Definition).Value);
    }

    [Fact]
    public async Task CreateFullProtocol_ActionsHaveDifferentTimings()
    {
        await EnsureQuestionnaireExists();
        var (start, end) = GetDefaultPeriod();

        var plan = await _planDefinitionService.CreatePromAssessmentProtocolAsync(
            QuestionnaireUrl, start, end,
            labOrderActivityUrl: LabOrderUrl,
            followUpActivityUrl: FollowUpUrl);

        var questionnaireTiming = (Period)plan.Action[0].Timing;
        var labTiming = (Period)plan.Action[1].Timing;
        var followUpTiming = (Period)plan.Action[2].Timing;

        _output.WriteLine($"Questionnaire: {questionnaireTiming.Start} to {questionnaireTiming.End}");
        _output.WriteLine($"Lab order:     {labTiming.Start} to {labTiming.End}");
        _output.WriteLine($"Follow-up:     {followUpTiming.Start} to {followUpTiming.End}");

        Assert.NotNull(questionnaireTiming.Start);
        Assert.NotNull(labTiming.Start);
        Assert.NotNull(followUpTiming.Start);
    }

    [Fact]
    public async Task CreatePromProtocol_HasCanonicalUrl()
    {
        await EnsureQuestionnaireExists();
        var (start, end) = GetDefaultPeriod();

        var plan = await _planDefinitionService.CreatePromAssessmentProtocolAsync(
            QuestionnaireUrl, start, end);

        Assert.Equal(PlanDefinitionUrl, plan.Url);
        Assert.Equal("1.0.0", plan.Version);
    }

    [Fact]
    public async Task ReadPlanDefinition_ReturnsSameResource()
    {
        await EnsureQuestionnaireExists();
        var (start, end) = GetDefaultPeriod();

        var created = await _planDefinitionService.CreatePromAssessmentProtocolAsync(
            QuestionnaireUrl, start, end);

        var fetched = await _planDefinitionService.ReadPlanDefinitionAsync(created.Id);

        Assert.Equal(created.Id, fetched.Id);
        Assert.Equal(created.Title, fetched.Title);
    }

    [Fact]
    public async Task SearchByTitle_FindsPlanDefinition()
    {
        await EnsureQuestionnaireExists();
        var (start, end) = GetDefaultPeriod();
        await _planDefinitionService.CreatePromAssessmentProtocolAsync(
            QuestionnaireUrl, start, end);

        var bundle = await _planDefinitionService.SearchByTitleAsync("PROM");

        Assert.NotEmpty(bundle.Entry);
        var found = (PlanDefinition)bundle.Entry[0].Resource;
        Assert.Contains("PROM", found.Title);
    }

    [Fact]
    public async Task SearchByUrl_FindsPlanDefinition()
    {
        await EnsureQuestionnaireExists();
        var (start, end) = GetDefaultPeriod();
        await _planDefinitionService.CreatePromAssessmentProtocolAsync(
            QuestionnaireUrl, start, end);

        var bundle = await _planDefinitionService.SearchByUrlAsync(PlanDefinitionUrl);

        Assert.NotEmpty(bundle.Entry);
    }

    [Fact]
    public async Task SearchByStatus_FindsActivePlanDefinitions()
    {
        await EnsureQuestionnaireExists();
        var (start, end) = GetDefaultPeriod();
        await _planDefinitionService.CreatePromAssessmentProtocolAsync(
            QuestionnaireUrl, start, end);

        var bundle = await _planDefinitionService.SearchByStatusAsync("active");

        Assert.NotEmpty(bundle.Entry);
        Assert.All(bundle.Entry, entry =>
        {
            var pd = (PlanDefinition)entry.Resource;
            Assert.Equal(PublicationStatus.Active, pd.Status);
        });
    }
}
