using FluentValidation;

namespace CliniSys.Application.Commands.Account.UpdateProfilePicture;

/// <summary>Validates <see cref="UpdateProfilePictureCommand"/>.</summary>
public class UpdateProfilePictureCommandValidator : AbstractValidator<UpdateProfilePictureCommand>
{
    /// <summary>Defines validation rules.</summary>
    public UpdateProfilePictureCommandValidator() =>
        RuleFor(x => x.ProfilePictureBase64)
            .Must(v => v is null || (v.StartsWith("data:image/") && v.IndexOf(',') >= 0
                && (v[(v.IndexOf(',') + 1)..].Length * 3 / 4) <= 512 * 1024))
            .WithMessage("Profile picture must be a valid base64 image data URI (max 512 KB).");
}
