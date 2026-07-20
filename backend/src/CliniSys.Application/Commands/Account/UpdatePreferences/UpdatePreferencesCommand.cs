using CliniSys.Application.Common.Interfaces;
using CliniSys.Domain.Enums;
using MediatR;

namespace CliniSys.Application.Commands.Account.UpdatePreferences;

/// <summary>Command to update a user's theme and language preferences.</summary>
/// <param name="UserId">The calling user's identifier.</param>
/// <param name="Theme">Preferred theme.</param>
/// <param name="Language">BCP-47 language tag (en-US, pt-BR, es-ES).</param>
public record UpdatePreferencesCommand(Guid UserId, ThemePreference Theme, string Language) : ICommand<Unit>;
