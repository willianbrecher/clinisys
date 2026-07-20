namespace CliniSys.Api.Requests.ClinicSettings;

/// <summary>HTTP body for PUT /api/clinic-settings.</summary>
/// <param name="OpenTime">Opening time HH:mm.</param>
/// <param name="CloseTime">Closing time HH:mm.</param>
/// <param name="OpenDays">Comma-separated weekday numbers.</param>
/// <param name="LogoBase64">Base64 data URI or <see langword="null"/> to remove logo.</param>
public record UpdateClinicSettingsRequest(string OpenTime, string CloseTime, string OpenDays, string? LogoBase64);
