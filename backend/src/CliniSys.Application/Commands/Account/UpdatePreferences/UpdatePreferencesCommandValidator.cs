using FluentValidation;

namespace CliniSys.Application.Commands.Account.UpdatePreferences;

/// <summary>Validates <see cref="UpdatePreferencesCommand"/>.</summary>
public class UpdatePreferencesCommandValidator : AbstractValidator<UpdatePreferencesCommand>
{
    private static readonly string[] SupportedLanguages = ["en-US", "pt-BR", "es-ES"];
    /// <summary>Defines validation rules.</summary>
    public UpdatePreferencesCommandValidator() =>
        RuleFor(x => x.Language).Must(l => SupportedLanguages.Contains(l))
            .WithMessage("Language must be one of: en-US, pt-BR, es-ES.");
}
