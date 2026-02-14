# SauronSheet: Visual Roadmap & Dependencies

## Phase Timeline (Gantt-style)

```
PHASE 0: Foundation (2-3w)
████████████████░░░░░░░░░░░░░
W1━━━━━━━━━━━━━━━━━━━━━━━━━━

PHASE 1: Authentication (3-4w)
        ████████████████████░░░░░░
        W4━━━━━━━━━━━━━━━━━━━━━━━━

PHASE 2: Domain Entities (2-3w)
                ████████████████░░
                W8━━━━━━━━━━━━━━━━

PHASE 3: Transaction Import (3-4w)  ← MVP LAUNCH
                    ████████████████████░░
                    W11━━━━━━━━━━━━━━━━━━

PHASE 4: Analytics (3-4w)  ← FULL MVP
                            ████████████████████░░
                            W15━━━━━━━━━━━━━━━━━

PHASE 5: Budgets (2-3w)  ← OPTIONAL
                                ████████████████░░
                                W19━━━━━━━━━━━━

PHASE 6: Deploy (2-3w)  ← PRODUCTION
                                    ████████████████░░
                                    W22━━━━━━━━━━━━

TOTAL: 16-20 weeks (MVP @ Week 18, Full @ Week 24)
```

## Critical Path & Dependencies

```
START
  ↓
┌─────────────────────────────────────┐
│  PHASE 0: Foundation (2-3w)         │
│  .NET scaffolding, MediatR, Supabase│
│  ✓ Tests: 5+ passing                │
│  ✓ CI/CD: Pipeline green            │
└─────────────────────────────────────┘
  ↓
┌─────────────────────────────────────┐
│  PHASE 1: Authentication (3-4w) ⭐  │
│  BLOCKS: All later phases            │
│  Register → Login → JWT              │
│  ✓ Tests: Auth pipeline working      │
│  ✓ Deploy: Staging                   │
└─────────────────────────────────────┘
  ↓
┌─────────────────────────────────────┐
│  PHASE 2: Domain Entities (2-3w) ⭐ │
│  BLOCKS: Transaction import, Analytics│
│  User, Transaction, Budget entities  │
│  ✓ Tests: 20+ Entity tests           │
│  ✓ Migrations: Schema ready          │
└─────────────────────────────────────┘
  ↓
┌─────────────────────────────────────┐
│  PHASE 3: Import Pipeline (3-4w) 🎯 │
│  BLOCKS: Analytics                   │
│  PDF Parse → Validate → Persist      │
│  ✓ Tests: End-to-end import working  │
│  ✓ MVP LAUNCH ← Production snapshot   │
└─────────────────────────────────────┘
  ↓
┌─────────────────────────────────────┐
│  PHASE 4: Analytics (3-4w) 📊       │
│  OPTIONAL BLOCKING: Budget status    │
│  Queries, Dashboard, Charts          │
│  ✓ Tests: Query performance OK       │
│  ✓ FULL MVP RELEASE ← Production v1  │
└─────────────────────────────────────┘
  ├─ OPTIONAL PATH ──────────────────┐
  │                                    │
  │ PHASE 5: Budgets (2-3w)           │
  │ Budget CRUD, Alerts, Email        │
  │ ✓ Tests: Alert logic OK           │
  │ ✓ ENHANCE MVP ← Backfill feature  │
  │                                    │
  └──────────────────────────────────┤
  ↓
┌─────────────────────────────────────┐
│  PHASE 6: Deploy (2-3w) 🚀          │
│  Polish, Performance, Security       │
│  Production Supabase, Vercel, Sentry│
│  ✓ Lighthouse: ≥90                  │
│  ✓ Load test: 1000 users OK         │
│  ✓ PRODUCTION RELEASE ← live.com    │
└─────────────────────────────────────┘
  ↓
 END
```

## Phase Dependency Matrix

```
              │ Ph0 │ Ph1 │ Ph2 │ Ph3 │ Ph4 │ Ph5 │ Ph6
──────────────┼─────┼─────┼─────┼─────┼─────┼─────┼─────
Phase 0       │  —  │  ✓  │  ✓  │  ✓  │  ✓  │  ✓  │  ✓
Phase 1       │     │  —  │  ✓  │  ✓  │  ✓  │  ✓  │  ✓
Phase 2       │     │     │  —  │  ✓  │  ✓  │  ✓  │  ✓
Phase 3       │     │     │     │  —  │  ✓  │  ✓  │  ✓
Phase 4       │     │     │     │     │  —  │  ~ │  ✓
Phase 5       │     │     │     │     │  ~  │  —  │  ✓
Phase 6       │     │     │     │     │     │      │  —

Legend:
  —  = No dependency
  ✓  = Must complete before
  ~  = Optional (nice-to-have)
```

## Release Strategy

```
┌────────────────────────────────────────────────────────────┐
│             DEPLOYMENT PIPELINE                            │
├────────────────────────────────────────────────────────────┤
│                                                            │
│  MAIN BRANCH (Production-ready)                           │
│  ↑                                                         │
│  └─ Merge from staging after Phase exit tests pass        │
│                                                            │
│  STAGING (Phase testing environment)                      │
│  ↑                                                         │
│  └─ Feature branch → Tests pass → Merge to staging        │
│                                                            │
│  FEATURE BRANCH (In-progress phase work)                  │
│  └─ develop locally → Commit → Push → GitHub Actions      │
│                                                            │
└────────────────────────────────────────────────────────────┘

Deployment Milestones:
  Phase 0 end  → Local/Dev validation only
  Phase 1 end  → Staging deployment (auth testing)
  Phase 2 end  → Staging deployment (entities tested)
  Phase 3 end  → Staging + MVP snapshot tag on GitHub
  Phase 4 end  → Staging + Full MVP tag on GitHub
  Phase 5 end  → Staging + Enhanced feature tag
  Phase 6 end  → Production deployment (v1.0.0)
  
Production Release:
  Tag: v1.0.0 on main
  Deploy: Vercel auto-deploy
  Monitor: Sentry error tracking for 24h
```

## MVP Scope vs Full Release

```
╔═══════════════════════╦═══════════════════════════════════════╗
║  MVP Scope            ║  Full Release Scope                   ║
║  (Phases 0-4)         ║  (Phases 0-6)                         ║
╠═══════════════════════╬═══════════════════════════════════════╣
║ ✅ Foundation         ║ ✅ Everything in MVP +                ║
║ ✅ Authentication     ║ ✅ Budget Management                  ║
║ ✅ Domain Entities    ║ ✅ Budget Alerts & Emails             ║
║ ✅ PDF Upload         ║ ✅ UI Polish (mobile, dark mode)      ║
║ ✅ Transaction CRUD   ║ ✅ Performance optimization           ║
║ ✅ Dashboard          ║ ✅ Security audit & hardening         ║
║ ✅ Analytics Charts   ║ ✅ Error tracking (Sentry)            ║
║ ✅ CSV Export         ║ ✅ Load testing validated             ║
║                       ║ ✅ Production deployment              ║
║ Time: ~18 weeks       ║ Time: ~24 weeks                       ║
║ Users: Early adopters ║ Users: General public                 ║
║ Deployment: Staging   ║ Deployment: Production (Vercel)       ║
╚═══════════════════════╩═══════════════════════════════════════╝
```

## Risk Heatmap by Phase

```
PHASE 0: Foundation
┌─────────────────────────────────────┐
│ Setup complexity         🟢 LOW      │ Quick scaffolding
│ Dependencies             🟢 LOW      │ External libs proven
│ Testing difficulty       🟢 LOW      │ Straightforward setup
└─────────────────────────────────────┘

PHASE 1: Authentication
┌─────────────────────────────────────┐
│ Integration complexity   🟠 MEDIUM  │ Supabase JWT + cookies
│ Token lifecycle          🟡 MEDIUM  │ Refresh, expiry edge cases
│ Security implications    🔴 HIGH    │ Breaches expose all data
│ MITIGATION: Early security review  │
└─────────────────────────────────────┘

PHASE 2: Domain Entities
┌─────────────────────────────────────┐
│ Complexity               🟢 LOW      │ Well-defined bounded context
│ Testing diff             🟢 LOW      │ Unit tests are simple
│ Schema design            🟡 MEDIUM  │ Need to get migration right
│ MITIGATION: DDD review in week 8   │
└─────────────────────────────────────┘

PHASE 3: PDF Import
┌─────────────────────────────────────┐
│ PDF parsing              🔴 HIGH    │ Bank format variations
│ Data quality             🟠 MEDIUM  │ Malformed PDFs, OCR issues
│ Duplicate detection      🟡 MEDIUM  │ False positives/negatives
│ MITIGATION: Spike week 1, test      │ with real bank PDFs
└─────────────────────────────────────┘

PHASE 4: Analytics
┌─────────────────────────────────────┐
│ Query performance        🟠 MEDIUM  │ 10k+ transactions slow queries
│ Aggregate accuracy       🟢 LOW      │ Simple SUM/GROUP BY
│ Chart rendering          🟡 MEDIUM  │ Large datasets in browser
│ MITIGATION: Index optimization     │ in week 15
└─────────────────────────────────────┘

PHASE 5: Budgets
┌─────────────────────────────────────┐
│ Business logic           🟢 LOW      │ Straightforward rules
│ Email delivery           🟡 MEDIUM  │ Spam filters, delivery
│ Job scheduling           🟡 MEDIUM  │ Hangfire setup, timezone
│ MITIGATION: Test email locally     │ before production
└─────────────────────────────────────┘

PHASE 6: Deploy
┌─────────────────────────────────────┐
│ Performance              🟠 MEDIUM  │ 1000 concurrent users
│ Security vulnerabilities 🔴 HIGH    │ OWASP Top 10
│ Data migration           🟡 MEDIUM  │ No existing prod DB
│ MITIGATION: Full security audit,   │ load test, 2-week prep
└─────────────────────────────────────┘
```

## Key Milestones & Success Criteria

```
Week 3   ✅ Phase 0 Complete
         └─ Foundation architecture working
         └─ First 5+ tests passing
         └─ CI/CD pipeline green
         └─ Decision: Proceed or rethink design

Week 7   ✅ Phase 1 Complete
         └─ User auth working (register → login → JWT)
         └─ Tenant isolation verified
         └─ Security review passed
         └─ Decision: Auth solid enough for production

Week 10  ✅ Phase 2 Complete
         └─ Domain entities 100% tested
         └─ Database schema migrated
         └─ DDD patterns implemented consistently
         └─ Decision: Entity design validated

Week 14  🎯 PHASE 3 COMPLETE = MVP LAUNCH
         └─ PDF parsing working (real bank formats)
         └─ Transaction import end-to-end tested
         └─ Staging deployment successful
         └─ Market: Early adopters can use SauronSheet

Week 18  📊 PHASE 4 COMPLETE = FULL MVP
         └─ Analytics dashboard live
         └─ Query performance validated
         └─ Charts working with real data
         └─ Market: MVP feature-complete

Week 21  (Optional) Phase 5 Complete
         └─ Budget management working
         └─ Alerts & emails verified
         └─ Backfill: MVP v1.1 ready

Week 24  🚀 PHASE 6 COMPLETE = PRODUCTION RELEASE
         └─ Security audit passed (OWASP)
         └─ Load test validated (1000 users)
         └─ Lighthouse scores ≥90
         └─ Market: Public launch (v1.0.0)
```

## Decision Gates & Go/No-Go Checkpoints

```
╔════════════════════════════════════════════════════════════╗
║  GATE 0: Phase 0 Complete                                 ║
╠════════════════════════════════════════════════════════════╣
║  Must have:                                               ║
║  ✓ Solution builds without warnings                       ║
║  ✓ MediatR pipeline resolves handlers                     ║
║  ✓ 5+ passing tests                                       ║
║  ✓ CI/CD pipeline green                                   ║
║  Decision: GO to Phase 1 or rethink architecture?        ║
╚════════════════════════════════════════════════════════════╝

╔════════════════════════════════════════════════════════════╗
║  GATE 1: Phase 1 Complete (Auth)                          ║
╠════════════════════════════════════════════════════════════╣
║  Must have:                                               ║
║  ✓ User can register → login → JWT in cookie             ║
║  ✓ All queries scoped to current user                     ║
║  ✓ 8+ auth integration tests passing                      ║
║  ✓ Security review: JWT, cookies, CORS OK                ║
║  Decision: GO to Phase 2 or debug auth issues?           ║
╚════════════════════════════════════════════════════════════╝

╔════════════════════════════════════════════════════════════╗
║  GATE 2: Phase 2 Complete (Domain)                        ║
╠════════════════════════════════════════════════════════════╣
║  Must have:                                               ║
║  ✓ All entities 100% unit tested                          ║
║  ✓ Domain invariants enforced                             ║
║  ✓ Migrations runnable on Supabase                        ║
║  ✓ Repository interfaces defined                          ║
║  Decision: GO to Phase 3 or redesign entities?           ║
╚════════════════════════════════════════════════════════════╝

╔════════════════════════════════════════════════════════════╗
║  GATE 3: Phase 3 Complete (MVP LAUNCH) 🎯                 ║
╠════════════════════════════════════════════════════════════╣
║  Must have:                                               ║
║  ✓ PDF upload → extract transactions working             ║
║  ✓ Duplicates detected & skipped                          ║
║  ✓ Transaction CRUD fully tested                          ║
║  ✓ Staging deployment successful                          ║
║  ✓ Snapshot tag: v0.1-mvp-import on GitHub               ║
║  Decision: GO to Phase 4 or fix import issues?           ║
║  Outcome: MVP 1.0 ready for early adopters               ║
╚════════════════════════════════════════════════════════════╝

╔════════════════════════════════════════════════════════════╗
║  GATE 4: Phase 4 Complete (FULL MVP) 📊                   ║
╠════════════════════════════════════════════════════════════╣
║  Must have:                                               ║
║  ✓ Dashboard displays all charts with real data          ║
║  ✓ Queries execute < 500ms (10k records)                 ║
║  ✓ CSV export working                                     ║
║  ✓ All analytics tests passing                            ║
║  ✓ Staging verified                                       ║
║  ✓ Release tag: v0.2-full-mvp on GitHub                  ║
║  Decision: GO to Phase 5/6 or stop at MVP?               ║
║  Outcome: Full MVP released; evaluate Phase 5 value      ║
╚════════════════════════════════════════════════════════════╝

╔════════════════════════════════════════════════════════════╗
║  GATE 5: Phase 5 Complete (Optional) 💰                   ║
╠════════════════════════════════════════════════════════════╣
║  Must have (if doing Phase 5):                            ║
║  ✓ Budget CRUD working                                    ║
║  ✓ Alerts generated correctly                             ║
║  ✓ Emails sent on threshold breach                        ║
║  ✓ All budget tests passing                               ║
║  ✓ Release tag: v0.3-with-budgets on GitHub              ║
║  Decision: GO to Phase 6 or stop at MVP?                 ║
║  Outcome: Enhanced MVP v1.1; proceed to production       ║
╚════════════════════════════════════════════════════════════╝

╔════════════════════════════════════════════════════════════╗
║  GATE 6: Phase 6 Complete (PRODUCTION) 🚀                 ║
╠════════════════════════════════════════════════════════════╣
║  Must have:                                               ║
║  ✓ Lighthouse ≥90 (all audits)                            ║
║  ✓ Load test: 1000 users, p95 < 2s                       ║
║  ✓ Security audit: OWASP Top 10 addressed                ║
║  ✓ All tests passing in CI/CD                             ║
║  ✓ Smoke tests verified on prod environment              ║
║  ✓ Release tag: v1.0.0 on GitHub                          ║
║  Decision: LAUNCH or postpone?                            ║
║  Outcome: Public release; live on production URL         ║
╚════════════════════════════════════════════════════════════╝
```

## Quick Reference: Phase Exit Commitments

```bash
# Phase 0 Exit Commit
git commit -m "feat: phase 0 foundation setup complete

- Implement 4-layer architecture (Domain, Application, Infrastructure, Frontend)
- Configure MediatR CQRS pipeline with validation & logging behaviors
- Setup Supabase project with migrations framework
- Integrate GitHub Actions CI/CD pipeline
- Document architecture with diagrams and folder structure"

# Phase 1 Exit Commit
git commit -m "feat(phase-1): authentication & multi-tenancy complete

- Supabase Auth integration (register, login, logout)
- JWT token management (secure httpOnly cookies)
- IUserContext DI abstraction with user claims extraction
- User domain entity with multi-tenant isolation
- Authorization specifications (IsUserOwner, IsAdmin)
- Razor Pages: Login, Register, Logout with form handling
- Database: users table with auth metadata
- 8+ integration tests: Auth pipeline, JWT validation, tenant isolation
- Security: CORS, HTTPS, secure cookies configured
- Deploy: Staging environment working"

# Phase 2 Exit Commit
git commit -m "feat(phase-2): domain entities & value objects complete

- Transaction, Category, Budget AggregateRoots with invariants
- Money, TransactionId, CategoryId ValueObjects
- Domain Specifications: ByDateRange, ByCategory, ByAmount
- Repository interfaces: ITransactionRepository, ICategoryRepository, IBudgetRepository
- Database migrations: transactions, categories, budgets tables
- 20+ unit tests: Entity invariants, value object operations (100% coverage)
- DDD documentation: Entity diagrams, bounded contexts
- Architecture validated: Impossible states prevented"

# Phase 3 Exit Commit - MVP LAUNCH 🎯
git commit -m "feat(phase-3): transaction import pipeline complete – MVP launch

- PDF parsing service: Extract transactions from bank statements
- ImportTransactionsFromPdfCommand: Parse, validate, persist
- Transaction CRUD: CreateTransactionCommand, UpdateTransactionCommand, DeleteTransactionCommand
- Supabase repositories: TransactionRepository, CategoryRepository, BudgetRepository
- Duplicate detection: Prevent re-imports via date+amount hash
- Database migration: pdf_imports table for audit trail
- Razor Pages: Upload.cshtml (file input), Index.cshtml (transaction list), Create/Edit.cshtml
- End-to-end test: Upload PDF → Import → Query results
- Integration tests: PDF parsing, transaction CRUD, error handling (10+ tests)
- Staging deployment: Users can upload PDFs and create transactions
- Tag: v0.1-mvp-import

MILESTONE: MVP ready for early adopters"

# Phase 4 Exit Commit - FULL MVP
git commit -m "feat(phase-4): analytics & dashboard complete – Full MVP released

- MediatR Queries: GetSpendingByCategoryQuery, GetMonthlyTrendsQuery, GetBudgetStatusQuery
- Pagination: GetTransactionListQuery with limit/offset
- Database indexes: transactions(user_id, date, category_id), budgets(user_id, month)
- Dashboard: Pie chart (spending by category), Line chart (12-month trends), Budget status cards
- Reports: Monthly breakdown, Category reports, Budget analysis
- CSV export: Filtered transactions with headers
- Chart.js integration: Client-side rendering with data binding
- Query performance: <500ms for 10k transactions confirmed
- Integration tests: Query pagination, filtering, chart accuracy (8+ tests)
- Lighthouse: Performance ≥90 on charts
- Staging verified with real transaction data
- Tag: v0.2-full-mvp

MILESTONE: Full MVP released; core functionality complete"

# Phase 5 Exit Commit (Optional)
git commit -m "feat(phase-5): budget management & alerts complete

- MediatR Commands: CreateBudgetCommand, UpdateBudgetCommand, DeleteBudgetCommand
- MediatR Queries: GetBudgetsQuery with status calculations
- BudgetAlertService: Check spending vs. limits, generate alerts at 80% & 100%
- Email notifications: SendGrid integration for budget alerts
- Hangfire job: Daily budget check scheduled at 8 AM
- Razor Pages: Budget list with CRUD operations, status color-coding
- Integration tests: Budget CRUD, alert generation, email sending (8+ tests)
- Staging: Budget alerts and notifications working
- Tag: v0.3-with-budgets

MILESTONE: Enhanced MVP with budget tracking"

# Phase 6 Exit Commit - PRODUCTION RELEASE 🚀
git commit -m "chore(phase-6): production deployment complete – v1.0.0

- Responsive design: Mobile (375px), tablet, desktop layouts verified
- Accessibility: WCAG 2.1 AA compliance, keyboard navigation, ARIA labels
- Alpine.js: Loading spinners, modal dialogs, form validation
- Error pages: 404/500 with helpful messages
- Sentry integration: Error tracking and monitoring
- Performance: Tailwind CSS minification, Chart.js lazy-loading
- Security audit: CORS, HTTPS, CSP, XSS, SQL injection, CSRF mitigations
- Production Supabase: Separate environment, automated backups
- Vercel deployment: vercel.json configured, auto-deploy on push
- Load test: 1000 concurrent users, p95 < 2s verified
- Smoke tests: All workflows validated (register → upload → dashboard)
- Lighthouse: Performance 95, Accessibility 92, Best Practices 92, SEO 100
- User documentation: FAQ, troubleshooting guide, screenshot tour
- Tag: v1.0.0

MILESTONE: Production release live; public launch ready"
```

---

## Notes

- **Timeline**: Estimated 16-20 weeks total; MVP @ week 18, Full release @ week 24
- **MVP Emphasis**: Phases 0-4 deliver standalone value; Phase 5 is optional enhancement
- **Incremental Delivery**: Each phase deployable independently
- **Risk**: Phase 3 (PDF parsing) highest risk; mitigate with early spike
- **Go/No-Go**: Fail fast at gates; don't push forward with flawed foundations

**Next Step**: Start Phase 0 with setup checklist from execution-checklist.md
