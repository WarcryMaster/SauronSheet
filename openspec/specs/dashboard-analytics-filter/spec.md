# Especificación: Dashboard Analytics Filter

## Propósito

El filtro de fecha pill (All Time / This Month / Last 3 Months / This Year) en
`/dashboard` DEBE aplicarse a las tres gráficas (Spending by Category, Monthly
Trends, Year over Year) y no solo a las tarjetas de resumen.

---

## Requisitos

### Requisito: Queries de gráfica aceptan rango de fechas

`GetMonthlySpendingByCategoryQuery` y `GetMonthlyTrendsQuery` DEBEN aceptar
`(DateTime FromDate, DateTime ToDate)` en lugar de `int Year`.

#### Escenario: Filtro "All Time" cubre todo el histórico

- DADO el filtro "all" y gastos en 2025-01, 2025-06 y 2026-02
- CUANDO se invoca `GetMonthlySpendingByCategoryQuery(MinValue, today)`
- ENTONCES el resultado incluye los tres meses sin filtrar por año natural

#### Escenario: Filtro "This Month" limita a un solo mes

- DADO el filtro "this-month" activo el 2026-06-15
- CUANDO se invoca `GetMonthlyTrendsQuery(2026-06-01, 2026-06-15)`
- ENTONCES el resultado contiene exactamente una entrada para el mes 6

#### Escenario: Rango sin transacciones devuelve lista vacía

- DADO el filtro "last-3-months" sin gastos en ese periodo
- CUANDO se invoca cualquiera de las dos queries
- ENTONCES el resultado es una lista vacía

### Requisito: Monthly Trends rellena meses sin gastos con cero

`GetMonthlyTrendsQueryHandler` DEBE emitir una entrada por cada mes del rango,
con `Amount = 0` cuando no hay gastos.

#### Escenario: Mes intermedio sin gastos aparece con importe cero

- DADO el rango 2026-04-01 a 2026-06-15 con gastos solo en abril y junio
- CUANDO se invoca `GetMonthlyTrendsQuery(2026-04-01, 2026-06-15)`
- ENTONCES el resultado contiene 3 entradas (abril, mayo, junio) y la de mayo tiene `Amount = 0`

### Requisito: Year over Year mantiene par de años completos

`GetYearlyComparisonQuery` MANTIENE su firma `(int Year1, int Year2)`. El
`DashboardModel` DEBE calcular el par a partir del filtro activo y reflejarlo
en el título de la gráfica, incluso cuando el filtro es sub-anual.

#### Escenario: Filtro "This Year" produce par (current − 1, current)

- DADO `DateFilter = "this-year"` el 2026-06-15
- CUANDO el PageModel resuelve el par para YoY
- ENTONCES Year1 = 2025, Year2 = 2026 y el título muestra "2025 vs 2026"

#### Escenario: Filtro "All Time" usa año real de transacciones

- DADO `DateFilter = "all"` con última transacción en 2026
- CUANDO el PageModel resuelve el par para YoY
- ENTONCES Year2 = 2026 y Year1 = 2025

#### Escenario: Sin transacciones usa año en curso

- DADO `DateFilter = "all"` y 0 transacciones
- CUANDO el PageModel resuelve el par para YoY
- ENTONCES Year1 = currentYear − 1, Year2 = currentYear

### Requisito: Pill filter recarga las tres gráficas vía HTMX

La página `/dashboard` DEBE actualizar las tarjetas de las tres gráficas cuando
el usuario selecciona un pill distinto, y DEBE destruir las instancias previas
de Chart.js antes del swap.

#### Escenario: Cambio de pill recarga Spending by Category

- DADO el usuario en `/dashboard` con filtro "this-year"
- CUANDO selecciona el pill "last-3-months"
- ENTONCES la canvas `categoryStackedChart` muestra datos del nuevo rango

#### Escenario: Cambio de pill recarga Monthly Trends y Year over Year

- DADO el usuario en `/dashboard` con filtro "this-year"
- CUANDO selecciona el pill "last-3-months"
- ENTONCES `monthlyTrendsChart` y `yearlyComparisonChart` muestran datos del nuevo rango

#### Escenario: Instancias previas se destruyen antes del swap

- DADO que el dashboard ya tiene gráficas inicializadas
- CUANDO se dispara `htmx:beforeSwap`
- ENTONCES `destroyAllCharts()` ejecuta y todas las instancias de Chart.js se destruyen

### Requisito: Payload `dashboard-category-data` se elimina

`Dashboard.cshtml` y `Dashboard.cshtml.cs` NO DEBEN serializar el bloque
`<script type="application/json" id="dashboard-category-data">`.

#### Escenario: HTML renderizado no contiene el payload

- DADO el PageModel ejecutado con cualquier filtro
- CUANDO se renderiza la página
- ENTONCES el HTML resultante no contiene `id="dashboard-category-data"`

### Requisito: `GetMonthlySpendingByCategoryQuery` tiene cobertura de tests

El proyecto DEBE incluir tests unitarios para
`GetMonthlySpendingByCategoryQuery` en
`tests/SauronSheet.Application.Tests/Features/Analytics/Queries/`.

#### Escenario: Tests cubren los casos relevantes

- DADO el nuevo handler
- CUANDO se ejecuta la suite de tests del proyecto Application
- ENTONCES existen tests para: rango con gastos, rango sin gastos, multi-mes, single-mes y cambio de rango

---

## Criterios de Aceptación

- [ ] Las tres gráficas (Spending by Category, Monthly Trends, YoY) responden al filtro pill activo
- [ ] `GetMonthlySpendingByCategoryQuery` y `GetMonthlyTrendsQuery` aceptan `(FromDate, ToDate)`
- [ ] `GetMonthlyTrendsQueryHandler` rellena con ceros los meses sin gastos
- [ ] YoY muestra dos años completos derivados del filtro; el título lo indica
- [ ] `destroyAllCharts()` se invoca en `htmx:beforeSwap`
- [ ] El payload `dashboard-category-data` ya no aparece en el HTML
- [ ] `dotnet test` pasa; `dotnet build` limpio

## Fuera de Alcance

- Selector de rango personalizado en la UI pill (la lógica `CalculateDateRange` ya existe; exponerla es otra historia)
- Nuevos tipos de gráfica o widgets de dashboard
- Cambios en el esquema de base de datos
