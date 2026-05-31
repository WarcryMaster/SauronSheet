using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Moq;
using SauronSheet.Application.Features.Auth.Commands;
using SauronSheet.Domain.Exceptions;
using SauronSheet.Frontend.Pages.Auth;
using Xunit;

namespace SauronSheet.Frontend.Tests.Pages.Auth;

public class RegisterModelTests
{
    [Fact]
    [Trait("Category", "Frontend")]
    public async Task OnPostAsync_CuandoHayDomainException_DevuelveLaPaginaYRegistraInformacion()
    {
        Mock<IMediator> mediatorMock = new Mock<IMediator>();
        Mock<ILogger<RegisterModel>> loggerMock = new Mock<ILogger<RegisterModel>>();

        mediatorMock
            .Setup(mediator => mediator.Send(It.IsAny<RegisterUserCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DomainException("Email is already registered."));

        RegisterModel model = CreateModel(mediatorMock, loggerMock);
        model.Input = new RegisterInputModel
        {
            Email = "user@example.com",
            Password = "password123",
            ConfirmPassword = "password123"
        };

        IActionResult result = await model.OnPostAsync();

        PageResult page = Assert.IsType<PageResult>(result);
        Assert.NotNull(page);
        Assert.Equal("Email is already registered.", model.ErrorMessage);
        VerifyLog(loggerMock, LogLevel.Information, "Registration failed for email");
    }

    private static RegisterModel CreateModel(
        Mock<IMediator> mediatorMock,
        Mock<ILogger<RegisterModel>> loggerMock)
    {
        RegisterModel model = new RegisterModel(mediatorMock.Object, loggerMock.Object)
        {
            PageContext = new PageContext(new ActionContext(
                new DefaultHttpContext(),
                new RouteData(),
                new PageActionDescriptor()))
        };

        return model;
    }

    private static void VerifyLog(Mock<ILogger<RegisterModel>> loggerMock, LogLevel expectedLevel, string expectedMessage)
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
