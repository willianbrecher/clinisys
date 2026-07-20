namespace CliniSys.Domain.Enums;

/// <summary>User roles in the clinic system.</summary>
public enum Role
{
    /// <summary>System administrator with full access.</summary>
    Admin,
    /// <summary>Front-desk staff for scheduling and patient management.</summary>
    Staff,
    /// <summary>A medical doctor linked to a Doctor profile.</summary>
    Doctor
}
