using CliniSys.Application.Commands.HealthPlans.UpdateHealthPlan;
using FluentValidation.TestHelper;

namespace CliniSys.Application.Tests.HealthPlans;

[TestFixture]
public class UpdateHealthPlanCommandValidatorTests
{
    private readonly UpdateHealthPlanCommandValidator _validator = new();

    [Test]
    public void Valid_command_passes()
    {
        _validator.TestValidate(new UpdateHealthPlanCommand(Guid.NewGuid(), "Unimed", "note"))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Empty_Name_fails()
    {
        _validator.TestValidate(new UpdateHealthPlanCommand(Guid.NewGuid(), "", null))
            .ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Test]
    public void Name_over_200_chars_fails()
    {
        _validator.TestValidate(new UpdateHealthPlanCommand(Guid.NewGuid(), new string('a', 201), null))
            .ShouldHaveValidationErrorFor(x => x.Name);
    }
}
