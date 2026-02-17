using SauronSheet.Domain.Exceptions;
using Xunit;

namespace SauronSheet.Domain.Tests.Exceptions;

/// <summary>
/// Unit tests for DomainException base class.
/// </summary>
public class DomainExceptionTests
{
    [Fact]
    [Trait("Category", "Domain")]
    public void DomainException_CarriesMessage()
    {
        const string message = "Invalid state detected";

        var ex = new DomainException(message);

        Assert.Equal(message, ex.Message);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void DomainException_CarriesInnerException()
    {
        const string message = "Outer error";
        var inner = new ArgumentException("Inner error");

        var ex = new DomainException(message, inner);

        Assert.Equal(message, ex.Message);
        Assert.Equal(inner, ex.InnerException);
    }
}
