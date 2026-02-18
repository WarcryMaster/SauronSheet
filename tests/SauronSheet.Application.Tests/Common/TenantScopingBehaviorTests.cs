using Xunit;
using Moq;
using MediatR;
using SauronSheet.Application.Common;
using SauronSheet.Application.Common.Behaviors;

namespace SauronSheet.Application.Tests.Common;

public class TenantScopingBehaviorTests
{
    [Fact]
    [Trait("Category", "Application")]
    public async Task TenantScoping_Authenticated_Proceeds()
    {
        // Arrange
        var mockUserContext = new Mock<IUserContext>();
        mockUserContext.Setup(x => x.IsAuthenticated).Returns(true);

        var behavior = new TenantScopingBehavior<TestRequest, string>(mockUserContext.Object);
        var requestHandlerDelegate = new Mock<RequestHandlerDelegate<string>>();
        requestHandlerDelegate.Setup(x => x()).ReturnsAsync("success");

        var request = new TestRequest();

        // Act
        var result = await behavior.Handle(request, requestHandlerDelegate.Object, CancellationToken.None);

        // Assert
        Assert.Equal("success", result);
        requestHandlerDelegate.Verify(x => x(), Times.Once);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task TenantScoping_Unauthenticated_ThrowsUnauthorized()
    {
        // Arrange
        var mockUserContext = new Mock<IUserContext>();
        mockUserContext.Setup(x => x.IsAuthenticated).Returns(false);

        var behavior = new TenantScopingBehavior<TestRequest, string>(mockUserContext.Object);
        var requestHandlerDelegate = new Mock<RequestHandlerDelegate<string>>();

        var request = new TestRequest();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            behavior.Handle(request, requestHandlerDelegate.Object, CancellationToken.None));

        Assert.Contains("not authenticated", exception.Message);
        requestHandlerDelegate.Verify(x => x(), Times.Never);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task TenantScoping_AnonymousRequest_SkipsCheck()
    {
        // Arrange
        var mockUserContext = new Mock<IUserContext>();
        mockUserContext.Setup(x => x.IsAuthenticated).Returns(false);

        var behavior = new TenantScopingBehavior<TestAnonymousRequest, string>(mockUserContext.Object);
        var requestHandlerDelegate = new Mock<RequestHandlerDelegate<string>>();
        requestHandlerDelegate.Setup(x => x()).ReturnsAsync("anonymous-success");

        var request = new TestAnonymousRequest();

        // Act
        var result = await behavior.Handle(request, requestHandlerDelegate.Object, CancellationToken.None);

        // Assert
        Assert.Equal("anonymous-success", result);
        requestHandlerDelegate.Verify(x => x(), Times.Once);
    }

    // Test doubles
    public class TestRequest : IRequest<string>
    {
    }

    public class TestAnonymousRequest : IRequest<string>, IAnonymousRequest
    {
    }
}
