using CliniSys.Application.Common.Interfaces;
using MediatR;

namespace CliniSys.Application.Commands.Users.DeactivateUser;

/// <summary>Handler for <see cref="DeactivateUserCommand"/>.</summary>
public class DeactivateUserCommandHandler : ICommandHandler<DeactivateUserCommand, Unit>
{
    private readonly IIdentityService _identity;
    /// <summary>Initialises the handler.</summary>
    /// <param name="identity">Identity service.</param>
    public DeactivateUserCommandHandler(IIdentityService identity) => _identity = identity;

    /// <summary>Locks out the user indefinitely.</summary>
    /// <param name="request">Deactivation command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see cref="Unit.Value"/>.</returns>
    public async Task<Unit> Handle(DeactivateUserCommand request, CancellationToken cancellationToken)
    {
        await _identity.DeactivateUserAsync(request.Id, cancellationToken);
        return Unit.Value;
    }
}
