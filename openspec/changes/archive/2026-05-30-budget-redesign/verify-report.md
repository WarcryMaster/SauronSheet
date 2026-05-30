## Verification Report

**Change**: budget-redesign
**Version**: N/A
**Mode**: Strict TDD

### Completeness
| Metric | Value |
|--------|-------|
| Tasks total | 41 |
| Tasks complete | 41 |
| Tasks incomplete | 0 |

### Build & Tests Execution
**Build**: ✅ Passed
```text
Command: dotnet build
Result: Build succeeded.
Warnings: 0
Errors: 0
```

**Tests**: ✅ 593 passed / ❌ 0 failed / ⚠️ 0 skipped
```text
Command: dotnet test
Domain: 242 passed
Application: 170 passed
Infrastructure: 116 passed
Integration: 10 passed
Frontend: 55 passed
Total: 593 passed, 0 failed, 0 skipped

Known limitation: Playwright E2E execution remains blocked by Supabase authentication/infrastructure.
```

**Coverage**: ❌ Not verifiable against the required thresholds
```text
Domain coverage command:
dotnet test tests/SauronSheet.Domain.Tests/SauronSheet.Domain.Tests.csproj --collect:"XPlat Code Coverage"

Produced coverage.cobertura.xml with:
- line-rate="0"
- lines-covered="0"
- lines-valid="651"

Application coverage command:
dotnet test tests/SauronSheet.Application.Tests/SauronSheet.Application.Tests.csproj --collect:"XPlat Code Coverage"

Produced coverage.cobertura.xml with:
- package name="SauronSheet.Domain"
- no SauronSheet.Application package/classes emitted

Solution-wide coverage command also reported:
- "No se encuentra ningún objeto datacollector con el nombre descriptivo 'XPlat Code Coverage'"

Conclusion: there is no valid runtime evidence proving Domain >80% or Application >70%.
```

### TDD Compliance
| Check | Result | Details |
|-------|--------|---------|
| TDD Evidence reported | ❌ | Engram apply-progress `#1318` has task summaries and counts, but no required `TDD Cycle Evidence` table |
| All tasks have tests | ❌ | Cannot verify per-task RED/GREEN mapping without the required table |
| RED confirmed (tests exist) | ❌ | Strict-TDD evidence is missing, so RED cannot be audited task-by-task |
| GREEN confirmed (tests pass) | ⚠️ | Runtime tests pass, but not traceable back to the missing TDD evidence matrix |
| Triangulation adequate | ⚠️ | Several behaviors are covered, but missing TDD evidence and blocked E2E prevent strict verification |
| Safety Net for modified files | ⚠️ | Not verifiable from the available artifact |

**TDD Compliance**: 0/6 checks fully passed

---

### Test Layer Distribution
| Layer | Tests | Files | Tools |
|-------|-------|-------|-------|
| Unit | 66 | 3 | xUnit |
| Integration | 76 | 9 | xUnit + Moq |
| E2E | 9 | 2 | Playwright (present, execution blocked by Supabase) |
| **Total** | **151** | **14** | |

---

### Changed File Coverage
| File | Line % | Branch % | Uncovered Lines | Rating |
|------|--------|----------|-----------------|--------|
| `src/SauronSheet.Domain/Entities/Budget.cs` | 79.62% | 64.28% | Not reliably attributable as layer coverage | ⚠️ Partial evidence only |
| `src/SauronSheet.Domain/Services/BudgetCalculationService.cs` | 63.79% | 52.38% | Not reliably attributable as layer coverage | ⚠️ Partial evidence only |
| `src/SauronSheet.Domain/Services/BudgetService.cs` | 50.00% / 94.73% | 43.75% / 80.00% | Duplicate async-state-machine entries; not valid for gate reporting | ⚠️ Partial evidence only |

**Average changed file coverage**: Not usable for acceptance. The generated reports do not provide a valid Domain/Application threshold proof.

---

### Assertion Quality
| File | Line | Assertion | Issue | Severity |
|------|------|-----------|-------|----------|
| `e2e/tests/budgets/visualization.spec.ts` | 50-86 | Repeated `main` visible assertions with optional branches | Smoke-style checks do not prove the view-specific business behavior required by the spec | WARNING |
| `e2e/tests/budgets/visualization.spec.ts` | 117-139 | Table assertions only if the table already exists | Test can pass without proving historical data rendering | WARNING |
| `e2e/tests/budgets/visualization.spec.ts` | 166-188 | Optional table checks plus generic summary-card count | Test is too weak to prove comparison semantics end-to-end | WARNING |

**Assertion quality**: 0 CRITICAL, 3 WARNING

---

### Quality Metrics
**Linter**: ➖ Not available
**Type Checker**: ✅ `dotnet build` completed with 0 errors and 0 warnings

### Spec Compliance Matrix
| Requirement | Scenario | Test / Evidence | Result |
|-------------|----------|-----------------|--------|
| Budget policy aggregate | Crear presupuesto permanente válido | `tests/SauronSheet.Application.Tests/Features/Budgets/Commands/CreateBudgetCommandHandlerTests.cs` → `Handle_ValidBudget_CreatesBudgetAndReturnsId`, `Handle_PermanentBudget_EffectiveUntilIsNull` | ✅ COMPLIANT |
| Budget policy aggregate | Crear presupuesto con fecha de fin | `CreateBudgetCommandHandlerTests.cs` → `Handle_AnnualBudget_CreatesWithCorrectGranularity` | ✅ COMPLIANT |
| Budget policy aggregate | Límite debe ser positivo | `tests/SauronSheet.Domain.Tests/Entities/BudgetTests.cs` → `Budget_ZeroLimit_ThrowsDomainException`, `Budget_NegativeLimit_ThrowsDomainException` | ✅ COMPLIANT |
| Budget policy aggregate | `EffectiveUntil` anterior a `EffectiveFrom` | `BudgetTests.cs` → `Budget_EffectiveUntil_Before_EffectiveFrom_ThrowsDomainException` | ✅ COMPLIANT |
| No solapamiento temporal | Segundo presupuesto sin solapamiento | `tests/SauronSheet.Domain.Tests/Services/BudgetServiceTests.cs` → `ValidateNoOverlap_AdjacentRanges_NoOverlap_Succeeds`; `CreateBudgetCommandHandlerTests.cs` → `Handle_AdjacentBudget_NoOverlap_Allowed` | ✅ COMPLIANT |
| No solapamiento temporal | Segundo presupuesto con solapamiento | `BudgetServiceTests.cs` → `ValidateNoOverlap_ExistingPermanentBudget_NewOverlaps_Throws`, `ValidateNoOverlap_OverlappingRanges_Throws`; `CreateBudgetCommandHandlerTests.cs` → `Handle_OverlappingBudget_ThrowsDomainException` | ✅ COMPLIANT |
| No solapamiento temporal | Constraint en base de datos evita duplicados concurrentes | Migration `supabase/migrations/20260530120000_budget_policies.sql` defines `budgets_no_overlap`, but no runtime test executed against the DB constraint | ❌ UNTESTED |
| Ciclo de vida | Actualizar límite | `tests/SauronSheet.Application.Tests/Features/Budgets/Commands/UpdateBudgetLimitCommandHandlerTests.cs` → `Handle_ValidUpdate_UpdatesLimitAndPersists` | ✅ COMPLIANT |
| Ciclo de vida | Actualizar período | `tests/SauronSheet.Application.Tests/Features/Budgets/Commands/UpdateBudgetPeriodCommandHandlerTests.cs` → `Handle_ValidUpdate_UpdatesGranularityAndLimit` | ✅ COMPLIANT |
| Ciclo de vida | Actualizar fechas | No update-dates command/handler/UI found. `Edit.cshtml.cs` only updates limit/granularity and deactivates. | ❌ FAILING |
| Ciclo de vida | Desactivar presupuesto permanente | `tests/SauronSheet.Application.Tests/Features/Budgets/Commands/DeactivateBudgetCommandHandlerTests.cs` → `Handle_DeactivatePermanentBudget_SetsEffectiveUntil` | ✅ COMPLIANT |
| Eliminación | Eliminar presupuesto activo físicamente | No application delete command/handler or frontend flow found; only deactivate flows in `Pages/Budgets/Index.cshtml.cs` and `Edit.cshtml.cs` | ❌ FAILING |
| Eliminación | Eliminar presupuesto histórico físicamente | No runtime path found that calls `IBudgetRepository.DeleteAsync(BudgetId)` from the budget feature | ❌ FAILING |
| BudgetCalculationService | Presupuesto mensual: un mes | `tests/SauronSheet.Domain.Tests/Services/BudgetCalculationServiceTests.cs` → `PeriodsElapsed_Monthly_SingleFullMonth_ReturnsOne` | ✅ COMPLIANT |
| BudgetCalculationService | Presupuesto mensual: acumulado anual | `BudgetCalculationServiceTests.cs` → `PeriodsElapsed_Monthly_TwelveFullMonths_ReturnsTwelve`, `Calculate_PermanentBudget_LargeRange_CountsAllPeriods` | ✅ COMPLIANT |
| BudgetCalculationService | Presupuesto trimestral: un trimestre | `BudgetCalculationServiceTests.cs` → `PeriodsElapsed_Quarterly_FullQuarter_ReturnsOne` | ✅ COMPLIANT |
| BudgetCalculationService | Presupuesto anual: un año | `BudgetCalculationServiceTests.cs` → `PeriodsElapsed_Annual_FullYear_ReturnsOne` | ✅ COMPLIANT |
| Períodos parciales | Mensual a mitad de mes cuenta completo | `BudgetCalculationServiceTests.cs` → `PeriodsElapsed_Monthly_PartialMonth_CountsAsComplete` | ✅ COMPLIANT |
| Períodos parciales | Trimestral parcial cuenta completo | `BudgetCalculationServiceTests.cs` → `PeriodsElapsed_Quarterly_PartialCrossesTwoQuarters_ReturnsTwo` | ✅ COMPLIANT |
| Vigencia | Iniciado a mitad de año | `BudgetCalculationServiceTests.cs` → `Calculate_MonthlyBudget_StartedMidYear_OnlyCountsMonthsAfterEffectiveFrom` | ✅ COMPLIANT |
| Vigencia | Consulta posterior a `EffectiveUntil` | `BudgetCalculationServiceTests.cs` → `Calculate_BudgetWithEffectiveUntil_QueryAfterEnd_ReturnsZeroPeriods` | ✅ COMPLIANT |
| Estado semáforo | Green | `BudgetCalculationServiceTests.cs` → `Calculate_OneMonth_UnderBudget_GreenStatus` | ✅ COMPLIANT |
| Estado semáforo | Overage | `BudgetCalculationServiceTests.cs` → `Calculate_OneMonth_OverBudget_OverageStatus` | ✅ COMPLIANT |
| Vistas de consulta | Vista mes actual | `src/SauronSheet.Frontend/Pages/Budgets/Metrics.cshtml.cs:64-68` computes month bounds correctly; E2E test exists in `e2e/tests/budgets/visualization.spec.ts` but was not run | ⚠️ PARTIAL |
| Vistas de consulta | Vista período actual | `src/SauronSheet.Frontend/Pages/Budgets/Metrics.cshtml.cs:52-57` uses the same month range as `Month`, so current-period-by-granularity is not implemented | ❌ FAILING |
| Vistas de consulta | Vista año actual | `Metrics.cshtml.cs:59-62` computes year bounds correctly; no passing runtime E2E evidence in this verification | ⚠️ PARTIAL |
| Vistas de consulta | Vista histórica por año anterior | `GetBudgetHistoryQueryHandler.cs` accepts any year; current verification has no passing test specifically proving prior-year history end-to-end | ⚠️ PARTIAL |
| Comparación budget vs actual | Categoría sin presupuesto con gasto | `tests/SauronSheet.Application.Tests/Features/Budgets/Queries/GetBudgetMetricsQueryHandlerTests.cs` → `Handle_CategoriesWithoutBudget_ShowsSinPresupuesto`; `GetBudgetVsActualQueryHandlerTests.cs` → `Handle_NoBudget_ShowsSinPresupuesto` | ✅ COMPLIANT |
| Dashboard widget | Dashboard con presupuestos activos | `src/SauronSheet.Frontend/Pages/Dashboard.cshtml:186-210` multiplies `PercentageUsed` by 100 again, so rendered percentages/progress are incorrect | ❌ FAILING |
| Dashboard widget | Dashboard sin presupuestos | `GetBudgetMetricsQueryHandler.cs:112-132` injects categories without budget into the widget data source, so the dashboard may not render the empty CTA state when there is spend but no budgets | ❌ FAILING |

**Compliance summary**: 19/29 scenarios compliant, 4 partial, 6 failing/untested

### Correctness (Static Evidence)
| Requirement | Status | Notes |
|------------|--------|-------|
| Domain aggregate redesign | ✅ Implemented | `Budget`, `BudgetPeriod`, `BudgetCalculationService`, and repository contract match the redesigned model |
| Monthly-budgets removal | ✅ Implemented | Old budget commands/queries referenced in the redesign are removed from the active feature surface |
| Date updates in edit flow | ❌ Missing | `Edit.cshtml.cs` exposes no editable `EffectiveFrom` / `EffectiveUntil` fields or command path |
| Physical deletion | ❌ Missing | Repository has `DeleteAsync`, but the budget application/frontend layer never exposes it |
| Current-period granularity view | ❌ Incorrect | `View=Period` currently reuses current month instead of per-budget current period |
| Tenant scoping of transaction fetch | ❌ Unsafe | Budget query handlers fetch transactions with date-only specifications, not `user + dateRange` as required |
| Code quality: TODO/stubs | ⚠️ Partial | No remaining budget stubs found, but `src/SauronSheet.Domain/Common/AggregateRoot.cs:13` still contains a `TODO` comment |

### Coherence (Design)
| Decision | Followed? | Notes |
|----------|-----------|-------|
| `BudgetCalculationService` in Domain | ✅ Yes | Implemented in `src/SauronSheet.Domain/Services/BudgetCalculationService.cs` |
| Partial periods count as full | ✅ Yes | Covered by domain tests for monthly and quarterly partial ranges |
| Temporal uniqueness in domain + DB | ⚠️ Partial | Domain service and SQL exclusion constraint exist, but DB constraint lacks a passing runtime verification |
| Single transaction query + in-memory distribution | ⚠️ Partial | Handlers do use one transaction query, but they do not add the required current-user filter |
| Drop + recreate migration | ✅ Yes | Migration `20260530120000_budget_policies.sql` drops and recreates `public.budgets` |
| Keep `DateRange` only for query use | ✅ Yes | `Budget` no longer embeds `DateRange`; query code still uses date-range specifications |
| Edit page allows limit, granularity, and dates | ❌ No | The implemented edit page only supports limit, granularity, and deactivate |

### Issues Found
**CRITICAL**:
1. Strict TDD verification FAILS because the required `TDD Cycle Evidence` table is missing from the apply-progress artifact (`Engram #1318`). In strict mode this is a protocol breach, not a cosmetic omission.
2. Coverage gate is not satisfied. The Domain coverage report produced `0/651` lines covered, the Application coverage report only emitted `SauronSheet.Domain` classes, and solution-wide coverage reported missing `XPlat Code Coverage` collectors for part of the solution. There is no valid evidence proving Domain >80% or Application >70%.
3. Budget date updates required by the spec are not implemented. `src/SauronSheet.Frontend/Pages/Budgets/Edit.cshtml.cs:21-29,61-92` exposes only limit/granularity editing, and there is no application command/handler for updating `EffectiveFrom` / `EffectiveUntil`.
4. Physical budget deletion required by the spec is not implemented. The active flows only deactivate budgets (`Pages/Budgets/Index.cshtml.cs:70-76`, `Pages/Budgets/Edit.cshtml.cs:139-145`), and no budget command/handler calls `IBudgetRepository.DeleteAsync`.
5. The “Current Period” view is functionally wrong. `src/SauronSheet.Frontend/Pages/Budgets/Metrics.cshtml.cs:52-57` reuses the current-month range instead of calculating the current period for each budget granularity.
6. Budget query handlers are not tenant-scoped as required by the design and repo rules. `GetBudgetMetricsQueryHandler.cs:63-67`, `GetBudgetHistoryQueryHandler.cs:68-72`, and `GetBudgetVsActualQueryHandler.cs:64-68` fetch transactions with `TransactionByDateRangeSpecification` only; `src/SauronSheet.Domain/Specifications/TransactionByDateRangeSpecification.cs:12-14` contains no user predicate.
7. Percentage rendering is broken in the dashboard and metrics UI. Domain/application already provide percentages as `0..100`, but `src/SauronSheet.Frontend/Pages/Budgets/Metrics.cshtml:88-115` and `src/SauronSheet.Frontend/Pages/Dashboard.cshtml:186-210` multiply them by 100 again, producing incorrect progress bars and labels.
8. The dashboard empty-state contract is broken. `GetBudgetMetricsQueryHandler.cs:112-132` includes spend-only categories without budgets, and `Dashboard.cshtml.cs:78` consumes that result directly for the widget. A user with spending but no budgets can miss the required empty CTA state.
9. The database overlap constraint scenario remains unverified at runtime. The migration defines `budgets_no_overlap`, but no passing test or staging verification proves the constraint actually blocks concurrent overlaps.

**WARNING**:
1. `e2e/tests/budgets/visualization.spec.ts` is too smoke-oriented to serve as strong behavioral evidence even once infrastructure is available; several assertions succeed after only proving that the page rendered.
2. `src/SauronSheet.Domain/Common/AggregateRoot.cs:13` still contains a `TODO` comment, so the code-quality gate is not fully clean.
3. Several UI scenarios are only partially verified because Playwright execution is blocked by Supabase infrastructure during this verification run.

**SUGGESTION**:
1. Add a dedicated budget delete command/handler plus end-to-end flow, or explicitly revise the spec/design to state that deactivation replaces deletion.
2. Add a user-scoped transaction specification (or composite specification) so budget queries enforce tenant isolation in the application layer instead of relying on environment-specific RLS behavior.
3. Fix the coverage toolchain before the next verify pass: add working `coverlet.collector` support for every relevant test project and emit per-layer reports that actually target Domain/Application assemblies.

### Verdict
FAIL
The change compiles and all .NET tests pass, but the implementation does not satisfy several mandatory spec behaviors, strict-TDD evidence is incomplete, and the required coverage thresholds cannot be proven with valid runtime data.
