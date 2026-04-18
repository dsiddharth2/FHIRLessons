using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;

namespace FhirLearning.Services;

public class ImplementationGuideService
{
    private readonly FhirClient _client;

    public ImplementationGuideService(FhirClient client)
    {
        _client = client;
    }

    public async Task<Bundle> SearchStructureDefinitionsAsync(string? urlFilter = null, int? count = null)
    {
        var searchParams = new SearchParams();

        if (urlFilter != null)
            searchParams.Add("url", urlFilter);

        if (count.HasValue)
            searchParams.Count = count.Value;

        return await _client.SearchAsync<StructureDefinition>(searchParams);
    }

    public async Task<StructureDefinition?> GetStructureDefinitionByUrlAsync(string profileUrl)
    {
        var searchParams = new SearchParams();
        searchParams.Add("url", profileUrl);
        searchParams.Count = 1;

        var bundle = await _client.SearchAsync<StructureDefinition>(searchParams);
        return bundle.Entry.Count > 0 ? (StructureDefinition)bundle.Entry[0].Resource : null;
    }

    public async Task<Bundle> SearchValueSetsAsync(string? urlFilter = null, int? count = null)
    {
        var searchParams = new SearchParams();

        if (urlFilter != null)
            searchParams.Add("url", urlFilter);

        if (count.HasValue)
            searchParams.Count = count.Value;

        return await _client.SearchAsync<ValueSet>(searchParams);
    }

    public async Task<ValueSet?> GetValueSetByUrlAsync(string valueSetUrl)
    {
        var searchParams = new SearchParams();
        searchParams.Add("url", valueSetUrl);
        searchParams.Count = 1;

        var bundle = await _client.SearchAsync<ValueSet>(searchParams);
        return bundle.Entry.Count > 0 ? (ValueSet)bundle.Entry[0].Resource : null;
    }

    public async Task<Bundle> SearchCodeSystemsAsync(string? urlFilter = null, int? count = null)
    {
        var searchParams = new SearchParams();

        if (urlFilter != null)
            searchParams.Add("url", urlFilter);

        if (count.HasValue)
            searchParams.Count = count.Value;

        return await _client.SearchAsync<CodeSystem>(searchParams);
    }
}
