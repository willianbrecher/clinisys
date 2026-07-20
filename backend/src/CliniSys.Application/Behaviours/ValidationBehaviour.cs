using FluentValidation;
using MediatR;

namespace CliniSys.Application.Behaviours;

/// <summary>
/// MediatR pipeline behaviour that runs all registered FluentValidation validators for the
/// request before the handler executes. Throws <see cref="ValidationException"/> on failure;
/// the API exception middleware maps this to HTTP 400.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public sealed class ValidationBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    /// <summary>Initialises the behaviour with all DI-registered validators for <typeparamref name="TRequest"/>.</summary>
    /// <param name="validators">Resolved validators.</param>
    public ValidationBehaviour(IEnumerable<IValidator<TRequest>> validators) =>
        _validators = validators;

    /// <summary>Validates the request, then calls the next handler in the pipeline.</summary>
    /// <param name="request">The incoming request.</param>
    /// <param name="next">Delegate to the next pipeline step.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The handler result.</returns>
    /// <exception cref="ValidationException">Thrown when any validator reports failures.</exception>
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any()) return await next();

        var context = new ValidationContext<TRequest>(request);
        var results = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken)));
        var failures = results.SelectMany(r => r.Errors).Where(f => f is not null).ToList();

        if (failures.Count > 0) throw new ValidationException(failures);

        return await next();
    }
}
