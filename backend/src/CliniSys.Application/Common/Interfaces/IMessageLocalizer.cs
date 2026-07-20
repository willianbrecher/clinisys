namespace CliniSys.Application.Common.Interfaces;

/// <summary>
/// Provides localized user-facing messages for the current request culture.
/// Used by FluentValidation validators to return translated error messages.
/// </summary>
public interface IMessageLocalizer
{
    /// <summary>Returns the localized string for the given dot-separated key.</summary>
    /// <param name="key">Dot-separated translation key (e.g. <c>validation.required</c>).</param>
    /// <returns>Localized string, or the key itself if not found.</returns>
    string this[string key] { get; }
}
