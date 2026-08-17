using CliniSys.Application.Common.Interfaces;
using CliniSys.Application.Common.Interfaces.Repositories;

namespace CliniSys.Application.Queries.Account.GetCurrentUserPreferences;

/// <summary>Handler for <see cref="GetCurrentUserPreferencesQuery"/>.</summary>
public class GetCurrentUserPreferencesQueryHandler
    : IQueryHandler<GetCurrentUserPreferencesQuery, CurrentUserPreferencesModel?>
{
    private readonly IUserRepository _users;
    /// <summary>Initialises the handler.</summary>
    /// <param name="users">User repository.</param>
    public GetCurrentUserPreferencesQueryHandler(IUserRepository users) => _users = users;

    /// <summary>Returns the user's current preferences, or <see langword="null"/> if the user no longer exists.</summary>
    /// <param name="request">Query with the user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The preferences, or <see langword="null"/>.</returns>
    public async Task<CurrentUserPreferencesModel?> Handle(
        GetCurrentUserPreferencesQuery request, CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(request.UserId, cancellationToken);
        return user is null ? null : new CurrentUserPreferencesModel(user.ThemePreference, user.LanguagePreference);
    }
}
