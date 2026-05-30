# Design: Rediseño del Sistema de Presupuestos — Políticas Permanentes

## Technical Approach

`Budget` se redefine como **política permanente** (aggregate root) con granularidad configurable. Se elimina `DateRange Period` y se introduce `BudgetPeriod` (enum) + `EffectiveFrom/Until`. Un `BudgetCalculationService` en Domain concentra toda la lógica de cálculo de períodos y límite acumulado. El spending se deriva siempre de transacciones en tiempo de consulta — nunca se almacena. Las cuatro vistas (mes, período, año, histórico) son consumidas por el mismo servicio con distintos rangos de fechas.

## Architecture Decisions

| Decisión | Opción elegida | Alternativa rechazada | Justificación |
|----------|---------------|----------------------|---------------|
| Ubicación de `BudgetCalculationService` | Domain Services | Application layer | Lógica pura de negocio sin dependencias externas; testeable unitariamente sin mocks de repositorio |
| Períodos parciales | Contar como completo (sin prorrateo) | Prorrateo proporcional | Simplifica la lógica, alineado con la spec. El usuario ve el límite completo del período aunque consulte a mitad |
| Unicidad temporal | Validación en `BudgetService` + exclusion range en DB | Solo constraint simple `user_id + category_id` | Permite historial de budgets no solapados para la misma categoría (ej: Q1→Q2), pero impide overlap |
| Spending calculation | Una sola query de transactions por rango, distribución en memoria | N queries individuales por budget | Elimina N+1. Patrón ya usado en `GetBudgetVsActualQueryHandler` actual |
| Migración de datos | Drop + recreate (sin migrar) | Migración con transformación | El usuario confirmó que los budgets mensuales viejos son prescindibles. Reduce complejidad y riesgo |
| `DateRange` VO | Se mantiene para queries (rango de consulta) | Eliminarlo por completo | Otros dominios lo usan (transactions, statements). Solo se elimina del aggregate `Budget` |

## Data Flow

### Crear presupuesto

```
UI (CreateBudget form)
  → CreateBudgetCommand(Guid CategoryId, decimal Limit, DateOnly EffectiveFrom,
                         DateOnly? EffectiveUntil, BudgetPeriod Period)
  → CreateBudgetCommandHandler
      ├── ICategoryRepository.GetByIdAsync() — validar categoría
      ├── BudgetService.ValidateNoOverlap() — verificar solapamiento
      ├── new Budget(id, userId, categoryId, effectiveFrom, effectiveUntil, period, limit)
      └── IBudgetRepository.AddAsync()
```

### Consultar métricas (cualquier vista)

```
UI (BudgetMetrics page, Dashboard widget, etc.)
  → GetBudgetMetricsQuery(DateOnly From, DateOnly To)
  → GetBudgetMetricsQueryHandler
      ├── IBudgetRepository.GetByUserIdAsync()
      ├── ITransactionRepository.FindBySpecificationAsync(user + dateRange) — UNA query
      ├── ICategoryRepository.GetByUserIdAsync()
      └── Para cada budget activo en el rango:
          BudgetCalculationService.Calculate(budget, from, to, spendForCategory)
            → BudgetMetricsResult { PeriodsElapsed, AccumulatedLimit, Spent,
                                     Remaining, PercentageUsed, StatusLevel }
```

## File Changes

| Archivo | Acción | Descripción |
|---------|--------|-------------|
| `Domain/Entities/Budget.cs` | Modify | Eliminar `DateRange Period`; añadir `EffectiveFrom`, `EffectiveUntil?`, `PeriodGranularity`. Nuevos métodos: `UpdateEffectiveDates()`, `UpdateGranularity()`, `IsActiveOn(DateOnly)` |
| `Domain/ValueObjects/BudgetPeriod.cs` | Create | Enum: `Monthly`, `Quarterly`, `Semester`, `Annual` |
| `Domain/Services/BudgetCalculationService.cs` | Create | `Calculate(Budget, DateOnly from, DateOnly to, Money spent) → BudgetMetricsResult`. Lógica de períodos transcurridos y límite acumulado |
| `Domain/Services/BudgetCalculationResult.cs` | Create | Record: `PeriodsElapsed`, `AccumulatedLimit`, `Spent`, `Remaining`, `PercentageUsed`, `StatusLevel` |
| `Domain/Services/BudgetService.cs` | Modify | `ValidateUniqueBudget()` → `ValidateNoOverlap(userId, categoryId, from, until?, excludeBudgetId?)`. Mover `GetStatusLevel()` aquí desde handler |
| `Domain/Repositories/IBudgetRepository.cs` | Modify | Eliminar `GetByUserAndCategoryAndMonthAsync`. Añadir `GetActiveByUserAndCategoryAsync(userId, categoryId, DateOnly asOf)` y `GetByUserAndDateRangeAsync(userId, from, to)` |
| `Infrastructure/Persistence/SupabaseBudgetRepository.cs` | Modify | `BudgetRow` reescrito: `effective_from DATE`, `effective_until DATE?`, `period_granularity VARCHAR`. Nuevos métodos de query |
| `supabase/migrations/YYYYMMDDHHMMSS_budget_policies.sql` | Create | Drop tabla vieja, create nueva con exclusion constraint |
| `Application/Features/Budgets/Commands/` | Modify | `CreateBudgetCommand` (nuevos campos), `UpdateBudgetCommand` (ampliado), nuevo `DeactivateBudgetCommand`, eliminar `DeleteBudgetCommand` → renombrar |
| `Application/Features/Budgets/Queries/` | Modify | Reescribir todos: `GetBudgetsQuery` → acepta rango de fechas. Nuevo `GetBudgetMetricsQuery`, `GetBudgetHistoryQuery`. Adaptar `GetBudgetVsActualQuery` y dashboard |
| `Application/Features/Budgets/DTOs/` | Modify | `BudgetDto` (nuevos campos), `BudgetMetricsDto` (reemplaza `BudgetStatusDto`), `BudgetPeriodSummaryDto`, `BudgetHistoryDto` |
| `Frontend/Pages/Budgets/*.cshtml[.cs]` | Modify | Rediseño de Create, Edit, Index, Detail, Comparison. Nuevas vistas: período actual, año actual, histórico |
| `tests/SauronSheet.Domain.Tests/` | Modify | Nuevos: `BudgetCalculationServiceTests`, `BudgetTests` (rediseño). Eliminados: tests de lógica mensual vieja |
| `tests/SauronSheet.Application.Tests/` | Modify | Reescritura de todos los handler tests de budgets (~8 archivos) |

## Interfaces / Contracts

```csharp
// Domain — new enum
public enum BudgetPeriod { Monthly, Quarterly, Semester, Annual }

// Domain — redesigned aggregate
public class Budget : AggregateRoot<BudgetId>
{
    public UserId UserId { get; }
    public CategoryId CategoryId { get; }
    public DateOnly EffectiveFrom { get; private set; }
    public DateOnly? EffectiveUntil { get; private set; }
    public BudgetPeriod PeriodGranularity { get; private set; }
    public Money Limit { get; private set; }

    public bool IsActiveOn(DateOnly date);
    public void UpdateLimit(Money newLimit);
    public void UpdateEffectiveDates(DateOnly from, DateOnly? until);
    public void UpdateGranularity(BudgetPeriod newGranularity);
    public void Deactivate(DateOnly asOf);
}

// Domain — calculation service
public class BudgetCalculationService
{
    public BudgetCalculationResult Calculate(
        Budget budget, DateOnly from, DateOnly to, Money spent);
    public int PeriodsElapsed(BudgetPeriod granularity, DateOnly from, DateOnly to);
}

// Domain — calculation result
public record BudgetCalculationResult(
    int PeriodsElapsed,
    Money AccumulatedLimit,
    Money Spent,
    Money Remaining,
    decimal PercentageUsed,
    BudgetStatusLevel StatusLevel);

// Application — key commands
public record CreateBudgetCommand(
    Guid CategoryId, decimal LimitAmount,
    DateOnly EffectiveFrom, DateOnly? EffectiveUntil,
    BudgetPeriod PeriodGranularity) : IRequest<Guid>;

public record UpdateBudgetLimitCommand(Guid BudgetId, decimal NewLimitAmount) : IRequest;
public record UpdateBudgetPeriodCommand(Guid BudgetId, BudgetPeriod NewPeriod, decimal NewLimitAmount) : IRequest;
public record DeactivateBudgetCommand(Guid BudgetId, DateOnly AsOf) : IRequest;

// Application — key queries
public record GetBudgetMetricsQuery(DateOnly From, DateOnly To) : IRequest<List<BudgetMetricsDto>>;
public record GetBudgetHistoryQuery(int Year) : IRequest<List<BudgetPeriodSummaryDto>>;
```

## Database Schema (migration sketch)

```sql
DROP TABLE IF EXISTS public.budgets CASCADE;

CREATE TABLE public.budgets (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL REFERENCES public.users(id) ON DELETE CASCADE,
    category_id UUID NOT NULL REFERENCES public.categories(id) ON DELETE CASCADE,
    effective_from DATE NOT NULL,
    effective_until DATE,
    period_granularity VARCHAR(10) NOT NULL DEFAULT 'Monthly'
        CHECK (period_granularity IN ('Monthly','Quarterly','Semester','Annual')),
    limit_amount DECIMAL(18,2) NOT NULL CHECK (limit_amount > 0),
    currency VARCHAR(3) NOT NULL DEFAULT 'EUR',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ,
    CONSTRAINT chk_effective_dates CHECK (
        effective_until IS NULL OR effective_until >= effective_from
    )
);

-- Exclusion constraint: no overlapping ranges per user+category
CREATE EXTENSION IF NOT EXISTS btree_gist;
ALTER TABLE public.budgets ADD CONSTRAINT budgets_no_overlap
    EXCLUDE USING gist (
        user_id WITH =,
        category_id WITH =,
        daterange(effective_from, COALESCE(effective_until, '9999-12-31'::date), '[]') WITH &&
    );

CREATE INDEX idx_budgets_user ON public.budgets(user_id);
CREATE INDEX idx_budgets_user_category ON public.budgets(user_id, category_id);
CREATE INDEX idx_budgets_effective ON public.budgets(effective_from, effective_until);

-- RLS policies (same pattern as before)
ALTER TABLE public.budgets ENABLE ROW LEVEL SECURITY;
CREATE POLICY "Users can view own budgets" ON public.budgets FOR SELECT USING (auth.uid() = user_id);
CREATE POLICY "Users can insert own budgets" ON public.budgets FOR INSERT WITH CHECK (auth.uid() = user_id);
CREATE POLICY "Users can update own budgets" ON public.budgets FOR UPDATE USING (auth.uid() = user_id) WITH CHECK (auth.uid() = user_id);
CREATE POLICY "Users can delete own budgets" ON public.budgets FOR DELETE USING (auth.uid() = user_id);
```

## Testing Strategy

| Capa | Qué probar | Enfoque |
|------|-----------|---------|
| Unit — Domain | `Budget` entity: invariantes, `IsActiveOn()`, guard methods | xUnit directo, sin mocks |
| Unit — Domain | `BudgetCalculationService`: períodos elapsed para cada granularidad, parciales, rangos con `EffectiveFrom/Until` | xUnit con casos de borde (inicio a mitad de año, período parcial, rango fuera de vigencia) |
| Unit — Domain | `BudgetService.ValidateNoOverlap()`: solapamiento, adyacentes, permanentes | xUnit + Moq de `IBudgetRepository` |
| Integration — Application | Cada command handler: validaciones, creación, actualización, desactivación | xUnit + Moq (repos mockeados) |
| Integration — Application | Cada query handler: métricas correctas, vistas, categorías sin budget | xUnit + Moq con datos de test en memoria |
| E2E — Frontend | Crear budget, editar límite, ver métricas del mes, ver histórico | Playwright con interacción real de usuario |

## Migration / Rollout

1. **Migración de DB**: Drop + recreate en una sola migración. Sin datos que preservar.
2. **Deploy atómico**: El nuevo código y la migración se despliegan juntos. No hay período de coexistencia.
3. **Rollback**: Revertir migración (recrear tabla vieja) + restaurar código desde rama de release.

## Open Questions

- [ ] ¿El constraint de exclusión `gist` con `daterange` funciona correctamente en Supabase Postgres (versión hosted)? Verificar que `btree_gist` esté disponible.
- [ ] ¿Debe `DeactivateBudget` fijar `EffectiveUntil` a la fecha actual o permitir una fecha futura?
