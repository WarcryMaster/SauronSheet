# Feature Specification: Bulk Delete Transactions

**Feature Branch**: `004-bulk-delete-transactions`  
**Created**: 2026-03-13  
**Status**: Ready for Tasks  
**Layer Scope**: Full-Stack (Application, Infrastructure, Frontend)

## Executive Summary

Users need a safe and efficient way to remove multiple transactions at once from their expense history. This feature provides:
- Visual selection via checkboxes on the transaction list
- Bulk delete action button
- Confirmation dialog with item count
- Atomic transaction deletion with rollback on failure

This enables data cleanup and correction without tedious one-by-one deletion.

---

## Clarifications

### Session 2026-03-13

- Q: State management during delete (network interruption, tab close, reload) → A: Delete is async; UI clears immediately but caches selection state to allow cancel within 5 seconds
- Q: Retry mechanism and error recovery limits → A: On error, transactions remain selected; user can retry; max 3 auto-retries on network errors, then manual retry button
- Q: Partial failure handling (some delete, some fail) → A: Atomic semantics: if any fail, rollback ALL deletions; show error; user can retry entire operation
- Q: Multi-tab concurrency (same user, different tabs) → A: Out of scope
- Q: Selection persistence across filter/sort/pagination → A: Selection clears when any filter/sort/pagination changes

---

## User Scenarios & Testing

### User Story 1 — Select and Delete Multiple Transactions (Priority: P1)

User views their transaction list and wants to clean up old or duplicate entries. They select several transactions using checkboxes and delete them together.

**Why this priority**: Core functionality — enables the value proposition of bulk operations. Essential for MVP.

**Independent Test**: "User can select 3 transactions, click delete, confirm, and all 3 are removed from database" — deliverable MVP.

**Acceptance Scenarios**:

1. **Given** user is on transaction list page with 10 transactions visible, **When** user clicks checkbox next to transaction #1, **Then** transaction #1 is marked selected (visual feedback) and "Delete Selected" button appears/enables
2. **Given** 3 transactions are selected, **When** user clicks "Delete Selected" button, **Then** confirmation dialog appears showing "Delete 3 transactions?" with Cancel/Confirm buttons
3. **Given** confirmation dialog is open, **When** user clicks "Confirm", **Then** all 3 selected transactions are deleted from database and list refreshes
4. **Given** confirmation dialog is open, **When** user clicks "Cancel", **Then** dialog closes and transactions remain selected and undeleted

---

### User Story 2 — Select All / Deselect All Toggle (Priority: P2)

User has a large transaction list and wants to select all transactions at once instead of clicking individual checkboxes.

**Why this priority**: Improves UX for bulk operations on large dataset. Reduces clicks for common workflow.

**Independent Test**: "User can check 'Select All' box, then all visible transactions are selected; unchecking deselects all" — independently testable UI feature.

**Acceptance Scenarios**:

1. **Given** transaction list with 20 items visible and none selected, **When** user clicks "Select All" checkbox in header, **Then** all 20 transactions are marked selected
2. **Given** all 20 transactions are selected, **When** user clicks "Select All" checkbox again to toggle, **Then** all 20 transactions are deselected
3. **Given** 15 out of 20 transactions are selected, **When** user clicks "Select All", **Then** all 20 become selected
4. **Given** all transactions selected and user deletes 5 of them, **When** page refreshes, **Then** "Select All" is unchecked (reflects partial selection state)

---

### User Story 3 — Prevent Accidental Deletion with Confirmation (Priority: P1)

User accidentally clicks delete or selects wrong items. System prevents data loss with clear confirmation.

**Why this priority**: Safety requirement — delete is irreversible. Must protect user data from mistakes.

**Independent Test**: "Confirmation dialog shows exact count of transactions to be deleted; user can cancel before any data is lost" — independently testable safety feature.

**Acceptance Scenarios**:

1. **Given** 7 transactions are selected for deletion, **When** user clicks "Delete Selected", **Then** confirmation dialog displays "Delete 7 transactions?" (exact count)
2. **Given** confirmation dialog open, **When** user closes dialog with X button or Alt+F4, **Then** delete is cancelled and transactions remain
3. **Given** user changes mind and clicks "Cancel", **Then** all 7 transactions remain selected and visible in list (user can try again)
4. **Given** user clicks "Confirm" and deletion succeeds, **Then** success message appears before list refreshes (visual confirmation of action)

---

### User Story 4 — Handle Delete Failures Gracefully (Priority: P2)

Network error, database issue, or permission problem occurs during deletion. User is informed and can retry.

**Why this priority**: Reliability — system must handle transient failures and inform user. Prevents confusion.

**Independent Test**: "If delete fails, error message is shown and transactions remain selected and undeleted; user can retry" — independently testable error handling.

**Acceptance Scenarios**:

1. **Given** delete operation fails due to network timeout, **When** server returns error, **Then** error message displays to user with reason (e.g., "Network error. Your transactions were not deleted. Please try again.")
2. **Given** error message is shown, **When** user clicks "Retry", **Then** delete operation attempts again
3. **Given** delete fails, **When** user closes dialog, **Then** selected transactions remain selected for retry
4. **Given** subset of transactions fail to delete (e.g., 1 of 5), **Then** user sees report of which transactions deleted and which failed

---

### Edge Cases

- **Empty selection**: User clicks "Delete Selected" with no transactions checked → button disabled
- **Single transaction**: User selects 1 transaction and deletes → confirmation shows "Delete 1 transaction?" and completes
- **All transactions**: User selects all 100 transactions → system confirms and deletes all in one operation (atomic)
- **Pagination & Selection**: User is on page 2, selects items, navigates to page 3, selects more, applies filter → selection CLEARS (not persisted across UI state changes)
- **Concurrent deletion**: User A deletes transaction while User B is viewing same transaction (multi-tenant isolation) → User B sees refresh with transaction gone
- **Permission check**: User tries to delete another user's transaction (should fail at Application layer) → error shown
- **Async delete with network interruption**: User selects 5 transactions, confirms delete, network drops during server-side deletion → server completes delete independently; UI may show stale state until refresh
- **Delete with auto-retry**: User selects 5, delete fails with network timeout → system auto-retries up to 3 times silently; if still fails, shows manual retry button; same 5 remain selected
- **Partial delete failure**: User deletes 10 transactions, 7 succeed, 3 fail due to permission error (should not happen, but edge case) → strategy is ATOMIC: rollback all 10; show error; user retries entire operation
- **Filter after select**: User selects 5 transactions, then filters by date → selection is cleared; user must reselect from filtered results

---

## Requirements

### Functional Requirements

- **FR-001**: System MUST display checkbox next to each transaction in list view
- **FR-002**: System MUST display "Delete Selected" button that enables only when 1+ transactions are checked
- **FR-003**: System MUST display "Select All / Deselect All" toggle to bulk-select visible transactions
- **FR-004**: System MUST show confirmation dialog displaying exact count of selected transactions before deletion
- **FR-005**: System MUST allow user to cancel delete operation from confirmation dialog (Cancel button, X button, or Escape key)
- **FR-006**: System MUST delete all selected transactions atomically; if any fail, rollback ALL deletions to maintain consistency
- **FR-007**: System MUST prevent deletion of transactions belonging to other users (multi-tenant safety)
- **FR-008**: System MUST show success message after delete completes
- **FR-009**: System MUST show error message with reason if delete fails; system attempts max 3 auto-retries on network errors, then shows manual retry button
- **FR-010**: System MUST preserve user's selection state if delete fails (transactions remain checked for manual retry)
- **FR-011**: System MUST clear selection state when user applies filter, sort, or pagination changes
- **FR-012**: System MUST implement async delete with 5-second cancel window; if user closes tab/navigates during delete, server completes operation independently

### Key Entities

- **Transaction**: Core domain entity with Id (Guid), UserId (string), Amount, Date, Description, Category
- **BulkDeleteRequest**: Application-layer command containing array of TransactionIds and UserId for atomic operation
- **DeletedTransactionsResult**: DTO returning count of deleted transactions, list of failed IDs (if any), error message

---

## Success Criteria

### Measurable Outcomes

- **SC-001**: Users can delete 5+ transactions in under 30 seconds (compared to 2-3 minutes with one-by-one deletion)
- **SC-002**: 95% of delete operations complete without errors (network reliability)
- **SC-003**: Zero accidental multi-tenant data loss (all deletions scoped to requesting user)
- **SC-004**: 100% of delete confirmations show accurate transaction count (no data corruption)
- **SC-005**: System handles concurrent bulk deletes from multiple users without race conditions

---

## Architecture & Design Notes

### Application Layer (CQRS)

**Command**: `BulkDeleteTransactionsCommand`
```csharp
public record BulkDeleteTransactionsCommand(UserId UserId, IReadOnlyList<TransactionId> TransactionIds) : IRequest<BulkDeleteResult>;

// Handler must:
// 1. Validate all TransactionIds belong to UserId (tenant check)
// 2. Load all transactions from repository
// 3. Delete all in single database transaction (atomicity guaranteed)
// 4. On any error: rollback entire deletion; return count=0 + error message
// 5. Return count of successfully deleted transactions
// 6. Retry strategy: Infrastructure layer implements 3 auto-retries on transient errors
```

### Infrastructure Layer

**Repository Method**: `DeleteTransactionsByIdsAsync(UserId userId, IEnumerable<TransactionId> ids)`
- Must scope to userId (WHERE user_id = @userId)
- Must be atomic (DELETE wrapped in transaction)
- Must return count of deleted rows

### Frontend Layer

**Razor Page**: `Pages/Transactions/Index.cshtml`
- Add checkboxes column to transaction table
- Add "Select All" checkbox in table header
- Add "Delete Selected" button (disabled by default)
- Show confirmation modal before deletion
- Show success/error toast after completion

---

## Deliverables Checklist

- ✅ Application Command + Handler (BulkDeleteTransactionsCommand)
- ✅ Application DTO (BulkDeleteResult)
- ✅ Infrastructure repository method (DeleteTransactionsByIdsAsync)
- ✅ Frontend page updates (checkboxes, select-all, delete button, confirmation modal)
- ✅ Comprehensive tests (unit + integration)
- ✅ Documentation

---

## Out of Scope (Deferred)

- Soft-delete / recovery (transactions deleted permanently; recovery via audit log not in scope)
- Bulk-delete across date ranges (use filtering instead; select manually)
- Scheduled bulk-delete (one-time operation in this phase)
- Email notification of bulk delete (future phase)
- Bulk delete via API without UI (UI-driven only, Application layer supports both)

---

## Assumptions

- Multi-user isolation is enforced at Application handler level (not UI)
- Bulk delete is immediate (no undo); users accept this trade-off for speed
- Maximum 1000 transactions can be selected per operation (system default limit via specification)
- Transaction list refreshes after successful delete without page reload
- Error handling relies on Sentry logging for infrastructure visibility
