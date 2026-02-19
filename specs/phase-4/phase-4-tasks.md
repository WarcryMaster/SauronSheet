# Tasks: Phase 4 — Analytics & Dashboard (MVP)

**Version**: 1.0.0  
**Created**: 2026-02-19  
**Phase Type**: Full-Stack (Features)  
**Scope**: All Layers (Domain + Application + Frontend)  
**Duration**: Weeks 14–18  
**Expected Tests**: ~186 total (154 Phase 0+1+2+3 + 32 Phase 4)  
**Phase 4 Breakdown**: 7 Domain + 25 Application  
**Input**: `specs/phase-4/phase-4-plan.md`, `specs/phase-4/phase-4-spec.md`

---

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Maps to user scenario from spec.md:
  - **[US1]** = Scenario 4.1 — View Analytics Dashboard (summary cards + recent transactions)
  - **[US2]** = Scenario 4.2 — Spending by Category (pie chart)
  - **[US3]** = Scenario 4.3 — Monthly Spending Trends (line chart)
  - **[US4]** = Scenario 4.4 — Yearly Spending Comparison (bar chart)
  - **[US5]** = Scenario 4.5 — Transaction Search & Filtering
  - **[US6]** = Scenario 4.6 — Transaction Summary Statistics
  - **[US7]** = Scenario 4.7 — Dashboard Responsive Design + Navigation

---

## Phase 1: Setup (Pre-Implementation Validation)

**Purpose**: Verify Phase 0–3 completion and Phase 4 readiness

- [ ] T001 Verificar build limpio ejecutando `dotnet build` — exit code 0, cero warnings
- [ ] T002 Verificar tests previos ejecutando `dotnet test` — ~154 tests pasan (Phase 0: 13 + Phase 1: 22 + Phase 2: 81 + Phase 3: 38)
- [ ] T003 Verificar que Domain tiene CERO dependencias externas revisando `src/SauronSheet.Domain/SauronSheet.Domain.csproj`
- [ ] T004 Verificar que `TransactionByAmountRangeSpecification` existe y sus 6 tests pasan en `tests/SauronSheet.Domain.Tests/Specifications/`
- [ ] T005 Verificar que existe `ISpecification<T>` con `BaseSpecification<T>` en `src/SauronSheet.Domain/Specifications/`
- [ ] T006 [P] Crear directorio `tests/SauronSheet.Domain.Tests/Specifications/` si no existe
- [ ] T007 [P] Crear directorio `src/SauronSheet.Application/Features/Analytics/DTOs/`
- [ ] T008 [P] Crear directorio `src/SauronSheet.Application/Features/Analytics/Queries/`
- [ ] T009 [P] Crear directorio `tests/SauronSheet.Application.Tests/Features/Analytics/Queries/`
- [ ] T010 [P] Crear directorio `src/SauronSheet.Frontend/Shared/Components/`
- [ ] T011 [P] Crear directorio `src/SauronSheet.Frontend/wwwroot/js/`

**Checkpoint**: Build limpio, ~154 tests green, directorios creados → Proceder a Phase 2

---

## Phase 2: Foundational — Domain Specifications (Blocks all user stories)

**Purpose**: New domain specifications required by ALL analytics queries and search

**⚠️ CRITICAL**: CompositeSpecification<T> y TransactionByDescriptionKeywordSpecification son prerequisito para TODOS los handlers de Application

### Tests Domain (RED Phase)

- [ ] T012 [P] Crear test stubs para TransactionByDescriptionKeywordSpecification (4 tests RED) en `tests/SauronSheet.Domain.Tests/Specifications/TransactionByDescriptionKeywordSpecificationTests.cs`
  - **Acción**: Crear archivo
  - **Descripción**: Crear clase `TransactionByDescriptionKeywordSpecificationTests` con 4 métodos `[Fact]` y `[Trait("Category", "Domain")]` que contengan `Assert.True(false, "Implement ...")`:
    - `DescriptionKeywordSpec_MatchesPartialKeyword()` — Transaction desc="Morning Coffee at Starbucks", keyword="coffee" → true
    - `DescriptionKeywordSpec_NoMatch_ReturnsFalse()` — desc="Grocery shopping", keyword="coffee" → false
    - `DescriptionKeywordSpec_EmptyKeyword_ThrowsDomainException()` — keyword="" → throws DomainException
    - `DescriptionKeywordSpec_CaseInsensitive()` — desc="COFFEE BEANS", keyword="coffee" → true
  - **Dependencias**: T005 (BaseSpecification<T> exists)
  - **Validación**: Archivo compila, 4 tests descubiertos, todos FALLAN (RED)

- [ ] T013 [P] Crear test stubs para CompositeSpecification<T> (3 tests RED) en `tests/SauronSheet.Domain.Tests/Specifications/CompositeSpecificationTests.cs`
  - **Acción**: Crear archivo
  - **Descripción**: Crear clase `CompositeSpecificationTests` con 3 métodos `[Fact]` y `[Trait("Category", "Domain")]` con `Assert.True(false, "Implement ...")`:
    - `CompositeSpec_And_CombinesTwoSpecs()` — User + Category specs, matching transaction → true
    - `CompositeSpec_And_RejectsMismatch()` — User matches, category doesn't → false
    - `CompositeSpec_And_MultipleSpecs()` — Triple composition: user + dateRange + category, partial match → false
  - **Dependencias**: T005 (BaseSpecification<T> exists)
  - **Validación**: Archivo compila, 3 tests descubiertos, todos FALLAN (RED)

- [ ] T014 Ejecutar `dotnet test --filter Category=Domain` — verificar 7 tests nuevos FALLAN (RED), ~30 tests previos PASAN
  - **Dependencias**: T012, T013

### Implementación Domain (GREEN Phase)

- [ ] T015 Implementar TransactionByDescriptionKeywordSpecification en `src/SauronSheet.Domain/Specifications/TransactionByDescriptionKeywordSpecification.cs`
  - **Acción**: Crear archivo
  - **Descripción**: Crear clase `TransactionByDescriptionKeywordSpecification` que hereda de `BaseSpecification<Transaction>`. Constructor recibe `string keyword`:
    - Validar que keyword no es null/empty/whitespace → lanzar `DomainException("Search keyword cannot be empty.")`
    - Criteria: `t => t.Description.ToLower().Contains(keyword.ToLower())` (case-insensitive partial match)
    - Namespace: `SauronSheet.Domain.Specifications`
  - **Dependencias**: T012 (tests exist in RED)
  - **Validación**: `dotnet test --filter ClassName~TransactionByDescriptionKeyword` — 4 tests PASAN

- [ ] T016 Implementar CompositeSpecification<T> en `src/SauronSheet.Domain/Specifications/CompositeSpecification.cs`
  - **Acción**: Crear archivo
  - **Descripción**: Crear clase `CompositeSpecification<T> : BaseSpecification<T> where T : class`. Constructor privado con `Expression<Func<T, bool>> criteria`. Método estático `And(ISpecification<T> left, ISpecification<T> right)`:
    - Crear `Expression.Parameter(typeof(T), "x")`
    - Combinar con `Expression.AndAlso(Expression.Invoke(left.Criteria, parameter), Expression.Invoke(right.Criteria, parameter))`
    - Retornar `new CompositeSpecification<T>(lambda)`
    - Namespace: `SauronSheet.Domain.Specifications`
  - **Dependencias**: T013 (tests exist in RED)
  - **Validación**: `dotnet test --filter ClassName~CompositeSpecification` — 3 tests PASAN

- [ ] T017 Ejecutar `dotnet test --filter Category=Domain` — ~37 tests PASAN (30 previos + 7 Phase 4)
  - **Dependencias**: T015, T016

**Checkpoint**: Domain complete — 7 tests nuevos GREEN, ~37 domain tests total → Proceder a user stories

---

## Phase 3: User Story 6 — Transaction Summary Statistics (Priority: P1) 🎯 MVP

**Goal**: Calcular totales (income, expenses, net, count) para un rango de fechas  
**Independent Test**: Ejecutar `dotnet test --filter ClassName~GetTransactionSummaryQuery` — 4 tests pasan  
**Scenario**: 4.6

### Tests (RED Phase)

- [ ] T018 [US6] Crear test stubs para GetTransactionSummaryQuery (4 tests RED) en `tests/SauronSheet.Application.Tests/Features/Analytics/Queries/GetTransactionSummaryQueryTests.cs`
  - **Acción**: Crear archivo
  - **Descripción**: Clase `GetTransactionSummaryQueryTests` con mock de `ITransactionRepository`, `IUserContext`. 4 métodos `[Fact]` + `[Trait("Category", "Application")]`:
    - `GetTransactionSummary_CalculatesCorrectly()` — +€500, +€200 income, -€300, -€100, -€50 expenses → TotalIncome=700, TotalExpenses=450, NetAmount=250, Count=5
    - `GetTransactionSummary_NoTransactions_ReturnsZeros()` — Empty → all zeros
    - `GetTransactionSummary_OnlyExpenses_NetIsNegative()` — -€300, -€200 → TotalIncome=0, TotalExpenses=500, NetAmount=-500
    - `GetTransactionSummary_RespectsDateRange()` — Jan €100 + Feb €200, range=Jan only → TotalExpenses=100, Count=1
  - **Dependencias**: T017 (domain specs ready)
  - **Validación**: 4 tests descubiertos, todos FALLAN (RED)

### Implementación (GREEN Phase)

- [ ] T019 [P] [US6] Crear TransactionSummaryDto en `src/SauronSheet.Application/Features/Analytics/DTOs/TransactionSummaryDto.cs`
  - **Acción**: Crear archivo
  - **Descripción**: `public record TransactionSummaryDto(decimal TotalIncome, decimal TotalExpenses, decimal NetAmount, int TransactionCount, string Currency, DateTime FromDate, DateTime ToDate);`
  - **Dependencias**: T007 (directorio Analytics/DTOs existe)
  - **Validación**: `dotnet build` exit code 0

- [ ] T020 [US6] Crear GetTransactionSummaryQuery en `src/SauronSheet.Application/Features/Analytics/Queries/GetTransactionSummaryQuery.cs`
  - **Acción**: Crear archivo
  - **Descripción**: `public record GetTransactionSummaryQuery(DateTime FromDate, DateTime ToDate) : IRequest<TransactionSummaryDto>;`
  - **Dependencias**: T019
  - **Validación**: `dotnet build` exit code 0

- [ ] T021 [US6] Crear GetTransactionSummaryQueryHandler en `src/SauronSheet.Application/Features/Analytics/Queries/GetTransactionSummaryQueryHandler.cs`
  - **Acción**: Crear archivo
  - **Descripción**: Handler que implementa `IRequestHandler<GetTransactionSummaryQuery, TransactionSummaryDto>`. Inyectar `ITransactionRepository`, `IUserContext`. Flow:
    1. Obtener UserId de IUserContext
    2. Componer specs: `TransactionByUserSpecification` + `TransactionByDateRangeSpecification` vía `CompositeSpecification.And()`
    3. Ejecutar `FindBySpecificationAsync(composedSpec)`
    4. Calcular TotalIncome (sum where Amount.IsPositive), TotalExpenses (sum Math.Abs where Amount.IsNegative), NetAmount (income - expenses), Count
    5. Retornar `TransactionSummaryDto`
  - **Dependencias**: T018 (tests en RED), T020
  - **Validación**: `dotnet test --filter ClassName~GetTransactionSummaryQuery` — 4 tests PASAN

**Checkpoint**: US6 complete — Resumen de transacciones funcional

---

## Phase 4: User Story 2 — Spending by Category (Priority: P1) 🎯 MVP

**Goal**: Breakdown de gastos por categoría con porcentajes, agrupación "Other" si >6 categorías  
**Independent Test**: Ejecutar `dotnet test --filter ClassName~GetSpendingByCategoryQuery` — 4 tests pasan  
**Scenario**: 4.2

### Tests (RED Phase)

- [ ] T022 [US2] Crear test stubs para GetSpendingByCategoryQuery (4 tests RED) en `tests/SauronSheet.Application.Tests/Features/Analytics/Queries/GetSpendingByCategoryQueryTests.cs`
  - **Acción**: Crear archivo
  - **Descripción**: Clase `GetSpendingByCategoryQueryTests` con mock de `ITransactionRepository`, `ICategoryRepository`, `IUserContext`. 4 métodos `[Fact]` + `[Trait("Category", "Application")]`:
    - `GetSpendingByCategory_WithTransactions_ReturnsGroupedData()` — 5 transacciones en 3 categorías → 3 entries con amounts y percentages correctos
    - `GetSpendingByCategory_NoTransactions_ReturnsEmptyList()` — Sin transacciones → lista vacía
    - `GetSpendingByCategory_OnlyIncomeTransactions_ReturnsEmptyList()` — Solo income → lista vacía (solo expenses)
    - `GetSpendingByCategory_MoreThanSixCategories_GroupsIntoOther()` — 8 categorías → 7 entries (top 6 + "Other")
  - **Dependencias**: T017 (domain specs ready)
  - **Validación**: 4 tests descubiertos, todos FALLAN (RED)

### Implementación (GREEN Phase)

- [ ] T023 [P] [US2] Crear CategorySpendingDto en `src/SauronSheet.Application/Features/Analytics/DTOs/CategorySpendingDto.cs`
  - **Acción**: Crear archivo
  - **Descripción**: `public record CategorySpendingDto(Guid? CategoryId, string CategoryName, string? CategoryColor, decimal Amount, string Currency, decimal Percentage);`
  - **Dependencias**: T007 (directorio existe)
  - **Validación**: `dotnet build` exit code 0

- [ ] T024 [US2] Crear GetSpendingByCategoryQuery en `src/SauronSheet.Application/Features/Analytics/Queries/GetSpendingByCategoryQuery.cs`
  - **Acción**: Crear archivo
  - **Descripción**: `public record GetSpendingByCategoryQuery(DateTime FromDate, DateTime ToDate) : IRequest<List<CategorySpendingDto>>;`
  - **Dependencias**: T023
  - **Validación**: `dotnet build` exit code 0

- [ ] T025 [US2] Crear GetSpendingByCategoryQueryHandler en `src/SauronSheet.Application/Features/Analytics/Queries/GetSpendingByCategoryQueryHandler.cs`
  - **Acción**: Crear archivo
  - **Descripción**: Handler `IRequestHandler<GetSpendingByCategoryQuery, List<CategorySpendingDto>>`. Inyectar `ITransactionRepository`, `ICategoryRepository`, `IUserContext`. Flow:
    1. Componer specs: user + dateRange vía CompositeSpecification.And()
    2. Cargar transacciones → filtrar solo expenses (Amount.IsNegative)
    3. Si no hay expenses → retornar lista vacía
    4. Calcular totalSpending = sum(Math.Abs(amount))
    5. Cargar categorías del usuario para lookup de nombre/color
    6. GroupBy CategoryId → para cada grupo: sum, nombre ("Uncategorized" si null), porcentaje
    7. Ordenar descendente por amount
    8. Si >6 categorías: agrupar restantes en entry "Other" (color="#6B7280")
    9. Retornar `List<CategorySpendingDto>`
  - **Dependencias**: T022 (tests en RED), T024
  - **Validación**: `dotnet test --filter ClassName~GetSpendingByCategoryQuery` — 4 tests PASAN

**Checkpoint**: US2 complete — Category breakdown funcional

---

## Phase 5: User Story 3 — Monthly Spending Trends (Priority: P1)

**Goal**: Tendencias mensuales de gastos e ingresos para un año (12 entries)  
**Independent Test**: Ejecutar `dotnet test --filter ClassName~GetMonthlyTrendsQuery` — 3 tests pasan  
**Scenario**: 4.3

### Tests (RED Phase)

- [ ] T026 [US3] Crear test stubs para GetMonthlyTrendsQuery (3 tests RED) en `tests/SauronSheet.Application.Tests/Features/Analytics/Queries/GetMonthlyTrendsQueryTests.cs`
  - **Acción**: Crear archivo
  - **Descripción**: Clase `GetMonthlyTrendsQueryTests` con mock de `ITransactionRepository`, `IUserContext`. 3 métodos `[Fact]` + `[Trait("Category", "Application")]`:
    - `GetMonthlyTrends_FullYear_Returns12Entries()` — Transacciones en Jan, Mar, Jun, Dec → 12 entries, meses sin datos = 0
    - `GetMonthlyTrends_NoTransactions_Returns12ZeroEntries()` — Vacío → 12 entries con zeros
    - `GetMonthlyTrends_SeparatesIncomeAndExpenses()` — Jan: €500 income, -€300 + -€100 expenses → TotalIncome=500, TotalExpenses=400, NetAmount=100
  - **Dependencias**: T017
  - **Validación**: 3 tests descubiertos, todos FALLAN (RED)

### Implementación (GREEN Phase)

- [ ] T027 [P] [US3] Crear MonthlyTrendDto en `src/SauronSheet.Application/Features/Analytics/DTOs/MonthlyTrendDto.cs`
  - **Acción**: Crear archivo
  - **Descripción**: `public record MonthlyTrendDto(int Month, string MonthName, decimal TotalExpenses, decimal TotalIncome, decimal NetAmount, string Currency, int TransactionCount);`
  - **Dependencias**: T007
  - **Validación**: `dotnet build` exit code 0

- [ ] T028 [US3] Crear GetMonthlyTrendsQuery en `src/SauronSheet.Application/Features/Analytics/Queries/GetMonthlyTrendsQuery.cs`
  - **Acción**: Crear archivo
  - **Descripción**: `public record GetMonthlyTrendsQuery(int Year) : IRequest<List<MonthlyTrendDto>>;`
  - **Dependencias**: T027
  - **Validación**: `dotnet build` exit code 0

- [ ] T029 [US3] Crear GetMonthlyTrendsQueryHandler en `src/SauronSheet.Application/Features/Analytics/Queries/GetMonthlyTrendsQueryHandler.cs`
  - **Acción**: Crear archivo
  - **Descripción**: Handler `IRequestHandler<GetMonthlyTrendsQuery, List<MonthlyTrendDto>>`. Inyectar `ITransactionRepository`, `IUserContext`. Flow:
    1. Componer specs: user + DateRange(Year/1/1 – Year/12/31) vía CompositeSpecification.And()
    2. Cargar transacciones del año
    3. GroupBy t.Date.Month
    4. Para cada mes (1-12): calcular income (sum positive), expenses (sum Math.Abs negative), net (income - expenses), count
    5. Meses sin transacciones → zero amounts
    6. Usar `CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(month)` para MonthName
    7. Retornar siempre 12 entries `List<MonthlyTrendDto>`
  - **Dependencias**: T026 (tests en RED), T028
  - **Validación**: `dotnet test --filter ClassName~GetMonthlyTrendsQuery` — 3 tests PASAN

**Checkpoint**: US3 complete — Monthly trends funcional

---

## Phase 6: User Story 4 — Yearly Spending Comparison (Priority: P2)

**Goal**: Comparar gastos entre dos años mes a mes  
**Independent Test**: Ejecutar `dotnet test --filter ClassName~GetYearlyComparisonQuery` — 3 tests pasan  
**Scenario**: 4.4

### Tests (RED Phase)

- [ ] T030 [US4] Crear test stubs para GetYearlyComparisonQuery (3 tests RED) en `tests/SauronSheet.Application.Tests/Features/Analytics/Queries/GetYearlyComparisonQueryTests.cs`
  - **Acción**: Crear archivo
  - **Descripción**: Clase `GetYearlyComparisonQueryTests` con mock de `ITransactionRepository`, `IUserContext`. 3 métodos `[Fact]` + `[Trait("Category", "Application")]`:
    - `GetYearlyComparison_TwoYears_ReturnsMonthlyComparison()` — Year 2025: Jan €100, Feb €200; Year 2026: Jan €150, Feb €180 → 12 entries con diferencias
    - `GetYearlyComparison_NoDataForOneYear_ReturnsZeros()` — 2024 vacío → Year1Amount=0 todos los meses
    - `GetYearlyComparison_PercentageChange_ZeroDivision()` — Year1 Jan=€0, Year2 Jan=€150 → PercentageChange=null
  - **Dependencias**: T017
  - **Validación**: 3 tests descubiertos, todos FALLAN (RED)

### Implementación (GREEN Phase)

- [ ] T031 [P] [US4] Crear YearlyComparisonDto en `src/SauronSheet.Application/Features/Analytics/DTOs/YearlyComparisonDto.cs`
  - **Acción**: Crear archivo
  - **Descripción**: `public record YearlyComparisonDto(int Month, string MonthName, decimal Year1Amount, decimal Year2Amount, decimal Difference, decimal? PercentageChange, string Currency);`
  - **Dependencias**: T007
  - **Validación**: `dotnet build` exit code 0

- [ ] T032 [US4] Crear GetYearlyComparisonQuery en `src/SauronSheet.Application/Features/Analytics/Queries/GetYearlyComparisonQuery.cs`
  - **Acción**: Crear archivo
  - **Descripción**: `public record GetYearlyComparisonQuery(int Year1, int Year2) : IRequest<List<YearlyComparisonDto>>;`
  - **Dependencias**: T031
  - **Validación**: `dotnet build` exit code 0

- [ ] T033 [US4] Crear GetYearlyComparisonQueryHandler en `src/SauronSheet.Application/Features/Analytics/Queries/GetYearlyComparisonQueryHandler.cs`
  - **Acción**: Crear archivo
  - **Descripción**: Handler `IRequestHandler<GetYearlyComparisonQuery, List<YearlyComparisonDto>>`. Inyectar `ITransactionRepository`, `IUserContext`. Flow:
    1. Cargar transacciones Year1 y Year2 (specs: user + dateRange de cada año) vía CompositeSpecification.And()
    2. Filtrar solo expenses (Amount.IsNegative) en ambos años
    3. GroupBy month en cada año → Dictionary<int, decimal>
    4. Para cada mes (1-12): calcular y1Amount, y2Amount, difference (y2-y1), percentageChange (null si y1=0)
    5. Usar `CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(month)`
    6. Retornar siempre 12 entries `List<YearlyComparisonDto>`
  - **Dependencias**: T030 (tests en RED), T032
  - **Validación**: `dotnet test --filter ClassName~GetYearlyComparisonQuery` — 3 tests PASAN

- [ ] T034 Ejecutar checkpoint analytics: `dotnet test --filter Category=Application` — verificar 14 tests nuevos + tests previos PASAN
  - **Dependencias**: T021, T025, T029, T033

**Checkpoint**: Todos los analytics handlers complete — 14 tests nuevos GREEN

---

## Phase 7: User Story 5 — Transaction Search & Filtering (Priority: P1) 🎯 MVP

**Goal**: Búsqueda multi-filter con keyword, date range, category, amount range y paginación  
**Independent Test**: Ejecutar `dotnet test --filter ClassName~SearchTransactionsQuery` — 8 tests pasan  
**Scenario**: 4.5

### Tests Recent Transactions (RED Phase)

- [ ] T035 [US5] Crear test stubs para GetRecentTransactionsQuery (3 tests RED) en `tests/SauronSheet.Application.Tests/Features/Transactions/Queries/GetRecentTransactionsQueryTests.cs`
  - **Acción**: Crear archivo
  - **Descripción**: Clase `GetRecentTransactionsQueryTests` con mock de `ITransactionRepository`, `ICategoryRepository`, `IUserContext`. 3 métodos `[Fact]` + `[Trait("Category", "Application")]`:
    - `GetRecentTransactions_ReturnsLastN()` — 20 transacciones, count=10 → retorna 10 ordenadas por fecha desc
    - `GetRecentTransactions_FewerThanN_ReturnsAll()` — 3 transacciones, count=10 → retorna 3
    - `GetRecentTransactions_NoTransactions_ReturnsEmptyList()` — Sin transacciones → lista vacía
  - **Dependencias**: T017
  - **Validación**: 3 tests descubiertos, todos FALLAN (RED)

### Tests Search Transactions (RED Phase)

- [ ] T036 [US5] Crear test stubs para SearchTransactionsQuery (8 tests RED) en `tests/SauronSheet.Application.Tests/Features/Transactions/Queries/SearchTransactionsQueryTests.cs`
  - **Acción**: Crear archivo
  - **Descripción**: Clase `SearchTransactionsQueryTests` con mock de `ITransactionRepository`, `ICategoryRepository`, `IUserContext`. 8 métodos `[Fact]` + `[Trait("Category", "Application")]`:
    - `SearchTransactions_ByKeyword_FiltersCorrectly()` — keyword="coffee" → 2 de 4 transacciones (case-insensitive)
    - `SearchTransactions_ByDateRange_FiltersCorrectly()` — fromDate/toDate → filtra correctamente
    - `SearchTransactions_ByCategory_FiltersCorrectly()` — categoryId → filtra por categoría
    - `SearchTransactions_ByAmountRange_FiltersCorrectly()` — min/max → filtra por rango de monto
    - `SearchTransactions_CombinedFilters_AppliesAll()` — keyword + category + date → AND logic
    - `SearchTransactions_NoFilters_ReturnsAllUserTransactions()` — Sin filtros → retorna todas (paginadas)
    - `SearchTransactions_NoResults_ReturnsEmptyPage()` — Filtros sin resultados → PaginatedResult vacío
    - `SearchTransactions_Paginated_RespectsPageSize()` — 100 resultados, page=2, size=25 → 25 items, TotalPages=4
  - **Dependencias**: T017
  - **Validación**: 8 tests descubiertos, todos FALLAN (RED)

### Implementación Recent Transactions (GREEN Phase)

- [ ] T037 [US5] Crear GetRecentTransactionsQuery en `src/SauronSheet.Application/Features/Transactions/Queries/GetRecentTransactionsQuery.cs`
  - **Acción**: Crear archivo
  - **Descripción**: `public record GetRecentTransactionsQuery(int Count = 10) : IRequest<List<TransactionDto>>;`
  - **Dependencias**: T035 (tests en RED)
  - **Validación**: `dotnet build` exit code 0

- [ ] T038 [US5] Crear GetRecentTransactionsQueryHandler en `src/SauronSheet.Application/Features/Transactions/Queries/GetRecentTransactionsQueryHandler.cs`
  - **Acción**: Crear archivo
  - **Descripción**: Handler `IRequestHandler<GetRecentTransactionsQuery, List<TransactionDto>>`. Inyectar `ITransactionRepository`, `ICategoryRepository`, `IUserContext`. Flow:
    1. Obtener UserId de IUserContext
    2. Cargar transacciones del usuario vía `GetByUserIdAsync(userId)`
    3. Cargar categorías para lookup de nombres
    4. Ordenar por fecha descendente → tomar Count primeros
    5. Mapear a `TransactionDto` (incluir categoryName del lookup)
    6. Retornar `List<TransactionDto>`
  - **Dependencias**: T037
  - **Validación**: `dotnet test --filter ClassName~GetRecentTransactionsQuery` — 3 tests PASAN

### Implementación Search Transactions (GREEN Phase)

- [ ] T039 [US5] Crear SearchTransactionsQuery en `src/SauronSheet.Application/Features/Transactions/Queries/SearchTransactionsQuery.cs`
  - **Acción**: Crear archivo
  - **Descripción**: `public record SearchTransactionsQuery(string? Keyword = null, DateTime? FromDate = null, DateTime? ToDate = null, Guid? CategoryId = null, decimal? MinAmount = null, decimal? MaxAmount = null, int Page = 1, int PageSize = 50) : IRequest<PaginatedResultDto<TransactionDto>>;`
  - **Dependencias**: T036 (tests en RED)
  - **Validación**: `dotnet build` exit code 0

- [ ] T040 [US5] Crear SearchTransactionsQueryHandler en `src/SauronSheet.Application/Features/Transactions/Queries/SearchTransactionsQueryHandler.cs`
  - **Acción**: Crear archivo
  - **Descripción**: Handler `IRequestHandler<SearchTransactionsQuery, PaginatedResultDto<TransactionDto>>`. Inyectar `ITransactionRepository`, `ICategoryRepository`, `IUserContext`. Flow:
    1. Comenzar con `TransactionByUserSpecification(userId)` como spec base
    2. Si Keyword != null → CompositeSpecification.And(spec, new TransactionByDescriptionKeywordSpecification(keyword))
    3. Si FromDate/ToDate != null → And con TransactionByDateRangeSpecification
    4. Si CategoryId != null → And con TransactionByCategorySpecification
    5. Si MinAmount/MaxAmount != null → And con TransactionByAmountRangeSpecification
    6. Ejecutar `FindBySpecificationAsync(composedSpec)`
    7. Cargar categorías para lookup de nombres
    8. Calcular totalCount, totalPages = ceil(totalCount / pageSize)
    9. Aplicar paginación: OrderByDescending(Date).Skip((page-1)*pageSize).Take(pageSize)
    10. Mapear a `TransactionDto` (con categoryName)
    11. Retornar `PaginatedResultDto<TransactionDto>(items, totalCount, page, pageSize, totalPages)`
  - **Dependencias**: T039, T015 (keyword spec), T016 (composite spec)
  - **Validación**: `dotnet test --filter ClassName~SearchTransactionsQuery` — 8 tests PASAN

- [ ] T041 [US5] Ejecutar checkpoint transaction queries: `dotnet test --filter Category=Application` — 25 tests nuevos + previos PASAN
  - **Dependencias**: T038, T040

**Checkpoint**: US5 complete — Recent transactions + Search funcional, todos handlers Application finalizados

---

## Phase 8: User Story 1 — Dashboard Page Frontend (Priority: P1) 🎯 MVP

**Goal**: Dashboard completo con summary cards, charts, recent transactions y date filter  
**Independent Test**: Ejecutar `dotnet run` → navegar a `/Dashboard` → cards, charts y recent transactions visibles  
**Scenarios**: 4.1, 4.6, 4.7

### Layout & Shared Components

- [ ] T042 [US1] Modificar `src/SauronSheet.Frontend/Shared/_Layout.cshtml` — agregar Chart.js CDN
  - **Acción**: Modificar archivo existente
  - **Descripción**: Agregar en la sección `<head>`:
    ```html
    <script src="https://cdn.jsdelivr.net/npm/chart.js@4.4.0/dist/chart.umd.min.js"></script>
    ```
    Actualizar navegación autenticada:
    - Dashboard (📊 /Dashboard) — link principal
    - Transactions (💳 /Transactions)
    - Search (🔍 /Transactions/Search)
    - Upload PDF (📄 /Transactions/Upload)
    - Categories (🏷️ /Categories)
    - Logout
  - **Dependencias**: T041 (todos los handlers listos)
  - **Validación**: `dotnet build` exit code 0

- [ ] T043 [P] [US1] Crear _DateRangeFilter partial en `src/SauronSheet.Frontend/Shared/Components/_DateRangeFilter.cshtml`
  - **Acción**: Crear archivo
  - **Descripción**: Partial view reutilizable con Alpine.js. Componente `x-data="{ showCustom: false }"`:
    - Select con opciones: This Month, Last Month, Last 3 Months, This Year, Custom Range
    - `x-on:change="showCustom = ($event.target.value === 'custom')"` toggle para inputs de fecha custom
    - Inputs de fecha (FromDate, ToDate) visibles solo cuando "Custom Range" seleccionado (`x-show="showCustom"`)
    - Botón "Apply" que hace submit del form
    - Estilizado con Tailwind CSS (flex, gap, rounded-md, etc.)
  - **Dependencias**: T010 (directorio Components/ existe)
  - **Validación**: Archivo creado, sin errores de sintaxis

- [ ] T044 [P] [US1] Crear charts.js en `src/SauronSheet.Frontend/wwwroot/js/charts.js`
  - **Acción**: Crear archivo
  - **Descripción**: JavaScript con 3 funciones de inicialización de Chart.js:
    - `initCategoryPieChart(canvasId, categoryData)` — Pie chart con colores default: ['#3B82F6','#10B981','#F59E0B','#EF4444','#8B5CF6','#EC4899','#6B7280']. Tooltip: nombre, €amount, percentage%
    - `initMonthlyTrendsChart(canvasId, monthlyData)` — Line chart con 2 datasets: Expenses (rojo #EF4444) e Income (verde #10B981), tension=0.3, fill=true. Y axis beginAtZero
    - `initYearlyComparisonChart(canvasId, yearlyData, year1Label, year2Label)` — Bar chart con 2 datasets: year1 (azul #3B82F6) y year2 (púrpura #8B5CF6). Y axis beginAtZero
    - Todas con: responsive=true, legend position='bottom'
  - **Dependencias**: T011 (directorio js/ existe)
  - **Validación**: Archivo creado, sin errores de sintaxis JS

### Dashboard Page

- [ ] T045 [US1] Reescribir Dashboard PageModel en `src/SauronSheet.Frontend/Pages/Dashboard.cshtml.cs`
  - **Acción**: Modificar archivo existente (reemplazo completo del stub Phase 1)
  - **Descripción**: Clase `DashboardModel : PageModel` con `[Authorize]`. Inyectar `IMediator`. Properties:
    - `TransactionSummaryDto Summary`
    - `List<CategorySpendingDto> SpendingByCategory`
    - `List<MonthlyTrendDto> MonthlyTrends`
    - `List<YearlyComparisonDto> YearlyComparison`
    - `List<TransactionDto> RecentTransactions`
    - `[BindProperty(SupportsGet=true)] string DateFilter = "this-month"`
    - `[BindProperty(SupportsGet=true)] DateTime? CustomFromDate`
    - `[BindProperty(SupportsGet=true)] DateTime? CustomToDate`
    - `DateTime FromDate`, `DateTime ToDate`
    - Método `OnGetAsync()`: calcular date range, enviar 5 queries MediatR (Summary, SpendingByCategory, MonthlyTrends, YearlyComparison, RecentTransactions)
    - Método privado `CalculateDateRange()`: switch expression para this-month, last-month, last-3-months, this-year, custom
  - **Dependencias**: T042, T021, T025, T029, T033, T038
  - **Validación**: `dotnet build` exit code 0

- [ ] T046 [US1] Reescribir Dashboard View en `src/SauronSheet.Frontend/Pages/Dashboard.cshtml`
  - **Acción**: Modificar archivo existente (reemplazo completo del stub Phase 1)
  - **Descripción**: Razor view con secciones:
    1. **Date Range Filter** — Form con `_DateRangeFilter` partial, action="/Dashboard", method="get"
    2. **Summary Cards** — Grid 4 columnas (desktop) / 2x2 (mobile):
       - Total Income (text-green-600, icono 📈)
       - Total Expenses (text-red-600, icono 📉)
       - Net Amount (green si ≥0, red si <0, icono 💰)
       - Transaction Count (text-blue-600, icono 📊)
       - Formato moneda: €X,XXX.XX
    3. **Charts** — Grid 2 columnas (desktop), stacked (mobile):
       - `<canvas id="categoryPieChart">` con título "Spending by Category"
       - `<canvas id="monthlyTrendsChart">` con título "Monthly Trends"
       - `<canvas id="yearlyComparisonChart">` con título "Year over Year"
    4. **Recent Transactions** — Tabla con Date, Description, Amount (coloreado), Category. Max 10 filas. Link "View all transactions →"
    5. **Empty State** — Cuando TransactionCount == 0: "No spending data yet. Import a PDF or add transactions to see analytics." con links de acción
    6. **Script block** — Serializar datos con `@Html.Raw(Json.Serialize(Model.SpendingByCategory))` etc. Invocar funciones de charts.js
    - Incluir `<script src="~/js/charts.js"></script>` antes del bloque de inicialización
  - **Dependencias**: T043, T044, T045
  - **Validación**: `dotnet run --project src/SauronSheet.Frontend/` → `/Dashboard` carga con cards, charts y tabla

**Checkpoint**: Dashboard renderiza correctamente — Proceder a Search page

---

## Phase 9: User Story 5 Frontend — Search Page (Priority: P1)

**Goal**: Página de búsqueda multi-filtro con results paginados  
**Independent Test**: Navegar a `/Transactions/Search` → filtros funcionan, paginación correcta  
**Scenario**: 4.5

- [ ] T047 [US5] Crear Search PageModel en `src/SauronSheet.Frontend/Pages/Transactions/Search.cshtml.cs`
  - **Acción**: Crear archivo
  - **Descripción**: Clase `SearchModel : PageModel` con `[Authorize]`. Inyectar `IMediator`. Properties:
    - `PaginatedResultDto<TransactionDto> Results`
    - `[BindProperty(SupportsGet=true)] string? Keyword`
    - `[BindProperty(SupportsGet=true)] DateTime? FromDate`
    - `[BindProperty(SupportsGet=true)] DateTime? ToDate`
    - `[BindProperty(SupportsGet=true)] Guid? CategoryId`
    - `[BindProperty(SupportsGet=true)] decimal? MinAmount`
    - `[BindProperty(SupportsGet=true)] decimal? MaxAmount`
    - `[BindProperty(SupportsGet=true)] int Page = 1`
    - `List<CategoryDto> Categories` (para dropdown)
    - `OnGetAsync()`: cargar categorías vía `GetCategoriesQuery`, ejecutar `SearchTransactionsQuery` con todos los filtros
  - **Dependencias**: T040 (search handler), T042 (layout con nav)
  - **Validación**: `dotnet build` exit code 0

- [ ] T048 [US5] Crear Search View en `src/SauronSheet.Frontend/Pages/Transactions/Search.cshtml`
  - **Acción**: Crear archivo
  - **Descripción**: Razor view con:
    1. **Filter Panel** — Form con method="get":
       - Keyword text input con icono 🔍
       - Date range: inputs type="date" para FromDate y ToDate
       - Category dropdown: opciones de `Model.Categories` + "All Categories" (value="")
       - Amount range: inputs type="number" para MinAmount y MaxAmount
       - Botón "Search" + link "Clear all filters" (href="/Transactions/Search")
    2. **Result Summary** — "Showing N of M transactions matching filters"
    3. **Results Table** — Tabla con Date, Description, Amount (coloreado verde/rojo), Category. Mismo formato que transaction list
    4. **Pagination** — Controles Previous/Next con query parameters preservados
    5. **Empty State** — "No transactions match your filters." con sugerencia de ajustar filtros
    - Todos los valores de filtro preservados en URL query parameters (SupportsGet=true)
  - **Dependencias**: T047
  - **Validación**: Navegar a `/Transactions/Search` → formulario visible, búsqueda funcional

**Checkpoint**: Search page funcional — filtros, paginación y URL params

---

## Phase 10: User Story 1 — Transaction List Update (Priority: P2)

**Goal**: Agregar filtros a la página existente de Transaction list  
**Scenario**: 4.5 (complemento)

- [ ] T049 [US1] Modificar Transaction Index con filtros en `src/SauronSheet.Frontend/Pages/Transactions/Index.cshtml`
  - **Acción**: Modificar archivo existente
  - **Descripción**: Agregar al tope de la página (antes de la tabla):
    - `_DateRangeFilter` partial view integrado en form
    - Category dropdown filter
    - Botón "Apply" que recarga con query parameters
    - Los filtros se aplican al `GetTransactionsQuery` existente
  - **Dependencias**: T043, T048
  - **Validación**: `/Transactions` muestra filtros funcionales

- [ ] T050 [US1] Modificar Transaction Index PageModel en `src/SauronSheet.Frontend/Pages/Transactions/Index.cshtml.cs` si necesario para soportar query params de filtro
  - **Acción**: Modificar archivo existente
  - **Descripción**: Agregar propiedades `[BindProperty(SupportsGet=true)]` para DateFilter, CustomFromDate, CustomToDate, CategoryId. Pasar filtros al query existente
  - **Dependencias**: T049
  - **Validación**: Filtros se aplican correctamente en la lista de transacciones

**Checkpoint**: Transaction list actualizada con filtros

---

## Phase 11: Polish & Cross-Cutting Concerns

**Purpose**: Validación final, responsive design, test coverage

- [ ] T051 Ejecutar build completo: `dotnet build` — zero errors, zero warnings
  - **Dependencias**: T050

- [ ] T052 Ejecutar test suite completa: `dotnet test` — ~186 tests PASAN (Phase 0:13 + Phase 1:22 + Phase 2:81 + Phase 3:38 + Phase 4:32)
  - **Dependencias**: T051

- [ ] T053 Verificar cobertura Domain ≥ 80%: `dotnet test tests/SauronSheet.Domain.Tests/ --collect:"XPlat Code Coverage"`
  - **Dependencias**: T052

- [ ] T054 Verificar cobertura Application ≥ 70%: `dotnet test tests/SauronSheet.Application.Tests/ --collect:"XPlat Code Coverage"`
  - **Dependencias**: T052

- [ ] T055 Verificar dependency rules — Domain tiene 0 project references y 0 NuGet packages en `src/SauronSheet.Domain/SauronSheet.Domain.csproj`
  - **Dependencias**: T052

- [ ] T056 Verificar dependency rules — Application solo referencia Domain en `src/SauronSheet.Application/SauronSheet.Application.csproj`
  - **Dependencias**: T052

- [ ] T057 Verificar responsive design: Dashboard en viewports 320px, 768px, 1024px+ — cards stack, charts stack, no horizontal scroll
  - **Dependencias**: T046

- [ ] T058 Verificar no hay errores JavaScript en consola del browser en todas las páginas (Dashboard, Search, Transactions)
  - **Dependencias**: T046, T048

- [ ] T059 E2E MVP Workflow: Register → Login → Import PDF → View Dashboard → Change Date Filter → Search Transactions → Verify charts render
  - **Dependencias**: T052

- [ ] T060 Verificar tenant isolation: dos usuarios distintos no ven datos del otro en analytics ni search
  - **Dependencias**: T059

**Checkpoint Final**: ✅ MVP COMPLETE — All ~186 tests green, coverage meets thresholds, dependency rules clean, E2E workflow passes

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1, T001-T011)**: No dependencies — start immediately
- **Foundational Domain (Phase 2, T012-T017)**: Depends on Setup — BLOCKS all user story handlers
- **US6 Summary (Phase 3, T018-T021)**: Depends on Foundational
- **US2 Category (Phase 4, T022-T025)**: Depends on Foundational — parallelizable con US6
- **US3 Monthly (Phase 5, T026-T029)**: Depends on Foundational — parallelizable con US2/US6
- **US4 Yearly (Phase 6, T030-T034)**: Depends on Foundational — parallelizable con US2/US3/US6
- **US5 Search (Phase 7, T035-T041)**: Depends on Foundational — parallelizable con US2/US3/US4/US6
- **Dashboard Frontend (Phase 8, T042-T046)**: Depends on ALL analytics handlers (T021, T025, T029, T033, T038)
- **Search Frontend (Phase 9, T047-T048)**: Depends on Search handler (T040) + Layout (T042)
- **Transaction List Update (Phase 10, T049-T050)**: Depends on Dashboard + Search frontend
- **Polish (Phase 11, T051-T060)**: Depends on ALL phases complete

### User Story Dependencies

- **US6 (Summary)**: Independent after Foundational — No dependencies on other stories
- **US2 (Category Spending)**: Independent after Foundational — Parallelizable con US3/US4/US5/US6
- **US3 (Monthly Trends)**: Independent after Foundational — Parallelizable
- **US4 (Yearly Comparison)**: Independent after Foundational — Parallelizable
- **US5 (Search + Recent)**: Independent after Foundational — Parallelizable
- **US1 (Dashboard Frontend)**: Depends on US2, US3, US4, US5, US6 handlers completados
- **US7 (Responsive)**: Parte de T046 (Dashboard view) y T057 (validation)

### Within Each User Story

1. Tests MUST be written and FAIL before implementation (RED-GREEN-REFACTOR)
2. DTOs before Query records
3. Query records before Handlers
4. Core implementation before frontend wiring
5. Story complete before moving to next priority

### Parallel Opportunities

**Backend Handlers (after Foundational):**
```
T018-T021 (US6 Summary)     ─┐
T022-T025 (US2 Category)    ─┤── All parallelizable (different files, no deps)
T026-T029 (US3 Monthly)     ─┤
T030-T033 (US4 Yearly)      ─┤
T035-T041 (US5 Search)      ─┘
```

**DTOs (within each story):**
```
T019 (SummaryDto)            ─┐
T023 (CategorySpendingDto)   ─┤── All parallelizable (different DTO files)
T027 (MonthlyTrendDto)       ─┤
T031 (YearlyComparisonDto)   ─┘
```

**Frontend Shared Components:**
```
T043 (_DateRangeFilter)      ─┐── Parallelizable (different files)
T044 (charts.js)             ─┘
```

---

## Implementation Strategy

### MVP First (Minimum Viable Analytics)

1. Complete Setup (Phase 1) + Foundational Domain (Phase 2)
2. Complete US6 (Summary) → STOP: summary cards work in isolation
3. Complete US2 (Category) → STOP: pie chart data available
4. Complete US5 (Search) → STOP: search API functional
5. Complete Dashboard Frontend (Phase 8) → **MVP DASHBOARD LIVE**
6. Add remaining stories + polish

### Incremental Delivery

1. **Setup + Foundation** → Domain specs ready
2. **All handlers** (parallel) → Backend API complete for all analytics
3. **Dashboard frontend** → Visual analytics live
4. **Search frontend** → Multi-filter search live
5. **Transaction list update** → Filters on existing page
6. **Polish** → E2E validation, coverage, responsive check
7. ✅ **MVP COMPLETE**

---

## Summary

| Metric | Value |
|--------|-------|
| Total tasks | 60 |
| Setup tasks | 11 (T001-T011) |
| Domain tasks | 6 (T012-T017) |
| Application tasks | 24 (T018-T041) |
| Frontend tasks | 9 (T042-T050) |
| Polish/Validation tasks | 10 (T051-T060) |
| Tests to write | 32 (7 Domain + 25 Application) |
| Parallel opportunities | DTOs (4 parallel), Handlers (5 user stories parallel), Frontend components (2 parallel) |
| User stories | 7 (US1-US7) mapped to Scenarios 4.1-4.7 |
| Suggested MVP scope | US6 + US2 + US5 + Dashboard Frontend (minimum to show analytics) |
