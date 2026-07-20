using CliniSys.Application.Common.Interfaces;
using CliniSys.Application.Common.Interfaces.Repositories;

namespace CliniSys.Application.Queries.ClinicSettings.GetClinicSettings;

/// <summary>Clinic settings response model.</summary>
/// <param name="Id">Settings identifier.</param>
/// <param name="OpenTime">Opening time in HH:mm.</param>
/// <param name="CloseTime">Closing time in HH:mm.</param>
/// <param name="OpenDays">Comma-separated weekday numbers (0=Sun…6=Sat).</param>
/// <param name="LogoBase64">Base64 data URI or <see langword="null"/>.</param>
public record ClinicSettingsModel(Guid Id, string OpenTime, string CloseTime, string OpenDays, string? LogoBase64);

/// <summary>Handler for <see cref="GetClinicSettingsQuery"/>.</summary>
public class GetClinicSettingsQueryHandler : IQueryHandler<GetClinicSettingsQuery, ClinicSettingsModel>
{
    private readonly IClinicSettingsRepository _repo;
    /// <summary>Initialises the handler.</summary>
    /// <param name="repo">Clinic settings repository.</param>
    public GetClinicSettingsQueryHandler(IClinicSettingsRepository repo) => _repo = repo;

    /// <summary>Returns the current clinic settings.</summary>
    /// <param name="request">The query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Clinic settings model.</returns>
    public async Task<ClinicSettingsModel> Handle(GetClinicSettingsQuery request, CancellationToken cancellationToken)
    {
        var s = await _repo.GetSingletonAsync(cancellationToken);
        return new ClinicSettingsModel(s.Id, s.OpenTime.ToString("HH:mm"),
            s.CloseTime.ToString("HH:mm"), s.OpenDays, s.LogoBase64);
    }
}
