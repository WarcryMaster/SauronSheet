# SauronSheet Constitution

## Core Principles

### I. Clean Architecture & Layered Dependencies

Strict adherence to Clean Architecture patterns with unidirectional dependency flow.
All layers isolated by responsibility:

- **Frontend** consumes Application layer via MediatR commands/queries.
- **Application** orchestrates Domain logic; depends only on Domain abstractions.
- **Infrastructure** implements Domain contracts (repository interfaces, external services).
- **Domain** contains zero external dependencies — no references to any other layer.

**Dependency Rules (NON-NEGOTIABLE):**
- Frontend → Application → Domain
- Infrastructure → Domain (implementations only)
- Domain MUST NOT reference Infrastructure, Application, or Frontend.
- Application MUST NOT reference Infrastructure or Frontend directly.
- Application accesses Infrastructure only via Domain-defined interfaces (e.g., `ITransactionRepository`).
- Dependency rule violations are treated as CRITICAL issues requiring immediate remediation.

**Layer Responsibilities:**

| Layer        | Responsibility                                         | Depends On   |
|-------------|--------------------------------------------------------|--------------|
| Frontend    | UI rendering, user input, page routing                 | Application  |
| Application | Use case orchestration, CQRS handlers, DTOs, pipeline behaviors | Domain       |
| Domain      | Entities, value objects, domain services, specifications, repository interfaces | Nothing      |
| Infrastructure | Supabase persistence, auth service, PDF parsing, external integrations | Domain       |

### II. CQRS + MediatR Pattern

All use cases expressed as Commands (state-changing) or Queries (read-only) dispatched through
MediatR pipeline.

**Commands:**
- Represent state-changing operations (create, update, delete, import).
- Return operation results (success/failure with typed response).
- Side effects permitted only in Command handlers.
- Each Command has a single handler with focused, testable responsibility.

**Queries:**
- Represent read-only data retrieval operations.
- Return DTOs (Data Transfer Objects), never domain entities directly.
- MUST be idempotent and side-effect free.
- Each Query has a single handler with focused responsibility.

**Pipeline Rules:**
- No direct service calls bypass the mediator pattern.
- All Commands/Queries routed through `IMediator.Send()`.
- Pipeline behaviors (validation, tenant scoping, logging) applied uniformly.
- New Commands/Queries MUST have a corresponding MediatR handler + integration test.

### III. Domain-Driven Design (DDD)

Core business logic encapsulated in Domain entities, value objects, and domain services.

**Entities (Aggregate Roots):**
- Maintain invariants and prevent invalid states at construction time.
- Created via parameterized constructors only — no public setters.
- Private setters permitted only for ORM/serialization compatibility.
- Each aggregate root owns its lifecycle and consistency boundary.
- Throw `DomainException` (or subclasses) when invariants are violated.

**Value Objects:**
- Enforce immutability and business constraints on construction.
- Value-based equality (two instances with same properties are equal).
- **Strong-typed IDs MUST be used** for entity identifiers (`TransactionId`, `UserId`, `CategoryId`)
  to prevent accidental ID mixing at compile time.
- Business value objects (e.g., `Money`, `DateRange`) encapsulate validation and arithmetic.

**Domain Services:**
- Coordinate multi-entity logic that does not belong to a single aggregate root.
- Used for cross-entity validation (e.g., name uniqueness across entities via repository interface).
- Domain services depend only on Domain interfaces — never on Infrastructure directly.

**System Defaults Pattern:**
- Immutable domain values defined as system defaults (e.g., default categories).
- Marked with a boolean flag (e.g., `IsSystemDefault`).
- Guard methods (e.g., `CanDelete()`) prevent modification or deletion of system defaults.

**Repository Interfaces:**
- Defined in Domain layer as contracts (`ITransactionRepository`, `ICategoryRepository`, etc.).
- Implementations live in Infrastructure layer only.
- Specifications define queries using domain language, not SQL.

**Specifications:**
- All specifications inherit from `ISpecification<T>` with `MaxResults = 1000` default.
- Pagination required for datasets exceeding default limit.
- Specifications express filtering in domain language (by date range, by category, by amount).

### IV. Test-First Development (NON-NEGOTIABLE)

All features begin with written tests before implementation code.

**Testing Pyramid:**

| Level            | Scope                                 | Tools                |
|------------------|---------------------------------------|----------------------|
| Unit Tests       | Domain entities, value objects, domain services | xUnit + Moq         |
| Integration Tests| Application handlers with mocked repositories | xUnit + Moq + in-memory doubles |
| End-to-End       | API → Database round-trip validation   | xUnit + test Supabase instance |

**Coverage Requirements:**

| Scope                        | Minimum Coverage | Notes                                 |
|------------------------------|------------------|---------------------------------------|
| Domain Layer (global)        | 80%              | Constitution minimum across all phases |
| Domain Layer (domain-only phases) | 100%         | When phase scope is Domain-only (e.g., Phase 2) |
| Application Layer            | 70%              | Handlers, pipeline behaviors, DTOs     |

**Enforcement Rules:**
- Failing tests MUST prove spec compliance before refactoring.
- No untested code paths merged to main branch.
- Tests serve as executable specification and regression prevention.
- Red-Green-Refactor cycle: write failing test → implement → refactor for clarity.
- Domain service tests MUST mock repository interfaces (not real databases).

### V. Spec-Driven Development

Development lifecycle follows a strict sequence for every feature:

1. **Write Test Spec** — Capture desired behavior in test code (TDD red phase).
2. **Create MediatR Handler/Query Stub** — Define the command/query contract.
3. **Define Domain Entities** — Build entities and value objects meeting handler requirements.
4. **Implement Infrastructure Persistence** — Add repository implementations.
5. **Wire Frontend UI** — Create Razor Pages to trigger operations.

**Specification Rules:**
- Specifications live in tests; implementation follows from proven specs.
- Complex features require feature-level documentation in Application layer
  describing handler orchestration before coding.
- **Single-file rule**: ALL phase specifications MUST be in a single file (`specs/phase-X-spec.md`).
  No `.clarification-*`, `.decisions-*`, or `.resolved-*` files.

**Phase Scope Boundaries:**
- Each phase spec MUST explicitly declare which layers are in scope.
- Deliverables MUST NOT include items from out-of-scope layers.
- If a phase is "Domain Layer ONLY", no Application commands/queries, Infrastructure
  implementations, or database migrations are permitted as deliverables.
- Out-of-scope items MUST be documented as deferred with target phase reference.

## Technology Stack & Standards

- **Runtime:** .NET Core 10+ (LTS versions only)
- **Architecture:** MediatR 12+ for CQRS; Clean Architecture with explicit layer separation
- **Frontend:** Razor Pages + Vanilla JavaScript + Material Design (CDN components only)
- **Database:** Supabase PostgreSQL with infrastructure-layer repository pattern
- **Authentication:** Supabase Auth (JWT tokens); multi-tenancy enforced at query level
- **Testing:** xUnit for unit tests; Moq for mocking; in-memory databases for integration tests
- **Code Style:** C# nullable reference types enabled; enforce StyleCop rules; LINQ preferred over loops


### UI & Librerías Externas

- **MDBootstrap (Material Design for Bootstrap, CDN):** Todos los componentes y estilos de la interfaz deben implementarse usando MDBootstrap (Material Design for Bootstrap) por CDN. No se permite Tailwind, Alpine.js, Chart.js ni librerías locales o por npm. La política de CDN es obligatoria y debe reflejarse en _Layout.cshtml y la documentación.

### Domain Patterns Reference

| Pattern         | Convention                                 | Example                       |
|----------------|--------------------------------------------|-------------------------------|
| Aggregate Root | Base class with Id, CreatedAt; parameterized constructor | `Transaction`, `Category`, `Budget` |
| Value Object   | Immutable; value-based equality; validated on construction | `Money`, `DateRange`, `TransactionId` |
| Strong-Typed ID| Wrapper around Guid or string; prevents ID type mixing | `TransactionId(Guid)`, `UserId(string)` |
| Domain Service | Cross-entity logic; depends on repository interfaces | `CategoryService`               |
| Specification  | Filtering with domain language; MaxResults default 1000 | `TransactionByDateRangeSpecification` |
| Domain Exception| Thrown on invariant violation; caught in Application layer | `DomainException`, `EntityNotFoundException` |
| Guard Method   | Returns bool to prevent invalid operations  | `Category.CanDelete()`          |
| System Default | Immutable seeded values; flagged with boolean property | `Category.IsSystemDefault`      |

## Development Workflow

1. **Feature Planning:** Create spec in `specs/phase-X-spec.md` describing scope boundaries,
   deliverables, Commands/Queries, expected behavior, and test specifications.
2. **Red-Green-Refactor:** Write failing test → implement handler/entity → refactor for clarity.
3. **Code Review:** PR must include passing tests, architecture diagram if complex, handler/query docs.
4. **Deployment:** Only green builds merge; Infrastructure changes require Supabase migration validation.
5. **Documentation:** Update `copilot-instructions.md` for new patterns; add code examples to features.

### Phase Scope Boundaries

Each phase MUST declare its layer scope explicitly in the spec file:

| Phase Type   | Layers In Scope         | Deliverables Allowed                                 |
|--------------|------------------------|------------------------------------------------------|
| Foundation   | All layers             | Solution structure, base abstractions, CI/CD         |
| Domain-Only  | Domain                 | Entities, value objects, services, specifications, repository interfaces, tests |
| Full-Stack   | All layers             | Commands, queries, handlers, DTOs, infrastructure implementations, UI, migrations |
| Polish       | Frontend + Infrastructure | UI refinements, performance, deployment, security    |

**Rule:** If a phase spec says "Domain Layer ONLY", any Application/Infrastructure deliverable
in that phase is a constitution violation requiring immediate correction.

## Critical Compliance Rules

- All PRs verified against dependency rules (no upward refs, no Infrastructure in Domain/Application).
- New Commands/Queries require MediatR handler + integration test demonstrating orchestration.
- Domain entity changes must pass all existing unit tests before refactor approval.
- Infrastructure persistence layer changes require Supabase migration scripts in source control.
- Authentication: Every query scoped to requesting user's tenant context (enforced in handler, not UI).
- Performance: Queries limited to 1000 rows by default; pagination required for larger datasets.
- Strong-typed IDs: All entity identifiers MUST use value object wrappers — raw Guid/string usage
  for entity IDs is a compliance violation.
- Phase scope: Deliverables MUST NOT cross declared layer boundaries for the phase.

## Governance

**Amendment Process:**
1. Violation detected → Issue created documenting architectural gap.
2. Fix proposed with test coverage and rationale.
3. Review ensures no cascading violations (e.g., adding new layers).
4. Amend constitution if root principle changed; otherwise fix implementation.
5. Commit includes `docs: constitution amendment vX.Y.Z` with change summary.

**Versioning:**
- MAJOR: Principle removal or redefinition (rare; signals architecture rethink).
- MINOR: New principle or section added; enforcement changes; expanded guidance.
- PATCH: Clarifications, examples, workflow adjustments (no principle changes).

**Compliance Review:**
- Sprint retro: architect reviews PRs vs. principles; reports violations.
- Quarterly: Full codebase audit using dependency analyzer; document exceptions with rationale.
- Use `.github/copilot-instructions.md` as runtime reference; constitution is governance source.

---

**Version**: 1.1.0 | **Ratified**: 2026-02-14 | **Last Amended**: 2026-02-15