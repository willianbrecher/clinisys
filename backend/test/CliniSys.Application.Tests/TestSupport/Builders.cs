using CliniSys.Application.Commands.Appointments.CreateAppointment;
using CliniSys.Application.Commands.Appointments.RescheduleAppointment;
using CliniSys.Application.Commands.Patients.CreatePatient;
using CliniSys.Application.Commands.Patients.UpdatePatient;
using CliniSys.Domain.Entities;
using CliniSys.Domain.Enums;

namespace CliniSys.Application.Tests.TestSupport;

/// <summary>
/// Factory helpers producing valid-by-default commands and entities. Each test overrides only
/// the field it exercises, keeping the arrange block about that field alone.
/// </summary>
public static class Builders
{
    public static readonly DateTime FutureStart =
        new(2099, 6, 1, 10, 0, 0, DateTimeKind.Utc);

    public static CreateAppointmentCommand CreateAppointmentCommand(
        Guid? patientId = null, Guid? doctorId = null, DateTime? startsAt = null,
        int durationMinutes = 30, string? notes = null) =>
        new(patientId ?? Guid.NewGuid(), doctorId ?? Guid.NewGuid(),
            startsAt ?? FutureStart, durationMinutes, notes);

    public static RescheduleAppointmentCommand RescheduleAppointmentCommand(
        Guid? id = null, DateTime? startsAt = null, int durationMinutes = 30) =>
        new(id ?? Guid.NewGuid(), startsAt ?? FutureStart, durationMinutes);

    public static CreatePatientCommand CreatePatientCommand(
        string fullName = "Ana Lima", DateOnly? dateOfBirth = null, string phone = "+55 11 90000-0000",
        string? email = "ana@example.com", string? notes = null,
        Guid? healthPlanId = null, string? healthPlanNumber = null) =>
        new(fullName, dateOfBirth ?? new DateOnly(1990, 1, 1), phone, email, notes,
            healthPlanId, healthPlanNumber);

    public static UpdatePatientCommand UpdatePatientCommand(
        Guid? id = null, string fullName = "Ana Lima", DateOnly? dateOfBirth = null,
        string phone = "+55 11 90000-0000", string? email = "ana@example.com", string? notes = null,
        Guid? healthPlanId = null, string? healthPlanNumber = null) =>
        new(id ?? Guid.NewGuid(), fullName, dateOfBirth ?? new DateOnly(1990, 1, 1), phone, email,
            notes, healthPlanId, healthPlanNumber);

    // Fully qualified: the CliniSys.Application.Tests.ClinicSettings test namespace shadows the
    // entity type here, the same clash the production handlers work around.
    public static Domain.Entities.ClinicSettings ClinicSettings(
        string openDays = "1,2,3,4,5", TimeOnly? openTime = null, TimeOnly? closeTime = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            OpenDays = openDays,
            OpenTime = openTime ?? new TimeOnly(8, 0),
            CloseTime = closeTime ?? new TimeOnly(18, 0),
        };

    public static Appointment Appointment(
        Guid? id = null, Guid? doctorId = null, Guid? patientId = null, DateTime? startsAt = null,
        int durationMinutes = 30, AppointmentStatus status = AppointmentStatus.Scheduled) =>
        new()
        {
            Id = id ?? Guid.NewGuid(),
            DoctorId = doctorId ?? Guid.NewGuid(),
            PatientId = patientId ?? Guid.NewGuid(),
            StartsAt = startsAt ?? FutureStart,
            DurationMinutes = durationMinutes,
            Status = status,
        };

    public static HealthPlan HealthPlan(
        Guid? id = null, string name = "Plano Saúde", string? notes = null, bool isActive = true) =>
        new() { Id = id ?? Guid.NewGuid(), Name = name, Notes = notes, IsActive = isActive };
}
