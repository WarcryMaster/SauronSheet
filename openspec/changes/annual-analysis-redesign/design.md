# Design: Rediseño del Dashboard de Análisis Anual

## Technical Approach

Rediseño puramente de frontend sobre la página `/Analysis/Annual`. No se modifica backend — los DTOs existentes (`AnnualAnalysisResultDto`, `AnnualAnalysisRowDto`, `AnnualAnalysisSummaryDto`, `YearOverYearVariationDto`) contienen todos los datos necesarios. La vista se reorganiza como dashboard con KPIs → gráficos → YoY → tablas colapsables con expansión de fila. Los datos para gráficos se precalculan como propiedades computadas en el PageModel y se serializan a JSON en bloques `<script type="application/json">`. Chart.js se inicializa desde `wwwroot/js/charts.js` mediante Alpine.js `$nextTick`. Las nuevas funciones de Chart.js reciben el elemento canvas como referencia directa y el objeto de datos ya parseado, consistente con la integración vía `$refs` de Alpine.js.

## Architecture Decisions

| Decisión | Opción elegida | Alternativa rechazada | Justificación |
|----------|---------------|----------------------|---------------|
| Preparación de datos de gráficos | PageModel properties + JSON serialization | Inline Razor aggregation | Separación de concerns; testable en Frontend tests; sigue patrón existente |
| Tipo de gráfico de distribución | Donut (4 segmentos: IncomeFixed/Variable + ExpenseFixed/Variable) | Stacked bar | Donut comunica proporciones con una sola mirada; más compacto; Chart.js soporte nativo |
| JSON data blocks vs inline data | Bloques `<script type="application/json">` en `.cshtml` | Pasar datos como argumentos JS | Patrón consolidado en la app; el parseo ocurre en Alpine.js y el objeto se pasa a init functions |
| Toggle de tablas | Un botón toggle para ambas tablas simultáneamente | Toggle independiente por tabla | Reduce ruido visual; el usuario que quiere detalle quiere ver ambos |
| Expansión mensual por fila | Alpine.js `expanded` por fila + mini-barras CSS | Columnas siempre visibles o modal | REQ-ANNUAL-040 exige expansión de fila; CSS bars evitan dependencia de Chart.js por fila; ligero |
| YoY section | Fila de compact cards con `Variation.*Pct` del DTO | Tabla YoY separada | Alineado con REQ-ANNUAL-030; cards con border-left de color comunican dirección instantáneamente |
| Chart.js init signatures | `initAnnualTrendChart(canvas, data)` — canvas es elemento, data es objeto parseado | `(canvasId, jsonDataId)` strings | Consistente con Alpine.js `$refs` que devuelve referencias directas a elementos; evita `getElementById` redundante; el parseo de JSON ocurre en Alpine.js, no dentro de la función |

## Data Flow

```
AnnualAnalysisHandler (backend, sin cambios)
  → AnnualAnalysisResultDto (Result)
  → PageModel (Annual.cshtml.cs)
      ├── IncomeRows, ExpenseRows → tablas colapsables con expansión de fila
      ├── Summary.* → KPIs
      ├── MonthlyIncomeTotals[12], MonthlyExpenseTotals[12] → trend chart JSON
      ├── ChartDataJson (labels + income/expense arrays) → <script id="annual-chart-data">
      ├── FixedVariableChartJson (donut 4 segments) → <script id="annual-distribution-data">
      ├── Variation.* → YoY compact cards
      └── FixedCostPercentage → KPI card
  → Annual.cshtml
      ├── <script id="annual-chart-data" type="application/json"> → Alpine parse → initAnnualTrendChart(canvas, data)
      ├── <script id="annual-distribution-data" type="application/json"> → Alpine parse → initAnnualDistributionChart(canvas, data)
      └── Alpine x-data → orchestration (toggle tables, row expansion, counters, YoY)
```

## File Changes

| Archivo | Acción | Descripción |
|---------|--------|-------------|
| `src/.../Pages/Analysis/Annual.cshtml` | REWRITE | Dashboard layout: KPIs + charts + YoY cards + collapsible tables with row expansion |
| `src/.../Pages/Analysis/Annual.cshtml.cs` | MODIFY | Añadir `MonthlyIncomeTotals`, `MonthlyExpenseTotals`, `ChartDataJson`, `FixedVariableChartJson`, `FixedCostPercentage` |
| `src/.../wwwroot/js/charts.js` | MODIFY | Añadir `initAnnualTrendChart(canvas, data)` y `initAnnualDistributionChart(canvas, data)` |
| `e2e/tests/07-annual-analysis.spec.ts` | REWRITE | Adaptar selectores `data-testid` al nuevo layout (ver sección E2E testids más abajo) |

## PageModel Additions

Propiedades calculadas nuevas en `AnnualModel` (`Annual.cshtml.cs`):

```csharp
// Monthly aggregates from all rows for the trend chart (12 entries, index 0 = January)
public decimal[] MonthlyIncomeTotals =>
    Result?.Rows.Any() == true
        ? Enumerable.Range(0, 12).Select(m => Result.Rows
            .Where(r => r.IsIncome)
            .Sum(r => r.MonthlyAmounts[m])).ToArray()
        : new decimal[12];

public decimal[] MonthlyExpenseTotals =>
    Result?.Rows.Any() == true
        ? Enumerable.Range(0, 12).Select(m => Result.Rows
            .Where(r => !r.IsIncome)
            .Sum(r => r.MonthlyAmounts[m])).ToArray()
        : new decimal[12];

// Monthly trend line chart data as JSON (consumed by initAnnualTrendChart)
public string ChartDataJson => HasData
    ? JsonSerializer.Serialize(new {
        labels = new[] { "Ene", "Feb", "Mar", "Abr", "May", "Jun",
                         "Jul", "Ago", "Sep", "Oct", "Nov", "Dic" },
        income = MonthlyIncomeTotals,
        expense = MonthlyExpenseTotals
    })
    : "{}";

// Donut chart JSON (4 segments: incomeFixed, incomeVar, expenseFixed, expenseVar)
public string FixedVariableChartJson => HasData
    ? JsonSerializer.Serialize(new {
        labels = new[] { "Ingresos Fijos", "Ingresos Variables",
                         "Gastos Fijos", "Gastos Variables" },
        values = new[] {
            Result!.Summary.IncomeFixed,
            Result.Summary.IncomeVariable,
            Result.Summary.ExpenseFixed,
            Result.Summary.ExpenseVariable
        },
        colors = new[] { "#14a44d", "#556B2F", "#dc4c64", "#e4a11b" }
    })
    : "{}";

// Fixed cost percentage with zero guard
public decimal FixedCostPercentage =>
    Result?.Summary.ExpenseTotal > 0
        ? Math.Round(Result.Summary.ExpenseFixed / Result.Summary.ExpenseTotal * 100, 1)
        : 0;
```

## Chart.js Init Functions

**Signature follows existing pattern** — first param is the canvas element (from Alpine.js `$refs`), second param is already-parsed data object:

```javascript
/**
 * Initialize the annual trend line chart (income vs expense over 12 months).
 * @param {HTMLCanvasElement} canvas — canvas element reference (from Alpine $refs)
 * @param {{labels: string[], income: number[], expense: number[]}} data — parsed JSON
 */
function initAnnualTrendChart(canvas, data) {
    if (!canvas || !data || !data.labels) return;
    const existing = Chart.getChart(canvas);
    if (existing) existing.destroy();

    const ctx = canvas.getContext('2d');
    new Chart(ctx, {
        type: 'line',
        data: {
            labels: data.labels,
            datasets: [
                { label: 'Ingresos', data: data.income, borderColor: tokens.success,
                  backgroundColor: hexToRgba(tokens.success, 0.1), tension: 0.3,
                  fill: true, pointRadius: 3, borderWidth: 2 },
                { label: 'Gastos', data: data.expense, borderColor: tokens.danger,
                  backgroundColor: hexToRgba(tokens.danger, 0.1), tension: 0.3,
                  fill: true, pointRadius: 3, borderWidth: 2 }
            ]
        },
        options: { ...chartDefaults, interaction: { mode: 'index', intersect: false } }
    });
}

/**
 * Initialize the donut chart for fixed/variable distribution.
 * @param {HTMLCanvasElement} canvas — canvas element reference (from Alpine $refs)
 * @param {{labels: string[], values: number[], colors: string[]}} data — parsed JSON
 */
function initAnnualDistributionChart(canvas, data) {
    if (!canvas || !data || !data.values) return;
    const existing = Chart.getChart(canvas);
    if (existing) existing.destroy();

    const ctx = canvas.getContext('2d');
    new Chart(ctx, {
        type: 'doughnut',
        data: {
            labels: data.labels,
            datasets: [{
                data: data.values,
                backgroundColor: data.colors,
                borderColor: '#fff',
                borderWidth: 2
            }]
        },
        options: {
            responsive: true, maintainAspectRatio: false,
            plugins: {
                legend: { position: 'bottom', labels: { usePointStyle: true, padding: 16 } },
                tooltip: { callbacks: { label: ctx => `${ctx.label}: €${ctx.parsed.toFixed(2)}` } }
            }
        }
    });
}
```

### Invocación desde Alpine.js

```html
<div x-data="{
    trendData: JSON.parse(document.getElementById('annual-chart-data')?.textContent || '{}'),
    distData: JSON.parse(document.getElementById('annual-distribution-data')?.textContent || '{}')
}" x-init="
    $nextTick(() => {
        initAnnualTrendChart($refs.trendCanvas, trendData);
        initAnnualDistributionChart($refs.distCanvas, distData);
    });
">
    <canvas x-ref="trendCanvas" aria-label="Gráfico de tendencia mensual: ingresos y gastos por mes" role="img"></canvas>
    <canvas x-ref="distCanvas" aria-label="Gráfico de distribución: ingresos y gastos fijos vs variables" role="img"></canvas>
</div>
```

## CSS / Layout

- KPI cards: `row row-cols-1 row-cols-md-2 row-cols-xl-4 g-3` + `card border-0 shadow-sm` con `border-start border-4` para acento de color
- Chart containers: `card border-0 shadow-sm p-3` con `height: 380px` fijo para altura consistente; cada `<canvas>` tiene `role="img"` y `aria-label` descriptivo
- Animated counters: Alpine.js `x-text` con transición CSS
- YoY compact cards: `row row-cols-2 row-cols-md-3 row-cols-lg-5 g-2`; cada card con `border-start border-3` (verde=mejora, rojo=empeora, gris=sin cambio)
- Mini-barras en expansión de fila: `style="width: X%"` calculado como `(monthAmount / rowMax) * 100`

## YoY Comparison Section (REQ-ANNUAL-030)

Sección de comparativa interanual ubicada entre los gráficos y las tablas colapsables. Lee directamente de `Model.Result?.Summary.Variation` (tipo `YearOverYearVariationDto`).

### Layout y métricas

Fila de 5 compact cards con las métricas del DTO:

| Card | Campo DTO | Badge color logic |
|------|-----------|-------------------|
| Income Fixed | `Variation.IncomeFixedPct` | ↑ verde, ↓ rojo |
| Income Variable | `Variation.IncomeVariablePct` | ↑ verde, ↓ rojo |
| Expense Fixed | `Variation.ExpenseFixedPct` | ↑ rojo, ↓ verde |
| Expense Variable | `Variation.ExpenseVariablePct` | ↑ rojo, ↓ verde |
| Net | `Variation.NetPct` | ↑ verde, ↓ rojo |

Cada card muestra: nombre de la métrica, valor actual (del `Summary`), valor año anterior (calculado si `HasPreviousYearData`), delta %, y flecha direccional.

### Alpine.js model (en `x-data`)

```javascript
variation: @Json.Serialize(Model.Result?.Summary.Variation),
hasVariation: @Json.Serialize(Model.Result?.Summary.Variation?.HasPreviousYearData == true)
```

### Sin datos del año anterior

Cuando `!hasVariation`, la fila YoY se reemplaza por un mensaje centrado: "Sin datos del año anterior" dentro de un `card border-0 shadow-sm p-3 text-center text-muted`.

### Reutilización de helpers existentes

Se reutilizan `GetVariationBadgeClass()`, `GetVariationArrow()`, y `FormatVariationPct()` ya definidos en `AnnualModel`. La lógica `isIncomeOrNet` ya existe y distingue correctamente la dirección semántica de ingresos vs gastos.

## Row-Level Expansion Design (REQ-ANNUAL-040)

Cada fila de las tablas colapsables es expandible individualmente para mostrar los 12 valores mensuales como mini-barras inline.

### Alpine.js model por fila

En lugar de iterar con `@foreach` de Razor, las filas usan `template x-for` con un modelo Alpine.js que incluye el flag `expanded`:

```javascript
// En el x-data del componente tabla:
incomeRows: @Json.Serialize(Model.IncomeRows.Select(r => new {
    movement = r.Movement,
    typeLabel = r.TypeLabel,
    average = r.Average,
    monthlyAmounts = r.MonthlyAmounts,
    annualMax = r.MonthlyAmounts.Max(),
    expanded = false
}))
```

### HTML structure

```html
<template x-for="row in incomeRows" :key="row.movement">
    <tbody>
        <!-- Main row: click toggles expansion -->
        <tr role="button" tabindex="0" @@click="row.expanded = !row.expanded"
            @@keydown.enter="row.expanded = !row.expanded"
            @@keydown.space.prevent="row.expanded = !row.expanded"
            class="cursor-pointer" :aria-expanded="row.expanded">
            <td><span x-text="row.expanded ? '▼' : '▶'" class="me-2"></span><span x-text="row.movement"></span></td>
            <td><span class="badge" :class="row.typeLabel.includes('Fijo') ? 'bg-success' : 'bg-warning'" x-text="row.typeLabel"></span></td>
            <td class="text-end" x-text="'€' + row.average.toFixed(2)"></td>
            <td class="text-end fw-bold" x-text="'€' + row.monthlyAmounts.reduce((a,b) => a+b, 0).toFixed(2)"></td>
        </tr>
        <!-- Expansion row: 12 mini-bars -->
        <tr x-show="row.expanded" x-transition>
            <td colspan="4" class="p-3 bg-light">
                <div class="d-flex gap-1 align-items-end" style="height: 48px;">
                    <template x-for="(amt, mIdx) in row.monthlyAmounts" :key="mIdx">
                        <div class="d-flex flex-column align-items-center" style="flex:1; min-width:0;">
                            <small class="text-muted" style="font-size:9px;"
                                   x-text="'€' + amt.toFixed(0)"></small>
                            <div class="rounded-1"
                                 :style="'width:100%; height:' + (row.annualMax > 0 ? (amt/row.annualMax * 100) : 0) + '%; background:' + (row.typeLabel.includes('Fijo') ? tokens.success : tokens.info)">
                            </div>
                            <small class="text-muted mt-1" style="font-size:9px;"
                                   x-text="['E','F','M','A','M','J','J','A','S','O','N','D'][mIdx]"></small>
                        </div>
                    </template>
                </div>
            </td>
        </tr>
    </tbody>
</template>
```

### Columnas visibles de la tabla principal

La tabla principal muestra solo 4 columnas (no 15): toggle, Movimiento, Tipo, Media mensual, Total anual. Las 12 columnas mensuales existen SOLO en la fila expandida.

## E2E Test IDs — Migration Plan

### testids ELIMINADOS (la vista antigua ya no existe)

| testid antiguo | Motivo |
|----------------|--------|
| `annual-income-section` | Reemplazado por layout de dashboard unificado |
| `annual-expense-section` | Reemplazado por layout de dashboard unificado |
| `income-fixed-card` | Reemplazado por `annual-kpi-income` (KPI agregado) |
| `income-variable-card` | Consolidado en KPI único |
| `income-total-card` | Consolidado en KPI único |
| `expense-fixed-card` | Consolidado en KPI único |
| `expense-variable-card` | Consolidado en KPI único |
| `expense-total-card` | Consolidado en KPI único |
| `annual-neto-card` | Reemplazado por `annual-kpi-net` |
| `yoy-badge-*` (6 badges) | Reemplazados por sección YoY dedicada |
| `annual-months-card` | Eliminado; no forma parte del nuevo diseño |

### testids NUEVOS

| testid nuevo | Elemento |
|-------------|---------|
| `annual-kpi-income` | KPI card: Ingresos Total |
| `annual-kpi-expense` | KPI card: Gastos Total |
| `annual-kpi-net` | KPI card: Neto |
| `annual-kpi-fixed-pct` | KPI card: % Coste Fijo |
| `annual-trend-chart` | Canvas del gráfico de tendencia |
| `annual-distribution-chart` | Canvas del gráfico de distribución |
| `annual-yoy-section` | Contenedor de la sección YoY |
| `annual-yoy-no-data` | Mensaje "Sin datos del año anterior" |
| `annual-detail-toggle` | Botón toggle para mostrar/ocultar tablas |
| `annual-income-table` | Tabla de ingresos colapsable (CONSERVADO) |
| `annual-expense-table` | Tabla de gastos colapsable (CONSERVADO) |
| `annual-empty-state` | Estado vacío (CONSERVADO) |

El spec E2E (`e2e/tests/07-annual-analysis.spec.ts`) debe reescribirse completamente para reflejar estos nuevos testids. Los únicos testids que sobreviven son `annual-income-table`, `annual-expense-table`, y `annual-empty-state`.

## Chart Accessibility (REQ-ANNUAL-090)

Cada `<canvas>` debe incluir:

```html
<canvas x-ref="trendCanvas"
        role="img"
        aria-label="Gráfico de tendencia mensual: ingresos y gastos por mes, de enero a diciembre">
</canvas>
<!-- Fallback table for screen readers (visually hidden) -->
<table class="visually-hidden" aria-hidden="true">
    <caption>Tendencia mensual (tabla de respaldo)</caption>
    <thead><tr><th>Mes</th><th>Ingresos</th><th>Gastos</th></tr></thead>
    <tbody>
        @for (int m = 0; m < 12; m++)
        {
            <tr>
                <td>@(new[]{"Ene","Feb","Mar","Abr","May","Jun","Jul","Ago","Sep","Oct","Nov","Dic"}[m])</td>
                <td>€@Model.MonthlyIncomeTotals[m].ToString("N2", CultureInfo.InvariantCulture)</td>
                <td>€@Model.MonthlyExpenseTotals[m].ToString("N2", CultureInfo.InvariantCulture)</td>
            </tr>
        }
    </tbody>
</table>
```

Mismo patrón para el gráfico de distribución (donut), con una tabla fallback de 4 filas (IncomeFixed, IncomeVariable, ExpenseFixed, ExpenseVariable).

## Testing Strategy

| Capa | Qué probar | Enfoque |
|------|-----------|---------|
| Unit — Frontend | `AnnualModel`: `MonthlyIncomeTotals`, `MonthlyExpenseTotals`, `ChartDataJson`, `FixedVariableChartJson`, `FixedCostPercentage` | xUnit + Moq con `AnnualAnalysisResultDto` simulado |
| Unit — Frontend | Lógica de color de badge YoY: `GetVariationBadgeClass`, `GetVariationArrow` | xUnit parametrizado con todos los casos (↑/↓/null/zero) |
| Unit — Frontend | `ChartDataJson` serializa correctamente con datos y sin datos | xUnit: assert `"{}"` cuando `!HasData`; assert estructura cuando `HasData` |
| Integration | `initAnnualTrendChart`: recibe JSON válido → renderiza sin errores | Test JS con datos de fixtures |
| Integration | `initAnnualDistributionChart`: recibe JSON válido → donut renderiza 4 segmentos | Test JS con datos de fixtures |
| E2E | KPIs visibles, gráficos renderizados, toggle tablas, row expansion, YoY section, data-testid | Playwright — spec completamente reescrita |

## Migration / Rollout

No migration required. Rollback: revertir `Annual.cshtml` + `Annual.cshtml.cs` a versión anterior. Las funciones nuevas en `charts.js` son inertes sin invocación.

## Open Questions

- [x] ¿Debe el `dashboard-analytics-filter` afectar a los gráficos del Annual? → **No** — Confirmado: son páginas independientes.
- [ ] ¿Deben las tablas usar `template x-for` de Alpine.js para las filas en lugar de `@foreach` de Razor? → Esto es necesario para el modelo de expansión por fila (cada fila necesita su propio `expanded` state). Implica que las filas se serializan como JSON al `x-data` de Alpine.js.
