# Tareas: Soporte Multi-Idioma (ES/EN)

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | 2,100-2,400 (~1,000 .resx data, ~1,200 code) |
| 800-line budget risk | High (mitigated by 7 chained PRs ≤400L each) |
| Chained PRs recommended | Yes |
| Suggested split | PR1 → PR2 → PR3 → PR4 → PR5 → PR6 → PR7 |

Decision needed before apply: No
Chained PRs recommended: Yes
Chain strategy: feature-branch-chain
800-line budget risk: High

### Suggested Work Units
| Unit | Goal | PR | Base branch |
|------|------|-----|-------------|
| 1 | Infraestructura + Login + E2E base | PR1 | feature/multi-language |
| 2 | Auth/Index/Error | PR2 | PR1 branch |
| 3 | Dashboard + Transactions | PR3 | PR1 branch |
| 4 | Categories + Budgets | PR4 | PR1 branch |
| 5 | Annual + servicios bilingües | PR5 | PR1 branch |
| 6 | charts.js + Flatpickr EN | PR6 | PR1 branch |
| 7 | Import errors + categorías sistema | PR7 | PR1 branch |

## Gate Review: Notas de diseño

1. **Clean Architecture**: `SharedResources.cs` + `.resx` en `Application/Resources/` (no Frontend). Vistas y servicios inyectan `IStringLocalizer<SharedResources>`. Application no depende de Frontend.
2. **InsightsService dual**: `IStringLocalizer` para frases fijas ("No discoveries"); `switch CurrentUICulture` para oraciones compuestas con variables ("Tus ingresos crecieron un 15.3%...").
3. **Cookie HttpOnly=false** (T1.2): necesario para JS del switcher. Riesgo XSS mitigado por `SameSite=Strict`.

## PR1: Infraestructura + Login piloto + E2E data-testid (~350 L)

- [x] **T1.1** Crear `src/SauronSheet.Application/Resources/SharedResources.cs` (marker) + `.es.resx` / `.en.resx` con ~50 claves mínimas para Login/_Layout/switcher. Test: xUnit verifica acceso `IStringLocalizer<SharedResources>`.
- [x] **T1.2** `Program.cs`: `AddLocalization`, `RequestLocalizationOptions` (culturas es-ES/en-US, fallback en), providers Cookie→QueryString→AcceptLanguage. Cookie `.AspNetCore.Culture` 1 año, `HttpOnly=false`, `SameSite=Strict`. `UseRequestLocalization` tras `UseResponseCompression`, antes de `UseStaticFiles`.
- [x] **T1.3** `_LanguageSwitcher.cshtml` (partial navbar, icono globo + dropdown ES/EN Alpine.js) + endpoint `POST /api/culture?c=`. `data-testid="lang-switcher"`.
- [x] **T1.4** `_Layout.cshtml`: `<html lang>` dinámico, `window.__i18n` scaffold, Flatpickr locale condicional, inject switcher.
- [x] **T1.5** `Login.cshtml`: `@inject IStringLocalizer<SharedResources> Localizer` + `data-testid` (email, password, submit, register-link, error).
- [x] **T1.6** E2E `00-culture.spec.ts`: conmutación ES↔EN, persistencia cookie, render cultura. Añadir `data-testid` a Login donde falten.
- [x] **T1.7** Unit tests: resolución `IStringLocalizer` ES/EN, middleware cultura, fallback inglés.

## PR2: Auth/Register/Index/Error (~300 L)

- [ ] **T2.1** Añadir ~40 claves Auth/Error a `.resx`. Test: xUnit resolución ES/EN.
- [ ] **T2.2** Traducir `Register.cshtml`, `Logout.cshtml` con `@inject IStringLocalizer<SharedResources>` + `data-testid`.
- [ ] **T2.3** Traducir `Index.cshtml`, `Error.cshtml` con `data-testid`.
- [ ] **T2.4** Actualizar `01-login.spec.ts`: migrar a `data-testid`. Añadir test Register.

## PR3: Dashboard + Transactions (~400 L)

- [ ] **T3.1** Añadir ~60 claves a `.resx`. Test: xUnit.
- [ ] **T3.2** Traducir `Dashboard.cshtml`, `Transactions/{Index,Add,Edit,Search,Upload}.cshtml` + `data-testid`.
- [ ] **T3.3** Traducir partails: `_NavItems`, `_Toast`, `_DateRangeFilter`, `_SkipToContent`.
- [ ] **T3.4** Actualizar E2E `02-upload-excel`, `03-edit-transaction`: `data-testid`.

## PR4: Categories + Budgets (~400 L)

- [ ] **T4.1** Añadir ~70 claves a `.resx`. Test: xUnit.
- [ ] **T4.2** Traducir `Categories/{Index,Subcategories}` + `_CategoryBadge` + `data-testid`.
- [ ] **T4.3** Traducir `Budgets/{Index,Create,Edit,History,Metrics,Comparison}` + `_BudgetStatusBadge`, `_BudgetStatusModal`.
- [ ] **T4.4** Actualizar E2E `03-budgets`, `04-budget-management`, `05-categories-lifecycle`, `budgets/visualization`: `data-testid`.

## PR5: Annual/Analysis + servicios bilingües (~400 L)

- [ ] **T5.1** Añadir ~50 claves Annual a `.resx`. Test: xUnit.
- [ ] **T5.2** Traducir `Annual.cshtml` (17 bloques REQ-EXEC-002–017 + YoY REQ-ANNUAL-030 + tablas REQ-ANNUAL-040 + empty state REQ-ANNUAL-070). Preservar `data-testid` existentes.
- [ ] **T5.3** Refactor `InsightsService.cs`: `static`→instance + `IStringLocalizer<SharedResources>`. Registrar en `DependencyInjection.cs` (Scoped).
- [ ] **T5.4** `GenerateSmartSummary()` culture-aware: `switch CurrentUICulture` para oraciones compuestas; `IStringLocalizer` para frases fijas. Sentry=EN. Test: xUnit con `CultureInfo.CurrentUICulture` set en thread.
- [ ] **T5.5** `GenerateDiscoveries()`: mismo patrón. Sentry breadcrumbs en inglés.
- [ ] **T5.6** Refactor `AnomalyDetectionService.cs`: `static`→instance + DI. `BuildDescription()` culture-aware.
- [ ] **T5.7** Actualizar PageModels que llamaban servicios `static` → constructor injection (~3 PageModels).
- [ ] **T5.8** E2E `07-annual-analysis`: `data-testid`, Smart Summary ES/EN.

## PR6: charts.js + Chart.js + Flatpickr locale EN (~250 L)

- [ ] **T6.1** Añadir ~25 claves `js.*` a `.resx` (leyendas charts). Test: xUnit.
- [ ] **T6.2** `_Layout.cshtml`: serializar `js.*` a `window.__i18n` vía `JsonSerializer`. Script `l10n/en.js` condicional.
- [ ] **T6.3** `charts.js`: labels desde `window.__i18n` + fallback + Sentry breadcrumb si clave ausente.
- [ ] **T6.4** E2E: Flatpickr meses ES/EN, chart labels cambian con cultura.

## PR7: Errores import/parser + categorías sistema (~300 L)

- [ ] **T7.1** Añadir ~40 claves import/parser/`category.system.*` a `.resx`. Test: xUnit.
- [ ] **T7.2** `ImportTransactionsCommandHandler.cs`: errores → `IStringLocalizer<SharedResources>`.
- [ ] **T7.3** `IngExcelStatementParser.cs`: errores → `IStringLocalizer<SharedResources>`. Infra→App válido en Clean Architecture.
- [ ] **T7.4** Categorías sistema: seed inglés inmutable; UI mapea `slug`→`category.system.{slug}`. Afecta `_CategoryBadge`, dropdowns, tablas.
- [ ] **T7.5** E2E: errores import ES/EN, categorías sistema ES/EN.
