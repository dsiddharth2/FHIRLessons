using FhirLearning.Services;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Task = System.Threading.Tasks.Task;

namespace FhirLearning.Tests;

public class FhirServerFixture : IAsyncLifetime
{
    public FhirClient Client { get; private set; } = null!;
    public CapabilityStatement CapabilityStatement { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        Client = FhirClientFactory.Create();
        CapabilityStatement = await Client.CapabilityStatementAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;
}
