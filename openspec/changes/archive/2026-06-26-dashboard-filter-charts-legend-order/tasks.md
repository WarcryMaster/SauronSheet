# Tasks: Dashboard Filter Charts & Legend Order

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | ~150 (additions) + ~15 (deletions) ≈ 165 net |
| 400-line budget risk | Low |
| Chained PRs recommended | Yes (force-chained, stacked-to-main) |
| Suggested split | PR 1 → PR 2 → PR 3 |
| Delivery strategy | force-chained |
| Chain strategy | stacked-to-main |
| Review budget | 800 lines |

Decision needed before apply: No
Chained PRs recommended: Yes
Chain strategy: stacked-to-main
400-line budget risk: Low

### Suggested Work Units

| Unit | Goal | Likely PR | Notes |
|------|------|-----------|-------|
| 1 | DTOs + Application queries (TDD) with new date-range signatures, Year field, and legend sort | PR 1 | Pure Application layer; builds clean; no UI impact yet |
| 2 | PageModel + Razor + charts.js wiring (pill filter, dead code, JSDoc) | PR 2 | Depends on PR 1; UI rehydration + `d.year` labels |
| 3 | Documentation + final verification (E2E + build) | PR 3 | Depends on PR 2; adds Charts section; locks in contract |

---

## Phase 1: Application Layer (TDD — Tests First)

### Work Unit 1 — PR 1 (base: main)

- [x] 1.1 RED: Crear `tests/SauronSheet.Application.Tests/Features/Analytics/Queries/GetMonthlySpendingByCategoryQueryTests.cs` con casos: rango con gastos, sin gastos, multi-mes, single-mes, cambio de rango
- [x] 1.2 RED: Actualizar `tests/SauronSheet.Application.Tests/Features/Analytics/Queries/GetMonthlyTrendsQueryTests.cs` para nueva firma `(FromDate, ToDate)`; añadir test del campo `Year` rellenado
- [x] 1.3 GREEN: Añadir campo `int Year` a `src/SauronSheet.Application/Features/Analytics/DTOs/MonthlyCategorySpendingDto.cs`
- [x] 1.4 GREEN: Añadir campo `int Year` a `src/SauronSheet.Application/Features/Analytics/DTOs/MonthlyTrendDto.cs`
- [x] 1.5 GREEN: Cambiar firma de `GetMonthlySpendingByCategoryQuery` a `(DateTime FromDate, DateTime ToDate)`
- [x] 1.6 GREEN: Reescribir `GetMonthlySpendingByCategoryQueryHandler` con `TransactionByDateRangeSpecification(FromDate, ToDate)`; ordenar por total descendente; desempate por nombre asc; poblar `Year` por entrada
- [x] 1.7 GREEN: Cambiar firma de `GetMonthlyTrendsQuery` a `(DateTime FromDate, DateTime ToDate)`
- [x] 1.8 GREEN: Reescribir `GetMonthlyTrendsQueryHandler` para iterar meses calendario solapados por el rango; emitir entrada por mes con `Amount=0` si no hay gastos; poblar `Year`
- [x] 1.9 REFACTOR: limpiar duplicación de `GetSpainMonth()` y normalización de fechas; verificar `dotnet test` pasa

## Phase 2: Frontend Wiring

### Work Unit 2 — PR 2 (base: PR 1)

- [x] 2.1 Actualizar `src/SauronSheet.Frontend/Pages/Dashboard.cshtml.cs`: pasar `FromDate/ToDate` a `GetMonthlySpendingByCategoryQuery` y `GetMonthlyTrendsQuery`; resolver YoY como `(ToDate.Year - 1, ToDate.Year)`; eliminar propiedad `SpendingByCategory` y `ResolveAnalyticsYearAsync` ya obsoleto
- [x] 2.2 Eliminar bloque `<script id="dashboard-category-data">` en `src/SauronSheet.Frontend/Pages/Dashboard.cshtml`
- [x] 2.3 Cambiar títulos de gráficas de `@Model.AnalyticsYear` a rango legible en `Dashboard.cshtml` (ej. `Spending by Category — Apr–Jun 2026`)
- [x] 2.4 En `src/SauronSheet.Frontend/wwwroot/js/charts.js`: `initCategoryStackedChart` usa `d.year`+`d.monthName` para labels; preserva orden del array
- [x] 2.5 En `charts.js`: `initMonthlyTrendsChart` usa `d.year`+`d.monthName`; respeta orden del payload
- [x] 2.6 Añadir JSDoc en `charts.js` declarando: "handler MUST order datasets; frontend MUST NOT reorder ni dedupe alterando la secuencia"
- [x] 2.7 Verificar `src/SauronSheet.Frontend/Pages/Budgets/Comparison.cshtml`: datasets `Budget` y `Actual` en ese orden (cumple regla)

## Phase 3: Documentation & Verification

### Work Unit 3 — PR 3 (base: PR 2)

- [x] 3.1 Añadir sección `## Charts` a `.github/instructions/razor-frontend.instructions.md` con regla explícita "legend order MUST match dataset order" + referencia al JSDoc contract
- [x] 3.2 Verificar `dotnet build` limpio en solución completa
- [x] 3.3 Verificar `dotnet test` con todos los proyectos; confirmar cobertura Application ≥ 70%
- [x] 3.4 Ejecutar Playwright E2E: cambio de pill recarga `categoryStackedChart`, `monthlyTrendsChart`, `yearlyComparisonChart`; renderiza datos del nuevo rango
- [x] 3.5 Verificar que el HTML renderizado de `/dashboard` no contiene `id="dashboard-category-data"`
- [x] 3.6 Verificar visualmente que el orden de leyenda coincide con orden de dataset en las tres gráficas
