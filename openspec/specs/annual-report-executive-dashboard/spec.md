# annual-report-executive-dashboard

## Propósito

Dashboard ejecutivo anual tipo revista (17 bloques). Reemplaza `annual-analysis-dashboard`. Sin IA. Vistas toggleables Resumen/Detalle (Alpine.js). Nav: ◀Año▶+desplegable.

## Requisitos

| ID | Nombre | DEBE | Escenarios (Given→When→Then) |
|----|--------|------|------------------------------|
| 001 | Executive Summary | Hero full-viewport: year, balance, income, expenses, savings, savings rate, YoY (abs+%), avg comparison, year rank. Cada métrica: value, diff, %, arrow | año+anterior existen→badges; sin anterior→badge oculto, rank "1st"; sin siguiente→▶disabled |
| 002 | Smart Summary | Texto reglado culture-aware: income change, category changes, savings milestone. DEBE producir texto en CurrentUICulture con autoría manual ES/EN. Sin IA | datos suficientes→2-4 frases (localizadas); year vacío→mensaje desde .resx |
| 003 | Multi-Year | Chart income/expense/savings/balance por año. Destaca año. Compara: prev, next, avg, best, worst | ≥2 años→chart; 1 año→"Single year" |
| 004 | Monthly Evolution | Chart líneas Jan-Dec income/expense/savings. Overlay: prev avg, hist avg. Best/worst month | 12 meses→líneas+overlays; meses vacíos→$0 |
| 005 | Category Distribution | Donut/barras: amount, %, ranking, YoY change, trend | categorías con YoY→segmentos; nueva→"New this year" |
| 006 | Category Rankings | Top expense/income. Biggest increase €, biggest decrease €, highest absolute, highest % | ≥1 categoría→ranking; 0 clasificables→"No classified" |
| 007 | Comp. Table | Category\|Prev\|Sel\|Next\|Δ€\|Δ%\|Trend. Sort diff desc | multi-year→tabla; sin next→"—" |
| 008 | Anomalías | >μ+2σ hist=anomalía. >3×media=extraordinario. Pico aislado=exceptional si no repite mismo mes año anterior | extremos→lista; sin anomalías→"No anomalies"; pico repetido→NO |
| 009 | Timeline | Eventos cronológicos: highest income, biggest expense, savings record. Icono por tipo | datos→4+ eventos; vacío→"No events" |
| 010 | Top Movements | Top 5-20 expenses/income/frequent. Click→transacción | trxs→links; 0 trxs→"No movements" |
| 011 | Ratios | Savings rate, avg monthly I/E/S, avg daily expense, avg per-trx E/I, trx count, avg ops/month | datos→ratios; div/0→"—" |
| 012 | Health Score | Sub-scores: Savings25% min(rate/0.2×100,100), IncomeStab15% 100−min(CV×100,100), ExpenseStab15% igual, CatDep10% 100−top3Share, Balance20% min(I/E×50,100), Trend15% (3+ inc→100, dec→0, interp). Total ponderado. Sub-scores visibles | ≥1 año→score; 0 trxs→"—" |
| 013 | Discoveries | "56% gasto=2 cats", "August=highest", "Mondays=highest", "8 meses reduciendo". Mín 3 | multi-mes→3+; insuficiente→"No discoveries" |
| 014 | Achievements | Best year, Savings record, Income record, 3yr inc savings, Lowest restaurant, Zero-debt year | récord→badge; sin→"No achievements" |
| 015 | Trends | Growing>10%↑, Stable−10~10%→, Declining<−10%↓; sin YoY→"insufficient" | categorías con YoY→clasificadas; nuevas→todas "insufficient" |
| 016 | Predictions | ≥2 años→proyección lineal I/E/S/balance+confianza; <2→"2 years needed" | ≥2→proyecciones; 1 año→mensaje |
| 017 | Hist. Comp. | A vs B, vs avg, vs best, vs worst: I/E/S/rate/balance abs+%diff | ≥2 años→métricas; 1 año→"Need 2+" |
| 018 | Year Nav | ◀Año▶+desplegable. Fetch+swap sin recarga. Toggle Resumen/Detalle. Skeleton `x-show="loading"` | clic◀→fetch; ◀en primero→disabled; toggle→sin recarga |
| 019 | Export NTH | PDF `@media print`, image Chart.js toBase64Image() | Detalle+click→descarga |
| LOC-001 | Trazas Sentry en inglés | Cualquier breadcrumb/mensaje Sentry emitido por `InsightsService` o bloques del dashboard DEBE permanecer en inglés, independientemente de la cultura UI activa. | cultura `es`→Sentry breadcrumb en inglés |

> **Localización aplicada (bloques 002–017)**: Los textos de UI de los bloques 002–017 se resuelven vía recursos de localización (`.resx`/`IViewLocalizer`/`window.__i18n`) según la cultura activa. Quedan prohibidos los literales hardcodeados en español o inglés dentro de `.cshtml`/JS del scope. Ver REQ-EXEC-LOC-001 para trazabilidad Sentry.

## Estados

| Estado | Comportamiento |
|--------|---------------|
| Carga | Skeleton loader. Sin parciales |
| Vacío año | `data-testid="annual-empty-state"` mensaje localizado desde `.resx` según cultura activa |
| Vacío componente | Mensajes localizados desde `.resx`/`window.__i18n` según cultura activa (no literales hardcodeados) |
| Error API | Toast+retry+Sentry breadcrumb (Sentry en inglés) |
| 1 año datos | Multi-year oculto. Predictions ocultas. Rank "1st" |
| 0 trxs clasificables | Mensaje localizado desde `.resx` según cultura activa |

## Accesibilidad (carry-forward REQ-ANNUAL-090)

Gráficos `aria-label`. Color+flecha+texto. Keyboard Enter/Space. `data-testid`: `annual-income-table`, `annual-expense-table`, `annual-empty-state`.

## Layout (carry-forward REQ-ANNUAL-080)

| Viewport | Hero | Charts | Tables |
|----------|------|--------|--------|
| ≥992px | Full | Full | Full |
| ≥576px | Compact | Stacked | H-scroll |
| <576px | Minimal | 1-col | H-scroll |
