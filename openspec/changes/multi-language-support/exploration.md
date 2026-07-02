# Exploración: Soporte Multi-Idioma (Español / Inglés)

**Fecha**: 2026-07-02
**Estado**: Completada
**Cambio**: `multi-language-support`

---

## 1. Estado Actual

### 1.1 Infraestructura de Localización

**Inexistente.** No hay absolutamente nada configurado:

| Elemento | ¿Presente? | Detalle |
|---|---|---|
| Archivos `.resx` | ❌ | Cero archivos en todo el proyecto |
| `IStringLocalizer<T>` / `IViewLocalizer` | ❌ | Ninguna referencia en `src/` |
| `RequestLocalizationOptions` / `UseRequestLocalization` | ❌ | No existe en `Program.cs` |
| `AddLocalization` en servicios | ❌ | No registrado |
| `supportedCultures` / `SupportedCultures` | ❌ | No configurado |
| Middleware de cultura (cookie/query) | ❌ | Ausente |
| `<html lang="en">` | ✅ Hardcoded | En `_Layout.cshtml`, fijo a `"en"` |
| Tabla `translations` en Supabase | ❌ | No existe |
| Campo de idioma en entidades de dominio | ❌ | Ninguna entidad tiene campo de idioma |

### 1.2 Distribución de Idiomas en la UI

**~85-90% inglés, ~10-15% español disperso en lugares específicos.**

#### Páginas completamente en inglés
- `Login.cshtml` — "Welcome back", "Sign in", "Email address", "Password", "Remember me", "Authentication failed"
- `Register.cshtml` — "Create your account", "Start tracking your finances for free", "Email address", "Password"
- `Index.cshtml` — "Take Control of Your Finances", "Get Started Free", "Excel Import", "Smart Analytics", "Budget Tracking"
- `Dashboard.cshtml` — "Total Income", "Total Expenses", "Net Amount", "Transactions", "All Time", "This Month", "Budget Status", "On Track", "Warning"
- `Error.cshtml` — "Something went wrong", "An unexpected error occurred", "Back to Home"
- `Categories/Index.cshtml` — "Category Management", "Income Categories", "Expense Categories", "Add New Category", "Save Category", "Edit", "Delete"
- `_BudgetStatusModal.cshtml` — "Deactivate budget", "Budget deactivation", "Are you sure", "Cancel"
- `_ConfirmDeleteModal.cshtml` — Confirmación de eliminación

#### Páginas mixtas (con bolsas de español)
- `_Layout.cshtml` — "Saltar al contenido principal" (skip to content). Resto en inglés: "Logout", "Get Started", "Close", "Toggle navigation"
- `Annual.cshtml` — Mayormente inglés (gráficos, KPIs), pero con español en etiquetas de health score: "Excelente", "Buena", "Regular", "Necesita atención", "Año completo", "Sin datos del año anterior", "Dependencia Categorías (10%)"
- `Transactions/Upload.cshtml` — Formato CSV en español: "F. VALOR | CATEGORÍA | SUBCATEGORÍA | DESCRIPCIÓN | COMENTARIO | IMPORTE (€) | SALDO (€)"

#### Código C# (Application Layer)
- **`InsightsService.cs`** — Devuelve resumen en español: "Sin datos para este año", "Tus ingresos crecieron un X%", "Tu tasa de ahorro fue del X%", "Tus dos mayores categorías de gasto representan el X%", "Tus gastos aumentaron un X%"
- **`AnomalyDetectionService.cs`** — Descripciones en inglés: "exceptional spike", "extraordinary expense", "anomaly above μ+2σ threshold"
- **`ImportTransactionsCommandHandler.cs`** — Errores en inglés: "Only Excel files (.xls, .xlsx) are accepted", "Could not parse the uploaded file", "Invalid date format", "Duplicate"
- **`IngExcelStatementParser.cs`** — Usa `es-ES` para parsing de decimales con coma y fechas `dd/MM/yyyy` de extractos bancarios españoles, pero mensajes de error en inglés

#### JavaScript
- **`charts.js`** — 100% inglés: "Expenses", "Income", "Net", "Savings", "Avg Income", "Avg Expense", "Anomaly amount", "Historical mean", "Year 1", "Year 2"
- **`alpine-loader.js`** — Sin strings de usuario

### 1.3 ViewData["Title"] — Títulos de Página

Todos los títulos están hardcodeados en inglés en cada `.cshtml`:

| Página | Título |
|---|---|
| Index | "Home" |
| Dashboard | "Dashboard" |
| Auth/Login | "Sign In - SauronSheet" |
| Auth/Register | "Create Account" |
| Transactions | "Transactions" |
| Transactions/Search | "Search Transactions" |
| Transactions/Add | "Add Transaction" |
| Transactions/Upload | "Upload Bank Statement" |
| Categories | "Category Management" |
| Budgets | "Budgets" |
| Budgets/Create | "Create Budget" |
| Budgets/Edit | "Edit Budget" |
| Budgets/Comparison | "Budget vs Actual" |
| Budgets/History | "Budget History" |
| Analysis/Annual | "Annual Dashboard" |
| Error | "Error" |

### 1.4 Librerías Externas con Soporte de Locale

| Librería | Estado Actual | Soporte i18n |
|---|---|---|
| **Flatpickr** | ✅ `l10n/es.js` cargado en `_Layout.cshtml`. Usa `d/m/Y` globalmente. Solo locale español disponible. | Excelente — tiene `l10n/en.js`, `l10n/de.js`, etc. Se puede cargar dinámicamente |
| **Chart.js** | ❌ Sin configuración de locale. Tooltips en inglés hardcodeados en `charts.js` | Tiene `Chart.register(require('chartjs-adapter-date-fns'))` pero no se usa. Tooltips se configuran por chart |
| **MDBootstrap** | N/A (componentes visuales sin texto localizado) | No aplica — es CSS/JS de UI |

### 1.5 Entidades de Dominio — Texto Visible al Usuario

| Entidad | Campos de texto | ¿Soporta traducciones? | Notas |
|---|---|---|---|
| `Category` | `Name` (CategoryName VO), `IconName` | ❌ | Nombres de categoría creados por el usuario. Sistema defaults en inglés ("Salary", "Groceries", etc.) |
| `Subcategory` | `Name` (SubcategoryName VO) | ❌ | Igual que categorías |
| `Budget` | Sin texto de usuario directo | ❌ | Solo datos financieros |
| `Transaction` | `Description`, `BankCategory`, `BankSubcategory` | ❌ | Vienen del extracto bancario (español para ING). `BankCategory` → resuelto a categoría interna |
| `bank_category_translations` | Tabla de mapeo banco→interno | ❌ | Es para resolver categorías bancarias, NO para UI i18n |

### 1.6 Sentry Traces

✅ **Conforme.** Todos los breadcrumbs, eventos, tags y métricas en Sentry están en inglés, como exige `AGENTS.md`:
- `"Excel import started"`, `"Excel import completed"`
- Tags: `"filename"`, `"imported"`, `"skipped"`, `"ext"`
- Métricas: `"app.import.started"`, `"app.import.rows_imported"`
- Capturas de excepción en handlers y middleware

No hay strings en español en las trazas de Sentry.

---

## 2. Áreas Afectadas

La implementación de multi-idioma tocará prácticamente todas las capas:

| Capa | Archivos | Naturaleza del cambio |
|---|---|---|
| **Frontend — Razor Pages** | 30 `.cshtml` (páginas, layouts, partials, componentes) | Extraer strings a recursos, reemplazar con `@Localizer["key"]` |
| **Frontend — JS** | `charts.js` (1100+ líneas) | Mover labels de chart a diccionario i18n inyectado desde servidor |
| **Frontend — Program.cs** | `Program.cs` | Agregar `AddLocalization()`, `RequestLocalizationOptions`, culture providers |
| **Frontend — _Layout.cshtml** | `<html lang="...">` | Dinamizar atributo `lang` según cultura activa |
| **Frontend — Flatpickr** | `_Layout.cshtml` | Cargar locale dinámicamente según cultura |
| **Application — InsightsService.cs** | ~80 líneas de strings | Hacer culture-aware (generar resumen en idioma activo) |
| **Application — AnomalyDetectionService.cs** | ~10 líneas de strings | Hacer culture-aware (descripciones de anomalías) |
| **Application — ImportTransactionsCommandHandler.cs** | ~15 mensajes de error | Potencialmente localizables (errores de usuario) |
| **Infrastructure — IngExcelStatementParser.cs** | ~5 mensajes de error | Potencialmente localizables |
| **Domain** | Sin cambios | Las entidades no deben depender de i18n |

---

## 3. Estrategias de Localización Evaluadas

### Estrategia A: `.resx` + `IStringLocalizer` (ASP.NET Core Nativo)

**Descripción**: Usar archivos `.resx` (Resources) con `IStringLocalizer<T>` para Razor Pages y `IViewLocalizer` para vistas. Agregar `RequestLocalizationOptions` con `CookieRequestCultureProvider` + `QueryStringRequestCultureProvider`. Flatpickr y Chart.js se configuran con locale matching.

**Pros**:
- Nativo de .NET, tooling maduro (ResXResourceManager, Visual Studio editor)
- Compile-time safety con `IStringLocalizer<T>` (claves tipadas)
- Excelente rendimiento (los recursos se embeben en el assembly)
- Documentación oficial extensa
- Soporte para fallback cultures (es → es-ES, en → en-US)

**Contras**:
- Modificar `.resx` requiere recompilar y redeploy
- Los strings de JS necesitan un mecanismo separado (JSON dictionary inyectado)
- ~30 archivos `.cshtml` que requieren extracción manual de strings (~500-800 strings estimados)
- No cubre contenido dinámico (nombres de categorías creadas por usuario)

**Esfuerzo**: Alto (extracción masiva de strings) pero riesgo técnico bajo.

---

### Estrategia B: DB-Driven (Tabla de Traducciones en Supabase)

**Descripción**: Crear tabla `translations(key, language, value)` en Supabase. Servicio `ITranslationService` que carga traducciones en memoria (caché). UI renderiza desde el servicio.

**Pros**:
- Editable en caliente (sin redeploy)
- Single source of truth en base de datos
- Fácil agregar idiomas nuevos
- Dashboard de administración de traducciones posible

**Contras**:
- Dependencia de BD para cada renderizado de página (necesita caché agresiva)
- Sin compile-time safety (claves son strings mágicos)
- Más complejo que `.resx` para el caso de uso (solo 2 idiomas)
- Cold start: hay que cargar traducciones al iniciar la app
- Performance: cada `IStringLocalizer` lookup es más lento que `.resx` embebido
- Overkill para MVP con solo español/inglés

**Esfuerzo**: Medio-Alto (infraestructura de caché, servicio, migración de BD, UI admin opcional).

---

### Estrategia C: Híbrida — `.resx` + Culture Middleware + JSON para JS (RECOMENDADA)

**Descripción**: Combinar lo mejor de ambos mundos:

1. **Strings estáticos de UI**: `.resx` + `IStringLocalizer<T>` (nativo, rápido, tipado)
   - Crear `Resources/` folder con `SharedResources.es.resx`, `SharedResources.en.resx`
   - Usar `IStringLocalizer<SharedResources>` en Razor Pages
   - `IViewLocalizer` para vistas que necesitan contexto de vista

2. **Strings dinámicos (InsightsService, AnomalyDetectionService)**: Culture-aware vía `CultureInfo.CurrentUICulture`
   - Generar resumen en español o inglés según cultura activa
   - Sin dependencia de recursos externos

3. **JavaScript (charts.js)**: Diccionario JSON inyectado desde servidor
   - Generar `window.__i18n = { "Expenses": "Gastos", ... }` en `_Layout.cshtml`
   - `charts.js` lee del diccionario en vez de strings hardcodeados

4. **Flatpickr**: Cargar `l10n/es.js` o `l10n/en.js` según cultura

5. **Culture providers**: Cookie (principal) + QueryString (para compartir URLs) + Accept-Language header (fallback)
   - Language switcher UI en el footer/navbar (bandera o dropdown)

6. **Middlewares**: `app.UseRequestLocalization()` después de `UseRouting()`

**Pros**:
- Pragmático: usa la herramienta correcta para cada tipo de string
- Compile-time safety donde importa (UI estática)
- JS no se vuelve una pesadilla de i18n (diccionario simple)
- Performance excelente (`.resx` embebido, JSON pequeño en `<script>`)
- Categorías de usuario siguen single-language (aceptable para MVP)

**Contras**:
- No resuelve traducción de contenido generado por usuario (nombres de categorías)
- Requiere disciplina para mantener sincronizados `.resx` y diccionario JS

**Esfuerzo**: Medio. Es la estrategia más balanceada para 2 idiomas.

---

### Comparativa

| Criterio | A: .resx puro | B: DB-Driven | C: Híbrida (recomendada) |
|---|---|---|---|
| Rendimiento | ⭐⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐⭐⭐ |
| Editabilidad sin deploy | ❌ | ✅ | ❌ (aceptable) |
| Compile-time safety | ✅ | ❌ | ✅ |
| Complejidad de implementación | Media | Alta | Media |
| Cobertura JS | ⚠️ Necesita addon | ⚠️ Necesita addon | ✅ Integrado |
| Escalabilidad a >2 idiomas | ✅ | ✅✅ | ✅ |
| Mantenibilidad | Alta | Media | Alta |
| Adecuación al stack | ✅✅ | ⚠️ Overkill | ✅✅✅ |

---

## 4. Recomendación

**Estrategia C (Híbrida)** es la opción recomendada para SauronSheet por las siguientes razones:

1. **Stack alignment**: ASP.NET Core + Razor Pages está diseñado para `.resx`. No tiene sentido luchar contra el framework.
2. **Solo 2 idiomas**: Para español/inglés, `.resx` es más que suficiente. DB-driven sería sobre-ingeniería.
3. **Performance**: Los `.resx` se compilan en el assembly — cero latencia de BD.
4. **JS pragmático**: Solo hay 2 archivos JS con strings de usuario (`charts.js` y algunos Alpine.js inline). Un diccionario JSON de ~40 claves los cubre completamente.
5. **Futuro**: Si en el futuro se necesitan traducciones comunitarias o admin UI, se puede migrar el backend a DB manteniendo la misma interfaz `IStringLocalizer`.

---

## 5. Riesgos Identificados

1. **Volumen de extracción**: ~500-800 strings estimados entre Razor, C#, y JS. Riesgo de omitir strings o introducir typos en las claves. **Mitigación**: generar script de auditoría que detecte strings hardcodeados restantes.

2. **Consistencia InsightsService**: Actualmente genera resumen en español con gramática compleja ("Tus ingresos crecieron"). La versión en inglés debe ser igual de natural, no una traducción literal. **Mitigación**: escribir ambas versiones manualmente, no usar traducción automática.

3. **Flatpickr `d/m/Y` vs `m/d/Y`**: Actualmente solo está configurado formato español. Si un usuario prefiere inglés, el datepicker debe cambiar a `m/d/Y`. **Mitigación**: cargar `l10n/en.js` dinámicamente según cultura.

4. **Categorías del sistema**: Los nombres de categorías por defecto ("Salary", "Groceries", "Rent", etc.) están en inglés. Si la UI está en español, ¿deberían traducirse? **Decisión pendiente**: se abordará en la fase de diseño.

5. **E2E tests**: Los tests de Playwright pueden romperse si cambian los labels/textos de la UI. **Mitigación**: usar `data-testid` para selectores en vez de texto, o actualizar tests tras el cambio.

6. **URLs con culture**: Agregar `{culture}` en las rutas (`/es/transactions` vs `/en/transactions`) complica el routing. **Recomendación**: NO usar culture en URL para MVP — usar solo cookie + query string + Accept-Language.

---

## 6. Conclusión

El sistema está en un estado de **monolingüe de facto en inglés** (~85-90%), con bolsas de español heredadas de decisiones puntuales en fases tempranas del proyecto (InsightsService, etiquetas de Annual Dashboard). No hay absolutamente ninguna infraestructura de localización.

La estrategia híbrida `.resx` + culture middleware + JSON para JS es la más adecuada. El esfuerzo es medio pero acotado: implica extraer sistemáticamente todos los strings a recursos, implementar el middleware de cultura, y adaptar los servicios que generan texto dinámico (InsightsService, AnomalyDetectionService).

**Listo para avanzar a fase de propuesta (`sdd-propose`).**
