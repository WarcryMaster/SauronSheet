using SauronSheet.Domain.Common;
using Xunit;
using Moq;
using SauronSheet.Application.Features.Subcategories.Commands;

using SauronSheet.Domain.Repositories;
using SauronSheet.Domain.Entities;
using SauronSheet.Domain.ValueObjects;
using SauronSheet.Domain.Exceptions;
using SauronSheet.Application.Tests.Common;

namespace SauronSheet.Application.Tests.Features.Subcategories.Commands;

public class CreateSubcategoryCommandHandlerTests
{
    private readonly Mock<ISubcategoryRepository> _mockSubcategoryRepo;
    private readonly Mock<ICategoryRepository> _mockCategoryRepo;
    private readonly Mock<IUserContext> _mockUserContext;

    public CreateSubcategoryCommandHandlerTests()
    {
        _mockSubcategoryRepo = new Mock<ISubcategoryRepository>();
        _mockCategoryRepo = new Mock<ICategoryRepository>();
        _mockUserContext = new Mock<IUserContext>();

        _mockUserContext.Setup(x => x.UserId).Returns("test-user-id");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task CreateSubcategory_ValidInput_ReturnsSubcategoryId()
    {
        // Arrange
        var handler = new CreateSubcategoryCommandHandler(
            _mockSubcategoryRepo.Object,
            _mockCategoryRepo.Object,
            _mockUserContext.Object);

        var categoryId = Guid.NewGuid();
        var userId = new UserId("test-user-id");
        var category = TestCategoryFactory.CreateUserCategory(
            categoryId: new CategoryId(categoryId),
            userId: userId,
            name: "Food");

        _mockCategoryRepo.Setup(x => x.GetByIdAsync(new CategoryId(categoryId)))
            .ReturnsAsync(category);

        _mockSubcategoryRepo.Setup(x => x.FindByNameAsync(
                userId,
                new CategoryId(categoryId),
                "Ropa"))
            .ReturnsAsync((Subcategory?)null);

        var command = new CreateSubcategoryCommand(
            CategoryId: categoryId,
            Name: "Ropa");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotEqual(Guid.Empty, result);
        _mockSubcategoryRepo.Verify(x => x.AddAsync(
            It.Is<Subcategory>(s =>
                s.Name.Value == "Ropa" &&
                s.CategoryId.Value == categoryId &&
                !s.IsAutoCreated),
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task CreateSubcategory_DuplicateName_ThrowsDomainException()
    {
        // Arrange
        var handler = new CreateSubcategoryCommandHandler(
            _mockSubcategoryRepo.Object,
            _mockCategoryRepo.Object,
            _mockUserContext.Object);

        var categoryId = Guid.NewGuid();
        var userId = new UserId("test-user-id");
        var category = TestCategoryFactory.CreateUserCategory(
            categoryId: new CategoryId(categoryId),
            userId: userId,
            name: "Food");

        _mockCategoryRepo.Setup(x => x.GetByIdAsync(new CategoryId(categoryId)))
            .ReturnsAsync(category);

        var existing = TestSubcategoryFactory.CreateUserSubcategory(
            userId: userId,
            categoryId: new CategoryId(categoryId),
            name: "Ropa");

        _mockSubcategoryRepo.Setup(x => x.FindByNameAsync(
                userId,
                new CategoryId(categoryId),
                "Ropa"))
            .ReturnsAsync(existing);

        var command = new CreateSubcategoryCommand(
            CategoryId: categoryId,
            Name: "Ropa");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(command, CancellationToken.None));

        Assert.Contains("already exists", ex.Message);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task CreateSubcategory_ParentCategoryNotFound_ThrowsEntityNotFoundException()
    {
        // Arrange
        var handler = new CreateSubcategoryCommandHandler(
            _mockSubcategoryRepo.Object,
            _mockCategoryRepo.Object,
            _mockUserContext.Object);

        var categoryId = Guid.NewGuid();

        _mockCategoryRepo.Setup(x => x.GetByIdAsync(new CategoryId(categoryId)))
            .ReturnsAsync((Category?)null);

        var command = new CreateSubcategoryCommand(
            CategoryId: categoryId,
            Name: "Ropa");

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task CreateSubcategory_CategoryBelongsToDifferentUser_ThrowsEntityNotFoundException()
    {
        // Arrange
        var handler = new CreateSubcategoryCommandHandler(
            _mockSubcategoryRepo.Object,
            _mockCategoryRepo.Object,
            _mockUserContext.Object);

        var categoryId = Guid.NewGuid();
        var otherUserId = new UserId("other-user");
        var category = TestCategoryFactory.CreateUserCategory(
            categoryId: new CategoryId(categoryId),
            userId: otherUserId,
            name: "Food");

        _mockCategoryRepo.Setup(x => x.GetByIdAsync(new CategoryId(categoryId)))
            .ReturnsAsync(category);

        var command = new CreateSubcategoryCommand(
            CategoryId: categoryId,
            Name: "Ropa");

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            handler.Handle(command, CancellationToken.None));
    }
}
