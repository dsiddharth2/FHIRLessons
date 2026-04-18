using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Serialization;

namespace FhirLearning.Services;

public class ValidationService
{
    private readonly FhirClient _client;

    public ValidationService(FhirClient client)
    {
        _client = client;
    }

    public async Task<OperationOutcome> ValidateResourceAsync(Resource resource, string? profileUrl = null)
    {
        var parameters = new Parameters();
        parameters.Add("resource", resource);

        if (profileUrl != null)
            parameters.Add("profile", new FhirUri(profileUrl));

        try
        {
            var result = await _client.TypeOperationAsync(
                "validate", resource.TypeName, parameters);
            return (OperationOutcome)result;
        }
        catch (FhirOperationException ex) when (ex.Outcome != null)
        {
            return ex.Outcome;
        }
    }

    public async Task<OperationOutcome> ValidateExistingResourceAsync(string resourceType, string id)
    {
        var result = await _client.InstanceOperationAsync(
            new Uri($"{resourceType}/{id}", UriKind.Relative), "validate");
        return (OperationOutcome)result;
    }

    public static List<OperationOutcome.IssueComponent> GetErrors(OperationOutcome outcome)
    {
        return outcome.Issue
            .Where(i => i.Severity is OperationOutcome.IssueSeverity.Error
                     or OperationOutcome.IssueSeverity.Fatal)
            .ToList();
    }

    public static List<OperationOutcome.IssueComponent> GetWarnings(OperationOutcome outcome)
    {
        return outcome.Issue
            .Where(i => i.Severity == OperationOutcome.IssueSeverity.Warning)
            .ToList();
    }

    public static bool IsValid(OperationOutcome outcome)
    {
        return !outcome.Issue.Any(i =>
            i.Severity is OperationOutcome.IssueSeverity.Error
                       or OperationOutcome.IssueSeverity.Fatal);
    }
}
