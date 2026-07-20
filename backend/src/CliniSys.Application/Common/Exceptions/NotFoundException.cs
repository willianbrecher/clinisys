namespace CliniSys.Application.Common.Exceptions;

/// <summary>Thrown when a requested resource does not exist. Maps to HTTP 404.</summary>
/// <param name="message">Human-readable description of the missing resource.</param>
public class NotFoundException(string message) : Exception(message);
