using FluentValidation;

namespace CliniSys.Application.Commands.Appointments.CreateAppointment;

/// <summary>Validates <see cref="CreateAppointmentCommand"/>.</summary>
public class CreateAppointmentCommandValidator : AbstractValidator<CreateAppointmentCommand>
{
    /// <summary>Defines validation rules.</summary>
    public CreateAppointmentCommandValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.DoctorId).NotEmpty();
        RuleFor(x => x.StartsAt).GreaterThan(DateTime.UtcNow).WithMessage("StartsAt must be in the future.");
        RuleFor(x => x.DurationMinutes).InclusiveBetween(5, 480);
    }
}
