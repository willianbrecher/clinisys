using CliniSys.Application.Common.Interfaces;
using MediatR;

namespace CliniSys.Application.Commands.ClinicSettings.UpdateClinicSettings;

/// <summary>Command to update clinic-wide settings.</summary>
/// <param name="OpenTime">Opening time in HH:mm.</param>
/// <param name="CloseTime">Closing time in HH:mm.</param>
/// <param name="OpenDays">Comma-separated weekday numbers (0=Sun…6=Sat).</param>
/// <param name="LogoBase64">Base64 data URI or <see langword="null"/> to clear the logo.</param>
public record UpdateClinicSettingsCommand(
    string OpenTime, string CloseTime, string OpenDays, string? LogoBase64) : ICommand<Unit>;
