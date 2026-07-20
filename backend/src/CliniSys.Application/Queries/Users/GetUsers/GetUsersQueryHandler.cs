using CliniSys.Application.Common.Interfaces;
using CliniSys.Application.Common.Interfaces.Repositories;
using CliniSys.Application.Common.Models;
using CliniSys.Domain.Enums;
using FluentValidation;

namespace CliniSys.Application.Queries.Users.GetUsers;

/// <summary>User response model.</summary>
/// <param name="Id">User identifier.</param>
/// <param name="Email">Email address.</param>
/// <param name="FullName">Display name.</param>
/// <param name="Role">User role.</param>
/// <param name="ThemePreference">Preferred theme.</param>
/// <param name="LanguagePreference">Preferred language.</param>
public record UserModel(Guid Id, string? Email, string FullName, Role Role,
    ThemePreference ThemePreference, string LanguagePreference);

/// <summary>Handler for <see cref="GetUsersQuery"/>.</summary>
public class GetUsersQueryHandler : IQueryHandler<GetUsersQuery, PagedResult<UserModel>>
{
    private readonly IUserRepository _repo;
    /// <summary>Initialises the handler.</summary>
    /// <param name="repo">User repository.</param>
    public GetUsersQueryHandler(IUserRepository repo) => _repo = repo;

    /// <summary>Returns paginated users.</summary>
    /// <param name="request">Query with pagination.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated user list.</returns>
    public async Task<PagedResult<UserModel>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        if (request.PageSize > 100) throw new ValidationException("PageSize cannot exceed 100.");
        var paged = await _repo.GetPagedAsync(request.Page, request.PageSize, cancellationToken);
        var items = paged.Items.Select(u =>
            new UserModel(u.Id, u.Email, u.FullName, u.Role, u.ThemePreference, u.LanguagePreference)).ToList();
        return new PagedResult<UserModel>(items, paged.Page, paged.PageSize, paged.TotalCount, paged.TotalPages);
    }
}
