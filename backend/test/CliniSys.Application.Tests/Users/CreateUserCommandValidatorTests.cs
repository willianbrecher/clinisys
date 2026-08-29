using CliniSys.Application.Commands.Users.CreateUser;
using CliniSys.Domain.Enums;
using FluentValidation.TestHelper;

namespace CliniSys.Application.Tests.Users;

[TestFixture]
public class CreateUserCommandValidatorTests
{
    private readonly CreateUserCommandValidator _validator = new();

    private static CreateUserCommand Command(
        string email = "user@example.com", string fullName = "New User",
        string password = "Passw0rd", Role role = Role.Staff, string? specialty = null) =>
        new(email, fullName, password, role, specialty);

    [Test]
    public void Valid_command_passes()
    {
        _validator.TestValidate(Command()).ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Malformed_Email_fails()
    {
        _validator.TestValidate(Command(email: "nope")).ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Test]
    public void Empty_FullName_fails()
    {
        _validator.TestValidate(Command(fullName: "")).ShouldHaveValidationErrorFor(x => x.FullName);
    }

    [TestCase("short1A", Description = "under 8 chars")]
    [TestCase("passw0rd", Description = "no uppercase")]
    [TestCase("PASSW0RD", Description = "no lowercase")]
    [TestCase("Password", Description = "no digit")]
    public void Weak_Password_fails(string password)
    {
        _validator.TestValidate(Command(password: password)).ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Test]
    public void Doctor_without_Specialty_fails()
    {
        _validator.TestValidate(Command(role: Role.Doctor, specialty: null))
            .ShouldHaveValidationErrorFor(x => x.Specialty);
    }

    [Test]
    public void Doctor_with_Specialty_passes()
    {
        _validator.TestValidate(Command(role: Role.Doctor, specialty: "Cardiology"))
            .ShouldNotHaveValidationErrorFor(x => x.Specialty);
    }

    [Test]
    public void Non_doctor_without_Specialty_passes()
    {
        _validator.TestValidate(Command(role: Role.Staff, specialty: null))
            .ShouldNotHaveValidationErrorFor(x => x.Specialty);
    }
}
