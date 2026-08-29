using CliniSys.Application.Commands.HealthPlans.CreateHealthPlan;
using FluentValidation.TestHelper;

namespace CliniSys.Application.Tests.HealthPlans;

[TestFixture]
public class CreateHealthPlanCommandValidatorTests
{
    private readonly CreateHealthPlanCommandValidator _validator = new();

    [Test]
    public void Valid_command_passes()
    {
        _validator.TestValidate(new CreateHealthPlanCommand("Unimed", null))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Empty_Name_fails()
    {
        _validator.TestValidate(new CreateHealthPlanCommand("", null))
            .ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Test]
    public void Name_over_200_chars_fails()
    {
        _validator.TestValidate(new CreateHealthPlanCommand(new string('a', 201), null))
            .ShouldHaveValidationErrorFor(x => x.Name);
    }
}
