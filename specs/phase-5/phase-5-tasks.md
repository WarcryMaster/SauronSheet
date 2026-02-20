# Tasks: Phase 5 — Budget Management & Alerts

**Version**: 1.0.0  
**Created**: 2026-02-20  
**Phase Type**: Full-Stack (Features)  
**Scope**: All Layers (Domain + Application + Infrastructure + Frontend)  
**Duration**: Weeks 19–21  
**Expected Tests**: ~243 total (186 Phase 0–4 + 57 Phase 5)  
**Phase 5 Breakdown**: 19 Domain Budget entity + 10 Domain BudgetService + 28 Application handlers  
**Input**: `specs/phase-5/phase-5-plan.md`, `specs/phase-5/phase-5-spec.md`

---

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Maps to user scenario from spec.md:
  - **[US1]** = Scenario 5.1 — Create a Budget (P1)
  - **[US2]** = Scenario 5.2 — View and Manage Budgets (P1)
  - **[US3]** = Scenario 5.3 — Edit a Budget (P2)
  - **[US4]** = Scenario 5.4 — Delete a Budget (P2)
  - **[US5]** = Scenario 5.5 — View Budget Status on Dashboard (P1)
  - **[US6]** = Scenario 5.6 — View Budget vs. Actual Comparison (P2)
  - **[US7]** = Scenario 5.7 — View Budget Detail (P3)

---

## Phase 1: Setup (Pre-Implementation Validation)

**Purpose**: Verify Phase 0–4 completion and Phase 5 readiness

- [X] T001 Verificar build limpio ejecutando `dotnet build` — exit code 0, cero warnings
- [X] T002 Verificar tests previos ejecutando `dotnet test` — ~186 tests pasan (Phase 0: 13 + Phase 1: 22 + Phase 2: 81 + Phase 3: 38 + Phase 4: 32)
- [X] T003 Verificar que Domain tiene CERO dependencias externas revisando `src/SauronSheet.Domain/SauronSheet.Domain.csproj`
- [X] T004 Verificar que `BudgetId` value object existe en `src/SauronSheet.Domain/ValueObjects/BudgetId.cs`
- [X] T005 Verificar que NO existe `Budget` entity en `src/SauronSheet.Domain/Entities/Budget.cs`
- [X] T006 Verificar que NO existe `IBudgetRepository` en `src/SauronSheet.Domain/Repositories/IBudgetRepository.cs`
- [X] T007 Verificar que NO existe `BudgetService` en `src/SauronSheet.Domain/Services/BudgetService.cs`
- [X] T008 Verificar que el Dashboard con Chart.js (Phase 4) es funcional accediendo a `/Dashboard`
- [X] T009 [P] Crear directorio `src/SauronSheet.Application/Features/Budgets/DTOs/`
- [X] T010 [P] Crear directorio `src/SauronSheet.Application/Features/Budgets/Commands/`
- [X] T011 [P] Crear directorio `src/SauronSheet.Application/Features/Budgets/Queries/`
- [X] T012 [P] Crear directorio `tests/SauronSheet.Application.Tests/Features/Budgets/Commands/`
- [X] T013 [P] Crear directorio `tests/SauronSheet.Application.Tests/Features/Budgets/Queries/`
- [X] T014 [P] Crear directorio `tests/SauronSheet.Domain.Tests/Services/`
- [X] T015 [P] Crear directorio `src/SauronSheet.Frontend/Pages/Budgets/`
- [X] T016 [P] Crear directorio `src/SauronSheet.Infrastructure/Persistence/Migrations/`

**Checkpoint**: Build limpio, ~186 tests green, directorios creados → Proceder a Phase 2

---

## Phase 2: Foundational — Domain Layer (Blocks all user stories)

**Purpose**: Budget entity, repository interface, domain service, and BudgetStatusLevel enum — prerequisites for ALL application handlers and frontend pages

**⚠️ CRITICAL**: No user story work can begin until the full Domain layer is complete

### 2A. BudgetStatusLevel Enum

- [X] T017 [P] Crear enum `BudgetStatusLevel` en `src/SauronSheet.Domain/ValueObjects/BudgetStatusLevel.cs`
  - **Acción**: Crear archivo
  - **Descripción**: Crear enum con 4 valores: `Green`, `Yellow`, `Red`, `Overage`. Incluir XML summary con umbrales: Green < 60%, Yellow 60–80%, Red 80–100%, Overage > 100%. Namespace: `SauronSheet.Domain.ValueObjects`.
  - **Dependencias**: Ninguna
  - **Validación**: `dotnet build --project src/SauronSheet.Domain/` compila sin errores

### 2B. Budget Entity — Tests RED

- [X] T018 Crear test stubs para Budget entity (19 tests RED) en `tests/SauronSheet.Domain.Tests/Entities/BudgetTests.cs`
  - **Acción**: Crear archivo
  - **Descripción**: Crear clase `BudgetTests` con 19 métodos `[Fact]` y `[Trait("Category", "Domain")]` que contengan `Assert.True(false, "Implement ...")`:
    - **Construcción (7 tests)**:
      - `Budget_ValidConstruction_SetsAllProperties()` — BudgetId, UserId, CategoryId, DateRange, Money(500,"EUR") → todas las propiedades set, CreatedAt set
      - `Budget_NullUserId_ThrowsArgumentNullException()` — null UserId → throws
      - `Budget_NullCategoryId_ThrowsArgumentNullException()` — null CategoryId → throws
      - `Budget_NullPeriod_ThrowsArgumentNullException()` — null DateRange → throws
      - `Budget_NullLimit_ThrowsArgumentNullException()` — null Money → throws
      - `Budget_ZeroLimit_ThrowsDomainException()` — Money(0) → throws DomainException("Budget limit must be positive.")
      - `Budget_NegativeLimit_ThrowsDomainException()` — Money(-100) → throws DomainException("Budget limit must be positive.")
    - **IsOverBudget (3 tests)**:
      - `IsOverBudget_SpendExceedsLimit_ReturnsTrue()` — limit 500, spend 600 → true
      - `IsOverBudget_SpendBelowLimit_ReturnsFalse()` — limit 500, spend 300 → false
      - `IsOverBudget_SpendEqualsLimit_ReturnsFalse()` — limit 500, spend 500 → false (at limit is not over)
    - **PercentageUsed (3 tests)**:
      - `PercentageUsed_ZeroSpend_ReturnsZero()` — limit 500, spend 0 → 0.0m
      - `PercentageUsed_HalfSpend_ReturnsFiftyPercent()` — limit 500, spend 250 → 0.50m
      - `PercentageUsed_OverSpend_ReturnsGreaterThanOne()` — limit 500, spend 625 → 1.25m (raw, UI caps)
    - **RemainingAmount (2 tests)**:
      - `RemainingAmount_UnderBudget_ReturnsPositive()` — limit 500, spend 300 → Money(200,"EUR")
      - `RemainingAmount_OverBudget_ReturnsNegative()` — limit 500, spend 700 → Money(-200,"EUR")
    - **UpdateLimit (2 tests)**:
      - `UpdateLimit_ValidPositiveLimit_UpdatesLimitAndTimestamp()` — UpdateLimit(600) → Limit = 600, UpdatedAt set
      - `UpdateLimit_ZeroLimit_ThrowsDomainException()` — UpdateLimit(0) → throws DomainException
    - **Currency Validation (2 tests)**:
      - `IsOverBudget_CurrencyMismatch_ThrowsInvalidOperationException()` — limit EUR, spend USD → throws InvalidOperationException (EnsureSameCurrency)
      - `PercentageUsed_CurrencyMismatch_ThrowsInvalidOperationException()` — limit EUR, spend USD → throws InvalidOperationException (EnsureSameCurrency)
  - **Dependencias**: T017 (BudgetStatusLevel exists), T004 (BudgetId exists)
  - **Validación**: Archivo compila, 19 tests descubiertos, todos FALLAN (RED)

### 2C. Budget Entity — GREEN

- [X] T019 Implementar Budget entity en `src/SauronSheet.Domain/Entities/Budget.cs`
  - **Acción**: Crear archivo
  - **Descripción**: Crear clase `Budget : AggregateRoot<BudgetId>` siguiendo el código exacto de `phase-5-plan.md` sección 1.3. Propiedades: `UserId`, `CategoryId`, `Period` (DateRange), `Limit` (Money). Constructor con validación: null guards (ArgumentNullException) + limit positivo (DomainException). Métodos: `IsOverBudget(Money)` con `EnsureSameCurrency`, `PercentageUsed(Money)` con `EnsureSameCurrency`, `RemainingAmount(Money)` delega a `Limit.Minus()`, `UpdateLimit(Money)` con validación + `UpdatedAt`. Método privado `EnsureSameCurrency(Money)` que lanza `InvalidOperationException` si moneda difiere. Namespace: `SauronSheet.Domain.Entities`.
  - **Dependencias**: T017, T018
  - **Validación**: `dotnet test --filter "ClassName~BudgetTests"` — 19 tests PASAN (GREEN)

- [X] T020 Verificar que tests previos no se rompieron ejecutando `dotnet test --filter Category=Domain` — ~56 tests pasan (37 previos + 19 nuevos)

### 2D. IBudgetRepository Interface

- [X] T021 [P] Crear interfaz `IBudgetRepository` en `src/SauronSheet.Domain/Repositories/IBudgetRepository.cs`
  - **Acción**: Crear archivo
  - **Descripción**: Crear interfaz con 6 métodos asíncronos según `phase-5-plan.md` sección 2.1: `GetByIdAsync(BudgetId)`, `GetByUserIdAsync(UserId)`, `GetByUserAndCategoryAndMonthAsync(UserId, CategoryId, DateRange)`, `AddAsync(Budget)`, `UpdateAsync(Budget)`, `DeleteAsync(BudgetId)`. Namespace: `SauronSheet.Domain.Repositories`.
  - **Dependencias**: T019 (Budget entity)
  - **Validación**: `dotnet build --project src/SauronSheet.Domain/` compila sin errores

### 2E. BudgetService — Tests RED

- [X] T022 Crear test stubs para BudgetService (10 tests RED) en `tests/SauronSheet.Domain.Tests/Services/BudgetServiceTests.cs`
  - **Acción**: Crear archivo
  - **Descripción**: Crear clase `BudgetServiceTests` con 10 métodos `[Fact]` y `[Trait("Category", "Domain")]` que contengan `Assert.True(false, "Implement ...")`:
    - **ValidateUniqueBudget (4 tests)**:
      - `ValidateUniqueBudget_DuplicateExists_ThrowsDomainException()` — mock returns existing budget → throws DomainException
      - `ValidateUniqueBudget_NoDuplicate_Succeeds()` — mock returns null → no exception
      - `ValidateUniqueBudget_SameUserDifferentCategory_Succeeds()` — different category → no exception
      - `ValidateUniqueBudget_SameUserSameCategoryDifferentMonth_Succeeds()` — different month → no exception
    - **GetStatusLevel (6 tests)**:
      - `GetStatusLevel_Under60Percent_ReturnsGreen()` — percentage = 0.50 → Green
      - `GetStatusLevel_At60Percent_ReturnsGreen()` — percentage = 0.60 → Green (threshold is > 0.6, not >=)
      - `GetStatusLevel_At75Percent_ReturnsYellow()` — percentage = 0.75 → Yellow
      - `GetStatusLevel_At80Percent_ReturnsYellow()` — percentage = 0.80 → Yellow (threshold is > 0.8, not >=)
      - `GetStatusLevel_At100Percent_ReturnsRed()` — percentage = 1.00 → Red
      - `GetStatusLevel_Over100Percent_ReturnsOverage()` — percentage = 1.25 → Overage
  - **Dependencias**: T021 (IBudgetRepository interface), T017 (BudgetStatusLevel)
  - **Validación**: Archivo compila, 10 tests descubiertos, todos FALLAN (RED)

### 2F. BudgetService — GREEN

- [X] T023 Implementar BudgetService en `src/SauronSheet.Domain/Services/BudgetService.cs`
  - **Acción**: Crear archivo
  - **Descripción**: Crear clase `BudgetService` siguiendo `phase-5-plan.md` sección 2.3. Constructor con `IBudgetRepository` (null guard). Método `ValidateUniqueBudget(UserId, CategoryId, DateRange)`: llama `GetByUserAndCategoryAndMonthAsync`, lanza `DomainException` si existe. Método estático `GetStatusLevel(decimal percentageUsed)`: switch expression con umbrales exclusivos `> 1.0m → Overage`, `> 0.8m → Red`, `> 0.6m → Yellow`, `_ → Green`. Namespace: `SauronSheet.Domain.Services`.
  - **Dependencias**: T021, T022
  - **Validación**: `dotnet test --filter "ClassName~BudgetServiceTests"` — 10 tests PASAN (GREEN)

- [X] T024 Verificar domain layer completo ejecutando `dotnet test --filter Category=Domain` — ~66 tests pasan (37 previos + 29 nuevos)

**Checkpoint**: Domain layer complete — 19 Budget entity + 10 BudgetService tests GREEN → Proceder a Phase 3

---

## Phase 3: Application DTOs (Shared by all user stories)

**Purpose**: Data transfer objects required by all commands and queries

- [X] T025 [P] Crear DTO `BudgetDto` en `src/SauronSheet.Application/Features/Budgets/DTOs/BudgetDto.cs`
  - **Acción**: Crear archivo
  - **Descripción**: Crear record `BudgetDto` con propiedades: `Guid Id`, `Guid CategoryId`, `string CategoryName`, `string? CategoryColor`, `decimal LimitAmount`, `string Currency`, `DateTime PeriodStart`, `DateTime PeriodEnd`, `DateTime CreatedAt`, `DateTime? UpdatedAt`. Namespace: `SauronSheet.Application.Features.Budgets.DTOs`.
  - **Dependencias**: T009
  - **Validación**: `dotnet build --project src/SauronSheet.Application/` compila

- [X] T026 [P] Crear DTO `BudgetStatusDto` en `src/SauronSheet.Application/Features/Budgets/DTOs/BudgetStatusDto.cs`
  - **Acción**: Crear archivo
  - **Descripción**: Crear record `BudgetStatusDto` con propiedades: `Guid Id`, `Guid CategoryId`, `string CategoryName`, `string? CategoryColor`, `decimal LimitAmount`, `decimal CurrentSpend`, `decimal RemainingAmount`, `decimal PercentageUsed`, `string StatusLevel`, `string Currency`, `DateTime PeriodStart`, `DateTime PeriodEnd`. Namespace: `SauronSheet.Application.Features.Budgets.DTOs`.
  - **Dependencias**: T009
  - **Validación**: `dotnet build --project src/SauronSheet.Application/` compila

- [X] T027 [P] Crear DTO `BudgetVsActualDto` en `src/SauronSheet.Application/Features/Budgets/DTOs/BudgetVsActualDto.cs`
  - **Acción**: Crear archivo
  - **Descripción**: Crear record `BudgetVsActualDto` con propiedades: `Guid? CategoryId`, `string CategoryName`, `string? CategoryColor`, `decimal? BudgetLimit`, `decimal ActualSpend`, `decimal? Difference`, `decimal? PercentageUsed`, `string? StatusLevel`, `string Currency`. Namespace: `SauronSheet.Application.Features.Budgets.DTOs`.
  - **Dependencias**: T009
  - **Validación**: `dotnet build --project src/SauronSheet.Application/` compila

- [X] T028 [P] Crear DTO `BudgetDashboardSummaryDto` en `src/SauronSheet.Application/Features/Budgets/DTOs/BudgetDashboardSummaryDto.cs`
  - **Acción**: Crear archivo
  - **Descripción**: Crear record `BudgetDashboardSummaryDto` con propiedades: `List<BudgetStatusDto> Budgets`, `int TotalBudgets`, `int OnTrackCount`, `int OverBudgetCount`. Namespace: `SauronSheet.Application.Features.Budgets.DTOs`.
  - **Dependencias**: T009, T026
  - **Validación**: `dotnet build --project src/SauronSheet.Application/` compila

- [X] T029 Verificar build completo ejecutando `dotnet build` — toda la solución compila sin errores

**Checkpoint**: DTOs definidos → Proceder a user stories

---

## Phase 4: User Story 1+2 — Create Budget + View Budget List (Priority: P1) 🎯 MVP

**Goal**: Crear presupuestos mensuales por categoría y ver la lista con indicadores de estado. Mapea Scenarios 5.1 (Create) y 5.2 (View/Manage).

**Independent Test**: Navegar a /Budgets/Create → crear presupuesto → verificar que aparece en /Budgets con CurrentSpend, Remaining, PercentageUsed y StatusLevel calculados.

### Tests Application — CreateBudget (RED)

- [X] T030 Crear test stubs para CreateBudgetCommandHandler (5 tests RED) en `tests/SauronSheet.Application.Tests/Features/Budgets/Commands/CreateBudgetCommandHandlerTests.cs`
  - **Acción**: Crear archivo
  - **Descripción**: Crear clase `CreateBudgetCommandHandlerTests` con mocks de `IBudgetRepository`, `ICategoryRepository`, `BudgetService`, `IUserContext`. 5 métodos `[Fact]` y `[Trait("Category", "Application")]` con `Assert.True(false, ...)`:
    - `Handle_ValidInput_CreatesBudgetAndReturnsId()` — happy path: category exists, no duplicate, returns BudgetId
    - `Handle_DuplicateBudget_ThrowsDomainException()` — BudgetService.ValidateUniqueBudget throws
    - `Handle_CategoryNotFound_ThrowsEntityNotFoundException()` — category lookup returns null
    - `Handle_ZeroLimit_ThrowsDomainException()` — Money(0) → Budget constructor throws
    - `Handle_TenantScoped_UsesCurrentUserContext()` — Budget created with userId from IUserContext
  - **Dependencias**: T012, T025, T026
  - **Validación**: 5 tests descubiertos, todos FALLAN (RED)

### Tests Application — GetBudgets (RED)

- [X] T031 Crear test stubs para GetBudgetsQueryHandler (4 tests RED) en `tests/SauronSheet.Application.Tests/Features/Budgets/Queries/GetBudgetsQueryHandlerTests.cs`
  - **Acción**: Crear archivo
  - **Descripción**: Crear clase `GetBudgetsQueryHandlerTests` con mocks de `IBudgetRepository`, `ITransactionRepository`, `ICategoryRepository`, `IUserContext`. 4 métodos `[Fact]` y `[Trait("Category", "Application")]` con `Assert.True(false, ...)`:
    - `Handle_BudgetsExist_ReturnsBudgetStatusDtoList()` — 3 budgets con transacciones → 3 BudgetStatusDto con campos calculados
    - `Handle_NoBudgets_ReturnsEmptyList()` — sin budgets → lista vacía
    - `Handle_WithYearMonthFilter_FiltersCorrectly()` — Year=2026, Month=2 → solo budgets de febrero
    - `Handle_TenantScoped_ReturnsOnlyOwnBudgets()` — solo budgets del usuario autenticado
  - **Dependencias**: T013, T026
  - **Validación**: 4 tests descubiertos, todos FALLAN (RED)

- [X] T032 Ejecutar `dotnet test --filter Category=Application` — verificar 9 tests nuevos FALLAN (RED), ~74 tests previos PASAN

### Implementación — CreateBudgetCommand (GREEN)

- [X] T033 [P] [US1] Crear command `CreateBudgetCommand` en `src/SauronSheet.Application/Features/Budgets/Commands/CreateBudgetCommand.cs`
  - **Acción**: Crear archivo
  - **Descripción**: Crear record `CreateBudgetCommand(Guid CategoryId, decimal LimitAmount, DateTime PeriodStart, DateTime PeriodEnd) : IRequest<Guid>`. Namespace: `SauronSheet.Application.Features.Budgets.Commands`.
  - **Dependencias**: T010
  - **Validación**: `dotnet build --project src/SauronSheet.Application/` compila

- [X] T034 [US1] Crear handler `CreateBudgetCommandHandler` en `src/SauronSheet.Application/Features/Budgets/Commands/CreateBudgetCommandHandler.cs`
  - **Acción**: Crear archivo
  - **Descripción**: Implementar `IRequestHandler<CreateBudgetCommand, Guid>` siguiendo `phase-5-plan.md` sección 4.2. Inyectar `IBudgetRepository`, `ICategoryRepository`, `BudgetService`, `IUserContext`. Flow: (1) extraer UserId de IUserContext, (2) validar categoría existe y pertenece al usuario, (3) construir DateRange, (4) construir Money, (5) validar unicidad via BudgetService, (6) crear Budget con new BudgetId(Guid.NewGuid()), (7) AddAsync, (8) retornar id.
  - **Dependencias**: T033, T019, T021, T023
  - **Validación**: `dotnet test --filter "ClassName~CreateBudgetCommandHandlerTests"` — 5 tests PASAN (GREEN)

### Implementación — GetBudgetsQuery (GREEN)

- [X] T035 [P] [US2] Crear query `GetBudgetsQuery` en `src/SauronSheet.Application/Features/Budgets/Queries/GetBudgetsQuery.cs`
  - **Acción**: Crear archivo
  - **Descripción**: Crear record `GetBudgetsQuery(int? Year = null, int? Month = null) : IRequest<List<BudgetStatusDto>>`. Retorna `BudgetStatusDto` (no `BudgetDto`) porque la lista necesita campos calculados (CurrentSpend, Remaining, PercentageUsed, StatusLevel) para progress bars y status indicators. Namespace: `SauronSheet.Application.Features.Budgets.Queries`.
  - **Dependencias**: T011, T026
  - **Validación**: `dotnet build --project src/SauronSheet.Application/` compila

- [X] T036 [US2] Crear handler `GetBudgetsQueryHandler` en `src/SauronSheet.Application/Features/Budgets/Queries/GetBudgetsQueryHandler.cs`
  - **Acción**: Crear archivo
  - **Descripción**: Implementar `IRequestHandler<GetBudgetsQuery, List<BudgetStatusDto>>` siguiendo `phase-5-plan.md` sección 5.2 actualizada. Inyectar `IBudgetRepository`, `ITransactionRepository`, `ICategoryRepository`, `IUserContext`. Flow: (1) cargar budgets del usuario, (2) filtrar por Year/Month si se proveen, (3) para cada budget: componer specification (User+DateRange+Category) → cargar transacciones → calcular currentSpend de gastos negativos → calcular PercentageUsed, RemainingAmount, StatusLevel, (4) mapear a BudgetStatusDto, (5) ordenar por CategoryName. Importar: `Domain.Services`, `Domain.Specifications`, `Domain.ValueObjects`.
  - **Dependencias**: T035, T019, T021, T023
  - **Validación**: `dotnet test --filter "ClassName~GetBudgetsQueryHandlerTests"` — 4 tests PASAN (GREEN)

### Frontend — Budget List + Create Pages

- [X] T037 [US2] Crear page model `IndexModel` en `src/SauronSheet.Frontend/Pages/Budgets/Index.cshtml.cs`
  - **Acción**: Crear archivo
  - **Descripción**: Crear `IndexModel : PageModel` con `[Authorize]`. Inyectar `IMediator`. Properties: `List<BudgetStatusDto> Budgets`, `[BindProperty(SupportsGet = true)] int? Year`, `[BindProperty(SupportsGet = true)] int? Month`. `OnGetAsync()`: enviar `GetBudgetsQuery(Year, Month)`. `OnPostDeleteAsync(Guid budgetId)`: enviar `DeleteBudgetCommand` y redirigir.
  - **Dependencias**: T036
  - **Validación**: `dotnet build --project src/SauronSheet.Frontend/` compila

- [X] T038 [US2] Crear vista `Index.cshtml` en `src/SauronSheet.Frontend/Pages/Budgets/Index.cshtml`
  - **Acción**: Crear archivo
  - **Descripción**: Razor page con `@model IndexModel`. Month selector (Year/Month). Tabla de budgets: Category Name | Limit | Current Spend | Remaining | % Used | Status Badge | Edit/Delete actions. Status badges con colores (Green/Yellow/Red/Overage). Empty state: "No budgets set for {month}. Create one to start tracking." con link a `/Budgets/Create`. Botón "Create Budget" linking a `/Budgets/Create`. Usar Tailwind CSS para estilos.
  - **Dependencias**: T037
  - **Validación**: Página renderiza al navegar a `/Budgets`

- [X] T039 [US1] Crear page model `CreateModel` en `src/SauronSheet.Frontend/Pages/Budgets/Create.cshtml.cs`
  - **Acción**: Crear archivo
  - **Descripción**: Crear `CreateModel : PageModel` con `[Authorize]`. Inyectar `IMediator`. Properties: `List<CategoryDto> Categories`, `[BindProperty] Guid CategoryId`, `[BindProperty] decimal LimitAmount`, `[BindProperty] DateTime Month` (default: 1er día mes actual). `OnGetAsync()`: cargar categorías. `OnPostAsync()`: construir DateRange (1st to last day), enviar `CreateBudgetCommand`, redirect a `/Budgets` con success message. Catch DomainException → mostrar error, retener form values.
  - **Dependencias**: T034
  - **Validación**: `dotnet build --project src/SauronSheet.Frontend/` compila

- [X] T040 [US1] Crear vista `Create.cshtml` en `src/SauronSheet.Frontend/Pages/Budgets/Create.cshtml`
  - **Acción**: Crear archivo
  - **Descripción**: Razor page con `@model CreateModel`. Formulario con: dropdown de categorías, month picker (default mes actual), input de monto límite (decimal positivo), label "EUR" (currency fija). Botones Submit/Cancel. Display de errores de validación. Tailwind CSS.
  - **Dependencias**: T039
  - **Validación**: Formulario renderiza al navegar a `/Budgets/Create`

- [X] T041 Verificar que CreateBudget + GetBudgets funciona end-to-end: crear budget → aparece en listado con status calculado

**Checkpoint**: US1+US2 funcional — crear y listar budgets con status → Proceder a US3+US4

---

## Phase 5: User Story 3+4 — Edit Budget + Delete Budget (Priority: P2)

**Goal**: Editar límite de presupuestos existentes y eliminar presupuestos. Mapea Scenarios 5.3 (Edit) y 5.4 (Delete).

**Independent Test**: Editar límite de un budget → verificar cambio. Eliminar budget → verificar que desaparece y transacciones no se afectan.

### Tests Application — UpdateBudget + DeleteBudget (RED)

- [X] T042 [P] Crear test stubs para UpdateBudgetCommandHandler (3 tests RED) en `tests/SauronSheet.Application.Tests/Features/Budgets/Commands/UpdateBudgetCommandHandlerTests.cs`
  - **Acción**: Crear archivo
  - **Descripción**: Crear clase `UpdateBudgetCommandHandlerTests` con mocks de `IBudgetRepository`, `IUserContext`. 3 métodos `[Fact]` y `[Trait("Category", "Application")]`:
    - `Handle_ValidUpdate_UpdatesBudgetLimit()` — happy path: budget exists, belongs to user, limit updated
    - `Handle_BudgetNotFound_ThrowsEntityNotFoundException()` — budget null → throws
    - `Handle_DifferentUserBudget_ThrowsEntityNotFoundException()` — budget userId != current user → throws
  - **Dependencias**: T012
  - **Validación**: 3 tests descubiertos, todos FALLAN (RED)

- [X] T043 [P] Crear test stubs para DeleteBudgetCommandHandler (4 tests RED) en `tests/SauronSheet.Application.Tests/Features/Budgets/Commands/DeleteBudgetCommandHandlerTests.cs`
  - **Acción**: Crear archivo
  - **Descripción**: Crear clase `DeleteBudgetCommandHandlerTests` con mocks de `IBudgetRepository`, `IUserContext`. 4 métodos `[Fact]` y `[Trait("Category", "Application")]`:
    - `Handle_ValidDelete_DeletesBudget()` — happy path: budget exists, DeleteAsync called
    - `Handle_BudgetNotFound_ThrowsEntityNotFoundException()` — budget null → throws
    - `Handle_DifferentUserBudget_ThrowsEntityNotFoundException()` — tenant isolation
    - `Handle_DeletedBudget_TransactionsUnaffected()` — verify no transaction side effects
  - **Dependencias**: T012
  - **Validación**: 4 tests descubiertos, todos FALLAN (RED)

### Implementación — UpdateBudget + DeleteBudget (GREEN)

- [X] T044 [P] [US3] Crear command `UpdateBudgetCommand` en `src/SauronSheet.Application/Features/Budgets/Commands/UpdateBudgetCommand.cs`
  - **Acción**: Crear archivo
  - **Descripción**: Crear record `UpdateBudgetCommand(Guid BudgetId, decimal NewLimitAmount, string Currency = "EUR") : IRequest<Unit>`. Namespace: `SauronSheet.Application.Features.Budgets.Commands`.
  - **Dependencias**: T010
  - **Validación**: Compila

- [X] T045 [US3] Crear handler `UpdateBudgetCommandHandler` en `src/SauronSheet.Application/Features/Budgets/Commands/UpdateBudgetCommandHandler.cs`
  - **Acción**: Crear archivo
  - **Descripción**: Implementar `IRequestHandler<UpdateBudgetCommand, Unit>` siguiendo `phase-5-plan.md` sección 4.4. Flow: (1) cargar budget por Id, (2) verificar UserId == current user, (3) construir Money, (4) `budget.UpdateLimit(newLimit)`, (5) UpdateAsync.
  - **Dependencias**: T044, T019, T021
  - **Validación**: `dotnet test --filter "ClassName~UpdateBudgetCommandHandlerTests"` — 3 tests PASAN (GREEN)

- [X] T046 [P] [US4] Crear command `DeleteBudgetCommand` en `src/SauronSheet.Application/Features/Budgets/Commands/DeleteBudgetCommand.cs`
  - **Acción**: Crear archivo
  - **Descripción**: Crear record `DeleteBudgetCommand(Guid BudgetId) : IRequest<Unit>`. Namespace: `SauronSheet.Application.Features.Budgets.Commands`.
  - **Dependencias**: T010
  - **Validación**: Compila

- [X] T047 [US4] Crear handler `DeleteBudgetCommandHandler` en `src/SauronSheet.Application/Features/Budgets/Commands/DeleteBudgetCommandHandler.cs`
  - **Acción**: Crear archivo
  - **Descripción**: Implementar `IRequestHandler<DeleteBudgetCommand, Unit>` siguiendo `phase-5-plan.md` sección 4.6. Flow: (1) cargar budget por Id, (2) verificar UserId == current user (else throw EntityNotFoundException), (3) DeleteAsync.
  - **Dependencias**: T046, T019, T021
  - **Validación**: `dotnet test --filter "ClassName~DeleteBudgetCommandHandlerTests"` — 4 tests PASAN (GREEN)

### Frontend — Edit Page

- [X] T048 [US3] Crear page model `EditModel` en `src/SauronSheet.Frontend/Pages/Budgets/Edit.cshtml.cs`
  - **Acción**: Crear archivo
  - **Descripción**: Crear `EditModel : PageModel` con `[Authorize]`. Inyectar `IMediator`. Properties: `BudgetStatusDto Budget`, `[BindProperty] Guid BudgetId`, `[BindProperty] decimal NewLimitAmount`. `OnGetAsync(Guid id)`: cargar budget via `GetBudgetByIdQuery`. `OnPostAsync()`: enviar `UpdateBudgetCommand`, redirect a `/Budgets`.
  - **Dependencias**: T045, T060 (GetBudgetByIdQueryHandler — se puede implementar stub primero)
  - **Validación**: `dotnet build --project src/SauronSheet.Frontend/` compila

- [X] T049 [US3] Crear vista `Edit.cshtml` en `src/SauronSheet.Frontend/Pages/Budgets/Edit.cshtml`
  - **Acción**: Crear archivo
  - **Descripción**: Razor page con `@model EditModel`. Display read-only: Category name, Month/Period. Input editable: monto límite. Display informativo: current spend y status. Botones Save/Cancel. Tailwind CSS.
  - **Dependencias**: T048
  - **Validación**: Página renderiza al navegar a `/Budgets/Edit/{id}`

**Checkpoint**: US3+US4 funcional — editar y eliminar budgets → Proceder a US5+US6

---

## Phase 6: User Story 5 — Budget Status on Dashboard (Priority: P1) 🎯 MVP

**Goal**: Widget de estado de presupuestos en el Dashboard existente. Mapea Scenario 5.5.

**Independent Test**: Navegar al Dashboard → verificar widget muestra progress bars con colores para presupuestos del mes actual.

### Tests Application — GetBudgetSummaryForDashboard (RED)

- [X] T050 Crear test stubs para GetBudgetSummaryForDashboardQueryHandler (4 tests RED) en `tests/SauronSheet.Application.Tests/Features/Budgets/Queries/GetBudgetSummaryForDashboardQueryHandlerTests.cs`
  - **Acción**: Crear archivo
  - **Descripción**: Crear clase `GetBudgetSummaryForDashboardQueryHandlerTests` con mocks de `IBudgetRepository`, `ITransactionRepository`, `ICategoryRepository`, `IUserContext`. 4 métodos `[Fact]` y `[Trait("Category", "Application")]`:
    - `Handle_BudgetsExist_ReturnsAggregatedSummary()` — 3 budgets: 2 on-track, 1 over → TotalBudgets=3, OnTrackCount=2, OverBudgetCount=1
    - `Handle_NoBudgets_ReturnsEmptySummary()` — TotalBudgets=0, Budgets=[]
    - `Handle_AllOnTrack_NoOverBudget()` — 2 budgets both < 60% → OnTrackCount=2, OverBudgetCount=0
    - `Handle_TenantScoped_OnlyCurrentUserBudgets()` — solo budgets del usuario autenticado
  - **Dependencias**: T013, T028
  - **Validación**: 4 tests descubiertos, todos FALLAN (RED)

### Implementación — GetBudgetSummaryForDashboard (GREEN)

- [X] T051 [P] [US5] Crear query `GetBudgetSummaryForDashboardQuery` en `src/SauronSheet.Application/Features/Budgets/Queries/GetBudgetSummaryForDashboardQuery.cs`
  - **Acción**: Crear archivo
  - **Descripción**: Crear record `GetBudgetSummaryForDashboardQuery(int Year, int Month) : IRequest<BudgetDashboardSummaryDto>`. Handler construye DateRange internamente a partir de year+month. Namespace: `SauronSheet.Application.Features.Budgets.Queries`.
  - **Dependencias**: T011, T028
  - **Validación**: Compila

- [X] T052 [US5] Crear handler `GetBudgetSummaryForDashboardQueryHandler` en `src/SauronSheet.Application/Features/Budgets/Queries/GetBudgetSummaryForDashboardQueryHandler.cs`
  - **Acción**: Crear archivo
  - **Descripción**: Implementar `IRequestHandler<GetBudgetSummaryForDashboardQuery, BudgetDashboardSummaryDto>` siguiendo `phase-5-plan.md` sección 5.8. Flow: (1) construir periodStart/periodEnd desde Year/Month, (2) cargar budgets del usuario filtrados por period, (3) si vacío → retornar empty summary, (4) cargar transacciones del periodo (batch), (5) agrupar gastos por categoría, (6) para cada budget: calcular spend/percentage/remaining/statusLevel, (7) contar overBudget, (8) retornar BudgetDashboardSummaryDto.
  - **Dependencias**: T051, T019, T021, T023
  - **Validación**: `dotnet test --filter "ClassName~GetBudgetSummaryForDashboardQueryHandlerTests"` — 4 tests PASAN (GREEN)

### Frontend — Dashboard Widget + Shared Components

- [X] T053 [P] [US5] Crear partial view `_BudgetProgressBar.cshtml` en `src/SauronSheet.Frontend/Shared/_BudgetProgressBar.cshtml`
  - **Acción**: Crear archivo
  - **Descripción**: Partial view con `@model BudgetStatusDto`. Barra de progreso con ancho % (capped a 100% visual). Color según StatusLevel: Green → bg-green-500, Yellow → bg-yellow-500, Red → bg-red-500, Overage → bg-red-700. Label de overflow si > 100%. Tailwind CSS.
  - **Dependencias**: T026
  - **Validación**: Compila sin errores

- [X] T054 [P] [US5] Crear partial view `_BudgetStatusBadge.cshtml` en `src/SauronSheet.Frontend/Shared/_BudgetStatusBadge.cshtml`
  - **Acción**: Crear archivo
  - **Descripción**: Partial view con `@model string` (StatusLevel). Badge con colores: Green → "On Track" (green-100/green-800), Yellow → "Warning" (yellow-100/yellow-800), Red → "Near Limit" (red-100/red-800), Overage → "Over Budget" (red-200/red-900). Tailwind rounded-full badge.
  - **Dependencias**: Ninguna
  - **Validación**: Compila sin errores

- [X] T055 [US5] Modificar page model `DashboardModel` en `src/SauronSheet.Frontend/Pages/Dashboard.cshtml.cs`
  - **Acción**: Modificar archivo existente
  - **Descripción**: Agregar property `BudgetDashboardSummaryDto? BudgetSummary`. En `OnGetAsync()`, agregar call: `BudgetSummary = await _mediator.Send(new GetBudgetSummaryForDashboardQuery(FromDate.Year, FromDate.Month));`. Importar namespaces de Budgets DTOs y Queries.
  - **Dependencias**: T052
  - **Validación**: `dotnet build --project src/SauronSheet.Frontend/` compila

- [X] T056 [US5] Modificar vista `Dashboard.cshtml` en `src/SauronSheet.Frontend/Pages/Dashboard.cshtml`
  - **Acción**: Modificar archivo existente
  - **Descripción**: Agregar sección "Budget Status" después de los charts de analytics. Si `BudgetSummary == null || TotalBudgets == 0` → empty state con link a `/Budgets/Create`. Si hay budgets → mostrar summary "X of Y budgets on track" + progress bars por budget usando `_BudgetProgressBar` partial. Links a `/Budgets/Detail/{id}`. Mostrar warning si OverBudgetCount > 0.
  - **Dependencias**: T055, T053, T054
  - **Validación**: Dashboard muestra widget de budget status

**Checkpoint**: US5 funcional — Dashboard muestra widget de presupuestos → Proceder a US6

---

## Phase 7: User Story 6 — Budget vs. Actual Comparison (Priority: P2)

**Goal**: Página de comparación detallada budget vs. gasto real por categoría. Mapea Scenario 5.6.

**Independent Test**: Navegar a /Budgets/Comparison → seleccionar mes → verificar tabla y chart muestran datos correctos.

### Tests Application — GetBudgetVsActual (RED)

- [X] T057 Crear test stubs para GetBudgetVsActualQueryHandler (5 tests RED) en `tests/SauronSheet.Application.Tests/Features/Budgets/Queries/GetBudgetVsActualQueryHandlerTests.cs`
  - **Acción**: Crear archivo
  - **Descripción**: Crear clase `GetBudgetVsActualQueryHandlerTests` con mocks de `IBudgetRepository`, `ITransactionRepository`, `ICategoryRepository`, `IUserContext`. 5 métodos `[Fact]` y `[Trait("Category", "Application")]`:
    - `Handle_BudgetsAndTransactions_ReturnsComparison()` — 2 categorías con budgets y transacciones
    - `Handle_CategoryWithSpendButNoBudget_ShowsNoBudget()` — categoría con gastos pero sin budget → BudgetLimit = null
    - `Handle_BudgetWithNoSpend_ShowsZeroActual()` — budget sin transacciones → ActualSpend = 0
    - `Handle_SummaryRow_TotalsCorrectly()` — total budgeted, total actual, total difference
    - `Handle_SortOrder_OverBudgetFirst()` — over-budget primero, luego por percentage desc
  - **Dependencias**: T013, T027
  - **Validación**: 5 tests descubiertos, todos FALLAN (RED)

### Implementación — GetBudgetVsActual (GREEN)

- [X] T058 [P] [US6] Crear query `GetBudgetVsActualQuery` en `src/SauronSheet.Application/Features/Budgets/Queries/GetBudgetVsActualQuery.cs`
  - **Acción**: Crear archivo
  - **Descripción**: Crear record `GetBudgetVsActualQuery(int Year, int Month) : IRequest<List<BudgetVsActualDto>>`. Handler construye DateRange internamente. Namespace: `SauronSheet.Application.Features.Budgets.Queries`.
  - **Dependencias**: T011, T027
  - **Validación**: Compila

- [X] T059 [US6] Crear handler `GetBudgetVsActualQueryHandler` en `src/SauronSheet.Application/Features/Budgets/Queries/GetBudgetVsActualQueryHandler.cs`
  - **Acción**: Crear archivo
  - **Descripción**: Implementar `IRequestHandler<GetBudgetVsActualQuery, List<BudgetVsActualDto>>` siguiendo `phase-5-plan.md` sección 5.6. Flow: (1) construir periodStart/periodEnd desde Year/Month, (2) cargar budgets del periodo, (3) cargar transacciones (batch), (4) agrupar gastos por categoría, (5) para categorías con budget: calcular difference/percentage/statusLevel, (6) para categorías sin budget pero con gastos: incluir con BudgetLimit = null, (7) ordenar: over-budget primero → percentage desc.
  - **Dependencias**: T058, T019, T021, T023
  - **Validación**: `dotnet test --filter "ClassName~GetBudgetVsActualQueryHandlerTests"` — 5 tests PASAN (GREEN)

### Frontend — Comparison Page

- [X] T060 [US6] Crear page model `ComparisonModel` en `src/SauronSheet.Frontend/Pages/Budgets/Comparison.cshtml.cs`
  - **Acción**: Crear archivo
  - **Descripción**: Crear `ComparisonModel : PageModel` con `[Authorize]`. Inyectar `IMediator`. Properties: `List<BudgetVsActualDto> Comparison`, `decimal TotalBudgeted`, `decimal TotalActual`, `decimal TotalDifference`, `[BindProperty(SupportsGet = true)] int? Year`, `[BindProperty(SupportsGet = true)] int? Month`. `OnGetAsync()`: default a year/month actual si no especificado, enviar `GetBudgetVsActualQuery(year, month)`, calcular totales.
  - **Dependencias**: T059
  - **Validación**: Compila

- [X] T061 [US6] Crear vista `Comparison.cshtml` en `src/SauronSheet.Frontend/Pages/Budgets/Comparison.cshtml`
  - **Acción**: Crear archivo
  - **Descripción**: Razor page con `@model ComparisonModel`. Month selector. Tabla comparación: Category | Budget Limit | Actual | Difference | Status. Summary row con totales. Horizontal bar chart (Chart.js) budget vs actual por categoría. Categorías sin budget muestran "No budget". Ordenado: over-budget primero. Tailwind CSS.
  - **Dependencias**: T060
  - **Validación**: Página renderiza al navegar a `/Budgets/Comparison`

**Checkpoint**: US6 funcional — comparación budget vs actual con chart → Proceder a US7

---

## Phase 8: User Story 7 — Budget Detail Page (Priority: P3)

**Goal**: Página de detalle de presupuesto individual con progress bar y lista de transacciones. Mapea Scenario 5.7.

**Independent Test**: Click en un budget → ver progress bar grande, resumen de gasto y transacciones del mes/categoría.

### Tests Application — GetBudgetById (RED)

- [X] T062 Crear test stubs para GetBudgetByIdQueryHandler (3 tests RED) en `tests/SauronSheet.Application.Tests/Features/Budgets/Queries/GetBudgetByIdQueryHandlerTests.cs`
  - **Acción**: Crear archivo
  - **Descripción**: Crear clase `GetBudgetByIdQueryHandlerTests` con mocks de `IBudgetRepository`, `ITransactionRepository`, `ICategoryRepository`, `IUserContext`. 3 métodos `[Fact]` y `[Trait("Category", "Application")]`:
    - `Handle_BudgetExists_ReturnsBudgetStatusDto()` — happy path con spend calculado
    - `Handle_BudgetNotFound_ThrowsEntityNotFoundException()` — budget null → throws
    - `Handle_ZeroTransactions_ReturnsZeroSpend()` — sin transacciones → CurrentSpend = 0, StatusLevel = Green
  - **Dependencias**: T013, T026
  - **Validación**: 3 tests descubiertos, todos FALLAN (RED)

### Implementación — GetBudgetById (GREEN)

- [X] T063 [P] [US7] Crear query `GetBudgetByIdQuery` en `src/SauronSheet.Application/Features/Budgets/Queries/GetBudgetByIdQuery.cs`
  - **Acción**: Crear archivo
  - **Descripción**: Crear record `GetBudgetByIdQuery(Guid BudgetId) : IRequest<BudgetStatusDto>`. Namespace: `SauronSheet.Application.Features.Budgets.Queries`.
  - **Dependencias**: T011, T026
  - **Validación**: Compila

- [X] T064 [US7] Crear handler `GetBudgetByIdQueryHandler` en `src/SauronSheet.Application/Features/Budgets/Queries/GetBudgetByIdQueryHandler.cs`
  - **Acción**: Crear archivo
  - **Descripción**: Implementar `IRequestHandler<GetBudgetByIdQuery, BudgetStatusDto>` siguiendo `phase-5-plan.md` sección 5.4. Flow: (1) cargar budget por Id, (2) verificar UserId == current user, (3) componer specification (User+DateRange+Category), (4) cargar transacciones, (5) calcular currentSpend/percentage/remaining/statusLevel, (6) cargar categoría para nombre/color, (7) retornar BudgetStatusDto.
  - **Dependencias**: T063, T019, T021, T023
  - **Validación**: `dotnet test --filter "ClassName~GetBudgetByIdQueryHandlerTests"` — 3 tests PASAN (GREEN)

### Frontend — Detail Page

- [X] T065 [US7] Crear page model `DetailModel` en `src/SauronSheet.Frontend/Pages/Budgets/Detail.cshtml.cs`
  - **Acción**: Crear archivo
  - **Descripción**: Crear `DetailModel : PageModel` con `[Authorize]`. Inyectar `IMediator`. Properties: `BudgetStatusDto Budget`, `List<TransactionDto> Transactions`. `OnGetAsync(Guid id)`: cargar budget via `GetBudgetByIdQuery`, cargar transacciones del periodo+categoría via `SearchTransactionsQuery` existente.
  - **Dependencias**: T064
  - **Validación**: Compila

- [X] T066 [US7] Crear vista `Detail.cshtml` en `src/SauronSheet.Frontend/Pages/Budgets/Detail.cshtml`
  - **Acción**: Crear archivo
  - **Descripción**: Razor page con `@model DetailModel`. Header: Category name, Month, Limit. Progress bar grande usando `_BudgetProgressBar` partial. Status badge usando `_BudgetStatusBadge` partial. Resumen: Current Spend / Limit | Remaining | Percentage. Lista de transacciones del mes/categoría. Botón Edit, link "Back to budgets". Tailwind CSS.
  - **Dependencias**: T065, T053, T054
  - **Validación**: Página renderiza al navegar a `/Budgets/Detail/{id}`

**Checkpoint**: US7 funcional — detalle de budget con transacciones → Proceder a Phase 9

---

## Phase 9: Infrastructure — Database Migration & Repository

**Purpose**: Persistencia en Supabase PostgreSQL

- [X] T067 Crear migración SQL `006_CreateBudgetsTable.sql` en `src/SauronSheet.Infrastructure/Persistence/Migrations/006_CreateBudgetsTable.sql`
  - **Acción**: Crear archivo
  - **Descripción**: Crear tabla `public.budgets` con columnas: `id UUID PRIMARY KEY`, `user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE`, `category_id UUID NOT NULL REFERENCES categories(id) ON DELETE CASCADE`, `period_start TIMESTAMPTZ NOT NULL`, `period_end TIMESTAMPTZ NOT NULL`, `limit_amount DECIMAL(18,2) NOT NULL CHECK (limit_amount > 0)`, `currency VARCHAR(3) NOT NULL DEFAULT 'EUR'`, `created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()`, `updated_at TIMESTAMPTZ`. Constraint: `UNIQUE(user_id, category_id, period_start)`. Indexes: `idx_budgets_user`, `idx_budgets_user_period`, `idx_budgets_category`. RLS: 4 policies (SELECT, INSERT, UPDATE, DELETE) using `auth.uid() = user_id`.
  - **Dependencias**: Ninguna (puede ejecutarse en paralelo con desarrollo)
  - **Validación**: SQL ejecuta sin errores en Supabase SQL editor

- [X] T068 Crear `SupabaseBudgetRepository` en `src/SauronSheet.Infrastructure/Persistence/SupabaseBudgetRepository.cs`
  - **Acción**: Crear archivo
  - **Descripción**: Implementar `IBudgetRepository` siguiendo `phase-5-plan.md` sección 6.2. Crear clase interna `BudgetRow : BaseModel` con atributos `[Table("budgets")]`, `[PrimaryKey]`, `[Column]` para cada campo. Métodos `ToDomain()` y `FromDomain(Budget)`. Implementar los 6 métodos del repositorio usando `Supabase.Client.From<BudgetRow>()` con filtros `.Where()`.
  - **Dependencias**: T021, T067
  - **Validación**: `dotnet build --project src/SauronSheet.Infrastructure/` compila

- [X] T069 Modificar `DependencyInjection.cs` en `src/SauronSheet.Infrastructure/DependencyInjection.cs`
  - **Acción**: Modificar archivo existente
  - **Descripción**: Agregar registros DI: `services.AddScoped<IBudgetRepository, SupabaseBudgetRepository>()` y `services.AddScoped<BudgetService>()`. Importar namespaces necesarios.
  - **Dependencias**: T068, T023
  - **Validación**: `dotnet build` — toda la solución compila

**Checkpoint**: Infrastructure complete — persistencia y DI configurados

---

## Phase 10: Polish & Cross-Cutting Concerns

**Purpose**: Navegación, UI warning de category deletion, y validación final

- [X] T070 Modificar `_Layout.cshtml` en `src/SauronSheet.Frontend/Shared/_Layout.cshtml`
  - **Acción**: Modificar archivo existente
  - **Descripción**: Agregar link "Budgets" en la sección de navegación autenticada, después de los links existentes de Transactions/Dashboard. HTML: `<a href="/Budgets" class="text-gray-300 hover:text-white px-3 py-2 rounded-md text-sm font-medium">Budgets</a>`.
  - **Dependencias**: T038
  - **Validación**: Link visible en navegación al estar autenticado

- [ ] T071 Agregar warning de category deletion con budgets activos
  - **Acción**: Modificar page(s) de categoría existente(s)
  - **Descripción**: En la confirmación de eliminación de categoría, consultar `IBudgetRepository.GetByUserIdAsync(userId)` filtrado por `CategoryId` para contar budgets asociados. Si `budgetCount > 0`, mostrar warning: "This category has X active budget(s). Deleting will also remove them." en el modal de confirmación Alpine.js. La eliminación procede via DB `ON DELETE CASCADE`. No se bloquea a nivel de dominio — budgets son tracking overlays.
  - **Dependencias**: T068
  - **Validación**: Al eliminar categoría con budget activo → aparece warning con conteo

- [X] T072 Verificar build completo ejecutando `dotnet build` — toda la solución compila sin errores ni warnings

- [X] T073 Ejecutar `dotnet test --filter Category=Domain` — ~66 domain tests PASAN (37 previos + 29 Phase 5)

- [X] T074 Ejecutar `dotnet test --filter Category=Application` — ~102 application tests PASAN (74 previos + 28 Phase 5)

- [X] T075 Ejecutar `dotnet test` — ~243 total tests PASAN (186 Phase 0–4 + 57 Phase 5)

- [X] T076 Verificar arquitectura: Domain.csproj CERO dependencias externas, Application.csproj → solo Domain, Infrastructure.csproj → solo Domain, Frontend.csproj → Application + Infrastructure

- [X] T077 Validar E2E budget workflow: (1) Login → (2) Crear budget Groceries €500 Feb 2026 → (3) Ver en lista con status → (4) Ver detalle con progress bar → (5) Editar límite a €600 → (6) Dashboard widget muestra progreso → (7) Comparison page muestra budget vs actual → (8) Eliminar budget → transacciones no afectadas → (9) Intentar crear duplicado → error mostrado

- [X] T078 Verificar coverage: Domain ≥ 80%, Application ≥ 70%

---

## Orden de implementación

```
T001–T016  Setup & Validación previa (secuencial T001-T008, paralelo T009-T016)
    │
    ▼
T017       BudgetStatusLevel enum (paralelo con T018)
T018       Budget entity tests RED
    │
    ▼
T019       Budget entity GREEN
T020       Verify domain tests
    │
    ▼
T021       IBudgetRepository interface (paralelo con T022)
T022       BudgetService tests RED
    │
    ▼
T023       BudgetService GREEN
T024       Verify domain complete
    │
    ▼
T025–T028  Application DTOs (todos paralelos)
T029       Verify build
    │
    ▼
T030–T031  Tests RED para US1+US2 (paralelos)
T032       Verify tests RED
    │
    ▼
T033–T034  CreateBudgetCommand (GREEN)
T035–T036  GetBudgetsQuery (GREEN, paralelo con T033-T034)
    │
    ▼
T037–T040  Frontend Budget List + Create pages
T041       Verify US1+US2 E2E
    │
    ▼
T042–T043  Tests RED para US3+US4 (paralelos)
T044–T047  UpdateBudget + DeleteBudget (GREEN)
T048–T049  Frontend Edit page
    │
    ▼
T050       Tests RED para US5
T051–T052  GetBudgetSummaryForDashboard (GREEN)
T053–T054  Shared partials (paralelos)
T055–T056  Dashboard widget integration
    │
    ▼
T057       Tests RED para US6
T058–T059  GetBudgetVsActual (GREEN)
T060–T061  Frontend Comparison page
    │
    ▼
T062       Tests RED para US7
T063–T064  GetBudgetById (GREEN)
T065–T066  Frontend Detail page
    │
    ▼
T067–T069  Infrastructure (migración + repositorio + DI)
    │
    ▼
T070–T078  Polish & Validación final
```

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — verificación de prerrequisitos
- **Foundational (Phase 2)**: Depends on Setup — BLOQUEA todas las user stories
- **DTOs (Phase 3)**: Depends on Domain (Phase 2) — usado por todos los handlers
- **US1+US2 (Phase 4)**: Depends on DTOs (Phase 3) — Create + List budgets (MVP)
- **US3+US4 (Phase 5)**: Depends on US1+US2 (necesita budgets existentes para editar/eliminar)
- **US5 (Phase 6)**: Depends on US1+US2 (necesita datos para el widget)
- **US6 (Phase 7)**: Depends on US1+US2 (necesita budgets para comparar)
- **US7 (Phase 8)**: Depends on US1+US2 (necesita budget individual)
- **Infrastructure (Phase 9)**: Puede empezar en paralelo pero se integra después de Application
- **Polish (Phase 10)**: Depends on ALL phases anteriores

### Within Each User Story

- Tests MUST ser escritos PRIMERO y FALLAR antes de implementación (TDD RED-GREEN)
- Command/Query definition antes del handler
- Handler antes del frontend page model
- Page model antes de la vista (.cshtml)

### Parallel Opportunities

- T009–T016: Todos los directorios en paralelo
- T017 + T018: BudgetStatusLevel + Budget tests en paralelo
- T025–T028: Todos los DTOs en paralelo
- T030 + T031: Tests RED de US1+US2 en paralelo
- T033 + T035: CreateBudgetCommand + GetBudgetsQuery queries en paralelo
- T042 + T043: Tests RED de US3+US4 en paralelo
- T044 + T046: UpdateBudget + DeleteBudget commands en paralelo
- T053 + T054: Shared partials en paralelo
- T067: Migración SQL puede prepararse desde el inicio

---

## Implementation Strategy

### MVP First (US1 + US2 + US5)

1. Complete Phase 1: Setup
2. Complete Phase 2: Domain Foundation (CRITICAL — blocks everything)
3. Complete Phase 3: DTOs
4. Complete Phase 4: US1+US2 (Create + List budgets)
5. Complete Phase 6: US5 (Dashboard widget)
6. **STOP and VALIDATE**: Budget creation, listing with status, and dashboard widget work E2E
7. Deploy/demo if ready — Core budget functionality complete

### Incremental Delivery

1. Setup + Domain + DTOs → Foundation ready
2. Add US1+US2 → Create + List budgets with status (MVP core)
3. Add US3+US4 → Edit + Delete budgets
4. Add US5 → Dashboard integration
5. Add US6 → Budget vs Actual comparison
6. Add US7 → Budget detail page
7. Infrastructure + Polish → Production-ready
8. Each story adds value without breaking previous stories

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Tests use exclusive `>` thresholds (not `>=`): 0.60 → Green, 0.80 → Yellow per spec clarification
- `GetBudgetsQuery` returns `List<BudgetStatusDto>` (not `BudgetDto`) — requires spend calculation
- All queries use `(int Year, int Month)` params — handlers construct DateRange internally
- `EnsureSameCurrency` validation added to Budget entity for consistency with Money arithmetic
- Category deletion shows UI warning about active budgets (DB cascade handles removal)
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
