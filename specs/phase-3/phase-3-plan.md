# Phase 3 Implementation Plan

**Version**: 1.0.0  
**Created**: 2026-02-15  
**Aligned with**: Constitution v1.1.0, Phase 3 Spec v1.0.0, Full Spec v1.0.0  
**Duration**: Weeks 9–13  
**Goal**: PDF import pipeline, transaction CRUD, category management, Supabase persistence

---

## Table of Contents

- [Executive Summary](#executive-summary)
- [Implementation Phases](#implementation-phases)
- [Task Breakdown by Component](#task-breakdown-by-component)
- [Dependency Graph](#dependency-graph)
- [Red-Green-Refactor Workflow](#red-green-refactor-workflow)
- [Validation Checkpoints](#validation-checkpoints)
- [Risk Mitigation](#risk-mitigation)

---

## Executive Summary

Phase 3 builds the first full-stack feature set on top of the foundation (Phase 0), authentication (Phase 1), and domain model (Phase 2). This phase implements the **Full-Stack (Features)** architecture with PDF import, transaction CRUD, category management, and complete database persistence.

**Key Deliverables:**
- ✅ `ImportBatch` entity (corrected from VO to Entity)
- ✅ 13 Application commands/queries with handlers
- ✅ 5 Application DTOs (TransactionDto, ImportResultDto, CategoryDto, etc.)
- ✅ 3 Supabase repository implementations
- ✅ PDF parser with PdfPig integration
- ✅ 4 database migrations (users, categories, transactions, pdf_imports)
- ✅ 4 Frontend Razor Pages (Upload, List, Add, Categories)
- ✅ 38 passing tests (33 Application + 5 Domain)
- ✅ Updated `_Layout.cshtml` with navigation
- ✅ Alpine.js integration for interactivity
- ✅ IPdfImportRepository interface in Domain layer

**Key Constraint**: All layers are in scope (Full-Stack Features phase). Domain changes are minimal (ImportBatch entity only).

**Constitutional Compliance:**
- ✅ Clean Architecture: Infrastructure implements Domain interfaces
- ✅ CQRS: 8 Commands + 5 Queries routed through MediatR pipeline
- ✅ DDD: ImportBatch entity, repository interfaces, CategoryService usage
- ✅ Test-First: 38 tests (5 Domain 100%, 33 Application ≥70%)
- ✅ Spec-Driven: Single phase spec, layer boundaries respected (all layers in scope)

---

## Implementation Phases

### Phase 3A: Domain Layer Additions (Days 1-2)
Add `ImportBatch` entity (corrected from VO to Entity) with tests.

### Phase 3B: Application Layer — DTOs & Interfaces (Days 2-3)
Define DTOs, IPdfParser interface, RawTransactionRow model.

### Phase 3C: Application Layer — Transaction Commands (Days 3-5)
Implement Import, Create, Update, Delete transaction handlers (10 tests).

### Phase 3D: Application Layer — Category Commands (Days 5-7)
Implement Create, Rename, Delete, GetCategories, SeedDefaults handlers (11 tests).

### Phase 3E: Infrastructure — Database Migrations (Days 7-8)
Apply 3 migrations (categories, transactions, pdf_imports) with RLS policies.

### Phase 3F: Infrastructure — Repositories (Days 8-10)
Implement SupabaseTransactionRepository, SupabaseCategoryRepository, SupabasePdfImportRepository.

### Phase 3G: Infrastructure — PDF Parser (Days 10-11)
Implement GenericBankPdfParser with PdfPig + PdfParserFactory.

### Phase 3H: Frontend — Pages (Days 11-13)
Build Upload, List, Add, Categories pages with Alpine.js interactivity.

### Phase 3I: Integration & Validation (Days 13-15)
E2E testing, coverage reporting, all 170 tests passing (121 prior + 49 new).

---

## Task Breakdown by Component

### 0. PRE-IMPLEMENTATION

#### 0.1: Environment Validation

**Task**: Verify Phase 0, 1, 2 completion and Phase 3 readiness

```sh
✓ Phase 0 build passing         # dotnet build
✓ Phase 0 tests passing         # 13 tests green
✓ Phase 1 build passing         # dotnet build
✓ Phase 1 tests passing         # 22 tests green
✓ Phase 2 build passing         # dotnet build
✓ Phase 2 tests passing         # 81 tests green (domain-only)
✓ Domain project zero deps      # Verify Domain.csproj has NO external packages
✓ Supabase Auth working         # Phase 1 auth functional
✓ Git workspace clean           # Phase 2 merged to main
```

**Acceptance Criteria:**
- Phase 0 + Phase 1 + Phase 2 combined tests pass (116 tests total)
- Domain layer has ZERO external NuGet dependencies
- All Phase 2 entities, value objects, and services are stable
- Supabase Auth is configured and working
- Git workspace is clean (ready for Phase 3 development)

---

### 1. DOMAIN LAYER EXTENSIONS

#### 1.1: Write Domain.Tests for ImportBatch Entity (RED Phase)

**Task**: Create test stubs for `ImportBatch` entity (5 tests)

**Directory structure** (create if not exists):
```sh
mkdir -p tests/SauronSheet.Domain.Tests/Entities
```

**File**: `tests/SauronSheet.Domain.Tests/Entities/ImportBatchTests.cs`

```csharp
using Xunit;
using SauronSheet.Domain.Entities;
using SauronSheet.Domain.Exceptions;

namespace SauronSheet.Domain.Tests.Entities;

public class ImportBatchTests
{
    [Fact]
    [Trait("Category", "Domain")]
    public void ImportBatch_ValidConstruction_SetsProperties()
    {
        // RED: Will fail until ImportBatch entity implemented
        Assert.True(false, "Implement ImportBatch entity");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void ImportBatch_EmptyFilename_ThrowsDomainException()
    {
        Assert.True(false, "Implement filename validation");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void ImportBatch_NegativeImportedCount_ThrowsDomainException()
    {
        Assert.True(false, "Implement imported count validation");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void ImportBatch_NegativeSkippedCount_ThrowsDomainException()
    {
        Assert.True(false, "Implement skipped count validation");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void ImportBatch_ToString_FormatsCorrectly()
    {
        Assert.True(false, "Implement ToString formatting");
    }
}
```

**Verification**:

```sh
dotnet test --filter Category=Domain --no-build
# Expected: 5 new Domain tests FAIL (red) — ImportBatch not yet implemented
# Expected: 116 prior tests (19 Phase 0+1 + 81 Phase 2 + 16 Phase 2 extras) still PASS
# Total discovered: 121 tests (116 passing, 5 failing)
```

---

#### 1.2: Implement ImportBatch Entity (GREEN Phase)

**Task**: Create `ImportBatch` entity with validation

**File**: `src/SauronSheet.Domain/Entities/ImportBatch.cs`

```csharp
namespace SauronSheet.Domain.Entities;

using Common;
using Exceptions;

public class ImportBatch : Entity<Guid>
{
    public string Filename { get; private set; }
    public int ImportedCount { get; private set; }
    public int SkippedCount { get; private set; }
    public DateTime ImportedAt { get; private set; }

    public ImportBatch(
        Guid id,
        string filename,
        int importedCount,
        int skippedCount,
        DateTime importedAt)
        : base(id)  // Call Entity<Guid> constructor
    {
        if (string.IsNullOrWhiteSpace(filename))
            throw new DomainException("Filename is required.");
        if (importedCount < 0)
            throw new DomainException("Imported count cannot be negative.");
        if (skippedCount < 0)
            throw new DomainException("Skipped count cannot be negative.");

        Filename = filename;
        ImportedCount = importedCount;
        SkippedCount = skippedCount;
        ImportedAt = importedAt;
    }

    public int TotalProcessed => ImportedCount + SkippedCount;

    public override string ToString()
        => $"{Filename}: {ImportedCount} imported, {SkippedCount} skipped at {ImportedAt:yyyy-MM-dd HH:mm}";
}
```

**Verification**:

```sh
dotnet test --filter "ClassName=SauronSheet.Domain.Tests.Entities.ImportBatchTests" --no-build
# Expected: 5 ImportBatch tests PASS
```

---

#### Checkpoint 1: Domain Layer Extensions Complete ✓

```sh
dotnet test --filter Category=Domain --no-build
# Expected: 121 tests PASS (116 Phase 0+1+2 + 5 Phase 3)
```

**Status**: All domain tests passing → Proceed to Phase 3B (Application DTOs)

**CRITICAL FIX C-2**: Add IPdfImportRepository to Domain layer

**File**: `src/SauronSheet.Domain/Repositories/IPdfImportRepository.cs`

```csharp
namespace SauronSheet.Domain.Repositories;

using Entities;

/// <summary>
/// Repository for tracking PDF import metadata.
/// CRITICAL FIX C-2: Moved from Infrastructure to Domain to comply with Clean Architecture.
/// Application layer can now depend on this interface without violating architecture rules.
/// </summary>
public interface IPdfImportRepository
{
    Task AddAsync(ImportBatch importBatch);
    Task<IReadOnlyList<ImportBatch>> GetByUserIdAsync(UserId userId);
}
```

**CRITICAL FIX I-4**: Add GetCountsByCategoriesAsync to ITransactionRepository

**File**: `src/SauronSheet.Domain/Repositories/ITransactionRepository.cs` (update existing)

Add this method to the interface:

```csharp
/// <summary>
/// Gets transaction counts grouped by category.
/// CRITICAL FIX I-4: Added to support CategoryDto.TransactionCount calculation.
/// </summary>
/// <param name="categoryIds">List of category IDs to count transactions for</param>
/// <returns>Dictionary mapping CategoryId to transaction count</returns>
Task<Dictionary<CategoryId, int>> GetCountsByCategoriesAsync(List<CategoryId> categoryIds);
```

---

### 2. APPLICATION LAYER — DTOS & INTERFACES

#### 2.1: Create Application DTOs (GREEN Phase)

**Task**: Define data transfer objects for Phase 3 features

**Directory structure**:
```sh
mkdir -p src/SauronSheet.Application/Features/Transactions/DTOs
mkdir -p src/SauronSheet.Application/Features/Categories/DTOs
```

**File**: `src/SauronSheet.Application/Features/Transactions/DTOs/TransactionDto.cs`

```csharp
namespace SauronSheet.Application.Features.Transactions.DTOs;

public record TransactionDto(
    Guid Id,
    decimal Amount,
    string Currency,
    DateTime Date,
    string Description,
    Guid? CategoryId,
    string? CategoryName,
    string? ImportedFrom,
    DateTime CreatedAt);
```

**File**: `src/SauronSheet.Application/Features/Transactions/DTOs/ImportResultDto.cs`

```csharp
namespace SauronSheet.Application.Features.Transactions.DTOs;

public record ImportResultDto(
    int ImportedCount,
    int SkippedCount,
    int TotalProcessed,
    string Filename,
    DateTime ImportedAt,
    List<ImportRowErrorDto> Errors);
```

**File**: `src/SauronSheet.Application/Features/Transactions/DTOs/ImportRowErrorDto.cs`

```csharp
namespace SauronSheet.Application.Features.Transactions.DTOs;

public record ImportRowErrorDto(
    int RowNumber,
    string RawData,
    string Reason);
```

**File**: `src/SauronSheet.Application/Features/Transactions/DTOs/PaginatedResultDto.cs`

```csharp
namespace SauronSheet.Application.Features.Transactions.DTOs;

public record PaginatedResultDto<T>(
    List<T> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);
```

**File**: `src/SauronSheet.Application/Features/Categories/DTOs/CategoryDto.cs`

```csharp
namespace SauronSheet.Application.Features.Categories.DTOs;

public record CategoryDto(
    Guid Id,
    string Name,
    string? Color,
    string? Icon,
    bool IsSystemDefault,
    int TransactionCount);
```

---

#### 2.2: Create IPdfParser Interface & RawTransactionRow (GREEN Phase)

**Task**: Define PDF parser contract

**File**: `src/SauronSheet.Application/Interfaces/IPdfParser.cs`

```csharp
namespace SauronSheet.Application.Interfaces;

using Common.Models;

public interface IPdfParser
{
    Task<List<RawTransactionRow>> ParseAsync(Stream pdfStream);
}
```

**File**: `src/SauronSheet.Application/Common/Models/RawTransactionRow.cs`

```csharp
namespace SauronSheet.Application.Common.Models;

public record RawTransactionRow(
    int RowNumber,
    string? DateRaw,
    string? DescriptionRaw,
    string? AmountRaw,
    string? CurrencyRaw);
```

**Verification**:

```sh
dotnet build
# Expected: Build succeeds (DTOs and interfaces compile)
```

---

### 3. APPLICATION LAYER — TRANSACTION COMMANDS

#### 3.1: Write Application.Tests for Transaction Commands (RED Phase)

**Task**: Create test stubs for transaction command handlers (16 tests)

**File**: `tests/SauronSheet.Application.Tests/Features/Transactions/Commands/ImportTransactionsFromPdfCommandTests.cs`

```csharp
using Xunit;
using Moq;
using SauronSheet.Application.Features.Transactions.Commands;
using SauronSheet.Application.Interfaces;
using SauronSheet.Application.Common;
using SauronSheet.Domain.Repositories;
using SauronSheet.Domain.Services;

namespace SauronSheet.Application.Tests.Features.Transactions.Commands;

public class ImportTransactionsFromPdfCommandTests
{
    [Fact]
    [Trait("Category", "Application")]
    public async Task ImportPdf_ValidPdf_ImportsTransactions()
    {
        // RED: Will fail until ImportTransactionsFromPdfCommandHandler implemented
        Assert.True(false, "Implement ImportTransactionsFromPdfCommandHandler");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task ImportPdf_DuplicateTransactions_SkipsDuplicates()
    {
        Assert.True(false, "Implement duplicate detection");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task ImportPdf_InvalidRows_ReportsErrors()
    {
        Assert.True(false, "Implement error reporting");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task ImportPdf_EmptyPdf_ReturnsZeroCounts()
    {
        Assert.True(false, "Implement empty PDF handling");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task ImportPdf_NullStream_ThrowsException()
    {
        Assert.True(false, "Implement null stream validation");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task ImportPdf_SeedsDefaultCategories()
    {
        Assert.True(false, "Implement system default seeding");
    }
}
```

**Similar test files to create:**
- `CreateTransactionCommandTests.cs` (4 tests: valid input, future date, with category, invalid category)
- `UpdateTransactionCategoryCommandTests.cs` (2 tests: valid update, wrong user)
- `UpdateTransactionDescriptionCommandTests.cs` (1 test: valid update)
- `DeleteTransactionCommandTests.cs` (3 tests: valid delete, wrong user, non-existent)

**File**: `tests/SauronSheet.Application.Tests/Features/Transactions/Queries/GetTransactionsQueryTests.cs`

```csharp
using Xunit;
using SauronSheet.Application.Features.Transactions.Queries;

namespace SauronSheet.Application.Tests.Features.Transactions.Queries;

public class GetTransactionsQueryTests
{
    [Fact]
    [Trait("Category", "Application")]
    public async Task GetTransactions_ReturnsOnlyUserTransactions()
    {
        Assert.True(false, "Implement GetTransactionsQueryHandler");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task GetTransactions_Paginated_RespectsPageSize()
    {
        Assert.True(false, "Implement pagination");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task GetTransactions_SortedByDateDescending()
    {
        Assert.True(false, "Implement sorting");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task GetTransactions_EmptyResult_ReturnsEmptyList()
    {
        Assert.True(false, "Implement empty result handling");
    }
}
```

**File**: `tests/SauronSheet.Application.Tests/Features/Transactions/Queries/GetTransactionByIdQueryTests.cs`

```csharp
using Xunit;
using SauronSheet.Application.Features.Transactions.Queries;

namespace SauronSheet.Application.Tests.Features.Transactions.Queries;

public class GetTransactionByIdQueryTests
{
    [Fact]
    [Trait("Category", "Application")]
    public async Task GetTransactionById_Exists_ReturnsDto()
    {
        Assert.True(false, "Implement GetTransactionByIdQueryHandler");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task GetTransactionById_WrongUser_ThrowsException()
    {
        Assert.True(false, "Implement tenant isolation");
    }
}
```

**Verification**:

```sh
dotnet test --filter Category=Application --no-build
# Expected: 16 new Application tests FAIL (red) — transaction handlers not yet implemented
# Expected: 16 Phase 1 tests still PASS
# Total discovered: 32 Application tests (16 passing, 16 failing)
```

---

#### 3.2: Implement Transaction Commands & Handlers (GREEN Phase)

**Task**: Create transaction command handlers

**File**: `src/SauronSheet.Application/Features/Transactions/Commands/ImportTransactionsFromPdfCommand.cs`

```csharp
namespace SauronSheet.Application.Features.Transactions.Commands;

using DTOs;
using MediatR;

public record ImportTransactionsFromPdfCommand(
    Stream PdfStream,
    string Filename) : IRequest<ImportResultDto>;
```

**File**: `src/SauronSheet.Application/Features/Transactions/Commands/ImportTransactionsFromPdfCommandHandler.cs`

```csharp
namespace SauronSheet.Application.Features.Transactions.Commands;

using System.Collections.Generic;
using System.Linq;
using Common;
using Common.Models;
using Domain.Entities;
using Domain.Repositories;
using Domain.Services;
using Domain.ValueObjects;
using Domain.Exceptions;
using DTOs;
using Interfaces;
using MediatR;
using Categories.Commands;

public class ImportTransactionsFromPdfCommandHandler
    : IRequestHandler<ImportTransactionsFromPdfCommand, ImportResultDto>
{
    private readonly IPdfParser _pdfParser;
    private readonly ITransactionRepository _transactionRepo;
    private readonly ICategoryRepository _categoryRepo;
    private readonly IPdfImportRepository _pdfImportRepo;
    private readonly IUserContext _userContext;
    private readonly IMediator _mediator;

    public ImportTransactionsFromPdfCommandHandler(
        IPdfParser pdfParser,
        ITransactionRepository transactionRepo,
        ICategoryRepository categoryRepo,
        IPdfImportRepository pdfImportRepo,
        IUserContext userContext,
        IMediator mediator)
    {
        _pdfParser = pdfParser;
        _transactionRepo = transactionRepo;
        _categoryRepo = categoryRepo;
        _pdfImportRepo = pdfImportRepo;
        _userContext = userContext;
        _mediator = mediator;
    }

    public async Task<ImportResultDto> Handle(
        ImportTransactionsFromPdfCommand request,
        CancellationToken cancellationToken)
    {
        if (request.PdfStream == null || request.PdfStream.Length == 0)
            throw new ArgumentException("PDF stream is required.", nameof(request.PdfStream));

        if (!request.Filename.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            throw new DomainException("Only PDF files are accepted.");

        var userId = new UserId(_userContext.UserId);

        // Seed system defaults if not present
        var systemDefaults = await _categoryRepo.GetSystemDefaultsAsync(userId);
        if (systemDefaults.Count == 0)
        {
            await _mediator.Send(new SeedSystemDefaultsCommand(), cancellationToken);
        }

        // Parse PDF
        var rawRows = await _pdfParser.ParseAsync(request.PdfStream);

        var importedCount = 0;
        var skippedCount = 0;
        var errors = new List<ImportRowErrorDto>();

        foreach (var row in rawRows)
        {
            try
            {
                // Validate row
                if (string.IsNullOrWhiteSpace(row.DateRaw) ||
                    string.IsNullOrWhiteSpace(row.DescriptionRaw) ||
                    string.IsNullOrWhiteSpace(row.AmountRaw))
                {
                    errors.Add(new ImportRowErrorDto(
                        row.RowNumber,
                        $"{row.DateRaw} | {row.DescriptionRaw} | {row.AmountRaw}",
                        "Missing required fields"));
                    skippedCount++;
                    continue;
                }

                // Parse date
                if (!DateTime.TryParse(row.DateRaw, out var date))
                {
                    errors.Add(new ImportRowErrorDto(
                        row.RowNumber,
                        row.DateRaw,
                        "Invalid date format"));
                    skippedCount++;
                    continue;
                }

                // Parse amount
                if (!decimal.TryParse(row.AmountRaw, out var amount))
                {
                    errors.Add(new ImportRowErrorDto(
                        row.RowNumber,
                        row.AmountRaw,
                        "Invalid amount format"));
                    skippedCount++;
                    continue;
                }

                var currency = row.CurrencyRaw ?? "EUR";
                var description = row.DescriptionRaw;

                // Check duplicate
                var isDuplicate = await _transactionRepo.ExistsDuplicateAsync(
                    userId, date, amount, description);

                if (isDuplicate)
                {
                    errors.Add(new ImportRowErrorDto(
                        row.RowNumber,
                        $"{date:yyyy-MM-dd} | {description} | {amount}",
                        "Duplicate"));
                    skippedCount++;
                    continue;
                }

                // Create transaction
                var transaction = new Transaction(
                    new TransactionId(Guid.NewGuid()),
                    userId,
                    new Money(amount, currency),
                    date,
                    description,
                    categoryId: null,
                    importedFrom: request.Filename);

                await _transactionRepo.AddAsync(transaction);
                importedCount++;
            }
            catch (DomainException ex)
            {
                errors.Add(new ImportRowErrorDto(
                    row.RowNumber,
                    $"{row.DateRaw} | {row.DescriptionRaw} | {row.AmountRaw}",
                    ex.Message));
                skippedCount++;
            }
        }

        // CRITICAL FIX C-2: Save import metadata using IPdfImportRepository
        var importBatch = new ImportBatch(
            Guid.NewGuid(),
            request.Filename,
            importedCount,
            skippedCount,
            DateTime.UtcNow);

        await _pdfImportRepo.AddAsync(importBatch);

        return new ImportResultDto(
            importedCount,
            skippedCount,
            importedCount + skippedCount,
            request.Filename,
            DateTime.UtcNow,
            errors);
    }
}
```

**File**: `src/SauronSheet.Application/Features/Transactions/Commands/CreateTransactionCommand.cs`

```csharp
namespace SauronSheet.Application.Features.Transactions.Commands;

using MediatR;

public record CreateTransactionCommand(
    decimal Amount,
    string Currency,
    DateTime Date,
    string Description,
    Guid? CategoryId) : IRequest<Guid>;
```

**File**: `src/SauronSheet.Application/Features/Transactions/Commands/CreateTransactionCommandHandler.cs`

```csharp
namespace SauronSheet.Application.Features.Transactions.Commands;

using Common;
using Domain.Entities;
using Domain.Repositories;
using Domain.ValueObjects;
using Domain.Exceptions;
using MediatR;

public class CreateTransactionCommandHandler
    : IRequestHandler<CreateTransactionCommand, Guid>
{
    private readonly ITransactionRepository _transactionRepo;
    private readonly ICategoryRepository _categoryRepo;
    private readonly IUserContext _userContext;

    public CreateTransactionCommandHandler(
        ITransactionRepository transactionRepo,
        ICategoryRepository categoryRepo,
        IUserContext userContext)
    {
        _transactionRepo = transactionRepo;
        _categoryRepo = categoryRepo;
        _userContext = userContext;
    }

    public async Task<Guid> Handle(
        CreateTransactionCommand request,
        CancellationToken cancellationToken)
    {
        var userId = new UserId(_userContext.UserId);
        var transactionId = new TransactionId(Guid.NewGuid());
        var money = new Money(request.Amount, request.Currency);

        CategoryId? categoryId = null;
        if (request.CategoryId.HasValue)
        {
            var category = await _categoryRepo.GetByIdAsync(new CategoryId(request.CategoryId.Value));
            if (category == null)
                throw new EntityNotFoundException("Category", request.CategoryId.Value);

            if (category.UserId != userId)
                throw new EntityNotFoundException("Category", request.CategoryId.Value); // Tenant isolation

            categoryId = new CategoryId(request.CategoryId.Value);
        }

        var transaction = new Transaction(
            transactionId,
            userId,
            money,
            request.Date,
            request.Description,
            categoryId);

        await _transactionRepo.AddAsync(transaction);
        return transactionId.Value;
    }
}
```

**Similar handlers to create:**
- `UpdateTransactionCategoryCommandHandler.cs`
- `UpdateTransactionDescriptionCommandHandler.cs`
- `DeleteTransactionCommandHandler.cs`
- `GetTransactionsQueryHandler.cs`
- `GetTransactionByIdQueryHandler.cs`

**Verification**:

```sh
dotnet test --filter Category=Application --no-build
# Expected: All 32 Application tests PASS (16 Phase 1 + 16 Phase 3 transaction tests)
```

---

### 4. APPLICATION LAYER — CATEGORY COMMANDS

#### 4.1: Write Application.Tests for Category Commands (RED Phase)

**Task**: Create test stubs for category command handlers (11 tests)

**File**: `tests/SauronSheet.Application.Tests/Features/Categories/Commands/CreateCategoryCommandTests.cs`

```csharp
using Xunit;
using SauronSheet.Application.Features.Categories.Commands;

namespace SauronSheet.Application.Tests.Features.Categories.Commands;

public class CreateCategoryCommandTests
{
    [Fact]
    [Trait("Category", "Application")]
    public async Task CreateCategory_ValidInput_ReturnsCategoryId()
    {
        Assert.True(false, "Implement CreateCategoryCommandHandler");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task CreateCategory_DuplicateName_ThrowsDomainException()
    {
        Assert.True(false, "Implement duplicate name validation");
    }
}
```

**Similar test files to create:**
- `RenameCategoryCommandTests.cs` (2 tests: valid rename, system default throws)
- `DeleteCategoryCommandTests.cs` (3 tests: no transactions, with transactions throws, system default throws)
- `GetCategoriesQueryTests.cs` (2 tests: includes system defaults, sorts system defaults first)
- `SeedSystemDefaultsCommandTests.cs` (2 tests: first time creates four, already exist returns existing)

**Verification**:

```sh
dotnet test --filter Category=Application --no-build
# Expected: 11 new Application tests FAIL (red) — category handlers not yet implemented
# Expected: 32 prior tests still PASS
# Total discovered: 43 Application tests (32 passing, 11 failing)
```

---

#### 4.2: Implement Category Commands & Handlers (GREEN Phase)

**Task**: Create category command handlers

**File**: `src/SauronSheet.Application/Features/Categories/Commands/CreateCategoryCommand.cs`

```csharp
namespace SauronSheet.Application.Features.Categories.Commands;

using MediatR;

public record CreateCategoryCommand(
    string Name,
    string? Color = null,
    string? Icon = null) : IRequest<Guid>;
```

**File**: `src/SauronSheet.Application/Features/Categories/Commands/CreateCategoryCommandHandler.cs`

```csharp
namespace SauronSheet.Application.Features.Categories.Commands;

using Common;
using Domain.Entities;
using Domain.Repositories;
using Domain.Services;
using Domain.ValueObjects;
using MediatR;

public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, Guid>
{
    private readonly ICategoryRepository _categoryRepo;
    private readonly CategoryService _categoryService;
    private readonly IUserContext _userContext;

    public CreateCategoryCommandHandler(
        ICategoryRepository categoryRepo,
        CategoryService categoryService,
        IUserContext userContext)
    {
        _categoryRepo = categoryRepo;
        _categoryService = categoryService;
        _userContext = userContext;
    }

    public async Task<Guid> Handle(
        CreateCategoryCommand request,
        CancellationToken cancellationToken)
    {
        var userId = new UserId(_userContext.UserId);

        // Validate unique name
        await _categoryService.ValidateUniqueName(userId, request.Name);

        var categoryId = new CategoryId(Guid.NewGuid());
        var category = new Category(categoryId, userId, request.Name, request.Color, request.Icon);

        await _categoryRepo.AddAsync(category);
        return categoryId.Value;
    }
}
```

**File**: `src/SauronSheet.Application/Features/Categories/Queries/GetCategoriesQueryHandler.cs`

**CRITICAL FIX I-4**: Implementar cálculo de TransactionCount

```csharp
namespace SauronSheet.Application.Features.Categories.Queries;

using Common;
using Domain.Repositories;
using Domain.ValueObjects;
using DTOs;
using MediatR;
using Categories.Commands;

public class GetCategoriesQueryHandler : IRequestHandler<GetCategoriesQuery, List<CategoryDto>>
{
    private readonly ICategoryRepository _categoryRepo;
    private readonly ITransactionRepository _transactionRepo;
    private readonly IUserContext _userContext;
    private readonly IMediator _mediator;

    public GetCategoriesQueryHandler(
        ICategoryRepository categoryRepo,
        ITransactionRepository transactionRepo,
        IUserContext userContext,
        IMediator mediator)
    {
        _categoryRepo = categoryRepo;
        _transactionRepo = transactionRepo;
        _userContext = userContext;
        _mediator = mediator;
    }

    public async Task<List<CategoryDto>> Handle(
        GetCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        var userId = new UserId(_userContext.UserId);

        // Seed system defaults if not present (idempotent)
        var systemDefaults = await _categoryRepo.GetSystemDefaultsAsync(userId);
        if (systemDefaults.Count == 0)
        {
            await _mediator.Send(new SeedSystemDefaultsCommand(), cancellationToken);
        }

        var categories = await _categoryRepo.GetByUserIdAsync(userId);

        // CRITICAL FIX I-4: Calculate TransactionCount per category
        var categoryIds = categories.Select(c => c.Id).ToList();
        var transactionCounts = await _transactionRepo.GetCountsByCategoriesAsync(categoryIds);

        var result = categories.Select(c => new CategoryDto(
            c.Id.Value,
            c.Name,
            c.Color,
            c.Icon,
            c.IsSystemDefault,
            transactionCounts.TryGetValue(c.Id, out var count) ? count : 0
        )).ToList();

        // Sort: system defaults first, then user-defined alphabetically
        return result
            .OrderByDescending(c => c.IsSystemDefault)
            .ThenBy(c => c.Name)
            .ToList();
    }
}
```

**Similar handlers to create:**
- `RenameCategoryCommandHandler.cs`
- `DeleteCategoryCommandHandler.cs`
- `SeedSystemDefaultsCommandHandler.cs`

**Verification**:

```sh
dotnet test --filter Category=Application --no-build
# Expected: All 43 Application tests PASS (32 transaction + 11 category tests)
```

---

#### Checkpoint 2: Application Layer Complete ✓

```sh
dotnet test --filter Category=Application --no-build
# Expected: 43 tests PASS (16 Phase 1 + 16 transaction + 11 category)
```

**Status**: All application tests passing → Proceed to Phase 3E (Infrastructure Migrations)

---

### 5. INFRASTRUCTURE LAYER — DATABASE MIGRATIONS

#### 5.1: Create Database Migration Files (GREEN Phase)

**Task**: Create SQL migration scripts for 4 tables (CRITICAL FIX I-3: add missing users table)

**Directory structure**:
```sh
mkdir -p src/SauronSheet.Infrastructure/Persistence/Migrations
```

**File**: `src/SauronSheet.Infrastructure/Persistence/Migrations/001_CreateUsersTable.sql`

**CRITICAL FIX I-3**: Esta migration estaba referenciada pero no existía en Phase 1.

```sql
-- Migration: 001_CreateUsersTable.sql
-- Purpose: User profile table (Supabase Auth manages auth.users)
-- NOTE: This migration should have been in Phase 1, but is added here as prerequisite

CREATE TABLE IF NOT EXISTS public.users (
    id UUID PRIMARY KEY REFERENCES auth.users(id) ON DELETE CASCADE,
    email TEXT NOT NULL UNIQUE,
    display_name TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ
);

-- Indexes
CREATE INDEX IF NOT EXISTS idx_users_email ON public.users(email);

-- Row Level Security
ALTER TABLE public.users ENABLE ROW LEVEL SECURITY;

CREATE POLICY "Users can view own profile"
    ON public.users FOR SELECT
    USING (auth.uid() = id);

CREATE POLICY "Users can update own profile"
    ON public.users FOR UPDATE
    USING (auth.uid() = id)
    WITH CHECK (auth.uid() = id);

COMMENT ON TABLE public.users IS 'User profiles linked to Supabase Auth';
COMMENT ON COLUMN public.users.id IS 'Foreign key to auth.users(id)';
```

**File**: `src/SauronSheet.Infrastructure/Persistence/Migrations/002_CreateCategoriesTable.sql`

```sql
-- Migration: 002_CreateCategoriesTable.sql
-- Purpose: Expense categories (system defaults + user-defined)

CREATE TABLE IF NOT EXISTS public.categories (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL REFERENCES public.users(id) ON DELETE CASCADE,
    name VARCHAR(100) NOT NULL,
    color VARCHAR(7),
    icon VARCHAR(50),
    is_system_default BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ,
    UNIQUE(user_id, name)
);

-- Indexes
CREATE INDEX IF NOT EXISTS idx_categories_user ON public.categories(user_id);
CREATE INDEX IF NOT EXISTS idx_categories_user_system ON public.categories(user_id, is_system_default);

-- Row Level Security
ALTER TABLE public.categories ENABLE ROW LEVEL SECURITY;

CREATE POLICY "Users can view own categories"
    ON public.categories FOR SELECT
    USING (auth.uid() = user_id);

CREATE POLICY "Users can insert own categories"
    ON public.categories FOR INSERT
    WITH CHECK (auth.uid() = user_id);

CREATE POLICY "Users can update own categories"
    ON public.categories FOR UPDATE
    USING (auth.uid() = user_id)
    WITH CHECK (auth.uid() = user_id);

CREATE POLICY "Users can delete own categories"
    ON public.categories FOR DELETE
    USING (auth.uid() = user_id);
```

**File**: `src/SauronSheet.Infrastructure/Persistence/Migrations/003_CreateTransactionsTable.sql`

```sql
-- Migration: 003_CreateTransactionsTable.sql
-- Purpose: Imported and manual transactions

CREATE TABLE IF NOT EXISTS public.transactions (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL REFERENCES public.users(id) ON DELETE CASCADE,
    amount DECIMAL(15,2) NOT NULL,
    currency VARCHAR(3) NOT NULL DEFAULT 'EUR',
    date TIMESTAMPTZ NOT NULL,
    description TEXT NOT NULL,
    category_id UUID REFERENCES public.categories(id) ON DELETE SET NULL,
    imported_from TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ
);

-- Indexes
CREATE INDEX IF NOT EXISTS idx_transactions_user_date ON public.transactions(user_id, date DESC);
CREATE INDEX IF NOT EXISTS idx_transactions_user_category ON public.transactions(user_id, category_id);
CREATE INDEX IF NOT EXISTS idx_transactions_duplicate
    ON public.transactions(user_id, date, amount, description);

-- Row Level Security
ALTER TABLE public.transactions ENABLE ROW LEVEL SECURITY;

CREATE POLICY "Users can view own transactions"
    ON public.transactions FOR SELECT
    USING (auth.uid() = user_id);

CREATE POLICY "Users can insert own transactions"
    ON public.transactions FOR INSERT
    WITH CHECK (auth.uid() = user_id);

CREATE POLICY "Users can update own transactions"
    ON public.transactions FOR UPDATE
    USING (auth.uid() = user_id)
    WITH CHECK (auth.uid() = user_id);

CREATE POLICY "Users can delete own transactions"
    ON public.transactions FOR DELETE
    USING (auth.uid() = user_id);
```

**File**: `src/SauronSheet.Infrastructure/Persistence/Migrations/004_CreatePdfImportsTable.sql`

```sql
-- Migration: 004_CreatePdfImportsTable.sql
-- Purpose: Metadata about imported PDF files

CREATE TABLE IF NOT EXISTS public.pdf_imports (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES public.users(id) ON DELETE CASCADE,
    filename TEXT NOT NULL,
    imported_count INT NOT NULL DEFAULT 0,
    skipped_count INT NOT NULL DEFAULT 0,
    imported_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Indexes
CREATE INDEX IF NOT EXISTS idx_pdf_imports_user ON public.pdf_imports(user_id);

-- Row Level Security
ALTER TABLE public.pdf_imports ENABLE ROW LEVEL SECURITY;

CREATE POLICY "Users can view own imports"
    ON public.pdf_imports FOR SELECT
    USING (auth.uid() = user_id);

CREATE POLICY "Users can insert own imports"
    ON public.pdf_imports FOR INSERT
    WITH CHECK (auth.uid() = user_id);
```

**Verification**:

```sh
# Apply migrations to Supabase manually (via Supabase SQL editor or CLI)
# Expected: 4 tables created with indexes and RLS policies
# CRITICAL FIX NC-1: Verify auth.uid() function is available (Supabase Auth automatically provides it)
```

**CRITICAL FIX NC-1 Verification**:
```sql
-- Test RLS policy in Supabase SQL editor:
-- Set session user (simulates authenticated request)
SELECT set_config('request.jwt.claims', '{"sub":"test-user-uuid"}', true);

-- Verify user can only see own data
SELECT * FROM public.users WHERE id = 'test-user-uuid';
-- Should return only rows where id matches the session user
```

---

### 6. INFRASTRUCTURE LAYER — REPOSITORIES

#### 6.1: Implement Supabase Repositories (GREEN Phase)

**Task**: Create repository implementations

**Directory structure**:
```sh
mkdir -p src/SauronSheet.Infrastructure/Persistence
```

**File**: `src/SauronSheet.Infrastructure/Persistence/SupabaseTransactionRepository.cs`

```csharp
namespace SauronSheet.Infrastructure.Persistence;

using Domain.Entities;
using Domain.Repositories;
using Domain.ValueObjects;
using Domain.Specifications;

public class SupabaseTransactionRepository : ITransactionRepository
{
    private readonly Supabase.Client _client;

    public SupabaseTransactionRepository(Supabase.Client client)
    {
        _client = client;
    }

    public async Task<Transaction?> GetByIdAsync(TransactionId id)
    {
        // TODO Phase 3F: Implement Supabase query
        // Query: SELECT * FROM transactions WHERE id = id.Value LIMIT 1
        // Map response to Transaction entity
        throw new NotImplementedException();
    }

    public async Task<IReadOnlyList<Transaction>> GetByUserIdAsync(UserId userId)
    {
        // TODO Phase 3F: Implement Supabase query
        // Query: SELECT * FROM transactions WHERE user_id = userId.Value ORDER BY date DESC
        throw new NotImplementedException();
    }

    public async Task<IReadOnlyList<Transaction>> FindBySpecificationAsync(
        ISpecification<Transaction> specification)
    {
        // TODO Phase 3F: Implement specification to Postgrest query translation
        // Apply criteria, MaxResults limit, sorting
        throw new NotImplementedException();
    }

    public async Task AddAsync(Transaction transaction)
    {
        // TODO Phase 3F: Implement Supabase insert
        // Map Transaction entity to Supabase row model, insert
        throw new NotImplementedException();
    }

    public async Task UpdateAsync(Transaction transaction)
    {
        // TODO Phase 3F: Implement Supabase update
        // Map Transaction entity to Supabase row model, update WHERE id = transaction.Id
        throw new NotImplementedException();
    }

    public async Task DeleteAsync(TransactionId id)
    {
        // TODO Phase 3F: Implement Supabase delete
        // DELETE FROM transactions WHERE id = id.Value
        throw new NotImplementedException();
    }

    public async Task<bool> ExistsAsync(TransactionId id)
    {
        // TODO Phase 3F: Implement Supabase exists check
        // SELECT COUNT(*) FROM transactions WHERE id = id.Value
        throw new NotImplementedException();
    }

    /// <summary>
    /// Checks if a duplicate transaction exists.
    /// CRITICAL FIX C-3: Duplicate detection ignores currency.
    /// Rationale: Users are unlikely to have same-day, same-amount transactions
    /// in different currencies. If this becomes an issue, currency can be added post-MVP.
    /// </summary>
    public async Task<bool> ExistsDuplicateAsync(
        UserId userId, DateTime date, decimal amount, string description)
    {
        // TODO Phase 3F: Implement duplicate check
        // SELECT COUNT(*) FROM transactions
        // WHERE user_id = userId.Value AND date = date AND amount = amount AND description = description
        // NOTE: Currency is NOT checked. Same amount in EUR and USD would NOT be considered duplicates.
        throw new NotImplementedException();
    }

    /// <summary>
    /// CRITICAL FIX I-4: Get transaction counts grouped by category.
    /// </summary>
    public async Task<Dictionary<CategoryId, int>> GetCountsByCategoriesAsync(List<CategoryId> categoryIds)
    {
        // TODO Phase 3F: Implement batch count query
        // SELECT category_id, COUNT(*) as count
        // FROM transactions
        // WHERE category_id IN (categoryIds)
        // GROUP BY category_id
        throw new NotImplementedException();
    }
}
```

**Similar repositories to create:**
- `SupabaseCategoryRepository.cs`
- `SupabasePdfImportRepository.cs`

**Note**: Full implementation deferred to actual Phase 3F coding. Stubs allow compilation.

---

### 7. INFRASTRUCTURE LAYER — PDF PARSER

#### 7.1: Add PdfPig NuGet Package

**Task**: Add PdfPig to Infrastructure project

```sh
dotnet add src/SauronSheet.Infrastructure/SauronSheet.Infrastructure.csproj package UglyToad.PdfPig --version 0.1.8
```

**Verification**:

```sh
grep "UglyToad.PdfPig" src/SauronSheet.Infrastructure/SauronSheet.Infrastructure.csproj
# Expected: Package reference found with version 0.1.8
```

---

#### 7.2: Implement PDF Parser (GREEN Phase)

**Task**: Create PDF parser with PdfPig

**Directory structure**:
```sh
mkdir -p src/SauronSheet.Infrastructure/PDF/Parsers
```

**File**: `src/SauronSheet.Infrastructure/PDF/PdfParserFactory.cs`

```csharp
namespace SauronSheet.Infrastructure.PDF;

using Application.Interfaces;
using Parsers;

public class PdfParserFactory
{
    public IPdfParser CreateParser(string? bankIdentifier = null)
    {
        // Strategy pattern: return bank-specific parser based on identifier
        // Default: GenericBankPdfParser
        return bankIdentifier switch
        {
            // Future: "bankname" => new BankNamePdfParser(),
            _ => new GenericBankPdfParser()
        };
    }
}
```

**File**: `src/SauronSheet.Infrastructure/PDF/Parsers/GenericBankPdfParser.cs`

```csharp
namespace SauronSheet.Infrastructure.PDF.Parsers;

using SauronSheet.Application.Common.Models;
using SauronSheet.Application.Interfaces;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

public class GenericBankPdfParser : IPdfParser
{
    public async Task<List<RawTransactionRow>> ParseAsync(Stream pdfStream)
    {
        var rows = new List<RawTransactionRow>();

        using (var document = PdfDocument.Open(pdfStream))
        {
            var rowNumber = 0;

            foreach (var page in document.GetPages())
            {
                var text = page.Text;
                var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

                foreach (var line in lines)
                {
                    rowNumber++;

                    // Heuristic parsing: assumes format "DD/MM/YYYY Description AMOUNT EUR"
                    // TODO Phase 3G: Implement robust parsing logic
                    var parts = line.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                    if (parts.Length < 3)
                        continue; // Skip invalid lines

                    var dateRaw = parts[0];
                    var amountRaw = parts[^2]; // Second-to-last part
                    var currencyRaw = parts[^1]; // Last part
                    var descriptionRaw = string.Join(" ", parts[1..^2]); // Middle parts

                    rows.Add(new RawTransactionRow(
                        rowNumber,
                        dateRaw,
                        descriptionRaw,
                        amountRaw,
                        currencyRaw));
                }
            }
        }

        return rows;
    }
}
```

**Note**: This is a simplified parser. Production parsing would use more sophisticated text extraction and pattern matching.

---

### 8. FRONTEND LAYER — PAGES

#### 8.1: Add Alpine.js to Layout (GREEN Phase)

**Task**: Update `_Layout.cshtml` with Alpine.js CDN and navigation

**File**: `src/SauronSheet.Frontend/Pages/Shared/_Layout.cshtml` (update existing)

Add Alpine.js script before `</head>`:

```html
<!-- In _Layout.cshtml <head> section, after Tailwind CDN -->
<script defer src="https://cdn.jsdelivr.net/npm/alpinejs@3/dist/cdn.min.js"></script>
```

Update navigation section with new links:

```html
<nav class="bg-white shadow">
    <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div class="flex justify-between h-16">
            <div class="flex items-center">
                <a href="/" class="flex items-center">
                    <h1 class="text-2xl font-bold text-blue-600">SauronSheet</h1>
                </a>
            </div>
            <div class="flex items-center space-x-4">
                @if (User?.Identity?.IsAuthenticated == true)
                {
                    <a href="/Dashboard" class="text-gray-700 hover:text-blue-600">Dashboard</a>
                    <a href="/Transactions" class="text-gray-700 hover:text-blue-600">💳 Transactions</a>
                    <a href="/Transactions/Upload" class="text-gray-700 hover:text-blue-600">📄 Upload PDF</a>
                    <a href="/Categories" class="text-gray-700 hover:text-blue-600">🏷️ Categories</a>
                    <span class="text-sm text-gray-600">@User.FindFirst("email")?.Value</span>
                    <form method="post" asp-page="/Auth/Logout" style="display: inline;">
                        <button type="submit" class="px-4 py-2 bg-red-600 text-white rounded hover:bg-red-700">
                            Logout
                        </button>
                    </form>
                }
                else
                {
                    <a href="/Auth/Login" class="text-gray-700 hover:text-blue-600">Sign In</a>
                    <a href="/Auth/Register" class="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700">
                        Sign Up
                    </a>
                }
            </div>
        </div>
    </div>
</nav>
```

---

#### 8.2: Create Upload PDF Page (GREEN Phase)

**Task**: Build PDF upload form page

**Directory structure**:
```sh
mkdir -p src/SauronSheet.Frontend/Pages/Transactions
```

**File**: `src/SauronSheet.Frontend/Pages/Transactions/Upload.cshtml`

```html
@page
@model UploadModel
@{
    ViewData["Title"] = "Upload PDF";
}

<div class="max-w-2xl mx-auto">
    <h1 class="text-3xl font-bold text-gray-900 mb-6">Upload Bank Statement</h1>

    @if (!string.IsNullOrEmpty(Model.ErrorMessage))
    {
        <div class="rounded-md bg-red-50 p-4 mb-6">
            <p class="text-sm text-red-700">@Model.ErrorMessage</p>
        </div>
    }

    @if (Model.ImportResult != null)
    {
        <div class="rounded-md bg-green-50 p-4 mb-6">
            <p class="text-lg font-semibold text-green-900">
                ✅ Import Complete: @Model.ImportResult.ImportedCount imported, @Model.ImportResult.SkippedCount skipped
            </p>
            @if (Model.ImportResult.Errors.Any())
            {
                <details class="mt-4">
                    <summary class="cursor-pointer text-sm text-green-700">Show errors (@Model.ImportResult.Errors.Count)</summary>
                    <ul class="mt-2 space-y-1">
                        @foreach (var error in Model.ImportResult.Errors)
                        {
                            <li class="text-sm text-red-600">Row @error.RowNumber: @error.Reason</li>
                        }
                    </ul>
                </details>
            }
            <a href="/Transactions" class="mt-4 inline-block text-blue-600 hover:text-blue-700">
                View imported transactions →
            </a>
        </div>
    }

    <form method="post" enctype="multipart/form-data" class="space-y-6">
        <div>
            <label for="pdfFile" class="block text-sm font-medium text-gray-700">
                Select PDF Bank Statement
            </label>
            <input type="file" id="pdfFile" name="PdfFile" accept=".pdf"
                class="mt-1 block w-full border border-gray-300 rounded-md shadow-sm p-2" />
            <p class="mt-1 text-sm text-gray-500">Max file size: 10MB</p>
        </div>

        <button type="submit"
            class="w-full flex justify-center py-2 px-4 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-blue-600 hover:bg-blue-700">
            Upload and Import
        </button>
    </form>
</div>
```

**File**: `src/SauronSheet.Frontend/Pages/Transactions/Upload.cshtml.cs`

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MediatR;
using SauronSheet.Application.Features.Transactions.Commands;
using SauronSheet.Application.Features.Transactions.DTOs;
using SauronSheet.Domain.Exceptions;

namespace SauronSheet.Frontend.Pages.Transactions;

[Authorize]
public class UploadModel : PageModel
{
    private readonly IMediator _mediator;

    [BindProperty]
    public IFormFile? PdfFile { get; set; }

    public ImportResultDto? ImportResult { get; set; }
    public string? ErrorMessage { get; set; }

    public UploadModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (PdfFile == null || PdfFile.Length == 0)
        {
            ErrorMessage = "Please select a PDF file.";
            return Page();
        }

        if (PdfFile.Length > 10 * 1024 * 1024) // 10MB
        {
            ErrorMessage = "File size exceeds 10MB limit.";
            return Page();
        }

        if (!PdfFile.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            ErrorMessage = "Only PDF files are accepted.";
            return Page();
        }

        // CRITICAL FIX NC-2: Add comprehensive error handling
        try
        {
            using var stream = PdfFile.OpenReadStream();
            ImportResult = await _mediator.Send(
                new ImportTransactionsFromPdfCommand(stream, PdfFile.FileName));
        }
        catch (HttpRequestException ex)
        {
            // Network error (Supabase offline, timeout, etc.)
            ErrorMessage = "Network error. Please check your connection and try again.";
            // TODO: Log exception for diagnostics
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("PDF"))
        {
            // PDF parsing error (from GenericBankPdfParser - NC-3)
            ErrorMessage = $"Could not parse PDF: {ex.Message}";
        }
        catch (DomainException ex)
        {
            // Domain validation error (future date, etc.)
            ErrorMessage = ex.Message;
        }
        catch (Exception ex)
        {
            // Unexpected error
            ErrorMessage = "An unexpected error occurred. Please try again later.";
            // TODO: Log exception for diagnostics
        }

        return Page();
    }
}
```

---

#### 8.3: Create Transaction List Page (GREEN Phase)

**Task**: Build paginated transaction list page

**File**: `src/SauronSheet.Frontend/Pages/Transactions/Index.cshtml`

```html
@page
@model IndexModel
@{
    ViewData["Title"] = "Transactions";
}

<div class="space-y-6">
    <div class="flex justify-between items-center">
        <h1 class="text-3xl font-bold text-gray-900">Transactions</h1>
        <div class="space-x-2">
            <a href="/Transactions/Upload" class="px-4 py-2 bg-green-600 text-white rounded hover:bg-green-700">
                📄 Upload PDF
            </a>
            <a href="/Transactions/Add" class="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700">
                ➕ Add Manual
            </a>
        </div>
    </div>

    @if (Model.Transactions.Items.Count == 0)
    {
        <div class="bg-blue-50 rounded-lg p-6 text-center">
            <p class="text-blue-900">No transactions yet. Import a PDF or add one manually.</p>
        </div>
    }
    else
    {
        <div class="bg-white rounded-lg shadow overflow-hidden">
            <table class="min-w-full divide-y divide-gray-200">
                <thead class="bg-gray-50">
                    <tr>
                        <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Date</th>
                        <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Description</th>
                        <th class="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">Amount</th>
                        <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Category</th>
                        <th class="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">Actions</th>
                    </tr>
                </thead>
                <tbody class="bg-white divide-y divide-gray-200">
                    @foreach (var transaction in Model.Transactions.Items)
                    {
                        <tr>
                            <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-900">@transaction.Date.ToString("yyyy-MM-dd")</td>
                            <td class="px-6 py-4 text-sm text-gray-900">@transaction.Description</td>
                            <td class="px-6 py-4 whitespace-nowrap text-sm text-right font-medium @(transaction.Amount < 0 ? "text-red-600" : "text-green-600")">
                                @transaction.Amount.ToString("F2") @transaction.Currency
                            </td>
                            <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                                @(transaction.CategoryName ?? "Uncategorized")
                            </td>
                            <td class="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                                <button class="text-red-600 hover:text-red-900">Delete</button>
                            </td>
                        </tr>
                    }
                </tbody>
            </table>
        </div>

        <!-- Pagination -->
        <div class="flex justify-between items-center">
            <p class="text-sm text-gray-700">
                Showing @Model.Transactions.Items.Count of @Model.Transactions.TotalCount transactions
            </p>
            <div class="space-x-2">
                @if (Model.Page > 1)
                {
                    <a href="?page=@(Model.Page - 1)" class="px-3 py-1 bg-gray-200 rounded hover:bg-gray-300">Previous</a>
                }
                @if (Model.Page < Model.Transactions.TotalPages)
                {
                    <a href="?page=@(Model.Page + 1)" class="px-3 py-1 bg-gray-200 rounded hover:bg-gray-300">Next</a>
                }
            </div>
        </div>
    }
</div>
```

**File**: `src/SauronSheet.Frontend/Pages/Transactions/Index.cshtml.cs`

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MediatR;
using SauronSheet.Application.Features.Transactions.Queries;
using SauronSheet.Application.Features.Transactions.DTOs;

namespace SauronSheet.Frontend.Pages.Transactions;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IMediator _mediator;

    public PaginatedResultDto<TransactionDto> Transactions { get; set; } = null!;

    [BindProperty(SupportsGet = true)]
    public int Page { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public int PageSize { get; set; } = 50;

    public IndexModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task OnGetAsync()
    {
        Transactions = await _mediator.Send(
            new GetTransactionsQuery(Page, PageSize));
    }
}
```

---

#### 8.4: Create Manual Add Transaction Page (GREEN Phase)

**Task**: Build manual transaction form page

**File**: `src/SauronSheet.Frontend/Pages/Transactions/Add.cshtml`

```html
@page
@model AddModel
@{
    ViewData["Title"] = "Add Transaction";
}

<div class="max-w-2xl mx-auto">
    <h1 class="text-3xl font-bold text-gray-900 mb-6">Add Transaction</h1>

    @if (!string.IsNullOrEmpty(Model.ErrorMessage))
    {
        <div class="rounded-md bg-red-50 p-4 mb-6">
            <p class="text-sm text-red-700">@Model.ErrorMessage</p>
        </div>
    }

    <form method="post" class="space-y-6">
        <div>
            <label for="date" class="block text-sm font-medium text-gray-700">Date</label>
            <input type="date" id="date" name="Input.Date" required
                class="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm"
                value="@DateTime.Today.ToString("yyyy-MM-dd")" />
        </div>

        <div>
            <label for="description" class="block text-sm font-medium text-gray-700">Description</label>
            <input type="text" id="description" name="Input.Description" required
                class="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm" />
        </div>

        <div>
            <label for="amount" class="block text-sm font-medium text-gray-700">Amount</label>
            <input type="number" id="amount" name="Input.Amount" required step="0.01"
                class="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm" />
            <p class="mt-1 text-sm text-gray-500">Negative for expenses, positive for income</p>
        </div>

        <div>
            <label for="category" class="block text-sm font-medium text-gray-700">Category</label>
            <select id="category" name="Input.CategoryId"
                class="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm">
                <option value="">Uncategorized</option>
                @foreach (var category in Model.Categories)
                {
                    <option value="@category.Id">@category.Name</option>
                }
            </select>
        </div>

        <div class="flex space-x-2">
            <button type="submit"
                class="flex-1 py-2 px-4 bg-blue-600 text-white rounded hover:bg-blue-700">
                Add Transaction
            </button>
            <a href="/Transactions" class="flex-1 py-2 px-4 bg-gray-200 text-gray-700 rounded hover:bg-gray-300 text-center">
                Cancel
            </a>
        </div>
    </form>
</div>
```

**File**: `src/SauronSheet.Frontend/Pages/Transactions/Add.cshtml.cs`

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MediatR;
using SauronSheet.Application.Features.Transactions.Commands;
using SauronSheet.Application.Features.Categories.Queries;
using SauronSheet.Application.Features.Categories.DTOs;
using SauronSheet.Domain.Exceptions;

namespace SauronSheet.Frontend.Pages.Transactions;

[Authorize]
public class AddModel : PageModel
{
    private readonly IMediator _mediator;

    [BindProperty]
    public AddTransactionInputModel Input { get; set; } = new();

    public List<CategoryDto> Categories { get; set; } = new();
    public string? ErrorMessage { get; set; }

    public AddModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task OnGetAsync()
    {
        Categories = await _mediator.Send(new GetCategoriesQuery());
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Categories = await _mediator.Send(new GetCategoriesQuery());

        if (!ModelState.IsValid)
            return Page();

        try
        {
            var transactionId = await _mediator.Send(new CreateTransactionCommand(
                Input.Amount,
                Input.Currency ?? "EUR",
                Input.Date,
                Input.Description,
                Input.CategoryId));

            return RedirectToPage("/Transactions/Index");
        }
        catch (DomainException ex)
        {
            ErrorMessage = ex.Message;
            return Page();
        }
    }
}

public class AddTransactionInputModel
{
    public DateTime Date { get; set; } = DateTime.Today;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Currency { get; set; } = "EUR";
    public Guid? CategoryId { get; set; }
}
```

---

#### 8.5: Create Category Management Page (GREEN Phase)

**Task**: Build category CRUD page

**Directory structure**:
```sh
mkdir -p src/SauronSheet.Frontend/Pages/Categories
```

**File**: `src/SauronSheet.Frontend/Pages/Categories/Index.cshtml`

```html
@page
@model CategoryIndexModel
@{
    ViewData["Title"] = "Categories";
}

<div class="max-w-4xl mx-auto space-y-6">
    <h1 class="text-3xl font-bold text-gray-900">Category Management</h1>

    @if (!string.IsNullOrEmpty(Model.ErrorMessage))
    {
        <div class="rounded-md bg-red-50 p-4">
            <p class="text-sm text-red-700">@Model.ErrorMessage</p>
        </div>
    }

    @if (!string.IsNullOrEmpty(Model.SuccessMessage))
    {
        <div class="rounded-md bg-green-50 p-4">
            <p class="text-sm text-green-700">@Model.SuccessMessage</p>
        </div>
    }

    <!-- Create Category Form -->
    <div class="bg-white rounded-lg shadow p-6">
        <h2 class="text-xl font-semibold text-gray-900 mb-4">Create New Category</h2>
        <form method="post" asp-page-handler="Create" class="space-y-4">
            <div class="grid grid-cols-3 gap-4">
                <div class="col-span-2">
                    <label for="name" class="block text-sm font-medium text-gray-700">Name</label>
                    <input type="text" id="name" name="NewCategory.Name" required
                        class="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm" />
                </div>
                <div>
                    <label class="block text-sm font-medium text-gray-700">&nbsp;</label>
                    <button type="submit" class="mt-1 w-full py-2 px-4 bg-blue-600 text-white rounded hover:bg-blue-700">
                        Create
                    </button>
                </div>
            </div>
        </form>
    </div>

    <!-- Category List -->
    <div class="bg-white rounded-lg shadow overflow-hidden">
        <table class="min-w-full divide-y divide-gray-200">
            <thead class="bg-gray-50">
                <tr>
                    <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Name</th>
                    <th class="px-6 py-3 text-center text-xs font-medium text-gray-500 uppercase">Type</th>
                    <th class="px-6 py-3 text-center text-xs font-medium text-gray-500 uppercase">Transactions</th>
                    <th class="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase">Actions</th>
                </tr>
            </thead>
            <tbody class="bg-white divide-y divide-gray-200">
                @foreach (var category in Model.Categories)
                {
                    <tr>
                        <td class="px-6 py-4 text-sm text-gray-900">@category.Name</td>
                        <td class="px-6 py-4 text-center text-sm">
                            @if (category.IsSystemDefault)
                            {
                                <span class="px-2 py-1 bg-gray-100 text-gray-600 rounded text-xs">🔒 System</span>
                            }
                            else
                            {
                                <span class="px-2 py-1 bg-blue-100 text-blue-600 rounded text-xs">User</span>
                            }
                        </td>
                        <td class="px-6 py-4 text-center text-sm text-gray-500">@category.TransactionCount</td>
                        <td class="px-6 py-4 text-right text-sm space-x-2">
                            @if (!category.IsSystemDefault)
                            {
                                <button class="text-blue-600 hover:text-blue-900">Rename</button>
                                <form method="post" asp-page-handler="Delete" asp-route-categoryId="@category.Id" style="display: inline;">
                                    <button type="submit" class="text-red-600 hover:text-red-900"
                                        onclick="return confirm('Delete this category?')">
                                        Delete
                                    </button>
                                </form>
                            }
                            else
                            {
                                <span class="text-gray-400 text-xs">Protected</span>
                            }
                        </td>
                    </tr>
                }
            </tbody>
        </table>
    </div>
</div>
```

**File**: `src/SauronSheet.Frontend/Pages/Categories/Index.cshtml.cs`

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MediatR;
using SauronSheet.Application.Features.Categories.Commands;
using SauronSheet.Application.Features.Categories.Queries;
using SauronSheet.Application.Features.Categories.DTOs;
using SauronSheet.Domain.Exceptions;

namespace SauronSheet.Frontend.Pages.Categories;

[Authorize]
public class CategoryIndexModel : PageModel
{
    private readonly IMediator _mediator;

    public List<CategoryDto> Categories { get; set; } = new();

    [BindProperty]
    public CreateCategoryInputModel? NewCategory { get; set; }

    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    public CategoryIndexModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task OnGetAsync()
    {
        Categories = await _mediator.Send(new GetCategoriesQuery());
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        try
        {
            await _mediator.Send(new CreateCategoryCommand(
                NewCategory!.Name,
                NewCategory.Color,
                NewCategory.Icon));
            SuccessMessage = "Category created successfully.";
        }
        catch (DomainException ex)
        {
            ErrorMessage = ex.Message;
        }

        Categories = await _mediator.Send(new GetCategoriesQuery());
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid categoryId)
    {
        try
        {
            await _mediator.Send(new DeleteCategoryCommand(categoryId));
            SuccessMessage = "Category deleted successfully.";
        }
        catch (DomainException ex)
        {
            ErrorMessage = ex.Message;
        }

        Categories = await _mediator.Send(new GetCategoriesQuery());
        return Page();
    }
}

public class CreateCategoryInputModel
{
    public string Name { get; set; } = string.Empty;
    public string? Color { get; set; }
    public string? Icon { get; set; }
}
```

---

### 9. INFRASTRUCTURE LAYER — DI UPDATES

#### 9.1: Update Infrastructure DependencyInjection.cs (GREEN Phase)

**Task**: Register repositories and PDF parser

**File**: `src/SauronSheet.Infrastructure/DependencyInjection.cs` (update existing)

Replace entire file content:

```csharp
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Auth;
using Domain.Services;
using Domain.Repositories;
using Application.Common;
using Application.Interfaces;
using Persistence;
using PDF;
using PDF.Parsers;

namespace SauronSheet.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var supabaseUrl = configuration["Supabase:Url"]
            ?? throw new InvalidOperationException("Configuration key 'Supabase:Url' is not set.");

        var supabaseKey = configuration["Supabase:Key"]
            ?? throw new InvalidOperationException("Configuration key 'Supabase:Key' is not set.");

        var jwtSecret = configuration["Supabase:JwtSecret"]
            ?? throw new InvalidOperationException("Configuration key 'Supabase:JwtSecret' is not set.");

        // Auth configuration (from Phase 1)
        services.Configure<AuthConfiguration>(options =>
        {
            options.JwtSecret = jwtSecret;
        });

        // Auth services (from Phase 1)
        services.AddHttpClient<IAuthService, SupabaseAuthService>()
            .ConfigureHttpClient(client => client.BaseAddress = new Uri(supabaseUrl));

        services.AddScoped<IUserContext, HttpUserContext>();
        services.AddHttpContextAccessor();

        // Supabase client registration (Phase 3 - CRITICAL FIX C-1)
        services.AddSingleton<Supabase.Client>(sp =>
        {
            var options = new Supabase.SupabaseOptions
            {
                AutoRefreshToken = true,
                AutoConnectRealtime = false
            };
            return new Supabase.Client(supabaseUrl, supabaseKey, options);
        });

        // Repository implementations (NEW in Phase 3)
        services.AddScoped<ITransactionRepository, SupabaseTransactionRepository>();
        services.AddScoped<ICategoryRepository, SupabaseCategoryRepository>();
        services.AddScoped<IPdfImportRepository, SupabasePdfImportRepository>();

        // PDF parsing (NEW in Phase 3)
        services.AddScoped<IPdfParser, GenericBankPdfParser>();
        services.AddSingleton<PdfParserFactory>();

        // Domain services (NEW in Phase 3)
        services.AddScoped<CategoryService>();

        return services;
    }
}
```

---

### 10. INTEGRATION & VALIDATION

#### 10.1: Full Build

**Task**: Verify entire solution builds with zero warnings

```sh
dotnet build
# Expected: Build succeeds
# Expected: Zero errors, zero warnings (TreatWarningsAsErrors=true)
```

---

#### 10.2: Run All Tests

**Task**: Execute all 170 tests (121 prior Domain + 49 Application total)

```sh
dotnet test
# Expected: 170 tests PASS
# Expected: 121 Domain tests (11 Phase 0 + 8 Phase 1 + 96 Phase 2 + 5 Phase 3 + extras)
# Expected: 49 Application tests (2 Phase 0 + 14 Phase 1 + 33 Phase 3)
```

**Breakdown**:
- Phase 0 Domain: 11 tests
- Phase 1 Domain: 8 tests (UserId, AuthResult, UserProfile)
- Phase 2 Domain: 96 tests (entities, VOs, services, specs)
- Phase 3 Domain: 5 tests (ImportBatch)
- Phase 2 extras: ~1 test
- **Total Domain**: 121 tests

- Phase 0 Application: 2 tests (MediatR registration)
- Phase 1 Application: 14 tests (auth commands/queries)
- Phase 3 Application: 33 tests (transaction + category handlers)
- **Total Application**: 49 tests

**Grand Total**: 121 Domain + 49 Application = **170 tests**

**Verify actual count**:

```sh
dotnet test --filter Category=Domain | grep "Passed"
dotnet test --filter Category=Application | grep "Passed"
```

---

#### 10.3: Generate Test Coverage Report

**Task**: Generate code coverage report

```sh
# Install coverlet globally if not already installed
dotnet tool install -g coverlet.console

# Run coverage for Domain layer
coverlet tests/SauronSheet.Domain.Tests/bin/Debug/net10.0/SauronSheet.Domain.Tests.dll \
  --target "dotnet" \
  --targetargs "test tests/SauronSheet.Domain.Tests/ --no-build --configuration Debug" \
  --format "opencover" \
  --output "./coverage-domain-phase3.xml" \
  --include "[SauronSheet.Domain]*" \
  --exclude "[SauronSheet.Domain.Tests]*"

# Run coverage for Application layer
coverlet tests/SauronSheet.Application.Tests/bin/Debug/net10.0/SauronSheet.Application.Tests.dll \
  --target "dotnet" \
  --targetargs "test tests/SauronSheet.Application.Tests/ --no-build --configuration Debug" \
  --format "opencover" \
  --output "./coverage-app-phase3.xml" \
  --include "[SauronSheet.Application]*" \
  --exclude "[SauronSheet.Application.Tests]*"

# Expected: Domain ≥ 80%, Application ≥ 70%
```

---

#### 10.4: Verify Dependency Rules

**Task**: Audit .csproj files to ensure Clean Architecture maintained

```sh
echo "=== Phase 3 Dependency Verification ==="

# Domain MUST have ZERO project references
DOMAIN_REFS=$(grep -c "ProjectReference" src/SauronSheet.Domain/SauronSheet.Domain.csproj || echo "0")
if [ "$DOMAIN_REFS" -eq "0" ]; then
  echo "✓ PASS - Domain has zero project references"
else
  echo "❌ FAIL - Domain has project references"
fi

# Application → Domain only
APP_DOMAIN=$(grep "ProjectReference" src/SauronSheet.Application/SauronSheet.Application.csproj | grep -c "Domain" || echo "0")
if [ "$APP_DOMAIN" -eq "1" ]; then
  echo "✓ PASS - Application references Domain only"
else
  echo "❌ FAIL - Application has incorrect references"
fi

# Infrastructure → Domain only
INFRA_DOMAIN=$(grep "ProjectReference" src/SauronSheet.Infrastructure/SauronSheet.Infrastructure.csproj | grep -c "Domain" || echo "0")
if [ "$INFRA_DOMAIN" -eq "1" ]; then
  echo "✓ PASS - Infrastructure references Domain only"
else
  echo "❌ FAIL - Infrastructure has incorrect references"
fi

# Frontend → Application + Infrastructure
FRONTEND_REFS=$(grep "ProjectReference" src/SauronSheet.Frontend/SauronSheet.Frontend.csproj | wc -l)
if [ "$FRONTEND_REFS" -eq "2" ]; then
  echo "✓ PASS - Frontend references 2 projects"
else
  echo "❌ FAIL - Frontend has incorrect reference count: $FRONTEND_REFS (expected 2)"
fi

# Verify PdfPig package in Infrastructure only
INFRA_PDFPIG=$(grep -c "UglyToad.PdfPig" src/SauronSheet.Infrastructure/SauronSheet.Infrastructure.csproj || echo "0")
if [ "$INFRA_PDFPIG" -ge "1" ]; then
  echo "✓ PASS - Infrastructure has PdfPig package"
else
  echo "❌ FAIL - Infrastructure missing PdfPig package"
fi
```

---

#### 10.5: Manual E2E Testing

**Test Scenarios**:

```
Test Scenario 1: User Registration + Login (from Phase 1)
- Navigate to /Auth/Register
- Register: test@example.com, password123
- Login: test@example.com, password123
- Expected: Redirect to /Dashboard, authenticated session active

Test Scenario 2: Upload PDF
- Navigate to /Transactions/Upload
- Upload sample PDF (create test PDF with 5 transaction rows)
- Expected: Import results show N imported, M skipped
- Navigate to /Transactions
- Expected: Imported transactions appear in list

Test Scenario 3: Manual Add Transaction
- Navigate to /Transactions/Add
- Fill form: date = today, description = "Coffee", amount = -5.50, category = "Other"
- Submit
- Expected: Redirect to /Transactions, new transaction appears

Test Scenario 4: Category Management
- Navigate to /Categories
- Expected: 4 system defaults visible (Groceries, Transport, Utilities, Other)
- Create new category: "Entertainment"
- Expected: Category appears in list, marked as "User"
- Attempt to delete system default
- Expected: Delete button disabled or throws error
- Delete "Entertainment" category (if no transactions)
- Expected: Category removed from list

Test Scenario 5: Tenant Isolation
- Open incognito window, register different user
- Upload PDF for second user
- Verify: First user does not see second user's transactions
- Verify: First user does not see second user's categories

Test Scenario 6: Duplicate Detection
- Upload same PDF twice
- Expected: Second upload shows 0 imported, N skipped (all duplicates)

Test Scenario 7: Frontend Navigation
- Verify all navigation links work
- Verify Alpine.js interactivity (if used for dropdowns/modals)
- Verify responsive design (resize browser to mobile width)
```

---

## Dependency Graph

```
┌─────────────────────────────────────────────────────┐
│              SauronSheet.sln (Phase 3)               │
└─────────────────────────────────────────────────────┘
                          │
        ┌─────────────────┼──────────────────┐
        │                 │                  │
    ┌───────────┐    ┌────────────┐   ┌──────────┐
    │   src/    │    │   tests/   │   │ Root Cfg │
    └───────────┘    └────────────┘   └──────────┘
        │                   │              │
   ┌────┴──────┐      ┌─────┴─────┐     │
   │            │      │           │     │
┌──────────┐  ┌──────┐┌──────────┐┌──┐ ┌──┐
│ Domain   │  │ App  ││Infra     ││F ││.c│
│(+Import) │  │(+Tx) ││(+Repos)  ││r││o│
│(Batch)   │  │(+Cat)││(+PdfPig) ││n││n│
└──────────┘  └──────┘└──────────┘└──┘ └──┘
   ↑              ↑        ↑           ↑   ↑
   │              │        │      (Pages)  global
Domain.Tests      App.Tests  Infra       json
(121 tests)       (49 tests)  (Supabase)  (SDK)
                                Frontend
                          (Upload/List/Add/Categories)
```

**Key Rules (Phase 3 Enforcement)**:
- Domain → ZERO dependencies (ImportBatch entity added)
- Application → Domain + MediatR (13 commands/queries, 5 DTOs, IPdfParser)
- Infrastructure → Domain + PdfPig + Supabase (3 repos, PDF parser)
- Frontend → Application + Infrastructure (4 Razor Pages, Alpine.js)
- IPdfParser defined in Application; GenericBankPdfParser implements in Infrastructure

---

## Red-Green-Refactor Workflow

### Example: Implementing ImportTransactionsFromPdfCommand

**Step 1: RED**
- Write test stub for T-3.01: `ImportPdf_ValidPdf_ImportsTransactions()`
- Test FAILS (ImportTransactionsFromPdfCommandHandler doesn't exist)

**Step 2: GREEN**
- Implement `ImportTransactionsFromPdfCommand` record and handler
- Minimal handler: parse PDF via IPdfParser, create Transaction entities, return ImportResultDto
- Test PASSES

**Step 3: REFACTOR**
- Add duplicate detection logic
- Add per-row error reporting
- Add system default seeding
- Write tests T-3.02 to T-3.06
- Tests FAIL initially, then implement logic → PASS
- Refactor for clarity: extract helper methods, improve error messages

**Result**: Import command fully tested, all edge cases covered

---

## Validation Checkpoints

### Checkpoint 3A: Domain Layer Extensions (End of Day 2)
```
Status: ✓ PASS
Verification Command: dotnet test --filter Category=Domain --no-build
Metrics:
  ✓ 121 domain tests PASS (116 Phase 0+1+2 + 5 Phase 3 ImportBatch)
  ✓ ImportBatch entity implemented (not VO)
  ✓ Domain.csproj still has ZERO dependencies
```

### Checkpoint 3B: Application DTOs (End of Day 3)
```
Status: ✓ PASS
Verification Command: dotnet build
Metrics:
  ✓ 5 DTOs compile (TransactionDto, ImportResultDto, ImportRowErrorDto, PaginatedResultDto, CategoryDto)
  ✓ IPdfParser interface defined
  ✓ RawTransactionRow model defined
```

### Checkpoint 3C: Transaction Commands (End of Day 5)
```
Status: ✓ PASS
Verification Command: dotnet test --filter Category=Application --no-build
Metrics:
  ✓ 32 Application tests PASS (16 Phase 1 + 16 Phase 3 transaction tests)
  ✓ Import, Create, Update, Delete transaction handlers working
  ✓ GetTransactions, GetTransactionById queries working
```

### Checkpoint 3D: Category Commands (End of Day 7)
```
Status: ✓ PASS
Verification Command: dotnet test --filter Category=Application --no-build
Metrics:
  ✓ 43 Application tests PASS (32 + 11 category tests)
  ✓ Create, Rename, Delete category handlers working
  ✓ GetCategories, SeedSystemDefaults working
```

### Checkpoint 3E: Database Migrations (End of Day 8)
```
Status: ✓ PASS
Verification Command: Manual verification in Supabase dashboard
Metrics:
  ✓ 3 migrations applied (categories, transactions, pdf_imports)
  ✓ All indexes created
  ✓ RLS policies active on all 3 tables
```

### Checkpoint 3F: Repositories (End of Day 10)
```
Status: ✓ PASS (with stubs)
Verification Command: dotnet build
Metrics:
  ✓ SupabaseTransactionRepository compiles
  ✓ SupabaseCategoryRepository compiles
  ✓ SupabasePdfImportRepository compiles
  ⚠️ Note: Full implementation pending actual Supabase client integration
```

### Checkpoint 3G: PDF Parser (End of Day 11)
```
Status: ✓ PASS
Verification Command: dotnet build
Metrics:
  ✓ PdfPig package installed
  ✓ GenericBankPdfParser compiles
  ✓ PdfParserFactory compiles
  ⚠️ Note: Parsing logic is simplified; production needs robust pattern matching
```

### Checkpoint 3H: Frontend Pages (End of Day 13)
```
Status: ✓ PASS
Verification Command: dotnet run --project src/SauronSheet.Frontend/
Visual Check (in browser):
  ✓ /Transactions/Upload page loads, file input visible
  ✓ /Transactions page loads, pagination visible
  ✓ /Transactions/Add page loads, form visible
  ✓ /Categories page loads, system defaults visible
  ✓ Navigation links work (Transactions, Upload, Categories)
  ✓ Alpine.js loaded (check browser console for errors)
```

### Checkpoint 3I: Integration & Validation (End of Day 15)
```
Status: ✓ PASS
Verification Commands (run in order):
  1. dotnet build                                    # Exit code 0, zero warnings
  2. dotnet test                                     # Output: "170 passed" (or final count)
  3. coverlet (domain + application coverage)        # Domain ≥ 80%, App ≥ 70%
  4. Bash script dependency verification             # All assertions PASS
  5. Manual E2E: upload PDF → list → add → categories # All flows working

Final Metrics:
  ✓ Full build: zero errors, zero warnings (TreatWarningsAsErrors enforced)
  ✓ All 170 tests: PASS (121 Domain + 49 Application)
  ✓ Coverage reports generated
  ✓ Dependency rules verified
  ✓ PDF import pipeline working end-to-end
  ✓ Transaction CRUD working
  ✓ Category management working
  ✓ RLS policies verified (tenant isolation)
  ✓ Solution ready for Phase 4 (Analytics & Dashboard)
```

---

## Risk Mitigation

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|-----------|
| PdfPig parsing fails on complex PDFs | Medium | Medium | Start with simple generic parser; add bank-specific parsers in future |
| Supabase repository implementation incomplete | High | High | Provide stubs in plan; full implementation in Phase 3F coding |
| Alpine.js CDN conflicts with existing JS | Low | Low | Minimal JS in Phase 0-2; Alpine.js is self-contained |
| ImportBatch entity ID generation confusion | Low | Medium | Handler passes Guid.NewGuid() explicitly; documented in code |
| RLS policies block legitimate operations | Medium | Medium | Test all CRUD operations with authenticated user; verify policies |
| PDF file size limit not enforced | Low | Medium | Validate in Frontend (file input) AND handler (10MB limit) |
| Duplicate detection hash collisions | Low | Low | Hash uses 4 fields (userId, date, amount, description); false positives unlikely |
| Category seeding race condition | Low | Low | UNIQUE(user_id, name) constraint prevents duplicates; idempotent check in handler |
| **NC-3: PDF encoding issues (UTF-16, scanned PDFs)** | Medium | Medium | **MITIGATION APPLIED**: Wrap parsing in try-catch; return descriptive errors; log warnings |
| **NC-3: Password-protected PDFs** | Low | Medium | **MITIGATION APPLIED**: PdfDocumentFormatException caught; user-friendly error message |
| **NC-4: Concurrent PDF uploads** | Low | Low | RLS + unique index prevents duplicates; UI shows spinner during upload (blocking) |
| **NC-2: Network failures (Supabase offline)** | Medium | High | **MITIGATION APPLIED**: HttpRequestException caught in PageModels; user-friendly error messages |

---

## Success Criteria Summary

| Criterion | Status | Objective Validation Command |
|-----------|--------|-----------|
| 5 domain tests pass | ✓ | `dotnet test --filter Category=Domain` → 121 tests (5 new) pass |
| 33 application tests pass | ✓ | `dotnet test --filter Category=Application` → 49 tests (33 new) pass |
| Total 170 tests pass | ✓ | `dotnet test` → output shows "170 passed" |
| Domain coverage ≥ 80% | ✓ | coverlet report shows Domain files ≥ 80% |
| Application coverage ≥ 70% | ✓ | coverlet report shows Application handlers ≥ 70% |
| Dependency rules enforced | ✓ | Bash script shows all assertions PASS |
| 3 database migrations applied | ✓ | Supabase dashboard shows tables + RLS policies |
| Upload PDF page renders | ✓ | Browser at `/Transactions/Upload` loads form |
| Transaction list page renders | ✓ | Browser at `/Transactions` loads paginated list |
| Add transaction page renders | ✓ | Browser at `/Transactions/Add` loads form |
| Category management page renders | ✓ | Browser at `/Categories` loads category list |
| PDF import works E2E | ✓ | Upload PDF → transactions appear in list |
| Tenant isolation works | ✓ | Two different users cannot see each other's data |
| Duplicate detection works | ✓ | Upload same PDF twice → second time: 0 imported, N skipped |
| System defaults seeded | ✓ | First category interaction creates 4 defaults |
| Alpine.js integration working | ✓ | No console errors, interactivity functional |

---

## Next Steps (Post-Phase 3)

Once Phase 3 is complete and all checkpoints PASS:

1. **Merge to main**: Create PR with all Phase 3 deliverables (first full-stack feature complete)
2. **Complete repository implementations**: Finish Supabase repository methods (if stubs used)
3. **Improve PDF parser**: Add bank-specific parsers for real-world PDFs
4. **Begin Phase 4**: Transition to Analytics & Dashboard (MVP completion)
5. **Phase 4 prep**: All prior tests still passing? ✓ Ready for Phase 4 analytics queries

---

**Created**: 2026-02-15  
**Version**: 1.0.0  
**Duration**: 15 days (Weeks 9–13)  
**Status**: Ready for implementation ✅
