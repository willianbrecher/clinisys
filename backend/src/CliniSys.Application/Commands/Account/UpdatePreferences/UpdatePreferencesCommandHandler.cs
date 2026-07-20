using CliniSys.Application.Common.Exceptions;
using CliniSys.Application.Common.Interfaces;
using CliniSys.Application.Common.Interfaces.Repositories;
using MediatR;

namespace CliniSys.Application.Commands.Account.UpdatePreferences;

/// <summary>Handler for <see cref="UpdatePreferencesCommand"/>.</summary>
public class UpdatePreferencesCommandHandler : ICommandHandler<UpdatePreferencesCommand, Unit>
{
    private readonly IUserRepository _users;
    /// <summary>Initialises the handler.</summary>
    /// <param name="users">User repository.</param>
    public UpdatePreferencesCommandHandler(IUserRepository users) => _users = users;

    /// <summary>Updates the user's theme and language preferences.</summary>
    /// <param name="request">New preferences.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see cref="Unit.Value"/>.</returns>
    public async Task<Unit> Handle(UpdatePreferencesCommand request, CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException($"User {request.UserId} not found.");
        user.ThemePreference    = request.Theme;
        user.LanguagePreference = request.Language;
        _users.Update(user);
        await _users.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
