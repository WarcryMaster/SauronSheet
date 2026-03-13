# Tasks: Bulk Delete Transactions

**Input**: Design documents from `/specs/004-bulk-delete-transactions/`
**Prerequisites**: spec.md (4 user stories), plan.md (5 implementation phases), constitution.md (5 principles)
**Feature Branch**: `004-bulk-delete-transactions`
**Status**: Ready for Implementation  
**Testing Strategy**: Test-First Development (TDD) with 28-44 tests across 3 layers

---

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: User story label (US1, US2, US3, US4)
- **File paths**: Absolute from project root; adjust for .NET structure (src/*, tests/*)

---

## Phase 1: Domain & Specification Layer (Foundational)

**Purpose**: Build domain-level filtering logic with strong-typed IDs and atomic constraints

**⚠️ CRITICAL**: Must complete before Application handler implementation

### Domain Unit Tests (TDD: Red Phase)

- [x] T001 [P] Create domain test file `tests/SauronSheet.Domain.Tests/Specifications/TransactionByIdSpecificationTests.cs`
- [x] T002 [P] Write test: `TransactionByIdSpecification_FiltersByUserId_Correctly` → verify UserId scoping
- [x] T003 [P] Write test: `TransactionByIdSpecification_MaxResults_Enforced` → verify 1000 limit rejection
- [x] T004 [P] Write test: `TransactionByIdSpecification_EmptyIds_ReturnsEmpty` → edge case empty list
- [x] T005 [P] Write test: `TransactionByIdSpecification_SingleId_FiltersSingleTransaction` → single ID scenario
- [x] T006 [P] Write test: `TransactionByIdSpecification_BulkIds_Filters100Plus` → bulk scenario 100+ IDs
- [x] T007 [P] Write test: `TransactionByIdSpecification_NullUserId_ThrowsDomainException` → null guard
- [x] T008 [P] Write test: `TransactionByIdSpecification_AtomicityPreserved_WhenFiltering` → invariant check

### Domain Implementation (Green Phase)

- [x] T009 Create specification file `src/SauronSheet.Domain/Specifications/TransactionByIdSpecification.cs`
- [x] T010 Implement `TransactionByIdSpecification` class inheriting from `ISpecification<Transaction>`
- [x] T011 Add `ApplyCriteria(IQueryable<Transaction>)` method filtering by UserId + TransactionIds
- [x] T012 Add `MaxResults = 1000` constant per specification pattern
- [x] T013 Add guard clauses for null UserId and empty TransactionIds
- [x] T014 Run all domain tests → verify 8/8 passing

**Checkpoint**: Domain specification layer complete and testable; 80%+ coverage achieved

---

## Phase 2: Application Layer - CQRS Handler (Command Orchestration)

**Purpose**: Define command contract, orchestrate domain logic, handle errors, implement retry strategy

### Application Unit Tests (TDD: Red Phase)

- [x] T015 [P] Create test file `tests/SauronSheet.Application.Tests/Features/Transactions/BulkDeleteTransactionsHandlerTests.cs`
- [x] T016 [P] Write test: `BulkDeleteTransactionsHandler_SuccessfulDelete_ReturnsCount` → happy path 5 items
- [x] T017 [P] Write test: `BulkDeleteTransactionsHandler_UserIdValidation_FailsOnMismatch` → tenant isolation
- [x] T018 [P] Write test: `BulkDeleteTransactionsHandler_MaxResultsExceeded_ThrowsDomainException` → >1000 rejection
- [x] T019 [P] Write test: `BulkDeleteTransactionsHandler_NetworkTimeout_RetriesThreeTimes` → 3 auto-retries
- [x] T020 [P] Write test: `BulkDeleteTransactionsHandler_PersistentNetworkError_ThrowsAfterThreeAttempts` → manual retry required
- [x] T021 [P] Write test: `BulkDeleteTransactionsHandler_PartialFailure_RollsBackAll` → atomic semantics (constraint error)
- [x] T022 [P] Write test: `BulkDeleteTransactionsHandler_CrossUserAttempt_FailsWithForbidden` → multi-tenant abuse test
- [x] T023 [P] Write test: `BulkDeleteTransactionsHandler_EmptySelection_ReturnsZero` → edge case
- [x] T024 [P] Write test: `BulkDeleteTransactionsHandler_ErrorMessage_IsUserFriendly` → message validation

### Application DTO Tests (TDD: Red Phase)

- [x] T025 [P] Create test file `tests/SauronSheet.Application.Tests/Features/Transactions/BulkDeleteResultDtoTests.cs`
- [x] T026 [P] Write test: `BulkDeleteResultDto_Serialization_Succeeds` → JSON roundtrip
- [x] T027 [P] Write test: `BulkDeleteResultDto_FailedIds_Tracked` → error reporting

### Application Implementation (Green Phase)

- [x] T028 Create command file `src/SauronSheet.Application/Features/Transactions/Commands/BulkDeleteTransactionsCommand.cs`
- [x] T029 Define `BulkDeleteTransactionsCommand` record with `UserId` and `IReadOnlyList<TransactionId>` parameters
- [x] T030 [P] Create DTO file `src/SauronSheet.Application/Features/Transactions/DTOs/BulkDeleteResultDto.cs`
- [x] T031 [P] Define `BulkDeleteResultDto` with properties: `Count`, `ErrorMessage`, `FailedTransactionIds`
- [x] T032 Create handler file `src/SauronSheet.Application/Features/Transactions/Commands/BulkDeleteTransactionsCommandHandler.cs`
- [x] T033 Implement handler: Validate UserId ownership (throw if mismatch)
- [x] T034 Implement handler: Create `TransactionByIdSpecification(userId, ids)` specification
- [x] T035 Implement handler: Call `_transactionRepository.DeleteTransactionsByIdsAsync(userId, ids)` wrapped in try-catch
- [x] T036 Implement handler: Retry logic (max 3 attempts, 1-second linear backoff, network errors only)
- [x] T037 Implement handler: Error mapping (distinguish transient vs business errors; show user-friendly message)
- [x] T038 Implement handler: Return `BulkDeleteResultDto(Count=deletedCount, ErrorMessage=null/message, FailedIds=[])`
- [x] T039 Run all handler tests → verify 10/10 passing

**Checkpoint**: Application layer complete with full CQRS orchestration; 95%+ coverage achieved

---

## Phase 3: Infrastructure Layer - Repository Implementation

**Purpose**: Implement atomic deletion with transaction support, error handling, Postgrest integration

### Infrastructure Tests (TDD: Red Phase)

- [ ] T040 [P] Create test file `tests/SauronSheet.Infrastructure.Tests/Persistence/TransactionRepositoryTests.cs`
- [ ] T041 [P] Write test: `TransactionRepository_DeleteTransactionsByIds_IsAtomic` → rollback on error
- [ ] T042 [P] Write test: `TransactionRepository_DeleteTransactionsByIds_ConcurrentDelete_Handles` → concurrent users
- [ ] T043 [P] Write test: `TransactionRepository_DeleteTransactionsByIds_ConstraintViolation_Rollsback` → active budget constraint
- [ ] T044 [P] Write test: `TransactionRepository_DeleteTransactionsByIds_ReturnsCorrectCount` → count accuracy
- [ ] T045 [P] Write test: `TransactionRepository_DeleteTransactionsByIds_UserId_Scoped` → WHERE user_id filter
- [ ] T046 [P] Write test: `TransactionRepository_DeleteTransactionsByIds_ThrowsOnNetworkError` → exception propagation
- [ ] T047 [P] Write test: `TransactionRepository_DeleteTransactionsByIds_HandlesEmptyList` → empty ID list

### Infrastructure Implementation (Green Phase)

- [ ] T048 Locate repository file `src/SauronSheet.Infrastructure/Persistence/Repositories/TransactionRepository.cs` (existing)
- [ ] T049 Add method signature: `public async Task<int> DeleteTransactionsByIdsAsync(UserId userId, IEnumerable<TransactionId> ids)`
- [ ] T050 Implement: Use Postgrest transaction context for atomicity
- [ ] T051 Implement: DELETE FROM transactions WHERE id IN (@ids) AND user_id = @userId
- [ ] T052 Implement: Catch Postgrest exceptions (transient: timeout, 503 → propagate for retry; non-transient → rethrow)
- [ ] T053 Implement: Return deleted row count
- [ ] T054 Run all infrastructure tests → verify 8/8 passing

**Checkpoint**: Infrastructure persistence complete with atomic guarantees; 85%+ coverage achieved

---

## Phase 4: Frontend UI - Selection & Delete UX

**Purpose**: Build checkbox selection, confirmation modal, retry button, cancel window with optimistic updates

### Frontend Tests (Manual + Integration)

- [ ] T055 [P] Create integration test file `tests/Integration/BulkDeleteUITests.cs` (Playwright/Selenium optional)
- [ ] T056 [P] Test script: "Cancel within 5s restores selection" → UI cache behavior
- [ ] T057 [P] Test script: "MaxResults >1000 disables checkboxes + error toast"
- [ ] T058 [P] Test script: "Manual retry button enabled after 3 failed attempts"
- [ ] T059 [P] Test script: "Selection clears on filter/sort/pagination"

### Frontend Implementation - Razor Page

- [ ] T060 Modify Razor page `src/SauronSheet.Frontend/Pages/Transactions/Index.cshtml`
- [ ] T061 Add checkbox column to transaction table (Header: "Select All" toggle + counter "0 / 1000 selected")
- [ ] T062 Add "Delete Selected" button (disabled by default, enables on 1+ checked)
- [ ] T063 Add visual indicator for MaxResults: "1000 / 1000 selected" counter; disable checkboxes after 1000
- [ ] T064 Create confirmation modal partial `src/SauronSheet.Frontend/Shared/Modals/_ConfirmDeleteModal.cshtml`
- [ ] T065 Modal shows: "Delete [count] transactions?" + "This action cannot be undone"
- [ ] T066 Modal buttons: Cancel, Confirm + 5-second countdown timer display
- [ ] T067 Modify page model `src/SauronSheet.Frontend/Pages/Transactions/Index.cshtml.cs`
- [ ] T068 Add handler method: `public async Task OnPostDeleteAsync()` to dispatch `BulkDeleteTransactionsCommand`
- [ ] T069 Add error handling: Distinguish network errors (show retry button) from business errors

### Frontend Implementation - JavaScript Selection Logic

- [ ] T070 Create JavaScript file `src/SauronSheet.Frontend/wwwroot/js/bulk-delete.js`
- [ ] T071 Implement: Track selected TransactionIds in JavaScript Set (client-side cache)
- [ ] T072 Implement: "Select All" toggle → check/uncheck all visible + update counter
- [ ] T073 Implement: Per-checkbox toggle → add/remove from Set + update counter
- [ ] T074 Implement: MaxResults enforcement → after 1000, disable remaining checkboxes + error toast
- [ ] T075 Implement: Clear Selection button → flush Set + uncheck all
- [ ] T076 Implement: Filter/Sort/Pagination handlers → clear selection + reset counter
- [ ] T077 Implement: Delete button click → show confirmation modal (5-second countdown)
- [ ] T078 Implement: Cancel button (within 5s) → restore selection from cache; hide modal
- [ ] T079 Implement: Cancel button (after 5s) → disable button; show "Delete in progress" message
- [ ] T080 Implement: Confirm button → dispatch POST to `/Transactions/DeleteAsync`
- [ ] T081 Implement: Auto-retry logic (max 3 attempts, 1-second linear backoff)
- [ ] T082 Implement: Persistent error → show manual Retry button; same selection preserved
- [ ] T083 Implement: Success → show success toast; refresh transaction list; clear selection
- [ ] T084 Implement: Test scenario "User cancels mid-delete" → verify next refresh shows true DB state

**Checkpoint**: Full UI implementation with optimistic updates, cancel window, and retry behavior

---

## Phase 5: Cross-Layer Integration & E2E Testing

**Purpose**: Verify complete feature flow from UI to database, performance, edge cases

### Integration Tests (Full Stack)

- [ ] T085 [P] Create E2E test file `tests/Integration/BulkDeleteFeatureTests.cs`
- [ ] T086 [P] Test: "E2E - User selects 5, confirms, all deleted from DB" → happy path
- [ ] T087 [P] Test: "E2E - Network timeout on delete, auto-retries 3x, succeeds on 2nd retry"
- [ ] T088 [P] Test: "E2E - User A deletes 5, User B attempts same IDs, fails with Forbidden"
- [ ] T089 [P] Test: "E2E - Concurrent delete from different users, isolation verified"
- [ ] T090 [P] Test: "E2E - Partial failure (1 constraint error in 10), all 10 rolled back"
- [ ] T091 [P] Test: "E2E - Performance: Delete 5+ transactions in <30 seconds"
- [ ] T092 [P] Test: "E2E - Selection persisted across manual retries; cleared on new filter"

### Integration Tasks

- [ ] T093 Setup test database (Supabase test instance or in-memory double for full-stack validation)
- [ ] T094 Create test fixture: Pre-populated 100 transactions for multi-user scenarios
- [ ] T095 Create test fixture: Constraint violation scenario (transaction with active budget)
- [ ] T096 Mock network errors (HttpClientFactory timeout simulation for transient error testing)
- [ ] T097 Run all E2E tests → verify 8/8 passing
- [ ] T098 Measure performance: Confirm <30 seconds for 5+ deletes
- [ ] T099 Run code coverage: Domain ≥80%, Application ≥95%, Infrastructure ≥85%

**Checkpoint**: Feature complete with verified E2E behavior and performance targets met

---

## Phase 6: Polish & Finalization

**Purpose**: Documentation, edge case validation, security hardening, PR preparation

### Documentation & Finalization

- [ ] T100 Add XML docstring comments to all public domain/application/infrastructure methods
- [ ] T101 Update README.md with feature overview and usage examples
- [ ] T102 Verify MDBootstrap styling applied to checkboxes, modal, buttons (no inline CSS)
- [ ] T103 Test accessibility: Tab navigation through checkboxes, modal keyboard support (Escape to cancel)
- [ ] T104 Security audit: Verify UserId scope enforced; CSRF token in POST request
- [ ] T105 Browser compatibility test: Chrome, Firefox, Safari (select/modal/toast rendering)
- [ ] T106 Clean up console logs (dev-only logging removed)
- [ ] T107 Final build: `dotnet build` → 0 errors, warnings acceptable (pre-existing)
- [ ] T108 Final test run: `dotnet test` → all 28-44 tests passing
- [ ] T109 Commit changes with message: "feat: implement bulk delete transactions (feature 004)"
- [ ] T110 Create PR: Link to spec.md, plan.md, and all test results

**Checkpoint**: Feature complete, tested, documented, and ready for code review

---

## Task Dependencies & Parallel Execution

### Sequential Dependencies (Blocking)

```
Phase 1 Foundational → Phase 2 → Phase 3 → Phase 4 → Phase 5 → Phase 6
```

### Parallel Opportunities (Within Phase)

**Phase 1**: T001-T008 can run in parallel (all domain tests)
**Phase 2**: T015-T027 can run in parallel (all application tests before implementation)
**Phase 3**: T040-T047 can run in parallel (all infrastructure tests before implementation)
**Phase 4**: T055-T059 can run in parallel (all frontend test scripts)
**Phase 5**: T085-T092 can run in parallel (all integration test scenarios)

### Estimated Timeline

| Phase | Tasks | Parallel | Sequential | Est. Duration |
|-------|-------|----------|-----------|--------------|
| Phase 1 | T001-T014 | 8 | 6 | 4-5 hours |
| Phase 2 | T015-T039 | 10 | 15 | 6-8 hours |
| Phase 3 | T040-T054 | 8 | 7 | 3-4 hours |
| Phase 4 | T060-T084 | 5 | 20 | 8-10 hours |
| Phase 5 | T085-T099 | 8 | 7 | 4-5 hours |
| Phase 6 | T100-T110 | 0 | 11 | 2-3 hours |
| **TOTAL** | **110 Tasks** | **39** | **71** | **27-35 hours** |

---

## Success Checklist (Done When...)

- ✅ All 28-44 tests passing (Phase 1-5)
- ✅ Domain coverage ≥80%
- ✅ Application coverage ≥95%
- ✅ Infrastructure coverage ≥85%
- ✅ Feature 004 spec fully implemented (all 4 user stories + edge cases)
- ✅ Performance: <30 seconds for 5+ deletes (SC-001)
- ✅ Multi-tenant isolation: Zero cross-user data leakage (SC-003)
- ✅ Confirmation accuracy: 100% correct count display (SC-004)
- ✅ All functional requirements (FR-001 to FR-013) met
- ✅ Code review approved; PR merged to main

---

**Status**: ✅ Ready for Implementation  
**Next Step**: Begin Phase 1 (Domain Layer) - Start with TDD red phase (write failing tests first)  
**Commit**: Push tasks.md to branch `004-bulk-delete-transactions`
