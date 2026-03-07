## Escenarios de Usuario

Los siguientes escenarios de usuario guían la validación y el enfoque de implementación de la Fase 6. Para el detalle completo, ver la sección correspondiente en la especificación: [Escenarios de Usuario y Testing](phase-6-spec.md#user-scenarios--testing).

| Escenario | Descripción breve |
|-----------|-------------------|
| 6.1 | Experiencia UI pulida, consistente y responsiva |
| 6.2 | Estados claros de carga, error y vacío |
| 6.3 | Cumplimiento de accesibilidad (WCAG 2.1 AA) |
| 6.4 | Flujo de recuperación de contraseña vía email |
| 6.5 | Optimización de rendimiento (TTI, FCP, CSS) |
| 6.6 | Despliegue producción en Vercel, CI/CD |
| 6.7 | Endurecimiento de seguridad y privacidad |

Cada uno de estos escenarios tiene criterios de aceptación y pruebas asociadas en la especificación. El plan de implementación asegura que la secuencia y paralelización de tareas cubran todos los flujos críticos de usuario, validando los entregables contra estos escenarios.

# Plan de Implementación: Fase 6 – Pulido UI, Rendimiento y Despliegue Producción

**Rama**: `phase-6-ui-polish` | **Fecha**: 2026-03-06 | **Especificación**: [specs/phase-6/phase-6-spec.md](specs/phase-6/phase-6-spec.md)
**Entrada**: Especificación funcional de `/specs/phase-6/phase-6-spec.md`

## Resumen Ejecutivo

La Fase 6 entrega el pulido final de la interfaz, optimización de rendimiento, accesibilidad, y el despliegue a producción de SauronSheet. El foco está en la consistencia visual, cumplimiento WCAG 2.1 AA, pipeline de Tailwind CSS, estados de error/carga, monitorización Sentry, endurecimiento de seguridad y despliegue en Vercel. No se introducen nuevas funcionalidades ni lógica de dominio; solo están en alcance las capas Frontend e Infraestructura. Todas las fases previas deben estar completas y estables antes de iniciar.

## Contexto Técnico

**Lenguaje/Versión**: C# 10, .NET 10, JavaScript (ES2020+), Tailwind CSS 3.x  
**Dependencias Principales**: Tailwind CSS (CLI standalone), Alpine.js (CDN, versión fijada), Chart.js (CDN, versión fijada), Sentry.AspNetCore, Microsoft.AspNetCore.ResponseCompression  
**Almacenamiento**: Supabase PostgreSQL (sin cambios de esquema)  
**Testing**: xUnit, Moq, Lighthouse, axe-core, pruebas manuales navegador/tecnología asistiva  
**Plataforma Objetivo**: Vercel (contenedor Docker, .NET 10), alternativo: Railway/Render  
**Tipo de Proyecto**: Web (Razor Pages, Clean Architecture)  
**Objetivos de Rendimiento**: TTI < 3s (Lighthouse Slow 4G), FCP < 1.5s escritorio, site.css < 50KB  
**Restricciones**: Sin nueva lógica de dominio; CSP estricta (sin 'unsafe-inline'); Sentry no captura PII financiera; toda la UI vía Tailwind config; accesibilidad ≥ 90 (Lighthouse)  
**Escala/Alcance**: Un usuario por tenant, 1–10k usuarios, 10+ pantallas, 27 tests nuevos

## Chequeo Constitucional

*PUERTA: Debe pasar antes de investigación. Revisión tras diseño.*

- Clean Architecture: Sin referencias ascendentes; solo Frontend/Infraestructura en alcance (fase Polish)
- CQRS/MediatR: Sin nuevos comandos/queries salvo RequestPasswordResetCommand (solo auth)
- DDD: Sin nuevas entidades/VOs; solo interfaz para password reset
- Test-First: 27 tests nuevos especificados; todos deben pasar antes de release
- Spec-Driven: Un solo archivo de especificación; todos los requisitos, tests y entregables en phase-6-spec.md
- Ningún entregable cruza límites de alcance

**Resultado:** Todas las puertas pasan. Sin violaciones constitucionales.

### Clarifications Session 2026-03-06 (Exhaustive Review II)
- **Q6:** Railway/Render Dockerfile Portability → **A (Single Dockerfile + Environment Variables)**
  - Decisión: UN Dockerfile con variables de entorno (`$ASPNETCORE_URLS=http://+:5000`)
  - Compatible con Vercel, Railway, Render
  - Validación requerida Week 22 para port exposure y manejo de env vars

- **Q7:** Health Check Endpoint — Sentry Tracing & Free Tier Quota → **A (EXCLUDE from Sentry)**
  - Decisión: `/health` excluded from Sentry tracing via `TracesToIgnore` pattern
  - Rationale: Health checks are operational noise; protects free tier quota
  - Implementation: Sentry configuration includes `TracesToIgnore.Add("/health")`

- **Q8:** Security CSP Test Validation — Automated vs Manual → **A (Automated CSP Reporting)**
  - Decisión: T-6.19 validated via automated CSP violation logging (Sentry/server logs)
  - Rationale: CSP header correct (T-6.18) + injection triggers violation logging = test passes
  - Manual DevTools testing is optional (not required for release gate)
  - Keeps testing automated; reduces manual burden

- **Q9:** Icon Consistency Verification — Manual vs Automated → **B (Manual Visual Inspection)**
  - Decisión: Icon library uniformity verified via visual inspection during UI polish (Week 23 Day 5)
  - Rationale: Icon consistency is style/UX concern best verified by eye, not automated test
  - T-6.01 focuses on CSS consistency; icon library uniformity is separate concern
  - Scenario 6.1 AC remains informational guidance (not test-gated)

## Estructura del Proyecto

### Documentación (esta fase)

```text
specs/phase-6/
├── phase-6-plan.md         # Este archivo (salida de /speckit.plan)
├── phase-6-spec.md         # Especificación funcional
└── phase-6-tasks.md        # Salida de /speckit.tasks
```

### Código Fuente (raíz del repositorio)

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

**Decisión de estructura**: Aplicación web, Clean Architecture, frontend Razor Pages, infraestructura para despliegue/monitorización.

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

---

## Implementation Sequencing & Task Roadmap

### Three-Phase Execution Strategy (Phases + Parallelization Approved)

**Phase 1: Setup (Blocking, Sequential)**
- Steps 1-2: Tailwind build pipeline setup + Script optimization with SRI
- Duration: ~2-3 days
- Blockers: None (starting point)
- Output: Compiled CSS < 50KB; pinned Alpine.js + Chart.js with integrity hashes

**Phase 2: Polish (Parallelizable by Feature Concern)**
- Steps 3-11: UI polish, responsive design, accessibility, loading/error/empty states, password reset, health check, print stylesheet
- Duration: ~10-14 days (parallelized)
- Parallelization opportunities:
  - **UI Polish Track** (steps 3, 10, 13): Consistent styling, favicon/meta tags, print stylesheet
  - **Accessibility Track** (step 6): WCAG 2.1 AA audit + remediation (parallel to UI)
  - **State Management Track** (steps 5, 7): Loading, error, empty states + toast component (parallel to UI & A11y)
  - **Feature Track** (step 7): Password reset flow (can start while UI polish runs)
- Output: All 27 deliverables from FR/UI scope completed and tested

**Phase 3: Deployment (Blocking, Sequential)**
- Steps 12-13: Security hardening + Performance optimization + Vercel deployment
- Duration: ~3-5 days
- Blockers: Phase 2 must be complete (new dependencies: response compression, Sentry config)
- Output: Production deployment live; all Phase 6 tests passing

### Task Dependencies (Critical Path)

```
Setup → [Polish (4 parallel tracks) + Pre-impl verification] → Deployment
 ↓                                                                ↓
Tailwind                                                   Security Headers
Alpine.js/Chart.js                                        & CSP Config
                ↓
        [Parallelizable]
        UI Polish
        Accessibility Audit
        State Management
        Feature (Password Reset)
                ↓
        [Blocked until Phase 2 complete]
        Sentry Integration
        Compression Config
        Dockerfile & Vercel Config
        CORS Setup
```

### Pre-Implementation Critical Path Item (Week 22, Day 1 — MUST DO FIRST)

**VERIFY DEPLOYMENT PLATFORM** ↓
- Contact Vercel support: Confirm Docker .NET 10 support on free tier
- If YES → Proceed with Vercel config (FR-6.09 as-is)
- If NO → Immediately pivot to Railway.app or Render.com (same Dockerfile works)
- Decision date: Week 22 start (Day 1 morning)
- This action **de-risks the entire Phase 3** and prevents wasted effort on unsupported platform

### Parallelization Constraints

- Phase 1 (Setup) is fully sequential; no parallelization possible
- Phase 2 (Polish) can run 4 parallel workstreams after Tailwind setup completes
- Phase 3 (Deployment) is sequential but depends only on Phase 2 outputs (not blocking)
- Cross-workstream dependency: All Phase 2 tracks must be complete before Phase 3 security/deployment changes
- Testing: Tests can begin in Phase 2 per component (T-6.01 UI once styling done, T-6.09 A11y once components ready)

### Schedule (Optimistic with Parallelization)

| Week | Day | Phase | Activity | Parallelization |
|------|-----|-------|----------|-----------------|
| 22   | 1   | Pre   | **Verify Vercel platform support** (risk mitigation) | Blocking |
| 22   | 2-3 | 1     | Tailwind setup + Alpine/Chart SRI | Sequential |
| 22   | 4-5 | 2     | UI Polish + A11y Audit + State Mgmt + Password Reset | **4 parallel tracks** |
| 23   | 1-4 | 2     | Continue Phase 2 tracks + testing | **Parallel** |
| 23   | 5   | 3     | Security + Compression + Sentry setup | Sequential (after Phase 2) |
| 24   | 1-3 | 3     | Vercel deployment + CORS + final testing | Sequential |
| 24   | 4   | Done  | Production live + monitoring active | ✅ Release |

### Workstream Leaders (If team-based)

- **Tailwind/Performance**: Single engineer (Steps 1-2, 9)
- **UI Polish**: 1-2 engineers (Steps 3-4, 10-11, 13-14)
- **Accessibility**: 1 engineer + designer (Step 6, test suite)
- **State Management**: 1 engineer (Steps 5-8)
- **Deployment**: DevOps/Infrastructure engineer (Steps 12, 15-16, Dockerfile/Vercel)

### Detailed Step Reference

**This plan's responsibility:** High-level sequencing, parallelization strategy, risk mitigation, and architectural decisions.

**Spec's responsibility:** Detailed implementation steps (13 ordered steps with full code, FR requirements, test specs).

**Link to detailed steps:** See [phase-6-spec.md § Recommended Implementation Order](phase-6-spec.md#recommended-implementation-order) for:
- Exact command syntax (Tailwind CLI, build commands)
- Code snippets (FR-6.01 through FR-6.12)
- Line-by-line requirements for each step
- Dependency details (e.g., Step 1 output feeds Step 2)
- Migration checklists and environment variable setup

Implement each step group in order per the spec's step sequence, but organize team effort using the **three-phase + four-track parallelization roadmap** above.

---

## Testing Strategy: Continuous TDD During Polish Phase

### Testing Roadmap (27 Tests, Parallelized by Workstream)

**Test-First Approach Approved:** Write test specs immediately after each feature step completes; validate before proceeding to next step.

**Workstream Testing Timeline:**

| Workstream | Steps | Tests | Timing | Validation Gate |
|------------|-------|-------|--------|-----------------|
| **UI Polish** | 3, 10, 13, 14 | T-6.01, T-6.02, T-6.03, T-6.04 | After each UI step | CSS audit + visual inspection + responsive checks |
| **Accessibility** | 6 | T-6.09, T-6.10, T-6.11, T-6.12, T-6.13 | After A11y audit complete | Lighthouse ≥90; axe-core clean; screen reader test |
| **State Management** | 5, 7, 8 | T-6.05, T-6.06, T-6.07, T-6.08 | After each component (loading → error → empty → toast) | Manual browser + automation |
| **Feature (Password Reset)** | 7 | T-6.25, T-6.26, T-6.27 | After PageModel/Handler implementation | End-to-end email send + reset flow |
| **Performance** | 9, 14, 15, 16, 17 | T-6.14, T-6.15, T-6.16, T-6.17 | After compression + caching + Tailwind setup | Lighthouse Perf ≥80; TTI measurement; file size audit |
| **Security** | 11, 12 | T-6.18, T-6.19 | After middleware + CSP implementation | Security headers audit; CSP validator |
| **Deployment** | 13, 15, 16, 17 | T-6.20, T-6.21, T-6.22, T-6.23, T-6.24 | Phase 3 sequential (Dockerfile built → Vercel deploy → app live) | Build success; endpoint health check; Sentry integration test |

**Critical Path Tests (Must Pass Before Release):**
- T-6.20–T-6.24 (Deployment): Full stack deployment smoke tests
- T-6.10 (A11y): Keyboard navigation on all pages
- T-6.15 (Perf): TTI < 3s on Slow 4G

**Regression Testing:**
- Before each Phase 2 workstream handoff, run Phase 0–5 full test suite (`dotnet test`) → confirm zero regressions
- Before Phase 3 (deployment), run ALL Phase 0–6 tests → green before proceeding

### Test Execution Schedule Per Phase

**Phase 1 (Setup, Week 22 Days 2-3):**
- T-6.14: Tailwind CSS size audit (< 50KB target)
- T-6.17: Verify static asset hashing works with asp-append-version
- Gate: Proceed to Phase 2 only if both pass

**Phase 2 (Polish, Week 22-23 Days 4–4):**
- **Daily gates per workstream:** Each track writes and runs tests matching its completed steps
- UI Polish track: T-6.01, T-6.02–T-6.04 (responsive) — validate each viewport size
- Accessibility track: T-6.09–T-6.13 — validate WCAG compliance incrementally
- State Management track: T-6.05–T-6.08 — validate each state type
- Feature track: T-6.25–T-6.27 — end-to-end password reset
- Performance track: T-6.15, T-6.16 — TTI and compression
- End-of-Phase-2 gate: ALL Phase 2 tests green + Phase 0–5 regression suite green

**Phase 3 (Deployment, Week 23-24 Days 5–3):**
- T-6.18–T-6.19: Security headers (before deployment)
- T-6.20–T-6.24: Deployment and monitoring tests (after live)
- Post-launch monitoring (48 hours): Manual verification of health check, Sentry event capture, database connectivity

### Test Execution Schedule Per Phase

**Phase 1 (Setup, Week 22 Days 2-3):**
- T-6.14: Tailwind CSS size audit (< 50KB target)
- T-6.17: Verify static asset hashing works with asp-append-version
- Gate: Proceed to Phase 2 only if both pass

**Phase 2 (Polish, Week 22-23 Days 4–4):**
- **Daily gates per workstream:** Each track writes and runs tests matching its completed steps
- UI Polish track: T-6.01, T-6.02–T-6.04 (responsive) — validate each viewport size
- Accessibility track: T-6.09–T-6.13 — validate WCAG compliance incrementally
- State Management track: T-6.05–T-6.08 — validate each state type
- Feature track: T-6.25–T-6.27 — end-to-end password reset
- Performance track: T-6.15, T-6.16 — TTI and compression
- End-of-Phase-2 gate: ALL Phase 2 tests green + Phase 0–5 regression suite green

**Phase 3 (Deployment, Week 23-24 Days 5–3):**
- T-6.18–T-6.19: Security headers (before deployment)
- T-6.20–T-6.24: Deployment and monitoring tests (after live)
- Post-launch monitoring (48 hours): Manual verification of health check, Sentry event capture, database connectivity

### Test Infrastructure

- **Unit/Integration**: xUnit + Moq (in Domain.Tests, Application.Tests)
- **Frontend/Browser**: Lighthouse CLI (automated via CI), manual browser testing (Chrome, Firefox, Edge)
- **Accessibility**: axe-core CLI or browser extension, screen reader (NVDA/JAWS if available)
- **Performance**: Lighthouse throttle profile (Slow 4G), WebPageTest (optional)
- **Security**: OWASP CSP validator, manual header audit
- **Deployment**: Docker build test, Vercel preview URL health check, public URL verification

---

## Pre-Implementation Risk Verification Protocol (Critical Path De-Risking)

### Risk R-6.1 Mitigation: Platform Support Lock-In (Week 22, Day 1 — MUST COMPLETE BEFORE STEP 1)

**Objective:** Eliminate platform selection uncertainty before Phase 1 begins. Vercel .NET 10 support is a go/no-go gate for weeks 23-24.

**Structured Verification Process:**

**Week 22, Day 1 — Morning (Before any development work starts)**

1. **Send formal inquiry to Vercel support**
   - Subject: Free-tier support for .NET 10 Docker containerized full-stack app
   - Provide:
     - Multi-stage Dockerfile (included in spec FR-6.09)
     - Expected runtime image size: ~150-200MB (asp:6.0 runtime base)
     - Workload characteristics: Razor Pages, SQLite/Supabase API calls, background health checks
     - Expected traffic: MVP (1-10k users)
   - Acceptance criteria: "Free tier supports persistent Docker containers with these specs; no deployment restrictions"

2. **Set decision deadline: Week 22, Day 1 — EOD (5 PM)**
   - Support email or Vercel Discord community response time: 2-4 hours typical
   - If no response by EOD, assume Vercel support is uncertain → **pivot to Railway.app immediately**

3. **Decision Tree:**

   ```
   Week 22 Day 1 EOD Decision
   ├─ YES (Vercel confirmed free tier support)
   │  ├─ Action: Proceed with Vercel config (FR-6.09 unchanged)
   │  ├─ Record: Vercel support ticket #, date, confirmation text
   │  ├─ Lock: Platform = Vercel; no mid-phase changes
   │  └─ Proceed to Phase 1 Step 1
   │
   ├─ NO (Vercel does not support free tier Docker .NET 10)
   │  ├─ Action: **PIVOT TO RAILWAY.APP IMMEDIATELY**
   │  ├─ Changes: Update Dockerfile (remove Vercel-specific config); use Railway CLI deploy
   │  ├─ Record: Vercel rejection reason + Railway decision date
   │  ├─ Lock: Platform = Railway; notify stakeholders
   │  └─ Proceed to Phase 1 Step 1 with Railway config
   │
   └─ NO RESPONSE / UNCERTAIN (Support unclear)
      ├─ Action: Assume unsupported → **PIVOT TO RAILWAY.APP**
      ├─ Reason: 72-hour window before Phase 3 starts; cannot risk running out of time
      ├─ Changes: Same as NO path (Dockerfile + Railway CLI)
      └─ Record: "No timely response from Vercel; selected Railway for schedule certainty"
   ```

4. **Post-Decision Lock-In**
   - Document decision + rationale in `/specs/phase-6/` as a comment in plan.md or inline note
   - Notify implementation team: "Platform is locked to [Vercel/Railway]; do not revisit"
   - Create GitHub issue (if using issues): "Phase 6 deployment platform: [Vercel/Railway] — Locked"

5. **Fallback Deployment Paths (Both Use Same Dockerfile)**

   **If Vercel (YES):**
   ```bash
   # Vercel deployment
   vercel login
   vercel deploy --prod --build-env ASPNETCORE_ENVIRONMENT=Production
   ```

   **If Railway (NO/Uncertain):**
   ```bash
   # Railway deployment
   railway login
   railway link              # Link to Railway project
   railway up --detach        # Deploy
   # CORS + env vars configured in Railway dashboard (same as Vercel)
   ```

   Note: Both platforms use the same Dockerfile; only deployment commands differ.

6. **Contingency Budget**
   - If platform pivot happens on Day 1 morning, **Phase 1 schedule shifts by ~4 hours** (Dockerfile validation + Railway CLI setup)
   - No impact on Phase 2 or 3 schedule (both start on time)
   - Risk: ELIMINATED by front-loading decision

### Why This Matters

- **Schedule Risk**: If Vercel says NO on Week 23 (mid-Phase), we lose 2-3 days pivoting → launch delay
- **Technical Risk**: Docker image might exceed Vercel limits; pivot timing proves it works before development
- **Team Risk**: Clear go/no-go decision prevents wasted effort on unsupported platform

### Decision Record Template (Fill in Week 22 Day 1)

```markdown
## Phase 6 Platform Verification — Decision Record

**Date**: 2026-03-[day]
**Verified by**: [Name/Engineer]
**Support Channel**: [Vercel email / Discord / other]
**Support Ticket/Reference**: [ID if applicable]

### Question
Can Vercel free tier support .NET 10 persistent Docker containers (~150-200MB runtime)?

### Response
[Insert Vercel support response verbatim]

### Decision
- ☑ YES → Use Vercel (FR-6.09 unchanged)
- ☐ NO → Pivot to Railway (update Dockerfile + config)
- ☐ NO RESPONSE (48h+ elapsed) → Pivot to Railway (schedule certainty)

### Lock-In Confirmation
Platform locked to: **[VERCEL / RAILWAY]**
No further platform changes are permitted; implementation proceeds with selected platform only.
```

---

## Dark Mode Scope: Time-Permitting Enhancement (NOT Launch Blocker)

### Decision: Option A — Dark Mode Optional

**Status**: NICE-TO-HAVE, low priority, not on critical path.

**Rationale:**
- 27 core tests (T-6.01 through T-6.27) do not include dark mode tests
- Success criteria (SC-6.1 through SC-6.18) do not require dark mode
- All 14 deliverables (D-6.01 through D-6.28) are achievable without dark mode
- Production release can proceed without dark mode

**Implementation Decision:**

| Scenario | Action |
|----------|--------|
| Phase 2 finishes Week 23 **Day 3 or earlier** (2+ days ahead of schedule) | **Implement dark mode** in remaining time; run new dark mode tests (visual inspection) |
| Phase 2 finishes Week 23 **Day 4 (on-schedule)** | **Skip dark mode**; use remaining time for UI polish refinements, performance tuning, or buffer |
| Phase 2 finishes Week 23 **Day 5 or later** (behind schedule) | **Skip dark mode**; focus on Phase 3 deployment readiness |

**If Dark Mode is Implemented (Time-Permitting):**

Follow the "Dark Mode Implementation" section in [phase-6-spec.md § Implementation Notes](phase-6-spec.md#dark-mode-implementation-optional):
```javascript
// tailwind.config.js: darkMode: 'class' already enabled
// Steps:
// 1. Add Toggle component to _Layout.cshtml (Alpine.js toggle logic)
// 2. Add dark: variants to all components (10-15 component classes)
// 3. Test with browser DevTools dark mode toggle
// 4. Manual luminance/contrast verification (maintain WCAG AA in dark mode)
// 5. Time estimate: 2-3 days
```

**If Dark Mode is NOT Implemented (Most Likely):**
- Tailwind config remains with `darkMode: 'class'` (no removal needed; just unused)
- No toggle UI shown to users
- Post-MVP backlog: "Dark Mode Support — Phase 7 (Enhancement Roadmap)"
- Users have option to use OS-level dark mode (CSS media query `prefers-color-scheme`), which Tailwind respects automatically

**Post-Launch Consideration:**
- Gather user feedback: if demand is high, implement in Phase 7
- Low effort to retrofit (toggle + dark: classes already configured)

---

## Recommendations for Next Steps

1. **Immediate (Today)**:
   - Review updated plan; confirm three-phase + testing + platform verification strategies align with team expectations
   - Assign platform verification task to engineer/architect for Week 22 Day 1

2. **This Week**:
   - Run `/speckit.tasks` to generate actionable, dependency-ordered task list with time estimates
   - Add tasks to GitHub or project management tool (GitHub Projects, Jira, etc.)
   - Schedule team kickoff for Phase 6 (reaffirm sequencing, testing gates, risk mitigation)

3. **Week 22, Day 1 Morning (CRITICAL)**:
   - Complete Vercel platform verification (decision lock-in within 24 hours)
   - Begin Phase 1 (Setup) if platform is confirmed

---
**Verified by**: [Name/Engineer]
**Support Channel**: [Vercel email / Discord / other]
**Support Ticket/Reference**: [ID if applicable]

### Question
Can Vercel free tier support .NET 10 persistent Docker containers (~150-200MB runtime)?

### Response
[Insert Vercel support response verbatim]

### Decision
- ☑ YES → Use Vercel (FR-6.09 unchanged)
- ☐ NO → Pivot to Railway (update Dockerfile + config)
- ☐ NO RESPONSE (48h+ elapsed) → Pivot to Railway (schedule certainty)

### Lock-In Confirmation
Platform locked to: **[VERCEL / RAILWAY]**
No further platform changes are permitted; implementation proceeds with selected platform only.
```

---

## Dark Mode Scope: Time-Permitting Enhancement (NOT Launch Blocker)

### Decision: Option A — Dark Mode Optional

**Status**: NICE-TO-HAVE, low priority, not on critical path.

**Rationale:**
- 27 core tests (T-6.01 through T-6.27) do not include dark mode tests
- Success criteria (SC-6.1 through SC-6.18) do not require dark mode
- All 14 deliverables (D-6.01 through D-6.28) are achievable without dark mode
- Production release can proceed without dark mode

**Implementation Decision:**

| Scenario | Action |
|----------|--------|
| Phase 2 finishes Week 23 **Day 3 or earlier** (2+ days ahead of schedule) | **Implement dark mode** in remaining time; run new dark mode tests (visual inspection) |
| Phase 2 finishes Week 23 **Day 4 (on-schedule)** | **Skip dark mode**; use remaining time for UI polish refinements, performance tuning, or buffer |
| Phase 2 finishes Week 23 **Day 5 or later** (behind schedule) | **Skip dark mode**; focus on Phase 3 deployment readiness |

**If Dark Mode is Implemented (Time-Permitting):**

Follow the "Dark Mode Implementation" section in [phase-6-spec.md § Implementation Notes](phase-6-spec.md#dark-mode-implementation-optional):
```javascript
// tailwind.config.js: darkMode: 'class' already enabled
// Steps:
// 1. Add Toggle component to _Layout.cshtml (Alpine.js toggle logic)
// 2. Add dark: variants to all components (10-15 component classes)
// 3. Test with browser DevTools dark mode toggle
// 4. Manual luminance/contrast verification (maintain WCAG AA in dark mode)
// 5. Time estimate: 2-3 days
```

**If Dark Mode is NOT Implemented (Most Likely):**
- Tailwind config remains with `darkMode: 'class'` (no removal needed; just unused)
- No toggle UI shown to users
- Post-MVP backlog: "Dark Mode Support — Phase 7 (Enhancement Roadmap)"
- Users have option to use OS-level dark mode (CSS media query `prefers-color-scheme`), which Tailwind respects automatically

**Post-Launch Consideration:**
- Gather user feedback: if demand is high, implement in Phase 7
- Low effort to retrofit (toggle + dark: classes already configured)

---

## Clarifications Session Summary

### All 5 Clarifications Resolved ✓

| Q# | Topic | Answer | Impact on Plan |
|----|-------|--------|-----------------|
| Q1 | Implementation Sequencing | **B: Phases + Parallelization** | Added three-phase roadmap + four parallel workstreams (Setup → Polish → Deployment) |
| Q2 | Task Detail Granularity | **A: Reference + Graph** | Plan links to spec's 13-step breakdown; avoids duplication; single source of truth |
| Q3 | Testing Strategy & Timing | **A: Continuous TDD** | Added test roadmap; tests written/validated immediately after each step; per-workstream testing gates |
| Q4 | Pre-Implementation Risk Verification | **A: Structured Protocol** | Added formal Week 22 Day 1 verification; Vercel platform lock-in; decision tree; fallback to Railway |
| Q5 | Dark Mode Scope | **A: Time-Permitting Only** | Dark mode is optional; not on critical path; implement if Phase 2 finishes early; defer to Phase 7 if needed |

### Integration Status

- ✅ **Implementation Sequencing** (Q1): Three-phase + four-track roadmap added to plan
- ✅ **Task Granularity** (Q2): Reference section linking spec to plan added
- ✅ **Testing Strategy** (Q3): Test roadmap with validation gates for each workstream added
- ✅ **Risk Verification** (Q4): Formal pre-implementation platform verification protocol with decision tree added
- ✅ **Dark Mode Scope** (Q5): Time-permitting decision + decision logic added

---

## Clarifications Session Complete ✓

**Date**: 2026-03-06  
**Total Questions Asked**: 5  
**Total Questions Answered**: 5  
**Quota Remaining**: 0/5  

**Sections Updated in plan.md**:
1. Implementation Sequencing & Task Roadmap (new)
2. Testing Strategy: Continuous TDD During Polish Phase (new)
3. Pre-Implementation Risk Verification Protocol (new)
4. Dark Mode Scope: Time-Permitting Enhancement (new)

**Status**: ✅ **ALL CRITICAL AMBIGUITIES RESOLVED**

The Phase 6 plan is now unambiguous, actionable, and ready for implementation tasks breakdown (next phase: `/speckit.tasks`).

---

## Recommendations for Next Steps

1. **Immediate (Today)**:
   - Review updated plan.md; confirm three-phase + testing + platform verification strategies align with team expectations
   - Assign platform verification task to engineer/architect for Week 22 Day 1

2. **This Week**:
   - Run `/speckit.tasks` to generate actionable, dependency-ordered task list with time estimates
   - Add tasks to GitHub or project management tool (GitHub Projects, Jira, etc.)
   - Schedule team kickoff for Phase 6 (reaffirm sequencing, testing gates, risk mitigation)

3. **Week 22, Day 1 Morning (CRITICAL)**:
   - Complete Vercel platform verification (decision lock-in within 24 hours)
   - Begin Phase 1 (Setup) if platform is confirmed

---
