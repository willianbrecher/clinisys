using CliniSys.Application.Common.Interfaces;
using MediatR;

namespace CliniSys.Application.Commands.Users.ReactivateUser;

/// <summary>Handler for <see cref="ReactivateUserCommand"/>.</summary>
public class ReactivateUserCommandHandler : ICommandHandler<ReactivateUserCommand, Unit>
{
    private readonly IIdentityService _identity;
    /// <summary>Initialises the handler.</summary>
    /// <param name="identity">Identity service.</param>
    public ReactivateUserCommandHandler(IIdentityService identity) => _identity = identity;

    /// <summary>Clears the user's lockout.</summary>
    /// <param name="request">Reactivation command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see cref="Unit.Value"/>.</returns>
    public async Task<Unit> Handle(ReactivateUserCommand request, CancellationToken cancellationToken)
    {
        await _identity.ReactivateUserAsync(request.Id, cancellationToken);
        return Unit.Value;
    }
}
