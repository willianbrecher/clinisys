using CliniSys.Domain.Enums;

namespace CliniSys.Domain.Entities;

/// <summary>An appointment scheduled between a patient and a doctor.</summary>
public class Appointment
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; set; }
    /// <summary>FK to the patient.</summary>
    public Guid PatientId { get; set; }
    /// <summary>Navigation property to the patient.</summary>
    public Patient Patient { get; set; } = null!;
    /// <summary>FK to the doctor.</summary>
    public Guid DoctorId { get; set; }
    /// <summary>Navigation property to the doctor.</summary>
    public Doctor Doctor { get; set; } = null!;
    /// <summary>UTC date and time the appointment starts.</summary>
    public DateTime StartsAt { get; set; }
    /// <summary>Duration in minutes.</summary>
    public int DurationMinutes { get; set; }
    /// <summary>Current lifecycle status.</summary>
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;
    /// <summary>Optional appointment notes.</summary>
    public string? Notes { get; set; }
    /// <summary>UTC timestamp when created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
