# Design: Soporte Multi-Idioma (ES/EN)

## Enfoque Técnico

Estrategia híbrida confirmada por la exploración: `.resx` + `IViewLocalizer`/`IStringLocalizer<T>` para UI estática, `CultureInfo.CurrentUICulture` con autoría bilingüe manual para servicios, y diccionario `window.__i18n` serializado desde el servidor para JavaScript. La infraestructura ASP.NET Core nativa (`AddLocalization`, `RequestLocalizationOptions`) provee el pipeline; sin NuGet nuevo.

## Decisiones de Arquitectura

### Decisión: Estructura de recursos — `SharedResources` único
**Elección**: Un único `SharedResources` (clase marker) + par `.resx` por cultura (`SharedResources.es.resx`, `SharedResources.en.resx`) en `src/SauronSheet.Frontend/Resources/`.
**Alternativas**: Recursos por área (`LoginResources`, `DashboardResources`).
**Rationale**: ~500-800 strings en MVP de 2 idiomas no justifica fragmentación. Un único contenedor simplifica el scaffolding de `window.__i18n`, evita inyección de múltiples `IStringLocalizer<T>` por página, y el fallback de claves es global. Si el volumen crece >2000 strings, se refactoriza a por-área sin cambiar el patrón de consumo.

### Decisión: `IViewLocalizer` en vistas, `IStringLocalizer<SharedResources>` en servicios
**Elección**: `@inject IViewLocalizer Localizer` en `.cshtml`; `IStringLocalizer<SharedResources>` en PageModels y servicios. Ambos resuelven contra el mismo `SharedResources`.
**Rationale**: `IViewLocalizer` localiza por vista-por-archivo (no requiere `SharedResources`), pero para centralizar en un `.resx` se configura `SharedResourceType` en `AddViewLocalization()`. En servicios (`InsightsService`), `IStringLocalizer<SharedResources>` permite resolver claves de texto dinámico.

### Decisión: `window.__i18n` — scaffolding desde `IStringLocalizer<SharedResources>`
**Elección**: `_Layout.cshtml` inyecta `IStringLocalizer<SharedResources>`, serializa un subconjunto de claves JS-relevantes (prefijo `js.`) a `window.__i18n` mediante `Json.Serialize`. No existe registro de claves compartido; la convención de prefijo `js.` es el contrato.
**Alternativas**: Endpoint API que sirve JSON i18n; archivo JSON estático.
**Rationale**: Un único source-of-truth (`.resx`) eliminado el drift. El prefijo `js.` separa claves JS de claves Razor sin tooling extra. `charts.js` consume `window.__i18n.legend.income` etc.

### Decisión: Pipeline de middleware — `UseRequestLocalization` antes de static files
**Elección**: `app.UseRequestLocalization()` inmediatamente después de `UseResponseCompression()`, antes de `UseStaticFiles()` y `UseRouting()`.
**Rationale**: La cultura debe resolverse antes de cualquier endpoint o static file para que `<html lang>` y `window.__i18n` sean correctos. Cookie: `.AspNetCore.Culture` (nombre estándar del framework), scope `/`, `HttpOnly=false` (legible por JS para el switcher), `SameSite=Strict`, expira 1 año. No entra en conflicto con la cookie de auth Supabase (nombre `sb-...`, scope distinto).

### Decisión: Language switcher — partial Razor + endpoint POST
**Elección**: Partial `_LanguageSwitcher.cshtml` (navbar derecha, icono globo + dropdown ES/EN con Alpine.js). Endpoint `POST /api/culture?c=es` setea cookie y redirige a `Referer`.
**Alternativas**: QueryString `?culture=es` (efímera); GET endpoint.
**Rationale**: POST evita crawlers que cambien cultura. El `Referer` redirect mantiene al usuario en la página actual. `data-testid="lang-switcher"` para E2E.

### Decisión: Servicios culture-aware — refactor `InsightsService` de estático a instancia
**Elección**: `InsightsService` y `AnomalyDetectionService` pasan de `static` a clases instancia inyectadas en DI, recibiendo `IStringLocalizer<SharedResources>`. El texto se genera con switch `CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "es"` produciendo ambas versiones autoras manualmente. Sentry breadcrumbs en inglés hardcoded (no localizados).
**Rationale**: El patrón `static` actual impide inyección de `IStringLocalizer`. El switch explícito cumple REQ-LOC-060 (autoría manual, no auto-traducción). Las trazas Sentry quedan fuera del switch.

### Decisión: Flatpickr locale dinámico — script condicional en `_Layout`
**Elección**: `_Layout` emite `<script src="l10n/{culture}.js">` condicionalmente y llama `flatpickr.localize(flatpickr.l10ns[culture])`. Chart.js no tiene locale nativo; sus labels se alimentan desde `window.__i18n`.
**Rationale**: `l10n/es.js` ya está cargado hoy; `l10n/en.js` se añade. Chart.js no requiere locale global; el diccionario basta.

### Decisión: Categorías del sistema — seed inglés + traducción de presentación
**Elección**: El seed canónico permanece en inglés ("Salary", "Groceries"). La presentación traduce vía `SharedResources` con clave `category.system.{slug}`. El `Category.Name` del dominio NO se modifica.
**Rationale**: Preserva integridad del agregado de dominio y el seed inmutable. La UI mapea `slug → localized name` en render. Cumple AGENTS.md (Domain sin cambios).

### Decisión: Specs anuales paralelas — ambas deltas válidas
**Elección**: `annual-report-executive-dashboard` es canónica (declara "Reemplaza"). Ambos deltas aplican: el delta `annual-analysis-dashboard` cubre REQ-ANNUAL-030/040/070 (YoY, tablas detalle, empty state) que persisten en `Annual.cshtml`; el delta `annual-report-executive-dashboard` cubre los 17 bloques ejecutivos y Smart Summary.
**Rationale**: `Annual.cshtml` implementa los 17 bloques ejecutivos pero conserva los elementos heredados (detail toggle, empty state) con `data-testid` compartidos.

## Flujo de Datos

```
Request → UseRequestLocalization (cookie/query/accept-lang → CultureInfo)
    → Razor Page (IViewLocalizer["key"] → SharedResources.es.resx)
    → PageModel (IStringLocalizer<SharedResources>["key"])
    → _Layout (window.__i18n = Json.Serialize(localizer["js.*"]))
    → charts.js (window.__i18n.legend.income)
    → InsightsService (switch CurrentUICulture → texto ES/EN, Sentry=EN)
```

## Archivos Afectados

| Archivo | Acción | Descripción |
|------|--------|-------------|
| `Frontend/Program.cs` | Modificar | `AddLocalization`, `RequestLocalizationOptions`, `UseRequestLocalization`, `AddViewLocalization(SharedResourceType)` |
| `Frontend/Resources/SharedResources.cs` | Crear | Clase marker vacía |
| `Frontend/Resources/SharedResources.es.resx`, `.en.resx` | Crear | ~500-800 strings por cultura |
| `Frontend/Pages/Shared/_Layout.cshtml` | Modificar | `lang` dinámico, `window.__i18n`, Flatpickr locale, `IViewLocalizer` |
| `Frontend/Pages/Shared/Components/_LanguageSwitcher.cshtml` | Crear | Dropdown ES/EN + endpoint |
| `Frontend/Pages/Shared/Components/_NavItems.cshtml` | Modificar | Strings localizados |
| `~30 .cshtml` (Pages/Auth, Dashboard, Transactions, Categories, Budgets, Analysis) | Modificar | `@Localizer["key"]` reemplaza literales |
| `Frontend/Pages/Analysis/Annual.cshtml` | Modificar | Literales ES/EN → `.resx`; preservar `data-testid` |
| `Frontend/wwwroot/js/charts.js` | Modificar | Labels desde `window.__i18n` |
| `Application/Features/Analytics/Services/InsightsService.cs` | Modificar | `static`→instancia, culture-aware, Sentry=EN |
| `Application/Features/Analytics/Services/AnomalyDetectionService.cs` | Modificar | Culture-aware |
| `Application/DependencyInjection.cs` | Modificar | Registrar `InsightsService`/`AnomalyDetectionService` en DI |
| `Application/ImportTransactionsCommandHandler.cs` | Modificar | Errores localizables |
| `Infrastructure/IngExcelStatementParser.cs` | Modificar | Errores localizables |
| `e2e/tests/*.spec.ts` | Modificar | Migrar selectores a `data-testid`; nuevo test de cultura |

## Interfaces / Contratos

```csharp
// Marker class — no logic, just a type for IStringLocalizer<SharedResources>
namespace SauronSheet.Frontend.Resources;
public class SharedResources { }

// InsightsService — instance with localizer
public class InsightsService
{
    private readonly IStringLocalizer<SharedResources> _t;
    public InsightsService(IStringLocalizer<SharedResources> t) => _t = t;
    public string GenerateSmartSummary(...) // switch on CultureInfo.CurrentUICulture
}
```

```javascript
// charts.js — consume window.__i18n (scaffolded by _Layout)
const i18n = window.__i18n ?? {};
const label = i18n.legend?.income ?? 'Income'; // fallback + Sentry breadcrumb
```

## Estrategia de Testing

| Capa | Qué probar | Enfoque |
|-------|-------------|----------|
| Unit | `InsightsService` produce ES/EN según cultura | xUnit + `CultureInfo.CurrentUICulture` set en thread |
| Unit | Fallback `.resx` (clave ausente → en) | xUnit + `IStringLocalizer` mock |
| Integration | Middleware resuelve cookie/query/accept-lang | `WebApplicationFactory` + `RequestLocalizationOptions` |
| E2E | Conmutación ES→EN, persistencia cookie, `data-testid` | Playwright: `lang-switcher`, reload, assert `data-testid` no texto |

## Migración / Rollout

No se requiere migración de datos. Feature-flag implícito: ausencia de cookie → fallback inglés (comportamiento actual). Rollback = revertir el PR de infraestructura (PR1).

## Plan de PRs Encadenados (force-chained, ≤400 líneas/review)

| PR | Contenido | Archivos clave | ~Líneas | Depende de |
|----|-----------|----------------|---------|------------|
| PR1 | Infraestructura + Login piloto | `Program.cs`, `SharedResources.cs/.resx` (mínimo), `_Layout` (lang/i18n/Flatpickr), `_LanguageSwitcher`, `Login.cshtml`, E2E Login+cultura | ~350 | — |
| PR2 | Auth/Register/Index/Error | 4 `.cshtml` + `.resx` claves | ~300 | PR1 |
| PR3 | Dashboard + Transactions (5 pages) | `Dashboard.cshtml`, `Transactions/*.cshtml`, `_NavItems` | ~400 | PR1 |
| PR4 | Categories + Budgets (8 pages) | `Categories/*.cshtml`, `Budgets/*.cshtml` | ~400 | PR1 |
| PR5 | Annual + servicios bilingües | `Annual.cshtml`, `InsightsService`, `AnomalyDetectionService`, `DependencyInjection` | ~400 | PR1 |
| PR6 | `charts.js` + Chart.js + Flatpickr EN | `charts.js`, `_Layout` locale EN | ~250 | PR1 |
| PR7 | Errores import/parser + categorías sistema | `ImportTransactionsCommandHandler`, `IngExcelStatementParser`, `SharedResources` (category.system.*) | ~300 | PR1 |

## Preguntas Abiertas

- [ ] Confirmar con producto (antes de PR7): categorías de sistema — seed inglés + traducción de presentación (recomendado) vs siempre inglés.
