# Roadmap: Dashboard Features — Backlog

Documento de brainstorm y backlog de features posibles para la página `/dashboard` de SauronSheet.
Cada item está priorizado y con estimación de esfuerzo para facilitar la planificación de sprints.

---

## Estado actual del Dashboard

| Sección | Descripción |
|---------|-------------|
| Date Range Filter | Filtros: all, last-month, this-month, this-year, custom, last-3-months |
| KPI Cards | Total Income, Total Expenses, Net Amount, Transactions count |
| Spending by Category | Gráfica stacked bar por categoría/mes (Chart.js) |
| Monthly Trends | Gráfica de líneas con tendencias mensuales |
| Year over Year Comparison | Comparativa interanual de gastos |
| Recent Transactions | Tabla con últimas 10 transacciones |
| Budget Status | Widget con progress bars de presupuestos activos |

### Módulos existentes en la app

- Transactions: CRUD + Upload (PDF/Excel) + Search + Filtros
- Budgets: CRUD + Comparison + Dashboard summary
- Categories: CRUD + Subcategorías + Resolución automática desde PDFs
- Auth: Supabase Auth + JWT cookies

---

## 🔴 Prioridad ALTA — Quick wins de alto impacto

### F1: Top 5 Gastos del Mes

| Campo | Detalle |
|-------|---------|
| **Descripción** | Lista de las 5 transacciones de gasto más caras del período seleccionado |
| **Por qué** | Mucho más útil que "10 transacciones recientes" para identificar gastos grandes de un vistazo |
| **Esfuerzo** | Bajo — 1 query nuevo + sección UI simple |
| **Dependencias** | Ninguna — usa datos existentes |
| **Requiere** | Query `GetTopExpensesQuery(year, month, limit)` ordenado por `Amount ASC` (negativos = gastos) |
| **UI sugerida** | Card con lista vertical, cada ítem: fecha, descripción, monto, categoría. Alternar con "Recent Transactions" o mostrar ambos. |

---

### F2: Savings Rate Widget

| Campo | Detalle |
|-------|---------|
| **Descripción** | Gauge o donut que muestra `(Income - Expenses) / Income * 100` |
| **Por qué** | La métrica más importante de salud financiera. El usuario ve de un vistazo si está ahorrando o no. |
| **Esfuerzo** | Bajo — ya tenemos `TotalIncome` y `TotalExpenses` en `TransactionSummaryDto` |
| **Dependencias** | Ninguna |
| **Requiere** | Cálculo en el PageModel, nuevo componente Chart.js (gauge o doughnut) |
| **UI sugerida** | Donut chart con % en el centro. Benchmark: 20% recommended. Color: verde si ≥20%, amarillo 10-20%, rojo <10%. |

---

### F3: Budget Burn Rate

| Campo | Detalle |
|-------|---------|
| **Descripción** | En el widget de Budget Status, mostrar si el ritmo de gasto es coherente con el presupuesto restante |
| **Por qué** | El widget actual solo muestra "X de Y on track". No dice si vas a llegar a fin de mes dentro del budget. |
| **Esfuerzo** | Bajo — cálculo con días del mes transcurridos vs gastados |
| **Dependencias** | BudgetSummaryDto existente |
| **Requiere** | Calcular: `% tiempo transcurrido` vs `% presupuesto usado`. Si `% usado > % tiempo` → alerta naranja. |
| **UI sugerida** | Debajo de cada progress bar, añadir línea: "Llevas el 45% del mes y usaste el 52% de tu budget — ⚠️ Ritmo alto" |

---

## 🟡 Prioridad MEDIA — Features diferenciadoras

### F4: Category Trend Arrows

| Campo | Detalle |
|-------|---------|
| **Descripción** | Junto a cada categoría en el gráfico stacked, mostrar flecha ↑↓ con % de variación vs mes anterior |
| **Por qué** | Detección visual instantánea de categorías que están creciendo o reduciéndose |
| **Esfuerzo** | Medio — requiere nuevo query o enriquecer el existente con datos del mes anterior |
| **Dependencias** | GetMonthlySpendingByCategoryQuery |
| **Requiere** | Comparar gasto de categoría actual vs mes anterior, calcular variación % |
| **UI sugerida** | Tooltip en el chart o leyenda debajo: "Alimentación: ↑12% vs mes pasado" con color rojo/verde |

---

### F5: Cash Flow Forecast

| Campo | Detalle |
|-------|---------|
| **Descripción** | Proyección de saldo a 30/60/90 días basada en gastos recurrentes e históricos |
| **Por qué** | Transforma datos históricos en actionable insight: "Si tu patrón continúa, a fin de mes tendrás €X" |
| **Esfuerzo** | Alto — requiere detección de recurrentes + algoritmo de proyección |
| **Dependencias** | Transacciones históricas suficientes (≥3 meses) |
| **Requiere** | Identificar transacciones recurrentes (mismo importe ±5%, misma categoría, cada mes) y extrapolar |
| **UI sugerida** | Línea punteada en el chart Monthly Trends que se extiende al futuro, con badge "Forecast: €X by end of month" |

---

### F6: Merchant/Payee Analysis

| Campo | Detalle |
|-------|---------|
| **Descripción** | Top 10 comercios donde más se gasta, con variación mensual |
| **Por qué** | Responder "¿Dónde se me va la plata?" es la pregunta #1 de cualquier usuario de finanzas |
| **Esfuerzo** | Medio — requiere parsear la descripción de transacciones para extraer merchant |
| **Dependencias** | Transacciones con descripciones parseadas |
| **Requiere** | Algoritmo de extracción de merchant name (regex o ML simple). Considerar normalización (ej: "AMAZON" = "Amazon.es" = "AMZN") |
| **UI sugerida** | Card con tabla: Comercio | Total gastado | % del total | Tendencia vs mes anterior |

---

### F7: Subscription Tracker

| Campo | Detalle |
|-------|---------|
| **Descripción** | Detectar y listar automáticamente cargos recurrentes (suscripciones) |
| **Por qué** | El gasto en suscripciones es invisible pero significativo. "Pagás €87/mes en suscripciones" es un wake-up call. |
| **Esfuerzo** | Alto — algoritmo de detección de recurrentes |
| **Dependencias** | ≥3 meses de datos históricos |
| **Requiere** | Detectar transacciones con: misma descripción (fuzzy match), mismo importe (±5%), frecuencia mensual |
| **UI sugerida** | Card dedicada: lista de suscripciones detectadas con nombre, importe, categoría, y toggle activa/pausada |

---

## 🟢 Prioridad BAJA — Roadmap futuro

### F8: Financial Health Score

| Campo | Detalle |
|-------|---------|
| **Descripción** | Score de 0-100 basado en: regularidad de ingresos, tasa de ahorro, cumplimiento de budgets, diversificación |
| **Por qué** | Gamificación + benchmarking. Los usuarios adoran ver un "score" que mejora con el tiempo. |
| **Esfuerzo** | Alto — requiere modelo de scoring ponderado |
| **Dependencias** | Múltiples métricas ya disponibles |
| **UI sugerida** | Gauge circular grande en la parte superior del dashboard. Desglose: "Ingresos regulares: 8/10, Ahorro: 7/10, Budgets: 9/10" |

---

### F9: Anomaly Alerts

| Campo | Detalle |
|-------|---------|
| **Descripción** | Detectar y destacar transacciones inusuales ("Esta transacción es 3x tu gasto promedio en esta categoría") |
| **Por qué** | Detección temprana de fraudes, errores de charge, o gastos extraordinarios |
| **Esfuerzo** | Medio — require cálculo de z-score o desviación estándar por categoría |
| **Dependencias** | Histórico de gastos por categoría |
| **UI sugerida** | Badge amarillo/rojo en transacciones anómalas. Sección "Alertas" en el dashboard si hay ≥1 |

---

### F10: Monthly Goals Tracker

| Campo | Detalle |
|-------|---------|
| **Descripción** | El usuario define metas mensuales (ahorrar €500, reducir restaurantes a €100) con progress bars |
| **Por qué** | Objetivos concretos motivan más que stats abstractos |
| **Esfuerzo** | Medio — requiere nuevo modelo Goal + CRUD + cálculo de progreso |
| **Dependencias** | Tabla goals en BD, UI de creación de metas |
| **UI sugerida** | Cards con progress bars: "Ahorrar €500 → €320/500 (64%) 🟢" |

---

### F11: Income vs Expense Heatmap

| Campo | Detalle |
|-------|---------|
| **Descripción** | Calendario tipo GitHub contributions donde cada día se coloree por intensidad de gasto |
| **Por qué** | Detectar patrones temporales: "Los viernes gastás 40% más" |
| **Esfuerzo** | Medio — heatmap chart + query por día |
| **Dependencias** | Ninguna |
| **UI sugerida** | Grid 7x5 (semanas del mes), cada celda coloreada de verde (poco gasto) a rojo (mucho gasto) |

---

### F12: Quick Actions Bar

| Campo | Detalle |
|-------|---------|
| **Descripción** | Botones de acceso rápido contextuales: "Añadir gasto", "Subir extracto", "Ver budget" |
| **Por qué** | Reduce clicks para las acciones más comunes |
| **Esfuerzo** | Bajo — solo UI |
| **Dependencias** | Ninguna |
| **UI sugerida** | Floating action button (FAB) o barra de herramientas debajo del filtro de fechas |

---

## Matriz de decisión

| # | Feature | Esfuerzo | Impacto | ROI | Sugerencia |
|---|---------|----------|---------|-----|------------|
| F1 | Top 5 Gastos del Mes | 🟢 Bajo | 🔴 Alto | ⭐⭐⭐⭐⭐ | **Hacer primero** |
| F2 | Savings Rate Widget | 🟢 Bajo | 🔴 Alto | ⭐⭐⭐⭐⭐ | **Hacer primero** |
| F3 | Budget Burn Rate | 🟢 Bajo | 🟡 Medio | ⭐⭐⭐⭐ | Hacer pronto |
| F4 | Category Trend Arrows | 🟡 Medio | 🔴 Alto | ⭐⭐⭐⭐ | Siguiente sprint |
| F5 | Cash Flow Forecast | 🔴 Alto | 🔴 Alto | ⭐⭐⭐ | Requiere datos suficientes |
| F6 | Merchant Analysis | 🟡 Medio | 🟡 Medio | ⭐⭐⭐ | Requiere parsing de descriptions |
| F7 | Subscription Tracker | 🔴 Alto | 🟡 Medio | ⭐⭐⭐ | Requiere ≥3 meses de datos |
| F8 | Financial Health Score | 🔴 Alto | 🟡 Medio | ⭐⭐ | Roadmap largo |
| F9 | Anomaly Alerts | 🟡 Medio | 🟡 Medio | ⭐⭐⭐ | Cool feature |
| F10 | Monthly Goals Tracker | 🟡 Medio | 🟡 Medio | ⭐⭐⭐ | Requiere nuevo modelo |
| F11 | Heatmap | 🟡 Medio | 🟢 Bajo | ⭐⭐ | Visual appeal |
| F12 | Quick Actions Bar | 🟢 Bajo | 🟢 Bajo | ⭐⭐ | Nice to have |

---

## Sprint sugerido (orden de implementación)

**Sprint 1** — Quick wins (1-2 días):
1. F2: Savings Rate Widget
2. F1: Top 5 Gastos del Mes

**Sprint 2** — Budget intelligence (2-3 días):
3. F3: Budget Burn Rate
4. F4: Category Trend Arrows

**Sprint 3** — Análisis avanzado (3-5 días):
5. F6: Merchant/Payee Analysis
6. F9: Anomaly Alerts

**Sprint 4** — Features premium (5+ días):
7. F5: Cash Flow Forecast
8. F7: Subscription Tracker
9. F10: Monthly Goals Tracker

**Backlog futuro**:
- F8: Financial Health Score
- F11: Heatmap
- F12: Quick Actions Bar
