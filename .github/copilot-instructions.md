## AGENTS.md

See `../AGENTS.md` for the single source of truth for AI behavior in this repository.
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

## Razor Pages (Frontend)
- Each page has a PageModel handling GET/POST using primary constructors.
- MediatR calls in PageModel `OnGetAsync` / `OnPostAsync`.
- Pass data to views as `Model` properties.
- **MDBootstrap v9.2.0 (Cloudflare CDN) only** — no Tailwind, no Alpine.js.
- All external CSS/JS loaded via CDN in `_Layout.cshtml`; no local copies or npm.
- See `.github/instructions/razor-frontend.instructions.md` for full MDBootstrap API, PageModel patterns, and JS rules (auto-loaded for `.cshtml` files).

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
- ❌ **Never add _ViewImports.cshtml to Shared/** — breaks Tag Helpers on all forms. Only use it in `Pages/`. (Detail: `razor-frontend.instructions.md`)
- ❌ **Never use `data-mdb-button-init` on `<button type="submit">`** — breaks form submission in MDBootstrap v9+. (Detail: `razor-frontend.instructions.md`)

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

### 🎨 MDBootstrap (Lessons Learned → moved)

Full MDBootstrap API rules, CDN URLs, and version history are documented in `.github/instructions/razor-frontend.instructions.md` (auto-loaded for `.cshtml` files).

**Summary:** Use `mdb.Modal` not `bootstrap.Modal`. CDN: `cdnjs.cloudflare.com/ajax/libs/mdb-ui-kit/9.2.0/`.

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

_Last Updated: 2026-03-20 | Version: 1.4.0 (Scoped instructions split: csharp-quality.instructions.md + razor-frontend.instructions.md)_
