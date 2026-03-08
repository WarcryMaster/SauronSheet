using Xunit;
using Moq;
using SauronSheet.Application.Features.Categories.Queries;
using SauronSheet.Application.Features.Categories.Commands;
using SauronSheet.Application.Common;
using SauronSheet.Application.Tests.Common;
using SauronSheet.Domain.Repositories;
using SauronSheet.Domain.Entities;
using SauronSheet.Domain.ValueObjects;
using MediatR;

namespace SauronSheet.Application.Tests.Features.Categories.Queries;

public class GetCategoriesQueryTests
{
    private readonly Mock<ICategoryRepository> _mockCategoryRepo;
    private readonly Mock<ITransactionRepository> _mockTransactionRepo;
    private readonly Mock<IUserContext> _mockUserContext;
    private readonly Mock<IMediator> _mockMediator;

    public GetCategoriesQueryTests()
    {
        _mockCategoryRepo = new Mock<ICategoryRepository>();
        _mockTransactionRepo = new Mock<ITransactionRepository>();
        _mockUserContext = new Mock<IUserContext>();
        _mockMediator = new Mock<IMediator>();

        _mockUserContext.Setup(x => x.UserId).Returns("test-user-id");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task GetCategories_ReturnsUserCategories()
    {
        // Arrange
        var handler = new GetCategoriesQueryHandler(
            _mockCategoryRepo.Object,
            _mockTransactionRepo.Object,
            _mockUserContext.Object,
            _mockMediator.Object);

        var userId = new UserId("test-user-id");
        var categories = new List<Category>
        {
            TestCategoryFactory.CreateSystemCategory(userId: userId, name: "Groceries"),
            TestCategoryFactory.CreateUserCategory(userId: userId, name: "Entertainment")
        };

        _mockCategoryRepo.Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync(categories);

        _mockTransactionRepo.Setup(x => x.GetCountsByCategoriesAsync(It.IsAny<List<CategoryId>>()))
            .ReturnsAsync(new Dictionary<CategoryId, int>());

        var query = new GetCategoriesQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, c => c.Name == "Groceries");
        Assert.Contains(result, c => c.Name == "Entertainment");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task GetCategories_IncludesSystemDefaults()
    {
        // Arrange
        var handler = new GetCategoriesQueryHandler(
            _mockCategoryRepo.Object,
            _mockTransactionRepo.Object,
            _mockUserContext.Object,
            _mockMediator.Object);

        var userId = new UserId("test-user-id");
        var categories = new List<Category>
        {
            TestCategoryFactory.CreateSystemCategory(userId: userId, name: "Groceries"),
            TestCategoryFactory.CreateSystemCategory(userId: userId, name: "Transport")
        };

        _mockCategoryRepo.Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync(categories);

        _mockTransactionRepo.Setup(x => x.GetCountsByCategoriesAsync(It.IsAny<List<CategoryId>>()))
            .ReturnsAsync(new Dictionary<CategoryId, int>());

        var query = new GetCategoriesQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.All(result, c => Assert.True(c.IsSystemDefault));
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task GetCategories_SortedCorrectly()
    {
        // Arrange
        var handler = new GetCategoriesQueryHandler(
            _mockCategoryRepo.Object,
            _mockTransactionRepo.Object,
            _mockUserContext.Object,
            _mockMediator.Object);

        var userId = new UserId("test-user-id");
        var categories = new List<Category>
        {
            TestCategoryFactory.CreateUserCategory(userId: userId, name: "Zebra"),
            TestCategoryFactory.CreateSystemCategory(userId: userId, name: "Groceries"),
            TestCategoryFactory.CreateUserCategory(userId: userId, name: "Apple")
        };

        _mockCategoryRepo.Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync(categories);

        _mockTransactionRepo.Setup(x => x.GetCountsByCategoriesAsync(It.IsAny<List<CategoryId>>()))
            .ReturnsAsync(new Dictionary<CategoryId, int>());

        var query = new GetCategoriesQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.True(result[0].IsSystemDefault); // System defaults first
        Assert.Equal("Apple", result[1].Name); // Then alphabetically
        Assert.Equal("Zebra", result[2].Name);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task GetCategories_NoSystemDefaults_SeedsAutomatically()
    {
        // Arrange
        var handler = new GetCategoriesQueryHandler(
            _mockCategoryRepo.Object,
            _mockTransactionRepo.Object,
            _mockUserContext.Object,
            _mockMediator.Object);

        var userId = new UserId("test-user-id");

        // First call: no categories exist
        _mockCategoryRepo.Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync(new List<Category>());

        // Mediator will seed defaults
        _mockMediator.Setup(x => x.Send(It.IsAny<SeedSystemDefaultsCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() });

        _mockTransactionRepo.Setup(x => x.GetCountsByCategoriesAsync(It.IsAny<List<CategoryId>>()))
            .ReturnsAsync(new Dictionary<CategoryId, int>());

        var query = new GetCategoriesQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        _mockMediator.Verify(x => x.Send(It.IsAny<SeedSystemDefaultsCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
