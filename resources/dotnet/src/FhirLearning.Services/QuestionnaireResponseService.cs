using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;

namespace FhirLearning.Services;

public class QuestionnaireResponseService
{
    private readonly FhirClient _client;

    public QuestionnaireResponseService(FhirClient client)
    {
        _client = client;
    }

    public async Task<QuestionnaireResponse> CreateCompletedResponseAsync(
        string questionnaireUrl,
        string patientId,
        string? encounterId = null)
    {
        var response = new QuestionnaireResponse
        {
            Text = new Narrative
            {
                Status = Narrative.NarrativeStatus.Generated,
                Div = $"<div xmlns=\"http://www.w3.org/1999/xhtml\">Completed PRoM questionnaire response for Patient/{patientId}</div>"
            },
            Questionnaire = questionnaireUrl,
            Status = QuestionnaireResponse.QuestionnaireResponseStatus.Completed,
            Subject = new ResourceReference($"Patient/{patientId}"),
            Authored = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            Author = new ResourceReference($"Patient/{patientId}"),
            Item =
            [
                BuildGeneralHealthAnswers(),
                BuildPainAssessmentAnswers(),
                BuildMentalHealthAnswers(),
                new QuestionnaireResponse.ItemComponent
                {
                    LinkId = "4",
                    Text = "Additional comments or concerns",
                    Answer =
                    [
                        new QuestionnaireResponse.AnswerComponent
                        {
                            Value = new FhirString("Occasional mild headache in the evenings")
                        }
                    ]
                }
            ]
        };

        if (encounterId != null)
            response.Encounter = new ResourceReference($"Encounter/{encounterId}");

        return await _client.CreateAsync(response);
    }

    public async Task<QuestionnaireResponse> ReadResponseAsync(string id)
    {
        return await _client.ReadAsync<QuestionnaireResponse>($"QuestionnaireResponse/{id}");
    }

    public async Task<Bundle> SearchByPatientAsync(string patientId, int? count = null)
    {
        var searchParams = new SearchParams();
        searchParams.Add("subject", $"Patient/{patientId}");
        if (count.HasValue)
            searchParams.Count = count.Value;
        return await _client.SearchAsync<QuestionnaireResponse>(searchParams);
    }

    public async Task<Bundle> SearchByQuestionnaireAsync(string questionnaireUrl)
    {
        var searchParams = new SearchParams();
        searchParams.Add("questionnaire", questionnaireUrl);
        return await _client.SearchAsync<QuestionnaireResponse>(searchParams);
    }

    public async Task<Bundle> SearchByPatientAndQuestionnaireAsync(
        string patientId, string questionnaireUrl)
    {
        var searchParams = new SearchParams();
        searchParams.Add("subject", $"Patient/{patientId}");
        searchParams.Add("questionnaire", questionnaireUrl);
        return await _client.SearchAsync<QuestionnaireResponse>(searchParams);
    }

    private static QuestionnaireResponse.ItemComponent BuildGeneralHealthAnswers()
    {
        return new QuestionnaireResponse.ItemComponent
        {
            LinkId = "1",
            Text = "General Health",
            Item =
            [
                new QuestionnaireResponse.ItemComponent
                {
                    LinkId = "1.1",
                    Text = "In general, how would you rate your overall health?",
                    Answer =
                    [
                        new QuestionnaireResponse.AnswerComponent
                        {
                            Value = new Coding(
                                "http://example.org/fhir/CodeSystem/prom-answers",
                                "good", "Good")
                        }
                    ]
                },
                new QuestionnaireResponse.ItemComponent
                {
                    LinkId = "1.2",
                    Text = "How many days per week are you physically active for at least 30 minutes?",
                    Answer =
                    [
                        new QuestionnaireResponse.AnswerComponent
                        {
                            Value = new Integer(4)
                        }
                    ]
                }
            ]
        };
    }

    private static QuestionnaireResponse.ItemComponent BuildPainAssessmentAnswers()
    {
        return new QuestionnaireResponse.ItemComponent
        {
            LinkId = "2",
            Text = "Pain Assessment",
            Item =
            [
                new QuestionnaireResponse.ItemComponent
                {
                    LinkId = "2.1",
                    Text = "Rate your current pain level (0 = no pain, 10 = worst possible pain)",
                    Answer =
                    [
                        new QuestionnaireResponse.AnswerComponent
                        {
                            Value = new Integer(3)
                        }
                    ]
                },
                new QuestionnaireResponse.ItemComponent
                {
                    LinkId = "2.2",
                    Text = "Does pain interfere with your daily activities?",
                    Answer =
                    [
                        new QuestionnaireResponse.AnswerComponent
                        {
                            Value = new FhirBoolean(false)
                        }
                    ]
                }
            ]
        };
    }

    private static QuestionnaireResponse.ItemComponent BuildMentalHealthAnswers()
    {
        return new QuestionnaireResponse.ItemComponent
        {
            LinkId = "3",
            Text = "Mental Health",
            Item =
            [
                new QuestionnaireResponse.ItemComponent
                {
                    LinkId = "3.1",
                    Text = "Over the last 2 weeks, how often have you felt down, depressed, or hopeless?",
                    Answer =
                    [
                        new QuestionnaireResponse.AnswerComponent
                        {
                            Value = new Coding(
                                "http://example.org/fhir/CodeSystem/prom-answers",
                                "several-days", "Several days")
                        }
                    ]
                },
                new QuestionnaireResponse.ItemComponent
                {
                    LinkId = "3.2",
                    Text = "Over the last 2 weeks, how often have you felt nervous, anxious, or on edge?",
                    Answer =
                    [
                        new QuestionnaireResponse.AnswerComponent
                        {
                            Value = new Coding(
                                "http://example.org/fhir/CodeSystem/prom-answers",
                                "not-at-all", "Not at all")
                        }
                    ]
                }
            ]
        };
    }
}
