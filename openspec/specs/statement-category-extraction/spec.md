# Especificación: Statement Category Extraction

Extracción y resolución de categoría/subcategoría del extracto; las categorías y subcategorías se leen como literales del statement y se resuelven mediante normalización, lookup y creación controlada antes de persistir.

---

## Requisitos

### PCE-2: Función de normalización para deduplicación

| ID | Requisito | Escenarios |
|----|-----------|------------|
| PCE-2 | El sistema MUST computar una clave normalizada estable para lookup/deduplicación de categorías y subcategorías. La función MUST: (1) convertir a minúsculas, (2) eliminar diacríticos y tildes, (3) trim de whitespace extremo. MUST ser estática, determinista y centralizada en un único punto de Application layer. MUST NOT persistir la clave en DB en este cambio. | PCE-2a (tilde), PCE-2b (casing), PCE-2c (combinada) |

#### PCE-2a: Variante con tilde deduplica
- GIVEN literales "Alimentación" y "Alimentacion"
- WHEN se aplica la función de normalización a ambos
- THEN producen la misma clave normalizada

#### PCE-2b: Variante de casing deduplica
- GIVEN literales "COMPRAS" y "compras"
- WHEN se aplica la función de normalización
- THEN misma clave

#### PCE-2c: Tilde + casing combinados deduplicán
- GIVEN literales "Comisión Bancaria" y "comision bancaria"
- WHEN se aplica la función de normalización
- THEN misma clave

---

### PCE-3: Get-or-add de categoría para import de extracto

| ID | Requisito | Escenarios |
|----|-----------|------------|
| PCE-3 | `IStatementCategoryResolverService.ResolveOrCreateAsync(userId, rawCategory, rawSubcategory)` MUST buscar categoría por clave normalizada para el usuario; si no existe, MUST crear una nueva (name = literal del extracto, userId = usuario actual). MUST NOT coincidir ni crear categorías con `IsSystemDefault = true`. Si rawCategory es null/vacío, MUST retornar (null, null, RawOnly) sin crear nada. | PCE-3a (existente), PCE-3b (creación), PCE-3c (system default excluido), PCE-3d (rawCategory null), PCE-3e (concurrencia) |

#### PCE-3a: Categoría ya existe → reutiliza
- GIVEN Category(name="Compras", userId) con clave normalizada "compras"
- WHEN ResolveOrCreateAsync(userId, "COMPRAS", null)
- THEN retorna CategoryId existente, source=AutoMatched; no se crea duplicado

#### PCE-3b: Categoría no existe → se crea
- GIVEN usuario sin categoría cuya clave normalizada sea "inversionesfondos"
- WHEN ResolveOrCreateAsync(userId, "Inversiones Fondos", null)
- THEN Category(name="Inversiones Fondos", userId) creada; retorna nuevo CategoryId, source=AutoMatched

#### PCE-3c: System default no matchea ni bloquea
- GIVEN Category(clave normalizada="compras", IsSystemDefault=true)
- WHEN ResolveOrCreateAsync(userId, "Compras", null)
- THEN system default ignorado; nueva Category de usuario creada para el userId

#### PCE-3d: rawCategory null → RawOnly sin creación
- GIVEN rawCategory = null
- WHEN ResolveOrCreateAsync(userId, null, null)
- THEN CategoryId=null, SubcategoryId=null, source=RawOnly; nada creado

#### PCE-3e: Imports concurrentes de la misma categoría
- GIVEN dos llamadas simultáneas con rawCategory="Compras" para el mismo userId
- WHEN ambas ejecutan ResolveOrCreateAsync
- THEN exactamente una Category creada (UNIQUE constraint en DB + ON CONFLICT DO NOTHING)
- AND el segundo llamador recibe el mismo CategoryId que el primero

---

### PCE-4: Get-or-add de subcategoría para import de extracto

| ID | Requisito | Escenarios |
|----|-----------|------------|
| PCE-4 | Tras resolver la categoría, el servicio MUST buscar subcategoría por (userId, categoryId, clave_normalizada). Si no existe, MUST crear una con `IsAutoCreated = true`. Si rawSubcategory es null/vacío, MUST retornar SubcategoryId=null sin crear nada. | PCE-4a (existente), PCE-4b (creación auto), PCE-4c (null), PCE-4d (scope por categoría) |

#### PCE-4a: Subcategoría ya existe → reutiliza
- GIVEN Subcategory("ropa y complementos") bajo Category("Compras") para userId
- WHEN ResolveOrCreateAsync(userId, "Compras", "Ropa y Complementos")
- THEN SubcategoryId existente retornado; no se crea duplicado

#### PCE-4b: Subcategoría no existe → se crea con IsAutoCreated
- GIVEN ninguna subcategoría cuya clave normalizada sea "ropacomplementos" bajo la categoría
- WHEN ResolveOrCreateAsync(userId, "Compras", "Ropa y complementos")
- THEN Subcategory(name="Ropa y complementos", isAutoCreated=true) creada bajo categoryId

#### PCE-4c: rawSubcategory null → SubcategoryId null sin creación
- GIVEN rawSubcategory = null
- WHEN ResolveOrCreateAsync(userId, "Compras", null)
- THEN SubcategoryId=null; no se crea subcategoría

#### PCE-4d: Scope de subcategoría es categoryId, no global
- GIVEN Subcategory("online") bajo Category("Compras") Y Category("Ocio") para userId
- WHEN ResolveOrCreateAsync(userId, "Ocio", "online")
- THEN búsqueda y posible creación se hacen en scope de Category("Ocio"), independiente de la de "Compras"

---

### PCE-5: Persistencia con raw + IDs

| ID | Requisito | Escenarios |
|----|-----------|------------|
| PCE-5 | La transacción importada MUST persistir simultáneamente: (a) literales del extracto en BankCategory/BankSubcategory, (b) IDs resueltos en CategoryId/SubcategoryId. Import MUST NOT depender de categorías predefinidas ni system defaults. | PCE-5a (happy path), PCE-5b (sin subcategoría) |

#### PCE-5a: Raw + IDs persistidos — happy path
- GIVEN rawCategory="Compras", rawSubcategory="Ropa", IDs resueltos por StatementCategoryResolverService
- WHEN handler persiste la transacción
- THEN BankCategory="Compras", BankSubcategory="Ropa", CategoryId=resuelto, SubcategoryId=resuelto, source=AutoMatched

#### PCE-5b: Sin subcategoría en extracto
- GIVEN rawCategory="Compras", rawSubcategory=null
- WHEN handler persiste la transacción
- THEN BankCategory="Compras", BankSubcategory=null, SubcategoryId=null, CategoryId=resuelto, source=AutoMatched
