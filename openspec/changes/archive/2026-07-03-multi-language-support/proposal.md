# Propuesta: Soporte Multi-Idioma (Español / Inglés)

**Cambio**: `multi-language-support`
**Fecha**: 2026-07-02
**Estrategia de entrega**: force-chained (PRs encadenados ≤ ~400 líneas de revisión)
**Modo**: hybrid (openspec + engram)

---

## Intención

SauronSheet es monolingüe de facto en inglés (~85-90%) con bolsas de español heredadas (`InsightsService`, `Annual.cshtml`, `Upload.cshtml`, `_Layout.cshtml`). No existe infraestructura de localización alguna (sin `.resx`, sin `IStringLocalizer`, sin middleware de cultura). Esta change normaliza el idioma y habilita conmutación ES/EN por usuario, alineando UI estática, servicios que generan texto y librerías (Flatpickr, Chart.js) con la cultura activa.

## Alcance

### Incluido
- Infraestructura ASP.NET Core: `AddLocalization`, `RequestLocalizationOptions`, middleware `UseRequestLocalization`
- Culture providers: **Cookie (principal) + QueryString + Accept-Language (fallback). Sin cultura en la URL (MVP)**
- Recursos `.resx` (es/en) con `IStringLocalizer<T>`/`IViewLocalizer` para UI estática (~500-800 strings, ~30 `.cshtml`)
- Diccionario JSON inyectado (`window.__i18n`) para `charts.js`
- Servicios culture-aware: `InsightsService`, `AnomalyDetectionService` (generación bilingüe **manual**, no auto-traducción)
- Flatpickr: locale dinámico (es/en); Chart.js: labels desde diccionario
- Language switcher (navbar) con persistencia en cookie (1 año)
- Migración E2E a selectores `data-testid`

### Excluido (no-goals)
- Cultura en la URL (`/es/...`)
- Traducción de contenido generado por el usuario (nombres de categorías, descripciones de transacciones)
- Tabla `translations` en Supabase / admin UI (futuro)
- Más de 2 idiomas (solo ES/EN)
- Auto-traducción (todo el texto se autora manualmente)

## Capacidades

> Contrato con `sdd-spec`. Se investigó `openspec/specs/`.

### Nuevas
- `localization`: infraestructura de localización — conmutación y persistencia de cultura, recursos `.resx`, diccionario JS, locale de librerías y patrón culture-aware para servicios.

### Modificadas
- `annual-analysis-dashboard`: escenarios con texto UI español hardcodeado ("Sin datos del año anterior" REQ-ANNUAL-030, "Ver detalle" REQ-ANNUAL-040, "Sin datos para este año" REQ-ANNUAL-070) pasan a ser culture-dependent. **`sdd-spec` auditará el resto de specs por texto esperado culture-dependent.**

## Enfoque

Estrategia híbrida (recomendada en exploración): `.resx` para UI estática (nativa, rápida, tipada), `CultureInfo.CurrentUICulture` para texto dinámico de servicios, y diccionario JSON inyectado desde el servidor para JS. La herramienta correcta para cada tipo de string.

### Decisiones clave
| Decisión | Recomendación |
|---|---|
| Culture providers | Cookie + QueryString + Accept-Language fallback. Sin URL culture |
| Switcher | Navbar (lado derecho, icono globo + dropdown ES/EN) |
| Categorías del sistema | **Pregunta abierta** — ver Ronda de preguntas |
| `InsightsService` | Generación bilingüe manual por rama `CultureInfo.CurrentUICulture` |
| E2E | Migrar a `data-testid` (ya hay precedente en REQ-ANNUAL-040) + actualizar aserciones |

### Primer slice (PR1) — infraestructura + prueba integral
- `Program.cs`: registro de localización + providers + middleware
- `SharedResources` + `.resx` es/en con set mínimo de claves
- Switcher (navbar) + endpoint de persistencia de cookie
- `_Layout.cshtml`: `<html lang>` dinámico, Flatpickr locale dinámico, scaffold `window.__i18n`
- Una página completa (`Login`) traducida como prueba
- E2E: `data-testid` en `Login` + test de conmutación de cultura

### Slices posteriores (chained PRs)
- PR2: Auth/Index/Error · PR3: Dashboard/Transactions · PR4: Categories/Budgets · PR5: Annual/Analysis + `InsightsService`/`Anomaly` bilingüe · PR6: `charts.js` + Chart.js + Flatpickr `en` · PR7: errores de import/parser + categorías sistema (pendiente decisión)

## Reglas de negocio
- El idioma persiste por usuario vía cookie (1 año); ausente → Accept-Language del navegador → fallback inglés
- Culturas soportadas: `es` (es-ES), `en` (en-US); `en` es fallback por defecto
- El contenido generado por usuario **no** se traduce
- Las trazas de Sentry permanecen en inglés (no negociable, conforme a `AGENTS.md`)

## Áreas afectadas
| Área | Impacto |
|---|---|
| `Frontend/Program.cs` | Nuevo: registro localización, middleware, providers |
| `~30 .cshtml` (Pages/Layout/Partials) | Modificado: extracción a `@Localizer["key"]` |
| `wwwroot/js/charts.js` | Modificado: labels desde `window.__i18n` |
| `_Layout.cshtml` | Modificado: `lang` dinámico, Flatpickr locale, JSON scaffold |
| `Application/InsightsService.cs`, `AnomalyDetectionService.cs` | Modificado: culture-aware |
| `Application/ImportTransactionsCommandHandler.cs`, `Infrastructure/IngExcelStatementParser.cs` | Modificado: errores localizables |
| `Domain` | Sin cambios (no depende de i18n) |
| `e2e/` | Modificado: `data-testid` + test de cultura |

## Riesgos
| Riesgo | Prob. | Mitigación |
|---|---|---|
| Volumen de extracción (~500-800 strings) — omisiones/typos | Media | Script de auditoría de strings hardcodeados |
| `InsightsService`: traducción literal poco natural | Media | Autoría manual de ambas versiones |
| Drift entre `.resx` y diccionario JS | Media | Auditoría de claves compartidas |
| E2E se rompe al cambiar textos | Alta | Migración a `data-testid` |
| Conflicto de cookies (cultura vs Supabase Auth) | Baja | Verificar scopes/rutas de cookie |
| Tamaño del cambio vs presupuesto de revisión (800) | Alta | Chained PRs obligatorios |

## Plan de rollback
- Feature-flag de localización (gate de middleware + switcher). Desactivar → revierte a inglés-only actual.
- Cada extracción `.resx` es aditiva (fallback a inglés). Revertir un slice = revertir ese PR.
- La cookie de cultura se puede limpiar; fallback inglés.

## Dependencias
- `Microsoft.AspNetCore.Localization` (incluido en el framework, sin NuGet nuevo)
- Flatpickr `l10n/en.js` (ya en la distribución CDN de Flatpickr)

## Criterios de éxito
- [ ] Usuario conmuta ES/EN vía switcher; persiste entre sesiones (cookie)
- [ ] Toda la UI estática en páginas del scope renderiza en la cultura activa
- [ ] `InsightsService` genera resumen en idioma activo (natural, no literal)
- [ ] `charts.js` y Flatpickr siguen la cultura activa
- [ ] Sentry 100% en inglés; `dotnet build` + `dotnet test` verdes; E2E de cultura pasa

## Ronda de preguntas de propuesta
1. **Categorías del sistema** ("Salary", "Groceries", etc.): ¿(a) mantener inglés como dato canónico de seed y traducir solo en presentación vía recurso por clave, o (b) dejarlas siempre en inglés aunque la UI esté en español? **Recomiendo (a)** — seed inmutable en inglés + traducción de presentación — pero afecta la semántica del seed y necesita confirmación de producto antes de PR7.
