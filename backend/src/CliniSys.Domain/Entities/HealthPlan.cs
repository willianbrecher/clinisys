namespace CliniSys.Domain.Entities;

/// <summary>A registered health/insurance plan patients can be linked to.</summary>
public class HealthPlan
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; set; }
    /// <summary>Plan name (selected by patients — kept consistent across the app).</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Free-text details/notes about the plan.</summary>
    public string? Notes { get; set; }
    /// <summary>False when soft-deleted.</summary>
    public bool IsActive { get; set; } = true;
}
