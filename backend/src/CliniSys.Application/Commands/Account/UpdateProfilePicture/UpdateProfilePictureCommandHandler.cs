using CliniSys.Application.Common.Exceptions;
using CliniSys.Application.Common.Interfaces;
using CliniSys.Application.Common.Interfaces.Repositories;
using MediatR;

namespace CliniSys.Application.Commands.Account.UpdateProfilePicture;

/// <summary>Handler for <see cref="UpdateProfilePictureCommand"/>.</summary>
public class UpdateProfilePictureCommandHandler : ICommandHandler<UpdateProfilePictureCommand, Unit>
{
    private readonly IUserRepository _users;
    /// <summary>Initialises the handler.</summary>
    /// <param name="users">User repository.</param>
    public UpdateProfilePictureCommandHandler(IUserRepository users) => _users = users;

    /// <summary>Updates or clears the user's profile picture.</summary>
    /// <param name="request">Picture data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see cref="Unit.Value"/>.</returns>
    public async Task<Unit> Handle(UpdateProfilePictureCommand request, CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException($"User {request.UserId} not found.");
        user.ProfilePictureBase64 = request.ProfilePictureBase64;
        _users.Update(user);
        await _users.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
