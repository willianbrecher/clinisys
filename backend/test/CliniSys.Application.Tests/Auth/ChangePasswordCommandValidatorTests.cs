using CliniSys.Application.Commands.Auth.ChangePassword;
using FluentValidation.TestHelper;

namespace CliniSys.Application.Tests.Auth;

[TestFixture]
public class ChangePasswordCommandValidatorTests
{
    private readonly ChangePasswordCommandValidator _validator = new();

    private static ChangePasswordCommand Command(string current = "OldPass1", string @new = "NewPass1") =>
        new(Guid.NewGuid(), current, @new);

    [Test]
    public void Valid_command_passes()
    {
        _validator.TestValidate(Command()).ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Empty_CurrentPassword_fails()
    {
        _validator.TestValidate(Command(current: ""))
            .ShouldHaveValidationErrorFor(x => x.CurrentPassword);
    }

    [Test]
    public void NewPassword_shorter_than_8_fails()
    {
        _validator.TestValidate(Command(@new: "Short1"))
            .ShouldHaveValidationErrorFor(x => x.NewPassword);
    }

    [Test]
    public void NewPassword_equal_to_CurrentPassword_fails()
    {
        _validator.TestValidate(Command(current: "SamePass1", @new: "SamePass1"))
            .ShouldHaveValidationErrorFor(x => x.NewPassword);
    }
}
