namespace Application.Categories.Queries;

using MediatR;

/// <summary>
/// Handler for GetCategoriesQuery
/// </summary>
public class GetCategoriesQueryHandler : IRequestHandler<GetCategoriesQuery, GetCategoriesQueryDto>
{
    private readonly IUserContext _userContext;

    public GetCategoriesQueryHandler(IUserContext userContext)
    {
        _userContext = userContext;
    }

    public async Task<GetCategoriesQueryDto> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        // Phase 0: Return empty list (no persistence yet)
        var categories = new List<CategoryDto>();

        var result = new GetCategoriesQueryDto
        {
            Categories = categories,
            TenantId = request.UserId
        };

        return await Task.FromResult(result);
    }
}

/// <summary>
/// Response DTO for GetCategoriesQuery - implements ITenantScoped for multi-tenancy validation
/// </summary>
public class GetCategoriesQueryDto : ITenantScoped
{
    public IEnumerable<CategoryDto> Categories { get; set; } = new List<CategoryDto>();
    public Guid TenantId { get; set; }
}
