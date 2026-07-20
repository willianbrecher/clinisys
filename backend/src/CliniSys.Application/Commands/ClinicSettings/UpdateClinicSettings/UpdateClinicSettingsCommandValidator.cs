using FluentValidation;

namespace CliniSys.Application.Commands.ClinicSettings.UpdateClinicSettings;

/// <summary>Validates <see cref="UpdateClinicSettingsCommand"/>.</summary>
public class UpdateClinicSettingsCommandValidator : AbstractValidator<UpdateClinicSettingsCommand>
{
    /// <summary>Defines validation rules.</summary>
    public UpdateClinicSettingsCommandValidator()
    {
        RuleFor(x => x.OpenTime).NotEmpty().Matches(@"^\d{2}:\d{2}$").WithMessage("OpenTime must be HH:mm.");
        RuleFor(x => x.CloseTime).NotEmpty().Matches(@"^\d{2}:\d{2}$").WithMessage("CloseTime must be HH:mm.");
        RuleFor(x => x.OpenDays).NotEmpty().Matches(@"^[0-6](,[0-6])*$").WithMessage("OpenDays must be comma-separated 0–6.");
        RuleFor(x => x.LogoBase64).Must(IsValidImage).When(x => x.LogoBase64 is not null)
            .WithMessage("LogoBase64 must be a valid base64 image data URI (max 512 KB).");
    }

    private static bool IsValidImage(string? v)
    {
        if (v is null || !v.StartsWith("data:image/")) return false;
        var i = v.IndexOf(',');
        return i >= 0 && (v[(i + 1)..].Length * 3 / 4) <= 512 * 1024;
    }
}
