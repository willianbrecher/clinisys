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
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.Specialty).NotEmpty().When(x => x.Role == Role.Doctor)
            .WithMessage("Specialty is required when role is Doctor.");
    }
}
