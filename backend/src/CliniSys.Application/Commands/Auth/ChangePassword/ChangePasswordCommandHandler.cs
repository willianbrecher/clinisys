using CliniSys.Application.Common.Interfaces;
using MediatR;

namespace CliniSys.Application.Commands.Auth.ChangePassword;

/// <summary>Handler for <see cref="ChangePasswordCommand"/>.</summary>
public class ChangePasswordCommandHandler : ICommandHandler<ChangePasswordCommand, Unit>
{
    private readonly IIdentityService _identity;
    /// <summary>Initialises the handler.</summary>
    /// <param name="identity">Identity service.</param>
    public ChangePasswordCommandHandler(IIdentityService identity) => _identity = identity;

    /// <summary>Changes the user's password after verifying the current one.</summary>
    /// <param name="request">Password change data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see cref="Unit.Value"/>.</returns>
    public async Task<Unit> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        await _identity.ChangePasswordAsync(request.UserId, request.CurrentPassword, request.NewPassword, cancellationToken);
        return Unit.Value;
    }
}
