using CliniSys.Application.Commands.Account.UpdateProfilePicture;
using FluentValidation.TestHelper;

namespace CliniSys.Application.Tests.Account;

[TestFixture]
public class UpdateProfilePictureCommandValidatorTests
{
    private readonly UpdateProfilePictureCommandValidator _validator = new();

    private static UpdateProfilePictureCommand Command(string? picture) =>
        new(Guid.NewGuid(), picture);

    [Test]
    public void Null_picture_passes()
    {
        _validator.TestValidate(Command(null)).ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Small_image_data_uri_passes()
    {
        _validator.TestValidate(Command("data:image/png;base64,aGVsbG8=")).ShouldNotHaveAnyValidationErrors();
    }

    [TestCase("not-a-data-uri")]
    [TestCase("data:text/plain,hello")]
    public void Non_image_value_fails(string picture)
    {
        _validator.TestValidate(Command(picture)).ShouldHaveValidationErrorFor(x => x.ProfilePictureBase64);
    }

    [Test]
    public void Image_over_512kb_fails()
    {
        var oversized = "data:image/png;base64," + new string('A', 700 * 1024);
        _validator.TestValidate(Command(oversized)).ShouldHaveValidationErrorFor(x => x.ProfilePictureBase64);
    }
}
