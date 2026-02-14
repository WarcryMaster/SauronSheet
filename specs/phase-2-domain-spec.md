# Phase 2: Core Data Model & Domain Entities

**Version**: 1.0.0  
**Status**: Ready for speckit.tasks generation  
**Duration**: 2-3 weeks  
**Depends On**: Phase 1 Complete

---

## Executive Summary

**Objective**: Create the core domain entities + validation rules for expense tracking

**What We Build**:
- Category entity (with user ownership + validation)
- Transaction entity (with calculation methods)
- Budget entity (with overage detection)
- Domain value objects (Money, DateRange)
- Domain specifications (filtering by category, date range, amount)

**Core Domain Rules**:
- Category must have unique name per user
- Transaction amount must be positive
- Budget threshold → warning when exceeded
- All entities immutable once created (only soft deletes)

---

## Entities to Implement

**Category**:
- Id, UserId, Name, Description, Color, CreatedAt
- Methods: CanDelete(), Validate()
- Domain Rules: Name unique per user, Name not empty

**Transaction**:
- Id, UserId, CategoryId, Amount, Description, TransactionDate, CreatedAt
- Methods: IsValid(), GetMonth(), GetYear()
- Domain Rules: Amount > 0, Description not null, Date not future

**Budget**:
- Id, UserId, CategoryId, MonthlyLimit, AlertThreshold, CreatedAt
- Methods: IsOverBudget(), PercentageUsed(), GetRemaining()
- Domain Rules: MonthlyLimit > 0, Threshold 0-100%

**Value Objects**:
- Money (Amount, Currency) with validation
- DateRange (StartDate, EndDate) with ordering

---

## Deliverables (20 items)

**Domain Layer** (10):
- [ ] Category entity + validation
- [ ] Transaction entity + calculation methods
- [ ] Budget entity + threshold logic
- [ ] Money value object
- [ ] DateRange value object
- [ ] CategorySpecification
- [ ] TransactionByDateSpecification
- [ ] TransactionByAmountSpecification
- [ ] CategoryCreatedDomainEvent
- [ ] TransactionCreatedDomainEvent

**Application Layer** (7):
- [ ] CreateCategoryCommand + handler
- [ ] UpdateCategoryCommand + handler
- [ ] CreateTransactionCommand + handler
- [ ] GetTransactionsByMonthQuery + handler
- [ ] GetCategoryBreakdownQuery + handler
- [ ] CategoryDto
- [ ] TransactionDto

**Infrastructure Layer** (3):
- [ ] Category repository implementation
- [ ] Transaction repository implementation
- [ ] Migration: 002_CreateCategoryAndTransactionTables.sql

---

## Test Specifications (20 tests)

Domain tests (12):
- T02-001: Category validates name not empty
- T02-002: Category enforces unique name per user
- T02-003: Transaction requires positive amount
- T02-004: Transaction blocks future dates
- T02-005: Budget calculates percentage correctly
- T02-006: Budget detects overage condition
- T02-007: Money value object comparison works
- T02-008: DateRange validates ordering
- T02-009: Category soft delete works
- T02-010: Transaction queries by month
- T02-011: Specification MaxResults enforced
- T02-012: Domain events published on create

Application tests (8):
- T02-013: CreateCategoryCommand creates + validates
- T02-014: GetTransactionsByMonthQuery filters correctly
- T02-015: GetCategoryBreakdownQuery respects tenant
- T02-016: Category repository CRUD
- T02-017: Transaction repository CRUD
- T02-018: Multiple categories per user work
- T02-019: Transaction filtering by date works
- T02-020: Schema migrations apply correctly

---

## Success Criteria

✅ All 20 tests passing  
✅ Domain entities immutable (no public setters)  
✅ Value objects validated on construct  
✅ Specifications filter correctly  
✅ Multi-tenancy enforced in queries  
✅ Code coverage ≥80%  

---

## Next Phase

Phase 3: Transaction Import (PDF Parsing) - 3-4 weeks
- Implement PDF parsing library integration
- Create ImportTransactionsFromPdfCommand
- Validate + map PDF data to Transaction entities
- Bulk insert + transaction management
