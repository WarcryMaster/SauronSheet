# Tareas: Rediseño Integral del Informe Anual

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | ~1150 (T1: ~400, T2: ~400, T3: ~350) |
| 400-line budget risk | Medium |
| Chained PRs recommended | Yes |
| Suggested split | PR 1 → PR 2 → PR 3 |
| Delivery strategy | force-chained |
| Chain strategy | stacked-to-main |

Decision needed before apply: No
Chained PRs recommended: Yes
Chain strategy: stacked-to-main
400-line budget risk: Medium

### Suggested Work Units

| Unit | Goal | Likely PR | Base | Notes |
|------|------|-----------|------|-------|
| T1 | Core KPIs + Summary + Ratios + Health Score + Year Nav | PR 1 | main | Domain repo method + DTOs base + Handler + Hero view |
| T2 | Multi-year + Monthly + Categories + Timeline + Top Movements | PR 2 | main | Charts (+3 init) + secciones intermedias |
| T3 | Anomalies + Discoveries + Achievements + Trends + Predictions + Hist. Comp. | PR 3 | main | Charts (+2 init) + secciones avanzadas |

---

## PR 1: Core Tier (T1) — REQ-001, -002, -011, -012, -018

### Fase 1: Infrastructure (RED → GREEN → REFACTOR)

- [x] 1.1 Añadir `GetByUserIdAndYearRangeAsync(UserId, int fromYear, int toYear)` a `ITransactionRepository` — server-side date filter vía EXTRACT(YEAR). RED: test que falla en `ITransactionRepository` interface test.
- [x] 1.2 Implementar en `SupabaseTransactionRepository.cs`: query Postgrest con `Where(x => x.Date.Year >= fromYear && x.Date.Year <= toYear)`. GREEN: test unitario con mock pasa.
- [x] 1.3 REFACTOR: verificar que no hay duplicación con `FindBySpecificationAsync`; añadir Sentry breadcrumb en método nuevo. ✅ `SupabaseTransactionRepository.GetByUserIdAndYearRangeAsync` ahora añade breadcrumb `repository.query` con `userId/fromYear/toYear`.

### Fase 2: DTOs Base (RED → GREEN → REFACTOR)

- [x] 2.1 Equivalencia funcional (decisión de cierre): se mantiene contrato legacy `GetAnnualDashboardResultDto` como DTO activo del dashboard anual; `AnnualDashboardDto.cs` queda únicamente como artefacto de referencia/no operativo. Evidencia: `GetAnnualDashboardQuery : IRequest<GetAnnualDashboardResultDto>`, `GetAnnualDashboardQueryHandler : IRequestHandler<GetAnnualDashboardQuery, GetAnnualDashboardResultDto>` y `Annual.cshtml.cs` consume `GetAnnualDashboardResultDto`.
- [x] 2.2 Crear `AnnualDashboardSummaryDto.cs`: income, expense, net, savings, savingsRate, YoY abs+%, rank, prev/next pointers. GREEN: test de cálculo con datos fabricados.
- [x] 2.3 Crear `AnnualDashboardRatiosDto.cs`: savingsRate, avgMonthly I/E/S, avgDaily, avgPerTrx, trxCount, avgOpsPerMonth. Values nulos → string "—". RED: test de div/0.
- [x] 2.4 Crear `AnnualDashboardHealthScoreDto.cs`: total (0-100) + 6 sub-scores con label + weight + value. GREEN: test de fórmula determinística.
- [x] 2.5 REFACTOR naming (cerrado por decisión): no se ejecuta renombrado de contrato para evitar ruptura; se conserva naming legacy `GetAnnualDashboardResultDto` por compatibilidad y se declara explícitamente que `AnnualDashboardDto` no es el contrato productivo.

### Fase 3: Servicios Core (RED → GREEN → REFACTOR)

- [x] 3.1 Crear `AnnualSummaryService.cs` en `Features/Analytics/Services/` — procesa `IReadOnlyList<Transaction>` + año + clasificación → `AnnualDashboardSummaryDto`. RED: test con 2 años de datos fabricados.
- [x] 3.2 Crear `FinancialRatiosService.cs` — computa ratios desde transactions agregadas. GREEN: test con transacciones mensuales simuladas.
- [x] 3.3 Crear `HealthScoreService.cs` — 6 sub-scores ponderados: Savings(25%), IncomeStab(15%), ExpenseStab(15%), CatDep(10%), Balance(20%), Trend(15%). RED: test de ponderación exacta.
- [x] 3.4 Crear `InsightsService.cs` — genera `SmartSummary` de 2-4 frases regladas (sin IA): cambio income, cambios categorías, hito ahorro. GREEN: test de narrativa esperada.
- [x] 3.5 REFACTOR: los 4 servicios son puros (static, `IReadOnlyList<Transaction>` input). Verificar sin dependencias externas. Añadir Sentry `SentrySdk.Metrics.Distribution` para tiempo de cómputo. ✅ Métricas añadidas en `GetAnnualDashboardQueryHandler` alrededor de cada invocación T1 (`annual_summary_ms`, `financial_ratios_ms`, `health_score_ms`, `smart_summary_ms`) para preservar pureza de servicios.

### Fase 4: Query Composite (RED → GREEN → REFACTOR)

- [x] 4.1 Equivalencia funcional (contrato legacy aprobado): `GetAnnualDashboardQuery.cs` devuelve `IRequest<GetAnnualDashboardResultDto>` con `int Year`; no se migra a `AnnualDashboardDto` por decisión explícita de producto/compatibilidad.
- [x] 4.2 Crear `GetAnnualDashboardQueryHandler.cs` — carga transactions 1 vez via `GetByUserIdAndYearRangeAsync`, particiona en memoria por año, ejecuta `AnnualClassificationEngine` para año actual, delega en T1 services → `AnnualDashboardDto`. GREEN: test con mock de repositorio.
- [x] 4.3 REFACTOR: verificar que handler NO carga N veces; añadir Sentry `ISpan` para tracing del handler completo. ✅ Handler crea child span desde `SentrySdk.GetSpan()` y emite `handler_ms` al finalizar.
- [x] 4.4 Equivalencia funcional en DI: no se añade registro explícito porque `AddMediatR(...RegisterServicesFromAssembly(...))` ya registra `GetAnnualDashboardQueryHandler` automáticamente y los servicios T1 son `static` puros (sin dependencia ni necesidad de registro DI).

### Fase 5: Frontend — Hero + Year Nav (RED → GREEN → REFACTOR)

- [x] 5.1 Reescribir `Annual.cshtml.cs` — PageModel ligero: bindea `Year`, llama `IMediator.Send(new GetAnnualDashboardQuery(year))`, expone `DashboardData`. RED: test de PageModel recibe DTO correcto.
- [x] 5.2 Reescribir `Annual.cshtml` — hero full-viewport ejecutivo: año, balance, income, expense, savings, savings rate, YoY badges, year rank. Mismo layout tipo revista con `data-testid` preservados o mapeados. RED: test de renderizado.
- [x] 5.3 Añadir navegación año vía HTMX: `◀Año▶` + desplegable. `hx-get="/Analysis/Annual?Year=N"` swap `#annual-content`. Botón ◀ disabled en primer año, ▶ disabled si no hay año siguiente. GREEN: test E2E de navegación.
- [x] 5.4 Añadir skeleton loader con `x-show="loading"` durante fetch HTMX. GREEN: test visual.
- [x] 5.5 Añadir sección Smart Summary (REQ-002) debajo del hero. GREEN: test de renderizado con y sin datos.
- [x] 5.6 Añadir sección Financial Ratios (REQ-011) — tarjetas compactas. GREEN: test de renderizado.
- [x] 5.7 Añadir sección Health Score (REQ-012) — score total + 6 sub-scores visibles en tarjetas. GREEN: test de renderizado.
- [x] 5.8 REFACTOR: verificar `data-testid` legacy: `annual-kpi-income`, `annual-kpi-expense`, `annual-kpi-net`, `annual-kpi-fixed-pct`, `annual-empty-state` preservados.

### Fase 6: Tests PR 1

- [x] 6.1 Escribir tests unitarios de `AnnualSummaryService` (8 tests): empty, single-year, multi-year, rank 1st, YoY abs+%, sin año anterior, sin año siguiente.
- [x] 6.2 Escribir tests unitarios de `FinancialRatiosService` (6 tests): ratios normales, div/0, sin transacciones, 1 transacción.
- [x] 6.3 Escribir tests unitarios de `HealthScoreService` (6 tests): score perfecto, score mínimo, valores intermedios, sub-scores individuales.
- [x] 6.4 Escribir tests unitarios de `InsightsService` smart summary (4 tests): income creció, categorías dominantes, hito ahorro, año vacío.
- [x] 6.5 Escribir test de `GetAnnualDashboardQueryHandler` (4 tests): flujo completo T1, año vacío, año sin anterior, mock de repositorio.
- [x] 6.6 Escribir test de PageModel `AnnualModel` (3 tests): bindea año correcto, llama mediator, recibe DashboardData. ✅ Añadidos tests `OnGetAsync_WithBoundYear_SendsQueryWithSameYear`, `OnGetAsync_WithoutBoundYear_UsesCurrentUtcYear`, `OnGetAsync_AssignsResultFromMediatorResponse`.
- [x] 6.7 Actualizar E2E `07-annual-analysis.spec.ts` (4 tests): hero KPIs visibles, year nav ◀▶, health score renderizado, empty state.
- [x] 6.8 Verificar cobertura: `dotnet test --collect:"XPlat Code Coverage"` — domain ≥80%, application ≥70%.

---

## PR 2: Multi-Year Tier (T2) — REQ-003, -004, -005, -006, -007, -009, -010

### Fase 1: DTOs T2 (RED → GREEN → REFACTOR)

- [x] 1.1 Crear `AnnualDashboardMultiYearDto.cs` — income/expense/savings/balance arrays por año + highlight selected + prev/next/avg/best/worst pointers. RED: test de construcción multi-year. ✅ 12 tests DTOs T2 (1.1-1.6)
- [x] 1.2 Crear `AnnualDashboardMonthlyDto.cs` — 12 meses income/expense/savings + overlays prev avg + hist avg + best/worst month labels. GREEN: test con 12 meses simulados.
- [x] 1.3 Crear `CategoryItemDto.cs` — amount, %, ranking, YoY change, trend icon, isNew badge. GREEN: test de ranking desc.
- [x] 1.4 Crear `CategoryComparisonTableDto.cs` — tabla Category×Year (prev/selected/next): Δ€, Δ%, trend. RED: test de sort por diff desc.
- [x] 1.5 Crear `TimelineEventDto.cs` — type, label, date, amount, icon. GREEN: test de 4+ eventos cronológicos.
- [x] 1.6 Crear `TopMovementDto.cs` — description, amount, date, category, type (income/expense/frequent), transaction link. RED: test de top 10 expense/income.

### Fase 2: Servicios T2 (RED → GREEN → REFACTOR)

- [x] 2.1 Crear `MultiYearComparisonService.cs` — agrega income/expense/savings/balance por año; computa best/worst/avg. RED: test con 5 años de datos. ✅ 87 tests servicios T2 (2.1-2.6)
- [x] 2.2 Crear `MonthlyEvolutionService.cs` — suma mensual I/E/S; overlays prev avg + hist avg; detección best/worst month. GREEN: test con mes vacío → $0.
- [x] 2.3 Crear `CategoryAnalysisService.cs` — agrupa `ClassifiedRow` por categoría; ranking; YoY; tabla comparativa; "New this year". RED: test de categorías con y sin YoY.
- [x] 2.4 Crear `TimelineService.cs` — detecta 4+ eventos: highest income trx, biggest expense, savings record, first/last trx. GREEN: test con <4 eventos → muestra existentes.
- [x] 2.5 Crear `TopMovementsService.cs` — top 10 expense income + más frecuentes. RED: test con 0 trxs → "No movements".
- [x] 2.6 REFACTOR: todos puros, static, `IReadOnlyList<Transaction>` input. Añadir Sentry Metrics.Distribution.

### Fase 3: Handler Extension T2 (RED → GREEN → REFACTOR)

- [x] 3.1 Extender `GetAnnualDashboardQueryHandler` para orquestar T2 services después de T1. Las transacciones ya están cargadas y particionadas por año. RED: test de handler con T1+T2. ✅ Handler + DTO extendidos
- [x] 3.2 Añadir propiedades T2 a `GetAnnualDashboardResultDto`: `MultiYear`, `MonthlyEvolution`, `Categories`, `CategoryTable`, `Timeline`, `TopExpenses`, `TopIncomes`, `MostFrequent`. GREEN: test de DTO completo T2.
- [x] 3.3 REFACTOR: verificar que T2 no duplica lógica de carga (reusa prevYearTxs ya cargadas); Sentry spans por servicio.

### Fase 4: Frontend — Charts + Secciones T2 (RED → GREEN → REFACTOR)

- [x] 4.1 Añadir `initMultiYearChart()` en `charts.js` — bar chart income/expense/savings/balance multi-year. Highlight año actual. RED: test de inicialización con datos. ✅ 3 chart funcs + 6 secciones HTML + PageModel JSON props
- [x] 4.2 Añadir `initMonthlyEvolutionChart()` en `charts.js` — líneas Jan-Dec income/expense/savings + overlays prev avg + hist avg. GREEN: test con overlays.
- [x] 4.3 Añadir `initCategoryDonutChart()` en `charts.js` — donut/barras: amount, ranking, % total. RED: test de segmentos con YoY.
- [x] 4.4 Añadir sección Multi-Year Comparison (REQ-003) en `Annual.cshtml` — bar chart. Oculto si 1 año (HasMultiYear=false). GREEN: test de visibilidad condicional.
- [x] 4.5 Añadir sección Monthly Evolution (REQ-004) — chart líneas + best/worst month labels. GREEN: test de renderizado.
- [x] 4.6 Añadir sección Category Distribution + Rankings (REQ-005, -006) — donut + tabla ranking con server-side fallback reh. GREEN: test de "No classified".
- [x] 4.7 Añadir sección Category Comparison Table (REQ-007) — Category│Prev│Sel│Next│Δ€│Δ%│Trend. Sort diff desc. GREEN: test de sort.
- [x] 4.8 Añadir sección Timeline (REQ-009) — eventos cronológicos con icono, amount, label. GREEN: test de "No events".
- [x] 4.9 Añadir sección Top Movements (REQ-010) — top 10 expense/income/frequent con tablas server-side. GREEN: test de link funcional.
- [x] 4.10 Actualizar toggle Resumen/Detalle para T2 secciones. GREEN: test de toggle Alpine.js. ✅ Already implemented — T2 sections (multi-year, monthly, categories, timeline, top movements) are rendered outside the detail toggle; only income/expense tables are toggled.

### Fase 5: Tests PR 2

- [x] 5.1 Tests unitarios `MultiYearComparisonService` (5 tests): 2+ años, 1 año, best/worst/avg, año sin siguiente. ✅ 337 App tests + 96 Frontend tests passing
- [x] 5.2 Tests unitarios `MonthlyEvolutionService` (4 tests): 12 meses completos, meses vacíos, overlays, best/worst.
- [x] 5.3 Tests unitarios `CategoryAnalysisService` (6 tests): ranking desc, YoY change, "New this year", sin categorías clasificables.
- [x] 5.4 Tests unitarios `TimelineService` (4 tests): 4+ eventos, <4 eventos, sin transacciones, tipos de evento.
- [x] 5.5 Tests unitarios `TopMovementsService` (4 tests): top 10 expense, top 10 income, frecuentes, 0 trxs.
- [x] 5.6 Extender test de handler T1+T2 (3 tests): datos multi-year → ReturnsMultiYearComparisonData, categorías → ReturnsCategoryDistribution, timeline+top → ReturnsTimelineAndTopMovements. ✅ 340 tests passing
- [x] 5.7 E2E tests (4 tests): multi-year chart visible con ≥2 años, monthly evolution chart, category distribution donut, timeline + top movements sections.
- [x] 5.8 Verificar cobertura: `dotnet test --collect:"XPlat Code Coverage"` — domain ≥80%, application ≥70%. ✅ Evidencia final: Domain 84.57%, Application 85.08%.

---

## PR 3: Advanced Tier (T3) — REQ-008, -013, -014, -015, -016, -017

### Fase 1: DTOs T3 (RED → GREEN → REFACTOR)

- [x] 1.1 Crear `AnomalyDto.cs` — category, month, amount, μ, σ, type (anomaly/extraordinary/exceptional), description. RED: test de construcción.
- [x] 1.2 Crear `DiscoveryDto.cs` — icon, title, description, category. GREEN: test de 3+ discoveries.
- [x] 1.3 Crear `AchievementDto.cs` — id, title, description, icon, unlocked. RED: test de badge rules.
- [x] 1.4 Crear `TrendDto.cs` — category, direction (growing/stable/declining), change%, icon. GREEN: test de clasificación >10%↑ / -10%~10%→ / <-10%↓.
- [x] 1.5 Crear `PredictionDto.cs` — projections I/E/S/balance con confidence (R²), yearsRequired, hasEnoughData. RED: test con ≥2 años.
- [x] 1.6 Crear `HistoricalComparisonDto.cs` — current vs prev/avg/best/worst: abs+% diff para I/E/S/rate/balance. GREEN: test con 3+ años.

### Fase 2: Servicios T3 (RED → GREEN → REFACTOR)

- [x] 2.1 Crear `AnomalyDetectionService.cs` — μ+2σ por categoría = anomalía; >3× media = extraordinario; pico aislado verifica mismo mes año anterior. RED: test de detección estadística.
- [x] 2.2 Extender `InsightsService.cs` con método `GenerateDiscoveries()` — 3+ hallazgos reglados: "% gasto en 2 cats", "Agosto = mayor gasto", "Lunes = mayor gasto". GREEN: test de mínimo 3 hallazgos.
- [x] 2.3 Crear `AchievementsService.cs` — badges: Best year, Savings record, Income record, 3yr streak, Lowest restaurant, Zero-debt year. RED: test de cada badge.
- [x] 2.4 Crear `TrendDetectionService.cs` — YoY category change: growing>10%↑, stable−10~10%→, declining<−10%↓; nuevas → "insufficient". GREEN: test de clasificación.
- [x] 2.5 Crear `PredictionService.cs` — regresión lineal mínimos cuadrados I/E/S/balance + R² confianza. RED: test con 2 años, test con 1 año → "2 years needed".
- [x] 2.6 Crear `HistoricalComparisonService.cs` — current vs prev/avg/best/worst abs+% diff. GREEN: test con 1 año → "Need 2+".
- [x] 2.7 REFACTOR: todos puros. Añadir Sentry Metrics.Distribution por servicio.

### Fase 3: Handler Extension T3 (RED → GREEN → REFACTOR)

- [x] 3.1 Extender `GetAnnualDashboardQueryHandler` para T3 services. RED: test handler completo T1+T2+T3.
- [x] 3.2 Añadir propiedades T3 a `AnnualDashboardDto`: `Anomalies`, `Discoveries`, `Achievements`, `Trends`, `Predictions`, `HistoricalComparison`. GREEN: test DTO completo.
- [x] 3.3 REFACTOR: handler sigue cargando 1 vez las transacciones; T3 reusa datos ya particionados.

### Fase 4: Frontend — Charts + Secciones T3 (RED → GREEN → REFACTOR)

- [x] 4.1 Añadir `initHealthGaugeChart()` en `charts.js` — gauge/radar opcional si T1 no lo incluyó. RED: test de renderizado. ✅ No requerido: health gauge ya estaba cubierto en T1.
- [x] 4.2 Añadir `initAnomalyChart()` en `charts.js` — scatter/bar anomalías por categoría. GREEN: test de visualización.
- [x] 4.3 Añadir sección Anomalías (REQ-008) — lista o tabla de transacciones anómalas/extraordinarias/exceptionales. GREEN: test "No anomalies".
- [x] 4.4 Añadir sección Discoveries (REQ-013) — 3+ tarjetas con hallazgos automáticos. GREEN: test "No discoveries".
- [x] 4.5 Añadir sección Achievements (REQ-014) — badges con iconos. GREEN: test "No achievements".
- [x] 4.6 Añadir sección Trends (REQ-015) — tarjetas growing/stable/declining con iconos color. GREEN: test "insufficient".
- [x] 4.7 Añadir sección Predictions (REQ-016) — proyección I/E/S/balance + nivel confianza. Oculto si <2 años. RED: test de visibilidad condicional.
- [x] 4.8 Añadir sección Historical Comparison (REQ-017) — A vs B/avg/best/worst abs+% diff. GREEN: test "Need 2+".
- [x] 4.9 REFACTOR: verificar que no hay duplicación de `initMultiYearChart` / `initMonthlyEvolutionChart` (ya en T2).

### Fase 5: Tests PR 3

- [x] 5.1 Tests unitarios `AnomalyDetectionService` (5 tests): μ+2σ detección, >3× media, pico aislado, pico repetido (NO anomalía), sin anomalías.
- [x] 5.2 Tests unitarios `InsightsService.GenerateDiscoveries` (4 tests): 3+ discoveries, datos insuficientes, categorías dominantes, patrón día semana.
- [x] 5.3 Tests unitarios `AchievementsService` (6 tests): cada badge individualmente, ningún badge.
- [x] 5.4 Tests unitarios `TrendDetectionService` (4 tests): growing/stable/declining, categorías sin YoY → "insufficient".
- [x] 5.5 Tests unitarios `PredictionService` (4 tests): ≥2 años proyección lineal, 1 año "2 years needed", R² confianza, valores extremos.
- [x] 5.6 Tests unitarios `HistoricalComparisonService` (4 tests): ≥2 años, 1 año "Need 2+", abs diff, % diff.
- [x] 5.7 Extender test de handler T1+T2+T3 (2 tests): flujo completo, año con 1 año (T2/T3 parcial oculto).
- [x] 5.8 E2E tests (4 tests): anomalías visibles, discoveries badges, predictions (≥2 años), historical comparison.
- [x] 5.9 Verificar cobertura: `dotnet test --collect:"XPlat Code Coverage"` — domain ≥80%, application ≥70%. ✅ 2026-06-29 retry correctivo: Domain 84.57% (`test-results/dotnet/859f215d-38f3-4ad7-a195-a8b203a3b6ef/coverage.cobertura.xml`), Application 85.08% (`test-results/dotnet/4a89cea6-43cd-415a-bb6a-e01d80c45acf/coverage.cobertura.xml`).

### Fase 6: Limpieza Final T3

- [x] 6.1 Verificar que `data-testid` legacy (annual-kpi-income, annual-kpi-expense, annual-kpi-net, annual-kpi-fixed-pct, annual-trend-chart, annual-distribution-chart, annual-yoy-section, annual-detail-toggle, annual-income-table, annual-expense-table, annual-empty-state, annual-yoy-no-data) están preservados o mapeados.
- [x] 6.2 Añadir `@media print` básico en `report-print.css` sin lógica de exportación (REQ-019 diferido a backlog).
