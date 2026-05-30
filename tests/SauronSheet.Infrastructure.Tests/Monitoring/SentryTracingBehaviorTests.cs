namespace SauronSheet.Infrastructure.Tests.Monitoring;

using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using SauronSheet.Domain.Exceptions;
using SauronSheet.Infrastructure.Monitoring;
using Xunit;

/// <summary>
/// Verifies the exception-propagation contract of SentryTracingBehavior.
/// Direct assertions on SentrySdk.CaptureException are intentionally omitted:
/// SentrySdk is a static class that requires a full DSN + fake ISentryClient to intercept,
/// which would require adding the Sentry.Testing package — out of scope for a minimal fix.
/// The tests below cover the observable behavioral contract: exceptions must be re-thrown,
/// not swallowed, regardless of type.
/// </summary>
[Trait("Category", "Infrastructure")]
public class SentryTracingBehaviorTests
{
    private readonly SentryTracingBehavior<FakeRequest, string> _behavior = new();

    // Minimal request type for pipeline wiring
    private record FakeRequest : IRequest<string>;

    [Fact]
    public async Task Handle_SuccessfulHandler_ReturnsResponse()
    {
        // Arrange
        var request = new FakeRequest();
        Task<string> Next() => Task.FromResult("ok");

        // Act
        var result = await _behavior.Handle(request, Next, CancellationToken.None);

        // Assert
        Assert.Equal("ok", result);
    }

    [Fact]
    public async Task Handle_DomainException_Rethrows()
    {
        // Arrange — mirrors the real production scenario (duplicate budget)
        var request = new FakeRequest();
        var exception = new DomainException("A budget for this category in May 2026 already exists.");
        Task<string> Next() => Task.FromException<string>(exception);

        // Act & Assert
        var thrown = await Assert.ThrowsAsync<DomainException>(
            () => _behavior.Handle(request, Next, CancellationToken.None));

        Assert.Same(exception, thrown);
    }

    [Fact]
    public async Task Handle_UnauthorizedAccessException_Rethrows()
    {
        // Arrange — existing behavior regression test
        var request = new FakeRequest();
        var exception = new UnauthorizedAccessException("Invalid credentials.");
        Task<string> Next() => Task.FromException<string>(exception);

        // Act & Assert
        var thrown = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _behavior.Handle(request, Next, CancellationToken.None));

        Assert.Same(exception, thrown);
    }

    [Fact]
    public async Task Handle_UnexpectedSystemException_Rethrows()
    {
        // Arrange — system errors must still propagate (and WILL be captured by Sentry)
        var request = new FakeRequest();
        var exception = new InvalidOperationException("Database connection lost.");
        Task<string> Next() => Task.FromException<string>(exception);

        // Act & Assert
        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _behavior.Handle(request, Next, CancellationToken.None));

        Assert.Same(exception, thrown);
    }
}
