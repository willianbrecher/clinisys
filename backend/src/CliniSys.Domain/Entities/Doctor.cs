namespace CliniSys.Domain.Entities;

/// <summary>Doctor profile linked 1:1 to an <see cref="ApplicationUser"/> with role Doctor.</summary>
public class Doctor
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; set; }
    /// <summary>FK to the associated user.</summary>
    public Guid UserId { get; set; }
    /// <summary>Navigation property to the user.</summary>
    public ApplicationUser User { get; set; } = null!;
    /// <summary>Free-form medical specialty (e.g. "Cardiology").</summary>
    public string Specialty { get; set; } = string.Empty;
    /// <summary>False when soft-deleted.</summary>
    public bool IsActive { get; set; } = true;
}
