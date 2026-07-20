using CliniSys.Application.Common.Interfaces;
using MediatR;

namespace CliniSys.Application.Commands.Account.UpdateProfilePicture;

/// <summary>Command to set or clear a user's profile picture.</summary>
/// <param name="UserId">The calling user's identifier.</param>
/// <param name="ProfilePictureBase64">Base64 data URI, or <see langword="null"/> to remove.</param>
public record UpdateProfilePictureCommand(Guid UserId, string? ProfilePictureBase64) : ICommand<Unit>;
