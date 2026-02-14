# Phase 0: Foundation & Infrastructure Setup

**Quick Start**: Read [SPEC.md](./SPEC.md)

## What's in This Folder

- **SPEC.md** - Complete specification for Phase 0 (all requirements, tests, deliverables)
- **README.md** - This file

## Phase 0 at a Glance

| Item | Value |
|------|-------|
| Duration | 2-3 weeks |
| Goal | Establish 4-layer architecture + MediatR CQRS + CI/CD |
| Tests | 11 unit/integration tests (T00-001 to T00-011) |
| Success | `dotnet build` (0 warnings) + `dotnet test` (11/11 pass) |
| Blocks | All future phases |

## Start Here

1. Print [SPEC.md](./SPEC.md) - Full specification
2. Follow [execution-checklist.md](../../.specify/memory/execution-checklist.md#phase-0) for step-by-step tasks
3. Write tests **BEFORE** code (Test-First Development)
4. Commit when all 11 tests pass

## Key Outputs

- ✅ 4-layer solution structure (Domain, Application, Infrastructure, Frontend)
- ✅ MediatR CQRS pattern configured
- ✅ Exception hierarchy (DomainException + subclasses)
- ✅ ScopedQueryBehavior for multi-tenancy enforcement
- ✅ Example Command/Query patterns
- ✅ GitHub Actions CI/CD pipeline
- ✅ 11 passing tests

## Exit Criteria

Phase 0 is **COMPLETE** when:

```bash
✅ dotnet build        # 0 warnings
✅ dotnet test         # 11/11 tests pass
✅ GitHub Actions      # Pipeline green
✅ Commit made         # "feat: phase 0 foundation setup complete"
```
