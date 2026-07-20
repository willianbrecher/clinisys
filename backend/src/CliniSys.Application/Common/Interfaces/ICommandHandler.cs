using MediatR;

namespace CliniSys.Application.Common.Interfaces;

/// <summary>Marker interface for a CQRS command handler.</summary>
/// <typeparam name="TCommand">The command type.</typeparam>
/// <typeparam name="TResult">The result type.</typeparam>
public interface ICommandHandler<in TCommand, TResult> : IRequestHandler<TCommand, TResult>
    where TCommand : ICommand<TResult> { }
