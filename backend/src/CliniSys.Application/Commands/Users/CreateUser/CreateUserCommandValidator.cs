using CliniSys.Domain.Enums;
using FluentValidation;

namespace CliniSys.Application.Commands.Users.CreateUser;

/// <summary>Validates <see cref="CreateUserCommand"/>.</summary>
public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    /// <summary>Defines validation rules.</summary>
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8)
            .Matches("[a-z]").WithMessage("Password must contain a lowercase letter.")
            .Matches("[A-Z]").WithMessage("Password must contain an uppercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain a digit.");
        RuleFor(x => x.Specialty).NotEmpty().When(x => x.Role == Role.Doctor)
            .WithMessage("Specialty is required when role is Doctor.");
    }
}
