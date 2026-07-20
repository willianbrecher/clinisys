namespace CliniSys.Domain.Enums;

/// <summary>Lifecycle states for an appointment.</summary>
public enum AppointmentStatus
{
    /// <summary>Booked but not yet confirmed.</summary>
    Scheduled,
    /// <summary>Confirmed by doctor or staff.</summary>
    Confirmed,
    /// <summary>Appointment has taken place.</summary>
    Completed,
    /// <summary>Cancelled before the appointment.</summary>
    Cancelled,
    /// <summary>Patient did not show up.</summary>
    NoShow
}
