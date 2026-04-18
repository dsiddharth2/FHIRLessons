using FhirLearning.Services;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Xunit.Abstractions;
using Task = System.Threading.Tasks.Task;

namespace FhirLearning.Tests;

public class Step11_QuestionnaireResponseTests : IClassFixture<FhirServerFixture>
{
    private readonly FhirServerFixture _fixture;
    private readonly PatientService _patientService;
    private readonly QuestionnaireService _questionnaireService;
    private readonly QuestionnaireResponseService _responseService;
    private readonly ITestOutputHelper _output;

    private const string QuestionnaireUrl = "http://example.org/fhir/Questionnaire/prom-test";

    public Step11_QuestionnaireResponseTests(FhirServerFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _patientService = new PatientService(fixture.Client);
        _questionnaireService = new QuestionnaireService(fixture.Client);
        _responseService = new QuestionnaireResponseService(fixture.Client);
        _output = output;
    }

    private async Task<Questionnaire> EnsureQuestionnaireExists()
    {
        var bundle = await _questionnaireService.SearchQuestionnairesByUrlAsync(QuestionnaireUrl);
        if (bundle.Entry.Count > 0)
            return (Questionnaire)bundle.Entry[0].Resource;

        return await _questionnaireService.CreatePromQuestionnaireAsync();
    }

    private async Task<Patient> CreateTestPatient()
    {
        return await _patientService.CreateUsCorePatientAsync(
            "ResponseTest", "PROM",
            AdministrativeGender.Female, "1990-03-15",
            $"MRN-QR-{Guid.NewGuid():N}");
    }

    [Fact]
    public async Task CreateCompletedResponse_ReturnsWithId()
    {
        await EnsureQuestionnaireExists();
        var patient = await CreateTestPatient();

        var response = await _responseService.CreateCompletedResponseAsync(
            QuestionnaireUrl, patient.Id);

        _output.WriteLine($"Created QuestionnaireResponse/{response.Id}");
        _output.WriteLine(response.ToJson(pretty: true));

        Assert.NotNull(response.Id);
        Assert.Equal(QuestionnaireResponse.QuestionnaireResponseStatus.Completed, response.Status);
        Assert.Equal(QuestionnaireUrl, response.Questionnaire);
    }

    [Fact]
    public async Task CreateCompletedResponse_HasCorrectStructure()
    {
        await EnsureQuestionnaireExists();
        var patient = await CreateTestPatient();

        var response = await _responseService.CreateCompletedResponseAsync(
            QuestionnaireUrl, patient.Id);

        Assert.Equal(4, response.Item.Count);

        Assert.Equal("1", response.Item[0].LinkId);
        Assert.Equal("General Health", response.Item[0].Text);
        Assert.Equal(2, response.Item[0].Item.Count);

        Assert.Equal("2", response.Item[1].LinkId);
        Assert.Equal("Pain Assessment", response.Item[1].Text);
        Assert.Equal(2, response.Item[1].Item.Count);

        Assert.Equal("3", response.Item[2].LinkId);
        Assert.Equal("Mental Health", response.Item[2].Text);
        Assert.Equal(2, response.Item[2].Item.Count);

        Assert.Equal("4", response.Item[3].LinkId);
    }

    [Fact]
    public async Task CreateCompletedResponse_GeneralHealthAnswersCorrect()
    {
        await EnsureQuestionnaireExists();
        var patient = await CreateTestPatient();

        var response = await _responseService.CreateCompletedResponseAsync(
            QuestionnaireUrl, patient.Id);

        var healthRating = response.Item[0].Item[0];
        Assert.Equal("1.1", healthRating.LinkId);
        var ratingAnswer = (Coding)healthRating.Answer[0].Value;
        Assert.Equal("good", ratingAnswer.Code);
        Assert.Equal("Good", ratingAnswer.Display);

        var daysActive = response.Item[0].Item[1];
        Assert.Equal("1.2", daysActive.LinkId);
        var daysAnswer = (Integer)daysActive.Answer[0].Value;
        Assert.Equal(4, daysAnswer.Value);
    }

    [Fact]
    public async Task CreateCompletedResponse_PainAnswersCorrect()
    {
        await EnsureQuestionnaireExists();
        var patient = await CreateTestPatient();

        var response = await _responseService.CreateCompletedResponseAsync(
            QuestionnaireUrl, patient.Id);

        var painLevel = response.Item[1].Item[0];
        Assert.Equal("2.1", painLevel.LinkId);
        var painAnswer = (Integer)painLevel.Answer[0].Value;
        Assert.Equal(3, painAnswer.Value);

        var painInterferes = response.Item[1].Item[1];
        Assert.Equal("2.2", painInterferes.LinkId);
        var interferesAnswer = (FhirBoolean)painInterferes.Answer[0].Value;
        Assert.False(interferesAnswer.Value);
    }

    [Fact]
    public async Task CreateCompletedResponse_MentalHealthAnswersCorrect()
    {
        await EnsureQuestionnaireExists();
        var patient = await CreateTestPatient();

        var response = await _responseService.CreateCompletedResponseAsync(
            QuestionnaireUrl, patient.Id);

        var depression = response.Item[2].Item[0];
        Assert.Equal("3.1", depression.LinkId);
        var depressionAnswer = (Coding)depression.Answer[0].Value;
        Assert.Equal("several-days", depressionAnswer.Code);

        var anxiety = response.Item[2].Item[1];
        Assert.Equal("3.2", anxiety.LinkId);
        var anxietyAnswer = (Coding)anxiety.Answer[0].Value;
        Assert.Equal("not-at-all", anxietyAnswer.Code);
    }

    [Fact]
    public async Task CreateCompletedResponse_CommentsCorrect()
    {
        await EnsureQuestionnaireExists();
        var patient = await CreateTestPatient();

        var response = await _responseService.CreateCompletedResponseAsync(
            QuestionnaireUrl, patient.Id);

        var comments = response.Item[3];
        Assert.Equal("4", comments.LinkId);
        var commentText = (FhirString)comments.Answer[0].Value;
        Assert.Equal("Occasional mild headache in the evenings", commentText.Value);
    }

    [Fact]
    public async Task CreateCompletedResponse_LinksToPatient()
    {
        await EnsureQuestionnaireExists();
        var patient = await CreateTestPatient();

        var response = await _responseService.CreateCompletedResponseAsync(
            QuestionnaireUrl, patient.Id);

        Assert.Equal($"Patient/{patient.Id}", response.Subject.Reference);
        Assert.Equal($"Patient/{patient.Id}", response.Author.Reference);
    }

    [Fact]
    public async Task CreateCompletedResponse_WithEncounter()
    {
        await EnsureQuestionnaireExists();
        var patient = await CreateTestPatient();

        var encounterService = new EncounterService(_fixture.Client);
        var encounter = await encounterService.CreateAmbulatoryEncounterAsync(
            patient.Id, DateTimeOffset.UtcNow.AddHours(-2), DateTimeOffset.UtcNow.AddHours(-1),
            "185349003", "Encounter for check up");

        var response = await _responseService.CreateCompletedResponseAsync(
            QuestionnaireUrl, patient.Id, encounterId: encounter.Id);

        Assert.Equal($"Encounter/{encounter.Id}", response.Encounter.Reference);
    }

    [Fact]
    public async Task CreateCompletedResponse_HasAuthoredDate()
    {
        await EnsureQuestionnaireExists();
        var patient = await CreateTestPatient();

        var response = await _responseService.CreateCompletedResponseAsync(
            QuestionnaireUrl, patient.Id);

        Assert.NotNull(response.Authored);
        _output.WriteLine($"Authored: {response.Authored}");
    }

    [Fact]
    public async Task ReadResponse_ReturnsSameResource()
    {
        await EnsureQuestionnaireExists();
        var patient = await CreateTestPatient();

        var created = await _responseService.CreateCompletedResponseAsync(
            QuestionnaireUrl, patient.Id);

        var fetched = await _responseService.ReadResponseAsync(created.Id);

        _output.WriteLine($"Read QuestionnaireResponse/{fetched.Id}");

        Assert.Equal(created.Id, fetched.Id);
        Assert.Equal(created.Questionnaire, fetched.Questionnaire);
        Assert.Equal(created.Status, fetched.Status);
        Assert.Equal(created.Item.Count, fetched.Item.Count);
    }

    [Fact]
    public async Task SearchByPatient_FindsResponses()
    {
        await EnsureQuestionnaireExists();
        var patient = await CreateTestPatient();
        await _responseService.CreateCompletedResponseAsync(QuestionnaireUrl, patient.Id);

        var bundle = await _responseService.SearchByPatientAsync(patient.Id);

        _output.WriteLine($"Search by patient returned {bundle.Entry.Count} result(s)");

        Assert.NotEmpty(bundle.Entry);
        var found = (QuestionnaireResponse)bundle.Entry[0].Resource;
        Assert.Equal($"Patient/{patient.Id}", found.Subject.Reference);
    }

    [Fact]
    public async Task SearchByQuestionnaire_FindsResponses()
    {
        await EnsureQuestionnaireExists();
        var patient = await CreateTestPatient();
        await _responseService.CreateCompletedResponseAsync(QuestionnaireUrl, patient.Id);

        var bundle = await _responseService.SearchByQuestionnaireAsync(QuestionnaireUrl);

        _output.WriteLine($"Search by questionnaire returned {bundle.Entry.Count} result(s)");

        Assert.NotEmpty(bundle.Entry);
        var found = (QuestionnaireResponse)bundle.Entry[0].Resource;
        Assert.Equal(QuestionnaireUrl, found.Questionnaire);
    }

    [Fact]
    public async Task SearchByPatientAndQuestionnaire_FindsResponses()
    {
        await EnsureQuestionnaireExists();
        var patient = await CreateTestPatient();
        await _responseService.CreateCompletedResponseAsync(QuestionnaireUrl, patient.Id);

        var bundle = await _responseService.SearchByPatientAndQuestionnaireAsync(
            patient.Id, QuestionnaireUrl);

        _output.WriteLine($"Combined search returned {bundle.Entry.Count} result(s)");

        Assert.NotEmpty(bundle.Entry);
        var found = (QuestionnaireResponse)bundle.Entry[0].Resource;
        Assert.Equal($"Patient/{patient.Id}", found.Subject.Reference);
        Assert.Equal(QuestionnaireUrl, found.Questionnaire);
    }

    [Fact]
    public async Task LinkIdsMatchQuestionnaire()
    {
        var questionnaire = await EnsureQuestionnaireExists();
        var patient = await CreateTestPatient();
        var response = await _responseService.CreateCompletedResponseAsync(
            QuestionnaireUrl, patient.Id);

        var questionnaireLinkIds = GetAllLinkIdsFromQuestionnaire(questionnaire.Item);
        var responseLinkIds = GetAllLinkIds(response.Item);

        _output.WriteLine($"Questionnaire linkIds: {string.Join(", ", questionnaireLinkIds)}");
        _output.WriteLine($"Response linkIds:      {string.Join(", ", responseLinkIds)}");

        foreach (var linkId in responseLinkIds)
        {
            Assert.Contains(linkId, questionnaireLinkIds);
        }
    }

    private static List<string> GetAllLinkIds(List<QuestionnaireResponse.ItemComponent> items)
    {
        var linkIds = new List<string>();
        foreach (var item in items)
        {
            linkIds.Add(item.LinkId);
            if (item.Item is { Count: > 0 })
                linkIds.AddRange(GetAllLinkIds(item.Item));
        }
        return linkIds;
    }

    private static List<string> GetAllLinkIdsFromQuestionnaire(List<Questionnaire.ItemComponent> items)
    {
        var linkIds = new List<string>();
        foreach (var item in items)
        {
            linkIds.Add(item.LinkId);
            if (item.Item is { Count: > 0 })
                linkIds.AddRange(GetAllLinkIdsFromQuestionnaire(item.Item));
        }
        return linkIds;
    }
}
