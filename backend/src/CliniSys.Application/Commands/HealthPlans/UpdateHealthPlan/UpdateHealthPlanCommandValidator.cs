using FluentValidation;

namespace CliniSys.Application.Commands.HealthPlans.UpdateHealthPlan;

/// <summary>Validates <see cref="UpdateHealthPlanCommand"/>.</summary>
public class UpdateHealthPlanCommandValidator : AbstractValidator<UpdateHealthPlanCommand>
{
    /// <summary>Defines validation rules.</summary>
    public UpdateHealthPlanCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
