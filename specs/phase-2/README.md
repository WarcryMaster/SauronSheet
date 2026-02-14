# Phase 2: Core Data Model & Domain Entities

**Quick Start**: Read [SPEC.md](./SPEC.md)

## Phase 2 at a Glance

| Item | Value |
|------|-------|
| Duration | 2-3 weeks |
| Depends on | Phase 0 + Phase 1 |
| Goal | Category, Transaction, Budget entities |
| Tests | 20 unit tests (T02-001 to T02-020) |
| Blocks | Phase 3 (PDF Import) |

## Key Entities

- **Category**: User-created or system default (Groceries, Transport, Utilities, Other)
- **Transaction**: Amount + date + category + description
- **Budget**: Monthly limit per category
- **Money**: Value object for currency + amount

## Start Here

1. Read [SPEC.md](./SPEC.md)
2. Create Category, Transaction, Budget entities
3. Create Money value object
4. Implement repositories (Supabase)
5. Write 20 tests
6. Create UI pages

## Exit Criteria

```bash
✅ dotnet test         # 20/20 Phase 2 tests pass
✅ Phase 0 + Phase 1 tests still pass  # 11 + 8 = 19
✅ Total: 39 tests passing
✅ Categories, Transactions, Budgets in Supabase
✅ Money value object in use
```
