# SauronSheet Phase 5: Budget Management & Alerts

**Version**: 1.0.0  
**Duration**: 2-3 weeks  
**Status**: ⏳ Optional (can be deferred post-MVP)  
**Depends**: Phase 0, Phase 1, Phase 2

---

## Goal

Add budget creation, tracking, and alert system. Users can set per-category monthly budgets and receive notifications when approaching/exceeding limits. This phase is **OPTIONAL** and can be deferred after MVP release.

---

## Requirements

### Functional Requirements

| ID | Requirement | Acceptance Criteria |
|----|-------------|-------------------|
| **FR-001** | Create budget for category + month | Set limit, receive confirmation |
| **FR-002** | Budget alerts at 75%, 90%, 100% | Email notification when threshold reached |
| **FR-003** | Budget comparison dashboard | Show spent vs limit progress bars |
| **FR-004** | Edit/delete budget | Users modify or remove budgets |
| **FR-005** | Budget templates | Quick-set common budgets (e.g., $500 groceries) |
| **FR-006** | Recurring budgets | Auto-create next month's budget from prior month |

### Non-Functional Requirements
- NF-001: 8+ integration tests for budget operations
- NF-002: Email notifications sent within 5 minutes of threshold
- NF-003: Alert deduplication: max 1 email per threshold per day
- NF-004: Budget queries include remaining balance calculation

---

## Architecture

### New Components
- `CreateBudgetCommand` 
- `UpdateBudgetCommand`
- `DeleteBudgetCommand`
- `CreateRecurringBudgetCommand`
- `GetBudgetAlertsQuery`
- `BudgetAlertService` (monitoring + email dispatch)
- Background job: Daily budget check

---

## Deliverables

### Domain Layer
- [ ] `Domain/ValueObjects/BudgetAlert.cs` - Alert status (Warning, Critical, Exceeded)
- [ ] `Domain/Services/BudgetMonitoringService.cs` - Calculate spent, determine alert level

### Application Layer
- [ ] `Application/Features/Budgets/CreateBudgetCommand.cs` + handler
- [ ] `Application/Features/Budgets/UpdateBudgetCommand.cs` + handler
- [ ] `Application/Features/Budgets/DeleteBudgetCommand.cs` + handler
- [ ] `Application/Features/Budgets/GetBudgetAlertsQuery.cs` + handler
- [ ] `Application/Features/Budgets/GetBudgetTemplatesQuery.cs` + handler
- [ ] `Application/Tests/Features/Budgets/BudgetOperationTests.cs` (8 tests)

### Infrastructure Layer
- [ ] `Infrastructure/Services/EmailAlertService.cs` - Send budget alerts via email
- [ ] `Infrastructure/BackgroundJobs/DailyBudgetCheckJob.cs` - Scheduled monitoring
- [ ] `Infrastructure/Persistence/Migrations/007_AddBudgetAlertTracking.sql`

### Frontend Layer
- [ ] `Frontend/Pages/Budgets/Index.cshtml` - Budget list + edit form
- [ ] `Frontend/Pages/Budgets/Create.cshtml` - Create budget page
- [ ] `Frontend/Pages/Budgets/Templates.cshtml` - Quick-set templates

---

## Test Specifications

### Budget Tests (8+ tests)

- **T05-001**: Budget creation persists monthly limit
- **T05-002**: Alert triggered at 75% spent threshold
- **T05-003**: Alert triggered at 90% spent threshold
- **T05-004**: Alert triggered at 100% (exceeded) threshold
- **T05-005**: Duplicate alerts suppressed (max 1 per threshold per day)
- **T05-006**: Email sent with budget status + recommendation
- **T05-007**: Recurring budget auto-creates monthly copies
- **T05-008**: GetBudgetAlertsQuery returns ordered by alert level

---

## Success Criteria

✅ Phase 5 (Optional) is complete when:

1. `dotnet test` shows **8+/8+ Phase 5 tests passing**
2. Budgets created per user per category per month
3. Alerts sent at 75%, 90%, 100% thresholds
4. Email notifications working
5. Budget dashboard showing progress bars
6. Recurring budgets functional

---

## Timeline

- **Week 1**: Budget entity enhancements + BudgetMonitoringService
- **Week 2**: Commands + queries + 8 tests
- **Week 3**: Email alerts + background job setup (optional)

Status: **OPTIONAL** - Can ship MVP without this phase.

---

**Specification Version**: 1.0.0  
**Last Updated**: 2026-02-14
