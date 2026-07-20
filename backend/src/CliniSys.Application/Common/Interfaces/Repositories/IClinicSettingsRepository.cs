using CliniSys.Domain.Entities;

namespace CliniSys.Application.Common.Interfaces.Repositories;

/// <summary>Repository for the singleton <see cref="ClinicSettings"/> row.</summary>
public interface IClinicSettingsRepository : IRepository<ClinicSettings>
{
    /// <summary>Returns the single clinic settings row, creating a default one if it does not exist.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The <see cref="ClinicSettings"/> instance.</returns>
    Task<ClinicSettings> GetSingletonAsync(CancellationToken ct = default);
}
