# Phase 0: Research & Clarifications

**Feature**: 002-category-management | **Date**: March 7, 2026 | **Status**: ✅ COMPLETE

---

## Summary

All technical uncertainties resolved through dedicated clarification session. 5 critical architectural decisions finalized with documented rationale. **Ready to proceed to Phase 1 Design (completed) and Phase 2 Task Generation.**

---

## Research Completion Status

### Initialization Research: SKIPPED ✅

**Rationale**: Feature 2 specification already comprehensive with detailed requirements, 24 system categories defined, 5 user stories, acceptance criteria, and architecture patterns specified. No "NEEDS CLARIFICATION" markers requiring foundational research.

### Technology Validation: SKIPPED ✅

**Rationale**: SauronSheet tech stack already established across Phases 0-4:
- .NET 10 + MediatR 12+ (proven in Production via Phase 4)
- Supabase PostgreSQL (production-grade, in use since Phase 1)
- xUnit + Moq testing framework (standard across domain tests)
- Clean Architecture + CQRS patterns (established in Phase 0 Foundation, enforced by Constitution v1.1.0)

**No new technologies required for Category Management**; feature uses proven stack.

### Clarification Session: COMPLETE ✅

**5 Critical Decisions Resolved** (March 7, 2026):

1. **Validation Architecture**
   - Question: Where should validation live? (Domain-only / Application-only / Hybrid)
   - Decision: D - **Hybrid Pattern** (selected from 4 options)
   - Rationale: Domain ValueObjects (CategoryName, ColorHex) enforce invariants; Domain Service (CategoryService) handles cross-entity rules; Application handlers orchestrate; Frontend provides UX feedback (defense-in-depth)
   - **Status**: ✅ RESOLVED — Integrated into spec.md Critical Decisions section
   - **Implementation Impact**: Requires 3 ValueObjects, 1 Domain Service, 5 MediatR handlers with validation orchestration

2. **CQRS Handler Count**
   - Question: How many separate MediatR handlers? (2 / 4 / 5 / 7)
   - Decision: B - **5 Handlers** (selected from 4 options)
   - Rationale: Granular responsibility per CQRS principle:
     - GetAllCategoriesQuery + Handler
     - SearchCategoriesQuery + Handler
     - CreateCategoryCommand + Handler
     - UpdateCategoryCommand + Handler
     - DeleteCategoryCommand + Handler
   - **Status**: ✅ RESOLVED — All 5 handlers specified in Architecture section
   - **Implementation Impact**: Requires 5 separate handler classes + 5 integration tests

3. **Delete Guard Logic Pattern**
   - Question: Where does delete transaction guard logic belong? (Domain Service / Application Handler / Both)
   - Decision: B+C - **Hybrid (Application query + Domain Service guard)**
   - Rationale: Efficiency + Invariant Maintenance:
     - Application handler executes EXISTS query against transactions table (efficient SQL check)
     - Passes boolean result to Domain Service CanDeleteCategory()
     - Service maintains delete guard logic (including IsSystemDefault check)
     - Hybrid approach prevents orphaned category records while avoiding N+1 queries
   - **Status**: ✅ RESOLVED — Documented in Critical Decisions + Architecture DeleteCategoryCommandHandler section
   - **Implementation Impact**: Requires repository ITrasactionRepository.GetCountAsync(categoryId); CategoryService.CanDeleteCategory(category, bool hasTransactions)

4. **24 System Category Seeding Strategy**
   - Question: How to seed 24 immutable system defaults? (Application command / SQL migration / Database seed script / GUI wizard)
   - Decision: A - **SQL Migration** (selected from 4 options)
   - Rationale: Constitution adherence + Infrastructure isolation:
     - File-based SQL migration (20260307_SeedSystemDefaultCategories.sql) in source control
     - Idempotent logic (IF NOT EXISTS) prevents duplicate inserts on redeploy
     - No Application-level seed command required
     - Database handles initialization automatically on first deployment
     - Follows Phase 0 Infrastructure setup patterns
   - **Status**: ✅ RESOLVED — Migration file path specified in plan.md Infrastructure section
   - **Implementation Impact**: Requires 1 SQL migration file with 24 INSERT statements + 1 categories table CREATE

5. **Color Hex Validation: Single vs. Layered**
   - Question: Where to validate color hex format? (Frontend HTML5 picker only / Domain ValueObject only / Both)
   - Decision: C - **Defense-in-Depth (Frontend + Domain)** (selected from 3 options)
   - Rationale: User experience + Invariant enforcement:
     - Frontend HTML5 color picker (`<input type="color">`) provides immediate visual feedback, prevents invalid hex entry
     - Domain ValueObject ColorHex regex `#[0-9A-F]{6}` enforces invariant at persistence
     - Both layers validate independently (defense-in-depth security principle)
     - If frontend bypassed, Domain still prevents invalid data
   - **Status**: ✅ RESOLVED — Implemented in spec.md Key Entities ColorHex definition
   - **Implementation Impact**: Requires HTML5 input in frontend + regex validation in Domain ValueObject

---

## Design Pattern Confirmations

✅ **Strong-Typed IDs**: CategoryId(Guid) prevents ID mixing at compile time — Constitution-compliant (Principle III.DDD)

✅ **ValueObjects with Validation**: CategoryName, ColorHex enforce constraints at construction — Constitution-compliant (Principle III.DDD)

✅ **Domain Service Pattern**: CategoryService(ICategoryRepository) coordinates multi-entity logic — Constitution-compliant (Principle III.DDD)

✅ **Guard Methods**: Category.CanDelete(bool) prevents invalid operations — Constitution-compliant (Principle III.DDD)

✅ **System Defaults Pattern**: IsSystemDefault flag + guard protection — Constitution-compliant (Principle III.DDD)

✅ **CQRS Routing**: All 5 operations via MediatR pipeline — Constitution-compliant (Principle II)

✅ **Repository Pattern**: ICategoryRepository in Domain, implementation in Infrastructure — Constitution-compliant (Principle I)

✅ **Clean Architecture Layering**: No upward dependencies — Constitution-compliant (Principle I)

---

## Dependencies & Risks

### Resolved Risks

| Risk | Mitigation | Status |
|------|-----------|--------|
| Multiple validation layers (Frontend/Domain/DB) could diverge | Centralize rules in Domain; Frontend mirrors Domain; DB constraints redundant | ✅ Mitigated by defense-in-depth architecture |
| Transaction count query performance impact on delete | Use EXISTS query (efficient); database index on transaction.category_id | ✅ Index specified in migration |
| System category immutability enforcement | IsSystemDefault flag + CanDelete() guard + database trigger protection | ✅ Triple-layer protection |
| User isolation (tenant scoping) leaks | All queries filtered by UserId; enforced in handlers + repository | ✅ Enforced at 2 layers (handler + DB) |

### No Blocking Dependencies

- ✅ User authentication already implemented (Phase 1)
- ✅ MediatR pipeline configured (Phase 0)
- ✅ Supabase database ready (Phase 1)
- ✅ Bootstrap icon library available (Phase 4)
- ✅ Transaction entity defined (Phase 3)

---

## Next Phase: Execution Planning

**Phase 2: Task Generation** — Run `/speckit.tasks` to generate `tasks.md` with:
- Dependency-ordered task breakdown (~18-22 tasks)
- Effort estimates per task
- Acceptance criteria per task
- Team assignment recommendations

**Recommended Execution**:
1. Domain layer (Entities + ValueObjects + Services) + Unit tests (5-6 tasks)
2. Infrastructure layer (Repository + Migration) + Integration tests (2-3 tasks)
3. Application layer (Handlers + DTOs) + Integration tests (4-5 tasks)
4. Frontend layer (Pages + Forms + Validation) + E2E tests (3-4 tasks)

**Expected Timeline**: ~2-3 weeks (assuming standard velocity of 8-10 story points/week)

---

_Last Updated: March 7, 2026 | Phase: 0 (Research) Status: Complete | Next: Phase 2 Task Generation_

