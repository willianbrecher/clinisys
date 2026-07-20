using CliniSys.Application.Common.Interfaces;
using MediatR;

namespace CliniSys.Application.Commands.Users.ResetPassword;

/// <summary>Handler for <see cref="ResetPasswordCommand"/>.</summary>
public class ResetPasswordCommandHandler : ICommandHandler<ResetPasswordCommand, Unit>
{
    private readonly IIdentityService _identity;
    /// <summary>Initialises the handler.</summary>
    /// <param name="identity">Identity service.</param>
    public ResetPasswordCommandHandler(IIdentityService identity) => _identity = identity;

    /// <summary>Resets the target user's password.</summary>
    /// <param name="request">Reset command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see cref="Unit.Value"/>.</returns>
    public async Task<Unit> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        await _identity.ResetPasswordAsync(request.UserId, request.NewPassword, cancellationToken);
        return Unit.Value;
    }
}
