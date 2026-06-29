# Diseño: Rediseño Integral del Informe Anual

## Enfoque Técnico

Un solo `GetAnnualDashboardQuery` (IRequest) cuyo handler carga transacciones **UNA VEZ** con filtro server-side por rango de años y delega en 12 sub-servicios puros. Cada servicio es una clase C# estática/singleton sin dependencias de infraestructura — recibe `IReadOnlyList<Transaction>` y devuelve su porción del `AnnualDashboardDto` compuesto. Frontend: Alpine.js + Chart.js + MDBootstrap con datos vía `application/json` blocks (patrón Dashboard existente). Navegación de año vía fetch+swap HTMX.

## Decisiones de Arquitectura

| Decisión | Opción | Alternativas | Justificación |
|----------|--------|-------------|---------------|
| Orquestación | **Handler compuesto** | PageModel orquesta N queries | Evita N+1; PageModel queda thin; testeo unitario del flujo completo |
| DTO | **DTO único anidado** `AnnualDashboardDto` | DTOs separados por sección | Serialización simple a JSON; un solo contrato frontend; facilita cached response |
| Carga transacciones | **1 vez con server-side filter** | Load per-service (N veces) | La actual `FindBySpecificationAsync` filtra in-memory — cargar N años repetiría el problema |
| Registro servicios | **Singleton** (estáticos + `IReadOnlyList<Transaction>` input) | Scoped con DI compleja | Son transformaciones puras sin estado; mismo patrón que `AnnualClassificationEngine` |
| Datos frontend | **JSON blocks** (`<script type="application/json">`) | HTMX partials, fetch API REST | Mismo patrón que Dashboard.cshtml; sin nueva API; swap completo con HTMX |
| Navegación año | **HTMX fetch+swap** (`hx-get`, `hx-target`, `destroyAllCharts`) | Full page reload, Alpine fetch | Mismo patrón que filtros Dashboard; sin recarga; preserva estado Alpine |
| Versión Chart.js | **Chart.js 4.x CDN** (existente) | Chart.js 3.x, ECharts, D3 | Ya está en `_Layout.cshtml`; todas las funciones nuevas extienden `charts.js` |
| Health Score | **6 sub-scores ponderados** (reglas por spec REQ-012) | ML model, heuristic simple | Sin IA; fórmula determinística; sub-scores visibles para transparencia |
| Anomalías | **μ+2σ histórico** + 3× media mensual | IQR, Z-score, MAD | Per spec REQ-008; fórmula estándar; pico aislado verifica mismo mes año anterior |
| Predicción | **Regresión lineal simple** (mínimos cuadrados) | Media móvil, Holt-Winters, ARIMA | Simple, interpretable; solo para ≥2 años; proyección con nivel de confianza |

## Arquitectura de Servicios

```
GetAnnualDashboardQuery
  │
  └── GetAnnualDashboardQueryHandler
        │
        ├── ITransactionRepository.GetByUserIdAndYearRangeAsync(userId, fromYear, toYear)
        │     └── Server-side: WHERE user_id=X AND EXTRACT(YEAR FROM date) BETWEEN Y AND Z
        │
        ├── ISubcategoryRepository.GetByUserIdAsync(userId)
        │     └── (misma llamada existente — necesaria para AnnualClassificationEngine)
        │
        ├── AnnualClassificationEngine.Classify(transactions, subcategoryNames, year)
        │     └── (REUTILIZADO — sin cambios)
        │
        ├── Core Services (T1):
        │   ├── AnnualSummaryService(transactions, year, classifiedRows, subcategoryNames)
        │   ├── FinancialRatiosService(transactions, year)
        │   └── HealthScoreService(summary, ratios, categoryData)
        │
        ├── Multi-Year Services (T2):
        │   ├── MultiYearComparisonService(allYearsSummaries, selectedYear)
        │   ├── MonthlyEvolutionService(transactions, year, allYearsTransactions)
        │   ├── CategoryAnalysisService(classifiedRows, prevYearRows, nextYearRows)
        │   ├── TimelineService(transactions, year)
        │   └── TopMovementsService(transactions, year, topN: 10)
        │
        ├── Advanced Services (T3):
        │   ├── AnomalyDetectionService(transactions, categoryData, allYears)
        │   ├── InsightsService(summary, categories, anomalies, monthlyData, transactions)
        │   ├── AchievementsService(allYearsSummaries)
        │   ├── TrendDetectionService(categoryYoYChanges)
        │   └── PredictionService(yearlySummaries, selectedYear)
        │
        ├── Cross-Cutting:
        │   └── HistoricalComparisonService(currentSummary, allYearsSummaries)
        │
        └── → AnnualDashboardDto (compuesto, serializado a JSON blocks)
```

**Nota**: Los servicios viven en `Features/Analytics/Services/` (no en `Application/Services/` genérico) para mantener cohesión con el feature de analytics existente y evitar colisión con `BankCategoryResolutionService` y otros servicios generales de Application.

## Flujo de Datos

```
PageModel (OnGetAsync)
  → GetAnnualDashboardQuery(year)
  → Handler:
      1. Load transactions: GetByUserIdAndYearRangeAsync(userId, minYear, maxYear) [1 llamada, server-side date filter]
         minYear = min(AvailableYears) o year-5 si no hay histórico
         maxYear = year+1 (siguiente año si existe)
      2. Load subcategories: ISubcategoryRepository.GetByUserIdAsync(userId)
      3. Partition transactions by year in-memory
      4. Run AnnualClassificationEngine for current year (reutilizado)
      5. Delegate to 12 sub-services with partitioned data
  → AnnualDashboardDto
  → PageModel asigna a Model.DashboardData
  → Annual.cshtml renderiza HTML + JSON blocks
  → Alpine.js $nextTick → initCharts() lee JSON blocks → Chart.js
  → Navegación año: HTMX hx-get="/Analysis/Annual?Year=N" swap="#annual-content"
```

## Estructura del DTO Compuesto

```csharp
public record AnnualDashboardDto(
    int Year,
    bool HasData,

    // T1 — Core
    AnnualDashboardSummaryDto Summary,          // REQ-001
    string SmartSummary,                        // REQ-002
    AnnualDashboardRatiosDto Ratios,            // REQ-011
    AnnualDashboardHealthScoreDto HealthScore,  // REQ-012
    IReadOnlyList<int> AvailableYears,          // REQ-018
    bool HasPreviousYear, bool HasNextYear,

    // T2 — Multi-year & Categories
    AnnualDashboardMultiYearDto? MultiYear,     // REQ-003 (null si 1 año)
    AnnualDashboardMonthlyDto MonthlyEvolution, // REQ-004
    IReadOnlyList<CategoryItemDto> Categories,  // REQ-005, 006
    CategoryComparisonTableDto CategoryTable,   // REQ-007
    IReadOnlyList<TimelineEventDto> Timeline,   // REQ-009
    IReadOnlyList<TopMovementDto> TopMovements, // REQ-010

    // T3 — Advanced
    IReadOnlyList<AnomalyDto> Anomalies,        // REQ-008
    IReadOnlyList<DiscoveryDto> Discoveries,    // REQ-013
    IReadOnlyList<AchievementDto> Achievements, // REQ-014
    IReadOnlyList<TrendDto> Trends,             // REQ-015
    PredictionDto? Predictions,                 // REQ-016 (null si <2 años)
    HistoricalComparisonDto? HistoricalComparison // REQ-017
);
```

## Cambios por Capa

| Capa | Archivo | Acción | Descripción |
|------|---------|--------|-------------|
| Domain | `ITransactionRepository.cs` | Modificar | Añadir `GetByUserIdAndYearRangeAsync(UserId, int fromYear, int toYear)` con server-side date filter vía EXTRACT(YEAR) |
| Application | `Queries/GetAnnualDashboardQuery.cs` | Crear | `IRequest<AnnualDashboardDto>` |
| Application | `Queries/GetAnnualDashboardQueryHandler.cs` | Crear | Handler compuesto que orquesta servicios |
| Application | `Features/Analytics/Services/*.cs` (×12) | Crear | Servicios puros por funcionalidad (no en `Application/Services/` genérico) |
| Application | `DTOs/AnnualDashboard*.cs` (×14) | Crear | DTOs del nuevo dashboard |
| Application | `DependencyInjection.cs` | Modificar | Registrar nuevos servicios + handler |
| Infra | `SupabaseTransactionRepository.cs` | Modificar | Implementar `GetByUserIdAndDateRangeAsync` con server-side date filter |
| Frontend | `Pages/Analysis/Annual.cshtml` | Reescribir | Layout revista ejecutiva, hero, scroll narrative, collapsible sections |
| Frontend | `Pages/Analysis/Annual.cshtml.cs` | Reescribir | PageModel ligero que llama `GetAnnualDashboardQuery` |
| Frontend | `wwwroot/js/charts.js` | Modificar | +5 funciones init (multi-year bar, category donut/bar, timeline, health gauge/radar) |
| Frontend | `wwwroot/css/report-print.css` | Crear | `@media print` para PDF export (REQ-019) |
| Analysis | `Classification/AnnualClassificationEngine.cs` | **No tocar** | Se reutiliza tal cual |
| Analysis | `DTOs/AnnualAnalysis*.cs` | **No tocar** | Se mantienen para compatibilidad |

## Estrategia de Cálculo (REQ → Implementación)

| REQ | Servicio | Cómo |
|-----|----------|------|
| 001 | AnnualSummaryService | Clasifica via `AnnualClassificationEngine`; computa income/expense/net/savings/rate; YoY absoluto y %; rank entre todos años del usuario |
| 002 | InsightsService (narrative) | 2-4 frases regladas: "Tus ingresos subieron X%", "Tus 2 mayores categorías = Y% del gasto", hitos de ahorro |
| 003 | MultiYearComparisonService | Agrega income/expense/savings/balance por año; highlight año actual; comp con prev, next, avg, best, worst |
| 004 | MonthlyEvolutionService | Suma income/expense/savings por mes (12 arrays); overlay media prev años y media histórica; best/worst month label |
| 005 | CategoryAnalysisService | Agrupa classifiedRows por categoría; ranking desc; % del total; YoY change; badge "New this year". Incluye rankings (top expense/income, biggest increase/decrease) |
| 006 | CategoryAnalysisService (mismo que 005) | Top expense/income categories por total; biggest increase/decrease (€, %); highest absolute |
| 007 | CategoryAnalysisService (mismo que 005) | Tabla Category×Year (prev/selected/next) con Δ€, Δ%, trend icon |
| 008 | AnomalyDetectionService | Por categoría: μ + 2σ mensual = anomalía > umbral; >3× media = extraordinario; verifica mismo mes año anterior |
| 009 | TimelineService | 4+ eventos cronológicos: highest income trx, biggest expense, savings record, first/last trx del año |
| 010 | TopMovementsService | Top expense/income por importe (top 10) + más frecuentes. Click → navega a transacción |
| 011 | FinancialRatiosService | Savings rate, avg monthly, avg daily, avg per-trx, trx count, avg ops/month. Div/0 → "—" |
| 012 | HealthScoreService | Savings 25% | IncomeStab 15% | ExpenseStab 15% | CatDep 10% | Balance 20% | Trend 15%. Fórmulas por spec |
| 013 | InsightsService (mismo que 002) | 3+ hallazgos: top categories %, best month, weekday pattern, months reducing |
| 014 | AchievementsService | Badge rules: best year net, savings record, income record, 3yr streak, lowest restaurant, zero-debt |
| 015 | TrendDetectionService | YoY category change: >10%↑ growing, -10%~10% stable, <-10% declining. Insufficient data para nuevas |
| 016 | PredictionService | Regresión lineal (mínimos cuadrados) sobre I/E/S/balance anual. Confianza basada en R². ≥2 años requerido |
| 017 | HistoricalComparisonService | Año actual vs anterior, vs media histórica, vs best, vs worst: abs + % diff |
| 018 | (Frontend — HTMX) | HTMX fetch+swap con `hx-get`; ◀ disabled en primer año, ▶ disabled si no hay año siguiente. No requiere servicio backend separado |
| 019 | (NTH — diferido a backlog) | `@media print` + Chart.js `toBase64Image()`. No se implementa en este cambio |

## Estrategia de Slicing (T1/T2/T3)

| Tier | Funcionalidades | Servicios | Límite cambio |
|------|----------------|-----------|---------------|
| **T1 (Core)** | REQ-001, -002, -011, -012, -018. Resumen ejecutivo, smart summary, ratios, health score, year nav | Handler + AnnualSummaryService + FinancialRatiosService + HealthScoreService + InsightsService + IRepository (nuevo método) + DTOs base + Annual.cshtml (hero) + PageModel | ~400 líneas |
| **T2 (Multi-Year)** | REQ-003, -004, -005, -006, -007, -009, -010. Multi-year chart, monthly evolution, categorías, timeline, top movements | MultiYearComparisonService + MonthlyEvolutionService + CategoryAnalysisService + TimelineService + TopMovementsService + DTOs + charts.js (+3 funciones) + secciones Annual.cshtml | ~400 líneas |
| **T3 (Advanced)** | REQ-008, -013, -014, -015, -016, -017. Anomalías, descubrimientos, logros, tendencias, predicciones, hist. comparison | AnomalyDetectionService + AchievementsService + TrendDetectionService + PredictionService + HistoricalComparisonService + DTOs + secciones Annual.cshtml + charts.js (+2 funciones) | ~350 líneas |
| **Backlog** | REQ-019 — Export PDF | Se difiere. No se implementa en este cambio | — |

## Estrategia de Tests

| Capa | Coverage | Enfoque |
|------|----------|---------|
| Unit (Domain) | 80% | Tests de servicios puros con transacciones fabricadas (sin mocks) |
| Unit (Application) | 70% | Handler con mock de repositorio; verifica composición correcta del DTO |
| Integration | — | Opcional: handler real + Supabase local; verifica server-side filter |
| E2E (Playwright) | Flujo completo | Navegación año ◀▶, toggle Resumen/Detalle, KPIs visibles, health score renderizado |

## Decisiones Tomadas (Preguntas Abiertas Resueltas)

- [x] **Estrategia de carga**: 1 llamada server-side con rango amplio `GetByUserIdAndYearRangeAsync(userId, minYear, maxYear)`. Las transacciones se particionan en memoria por año. minYear = min(años disponibles del usuario) o year-5. maxYear = year+1.
- [x] **Top movements**: Top 10 fijo (ni 5 ni 20). Suficiente para identificar patrones sin saturar UI. Configurable en código vía constante, no por parámetro de request.
- [x] **Health score sub-scores**: Visibles siempre en tarjetas compactas (no solo en detalle), tal como especifica el spec.
- [x] **Timeline mínimo eventos**: Si hay menos de 4 eventos, se muestran los que existan. No hay mínimo artificial.
- [x] **Export PDF (REQ-019)**: Diferido a backlog. No se implementa en este cambio. Se añade `@media print` básico para que el navegador pueda imprimir, sin jsPDF ni lógica de exportación.
