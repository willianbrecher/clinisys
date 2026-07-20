using CliniSys.Application.Common.Interfaces.Repositories;
using CliniSys.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CliniSys.Infrastructure.Persistence.Repositories;

internal class ClinicSettingsRepository : Repository<ClinicSettings>, IClinicSettingsRepository
{
    private static readonly Guid SingletonId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public ClinicSettingsRepository(AppDbContext context) : base(context) { }

    public async Task<ClinicSettings> GetSingletonAsync(CancellationToken ct = default)
    {
        var s = await _set.FirstOrDefaultAsync(ct);
        if (s is not null) return s;

        s = new ClinicSettings
        {
            Id = SingletonId,
            OpenTime  = new TimeOnly(8, 0),
            CloseTime = new TimeOnly(18, 0),
            OpenDays  = "1,2,3,4,5"
        };
        await _set.AddAsync(s, ct);
        await _context.SaveChangesAsync(ct);
        return s;
    }
}
