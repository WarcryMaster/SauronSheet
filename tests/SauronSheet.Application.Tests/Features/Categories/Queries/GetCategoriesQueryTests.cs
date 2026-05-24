using SauronSheet.Domain.Common;
using Xunit;
using Moq;
using SauronSheet.Application.Features.Categories.Queries;

using SauronSheet.Application.Tests.Common;
using SauronSheet.Domain.Repositories;
using SauronSheet.Domain.Entities;
using SauronSheet.Domain.ValueObjects;

namespace SauronSheet.Application.Tests.Features.Categories.Queries;

public class GetCategoriesQueryTests
{
    private readonly Mock<ICategoryRepository> _mockCategoryRepo;
    private readonly Mock<ITransactionRepository> _mockTransactionRepo;
    private readonly Mock<IUserContext> _mockUserContext;

    public GetCategoriesQueryTests()
    {
        _mockCategoryRepo = new Mock<ICategoryRepository>();
        _mockTransactionRepo = new Mock<ITransactionRepository>();
        _mockUserContext = new Mock<IUserContext>();

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
            _mockUserContext.Object);

        var userId = new UserId("test-user-id");
        var categories = new List<Category>
        {
            TestCategoryFactory.CreateUserCategory(userId: userId, name: "Groceries"),
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
    public async Task GetCategories_SortedAlphabetically()
    {
        // Arrange
        var handler = new GetCategoriesQueryHandler(
            _mockCategoryRepo.Object,
            _mockTransactionRepo.Object,
            _mockUserContext.Object);

        var userId = new UserId("test-user-id");
        var categories = new List<Category>
        {
            TestCategoryFactory.CreateUserCategory(userId: userId, name: "Zebra"),
            TestCategoryFactory.CreateUserCategory(userId: userId, name: "Apple"),
            TestCategoryFactory.CreateUserCategory(userId: userId, name: "Banana")
        };

        _mockCategoryRepo.Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync(categories);

        _mockTransactionRepo.Setup(x => x.GetCountsByCategoriesAsync(It.IsAny<List<CategoryId>>()))
            .ReturnsAsync(new Dictionary<CategoryId, int>());

        var query = new GetCategoriesQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert — purely alphabetical, no system-default bias
        Assert.Equal(3, result.Count);
        Assert.Equal("Apple", result[0].Name);
        Assert.Equal("Banana", result[1].Name);
        Assert.Equal("Zebra", result[2].Name);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task GetCategories_NoLongerSeedsSystemDefaults()
    {
        // Arrange
        var handler = new GetCategoriesQueryHandler(
            _mockCategoryRepo.Object,
            _mockTransactionRepo.Object,
            _mockUserContext.Object);

        var userId = new UserId("test-user-id");

        // No categories exist in the repo
        _mockCategoryRepo.Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync(new List<Category>());

        _mockTransactionRepo.Setup(x => x.GetCountsByCategoriesAsync(It.IsAny<List<CategoryId>>()))
            .ReturnsAsync(new Dictionary<CategoryId, int>());

        var query = new GetCategoriesQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert — handler no longer calls MediatR to seed defaults
        // Simply returns whatever the repo returns (empty list is valid)
        Assert.Empty(result);
    }
}
