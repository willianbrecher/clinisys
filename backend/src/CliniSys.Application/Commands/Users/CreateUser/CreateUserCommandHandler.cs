using CliniSys.Application.Common.Interfaces;
using CliniSys.Application.Common.Interfaces.Repositories;
using CliniSys.Domain.Entities;
using CliniSys.Domain.Enums;

namespace CliniSys.Application.Commands.Users.CreateUser;

/// <summary>Handler for <see cref="CreateUserCommand"/>.</summary>
public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, Guid>
{
    private readonly IIdentityService _identity;
    private readonly IDoctorRepository _doctors;

    /// <summary>Initialises the handler.</summary>
    /// <param name="identity">Identity service for user creation.</param>
    /// <param name="doctors">Doctor repository for linked profile creation.</param>
    public CreateUserCommandHandler(IIdentityService identity, IDoctorRepository doctors)
    {
        _identity = identity; _doctors = doctors;
    }

    /// <summary>Creates the user account and, if role is Doctor, a linked Doctor profile.</summary>
    /// <param name="request">User creation data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The new user's <see cref="Guid"/>.</returns>
    public async Task<Guid> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var userId = await _identity.CreateUserAsync(
            request.Email, request.FullName, request.Password, request.Role, cancellationToken);

        if (request.Role == Role.Doctor)
        {
            var doctor = new Doctor { Id = Guid.NewGuid(), UserId = userId, Specialty = request.Specialty! };
            await _doctors.AddAsync(doctor, cancellationToken);
            await _doctors.SaveChangesAsync(cancellationToken);
        }

        return userId;
    }
}
