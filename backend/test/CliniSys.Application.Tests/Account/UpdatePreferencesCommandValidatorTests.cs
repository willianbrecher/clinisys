using CliniSys.Application.Commands.Account.UpdatePreferences;
using CliniSys.Domain.Enums;
using FluentValidation.TestHelper;

namespace CliniSys.Application.Tests.Account;

[TestFixture]
public class UpdatePreferencesCommandValidatorTests
{
    private readonly UpdatePreferencesCommandValidator _validator = new();

    private static UpdatePreferencesCommand Command(string language) =>
        new(Guid.NewGuid(), ThemePreference.System, language);

    [TestCase("en-US")]
    [TestCase("pt-BR")]
    [TestCase("es-ES")]
    public void Supported_language_passes(string language)
    {
        _validator.TestValidate(Command(language)).ShouldNotHaveAnyValidationErrors();
    }

    [TestCase("fr-FR")]
    [TestCase("en")]
    [TestCase("")]
    public void Unsupported_language_fails(string language)
    {
        _validator.TestValidate(Command(language)).ShouldHaveValidationErrorFor(x => x.Language);
    }
}
