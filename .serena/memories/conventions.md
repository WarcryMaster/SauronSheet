# Conventions

## Code style (mandatory)
- NO `var` — explicit types everywhere (including tests)
- CancellationToken always last param, forwarded downstream
- Dispose resources deterministically
- No `List<T>` in public APIs
- Avoid multiple enumeration; prefer TryGetValue
- `ConfigureAwait(false)` in infrastructure/library code only
- SQL always parameterized; validate all external inputs
- Modern throw helpers; preserve stack traces with `throw;`

## Domain patterns (see `.github/instructions/domain-patterns.instructions.md`)
| Pattern | Convention | Example |
|---|---|---|
| Aggregate Root | Base class; parameterized ctor; no public setters | Transaction, Category, Budget |
| Value Object | Immutable; value-based equality; validated on construction | Money, DateRange |
| Strong-Typed ID | Wrapper around Guid/string | TransactionId(Guid), UserId(string) |
| Domain Service | Cross-entity logic; depends on repo interfaces only | CategoryService |
| Specification | Filtering; MaxResults default 1000 | TransactionByDateRangeSpecification |
| Domain Exception | Thrown on invariant violation | DomainException |
| Guard Method | Returns bool | Category.CanDelete() |
| System Default | Immutable seeded values; flagged with bool property | Category.IsSystemDefault |

## Architecture
- Clean Architecture: Domain -> Application -> Infrastructure + Frontend
- Domain has ZERO NuGet dependencies (mandatory)
- Application only depends on Domain
- Infrastructure implements Domain repository interfaces (Supabase)
- Frontend depends on Application only (not Infrastructure)
- Dependency injection wired in each project's DependencyInjection.cs

## Sentry
- Only observability pipeline. No Console.WriteLine/Debug/Trace anywhere
- Backend: SentrySdk. Frontend: JS Sentry SDK

## E2E
- Every frontend code change MUST include affected E2E test review/update
- Playwright tests in e2e/ directory

## CDN
- NO local copies of frontend libs. NO npm for CSS/JS. Load from CDN in _Layout.cshtml
