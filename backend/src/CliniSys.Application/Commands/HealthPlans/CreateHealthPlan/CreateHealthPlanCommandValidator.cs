using FluentValidation;

namespace CliniSys.Application.Commands.HealthPlans.CreateHealthPlan;

/// <summary>Validates <see cref="CreateHealthPlanCommand"/>.</summary>
public class CreateHealthPlanCommandValidator : AbstractValidator<CreateHealthPlanCommand>
{
    /// <summary>Defines validation rules.</summary>
    public CreateHealthPlanCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
