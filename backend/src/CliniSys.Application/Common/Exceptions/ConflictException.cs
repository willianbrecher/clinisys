namespace CliniSys.Application.Common.Exceptions;

/// <summary>Thrown when a request conflicts with current state (e.g. double-booking). Maps to HTTP 409.</summary>
/// <param name="message">Human-readable description of the conflict.</param>
public class ConflictException(string message) : Exception(message);
