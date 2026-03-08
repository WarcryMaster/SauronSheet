namespace SauronSheet.Application.Features.Categories.Commands;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Domain.Repositories;
using Domain.Services;
using Domain.ValueObjects;
using MediatR;

public class SeedSystemDefaultsCommandHandler
    : IRequestHandler<SeedSystemDefaultsCommand, List<Guid>>
{
    private readonly ICategoryRepository _categoryRepo;
    private readonly CategoryService _categoryService;
    private readonly IUserContext _userContext;

    public SeedSystemDefaultsCommandHandler(
        ICategoryRepository categoryRepo,
        CategoryService categoryService,
        IUserContext userContext)
    {
        _categoryRepo = categoryRepo;
        _categoryService = categoryService;
        _userContext = userContext;
    }

    public async Task<List<Guid>> Handle(
        SeedSystemDefaultsCommand request,
        CancellationToken cancellationToken)
    {
        var userId = new UserId(_userContext.UserId);

        // Feature 3: Check if system defaults already exist (idempotent)
        // Note: This handler may be deprecated after Feature 3 (Task 6.5)
        var existingDefaults = await _categoryRepo.GetSystemDefaultsAsync();
        if (existingDefaults.Count == 24)
        {
            // Already seeded, return existing IDs
            return existingDefaults.Select(c => c.Id.Value).ToList();
        }

        // Get system defaults from domain service (no userId param now)
        var systemDefaults = _categoryService.GetSystemDefaults();

        var createdIds = new List<Guid>();
        foreach (var category in systemDefaults)
        {
            await _categoryRepo.AddAsync(category);
            createdIds.Add(category.Id.Value);
        }

        return createdIds;
    }
}
