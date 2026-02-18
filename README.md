# SauronSheet

A modern, multi-user expense tracking application that imports bank transactions from PDF statements and provides detailed analytics and spending reports.

## Features

- 📄 **PDF Bank Statement Import** — Automatically parse and import transactions from bank PDFs.
- 📊 **Analytics Dashboard** — View spending by category, trends over time, and monthly/yearly comparisons.
- 👥 **Multi-User Support** — Secure authentication with individual expense tracking.
- 💰 **Budget Tracking** — Set and monitor budgets by category with overage detection.
- 📈 **Detailed Reports** — Export and analyze spending patterns.
- 🎨 **Clean Interface** — Modern, responsive UI built with Tailwind CSS.

## Tech Stack

### Backend

- **.NET Core 10** — Web framework and API backend.
- **MediatR** — CQRS pattern with mediator for request handling.
- **Clean Architecture** — Layered application with strict dependency rules.

### Frontend

- **Razor Pages** — Server-side template rendering.
- **Tailwind CSS** — Utility-first styling framework.
- **Alpine.js** — Lightweight interactive components.
- **Vanilla JavaScript** — Additional interactivity where needed.

### Database & Auth

- **Supabase** — PostgreSQL database and Supabase Auth for multi-user authentication.

## Project Structure

```
SauronSheet/
├── Frontend/           # Razor Pages web interface
│   ├── Pages/          # Page handlers (.cshtml.cs) and views (.cshtml)
│   ├── Shared/         # Layouts, partial views
│   ├── wwwroot/        # Static assets (Tailwind CSS, JavaScript)
│   └── Program.cs      # Startup configuration
│
├── Application/        # Business logic orchestration (CQRS)
│   ├── Features/
│   │   ├── Transactions/ # Transaction import, queries, commands
│   │   ├── Analytics/    # Spending reports, charts, trends
│   │   ├── Budgets/      # Budget management commands/queries
│   │   └── Auth/         # User authentication commands
│   └── Common/          # Base handlers, IUserContext, pipeline behaviors
│
├── Domain/             # Core business entities and rules
│   ├── Entities/       # Transaction, Category, Budget (AggregateRoots)
│   ├── ValueObjects/   # Money, DateRange, TransactionId, UserId, CategoryId
│   ├── Services/       # CategoryService (cross-entity domain logic)
│   ├── Specifications/ # ISpecification, filtering by date/category/amount
│   ├── Repositories/   # Interfaces ONLY (ITransactionRepository, etc.)
│   ├── Exceptions/     # DomainException, EntityNotFoundException
│   └── Common/         # Base Entity, ValueObject abstractions
│
├── Infrastructure/     # External integrations and persistence
│   ├── Persistence/    # Supabase repository implementations
│   ├── Auth/           # Supabase authentication (SupabaseAuthService)
│   └── PDF/            # PDF parsing implementation
│
└── specs/              # Phase specifications (one file per phase)
```

## Getting Started

### Prerequisites

- .NET 10 SDK or higher
- Node.js 18+ (optional, if modifying Tailwind)
- Supabase account (free tier available at https://supabase.com)

### Installation

1. **Clone the repository**
    ```bash
    git clone https://github.com/yourusername/SauronSheet.git
    cd SauronSheet
    ```

2. **Configure Supabase**

    Create a Supabase project at https://supabase.com  
    Copy your project URL and API keys  
    Create `appsettings.json` in `Frontend/`:

    ```json
    {
      "Supabase": {
        "Url": "your-project-url",
        "Key": "your-public-key"
      }
    }
    ```

3. **Build the project**

    ```bash
    dotnet build
    ```

4. **Run the application**

    ```bash
    dotnet run --project Frontend/
    ```

    Navigate to https://localhost:7000 (or configured port).

## Architecture

### Clean Architecture with CQRS

SauronSheet follows Clean Architecture principles with CQRS (Command Query Responsibility Segregation) pattern:

- **Domain Layer** — Contains core business entities, value objects, domain services, specifications, and repository interfaces. Zero external dependencies.
- **Application Layer** — Orchestrates use cases using MediatR CQRS commands and queries. Depends only on Domain.
- **Infrastructure Layer** — Handles data persistence (Supabase) and external services. Implements Domain contracts.
- **Frontend Layer** — Presents UI and collects user input. Communicates with Application via MediatR.

#### Dependency Rules

```
Frontend ──→ Application ──→ Domain
                                ↑
Infrastructure ─────────────────┘
```
- Domain never references any other layer.
- Application accesses Infrastructure only via Domain-defined interfaces.
- Infrastructure implements Domain contracts (repositories, services).
- No upward dependencies allowed.

### Domain-Driven Design

#### Aggregate Roots

```csharp
// Entities use parameterized constructors, no public setters
// Strong-typed IDs prevent accidental ID mixing at compile time
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
        if (date > DateTime.UtcNow) throw new DomainException("Date cannot be in the future.");
        if (string.IsNullOrWhiteSpace(description)) throw new DomainException("Description required.");
        // ... assign properties
    }
}
```

#### Value Objects

```csharp
// Strong-typed IDs
public record TransactionId(Guid Value);
public record UserId(string Value);
public record CategoryId(Guid Value);

// Business value objects with validation and arithmetic
public record Money(decimal Amount, string Currency = "EUR")
{
    public Money Plus(Money other) => /* ... */;
    public Money Minus(Money other) => /* ... */;
}

public record DateRange(DateTime StartDate, DateTime EndDate);
```

#### Domain Services

```csharp
// Cross-entity logic coordinated via domain service
public class CategoryService
{
    private readonly ICategoryRepository _categoryRepo;

    public async Task ValidateUniqueName(UserId userId, string name) { /* ... */ }
    public bool CanDeleteCategory(Category category, bool hasActiveTransactions) => /* ... */;
    public IReadOnlyList<Category> GetSystemDefaults() => /* 4 default categories */;
}
```

### CQRS Pattern

**Commands (state-changing operations):**
```csharp
public class ImportTransactionsFromPdfCommand : IRequest<ImportResult> { }
public class CreateBudgetCommand : IRequest<BudgetId> { }
```

**Queries (read-only operations):**
```csharp
public class GetSpendingByCategoryQuery : IRequest<List<CategorySpending>> { }
public class GetMonthlyTrendsQuery : IRequest<List<MonthlyTrend>> { }
```

All requests routed through MediatR pipeline for consistency and middleware support:

```csharp
await _mediator.Send(new GetSpendingByCategoryQuery(userId, month));
```

## Development Workflow

### Spec-Driven Development

1. **Write Specification** — Define expected behavior in a test (TDD red phase).
2. **Create CQRS Handler** — Implement command/query handler to satisfy spec.
3. **Add Domain Logic** — Implement entities/services needed by handler.
4. **Build Infrastructure** — Add repository implementations for Supabase.
5. **Wire Frontend** — Create Razor page to trigger command/query.
6. **Test End-to-End** — Verify complete flow.

### Build Commands

```bash
# Build solution
dotnet build

# Run application (development)
dotnet run --project Frontend/

# Run all tests
dotnet test

# Run domain tests only
dotnet test --filter "Category=Domain"

# Run application tests only
dotnet test --filter "Category=Application"

# Build for production
dotnet publish -c Release
```

## File Organization

- **New Features:** Create folder in `Application/Features/YourFeature/`
  - `Commands/YourCommand.cs` + `YourCommandHandler.cs`
  - `Queries/YourQuery.cs` + `YourQueryHandler.cs`
  - `DTOs/YourDto.cs`
- **New Entities:** Add to `Domain/Entities/YourEntity.cs`
- **New Value Objects:** Add to `Domain/ValueObjects/YourValueObject.cs`
- **New Domain Services:** Add to `Domain/Services/YourService.cs`
- **New Repositories:** Define interface in `Domain/Repositories/`, implement in `Infrastructure/Persistence/`
- **New Pages:** Create `.cshtml` and `.cshtml.cs` files in `Frontend/Pages/`

## Key Features Explained

### PDF Import

- User uploads bank PDF in the upload page.
- Backend extracts transaction rows using PDF parsing library.
- Validates against domain rules (required fields, amount format, date range).
- Creates Transaction domain entities with Money value objects.
- Persists to Supabase via repository interface.
- Returns confirmation with summary (N imported, M skipped with reasons).

### Analytics Dashboard

- Query handlers aggregate spending data from Supabase.
- Domain specifications filter transactions by date range, category, amount.
- Frontend renders charts using Chart.js or similar library.
- Supports filtering by date range, category, budget status.
- All queries scoped to current user's tenant.

### Multi-User Authentication

- Supabase Auth provides signup/login/logout functionality.
- JWT tokens stored in secure cookies.
- Each PageModel extracts userId from JWT claims.
- Queries automatically scoped to current user — enforced in handlers, not UI.

### Budget Management

- Users set monthly budget limits per category.
- Budget entity tracks limits with Money value object.
- `IsOverBudget(currentSpend)` detects overage condition.
- `PercentageUsed(currentSpend)` calculates usage percentage.
- Unique constraint: one budget per user-category-month combination.
- System default categories (Groceries, Transport, Utilities, Other) are immutable.

## Configuration

Create or modify `Frontend/appsettings.json`:

```json
{
  "Supabase": {
    "Url": "https://your-project.supabase.co",
    "AnonKey": "your-anon-key"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

Environment-specific settings:

- `appsettings.Development.json` — Local development.
- `appsettings.Production.json` — Production deployment.

## Testing

### Testing Pyramid

| Level              | Scope                                 | Tools                |
|--------------------|---------------------------------------|----------------------|
| Unit Tests         | Domain entities, value objects, domain services | xUnit + Moq         |
| Integration Tests  | Application handlers with mocked repositories | xUnit + Moq + in-memory doubles |
| End-to-End Tests   | API → Database round-trip             | xUnit + test Supabase instance   |

### Coverage Requirements

| Scope                        | Minimum Coverage |
|------------------------------|-----------------|
| Domain Layer (domain-only phases) | 100%        |
| Domain Layer (global minimum)     | 80%         |
| Application Layer                 | 70%         |

### Run Tests

```bash
# Run all tests
dotnet test

# Run domain tests only
dotnet test --filter "Category=Domain"

# Run application tests only
dotnet test --filter "Category=Application"
```

## Deployment to Vercel

SauronSheet is deployed to Vercel (free tier) for scalable, serverless hosting.

### Prerequisites

- Vercel account (free at https://vercel.com).
- GitHub repository connected to Vercel.
- Supabase project URL and API keys.

### Setup

Create `vercel.json` in `Frontend/`:

```json
{
  "buildCommand": "dotnet publish -c Release -o ./out",
  "outputDirectory": "./out",
  "framework": "dotnet",
  "nodeVersion": "18.x"
}
```

Connect GitHub to Vercel

- Import repository from GitHub.
- Select Frontend as the root directory.
- Set build settings as above.

Configure Environment Variables

In Vercel dashboard, add environment variables:

```
Supabase__Url=https://your-project.supabase.co
Supabase__Key=your-public-anon-key
```

Deploy

- Push to main branch.
- Vercel automatically builds and deploys.
- Live URL provided after deployment.

## Production Checklist

- CORS configured in Supabase for Vercel domain.
- Environment variables set in Vercel dashboard.
- Database backups enabled in Supabase.
- Error logging configured (Sentry or similar).
- Custom domain configured (optional).

## Database Schema

Key Supabase tables:

- `users` — User profiles and authentication metadata.
- `transactions` — Imported bank transactions with category, amount (Money), and date.
- `categories` — Expense categories per user (custom + 4 system defaults).
- `budgets` — Monthly budget limits per category per user.
- `pdf_imports` — Metadata about imported PDF files.

## Troubleshooting

**"Connection to Supabase failed"**

- Verify API keys in appsettings.json.
- Check Supabase project is running.
- Ensure network allows HTTPS requests.

**"PDF import fails"**

- Verify PDF format matches expected bank structure.
- Check error logs for parsing issues.
- System returns descriptive errors per failed row.

**"Budget calculations seem wrong"**

- Verify Money value objects use same currency.
- Check budget month matches transaction dates.
- Ensure unique constraint: one budget per user-category-month.

## Performance Tips

- Use pagination for transaction lists (default MaxResults = 1000).
- Cache category/budget data in frontend (rarely changes).
- Add database indexes on userId, date columns in Supabase.
- Use analytics queries with proper date range filters.
- Queries limited to 1000 rows by default; pagination required for larger datasets.

## Contributing

- Create a new branch: `git checkout -b feature/your-feature`.
- Follow spec-driven development workflow.
- Write tests before implementing (TDD).
- Use strong-typed IDs and value objects for domain entities.
- Ensure all tests pass: `dotnet test`.
- Submit a pull request with detailed description.

## Roadmap

- Phase 0: Foundation & Infrastructure Setup
- Phase 1: Authentication & Multi-Tenancy
- Phase 2: Core Data Model & Domain Entities
- Phase 3: Transaction Import Pipeline (PDF Parsing & CRUD)
- Phase 4: Analytics & Dashboard (MVP Complete)
- Phase 5: Budget Management & Alerts
- Phase 6: UI Polish, Performance & Production Deployment

## Post-MVP Backlog

- Budget alerts via mobile push notifications.
- Multi-currency support with exchange rates.
- Scheduled transaction rules (recurring expenses).
- Social features (shared budgets, spending groups).
- AI-powered spending suggestions.

## License

[Your License Here]

## Support

For questions or issues:

🐛 GitHub Issues: https://github.com/yourusername/SauronSheet/issues