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

public class RenameSubcategoryCommandHandlerTests
{
    private readonly Mock<ISubcategoryRepository> _mockSubcategoryRepo;
    private readonly Mock<ICategoryRepository> _mockCategoryRepo;
    private readonly Mock<IUserContext> _mockUserContext;
    private readonly UserId _userId;
    private readonly CategoryId _categoryId;

    public RenameSubcategoryCommandHandlerTests()
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
    public async Task RenameSubcategory_ValidInput_UpdatesName()
    {
        // Arrange
        var handler = new RenameSubcategoryCommandHandler(
            _mockSubcategoryRepo.Object,
            _mockCategoryRepo.Object,
            _mockUserContext.Object);

        var subcategoryId = SubcategoryId.New();
        var subcategory = TestSubcategoryFactory.CreateUserSubcategory(
            subcategoryId: subcategoryId,
            userId: _userId,
            categoryId: _categoryId,
            name: "Old Name");

        var category = TestCategoryFactory.CreateUserCategory(
            categoryId: _categoryId,
            userId: _userId,
            name: "Food");

        _mockSubcategoryRepo.Setup(x => x.GetByIdAsync(subcategoryId))
            .ReturnsAsync(subcategory);

        _mockCategoryRepo.Setup(x => x.GetByIdAsync(_categoryId))
            .ReturnsAsync(category);

        _mockSubcategoryRepo.Setup(x => x.FindByNameAsync(
                _userId,
                _categoryId,
                "New Name"))
            .ReturnsAsync((Subcategory?)null);

        var command = new RenameSubcategoryCommand(
            SubcategoryId: subcategoryId.Value,
            NewName: "New Name");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(Unit.Value, result);
        _mockSubcategoryRepo.Verify(x => x.UpdateAsync(
            It.Is<Subcategory>(s => s.Name.Value == "New Name"),
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task RenameSubcategory_NotFound_ThrowsEntityNotFoundException()
    {
        // Arrange
        var handler = new RenameSubcategoryCommandHandler(
            _mockSubcategoryRepo.Object,
            _mockCategoryRepo.Object,
            _mockUserContext.Object);

        var subcategoryId = Guid.NewGuid();

        _mockSubcategoryRepo.Setup(x => x.GetByIdAsync(new SubcategoryId(subcategoryId)))
            .ReturnsAsync((Subcategory?)null);

        var command = new RenameSubcategoryCommand(
            SubcategoryId: subcategoryId,
            NewName: "New Name");

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task RenameSubcategory_CategoryBelongsToDifferentUser_ThrowsEntityNotFoundException()
    {
        // Arrange
        var handler = new RenameSubcategoryCommandHandler(
            _mockSubcategoryRepo.Object,
            _mockCategoryRepo.Object,
            _mockUserContext.Object);

        var subcategoryId = SubcategoryId.New();
        var subcategory = TestSubcategoryFactory.CreateUserSubcategory(
            subcategoryId: subcategoryId,
            userId: _userId,
            categoryId: _categoryId,
            name: "Old Name");

        var otherUserId = new UserId("other-user");
        var category = TestCategoryFactory.CreateUserCategory(
            categoryId: _categoryId,
            userId: otherUserId,
            name: "Food");

        _mockSubcategoryRepo.Setup(x => x.GetByIdAsync(subcategoryId))
            .ReturnsAsync(subcategory);

        _mockCategoryRepo.Setup(x => x.GetByIdAsync(_categoryId))
            .ReturnsAsync(category);

        var command = new RenameSubcategoryCommand(
            SubcategoryId: subcategoryId.Value,
            NewName: "New Name");

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            handler.Handle(command, CancellationToken.None));
    }
}
