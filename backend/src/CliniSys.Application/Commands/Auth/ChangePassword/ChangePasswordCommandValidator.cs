using FluentValidation;

namespace CliniSys.Application.Commands.Auth.ChangePassword;

/// <summary>Validates <see cref="ChangePasswordCommand"/>.</summary>
public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    /// <summary>Defines validation rules.</summary>
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8)
            .NotEqual(x => x.CurrentPassword).WithMessage("New password must differ from current password.");
    }
}
