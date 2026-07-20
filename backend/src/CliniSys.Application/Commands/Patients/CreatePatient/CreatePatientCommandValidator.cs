using FluentValidation;

namespace CliniSys.Application.Commands.Patients.CreatePatient;

/// <summary>Validates <see cref="CreatePatientCommand"/>.</summary>
public class CreatePatientCommandValidator : AbstractValidator<CreatePatientCommand>
{
    /// <summary>Defines validation rules.</summary>
    public CreatePatientCommandValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.DateOfBirth).NotEmpty().LessThan(DateOnly.FromDateTime(DateTime.UtcNow));
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Email).EmailAddress().When(x => x.Email is not null);
    }
}
