using Hl7.Fhir.Model;
using Task = System.Threading.Tasks.Task;

namespace FhirLearning.Tests;

public class Step1_ServerConnectivityTests : IClassFixture<FhirServerFixture>
{
    private readonly FhirServerFixture _fixture;

    public Step1_ServerConnectivityTests(FhirServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void Server_ReturnsCapabilityStatement()
    {
        Assert.NotNull(_fixture.CapabilityStatement);
        Assert.Equal(FHIRVersion.N4_0_1, _fixture.CapabilityStatement.FhirVersion);
    }

    [Fact]
    public void Server_SupportsPatientResource()
    {
        var patientResource = _fixture.CapabilityStatement.Rest
            .SelectMany(r => r.Resource)
            .FirstOrDefault(r => r.Type == "Patient");

        Assert.NotNull(patientResource);
    }
}
