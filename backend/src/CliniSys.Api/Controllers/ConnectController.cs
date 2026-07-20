using System.Security.Claims;
using CliniSys.Application.Common.Interfaces.Repositories;
using CliniSys.Domain.Entities;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace CliniSys.Api.Controllers;

/// <summary>
/// Handles the OpenIddict ROPC token endpoint passthrough.
/// Issues JWT access tokens for valid username/password credentials.
/// </summary>
[ApiController]
public class ConnectController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IDoctorRepository _doctorRepository;

    /// <summary>Initialises the controller.</summary>
    /// <param name="userManager">ASP.NET Core Identity user manager.</param>
    /// <param name="doctorRepository">Repository to resolve the caller's linked doctor profile.</param>
    public ConnectController(UserManager<ApplicationUser> userManager, IDoctorRepository doctorRepository)
    {
        _userManager      = userManager;
        _doctorRepository = doctorRepository;
    }

    /// <summary>
    /// Issues an OAuth 2.0 access token for a valid username/password pair.
    /// Custom claims in the token: <c>role</c>, <c>theme</c>, <c>language</c>, <c>fullName</c>,
    /// and <c>doctorId</c> (Doctor role only).
    /// </summary>
    /// <returns>Standard OAuth 2.0 token response.</returns>
    [HttpPost("~/connect/token"), Produces("application/json")]
    public async Task<IActionResult> Exchange()
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("OpenIddict server request cannot be retrieved.");

        if (!request.IsPasswordGrantType())
            throw new InvalidOperationException("Grant type not supported.");

        var user = await _userManager.FindByNameAsync(request.Username!);
        if (user is null || !await _userManager.CheckPasswordAsync(user, request.Password!))
        {
            var props = new AuthenticationProperties(new Dictionary<string, string?>
            {
                [OpenIddictServerAspNetCoreConstants.Properties.Error]            = Errors.InvalidGrant,
                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "Invalid credentials."
            });
            return Forbid(props, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        var identity = new ClaimsIdentity(
            authenticationType: TokenValidationParameters.DefaultAuthenticationType,
            nameType: Claims.Name, roleType: Claims.Role);

        identity
            .SetClaim(Claims.Subject, user.Id.ToString())
            .SetClaim(Claims.Name,    user.Email!)
            .SetClaim("role",         user.Role.ToString())
            .SetClaim("theme",        user.ThemePreference.ToString())
            .SetClaim("language",     user.LanguagePreference)
            .SetClaim("fullName",     user.FullName);

        if (user.Role == CliniSys.Domain.Enums.Role.Doctor)
        {
            var doctor = await _doctorRepository.GetByUserIdAsync(user.Id);
            if (doctor is not null)
                identity.SetClaim("doctorId", doctor.Id.ToString());
        }

        identity.SetDestinations(c => c.Type switch
        {
            Claims.Name or Claims.Email => [Destinations.AccessToken, Destinations.IdentityToken],
            _ => [Destinations.AccessToken]
        });

        return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }
}
