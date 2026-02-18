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

        // Check if system defaults already exist (idempotent)
        var existingDefaults = await _categoryRepo.GetSystemDefaultsAsync(userId);
        if (existingDefaults.Count == 4)
        {
            // Already seeded, return existing IDs
            return existingDefaults.Select(c => c.Id.Value).ToList();
        }

        // Get system defaults from domain service
        var systemDefaults = _categoryService.GetSystemDefaults(userId);

        var createdIds = new List<Guid>();
        foreach (var category in systemDefaults)
        {
            await _categoryRepo.AddAsync(category);
            createdIds.Add(category.Id.Value);
        }

        return createdIds;
    }
}
