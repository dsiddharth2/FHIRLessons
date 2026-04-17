using Hl7.Fhir.Rest;

namespace FhirLearning.Services;

public static class FhirClientFactory
{
    private const string DefaultServerUrl = "http://localhost:8080/fhir";

    public static FhirClient Create(string? serverUrl = null)
    {
        var settings = new FhirClientSettings
        {
            PreferredFormat = ResourceFormat.Json,
            ReturnPreference = ReturnPreference.Representation
        };

        return new FhirClient(serverUrl ?? DefaultServerUrl, settings);
    }
}
