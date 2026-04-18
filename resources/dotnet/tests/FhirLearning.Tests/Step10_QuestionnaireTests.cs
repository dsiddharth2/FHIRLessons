using FhirLearning.Services;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Xunit.Abstractions;
using Task = System.Threading.Tasks.Task;

namespace FhirLearning.Tests;

public class Step10_QuestionnaireTests : IClassFixture<FhirServerFixture>
{
    private readonly QuestionnaireService _questionnaireService;
    private readonly ITestOutputHelper _output;

    public Step10_QuestionnaireTests(FhirServerFixture fixture, ITestOutputHelper output)
    {
        _questionnaireService = new QuestionnaireService(fixture.Client);
        _output = output;
    }

    [Fact]
    public async Task CreatePromQuestionnaire_ReturnsWithId()
    {
        var questionnaire = await _questionnaireService.CreatePromQuestionnaireAsync();

        _output.WriteLine($"Created Questionnaire/{questionnaire.Id}");
        _output.WriteLine(questionnaire.ToJson(pretty: true));

        Assert.NotNull(questionnaire.Id);
        Assert.Equal("PRoM Test Questionnaire", questionnaire.Title);
        Assert.Equal(PublicationStatus.Active, questionnaire.Status);
    }

    [Fact]
    public async Task CreatePromQuestionnaire_HasCorrectStructure()
    {
        var questionnaire = await _questionnaireService.CreatePromQuestionnaireAsync();

        Assert.Equal(4, questionnaire.Item.Count);

        var generalHealth = questionnaire.Item[0];
        Assert.Equal("1", generalHealth.LinkId);
        Assert.Equal("General Health", generalHealth.Text);
        Assert.Equal(Questionnaire.QuestionnaireItemType.Group, generalHealth.Type);
        Assert.Equal(2, generalHealth.Item.Count);

        var painAssessment = questionnaire.Item[1];
        Assert.Equal("2", painAssessment.LinkId);
        Assert.Equal("Pain Assessment", painAssessment.Text);
        Assert.Equal(Questionnaire.QuestionnaireItemType.Group, painAssessment.Type);
        Assert.Equal(2, painAssessment.Item.Count);

        var mentalHealth = questionnaire.Item[2];
        Assert.Equal("3", mentalHealth.LinkId);
        Assert.Equal("Mental Health", mentalHealth.Text);
        Assert.Equal(Questionnaire.QuestionnaireItemType.Group, mentalHealth.Type);
        Assert.Equal(2, mentalHealth.Item.Count);

        var comments = questionnaire.Item[3];
        Assert.Equal("4", comments.LinkId);
        Assert.Equal(Questionnaire.QuestionnaireItemType.Text, comments.Type);
        Assert.False(comments.Required);
    }

    [Fact]
    public async Task CreatePromQuestionnaire_HealthRatingHasFiveOptions()
    {
        var questionnaire = await _questionnaireService.CreatePromQuestionnaireAsync();

        var healthRating = questionnaire.Item[0].Item[0];
        Assert.Equal("1.1", healthRating.LinkId);
        Assert.Equal(Questionnaire.QuestionnaireItemType.Choice, healthRating.Type);
        Assert.True(healthRating.Required);
        Assert.Equal(5, healthRating.AnswerOption.Count);

        var options = healthRating.AnswerOption
            .Select(o => ((Coding)o.Value).Display)
            .ToList();

        Assert.Contains("Excellent", options);
        Assert.Contains("Very Good", options);
        Assert.Contains("Good", options);
        Assert.Contains("Fair", options);
        Assert.Contains("Poor", options);
    }

    [Fact]
    public async Task CreatePromQuestionnaire_MentalHealthHasFrequencyScale()
    {
        var questionnaire = await _questionnaireService.CreatePromQuestionnaireAsync();

        var depression = questionnaire.Item[2].Item[0];
        Assert.Equal("3.1", depression.LinkId);
        Assert.Equal(Questionnaire.QuestionnaireItemType.Choice, depression.Type);
        Assert.Equal(4, depression.AnswerOption.Count);

        var anxiety = questionnaire.Item[2].Item[1];
        Assert.Equal("3.2", anxiety.LinkId);
        Assert.Equal(Questionnaire.QuestionnaireItemType.Choice, anxiety.Type);
        Assert.Equal(4, anxiety.AnswerOption.Count);

        var frequencyOptions = depression.AnswerOption
            .Select(o => ((Coding)o.Value).Display)
            .ToList();

        Assert.Contains("Not at all", frequencyOptions);
        Assert.Contains("Nearly every day", frequencyOptions);
    }

    [Fact]
    public async Task CreatePromQuestionnaire_PainItemsHaveCorrectTypes()
    {
        var questionnaire = await _questionnaireService.CreatePromQuestionnaireAsync();

        var painLevel = questionnaire.Item[1].Item[0];
        Assert.Equal("2.1", painLevel.LinkId);
        Assert.Equal(Questionnaire.QuestionnaireItemType.Integer, painLevel.Type);
        Assert.True(painLevel.Required);

        var painInterferes = questionnaire.Item[1].Item[1];
        Assert.Equal("2.2", painInterferes.LinkId);
        Assert.Equal(Questionnaire.QuestionnaireItemType.Boolean, painInterferes.Type);
        Assert.True(painInterferes.Required);
    }

    [Fact]
    public async Task ReadQuestionnaire_ReturnsSameResource()
    {
        var created = await _questionnaireService.CreatePromQuestionnaireAsync();

        var fetched = await _questionnaireService.ReadQuestionnaireAsync(created.Id);

        _output.WriteLine($"Read Questionnaire/{fetched.Id}");

        Assert.Equal(created.Id, fetched.Id);
        Assert.Equal(created.Title, fetched.Title);
        Assert.Equal(created.Url, fetched.Url);
        Assert.Equal(created.Item.Count, fetched.Item.Count);
    }

    [Fact]
    public async Task SearchByTitle_FindsQuestionnaire()
    {
        await _questionnaireService.CreatePromQuestionnaireAsync();

        var bundle = await _questionnaireService.SearchQuestionnairesByTitleAsync("PRoM");

        _output.WriteLine($"Search by title returned {bundle.Entry.Count} result(s)");

        Assert.NotEmpty(bundle.Entry);
        var found = (Questionnaire)bundle.Entry[0].Resource;
        Assert.Contains("PRoM", found.Title);
    }

    [Fact]
    public async Task SearchByUrl_FindsQuestionnaire()
    {
        await _questionnaireService.CreatePromQuestionnaireAsync();

        var bundle = await _questionnaireService.SearchQuestionnairesByUrlAsync(
            "http://example.org/fhir/Questionnaire/prom-test");

        _output.WriteLine($"Search by URL returned {bundle.Entry.Count} result(s)");

        Assert.NotEmpty(bundle.Entry);
        var found = (Questionnaire)bundle.Entry[0].Resource;
        Assert.Equal("http://example.org/fhir/Questionnaire/prom-test", found.Url);
    }

    [Fact]
    public async Task SearchByStatus_FindsActiveQuestionnaires()
    {
        await _questionnaireService.CreatePromQuestionnaireAsync();

        var bundle = await _questionnaireService.SearchQuestionnairesByStatusAsync("active");

        _output.WriteLine($"Search by status=active returned {bundle.Entry.Count} result(s)");

        Assert.NotEmpty(bundle.Entry);
        Assert.All(bundle.Entry, entry =>
        {
            var q = (Questionnaire)entry.Resource;
            Assert.Equal(PublicationStatus.Active, q.Status);
        });
    }

    [Fact]
    public async Task CreatePromQuestionnaire_HasCanonicalUrl()
    {
        var questionnaire = await _questionnaireService.CreatePromQuestionnaireAsync();

        Assert.Equal("http://example.org/fhir/Questionnaire/prom-test", questionnaire.Url);
        Assert.Equal("1.0.0", questionnaire.Version);
        Assert.Equal("PromTestQuestionnaire", questionnaire.Name);
    }

    [Fact]
    public async Task CreatePromQuestionnaire_AllRequiredQuestionsMarkedRequired()
    {
        var questionnaire = await _questionnaireService.CreatePromQuestionnaireAsync();

        var allQuestions = questionnaire.Item
            .Where(i => i.Type == Questionnaire.QuestionnaireItemType.Group)
            .SelectMany(g => g.Item)
            .ToList();

        _output.WriteLine($"Found {allQuestions.Count} questions across groups");
        foreach (var q in allQuestions)
        {
            _output.WriteLine($"  [{q.LinkId}] {q.Text} — required: {q.Required}");
        }

        Assert.All(allQuestions, q => Assert.True(q.Required,
            $"Question {q.LinkId} should be required"));

        var comments = questionnaire.Item.First(i => i.LinkId == "4");
        Assert.False(comments.Required);
    }
}
