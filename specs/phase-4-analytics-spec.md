# Phase 4: Analytics Dashboard & MVP Release

**Version**: 1.0.0  
**Status**: Ready for speckit.tasks generation  
**Duration**: 3-4 weeks  
**Depends On**: Phase 3 Complete  
**🎯 MILESTONE**: MVP RELEASE (End Week 18)

---

## Executive Summary

**Objective**: Create analytics dashboard + release MVP (Week 18)

**What We Build**:
- Dashboard with spending charts (Chart.js or Recharts)
- Category breakdown (pie chart)
- Monthly trends (line chart)
- Budget status indicators
- Date range filtering
- Export reports (CSV + PDF)

**MVP Includes**:
- ✅ User auth + multi-tenancy
- ✅ Manual transaction entry
- ✅ PDF import (3+ bank formats)
- ✅ Analytics dashboard
- ✅ Budget management
- ✅ Deployed to Vercel

---

## Dashboard Queries

**GetSpendingByCategoryQuery**:
- Returns: Category + total spend + % of total
- Filters: Date range, exclude/include categories
- Visualization: Pie chart

**GetMonthlyTrendQuery**:
- Returns: Month + total spend + category breakdown
- Filters: Date range (year or 6 months)
- Visualization: Stacked bar chart

**GetBudgetStatusQuery**:
- Returns: Category + limit + spent + % used + status (OK, WARNING, OVER)
- Filters: Current month + historical comparisons
- Visualization: Progress bars

**GetTopExpensesQuery**:
- Returns: Top N transactions by amount
- Filters: Category, date range
- Visualization: Table

---

## Deliverables (18 items)

**Queries (6)**:
- [ ] GetSpendingByCategoryQuery + handler + Dto
- [ ] GetMonthlyTrendQuery + handler + Dto
- [ ] GetBudgetStatusQuery + handler + Dto
- [ ] GetTopExpensesQuery + handler + Dto
- [ ] GetDashboardSummaryQuery (aggregates all)
- [ ] ExportReportQuery + handler (CSV/PDF)

**Frontend (8)**:
- [ ] Dashboard.cshtml + PageModel
- [ ] Dashboard charts (Chart.js integration)
- [ ] Category breakdown chart
- [ ] Monthly trends chart
- [ ] Budget status cards
- [ ] Date range filter component
- [ ] Export button (CSV + PDF)
- [ ] Responsive layout (mobile-first)

**Reports (2)**:
- [ ] CSV export service
- [ ] PDF export service (iText)

**Analytics Support** (2):
- [ ] Caching layer (Redis or in-memory) for aggregations
- [ ] Scheduled report email (optional Phase 4 or Phase 5)

---

## Test Specifications (8 tests)

- T04-001: GetSpendingByCategoryQuery aggregates correctly
- T04-002: GetMonthlyTrendQuery filters by date range
- T04-003: GetBudgetStatusQuery shows correct status
- T04-004: Dashboard respects multi-tenancy
- T04-005: Date filtering works (month, quarter, year, custom)
- T04-006: CSV export generates valid file
- T04-007: PDF export generates valid document
- T04-008: Charts render without JavaScript errors

---

## Performance Requirements (MVP)

- Dashboard loads in <2s (with 10,000 transactions)
- Chart rendering <500ms
- Export <5s for 1 year of data
- Caching for aggregations (cache ttl: 1 hour)

---

## MVP Feature Checklist

Core:
- [x] User authentication
- [x] Multi-tenancy
- [x] Manual transaction entry
- [x] PDF bank import
- [x] Category management
- [x] Budget alerts
- [x] Analytics dashboard
- [x] Responsive design

Deployment:
- [ ] Environment variables configured (Supabase URL, API key)
- [ ] Database migrations applied
- [ ] GitHub Actions CI/CD working
- [ ] Deployed to Vercel (staging + production)
- [ ] HTTPS enabled
- [ ] Error tracking (Sentry) configured

---

## Success Criteria

✅ All 8 tests passing  
✅ Dashboard loads <2s  
✅ Mobile-responsive  
✅ All analytics queries working  
✅ MVP deployed to staging/production  
✅ User sign-up + login working end-to-end  

---

## MVP Launch Checklist

- [ ] Code review + approval
- [ ] All tests passing on CI/CD
- [ ] Performance testing (<2s dashboard load)
- [ ] Security audit (no SQL injection, XSS, CSRF)
- [ ] Accessibility review (WCAG 2.1 A minimum)
- [ ] Documentation complete
- [ ] Deployment to staging verified
- [ ] User testing on staging
- [ ] Deployment to production
- [ ] Monitor for 24h (error tracking, performance)

---

## Next Phase

Phase 5: Budget Management & Alerts (2-3 weeks) - OPTIONAL
- Advanced budget rules
- Recurring expense alerts
- Email notifications
- Spending forecasting

Phase 6: UI Polish & Production (2-3 weeks)
- Performance optimization
- Accessibility (WCAG 2.1 AA)
- Error handling + user feedback
- Production deployment hardening
