# Phase 2 Tasks — Core Data Model & Domain Entities

**Version**: 1.0.0  
**Created**: 2026-02-15  
**Phase Type**: Domain-Only ⚠️  
**Scope**: Domain Layer ONLY (NO Application, Infrastructure, or Frontend code)  
**Duration**: Weeks 6–8  
**Expected Tests**: 96-98 total (19 Phase 0+1 + 77-79 Phase 2)
**Phase 2 Breakdown**: 6 Strong-Typed IDs + 13 Money + 5 DateRange + 10 Transaction + 12 Category + 15 Budget + 8 CategoryService + 8-10 Specifications

---

## 1. Pre-Implementation Validation

### Tarea 1.0 – Verificar que Phase 0 y Phase 1 están completos
- **Path**: N/A (validation via command line)
- **Acción**: Ejecutar comandos de validación
- **Descripción**: Ejecutar `dotnet build` y `dotnet test` para asegurar que todas las fases anteriores compilan sin errores y todos los tests pasan (Phase 0: 13 tests, Phase 1: 22 tests)
- **Dependencias**: Phase 0 y Phase 1 completadas
- **Criterios de validación**:
  - `dotnet build` exit code 0, cero warnings
  - `dotnet test` output contiene "35 passed" (11 Phase 0 Domain + 8 Phase 1 Domain + 2 Phase 0 Application + 14 Phase 1 Application)
  - `dotnet test --filter Category=Domain` retorna 19 tests (11 Phase 0 + 8 Phase 1)
  - Desglose Domain: 11 Phase 0 (Entity, ValueObject, DomainException, ISpecification) + 8 Phase 1 (UserId, AuthResult, UserProfile)
  - Git workspace limpio (no cambios uncommitted)

---

## 2. Domain Layer Extensions — Directory Structure

### Tarea 2.1 – Crear estructura de carpetas Domain para Phase 2
- **Path**: `src/SauronSheet.Domain/ValueObjects/`, `src/SauronSheet.Domain/Services/`, `src/SauronSheet.Domain/Specifications/`, `src/SauronSheet.Domain/Entities/`
- **Acción**: Crear directorios
- **Descripción**: Crear los directorios necesarios dentro de `src/SauronSheet.Domain/` para organizar los nuevos archivos de Phase 2. Directorios a crear:
  - `ValueObjects/` (si no existe) — para strong-typed IDs y business value objects
  - `Services/` (si no existe) — para domain services (CategoryService)
  - `Specifications/` (si no existe) — para specification classes
  - `Entities/` (si no existe) — para aggregate roots (Transaction, Category, Budget)
- **Dependencias**: Phase 0 (Domain project debe existir)
- **Criterios de validación**: Todos los directorios existen y están vacíos (excepto ValueObjects si tiene UserId de Phase 1)

### Tarea 2.2 – Crear estructura de carpetas Domain.Tests para Phase 2
- **Path**: `tests/SauronSheet.Domain.Tests/ValueObjects/`, `tests/SauronSheet.Domain.Tests/Entities/`, `tests/SauronSheet.Domain.Tests/Services/`, `tests/SauronSheet.Domain.Tests/Specifications/`
- **Acción**: Crear directorios
- **Descripción**: Crear los directorios para tests organizados por componente dentro de `tests/SauronSheet.Domain.Tests/`. Directorios a crear:
  - `ValueObjects/` — tests para value objects
  - `Entities/` — tests para aggregate roots
  - `Services/` — tests para domain services
  - `Specifications/` — tests para specifications
- **Dependencias**: Phase 0 (Domain.Tests project debe existir)
- **Criterios de validación**: Todos los directorios existen y están vacíos

---

## 3. Domain Layer — VALUE OBJECTS (Phase 2A & 2B)

### Tarea 3.1 – Crear test stubs para Strong-Typed IDs (RED phase)
- **Path**: `tests/SauronSheet.Domain.Tests/ValueObjects/TransactionIdTests.cs`, `tests/SauronSheet.Domain.Tests/ValueObjects/CategoryIdTests.cs`, `tests/SauronSheet.Domain.Tests/ValueObjects/BudgetIdTests.cs`
- **Acción**: Crear 3 archivos
- **Descripción**: Crear test classes con stubs para los 3 strong-typed ID value objects. Cada archivo debe contener 2 test methods con `[Fact]` y `[Trait("Category", "Domain")]`:
  - TransactionIdTests: `TransactionId_ValidGuid_SetsValue()`, `TransactionId_EmptyGuid_ThrowsDomainException()`
  - CategoryIdTests: `CategoryId_ValidGuid_SetsValue()`, `CategoryId_EmptyGuid_ThrowsDomainException()`
  - BudgetIdTests: `BudgetId_ValidGuid_SetsValue()`, `BudgetId_EmptyGuid_ThrowsDomainException()`
  - Cada test debe contener `Assert.True(false, "Implement ...")` para fallar intencionalmente (RED phase)
- **Dependencias**: Tarea 2.1 (estructura de carpetas creada)
- **Criterios de validación**:
  - Los 3 archivos compilan sin errores
  - `dotnet test --filter Category=Domain` descubre 6 nuevos tests
  - Los 6 tests fallan (red phase)
  - Los 19 tests anteriores de Phase 0 + Phase 1 siguen pasando (no hay regresión)

### Tarea 3.2 – Implementar Strong-Typed IDs (GREEN phase)
- **Path**: `src/SauronSheet.Domain/ValueObjects/TransactionId.cs`, `src/SauronSheet.Domain/ValueObjects/CategoryId.cs`, `src/SauronSheet.Domain/ValueObjects/BudgetId.cs`
- **Acción**: Crear 3 archivos
- **Descripción**: Implementar los 3 strong-typed ID value objects como `record` que hereda de `ValueObject` (Phase 0). Cada VO debe:
  - Tener propiedad pública `Guid Value { get; }`
  - Validar en el constructor que `Value != Guid.Empty`, lanzar `DomainException` si es vacío
  - Implementar `ToString()` retornando `Value.ToString()`
  - Usar namespace correcto: `SauronSheet.Domain.ValueObjects`
  - Incluir `using` necesarios (Common, Exceptions)
- **Dependencias**: Tarea 3.1 (test stubs creados), Phase 0 (ValueObject base class existe)
- **Criterios de validación**:
  - Los 3 archivos compilan sin errores
  - `dotnet test --filter ClassName=TransactionIdTests` pasa 2 tests
  - `dotnet test --filter ClassName=CategoryIdTests` pasa 2 tests
  - `dotnet test --filter ClassName=BudgetIdTests` pasa 2 tests
  - `dotnet test --filter Category=Domain` retorna 25 tests passing
  - Desglose: 19 Phase 0+1 + 6 Strong-Typed IDs = 25 total

### Tarea 3.3 – Crear test stubs para Money & DateRange (RED phase)
- **Path**: `tests/SauronSheet.Domain.Tests/ValueObjects/MoneyTests.cs`, `tests/SauronSheet.Domain.Tests/ValueObjects/DateRangeTests.cs`
- **Acción**: Crear 2 archivos
- **Descripción**: Crear test classes con stubs para Money y DateRange value objects. Cantidad de tests según phase-2-spec:
  - MoneyTests: 13 test methods (T-2.23 a T-2.31, T-2.54, T-2.68–T-2.70)
  - DateRangeTests: 5 test methods (T-2.32–T-2.34, T-2.55, T-2.71)
  - Todos los tests deben tener `[Fact]` y `[Trait("Category", "Domain")]`
  - Todos deben contener `Assert.True(false, "Implement ...")` (RED phase)
- **Dependencias**: Tarea 2.1 (estructura creada)
- **Criterios de validación**:
  - Los 2 archivos compilan
  - `dotnet test --filter Category=Domain` descubre 18 nuevos tests (13 Money + 5 DateRange)
  - Los 18 tests fallan (RED phase)
  - Los 25 tests anteriores siguen pasando (no hay regresión)

### Tarea 3.4 – Implementar Money Value Object (GREEN phase)
- **Path**: `src/SauronSheet.Domain/ValueObjects/Money.cs`
- **Acción**: Crear archivo
- **Descripción**: Implementar `Money` como `record` que hereda de `ValueObject` con:
  - Propiedades públicas: `decimal Amount { get; }`, `string Currency { get; }`
  - Constructor que valide:
    - `Currency` no sea null/vacío, lanzar `DomainException` si no cumple
    - Guardar `Amount` y `Currency`
    - Default currency = "EUR" (parámetro con default)
  - Métodos:
    - `Plus(Money other)` — valida `other != null`, suma amounts del mismo currency, lanza excepción si currencies diferentes
    - `Minus(Money other)` — valida `other != null`, resta amounts del mismo currency, lanza excepción si currencies diferentes
    - Properties de solo lectura: `bool IsPositive`, `bool IsNegative`, `bool IsZero`
    - `ToString()` retorna `"{Amount:F2} {Currency}"` (ej. "150.00 EUR")
  - Método privado: `EnsureSameCurrency(Money other)` — lanza `DomainException` si currencies diferentes con mensaje descriptivo
- **Dependencias**: Tarea 3.3 (tests creados), Phase 0 (ValueObject base)
- **Criterios de validación**:
  - Archivo compila sin errores
  - `dotnet test --filter ClassName=MoneyTests` pasa todos 13 tests
  - `dotnet test --filter Category=Domain` retorna 38 tests passing
  - Desglose: 19 Phase 0+1 + 6 IDs + 13 Money = 38 total

### Tarea 3.5 – Implementar DateRange Value Object (GREEN phase)
- **Path**: `src/SauronSheet.Domain/ValueObjects/DateRange.cs`
- **Acción**: Crear archivo
- **Descripción**: Implementar `DateRange` como `record` que hereda de `ValueObject` con:
  - Propiedades públicas: `DateTime StartDate { get; }`, `DateTime EndDate { get; }`
  - Constructor que valide:
    - `EndDate >= StartDate`, lanzar `DomainException` si no cumple
    - Guardar ambas propiedades
  - Método: `ToString()` retorna `"{StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd}"`
  - C# records manejan value-based equality automáticamente
- **Dependencias**: Tarea 3.3 (tests creados), Phase 0 (ValueObject base)
- **Criterios de validación**:
  - Archivo compila sin errores
  - `dotnet test --filter ClassName=DateRangeTests` pasa todos 5 tests
  - `dotnet test --filter Category=Domain` retorna 43 tests passing
  - Desglose: 19 Phase 0+1 + 6 IDs + 13 Money + 5 DateRange = 43 total

---

## 4. Domain Layer — ENTITIES (Phase 2C & 2D & 2E)

### Tarea 4.1 – Crear test stubs para Transaction Entity (RED phase)
- **Path**: `tests/SauronSheet.Domain.Tests/Entities/TransactionTests.cs`
- **Acción**: Crear archivo
- **Descripción**: Crear test class con 10 test methods para Transaction aggregate root (T-2.01–T-2.06, T-2.51, T-2.52, T-2.57, T-2.58):
  - Todos con `[Fact]` y `[Trait("Category", "Domain")]`
  - Todos con `Assert.True(false, "Implement ...")`
- **Dependencias**: Tarea 2.1 (estructura creada)
- **Criterios de validación**:
  - Archivo compila
  - `dotnet test --filter Category=Domain` descubre 10 nuevos tests
  - Los 10 tests fallan (RED phase)
  - Los 43 tests anteriores siguen pasando

### Tarea 4.2 – Implementar Transaction Entity (GREEN phase)
- **Path**: `src/SauronSheet.Domain/Entities/Transaction.cs`
- **Acción**: Crear archivo
- **Descripción**: Implementar `Transaction` como clase que hereda de `AggregateRoot<TransactionId>` (Phase 0) con:
  - Propiedades privadas: UserId, Money Amount, DateTime Date, string Description, CategoryId?, string? ImportedFrom
  - Validaciones en constructor: UserId != null, Amount != null, Date <= UtcNow, Description no vacío
  - Método `Categorize(CategoryId? categoryId)` — permite null para descategorizar, actualiza UpdatedAt
  - Método `UpdateDescription(string newDescription)` — valida no vacío, actualiza Description y UpdatedAt
  - XML comments documentando comportamiento de Categorize
- **Dependencias**: Tarea 4.1 (tests), Phase 0 (AggregateRoot), Phase 1 (UserId), Task 3.2 (TransactionId), Task 3.4 (Money)
- **Criterios de validación**:
  - Archivo compila sin errores
  - `dotnet test --filter ClassName=TransactionTests` pasa 10 tests
  - `dotnet test --filter Category=Domain` retorna 53 tests passing
  - Desglose: 43 value objects + 10 Transaction = 53 total

### Tarea 4.3 – Crear test stubs para Category Entity (RED phase)
- **Path**: `tests/SauronSheet.Domain.Tests/Entities/CategoryTests.cs`
- **Acción**: Crear archivo
- **Descripción**: Crear test class con 12 test methods para Category aggregate root (T-2.07–T-2.14, T-2.53, T-2.59–T-2.61)
- **Dependencias**: Tarea 2.1 (estructura creada)
- **Criterios de validación**:
  - Archivo compila
  - `dotnet test --filter Category=Domain` descubre 12 nuevos tests
  - Los 12 tests fallan (RED phase)
  - Los 53 tests anteriores siguen pasando

### Tarea 4.4 – Implementar Category Entity (GREEN phase)
- **Path**: `src/SauronSheet.Domain/Entities/Category.cs`
- **Acción**: Crear archivo
- **Descripción**: Implementar `Category` como clase que hereda de `AggregateRoot<CategoryId>` con:
  - Constructores: privado (interno), público (user-defined), factory `CreateSystemDefault`
  - Guard methods: `CanDelete(bool hasActiveTransactions)`, `CanRename()`
  - Mutation method: `Rename(string newName)` — valida no system default, nombre no vacío, mismo nombre = no-op, actualiza Name y UpdatedAt
- **Dependencias**: Tarea 4.3 (tests), Phase 0 (AggregateRoot), Phase 1 (UserId), Task 3.2 (CategoryId)
- **Criterios de validación**:
  - Archivo compila sin errores
  - `dotnet test --filter ClassName=CategoryTests` pasa 12 tests
  - `dotnet test --filter Category=Domain` retorna 65 tests passing
  - Desglose: 53 (VOs + Transaction) + 12 Category = 65 total

### Tarea 4.5 – Crear test stubs para Budget Entity (RED phase)
- **Path**: `tests/SauronSheet.Domain.Tests/Entities/BudgetTests.cs`
- **Acción**: Crear archivo
- **Descripción**: Crear test class con 15 test methods para Budget aggregate root (T-2.15–T-2.22, T-2.56, T-2.62–T-2.67)
- **Dependencias**: Tarea 2.1 (estructura creada)
- **Criterios de validación**:
  - Archivo compila
  - `dotnet test --filter Category=Domain` descubre 15 nuevos tests
  - Los 15 tests fallan (RED phase)
  - Los 65 tests anteriores siguen pasando

### Tarea 4.6 – Implementar Budget Entity (GREEN phase)
- **Path**: `src/SauronSheet.Domain/Entities/Budget.cs`
- **Acción**: Crear archivo
- **Descripción**: Implementar `Budget` como clase que hereda de `AggregateRoot<BudgetId>` con:
  - Validaciones constructor: UserId, CategoryId, Month no null; Limit.Amount > 0
  - Métodos:
    - `IsOverBudget(Money currentSpend)` — retorna currentSpend.Amount > Limit.Amount
    - `PercentageUsed(Money currentSpend)` — retorna currentSpend.Amount / Limit.Amount (sin guard de Limit == 0, constructor lo previene)
    - `RemainingAmount(Money currentSpend)` — valida currency match, lanza DomainException si no coinciden, retorna Limit.Minus(currentSpend)
    - `UpdateLimit(Money newLimit)` — valida > 0, actualiza Limit y UpdatedAt
  - XML comments documentando RemainingAmount y excepción de currency mismatch
- **Dependencias**: Tarea 4.5 (tests), Phase 0 (AggregateRoot), Phase 1 (UserId), Task 3.2 (BudgetId, CategoryId), Task 3.4 (Money), Task 3.5 (DateRange)
- **Criterios de validación**:
  - Archivo compila sin errores
  - `dotnet test --filter ClassName=BudgetTests` pasa 15 tests
  - `dotnet test --filter Category=Domain` retorna 80 tests passing
  - Desglose: 65 (VOs + Transaction + Category) + 15 Budget = 80 total

---

## 5. Domain Layer — DOMAIN SERVICES & SPECIFICATIONS

### Tarea 5.1 – Crear test stubs para CategoryService (RED phase)
- **Path**: `tests/SauronSheet.Domain.Tests/Services/CategoryServiceTests.cs`
- **Acción**: Crear archivo
- **Descripción**: Crear test class con 8 test methods para CategoryService (T-2.39–T-2.44, T-2.76, T-2.77) usando Moq para mocking de ICategoryRepository
- **Dependencias**: Tarea 2.1 (estructura creada)
- **Criterios de validación**:
  - Archivo compila
  - `dotnet test --filter Category=Domain` descubre 8 nuevos tests
  - Los 8 tests fallan (RED phase)
  - Los 80 tests anteriores siguen pasando

### Tarea 5.2 – Crear test stubs para Specifications (RED phase)
- **Path**: `tests/SauronSheet.Domain.Tests/Specifications/SpecificationTests.cs`
- **Acción**: Crear archivo
- **Descripción**: Crear test class con 10 test methods para Specifications (T-2.45–T-2.50 = 6 tests, T-2.78–T-2.81 = 4 tests):
  - DateRangeSpec: MatchesInRange, ExcludesOutOfRange, IncludesBoundaryDates
  - CategorySpec: MatchesWithCategory, ExcludesWithDifferentCategory
  - AmountRangeSpec: MatchesInRange, ExcludesOutOfRange
  - UserSpec: MatchesForUser, ExcludesForDifferentUser
  - AllSpecs: DefaultMaxResults_1000
  - Todos con `[Fact]` y `[Trait("Category", "Domain")]`, todos con `Assert.True(false, "Implement ...")`
- **Dependencias**: Tarea 2.1 (estructura creada)
- **Criterios de validación**:
  - Archivo compila
  - `dotnet test --filter Category=Domain` descubre 10 nuevos tests
  - Los 10 tests fallan (RED phase)
  - Los 80 tests anteriores siguen pasando

### Tarea 5.3 – Implementar CategoryService (GREEN phase)
- **Path**: `src/SauronSheet.Domain/Services/CategoryService.cs`
- **Acción**: Crear archivo
- **Descripción**: Implementar `CategoryService` con:
  - Constructor: recibe ICategoryRepository, valida != null
  - `ValidateUniqueName(UserId, string)` — async, lanza DomainException si nombre ya existe
  - `CanDeleteCategory(Category, bool)` — delega a category.CanDelete
  - `GetSystemDefaults(UserId)` — retorna 4 categorías (Groceries, Transport, Utilities, Other) con IDs aleatorios
  - XML comments en GetSystemDefaults documentando IDs no determinísticos (Phase 2 nota: Phase 3 tendrá IDs persistidos)
- **Dependencias**: Tarea 5.1 (tests), debe estar definida interfaz `ICategoryRepository` en Domain/Repositories
- **Criterios de validación**:
  - Archivo compila sin errores
  - `dotnet test --filter ClassName=CategoryServiceTests` pasa 8 tests
  - `dotnet test --filter Category=Domain` retorna 88 tests passing
  - Desglose: 80 (VOs + Entities) + 8 CategoryService = 88 total

### Tarea 5.4 – Crear BaseSpecification abstract class (GREEN phase)
- **Path**: `src/SauronSheet.Domain/Specifications/BaseSpecification.cs`
- **Acción**: Crear archivo
- **Descripción**: Implementar `BaseSpecification<T>` como clase abstracta que implementa `ISpecification<T>` (Phase 0) con:
  - Propiedad `Expression<Func<T, bool>> Criteria` — asignada en constructor
  - Propiedad `int MaxResults` — default 1000, protected set
  - Propiedades `List<Expression<Func<T, object>>> Includes` y `List<string> IncludeStrings` — inicializadas vacías
  - Comentario TODO Phase 3 documentando que Includes/IncludeStrings serán usados para eager loading
  - Constructor protegido: valida criteria != null, lanza ArgumentNullException si es null
- **Dependencias**: Phase 0 (ISpecification<T> interface)
- **Criterios de validación**:
  - Archivo compila sin errores
  - BaseSpecification compila y se puede heredar

### Tarea 5.5 – Crear 4 Concrete Specifications (GREEN phase)
- **Path**: 
  - `src/SauronSheet.Domain/Specifications/TransactionByDateRangeSpecification.cs`
  - `src/SauronSheet.Domain/Specifications/TransactionByCategorySpecification.cs`
  - `src/SauronSheet.Domain/Specifications/TransactionByAmountRangeSpecification.cs`
  - `src/SauronSheet.Domain/Specifications/TransactionByUserSpecification.cs`
- **Acción**: Crear 4 archivos
- **Descripción**: Implementar 4 concrete specifications que heredan de `BaseSpecification<Transaction>` con criterias específicas para cada filtro
- **Dependencias**: Tarea 5.4 (BaseSpecification), Task 4.2 (Transaction), Task 3.2–3.5 (ValueObjects)
- **Criterios de validación**:
  - Los 4 archivos compilan sin errores
  - `dotnet test --filter ClassName=SpecificationTests` pasa 10 tests
  - `dotnet test --filter Category=Domain` retorna 98 tests passing
  - Desglose: 88 (VOs + Entities + Service) + 10 Specifications = 98 total
  - Nota: Target 96 (19 Phase 0+1 + 77 Phase 2), tenemos 2 tests extra de cobertura bonus

---

## 6. Domain Layer — REPOSITORY INTERFACES

### Tarea 6.1 – Crear ITransactionRepository interface
- **Path**: `src/SauronSheet.Domain/Repositories/ITransactionRepository.cs`
- **Acción**: Crear archivo
- **Descripción**: Definir interfaz `ITransactionRepository` con métodos async Task:
  - `GetByIdAsync(TransactionId)` — retorna Transaction? (nullable)
  - `GetByUserIdAsync(UserId)` — retorna IReadOnlyList<Transaction> (nunca null)
  - `FindBySpecificationAsync(ISpecification<Transaction>)` — retorna IReadOnlyList<Transaction>, XML comments documentando enforcement de MaxResults
  - `AddAsync(Transaction)`, `UpdateAsync(Transaction)`, `DeleteAsync(TransactionId)`
  - `ExistsAsync(TransactionId)` — retorna bool
  - `ExistsDuplicateAsync(UserId, DateTime, decimal, string)` — retorna bool para detección de duplicados
- **Dependencias**: Phase 0 (ISpecification<T>), Task 4.2 (Transaction), Task 3.2 (TransactionId), Phase 1 (UserId)
- **Criterios de validación**:
  - Archivo compila sin errores
  - Interfaz es compilable y usable (sin implementación)

### Tarea 6.2 – Crear ICategoryRepository interface
- **Path**: `src/SauronSheet.Domain/Repositories/ICategoryRepository.cs`
- **Acción**: Crear archivo
- **Descripción**: Definir interfaz `ICategoryRepository` con métodos async Task para GetByIdAsync, GetByUserIdAsync, FindByNameAndUserAsync, GetSystemDefaultsAsync, AddAsync, UpdateAsync, DeleteAsync, HasTransactionsAsync
- **Dependencias**: Task 4.4 (Category), Task 3.2 (CategoryId), Phase 1 (UserId)
- **Criterios de validación**:
  - Archivo compila sin errores
  - Interfaz es compilable y usable

### Tarea 6.3 – Crear IBudgetRepository interface
- **Path**: `src/SauronSheet.Domain/Repositories/IBudgetRepository.cs`
- **Acción**: Crear archivo
- **Descripción**: Definir interfaz `IBudgetRepository` con métodos async Task para GetByIdAsync, GetByUserIdAsync, GetByUserAndCategoryAndMonthAsync, AddAsync, UpdateAsync, DeleteAsync
- **Dependencias**: Task 4.6 (Budget), Task 3.2 (BudgetId, CategoryId), Task 3.5 (DateRange), Phase 1 (UserId)
- **Criterios de validación**:
  - Archivo compila sin errores
  - Interfaz es compilable y usable

---

## 7. Full Integration Testing

### Tarea 7.1 – Ejecutar build completo de solución
- **Path**: N/A (command line)
- **Acción**: Ejecutar comando
- **Descripción**: Ejecutar `dotnet build` desde la raíz de la solución para verificar que todos los nuevos archivos de Phase 2 compilan correctamente sin errores ni warnings
- **Dependencias**: Todas las tareas 3.1–6.3 completadas
- **Criterios de validación**:
  - Exit code 0
  - Output muestra "Build succeeded"
  - Cero errores
  - Cero warnings (TreatWarningsAsErrors=true debe detectar cualquier warning como error)

### Tarea 7.2 – Ejecutar todos los tests del Domain (RED + GREEN)
- **Path**: N/A (command line)
- **Acción**: Ejecutar comando
- **Descripción**: Ejecutar `dotnet test --filter Category=Domain --no-build` para verificar que todos los tests de Phase 2 pasan junto con los 19 tests de Phase 0 + Phase 1
- **Dependencias**: Tarea 7.1 (build exitoso)
- **Criterios de validación**:
  - Exit code 0
  - Output contiene "96 passed" o "98 passed" (con 2 tests bonus)
  - Desglose esperado:
    * Phase 0+1: 19 tests (11 Phase 0 + 8 Phase 1)
    * Phase 2: 77-79 tests (6 IDs + 13 Money + 5 DateRange + 10 Transaction + 12 Category + 15 Budget + 8 Service + 8-10 Specs)
    * Total: 96-98 tests
  - No hay test skipped o failed

### Tarea 7.3 – Verificar cobertura Domain >= 100%
- **Path**: N/A (coverage report)
- **Acción**: Generar reporte de cobertura
- **Descripción**: Usar `coverlet` para generar reporte de cobertura del Domain layer usando comando:
  ```
  coverlet tests/SauronSheet.Domain.Tests/bin/Debug/net10.0/SauronSheet.Domain.Tests.dll \
    --target "dotnet" \
    --targetargs "test tests/SauronSheet.Domain.Tests/ --no-build --configuration Debug" \
    --format "opencover" \
    --output "./coverage-domain.xml" \
    --include "[SauronSheet.Domain]*" \
    --exclude "[SauronSheet.Domain.Tests]*"
  ```
  Phase 2 Domain debe tener 100% cobertura (fase domain-only mandate)
- **Dependencias**: Tarea 7.2 (tests verdes)
- **Criterios de validación**:
  - Archivo `coverage-domain.xml` generado
  - Phase 2 Domain (Entities/, ValueObjects/, Services/, Specifications/) = 100%
  - Overall Domain = 100% (fase domain-only)

### Tarea 7.4 – Auditar dependencias del proyecto
- **Path**: N/A (validation via file inspection)
- **Acción**: Validar dependencias
- **Descripción**: Verificar que Domain.csproj sigue teniendo CERO dependencias, que Application.csproj referencia SOLO Domain, que Infrastructure.csproj referencia ONLY Domain
- **Dependencias**: Tarea 7.1 (build completo)
- **Criterios de validación**:
  - Domain.csproj: 0 ProjectReferences, 0 PackageReferences
  - Application.csproj: 1 ProjectReference (Domain)
  - Infrastructure.csproj: 1 ProjectReference (Domain)
  - No hay violaciones de Clean Architecture

### Tarea 7.5 – Verificar que Phase 0 + Phase 1 tests siguen pasando (regresión)
- **Path**: N/A (command line)
- **Acción**: Ejecutar comandos
- **Descripción**: Ejecutar `dotnet test --filter "Category=Domain"` y `dotnet test --filter "Category=Application"` para verificar no hay regresiones
- **Dependencias**: Tarea 7.2 (todos Domain tests ejecutados)
- **Criterios de validación**:
  - Phase 0 Domain tests (11) aún pasan
  - Phase 1 Domain tests (8) aún pasan
  - Phase 0 Application tests (2) aún pasan
  - Phase 1 Application tests (14) aún pasan

### Tarea 7.6 – Audit final: Phase 2 es Domain-Only
- **Path**: N/A (validation)
- **Acción**: Auditar cambios
- **Descripción**: Verificar que ÚNICAMENTE se modificó/creó contenido en Domain/ y Domain.Tests/, sin tocar Application/, Infrastructure/, Frontend/
- **Dependencias**: Tarea 7.1 (build completo)
- **Criterios de validación**:
  - Git diff muestra SOLO cambios en Domain/ y Domain.Tests/
  - Phase scope boundary respetada (Domain-Only)
  - Constitutional compliance: NO Application commands/queries, NO Infrastructure impl, NO Frontend pages

---

## Orden de Implementación

Ejecutar las tareas en el siguiente orden secuencial:

1. Tarea 1.0 – Verificar que Phase 0 y Phase 1 están completos
2. Tarea 2.1 – Crear estructura de carpetas Domain para Phase 2
3. Tarea 2.2 – Crear estructura de carpetas Domain.Tests para Phase 2
4. Tarea 3.1 – Crear test stubs para Strong-Typed IDs (RED)
5. Tarea 3.2 – Implementar Strong-Typed IDs (GREEN)
6. Tarea 3.3 – Crear test stubs para Money & DateRange (RED)
7. Tarea 3.4 – Implementar Money (GREEN)
8. Tarea 3.5 – Implementar DateRange (GREEN)
9. Tarea 4.1 – Crear test stubs para Transaction (RED)
10. Tarea 4.2 – Implementar Transaction (GREEN)
11. Tarea 4.3 – Crear test stubs para Category (RED)
12. Tarea 4.4 – Implementar Category (GREEN)
13. Tarea 4.5 – Crear test stubs para Budget (RED)
14. Tarea 4.6 – Implementar Budget (GREEN)
15. Tarea 5.1 – Crear test stubs para CategoryService (RED)
16. Tarea 5.2 – Crear test stubs para Specifications (RED)
17. Tarea 5.3 – Implementar CategoryService (GREEN)
18. Tarea 5.4 – Crear BaseSpecification abstract class (GREEN)
19. Tarea 5.5 – Crear 4 Concrete Specifications (GREEN)
20. Tarea 6.1 – Crear ITransactionRepository interface
21. Tarea 6.2 – Crear ICategoryRepository interface
22. Tarea 6.3 – Crear IBudgetRepository interface
23. Tarea 7.1 – Ejecutar build completo de solución
24. Tarea 7.2 – Ejecutar todos los tests del Domain (100 tests)
25. Tarea 7.3 – Verificar cobertura Domain >= 100%
26. Tarea 7.4 – Auditar dependencias del proyecto
27. Tarea 7.5 – Verificar regresión en Phase 0 + Phase 1 tests
28. Tarea 7.6 – Audit final: Phase 2 es Domain-Only

---

**Total Tasks: 28**  
**Estimated Duration: 8 days (Weeks 6–8)**  
**Status**: Ready for implementation ✅
