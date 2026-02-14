namespace Application.Categories.Commands;

using Domain;
using MediatR;

/// <summary>
/// Handler for CreateCategoryCommand
/// </summary>
public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, CategoryDto>
{
    private readonly IUserContext _userContext;

    public CreateCategoryCommandHandler(IUserContext userContext)
    {
        _userContext = userContext;
    }

    public async Task<CategoryDto> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ValueObjectValidationException("Category name cannot be empty");

        // Create DTO (Phase 0: no persistence yet)
        var dto = new CategoryDto
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            TenantId = request.UserId,
            CreatedAt = DateTime.UtcNow
        };

        return await Task.FromResult(dto);
    }
}
