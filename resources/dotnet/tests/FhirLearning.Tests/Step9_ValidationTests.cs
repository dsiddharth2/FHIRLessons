using FhirLearning.Services;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Xunit.Abstractions;
using Task = System.Threading.Tasks.Task;

namespace FhirLearning.Tests;

public class Step9_ValidationTests : IClassFixture<FhirServerFixture>
{
    private readonly ValidationService _validationService;
    private readonly ITestOutputHelper _output;

    private const string UsCorePatientProfile =
        "http://hl7.org/fhir/us/core/StructureDefinition/us-core-patient";

    public Step9_ValidationTests(FhirServerFixture fixture, ITestOutputHelper output)
    {
        _validationService = new ValidationService(fixture.Client);
        _output = output;
    }

    private void PrintOutcome(OperationOutcome outcome, string label)
    {
        _output.WriteLine($"\n=== {label} ===");
        _output.WriteLine($"Valid: {ValidationService.IsValid(outcome)}");
        _output.WriteLine($"Issues ({outcome.Issue.Count}):");
        foreach (var issue in outcome.Issue)
        {
            var location = issue.Location != null ? string.Join(", ", issue.Location) : "—";
            _output.WriteLine($"  [{issue.Severity}] {issue.Diagnostics}");
            _output.WriteLine($"    Location: {location}");
        }
    }

    private static Patient CreateValidUsCorePatient()
    {
        return new Patient
        {
            Text = new Narrative
            {
                Status = Narrative.NarrativeStatus.Generated,
                Div = "<div xmlns=\"http://www.w3.org/1999/xhtml\">Jane TestValid, Female, DOB: 1990-05-15</div>"
            },
            Meta = new Meta { Profile = [UsCorePatientProfile] },
            Identifier = [new Identifier("http://hospital.example.org/mrn", $"MRN-{Guid.NewGuid():N}")],
            Name = [new HumanName { Family = "TestValid", Given = ["Jane"] }],
            Gender = AdministrativeGender.Female,
            BirthDate = "1990-05-15"
        };
    }

    [Fact]
    public async Task ValidUsCorePatient_PassesValidation()
    {
        var patient = CreateValidUsCorePatient();

        var outcome = await _validationService.ValidateResourceAsync(patient);

        PrintOutcome(outcome, "Valid US Core Patient");

        Assert.True(ValidationService.IsValid(outcome));
    }

    [Fact]
    public async Task PatientMissingName_FailsValidation()
    {
        var patient = new Patient
        {
            Meta = new Meta { Profile = [UsCorePatientProfile] },
            Identifier = [new Identifier("http://hospital.example.org/mrn", "MRN-NONAME")],
            Gender = AdministrativeGender.Male
        };

        var outcome = await _validationService.ValidateResourceAsync(patient);

        PrintOutcome(outcome, "Patient missing name");

        var errors = ValidationService.GetErrors(outcome);
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Diagnostics != null && e.Diagnostics.Contains("name"));
    }

    [Fact]
    public async Task PatientMissingIdentifier_FailsValidation()
    {
        var patient = new Patient
        {
            Meta = new Meta { Profile = [UsCorePatientProfile] },
            Name = [new HumanName { Family = "NoId", Given = ["Test"] }],
            Gender = AdministrativeGender.Female
        };

        var outcome = await _validationService.ValidateResourceAsync(patient);

        PrintOutcome(outcome, "Patient missing identifier");

        var errors = ValidationService.GetErrors(outcome);
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Diagnostics != null && e.Diagnostics.Contains("identifier"));
    }

    [Fact]
    public async Task PatientMissingGender_FailsValidation()
    {
        var patient = new Patient
        {
            Meta = new Meta { Profile = [UsCorePatientProfile] },
            Identifier = [new Identifier("http://hospital.example.org/mrn", "MRN-NOGENDER")],
            Name = [new HumanName { Family = "NoGender", Given = ["Test"] }]
        };

        var outcome = await _validationService.ValidateResourceAsync(patient);

        PrintOutcome(outcome, "Patient missing gender");

        var errors = ValidationService.GetErrors(outcome);
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Diagnostics != null && e.Diagnostics.Contains("gender"));
    }

    [Fact]
    public async Task PatientMissingEverything_HasMultipleErrors()
    {
        var patient = new Patient
        {
            Meta = new Meta { Profile = [UsCorePatientProfile] }
        };

        var outcome = await _validationService.ValidateResourceAsync(patient);

        PrintOutcome(outcome, "Patient missing everything");

        var errors = ValidationService.GetErrors(outcome);
        Assert.True(errors.Count >= 3,
            $"Expected at least 3 errors (name, gender, identifier), got {errors.Count}");
    }

    [Fact]
    public async Task ValidPatient_MayHaveWarnings_ButNoErrors()
    {
        var patient = new Patient
        {
            Meta = new Meta { Profile = [UsCorePatientProfile] },
            Identifier = [new Identifier("http://hospital.example.org/mrn", "MRN-MINIMAL")],
            Name = [new HumanName { Family = "Minimal", Given = ["Test"] }],
            Gender = AdministrativeGender.Other
        };

        var outcome = await _validationService.ValidateResourceAsync(patient);

        PrintOutcome(outcome, "Minimal valid patient (no birthDate, no extensions)");

        Assert.True(ValidationService.IsValid(outcome),
            "A Patient with identifier + name + gender should pass US Core validation");

        var warnings = ValidationService.GetWarnings(outcome);
        _output.WriteLine($"\nWarnings count: {warnings.Count}");
    }

    [Fact]
    public async Task ValidateCondition_WithoutCode_FailsValidation()
    {
        var condition = new Condition
        {
            Meta = new Meta
            {
                Profile =
                [
                    "http://hl7.org/fhir/us/core/StructureDefinition/us-core-condition-encounter-diagnosis"
                ]
            },
            ClinicalStatus = new CodeableConcept(
                "http://terminology.hl7.org/CodeSystem/condition-clinical", "active"),
            VerificationStatus = new CodeableConcept(
                "http://terminology.hl7.org/CodeSystem/condition-ver-status", "confirmed"),
            Subject = new ResourceReference("Patient/1")
        };

        var outcome = await _validationService.ValidateResourceAsync(condition);

        PrintOutcome(outcome, "Condition missing code");

        var errors = ValidationService.GetErrors(outcome);
        Assert.NotEmpty(errors);
    }

    [Fact]
    public async Task ValidateObservation_WithoutStatus_FailsValidation()
    {
        var observation = new Observation
        {
            Meta = new Meta
            {
                Profile =
                [
                    "http://hl7.org/fhir/us/core/StructureDefinition/us-core-vital-signs"
                ]
            },
            Code = new CodeableConcept("http://loinc.org", "8310-5", "Body temperature"),
            Subject = new ResourceReference("Patient/1"),
            Effective = new FhirDateTime(DateTimeOffset.UtcNow)
        };

        var outcome = await _validationService.ValidateResourceAsync(observation);

        PrintOutcome(outcome, "Observation missing status");

        var errors = ValidationService.GetErrors(outcome);
        Assert.NotEmpty(errors);
    }

    [Fact]
    public async Task ValidationOutcome_ContainsDiagnosticDetails()
    {
        var patient = new Patient
        {
            Meta = new Meta { Profile = [UsCorePatientProfile] }
        };

        var outcome = await _validationService.ValidateResourceAsync(patient);

        PrintOutcome(outcome, "Checking diagnostic details");

        foreach (var issue in outcome.Issue)
        {
            if (issue.Severity is OperationOutcome.IssueSeverity.Error
                              or OperationOutcome.IssueSeverity.Fatal)
            {
                Assert.NotNull(issue.Diagnostics);
                Assert.NotEmpty(issue.Diagnostics);
            }
        }
    }
}
