using CliniSys.Application.Commands.Users.ResetPassword;
using FluentValidation.TestHelper;

namespace CliniSys.Application.Tests.Users;

[TestFixture]
public class ResetPasswordCommandValidatorTests
{
    private readonly ResetPasswordCommandValidator _validator = new();

    [Test]
    public void Password_of_8_or_more_passes()
    {
        _validator.TestValidate(new ResetPasswordCommand(Guid.NewGuid(), "longenough"))
            .ShouldNotHaveAnyValidationErrors();
    }

    [TestCase("")]
    [TestCase("short")]
    public void Empty_or_short_password_fails(string password)
    {
        _validator.TestValidate(new ResetPasswordCommand(Guid.NewGuid(), password))
            .ShouldHaveValidationErrorFor(x => x.NewPassword);
    }
}
