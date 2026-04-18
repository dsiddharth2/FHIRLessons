using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;

namespace FhirLearning.Services;

public class QuestionnaireService
{
    private readonly FhirClient _client;

    public QuestionnaireService(FhirClient client)
    {
        _client = client;
    }

    public async Task<Questionnaire> CreatePromQuestionnaireAsync()
    {
        var questionnaire = new Questionnaire
        {
            Text = new Narrative
            {
                Status = Narrative.NarrativeStatus.Generated,
                Div = "<div xmlns=\"http://www.w3.org/1999/xhtml\">PRoM Test Questionnaire — general health, pain, and mental health assessment</div>"
            },
            Url = "http://example.org/fhir/Questionnaire/prom-test",
            Version = "1.0.0",
            Name = "PromTestQuestionnaire",
            Title = "PRoM Test Questionnaire",
            Status = PublicationStatus.Active,
            SubjectType = [ResourceType.Patient],
            Date = "2026-04-18",
            Publisher = "FHIR Learning Project",
            Description = new Markdown("A test Patient Reported Outcome Measure questionnaire covering general health, pain assessment, and mental health."),
            Item =
            [
                BuildGeneralHealthGroup(),
                BuildPainAssessmentGroup(),
                BuildMentalHealthGroup(),
                new Questionnaire.ItemComponent
                {
                    LinkId = "4",
                    Text = "Additional comments or concerns",
                    Type = Questionnaire.QuestionnaireItemType.Text,
                    Required = false
                }
            ]
        };

        return await _client.CreateAsync(questionnaire);
    }

    public async Task<Questionnaire> ReadQuestionnaireAsync(string id)
    {
        return await _client.ReadAsync<Questionnaire>($"Questionnaire/{id}");
    }

    public async Task<Bundle> SearchQuestionnairesByTitleAsync(string title)
    {
        var searchParams = new SearchParams();
        searchParams.Add("title", title);
        return await _client.SearchAsync<Questionnaire>(searchParams);
    }

    public async Task<Bundle> SearchQuestionnairesByUrlAsync(string url)
    {
        var searchParams = new SearchParams();
        searchParams.Add("url", url);
        return await _client.SearchAsync<Questionnaire>(searchParams);
    }

    public async Task<Bundle> SearchQuestionnairesByStatusAsync(string status)
    {
        var searchParams = new SearchParams();
        searchParams.Add("status", status);
        return await _client.SearchAsync<Questionnaire>(searchParams);
    }

    private static Questionnaire.ItemComponent BuildGeneralHealthGroup()
    {
        return new Questionnaire.ItemComponent
        {
            LinkId = "1",
            Text = "General Health",
            Type = Questionnaire.QuestionnaireItemType.Group,
            Item =
            [
                new Questionnaire.ItemComponent
                {
                    LinkId = "1.1",
                    Text = "In general, how would you rate your overall health?",
                    Type = Questionnaire.QuestionnaireItemType.Choice,
                    Required = true,
                    AnswerOption =
                    [
                        AnswerOption("excellent", "Excellent"),
                        AnswerOption("very-good", "Very Good"),
                        AnswerOption("good", "Good"),
                        AnswerOption("fair", "Fair"),
                        AnswerOption("poor", "Poor")
                    ]
                },
                new Questionnaire.ItemComponent
                {
                    LinkId = "1.2",
                    Text = "How many days per week are you physically active for at least 30 minutes?",
                    Type = Questionnaire.QuestionnaireItemType.Integer,
                    Required = true
                }
            ]
        };
    }

    private static Questionnaire.ItemComponent BuildPainAssessmentGroup()
    {
        return new Questionnaire.ItemComponent
        {
            LinkId = "2",
            Text = "Pain Assessment",
            Type = Questionnaire.QuestionnaireItemType.Group,
            Item =
            [
                new Questionnaire.ItemComponent
                {
                    LinkId = "2.1",
                    Text = "Rate your current pain level (0 = no pain, 10 = worst possible pain)",
                    Type = Questionnaire.QuestionnaireItemType.Integer,
                    Required = true
                },
                new Questionnaire.ItemComponent
                {
                    LinkId = "2.2",
                    Text = "Does pain interfere with your daily activities?",
                    Type = Questionnaire.QuestionnaireItemType.Boolean,
                    Required = true
                }
            ]
        };
    }

    private static Questionnaire.ItemComponent BuildMentalHealthGroup()
    {
        return new Questionnaire.ItemComponent
        {
            LinkId = "3",
            Text = "Mental Health",
            Type = Questionnaire.QuestionnaireItemType.Group,
            Item =
            [
                new Questionnaire.ItemComponent
                {
                    LinkId = "3.1",
                    Text = "Over the last 2 weeks, how often have you felt down, depressed, or hopeless?",
                    Type = Questionnaire.QuestionnaireItemType.Choice,
                    Required = true,
                    AnswerOption =
                    [
                        AnswerOption("not-at-all", "Not at all"),
                        AnswerOption("several-days", "Several days"),
                        AnswerOption("more-than-half", "More than half the days"),
                        AnswerOption("nearly-every-day", "Nearly every day")
                    ]
                },
                new Questionnaire.ItemComponent
                {
                    LinkId = "3.2",
                    Text = "Over the last 2 weeks, how often have you felt nervous, anxious, or on edge?",
                    Type = Questionnaire.QuestionnaireItemType.Choice,
                    Required = true,
                    AnswerOption =
                    [
                        AnswerOption("not-at-all", "Not at all"),
                        AnswerOption("several-days", "Several days"),
                        AnswerOption("more-than-half", "More than half the days"),
                        AnswerOption("nearly-every-day", "Nearly every day")
                    ]
                }
            ]
        };
    }

    private static Questionnaire.AnswerOptionComponent AnswerOption(string code, string display)
    {
        return new Questionnaire.AnswerOptionComponent
        {
            Value = new Coding("http://example.org/fhir/CodeSystem/prom-answers", code, display)
        };
    }
}
