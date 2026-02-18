# Phase 3 Tasks — Transaction Import Pipeline

**Version**: 1.0.0  
**Created**: 2026-02-15  
**Phase Type**: Full-Stack Features  
**Scope**: All Layers (Domain extensions + Application + Infrastructure + Frontend)  
**Duration**: Weeks 9–13  
**Expected Tests**: 170 total (121 Phase 0+1+2 + 49 Phase 3)  
**Phase 3 Breakdown**: 5 Domain + 33 Application + 11 Integration

---

## 1. Pre-Implementation Validation

### Tarea 1.0 – Verificar que Phase 0, 1, y 2 están completos
- **Path**: N/A (validation via command line)
- **Acción**: Ejecutar comandos de validación
- **Descripción**: Ejecutar `dotnet build` y `dotnet test` para asegurar que todas las fases anteriores compilan sin errores y todos los tests pasan (Phase 0+1+2: 121 tests total - 19 Domain base + 96 Domain Phase 2 + 2 Application base + 14 Application Phase 1 = 131 total, pero spec dice 121, verificar exacto count)
- **Dependencias**: Phase 0, 1, y 2 completadas
- **Criterios de validación**:
  - `dotnet build` exit code 0, cero warnings
  - `dotnet test` output contiene "121 passed" (verificar count exacto según spec)
  - `dotnet test --filter Category=Domain` retorna tests esperados
  - `dotnet test --filter Category=Application` retorna tests esperados
  - Git workspace limpio (no cambios uncommitted)

### Tarea 1.1 – Verificar que Phase 2 Domain está completo
- **Path**: N/A (validation)
- **Acción**: Validar
- **Descripción**: Verificar que existen todas las entidades de Phase 2 (Transaction, Category, Budget), value objects (TransactionId, CategoryId, BudgetId, Money, DateRange, UserId), domain services (CategoryService), y specifications necesarias para Phase 3
- **Dependencias**: Tarea 1.0
- **Criterios de validación**:
  - Archivo `src/SauronSheet.Domain/Entities/Transaction.cs` existe
  - Archivo `src/SauronSheet.Domain/Entities/Category.cs` existe
  - Archivo `src/SauronSheet.Domain/Entities/Budget.cs` existe
  - Archivo `src/SauronSheet.Domain/ValueObjects/Money.cs` existe
  - Archivo `src/SauronSheet.Domain/ValueObjects/DateRange.cs` existe
  - Todos los repositories interfaces (ITransactionRepository, ICategoryRepository, IBudgetRepository) existen en Domain/Repositories

---

## 2. Domain Layer Extensions

### Tarea 2.1 – Crear estructura de carpetas Domain para Phase 3
- **Path**: `src/SauronSheet.Domain/Entities/`, `src/SauronSheet.Domain/ValueObjects/`
- **Acción**: Verificar existencia (creadas en Phase 2)
- **Descripción**: Verificar que las carpetas necesarias existen. Si no existen (edge case), crearlas. Phase 2 debería haber creado `Entities/` y `ValueObjects/` ya.
- **Dependencias**: Tarea 1.1
- **Criterios de validación**: Directorios existen

### Tarea 2.2 – Crear estructura de carpetas Domain.Tests para Phase 3
- **Path**: `tests/SauronSheet.Domain.Tests/Entities/`, `tests/SauronSheet.Domain.Tests/ValueObjects/`
- **Acción**: Verificar existencia (creadas en Phase 2)
- **Descripción**: Verificar que las carpetas necesarias para tests de Phase 3 existen.
- **Dependencias**: Tarea 1.1
- **Criterios de validación**: Directorios existen

### Tarea 2.3 – Crear test stubs para ImportBatch Entity (RED phase)
- **Path**: `tests/SauronSheet.Domain.Tests/Entities/ImportBatchTests.cs`
- **Acción**: Crear archivo
- **Descripción**: Crear clase `ImportBatchTests` con 5 métodos de test stub (RED phase). Cada método debe tener `[Fact]` y `[Trait("Category", "Domain")]` y contener `Assert.True(false, "Implement ...")` para fallar intencionalmente:
  - `ImportBatch_ValidConstruction_SetsProperties()` → Test validación de construcción
  - `ImportBatch_NullFilename_ThrowsDomainException()` → Test guard de filename
  - `ImportBatch_NegativeCounts_ThrowsDomainException()` → Test guard de counts
  - `ImportBatch_EmptyFilename_ThrowsDomainException()` → Test guard de filename vacío
  - `ImportBatch_TransactionCount_CalculatesCorrectly()` → Test cálculo TotalProcessed (CRITICAL FIX I-4)
- **Dependencias**: Tarea 2.2
- **Criterios de validación**:
  - Archivo compila sin errores
  - 5 métodos presentes con `[Fact]` y `[Trait("Category", "Domain")]`
  - Todos contienen `Assert.True(false, ...)`

### Tarea 2.4 – Implementar ImportBatch Entity (GREEN phase)
- **Path**: `src/SauronSheet.Domain/Entities/ImportBatch.cs`
- **Acción**: Crear archivo
- **Descripción**: Implementar `ImportBatch` como **Entity** (NO Value Object - corrección de I-2 aplicada) que hereda de `Entity<Guid>` (NO AggregateRoot, es simple entity). Propiedades:
  - `Guid Id` (inherited from Entity<Guid>)
  - `string Filename` (private set)
  - `int ImportedCount` (private set)
  - `int SkippedCount` (private set)
  - `DateTime ImportedAt` (private set)
  - Constructor que valida:
    - `filename` no null/empty/whitespace → lanzar `DomainException`
    - `importedCount >= 0` → lanzar `DomainException` si negativo
    - `skippedCount >= 0` → lanzar `DomainException` si negativo
    - Asignar `ImportedAt = DateTime.UtcNow`
  - Property calculada: `int TotalProcessed => ImportedCount + SkippedCount` (CRITICAL FIX I-4)
  - Namespace: `SauronSheet.Domain.Entities`
- **Dependencias**: Tarea 2.3, Phase 0 (Entity<TId> base class)
- **Criterios de validación**:
  - Archivo compila sin errores
  - `dotnet test --filter ClassName=ImportBatchTests` pasa todos 5 tests
  - `dotnet test --filter Category=Domain` retorna 126 tests passing (121 Phase 0+1+2 + 5 Phase 3)

### Tarea 2.5 – Crear IPdfImportRepository interface en Domain (CRITICAL FIX C-2)
- **Path**: `src/SauronSheet.Domain/Repositories/IPdfImportRepository.cs`
- **Acción**: Crear archivo
- **Descripción**: Crear interfaz `IPdfImportRepository` en Domain layer (NO en Infrastructure). Métodos:
  - `Task AddAsync(ImportBatch importBatch)`
  - `Task<IReadOnlyList<ImportBatch>> GetByUserIdAsync(UserId userId)`
  - XML comments documentando que es Domain contract, implementación en Infrastructure
  - CRITICAL FIX C-2: Movido de Infrastructure a Domain para cumplir Clean Architecture
- **Dependencias**: Tarea 2.4 (ImportBatch), Phase 1 (UserId)
- **Criterios de validación**:
  - Archivo compila sin errores
  - Interfaz es usable desde Application layer (no depende de Infrastructure)

### Tarea 2.6 – Agregar GetCountsByCategoriesAsync a ITransactionRepository (CRITICAL FIX I-4)
- **Path**: `src/SauronSheet.Domain/Repositories/ITransactionRepository.cs`
- **Acción**: Modificar archivo (agregar método)
- **Descripción**: Agregar método a la interfaz existente (creada en Phase 2):
```csharp
/// <summary>
/// Gets transaction counts grouped by category.
/// CRITICAL FIX I-4: Added to support CategoryDto.TransactionCount calculation.
/// </summary>
/// <param name="categoryIds">List of category IDs to count transactions for</param>
  /// <returns>Dictionary mapping CategoryId to transaction count</returns>
  Task<Dictionary<CategoryId, int>> GetCountsByCategoriesAsync(List<CategoryId> categoryIds);
```
  - Agregar XML comments como se muestra arriba
  - No implementar (es interfaz)
- **Dependencias**: Phase 2 (ITransactionRepository existe)
- **Criterios de validación**:
  - Archivo compila sin errores
  - Método agregado correctamente a la interfaz

### Tarea 2.7 – Ejecutar tests Domain en GREEN phase
- **Path**: N/A (command line)
- **Acción**: Ejecutar comando
- **Descripción**: Ejecutar `dotnet test --filter Category=Domain --no-build` para verificar que todos los tests de Domain pasan (Phase 0+1+2: 121 tests + Phase 3: 5 tests = 126 total)
- **Dependencias**: Tarea 2.4
- **Criterios de validación**:
  - `dotnet test --filter Category=Domain --no-build` retorna "126 passed"
  - Exit code 0

---

## 3. Application Layer Tests (RED Phase)

### Tarea 3.1 – Crear estructura de carpetas Application.Tests para Phase 3
- **Path**: `tests/SauronSheet.Application.Tests/Features/Transactions/Commands/`, `tests/SauronSheet.Application.Tests/Features/Transactions/Queries/`, `tests/SauronSheet.Application.Tests/Features/Categories/Commands/`, `tests/SauronSheet.Application.Tests/Features/Categories/Queries/`
- **Acción**: Crear directorios
- **Descripción**: Crear estructura de carpetas para tests de handlers de Phase 3:
  - `Features/Transactions/Commands/` - Tests de transaction commands
  - `Features/Transactions/Queries/` - Tests de transaction queries
  - `Features/Categories/Commands/` - Tests de category commands
  - `Features/Categories/Queries/` - Tests de category queries
- **Dependencias**: Phase 0 (Application.Tests project existe)
- **Criterios de validación**: Todos los directorios existen y están vacíos

### Tarea 3.2 – Crear test stubs para Transaction Commands (RED phase)
- **Path**: `tests/SauronSheet.Application.Tests/Features/Transactions/Commands/` (múltiples archivos)
- **Acción**: Crear 4 archivos
- **Descripción**: Crear test classes con stubs para transaction command handlers (16 tests total):
  - `ImportTransactionsFromPdfCommandTests.cs` (4 tests)
  - `CreateTransactionCommandTests.cs` (4 tests)
  - `UpdateTransactionCategoryCommandTests.cs` (3 tests)
  - `DeleteTransactionCommandTests.cs` (5 tests)
  - Todos con `[Fact]` y `[Trait("Category", "Application")]` y `Assert.True(false, "Implement ...")`
- **Dependencias**: Tarea 3.1
- **Criterios de validación**:
  - 4 archivos compilan
  - 16 tests descubiertos
  - Todos fallan (RED phase)

### Tarea 3.3 – Crear test stubs para Transaction Queries (RED phase)
- **Path**: `tests/SauronSheet.Application.Tests/Features/Transactions/Queries/GetTransactionsQueryTests.cs`
- **Acción**: Crear archivo
- **Descripción**: Crear test class con 6 test methods para GetTransactionsQuery handler:
  - `GetTransactions_ReturnsOnlyUserTransactions()`
  - `GetTransactions_Paginated_RespectsPageSize()`
  - `GetTransactions_DefaultSorting_ByDateDesc()`
  - `GetTransactions_EmptyResult_ReturnsEmptyList()`
  - `GetTransactions_FilterByCategory_AppliesFilter()`
  - `GetTransactions_FilterByDateRange_AppliesFilter()`
- **Dependencias**: Tarea 3.1
- **Criterios de validación**:
  - Archivo compila
  - 6 tests descubiertos
  - Todos fallan (RED phase)

### Tarea 3.4 – Crear test stubs para Category Commands (RED phase)
- **Path**: `tests/SauronSheet.Application.Tests/Features/Categories/Commands/` (múltiples archivos)
- **Acción**: Crear 4 archivos
- **Descripción**: Crear test classes para category command handlers (11 tests total):
  - `CreateCategoryCommandTests.cs` (3 tests)
  - `RenameCategoryCommandTests.cs` (3 tests)
  - `DeleteCategoryCommandTests.cs` (3 tests)
  - `SeedSystemDefaultsCommandTests.cs` (2 tests)
- **Dependencias**: Tarea 3.1
- **Criterios de validación**:
  - 4 archivos compilan
  - 11 tests descubiertos
  - Todos fallan (RED phase)

### Tarea 3.5 – Crear test stubs para Category Queries (RED phase)
- **Path**: `tests/SauronSheet.Application.Tests/Features/Categories/Queries/GetCategoriesQueryTests.cs`
- **Acción**: Crear archivo
- **Descripción**: Crear test class con 4 test methods para GetCategoriesQuery handler (incluyendo test de seeding idempotente):
  - `GetCategories_ReturnsUserCategories()`
  - `GetCategories_IncludesSystemDefaults()`
  - `GetCategories_SortedCorrectly()` (system defaults first, then alphabetically)
  - `GetCategories_NoSystemDefaults_SeedsAutomatically()` (verifica seeding via MediatR - CLARIFICATION A-1)
  - Nota: Test de TransactionCount calculation será en tarea posterior (implementación handler)
- **Dependencias**: Tarea 3.1
- **Criterios de validación**:
  - Archivo compila
  - 4 tests descubiertos
  - Todos fallan (RED phase)

### Tarea 3.6 – Ejecutar tests Application en RED phase
- **Path**: N/A (command line)
- **Acción**: Ejecutar comando
- **Descripción**: Ejecutar `dotnet test --filter Category=Application --no-build` para verificar que los nuevos 33 tests de Application fallan (RED phase) y los 16 tests anteriores (Phase 0+1) aún pasan
- **Dependencias**: Tareas 3.2 a 3.5
- **Criterios de validación**:
  - Output contiene "33 failed" (tests nuevos Phase 3)
  - Output contiene "16 passed" (tests Phase 0+1)
  - Total: 49 tests discovered

---

## 4. Application Layer - DTOs & Commands

### Tarea 4.1 – Crear estructura de carpetas Application para Phase 3
- **Path**: `src/SauronSheet.Application/Features/Transactions/Commands/`, `src/SauronSheet.Application/Features/Transactions/Queries/`, `src/SauronSheet.Application/Features/Transactions/DTOs/`, `src/SauronSheet.Application/Features/Categories/Commands/`, `src/SauronSheet.Application/Features/Categories/Queries/`, `src/SauronSheet.Application/Features/Categories/DTOs/`
- **Acción**: Crear directorios
- **Descripción**: Crear estructura de carpetas para features de Phase 3:
  - `Features/Transactions/Commands/` - Transaction command records y handlers
  - `Features/Transactions/Queries/` - Transaction query records y handlers
  - `Features/Transactions/DTOs/` - Transaction DTOs
  - `Features/Categories/Commands/` - Category command records y handlers
  - `Features/Categories/Queries/` - Category query records y handlers
  - `Features/Categories/DTOs/` - Category DTOs
- **Dependencias**: Phase 0 (Application project existe)
- **Criterios de validación**: Todos los directorios existen y están vacíos

### Tarea 4.2 – Crear DTOs de Transaction y Category
- **Path**: `src/SauronSheet.Application/Features/Transactions/DTOs/` y `src/SauronSheet.Application/Features/Categories/DTOs/`
- **Acción**: Crear 5 archivos
- **Descripción**: Crear record DTOs:
  - `TransactionDto.cs`: `record TransactionDto(Guid Id, decimal Amount, string Currency, DateTime Date, string Description, Guid? CategoryId, string? ImportedFrom)`
  - `ImportResultDto.cs`: `record ImportResultDto(int ImportedCount, int SkippedCount, int TotalProcessed, string Filename, DateTime ImportedAt, List<ImportRowErrorDto> Errors)`
  - `ImportRowErrorDto.cs`: `record ImportRowErrorDto(int RowNumber, string RawData, string ErrorMessage)`
  - `CategoryDto.cs`: `record CategoryDto(Guid Id, string Name, string? Color, string? Icon, bool IsSystemDefault, int TransactionCount)` (CRITICAL FIX I-4: TransactionCount property)
  - `PaginatedResultDto<T>.cs`: generic record con `List<T> Items, int TotalCount, int PageNumber, int PageSize, int TotalPages` (CLARIFICATION A-4: TotalPages = (int)Math.Ceiling(TotalCount / (double)PageSize))
- **Dependencias**: Tarea 4.1
- **Criterios de validación**:
  - Todos los archivos compilan
  - DTOs son records (no clases)
  - Todas las propiedades presentes

### Tarea 4.3 – Crear Transaction Commands
- **Path**: `src/SauronSheet.Application/Features/Transactions/Commands/` (múltiples archivos)
- **Acción**: Crear 4 archivos
- **Descripción**: Crear command records que implementan `IRequest<...>`:
  - `ImportTransactionsFromPdfCommand.cs`: `record ImportTransactionsFromPdfCommand(Stream PdfStream, string Filename) : IRequest<ImportResultDto>`
  - `CreateTransactionCommand.cs`: `record CreateTransactionCommand(decimal Amount, string Currency, DateTime Date, string Description, Guid? CategoryId) : IRequest<Guid>`
  - `UpdateTransactionCategoryCommand.cs`: `record UpdateTransactionCategoryCommand(Guid TransactionId, Guid? CategoryId) : IRequest<Unit>`
  - `DeleteTransactionCommand.cs`: `record DeleteTransactionCommand(Guid TransactionId) : IRequest<Unit>`
  - Todos en namespace `SauronSheet.Application.Features.Transactions.Commands`
- **Dependencias**: Tarea 4.1
- **Criterios de validación**:
  - Todos los archivos compilan
  - Commands son records
  - Implementan IRequest<T> correctamente

### Tarea 4.4 – Crear Transaction Queries
- **Path**: `src/SauronSheet.Application/Features/Transactions/Queries/GetTransactionsQuery.cs`
- **Acción**: Crear archivo
- **Descripción**: Crear query record:
  - `GetTransactionsQuery.cs`: `record GetTransactionsQuery(int PageNumber = 1, int PageSize = 50, Guid? CategoryId = null, DateTime? StartDate = null, DateTime? EndDate = null) : IRequest<PaginatedResultDto<TransactionDto>>`
  - Namespace: `SauronSheet.Application.Features.Transactions.Queries`
- **Dependencias**: Tarea 4.1, Tarea 4.2 (DTOs)
- **Criterios de validación**:
  - Archivo compila
  - Query es record
  - Implementa IRequest<PaginatedResultDto<TransactionDto>>

### Tarea 4.5 – Crear Category Commands
- **Path**: `src/SauronSheet.Application/Features/Categories/Commands/` (múltiples archivos)
- **Acción**: Crear 4 archivos
- **Descripción**: Crear command records:
  - `CreateCategoryCommand.cs`: `record CreateCategoryCommand(string Name, string? Color = null, string? Icon = null) : IRequest<Guid>`
  - `RenameCategoryCommand.cs`: `record RenameCategoryCommand(Guid CategoryId, string NewName) : IRequest<Unit>`
  - `DeleteCategoryCommand.cs`: `record DeleteCategoryCommand(Guid CategoryId) : IRequest<Unit>`
  - `SeedSystemDefaultsCommand.cs`: `record SeedSystemDefaultsCommand() : IRequest<List<Guid>>` (retorna IDs creados)
  - Todos en namespace `SauronSheet.Application.Features.Categories.Commands`
- **Dependencias**: Tarea 4.1
- **Criterios de validación**:
  - Todos los archivos compilan
  - Commands son records
  - Implementan IRequest<T> correctamente

### Tarea 4.6 – Crear Category Queries
- **Path**: `src/SauronSheet.Application/Features/Categories/Queries/GetCategoriesQuery.cs`
- **Acción**: Crear archivo
- **Descripción**: Crear query record:
  - `GetCategoriesQuery.cs`: `record GetCategoriesQuery() : IRequest<List<CategoryDto>>`
  - Namespace: `SauronSheet.Application.Features.Categories.Queries`
- **Dependencias**: Tarea 4.1, Tarea 4.2 (DTOs)
- **Criterios de validación**:
  - Archivo compila
  - Query es record
  - Implementa IRequest<List<CategoryDto>>

---

## 5. Application Layer - Common & Interfaces

### Tarea 5.1 – Crear IPdfParser interface en Application layer
- **Path**: `src/SauronSheet.Application/Interfaces/IPdfParser.cs`
- **Acción**: Crear archivo
- **Descripción**: Crear interfaz `IPdfParser` con método:
  - `Task<List<RawTransactionRow>> ParseAsync(Stream pdfStream)`
  - Crear directorio `Interfaces/` si no existe
  - Namespace: `SauronSheet.Application.Interfaces`
- **Dependencias**: Tarea 4.1
- **Criterios de validación**:
  - Archivo compila
  - Interfaz tiene método ParseAsync

### Tarea 5.2 – Crear RawTransactionRow model
- **Path**: `src/SauronSheet.Application/Common/Models/RawTransactionRow.cs`
- **Acción**: Crear archivo
- **Descripción**: Crear record `RawTransactionRow` para representar filas parseadas de PDF antes de validación de dominio:
  - `record RawTransactionRow(int RowNumber, string DateRaw, string DescriptionRaw, string AmountRaw, string? CurrencyRaw = null)`
  - Crear directorio `Common/Models/` si no existe
  - Namespace: `SauronSheet.Application.Common.Models`
- **Dependencias**: Tarea 4.1
- **Criterios de validación**:
  - Archivo compila
  - RawTransactionRow es record con todas las propiedades

---

## 6. Application Layer - Handlers (GREEN Phase)

### Tarea 6.1 – Implementar ImportTransactionsFromPdfCommandHandler (GREEN)
- **Path**: `src/SauronSheet.Application/Features/Transactions/Commands/ImportTransactionsFromPdfCommandHandler.cs`
- **Acción**: Crear archivo
- **Descripción**: Implementar handler `ImportTransactionsFromPdfCommandHandler : IRequestHandler<ImportTransactionsFromPdfCommand, ImportResultDto>`. Constructor inyecta:
  - `IPdfParser _pdfParser`
  - `ITransactionRepository _transactionRepo`
  - `ICategoryRepository _categoryRepo`
  - `IPdfImportRepository _pdfImportRepo` (CRITICAL FIX C-2)
  - `IUserContext _userContext`
  - `IMediator _mediator` (para seeding system defaults si es necesario - CLARIFICATION A-1)
  - Método `Handle`:
    1. Validar file != null, filename != empty
    2. Get UserId from `_userContext.UserId`
    3. **Seed system defaults if not present** via `_mediator.Send(new SeedSystemDefaultsCommand())` (NOT inline check - CLARIFICATION A-1)
    4. Parse PDF via `_pdfParser.ParseAsync(pdfStream)` → `List<RawTransactionRow>`
    5. For each row: validate, parse date/amount, check duplicate via `_transactionRepo.ExistsDuplicateAsync(userId, date, amount, description)` (CRITICAL FIX C-3: ignores currency in duplicate check)
    6. Create Transaction entities for valid rows, persist via `_transactionRepo.AddAsync(...)`
    7. Track skipped rows with ImportRowErrorDto (reason: validation error, duplicate, etc.)
    8. **Save import metadata** via `_pdfImportRepo.AddAsync(new ImportBatch(...))` (CRITICAL FIX C-2)
    9. Return ImportResultDto with counts and errors
- **Dependencias**: Tarea 4.3, Tarea 5.1, Tarea 5.2, Domain (Transaction, ImportBatch), Phase 1 (IUserContext)
- **Criterios de validación**:
  - Archivo compila sin errores
  - Tests T-3.01 a T-3.03 pasan (import handler tests)

### Tarea 6.2 – Implementar CreateTransactionCommandHandler (GREEN)
- **Path**: `src/SauronSheet.Application/Features/Transactions/Commands/CreateTransactionCommandHandler.cs`
- **Acción**: Crear archivo
- **Descripción**: Implementar handler para crear transaction manual. Constructor inyecta `ITransactionRepository`, `IUserContext`. Método `Handle`:
  - Get UserId from `_userContext.UserId`
  - Crear TransactionId (Guid.NewGuid())
  - Validar CategoryId existe si no es null (via `_categoryRepo.ExistsAsync` o catch EntityNotFoundException)
  - Crear entity Transaction con domain validation
  - Persistir via `_transactionRepo.AddAsync(...)`
  - Retornar TransactionId.Value (Guid)
- **Dependencias**: Tarea 4.3, Domain (Transaction), Phase 1 (IUserContext)
- **Criterios de validación**:
  - Archivo compila
  - Tests T-3.04 y T-3.05 pasan

### Tarea 6.3 – Implementar UpdateTransactionCategoryCommandHandler (GREEN)
- **Path**: `src/SauronSheet.Application/Features/Transactions/Commands/UpdateTransactionCategoryCommandHandler.cs`
- **Acción**: Crear archivo
- **Descripción**: Implementar handler para categorizar transaction. Constructor inyecta `ITransactionRepository`, `IUserContext`. Método `Handle`:
  - Get transaction via `GetByIdAsync(TransactionId)`
  - Verify UserId matches (tenant scoping)
  - Call `transaction.Categorize(categoryId)` (domain method)
  - Persistir via `_transactionRepo.UpdateAsync(transaction)`
  - Retornar Unit.Value
- **Dependencias**: Tarea 4.3, Domain (Transaction)
- **Criterios de validación**:
  - Archivo compila
  - Test T-3.06 pasa

### Tarea 6.4 – Implementar DeleteTransactionCommandHandler (GREEN)
- **Path**: `src/SauronSheet.Application/Features/Transactions/Commands/DeleteTransactionCommandHandler.cs`
- **Acción**: Crear archivo
- **Descripción**: Implementar handler para eliminar transaction. Constructor inyecta `ITransactionRepository`, `IUserContext`. Método `Handle`:
  - Get transaction via `GetByIdAsync(TransactionId)`
  - Verify UserId matches (or throw EntityNotFoundException if belongs to different user - tenant scoping)
  - Delete via `_transactionRepo.DeleteAsync(transactionId)`
  - Retornar Unit.Value
- **Dependencias**: Tarea 4.3, Domain (Transaction)
- **Criterios de validación**:
  - Archivo compila
  - Tests T-3.07 y T-3.08 pasan

### Tarea 6.5 – Implementar GetTransactionsQueryHandler (GREEN)
- **Path**: `src/SauronSheet.Application/Features/Transactions/Queries/GetTransactionsQueryHandler.cs`
- **Acción**: Crear archivo
- **Descripción**: Implementar handler para query paginada de transactions. Constructor inyecta `ITransactionRepository`, `IUserContext`. Método `Handle`:
  - Get UserId from `_userContext.UserId`
  - Build specification chain: UserSpec AND (optionally) CategorySpec AND (optionally) DateRangeSpec
  - Aplicar paginación en handler: skip = (pageNumber - 1) * pageSize, take = pageSize
  - Get transactions via `FindBySpecificationAsync(compositeSpec)` (implementation in Infrastructure will handle limit)
  - Get total count via `GetByUserIdAsync(userId).Count` (for pagination metadata)
  - Map to TransactionDto
  - Retornar PaginatedResultDto<TransactionDto> con TotalPages calculado (CLARIFICATION A-4)
- **Dependencias**: Tarea 4.4, Domain (Specifications)
- **Criterios de validación**:
  - Archivo compila
  - Tests T-3.09 y T-3.10 pasan

### Tarea 6.6 – Implementar CreateCategoryCommandHandler (GREEN)
- **Path**: `src/SauronSheet.Application/Features/Categories/Commands/CreateCategoryCommandHandler.cs`
- **Acción**: Crear archivo
- **Descripción**: Implementar handler para crear category. Constructor inyecta `ICategoryRepository`, `CategoryService`, `IUserContext`. Método `Handle`:
  - Get UserId from `_userContext.UserId`
  - Validar unique name via `_categoryService.ValidateUniqueName(userId, request.Name)` (lanza exception si duplicado)
  - Crear CategoryId (Guid.NewGuid())
  - Crear entity Category (user-defined, NOT system default)
  - Persistir via `_categoryRepo.AddAsync(category)`
  - Retornar CategoryId.Value (Guid)
- **Dependencias**: Tarea 4.5, Domain (Category, CategoryService)
- **Criterios de validación**:
  - Archivo compila
  - Tests T-3.11 y T-3.12 pasan

### Tarea 6.7 – Implementar RenameCategoryCommandHandler (GREEN)
- **Path**: `src/SauronSheet.Application/Features/Categories/Commands/RenameCategoryCommandHandler.cs`
- **Acción**: Crear archivo
- **Descripción**: Implementar handler para renombrar category. Constructor inyecta `ICategoryRepository`, `CategoryService`, `IUserContext`. Método `Handle`:
  - Get category via `GetByIdAsync(CategoryId)`
  - Verify UserId matches
  - Call `category.Rename(newName)` (domain method, throws if system default)
  - Validar unique name via `_categoryService.ValidateUniqueName(userId, newName)` (opcional, Rename podría hacer esto internamente)
  - Persistir via `_categoryRepo.UpdateAsync(category)`
  - Retornar Unit.Value
- **Dependencias**: Tarea 4.5, Domain (Category)
- **Criterios de validación**:
  - Archivo compila
  - Test T-3.13 pasa (intento de rename system default → exception)

### Tarea 6.8 – Implementar DeleteCategoryCommandHandler (GREEN)
- **Path**: `src/SauronSheet.Application/Features/Categories/Commands/DeleteCategoryCommandHandler.cs`
- **Acción**: Crear archivo
- **Descripción**: Implementar handler para eliminar category. Constructor inyecta `ICategoryRepository`, `ITransactionRepository`, `CategoryService`, `IUserContext`. Método `Handle`:
  - Get category via `GetByIdAsync(CategoryId)`
  - Verify UserId matches
  - Check if has transactions via `_transactionRepo.HasTransactionsAsync(categoryId)` (o count via repository)
  - Validar can delete via `_categoryService.CanDeleteCategory(category, hasTransactions)` (domain logic)
  - Delete via `_categoryRepo.DeleteAsync(categoryId)`
  - Retornar Unit.Value
- **Dependencias**: Tarea 4.5, Domain (Category, CategoryService)
- **Criterios de validación**:
  - Archivo compila
  - Test T-3.14 pasa (category with transactions → exception)

### Tarea 6.9 – Implementar SeedSystemDefaultsCommandHandler (GREEN)
- **Path**: `src/SauronSheet.Application/Features/Categories/Commands/SeedSystemDefaultsCommandHandler.cs`
- **Acción**: Crear archivo
- **Descripción**: Implementar handler para seeding de categorías default. Constructor inyecta `ICategoryRepository`, `CategoryService`, `IUserContext`. Método `Handle`:
  - Get UserId from `_userContext.UserId`
  - Call `_categoryService.GetSystemDefaults(userId)` para obtener 4 categorías default
  - For each default category: persistir via `_categoryRepo.AddAsync(category)`
  - Retornar lista de CategoryIds creados
  - **Idempotencia**: Handler puede verificar si ya existen defaults via `_categoryRepo.GetSystemDefaultsAsync(userId)` y retornar IDs existentes si ya están seeded
- **Dependencias**: Tarea 4.5, Domain (CategoryService)
- **Criterios de validación**:
  - Archivo compila
  - Test T-3.15 pasa (seeding crea exactamente 4)

### Tarea 6.10 – Implementar GetCategoriesQueryHandler (GREEN)
- **Path**: `src/SauronSheet.Application/Features/Categories/Queries/GetCategoriesQueryHandler.cs`
- **Acción**: Crear archivo
- **Descripción**: Implementar handler para query de categories. Constructor inyecta `ICategoryRepository`, `ITransactionRepository`, `IUserContext`, `IMediator`. Método `Handle`:
  - Get UserId from `_userContext.UserId`
  - **Seed system defaults if not present** via `_mediator.Send(new SeedSystemDefaultsCommand())` (idempotent) (CLARIFICATION A-1: via MediatR, NO inline)
  - Get categories via `_categoryRepo.GetByUserIdAsync(userId)`
  - **Calculate TransactionCount per category** via `_transactionRepo.GetCountsByCategoriesAsync(categoryIds)` (CRITICAL FIX I-4)
  - Map to CategoryDto (incluir TransactionCount en DTO)
  - Sort: system defaults first, then user-defined alphabetically
  - Retornar List<CategoryDto>
- **Dependencias**: Tarea 4.6, Domain (Category), Tarea 2.6 (GetCountsByCategoriesAsync)
- **Criterios de validación**:
  - Archivo compila
  - Test T-3.16 pasa (includes system defaults)

### Tarea 6.11 – Ejecutar tests Application para Handlers (GREEN)
- **Path**: N/A (command line)
- **Acción**: Ejecutar comando
- **Descripción**: Ejecutar `dotnet test --filter Category=Application --no-build` para verificar que todos los 49 tests de Application pasan (16 Phase 0+1 + 33 Phase 3)
- **Dependencias**: Tareas 6.1 a 6.10
- **Criterios de validación**:
  - Output contiene "49 passed"
  - Exit code 0
  - Todos los handlers compilan sin errores

---

## 7. Infrastructure Layer - NuGet Packages

### Tarea 7.1 – Agregar NuGet package PdfPig (CLARIFICATION A-3)
- **Path**: `src/SauronSheet.Infrastructure/SauronSheet.Infrastructure.csproj`
- **Acción**: Agregar dependencia
- **Descripción**: Agregar el NuGet package `UglyToad.PdfPig` **versión 0.1.8** al proyecto Infrastructure. Comando: `dotnet add src/SauronSheet.Infrastructure/SauronSheet.Infrastructure.csproj package UglyToad.PdfPig --version 0.1.8`
- **Dependencias**: Phase 0 (Infrastructure project existe)
- **Criterios de validación**:
  - Ejecutar `dotnet build src/SauronSheet.Infrastructure/`
  - Build exitoso sin errores
  - Package reference con versión 0.1.8 en .csproj

---

## 8. Infrastructure Layer - PDF Parsing

### Tarea 8.1 – Crear directorio PDF en Infrastructure
- **Path**: `src/SauronSheet.Infrastructure/PDF/`, `src/SauronSheet.Infrastructure/PDF/Parsers/`
- **Acción**: Crear directorios
- **Descripción**: Crear estructura de carpetas para PDF parsing:
  - `PDF/` - Raíz de parsing
  - `PDF/Parsers/` - Implementaciones específicas de parsers
- **Dependencias**: Phase 0 (Infrastructure project existe)
- **Criterios de validación**: Directorios existen y están vacíos

### Tarea 8.2 – Implementar PdfParserFactory
- **Path**: `src/SauronSheet.Infrastructure/PDF/PdfParserFactory.cs`
- **Acción**: Crear archivo
- **Descripción**: Implementar factory class `PdfParserFactory` con método:
  - `public IPdfParser CreateParser(string? bankIdentifier = null)`
  - Strategy pattern: retorna parser específico según identifier
  - Default: `GenericBankPdfParser`
  - Future: switch statement para bank-specific parsers
  - Namespace: `SauronSheet.Infrastructure.PDF`
- **Dependencias**: Tarea 8.1, Application (IPdfParser interface)
- **Criterios de validación**:
  - Archivo compila
  - Factory retorna GenericBankPdfParser por default

### Tarea 8.3 – Implementar GenericBankPdfParser (GREEN con NC-3 mitigations)
- **Path**: `src/SauronSheet.Infrastructure/PDF/Parsers/GenericBankPdfParser.cs`
- **Acción**: Crear archivo
- **Descripción**: Implementar parser `GenericBankPdfParser : IPdfParser`. Método `ParseAsync(Stream pdfStream)`:
  - Usar PdfPig para abrir PDF: `PdfDocument.Open(pdfStream)`
  - **CRITICAL FIX NC-3: Error handling para encoding issues**:
    - Wrap parsing en try-catch
    - Catch `PdfDocumentFormatException` (password-protected/corrupted PDFs)
    - Catch encoding errors (UTF-16, scanned PDFs)
    - Log warnings y retornar rows parciales si es posible
    - Lanzar `InvalidOperationException` con mensaje descriptivo para usuario
  - Extract text de cada page via `page.Text`
  - Parse lines into RawTransactionRow (heuristic: split by whitespace, extract date/description/amount/currency)
  - Retornar `List<RawTransactionRow>`
  - Namespace: `SauronSheet.Infrastructure.PDF.Parsers`
  - **Using statements**: usar full namespace `SauronSheet.Application.Common.Models` (CLARIFICATION A-7)
- **Dependencias**: Tarea 8.1, Tarea 7.1 (PdfPig package), Application (RawTransactionRow)
- **Criterios de validación**:
  - Archivo compila
  - Parser puede abrir PDF simple y extraer texto
  - Error handling para PDFs malformed/password-protected

---

## 9. Infrastructure Layer - Persistence (Repositories)

### Tarea 9.1 – Crear directorio Persistence en Infrastructure
- **Path**: `src/SauronSheet.Infrastructure/Persistence/`
- **Acción**: Verificar existencia (puede haber sido creada en Phase 1)
- **Descripción**: Verificar que el directorio existe. Si no existe, crearlo.
- **Dependencias**: Phase 0 (Infrastructure project existe)
- **Criterios de validación**: Directorio existe

### Tarea 9.2 – Implementar SupabaseTransactionRepository (GREEN con stubs)
- **Path**: `src/SauronSheet.Infrastructure/Persistence/SupabaseTransactionRepository.cs`
- **Acción**: Crear archivo
- **Descripción**: Implementar `SupabaseTransactionRepository : ITransactionRepository`. Constructor inyecta `Supabase.Client _client` (CRITICAL FIX C-1: registrado en DI). Métodos:
  - `GetByIdAsync(TransactionId)`, `GetByUserIdAsync(UserId)`, `FindBySpecificationAsync(ISpecification<Transaction>)`, `AddAsync(Transaction)`, `UpdateAsync(Transaction)`, `DeleteAsync(TransactionId)`, `ExistsAsync(TransactionId)`
  - `ExistsDuplicateAsync(UserId, DateTime, decimal, string)` - **CRITICAL FIX C-3: duplicate detection ignores currency** (documented in XML comments)
  - `GetCountsByCategoriesAsync(List<CategoryId>)` - **CRITICAL FIX I-4: batch count query**
  - **Nota**: Implementación completa de Supabase queries deferred to actual Phase 3 coding (stubs con TODO comments acceptable for now)
  - Namespace: `SauronSheet.Infrastructure.Persistence`
- **Dependencias**: Tarea 9.1, Domain (ITransactionRepository, Transaction)
- **Criterios de validación**:
  - Archivo compila
  - Implementa ITransactionRepository completamente (con TODOs aceptables)

### Tarea 9.3 – Implementar SupabaseCategoryRepository (GREEN con stubs)
- **Path**: `src/SauronSheet.Infrastructure/Persistence/SupabaseCategoryRepository.cs`
- **Acción**: Crear archivo
- **Descripción**: Implementar `SupabaseCategoryRepository : ICategoryRepository`. Constructor inyecta `Supabase.Client _client`. Métodos:
  - `GetByIdAsync(CategoryId)`, `GetByUserIdAsync(UserId)`, `FindByNameAndUserAsync(UserId, string)`, `GetSystemDefaultsAsync(UserId)`, `AddAsync(Category)`, `UpdateAsync(Category)`, `DeleteAsync(CategoryId)`, `HasTransactionsAsync(CategoryId)`
  - Namespace: `SauronSheet.Infrastructure.Persistence`
- **Dependencias**: Tarea 9.1, Domain (ICategoryRepository, Category)
- **Criterios de validación**:
  - Archivo compila
  - Implementa ICategoryRepository completamente (stubs aceptables)

### Tarea 9.4 – Implementar SupabasePdfImportRepository (GREEN con stubs)
- **Path**: `src/SauronSheet.Infrastructure/Persistence/SupabasePdfImportRepository.cs`
- **Acción**: Crear archivo
- **Descripción**: Implementar `SupabasePdfImportRepository : IPdfImportRepository` (CRITICAL FIX C-2: interfaz definida en Domain). Constructor inyecta `Supabase.Client _client`. Métodos:
  - `AddAsync(ImportBatch)`, `GetByUserIdAsync(UserId)`
  - Namespace: `SauronSheet.Infrastructure.Persistence`
- **Dependencies**: Tarea 9.1, Domain (IPdfImportRepository, ImportBatch)
- **Criterios de validación**:
  - Archivo compila
  - Implementa IPdfImportRepository completamente

---

## 10. Infrastructure Layer - Database Migrations

### Tarea 10.1 – Crear directorio Migrations en Infrastructure
- **Path**: `src/SauronSheet.Infrastructure/Persistence/Migrations/`
- **Acción**: Crear directorio (o verificar si existe de Phase 1)
- **Descripción**: Crear directorio para alojar scripts SQL de migración.
- **Dependencias**: Tarea 9.1
- **Criterios de validación**: Directorio existe

### Tarea 10.2 – Crear migration 001_CreateUsersTable.sql (CRITICAL FIX I-3)
- **Path**: `src/SauronSheet.Infrastructure/Persistence/Migrations/001_CreateUsersTable.sql`
- **Acción**: Crear archivo
- **Descripción**: Crear migration SQL para tabla `users` (referenciada en plan pero faltante - CRITICAL FIX I-3):
```sql
-- Migration: 001_CreateUsersTable.sql
-- Purpose: User profile table (Supabase Auth manages auth.users)
-- NOTE: This migration should have been in Phase 1, but is added here as prerequisite

CREATE TABLE IF NOT EXISTS public.users (
    id UUID PRIMARY KEY REFERENCES auth.users(id) ON DELETE CASCADE,
    email TEXT NOT NULL UNIQUE,
    display_name TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ
);

CREATE INDEX IF NOT EXISTS idx_users_email ON public.users(email);

ALTER TABLE public.users ENABLE ROW LEVEL SECURITY;

CREATE POLICY "Users can view own profile"
    ON public.users FOR SELECT
    USING (auth.uid() = id);

CREATE POLICY "Users can update own profile"
    ON public.users FOR UPDATE
    USING (auth.uid() = id)
    WITH CHECK (auth.uid() = id);
```
- **Dependencias**: Tarea 10.1
- **Criterios de validación**:
  - Archivo contiene SQL válido
  - Puede ser ejecutado en Supabase SQL Editor

### Tarea 10.3 – Crear migration 002_CreateCategoriesTable.sql
- **Path**: `src/SauronSheet.Infrastructure/Persistence/Migrations/002_CreateCategoriesTable.sql`
- **Acción**: Crear archivo
- **Descripción**: Crear migration SQL para tabla `categories`:
```sql
CREATE TABLE IF NOT EXISTS public.categories (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL REFERENCES public.users(id) ON DELETE CASCADE,
    name VARCHAR(100) NOT NULL,
    color VARCHAR(7),
    icon VARCHAR(50),
    is_system_default BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ,
    UNIQUE(user_id, name)
);

CREATE INDEX IF NOT EXISTS idx_categories_user ON public.categories(user_id);

ALTER TABLE public.categories ENABLE ROW LEVEL SECURITY;

CREATE POLICY "Users access own categories"
    ON public.categories FOR ALL
    USING (auth.uid() = user_id);
```
- **Dependencias**: Tarea 10.1, Tarea 10.2 (users table prereq)
- **Criterios de validación**:
  - Archivo contiene SQL válido
  - Unique constraint (user_id, name) presente

### Tarea 10.4 – Crear migration 003_CreateTransactionsTable.sql
- **Path**: `src/SauronSheet.Infrastructure/Persistence/Migrations/003_CreateTransactionsTable.sql`
- **Acción**: Crear archivo
- **Descripción**: Crear migration SQL para tabla `transactions`:
```sql
CREATE TABLE IF NOT EXISTS public.transactions (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL REFERENCES public.users(id) ON DELETE CASCADE,
    amount DECIMAL(15,2) NOT NULL,
    currency VARCHAR(3) NOT NULL DEFAULT 'EUR',
    date TIMESTAMPTZ NOT NULL,
    description TEXT NOT NULL,
    category_id UUID REFERENCES public.categories(id) ON DELETE SET NULL,
    imported_from TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ
);

CREATE INDEX IF NOT EXISTS idx_transactions_user_date ON public.transactions(user_id, date DESC);
CREATE INDEX IF NOT EXISTS idx_transactions_user_category ON public.transactions(user_id, category_id);

ALTER TABLE public.transactions ENABLE ROW LEVEL SECURITY;

CREATE POLICY "Users access own transactions"
    ON public.transactions FOR ALL
    USING (auth.uid() = user_id);
```
- **Dependencias**: Tarea 10.1, Tarea 10.2, Tarea 10.3 (users + categories prereq)
- **Criterios de validación**:
  - Archivo contiene SQL válido
  - Indexes para performance (user_date, user_category)

### Tarea 10.5 – Crear migration 004_CreatePdfImportsTable.sql (CRITICAL FIX I-2 naming)
- **Path**: `src/SauronSheet.Infrastructure/Persistence/Migrations/004_CreatePdfImportsTable.sql`
- **Acción**: Crear archivo
- **Descripción**: Crear migration SQL para tabla `pdf_imports` (NOT import_batches - CRITICAL FIX I-2):
```sql
CREATE TABLE IF NOT EXISTS public.pdf_imports (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL REFERENCES public.users(id) ON DELETE CASCADE,
    filename TEXT NOT NULL,
    imported_count INT NOT NULL,
    skipped_count INT NOT NULL,
    imported_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_pdf_imports_user ON public.pdf_imports(user_id);

ALTER TABLE public.pdf_imports ENABLE ROW LEVEL SECURITY;

CREATE POLICY "Users access own imports"
    ON public.pdf_imports FOR ALL
    USING (auth.uid() = user_id);
```
- **Dependencias**: Tarea 10.1, Tarea 10.2 (users table prereq)
- **Criterios de validación**:
  - Archivo contiene SQL válido
  - Table name es `pdf_imports` (NOT `import_batches`)

### Tarea 10.6 – Verificar RLS policies (NC-1 CRITICAL VERIFICATION)
- **Path**: N/A (Supabase SQL Editor)
- **Acción**: Ejecutar test SQL
- **Descripción**: Después de aplicar migrations, ejecutar test SQL en Supabase para verificar RLS policies funcionan:
```sql
-- Test RLS policy in Supabase SQL editor:
-- Set session user (simulates authenticated request)
SELECT set_config('request.jwt.claims', '{"sub":"test-user-uuid"}', true);

-- Verify user can only see own data
SELECT * FROM public.users WHERE id = 'test-user-uuid';
-- Should return only rows where id matches the session user
```
- **Dependencias**: Tareas 10.2 a 10.5 (migrations aplicadas en Supabase)
- **Criterios de validación**:
  - `auth.uid()` function es accesible (Supabase Auth provides it automatically)
  - RLS policies bloquean acceso cross-user

---

## 11. Infrastructure Layer - DI Updates

### Tarea 11.1 – Actualizar DependencyInjection.cs en Infrastructure (REEMPLAZO completo - CRITICAL FIX C-1)
- **Path**: `src/SauronSheet.Infrastructure/DependencyInjection.cs`
- **Acción**: Modificar archivo (reemplazar contenido completo)
- **Descripción**: Actualizar DI para registrar todos los servicios de Phase 3. **CRITICAL FIX C-1**: Registrar Supabase Client correctamente:
```csharp
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Auth;
using Domain.Services;
using Domain.Repositories;
using Application.Common;
using Application.Interfaces;
using Persistence;
using PDF;
using PDF.Parsers;

namespace SauronSheet.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var supabaseUrl = configuration["Supabase:Url"]
            ?? throw new InvalidOperationException("Configuration key 'Supabase:Url' is not set.");

        var supabaseKey = configuration["Supabase:Key"]
            ?? throw new InvalidOperationException("Configuration key 'Supabase:Key' is not set.");

        var jwtSecret = configuration["Supabase:JwtSecret"]
            ?? throw new InvalidOperationException("Configuration key 'Supabase:JwtSecret' is not set.");

        // Auth configuration (from Phase 1)
        services.Configure<AuthConfiguration>(options =>
        {
            options.JwtSecret = jwtSecret;
        });

        // Auth services (from Phase 1)
        services.AddHttpClient<IAuthService, SupabaseAuthService>()
            .ConfigureHttpClient(client => client.BaseAddress = new Uri(supabaseUrl));

        services.AddScoped<IUserContext, HttpUserContext>();
        services.AddHttpContextAccessor();

        // Supabase client registration (Phase 3 - CRITICAL FIX C-1)
        services.AddSingleton<Supabase.Client>(sp =>
        {
            var options = new Supabase.SupabaseOptions
            {
                AutoRefreshToken = true,
                AutoConnectRealtime = false
            };
            return new Supabase.Client(supabaseUrl, supabaseKey, options);
        });

        // Repository implementations (NEW in Phase 3)
        services.AddScoped<ITransactionRepository, SupabaseTransactionRepository>();
        services.AddScoped<ICategoryRepository, SupabaseCategoryRepository>();
        services.AddScoped<IPdfImportRepository, SupabasePdfImportRepository>();

        // PDF parsing (NEW in Phase 3)
        services.AddScoped<IPdfParser, GenericBankPdfParser>();
        services.AddSingleton<PdfParserFactory>();

        // Domain services (NEW in Phase 3)
        services.AddScoped<CategoryService>();

        return services;
    }
}
```
- **Dependencias**: Tareas 9.2, 9.3, 9.4, Tarea 8.2, Tarea 8.3, Phase 2 (CategoryService)
- **Criterios de validación**:
  - Archivo compila
  - Todos los servicios registrados correctamente
  - Supabase.Client registrado como Singleton con options

---

## 12. Frontend Layer - Program.cs & Configuration

### Tarea 12.1 – Verificar que Frontend Program.cs tiene DI registrations de Phase 1
- **Path**: `src/SauronSheet.Frontend/Program.cs`
- **Acción**: Verificar contenido
- **Descripción**: Verificar que Program.cs tiene las registraciones de Phase 1:
  - `builder.Services.AddApplicationServices();`
  - `builder.Services.AddInfrastructureServices(builder.Configuration);`
  - Auth middleware (UseMiddleware<JwtCookieMiddleware>, UseAuthentication, UseAuthorization)
- **Dependencias**: Phase 1 (Program.cs updated)
- **Criterios de validación**:
  - Program.cs contiene todas las registraciones necesarias
  - No hay cambios necesarios para Phase 3 (DI ya configurado)

### Tarea 12.2 – Verificar appsettings.json tiene config de Supabase
- **Path**: `src/SauronSheet.Frontend/appsettings.json`
- **Acción**: Verificar contenido
- **Descripción**: Verificar que appsettings.json tiene sección Supabase:
```json
{
  "Supabase": {
    "Url": "https://your-project.supabase.co",
    "Key": "your-anon-key",
    "JwtSecret": "your-jwt-secret"
  },
  ...
}
```
- **Dependencias**: Phase 1 (appsettings.json updated)
- **Criterios de validación**:
  - appsettings.json contiene sección Supabase
  - Todas las claves presentes (Url, Key, JwtSecret)

---

## 13. Frontend Layer - Pages Structure

### Tarea 13.1 – Crear estructura de carpetas Frontend para Phase 3
- **Path**: `src/SauronSheet.Frontend/Pages/Transactions/`, `src/SauronSheet.Frontend/Pages/Categories/`
- **Acción**: Crear directorios
- **Descripción**: Crear estructura de carpetas para páginas de Phase 3:
  - `Pages/Transactions/` - Transaction pages (Upload, Index, Add)
  - `Pages/Categories/` - Category pages (Index)
- **Dependencias**: Phase 0 (Frontend project existe)
- **Criterios de validación**: Directorios existen y están vacíos

---

## 14. Frontend Layer - Transaction Pages

### Tarea 14.1 – Crear página Upload PDF
- **Path**: `src/SauronSheet.Frontend/Pages/Transactions/Upload.cshtml`
- **Acción**: Crear archivo
- **Descripción**: Crear Razor page para upload de PDF con:
  - File input (accept=".pdf")
  - Upload button
  - Progress indicator (opcional)
  - Results display area (imported count, skipped count, error list)
  - Tailwind styling
  - Formulario POST a PageModel
- **Dependencias**: Tarea 13.1
- **Criterios de validación**:
  - Archivo compila como Razor template
  - Form con file input y submit button

### Tarea 14.2 – Crear PageModel Upload.cshtml.cs (con NC-2 error handling)
- **Path**: `src/SauronSheet.Frontend/Pages/Transactions/Upload.cshtml.cs`
- **Acción**: Crear archivo
- **Descripción**: Crear PageModel `UploadModel` que:
  - Inyecta `IMediator`
  - Property `[BindProperty] IFormFile PdfFile`
  - Property `ImportResultDto? ImportResult`
  - Property `string? ErrorMessage`
  - **Atributo `[Authorize]` en clase** (CRITICAL FIX I-5)
  - Método `OnPostAsync()`:
    - Validar PdfFile != null, size <= 10MB, extension == ".pdf"
    - Enviar `ImportTransactionsFromPdfCommand(stream, filename)` via mediator
    - **CRITICAL FIX NC-2: Comprehensive error handling**:
      - Catch `HttpRequestException` (network errors) → user-friendly message
      - Catch `InvalidOperationException` when message contains "PDF" (PDF parsing errors from NC-3) → "Could not parse PDF: {message}"
      - Catch `DomainException` (domain validation) → show message
      - Catch general `Exception` → "An unexpected error occurred"
    - Guardar result en `ImportResult`
    - Retornar Page()
  - Namespace: `SauronSheet.Frontend.Pages.Transactions`
- **Dependencias**: Tarea 14.1, Application (ImportTransactionsFromPdfCommand)
- **Criterios de validación**:
  - Archivo compila
  - PageModel tiene [Authorize] attribute
  - Error handling completo (NC-2)

### Tarea 14.3 – Crear página Transaction List
- **Path**: `src/SauronSheet.Frontend/Pages/Transactions/Index.cshtml`
- **Acción**: Crear archivo
- **Descripción**: Crear Razor page para listar transactions con:
  - Tabla con columnas: Date, Description, Amount, Category, Actions (categorize, delete)
  - Paginación (previous/next buttons, page numbers)
  - Category filter dropdown (Alpine.js - CLARIFICATION A-2: versión @3)
  - Date range filter (start date, end date inputs)
  - Tailwind styling
  - Alpine.js para interactividad (dropdowns, filters)
- **Dependencias**: Tarea 13.1
- **Criterios de validación**:
  - Archivo compila como Razor template
  - Tabla con transaction data binding

### Tarea 14.4 – Crear PageModel Index.cshtml.cs
- **Path**: `src/SauronSheet.Frontend/Pages/Transactions/Index.cshtml.cs`
- **Acción**: Crear archivo
- **Descripción**: Crear PageModel `IndexModel` que:
  - Inyecta `IMediator`
  - Property `PaginatedResultDto<TransactionDto> Transactions`
  - **Atributo `[Authorize]` en clase** (CRITICAL FIX I-5)
  - Query parameters: `int? PageNumber`, `Guid? CategoryId`, `DateTime? StartDate`, `DateTime? EndDate`
  - Método `OnGetAsync(...)`:
    - Enviar `GetTransactionsQuery(pageNumber, categoryId, startDate, endDate)` via mediator
    - Guardar result en `Transactions`
    - Retornar Page()
  - Namespace: `SauronSheet.Frontend.Pages.Transactions`
- **Dependencias**: Tarea 14.3, Application (GetTransactionsQuery)
- **Criterios de validación**:
  - Archivo compila
  - PageModel tiene [Authorize] attribute

### Tarea 14.5 – Crear página Add Transaction
- **Path**: `src/SauronSheet.Frontend/Pages/Transactions/Add.cshtml`
- **Acción**: Crear archivo
- **Descripción**: Crear Razor page para agregar transaction manual con:
  - Form inputs: Date (date picker), Description (text), Amount (decimal), Currency (dropdown, default EUR), Category (dropdown opcional)
  - Submit button
  - Error message area
  - Tailwind styling
- **Dependencias**: Tarea 13.1
- **Criterios de validación**:
  - Archivo compila como Razor template
  - Form con todos los inputs

### Tarea 14.6 – Crear PageModel Add.cshtml.cs (con NC-2 error handling)
- **Path**: `src/SauronSheet.Frontend/Pages/Transactions/Add.cshtml.cs`
- **Acción**: Crear archivo
- **Descripción**: Crear PageModel `AddModel` que:
  - Inyecta `IMediator`
  - **Atributo `[Authorize]` en clase** (CRITICAL FIX I-5)
  - Nested class `AddTransactionInputModel` con propiedades: Date, Description, Amount, Currency, CategoryId (CLARIFICATION A-6: nested class confirmado)
  - Property `[BindProperty] AddTransactionInputModel Input`
  - Property `List<CategoryDto> Categories` (para dropdown)
  - Property `string? ErrorMessage`
  - Método `OnGetAsync()`:
    - Enviar `GetCategoriesQuery()` via mediator para popular dropdown
    - Guardar result en `Categories`
    - Retornar Page()
  - Método `OnPostAsync()`:
    - Validar ModelState
    - Enviar `CreateTransactionCommand(input.Amount, input.Currency, input.Date, input.Description, input.CategoryId)` via mediator
    - **CRITICAL FIX NC-2: Error handling**:
      - Catch `HttpRequestException` → network error message
      - Catch `EntityNotFoundException` → "Selected category is invalid"
      - Catch `DomainException` → show message
      - Catch general `Exception` → unexpected error
    - Si success: RedirectToPage("/Transactions/Index")
    - Si error: mostrar ErrorMessage, retornar Page()
  - Namespace: `SauronSheet.Frontend.Pages.Transactions`
- **Dependencias**: Tarea 14.5, Application (CreateTransactionCommand, GetCategoriesQuery)
- **Criterios de validación**:
  - Archivo compila
  - PageModel tiene [Authorize] attribute
  - Nested class AddTransactionInputModel presente
  - Error handling completo (NC-2)

---

## 15. Frontend Layer - Category Pages

### Tarea 15.1 – Crear página Category Index
- **Path**: `src/SauronSheet.Frontend/Pages/Categories/Index.cshtml`
- **Acción**: Crear archivo
- **Descripción**: Crear Razor page para gestionar categories con:
  - Lista de categories (table: Name, Color, Icon, System Default flag, Transaction Count, Actions)
  - Create form (inline o modal): Name, Color (color picker), Icon (text input)
  - Edit button (inline edit con Alpine.js) - solo user-defined
  - Delete button (confirmation dialog) - solo user-defined sin transactions
  - System defaults visualmente diferenciados (badge, disabled edit/delete)
  - Tailwind styling
  - Alpine.js para interactividad (confirmation dialogs, inline edit)
- **Dependencias**: Tarea 13.1
- **Criterios de validación**:
  - Archivo compila como Razor template
  - Tabla con category data binding

### Tarea 15.2 – Crear PageModel CategoryIndex.cshtml.cs
- **Path**: `src/SauronSheet.Frontend/Pages/Categories/Index.cshtml.cs`
- **Acción**: Crear archivo
- **Descripción**: Crear PageModel `CategoryIndexModel` que:
  - Inyecta `IMediator`
  - **Atributo `[Authorize]` en clase** (CRITICAL FIX I-5)
  - Property `List<CategoryDto> Categories`
  - Nested class `CreateCategoryInputModel` con propiedades: Name, Color, Icon
  - Property `[BindProperty] CreateCategoryInputModel Input`
  - Property `string? ErrorMessage`
  - Método `OnGetAsync()`:
    - Enviar `GetCategoriesQuery()` via mediator
    - Guardar result en `Categories`
    - Retornar Page()
  - Método `OnPostCreateAsync()`:
    - Enviar `CreateCategoryCommand(input.Name, input.Color, input.Icon)` via mediator
    - Si success: RedirectToPage("/Categories/Index")
    - Si error (DomainException "already exists"): mostrar ErrorMessage, retornar Page()
  - Método `OnPostRenameAsync(Guid categoryId, string newName)`:
    - Enviar `RenameCategoryCommand(categoryId, newName)` via mediator
    - Manejar errores (system default, etc.)
  - Método `OnPostDeleteAsync(Guid categoryId)`:
    - Enviar `DeleteCategoryCommand(categoryId)` via mediator
    - Manejar errores (has transactions, system default)
  - Namespace: `SauronSheet.Frontend.Pages.Categories`
- **Dependencias**: Tarea 15.1, Application (Category commands/queries)
- **Criterios de validación**:
  - Archivo compila
  - PageModel tiene [Authorize] attribute
  - 3 métodos POST (create, rename, delete)

---

## 16. Frontend Layer - Layout Updates

### Tarea 16.1 – Actualizar _Layout.cshtml con Alpine.js CDN (CLARIFICATION A-2)
- **Path**: `src/SauronSheet.Frontend/Pages/Shared/_Layout.cshtml`
- **Acción**: Modificar archivo (agregar script)
- **Descripción**: Agregar Alpine.js CDN al layout existente (de Phase 1):
  - En `<head>` section, DESPUÉS de Tailwind CDN, agregar:
```html
<!-- Alpine.js for interactivity (Phase 3) -->
  <script defer src="https://cdn.jsdelivr.net/npm/alpinejs@3/dist/cdn.min.js"></script>
```
  - CLARIFICATION A-2: versión `@3` (latest v3), NOT `@3.x.x`
- **Dependencias**: Phase 1 (_Layout.cshtml existe con Tailwind CDN)
- **Criterios de validación**:
  - Archivo compila
  - Alpine.js CDN script agregado DESPUÉS de Tailwind

### Tarea 16.2 – Actualizar _Layout.cshtml navegación con enlaces Phase 3
- **Path**: `src/SauronSheet.Frontend/Pages/Shared/_Layout.cshtml`
- **Acción**: Modificar archivo (agregar nav links)
- **Descripción**: Actualizar la sección de navegación del layout (de Phase 1) para agregar enlaces a páginas de Phase 3:
  - En el bloque de navegación autenticada (cuando `User.Identity.IsAuthenticated`), agregar:
    - Link a `/Transactions/Upload` ("Upload PDF")
    - Link a `/Transactions` ("Transactions")
    - Link a `/Transactions/Add` ("Add Transaction")
    - Link a `/Categories` ("Categories")
  - Mantener links existentes: Dashboard, logout
  - Usar Tailwind classes para styling consistente
- **Dependencias**: Tarea 16.1, Phase 1 (_Layout.cshtml con auth-aware navigation)
- **Criterios de validación**:
  - Archivo compila
  - Todos los enlaces presentes en navegación

---

## 17. Integration & Validation

### Tarea 17.1 – Full solution build
- **Path**: N/A (command line)
- **Acción**: Ejecutar comando
- **Descripción**: Ejecutar `dotnet build` desde raíz de solución para compilar todos los proyectos con cambios de Phase 3
- **Dependencias**: Todas las tareas anteriores
- **Criterios de validación**:
  - Exit code 0
  - Cero errores
  - Cero warnings (TreatWarningsAsErrors=true)

### Tarea 17.2 – Ejecutar todos los tests
- **Path**: N/A (command line)
- **Acción**: Ejecutar comando
- **Descripción**: Ejecutar `dotnet test` para verificar que todos los tests pasan (Phase 0+1+2: 121 tests + Phase 3: 49 tests)
- **Dependencias**: Tarea 17.1
- **Criterios de validación**:
  - Exit code 0
  - **Output contiene "170 passed"** (CRITICAL FIX I-1: corregido count)
  - Breakdown:
    * Phase 0 Domain: 11 tests
    * Phase 1 Domain: 8 tests
    * Phase 2 Domain: 96 tests + extras (1 test) = 97
    * Phase 3 Domain: 5 tests
    * **Total Domain: 121 tests**
    * Phase 0 Application: 2 tests
    * Phase 1 Application: 14 tests
    * Phase 3 Application: 33 tests
    * **Total Application: 49 tests**
    * **Grand Total: 170 tests**

### Tarea 17.3 – Verificar Domain coverage >= 80%
- **Path**: N/A (coverage report)
- **Acción**: Generar reporte
- **Descripción**: Usar coverlet para generar reporte de cobertura del Domain layer:
```sh
dotnet tool install -g coverlet.console
coverlet tests/SauronSheet.Domain.Tests/bin/Debug/net10.0/SauronSheet.Domain.Tests.dll \
  --target "dotnet" \
  --targetargs "test tests/SauronSheet.Domain.Tests/ --no-build --configuration Debug" \
  --format "opencover" \
  --output "./coverage-domain.xml" \
  --include "[SauronSheet.Domain]*" \
  --exclude "[SauronSheet.Domain.Tests]*"
```
- **Dependencias**: Tarea 17.1
- **Criterios de validación**:
  - coverage-domain.xml generado
  - Domain layer coverage >= 80% (constitution minimum)

### Tarea 17.4 – Verificar Application coverage >= 70%
- **Path**: N/A (coverage report)
- **Acción**: Generar reporte
- **Descripción**: Usar coverlet para generar reporte de cobertura del Application layer:
```sh
coverlet tests/SauronSheet.Application.Tests/bin/Debug/net10.0/SauronSheet.Application.Tests.dll \
  --target "dotnet" \
  --targetargs "test tests/SauronSheet.Application.Tests/ --no-build --configuration Debug" \
  --format "opencover" \
  --output "./coverage-app.xml" \
  --include "[SauronSheet.Application]*" \
  --exclude "[SauronSheet.Application.Tests]*"
```
- **Dependencias**: Tarea 17.1
- **Criterios de validación**:
  - coverage-app.xml generado
  - Application layer coverage >= 70% (constitution minimum)

### Tarea 17.5 – Verificar dependencias (Domain = 0 refs)
- **Path**: `src/SauronSheet.Domain/SauronSheet.Domain.csproj`
- **Acción**: Auditar
- **Descripción**: Verificar que Domain.csproj tiene CERO `<ProjectReference>` y CERO `<PackageReference>`. Comando:
```sh
grep -E "ProjectReference|PackageReference" src/SauronSheet.Domain/SauronSheet.Domain.csproj
```
  Debe retornar vacío.
- **Dependencias**: Tarea 17.1
- **Criterios de validación**:
  - No hay líneas con ProjectReference
  - No hay líneas con PackageReference

### Tarea 17.6 – Verificar dependencias (Application -> Domain only)
- **Path**: `src/SauronSheet.Application/SauronSheet.Application.csproj`
- **Acción**: Auditar
- **Descripción**: Verificar que Application.csproj referencia SOLO Domain (y MediatR packages). Comando:
```sh
grep "ProjectReference" src/SauronSheet.Application/SauronSheet.Application.csproj | wc -l
```
  Debe retornar 1 (solo Domain)
- **Dependencias**: Tarea 17.1
- **Criterios de validación**:
  - ProjectReference count == 1
  - La referencia es a Domain

### Tarea 17.7 – Verificar dependencias (Infrastructure -> Domain only)
- **Path**: `src/SauronSheet.Infrastructure/SauronSheet.Infrastructure.csproj`
- **Acción**: Auditar
- **Descripción**: Verificar que Infrastructure.csproj referencia SOLO Domain (no Application ni Frontend)
- **Dependencias**: Tarea 17.1
- **Criterios de validación**:
  - ProjectReference count == 1
  - La referencia es a Domain

### Tarea 17.8 – Verificar dependencias (Frontend -> App + Infra)
- **Path**: `src/SauronSheet.Frontend/SauronSheet.Frontend.csproj`
- **Acción**: Auditar
- **Descripción**: Verificar que Frontend.csproj referencia Application e Infrastructure (exactamente 2 ProjectReferences)
- **Dependencias**: Tarea 17.1
- **Criterios de validación**:
  - ProjectReference count == 2
  - Referencias son a Application e Infrastructure

### Tarea 17.9 – Aplicar migrations en Supabase
- **Path**: N/A (Supabase dashboard)
- **Acción**: Ejecutar SQL
- **Descripción**: En el dashboard de Supabase, navegar a SQL Editor, ejecutar las 4 migrations en orden:
  1. 001_CreateUsersTable.sql
  2. 002_CreateCategoriesTable.sql
  3. 003_CreateTransactionsTable.sql
  4. 004_CreatePdfImportsTable.sql
  - Esto crea las tablas con políticas de RLS
- **Dependencias**: Tareas 10.2 a 10.5 (migrations creadas)
- **Criterios de validación**:
  - 4 tablas visibles en Supabase dashboard (Tables view)
  - RLS habilitado en todas
  - Policies creadas correctamente

### Tarea 17.10 – Manual E2E: Upload PDF y ver transactions
- **Path**: N/A (manual testing en navegador)
- **Acción**: Probar
- **Descripción**:
  1. Ejecutar `dotnet run --project src/SauronSheet.Frontend/`
  2. Login con usuario existente (de Phase 1)
  3. Navegar a /Transactions/Upload
  4. Subir PDF de prueba (sample bank statement)
  5. Verificar ImportResultDto muestra imported count, skipped count, errors
  6. Navegar a /Transactions
  7. Verificar transactions importadas aparecen en lista
- **Dependencias**: Tarea 17.9 (migrations aplicadas)
- **Criterios de validación**:
  - Upload exitoso sin errores
  - Transactions visibles en lista
  - Paginación funciona

### Tarea 17.11 – Manual E2E: Crear category y categorizar transaction
- **Path**: N/A (manual testing en navegador)
- **Acción**: Probar
- **Descripción**:
  1. Navegar a /Categories
  2. Crear nueva category (Name: "Test", Color: "#ff0000")
  3. Verificar category aparece en lista
  4. Navegar a /Transactions
  5. Seleccionar category "Test" en dropdown de una transaction
  6. Verificar category se actualiza
- **Dependencias**: Tarea 17.9
- **Criterios de validación**:
  - Category creada exitosamente
  - Categorización de transaction funciona
  - System defaults están presentes y protegidos

### Tarea 17.12 – Manual E2E: Tenant isolation
- **Path**: N/A (manual testing en navegador)
- **Acción**: Probar
- **Descripción**:
  1. En navegador normal, login como User A
  2. Crear transaction y category
  3. En navegador incógnito, login como User B
  4. Verificar que User B NO ve transactions ni categories de User A
  5. Crear data como User B
  6. Verificar User A NO ve data de User B
- **Dependencias**: Tarea 17.9
- **Criterios de validación**:
  - Aislamiento de tenant verificado
  - Cada usuario ve solo sus propios datos

---

## Orden de implementación

Ejecutar las tareas en el siguiente orden secuencial:

1. Tarea 1.0 – Verificar que Phase 0, 1, y 2 están completos
2. Tarea 1.1 – Verificar que Phase 2 Domain está completo
3. Tarea 2.1 – Crear estructura de carpetas Domain para Phase 3
4. Tarea 2.2 – Crear estructura de carpetas Domain.Tests para Phase 3
5. Tarea 2.3 – Crear test stubs para ImportBatch Entity (RED phase)
6. Tarea 2.4 – Implementar ImportBatch Entity (GREEN phase)
7. Tarea 2.5 – Crear IPdfImportRepository interface en Domain (CRITICAL FIX C-2)
8. Tarea 2.6 – Agregar GetCountsByCategoriesAsync a ITransactionRepository (CRITICAL FIX I-4)
9. Tarea 2.7 – Ejecutar tests Domain en GREEN phase
10. Tarea 3.1 – Crear estructura de carpetas Application.Tests para Phase 3
11. Tarea 3.2 – Crear test stubs para Transaction Commands (RED phase)
12. Tarea 3.3 – Crear test stubs para Transaction Queries (RED phase)
13. Tarea 3.4 – Crear test stubs para Category Commands (RED phase)
14. Tarea 3.5 – Crear test stubs para Category Queries (RED phase)
15. Tarea 3.6 – Ejecutar tests Application en RED phase
16. Tarea 4.1 – Crear estructura de carpetas Application para Phase 3
17. Tarea 4.2 – Crear DTOs de Transaction y Category
18. Tarea 4.3 – Crear Transaction Commands
19. Tarea 4.4 – Crear Transaction Queries
20. Tarea 4.5 – Crear Category Commands
21. Tarea 4.6 – Crear Category Queries
22. Tarea 5.1 – Crear IPdfParser interface en Application layer
23. Tarea 5.2 – Crear RawTransactionRow model
24. Tarea 6.1 – Implementar ImportTransactionsFromPdfCommandHandler (GREEN)
25. Tarea 6.2 – Implementar CreateTransactionCommandHandler (GREEN)
26. Tarea 6.3 – Implementar UpdateTransactionCategoryCommandHandler (GREEN)
27. Tarea 6.4 – Implementar DeleteTransactionCommandHandler (GREEN)
28. Tarea 6.5 – Implementar GetTransactionsQueryHandler (GREEN)
29. Tarea 6.6 – Implementar CreateCategoryCommandHandler (GREEN)
30. Tarea 6.7 – Implementar RenameCategoryCommandHandler (GREEN)
31. Tarea 6.8 – Implementar DeleteCategoryCommandHandler (GREEN)
32. Tarea 6.9 – Implementar SeedSystemDefaultsCommandHandler (GREEN)
33. Tarea 6.10 – Implementar GetCategoriesQueryHandler (GREEN)
34. Tarea 6.11 – Ejecutar tests Application para Handlers (GREEN)
35. Tarea 7.1 – Agregar NuGet package PdfPig (CLARIFICATION A-3)
36. Tarea 8.1 – Crear directorio PDF en Infrastructure
37. Tarea 8.2 – Implementar PdfParserFactory
38. Tarea 8.3 – Implementar GenericBankPdfParser (GREEN con NC-3 mitigations)
39. Tarea 9.1 – Crear directorio Persistence en Infrastructure
40. Tarea 9.2 – Implementar SupabaseTransactionRepository (GREEN con stubs)
41. Tarea 9.3 – Implementar SupabaseCategoryRepository (GREEN con stubs)
42. Tarea 9.4 – Implementar SupabasePdfImportRepository (GREEN con stubs)
43. Tarea 10.1 – Crear directorio Migrations en Infrastructure
44. Tarea 10.2 – Crear migration 001_CreateUsersTable.sql (CRITICAL FIX I-3)
45. Tarea 10.3 – Crear migration 002_CreateCategoriesTable.sql
46. Tarea 10.4 – Crear migration 003_CreateTransactionsTable.sql
47. Tarea 10.5 – Crear migration 004_CreatePdfImportsTable.sql (CRITICAL FIX I-2 naming)
48. Tarea 10.6 – Verificar RLS policies (NC-1 CRITICAL VERIFICATION)
49. Tarea 11.1 – Actualizar DependencyInjection.cs en Infrastructure (REEMPLAZO completo - CRITICAL FIX C-1)
50. Tarea 12.1 – Verificar que Frontend Program.cs tiene DI registrations de Phase 1
51. Tarea 12.2 – Verificar appsettings.json tiene config de Supabase
52. Tarea 13.1 – Crear estructura de carpetas Frontend para Phase 3
53. Tarea 14.1 – Crear página Upload PDF
54. Tarea 14.2 – Crear PageModel Upload.cshtml.cs (con NC-2 error handling)
55. Tarea 14.3 – Crear página Transaction List
56. Tarea 14.4 – Crear PageModel Index.cshtml.cs
57. Tarea 14.5 – Crear página Add Transaction
58. Tarea 14.6 – Crear PageModel Add.cshtml.cs (con NC-2 error handling)
59. Tarea 15.1 – Crear página Category Index
60. Tarea 15.2 – Crear PageModel CategoryIndex.cshtml.cs
61. Tarea 16.1 – Actualizar _Layout.cshtml con Alpine.js CDN (CLARIFICATION A-2)
62. Tarea 16.2 – Actualizar _Layout.cshtml navegación con enlaces Phase 3
63. Tarea 17.1 – Full solution build
64. Tarea 17.2 – Ejecutar todos los tests
65. Tarea 17.3 – Verificar Domain coverage >= 80%
66. Tarea 17.4 – Verificar Application coverage >= 70%
67. Tarea 17.5 – Verificar dependencias (Domain = 0 refs)
68. Tarea 17.6 – Verificar dependencias (Application -> Domain only)
69. Tarea 17.7 – Verificar dependencias (Infrastructure -> Domain only)
70. Tarea 17.8 – Verificar dependencias (Frontend -> App + Infra)
71. Tarea 17.9 – Aplicar migrations en Supabase
72. Tarea 17.10 – Manual E2E: Upload PDF y ver transactions
73. Tarea 17.11 – Manual E2E: Crear category y categorizar transaction
74. Tarea 17.12 – Manual E2E: Tenant isolation

---

**Total Tasks: 73**  
**Estimated Duration: 13 days (Weeks 9–13)**  
**Status**: Ready for implementation ✅
