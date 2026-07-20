using FluentValidation;

namespace CliniSys.Application.Commands.Appointments.RescheduleAppointment;

/// <summary>Validates <see cref="RescheduleAppointmentCommand"/>.</summary>
public class RescheduleAppointmentCommandValidator : AbstractValidator<RescheduleAppointmentCommand>
{
    /// <summary>Defines validation rules.</summary>
    public RescheduleAppointmentCommandValidator()
    {
        RuleFor(x => x.StartsAt).GreaterThan(DateTime.UtcNow).WithMessage("StartsAt must be in the future.");
        RuleFor(x => x.DurationMinutes).InclusiveBetween(5, 480);
    }
}
