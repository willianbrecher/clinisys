using CliniSys.Application.Commands.Patients.CreatePatient;
using CliniSys.Application.Tests.TestSupport;
using FluentValidation.TestHelper;

namespace CliniSys.Application.Tests.Patients;

[TestFixture]
public class CreatePatientCommandValidatorTests
{
    private readonly CreatePatientCommandValidator _validator = new();

    [Test]
    public void Valid_command_passes()
    {
        var result = _validator.TestValidate(Builders.CreatePatientCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Empty_FullName_fails()
    {
        var result = _validator.TestValidate(Builders.CreatePatientCommand(fullName: ""));
        result.ShouldHaveValidationErrorFor(x => x.FullName);
    }

    [Test]
    public void FullName_over_200_chars_fails()
    {
        var result = _validator.TestValidate(Builders.CreatePatientCommand(fullName: new string('a', 201)));
        result.ShouldHaveValidationErrorFor(x => x.FullName);
    }

    [Test]
    public void DateOfBirth_in_the_future_fails()
    {
        var result = _validator.TestValidate(
            Builders.CreatePatientCommand(dateOfBirth: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1))));
        result.ShouldHaveValidationErrorFor(x => x.DateOfBirth);
    }

    [Test]
    public void Empty_Phone_fails()
    {
        var result = _validator.TestValidate(Builders.CreatePatientCommand(phone: ""));
        result.ShouldHaveValidationErrorFor(x => x.Phone);
    }

    [Test]
    public void Malformed_Email_fails()
    {
        var result = _validator.TestValidate(Builders.CreatePatientCommand(email: "not-an-email"));
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Test]
    public void Null_Email_passes()
    {
        var result = _validator.TestValidate(Builders.CreatePatientCommand(email: null));
        result.ShouldNotHaveValidationErrorFor(x => x.Email);
    }

    [Test]
    public void HealthPlanNumber_over_50_chars_fails()
    {
        var result = _validator.TestValidate(
            Builders.CreatePatientCommand(healthPlanNumber: new string('9', 51)));
        result.ShouldHaveValidationErrorFor(x => x.HealthPlanNumber);
    }
}
