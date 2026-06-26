# Tasks: Transaction Edit

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | 950 – 1100 (código + tests) |
| 400-line budget risk | High |
| Chained PRs recommended | Yes |
| Suggested split | 4 PRs (stacked-to-main) |
| Delivery strategy | ask-on-risk |
| Chain strategy | stacked-to-main |

Decision needed before apply: Yes
Chained PRs recommended: Yes
Chain strategy: stacked-to-main
400-line budget risk: High

### Suggested Work Units

| Unit | Goal | Likely PR | Notes |
|------|------|-----------|-------|
| 1 | Foundation read-side: dominio + query | PR 1 | Base branch: `main`. Incluye tests. Acopla `Transaction.Update()` y el `GetTransactionByIdQuery`. Necesario antes de cualquier flujo de UI. |
| 2 | Write-side: command + validaciones | PR 2 | Base branch: `main` (stacked). Necesita PR 1. Handler con 5 ramas de validación. Tests completos. |
| 3 | UI: página edit + botón index | PR 3 | Base branch: `main` (stacked). Necesita PR 1 y PR 2. Sigue el patrón de `Budgets/Edit`. |
| 4 | E2E: flujo completo de edición | PR 4 | Base branch: `main` (stacked). Necesita PR 3. Cubre happy path, duplicado, ID inexistente. |

> El orquestador debe preguntar al usuario la chain strategy (`stacked-to-main` vs `feature-branch-chain` vs `size:exception`) antes de lanzar `sdd-apply`. Recomendación por defecto: `stacked-to-main` por la naturaleza secuencial y limpia del trabajo (cada PR deja `main` verde y desacoplado).

## Implementation Rules

- TDD obligatorio (config `strict_tdd: true`): cada sub-tarea de implementación va precedida de su test RED. No commit de código de producción sin test que lo cubra.
- Idioma: títulos, descripciones y comentarios en español neutro. Identificadores, strings y trazas en inglés.
- Clean Architecture: las dependencias siguen Frontend → Application → Domain. La query y el command despachan por `IMediator`.
- Observabilidad: cualquier `catch (Exception ex)` en handlers y PageModels captura a Sentry con `scope.SetTag` y mensaje genérico al usuario (sin filtrar `ex.Message`).
- 530 palabras: este artefacto cumple el límite del skill.

## Phase 1 — Domain (PR 1, base)

- [x] 1.1 **RED** — `tests/SauronSheet.Domain.Tests/Entities/TransactionUpdateTests.cs` (nuevo, ~80 líneas): tests unitarios de `Transaction.Update()`. Casos: descripción vacía/whitespace lanza `ArgumentException`; `amount` null lanza `ArgumentNullException`; `UpdatedAt` cambia a `DateTime.UtcNow` (±2s); `ImportedFrom`, `BankCategory`, `BankSubcategory`, `Balance` no se modifican tras `Update`; campos editables (`Amount`, `Date`, `Description`, `CategoryId`, `SubcategoryId`, `CategorySource`) se asignan correctamente. Acceptance: `dotnet test tests/SauronSheet.Domain.Tests` falla con `Method not found` o `Update does not exist`. Dependencias: ninguna. Complejidad: S.
- [x] 1.2 **GREEN** — `src/SauronSheet.Domain/Entities/Transaction.cs` (modificar, ~25 líneas): añadir `public void Update(Money amount, DateTime date, string description, CategoryId? categoryId, SubcategoryId? subcategoryId, CategorySource categorySource)` con guard clauses para `amount` y `description`, asignación de los 6 parámetros y `UpdatedAt = DateTime.UtcNow`. No tocar constructores ni `Categorize`. Acceptance: `dotnet test tests/SauronSheet.Domain.Tests` pasa los tests del punto 1.1 verde. Dependencias: 1.1. Complejidad: S.

## Phase 2 — Application: GetTransactionByIdQuery (PR 1, base)

- [x] 2.1 **RED** — `tests/SauronSheet.Application.Tests/Features/Transactions/Queries/GetTransactionByIdQueryHandlerTests.cs` (nuevo, ~80 líneas): 3 escenarios con Moq sobre `ITransactionRepository`, `ICategoryRepository`, `ISubcategoryRepository`, `IUserContext`. (a) ID existente del propio usuario → DTO con `CategoryName` y `SubcategoryName` poblados vía batch lookup. (b) ID inexistente → `EntityNotFoundException`. (c) ID de otro usuario → `EntityNotFoundException` (tenant). Acceptance: tests rojos (handler no existe). Dependencias: 1.2. Complejidad: S.
- [x] 2.2 **GREEN** — `src/SauronSheet.Application/Features/Transactions/Queries/GetTransactionByIdQuery.cs` (nuevo, ~3 líneas): record `GetTransactionByIdQuery(Guid TransactionId) : IRequest<TransactionDto>`. Acceptance: archivo compila aislado. Dependencias: 1.2. Complejidad: S.
- [x] 2.3 **GREEN** — `src/SauronSheet.Application/Features/Transactions/Queries/GetTransactionByIdQueryHandler.cs` (nuevo, ~70 líneas): handler con `ITransactionRepository.GetByIdAsync` + chequeo `transaction.UserId.Value != userId.Value` → `EntityNotFoundException`. Carga categorías y subcategorías del usuario en batch (evita N+1) y mapea a `TransactionDto` con `CategoryName`/`SubcategoryName` por `TryGetValue`. Reutilizar `TransactionDto` existente si ya expone esos campos. Acceptance: tests del 2.1 en verde; sin warnings de nullability. Dependencias: 2.1, 2.2. Complejidad: M.

## Phase 3 — Application: UpdateTransactionCommand (PR 2, stacked)

- [x] 3.1 **RED** — `tests/SauronSheet.Application.Tests/Features/Transactions/Commands/UpdateTransactionCommandHandlerTests.cs` (nuevo, ~200 líneas): 5 escenarios con Moq. (a) **Happy path** — comando válido → `transaction.Update(...)` invocado con los parámetros correctos y `_transactionRepo.UpdateAsync` llamado 1 vez. (b) **No encontrada** — `GetByIdAsync` retorna null → `EntityNotFoundException`. (c) **Tenant incorrecto** — `transaction.UserId` ≠ `userContext.UserId` → `EntityNotFoundException` y no se llama a `UpdateAsync`. (d) **Duplicado** — `ExistsDuplicateAsync` retorna true y el duplicado no es la propia transacción (mismo date+amount+description+balance pero ID distinto) → `DuplicateEntityException`. (e) **Subcategory inválida** — `SubcategoryId` no pertenece a la `CategoryId` seleccionada → `DomainException`. Acceptance: 5 tests rojos (handler no existe). Dependencias: 1.2, 2.3. Complejidad: M.
- [x] 3.2 **GREEN** — `src/SauronSheet.Application/Features/Transactions/Commands/UpdateTransactionCommand.cs` (nuevo, ~10 líneas): record `UpdateTransactionCommand(Guid TransactionId, DateTime Date, string Description, decimal Amount, string Currency, Guid? CategoryId, Guid? SubcategoryId) : IRequest<Unit>`. Acceptance: compila. Dependencias: ninguna (estructura). Complejidad: S.
- [x] 3.3 **GREEN** — `src/SauronSheet.Application/Features/Transactions/Commands/UpdateTransactionCommandHandler.cs` (nuevo, ~130 líneas): handler con orden estricto: (1) `GetByIdAsync` + tenant check → `EntityNotFoundException`; (2) si `CategoryId.HasValue` validar ownership de categoría; (3) si `SubcategoryId.HasValue` validar que la subcategoría pertenece a la categoría → `DomainException` con mensaje fijo y traducido; (4) `ExistsDuplicateAsync` con los 5 argumentos; excluir self comparando IDs de los duplicados en memoria; (5) `transaction.Update(amount, date, description, categoryId, subcategoryId, computedSource)` donde `computedSource` = `UserOverride` si la categoría cambió, si no la existente; (6) `UpdateAsync`. Capturar `HttpRequestException` y `Exception` a Sentry con `scope.SetTag("handler", "UpdateTransactionCommandHandler")` y relanzar `DomainException` con mensaje genérico. Acceptance: 5 tests del 3.1 en verde. Dependencias: 3.1, 3.2. Complejidad: L.

## Phase 4 — Frontend: Edit page (PR 3, stacked)

- [x] 4.1 — `src/SauronSheet.Frontend/Pages/Transactions/Edit.cshtml.cs` (nuevo, ~150 líneas): `EditModel : PageModel` con `[Authorize]`, `[ValidateAntiForgeryToken]`, `IMediator` inyectado, `TransactionDto? Transaction` para resumen, `[BindProperty]` para `Date`, `Description`, `Amount`, `Currency`, `CategoryId`, `SubcategoryId`. `OnGetAsync(Guid id)` despacha `GetTransactionByIdQuery`; si null o `EntityNotFoundException` → `RedirectToPage("/Transactions/Index")`. `OnPostAsync` despacha `UpdateTransactionCommand`; éxito → `TempData["SuccessMessage"]` + redirect; `DuplicateEntityException` y `DomainException` → `ErrorMessage` y recarga; `HttpRequestException` y `Exception` → Sentry + mensaje genérico (sin `ex.Message`). Acceptance: `dotnet build` sin warnings; `dotnet test tests/SauronSheet.Frontend.Tests` pasa. Dependencias: 3.3, 2.3. Complejidad: M.
- [x] 4.2 — `src/SauronSheet.Frontend/Pages/Transactions/Edit.cshtml` (nuevo, ~200 líneas): replica layout de `Budgets/Edit.cshtml`. Bloque superior read-only: importe, fecha, descripción, categoría actual, subcategoría actual, fuente de categorización, balance, banco de origen. Formulario: import (text), date (Flatpickr via `x-data x-init="flatpickr(...)"`), description, amount, currency, category (datalist con categorías del usuario cargado por el PageModel), subcategory (datalist reactivo via `x-effect`). Botón submit con `x-data="{ loading: false }"`, `@@submit="loading = true"`, `:disabled="loading"`, spinner MDB. Bloque de error con `x-show="errorMessage"` + `x-transition`. Rutas estáticas vía `~/...` + `asp-append-version="true"`. Acceptance: renderiza sin errores de Alpine/MDB; cobertura manual de los 5 escenarios del handler. Dependencias: 4.1. Complejidad: L.

## Phase 5 — Frontend: Index button (PR 3, stacked)

- [x] 5.1 — `src/SauronSheet.Frontend/Pages/Transactions/Index.cshtml` (modificar, ~15 líneas): añadir columna `Actions` (o celda adicional en columna existente) con botón `✏️` (MDB outline-secondary, `btn-floating btn-sm`) que enlaza a `/transactions/edit/{id}` via tag helper `asp-page="/Transactions/Edit" asp-route-id="@transaction.Id"`. No tocar backend ni queries. Acceptance: clic en el botón abre `/Transactions/Edit/{id}`; inspección DOM muestra el atributo `href` correcto. Dependencias: 4.1 (solo la ruta, no requiere handler). Complejidad: S.

## Phase 6 — E2E tests (PR 4, stacked)

- [x] 6.1 — `e2e/tests/06-edit-transaction.spec.ts` (nuevo, ~150 líneas): suite Playwright que reutiliza la sesión autenticada del test 01. Casos: (a) navegar a `/transactions`, pulsar botón edit de la primera fila → URL contiene `/transactions/edit/{guid}` y el campo descripción se rellena con el valor actual; (b) modificar descripción, guardar → redirect a `/transactions` y mensaje de éxito visible (`role="alert"`); (c) intentar guardar con descripción duplicada de otra transacción del mismo día → mensaje de error visible y la URL sigue siendo `/transactions/edit/{id}`; (d) navegar a `/transactions/edit/{guid-inexistente}` → redirect a `/transactions`; (e) tras edición, recargar Index y verificar que el cambio persiste. Acceptance: `npx playwright test --config=e2e/playwright.config.ts --project=chromium e2e/tests/06-edit-transaction.spec.ts` pasa los 5 casos. Dependencias: 4.2, 5.1. Complejidad: M.

## Rollback Plan

- Cada PR se revierte con `git revert <sha>`; no hay migraciones de BD ni breaking changes en API pública. La interfaz `Transaction.Update()` es aditiva.
- Si un PR rompe Domain o Application, los tests rojos saltan antes de merge; no se llega a `main` con código roto.
