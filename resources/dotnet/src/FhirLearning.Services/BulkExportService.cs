using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Serialization;

namespace FhirLearning.Services;

public class BulkExportService
{
    private readonly FhirClient _client;

    public BulkExportService(FhirClient client)
    {
        _client = client;
    }

    public async Task<Bundle> PatientEverythingAsync(string patientId)
    {
        return await _client.InstanceOperationAsync(
            new Uri($"Patient/{patientId}", UriKind.Relative),
            "everything") as Bundle
            ?? throw new InvalidOperationException("$everything did not return a Bundle");
    }

    public async Task<List<T>> ExportResourceTypeAsync<T>(int? maxPages = null) where T : Resource, new()
    {
        var resources = new List<T>();
        var searchParams = new SearchParams { Count = 100 };

        var bundle = await _client.SearchAsync<T>(searchParams);
        var pageCount = 0;

        while (bundle != null)
        {
            pageCount++;

            foreach (var entry in bundle.Entry)
            {
                if (entry.Resource is T resource)
                    resources.Add(resource);
            }

            if (maxPages.HasValue && pageCount >= maxPages.Value)
                break;

            bundle = await _client.ContinueAsync(bundle);
        }

        return resources;
    }

    public async Task<Bundle> ExportSinceAsync<T>(DateTimeOffset since) where T : Resource, new()
    {
        var searchParams = new SearchParams();
        searchParams.Add("_lastUpdated", $"ge{since:yyyy-MM-ddTHH:mm:ssZ}");
        return await _client.SearchAsync<T>(searchParams);
    }

    public static string ToNdjson<T>(List<T> resources) where T : Resource
    {
        var lines = resources.Select(r => r.ToJson());
        return string.Join("\n", lines);
    }
}
