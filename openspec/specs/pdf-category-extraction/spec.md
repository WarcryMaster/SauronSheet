# Especificación: PDF Category Extraction

Extracción de categoría/subcategoría del PDF; path ING usa `IngControlledTaxonomy` (lista controlada, extracción L→R, RawOnly fallback); path no-ING extrae literales sin lista cerrada; normalización solo para lookup/deduplicación; get-or-add para resolución de IDs antes de persistir.

---

## Requisitos

### PCE-1: Extracción de literales sin listas cerradas

| ID    | Requisito | Escenarios |
|-------|-----------|------------|
| PCE-1 | Para el path ING, `IngBankPdfParser` MUST extraer categoría y subcategoría directamente desde las zonas X del bloque (`IngRawColumnExtractor` + `IngColumnThresholds`) sin instanciar `IngControlledTaxonomy` ni usar ninguna lista cerrada. Cuando la geometría no produce señal fiable, MUST aplicar fallback conservador: `Category = null`, `SubCategory = null`, descripción literal conservada; la transacción NO debe descartarse. Para el path no-ING el comportamiento es inalterado. Solo whitespace trim en ambos paths. | PCE-1a, PCE-1b, PCE-1c, PCE-1d, PCE-1e, PCE-1f |

(Previously: path ING usaba `IngControlledTaxonomy` L→R como fuente primaria; valores no reconocidos preservados como `RawOnly`.)

#### PCE-1a: DAZN — categoría + subcategoría + descripción desde columnas

- GIVEN fila ING con palabras en zona categoría `Compras`, zona subcategoría `Online`, zona descripción `DAZN`
- WHEN `IngRawColumnExtractor` clasifica por zona X usando `IngColumnThresholds`
- THEN `Category = "Compras"`, `SubCategory = "Online"`, `Description = "DAZN"`

#### PCE-1b: Parking — literal multipalabra preservado en subcategoría

- GIVEN fila ING con zona categoría `Vehículo y transporte` y zona subcategoría `Parking y garaje`
- WHEN `IngRawColumnExtractor` clasifica palabras por zona X
- THEN `Category = "Vehículo y transporte"`, `SubCategory = "Parking y garaje"`

#### PCE-1c: Nómina — categoría sin subcategoría

- GIVEN fila ING con zona categoría `Nominas` y zona subcategoría sin palabras
- WHEN `IngRawColumnExtractor` procesa el bloque
- THEN `Category = "Nominas"`, `SubCategory = null`

#### PCE-1d: Traspaso entre cuentas propias

- GIVEN fila ING con zona categoría `Mis cuentas y depósitos` y zona subcategoría `Traspasos propios`
- WHEN `IngRawColumnExtractor` clasifica por zona X
- THEN `Category = "Mis cuentas y depósitos"`, `SubCategory = "Traspasos propios"`

#### PCE-1e: Fallback conservador — geometría insuficiente

- GIVEN bloque ING donde `IngColumnThresholds` no derivan umbrales fiables para ese bloque
- WHEN `IngRawColumnExtractor` no produce señal de columna
- THEN `Category = null`, `SubCategory = null`; descripción literal conservada; transacción no descartada

#### PCE-1f: Path no-ING sin lista cerrada

- GIVEN PDF de otro banco procesado por parser genérico
- WHEN el parser procesa la fila
- THEN categoría y subcategoría se extraen como literales del PDF sin comparar contra ninguna lista

---

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

### PCE-3: Get-or-add de categoría para import PDF

| ID | Requisito | Escenarios |
|----|-----------|------------|
| PCE-3 | `IPdfCategoryResolverService.ResolveOrCreateAsync(userId, rawCategory, rawSubcategory)` MUST buscar categoría por clave normalizada para el usuario; si no existe, MUST crear una nueva (name = literal PDF, userId = usuario actual). MUST NOT coincidir ni crear categorías con `IsSystemDefault = true`. Si rawCategory es null/vacío, MUST retornar (null, null, RawOnly) sin crear nada. | PCE-3a (existente), PCE-3b (creación), PCE-3c (system default excluido), PCE-3d (rawCategory null), PCE-3e (concurrencia) |

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

### PCE-4: Get-or-add de subcategoría para import PDF

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
| PCE-5 | La transacción importada MUST persistir simultáneamente: (a) literales del PDF en BankCategory/BankSubcategory, (b) IDs resueltos en CategoryId/SubcategoryId. Import MUST NOT depender de categorías predefinidas ni system defaults. | PCE-5a (happy path), PCE-5b (sin subcategoría) |

#### PCE-5a: Raw + IDs persistidos — happy path
- GIVEN rawCategory="Compras", rawSubcategory="Ropa", IDs resueltos por PdfCategoryResolverService
- WHEN handler persiste la transacción
- THEN BankCategory="Compras", BankSubcategory="Ropa", CategoryId=resuelto, SubcategoryId=resuelto, source=AutoMatched

#### PCE-5b: Sin subcategoría PDF
- GIVEN rawCategory="Compras", rawSubcategory=null
- WHEN handler persiste la transacción
- THEN BankCategory="Compras", BankSubcategory=null, SubcategoryId=null, CategoryId=resuelto, source=AutoMatched
