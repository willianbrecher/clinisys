using CliniSys.Application.Behaviours;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using NSubstitute;

namespace CliniSys.Application.Tests.Behaviours;

[TestFixture]
public class ValidationBehaviourTests
{
    // Public so Castle DynamicProxy (via NSubstitute) can reference them as generic arguments.
    public record FakeRequest(string Value) : IRequest<FakeResponse>;
    public record FakeResponse(string Value);

    private static readonly FakeRequest Request = new("x");
    private static readonly FakeResponse Response = new("handled");

    private int _nextCalls;
    private RequestHandlerDelegate<FakeResponse> _next = null!;

    [SetUp]
    public void SetUp()
    {
        _nextCalls = 0;
        _next = _ => { _nextCalls++; return Task.FromResult(Response); };
    }

    [Test]
    public async Task Handle_WithNoValidators_CallsNextAndReturnsItsResult()
    {
        var sut = new ValidationBehaviour<FakeRequest, FakeResponse>([]);

        var result = await sut.Handle(Request, _next, CancellationToken.None);

        result.Should().Be(Response);
        _nextCalls.Should().Be(1);
    }

    [Test]
    public async Task Handle_WhenAllValidatorsPass_CallsNext()
    {
        var validator = Substitute.For<IValidator<FakeRequest>>();
        validator.ValidateAsync(Arg.Any<ValidationContext<FakeRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());
        var sut = new ValidationBehaviour<FakeRequest, FakeResponse>([validator]);

        var result = await sut.Handle(Request, _next, CancellationToken.None);

        result.Should().Be(Response);
        _nextCalls.Should().Be(1);
    }

    [Test]
    public async Task Handle_WhenAValidatorFails_ThrowsValidationExceptionWithoutCallingNext()
    {
        var failure = new ValidationFailure("Value", "Value is required");
        var validator = Substitute.For<IValidator<FakeRequest>>();
        validator.ValidateAsync(Arg.Any<ValidationContext<FakeRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult([failure]));
        var sut = new ValidationBehaviour<FakeRequest, FakeResponse>([validator]);

        var act = () => sut.Handle(Request, _next, CancellationToken.None);

        (await act.Should().ThrowAsync<ValidationException>())
            .Which.Errors.Should().ContainSingle(e => e.ErrorMessage == "Value is required");
        _nextCalls.Should().Be(0);
    }
}
