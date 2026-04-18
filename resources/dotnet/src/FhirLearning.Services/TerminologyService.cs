using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Task = System.Threading.Tasks.Task;

namespace FhirLearning.Services;

public class TerminologyService
{
    private readonly FhirClient _client;

    public TerminologyService(FhirClient client)
    {
        _client = client;
    }

    public async Task<bool> IsCdcRaceEthnicityIndexedAsync()
    {
        try
        {
            var parameters = new Parameters();
            parameters.Add("url", new FhirUri("urn:oid:2.16.840.1.113883.6.238"));
            parameters.Add("code", new Code("2106-3"));

            var result = await _client.TypeOperationAsync("validate-code", "CodeSystem", parameters);
            if (result is Parameters p)
            {
                var resultParam = p.Parameter.FirstOrDefault(x => x.Name == "result");
                return resultParam?.Value is FhirBoolean b && b.Value == true;
            }
        }
        catch { }

        return false;
    }

    public async Task EnsureCdcRaceEthnicityLoadedAsync()
    {
        if (await IsCdcRaceEthnicityIndexedAsync())
            return;

        const string cdcSystem = "urn:oid:2.16.840.1.113883.6.238";

        var codeSystem = new CodeSystem
        {
            Id = "cdcrec",
            Url = cdcSystem,
            Name = "CDCRaceAndEthnicity",
            Title = "CDC Race and Ethnicity",
            Status = PublicationStatus.Active,
            Content = CodeSystemContentMode.Complete,
            CaseSensitive = true,
            Version = "1.2",
            Concept =
            [
                new CodeSystem.ConceptDefinitionComponent { Code = "1002-5", Display = "American Indian or Alaska Native" },
                new CodeSystem.ConceptDefinitionComponent { Code = "2028-9", Display = "Asian" },
                new CodeSystem.ConceptDefinitionComponent { Code = "2054-5", Display = "Black or African American" },
                new CodeSystem.ConceptDefinitionComponent { Code = "2076-8", Display = "Native Hawaiian or Other Pacific Islander" },
                new CodeSystem.ConceptDefinitionComponent { Code = "2106-3", Display = "White" },
                new CodeSystem.ConceptDefinitionComponent { Code = "2135-2", Display = "Hispanic or Latino" },
                new CodeSystem.ConceptDefinitionComponent { Code = "2186-5", Display = "Not Hispanic or Latino" },
            ]
        };

        await _client.UpdateAsync(codeSystem);

        // US Core's omb-race-category and omb-ethnicity-category ValueSets reference
        // external NLM VSAC ValueSets that HAPI can't resolve. We replace them with
        // pre-expanded versions containing the actual OMB codes.
        await ReplaceValueSetWithExpansionAsync(
            "http://hl7.org/fhir/us/core/ValueSet/omb-race-category",
            "OMB Race Categories",
            cdcSystem,
            [
                ("1002-5", "American Indian or Alaska Native"),
                ("2028-9", "Asian"),
                ("2054-5", "Black or African American"),
                ("2076-8", "Native Hawaiian or Other Pacific Islander"),
                ("2106-3", "White"),
            ]);

        await ReplaceValueSetWithExpansionAsync(
            "http://hl7.org/fhir/us/core/ValueSet/omb-ethnicity-category",
            "OMB Ethnicity Categories",
            cdcSystem,
            [
                ("2135-2", "Hispanic or Latino"),
                ("2186-5", "Not Hispanic or Latino"),
            ]);
    }

    private async Task ReplaceValueSetWithExpansionAsync(
        string url, string name, string system,
        (string code, string display)[] codes)
    {
        // Find the existing ValueSet loaded by the US Core IG
        var searchParams = new SearchParams();
        searchParams.Add("url", url);
        searchParams.Count = 1;
        var bundle = await _client.SearchAsync<ValueSet>(searchParams);

        if (bundle.Entry.Count == 0)
            return;

        var existing = (ValueSet)bundle.Entry[0].Resource;

        // Replace compose with direct code references and add a pre-built expansion
        existing.Compose = new ValueSet.ComposeComponent
        {
            Include =
            [
                new ValueSet.ConceptSetComponent
                {
                    System = system,
                    Concept = codes.Select(c =>
                        new ValueSet.ConceptReferenceComponent { Code = c.code, Display = c.display }).ToList()
                }
            ]
        };

        existing.Expansion = new ValueSet.ExpansionComponent
        {
            Timestamp = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            Contains = codes.Select(c => new ValueSet.ContainsComponent
            {
                System = system,
                Code = c.code,
                Display = c.display
            }).ToList()
        };

        await _client.UpdateAsync(existing);
    }
}
