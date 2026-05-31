using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using SauronSheet.Application.Features.Auth.Commands;
using SauronSheet.Domain.Exceptions;
using SauronSheet.Frontend.Pages.Auth;
using Xunit;

namespace SauronSheet.Frontend.Tests.Pages.Auth;

public class LoginModelTests
{
    [Fact]
    [Trait("Category", "Frontend")]
    public async Task OnPostAsync_CuandoLasCredencialesSonInvalidas_DevuelveLaPaginaYRegistraInformacion()
    {
        // Arrange
        Mock<IMediator> mediatorMock = new Mock<IMediator>();
        Mock<ILogger<LoginModel>> loggerMock = new Mock<ILogger<LoginModel>>();
        Mock<IWebHostEnvironment> environmentMock = new Mock<IWebHostEnvironment>();
        environmentMock.SetupGet(environment => environment.EnvironmentName).Returns(Environments.Development);

        mediatorMock
            .Setup(mediator => mediator.Send(
                It.Is<LoginUserCommand>(command => command.Email == "user@example.com" && command.Password == "bad-password"),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedAccessException("Invalid credentials."));

        LoginModel model = CreateModel(mediatorMock, loggerMock, environmentMock);
        model.Input = new LoginInputModel
        {
            Email = "user@example.com",
            Password = "bad-password"
        };

        // Act
        IActionResult result = await model.OnPostAsync();

        // Assert
        PageResult page = Assert.IsType<PageResult>(result);
        Assert.NotNull(page);
        Assert.Equal("Invalid email or password.", model.ErrorMessage);
        VerifyLog(loggerMock, LogLevel.Information, "Login failed for email");
    }

    [Fact]
    [Trait("Category", "Frontend")]
    public async Task OnPostAsync_CuandoHayDomainException_DevuelveLaPaginaYRegistraInformacion()
    {
        // Arrange
        Mock<IMediator> mediatorMock = new Mock<IMediator>();
        Mock<ILogger<LoginModel>> loggerMock = new Mock<ILogger<LoginModel>>();
        Mock<IWebHostEnvironment> environmentMock = new Mock<IWebHostEnvironment>();
        environmentMock.SetupGet(environment => environment.EnvironmentName).Returns(Environments.Development);

        mediatorMock
            .Setup(mediator => mediator.Send(It.IsAny<LoginUserCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DomainException("Account requires confirmation."));

        LoginModel model = CreateModel(mediatorMock, loggerMock, environmentMock);
        model.Input = new LoginInputModel
        {
            Email = "user@example.com",
            Password = "password"
        };

        // Act
        IActionResult result = await model.OnPostAsync();

        // Assert
        PageResult page = Assert.IsType<PageResult>(result);
        Assert.NotNull(page);
        Assert.Equal("Account requires confirmation.", model.ErrorMessage);
        VerifyLog(loggerMock, LogLevel.Information, "Login failed for email");
    }

    private static LoginModel CreateModel(
        Mock<IMediator> mediatorMock,
        Mock<ILogger<LoginModel>> loggerMock,
        Mock<IWebHostEnvironment> environmentMock)
    {
        LoginModel model = new LoginModel(mediatorMock.Object, loggerMock.Object, environmentMock.Object)
        {
            PageContext = new PageContext(new ActionContext(
                new DefaultHttpContext(),
                new RouteData(),
                new PageActionDescriptor()))
        };

        return model;
    }

    private static void VerifyLog(Mock<ILogger<LoginModel>> loggerMock, LogLevel expectedLevel, string expectedMessage)
    {
        loggerMock.Verify(
            logger => logger.Log(
                It.Is<LogLevel>(level => level == expectedLevel),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((value, _) => value.ToString()!.Contains(expectedMessage, StringComparison.Ordinal)),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
