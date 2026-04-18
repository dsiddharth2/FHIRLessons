using FhirLearning.Services;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Xunit.Abstractions;
using Task = System.Threading.Tasks.Task;

namespace FhirLearning.Tests;

public class Step8_ImplementationGuideTests : IClassFixture<FhirServerFixture>
{
    private readonly ImplementationGuideService _igService;
    private readonly ITestOutputHelper _output;

    public Step8_ImplementationGuideTests(FhirServerFixture fixture, ITestOutputHelper output)
    {
        _igService = new ImplementationGuideService(fixture.Client);
        _output = output;
    }

    [Fact]
    public async Task UsCoreProfiles_AreLoaded()
    {
        var bundle = await _igService.SearchStructureDefinitionsAsync(count: 100);

        var usCoreProfiles = bundle.Entry
            .Select(e => (StructureDefinition)e.Resource)
            .Where(sd => sd.Url != null && sd.Url.Contains("us-core"))
            .ToList();

        _output.WriteLine($"Total StructureDefinitions: {bundle.Total}");
        _output.WriteLine($"US Core profiles: {usCoreProfiles.Count}");
        foreach (var sd in usCoreProfiles)
            _output.WriteLine($"  - {sd.Name} ({sd.Url})");

        Assert.True(usCoreProfiles.Count > 0, "No US Core StructureDefinitions found — is the IG loaded?");
    }

    [Fact]
    public async Task UsCorePatientProfile_Exists()
    {
        var profile = await _igService.GetStructureDefinitionByUrlAsync(
            "http://hl7.org/fhir/us/core/StructureDefinition/us-core-patient");

        Assert.NotNull(profile);

        _output.WriteLine($"\n=== US Core Patient Profile ===");
        _output.WriteLine($"Name: {profile.Name}");
        _output.WriteLine($"URL: {profile.Url}");
        _output.WriteLine($"Type: {profile.Type}");
        _output.WriteLine($"Base: {profile.BaseDefinition}");
        _output.WriteLine($"Status: {profile.Status}");

        Assert.Equal("Patient", profile.Type);
        Assert.Equal("http://hl7.org/fhir/StructureDefinition/Patient", profile.BaseDefinition);
    }

    [Fact]
    public async Task UsCorePatientProfile_RequiresNameGenderIdentifier()
    {
        var profile = await _igService.GetStructureDefinitionByUrlAsync(
            "http://hl7.org/fhir/us/core/StructureDefinition/us-core-patient");

        Assert.NotNull(profile);

        var differential = profile.Differential.Element;

        _output.WriteLine("=== US Core Patient — Differential Elements ===");
        foreach (var element in differential)
        {
            var flags = new List<string>();
            if (element.Min > 0) flags.Add($"min:{element.Min}");
            if (element.MustSupport == true) flags.Add("must-support");
            var flagStr = flags.Count > 0 ? $" [{string.Join(", ", flags)}]" : "";
            _output.WriteLine($"  {element.Path}{flagStr}");
        }

        Assert.Contains(differential, e => e.Path == "Patient.identifier" && e.Min > 0);
        Assert.Contains(differential, e => e.Path == "Patient.name" && e.Min > 0);
        Assert.Contains(differential, e => e.Path == "Patient.gender" && e.Min > 0);
    }

    [Fact]
    public async Task UsCoreConditionProfile_Exists()
    {
        var profile = await _igService.GetStructureDefinitionByUrlAsync(
            "http://hl7.org/fhir/us/core/StructureDefinition/us-core-condition-encounter-diagnosis");

        Assert.NotNull(profile);

        _output.WriteLine($"Name: {profile.Name}");
        _output.WriteLine($"URL: {profile.Url}");
        _output.WriteLine($"Type: {profile.Type}");

        Assert.Equal("Condition", profile.Type);
    }

    [Fact]
    public async Task UsCoreVitalSignsProfile_Exists()
    {
        var profile = await _igService.GetStructureDefinitionByUrlAsync(
            "http://hl7.org/fhir/us/core/StructureDefinition/us-core-vital-signs");

        Assert.NotNull(profile);

        _output.WriteLine($"Name: {profile.Name}");
        _output.WriteLine($"URL: {profile.Url}");
        _output.WriteLine($"Type: {profile.Type}");

        Assert.Equal("Observation", profile.Type);
    }

    [Fact]
    public async Task UsCoreProcedureProfile_Exists()
    {
        var profile = await _igService.GetStructureDefinitionByUrlAsync(
            "http://hl7.org/fhir/us/core/StructureDefinition/us-core-procedure");

        Assert.NotNull(profile);

        _output.WriteLine($"Name: {profile.Name}");
        _output.WriteLine($"URL: {profile.Url}");
        _output.WriteLine($"Type: {profile.Type}");

        Assert.Equal("Procedure", profile.Type);
    }

    [Fact]
    public async Task UsCoreValueSets_AreLoaded()
    {
        var bundle = await _igService.SearchValueSetsAsync(count: 100);

        var usCoreValueSets = bundle.Entry
            .Select(e => (ValueSet)e.Resource)
            .Where(vs => vs.Url != null && vs.Url.Contains("us-core"))
            .ToList();

        _output.WriteLine($"Total ValueSets: {bundle.Total}");
        _output.WriteLine($"US Core ValueSets: {usCoreValueSets.Count}");
        foreach (var vs in usCoreValueSets)
            _output.WriteLine($"  - {vs.Name} ({vs.Url})");

        Assert.True(usCoreValueSets.Count > 0, "No US Core ValueSets found — is the IG loaded?");
    }

    [Fact]
    public async Task BirthSexValueSet_Exists()
    {
        var valueSet = await _igService.GetValueSetByUrlAsync(
            "http://hl7.org/fhir/us/core/ValueSet/birthsex");

        Assert.NotNull(valueSet);

        _output.WriteLine($"Name: {valueSet.Name}");
        _output.WriteLine($"URL: {valueSet.Url}");
        _output.WriteLine($"Status: {valueSet.Status}");
    }

    [Fact]
    public async Task OmbRaceCategoryValueSet_Exists()
    {
        var valueSet = await _igService.GetValueSetByUrlAsync(
            "http://hl7.org/fhir/us/core/ValueSet/omb-race-category");

        Assert.NotNull(valueSet);

        _output.WriteLine($"Name: {valueSet.Name}");
        _output.WriteLine($"URL: {valueSet.Url}");
    }

    [Fact]
    public async Task AllCoreProfilesForOurResources_ArePresent()
    {
        var profileUrls = new[]
        {
            "http://hl7.org/fhir/us/core/StructureDefinition/us-core-patient",
            "http://hl7.org/fhir/us/core/StructureDefinition/us-core-encounter",
            "http://hl7.org/fhir/us/core/StructureDefinition/us-core-condition-encounter-diagnosis",
            "http://hl7.org/fhir/us/core/StructureDefinition/us-core-procedure",
        };

        _output.WriteLine("=== Checking all US Core profiles for our resource types ===");

        foreach (var url in profileUrls)
        {
            var profile = await _igService.GetStructureDefinitionByUrlAsync(url);
            var shortName = url.Split('/').Last();
            _output.WriteLine($"  {shortName}: {(profile != null ? "LOADED" : "MISSING")}");
            Assert.NotNull(profile);
        }
    }
}
