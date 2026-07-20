using MediatR;

namespace CliniSys.Application.Common.Interfaces;

/// <summary>Marker interface for a CQRS query returning <typeparamref name="TResult"/>.</summary>
/// <typeparam name="TResult">The handler return type.</typeparam>
public interface IQuery<TResult> : IRequest<TResult> { }
