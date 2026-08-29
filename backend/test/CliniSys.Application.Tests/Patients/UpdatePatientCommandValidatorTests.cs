using CliniSys.Application.Commands.Patients.UpdatePatient;
using CliniSys.Application.Tests.TestSupport;
using FluentValidation.TestHelper;

namespace CliniSys.Application.Tests.Patients;

[TestFixture]
public class UpdatePatientCommandValidatorTests
{
    private readonly UpdatePatientCommandValidator _validator = new();

    [Test]
    public void Valid_command_passes()
    {
        var result = _validator.TestValidate(Builders.UpdatePatientCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Empty_FullName_fails()
    {
        var result = _validator.TestValidate(Builders.UpdatePatientCommand(fullName: ""));
        result.ShouldHaveValidationErrorFor(x => x.FullName);
    }

    [Test]
    public void DateOfBirth_in_the_future_fails()
    {
        var result = _validator.TestValidate(
            Builders.UpdatePatientCommand(dateOfBirth: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1))));
        result.ShouldHaveValidationErrorFor(x => x.DateOfBirth);
    }

    [Test]
    public void Malformed_Email_fails()
    {
        var result = _validator.TestValidate(Builders.UpdatePatientCommand(email: "nope"));
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Test]
    public void HealthPlanNumber_over_50_chars_fails()
    {
        var result = _validator.TestValidate(
            Builders.UpdatePatientCommand(healthPlanNumber: new string('9', 51)));
        result.ShouldHaveValidationErrorFor(x => x.HealthPlanNumber);
    }
}
