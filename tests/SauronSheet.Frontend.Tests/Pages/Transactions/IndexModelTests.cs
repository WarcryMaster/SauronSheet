using System.Security.Claims;
using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using Moq;
using SauronSheet.Application.Features.Transactions.DTOs;
using SauronSheet.Application.Features.Transactions.Commands;
using TransactionsIndexPageModel = SauronSheet.Frontend.Pages.Transactions.IndexModel;
using SauronSheet.Frontend.Helpers;
using SauronSheet.Domain.Exceptions;
using Xunit;

namespace SauronSheet.Frontend.Tests.Pages.Transactions;

public class IndexModelTests
{
    private static readonly Guid TransactionId = Guid.NewGuid();
    private static readonly DateTime TransactionDate = new(2026, 5, 25);
    private static readonly DateTime CreatedAt = new(2026, 5, 25, 10, 30, 0, DateTimeKind.Utc);

    [Fact]
    [Trait("Category", "Frontend")]
    public void Build_ResolvedCategoryAndSubcategory_ReturnsResolvedValues()
    {
        // CategorySource=UserOverride: user has manually assigned a category.
        // Per DH-1c the user's override (CategoryName) MUST take precedence over BankCategory.
        TransactionDto transaction = CreateTransaction(
            categoryName: "Food",
            subcategoryName: "Dining Out",
            bankCategory: "Compras",
            bankSubcategory: "Restaurantes",
            categorySource: "UserOverride");
        TransactionCategoryDisplay result = TransactionCategoryDisplayHelper.Build(transaction);

        Assert.Equal("Food", result.PrimaryText);
        Assert.Equal("Dining Out", result.SecondaryText);
        Assert.False(result.IsUncategorized);
        Assert.False(result.UsesRawCategoryFallback);
    }

    [Fact]
    [Trait("Category", "Frontend")]
    public void Build_ResolvedCategoryWithoutSubcategory_UsesBankSubcategoryFallbackIndependently()
    {
        // CategorySource=UserOverride: per DH-1c, CategoryName shown.
        // Subcategory: resolvedSubcategory=null → bankSubcategory used (DH-1 only covers primary).
        TransactionDto transaction = CreateTransaction(
            categoryName: "Food",
            subcategoryName: null,
            bankCategory: "Compras",
            bankSubcategory: "Restaurantes",
            categorySource: "UserOverride");
        TransactionCategoryDisplay result = TransactionCategoryDisplayHelper.Build(transaction);

        Assert.Equal("Food", result.PrimaryText);
        Assert.Equal("Restaurantes", result.SecondaryText);
    }

    [Fact]
    [Trait("Category", "Frontend")]
    public void Build_NoResolvedCategory_UsesRawBankCategoryAndSubcategory()
    {
        TransactionDto transaction = CreateTransaction(
            categoryName: null,
            subcategoryName: null,
            bankCategory: "Compras",
            bankSubcategory: "Ropa y complementos");
        TransactionCategoryDisplay result = TransactionCategoryDisplayHelper.Build(transaction);

        Assert.Equal("Compras", result.PrimaryText);
        Assert.Equal("Ropa y complementos", result.SecondaryText);
        Assert.False(result.IsUncategorized);
        Assert.True(result.UsesRawCategoryFallback);
    }

    [Fact]
    [Trait("Category", "Frontend")]
    public void Build_NoResolvedCategoryOrBankSubcategory_UsesRawBankCategoryOnly()
    {
        TransactionDto transaction = CreateTransaction(
            categoryName: null,
            subcategoryName: null,
            bankCategory: "Compras",
            bankSubcategory: null);
        TransactionCategoryDisplay result = TransactionCategoryDisplayHelper.Build(transaction);

        Assert.Equal("Compras", result.PrimaryText);
        Assert.Null(result.SecondaryText);
    }

    [Fact]
    [Trait("Category", "Frontend")]
    public void Build_NoResolvedCategoryButResolvedSubcategory_UsesResolvedSubcategory()
    {
        TransactionDto transaction = CreateTransaction(
            categoryName: null,
            subcategoryName: "Dining Out",
            bankCategory: "Compras",
            bankSubcategory: "Restaurantes");

        TransactionCategoryDisplay result = TransactionCategoryDisplayHelper.Build(transaction);

        Assert.Equal("Compras", result.PrimaryText);
        Assert.Equal("Dining Out", result.SecondaryText);
    }

    [Fact]
    [Trait("Category", "Frontend")]
    public void Build_NoUsefulPrimaryCategoryInformation_ReturnsUncategorizedAndBankSubcategoryWhenAvailable()
    {
        TransactionDto transaction = CreateTransaction(
            categoryName: null,
            subcategoryName: null,
            bankCategory: "   ",
            bankSubcategory: " Other ");

        TransactionCategoryDisplay result = TransactionCategoryDisplayHelper.Build(transaction);

        Assert.Equal("Uncategorized", result.PrimaryText);
        Assert.Equal("Other", result.SecondaryText);
        Assert.True(result.IsUncategorized);
        Assert.False(result.UsesRawCategoryFallback);
    }

    [Fact]
    [Trait("Category", "Frontend")]
    public void Build_NoUsefulCategoryInformation_ReturnsUncategorized()
    {
        TransactionDto transaction = CreateTransaction(
            categoryName: null,
            subcategoryName: null,
            bankCategory: "   ",
            bankSubcategory: " ");
        TransactionCategoryDisplay result = TransactionCategoryDisplayHelper.Build(transaction);

        Assert.Equal("Uncategorized", result.PrimaryText);
        Assert.Null(result.SecondaryText);
        Assert.True(result.IsUncategorized);
    }

    [Fact]
    [Trait("Category", "Frontend")]
    public void Build_ResolvedCategoryWhitespace_FallsBackToTrimmedBankValues()
    {
        TransactionDto transaction = CreateTransaction(
            categoryName: "   ",
            subcategoryName: "Ignored",
            bankCategory: " Compras ",
            bankSubcategory: " Ropa ");
        TransactionCategoryDisplay result = TransactionCategoryDisplayHelper.Build(transaction);

        Assert.Equal("Compras", result.PrimaryText);
        Assert.Equal("Ignored", result.SecondaryText);
    }

    [Fact]
    [Trait("Category", "Frontend")]
    public void Build_NoUsefulCategoryInformation_MarksUncategorized()
    {
        TransactionDto transaction = CreateTransaction(
            categoryName: null,
            subcategoryName: null,
            bankCategory: null,
            bankSubcategory: null);
        TransactionCategoryDisplay result = TransactionCategoryDisplayHelper.Build(transaction);

        Assert.True(result.IsUncategorized);
    }

    [Fact]
    [Trait("Category", "Frontend")]
    public void Build_ResolvedOrRawCategoryExists_DoesNotMarkUncategorized()
    {
        TransactionDto transaction = CreateTransaction(
            categoryName: null,
            subcategoryName: null,
            bankCategory: "Compras",
            bankSubcategory: null);
        TransactionCategoryDisplay result = TransactionCategoryDisplayHelper.Build(transaction);

        Assert.False(result.IsUncategorized);
    }

    [Fact]
    [Trait("Category", "Frontend")]
    public void Build_NoResolvedCategoryButBankCategoryExists_MarksRawCategoryFallback()
    {
        TransactionDto transaction = CreateTransaction(
            categoryName: null,
            subcategoryName: null,
            bankCategory: "Compras",
            bankSubcategory: "Ropa y complementos");
        TransactionCategoryDisplay result = TransactionCategoryDisplayHelper.Build(transaction);

        Assert.True(result.UsesRawCategoryFallback);
    }

    [Fact]
    [Trait("Category", "Frontend")]
    public void Build_ResolvedCategoryExists_DoesNotMarkRawCategoryFallback()
    {
        // CategorySource=UserOverride: user has explicitly assigned a category.
        // Per DH-1c, CategoryName is shown → UsesRawCategoryFallback MUST be false.
        TransactionDto transaction = CreateTransaction(
            categoryName: "Food",
            subcategoryName: "Dining Out",
            bankCategory: "Compras",
            bankSubcategory: "Restaurantes",
            categorySource: "UserOverride");
        TransactionCategoryDisplay result = TransactionCategoryDisplayHelper.Build(transaction);

        Assert.False(result.UsesRawCategoryFallback);
    }

    [Fact]
    [Trait("Category", "Frontend")]
    public async Task OnPostDeleteAsync_CuandoHayDomainException_DevuelveErrorYRegistraInformacion()
    {
        Mock<IMediator> mediatorMock = new Mock<IMediator>();
        Mock<ILogger<TransactionsIndexPageModel>> loggerMock = new Mock<ILogger<TransactionsIndexPageModel>>();

        mediatorMock
            .Setup(mediator => mediator.Send(It.IsAny<DeleteTransactionCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new EntityNotFoundException("Transaction", TransactionId));

        TransactionsIndexPageModel model = CreateModel(mediatorMock, loggerMock);

        IActionResult result = await model.OnPostDeleteAsync(TransactionId);

        RedirectToPageResult redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Null(redirect.PageName);
        Assert.Equal($"Entity 'Transaction' with id '{TransactionId}' was not found.", model.TempData["ErrorMessage"]);
        VerifyLog(loggerMock, LogLevel.Information, "Transaction deletion failed");
    }

    [Fact]
    [Trait("Category", "Frontend")]
    public async Task OnPostBulkDeleteAsync_SinClaimDeUsuario_DevuelveErrorSinRegistrarError()
    {
        Mock<IMediator> mediatorMock = new Mock<IMediator>();
        Mock<ILogger<TransactionsIndexPageModel>> loggerMock = new Mock<ILogger<TransactionsIndexPageModel>>();
        Dictionary<string, StringValues> formData = new Dictionary<string, StringValues>
        {
            ["transactionIds"] = JsonSerializer.Serialize(new[] { TransactionId.ToString() })
        };

        TransactionsIndexPageModel model = CreateModel(mediatorMock, loggerMock, formData, includeUserClaim: false);

        IActionResult result = await model.OnPostBulkDeleteAsync();

        RedirectToPageResult redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Null(redirect.PageName);
        Assert.Equal("Authentication error. Please log in again.", model.TempData["ErrorMessage"]);
        VerifyLog(loggerMock, LogLevel.Information, "Bulk delete skipped because the user claim was missing");
        mediatorMock.Verify(mediator => mediator.Send(It.IsAny<BulkDeleteTransactionsCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Category", "Frontend")]
    public async Task OnPostBulkDeleteAsync_CuandoExpiraLaAutenticacion_DevuelveErrorSinRegistrarError()
    {
        Mock<IMediator> mediatorMock = new Mock<IMediator>();
        Mock<ILogger<TransactionsIndexPageModel>> loggerMock = new Mock<ILogger<TransactionsIndexPageModel>>();
        Dictionary<string, StringValues> formData = new Dictionary<string, StringValues>
        {
            ["transactionIds"] = JsonSerializer.Serialize(new[] { TransactionId.ToString() })
        };

        mediatorMock
            .Setup(mediator => mediator.Send(It.IsAny<BulkDeleteTransactionsCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedAccessException("Session expired."));

        TransactionsIndexPageModel model = CreateModel(mediatorMock, loggerMock, formData);

        IActionResult result = await model.OnPostBulkDeleteAsync();

        RedirectToPageResult redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Null(redirect.PageName);
        Assert.Equal("Authentication error. Please log in again.", model.TempData["ErrorMessage"]);
        VerifyLog(loggerMock, LogLevel.Information, "Bulk delete failed because authentication expired");
    }

    private static TransactionDto CreateTransaction(
        string? categoryName,
        string? subcategoryName,
        string? bankCategory,
        string? bankSubcategory,
        string categorySource = "RawOnly")
    {
        return new TransactionDto(
            Id: TransactionId,
            Amount: -25.50m,
            Currency: "EUR",
            Date: TransactionDate,
            Description: "Test transaction",
            CategoryId: null,
            CategoryName: categoryName,
            ImportedFrom: "statement.pdf",
            CreatedAt: CreatedAt,
            BankCategory: bankCategory,
            BankSubcategory: bankSubcategory,
            SubcategoryId: null,
            SubcategoryName: subcategoryName,
            CategorySource: categorySource);
    }

    private static TransactionsIndexPageModel CreateModel(
        Mock<IMediator> mediatorMock,
        Mock<ILogger<TransactionsIndexPageModel>>? loggerMock = null,
        Dictionary<string, StringValues>? formValues = null,
        bool includeUserClaim = true)
    {
        DefaultHttpContext httpContext = new DefaultHttpContext();

        if (includeUserClaim)
        {
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim("sub", "test-user-id") },
                "TestAuth"));
        }

        if (formValues != null)
        {
            httpContext.Request.ContentType = "application/x-www-form-urlencoded";
            httpContext.Request.Form = new FormCollection(formValues);
        }

        PageContext pageContext = new PageContext(new ActionContext(
            httpContext,
            new RouteData(),
            new PageActionDescriptor(),
            new ModelStateDictionary()));

        ILogger<TransactionsIndexPageModel> logger = loggerMock?.Object ?? NullLogger<TransactionsIndexPageModel>.Instance;

        TransactionsIndexPageModel model = new TransactionsIndexPageModel(mediatorMock.Object, logger)
        {
            PageContext = pageContext,
            TempData = new TestTempDataDictionary()
        };

        return model;
    }

    private static void VerifyLog(Mock<ILogger<TransactionsIndexPageModel>> loggerMock, LogLevel expectedLevel, string expectedMessage)
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

    private sealed class TestTempDataDictionary : Dictionary<string, object?>, Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionary
    {
        public new object? this[string key]
        {
            get => TryGetValue(key, out object? value) ? value : null;
            set => base[key] = value;
        }

        public new void Add(string key, object? value) => base.Add(key, value);

        public void Keep()
        {
        }

        public void Keep(string key)
        {
        }

        public object? Peek(string key) => this[key];

        public void Load()
        {
        }

        public void Save()
        {
        }
    }
}
