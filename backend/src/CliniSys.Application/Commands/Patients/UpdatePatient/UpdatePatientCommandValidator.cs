using FluentValidation;

namespace CliniSys.Application.Commands.Patients.UpdatePatient;

/// <summary>Validates <see cref="UpdatePatientCommand"/>.</summary>
public class UpdatePatientCommandValidator : AbstractValidator<UpdatePatientCommand>
{
    /// <summary>Defines validation rules.</summary>
    public UpdatePatientCommandValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.DateOfBirth).NotEmpty().LessThan(DateOnly.FromDateTime(DateTime.UtcNow));
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Email).EmailAddress().When(x => x.Email is not null);
        RuleFor(x => x.HealthPlanNumber).MaximumLength(50);
    }
}
