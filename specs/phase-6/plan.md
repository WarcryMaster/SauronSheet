# Implementation Plan: Phase 6 – UI Polish, Performance & Production Deployment

**Branch**: `phase-6-ui-polish` | **Date**: 2026-03-06 | **Spec**: [specs/phase-6/phase-6-spec.md](specs/phase-6/phase-6-spec.md)
**Input**: Feature specification from `/specs/phase-6/phase-6-spec.md`

## Summary

Phase 6 delivers the final polish, performance, accessibility, and production deployment for SauronSheet. The focus is on UI consistency, accessibility (WCAG 2.1 AA), Tailwind CSS build pipeline, error and loading states, Sentry error monitoring, security hardening, and Vercel deployment. No new features or domain logic are introduced; only Frontend and Infrastructure layers are in scope. All prior phases must be complete and stable before starting.

## Technical Context

**Language/Version**: C# 10, .NET 10, JavaScript (ES2020+), Tailwind CSS 3.x  
**Primary Dependencies**: Tailwind CSS (standalone CLI), Alpine.js (CDN, pinned), Chart.js (CDN, pinned), Sentry.AspNetCore, Microsoft.AspNetCore.ResponseCompression  
**Storage**: Supabase PostgreSQL (no schema changes)  
**Testing**: xUnit, Moq, Lighthouse, axe-core, manual browser/assistive tech tests  
**Target Platform**: Vercel (Docker container, .NET 10), fallback: Railway/Render  
**Project Type**: Web (Razor Pages, Clean Architecture)  
**Performance Goals**: TTI < 3s (Lighthouse Slow 4G), FCP < 1.5s desktop, site.css < 50KB  
**Constraints**: No new domain logic; strict CSP (no 'unsafe-inline'); Sentry must not capture financial PII; all UI via Tailwind config; accessibility ≥ 90 (Lighthouse)  
**Scale/Scope**: Single-tenant per user, 1–10k users, 10+ screens, 27 new tests

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- Clean Architecture: No upward references; only Frontend/Infrastructure in scope (Polish phase)
- CQRS/MediatR: No new commands/queries except RequestPasswordResetCommand (auth only)
- DDD: No new entities/VOs; only interface addition for password reset
- Test-First: 27 new tests specified; all must pass before release
- Spec-Driven: Single spec file; all requirements, tests, and deliverables in phase-6-spec.md
- No deliverables cross scope boundaries

**Result:** All gates pass. No constitution violations.

## Project Structure

### Documentation (this feature)

```text
specs/phase-6/
├── plan.md              # This file (/speckit.plan output)
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
└── tasks.md             # Phase 2 output (/speckit.tasks)
```

### Source Code (repository root)

```text
src/
├── SauronSheet.Frontend/
│   ├── Dockerfile
│   ├── vercel.json
│   ├── .vercelignore
│   ├── tailwind.config.js
│   ├── tailwind-input.css
│   ├── Pages/
│   │   ├── Auth/
│   │   │   ├── ForgotPassword.cshtml
│   │   │   ├── ForgotPassword.cshtml.cs
│   │   │   └── Login.cshtml
│   │   ├── NotFound.cshtml
│   │   ├── NotFound.cshtml.cs
│   ├── Shared/
│   │   ├── _Layout.cshtml
│   │   └── Components/
│   │       ├── _Toast.cshtml
│   │       └── _SkipToContent.cshtml
│   ├── wwwroot/
│   │   ├── css/site.css
│   │   ├── js/charts.js
│   │   ├── favicon.svg
│   │   └── images/og-image.png
│   └── Program.cs
├── SauronSheet.Infrastructure/
│   ├── Monitoring/SentryConfiguration.cs
│   ├── Auth/SupabaseAuthService.cs
│   └── Middleware/SecurityHeadersMiddleware.cs
```

**Structure Decision**: Web application, Clean Architecture, Razor Pages frontend, Infrastructure for deployment/monitoring.

## Complexity Tracking

No constitution violations. No complexity justifications required.
