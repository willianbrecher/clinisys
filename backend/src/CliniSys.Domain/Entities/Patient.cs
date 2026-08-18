namespace CliniSys.Domain.Entities;

/// <summary>A clinic patient. Patients do not have login accounts.</summary>
public class Patient
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; set; }
    /// <summary>Full name.</summary>
    public string FullName { get; set; } = string.Empty;
    /// <summary>Date of birth.</summary>
    public DateOnly DateOfBirth { get; set; }
    /// <summary>Contact phone number.</summary>
    public string Phone { get; set; } = string.Empty;
    /// <summary>Optional contact email.</summary>
    public string? Email { get; set; }
    /// <summary>Optional notes (insurance, medical, etc.).</summary>
    public string? Notes { get; set; }
    /// <summary>Optional linked health plan.</summary>
    public Guid? HealthPlanId { get; set; }
    /// <summary>Navigation to the linked health plan.</summary>
    public HealthPlan? HealthPlan { get; set; }
    /// <summary>Optional patient's own membership/card number under the linked plan.</summary>
    public string? HealthPlanNumber { get; set; }
    /// <summary>False when soft-deleted.</summary>
    public bool IsActive { get; set; } = true;
}
