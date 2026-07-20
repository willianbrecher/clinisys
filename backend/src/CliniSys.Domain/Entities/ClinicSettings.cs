namespace CliniSys.Domain.Entities;

/// <summary>Singleton row storing clinic-wide configuration.</summary>
public class ClinicSettings
{
    /// <summary>Primary key (only one row exists).</summary>
    public Guid Id { get; set; }
    /// <summary>Time the clinic opens each working day.</summary>
    public TimeOnly OpenTime { get; set; }
    /// <summary>Time the clinic closes each working day.</summary>
    public TimeOnly CloseTime { get; set; }
    /// <summary>Comma-separated weekday numbers, 0=Sun…6=Sat (e.g. <c>"1,2,3,4,5"</c>).</summary>
    public string OpenDays { get; set; } = "1,2,3,4,5";
    /// <summary>Optional clinic logo as a base64 data URI. <see langword="null"/> means no logo.</summary>
    public string? LogoBase64 { get; set; }
}
