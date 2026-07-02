# Tech Stack

## Runtime & Language
- .NET SDK 10.0.100 (global.json, rollForward: latestMinor)
- .NET 10 / net10.0 / C# latest / Nullable enabled / ImplicitUsings / TreatWarningsAsErrors
- Directory.Build.props shared by all projects (Frontend + Infra override TreatWarningsAsErrors to false)

## Backend
- CQRS: MediatR 12.0.0, MediatR.Extensions.Microsoft.DependencyInjection 11.0.0
- DB: supabase-csharp 0.16.2 (PostgreSQL)
- Auth: supabase-csharp (Supabase Auth)
- Excel: ExcelDataReader 3.7.0
- Monitoring: Sentry.AspNetCore 6.1.0 / Sentry 6.2.0

## Frontend
- ASP.NET Core Razor Pages (built-in, no SPA framework)
- MDBootstrap 5, Alpine.js v3, HTMX v2, Chart.js, Flatpickr, Font Awesome — ALL CDN-only
- No npm local copies. No minified files in repo. Loaded from _Layout.cshtml

## Testing
- Unit: xUnit + Moq
- E2E: Playwright ^1.61.1 (in devDependencies, tests in e2e/)
- Runsettings: test-results/dotnet per test.runsettings
- Coverage targets: Domain 80%, Application 70%

## CI/CD
- GitHub Actions workflow at .github/workflows/master_sauronsheet.yml
