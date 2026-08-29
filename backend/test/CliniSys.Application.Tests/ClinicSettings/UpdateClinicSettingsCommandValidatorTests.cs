using CliniSys.Application.Commands.ClinicSettings.UpdateClinicSettings;
using FluentValidation.TestHelper;

namespace CliniSys.Application.Tests.ClinicSettings;

[TestFixture]
public class UpdateClinicSettingsCommandValidatorTests
{
    private readonly UpdateClinicSettingsCommandValidator _validator = new();

    private static UpdateClinicSettingsCommand Command(
        string openTime = "08:00", string closeTime = "18:00",
        string openDays = "1,2,3,4,5", string? logoBase64 = null) =>
        new(openTime, closeTime, openDays, logoBase64);

    [Test]
    public void Valid_command_passes()
    {
        _validator.TestValidate(Command()).ShouldNotHaveAnyValidationErrors();
    }

    [TestCase("8:00")]
    [TestCase("0800")]
    [TestCase("")]
    public void Malformed_OpenTime_fails(string openTime)
    {
        _validator.TestValidate(Command(openTime: openTime)).ShouldHaveValidationErrorFor(x => x.OpenTime);
    }

    [TestCase("1;2;3")]
    [TestCase("7")]
    [TestCase("")]
    public void Malformed_OpenDays_fails(string openDays)
    {
        _validator.TestValidate(Command(openDays: openDays)).ShouldHaveValidationErrorFor(x => x.OpenDays);
    }

    [Test]
    public void Non_image_LogoBase64_fails()
    {
        _validator.TestValidate(Command(logoBase64: "data:text/plain,hello"))
            .ShouldHaveValidationErrorFor(x => x.LogoBase64);
    }

    [Test]
    public void Null_LogoBase64_passes()
    {
        _validator.TestValidate(Command(logoBase64: null)).ShouldNotHaveValidationErrorFor(x => x.LogoBase64);
    }
}
