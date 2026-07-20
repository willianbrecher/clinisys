using CliniSys.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace CliniSys.Domain.Entities;

/// <summary>A system user who can log in. Backed by ASP.NET Core Identity.</summary>
public class ApplicationUser : IdentityUser<Guid>
{
    /// <summary>Full display name.</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Role controlling access within the system.</summary>
    public Role Role { get; set; }

    /// <summary>Optional profile picture as a base64 data URI (e.g. <c>data:image/png;base64,...</c>).</summary>
    public string? ProfilePictureBase64 { get; set; }

    /// <summary>Preferred colour theme. Defaults to <see cref="ThemePreference.System"/>.</summary>
    public ThemePreference ThemePreference { get; set; } = ThemePreference.System;

    /// <summary>BCP-47 language tag. Supported: <c>en-US</c>, <c>pt-BR</c>, <c>es-ES</c>. Defaults to <c>en-US</c>.</summary>
    public string LanguagePreference { get; set; } = "en-US";
}
