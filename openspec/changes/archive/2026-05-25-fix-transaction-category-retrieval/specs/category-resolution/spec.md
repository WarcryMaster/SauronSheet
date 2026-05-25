# Delta para category-resolution

> Corrección de tres defectos de implementación contra CR-1, CR-2 y DT-1 del spec principal.
> No se añaden ni eliminan requisitos; se enriquecen los bloques existentes con escenarios de regresión explícitos.

---

## MODIFIED Requirements

### Requirement: CR-1

Toda transacción MUST guardar bank_category y bank_subcategory del PDF parser sin transformación (excepto whitespace trim).
(Previously: los escenarios no cubrían el trim explícitamente; faltaba CR-1c como regresión)

#### CR-1a: Valores literales preservados

- GIVEN RawTransactionRow(Category="Compras", SubCategory="Ropa y complementos")
- WHEN se crea la transacción importada
- THEN BankCategory="Compras" Y BankSubcategory="Ropa y complementos"

#### CR-1b: Subcategoría nula

- GIVEN RawTransactionRow(Category="Compras", SubCategory=null)
- WHEN se crea la transacción importada
- THEN BankCategory="Compras" Y BankSubcategory=null

#### CR-1c: Whitespace trimmed en persistencia (regresión)

- GIVEN RawTransactionRow(Category="  Compras  ", SubCategory=" Ropa ")
- WHEN el import handler persiste la transacción
- THEN BankCategory="Compras" Y BankSubcategory="Ropa"

---

### Requirement: CR-2

ICategoryResolutionService MUST aceptar (userId, bankCategory, bankSubcategory) y retornar (CategoryId?, SubcategoryId?, CategorySource). MUST NOT crear nada. La búsqueda de traducción MUST evaluar la coincidencia exacta (bank_category + bank_subcategory) ANTES del fallback genérico (bank_category, null).
(Previously: la implementación devolvía el match genérico antes que el exacto — escenario CR-2e faltaba)

#### CR-2a: Match vía traducción

- GIVEN bank_category_translations con (bank_category="Aliment.", resolved_category_name="Alimentación") Y Category("Alimentación") existe
- WHEN Resolve(userId, "Aliment.", null)
- THEN resolved_category_name="Alimentación", Category("Alimentación") encontrada por nombre, source=AutoMatched

#### CR-2b: Match directo (case-insensitive)

- GIVEN bank_category="compras" Y Category("Compras")
- WHEN Resolve(userId, "compras", null)
- THEN CategoryId=id("Compras"), source=AutoMatched

#### CR-2c: Sin match → RawOnly

- GIVEN bank_category="ING Direct" sin traducción ni categoría del usuario
- WHEN ResolutionService.Resolve(userId, "ING Direct", null)
- THEN CategoryId=null, SubcategoryId=null, source=RawOnly

#### CR-2d: Subcategoría anidada

- GIVEN Category("Compras") con Subcategory("Ropa y complementos")
- WHEN ResolutionService.Resolve(userId, "Compras", "Ropa y complementos")
- THEN CategoryId Y SubcategoryId asignados, source=AutoMatched

#### CR-2e: Traducción exacta prevalece sobre genérica (regresión)

- GIVEN translations: fila-A (bank_category="Compras", bank_subcategory="Ropa", resolved="Moda") Y fila-B (bank_category="Compras", bank_subcategory=null, resolved="General")
- WHEN Resolve(userId, "Compras", "Ropa")
- THEN resolved_category_name="Moda" (exacta gana; fila-B ignorada)

---

### Requirement: DT-1

TransactionDto MUST añadir BankCategory, BankSubcategory, SubcategoryId, SubcategoryName, CategorySource. Los query handlers MUST poblar SubcategoryName cuando SubcategoryId != null; MUST NOT devolver null en ese caso.
(Previously: SubcategoryName presente en el DTO pero nunca poblada por los handlers de lectura)

#### DT-1: DTO completo

- GIVEN Transaction con bankCategory="Compras", source=AutoMatched
- WHEN se mapea a TransactionDto
- THEN BankCategory="Compras" Y CategorySource=AutoMatched

#### DT-1b: SubcategoryName poblada en lectura (regresión)

- GIVEN Transaction con SubcategoryId=X Y Subcategory(id=X, name="Ropa") en repositorio
- WHEN un query handler construye TransactionDto
- THEN SubcategoryName="Ropa" (NOT null)

#### DT-1c: SubcategoryName null cuando sin subcategoría

- GIVEN Transaction con SubcategoryId=null
- WHEN un query handler construye TransactionDto
- THEN SubcategoryName=null
