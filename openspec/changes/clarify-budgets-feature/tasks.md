# Tasks: Consolidar presupuestos mensuales por categoría

## Review Workload Forecast

| Campo | Valor |
|-------|-------|
| Líneas estimadas modificadas | ~150–200 |
| Riesgo presupuesto 400 líneas | Low |
| Chained PRs recomendados | Yes |
| División sugerida | PR 1 → PR 2 → PR 3 |
| Delivery strategy | auto-chain |
| Chain strategy | stacked-to-main |

Decision needed before apply: No
Chained PRs recommended: Yes
Chain strategy: stacked-to-main
400-line budget risk: Low

> **Nota**: el cambio es pequeño (~150–200 líneas), pero la sesión usa `force-chained`.
> Se planifican 3 slices autónomas compatibles con stacked-to-main.

### Suggested Work Units

| Unit | Objetivo | PR | Rama base |
|------|----------|----|-----------|
| 1 | Domain threshold fix + unit tests | PR 1 | `main` |
| 2 | Application OnTrack fix + handler tests | PR 2 | rama PR 1 |
| 3 | Cobertura E2E budgets | PR 3 | rama PR 2 |

---

## Phase 1: Domain — Alineación de umbrales (PR 1)

- [x] 1.1 **[RED]** Añadir tests fallidos en `tests/SauronSheet.Domain.Tests/Services/BudgetServiceTests.cs` con límites 0.74, 0.75, 0.99, 1.0, 1.01 contra los nuevos umbrales del spec
- [x] 1.2 **[GREEN]** Actualizar `src/SauronSheet.Domain/Services/BudgetService.cs` — `GetStatusLevel`: Green < 0.75, Yellow 0.75–<1.0, Red == 1.0, Overage > 1.0
- [x] 1.3 **[REFACTOR]** Actualizar comentarios XML en `src/SauronSheet.Domain/ValueObjects/BudgetStatusLevel.cs` para reflejar los nuevos umbrales
- [x] 1.4 Ejecutar `dotnet test tests/SauronSheet.Domain.Tests` — todos pasan

---

## Phase 2: Application — Semántica OnTrack (PR 2)

- [ ] 2.1 **[RED]** Añadir test fallido en `tests/SauronSheet.Application.Tests/Features/Budgets/Queries/GetBudgetSummaryForDashboardQueryHandlerTests.cs` — estado Red NO es on-track
- [ ] 2.2 **[GREEN]** Corregir agregación en `src/SauronSheet.Application/Features/Budgets/Queries/GetBudgetSummaryForDashboardQueryHandler.cs` — contar solo Green + Yellow como on-track
- [ ] 2.3 **[VERIFY]** Ejecutar `dotnet test tests/SauronSheet.Application.Tests` — confirmar que `GetBudgetVsActualQueryHandlerTests` ya cubre categorías sin presupuesto
- [ ] 2.4 Ejecutar `dotnet test --collect:"XPlat Code Coverage"` — Domain ≥ 80 %, Application ≥ 70 %

---

## Phase 3: Cobertura E2E (PR 3)

- [ ] 3.1 Crear `e2e/tests/03-budgets.spec.ts` extendiendo `auth.fixture.ts`; worker único, ejecución secuencial
- [ ] 3.2 Escenario: crear presupuesto → verificar que aparece en el listado del mes
- [ ] 3.3 Escenario: abrir comparación → categoría sin presupuesto con gasto muestra etiqueta "Sin presupuesto" e importe
- [ ] 3.4 Escenario: abrir dashboard → widget refleja exclusivamente el mes actual (no mes anterior ni futuro)
- [ ] 3.5 Ejecutar `npx playwright test --config=e2e/playwright.config.ts --project=chromium` — todos los escenarios pasan

---

## Phase 4: Integridad de build

- [ ] 4.1 Ejecutar `dotnet build` — cero advertencias, cero errores (`TreatWarningsAsErrors=true` en Domain y Application)
- [ ] 4.2 Confirmar que `dotnet format` no produce cambios pendientes
