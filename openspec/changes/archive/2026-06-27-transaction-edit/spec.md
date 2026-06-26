# Especificación: Edición de Transacciones

## Resumen

Añadir funcionalidad de edición para transacciones existentes mediante una página dedicada `/Transactions/Edit/{id}`, permitiendo modificar fecha, descripción, importe, moneda, categoría y subcategoría, con validación de duplicados, aislamiento por tenant y ownership de categoría/subcategoría.

## Requisitos Funcionales

### RF-1: Domain — Transaction.Update()
- `Transaction.Update(Money, DateTime, string, CategoryId?, SubcategoryId?, CategorySource)` actualiza `Amount`, `Date`, `Description`, `CategoryId`, `SubcategoryId`, `CategorySource` y `UpdatedAt`.
- Preserva metadatos de importación (`ImportedFrom`, `BankCategory`, `BankSubcategory`, `Balance`).
- Guard clauses: `description` vacía/whitespace, `amount` null, descripción >500 caracteres, fecha inválida.

### RF-2: Query — GetTransactionById
- `GetTransactionByIdQuery(Guid TransactionId)` retorna `TransactionDto` con nombres de categoría y subcategoría.
- Tenant isolation: si el ID no pertenece al usuario actual → `EntityNotFoundException`.
- Batch lookup de nombres de categoría/subcategoría vía diccionario en memoria (evita N+1).

### RF-3: Command — UpdateTransactionCommand
- `UpdateTransactionCommand` con orden estricto de validación: (1) tenant check, (2) ownership de categoría, (3) subcategoría pertenece a categoría, (4) detección de duplicados (excluyendo self por ID), (5) ejecución de `Transaction.Update()` y persistencia.
- `CategorySource` = `UserOverride` si cambia categoría; se preserva si no cambia.
- Errores: `EntityNotFoundException`, `DuplicateEntityException`, `DomainException`.

### RF-4: UI — Página Edit
- Página `/Transactions/Edit/{id}` con resumen read-only y formulario editable.
- Flatpickr para fecha, datalist para categorías, subcategorías reactivas vía Alpine.js `x-effect`.
- Botón submit con spinner (`x-data="{ loading: false }"`, `:disabled="loading"`).
- Bloque de error con `x-show` + `x-transition`.

### RF-5: UI — Botón en Index
- Botón ✏️ en columna Actions de `/Transactions` que enlaza a `/Transactions/Edit/{id}`.

## Criterios de Aceptación

| # | Criterio | Verificación |
|---|----------|--------------|
| CA-1 | `Transaction.Update()` aplica guard clauses y actualiza campos editables | Tests unitarios Domain |
| CA-2 | Query retorna DTO con nombres de categoría/subcategoría | Tests unitarios Query Handler |
| CA-3 | Command rechaza transacción de otro usuario | Tests unitarios Command Handler |
| CA-4 | Command detecta duplicados excluyendo self | Tests unitarios Command Handler |
| CA-5 | Edición exitosa → redirect a Index con mensaje de éxito | Tests E2E |
| CA-6 | ID inexistente → redirect a Index | Tests E2E |
