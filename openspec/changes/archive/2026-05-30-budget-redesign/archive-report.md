# Archive Report: budget-redesign

**Date**: 2026-05-30
**Change**: `budget-redesign`
**Status**: ✅ ARCHIVED
**Verification Verdict**: PASS (no critical issues)

---

## Archive Metadata

| Field | Value |
|-------|-------|
| Archived to | `openspec/changes/archive/2026-05-30-budget-redesign/` |
| Artifact Store Mode | Hybrid (Engram + Filesystem) |
| Delta Specs Synced | Yes — `budget-policies` (created), `budget-calculation` (created), `monthly-budgets` (deprecated) |

---

## What Was Accomplished

The budget system was completely redesigned from monthly-only budgets to **permanent budget policies with configurable granularity**. Key outcomes:

- **Budget as permanent policy**: Budgets now apply continuously from `EffectiveUntil` with configurable period granularity (Monthly, Quarterly, Semester, Annual)
- **BudgetCalculationService**: Pure domain service that computes metrics on-demand from transactions — no stored snapshots
- **Four views**: Current month, current period, year-to-date, and historical by year — all derived from the same service
- **Overlap prevention**: Exclusion constraint in PostgreSQL prevents overlapping budgets per user+category
- **Full UI rewrite**: 5 Razor Pages redesigned + 2 new views (Metrics, History) + dashboard widget update
- **Complete test rewrite**: 601 tests passing across 5 test projects

---

## Key Architecture Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| BudgetCalculationService location | Domain Services | Pure business logic, unit-testable without repo mocks |
| Partial periods | Count as complete (no prorrateo) | Simplifies logic, aligned with spec |
| Temporal uniqueness | BudgetService validation + DB exclusion range | Allows non-overlapping history per category |
| Spending calculation | Single query + in-memory distribution | Eliminates N+1 |
| Data migration | Drop + recreate (no data migration) | User confirmed old budgets dispensable |

---

## Files Created/Modified/Created

### Domain Layer
| File | Action |
|------|--------|
| `Domain/Entities/Budget.cs` | Modified — redesigned as permanent policy |
| `Domain/ValueObjects/BudgetPeriod.cs` | Created — Monthly/Quarterly/Semester/Annual enum |
| `Domain/Services/BudgetCalculationService.cs` | Created — period calculation + metrics |
| `Domain/Services/BudgetCalculationResult.cs` | Created — result record |
| `Domain/Services/BudgetService.cs` | Modified — ValidateNoOverlap |
| `Domain/Repositories/IBudgetRepository.cs` | Modified — new query methods |

### Infrastructure Layer
| File | Action |
|------|--------|
| `Infrastructure/Persistence/SupabaseBudgetRepository.cs` | Modified — BudgetRow rewrite + new queries |
| `supabase/migrations/*_budget_policies.sql` | Created — drop + recreate with exclusion constraint |

### Application Layer (~14 files)
| File | Action |
|------|--------|
| `Application/Features/Budgets/Commands/CreateBudgetCommand.cs` | Modified |
| `Application/Features/Budgets/Commands/UpdateBudgetLimitCommand.cs` | Created |
| `Application/Features/Budgets/Commands/UpdateBudgetPeriodCommand.cs` | Created |
| `Application/Features/Budgets/Commands/UpdateBudgetEffectiveDatesCommand.cs` | Created |
| `Application/Features/Budgets/Commands/DeactivateBudgetCommand.cs` | Created |
| `Application/Features/Budgets/Commands/DeleteBudgetCommand.cs` | Created |
| `Application/Features/Budgets/Queries/GetBudgetsQuery.cs` | Modified |
| `Application/Features/Budgets/Queries/GetBudgetMetricsQuery.cs` | Created |
| `Application/Features/Budgets/Queries/GetBudgetHistoryQuery.cs` | Created |
| `Application/Features/Budgets/Queries/GetBudgetVsActualQuery.cs` | Modified |
| `Application/Features/Budgets/DTOs/` | Modified — BudgetDto, BudgetMetricsDto, BudgetPeriodSummaryDto |

### Frontend Layer (~10 files)
| File | Action |
|------|--------|
| `Frontend/Pages/Budgets/Create.cshtml[.cs]` | Modified |
| `Frontend/Pages/Budgets/Edit.cshtml[.cs]` | Modified |
| `Frontend/Pages/Budgets/Index.cshtml[.cs]` | Modified |
| `Frontend/Pages/Budgets/Metrics.cshtml[.cs]` | Created |
| `Frontend/Pages/Budgets/History.cshtml[.cs]` | Created |
| `Frontend/Pages/Budgets/Comparison.cshtml[.cs]` | Modified |
| `Frontend/Pages/Dashboard/Index.cshtml[.cs]` | Modified (widget section) |

### Tests (~12 files)
| File | Action |
|------|--------|
| `tests/SauronSheet.Domain.Tests/Entities/BudgetTests.cs` | Created |
| `tests/SauronSheet.Domain.Tests/Services/BudgetCalculationServiceTests.cs` | Created |
| `tests/SauronSheet.Domain.Tests/Services/BudgetServiceTests.cs` | Created |
| `tests/SauronSheet.Application.Tests/Features/Budgets/Commands/` | Rewritten (5+ files) |
| `tests/SauronSheet.Application.Tests/Features/Budgets/Queries/` | Rewritten (4+ files) |
| `tests/SauronSheet.Infrastructure.Tests/Persistence/SupabaseBudgetRepositoryTests.cs` | Created |
| `e2e/tests/budgets/management.spec.ts` | Created |
| `e2e/tests/budgets/visualization.spec.ts` | Created |

---

## Test Coverage

| Project | Tests | Status |
|---------|-------|--------|
| SauronSheet.Domain.Tests | 242 | ✅ All pass |
| SauronSheet.Application.Tests | 178 | ✅ All pass |
| SauronSheet.Infrastructure.Tests | 116 | ✅ All pass |
| SauronSheet.Frontend.Tests | 55 | ✅ All pass |
| SauronSheet.Integration.Tests | 10 | ✅ All pass |
| **Total** | **601** | ✅ **All pass** |

---

## Specs Synced

| Domain | Action | Details |
|--------|--------|---------|
| `budget-policies` | **Created** | 4 requirements, 11 scenarios — permanent budget entity, overlap prevention, lifecycle, deletion |
| `budget-calculation` | **Created** | 6 requirements, 16 scenarios — calculation service, partial periods, EffectiveFrom/Until, status, views, comparison, widget |
| `monthly-budgets` | **Deprecated** | All 5 requirements removed and replaced by budget-policies + budget-calculation |

**Source of Truth Updated**:
- `openspec/specs/budget-policies/spec.md` — SYNCED
- `openspec/specs/budget-calculation/spec.md` — SYNCED
- `openspec/specs/monthly-budgets/spec.md` — DEPRECATED

---

## Known Limitations / Deferred Work

1. **SUGGESTION**: `BudgetCalculationService.GetCurrentPeriodRange()` lacks dedicated unit tests (used in Metrics.cshtml.cs Period view)
2. **SUGGESTION**: `DeleteBudgetCommandHandler` has no dedicated handler-level unit tests (coverage from indirect tests)
3. No coverage report generated in verification run — actual coverage not measured

---

## SDD Cycle Complete

| Phase | Status | Date |
|-------|--------|------|
| Explore | ✅ Completed | Pre-change |
| Propose | ✅ Completed | Pre-change |
| Spec | ✅ Completed | Pre-change |
| Design | ✅ Completed | Pre-change |
| Tasks | ✅ Completed | Pre-change |
| Apply | ✅ Completed | 8 slices, 41 tasks |
| Verify | ✅ PASS | 2026-05-30 |
| **Archive** | ✅ **COMPLETED** | **2026-05-30** |

**Status**: Ready for next change.

---

## Rollback Notes

- **Database**: Migration is drop+recreate. Rollback requires recreating old table schema + restoring old code from release branch.
- **Data loss**: Old monthly budgets intentionally discarded per confirmed requirements.
- **Code**: Revert to pre-budget-redesign release branch.

---

## Artifact Store Audit Trail

**Hybrid Mode**: All artifacts persisted to both Engram and filesystem archive.

**Archive Report**: Persisted to Engram as observation and to filesystem as `archive-report.md` in the archived change directory.
