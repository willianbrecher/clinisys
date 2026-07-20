using FluentValidation;

namespace CliniSys.Application.Commands.Users.ResetPassword;

/// <summary>Validates <see cref="ResetPasswordCommand"/>.</summary>
public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    /// <summary>Defines validation rules.</summary>
    public ResetPasswordCommandValidator() =>
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8);
}
