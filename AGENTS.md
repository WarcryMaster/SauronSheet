# SauronSheet AI Instructions

## Source of Truth

This file is the single source of truth for AI behavior in this repository.
Read it before acting, and follow the linked instruction files for file-type-specific rules.

## Language
Always when user interact with the IA and sdd artifacts must be in neutral Spanish. Never with Argentinian Accent or Vose! Code and IA Code like this document Agent.md must be in english.

## Working Rules

- Operate in agent mode: avoid asking for permission on routine read-only tasks.
- Follow Spec-Driven Development: Spec -> Plan -> Task -> Implement.
- Minimize conversational overhead; execute clear tasks directly.
- If a command fails, analyze, fix, and retry. Stop only if you are stuck in a loop.
- Use Sentry for ALL observability: runtime logging, tracing, diagnostics, error capture, and important business events. Do not use Console.WriteLine, Debug.WriteLine, Trace.WriteLine, or any other logging mechanism. Sentry is the single pipeline for backend (SentrySdk) and frontend (JavaScript Sentry SDK).
- Keep source code, identifiers, comments, docstrings, commit messages, PRs, ADRs, and ALL runtime traces (Sentry, logs, breadcrumbs, metrics) in English.
- Keep specs, plans, requirements, and acceptance criteria in Spanish.
- Converse with the AI in Spanish. Code, identifiers, and traces stay in English; everything else — specs, docs, discussions with the AI — goes in Spanish.
- Use neutral Spanish (from Spain / castellano) in all AI conversations. No regional dialects (Rioplatense, Mexican, etc.). This overrides any global or personal configuration that specifies otherwise.

## Architecture Rules

### Clean Architecture (Mandatory)
- Unidirectional dependencies: Frontend -> Application -> Domain.
- Infrastructure -> Domain only.
- Domain must NOT reference Application, Infrastructure, or Frontend.
- Application must NOT reference Infrastructure or Frontend directly.

### CQRS + MediatR
- Commands for state-changing operations (CreateExpense, ImportTransactionsFromPdf, UpdateBudget).
- Queries for read-only operations (GetExpensesByMonth, GetCategoryBreakdown).
- Dispatch via `await _mediator.Send(command)`.
- All requests routed through MediatR pipeline for consistency and middleware support.
- Handlers are thin orchestrators — keep domain logic in Domain layer.

### Domain-Driven Design
- Strong-typed IDs mandatory (TransactionId, UserId). Never raw Guid/string.
- Immutability and explicit invariants required in the Domain.
- Domain services for cross-entity logic (CategoryService, BudgetService).
- Repositories abstract persistence via specifications.
- System defaults flagged with boolean property (`IsSystemDefault`), protected by guard methods.
- Entities use parameterized constructors; no public setters.

### Supabase Integration (Infrastructure Only)
- Use Postgrest client in Infrastructure layer only.
- Never expose Supabase directly to Application or Domain layers.
- Repository implementations live ONLY in `Infrastructure/Persistence/`.
- All queries scoped to current user's tenant (enforced in handler, not UI).

### Authentication
- Supabase Auth for multi-user login (signup, login, logout, JWT refresh).
- JWT token stored in secure HttpOnly cookies.
- PageModel: extract userId from `User.FindFirst("sub")`.
- Never trust client-side userId from form inputs.

### Database Migrations (Supabase CLI)
- All migrations live in `supabase/migrations/` — this is the single source of truth.
- Naming convention: `YYYYMMDDHHMMSS_descriptive_name.sql` (sequential timestamps).
- Never modify migration files after they have been applied to production.
- To create a new migration: `supabase migration new <name>`, then write SQL in the generated file.
- To apply migrations locally: `supabase migration up`.
- To apply migrations to production: `supabase db push --linked` (automated in CI/CD).
- To reset local database: `supabase db reset`.
- CI/CD runs `supabase db push --linked` automatically on deploy (GitHub Actions).
- Required GitHub Actions secrets: `SUPABASE_ACCESS_TOKEN`.
- Never put migration files in `src/` — they belong in `supabase/migrations/`.
- If you see `PGRST205` errors about missing tables, a migration was not applied. Check `supabase/migrations/` and apply it.

## Domain Patterns Quick Reference

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

## Quality Rules

- **Never use `var`** — always use explicit type declarations. `var` oculta la intención del código y obliga al lector a inferir el tipo, lo que reduce la legibilidad. Como dice Uncle Bob: la claridad es lo primero. En tests también: cada declaración debe ser autoevidente sin necesidad de hover en el IDE.
- Keep `CancellationToken` last and forward it downstream.
- Dispose resources deterministically.
- Use modern throw helpers and preserve stack traces with `throw;`.
- Do not expose `List<T>` in public APIs.
- Avoid multiple enumeration and prefer `TryGetValue` where applicable.
- Parameterize SQL and validate all external inputs.
- Use `ConfigureAwait(false)` in infrastructure/library code when context capture is unnecessary.

## Frontend Rules

### Technology Stack (Mandatory)
- Razor Pages with PageModel patterns and antiforgery protection.
- Interactive layer: Alpine.js v3 + HTMX v2 + Chart.js (latest), all via CDN.
  - Alpine.js for declarative reactivity: `x-data`, `x-show`, `x-transition`, `x-model`, `x-on`, `x-init`.
  - HTMX for Ajax from HTML: `hx-get`, `hx-target`, `hx-swap`, `hx-select`, `hx-indicator`.
  - Chart.js for interactive analytics; colors resolved from CSS custom properties via `getComputedStyle`.
- Use MDBootstrap via CDN (mdb-ui-kit v9.2.0) for CSS layout, components, and grid. Not Bootstrap or local alternatives.
- All local static assets referenced from Razor (`wwwroot/css`, `wwwroot/js`, `wwwroot/img`) MUST use `~/...` paths plus `asp-append-version="true"`. Never hardcode `/css/...`, `/js/...`, or `/img/...` for local assets.
- All CDN scripts load in this order: MDB CSS → Alpine.js (defer) → HTMX → Chart.js → charts.js.

### Alpine.js Mandatory Patterns

**FORBIDDEN — NEVER USE:**
- ❌ `onclick`, `onchange`, `onsubmit` inline attributes → use Alpine.js `x-on:` / `@@event` directives
- ❌ `DOMContentLoaded` for initialization → use Alpine.js `x-init` or HTMX lifecycle events (`htmx:afterSwap`)
- ❌ `<script>` blocks with vanilla JS for page interactivity → use Alpine.js `x-data` components
- ❌ `document.getElementById()` for state reading → use Alpine.js `x-data` properties and `$refs`
- ❌ `addEventListener()` for UI events → use `@@click`, `@@change`, `@@submit`, `@@input`, `@@keydown`
- ❌ `classList.add/remove('d-none')` for visibility → use `x-show` + `x-transition`
- ❌ `.textContent = '...'` for dynamic text → use `x-text` or `x-html`
- ❌ `.disabled = true/false` for buttons → use `:disabled` + reactive state
- ❌ `.style.backgroundColor = '...'` → use `:style` binding
- ❌ Separate `.js` files for page-specific logic → embed in Alpine.js `x-data`
- ❌ `d-flex`, `d-block`, `d-grid` (MDB classes with `!important`) on elements with `x-show` → wrap in inner div
- ❌ `@Json.Serialize()` inside double-quoted HTML attributes → use single-quoted attributes (`x-data='...'`) and double-quoted JS strings
- ❌ `@(decimal)` without invariant culture inside JavaScript → use `.ToString(System.Globalization.CultureInfo.InvariantCulture)`
- ❌ `<input type="date">` — use Flatpickr via Alpine.js `x-init` instead
- ❌ `<template x-for>` inside `<select>` — browsers move `<template>` out of `<select>` in the DOM, breaking Alpine.js. Use `rebuildOptions()` + `x-effect` to rebuild `<option>` elements via DOM manipulation instead (see Budgets/Create.cshtml for the pattern)

**REQUIRED — ALWAYS USE:**
- ✅ `x-data` on every interactive component (forms, filters, modals, toolbars)
- ✅ `x-show` + `x-transition` for any conditional visibility
- ✅ `x-model` for all form inputs (text, select, checkbox, radio, color)
- ✅ `:disabled` + spinner on ALL submit buttons with `x-data="{ loading: false }"`
- ✅ `@@submit="loading = true"` on all forms
- ✅ `x-text` for dynamic text content
- ✅ `:class` for conditional CSS classes
- ✅ `$refs` for DOM element references when unavoidable
- ✅ MDB modals via `new mdb.Modal(el).show()` from Alpine.js methods (DO NOT use `data-mdb-toggle` on buttons, call `show()` explicitly)
- ✅ `htmx:beforeSwap` → `destroyAllCharts()` for any page with Chart.js + HTMX
- ✅ Flatpickr via `x-data x-init="flatpickr($el, { dateFormat: 'Y-m-d', altInput: true, altFormat: 'd/m/Y', allowInput: true })"` for ALL date inputs. Never use `type="date"`.
- ✅ `<template x-for>` inside `<select>` — browsers move `<template>` out of `<select>` in the DOM, breaking Alpine.js. Use `rebuildOptions()` + `x-effect` to rebuild `<option>` elements via DOM manipulation instead (see Budgets/Create.cshtml for the pattern)

### Cross-Attribute Compatibility
- **CRITICAL: MDB uses `data-mdb-*` attributes, NOT `data-bs-*` (Bootstrap).**
  - `data-mdb-toggle="dropdown"` (not `data-bs-toggle`)
  - `data-mdb-target="#modal"` (not `data-bs-target`)
  - `data-mdb-dismiss="modal"` (not `data-bs-dismiss`)
  - `data-mdb-auto-close="outside"` (not `data-bs-auto-close`)
  - `data-mdb-ripple-init` (not `data-bs-ripple`)
  - Alpine.js uses its own attributes: `x-data`, `x-show`, `x-on`, `x-model`, `x-bind` — these do NOT conflict with `data-mdb-*`
  - HTMX uses its own attributes: `hx-get`, `hx-target`, `hx-swap` — these do NOT conflict with `data-mdb-*`
- **DESIGN.md is the visual source of truth for all UI work.** Read `DESIGN.md` BEFORE changing any `.cshtml`, CSS, component layout, spacing, or interaction pattern.
- When delegating frontend/UI work to any sub-agent, ALWAYS include `DESIGN.md` and the Frontend Rules section of this file in the prompt/context.
- Keep JavaScript modern: `const` / `let`, event listeners, null checks, and server-side revalidation.
- See `.github/instructions/razor-frontend.instructions.md` for full MDBootstrap API and PageModel patterns (auto-loaded for `.cshtml` files).
- Utilizar la metodología https://github.com/voltagent/awesome-design-md para la generación de interfaces. Todas las interfaces frontend deben crearse siguiendo esta metodología.

## Error Handling and Leak Prevention

- **Never expose internal exception details to users.** Show generic messages only.
- `catch (Exception ex)` blocks must NEVER include `ex.Message` in user-facing output.
- `catch (DomainException ex)` may show `ex.Message` since domain exceptions are user-safe by design (business rule violations).
- `catch (HttpRequestException ex)` must always show a generic network error message.
- **Every catch block must capture to Sentry** via `SentrySdk.CaptureException` with appropriate scope tags and level.
- Infrastructure layer must never embed raw exception messages in return values (`AuthResult.Failure`, etc.). Log to Sentry, return generic.
- Application layer command handlers must not propagate infrastructure error messages as DomainException messages. Use fixed/translated messages.
- Validation: before adding a new catch block or error path, verify the message cannot contain sensitive infrastructure details (hostnames, IPs, connection strings, file paths, stack traces).
- Exception type hierarchy: catch specific types (HttpRequestException, DomainException) before the generic `Exception` fallback.

## Testing Strategy

### Testing Pyramid
| Level         | Scope                                | Tools         | When                              |
|--------------|--------------------------------------|---------------|-----------------------------------|
| Unit Tests   | Domain entities, VOs, domain services| xUnit + Moq   | Every phase with domain changes   |
| Integration  | Application handlers (mocked repos)  | xUnit + Moq + in-memory doubles | App layer scope phases |
| End-to-End   | Playwright browser tests             | Playwright    | UI/UX scope phases                |

### Coverage Requirements
| Scope                | Minimum Coverage |
|----------------------|------------------|
| Domain Layer         | 80%              |
| Application Layer    | 70%              |

### Mandatory Rules
- Tests are mandatory BEFORE implementation in every feature (Red-Green-Refactor).
- Domain service tests MUST mock repository interfaces (not real databases).
- Tests serve as executable specification and regression prevention.

### Test Commands
```bash
# Run all .NET tests
dotnet test

# Run Playwright E2E tests (starts app automatically)
npx playwright test --config=e2e/playwright.config.ts --project=chromium
```

### E2E Testing Policy: Real User Interaction Required

**Fundamental rule:** E2E interface tests **MUST** act as a real user navigating and interacting with the web. If they don't simulate real interaction, **they are worthless**.

**DO:**
- Use `page.click()`, `page.fill()`, `page.selectOption()` to interact with elements.
- Wait for elements to be visible before interacting (`waitFor`, `toBeVisible`).
- Handle native browser dialogs (`page.on('dialog')`).
- Navigate the app as a real user would (clicks on buttons, links, forms).
- Verify observable results in the UI (visible text, redirects, success/error messages).

**DO NOT:**
- ❌ Use `page.evaluate()` to execute direct JavaScript in the DOM.
- ❌ Use `fetch()` inside `page.evaluate()` to call APIs directly.
- ❌ Manipulate the DOM directly (`document.querySelector`, `input.type = 'text'`).
- ❌ Skip UI flows (modals, confirmations, form validations).
- ❌ Verify database state directly instead of the UI.

**Exception:** Post-execution cleanup helpers (`test.afterAll`) **MAY** use `fetch()` directly, as they are maintenance operations, not tests themselves.

**Reason:** An E2E test that doesn't simulate real interaction doesn't test real use cases. It only verifies the API works, which integration tests already cover. E2E tests must validate that **the user can complete their task** through the interface.

## Documentation and Review Rules

- Design docs should lead with the decision, then the details.
- Use tables, checklists, and clear review paths.
- For chained work, state what is in scope, what is out of scope, and the dependency order.

## Auto-Loaded Instruction Files

These files are referenced by the editor/IDE through `applyTo`-style scoping and should be kept in sync with this root policy:

| File | Applies to | Notes |
|---|---|---|
| `.github/instructions/csharp-quality.instructions.md` | `**/*.cs` | Always-on C# baseline rules |
| `.github/instructions/csharp-rules-design-and-naming.instructions.md` | on-demand | API, design, naming, globalization rules |
| `.github/instructions/csharp-rules-performance-and-maintainability.instructions.md` | on-demand | Performance and maintainability rules |
| `.github/instructions/csharp-rules-reliability-and-usage.instructions.md` | on-demand | Reliability, async, disposal, usage rules |
| `.github/instructions/csharp-rules-security-platform-and-il.instructions.md` | on-demand | Security, platform, serialization, IL rules |
| `.github/instructions/razor-frontend.instructions.md` | `**/*.cshtml` | Razor Pages and frontend rules |

## Common Pitfalls & Lessons Learned

### Architecture & Code
- ❌ Application referencing Infrastructure directly (use Domain interfaces).
- ❌ Domain logic in MediatR handlers (handlers are thin orchestrators).
- ❌ Mixing query/command logic (separate concerns strictly).
- ❌ Supabase client leaking into Application layer.
- ❌ Raw Guid or string for entity IDs (MUST use strong-typed value objects).
- ❌ Public setters on domain entities (use parameterized constructors).
- ❌ Never put `_ViewImports.cshtml` in `Shared/` — only in `Pages/` (breaks Tag Helpers).
- ❌ Never use `data-mdb-button-init` on `<button type="submit">` — breaks form submission in MDBootstrap v9+.
- ❌ Never call `new mdb.Input(el)` on a raw `<input>` without `.form-outline` wrapper — causes `Cannot read properties of null (reading 'classList')`. Only use `mdb.Input()` inside `.form-outline` divs with sibling `<label>`.
- ❌ Never use `<input type="date">` — use Flatpickr (`type="text"` + `x-init`). Flatpickr hides the original input and uses a hidden `<input type="hidden">` for the value. E2E tests MUST use `page.evaluate()` + Flatpickr API to set date values, not `page.fill()`.
- ❌ Never reference local CSS, JS, or image assets with hardcoded `/css/...`, `/js/...`, or `/img/...` paths in Razor. Use `~/...` + `asp-append-version="true"` to prevent stale-cache drift between local and production.
- ❌ Never embed Razor data in JS via `@Html.Raw(Json.Serialize(model))` (in `on*` attributes or inside `<script>` blocks) or via `@Html.Encode(value)` inside an HTML attribute. Both produce double-encoding / XSS / attribute-breakage bugs. Use `data-*` + a delegated listener for per-item data, or `<script type="application/json">` + `JSON.parse` for larger payloads. Full rationale and patterns in [`docs/adr/0002-safe-json-data-passing.md`](docs/adr/0002-safe-json-data-passing.md).

### Supabase/Postgrest C# Client

#### 0. PGRST205: Table Not Found in Schema Cache
If you see `PGRST205: Could not find the table 'public.XXX' in the schema cache`, the table does not exist or the migration was not applied.
**Solución**: Check `supabase/migrations/` for the correct migration file. Apply it with `supabase db push --linked` (production) or `supabase migration up` (local). Do NOT create tables manually via SQL Editor — always use migrations.

#### 1. OR Conditions NOT Supported
El cliente Postgrest C# (supabase-csharp 0.16.2) no soporta OR dentro de `.Where()`. 
**Solución**: dos consultas separadas y combinar en memoria.

#### 2. Method Calls in .Where() Lambda NOT Supported (CRITICAL)
```csharp
// ❌ INCORRECTO — method call inside lambda
await _client.From<TransactionRow>()
    .Where(x => x.Id == id.Value.ToString())
    .Delete();

// ✅ CORRECTO — convert outside
var idString = id.Value.ToString();
await _client.From<TransactionRow>()
    .Where(x => x.Id == idString)
    .Delete();
```

### PDF Parser: Dual-Format Number Normalization
Los PDFs bancarios usan formato europeo (coma decimal) o anglo (punto decimal).
Ver `Infrastructure/PDF/Parsers/` para la lógica de normalización.

## Priority

If there is any conflict, this file and the linked instruction files take priority over other conversational context.
