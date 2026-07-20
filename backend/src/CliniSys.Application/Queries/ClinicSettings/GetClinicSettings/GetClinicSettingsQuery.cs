using CliniSys.Application.Common.Interfaces;

namespace CliniSys.Application.Queries.ClinicSettings.GetClinicSettings;

/// <summary>Query that retrieves the singleton clinic settings row.</summary>
public record GetClinicSettingsQuery : IQuery<ClinicSettingsModel>;
