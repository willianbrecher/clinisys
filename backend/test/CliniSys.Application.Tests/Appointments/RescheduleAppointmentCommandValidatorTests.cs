using CliniSys.Application.Commands.Appointments.RescheduleAppointment;
using CliniSys.Application.Tests.TestSupport;
using FluentValidation.TestHelper;

namespace CliniSys.Application.Tests.Appointments;

[TestFixture]
public class RescheduleAppointmentCommandValidatorTests
{
    private readonly RescheduleAppointmentCommandValidator _validator = new();

    [Test]
    public void Valid_command_passes()
    {
        var result = _validator.TestValidate(Builders.RescheduleAppointmentCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void StartsAt_in_the_past_fails()
    {
        var result = _validator.TestValidate(
            Builders.RescheduleAppointmentCommand(startsAt: DateTime.UtcNow.AddMinutes(-1)));
        result.ShouldHaveValidationErrorFor(x => x.StartsAt);
    }

    [TestCase(4)]
    [TestCase(481)]
    public void DurationMinutes_outside_5_to_480_fails(int minutes)
    {
        var result = _validator.TestValidate(
            Builders.RescheduleAppointmentCommand(durationMinutes: minutes));
        result.ShouldHaveValidationErrorFor(x => x.DurationMinutes);
    }
}
