---
description: "Use when designing domain entities, value objects, strong-typed IDs, domain services, specifications, or domain exceptions. Quick reference for DDD building blocks used in this project."
---

# Domain Patterns Quick Reference

| Pattern | Convention | Example |
|---|---|---|
| Aggregate Root | Base class; parameterized constructor; no public setters | Transaction, Category, Budget |
| Value Object | Immutable; value-based equality; validated on construction | Money, DateRange |
| Strong-Typed ID | Wrapper around Guid/string; prevents ID mixing at compile time | TransactionId(Guid), UserId(string) |
| Domain Service | Cross-entity logic; depends on repository interfaces only | CategoryService |
| Specification | Filtering with domain language; MaxResults default 1000 | TransactionByDateRangeSpecification |
| Domain Exception | Thrown on invariant violation; caught in Application layer | DomainException |
| Guard Method | Returns bool to prevent invalid operations | Category.CanDelete() |
| System Default | Immutable seeded values; flagged with boolean property | Category.IsSystemDefault |
