using CliniSys.Application.Commands.Appointments.CreateAppointment;
using CliniSys.Application.Tests.TestSupport;
using FluentValidation.TestHelper;

namespace CliniSys.Application.Tests.Appointments;

[TestFixture]
public class CreateAppointmentCommandValidatorTests
{
    private readonly CreateAppointmentCommandValidator _validator = new();

    [Test]
    public void Valid_command_passes()
    {
        var result = _validator.TestValidate(Builders.CreateAppointmentCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Empty_PatientId_fails()
    {
        var result = _validator.TestValidate(Builders.CreateAppointmentCommand(patientId: Guid.Empty));
        result.ShouldHaveValidationErrorFor(x => x.PatientId);
    }

    [Test]
    public void Empty_DoctorId_fails()
    {
        var result = _validator.TestValidate(Builders.CreateAppointmentCommand(doctorId: Guid.Empty));
        result.ShouldHaveValidationErrorFor(x => x.DoctorId);
    }

    [Test]
    public void StartsAt_in_the_past_fails()
    {
        var result = _validator.TestValidate(
            Builders.CreateAppointmentCommand(startsAt: DateTime.UtcNow.AddMinutes(-1)));
        result.ShouldHaveValidationErrorFor(x => x.StartsAt);
    }

    [TestCase(4)]
    [TestCase(481)]
    public void DurationMinutes_outside_5_to_480_fails(int minutes)
    {
        var result = _validator.TestValidate(Builders.CreateAppointmentCommand(durationMinutes: minutes));
        result.ShouldHaveValidationErrorFor(x => x.DurationMinutes);
    }

    [TestCase(5)]
    [TestCase(60)]
    [TestCase(480)]
    public void DurationMinutes_within_5_to_480_passes(int minutes)
    {
        var result = _validator.TestValidate(Builders.CreateAppointmentCommand(durationMinutes: minutes));
        result.ShouldNotHaveValidationErrorFor(x => x.DurationMinutes);
    }
}
