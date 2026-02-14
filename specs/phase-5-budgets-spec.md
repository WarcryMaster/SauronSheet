# Phase 5: Budget Management & Advanced Features

**Version**: 1.0.0  
**Status**: Ready for speckit.tasks generation  
**Duration**: 2-3 weeks  
**Depends On**: Phase 4 Complete  
**Note**: OPTIONAL - Can defer to post-MVP if timeline slips

---

## Executive Summary

**Objective**: Advanced budget features + spending alerts + forecasting

**What We Build**:
- Advanced budget rules (recurring, category-specific)
- Email alerts (overbudget, big spender, weekly summary)
- Spending forecast (AI-assisted or trend-based)
- Budget planning tools (what-if scenarios)
- Recurring expense tracking + automation

---

## Features

**Budget Rules**:
- Monthly budget per category
- Alert thresholds (50%, 75%, 90%, 100%)
- Recurring expenses (fixed monthly bills)
- Seasonal adjustments

**Email Alerts**:
- Budget exceeded alert (immediate)
- Big spender alert (top 5% transaction)
- Weekly summary email
- Monthly budget report

**Forecasting**:
- Monthly spending trend (last 3 months)
- Predicted month-end total
- Category forecast

**Recurring Expenses**:
- Mark transaction as recurring
- Auto-generate future occurrences
- Skip/edit individual occurrences

---

## Deliverables (15 items)

**Domain**:
- [ ] RecurringExpense entity
- [ ] BudgetRule entity (advanced)
- [ ] SpendingForecast value object
- [ ] BudgetAlert domain event

**Application**:
- [ ] CreateRecurringExpenseCommand + handler
- [ ] UpdateBudgetRuleCommand + handler
- [ ] GetRecurringExpensesQuery + handler
- [ ] GetForecastQuery + handler
- [ ] SendBudgetAlertCommand + handler
- [ ] EmailService interface + abstraction

**Infrastructure**:
- [ ] Email service (SendGrid or SMTP)
- [ ] Scheduled job (recurring expense generation)
- [ ] Migration: 004_CreateRecurringExpensesTable.sql
- [ ] AlertHistory repository

**Frontend**:
- [ ] Budget rules management page
- [ ] Alert settings page
- [ ] Weekly summary template

---

## Test Specifications (8 tests)

- T05-001: RecurringExpense generates next occurrence
- T05-002: BudgetRule alert threshold calculation
- T05-003: SendBudgetAlertCommand sends email
- T05-004: Forecast calculation correct
- T05-005: Scheduled job generates recurring expenses
- T05-006: Email alerts respect user preferences
- T05-007: Recurring expense skipping/editing
- T05-008: Multi-tenant alert isolation

---

## Success Criteria

✅ Recurring expenses auto-generated  
✅ Email alerts sent on schedule  
✅ Forecast accuracy within 10% of actual  
✅ 8/8 tests passing  
✅ Email service production-ready  

---

## Deployment Notes

- Email service requires API key (SendGrid/SMTP credentials)
- Background job scheduler (Hangfire or other)
- Production email domain (for deliverability)

---

## Next Phase

Phase 6: UI Polish & Production Release (2-3 weeks)
- Performance optimization
- Accessibility compliance
- Error handling + monitoring
- Production deployment
