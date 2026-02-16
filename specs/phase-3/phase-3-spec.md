
# Phase 3: Transaction Import Pipeline

## Quick Reference

- **Status**: Draft
- **Layer Scope**: All layers (Full-Stack Features)
- **Phase Type**: Full-Stack (Features)
- **Duration**: Weeks 9–13
- **Goal**: PDF import pipeline, transaction CRUD, category management, Supabase persistence — first full-stack feature phase
- **Depends On**: Phase 0 (foundation), Phase 1 (auth + tenant scoping), Phase 2 (domain model — entities, VOs, services, repository interfaces, specifications)
- **Unlocks**: Phase 4 (Analytics & Dashboard — MVP completion)

---

## Critical Decisions

| ID      | Decision                                         | Rationale                                                                 | Date       |
|---------|--------------------------------------------------|---------------------------------------------------------------------------|------------|
| CD-3.1  | PDF parsing library: PdfPig (Apache 2.0 license) | Open-source, .NET native, no Java dependency (unlike iTextSharp); reads text and tables | 2026-02-15 |
| CD-3.2  | Bank-specific parser strategy pattern            | Different banks produce different PDF formats; extensible without modifying core logic | 2026-02-15 |
| CD-3.3  | Bulk import with per-row error reporting         | Users need to know which rows failed and why; partial success is acceptable | 2026-02-15 |
| CD-3.4  | Duplicate detection via date + amount + description hash | Prevents re-importing same transactions; checked at repository level | 2026-02-15 |
| CD-3.5  | Supabase RLS policies for all tables             | Belt-and-suspenders: tenant isolation at DB level AND handler level        | 2026-02-15 |
| CD-3.6  | System default categories seeded on first user interaction | Seed when user first accesses categories or imports; idempotent operation | 2026-02-15 |
| CD-3.7  | Max PDF file size: 10MB                          | Reasonable limit for bank statements; prevents abuse; validated in Frontend + handler | 2026-02-15 |
| CD-3.8  | Transaction list pagination: 50 per page default | Balances performance and UX; configurable via query parameter             | 2026-02-15 |
| CD-3.9  | `IPdfParser` interface defined in Application layer | Parser is an application concern (use case orchestration); not a domain concept | 2026-02-15 |
| CD-3.10 | Category assignment on import defaults to null (uncategorized) | Auto-categorization deferred to post-MVP; user manually categorizes after import | 2026-02-15 |

---

## Executive Summary

### In Scope

| Area           | Deliverable                                                                                           |
|----------------|-------------------------------------------------------------------------------------------------------|
| Domain         | `ImportBatch` value object (batch metadata), minor entity additions if needed                          |
| Application    | `ImportTransactionsFromPdfCommand` + handler (PDF import pipeline orchestration)                       |
| Application    | `CreateTransactionCommand` + handler (manual transaction creation)                                     |
| Application    | `UpdateTransactionCategoryCommand` + handler                                                           |
| Application    | `UpdateTransactionDescriptionCommand` + handler                                                        |
| Application    | `DeleteTransactionCommand` + handler                                                                   |
| Application    | `GetTransactionsQuery` + handler (paginated, sorted, filtered)                                         |
| Application    | `GetTransactionByIdQuery` + handler                                                                    |
| Application    | `CreateCategoryCommand` + handler                                                                      |
| Application    | `RenameCategoryCommand` + handler                                                                      |
| Application    | `DeleteCategoryCommand` + handler                                                                      |
| Application    | `GetCategoriesQuery` + handler (includes system defaults)                                              |
| Application    | `SeedSystemDefaultsCommand` + handler (idempotent)                                                     |
| Application    | `IPdfParser` interface, `RawTransactionRow` model, `ImportResultDto`, `TransactionDto`, `CategoryDto` |
| Infrastructure | `SupabaseTransactionRepository` (implements `ITransactionRepository`)                                  |
| Infrastructure | `SupabaseCategoryRepository` (implements `ICategoryRepository`)                                        |
| Infrastructure | `SupabasePdfImportRepository` (tracks import batches)                                                  |
| Infrastructure | `PdfParserFactory` + `GenericBankPdfParser` (strategy pattern)                                         |
| Infrastructure | Database migrations: `transactions`, `categories`, `pdf_imports` tables with indexes and RLS           |
| Frontend       | Upload PDF page (`/Transactions/Upload`)                                                               |
| Frontend       | Transaction list page (`/Transactions`) — paginated, sorted                                            |
| Frontend       | Manual add transaction page (`/Transactions/Add`)                                                      |
| Frontend       | Category management page (`/Categories`)                                                               |
| Frontend       | Updated `_Layout.cshtml` navigation with new page links                                                |
| Tests          | ≥28 tests (application handler integration tests)                                                      |

### Deferred (NOT in this phase)

| Item                           | Target Phase | Reason                                       |
|--------------------------------|--------------|----------------------------------------------|
| Analytics/charts               | Phase 4      | Analytics & Dashboard phase                  |
| Budget CRUD                    | Phase 5      | Budget Management phase                      |
| Bulk edit/delete transactions  | Post-MVP     | Not critical for MVP                         |
| Multi-bank format support      | Post-MVP     | Start with one generic bank format           |
| Transaction search/filtering   | Phase 4      | Part of analytics dashboard                  |
| Auto-categorization rules      | Post-MVP     | ML/rules engine complexity                   |
| CSV/Excel import               | Post-MVP     | PDF is primary import format                 |
| Transaction attachments        | Post-MVP     | Receipt photos, etc.                         |
| Bulk category assignment       | Post-MVP     | Select multiple → assign category            |

---

## User Scenarios & Testing

### Scenario 3.1: Upload PDF Bank Statement

**As a** user
**I want to** upload a PDF bank statement
**So that** my transactions are automatically imported

**Acceptance Criteria:**
- Upload page accepts PDF files only (file type validation: `.pdf` extension + `application/pdf` MIME)
- Max file size: 10MB — larger files rejected with descriptive error
- On upload: parse PDF → extract transaction rows → validate → check duplicates → persist
- Response shows import summary:
  - N transactions imported successfully
  - M transactions skipped (with per-row reasons: "Duplicate", "Invalid date", "Missing description", etc.)
  - Import batch metadata saved (filename, imported count, skipped count, timestamp)
- Imported transactions appear in transaction list immediately
- Each imported transaction has `ImportedFrom` set to PDF filename
- Duplicate transactions are detected by matching `(userId, date, amount, description)` and skipped
- System default categories are seeded for the user if not already present (idempotent)
- Empty PDF (no parseable transactions) returns success with 0 imported, 0 skipped, and informational message

### Scenario 3.2: View Transaction List

**As a** user
**I want to** see all my transactions in a paginated list
**So that** I can review my spending

**Acceptance Criteria:**
- Paginated list of transactions (default 50 per page)
- Sorted by date descending (newest first)
- Each row shows: date (formatted), description, amount (with currency), category name (or "Uncategorized")
- Pagination controls: Previous / Next / Page numbers
- Scoped to current authenticated user only (other users' transactions never visible)
- Responsive design: table on desktop, card layout on mobile
- Empty state: "No transactions yet. Import a PDF or add one manually." with links

### Scenario 3.3: Manually Add Transaction

**As a** user
**I want to** manually add a transaction
**So that** I can track cash expenses or items not in my bank statement

**Acceptance Criteria:**
- Form fields: Date (date picker), Description (text), Amount (decimal input), Category (dropdown)
- Category dropdown includes all user categories + system defaults, plus "Uncategorized" option
- Client-side validation: all required fields filled, amount is a valid number, date is not in the future
- Server-side validation: domain rules enforced (date not future, description not empty)
- On success: redirect to transaction list with success message
- On failure: display specific validation errors, form retains entered values
- Amount: positive values for income, negative for expenses (or toggle for income/expense)

### Scenario 3.4: Categorize Transaction

**As a** user
**I want to** assign or change the category of a transaction
**So that** my spending is organized

**Acceptance Criteria:**
- Transaction list shows current category per row
- Edit category: dropdown selector or inline edit button
- Category selection includes all user categories + system defaults + "Uncategorized" (null)
- On change: update persisted immediately via command
- Optimistic UI update (show new category before server confirms)
- Scoped to current user only

### Scenario 3.5: Manage Categories

**As a** user
**I want to** create, rename, and delete custom categories
**So that** I can organize spending my way

**Acceptance Criteria:**
- Category management page lists all categories (system defaults + user-defined)
- System defaults displayed with visual indicator (badge, lock icon) and edit/delete disabled
- **Create**: form with name (required), optional color (hex picker), optional icon (icon selector or text)
- **Rename**: inline edit for user-defined categories only; name uniqueness enforced per user
- **Delete**: confirmation dialog; blocked with error message if category has active transactions
- Name uniqueness enforced: attempting to create/rename to an existing name shows error
- System defaults seeded automatically if not present when page loads

### Scenario 3.6: Delete Transaction

**As a** user
**I want to** delete a transaction
**So that** I can remove incorrect or duplicate entries

**Acceptance Criteria:**
- Delete button on each transaction row (icon or text)
- Confirmation dialog: "Are you sure you want to delete this transaction? This action cannot be undone."
- On confirm: transaction removed from database and list
- On cancel: no action taken
- Scoped to current user only — cannot delete another user's transactions
- Attempting to delete a non-existent transaction returns appropriate error

### Scenario 3.7: System Default Categories Seeded Automatically

**As a** user
**I want** the 4 default categories to exist when I first use the app
**So that** I can start categorizing immediately without manual setup

**Acceptance Criteria:**
- On first category-related interaction (import, view categories, add transaction), defaults are seeded
- Seed operation is idempotent: running multiple times does not create duplicates
- 4 defaults created: Groceries, Transport, Utilities, Other
- All marked `IsSystemDefault = true`
- Associated with the current user's `UserId`

---

## Functional Requirements

### FR-3.01: Domain Layer Additions

#### ImportBatch Value Object

```csharp
public record ImportBatch : ValueObject
{
    public string Filename { get; }
    public int ImportedCount { get; }
    public int SkippedCount { get; }
    public DateTime ImportedAt { get; }

    public ImportBatch(string filename, int importedCount, int skippedCount, DateTime importedAt)
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
FR-3.02: Application Layer — Commands & Queries
text
Application/
├── Features/
│   ├── Transactions/
│   │   ├── Commands/
│   │   │   ├── ImportTransactionsFromPdfCommand.cs
│   │   │   ├── ImportTransactionsFromPdfCommandHandler.cs
│   │   │   ├── CreateTransactionCommand.cs
│   │   │   ├── CreateTransactionCommandHandler.cs
│   │   │   ├── UpdateTransactionCategoryCommand.cs
│   │   │   ├── UpdateTransactionCategoryCommandHandler.cs
│   │   │   ├── UpdateTransactionDescriptionCommand.cs
│   │   │   ├── UpdateTransactionDescriptionCommandHandler.cs
│   │   │   ├── DeleteTransactionCommand.cs
│   │   │   └── DeleteTransactionCommandHandler.cs
│   │   ├── Queries/
│   │   │   ├── GetTransactionsQuery.cs
│   │   │   ├── GetTransactionsQueryHandler.cs
│   │   │   ├── GetTransactionByIdQuery.cs
│   │   │   └── GetTransactionByIdQueryHandler.cs
│   │   └── DTOs/
│   │       ├── TransactionDto.cs
│   │       ├── ImportResultDto.cs
│   │       ├── ImportRowErrorDto.cs
│   │       └── PaginatedResultDto.cs
│   └── Categories/
│       ├── Commands/
│       │   ├── CreateCategoryCommand.cs
│       │   ├── CreateCategoryCommandHandler.cs
│       │   ├── RenameCategoryCommand.cs
│       │   ├── RenameCategoryCommandHandler.cs
│       │   ├── DeleteCategoryCommand.cs
│       │   ├── DeleteCategoryCommandHandler.cs
│       │   ├── SeedSystemDefaultsCommand.cs
│       │   └── SeedSystemDefaultsCommandHandler.cs
│       ├── Queries/
│       │   ├── GetCategoriesQuery.cs
│       │   └── GetCategoriesQueryHandler.cs
│       └── DTOs/
│           └── CategoryDto.cs
├── Interfaces/
│   └── IPdfParser.cs
└── Common/
    └── Models/
        └── RawTransactionRow.cs
ImportTransactionsFromPdfCommand
csharp
public record ImportTransactionsFromPdfCommand(
    Stream PdfStream,
    string Filename
) : IRequest<ImportResultDto>;
Handler Flow:

1. Validate file: not null, not empty, filename ends with ".pdf"
2. Get `UserId` from `IUserContext`
3. Seed system default categories if not present (via `SeedSystemDefaultsCommand` or inline check)
4. Parse PDF via `IPdfParser.ParseAsync(pdfStream)` → `List<RawTransactionRow>`
5. For each `RawTransactionRow`:
   a. Validate: date not future, description not empty, amount parseable
   b. Check duplicate: `ITransactionRepository.ExistsDuplicateAsync(userId, date, amount, description)`
   c. If valid and not duplicate:
      - Create `Transaction` entity
      - Add to import batch list
   d. If invalid or duplicate:
      - Add to skipped list with reason
6. Persist all valid transactions via `ITransactionRepository.AddAsync()`
7. Save import batch metadata via `IPdfImportRepository`
8. Return `ImportResultDto` with counts and error details
CreateTransactionCommand
csharp
public record CreateTransactionCommand(
    decimal Amount,
    string Currency,
    DateTime Date,
    string Description,
    Guid? CategoryId
) : IRequest<Guid>;
Handler Flow:

1. Get `UserId` from `IUserContext`
2. Create `TransactionId` (new Guid)
3. Create `Money` value object from Amount + Currency
4. If `CategoryId` provided: validate category exists and belongs to user
5. Create `Transaction` entity (invariants enforced by constructor)
6. Persist via `ITransactionRepository.AddAsync()`
7. Return `TransactionId.Value`
UpdateTransactionCategoryCommand
csharp
public record UpdateTransactionCategoryCommand(
    Guid TransactionId,
    Guid? CategoryId
) : IRequest<Unit>;
Handler Flow:

1. Get `UserId` from `IUserContext`
2. Load `Transaction` by Id; throw `EntityNotFoundException` if not found
3. Verify `Transaction.UserId` matches current user; throw if mismatch (tenant isolation)
4. If `CategoryId` provided: validate category exists and belongs to user
5. Call `Transaction.Categorize(categoryId)` or set to null
6. Persist via `ITransactionRepository.UpdateAsync()`
UpdateTransactionDescriptionCommand
csharp
public record UpdateTransactionDescriptionCommand(
    Guid TransactionId,
    string NewDescription
) : IRequest<Unit>;
Handler Flow:

1. Get `UserId` from `IUserContext`
2. Load `Transaction` by Id; throw `EntityNotFoundException` if not found
3. Verify `Transaction.UserId` matches current user
4. Call `Transaction.UpdateDescription(newDescription)` (invariant enforced by entity)
5. Persist via `ITransactionRepository.UpdateAsync()`
DeleteTransactionCommand
csharp
public record DeleteTransactionCommand(
    Guid TransactionId
) : IRequest<Unit>;
Handler Flow:

1. Get `UserId` from `IUserContext`
2. Load `Transaction` by Id; throw `EntityNotFoundException` if not found
3. Verify `Transaction.UserId` matches current user
4. Delete via `ITransactionRepository.DeleteAsync()`
#### GetTransactionsQuery

```csharp
public record GetTransactionsQuery(
    int Page = 1,
    int PageSize = 50,
    Guid? CategoryId = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null
) : IRequest<PaginatedResultDto<TransactionDto>>;
```

**Handler Flow:**

1. Get `UserId` from `IUserContext`
2. Build specification(s) based on filters + `UserId`
3. Query via `ITransactionRepository.FindBySpecificationAsync()`
4. Apply pagination (skip/take)
5. Map `Transaction` entities → `TransactionDto` list
6. Return `PaginatedResultDto` with items, total count, page info

#### GetTransactionByIdQuery

```csharp
public record GetTransactionByIdQuery(
    Guid TransactionId
) : IRequest<TransactionDto>;
```

**Handler Flow:**

1. Get `UserId` from `IUserContext`
2. Load `Transaction` by Id; throw `EntityNotFoundException` if not found
3. Verify `Transaction.UserId` matches current user
4. Map to `TransactionDto` and return

#### CreateCategoryCommand

```csharp
public record CreateCategoryCommand(
    string Name,
    string? Color = null,
    string? Icon = null
) : IRequest<Guid>;
```

**Handler Flow:**

1. Get `UserId` from `IUserContext`
2. Validate unique name via `CategoryService.ValidateUniqueName(userId, name)`
3. Create `CategoryId` (new Guid)
4. Create `Category` entity (public constructor — `IsSystemDefault = false`)
5. Persist via `ICategoryRepository.AddAsync()`
6. Return `CategoryId.Value`

#### RenameCategoryCommand

```csharp
public record RenameCategoryCommand(
    Guid CategoryId,
    string NewName
) : IRequest<Unit>;
```

**Handler Flow:**

1. Get `UserId` from `IUserContext`
2. Load `Category` by Id; throw `EntityNotFoundException` if not found
3. Verify `Category.UserId` matches current user
4. Validate unique name via `CategoryService.ValidateUniqueName(userId, newName)`
5. Call `Category.Rename(newName)` (guards enforced by entity)
6. Persist via `ICategoryRepository.UpdateAsync()`

#### DeleteCategoryCommand

```csharp
public record DeleteCategoryCommand(
    Guid CategoryId
) : IRequest<Unit>;
```

**Handler Flow:**

1. Get `UserId` from `IUserContext`
2. Load `Category` by Id; throw `EntityNotFoundException` if not found
3. Verify `Category.UserId` matches current user
4. Check `hasActiveTransactions` via `ICategoryRepository.HasTransactionsAsync(categoryId)`
5. Verify `Category.CanDelete(hasActiveTransactions)`; if false → throw `DomainException`
6. Delete via `ICategoryRepository.DeleteAsync()`

#### GetCategoriesQuery

```csharp
public record GetCategoriesQuery : IRequest<List<CategoryDto>>;
```

**Handler Flow:**

1. Get `UserId` from `IUserContext`
2. Ensure system defaults exist (seed if not present — idempotent)
3. Load all categories via `ICategoryRepository.GetByUserIdAsync(userId)`
4. Map to `CategoryDto` list (include `IsSystemDefault` flag, transaction count)
5. Sort: system defaults first, then user-defined alphabetically

#### SeedSystemDefaultsCommand

```csharp
public record SeedSystemDefaultsCommand : IRequest<List<Guid>>;
```

**Handler Flow:**

1. Get `UserId` from `IUserContext`
2. Check if system defaults already exist via `ICategoryRepository.GetSystemDefaultsAsync(userId)`
3. If already exist (count == 4): return existing IDs (idempotent — no duplicates)
4. If not exist: generate via `CategoryService.GetSystemDefaults(userId)`
5. Persist each via `ICategoryRepository.AddAsync()`
6. Return list of `CategoryId.Value`

### FR-3.03: Application DTOs

#### TransactionDto

```csharp
public record TransactionDto(
    Guid Id,
    decimal Amount,
    string Currency,
    DateTime Date,
    string Description,
    Guid? CategoryId,
    string? CategoryName,
    string? ImportedFrom,
    DateTime CreatedAt
);
```

#### ImportResultDto

```csharp
public record ImportResultDto(
    int ImportedCount,
    int SkippedCount,
    int TotalProcessed,
    string Filename,
    DateTime ImportedAt,
    List<ImportRowErrorDto> Errors
);
```

#### ImportRowErrorDto

```csharp
public record ImportRowErrorDto(
    int RowNumber,
    string RawData,
    string Reason
);
```

#### PaginatedResultDto<T>

```csharp
public record PaginatedResultDto<T>(
    List<T> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);
```

#### CategoryDto

```csharp
public record CategoryDto(
    Guid Id,
    string Name,
    string? Color,
    string? Icon,
    bool IsSystemDefault,
    int TransactionCount
);
```

### FR-3.04: Application Interfaces

#### IPdfParser

```csharp
public interface IPdfParser
{
    Task<List<RawTransactionRow>> ParseAsync(Stream pdfStream);
}
```

#### RawTransactionRow

```csharp
public record RawTransactionRow(
    int RowNumber,
    string? DateRaw,
    string? DescriptionRaw,
    string? AmountRaw,
    string? CurrencyRaw
);
FR-3.05: Infrastructure — PDF Parsing
text
Infrastructure/
├── PDF/
│   ├── PdfParserFactory.cs
│   ├── Parsers/
│   │   └── GenericBankPdfParser.cs
│   └── Models/
│       └── (uses Application RawTransactionRow)
PdfParserFactory
csharp
public class PdfParserFactory
{
    public IPdfParser CreateParser(string? bankIdentifier = null)
    {
        // Strategy pattern: return bank-specific parser based on identifier
        // Default: GenericBankPdfParser
        return bankIdentifier switch
        {
            // "bankname" => new BankNamePdfParser(),
            _ => new GenericBankPdfParser()
        };
    }
}
GenericBankPdfParser
csharp
public class GenericBankPdfParser : IPdfParser
{
    public async Task<List<RawTransactionRow>> ParseAsync(Stream pdfStream)
    {
        // 1. Open PDF using PdfPig
        // 2. Extract text from each page
        // 3. Parse lines into rows (heuristic: date pattern, amount pattern)
        // 4. Return list of RawTransactionRow
        // 5. Handle malformed lines gracefully (skip with null fields)
    }
}
#### Parsing Heuristics:

- **Date pattern:** dd/MM/yyyy, dd-MM-yyyy, yyyy-MM-dd (configurable)
- **Amount pattern:** decimal with optional minus sign, comma or dot separator
- **Description:** remaining text between date and amount
- **Currency:** detected from header or defaults to "EUR"
FR-3.06: Infrastructure — Repository Implementations
text
Infrastructure/
├── Persistence/
│   ├── SupabaseTransactionRepository.cs
│   ├── SupabaseCategoryRepository.cs
│   ├── SupabasePdfImportRepository.cs
│   └── Migrations/
│       ├── 001_CreateUsersTable.sql          # (from Phase 1)
│       ├── 002_CreateCategoriesTable.sql
│       ├── 003_CreateTransactionsTable.sql
│       └── 004_CreatePdfImportsTable.sql
Repository Implementation Pattern
csharp
public class SupabaseTransactionRepository : ITransactionRepository
{
    private readonly Supabase.Client _client;

    public SupabaseTransactionRepository(Supabase.Client client)
    {
        _client = client;
    }

    public async Task<Transaction?> GetByIdAsync(TransactionId id)
    {
        // Query Supabase Postgrest API
        // Map response to Transaction entity
        // Return null if not found
    }

    public async Task AddAsync(Transaction transaction)
    {
        // Map Transaction entity to Supabase row model
        // Insert via Postgrest API
    }

    // ... all ITransactionRepository methods
}
Entity ↔ Supabase Mapping:

| Entity Property     | Supabase Column | Type          | Notes                        |
|---------------------|-----------------|---------------|------------------------------|
| Id.Value            | id              | UUID          | Primary key                  |
| UserId.Value        | user_id         | UUID          | FK to users.id               |
| Amount.Amount       | amount          | DECIMAL(15,2) | Monetary amount              |
| Amount.Currency     | currency        | VARCHAR(3)    | Currency code                |
| Date                | date            | TIMESTAMPTZ   | Transaction date             |
| Description         | description     | TEXT          | Transaction description      |
| CategoryId?.Value   | category_id     | UUID          | Nullable FK to categories.id |
| ImportedFrom        | imported_from   | TEXT          | Nullable PDF filename        |
| CreatedAt           | created_at      | TIMESTAMPTZ   | Auto-set                     |
| UpdatedAt           | updated_at      | TIMESTAMPTZ   | Nullable; set on update      |
FR-3.07: Database Migrations
002_CreateCategoriesTable.sql
sql
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
003_CreateTransactionsTable.sql
sql
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
#### 004_CreatePdfImportsTable.sql

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

### FR-3.08: Frontend Pages

```
Frontend/
├── Pages/
│   ├── Transactions/
│   │   ├── Index.cshtml                # Transaction list (paginated)
│   │   ├── Index.cshtml.cs
│   │   ├── Upload.cshtml               # PDF upload form + results
│   │   ├── Upload.cshtml.cs
│   │   ├── Add.cshtml                  # Manual add transaction form
│   │   └── Add.cshtml.cs
│   ├── Categories/
│   │   ├── Index.cshtml                # Category management
│   │   └── Index.cshtml.cs
│   ├── Dashboard.cshtml                # (from Phase 1 — updated with nav links)
│   └── Dashboard.cshtml.cs
├── Shared/
│   └── _Layout.cshtml                  # Updated navigation
```

#### Upload PDF Page (/Transactions/Upload)

```csharp
[Authorize]
public class UploadModel : PageModel
{
    private readonly IMediator _mediator;

    [BindProperty]
    public IFormFile? PdfFile { get; set; }

    public ImportResultDto? ImportResult { get; set; }
    public string? ErrorMessage { get; set; }

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

        try
        {
            using var stream = PdfFile.OpenReadStream();
            ImportResult = await _mediator.Send(
                new ImportTransactionsFromPdfCommand(stream, PdfFile.FileName));
        }
        catch (DomainException ex)
        {
            ErrorMessage = ex.Message;
        }

        return Page();
    }
}
View Requirements:

- File input with drag-and-drop zone (styled with Tailwind)
- File type restriction (.pdf only via `accept` attribute)
- Upload button: "Import Transactions"
- Loading spinner during upload
- After upload: display `ImportResultDto` summary:
  - Success badge with imported count
  - Warning badge with skipped count (if > 0)
  - Expandable error list showing per-row errors
- Link to transaction list: "View imported transactions →"
Transaction List Page (/Transactions)
csharp
[Authorize]
public class TransactionListModel : PageModel
{
    private readonly IMediator _mediator;

    public PaginatedResultDto<TransactionDto> Transactions { get; set; }

    [BindProperty(SupportsGet = true)]
    public int Page { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public int PageSize { get; set; } = 50;

    public async Task OnGetAsync()
    {
        Transactions = await _mediator.Send(
            new GetTransactionsQuery(Page, PageSize));
    }
}
View Requirements:

- Table layout: Date | Description | Amount | Category | Actions
- Amount formatting: negative in red, positive in green, with currency symbol
- Category: name or "Uncategorized" badge
- Actions: Edit category (dropdown), Delete (button with confirmation)
- Pagination bar at bottom
- Empty state with helpful links
- Link to Upload PDF and Add Transaction pages
Manual Add Transaction Page (/Transactions/Add)
csharp
[Authorize]
public class AddTransactionModel : PageModel
{
    private readonly IMediator _mediator;

    [BindProperty]
    public AddTransactionInputModel Input { get; set; } = new();

    public List<CategoryDto> Categories { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

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

            return RedirectToPage("/Transactions/Index")
                .WithSuccess("Transaction added successfully.");
        }
        catch (DomainException ex)
        {
            ErrorMessage = ex.Message;
            return Page();
        }
    }
}
View Requirements:

- Form: Date picker, Description text input, Amount number input, Currency selector (default EUR), Category dropdown
- Client-side validation with Tailwind-styled error messages
- Submit button: "Add Transaction"
- Cancel link → back to transaction list
Category Management Page (/Categories)
csharp
[Authorize]
public class CategoryManagementModel : PageModel
{
    private readonly IMediator _mediator;

    public List<CategoryDto> Categories { get; set; } = new();

    [BindProperty]
    public CreateCategoryInputModel? NewCategory { get; set; }

    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

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

    public async Task<IActionResult> OnPostRenameAsync(Guid categoryId, string newName)
    {
        try
        {
            await _mediator.Send(new RenameCategoryCommand(categoryId, newName));
            SuccessMessage = "Category renamed successfully.";
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
```

**View Requirements:**

- Category list: Name | Color swatch | Icon | System Default badge | Actions
- System defaults: lock icon, edit/delete buttons disabled/hidden
- Create form: inline or modal with Name (required), Color (color picker), Icon (text)
- Rename: inline edit with save/cancel buttons
- Delete: confirmation dialog with warning about active transactions
- Transaction count shown per category

### FR-3.09: Updated Navigation (_Layout.cshtml)

**Authenticated Navigation Items:**

| Label | Route | Icon (optional) |
|---|---|---|
| Dashboard | /Dashboard | 📊 |
| Transactions | /Transactions | 💳 |
| Upload PDF | /Transactions/Upload | 📄 |
| Categories | /Categories | 🏷️ |
| Logout | (POST action) | 🚪 |

### FR-3.10: Infrastructure DI Updates

```csharp
public static IServiceCollection AddInfrastructureServices(
    this IServiceCollection services,
    IConfiguration configuration)
{
    // ... (existing Supabase client + auth from Phase 0/1)

    // Repository implementations (NEW in Phase 3)
    services.AddScoped<ITransactionRepository, SupabaseTransactionRepository>();
    services.AddScoped<ICategoryRepository, SupabaseCategoryRepository>();

    // PDF parsing (NEW in Phase 3)
    services.AddScoped<IPdfParser, GenericBankPdfParser>();
    services.AddSingleton<PdfParserFactory>();

    // Domain services (NEW in Phase 3)
    services.AddScoped<CategoryService>();

    return services;
}
```

## Architecture Notes

### PDF Import Pipeline Flow

```
┌─────────────┐     ┌──────────────────────────────┐     ┌──────────────────┐
│   Frontend   │     │       Application Layer       │     │  Infrastructure  │
│  Upload Page │     │                                │     │                  │
└──────┬───────┘     └──────────────────────────────┘     └──────────────────┘
       │                                                          
       │ POST /Transactions/Upload                                
       │ (IFormFile PdfFile)                                      
       ▼                                                          
┌──────────────┐                                                  
│  PageModel   │                                                  
│  OnPostAsync │                                                  
└──────┬───────┘                                                  
       │ _mediator.Send(ImportTransactionsFromPdfCommand)          
       ▼                                                          
┌────────────────────────────────────────────────────┐            
│  TenantScopingBehavior (verify authenticated)       │            
└──────┬─────────────────────────────────────────────┘            
       ▼                                                          
┌────────────────────────────────────────────────────┐            
│  ImportTransactionsFromPdfCommandHandler             │            
│                                                      │            
│  1. Seed defaults (SeedSystemDefaultsCommand)        │            
│  2. Parse PDF ──────────────────────────────────────┼──► IPdfParser.ParseAsync()
│                                                      │    (GenericBankPdfParser)
│  3. For each RawTransactionRow:                      │            
│     a. Validate (domain rules)                       │            
│     b. Check duplicate ─────────────────────────────┼──► ITransactionRepository
│     c. Create Transaction entity (Domain)            │    .ExistsDuplicateAsync()
│                                                      │            
│  4. Persist valid transactions ─────────────────────┼──► ITransactionRepository
│                                                      │    .AddAsync()
│  5. Save import metadata ───────────────────────────┼──► IPdfImportRepository
│                                                      │    .AddAsync()
│  6. Return ImportResultDto                           │            
└──────────────────────────────────────────────────────┘            
```

### NuGet Packages (Phase 3 Additions)

| Project | New Packages | Notes |
|---|---|---|
| SauronSheet.Domain | None (still zero) | Constitution mandate maintained |
| SauronSheet.Application | No new packages | MediatR already registered in Phase 0 |
| SauronSheet.Infrastructure | UglyToad.PdfPig (Apache 2.0) | PDF text extraction library |
| SauronSheet.Application.Tests | No new packages | xUnit + Moq already available |

### Layer Dependencies (Phase 3 Additions)

| Layer | New Dependencies |
|---|---|
| Domain | None — adds ImportBatch VO only |
| Application | Domain (entities, VOs, repository interfaces, CategoryService) |
| Infrastructure | Domain (implements repository interfaces), PdfPig (PDF parsing) |
| Frontend | Application (MediatR commands/queries), Infrastructure (DI only) |

---

## Test Specifications

### Transaction Command Tests

1. **TEST T-3.01: ImportPdf_ValidPdf_ImportsTransactions**
   - **GIVEN** a PDF stream containing 5 valid transaction rows
   - **AND** `IPdfParser.ParseAsync` returns 5 `RawTransactionRow` objects
   - **AND** `ITransactionRepository.ExistsDuplicateAsync` returns false for all
   - **WHEN** `ImportTransactionsFromPdfCommandHandler` handles the command
   - **THEN** `ImportResultDto.ImportedCount == 5`
   - **AND** `ImportResultDto.SkippedCount == 0`
   - **AND** `ITransactionRepository.AddAsync` called 5 times

2. **TEST T-3.02: ImportPdf_DuplicateTransactions_SkipsDuplicates**
   - **GIVEN** a PDF stream containing 3 transaction rows
   - **AND** `ITransactionRepository.ExistsDuplicateAsync` returns true for 1 row
   - **WHEN** `ImportTransactionsFromPdfCommandHandler` handles the command
   - **THEN** `ImportResultDto.ImportedCount == 2`
   - **AND** `ImportResultDto.SkippedCount == 1`
   - **AND** `ImportResultDto.Errors[0].Reason` contains "Duplicate"

3. **TEST T-3.03: ImportPdf_InvalidRows_ReportsErrors**
   - **GIVEN** a PDF stream containing 4 rows (1 with future date, 1 with empty description)
   - **AND** `IPdfParser.ParseAsync` returns 4 `RawTransactionRow` objects
   - **WHEN** `ImportTransactionsFromPdfCommandHandler` handles the command
   - **THEN** `ImportResultDto.ImportedCount == 2`
   - **AND** `ImportResultDto.SkippedCount == 2`
   - **AND** Errors contain specific reasons per row

4. **TEST T-3.04: ImportPdf_EmptyPdf_ReturnsZeroCounts**
   - **GIVEN** a PDF stream containing no parseable transactions
   - **AND** `IPdfParser.ParseAsync` returns empty list
   - **WHEN** `ImportTransactionsFromPdfCommandHandler` handles the command
   - **THEN** `ImportResultDto.ImportedCount == 0`
   - **AND** `ImportResultDto.SkippedCount == 0`
   - **AND** `ImportResultDto.TotalProcessed == 0`

5. **TEST T-3.05: ImportPdf_NullStream_ThrowsException**
   - **GIVEN** a null PDF stream
   - **WHEN** `ImportTransactionsFromPdfCommandHandler` handles the command
   - **THEN** throws `ArgumentException` or `DomainException` with descriptive message

6. **TEST T-3.06: ImportPdf_SeedsDefaultCategories**
   - **GIVEN** a valid PDF stream
   - **AND** `ICategoryRepository.GetSystemDefaultsAsync` returns empty list (no defaults yet)
   - **WHEN** `ImportTransactionsFromPdfCommandHandler` handles the command
   - **THEN** system default categories are seeded (4 categories created)

7. **TEST T-3.07: CreateTransaction_ValidInput_ReturnsTransactionId**
   - **GIVEN** valid amount, date (yesterday), description "Coffee", no categoryId
   - **WHEN** `CreateTransactionCommandHandler` handles the command
   - **THEN** returns a non-empty Guid (TransactionId)
   - **AND** `ITransactionRepository.AddAsync` called once

8. **TEST T-3.08: CreateTransaction_FutureDate_ThrowsDomainException**
   - **GIVEN** valid amount, date (tomorrow), description "Coffee"
   - **WHEN** `CreateTransactionCommandHandler` handles the command
   - **THEN** throws `DomainException` with message containing "cannot be in the future"
   - **AND** `ITransactionRepository.AddAsync` NOT called

9. **TEST T-3.09: CreateTransaction_WithCategory_ValidatesAndSets**
   - **GIVEN** valid amount, date, description, and a valid categoryId belonging to user
   - **AND** `ICategoryRepository.GetByIdAsync` returns the category
   - **WHEN** `CreateTransactionCommandHandler` handles the command
   - **THEN** returns `TransactionId`
   - **AND** the persisted transaction has the provided `CategoryId`

10. **TEST T-3.10: CreateTransaction_WithInvalidCategory_ThrowsException**
    - **GIVEN** valid amount, date, description, and a categoryId that does not exist
    - **AND** `ICategoryRepository.GetByIdAsync` returns null
    - **WHEN** `CreateTransactionCommandHandler` handles the command
    - **THEN** throws `EntityNotFoundException`

11. **TEST T-3.11: UpdateTransactionCategory_ValidInput_Updates**
    - **GIVEN** an existing transaction owned by user and a valid categoryId
    - **WHEN** `UpdateTransactionCategoryCommandHandler` handles the command
    - **THEN** `ITransactionRepository.UpdateAsync` called once
    - **AND** the transaction's `CategoryId` is updated

12. **TEST T-3.12: UpdateTransactionCategory_WrongUser_ThrowsException**
    - **GIVEN** an existing transaction owned by a DIFFERENT user
    - **WHEN** `UpdateTransactionCategoryCommandHandler` handles the command
    - **THEN** throws `EntityNotFoundException` (tenant isolation — not "forbidden")

13. **TEST T-3.13: UpdateTransactionDescription_ValidInput_Updates**
    - **GIVEN** an existing transaction owned by user and a new description "Updated desc"
    - **WHEN** `UpdateTransactionDescriptionCommandHandler` handles the command
    - **THEN** `ITransactionRepository.UpdateAsync` called once
    - **AND** the transaction's `Description` is "Updated desc"

14. **TEST T-3.14: DeleteTransaction_ValidInput_Removes**
    - **GIVEN** an existing transaction owned by user
    - **WHEN** `DeleteTransactionCommandHandler` handles the command
    - **THEN** `ITransactionRepository.DeleteAsync` called once

15. **TEST T-3.15: DeleteTransaction_WrongUser_ThrowsException**
    - **GIVEN** an existing transaction owned by a DIFFERENT user
    - **WHEN** `DeleteTransactionCommandHandler handles the command
    - **THEN** throws `EntityNotFoundException`

16. **TEST T-3.16: DeleteTransaction_NonExistent_ThrowsException**
    - **GIVEN** a `TransactionId` that does not exist
    - **AND** `ITransactionRepository.GetByIdAsync` returns null
    - **WHEN** `DeleteTransactionCommandHandler` handles the command
    - **THEN** throws `EntityNotFoundException`

text

### Transaction Query Tests

1. **TEST T-3.17: GetTransactions_ReturnsOnlyUserTransactions**
   - **GIVEN** 5 transactions for user A and 3 transactions for user B
   - **AND** current user is user A
   - **WHEN** `GetTransactionsQueryHandler` handles the query
   - **THEN** result contains exactly 5 transactions
   - **AND** all have `UserId` matching user A

2. **TEST T-3.18: GetTransactions_Paginated_RespectsPageSize**
   - **GIVEN** 100 transactions for current user
   - **AND** query with `Page = 1, PageSize = 50`
   - **WHEN** `GetTransactionsQueryHandler` handles the query
   - **THEN** `result.Items.Count == 50`
   - **AND** `result.TotalCount == 100`
   - **AND** `result.TotalPages == 2`

3. **TEST T-3.19: GetTransactions_SortedByDateDescending**
   - **GIVEN** transactions with dates Jan 1, Jan 15, Jan 10
   - **WHEN** `GetTransactionsQueryHandler` handles the query
   - **THEN** result items are ordered: Jan 15, Jan 10, Jan 1

4. **TEST T-3.20: GetTransactions_EmptyResult_ReturnsEmptyList**
   - **GIVEN** no transactions for current user
   - **WHEN** `GetTransactionsQueryHandler` handles the query
   - **THEN** `result.Items` is empty
   - **AND** `result.TotalCount == 0`

5. **TEST T-3.21: GetTransactionById_Exists_ReturnsDto**
   - **GIVEN** an existing transaction owned by current user
   - **WHEN** `GetTransactionByIdQueryHandler` handles the query
   - **THEN** returns `TransactionDto` with correct properties

6. **TEST T-3.22: GetTransactionById_WrongUser_ThrowsException**
   - **GIVEN** an existing transaction owned by a DIFFERENT user
   - **WHEN** `GetTransactionByIdQueryHandler` handles the query
   - **THEN** throws `EntityNotFoundException`

text

### Category Command Tests

1. **TEST T-3.23: CreateCategory_ValidInput_ReturnsCategoryId**
   - **GIVEN** name = "Entertainment", no existing category with that name
   - **AND** `ICategoryRepository.FindByNameAndUserAsync` returns null
   - **WHEN** `CreateCategoryCommandHandler` handles the command
   - **THEN** returns a non-empty Guid (CategoryId)
   - **AND** `ICategoryRepository.AddAsync` called once

2. **TEST T-3.24: CreateCategory_DuplicateName_ThrowsDomainException**
   - **GIVEN** name = "Groceries" (already exists for user)
   - **AND** `ICategoryRepository.FindByNameAndUserAsync` returns existing category
   - **WHEN** `CreateCategoryCommandHandler` handles the command
   - **THEN** throws `DomainException` with message containing "already exists"

3. **TEST T-3.25: RenameCategory_ValidInput_Renames**
   - **GIVEN** an existing user-defined category owned by user with name "Old"
   - **AND** new name "New" does not exist for this user
   - **WHEN** `RenameCategoryCommandHandler` handles the command
   - **THEN** `ICategoryRepository.UpdateAsync` called once
   - **AND** the category's `Name` is "New"

4. **TEST T-3.26: RenameCategory_SystemDefault_ThrowsDomainException**
   - **GIVEN** a system default category (`IsSystemDefault = true`)
   - **WHEN** `RenameCategoryCommandHandler` handles the command
   - **THEN** throws `DomainException` with message containing "Cannot rename a system default"

5. **TEST T-3.27: DeleteCategory_NoTransactions_Deletes**
   - **GIVEN** a user-defined category with no active transactions
   - **AND** `ICategoryRepository.HasTransactionsAsync` returns false
   - **WHEN** `DeleteCategoryCommandHandler` handles the command
   - **THEN** `ICategoryRepository.DeleteAsync` called once

6. **TEST T-3.28: DeleteCategory_WithTransactions_ThrowsDomainException**
   - **GIVEN** a user-defined category with active transactions
   - **AND** `ICategoryRepository.HasTransactionsAsync` returns true
   - **WHEN** `DeleteCategoryCommandHandler` handles the command
   - **THEN** throws `DomainException` with message containing "has active transactions"

7. **TEST T-3.29: DeleteCategory_SystemDefault_ThrowsDomainException**
   - **GIVEN** a system default category
   - **WHEN** `DeleteCategoryCommandHandler` handles the command
   - **THEN** throws `DomainException` (`CanDelete` returns false)

text

### Category Query Tests

1. **TEST T-3.30: GetCategories_IncludesSystemDefaults**
   - **GIVEN** 4 system defaults + 2 user-defined categories for current user
   - **WHEN** `GetCategoriesQueryHandler` handles the query
   - **THEN** result contains 6 categories
   - **AND** 4 have `IsSystemDefault == true`

2. **TEST T-3.31: GetCategories_SortsSystemDefaultsFirst**
   - **GIVEN** system defaults + user-defined categories
   - **WHEN** `GetCategoriesQueryHandler` handles the query
   - **THEN** system defaults appear before user-defined in the list

3. **TEST T-3.32: SeedSystemDefaults_FirstTime_CreatesFour**
   - **GIVEN** no system defaults exist for current user
   - **AND** `ICategoryRepository.GetSystemDefaultsAsync` returns empty list
   - **WHEN** `SeedSystemDefaultsCommandHandler` handles the command
   - **THEN** `ICategoryRepository.AddAsync` called 4 times
   - **AND** returns list of 4 Guids

4. **TEST T-3.33: SeedSystemDefaults_AlreadyExist_ReturnsExisting**
   - **GIVEN** 4 system defaults already exist for current user
   - **AND** `ICategoryRepository.GetSystemDefaultsAsync` returns 4 categories
   - **WHEN** `SeedSystemDefaultsCommandHandler` handles the command
   - **THEN** `ICategoryRepository.AddAsync` NOT called (idempotent)
   - **AND** returns list of 4 existing Guids

text

### Domain Addition Tests

1. **TEST T-3.34: ImportBatch_ValidConstruction_SetsProperties**
   - **GIVEN** `filename = "bank.pdf"`, `importedCount = 5`, `skippedCount = 2`, `importedAt = now`
   - **WHEN** `ImportBatch` is constructed
   - **THEN** all properties match
   - **AND** `TotalProcessed == 7`

2. **TEST T-3.35: ImportBatch_EmptyFilename_ThrowsDomainException**
   - **GIVEN** `filename = ""`
   - **WHEN** `ImportBatch` is constructed
   - **THEN** throws `DomainException` with message containing "Filename is required"

3. **TEST T-3.36: ImportBatch_NegativeImportedCount_ThrowsDomainException**
   - **GIVEN** `importedCount = -1`
   - **WHEN** `ImportBatch` is constructed
   - **THEN** throws `DomainException` with message containing "cannot be negative"

4. **TEST T-3.37: ImportBatch_NegativeSkippedCount_ThrowsDomainException**
   - **GIVEN** `skippedCount = -1`
   - **WHEN** `ImportBatch` is constructed
   - **THEN** throws `DomainException` with message containing "cannot be negative"

5. **TEST T-3.38: ImportBatch_ToString_FormatsCorrectly**
   - **GIVEN** `filename = "bank.pdf"`, `importedCount = 5`, `skippedCount = 2`
   - **WHEN** `ToString()` is called
   - **THEN** returns string containing "bank.pdf", "5 imported", "2 skipped"

text

---

## Test Summary

| Test ID | Test Name                                                   | Category    | Area                |
|---------|-------------------------------------------------------------|-------------|---------------------|
| T-3.01  | ImportPdf_ValidPdf_ImportsTransactions                      | Application | Import Pipeline     |
| T-3.02  | ImportPdf_DuplicateTransactions_SkipsDuplicates             | Application | Import Pipeline     |
| T-3.03  | ImportPdf_InvalidRows_ReportsErrors                         | Application | Import Pipeline     |
| T-3.04  | ImportPdf_EmptyPdf_ReturnsZeroCounts                        | Application | Import Pipeline     |
| T-3.05  | ImportPdf_NullStream_ThrowsException                        | Application | Import Pipeline     |
| T-3.06  | ImportPdf_SeedsDefaultCategories                            | Application | Import Pipeline     |
| T-3.07  | CreateTransaction_ValidInput_ReturnsTransactionId           | Application | Transaction CRUD    |
| T-3.08  | CreateTransaction_FutureDate_ThrowsDomainException          | Application | Transaction CRUD    |
| T-3.09  | CreateTransaction_WithCategory_ValidatesAndSets             | Application | Transaction CRUD    |
| T-3.10  | CreateTransaction_WithInvalidCategory_ThrowsException       | Application | Transaction CRUD    |
| T-3.11  | UpdateTransactionCategory_ValidInput_Updates                | Application | Transaction CRUD    |
| T-3.12  | UpdateTransactionCategory_WrongUser_ThrowsException         | Application | Transaction CRUD    |
| T-3.13  | UpdateTransactionDescription_ValidInput_Updates             | Application | Transaction CRUD    |
| T-3.14  | DeleteTransaction_ValidInput_Removes                        | Application | Transaction CRUD    |
| T-3.15  | DeleteTransaction_WrongUser_ThrowsException                 | Application | Transaction CRUD    |
| T-3.16  | DeleteTransaction_NonExistent_ThrowsException               | Application | Transaction CRUD    |
| T-3.17  | GetTransactions_ReturnsOnlyUserTransactions                 | Application | Transaction Queries |
| T-3.18  | GetTransactions_Paginated_RespectsPageSize                  | Application | Transaction Queries |
| T-3.19  | GetTransactions_SortedByDateDescending                      | Application | Transaction Queries |
| T-3.20  | GetTransactions_EmptyResult_ReturnsEmptyList                | Application | Transaction Queries |
| T-3.21  | GetTransactionById_Exists_ReturnsDto                        | Application | Transaction Queries |
| T-3.22  | GetTransactionById_WrongUser_ThrowsException                | Application | Transaction Queries |
| T-3.23  | CreateCategory_ValidInput_ReturnsCategoryId                 | Application | Category CRUD       |
| T-3.24  | CreateCategory_DuplicateName_ThrowsDomainException          | Application | Category CRUD       |
| T-3.25  | RenameCategory_ValidInput_Renames                           | Application | Category CRUD       |
| T-3.26  | RenameCategory_SystemDefault_ThrowsDomainException          | Application | Category CRUD       |
| T-3.27  | DeleteCategory_NoTransactions_Deletes                       | Application | Category CRUD       |
| T-3.28  | DeleteCategory_WithTransactions_ThrowsDomainException       | Application | Category CRUD       |
| T-3.29  | DeleteCategory_SystemDefault_ThrowsDomainException          | Application | Category CRUD       |
| T-3.30  | GetCategories_IncludesSystemDefaults                        | Application | Category Queries    |
| T-3.31  | GetCategories_SortsSystemDefaultsFirst                      | Application | Category Queries    |
| T-3.32  | SeedSystemDefaults_FirstTime_CreatesFour                    | Application | System Defaults     |
| T-3.33  | SeedSystemDefaults_AlreadyExist_ReturnsExisting             | Application | System Defaults     |
| T-3.34  | ImportBatch_ValidConstruction_SetsProperties                | Domain      | ImportBatch VO      |
| T-3.35  | ImportBatch_EmptyFilename_ThrowsDomainException             | Domain      | ImportBatch VO      |
| T-3.36  | ImportBatch_NegativeImportedCount_ThrowsDomainException     | Domain      | ImportBatch VO      |
| T-3.37  | ImportBatch_NegativeSkippedCount_ThrowsDomainException      | Domain      | ImportBatch VO      |
| T-3.38  | ImportBatch_ToString_FormatsCorrectly                       | Domain      | ImportBatch VO      |

**Total: 38 tests (33 Application + 5 Domain)**

**Tests by Area:**

| Area                | Test Count | Test IDs                     |
|---------------------|------------|------------------------------|
| Import Pipeline     | 6          | T-3.01–T-3.06               |
| Transaction CRUD    | 10         | T-3.07–T-3.16               |
| Transaction Queries | 6          | T-3.17–T-3.22               |
| Category CRUD       | 7          | T-3.23–T-3.29               |
| Category Queries    | 2          | T-3.30–T-3.31               |
| System Defaults     | 2          | T-3.32–T-3.33               |
| ImportBatch VO      | 5          | T-3.34–T-3.38               |

---

## Deliverables

| #      | Deliverable                                                        | Layer          | Acceptance                                                                 |
|--------|--------------------------------------------------------------------|----------------|----------------------------------------------------------------------------|
| D-3.01 | `ImportTransactionsFromPdfCommand` + handler                       | Application    | Tests T-3.01–T-3.06 pass                                                  |
| D-3.02 | `CreateTransactionCommand` + handler                               | Application    | Tests T-3.07–T-3.10 pass                                                  |
| D-3.03 | `UpdateTransactionCategoryCommand` + handler                       | Application    | Tests T-3.11–T-3.12 pass                                                  |
| D-3.04 | `UpdateTransactionDescriptionCommand` + handler                    | Application    | Test T-3.13 passes                                                         |
| D-3.05 | `DeleteTransactionCommand` + handler                               | Application    | Tests T-3.14–T-3.16 pass                                                  |
| D-3.06 | `GetTransactionsQuery` + handler                                   | Application    | Tests T-3.17–T-3.20 pass                                                  |
| D-3.07 | `GetTransactionByIdQuery` + handler                                | Application    | Tests T-3.21–T-3.22 pass                                                  |
| D-3.08 | `CreateCategoryCommand` + handler                                  | Application    | Tests T-3.23–T-3.24 pass                                                  |
| D-3.09 | `RenameCategoryCommand` + handler                                  | Application    | Tests T-3.25–T-3.26 pass                                                  |
| D-3.10 | `DeleteCategoryCommand` + handler                                  | Application    | Tests T-3.27–T-3.29 pass                                                  |
| D-3.11 | `GetCategoriesQuery` + handler                                     | Application    | Tests T-3.30–T-3.31 pass                                                  |
| D-3.12 | `SeedSystemDefaultsCommand` + handler                              | Application    | Tests T-3.32–T-3.33 pass                                                  |
| D-3.13 | DTOs: TransactionDto, ImportResultDto, ImportRowErrorDto, PaginatedResultDto, CategoryDto | Application | Compile; used by handlers and frontend |
| D-3.14 | `IPdfParser` interface + `RawTransactionRow` model                 | Application    | Contract defined; used by import handler                                   |
| D-3.15 | `ImportBatch` value object                                         | Domain         | Tests T-3.34–T-3.38 pass                                                  |
| D-3.16 | `GenericBankPdfParser` implementation                              | Infrastructure | Parses sample PDF correctly; returns RawTransactionRow list                |
| D-3.17 | `PdfParserFactory` (strategy pattern)                              | Infrastructure | Returns GenericBankPdfParser as default                                    |
| D-3.18 | `SupabaseTransactionRepository`                                    | Infrastructure | Implements all `ITransactionRepository` methods                            |
| D-3.18 | `SupabaseTransactionRepository`                                    | Infrastructure | Implements all `ITransactionRepository` methods                            |
| D-3.19 | `SupabaseCategoryRepository`                                       | Infrastructure | Implements all `ICategoryRepository` methods                               |
| D-3.20 | `SupabasePdfImportRepository`                                      | Infrastructure | Tracks import batch metadata                                               |
| D-3.21 | Database migration: `002_CreateCategoriesTable.sql`                | Infrastructure | Table + indexes + RLS policies applied to Supabase                         |
| D-3.22 | Database migration: `003_CreateTransactionsTable.sql`              | Infrastructure | Table + indexes + RLS policies applied to Supabase                         |
| D-3.23 | Database migration: `004_CreatePdfImportsTable.sql`                | Infrastructure | Table + indexes + RLS policies applied to Supabase                         |
| D-3.24 | Upload PDF page (`/Transactions/Upload`)                           | Frontend       | File upload → import results display                                       |
| D-3.25 | Transaction list page (`/Transactions`)                            | Frontend       | Paginated, sorted, category shown, delete action                           |
| D-3.26 | Manual add transaction page (`/Transactions/Add`)                  | Frontend       | Form → validation → redirect to list                                       |
| D-3.27 | Category management page (`/Categories`)                           | Frontend       | Create, rename, delete with system default protection                      |
| D-3.28 | Updated `_Layout.cshtml` navigation                                | Frontend       | New nav links: Transactions, Upload, Categories                            |
| D-3.29 | Updated Infrastructure `DependencyInjection.cs`                    | Infrastructure | Repositories + PDF parser + CategoryService registered                     |
| D-3.30 | Domain.Tests for ImportBatch (5 tests)                             | Tests          | `dotnet test --filter Category=Domain` all green                           |
| D-3.31 | Application.Tests for handlers (33 tests)                          | Tests          | `dotnet test --filter Category=Application` all green                      |

---

## Success Criteria

| #      | Criterion                                                                          | Metric                                                                |
|--------|------------------------------------------------------------------------------------|-----------------------------------------------------------------------|
| SC-3.1 | User can upload a PDF and see imported transactions                                | E2E: upload PDF → transaction list shows imported rows                |
| SC-3.2 | Duplicate transactions are detected and not re-imported                             | Upload same PDF twice → second time: 0 imported, N skipped           |
| SC-3.3 | Invalid rows in PDF are reported with per-row reasons                              | Import result shows specific error per skipped row                   |
| SC-3.4 | Manual transaction creation works end-to-end                                       | Add form → submit → appears in transaction list                      |
| SC-3.5 | Transaction category can be updated                                                | Change category dropdown → persisted → refresh shows new category    |
| SC-3.6 | Transaction description can be updated                                             | Edit description → persisted → refresh shows new description         |
| SC-3.7 | Transactions can be deleted with confirmation                                      | Delete button → confirm → removed from list and database             |
| SC-3.8 | Categories can be created, renamed, and deleted (with guards)                      | System defaults protected; user-defined CRUD works                   |
| SC-3.9 | System default categories seeded automatically (idempotent)                        | First interaction creates 4 defaults; subsequent calls are no-ops    |
| SC-3.10 | All data scoped to current user (tenant isolation)                                 | User A never sees User B's transactions or categories                |
| SC-3.11 | Transaction list is paginated (50 per page default)                                | 100+ transactions → pagination controls visible and functional       |
| SC-3.12 | Application layer test coverage ≥ 70%                                              | coverlet report on Application project                               |
| SC-3.13 | Domain test coverage ≥ 80%                                                         | coverlet report on Domain project (cumulative)                       |
| SC-3.14 | All Phase 3 tests pass (38 tests)                                                  | `dotnet test` all green                                              |
| SC-3.15 | All prior phase tests still pass (no regressions)                                  | `dotnet test` → Phase 0 + 1 + 2 + 3 all green                       |
| SC-3.16 | RLS policies verified: two users cannot see each other's data                      | Manual test with two Supabase users                                  |
| SC-3.17 | PDF file size limit enforced (10MB)                                                | Upload 11MB file → rejected with error message                       |
| SC-3.18 | Empty state pages display helpful messages with action links                       | No transactions → "No transactions yet" with Import/Add links        |

---

## Assumptions

1. **Phases 0, 1, and 2 are fully implemented and tested.** All base abstractions, auth, domain model, entities, value objects, services, and repository interfaces are available and stable.
2. **PdfPig library (`UglyToad.PdfPig`) is available via NuGet.** Apache 2.0 license is compatible with the project.
3. **A sample PDF bank statement is available for testing.** At minimum, a mock PDF or test fixture with known transaction data is used for integration tests.
4. **GenericBankPdfParser handles a reasonable "generic" bank statement format.** Specific bank format support is deferred to post-MVP. The parser uses heuristics (date patterns, amount patterns) to extract rows.
5. **Supabase Postgrest client supports all required CRUD operations.** If the client has limitations for specific queries (e.g., `ExistsDuplicateAsync`), raw SQL via Supabase RPC functions is acceptable.
6. **Frontend uses Tailwind CSS CDN** (same as Phase 0). No build pipeline changes.
7. **Alpine.js is introduced in this phase** for interactive components (confirmation dialogs, inline edit, dropdown selectors). Added via CDN in `_Layout.cshtml`.
8. **Pagination is implemented at the Application handler level**, not via Supabase-specific pagination features (to keep handlers testable with mocked repositories).
9. **Import batch metadata is stored in `pdf_imports` table** for audit trail purposes. It does not affect transaction functionality.
10. **Category `TransactionCount` in `CategoryDto` is calculated by querying the transaction table**, not stored as a denormalized field. This is acceptable for MVP performance.
11. **`IPdfParser` is defined in the Application layer** (not Domain) because PDF parsing is a use-case concern, not a domain concept. The interface is implemented in Infrastructure.
12. **No background job processing.** PDF import is synchronous within the HTTP request. For very large PDFs, this may cause timeout — mitigated by the 10MB file size limit.

---

## Risks & Mitigations

| ID    | Risk                                                               | Impact | Probability | Mitigation                                                                                  |
|-------|--------------------------------------------------------------------|--------|-------------|---------------------------------------------------------------------------------------------|
| R-3.1 | PDF parsing produces inconsistent results across bank formats      | High   | High        | Start with one generic format; strategy pattern allows adding bank-specific parsers later    |
| R-3.2 | Large PDF upload causes HTTP timeout                               | Medium | Medium      | 10MB file size limit; synchronous processing acceptable for MVP; async pipeline post-MVP    |
| R-3.3 | Supabase Postgrest client lacks `ExistsDuplicateAsync` equivalent  | Medium | Medium      | Implement via Supabase RPC function or composite query; wrap in repository                  |
| R-3.4 | Duplicate detection hash collisions (different transactions match) | Low    | Low         | Hash uses 4 fields (userId, date, amount, description); false positives extremely unlikely  |
| R-3.5 | RLS policies block legitimate operations                           | Medium | Medium      | Test all CRUD operations with authenticated user; verify policies match expected access      |
| R-3.6 | Alpine.js CDN conflicts with existing JavaScript                   | Low    | Low         | Minimal JS in Phase 0-2; Alpine.js is self-contained                                       |
| R-3.7 | Category seeding race condition (concurrent requests)              | Low    | Low         | `UNIQUE(user_id, name)` constraint prevents duplicate defaults; idempotent check in handler |
| R-3.8 | Pagination logic incorrect (off-by-one, wrong total)               | Low    | Medium      | Unit tests verify pagination math; boundary tests (page 0, last page, empty)                |
| R-3.9 | PdfPig library has unhandled PDF formats/encodings                 | Medium | Medium      | Wrap in try-catch; return empty list with error message for unparseable PDFs                 |

---

## Implementation Notes

### Recommended Implementation Order

1. **Step 1: Write Domain.Tests for ImportBatch (RED phase)**
   - Tests T-3.34–T-3.38
   - Verify: tests FAIL (red)

2. **Step 2: Implement ImportBatch value object (GREEN phase)**
   - `Domain/ValueObjects/ImportBatch.cs`
   - Verify: `dotnet test --filter Category=Domain` — new tests GREEN

3. **Step 3: Write Application.Tests for Category commands/queries (RED phase)**
   - Tests T-3.23–T-3.33
   - Mock: `ICategoryRepository`, `IUserContext`, `CategoryService`
   - Verify: tests FAIL (red)

4. **Step 4: Implement Category commands/queries + handlers (GREEN phase)**
   - `CreateCategoryCommand`, `RenameCategoryCommand`, `DeleteCategoryCommand`
   - `GetCategoriesQuery`, `SeedSystemDefaultsCommand`
   - Verify: `dotnet test --filter Category=Application` — category tests GREEN

5. **Step 5: Write Application.Tests for Transaction commands (RED phase)**
   - Tests T-3.07–T-3.16
   - Mock: `ITransactionRepository`, `ICategoryRepository`, `IUserContext`
   - Verify: tests FAIL (red)

6. **Step 6: Implement Transaction commands + handlers (GREEN phase)**
   - `CreateTransactionCommand`, `UpdateTransactionCategoryCommand`
   - `UpdateTransactionDescriptionCommand`, `DeleteTransactionCommand`
   - Verify: `dotnet test --filter Category=Application` — transaction command tests GREEN

7. **Step 7: Write Application.Tests for Transaction queries (RED phase)**
   - Tests T-3.17–T-3.22
   - Verify: tests FAIL (red)

8. **Step 8: Implement Transaction queries + handlers (GREEN phase)**
   - `GetTransactionsQuery`, `GetTransactionByIdQuery`
   - `PaginatedResultDto` logic
   - Verify: `dotnet test --filter Category=Application` — query tests GREEN

9. **Step 9: Write Application.Tests for Import pipeline (RED phase)**
   - Tests T-3.01–T-3.06
   - Mock: `IPdfParser`, `ITransactionRepository`, `ICategoryRepository`, `IUserContext`
   - Verify: tests FAIL (red)

10. **Step 10: Implement Import pipeline handler (GREEN phase)**
    - `ImportTransactionsFromPdfCommand` + handler
    - `ImportResultDto`, `ImportRowErrorDto`
    - Verify: `dotnet test --filter Category=Application` — import tests GREEN

11. **Step 11: Apply database migrations to Supabase**
    - Run `002_CreateCategoriesTable.sql`
    - Run `003_CreateTransactionsTable.sql`
    - Run `004_CreatePdfImportsTable.sql`
    - Verify: tables + indexes + RLS policies in Supabase dashboard

12. **Step 12: Implement Infrastructure repositories**
    - `SupabaseTransactionRepository`
    - `SupabaseCategoryRepository`
    - `SupabasePdfImportRepository`
    - Verify: manual CRUD tests against Supabase

13. **Step 13: Implement PDF parser**
    - `GenericBankPdfParser` (PdfPig integration)
    - `PdfParserFactory`
    - Verify: parse sample PDF → correct `RawTransactionRow` list

14. **Step 14: Update Infrastructure DependencyInjection.cs**
    - Register repositories, PDF parser, `CategoryService`
    - Verify: `dotnet build` succeeds

15. **Step 15: Implement Frontend — Category management page**
    - `/Categories` — CRUD with system default protection
    - Verify: manual E2E test

16. **Step 16: Implement Frontend — Transaction pages**
    - `/Transactions` — paginated list
    - `/Transactions/Add` — manual add form
    - `/Transactions/Upload` — PDF upload + results
    - Verify: manual E2E test

17. **Step 17: Update _Layout.cshtml navigation**
    - Add links: Transactions, Upload, Categories
    - Add Alpine.js CDN to layout
    - Verify: navigation works on all pages

18. **Step 18: End-to-end validation**
    - Register user → seed categories → upload PDF → view transactions
    - Manually add transaction → categorize → delete
    - Create custom category → rename → delete
    - Verify duplicate detection (upload same PDF twice)
    - Verify tenant isolation (two users cannot see each other's data)
    - Verify RLS policies in Supabase

19. **Step 19: Final test + coverage validation**
    - `dotnet build` → zero errors, zero warnings
    - `dotnet test` → ALL tests green (Phase 0 + 1 + 2 + 3)
    - Domain coverage ≥ 80% (cumulative)
    - Application coverage ≥ 70%
    - Audit: no forbidden layer references (no upward references)

text

### Spec-Driven Workflow Compliance

| Step | Workflow Stage        | Phase 3 Action                                                       |
|------|-----------------------|----------------------------------------------------------------------|
| 1    | Write Test Spec       | ✅ Tests written first (Steps 1, 3, 5, 7, 9)                         |
| 2    | Define Handler Stub   | ✅ MediatR commands/queries defined (Steps 4, 6, 8, 10)              |
| 3    | Build Domain          | ✅ ImportBatch VO (Step 2)                                            |
| 4    | Implement Persistence | ✅ Supabase repositories + migrations (Steps 11, 12)                 |
| 5    | Wire UI               | ✅ Frontend pages for transactions and categories (Steps 15, 16, 17) |
| 6    | End-to-end Test       | ✅ Full pipeline validation (Step 18)                                 |

### Testing Patterns Used in This Phase

| Pattern                  | Description                                                       | Example                                                     |
|--------------------------|-------------------------------------------------------------------|-------------------------------------------------------------|
| Handler Test (Mocked)    | Mock all repository interfaces; test handler logic in isolation   | `Mock<ITransactionRepository>` → verify `AddAsync` called   |
| Tenant Isolation Test    | Verify handler throws when entity belongs to different user       | Load transaction with userId ≠ current user → EntityNotFound |
| Idempotency Test         | Verify repeated operation has same result                         | `SeedSystemDefaults` twice → still only 4 categories        |
| Pagination Boundary Test | Verify page math with edge cases                                  | 0 items, 1 item, exactly PageSize items, PageSize + 1 items |
| Error Aggregation Test   | Verify import handler collects per-row errors without stopping    | 4 rows, 2 invalid → 2 imported + 2 errors with reasons     |
| Guard Propagation Test   | Verify domain guards propagate through handler as DomainException | Future date → handler throws DomainException (from entity)  |

### Alpine.js Introduction

This phase introduces Alpine.js via CDN for the following interactive components:

| Component           | Page                      | Alpine.js Feature Used             |
|---------------------|---------------------------|------------------------------------|
| Delete confirmation | Transaction list          | `x-data`, `x-show` for modal       |
| Inline category edit | Category management       | `x-data`, `x-model`, `x-on:click`  |
| Category dropdown   | Transaction list, Add form | `x-data`, `x-show`, `x-on:click`   |
| Import result expand | Upload page               | `x-data`, `x-show` for error list  |
| File drop zone      | Upload page               | `x-on:dragover`, `x-on:drop`       |

**Layout Addition:**

```html
<!-- In _Layout.cshtml <head> section, after Tailwind CDN -->
<script defer src="https://cdn.jsdelivr.net/npm/alpinejs@3.x.x/dist/cdn.min.js"></script>
IPdfImportRepository (Infrastructure Only)
Note: This repository does NOT have a Domain interface because pdf_imports is an infrastructure/audit concern, not a domain entity. It is used directly by the import handler for tracking purposes.

csharp
// Defined in Infrastructure layer (not Domain)
public interface IPdfImportRepository
{
    Task AddAsync(Guid userId, string filename, int importedCount, int skippedCount);
    Task<IReadOnlyList<PdfImportRecord>> GetByUserIdAsync(Guid userId);
}

public record PdfImportRecord(
    Guid Id,
    Guid UserId,
    string Filename,
    int ImportedCount,
    int SkippedCount,
    DateTime ImportedAt
);
Registration:

csharp
// In Infrastructure DependencyInjection.cs
services.AddScoped<IPdfImportRepository, SupabasePdfImportRepository>();
## Security Considerations

- ☑️ PDF upload validated: file type (.pdf extension + MIME type), file size (≤ 10MB)
- ☑️ All transaction/category operations verify `UserId` matches current user (handler-level tenant isolation)
- ☑️ RLS policies on all 3 new tables (belt-and-suspenders with handler-level checks)
- ☑️ ON DELETE SET NULL for `category_id` FK — deleting a category doesn't delete transactions
- ☑️ ON DELETE CASCADE for `user_id` FK — deleting a user cleans up all their data
- ☑️ Unique constraint `(user_id, name)` on categories prevents duplicate names at DB level
- ☑️ Duplicate transaction index `(user_id, date, amount, description)` supports efficient duplicate checks
- ☑️ No Supabase service key in frontend — only anon key (RLS enforces access)
- ☑️ Alpine.js confirmation dialog prevents accidental deletes
- ☑️ File stream disposed after parsing (using statement in handler)
Cumulative Test Count (Phases 0–3)

| Phase   | Domain Tests | Application Tests | Total Phase | Cumulative Total |
|---------|--------------|-------------------|-------------|------------------|
| Phase 0 | 11           | 2                 | 13          | 13               |
| Phase 1 | 8            | 14                | 22          | 35               |
| Phase 2 | 81           | 0                 | 81          | 116              |
| Phase 3 | 5            | 33                | 38          | 154              |
Phase Spec Version: 1.0.0 | Created: 2026-02-15 | Aligned with Constitution v1.1.0