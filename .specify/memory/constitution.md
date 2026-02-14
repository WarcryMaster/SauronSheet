# SauronSheet Constitution

## Core Principles

### I. Clean Architecture & Layered Dependencies
Strict adherence to Clean Architecture patterns with unidirectional dependency flow.
All layers isolated by responsibility: Frontend consumes Application layer; Application orchestrates
Domain logic; Infrastructure implements domain contracts; Domain modules contain zero external
dependencies. **MUST NOT violate:** Domain refs Infrastructure/Frontend; Application directly uses
Supabase; upward-layer calls to abstractions. Dependency rule violations are treated as critical
issues requiring immediate remediation.

### II. CQRS + MediatR Pattern
All use cases expressed as Commands (state-changing) or Queries (read-only) dispatched through
MediatR pipeline. Commands return operation results; Queries return data objects (DTOs). Each
Command/Query has single handler with focused, testable responsibility. No direct service calls
bypass the mediator pattern. Side effects permitted only in Command handlers; Queries remain
idempotent and side-effect free.

### III. Domain-Driven Design (DDD)
Core business logic encapsulated in Domain entities and value objects. Entities maintain invariants
and state transitions; value objects enforce immutability and business constraints. Domain services
coordinate multi-entity logic when needed. Repositories abstract persistence; specifications define
queries using domain language, not SQL. Domain layer expresses business rules in compiler-enforced
patterns (strong typing, sealed classes where invariants apply).

### IV. Test-First Development (NON-NEGOTIABLE)
All features begin with written tests before implementation code. Testing pyramid enforced: Unit
tests (Domain entities, value objects); Integration tests (Application handlers with mocked repos);
End-to-end validation (API  Database). Minimum test coverage: 80% for Domain, 70% for Application.
Failing tests prove spec compliance before refactoring. No untested paths merged to main branch.
Tests serve as executable specification and regression prevention.

### V. Spec-Driven Development
Development lifecycle follows: (1) Write test spec capturing desired behavior; (2) Create MediatR
handler/query stub; (3) Define Domain entities meeting handler requirements; (4) Implement
Infrastructure persistence; (5) Wire Frontend UI. Specifications live in tests; implementation
follows from proven specs. Complex features require feature-level documentation in Application
layer describing handler orchestration before coding.

## Technology Stack & Standards

- **Runtime:** .NET Core 10+ (LTS versions only)
- **Architecture:** MediatR 12+ for CQRS; Clean Architecture with explicit layer separation
- **Frontend:** Razor Pages + Vanilla JavaScript + Tailwind CSS (utility-first styling only)
- **Database:** Supabase PostgreSQL with infrastructure-layer repository pattern
- **Authentication:** Supabase Auth (JWT tokens); multi-tenancy enforced at query level
- **Testing:** xUnit for unit tests; Moq for mocking; In-memory databases for integration tests
- **Code Style:** C# nullable reference types enabled; enforce StyleCop rules; LINQ preferred over loops

## Development Workflow

1. **Feature Planning:** Create spec in Application layer describing Commands/Queries + expected behavior
2. **Red-Green-Refactor:** Write failing test  implement handler  refactor for clarity
3. **Code Review:** PR must include passing tests, architecture diagram if complex, handler/query docs
4. **Deployment:** Only green builds merge; Infrastructure changes require Supabase migration validation
5. **Documentation:** Update copilot-instructions.md for new patterns; add code examples to features

## Critical Compliance Rules

- All PRs verified against dependency rules (no upward refs, no Infrastructure in Domain/Application)
- New Commands/Queries require MediatR handler + integration test demonstrating orchestration
- Domain entity changes must pass all existing unit tests before refactor approval
- Infrastructure persistence layer changes require Supabase migration scripts in source control
- Authentication: Every query scoped to requesting user's tenant context (enforced in handler, not UI)
- Performance: Queries limited to 1000 rows by default; pagination required for larger datasets

## Governance

**Amendment Process:**
1. Violation detected  Issue created documenting architectural gap
2. Fix proposed with test coverage and rationale
3. Review ensures no cascading violations (e.g., adding new layers)
4. Amend constitution if root principle changed; otherwise fix implementation
5. Commit includes "docs: constitution amendment vX.Y.Z" with change summary

**Versioning:**
- MAJOR: Principle removal or redefinition (rare; signals architecture rethink)
- MINOR: New principle or section added; enforcement changes
- PATCH: Clarifications, examples, workflow adjustments (no principle changes)

**Compliance Review:**
- Sprint retro: architect reviews PRs vs. principles; reports violations
- Quarterly: Full codebase audit using dependency analyzer; document exceptions with rationale
- Use .github/copilot-instructions.md as runtime reference; constitution is governance source

---

**Version**: 1.0.0 | **Ratified**: 2026-02-14 | **Last Amended**: 2026-02-14
