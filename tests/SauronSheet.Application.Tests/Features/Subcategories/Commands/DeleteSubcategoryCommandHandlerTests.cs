using SauronSheet.Domain.Common;
using Xunit;
using Moq;
using MediatR;
using SauronSheet.Application.Features.Subcategories.Commands;

using SauronSheet.Domain.Repositories;
using SauronSheet.Domain.Entities;
using SauronSheet.Domain.ValueObjects;
using SauronSheet.Domain.Exceptions;
using SauronSheet.Application.Tests.Common;

namespace SauronSheet.Application.Tests.Features.Subcategories.Commands;

public class DeleteSubcategoryCommandHandlerTests
{
    private readonly Mock<ISubcategoryRepository> _mockSubcategoryRepo;
    private readonly Mock<ICategoryRepository> _mockCategoryRepo;
    private readonly Mock<IUserContext> _mockUserContext;
    private readonly UserId _userId;
    private readonly CategoryId _categoryId;

    public DeleteSubcategoryCommandHandlerTests()
    {
        _mockSubcategoryRepo = new Mock<ISubcategoryRepository>();
        _mockCategoryRepo = new Mock<ICategoryRepository>();
        _mockUserContext = new Mock<IUserContext>();

        _userId = new UserId("test-user-id");
        _categoryId = CategoryId.New();

        _mockUserContext.Setup(x => x.UserId).Returns("test-user-id");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task DeleteSubcategory_NoTransactions_DeletesSuccessfully()
    {
        // Arrange
        var handler = new DeleteSubcategoryCommandHandler(
            _mockSubcategoryRepo.Object,
            _mockCategoryRepo.Object,
            _mockUserContext.Object);

        var subcategoryId = SubcategoryId.New();
        var subcategory = TestSubcategoryFactory.CreateUserSubcategory(
            subcategoryId: subcategoryId,
            userId: _userId,
            categoryId: _categoryId,
            name: "Ropa");

        var category = TestCategoryFactory.CreateUserCategory(
            categoryId: _categoryId,
            userId: _userId,
            name: "Food");

        _mockSubcategoryRepo.Setup(x => x.GetByIdAsync(subcategoryId))
            .ReturnsAsync(subcategory);

        _mockCategoryRepo.Setup(x => x.GetByIdAsync(_categoryId))
            .ReturnsAsync(category);

        _mockSubcategoryRepo.Setup(x => x.HasTransactionsAsync(subcategoryId))
            .ReturnsAsync(false);

        var command = new DeleteSubcategoryCommand(
            SubcategoryId: subcategoryId.Value);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(Unit.Value, result);
        _mockSubcategoryRepo.Verify(x => x.DeleteAsync(subcategoryId), Times.Once);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task DeleteSubcategory_HasTransactions_ThrowsDomainException()
    {
        // Arrange
        var handler = new DeleteSubcategoryCommandHandler(
            _mockSubcategoryRepo.Object,
            _mockCategoryRepo.Object,
            _mockUserContext.Object);

        var subcategoryId = SubcategoryId.New();
        var subcategory = TestSubcategoryFactory.CreateUserSubcategory(
            subcategoryId: subcategoryId,
            userId: _userId,
            categoryId: _categoryId,
            name: "Ropa");

        var category = TestCategoryFactory.CreateUserCategory(
            categoryId: _categoryId,
            userId: _userId,
            name: "Food");

        _mockSubcategoryRepo.Setup(x => x.GetByIdAsync(subcategoryId))
            .ReturnsAsync(subcategory);

        _mockCategoryRepo.Setup(x => x.GetByIdAsync(_categoryId))
            .ReturnsAsync(category);

        _mockSubcategoryRepo.Setup(x => x.HasTransactionsAsync(subcategoryId))
            .ReturnsAsync(true);

        var command = new DeleteSubcategoryCommand(
            SubcategoryId: subcategoryId.Value);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(command, CancellationToken.None));

        Assert.Contains("active transactions", ex.Message);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task DeleteSubcategory_NotFound_ThrowsEntityNotFoundException()
    {
        // Arrange
        var handler = new DeleteSubcategoryCommandHandler(
            _mockSubcategoryRepo.Object,
            _mockCategoryRepo.Object,
            _mockUserContext.Object);

        var subcategoryId = Guid.NewGuid();

        _mockSubcategoryRepo.Setup(x => x.GetByIdAsync(new SubcategoryId(subcategoryId)))
            .ReturnsAsync((Subcategory?)null);

        var command = new DeleteSubcategoryCommand(
            SubcategoryId: subcategoryId);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task DeleteSubcategory_CategoryBelongsToDifferentUser_ThrowsEntityNotFoundException()
    {
        // Arrange
        var handler = new DeleteSubcategoryCommandHandler(
            _mockSubcategoryRepo.Object,
            _mockCategoryRepo.Object,
            _mockUserContext.Object);

        var subcategoryId = SubcategoryId.New();
        var subcategory = TestSubcategoryFactory.CreateUserSubcategory(
            subcategoryId: subcategoryId,
            userId: _userId,
            categoryId: _categoryId,
            name: "Ropa");

        var otherUserId = new UserId("other-user");
        var category = TestCategoryFactory.CreateUserCategory(
            categoryId: _categoryId,
            userId: otherUserId,
            name: "Food");

        _mockSubcategoryRepo.Setup(x => x.GetByIdAsync(subcategoryId))
            .ReturnsAsync(subcategory);

        _mockCategoryRepo.Setup(x => x.GetByIdAsync(_categoryId))
            .ReturnsAsync(category);

        var command = new DeleteSubcategoryCommand(
            SubcategoryId: subcategoryId.Value);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            handler.Handle(command, CancellationToken.None));
    }
}
