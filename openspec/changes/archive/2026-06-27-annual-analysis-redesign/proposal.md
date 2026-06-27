# Propuesta: Rediseño del Análisis Anual

## Intención

La página `/Analysis/Annual` actual es poco intuitiva: muestra dos tablas de **15 columnas cada una** (Movimiento + Tipo + Media + 12 meses), sin gráficos ni tendencias visuales. El usuario no puede escanear la salud financiera del año de un vistazo. La sobrecarga de información entierra los datos clave y hace imposible detectar patrones estacionales o comparar fijo/variable sin leer celda por celda.

## Alcance

### Dentro del alcance
- Rediseño visual tipo dashboard con KPIs, gráficos Chart.js y tablas colapsables
- Fila KPI superior: 4 tarjetas (Total Ingresos, Total Gastos, Neto, % Coste Fijo)
- Gráfico de líneas de tendencia mensual (ingresos vs gastos)
- Gráfico de distribución fijo/variable (donut o barras apiladas)
- Sección YoY con comparativa visual año anterior
- Tablas de detalle colapsables por defecto (toggle para ver desglose completo)
- Actualizar E2E tests para reflejar el nuevo layout

### Fuera del alcance
- Modificar el motor de clasificación (`AnnualClassificationEngine`)
- Añadir nuevas consultas a base de datos o cambiar el handler de backend
- Cambiar los DTOs existentes (ya contienen todos los datos necesarios)
- Añadir selectores de rango personalizado o filtros adicionales
- Migrar a otra librería de gráficos (seguimos con Chart.js)

## Capacidades

### Nuevas Capacidades
- `annual-analysis-dashboard`: Layout rediseñado con KPIs, gráficos de tendencia y distribución, comparativa YoY y tablas colapsables accesibles por toggle.

### Capacidades Modificadas
Ninguna. Es un rediseño puro de frontend. El handler, los DTOs y el motor de clasificación no cambian sus requisitos funcionales.

## Enfoque

Rediseño dashboard-style que reutiliza los DTOs existentes (`AnnualAnalysisResultDto`, `AnnualAnalysisSummaryDto`, `YearOverYearVariationDto`). El backend no se toca. La vista se reorganiza en:

1. **Selector de año** (se conserva el `<select>` actual)
2. **Fila KPI**: 4 tarjetas con formato condicional (positivo/negativo) y badges YoY
3. **Gráfico de tendencia**: línea dual (ingresos vs gastos por mes) con Chart.js, datos desde `AnnualAnalysisRowDto.MonthlyAmounts` agregados
4. **Distribución fijo/variable**: donut o stacked bar desde `Summary.IncomeFixed/Variable` y `ExpenseFixed/Variable`
5. **Sección YoY**: comparativa visual de los 7 campos del `YearOverYearVariationDto`
6. **Tablas colapsables**: ocultas por defecto, accesibles vía `x-show` toggle; conservan los `data-testid` actuales para E2E

Los gráficos se integran en `wwwroot/js/charts.js` siguiendo el patrón existente (init + JSDoc + tokens CSS de `DESIGN.md`). Se usará HTMX para recarga parcial del panel de gráficos al cambiar de año.

## Áreas afectadas

| Área | Impacto | Descripción |
|------|---------|-------------|
| `Pages/Analysis/Annual.cshtml` | Reescritura | Layout dashboard con KPIs + charts + collapsible tables |
| `Pages/Analysis/Annual.cshtml.cs` | Ligero | Añadir propiedades calculadas para charts (agregados mensuales) |
| `wwwroot/js/charts.js` | Ampliación | Nuevas funciones init para trend line + distribution chart |
| `e2e/tests/07-annual-analysis.spec.ts` | Actualización | Adaptar selectores y flujos al nuevo layout |

## Riesgos

| Riesgo | Probabilidad | Mitigación |
|--------|-------------|------------|
| Rotura de E2E tests por cambio de `data-testid` | Alta | Conservar testids en tablas colapsables; mapear nuevos testids en el spec |
| Agregación incorrecta de datos mensuales para charts | Media | Validar con tests unitarios las sumas por mes desde las filas existentes |
| Inconsistencia visual con el resto del dashboard | Baja | Seguir tokens de `DESIGN.md` y patrón de `Dashboard/Index.cshtml` |

## Plan de rollback

Revertir `Annual.cshtml` y `Annual.cshtml.cs` a la versión anterior. Las funciones nuevas en `charts.js` no rompen nada si no se invocan. El backend no se modifica.

## Dependencias

- Chart.js ya está cargado globalmente en `_Layout.cshtml`
- `DESIGN.md` para tokens de color y espaciado
- `charts.js` con patrón `destroyAllCharts()` ya implementado en `htmx:beforeSwap`

## Criterios de éxito

- [ ] La página muestra KPIs, gráficos y comparativa YoY antes que las tablas de detalle
- [ ] Las tablas de 15 columnas están colapsadas por defecto y se expanden con toggle
- [ ] Los 3 tests E2E existentes (`07-annual-analysis.spec.ts`) pasan con el nuevo layout
- [ ] `dotnet test` y `dotnet build` pasan sin errores
- [ ] Los colores de gráficos usan exclusivamente tokens de `DESIGN.md`
