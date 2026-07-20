using CliniSys.Application.Common.Interfaces;
using CliniSys.Application.Common.Models;

namespace CliniSys.Application.Queries.Users.GetUsers;

/// <summary>Query to retrieve a paginated list of all users.</summary>
/// <param name="Page">1-based page number.</param>
/// <param name="PageSize">Items per page (max 100).</param>
public record GetUsersQuery(int Page = 1, int PageSize = 20) : IPagedQuery<UserModel>;
