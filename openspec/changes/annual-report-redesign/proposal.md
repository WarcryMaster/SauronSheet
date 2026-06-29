# Propuesta: Rediseño Completo del Informe Anual

## Intención

`/Analysis/Annual` tiene 4 KPIs y 2 gráficos. Necesita 17 funcionalidades más: ahorro, multi-year, salud financiera, anomalías, predicciones, logros. Rediseño total con nueva arquitectura backend.

## Alcance

**In**: Executive summary full-viewport, smart summary, KPIs + savings rate + ratios + health score 0-100, multi-year, monthly best/worst, category ranking YoY, anomaly detection, timeline, top movements, achievements, trends, predictions, year nav prev/next.

**Out**: Modificar `Transaction.cs`, IA/ML, frameworks nuevos, export PDF. El motor clasificación se extrae sin modificar lógica.

## Capacidades

**Nueva**: `annual-report-executive-dashboard` — reemplaza `annual-analysis-dashboard`.

**Modificada**: Ninguna.

## Alternativas de Diseño

| # | UX | Pros | Contras | Esfuerzo |
|---|----|------|---------|----------|
| **A. Executive Magazine ★** | Scroll narrativo tipo revista: hero (summary+KPIs) → ratios → charts → timeline → detecciones | First viewport sin scroll; flujo guiado | ~1500 ln Razor | Alto |
| **B. Tabbed** | Pestañas: Resumen, Comparativa, Categorías, Advanced | Carga perezosa; testeable | Fricción entre tabs | Medio |
| **C. Vertical Grid** | 2-3 columnas con widgets auto-contenidos | Responsive; reutilizable | Sin jerarquía; sobrecarga | Medio-Alto |

**Recomendada: A** — el usuario entiende su año en un vistazo. Narrativa guía: resumen → KPIs → contexto → profundidad.

## Enfoque

`GetAnnualDashboardQuery` (single request) → Handler compuesto carga transacciones UNA VEZ y delega en servicios internos: Summary, SavingsRate, HealthScore, CategoryAnalysis, AnomalyDetection, Insights, Achievements, Timeline, Trends, Predictions, TopMovements, Ratios. Cada servicio es puro, testeable y opcional por feature flag. Frontend: Alpine.js + Chart.js + MDBootstrap.

## Áreas Afectadas

`Annual.cshtml` (rewrite), `Annual.cshtml.cs` (rewrite), `charts.js` (+5 init), `report-print.css` (new), `GetAnnualDashboardQuery.cs` + handlers (new), `Services/*.cs` ×10 (new), `DTOs/` ×10 (new), `SupabaseTransactionRepository.cs` (+server-side filter), tests ×40 + 6 E2E (new/update).

## Riesgos

| Riesgo | Prob. | Mitigación |
|--------|-------|------------|
| Load N veces transacciones | Alta | Server-side filter; carga 1 vez |
| Scope creep 17 feats | Alta | Slicing T1→T2→T3 |
| `data-testid` legacy rotos | Media | Preservar 6 existentes |

## Rollback

Revertir `Annual.cshtml` + `.cs`. No registrar handler en DI aísla backend. Rollback repo si se añadió método.

## Dependencias

Server-side filter en repositorio. T2/T3 requieren T1. Predictions: ≥2 años datos.

## Criterios de Éxito

- [ ] Smart summary + KPIs visibles sin scroll
- [ ] 4 KPIs legacy preservan `data-testid`
- [ ] Health score 0-100 sin IA; carga única transacciones
- [ ] `dotnet test` + E2E pasan; cobertura ≥80% domain, ≥70% application
