# Phase 6: UI Polish, Performance & Production Deployment

## Quick Reference

- **Status**: Draft
- **Layer Scope**: Frontend + Infrastructure (Polish)
- **Phase Type**: Polish
- **Duration**: Weeks 22–24
- **Goal**: UI refinements, Tailwind build pipeline, accessibility, performance optimization, Vercel deployment, error monitoring, security hardening
- **Depends On**: Phase 0–5 (all prior phases — complete feature set)
- **Unlocks**: 🚀 **Production Release**

> ⚠️ **POLISH PHASE**: Only Frontend and Infrastructure layers in scope. **No new domain entities, application commands/queries, or business logic.** Existing behavior is preserved; only presentation, performance, and deployment concerns are addressed.

---

## Critical Decisions

| ID | Decision | Rationale | Date |
|---|---|---|---|
| CD-6.1 | Tailwind CSS build pipeline via Tailwind CLI (standalone) | No Node.js dependency required; standalone binary purges unused classes and minifies output | 2026-02-15 |
| CD-6.2 | Alpine.js pinned version via CDN with SRI hash | Security best practice; integrity verified on load | 2026-02-15 |
| CD-6.3 | Vercel for hosting (.NET via Docker container) | Free tier, CI/CD integration, auto-deploy on push to main. **Pre-implementation verification:** Confirm Vercel .NET 10 support on free tier Week 22; pivot to Railway/Render if unsupported | 2026-02-15 |
| CD-6.4 | Sentry for error monitoring (.NET + JavaScript) | Industry standard; free tier sufficient for MVP; captures both server and client errors | 2026-02-15 |
| CD-6.5 | WCAG 2.1 AA as accessibility baseline | Legal compliance in many jurisdictions; good UX practice | 2026-02-15 |
| CD-6.6 | Password reset flow via Supabase Auth built-in | No custom implementation needed; Supabase handles email sending | 2026-02-15 |
| CD-6.7 | CSP (Content Security Policy) headers configured | Prevents XSS and data injection attacks in production | 2026-02-15 |
| CD-6.8 | Response caching for static assets (1 year) + no-cache for pages | Standard web performance practice; pages always fresh, assets cached | 2026-02-15 |
| CD-6.9 | Database indexes audit — no new indexes unless performance requires | Phase 3 indexes cover all current query patterns; avoid premature optimization | 2026-02-15 |
| CD-6.10 | No new features — bugfixes and UX improvements only | Constitution: Polish phase scope boundaries; new features deferred to post-production | 2026-02-15 |

---

## Clarifications

### Session 2026-03-06
- **Q1:** CSP Security Posture for Launch → **A (Strict by Default)**
  - CSP must NOT include `'unsafe-inline'` at launch
  - All inline styles refactored to Tailwind classes before production deploy
  - CSP: `default-src 'self'; script-src 'self' https://cdn.jsdelivr.net https://js.sentry-cdn.com; style-src 'self'; img-src 'self' data: https:; font-src 'self' https://fonts.gstatic.com; connect-src 'self' https://*.supabase.co https://*.sentry.io; frame-ancestors 'none'`
  - Acceptance: SC-6.8 explicitly requires strict CSP with no 'unsafe-inline' exceptions

- **Q2:** TTI Performance Target — 3G Profile Definition → **A (Lighthouse "Slow 4G")**
  - Network profile: 1.6 Mbps down, 750 Kbps up, 150ms latency
  - Measured via Lighthouse throttle profile (industry standard, reproducible)
  - Scenario 6.5 & Test T-6.15 updated

- **Q3:** "Consistent UI" Design System → **A (Tailwind Config-Based ONLY)**
  - Leverage `tailwind.config.js` theme; no additional component library
  - Test T-6.01 validates via HTML audit + visual inspection
  - FR-6.01 CSS layers sufficient

- **Q4:** Sentry "PII" Scope — Financial Data Handling → **B (Financial PII Excluded)**
  - Exclude: User ID, email, transaction amounts, budget values, category names, date ranges
  - Include: Error type, stack trace, request path, HTTP method (non-sensitive context only)
  - FR-6.08 implements BeforeSend hook to filter sensitive fields
  - Scenario 6.7 updated; privacy compliance enforced

- **Q5:** Vercel .NET Deployment — Contingency Activation Rule → **A (Pre-Implementation Verification)**
  - **Action (Week 22 start):** Contact Vercel support; confirm Docker .NET 10 support on free tier
  - If unsupported: immediately pivot to Railway.app or Render.com (both support Docker free tier with same Dockerfile)
  - If supported: proceed with FR-6.09 Dockerfile as-is
  - Risk R-6.1 mitigated; deployment platform locked in before Step 1

---

## Executive Summary

### In Scope

| Area | Deliverable |
|---|---|
| Frontend | Tailwind CSS build pipeline: standalone CLI, purge unused classes, minified production output |
| Frontend | Remove Tailwind CDN; replace with compiled CSS file |
| Frontend | Alpine.js pinned version with SRI integrity hash |
| Frontend | Chart.js pinned version with SRI integrity hash |
| Frontend | Loading states for all async operations (spinners, skeleton screens) |
| Frontend | Error states for all pages (user-friendly error messages, retry actions) |
| Frontend | Empty states audit and improvement (consistent messaging, action links) |
| Frontend | Responsive design audit and fixes (mobile-first pass on all pages) |
| Frontend | Accessibility audit and remediation (WCAG 2.1 AA baseline) |
| Frontend | Password reset flow (Supabase Auth integration) |
| Frontend | Session management UI (active session indicator, logout all option) |
| Frontend | User profile display (email, display name, member since) |
| Frontend | Toast notifications for success/error feedback (Alpine.js component) |
| Frontend | Dark mode support (Tailwind `dark:` variants — optional, time-permitting) |
| Frontend | Favicon and meta tags (Open Graph, description, title per page) |
| Frontend | 404 Not Found page (styled, consistent with layout) |
| Frontend | Print-friendly stylesheet for transaction list and budget comparison |
| Infrastructure | Vercel deployment configuration (`vercel.json`, Dockerfile, environment variables) |
| Infrastructure | CI/CD pipeline: auto-deploy on push to main branch |
| Infrastructure | Sentry error monitoring (.NET SDK + JavaScript SDK) |
| Infrastructure | Security headers: CSP, X-Content-Type-Options, X-Frame-Options, Referrer-Policy |
| Infrastructure | CORS configuration for Supabase ↔ Vercel domain |
| Infrastructure | Response caching and compression middleware |
| Infrastructure | Database indexes audit (verify existing indexes cover all query patterns) |
| Infrastructure | Health check endpoint (`/health`) for monitoring |
| Infrastructure | Environment-specific configuration (Development vs Production) |
| Infrastructure | Custom domain configuration documentation (optional) |
| Tests | ≥20 tests (performance benchmarks, deployment smoke tests, accessibility checks) |

### Deferred (NOT in this phase — Post-Production Backlog)

| Item | Reason |
|---|---|
| New features (any) | Polish phase: presentation + deployment only |
| Multi-currency support | Major domain change |
| Social login (Google, GitHub) | Requires OAuth configuration |
| Multi-factor authentication | Not required for expense tracking MVP |
| Push notifications for budget alerts | Requires notification infrastructure |
| Scheduled reports / background jobs | Requires job scheduler |
| AI-powered spending suggestions | ML infrastructure needed |
| Account deletion (GDPR) | Legal review needed |
| Data export (CSV/PDF) | Feature addition |
| Recurring transaction rules | Feature addition |

---

## User Scenarios & Testing

### Scenario 6.1: Polished UI Experience

**As a** user
**I want** the application to have a consistent, polished, and responsive UI
**So that** I have a professional and enjoyable user experience

**Acceptance Criteria:**
- Consistent styling across all pages via `tailwind.config.js` theme (colors, spacing, typography)
- All interactive elements use Tailwind classes from theme (no hardcoded hex values or pixel sizes)
- All form inputs use consistent styling (borders, focus rings, error states — per FR-6.01 config)
- Buttons use consistent sizing and color scheme (primary, secondary, danger — per FR-6.01 CSS layers)
- Icons are consistent (same icon library or style throughout)
- No Tailwind class conflicts or CSS overrides visible
- All pages render correctly with compiled CSS from `site.css`
- Responsive design: all pages usable on 320px (mobile), 768px (tablet), 1024px+ (desktop)
- No horizontal scroll on any viewport width ≥ 320px
- Smooth transitions for interactive elements (dropdowns, modals, accordions)
- All text is readable (minimum 16px body, adequate contrast ratios)

### Scenario 6.2: Loading, Error, and Empty States

**As a** user
**I want** clear feedback when the app is loading, encounters errors, or has no data
**So that** I always know what's happening and what to do next

**Acceptance Criteria:**
- **Loading states:**
  - Spinner/skeleton shown during page loads and form submissions
  - PDF upload shows progress indicator
  - Dashboard charts show loading placeholder before data renders
  - "Loading..." text for screen readers
- **Error states:**
  - Server errors show user-friendly message (not stack trace)
  - Network errors show "Connection lost. Please try again." with retry button
  - Form validation errors shown inline with red text under field
  - Global error toast for unexpected errors
- **Empty states (per page):**

| Page | Empty State Message | Action Link |
|---|---|---|
| Transaction list | "No transactions yet." | Import PDF / Add manually |
| Dashboard | "No spending data. Import or add transactions to see analytics." | Import PDF / Add manually |
| Categories | "Default categories are ready. Create custom ones below." | Create category form |
| Budgets | "No budgets set for {month}. Create one to start tracking." | Create budget |
| Budget comparison | "No data for {month}. Set budgets and add transactions first." | Create budget / Import PDF |
| Search results | "No transactions match your filters." | Clear filters button |

### Scenario 6.3: Accessibility Compliance

**As a** user with disabilities
**I want** the application to be accessible
**So that** I can use it with assistive technology

**Acceptance Criteria:**
- All images have `alt` text (or `role="presentation"` for decorative)
- All form inputs have associated `<label>` elements
- All interactive elements are keyboard-navigable (Tab, Enter, Escape)
- Focus visible on all interactive elements (`:focus-visible` ring)
- Color is not the only indicator of state (icons/text accompany color coding)
- ARIA labels on icon-only buttons (e.g., delete, edit)
- Minimum contrast ratio 4.5:1 for normal text, 3:1 for large text (WCAG AA)
- Skip to main content link
- `lang` attribute on `<html>` element
- Page titles unique and descriptive (e.g., "Transactions - SauronSheet")
- Charts have text alternatives (summary table or `aria-label`)
- Modal dialogs trap focus and have proper ARIA roles
- Budget status communicated via text + color (not color alone)

### Scenario 6.4: Password Reset Flow

**As a** user who forgot my password
**I want to** reset my password via email
**So that** I can regain access to my account

**Acceptance Criteria:**
- "Forgot password?" link on login page
- Password reset page at `/Auth/ForgotPassword`
- Form: email input + submit button
- On submit: Supabase sends password reset email
- Success message: "If an account exists with that email, you'll receive a reset link."
  - (Generic message prevents email enumeration)
- Reset link in email → Supabase hosted reset page → user sets new password
- After reset: user can log in with new password
- Invalid/expired reset links show appropriate error

### Scenario 6.5: Performance Optimization

**As a** user
**I want** the application to load and respond quickly
**So that** I can use it efficiently without frustration

**Acceptance Criteria:**
- Time to Interactive (TTI) under 3 seconds on Lighthouse "Slow 4G" profile (1.6 Mbps, 750 Kbps, 150ms latency)
- First Contentful Paint (FCP) under 1.5 seconds on desktop (unthrottled)
- Tailwind CSS file size < 50KB after purge and minification (vs. ~3MB CDN)
- Static assets cached for 1 year (cache-busting via file hash)
- HTML responses use gzip/brotli compression
- No render-blocking JavaScript (defer/async on all scripts)
- Lazy loading for Chart.js (load only on pages with charts)
- Database queries complete within 500ms (verified via logging)
- No N+1 query patterns in any handler

### Scenario 6.6: Production Deployment

**As a** developer
**I want to** deploy the application to production on Vercel
**So that** users can access the application on the internet

**Acceptance Criteria:**
- Vercel deployment configured and successful
- Application accessible via Vercel-provided URL (e.g., `sauronsheet.vercel.app`)
- Environment variables set in Vercel dashboard (Supabase URL, Key, JWT Secret, Sentry DSN)
- Automatic deployment on push to `main` branch
- HTTPS enforced (Vercel provides SSL automatically)
- CORS configured in Supabase for Vercel domain
- Health check endpoint (`/health`) returns 200 OK with system status
- Error monitoring active (Sentry captures errors in production)
- Custom domain configured (optional, documented)

### Scenario 6.7: Security Hardening

**As a** security-conscious user
**I want** the application to follow security best practices
**So that** my financial data is protected

**Acceptance Criteria:**
- Security headers set on all responses:
  - `Content-Security-Policy`: restrict script/style sources
  - `X-Content-Type-Options: nosniff`
  - `X-Frame-Options: DENY`
  - `Referrer-Policy: strict-origin-when-cross-origin`
  - `Permissions-Policy: camera=(), microphone=(), geolocation=()`
- JWT cookies: HttpOnly, Secure, SameSite=Strict (verified in production)
- No sensitive data in URL parameters (passwords, tokens)
- Supabase anon key used (not service key) in frontend
- Rate limiting on auth endpoints (Supabase built-in)
- Error messages don't leak implementation details in production
- `X-Powered-By` header removed
- **Financial data NOT sent to Sentry:** User ID, email, transaction amounts, budget values, category names
- **Only logged to Sentry:** Error type, stack trace, request path/method (anonymized context)

---

## Functional Requirements

### FR-6.01: Tailwind CSS Build Pipeline
Frontend/
├── tailwind.config.js # NEW: Tailwind configuration
├── tailwind-input.css # NEW: Tailwind directives (@tailwind base/components/utilities)
├── wwwroot/
│ └── css/
| │ ├── site.css # REPLACED: compiled + purged + minified Tailwind output
│ └── site.css.map # Source map (development only)

#### tailwind.config.js

```javascript
/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    './Pages/**/*.cshtml',
    './Shared/**/*.cshtml',
    './wwwroot/js/**/*.js'
  ],
  darkMode: 'class',
  theme: {
    extend: {
      colors: {
        primary: {
          50: '#eff6ff',
          100: '#dbeafe',
          500: '#3b82f6',
          600: '#2563eb',
          700: '#1d4ed8',
        },
        budget: {
          green: '#10B981',
          yellow: '#F59E0B',
          red: '#EF4444',
          overage: '#B91C1C',
        }
      }
    }
  },
  plugins: []
}
tailwind-input.css
css
@tailwind base;
@tailwind components;
@tailwind utilities;

@layer components {
  .btn-primary {
    @apply bg-primary-600 text-white px-4 py-2 rounded-md hover:bg-primary-700 focus:ring-2 focus:ring-primary-500 focus:outline-none transition-colors;
  }
  .btn-secondary {
    @apply bg-gray-200 text-gray-800 px-4 py-2 rounded-md hover:bg-gray-300 focus:ring-2 focus:ring-gray-400 focus:outline-none transition-colors;
  }
  .btn-danger {
    @apply bg-red-600 text-white px-4 py-2 rounded-md hover:bg-red-700 focus:ring-2 focus:ring-red-500 focus:outline-none transition-colors;
  }
  .input-field {
    @apply border border-gray-300 rounded-md px-3 py-2 w-full focus:ring-2 focus:ring-primary-500 focus:border-primary-500 focus:outline-none;
  }
  .input-error {
    @apply border-red-500 focus:ring-red-500 focus:border-red-500;
  }
  .card {
    @apply bg-white rounded-lg shadow-md p-6;
  }
}
Build Commands
bash
# Development (watch mode)
npx tailwindcss -i ./tailwind-input.css -o ./wwwroot/css/site.css --watch

# Production (purged + minified)
npx tailwindcss -i ./tailwind-input.css -o ./wwwroot/css/site.css --minify

# Or using standalone CLI (no Node.js required)
./tailwindcss -i ./tailwind-input.css -o ./wwwroot/css/site.css --minify
```

#### Layout Update (Remove CDN)

```html
<!-- BEFORE (Phase 0–5): CDN -->
<!-- <script src="https://cdn.tailwindcss.com"></script> -->

<!-- AFTER (Phase 6): Compiled CSS -->
<link rel="stylesheet" href="~/css/site.css" asp-append-version="true" />
```

### FR-6.02: Script Optimization

#### Updated _Layout.cshtml Head

```html
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <meta name="description" content="SauronSheet — Track your expenses, import bank statements, and visualize spending." />
    <meta property="og:title" content="SauronSheet" />
    <meta property="og:description" content="Multi-user expense tracking with PDF import and analytics." />
    <meta property="og:type" content="website" />

    <title>@ViewData["Title"] - SauronSheet</title>

    <!-- Compiled Tailwind CSS -->
    <link rel="stylesheet" href="~/css/site.css" asp-append-version="true" />

    <!-- Favicon -->
    <link rel="icon" type="image/svg+xml" href="~/favicon.svg" />

    <!-- Sentry JavaScript SDK (production only) -->
    @if (!HostEnvironment.IsDevelopment())
    {
        <script src="https://js.sentry-cdn.com/your-dsn-key.min.js"
                crossorigin="anonymous"
                integrity="sha384-..."
                defer></script>
    }
</head>
```

#### Updated _Layout.cshtml Scripts (Before Closing Body)

```html
    <!-- Alpine.js (pinned version + SRI) -->
    <script src="https://cdn.jsdelivr.net/npm/alpinejs@3.14.0/dist/cdn.min.js"
            integrity="sha384-..."
            crossorigin="anonymous"
            defer></script>

    <!-- Chart.js (loaded only on pages that need it) -->
    @if (ViewData["LoadChartJs"] != null)
    {
        <script src="https://cdn.jsdelivr.net/npm/chart.js@4.4.0/dist/chart.umd.min.js"
                integrity="sha384-..."
                crossorigin="anonymous"
                defer></script>
    }

    <!-- Page-specific scripts -->
    @await RenderSectionAsync("Scripts", required: false)
</body>
```

### FR-6.03: Password Reset Flow

Frontend/
├── Pages/
│   └── Auth/
│       ├── Login.cshtml               # UPDATED: add "Forgot password?" link
│       ├── Login.cshtml.cs
│       ├── ForgotPassword.cshtml      # NEW
│       ├── ForgotPassword.cshtml.cs   # NEW
│       ├── Register.cshtml            # (unchanged)
│       └── Register.cshtml.cs

#### ForgotPassword PageModel

```csharp
public class ForgotPasswordModel : PageModel
{
    private readonly IMediator _mediator;

    [BindProperty]
    public string Email { get; set; } = string.Empty;

    public bool EmailSent { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Email))
        {
            ModelState.AddModelError(nameof(Email), "Email is required.");
            return Page();
        }

        // Always show success to prevent email enumeration
        await _mediator.Send(new RequestPasswordResetCommand(Email));
        EmailSent = true;
        return Page();
    }
}
```

#### RequestPasswordResetCommand (Application)

```csharp
public record RequestPasswordResetCommand(string Email) : IRequest<Unit>, IAnonymousRequest;
```

**Handler Flow:**

1. Call `IAuthService.RequestPasswordResetAsync(email)`
2. Supabase sends reset email if account exists
3. Return `Unit` (success regardless — prevents enumeration)
4. Log attempt for audit purposes

#### IAuthService Addition

```csharp
// Add to existing IAuthService interface (Domain)
Task RequestPasswordResetAsync(string email);
```

**Note:** This is a minor addition to an existing Domain interface. It does not create new domain entities or business logic. The implementation is purely in Infrastructure (Supabase API call).

### FR-6.04: Toast Notification Component

```html
<!-- Shared/Components/_Toast.cshtml -->
<div x-data="toastManager()"
     x-on:show-toast.window="show($event.detail)"
     class="fixed top-4 right-4 z-50 space-y-2">
    <template x-for="toast in toasts" :key="toast.id">
        <div x-show="toast.visible"
             x-transition:enter="transition ease-out duration-300"
             x-transition:enter-start="opacity-0 translate-x-8"
             x-transition:enter-end="opacity-100 translate-x-0"
             x-transition:leave="transition ease-in duration-200"
             x-transition:leave-start="opacity-100"
             x-transition:leave-end="opacity-0"
             :class="{
                 'bg-green-50 border-green-500 text-green-800': toast.type === 'success',
                 'bg-red-50 border-red-500 text-red-800': toast.type === 'error',
                 'bg-yellow-50 border-yellow-500 text-yellow-800': toast.type === 'warning',
                 'bg-blue-50 border-blue-500 text-blue-800': toast.type === 'info'
             }"
             class="border-l-4 p-4 rounded-md shadow-lg max-w-sm"
             role="alert"
             :aria-live="toast.type === 'error' ? 'assertive' : 'polite'">
            <div class="flex items-center justify-between">
                <p x-text="toast.message" class="text-sm font-medium"></p>
                <button x-on:click="dismiss(toast.id)"
                        class="ml-4 text-gray-400 hover:text-gray-600"
                        aria-label="Dismiss notification">
                    ✕
                </button>
            </div>
        </div>
    </template>
</div>

<script>
function toastManager() {
    return {
        toasts: [],
        show({ message, type = 'info', duration = 5000 }) {
            const id = Date.now();
            this.toasts.push({ id, message, type, visible: true });
            if (duration > 0) {
                setTimeout(() => this.dismiss(id), duration);
            }
        },
        dismiss(id) {
            const toast = this.toasts.find(t => t.id === id);
            if (toast) toast.visible = false;
            setTimeout(() => {
                this.toasts = this.toasts.filter(t => t.id !== id);
            }, 300);
        }
    };
}
</script>
FR-6.05: Health Check Endpoint
csharp
// In Program.cs
app.MapGet("/health", async (Supabase.Client supabase) =>
{
    try
    {
        // Verify Supabase connectivity
        // Return 200 with status
        return Results.Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            version = "1.0.0",
            checks = new
            {
                database = "connected",
                auth = "available"
            }
        });
    }
    catch (Exception ex)
    {
        return Results.Json(new
        {
            status = "unhealthy",
            timestamp = DateTime.UtcNow,
            error = "Service unavailable"
        }, statusCode: 503);
    }
});
FR-6.06: Security Headers Middleware
csharp
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
        headers.Remove("X-Powered-By");

        // CSP: Strict by default (no unsafe-inline; all inline styles refactored to Tailwind)
        headers["Content-Security-Policy"] = string.Join("; ",
            "default-src 'self'",
            "script-src 'self' https://cdn.jsdelivr.net https://js.sentry-cdn.com",
            "style-src 'self'",
            "img-src 'self' data: https:",
            "font-src 'self' https://fonts.gstatic.com",
            "connect-src 'self' https://*.supabase.co https://*.sentry.io",
            "frame-ancestors 'none'"
        );

        await _next(context);
    }
}
```

### FR-6.07: Response Caching & Compression

```csharp
// In Program.cs

// Compression
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});

builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Optimal;
});

// Static file caching
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // Cache static assets for 1 year (cache-busted via asp-append-version)
        ctx.Context.Response.Headers.Append("Cache-Control", "public, max-age=31536000, immutable");
    }
});

// No caching for Razor Pages (dynamic content)
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/health") ||
        context.Request.Path.Value?.EndsWith(".cshtml") == true ||
        !context.Request.Path.StartsWithSegments("/css") &&
        !context.Request.Path.StartsWithSegments("/js") &&
        !context.Request.Path.StartsWithSegments("/images"))
    {
        context.Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
    }
    await next();
});
```

### FR-6.08: Sentry Error Monitoring

Infrastructure/
├── Monitoring/
│   └── SentryConfiguration.cs         # NEW

```csharp
// In Program.cs
builder.WebHost.UseSentry(options =>
{
    options.Dsn = builder.Configuration["Sentry:Dsn"];
    options.Environment = builder.Environment.EnvironmentName;
    options.TracesSampleRate = builder.Environment.IsProduction() ? 0.1 : 1.0;
    options.SendDefaultPii = false; // Don't send personal data
    
    // Custom BeforeSend hook to filter financial PII
    options.BeforeSend = (transaction, hint) =>
    {
        // Remove sensitive contexts
        if (transaction.Contexts.ContainsKey("transaction"))
        {
            transaction.Contexts.Remove("transaction");
        }
        
        // Filter request data: remove cookies, body (may contain transaction data)
        if (transaction.Request != null)
        {
            transaction.Request.Cookies = null;
            transaction.Request.Data = null; // POST body
        }
        
        // Filter extra context for financial PII
        if (transaction.Extra != null)
        {
            var keysToRemove = transaction.Extra.Keys
                .Where(k => k.Contains("transaction", StringComparison.OrdinalIgnoreCase) ||
                           k.Contains("amount", StringComparison.OrdinalIgnoreCase) ||
                           k.Contains("budget", StringComparison.OrdinalIgnoreCase) ||
                           k.Contains("category", StringComparison.OrdinalIgnoreCase) ||
                           k.Contains("email", StringComparison.OrdinalIgnoreCase) ||
                           k.Contains("user", StringComparison.OrdinalIgnoreCase))
                .ToList();
            
            foreach (var key in keysToRemove)
            {
                transaction.Extra.Remove(key);
            }
        }
        
        return transaction;
    };
});

// After app.Build()
app.UseSentryTracing();
```

**appsettings.Production.json:**

```json
{
  "Sentry": {
    "Dsn": "https://your-sentry-dsn@sentry.io/project-id"
  }
}
```

### FR-6.09: Vercel Deployment Configuration

Frontend/
├── Dockerfile                         # NEW: .NET containerized build
├── vercel.json                        # NEW: Vercel configuration
├── .vercelignore                      # NEW: Files to exclude

#### Dockerfile

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY *.sln .
COPY src/SauronSheet.Domain/*.csproj src/SauronSheet.Domain/
COPY src/SauronSheet.Application/*.csproj src/SauronSheet.Application/
COPY src/SauronSheet.Infrastructure/*.csproj src/SauronSheet.Infrastructure/
COPY src/SauronSheet.Frontend/*.csproj src/SauronSheet.Frontend/

# Restore
RUN dotnet restore

# Copy everything and build
COPY . .
RUN dotnet publish src/SauronSheet.Frontend/ -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Set environment
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080
ENTRYPOINT ["dotnet", "SauronSheet.Frontend.dll"]
vercel.json
json
{
  "version": 2,
  "builds": [
    {
      "src": "Dockerfile",
      "use": "@vercel/static-build",
      "config": {
        "distDir": "."
      }
    }
  ],
  "routes": [
    {
      "src": "/(.*)",
      "dest": "/"
    }
  ]
}
Note: Vercel .NET deployment may require Docker-based builds. Alternative: deploy to Azure App Service free tier or Railway.app if Vercel .NET support is limited. The Dockerfile works with any container host.

Environment Variables (Vercel Dashboard)
Variable	Value	Notes
Supabase__Url	https://your-project.supabase.co	Supabase project URL
Supabase__Key	your-anon-key	Public anon key only
Supabase__JwtSecret	your-jwt-secret	For JWT validation
Sentry__Dsn	https://...@sentry.io/...	Error monitoring
ASPNETCORE_ENVIRONMENT	Production	Runtime environment
FR-6.10: 404 Not Found Page
csharp
// Pages/NotFound.cshtml.cs
public class NotFoundModel : PageModel
{
    public void OnGet() { }
}

// In Program.cs
app.UseStatusCodePagesWithReExecute("/NotFound");
```

**View Requirements:**
- Centered card with "404 — Page Not Found" heading
- Friendly message: "The page you're looking for doesn't exist or has been moved."
- Navigation links: Dashboard, Transactions, Home
- Consistent with layout styling
- Accessible (proper heading hierarchy, link descriptions)

### FR-6.11: Print Stylesheet

```css
/* In tailwind-input.css or separate print.css */
@media print {
    nav, footer, .no-print, button, [x-data] { display: none !important; }
    body { font-size: 12pt; color: #000; background: #fff; }
    .card { box-shadow: none; border: 1px solid #ddd; }
    table { border-collapse: collapse; width: 100%; }
    th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }
    .budget-progress-bar { border: 1px solid #999; }
    a { color: #000; text-decoration: none; }
    a::after { content: " (" attr(href) ")"; font-size: 0.8em; color: #666; }
}
```

### FR-6.12: Updated Program.cs (Complete)

```csharp
var builder = WebApplication.CreateBuilder(args);

// Sentry
builder.WebHost.UseSentry(o =>
{
    o.Dsn = builder.Configuration["Sentry:Dsn"];
    o.Environment = builder.Environment.EnvironmentName;
    o.TracesSampleRate = builder.Environment.IsProduction() ? 0.1 : 1.0;
    o.SendDefaultPii = false;
});

// Layer registrations
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// Auth services
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserContext, HttpUserContext>();

// Compression
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});

// Razor Pages
builder.Services.AddRazorPages();

var app = builder.Build();

// Middleware pipeline (order matters)
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/NotFound");
app.UseResponseCompression();
app.UseHttpsRedirection();

// Security headers
app.UseMiddleware<SecurityHeadersMiddleware>();

// Static files with caching
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.Append(
            "Cache-Control", "public, max-length=31536000, immutable");
    }
});

app.UseRouting();
app.UseSentryTracing();

// Auth middleware
app.UseMiddleware<JwtCookieMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

// Health check
app.MapGet("/health", /* ... */);

app.MapRazorPages();
app.Run();
```

## Architecture Notes

### Deployment Architecture

```
┌─────────────────────────────────────────────────────────────┐
│ Internet │
└───────────────────────────┬─────────────────────────────────┘
│ HTTPS
▼
┌─────────────────────────────────────────────────────────────┐
│ Vercel Edge Network │
│ (CDN, SSL termination, routing) │
└───────────────────────────┬─────────────────────────────────┘
│
▼
┌─────────────────────────────────────────────────────────────┐
│ Vercel Serverless / Container │
│ │
│ ┌─────────────────────────────────────────────────────┐ │
│ │ SauronSheet.Frontend (.NET 10) │ │
│ │ │ │
│ │ Program.cs → Middleware Pipeline: │ │
│ │ SecurityHeaders → Compression → Static Files │ │
│ │ → JwtCookie → Auth → Sentry → Razor Pages │ │
│ │ │ │
│ │ Layers: │ │
│ │ Frontend (Razor Pages + Tailwind + Alpine.js) │ │
│ │ Application (MediatR handlers) │ │
│ │ Domain (entities, VOs, services) │ │
│ │ Infrastructure (Supabase client, PDF parser) │ │
│ └──────────────────────┬──────────────────────────────┘ │
│ │ │
└──────────────────────────┼──────────────────────────────────┘
│ HTTPS (Postgrest API + Auth API)
▼
┌─────────────────────────────────────────────────────────────┐
│ Supabase Cloud │
│ │
│ ┌──────────────┐ ┌──────────────┐ ┌──────────────────┐ │
│ │ PostgreSQL │ │ Auth (JWT) │ │ Row Level │ │
│ │ Database │ │ Service │ │ Security (RLS) │ │
│ │ │ │ │ │ │ │
│ │ Tables: │ │ Users │ │ Policies on │ │
│ │ - users │ │ JWT tokens │ │ all tables │ │
│ │ - transactions│ │ Password │ │ │ │
│ │ - categories │ │ reset │ │ │ │
│ │ - budgets │ │ │ │ │ │
│ │ - pdf_imports│ │ │ │ │ │
│ └──────────────┘ └──────────────┘ └──────────────────┘ │
└─────────────────────────────────────────────────────────────┘
│
▼
┌─────────────────────────────────────────────────────────────┐
│ Sentry Cloud │
│ (Error monitoring + performance) │
└─────────────────────────────────────────────────────────────┘
```

---

### NuGet Packages (Phase 6 Additions)

| Project                       | New Packages                                  | Notes                                    |
|-------------------------------|-----------------------------------------------|------------------------------------------|
| `SauronSheet.Domain`         | **None** (still zero)                         | Constitution mandate maintained          |
| `SauronSheet.Application`    | None                                          | No new packages                          |
| `SauronSheet.Infrastructure` | `Sentry.AspNetCore`                           | Error monitoring SDK                     |
| `SauronSheet.Frontend`       | `Microsoft.AspNetCore.ResponseCompression`    | Built-in (may already be available)      |

### File Structure (Phase 6 Additions)
Frontend/
├── Dockerfile # NEW
├── vercel.json # NEW
├── .vercelignore # NEW
├── tailwind.config.js # NEW
├── tailwind-input.css # NEW
├── Pages/
│ ├── Auth/
│ │ ├── ForgotPassword.cshtml # NEW
│ │ ├── ForgotPassword.cshtml.cs # NEW
│ │ └── Login.cshtml # UPDATED (forgot password link)
│ ├── NotFound.cshtml # NEW
│ ├── NotFound.cshtml.cs # NEW
│ └── (all existing pages — UI polished)
├── Shared/
│ ├── _Layout.cshtml # UPDATED (CDN → compiled CSS, SRI, meta tags)
│ ├── Components/
│ │ ├── _Toast.cshtml # NEW
│ │ └── _SkipToContent.cshtml # NEW (accessibility)
├── wwwroot/
│ ├── css/
│ │ └── site.css # REPLACED (compiled Tailwind)
│ ├── js/
│ │ └── charts.js # UPDATED (lazy loading)
│ ├── favicon.svg # NEW
│ └── images/
│ └── og-image.png # NEW (Open Graph)
└── Program.cs # UPDATED (compression, security, Sentry, health)

Infrastructure/
├── Monitoring/
│ └── SentryConfiguration.cs # NEW
├── Auth/
│ └── SupabaseAuthService.cs # UPDATED (password reset method)
└── Middleware/
└── SecurityHeadersMiddleware.cs # NEW

text

---

## Test Specifications

### UI Polish Tests
TEST T-6.01: UI_ConsistentStyling_AllPages
GIVEN the application is running with compiled Tailwind CSS (site.css from tailwind.config.js theme)
WHEN each page is loaded (Dashboard, Transactions, Categories, Budgets, Search, Upload)
THEN all pages use button classes from tailwind.config.js (@layer components: .btn-primary, .btn-secondary, .btn-danger)
AND all form inputs use .input-field and .input-error classes
AND all cards use .card class
AND no inline styles or hardcoded colors override Tailwind theme
AND visual inspection confirms consistent spacing (via margin/padding Tailwind scale)
AND no CSS class conflicts or overrides in DevTools Styles panel

TEST T-6.02: UI_ResponsiveDesign_Mobile320
GIVEN the application is loaded in a 320px viewport
WHEN each page is viewed
THEN no horizontal scrollbar appears
AND all content is readable without zooming
AND navigation collapses to hamburger menu
AND tables convert to card layout where appropriate

TEST T-6.03: UI_ResponsiveDesign_Tablet768
GIVEN the application is loaded in a 768px viewport
WHEN each page is viewed
THEN layout adapts appropriately (2-column where suitable)
AND charts are readable at this width
AND navigation is functional

TEST T-6.04: UI_ResponsiveDesign_Desktop1024
GIVEN the application is loaded in a 1024px+ viewport
WHEN each page is viewed
THEN full desktop layout is displayed
AND sidebar/navigation fully visible
AND charts display side-by-side where applicable

TEST T-6.05: UI_LoadingStates_Visible
GIVEN a slow network connection (simulated)
WHEN a page with async data is loaded (Dashboard, Transaction list)
THEN loading spinners or skeleton screens are visible
AND content replaces loading state when data arrives

TEST T-6.06: UI_ErrorStates_UserFriendly
GIVEN an API error occurs during page load
WHEN the error page renders
THEN a user-friendly error message is displayed (not stack trace)
AND a "Try again" or "Go back" action is available
AND the error is logged to Sentry

TEST T-6.07: UI_EmptyStates_AllPages
GIVEN a new user with no data
WHEN each data page is viewed (Transactions, Dashboard, Budgets, Categories)
THEN appropriate empty state message is shown
AND action links guide the user (Import PDF, Add Transaction, Create Budget)

TEST T-6.08: UI_ToastNotifications_ShowAndDismiss
GIVEN a user completes an action (create category, delete transaction, etc.)
WHEN the action succeeds or fails
THEN a toast notification appears in the top-right corner
AND success toasts are green, error toasts are red
AND toasts auto-dismiss after 5 seconds
AND toasts can be manually dismissed via close button

text

### Accessibility Tests
TEST T-6.09: A11y_FormLabels_AllInputs
GIVEN any page with form inputs
WHEN the HTML is inspected
THEN every 
, 
 has an associated <label>
OR has an aria-label / aria-labelledby attribute

TEST T-6.10: A11y_KeyboardNavigation_AllPages
GIVEN any page is loaded
WHEN navigating using only Tab, Shift+Tab, Enter, and Escape keys
THEN all interactive elements can be reached and activated
AND focus is visible on the currently focused element
AND modal dialogs trap focus within the modal

TEST T-6.11: A11y_ContrastRatios_MeetWCAG
GIVEN the application's color scheme
WHEN contrast ratios are measured (e.g., via axe-core or Lighthouse)
THEN all text meets WCAG AA minimum contrast (4.5:1 normal, 3:1 large)
AND budget status colors have accompanying text labels (not color-only)

TEST T-6.12: A11y_ScreenReaderCompatibility
GIVEN a screen reader user navigates the dashboard
WHEN charts are encountered
THEN chart data is available as text alternative (aria-label or summary table)
AND budget status is communicated via text (not just color)
AND navigation landmarks are present (main, nav, footer)

TEST T-6.13: A11y_SkipToContent_Present
GIVEN any page is loaded
WHEN the first Tab keypress is made
THEN a "Skip to main content" link becomes visible
AND activating it moves focus to the main content area

### Performance Tests
TEST T-6.14: Perf_TailwindCSS_PurgedSize
GIVEN the Tailwind build has been run with --minify
WHEN the output CSS file size is measured
THEN site.css is less than 50KB (vs ~3MB CDN version)

TEST T-6.15: Perf_TTI_Under3Seconds
GIVEN a mobile network simulation (Lighthouse "Slow 4G": 1.6 Mbps, 750 Kbps, 150ms)
WHEN the dashboard page is loaded
THEN Time to Interactive (TTI) is under 3 seconds
(Measured via Lighthouse throttle profile; WebPageTest also acceptable if Slow 4G used)

TEST T-6.16: Perf_ResponseCompression_Active
GIVEN a request to any page
WHEN the response headers are inspected
THEN Content-Encoding is brotli or gzip
AND the response body is compressed

TEST T-6.17: Perf_StaticAssets_Cached
GIVEN a request to a static asset (CSS, JS, image)
WHEN the response headers are inspected
THEN Cache-Control includes max-age=31536000 and immutable
AND the URL includes a version hash (asp-append-version)

### Security Tests
TEST T-6.18: Security_Headers_Present
GIVEN a request to any page
WHEN the response headers are inspected
THEN X-Content-Type-Options is "nosniff"
AND X-Frame-Options is "DENY"
AND Referrer-Policy is "strict-origin-when-cross-origin"
AND Content-Security-Policy is set
AND X-Powered-By header is NOT present

TEST T-6.19: Security_CSP_BlocksInlineScript
GIVEN the Content-Security-Policy header is set
WHEN an inline script injection is attempted
THEN the browser blocks the script execution
(Verified via CSP reporting or manual test)

### Deployment Tests
TEST T-6.20: Deploy_HealthCheck_Returns200
GIVEN the application is deployed to production
WHEN GET /health is called
THEN response status is 200
AND body contains { "status": "healthy" }
AND database check returns "connected"

TEST T-6.21: Deploy_ProductionBuild_Succeeds
GIVEN the Dockerfile
WHEN docker build is executed
THEN the build completes successfully
AND the resulting image runs without errors

TEST T-6.22: Deploy_EnvironmentVariables_Configured
GIVEN the production environment
WHEN the application starts
THEN Supabase URL, Key, and JwtSecret are resolved
AND Sentry DSN is resolved
AND no configuration validation errors

TEST T-6.23: Deploy_HTTPS_Enforced
GIVEN a production deployment
WHEN an HTTP request is made
THEN it is redirected to HTTPS (301/302)

TEST T-6.24: Deploy_SentryCaptures_Errors
GIVEN a production deployment with Sentry configured
WHEN an unhandled exception occurs
THEN the error is captured in Sentry dashboard
AND includes stack trace, user context (anonymized), and request info

### Password Reset Tests
TEST T-6.25: PasswordReset_ValidEmail_SendsEmail
GIVEN a registered email "user@example.com"
WHEN RequestPasswordResetCommand is handled
THEN IAuthService.RequestPasswordResetAsync is called with the email
AND no exception is thrown

TEST T-6.26: PasswordReset_UnknownEmail_NoException
GIVEN an email not in the system "unknown@example.com"
WHEN RequestPasswordResetCommand is handled
THEN no exception is thrown (prevents email enumeration)
AND IAuthService.RequestPasswordResetAsync is still called

TEST T-6.27: PasswordReset_EmptyEmail_ValidationError
GIVEN an empty email string
WHEN the ForgotPassword form is submitted
THEN validation error "Email is required" is displayed
AND IAuthService.RequestPasswordResetAsync is NOT called

---

## Test Summary

| Test ID | Test Name                                            | Category       | Area              |
|---------|------------------------------------------------------|----------------|-------------------|
| T-6.01  | UI_ConsistentStyling_AllPages                        | Frontend       | UI Polish         |
| T-6.02  | UI_ResponsiveDesign_Mobile320                        | Frontend       | Responsive        |
| T-6.03  | UI_ResponsiveDesign_Tablet768                        | Frontend       | Responsive        |
| T-6.04  | UI_ResponsiveDesign_Desktop1024                      | Frontend       | Responsive        |
| T-6.05  | UI_LoadingStates_Visible                             | Frontend       | UX States         |
| T-6.06  | UI_ErrorStates_UserFriendly                          | Frontend       | UX States         |
| T-6.07  | UI_EmptyStates_AllPages                              | Frontend       | UX States         |
| T-6.08  | UI_ToastNotifications_ShowAndDismiss                 | Frontend       | UX States         |
| T-6.09  | A11y_FormLabels_AllInputs                            | Frontend       | Accessibility     |
| T-6.10  | A11y_KeyboardNavigation_AllPages                     | Frontend       | Accessibility     |
| T-6.11  | A11y_ContrastRatios_MeetWCAG                         | Frontend       | Accessibility     |
| T-6.12  | A11y_ScreenReaderCompatibility                       | Frontend       | Accessibility     |
| T-6.13  | A11y_SkipToContent_Present                           | Frontend       | Accessibility     |
| T-6.14  | Perf_TailwindCSS_PurgedSize                          | Infrastructure | Performance       |
| T-6.15  | Perf_TTI_Under3Seconds                               | Infrastructure | Performance       |
| T-6.16  | Perf_ResponseCompression_Active                      | Infrastructure | Performance       |
| T-6.17  | Perf_StaticAssets_Cached                             | Infrastructure | Performance       |
| T-6.18  | Security_Headers_Present                             | Infrastructure | Security          |
| T-6.19  | Security_CSP_BlocksInlineScript                      | Infrastructure | Security          |
| T-6.20  | Deploy_HealthCheck_Returns200                        | Infrastructure | Deployment        |
| T-6.21  | Deploy_ProductionBuild_Succeeds                      | Infrastructure | Deployment        |
| T-6.22  | Deploy_EnvironmentVariables_Configured               | Infrastructure | Deployment        |
| T-6.23  | Deploy_HTTPS_Enforced                                | Infrastructure | Deployment        |
| T-6.24  | Deploy_SentryCaptures_Errors                         | Infrastructure | Deployment        |
| T-6.25  | PasswordReset_ValidEmail_SendsEmail                  | Application    | Password Reset    |
| T-6.26  | PasswordReset_UnknownEmail_NoException               | Application    | Password Reset    |
| T-6.27  | PasswordReset_EmptyEmail_ValidationError             | Application    | Password Reset    |

**Total: 27 tests (8 Frontend + 16 Infrastructure + 3 Application)**

**Tests by Area:**

| Area           | Test Count | Test IDs                     |
|----------------|------------|------------------------------|
| UI Polish      | 1          | T-6.01                       |
| Responsive     | 3          | T-6.02–T-6.04               |
| UX States      | 4          | T-6.05–T-6.08               |
| Accessibility  | 5          | T-6.09–T-6.13               |
| Performance    | 4          | T-6.14–T-6.17               |
| Security       | 2          | T-6.18–T-6.19               |
| Deployment     | 5          | T-6.20–T-6.24               |
| Password Reset | 3          | T-6.25–T-6.27               |

---

## Deliverables

| #      | Deliverable                                                          | Layer          | Acceptance                                                            |
|--------|----------------------------------------------------------------------|----------------|-----------------------------------------------------------------------|
| D-6.01 | Tailwind CSS build pipeline (config + compiled CSS)                  | Frontend       | site.css < 50KB; CDN removed from layout                             |
| D-6.02 | Alpine.js + Chart.js pinned versions with SRI                        | Frontend       | Integrity hashes verified; no CDN version drift                       |
| D-6.03 | Loading states on all async pages                                    | Frontend       | Tests T-6.05 passes                                                   |
| D-6.04 | Error states on all pages                                            | Frontend       | Test T-6.06 passes; no stack traces in production                     |
| D-6.05 | Empty states on all data pages                                       | Frontend       | Test T-6.07 passes; all pages have action links                       |
| D-6.06 | Toast notification component                                         | Frontend       | Test T-6.08 passes; success/error/warning/info styles                 |
| D-6.07 | Responsive design audit pass                                         | Frontend       | Tests T-6.02–T-6.04 pass; all viewports verified                     |
| D-6.08 | Accessibility audit and remediation (WCAG 2.1 AA)                    | Frontend       | Tests T-6.09–T-6.13 pass                                             |
| D-6.09 | Password reset flow (`/Auth/ForgotPassword`)                         | Frontend       | Tests T-6.25–T-6.27 pass; Supabase sends reset email                 |
| D-6.10 | 404 Not Found page                                                   | Frontend       | Styled, consistent with layout                                        |
| D-6.11 | Print stylesheet                                                     | Frontend       | Transaction list and budget comparison print cleanly                  |
| D-6.12 | Favicon + meta tags (OG, description)                                | Frontend       | Present on all pages; social sharing preview works                    |
| D-6.13 | Page-specific titles                                                 | Frontend       | Each page has unique `<title>` (e.g., "Dashboard - SauronSheet")     |
| D-6.14 | Chart.js lazy loading                                                | Frontend       | Chart.js loaded only on Dashboard and Comparison pages                |
| D-6.15 | Security headers middleware                                          | Infrastructure | Test T-6.18 passes; all headers present                               |
| D-6.16 | CSP (Content Security Policy) configured                             | Infrastructure | Test T-6.19 passes                                                    |
| D-6.17 | Response compression (Brotli + Gzip)                                 | Infrastructure | Test T-6.16 passes                                                    |
| D-6.18 | Static asset caching (1 year + version hash)                         | Infrastructure | Test T-6.17 passes                                                    |
| D-6.19 | Sentry error monitoring (.NET + JS)                                  | Infrastructure | Test T-6.24 passes; errors visible in Sentry dashboard                |
| D-6.20 | Health check endpoint (`/health`)                                    | Infrastructure | Test T-6.20 passes; returns system status JSON                        |
| D-6.21 | Dockerfile for containerized deployment                              | Infrastructure | Test T-6.21 passes; image builds and runs                             |
| D-6.22 | Vercel deployment configuration                                      | Infrastructure | Tests T-6.22–T-6.23 pass; auto-deploy on push to main                |
| D-6.23 | CORS configuration for Supabase ↔ Vercel                             | Infrastructure | No CORS errors in production; Supabase dashboard configured           |
| D-6.24 | Environment-specific config (Dev vs Prod)                            | Infrastructure | appsettings.Development.json + appsettings.Production.json            |
| D-6.25 | `RequestPasswordResetCommand` + handler                              | Application    | Tests T-6.25–T-6.27 pass                                             |
| D-6.26 | Updated `IAuthService` (password reset method)                       | Domain         | Interface addition; implemented in SupabaseAuthService                |
| D-6.27 | Updated `SupabaseAuthService` (password reset)                       | Infrastructure | Calls Supabase Auth password reset API                                |
| D-6.28 | All Phase 6 tests (27 tests)                                        | Tests          | All green                                                              |

---

## Success Criteria

| #      | Criterion                                                                          | Metric                                                                   |
|--------|------------------------------------------------------------------------------------|--------------------------------------------------------------------------|
| SC-6.1 | Tailwind CSS compiled and purged (CDN removed)                                     | site.css < 50KB; no Tailwind CDN `<script>` in layout                    |
| SC-6.2 | All pages responsive on mobile (320px), tablet (768px), desktop (1024px+)          | Visual verification + Tests T-6.02–T-6.04                                |
| SC-6.3 | Loading, error, and empty states present on all pages                              | Tests T-6.05–T-6.07 pass                                                |
| SC-6.4 | WCAG 2.1 AA accessibility baseline met                                             | Tests T-6.09–T-6.13 pass; Lighthouse accessibility score ≥ 90           |
| SC-6.5 | Password reset flow works end-to-end                                               | Forgot password → email → reset → login with new password                |
| SC-6.6 | TTI under 3 seconds on mobile 3G                                                  | Test T-6.15 passes; Lighthouse performance score ≥ 80                    |
| SC-6.7 | Response compression active (Brotli/Gzip)                                          | Test T-6.16 passes                                                       |
| SC-6.8 | Security headers present and strict (no unsafe-inline in CSP)                      | Test T-6.18 passes; CSP blocks inline scripts/styles  |
| SC-6.9 | Sentry error monitoring active in production                                       | Test T-6.24 passes; test error visible in Sentry dashboard               |
| SC-6.10| Health check endpoint returns 200 OK                                               | Test T-6.20 passes                                                       |
| SC-6.11| Production deployment successful on Vercel (or alternative host)                   | Application accessible via public URL                                    |
| SC-6.12| HTTPS enforced in production                                                       | Test T-6.23 passes; HTTP redirects to HTTPS                              |
| SC-6.13| Auto-deploy on push to main branch                                                 | Push commit → deployment triggered → new version live                    |
| SC-6.14| All Phase 6 tests pass (27 tests)                                                  | `dotnet test` all green                                                  |
| SC-6.15| All prior phase tests still pass (no regressions)                                  | `dotnet test` → Phase 0–6 all green                                     |
| SC-6.16| No JavaScript console errors on any page in production                             | Browser DevTools verification                                            |
| SC-6.17| Static assets cached with version hashing                                          | Test T-6.17 passes                                                       |
| SC-6.18| 🚀 **PRODUCTION RELEASE**: Application live and fully operational                  | Public URL accessible; all features working; monitoring active           |

---

## Assumptions

1. **Phases 0–5 are fully implemented and tested.** All features are complete and stable. Phase 6 only polishes and deploys.
2. **Tailwind CLI standalone binary** is available for the target platform (macOS, Linux, Windows). No Node.js installation required.
3. **Vercel supports Docker-based .NET deployments** on the free tier. If not, alternative hosts (Railway.app, Render.com, Azure App Service free tier) are acceptable alternatives with the same Dockerfile.
4. **Sentry free tier** is sufficient for MVP error monitoring (5,000 events/month, 1 user).
5. **Supabase Auth has built-in password reset** functionality that sends emails without custom SMTP configuration.
6. **SRI (Subresource Integrity) hashes** are generated for Alpine.js and Chart.js CDN resources. Hashes obtained from the CDN provider or generated via `shasum`.
7. **No new domain entities or application commands/queries are created** except the minimal `RequestPasswordResetCommand` (which is an auth concern, not a new feature).
8. **Dark mode is optional** (time-permitting). The Tailwind config includes `darkMode: 'class'` but full dark mode implementation is not a requirement for launch.
9. **Print stylesheet is basic** — ensures readability when printing transaction lists and budget comparisons. Pixel-perfect print layout is not required.
10. **Lighthouse scores are targets, not hard gates.** Accessibility ≥ 90, Performance ≥ 80 are goals. Specific issues discovered during audit are prioritized by impact.

---

## Risks & Mitigations

| ID    | Risk                                                                     | Impact | Probability | Mitigation                                                                                       |
|-------|--------------------------------------------------------------------------|--------|-------------|--------------------------------------------------------------------------------------------------|
| R-6.1 | Vercel does not support .NET Docker containers on free tier              | High   | Medium      | **Pre-implementation (Week 22 start):** Contact Vercel support; confirm Docker .NET 10 support. If unsupported, pivot to Railway.app/Render.com immediately (same Dockerfile works). |
| R-6.2 | Tailwind CSS purge removes classes used by Alpine.js dynamic bindings   | Medium | Medium      | Safelist dynamic classes in tailwind.config.js; test all interactive components after purge       |
| R-6.3 | SRI hash mismatch on CDN resource update                                | Low    | Low         | Pin exact versions (e.g., `alpinejs@3.14.0`); SRI hash matches specific version                 |
| R-6.4 | Refactoring inline styles creates schedule pressure                        | Low    | Low         | Refactor all inline styles to Tailwind classes during UI polish pass (Step 3); verify no 'unsafe-inline' needed |
| R-6.5 | Sentry free tier event limit exceeded                                   | Low    | Low         | Set `TracesSampleRate` to 0.1 in production; upgrade plan if needed                              |
| R-6.6 | Accessibility audit reveals extensive remediation needed                 | Medium | Medium      | Prioritize by impact (keyboard nav > contrast > ARIA); fix critical issues, document remaining   |
| R-6.7 | Password reset email delivery issues (Supabase SMTP)                    | Medium | Low         | Test with real email; Supabase uses built-in email service; custom SMTP configurable if needed    |
| R-6.8 | Docker image size too large for free tier hosting                        | Medium | Medium      | Multi-stage build (already in Dockerfile); use `aspnet` runtime image (not SDK); target < 200MB  |
| R-6.9 | Performance benchmarks not met (TTI > 3s)                               | Medium | Medium      | Identify bottleneck (CSS size? API latency? JS bundle?); optimize specific area                  |

---

## Implementation Notes

### Recommended Implementation Order
**Pre-implementation Task (Week 22, Day 1) — CRITICAL PATH ITEM:**

**VERIFY DEPLOYMENT PLATFORM** ↑ **DO THIS FIRST — De-Risk Schedule**
└── Contact Vercel support: confirm Docker .NET 10 support on free tier
└── Provide: .NET 10, multi-stage Dockerfile, ~200MB final image size
└── **If supported:** Continue with Vercel config (FR-6.09 as-is)
└── **If NOT supported:** Pivot to Railway.app or Render.com immediately (same Dockerfile works)
└── Both platforms support Docker free tier; only deployment config differs
└── **Outcome:** Lock deployment platform decision BEFORE Step 1 begins
└── This removes Risk R-6.1; prevents wasted effort on unsupported platform

Step 1: Tailwind CSS build pipeline
└── Install Tailwind CLI (standalone binary)
└── Create tailwind.config.js + tailwind-input.css
└── Compile: `./tailwindcss -i ./tailwind-input.css -o ./wwwroot/css/site.css --minify`
└── Remove Tailwind CDN `<script>` from _Layout.cshtml
└── Replace with `<link rel="stylesheet" href="~/css/site.css" asp-append-version="true" />`
└── Verify all pages render correctly with compiled CSS
└── Run T-6.14 (purged size < 50KB)

Step 2: Script optimization + SRI
└── Pin Alpine.js version with integrity hash in _Layout.cshtml
└── Pin Chart.js version with integrity hash
└── Add conditional Chart.js loading (ViewData["LoadChartJs"])
└── Add `defer` attribute to all external scripts
└── Verify no render-blocking scripts

Step 3: UI polish pass (all pages)
└── Consistent button styles (btn-primary, btn-secondary, btn-danger)
└── Consistent form inputs (input-field, input-error)
└── Consistent card components
└── Consistent spacing and typography
└── Run T-6.01 (consistent styling)

Step 4: Responsive design audit
└── Test all pages at 320px (mobile)
└── Test all pages at 768px (tablet)
└── Test all pages at 1024px+ (desktop)
└── Fix layout issues (horizontal scroll, overflow, collapsed elements)
└── Run T-6.02–T-6.04

Step 5: Loading, error, and empty states
└── Add loading spinners/skeletons to Dashboard, Transaction list, Budget list
└── Add error state components (user-friendly messages + retry)
└── Add empty state messages with action links (per page — see Scenario 6.2 table)
└── Add toast notification component (_Toast.cshtml)
└── Run T-6.05–T-6.08

Step 6: Accessibility remediation
└── Audit all forms for <label> associations
└── Add ARIA labels to icon-only buttons
└── Add skip-to-content link (_SkipToContent.cshtml)
└── Verify keyboard navigation on all pages
└── Verify contrast ratios (Lighthouse or axe-core)
└── Add text alternatives for charts
└── Add `lang` attribute to <html>
└── Add unique page titles
└── Run T-6.09–T-6.13

Step 7: Password reset flow
└── Add "Forgot password?" link to Login page
└── Create ForgotPassword.cshtml + PageModel
└── Add `RequestPasswordResetCommand` + handler in Application
└── Add `RequestPasswordResetAsync` method to IAuthService (Domain)
└── Implement in SupabaseAuthService (Infrastructure)
└── Run T-6.25–T-6.27

Step 8: Security hardening
└── Create SecurityHeadersMiddleware
└── Register middleware in Program.cs (before static files)
└── Configure CSP policy (allow self, Supabase, CDNs)
└── Remove X-Powered-By header
└── Verify JWT cookie flags (HttpOnly, Secure, SameSite=Strict)
└── Run T-6.18–T-6.19

Step 9: Performance optimization
└── Add response compression (Brotli + Gzip)
└── Configure static file caching (1 year, immutable)
└── Configure no-cache for dynamic pages
└── Verify no N+1 query patterns in handlers
└── Database indexes audit (verify Phase 3 indexes cover all patterns)
└── Run T-6.14–T-6.17

Step 10: Additional frontend polish
└── Create 404 Not Found page (NotFound.cshtml)
└── Add print stylesheet
└── Add favicon.svg + Open Graph meta tags
└── Add page-specific <title> elements
└── Dark mode setup (optional, time-permitting)

Step 11: Sentry error monitoring
└── Add Sentry.AspNetCore NuGet to Infrastructure
└── Configure in Program.cs (DSN from config, environment, sample rate)
└── Add Sentry JS SDK to _Layout.cshtml (production only)
└── Verify error capture with test exception
└── Run T-6.24

Step 12: Deployment
└── Create Dockerfile (multi-stage build)
└── Create vercel.json (or alternative host config)
└── Create .vercelignore
└── Set environment variables in hosting dashboard
└── Configure CORS in Supabase for production domain
└── Create appsettings.Production.json
└── Add health check endpoint (/health)
└── Deploy to production
└── Verify auto-deploy on push to main
└── Run T-6.20–T-6.23

Step 13: Final verification
└── Run ALL tests (Phase 0–6): `dotnet test`
└── Lighthouse audit: Performance ≥ 80, Accessibility ≥ 90
└── Browser testing: Chrome, Firefox, Edge (no console errors)
└── Verify all success criteria (SC-6.1 through SC-6.18)
└── 🚀 PRODUCTION RELEASE

### Migration Checklist: CDN → Build Pipeline

| Item | Before (Phase 0–5) | After (Phase 6) |
|---|---|---|
| Tailwind CSS | `<script src="cdn.tailwindcss.com">` | `<link href="~/css/site.css" asp-append-version="true">` |
| Tailwind config | None (CDN defaults) | `tailwind.config.js` with custom theme |
| CSS file size | ~3MB (full CDN) | < 50KB (purged + minified) |
| Alpine.js | Unpinned CDN | Pinned version + SRI hash |
| Chart.js | Loaded on every page | Lazy loaded (only on chart pages) |
| Build step required | No | Yes: `tailwindcss --minify` before publish |
|
|
 Tailwind config                   
|
 None (CDN defaults)                          
|
`tailwind.config.js`
 with custom theme                   
|
|
 CSS file size                     
|
 ~3MB (full CDN)                              
|
 < 50KB (purged + minified)                               
|
|
 Alpine.js                         
|
 Unpinned CDN                                 
|
 Pinned version + SRI hash                                
|
|
 Chart.js                          
|
 Loaded on every page                         
|
 Lazy loaded (only on chart pages)                        
|
|
 Build step required               
|
 No                                           
|
 Yes: 
`tailwindcss --minify`
 before publish               
|

### Dark Mode Implementation (Optional)

If time permits, dark mode support follows this approach:

```javascript
// tailwind.config.js already includes: darkMode: 'class'

// Toggle logic (Alpine.js component in _Layout.cshtml)
function darkModeToggle() {
    return {
        dark: localStorage.getItem('darkMode') === 'true',
        init() {
            this.applyMode();
        },
        toggle() {
            this.dark = !this.dark;
            localStorage.setItem('darkMode', this.dark);
            this.applyMode();
        },
        applyMode() {
            document.documentElement.classList.toggle('dark', this.dark);
                 }
            };
        }
        ```

        #### Key Dark Mode Classes to Add per Component

        - **Cards:** `dark:bg-gray-800 dark:text-gray-100`
        - **Inputs:** `dark:bg-gray-700 dark:border-gray-600 dark:text-white`
        - **Buttons:** `dark:bg-primary-700 dark:hover:bg-primary-800`
        - **Tables:** `dark:bg-gray-800 dark:divide-gray-700`
        - **Budget indicators:** colors remain same (green/yellow/red have sufficient contrast on dark)

        **Note:** Dark mode is a nice-to-have. If it introduces visual bugs or delays launch, defer to post-production.

        ### Supabase CORS Configuration

In Supabase Dashboard → Settings → API → CORS Allowed Origins:

```
https://sauronsheet.vercel.app
https://*.vercel.app        # Preview deployments
http://localhost:7000        # Local development
```

### Production Environment Variables Checklist

| Variable | Source | Required | Notes |
|---|---|---|---|
| Supabase__Url | Supabase Dashboard → Settings | ✅ | Project URL |
| Supabase__Key | Supabase Dashboard → Settings | ✅ | Anon key only (not service key) |
| Supabase__JwtSecret | Supabase Dashboard → Settings | ✅ | JWT validation secret |
| Sentry__Dsn | Sentry Dashboard → Project | ✅ | Error monitoring DSN |
| ASPNETCORE_ENVIRONMENT | Hosting dashboard | ✅ | Set to Production |
| ASPNETCORE_URLS | Dockerfile | ✅ | http://+:8080 (set in Dockerfile) |

### Pre-Launch Checklist

- [ ] All Phase 0–5 tests pass (no regressions)
- [ ] All Phase 6 tests pass (27 tests)
- [ ] Tailwind CDN removed; compiled CSS < 50KB
- [ ] Alpine.js + Chart.js pinned with SRI
- [ ] All pages responsive (320px, 768px, 1024px+)
- [ ] Loading, error, empty states on all pages
- [ ] Toast notifications working
- [ ] Accessibility audit passed (Lighthouse ≥ 90)
- [ ] Password reset flow works end-to-end
- [ ] 404 page styled and functional
- [ ] Print stylesheet works for transaction list + budgets
- [ ] Favicon + meta tags present
- [ ] Security headers on all responses
- [ ] CSP configured and tested
- [ ] Response compression active (Brotli/Gzip)
- [ ] Static assets cached with version hash
- [ ] Sentry capturing errors in production
- [ ] Health check endpoint returns 200 OK
- [ ] Dockerfile builds successfully
- [ ] Production deployment live on public URL
- [ ] HTTPS enforced
- [ ] CORS configured in Supabase
- [ ] Auto-deploy on push to main working
- [ ] No console errors in Chrome, Firefox, Edge
- [ ] Lighthouse Performance ≥ 80
- [ ] Database backups enabled in Supabase
- [ ] README updated with production URL

### Post-Launch Monitoring (First 48 Hours)

| Check | Frequency | Tool |
|---|---|---|
| Error rate in Sentry | Hourly | Sentry dashboard |
| Health check endpoint | Every 5 min | Uptime monitor (UptimeRobot free) |
| Response times | Hourly | Sentry performance tab |
| Supabase usage (DB size, API calls) | Daily | Supabase dashboard |
| Auth flow (register, login, reset) | Once | Manual smoke test |
| PDF import pipeline | Once | Manual smoke test |
| Dashboard analytics accuracy | Once | Manual verification |
| Budget status indicators | Once | Manual verification |

### Glossary (Phase 6 Specific)

| Term | Definition |
|---|---|
| SRI | Subresource Integrity — hash attribute ensuring CDN resources haven't been tampered with |
| CSP | Content Security Policy — HTTP header restricting resource sources to prevent XSS |
| TTI | Time to Interactive — time until page is fully interactive for user input |
| FCP | First Contentful Paint — time until first visible content is rendered |
| Purge | Tailwind CSS feature that removes unused utility classes from production build |
| Cache busting | Technique of appending version hash to asset URLs to invalidate browser cache |
| Brotli | Modern compression algorithm; better ratio than Gzip for text content |
| RLS | Row Level Security — Supabase/PostgreSQL feature enforcing per-user data access |

---

_Phase 6 Specification — Version 1.0.0 | Last Updated: 2026-02-15 | Constitution v1.1.0_

🚀 **This is the final phase. Successful completion marks PRODUCTION RELEASE.**