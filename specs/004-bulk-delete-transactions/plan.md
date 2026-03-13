# Implementation Plan: Bulk Delete Transactions

**Branch**: `004-bulk-delete-transactions` | **Date**: 2026-03-13 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/004-bulk-delete-transactions/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

**Primary Requirement**: Enable users to safely delete multiple transactions at once via multi-select checkboxes, atomic deletion command, and confirmation workflow.

**Technical Approach**:
- **Application Layer**: `BulkDeleteTransactionsCommand` with handler that validates ownership and orchestrates atomic delete via specification
- **Infrastructure Layer**: Repository method `DeleteTransactionsByIdsAsync()` wrapping deletions in single database transaction for atomicity and rollback on failure
- **Frontend Layer**: Razor Page with checkbox column (select all/deselect all), delete button, async confirmation modal, retry behavior on network errors
- **Error Recovery**: Max 3 auto-retries on transient errors; manual retry button on persistent failure; selection state preserved for recovery
- **Safety Guarantees**: Multi-tenant isolation (UserId scoping), atomic all-or-nothing semantics, confirmation count accuracy

## Technical Context

**Language/Version**: C# .NET 10 LTS  
**Primary Dependencies**: MediatR 12+ (CQRS), Supabase (Postgrest C# client v0.16.2)  
**Storage**: PostgreSQL (Supabase) with table-level transaction support  
**Testing**: xUnit + Moq (in-memory doubles for repository abstraction)  
**Target Platform**: .NET Core web application (Razor Pages + vanilla JS frontend)  
**Project Type**: Full-Stack monolith with Clean Architecture (Frontend → Application → Domain → Infrastructure)  
**Performance Goals**: Delete 5+ transactions in <30 seconds; 95% operation success rate (network reliability)  
**Constraints**:
- Atomic semantics (all-or-nothing deletion; transaction rollback on any failure)
- Max 1000 transaction selection per operation (default specification limit); enforced at UI with error toast
- Multi-tenant isolation enforced at Application handler level (UserId scoping)
- 3 auto-retries maximum on transient network errors (network timeout, HTTP 503, Postgrest unavailability)
- Retry backoff: Linear 1 second between attempts (no exponential backoff; simple predictable UX)
- Transient errors: Only network-level (HttpRequestException, timeout on Postgrest) → retry; business logic errors (constraint violation, permission denied) → no retry, show error

**Scale/Scope**: Single-user per request; no concurrent cross-user conflict handling (multi-tab concurrency out of scope)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

**Status**: ✅ PASS (All 5 core principles compliant)

### I. Clean Architecture & Layered Dependencies
- ✅ **Command Isolation**: `BulkDeleteTransactionsCommand` dispatched via MediatR; handler orchestrates Domain service
- ✅ **Dependency Flow**: Frontend (UI) → Application (handler) → Domain (specification) → Infrastructure (repository)
- ✅ **No Upward References**: Domain layer contains zero Infrastructure/Application dependencies
- ✅ **Repository Abstraction**: Infrastructure implements `ITransactionRepository.DeleteTransactionsByIdsAsync()` per Domain interface

### II. CQRS + MediatR Pattern
- ✅ **Command Definition**: `BulkDeleteTransactionsCommand(UserId, IReadOnlyList<TransactionId>)` with focused handler
- ✅ **Single Handler**: One MediatR handler per command with testable responsibility (validation + orchestration)
- ✅ **Side Effects Scoped**: Delete operations isolated to handler execution; no cross-handler side effects
- ✅ **Pipeline Routing**: All delete operations routed through `IMediator.Send()` with validation pipeline behavior

### III. Domain-Driven Design (DDD)
- ✅ **Strong-Typed IDs**: Uses `TransactionId(Guid)` and `UserId(string)` value objects; prevents ID type confusion
- ✅ **Aggregate Roots**: `Transaction` entity maintains invariants (UserId, createdAt immutability)
- ✅ **Specification Pattern**: Uses `ISpecification<Transaction>` for domain-language deletion filtering
- ✅ **System Defaults**: Not applicable to delete feature (no system defaults in deletion scope)
- ✅ **Domain Exceptions**: Handler catches and translates to application-layer error messages

### IV. Test-First Development (NON-NEGOTIABLE)
- ✅ **Minimum Coverage**: Application handler requires 100% coverage (delete success, failure, retry scenarios)
- ✅ **Domain Coverage**: At least 80% (specification validation, UserId scoping)
- ✅ **Testing Strategy**: 
  - Unit: Specification filtering logic with mocked repository
  - Integration: Handler with in-memory transaction repository
  - E2E: Cancel window, network retry behavior (deferred to Phase 5 if Infrastructure E2E needed)

### V. Spec-Driven Development
- ✅ **Phase Scope Declaration**: Full-Stack (Application command, Infrastructure repository, Frontend UI)
- ✅ **Single Spec File**: All requirements documented in single `spec.md` (no `.clarification-*` files)
- ✅ **Deliverables Within Scope**: Command handler, repository method, UI components all in declared scope
- ✅ **Out-of-Scope Documented**: Multi-tab concurrency deferred; soft-delete deferred to Phase 5

**Conclusion**: No constitution violations. Feature ready for planning and task generation.

## Project Structure

### Documentation (this feature)

```text
specs/004-bulk-delete-transactions/
├── spec.md              # ✅ Feature specification (completed)
├── plan.md              # THIS FILE (/speckit.plan command output)
├── research.md          # [PHASE 0] Output (/speckit.plan command)
├── data-model.md        # [PHASE 1] Output (/speckit.plan command)
├── quickstart.md        # [PHASE 1] Output (/speckit.plan command)
├── contracts/           # [PHASE 1] Output (/speckit.plan command)
└── tasks.md             # [PHASE 2] Output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (Repository Structure)

```text
SauronSheet/
├── src/
│   ├── SauronSheet.Domain/              # [LAYER] Domain layer (entities, value objects, specifications)
│   │   ├── Entities/
│   │   │   └── Transaction.cs           # Aggregate root with strong-typed Id
│   │   ├── ValueObjects/
│   │   │   ├── TransactionId.cs         # Strong-typed ID
│   │   │   └── UserId.cs
│   │   ├── Repositories/
│   │   │   └── ITransactionRepository.cs # Interface for Delete method
│   │   ├── Specifications/
│   │   │   └── TransactionByIdSpecification.cs # Filtering by IDs
│   │   ├── Exceptions/
│   │   │   └── DomainException.cs
│   │   ├── Common/
│   │   │   └── AggregateRoot.cs
│   │   └── SauronSheet.Domain.csproj
│   │
│   ├── SauronSheet.Application/         # [LAYER] Application layer (CQRS handlers, DTOs)
│   │   ├── Features/
│   │   │   └── Transactions/
│   │   │       ├── Commands/
│   │   │       │   ├── BulkDeleteTransactionsCommand.cs    # NEW: Command definition
│   │   │       │   └── BulkDeleteTransactionsHandler.cs    # NEW: MediatR handler
│   │   │       └── DTOs/
│   │   │           └── BulkDeleteResultDto.cs              # NEW: Response DTO
│   │   ├── Common/
│   │   │   └── IPipeline...cs           # Validation behaviors
│   │   └── SauronSheet.Application.csproj
│   │
│   ├── SauronSheet.Infrastructure/      # [LAYER] Infrastructure layer (persistence, auth)
│   │   ├── Persistence/
│   │   │   └── Repositories/
│   │   │       └── TransactionRepository.cs  # NEW: DeleteTransactionsByIdsAsync() impl
│   │   ├── Auth/
│   │   │   └── SupabaseAuthService.cs
│   │   └── SauronSheet.Infrastructure.csproj
│   │
│   └── SauronSheet.Frontend/            # [LAYER] Frontend layer (Razor Pages)
│       ├── Pages/
│       │   └── Transactions/
│       │       ├── Index.cshtml         # MODIFIED: Add checkboxes, delete button
│       │       └── Index.cshtml.cs      # MODIFIED: Add delete command handler
│       ├── Shared/
│       │   ├── _Layout.cshtml           # MDBootstrap CDN (no changes)
│       │   └── Modals/
│       │       └── _ConfirmDeleteModal.cshtml  # NEW: Confirmation modal
│       ├── wwwroot/
│       │   └── js/
│       │       └── bulk-delete.js       # NEW: Selection / delete UI logic
│       └── SauronSheet.Frontend.csproj
│
└── tests/
    ├── SauronSheet.Domain.Tests/        # [LAYER] Domain unit tests
    │   └── Specifications/
    │       └── TransactionByIdSpecificationTests.cs # NEW: 8-10 tests
    │
    ├── SauronSheet.Application.Tests/   # [LAYER] Application integration tests
    │   └── Features/
    │       └── Transactions/
    │           ├── BulkDeleteTransactionsHandlerTests.cs  # NEW: 15-18 tests
    │           └── BulkDeleteResultDtoTests.cs            # NEW: 2-3 tests
    │
    └── SauronSheet.Infrastructure.Tests/  # [LAYER] Infrastructure tests
        └── Persistence/
            └── TransactionRepositoryTests.cs # NEW: DeleteAsync() tests (5-8 tests)
```

**Structure Decision**: Feature uses existing Clean Architecture with full-stack scope:
- **Domain Layer**: Transaction entity, strong-typed IDs, specification for delete filtering
- **Application Layer**: BulkDeleteTransactionsCommand + handler with MediatR routing
- **Infrastructure Layer**: TransactionRepository implementation with atomic Supabase delete
- **Frontend Layer**: Razor Page (Index.cshtml) + JavaScript (bulk-delete.js) + confirmation modal

**No new projects created** — feature implemented within existing layered structure.

## Complexity Tracking

> **No Constitution Violations Detected** — This feature adheres to all 5 core principles.

Feature complexity is **MEDIUM** due to:

| Aspect | Complexity | Mitigation |
|--------|-----------|-----------|
| Atomic transaction semantics | Medium | Database transaction wrapping; rollback on any repository error |
| Error recovery with retries | Medium | Infrastructure layer implements 3 auto-retry policy on `Postgrest` transient errors |
| Multi-tenant isolation | Low | Enforced at Application handler level (UserId parameter scoping) |
| Specification-based filtering | Low | Reuse existing `ISpecification<T>` pattern; single specification for ID-based delete |
| Frontend selection state management | Medium | JavaScript client-side state; selection clears on filter/sort/pagination; cancel window 5 seconds |

**Estimated Test Coverage**:
- **Domain Tests** (Specifications): 8-10 tests (90%+ coverage)
- **Application Tests** (Handler): 15-18 tests with mocked repository (95%+ coverage)
- **Infrastructure Tests** (Repository): 5-8 tests (85%+ coverage)
- **Frontend Tests** (optional): Browser console validation of selection state

**Total Test Count**: 28-44 tests (conservative estimate for full coverage)

---

## Implementation Phases

### Phase 1: Domain & Specification Layer
- Verify `Transaction` aggregate root immutability constraints
- Create `TransactionByIdsSpecification` for filtering by multiple IDs
- Unit tests for specification with various ID counts (empty, single, bulk 100+)

### Phase 2: Application Layer (CQRS Handler)
- Define `BulkDeleteTransactionsCommand` record
- Implement `BulkDeleteTransactionsCommandHandler` with:
  - UserId tenant validation
  - Specification-based filtering
  - Call to repository delete method
  - Error mapping to user-friendly messages
  - Retry policy orchestration (3 attempts)
- Define `BulkDeleteResultDto` (count, error message, failed IDs list)
- Integration tests: mock repository, test success / failure / partial failure scenarios

### Phase 3: Infrastructure Layer
- Implement `DeleteTransactionsByIdsAsync(UserId, IEnumerable<TransactionId>)` in `TransactionRepository`
- Wrap deletion in PostgreSQL transaction
- Return count of deleted rows
- Handle Postgrest transient errors (net timeouts, temporary unavailability) for retry policy
- Infrastructure tests with in-memory doubles

### Phase 4: Frontend UI (Razor Pages)
- Add checkbox column to transaction list (Index.cshtml)
- Add "Select All / Deselect All" toggle in table header; disable if >1000 visible items
- Enable "Delete Selected" button on 1+ selections; disable if >1000 selected
- Create `_ConfirmDeleteModal.cshtml` partial with count display + MaxResults validation
- Implement `bulk-delete.js`:
  - Track selected TransactionIds; enforce MaxResults ≤ 1000 (show error toast if exceeded)
  - Clear selection on filter/sort/pagination
  - Handle delete command dispatch via MediatR (optimistic: clear selection immediately)
  - Implement cancel window (5-second grace period): if user clicks Cancel, UI restores selection from cache
  - Auto-retry on error: max 3 attempts with 1-second linear backoff (only for network errors)
  - Show retry button on persistent error (after 3 attempts); same selection preserved
  - Test scenario: User cancels mid-delete → server completes delete independently; next page load shows transaction gone

### Phase 5: Integration & E2E Testing (Optional)
- Full-stack test: UI → Handler → Repository → Database
- Network interrupt simulation (Postman/curl)
- Verify rollback behavior on partial failure
- Performance validation (<30 seconds for 100+ deletes)

---

## Success Metrics

| Metric | Target | Validation Method |
|--------|--------|------------------|
| Atomic deletion guarantee | 100% rollback on any error | Test suite: partial failure scenario |
| Confirmation accuracy | 100% correct count display | Test: count matches UI display |
| Error recovery | Max 3 auto-retries, then manual button | Test: network timeout scenario |
| Multi-tenant isolation | Zero data leakage | Test: cross-user delete attempt |
| Performance (5 deletes) | <30 seconds | Manual load test |
| Test coverage (Domain) | ≥80% | Code coverage tool |
| Test coverage (Application) | ≥95% | Code coverage tool |

---

## Risk Mitigation

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| Partial delete failure (DB constraint) | Medium | High | Atomic transaction; rollback all; unit tests for constraint violation scenario (e.g., active budget) |
| Network timeout during delete | High | Medium | 3 auto-retries at Infrastructure layer (1s linear backoff); manual retry UI button on persistent error |
| Multi-tenant ID mixing | Low | Critical | UserId parameter validation at handler entry; test with cross-tenant attempt (User A selects, User B logs in) |
| Selection state loss on page refresh | Medium | Low | JavaScript cache selection in memory; UI restores from cache on cancel within 5s; clear on logout |
| Large bulk delete timeout | Low | Medium | Specification MaxResults=1000 limit; UI enforces at selection (error toast); spec layer rejects if >1000 sent |
| Optimistic delete UI mismatch | Medium | Low | Test scenario: User cancels within 5s, server completes anyway → next page load reflects true DB state |
| Cancel after timeout (>5s) | Low | Low | Cancel button disabled after 5s; UI shows "Delete in progress" message; closes automatically after complete |

---

## Deliverables Summary

| Layer | Deliverable | File Path | Lines of Code | Tests |
|-------|------------|-----------|---------------|-------|
| Domain | Specification | `Specifications/TransactionByIdsSpecification.cs` | 15-20 | 8-10 |
| Application | Command | `Features/Transactions/Commands/BulkDeleteTransactionsCommand.cs` | 8-12 | — |
| Application | Handler | `Features/Transactions/Commands/BulkDeleteTransactionsHandler.cs` | 30-40 | 15-18 |
| Application | DTO | `Features/Transactions/DTOs/BulkDeleteResultDto.cs` | 5-10 | 2-3 |
| Infrastructure | Repository Method | `Persistence/Repositories/TransactionRepository.cs` | 20-30 | 5-8 |
| Frontend | Razor Page | `Pages/Transactions/Index.cshtml` | 50-80 | — |
| Frontend | Page Model | `Pages/Transactions/Index.cshtml.cs` | 15-25 | — |
| Frontend | Modal | `Shared/Modals/_ConfirmDeleteModal.cshtml` | 20-30 | — |
| Frontend | JavaScript | `wwwroot/js/bulk-delete.js` | 60-100 | — |
| Tests | Domain Tests | `SauronSheet.Domain.Tests/Specifications/*` | 80-120 | 8-10 |
| Tests | Application Tests | `SauronSheet.Application.Tests/Features/Transactions/*` | 200-300 | 15-18 |
| Tests | Infrastructure Tests | `SauronSheet.Infrastructure.Tests/Persistence/*` | 100-150 | 5-8 |

**Total Implementation**: ~800-1500 lines of code + ~450-650 lines of tests

---

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────┐
│                    FRONTEND LAYER                       │
│    Razor Pages (Index.cshtml) + JavaScript              │
│    ┌──────────────────────────────────────────────┐     │
│    │ [x] Trans 1  [x] Trans 2  [x] Trans 3        │     │
│    │ [Delete Selected] ← disabled until 1+ check  │     │
│    │ ┌─ Confirmation Modal ───────────────────┐  │     │
│    │ │ Delete 3 transactions? [Cancel] [Confirm] │  │     │
│    │ └───────────────────────────────────────┘  │     │
│    └──────────────────────────────────────────────┘     │
│                         ↓                                 │
│    MediatR.Send(BulkDeleteTransactionsCommand)          │
└─────────────────────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────┐
│              APPLICATION LAYER (CQRS)                   │
│    BulkDeleteTransactionsCommandHandler                  │
│    ┌──────────────────────────────────────────────┐     │
│    │ 1. Validate UserId ownership                 │     │
│    │ 2. Create TransactionByIdsSpecification      │     │
│    │ 3. Call repository.DeleteAsync(spec)        │     │
│    │ 4. Retry logic: 3 attempts on network error │     │
│    │ 5. Return BulkDeleteResultDto               │     │
│    └──────────────────────────────────────────────┘     │
└─────────────────────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────┐
│               DOMAIN LAYER                              │
│    TransactionByIdsSpecification                         │
│    ┌──────────────────────────────────────────────┐     │
│    │ Filter: WHERE id IN (@ids) AND              │     │
│    │         user_id = @userId                   │     │
│    │ Max Results: 1000 (specification default)   │     │
│    └──────────────────────────────────────────────┘     │
└─────────────────────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────┐
│            INFRASTRUCTURE LAYER                         │
│    TransactionRepository.DeleteTransactionsByIdsAsync   │
│    ┌──────────────────────────────────────────────┐     │
│    │ BEGIN TRANSACTION                            │     │
│    │ DELETE FROM transactions                     │     │
│    │   WHERE id IN (...)                          │     │
│    │   AND user_id = @userId                      │     │
│    │ [On Error: ROLLBACK ALL]                     │     │
│    │ COMMIT                                       │     │
│    └──────────────────────────────────────────────┘     │
└─────────────────────────────────────────────────────────┘
                         ↓
             Supabase PostgreSQL Database
```

---

## Frontend Considerations

**Cancel Window Semantics (Decision: Optimistic)**
- Delete is immediate in UI: selection clears, list refreshes, success toast shown
- User clicks "Cancel" within 5-second window: UI restores cached selection; no server abort sent (cancel is client-side only)
- Rationale: Simpler architecture (no server-side job tracking); reduces state management complexity; aligns with fast delete UX
- Server-side: Delete proceeds independently after handler execution; no cancellation RPC endpoint needed
- Test scenario: User confirms delete, network hiccup for 2 seconds, user clicks Cancel within 5s → selection restored in UI; server eventually completes delete; next refresh shows transaction gone

**Retry State Machine**
- **Initial**: User confirmed delete → send command → 3 auto-retries begin (network errors only)
- **After 3 failed retries**: Error message displayed; same selection preserved; manual "Retry" button enabled
- **Manual retry click**: Reset retry counter to 0; send command again; max 3 new attempts
- **Persistent error (>3 attempts)**: Error modal permanent until user clicks "Retry", dismisses with X, or navigates away
- Test scenarios: 
  - User clicks Retry button 5 times → confirm message updates after each attempt
  - Error occurs on 2nd of 3 retries → still shows Retry button (not "Give up")

**MaxResults Enforcement at UI Layer**
- Checkboxes disabled after 1000th transaction selected
- Error toast: "Maximum 1000 transactions per operation. Please select fewer items."
- Visual indicator: "1000 / 1000 selected" counter displayed
- Clear Selection button provided for easy re-selection
- Test scenario: User selects 1001 items → 1001st checkbox remains unchecked; error toast shown

---

## Test Scenarios (Implementation Reference)

### Domain Layer Tests
- `TransactionByIdsSpecification_MaxResults_Enforced`: Verify specification rejects >1000 IDs
- `TransactionByIdsSpecification_FiltersByUserId_Correctly`: Ensure multi-tenant isolation in spec

### Application Layer Tests
- `BulkDeleteTransactionsHandler_SuccessfulDelete_ReturnsCount`: Happy path
- `BulkDeleteTransactionsHandler_PartialFailure_RollsBackAll`: Atomic semantics (1 constraint error → all reverted)
- `BulkDeleteTransactionsHandler_NetworkTimeout_RetriesThreeTimes`: Auto-retry logic with 1s backoff
- `BulkDeleteTransactionsHandler_PersistentNetworkError_ThrowsAfterThreeAttempts`: Manual retry required
- `BulkDeleteTransactionsHandler_CrossUserAttempt_FailsWithForbidden`: Multi-tenant isolation
- `BulkDeleteTransactionsHandler_MaxResultsExceeded_ThrowsDomainException`: Selection >1000 rejected

### Infrastructure Layer Tests
- `TransactionRepository_DeleteTransactionsByIds_IsAtomic`: Rollback on single error
- `TransactionRepository_DeleteTransactionsByIds_ConcurrentDelete_Handles`: Concurrent calls from different users
- `TransactionRepository_DeleteTransactionsByIds_ConstraintViolation_Rollsback`: Real constraint (e.g., active budget)

### Frontend Tests (Manual/Browser Console)
- Cancel within 5s restores selection
- MaxResults >1000 disables checkboxes + error toast
- Manual retry button enables after 3 failed attempts
- Selection clears on filter/sort/pagination

---

**Status**: ✅ Ready for `/speckit.tasks` command to generate actionable development tasks.
**Next Step**: User executes `/speckit.tasks` to break down phases into granular, dependency-ordered implementation tasks.
