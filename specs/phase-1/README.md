# Phase 1: Authentication & Multi-Tenancy Foundation

**Quick Start**: Read [SPEC.md](./SPEC.md)

## Phase 1 at a Glance

| Item | Value |
|------|-------|
| Duration | 3-4 weeks |
| Depends on | Phase 0 (Foundation) |
| Goal | User auth + multi-tenancy isolation |
| Tests | 8 integration tests (T01-001 to T01-008) |
| Blocks | Phase 2 (Domain Entities) |

## Key Deliverables

- ✅ User registration + login
- ✅ Supabase Auth integration
- ✅ IUserContext implementation
- ✅ JWT token handling
- ✅ Multi-tenancy enforcement (ScopedQueryBehavior active)
- ✅ 8 passing tests

## Start Here

1. Read [SPEC.md](./SPEC.md)
2. Create User entity + Email value object
3. Implement SupabaseUserContext
4. Write 8 tests (T01-001 to T01-008)
5. Create Login/Register pages

## Exit Criteria

```bash
✅ dotnet test         # 8/8 Phase 1 tests pass
✅ Phase 0 tests still pass  # 11/11
✅ Supabase Auth working
✅ Login page functional
```
