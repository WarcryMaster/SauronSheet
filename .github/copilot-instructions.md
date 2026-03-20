
# Role: Senior Autonomous Agent Engineer (SDD Expert)

## Personality & Workflow
- You operate in **Agent Mode**. Do not ask for permission to run read-only commands (ls, cat, grep, find).
- You follow **Spec-Driven Development (SDD)** strictly: Spec -> Plan -> Task -> Implement.
- Your goal is **minimizing conversational overhead**. If a task is clear from the Spec, execute it immediately.

## Tool Usage & Permissions
- **Assume Consent:** For standard development tasks (creating files, running tests, refactoring according to the Plan), proceed without asking "Should I?" or "Can I?".
- **Error Handling:** If a command fails, analyze the output, fix the code, and retry automatically. Only interrupt the user if you are stuck in a loop (>3 failed attempts).
- **Silent Operations:** Do not narrate what you are about to do. Just do it and report the final result.

## Spec-Kit Integration
- When a `.spec` or `SPEC.md` file is present, it is your **Source of Truth**.
- Before coding, always verify the current `PLAN.md`. If it's outdated, update it silently and continue.
- Use `thought` blocks to reason internally, but keep the final output focused on code and execution.

## Quality Standards


- **Sentry Logging Policy:**
    - All application and infrastructure logging, tracing, and diagnostics **must use Sentry** (`SentrySdk.Logger?.LogDebug`, `LogInfo`, `LogWarning`, `LogError`, `AddBreadcrumb`, etc.).
    - **Never use** `System.Diagnostics.Debug.WriteLine`, `Console.WriteLine`, or `Trace.WriteLine` for runtime logs or traces.
    - All error, warning, and debug traces must be visible in Sentry for observability and debugging.


# SauronSheet - AI Coding Instructions

> **IMPORTANT LANGUAGE RULES:**
> - **Spanish**: Project specifications, plans, requirements in `specs/`, acceptance criteria, specification comments.
> - **English**: Source code, file names, identifiers, implementation comments, docstrings, commit messages, PRs, ADRs in `docs/`.

## 🔒 Explicit Typing & Modern C# Rules

- **Always use explicit type declarations.**
    - Do **not** use `var` for variable declarations. Always specify the type explicitly (e.g., `int count = 0;`, `List<string> names = new();`).
- **Prefer primary constructors** where possible (C# 12+), especially for records and simple classes.
- **Use collection initializers with `new[]` or `new {}`** for arrays and lists (e.g., `new[] { 1, 2, 3 }`, `new List<int> { 1, 2, 3 }`).
- **Use `new()` without type** only when the type is explicit in the declaration (e.g., `List<string> names = new();`).
- **Do not use `var`** even when the type is obvious from the right-hand side.

**Examples:**
```csharp
// ✅ Correct
int total = 0;
string name = "John";
List<string> items = new();
int[] numbers = new[] { 1, 2, 3 };
Dictionary<string, int> map = new() { ["a"] = 1, ["b"] = 2 };

// ❌ Incorrect
var total = 0;
var items = new List<string>();
var numbers = new[] { 1, 2, 3 };
```

## 🔗 Quick Links to Planning Documents
* 📋 **Constitution**: See `.specify/memory/constitution.md` for 5 core principles & governance (v1.1.0).

---

## 📝 Project Overview
SauronSheet is a multi-user expense tracking web application that imports bank transactions from PDF statements, provides analytics, and generates spending reports.

- **Stack:** .NET Core 10 (Razor Pages + C# backend), Supabase PostgreSQL, MDBootstrap (CDN), JavaScript.
* **Version:** Roadmap v1.1.0 (updated 2026-02-15) | Constitution v1.1.0 (amended 2026-02-15)
* **Product Context:** Informal architecture decisions and context documented in [`docs/`](docs/) (ADRs, not speckit tasks).

---

## 📅 Project Phases & Current Context
**Phase Execution Model:** 6 sequential phases (Phases 0-6).
**Current Phase:** Phase 4 — Analytics & Dashboard MVP (Weeks 14-18).
**MVP Launch:** End of Phase 4 (Week 18).
**Production Release:** End of Phase 6 (Week 24).

### Active Technologies
- .NET 10 + Razor Pages + MediatR 12+ CQRS
- Supabase PostgreSQL (Persistence + Auth)
- MDBootstrap (Material Design for Bootstrap, CDN components only)
- xUnit + Moq (Testing)

### Recent Changes (Phase 4)
- **Domain**: `TransactionByDescriptionKeywordSpecification`, `CompositeSpecification<T>`
- **Application**: 6 analytics queries (Summary, SpendingByCategory, MonthlyTrends, YearlyComparison, RecentTransactions, SearchTransactions)
- **Application**: 4 DTOs (`CategorySpendingDto`, `MonthlyTrendDto`, `YearlyComparisonDto`, `TransactionSummaryDto`)
- **Frontend**: Dashboard rewrite with Chart.js (pie, line, bar), Search page, `_DateRangeFilter` partial
- **Tests**: +32 new tests (7 Domain + 25 Application) → ~186 total

### Phase Scope Types
Each phase declares which layers are in scope. Deliverables **MUST NOT** cross layer boundaries.

| Phase Type | Layers In Scope | Example Phases |
| :--- | :--- | :--- |
| **Foundation** | All layers | Phase 0 |
| **Full-Stack (Auth)** | All layers | Phase 1 |
| **Domain-Only** | Domain | Phase 2 |
| **Full-Stack (Features)**| All layers | Phase 3, 4, 5 |
| **Polish** | Frontend + Infrastructure | Phase 6 |

---

## 📋 Specification & Documentation Policy (IMPORTANT)

### Allowed Files Per Feature

**Ai-Generated (Copilot/Agent):**
- `[###]-[feature]/spec.md` — ONLY this file. Feature spec with user stories, requirements, test specs, success criteria.
  - Created by: `/speckit.specify` command
  - Content: Full specification before planning begins

**User-Executed speckit Commands:**
- `plan.md` — Generated by `/speckit.plan` command (user runs, not AI)
- `tasks.md` — Generated by `/speckit.tasks` command (user runs, not AI)

### ❌ DO NOT CREATE (Ai Strictly Forbidden):
- **plan.md** — User runs `/speckit.plan` to generate from spec.md
- **tasks.md** — User runs `/speckit.tasks` to generate from plan.md
- **README.md** in feature folders — Not needed; spec.md is self-documenting
- **Intermediate speckit artifacts**: `research.md`, `data-model.md`, `quickstart.md`, `contracts/`
- **Template copies**: never copy `.specify/templates/*` into `specs/`. Templates live only in `.specify/templates/`.
- **Split spec documents**: `.clarification-*.md`, `.decisions-*.md`, `.resolved-*.md`
- **Redundant agent files**: Do NOT create `.github/agents/copilot-instructions.md` — the root `.github/copilot-instructions.md` is the single source.
- **Analysis/reasoning files**: `*ANALYSIS.md`, `*CORRECTIONS.md`, `*READY.md`, `*AUDIT.md`, temp `.txt` files
- **Scaffold directories**: `specs/master/`, `specs/[branch-name]/` or any non-phase folders

### ✅ AI MUST CREATE ONLY:
1. **Feature spec** (`specs/[###-feature-name]/spec.md`) — via `/speckit.specify`
2. **Source code & tests** — actual deliverables
3. **Constitution & memory** (`.specify/memory/`) — rarely updated
4. **Root instructions** (`.github/copilot-instructions.md`) — single file, update in place
5. **ADRs** (`docs/adr/[NNNN]-description.md`) — architecture decisions only

### Speckit Workflow (Ai assists, user orchestrates)

**User executes these commands in sequence:**
1. `user$ /speckit.specify "description..."` → AI creates `spec.md` only
2. `user$ /speckit.plan` → Tool generates `plan.md` from spec.md
3. `user$ /speckit.tasks` → Tool generates `tasks.md` from plan.md
4. `user$ /speckit.implement` → Tool processes tasks (Ai implements)

**Ai must NOT anticipate or pre-generate these files.**

### Key Rules
- All research, analysis, and design reasoning goes **directly into spec.md** — not into separate documents.
- If a speckit script generates intermediate files, do NOT create them manually.
- Never leave temporary files in the workspace after completing a task.
- Update existing docs instead of creating new ones.
- **Spec.md is complete and self-sufficient.** Users run `/speckit.plan` when ready to proceed.
🏛️ Architecture & Dependency Rules
Pattern: Clean Architecture + CQRS + Mediator Pattern.

**Directory Structure**
```
SauronSheet/
├── Frontend/                          # .NET Razor Pages + JS + Tailwind
│   ├── Pages/                         # Page handlers (.cshtml.cs) and views (.cshtml)
│   ├── Shared/                        # Layouts, partial views
│   ├── wwwroot/                       # Static assets (Tailwind CSS, JavaScript)
│   └── Program.cs                     # Startup configuration
│
├── Application/                       # Business logic orchestration (CQRS)
│   ├── Features/                      # Organized by feature
│   │   └── [Feature]/
│   │       ├── Commands/              # CreateXCommand.cs + handler
│   │       ├── Queries/               # GetXQuery.cs + handler
│   │       └── DTOs/                  # XDto.cs
│   └── Common/                        # Base handlers, IUserContext, pipeline behaviors
│
├── Domain/                            # Core business entities and rules
│   ├── Entities/                      # Transaction, Category, Budget (AggregateRoots)
│   ├── ValueObjects/                  # Money, DateRange, TransactionId, UserId, CategoryId
│   ├── Services/                      # CategoryService (cross-entity domain logic)
│   ├── Specifications/                # ISpecification<T>, filtering by date/category/amount
│   ├── Repositories/                  # Interfaces ONLY (ITransactionRepository, etc.)
│   ├── Exceptions/                    # DomainException, EntityNotFoundException
│   └── Common/                        # Base Entity, ValueObject abstractions
│
├── Infrastructure/                    # External integrations and persistence
│   ├── Persistence/                   # Supabase repository implementations
│   ├── Auth/                          # Supabase authentication (SupabaseAuthService)
│   └── PDF/                           # PDF parsing implementation
│
└── specs/                             # Phase specifications (ONE FILE PER PHASE)
```
**Non-Negotiable Dependency Rules**
Frontend → Application (via MediatR commands/queries).
Application → Domain (orchestrates domain logic via abstractions).
Infrastructure → Domain (implements domain contracts only).
Domain MUST NOT reference any other layer.
Application MUST NOT reference Infrastructure or Frontend directly.
Application accesses Infrastructure ONLY via Domain-defined interfaces.

**Core Workflows**

**PDF Import Pipeline:**
- Frontend: User uploads PDF.
- Application: Parse PDF via MediatR command (ImportTransactionsFromPdf).
- Domain: Validate transactions, apply business rules.
- Infrastructure: Persist to Supabase.
- Frontend: Refresh dashboard.

**Analytics:**
- Query layer in Application (MediatR queries: GetSpendingByCategoryQuery, GetMonthlyTrendQuery).
- Domain specifications filter transactions.
- Results formatted in Frontend Razor Pages.

---

## 💎 Key Patterns & Conventions

### Domain Layer Patterns

**Entities (Aggregate Roots):**
```csharp
// Parameterized constructors only, no public setters
// Throw DomainException on invariant violation
public class Transaction : AggregateRoot
{
    public TransactionId Id { get; private set; }
    public UserId UserId { get; private set; }
    public Money Amount { get; private set; }
    public DateTime Date { get; private set; }
    public string Description { get; private set; }
    public string? ImportedFrom { get; private set; }

    public Transaction(TransactionId id, UserId userId, Money amount, DateTime date, string description)
    {
        if (date > DateTime.UtcNow) throw new DomainException("Transaction date cannot be in the future.");
        if (string.IsNullOrWhiteSpace(description)) throw new DomainException("Description is required.");
        // ... assign properties
    }
}
```

**Value Objects (Strong-Typed IDs + Business Values):**
```csharp
// Strong-typed IDs prevent mixing entity identifiers at compile time
public record TransactionId(Guid Value)
{
    public TransactionId() : this(Guid.Empty) =>
        throw new DomainException("TransactionId cannot be empty.");
}
public record UserId(string Value);
public record CategoryId(Guid Value);

// Business value objects encapsulate validation and arithmetic
public record Money(decimal Amount, string Currency = "EUR")
{
    public Money Plus(Money other) => /* ... */;
    public Money Minus(Money other) => /* ... */;
}
public record DateRange(DateTime StartDate, DateTime EndDate);
```

**Domain Services:**
```csharp
// Cross-entity logic that doesn't belong to a single aggregate
public class CategoryService
{
    private readonly ICategoryRepository _categoryRepo;

    public async Task ValidateUniqueName(UserId userId, string name)
    {
        var existing = await _categoryRepo.FindByNameAndUserAsync(userId, name);
        if (existing is not null) throw new DomainException($"Category '{name}' already exists.");
    }

    public bool CanDeleteCategory(Category category, bool hasActiveTransactions)
        => !category.IsSystemDefault && !hasActiveTransactions;

    public IReadOnlyList<Category> GetSystemDefaults() => /* 4 default categories */;
}
```

**Guard Methods & System Defaults:**
```csharp
// Guard methods prevent invalid operations
public bool CanDelete() => !IsSystemDefault && !HasActiveTransactions;

// System defaults are immutable seeded values
public bool IsSystemDefault { get; private set; }
```

**Domain Patterns Quick Reference**
| Pattern         | Convention                                 | Example                       |
|----------------|--------------------------------------------|-------------------------------|
| Aggregate Root | Base class; parameterized constructor; no public setters | Transaction, Category, Budget |
| Value Object   | Immutable; value-based equality; validated on construction | Money, DateRange             |
| Strong-Typed ID| Wrapper around Guid/string; prevents ID mixing at compile time | TransactionId(Guid), UserId(string) |
| Domain Service | Cross-entity logic; depends on repository interfaces only | CategoryService               |
| Specification  | Filtering with domain language; MaxResults default 1000 | TransactionByDateRangeSpecification |
| Domain Exception| Thrown on invariant violation; caught in Application layer | DomainException               |
| Guard Method   | Returns bool to prevent invalid operations  | Category.CanDelete()          |
| System Default | Immutable seeded values; flagged with boolean property | Category.IsSystemDefault      |

---

## CQRS & MediatR
- Commands (state-changing): CreateExpense, ImportTransactionsFromPdf, UpdateBudget.
- Queries (read-only): GetExpensesByMonth, GetCategoryBreakdown, GetYearlyComparison.
- Dispatch: `await _mediator.Send(new GetSpendingByCategoryQuery(userId, month));`
- All requests routed through MediatR pipeline for consistency and middleware support.

---

## Supabase Integration (Infrastructure Only)
- Use Postgrest client in Infrastructure layer.
- Create PostgreSQL migrations for schema.
- Never expose Supabase directly to Application layer.
- Repository implementations live ONLY in Infrastructure/Persistence/.

---


- Each page has a PageModel handling GET/POST.
- MediatR calls in PageModel `OnGetAsync` / `OnPostAsync`.
- Pass data to views as Model.
- Usa solo componentes y estilos de MDBootstrap (Material Design for Bootstrap, por CDN) para toda la interfaz. No se permite Tailwind, Alpine.js ni Chart.js.



### 📦 Política de librerías externas (CDN)
- **Obligatorio:** Todas las librerías externas de CSS/JS (MDBootstrap, etc.) deben cargarse **exclusivamente mediante CDN** en _Layout.cshtml, tanto en desarrollo como en producción.
- **Prohibido:** No se permite el uso de copias locales, npm, ni minificados en el repositorio para estas librerías.
- **Motivo:** Garantiza consistencia visual, cero problemas de build, y despliegue instantáneo en cualquier entorno.
- **Versiones:** Usa **siempre la última versión estable** de cada librería. Actualiza regularmente para corregir bugs y mejorar compatibilidad.
  - **MDBootstrap**: Actualmente v9.2.0 (Cloudflare CDN) — CSS + UMD JS
  - **Font Awesome**: Versión en uso en project (jsdelivr CDN)
  - **Chart.js**: Versión en uso en project (jsdelivr CDN)
- Si agregas una nueva librería externa, **debes** usar la versión oficial más reciente por CDN y declararla en _Layout.cshtml.
- **Mantenimiento:** Revisa trimestral si hay actualizaciones de versiones y aplícalas para evitar problemas de compatibilidad, seguridad y rendimiento.

```csharp
// Example PageModel pattern
public class DashboardModel : PageModel
{
    private readonly IMediator _mediator;

    public DashboardModel(IMediator mediator) => _mediator = mediator;

    public SpendingByCategoryDto SpendingData { get; set; }

    public async Task OnGetAsync()
    {
        var userId = User.FindFirst("sub")?.Value;
        SpendingData = await _mediator.Send(new GetSpendingByCategoryQuery(userId));
    }
}
```

---

## Authentication
- Supabase Auth for multi-user login (signup, login, logout, JWT refresh).
- JWT token stored in secure cookies.
- PageModel: Extract userId from `User.FindFirst("sub")`.
- All queries scoped to current user's tenant (enforced in handler, not UI).

---

## 🧪 Testing Strategy

### Testing Pyramid
| Level         | Scope                                | Tools         | When                              |
|--------------|--------------------------------------|---------------|-----------------------------------|
| Unit Tests   | Domain entities, VOs, domain services| xUnit + Moq   | Every phase with domain changes   |
| Integration  | Application handlers (mocked repos)  | xUnit + Moq + in-memory doubles | App layer scope phases |
| End-to-End   | API → Database round-trip            | xUnit + test Supabase instance  | Infrastructure scope phases |

### Coverage Requirements
| Scope                | Minimum Coverage | Notes                        |
|----------------------|------------------|------------------------------|
| Domain Layer (domain-only phases) | 100%             | When phase scope is Domain-only (e.g., Phase 2) |
| Domain Layer (global minimum)     | 80%              | Constitution minimum across all phases |
| Application Layer                 | 70%              | Handlers, pipeline behaviors, DTOs |

### Testing Rules
- Domain service tests MUST mock repository interfaces (not real databases).
- Write tests BEFORE implementation (Red-Green-Refactor).
- Tests serve as executable specification and regression prevention.

---

## ⚙️ Development Commands
```bash
# Build solution
dotnet build

# Run application (development)
dotnet run --project src/SauronSheet.Frontend/

# Run all tests (~186 total)
dotnet test

# Run domain tests only (~37 tests)
dotnet test --filter "Category=Domain"

# Run application tests only (~74 tests)
dotnet test --filter "Category=Application"

# Build for production
dotnet publish -c Release
```

---

## 🚀 Deployment (Vercel — Free Tier)
- Deploy the Frontend project to Vercel.
- Environment variables (Supabase URL, API keys) configured in Vercel dashboard.
- Auto-deploy on push to main branch.

**Vercel Configuration (Frontend/vercel.json):**
```json
{
  "buildCommand": "dotnet publish -c Release -o ./out",
  "outputDirectory": "./out",
  "framework": "dotnet",
  "nodeVersion": "18.x"
}
```

**Environment Variables (Vercel Dashboard):**
```
Supabase__Url=https://your-project.supabase.co
Supabase__Key=your-public-anon-key
```

---

## ⚠️ Common Pitfalls to Avoid
- ❌ Application referencing Infrastructure directly (use Domain interfaces).
- ❌ Domain logic in MediatR handlers (keep handlers as thin orchestrators).
- ❌ Mixing query/command logic (separate concerns strictly).
- ❌ Supabase client leaking into Application layer.
- ❌ Public setters on domain entities (use parameterized constructors).
- ❌ Raw Guid or string for entity IDs (MUST use strong-typed value objects).
- ❌ Creating multiple spec files for same phase (consolidate into ONE).
- ❌ Splitting spec info across .clarification-, .decisions-, .resolved- files.
- ❌ Deliverables crossing declared phase layer boundaries.
- ❌ Domain events without IDomainEvent base infrastructure (verify Phase 0 first).
- ❌ **Creating intermediate speckit artifacts** (`research.md`, `data-model.md`, `quickstart.md`, `contracts/`).
- ❌ **Copying templates into specs/** (templates live only in `.specify/templates/`).
- ❌ **Creating duplicate agent instruction files** (only `.github/copilot-instructions.md` exists).
- ❌ **Leaving temporary files in repository** (analysis, audit, reasoning docs — delete after use).
- ❌ **Never add _ViewImports.cshtml to Shared/** in Razor Pages projects. This breaks Tag Helpers for all forms in Pages/Auth/Login and other pages, causing login and other POST forms to silently fail. Only use _ViewImports.cshtml in Pages/.
- ❌ **Never use `data-mdb-button-init` on `<button type="submit">` in forms.** In MDBootstrap v9+, this can break native form submission and cause the button to do nothing. Use only `data-mdb-ripple-init` for visual effects on submit buttons.

### 🐞 Lessons Learned: Supabase/Postgrest C# Client

#### 1. OR Conditions NOT Supported
- **Problem**: El cliente Postgrest C# (supabase-csharp 0.16.2) no soporta expresiones OR (`||`) dentro de `.Where()` en LINQ. Si se usa `.Where(x => x.UserId == userId.Value || x.UserId == null)`, genera una cadena vacía para el UUID y produce el error `invalid input syntax for type uuid: ""` en PostgreSQL.
- **Solution**: Realiza dos consultas separadas (una para el usuario, otra para system defaults) y combina los resultados en memoria. Nunca dependas de OR en el lado del cliente para columnas UUID.
- **Symptom**: Excepción Postgrest con código 22P02 y mensaje sobre UUID vacío cuando se consulta una tabla con columna uuid y filtro OR.

#### 2. Method Calls in .Where() Lambda NOT Supported (CRITICAL)
- **Problem**: El cliente Postgrest C# **no puede transpiler métodos de conversión o llamadas de función dentro de expressions lambda**. Ejemplos problemáticos:
  - `.Where(x => x.Id == id.Value.ToString())` → System.NotImplementedException: "Unsupported method"
  - `.Where(x => x.Amount == decimal.Parse(amount))`
  - Cualquier llamada a método en la lambda se traduce a "Unsupported method"
- **Solution**: **Siempre convierte/prepara los valores ANTES de pasar al .Where()**. El cliente espera solo comparaciones simples de propiedades.
- **Correct Pattern**:
  ```csharp
  // ❌ INCORRECTO (not supported):
  var idString = id.Value.ToString();
  await _client.From<TransactionRow>()
      .Where(x => x.Id == id.Value.ToString())  // Method call inside
      .Delete();

  // ✅ CORRECTO:
  var idString = id.Value.ToString();  // Convert outside
  await _client.From<TransactionRow>()
      .Where(x => x.Id == idString)  // Simple comparison only
      .Delete();
  ```
- **Symptom**: System.NotImplementedException at `WhereExpressionVisitor.VisitMethodCall()` when deleting, updating, or querying by ID.
- **Affected Methods**: GetByIdAsync, UpdateAsync, DeleteAsync, ExistsAsync, any .Where() with method calls.

Ejemplo de patrón correcto:
```csharp
// INCORRECTO (no usar):
await _client.From<CategoryRow>()
    .Where(x => x.UserId == userId.Value || x.UserId == null)
    .Get();

// CORRECTO:
var userRows = await _client.From<CategoryRow>()
    .Where(x => x.UserId == userId.Value)
    .Get();
var systemRows = await _client.From<CategoryRow>()
    .Where(x => x.IsSystemDefault == true)
    .Get();
var all = userRows.Models.Concat(systemRows.Models);
```

### 🔢 PDF Parser: Dual-Format Number Normalization

**Problem:** Bank PDFs may use either European (decimal comma) or Anglo (decimal point) format.
- European: `1.246,74` (point = thousands separator, comma = decimal)
- Anglo: `1,246.74` (comma = thousands separator, point = decimal)

**Solution (Infrastructure/PDF/Parsers):**

All amount parsing normalizes to standard format (point decimal, no thousands separator):

```csharp
private static string? NormalizeAmount(string? amount)
{
    if (string.IsNullOrWhiteSpace(amount))
        return null;

    amount = amount.Trim();

    // If both separators present: rightmost is decimal
    if (amount.Contains(',') && amount.Contains('.'))
    {
        var lastCommaIndex = amount.LastIndexOf(',');
        var lastDotIndex = amount.LastIndexOf('.');

        if (lastCommaIndex > lastDotIndex)
            // European: "1.246,74" → "1246.74"
            return amount
                .Replace(".", string.Empty)
                .Replace(",", ".");
        else
            // Anglo: "1,246.74" → "1246.74"
            return amount.Replace(",", string.Empty);
    }

    // Single separator: coma → point (European decimal)
    if (amount.Contains(','))
        return amount.Replace(",", ".");

    // Only point or no separator: already normalized
    return amount;
}
```

**Places to Update:** `IngBankPdfParser.cs`, `GenericBankPdfParser.cs`

**Testing:** `Infrastructure.Tests/PDF/Parsers/AmountNormalizationTests.cs` (22 test cases)

### 🎨 MDBootstrap vs Bootstrap API (CRITICAL FOR FRONTEND)

**Version & CDN Info:**
- **Current Version:** MDBootstrap v9.2.0 (Cloudflare CDN - `cdnjs.cloudflare.com`)
- **CSS:** `https://cdnjs.cloudflare.com/ajax/libs/mdb-ui-kit/9.2.0/mdb.min.css`
- **JS (UMD):** `https://cdnjs.cloudflare.com/ajax/libs/mdb-ui-kit/9.2.0/mdb.umd.min.js`

**Problem:** SauronSheet uses **MDBootstrap (mdb-ui-kit)** via CDN, NOT standard Bootstrap. MDBootstrap does NOT expose `bootstrap` as a global variable. Attempting to use `new bootstrap.Modal()` or `bootstrap.Modal.getInstance()` results in **ReferenceError: bootstrap is not defined**.

**Solution:** Always use `mdb.Modal` instead of `bootstrap.Modal` in frontend JavaScript:

```javascript
// ❌ INCORRECTO (will fail with ReferenceError):
const modal = new bootstrap.Modal(document.getElementById('myModal'));
bootstrap.Modal.getInstance(element)?.hide();

// ✅ CORRECTO:
const modal = new mdb.Modal(document.getElementById('myModal'));
mdb.Modal.getInstance(element)?.hide();
```

**Affected Code:**
- Any `.cshtml` file with inline `<script>` tags using modals
- Any `.js` file attempting to create or manipulate Bootstrap modals

**Files to Check Regularly:**
- All `.cshtml` files in `Pages/` subdirectories for inline modal code
- All `.js` files in `wwwroot/js/` that interact with modals
- Update references if upgrading MDBootstrap versions

**Testing:** Open browser console (F12) and verify no "bootstrap is not defined" errors when modal interactions occur. If upgrading versions, always test modal functionality.

### ⚠️ MDBootstrap Version Issues (Lesson Learned)

**Problem:** MDBootstrap v7.3.2 (from jsdelivr CDN) had intermittent issues with `mdb` object not being available after loading, particularly on slower connections or during page transitions. This caused "ReferenceError: mdb is not defined" errors even though the script was loaded.

**Solution:** Upgraded to **MDBootstrap v9.2.0 from Cloudflare CDN**, which resolved the timing and availability issues. Cloudflare CDN appears to provide better reliability than jsdelivr for this specific library.

**Correct URLs (v9.2.0 Cloudflare):**
```html
<!-- CSS -->
<link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/mdb-ui-kit/9.2.0/mdb.min.css" />

<!-- JS (UMD) -->
<script src="https://cdnjs.cloudflare.com/ajax/libs/mdb-ui-kit/9.2.0/mdb.umd.min.js"></script>
```

**Key Takeaway:** When updating CDN versions, verify the CDN provider too. Sometimes switching CDN sources (e.g., from jsdelivr to Cloudflare) resolves timing and availability issues better than just updating the version number.

---

## 📜 Constitutional Compliance (Non-Negotiable)
5 Core Principles (see .specify/memory/constitution.md v1.1.0 for full details):

- **Clean Architecture & Layered Dependencies** — No upward layer references; Application accesses Infrastructure via Domain interfaces only.
- **CQRS + MediatR Pattern** — Commands for state-changing ops, Queries for read-only; all routed through MediatR pipeline.
- **Domain-Driven Design** — Strong-typed IDs mandatory; domain services for cross-entity logic; system defaults pattern; guard methods; repositories abstract persistence via specifications.
- **Test-First Development (NON-NEGOTIABLE)** — Write tests before code; 100% domain coverage for domain-only phases; 80% domain global; 70% application minimum.
- **Spec-Driven Development** — Single-file rule per phase; phase scope boundaries enforced; out-of-scope deliverables are constitution violations.

**Critical Rules:**
- ❌ Domain/Application MUST NOT reference Infrastructure or Frontend directly.
- ❌ No direct Supabase client calls outside Infrastructure layer.
- ❌ All queries must be scoped to current user's tenant.
- ❌ Raw Guid/string for entity IDs (MUST use strong-typed value objects).
- ❌ Deliverables crossing declared phase layer boundaries.
- ✅ Every Command/Query requires MediatR handler + integration test.
- ✅ Domain invariants prevent invalid states (proven in unit tests).
- ✅ Entities use parameterized constructors; no public setters.
- ✅ System defaults flagged with boolean property; protected by guard methods.

---

## 🔄 Spec-Driven Development Workflow
1. **Write Test Spec** — Behavior spec in tests (TDD red phase).
2. **Define Handler** — Create MediatR Command/Query handler stub.
3. **Build Domain** — Implement entities/value objects to satisfy requirements.
4. **Implement Persistence** — Add Infrastructure repository implementations.
5. **Wire UI** — Create Razor Pages / Alpine.js to trigger operations.

**Phase Scope Enforcement:**
- Steps 2, 4, 5 are ONLY executed if the phase spec declares those layers in scope.
- For Domain-only phases (e.g., Phase 2): execute steps 1 and 3 only.
- Out-of-scope work is documented as "deferred to Phase X" in the spec.

---

_Last Updated: 2026-02-19 | Version: 1.3.0 (Aligned with Constitution v1.1.0, Phase 4 context merged)_