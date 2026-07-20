using MediatR;

namespace CliniSys.Application.Common.Interfaces;

/// <summary>Marker interface for a CQRS command returning <typeparamref name="TResult"/>.</summary>
/// <typeparam name="TResult">The handler return type.</typeparam>
public interface ICommand<TResult> : IRequest<TResult> { }
