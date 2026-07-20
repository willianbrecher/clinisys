using CliniSys.Application.Common.Interfaces;
using CliniSys.Application.Common.Interfaces.Repositories;
using MediatR;

namespace CliniSys.Application.Commands.ClinicSettings.UpdateClinicSettings;

/// <summary>Handler for <see cref="UpdateClinicSettingsCommand"/>.</summary>
public class UpdateClinicSettingsCommandHandler : ICommandHandler<UpdateClinicSettingsCommand, Unit>
{
    private readonly IClinicSettingsRepository _repo;
    /// <summary>Initialises the handler.</summary>
    /// <param name="repo">Clinic settings repository.</param>
    public UpdateClinicSettingsCommandHandler(IClinicSettingsRepository repo) => _repo = repo;

    /// <summary>Updates the singleton clinic settings row.</summary>
    /// <param name="request">Update data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see cref="Unit.Value"/>.</returns>
    public async Task<Unit> Handle(UpdateClinicSettingsCommand request, CancellationToken cancellationToken)
    {
        var s = await _repo.GetSingletonAsync(cancellationToken);
        s.OpenTime   = TimeOnly.Parse(request.OpenTime);
        s.CloseTime  = TimeOnly.Parse(request.CloseTime);
        s.OpenDays   = request.OpenDays;
        s.LogoBase64 = request.LogoBase64;
        _repo.Update(s);
        await _repo.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
