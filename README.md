# SauronSheet

A modern, multi-user expense tracking application that imports bank transactions from PDF statements and provides detailed analytics and spending reports.

## Features

- 📄 **PDF Bank Statement Import** - Automatically parse and import transactions from bank PDFs
- 📊 **Analytics Dashboard** - View spending by category, trends over time, and monthly/yearly comparisons
- 👥 **Multi-User Support** - Secure authentication with individual expense tracking
- 💰 **Budget Tracking** - Set and monitor budgets by category
- 📈 **Detailed Reports** - Export and analyze spending patterns
- 🎨 **Clean Interface** - Modern, responsive UI built with Tailwind CSS

## Tech Stack

### Backend
- **.NET Core 10** - Web framework and API backend
- **MediatR** - CQRS pattern with mediator for request handling
- **Clean Architecture** - Layered application with strict dependency rules

### Frontend
- **Razor Pages** - Server-side template rendering
- **Tailwind CSS** - Utility-first styling framework
- **Vanilla JavaScript** - Lightweight interactive components

### Database & Auth
- **Supabase** - PostgreSQL database and Supabase Auth for multi-user authentication

## Project Structure

```
SauronSheet/
├── Frontend/                          # Razor Pages web interface
│   ├── Pages/                         # Page handlers (.cshtml.cs) and views (.cshtml)
│   ├── wwwroot/                       # Static assets (Tailwind CSS, JavaScript)
│   └── Program.cs                     # Startup configuration
│
├── Application/                       # Business logic orchestration (CQRS)
│   ├── Features/
│   │   ├── Transactions/              # Transaction import, queries, commands
│   │   ├── Analytics/                 # Spending reports, charts, trends
│   │   └── Users/                     # User management, budgets
│   └── Common/                        # Base handlers, behaviors, DTOs
│
├── Domain/                            # Core business entities and rules
│   ├── Entities/                      # Transaction, User, Category, Budget
│   ├── ValueObjects/                  # Money, UserId, TransactionId
│   ├── Services/                      # Domain services (PDFParser, etc.)
│   └── Specifications/                # Filtering specifications
│
└── Infrastructure/                    # External integrations and persistence
    ├── Persistence/                   # Supabase repositories
    ├── Auth/                          # Supabase authentication
    └── PDF/                           # PDF parsing implementation
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
   - Create a Supabase project at https://supabase.com
   - Copy your project URL and API keys
   - Create `appsettings.json` in `Frontend/`:
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
   Navigate to `https://localhost:7000` (or configured port)

## Architecture

### Clean Architecture with CQRS

SauronSheet follows **Clean Architecture** principles with **CQRS (Command Query Responsibility Segregation)** pattern:

- **Domain Layer** - Contains core business entities and domain logic (no external dependencies)
- **Application Layer** - Orchestrates use cases using MediatR CQRS commands and queries
- **Infrastructure Layer** - Handles data persistence (Supabase) and external services
- **Frontend Layer** - Presents UI and collects user input

### Dependency Rules
```
Frontend ──→ Application ──→ Domain
    ↓                            ↑
Infrastructure ─────────────────┘
```
- High-level modules don't depend on low-level modules
- Both depend on abstractions
- Infrastructure never referenced directly above its layer

### CQRS Pattern

**Commands** (state-changing operations):
```csharp
public class ImportTransactionsFromPdfCommand : IRequest<ImportResult> { }
public class CreateBudgetCommand : IRequest<BudgetId> { }
```

**Queries** (read-only operations):
```csharp
public class GetSpendingByCategoryQuery : IRequest<List<CategorySpending>> { }
public class GetMonthlyTrendsQuery : IRequest<List<MonthlyTrend>> { }
```

All requests routed through **MediatR** pipeline for consistency and middleware support.

## Development Workflow

### Spec-Driven Development

1. **Write Specification** - Define expected behavior in a test
2. **Create CQRS Handler** - Implement command/query handler to satisfy spec
3. **Add Domain Logic** - Implement entities/services needed by handler
4. **Build Infrastructure** - Add repository implementations for Supabase
5. **Wire Frontend** - Create Razor page to trigger command/query
6. **Test End-to-End** - Verify complete flow

### Build Commands

```bash
# Build solution
dotnet build

# Run tests
dotnet test

# Run application (development)
dotnet run --project Frontend/

# Build for production
dotnet publish -c Release

# Apply migrations to Supabase (when using EF Core)
dotnet ef database update --project Infrastructure/
```

### File Organization

- **New Features:** Create folder in `Application/Features/YourFeature/`
  - `Commands/YourCommand.cs` + `YourCommandHandler.cs`
  - `Queries/YourQuery.cs` + `YourQueryHandler.cs`
  - `DTOs/YourDto.cs`
- **New Entities:** Add to `Domain/Entities/YourEntity.cs`
- **New Repositories:** Define interface in `Domain/Repositories/`, implement in `Infrastructure/Persistence/`
- **New Pages:** Create `.cshtml` and `.cshtml.cs` files in `Frontend/Pages/`

## Key Features Explained

### PDF Import
1. User uploads bank PDF in the upload page
2. Backend extracts transaction rows using iTextSharp or similar
3. Validates against domain rules (required fields, amounts format)
4. Creates `Transaction` domain entities
5. Persists to Supabase via repository
6. Returns confirmation to user

### Analytics Dashboard
- Query handlers aggregate spending data from Supabase
- Frontend renders charts using Chart.js or similar library
- Supports filtering by date range, category, budget status

### Multi-User Authentication
- Supabase Auth provides login/signup UI
- JWT tokens stored in cookies/session
- Each page model extracts userId from claims
- Queries automatically scoped to current user

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
- `appsettings.Development.json` - Local development
- `appsettings.Production.json` - Production deployment

## Testing

### Unit Tests (Domain)
```bash
dotnet test --filter "Category=Domain"
```

### Integration Tests (Application)
```bash
dotnet test --filter "Category=Application"
```

### Run All Tests
```bash
dotnet test
```

## Deployment to Vercel

SauronSheet is deployed to **Vercel** (free tier) for scalable, serverless hosting.

### Prerequisites
- Vercel account (free at https://vercel.com)
- GitHub repository connected to Vercel
- Supabase project URL and API keys

### Setup

1. **Create `vercel.json` in Frontend/**
   ```json
   {
     "buildCommand": "dotnet publish -c Release -o ./out",
     "outputDirectory": "./out",
     "framework": "dotnet",
     "nodeVersion": "18.x"
   }
   ```

2. **Connect GitHub to Vercel**
   - Import repository from GitHub
   - Select `Frontend` as the root directory
   - Set build settings as above

3. **Configure Environment Variables**
   - In Vercel dashboard, add environment variables:
     ```
     Supabase__Url=https://your-project.supabase.co
     Supabase__Key=your-public-anon-key
     ```

4. **Deploy**
   - Push to `main` branch
   - Vercel automatically builds and deploys
   - Live URL provided after deployment

### Production Checklist
- [ ] CORS configured in Supabase for Vercel domain
- [ ] Environment variables set in Vercel dashboard
- [ ] Database backups enabled in Supabase
- [ ] Error logging configured (Sentry or similar)
- [ ] Custom domain configured (optional)

## Contributing

1. Create a new branch: `git checkout -b feature/your-feature`
2. Follow spec-driven development workflow
3. Write tests before implementing
4. Ensure all tests pass: `dotnet test`
5. Submit a pull request with detailed description

## Database Schema

Key Supabase tables:

- **users** - User profiles and settings
- **transactions** - Imported bank transactions with category/amount
- **categories** - Expense categories (maintained by users)
- **budgets** - Budget limits per category
- **pdfs** - Metadata about imported PDF files

## Troubleshooting

**"Connection to Supabase failed"**
- Verify API keys in `appsettings.json`
- Check Supabase project is running
- Ensure network allows HTTPS requests

**"PDF import fails"**
- Verify PDF format matches expected bank structure
- Check error logs for parsing issues
- Consider PDF sample validation

## Performance Tips

- Use pagination for transaction lists (1000+ transactions)
- Cache category/budget data in frontend (rarely changes)
- Add database indexes on userId, date columns in Supabase
- Use Analytics queries with proper date filters

## Roadmap

- [ ] Budget alerts and notifications
- [ ] Recurring transaction rules
- [ ] Multi-bank PDF format support
- [ ] Mobile-responsive improvements
- [ ] Export reports as CSV/PDF
- [ ] Spending predictions

## License

[Your License Here]

## Support

For questions or issues:
- 📧 Email: support@example.com
- 🐛 GitHub Issues: https://github.com/yourusername/SauronSheet/issues
- 📚 Documentation: [Link to wiki or docs]

---

**SauronSheet** - Take control of your spending. 💰
