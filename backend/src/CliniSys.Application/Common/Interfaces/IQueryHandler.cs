using MediatR;

namespace CliniSys.Application.Common.Interfaces;

/// <summary>Marker interface for a CQRS query handler.</summary>
/// <typeparam name="TQuery">The query type.</typeparam>
/// <typeparam name="TResult">The result type.</typeparam>
public interface IQueryHandler<in TQuery, TResult> : IRequestHandler<TQuery, TResult>
    where TQuery : IQuery<TResult> { }
