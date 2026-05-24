# Especificación: Bank Category Resolution

Compara valores brutos del PDF contra categorías del usuario (case-insensitive), con override vía `bank_category_translations`. Sin IA ni auto-creación.

---

## 1. Captura de Valores Brutos (Category Resolution)

| ID | Requisito | Escenarios |
|----|-----------|------------|
| CR-1 | Toda transacción MUST guardar bank_category y bank_subcategory del PDF parser sin transformación (excepto whitespace trim) | CR-1a (literales), CR-1b (null) |
| CR-2 | ICategoryResolutionService MUST aceptar (userId, bankCategory, bankSubcategory) y retornar (CategoryId?, SubcategoryId?, CategorySource). MUST NOT crear nada | CR-2a (match traducción), CR-2b (match directo), CR-2c (sin match), CR-2d (subcat anidada) |

#### CR-1a: Valores literales preservados
- GIVEN RawTransactionRow(Category="Compras", SubCategory="Ropa y complementos")
- WHEN se crea la transacción importada
- THEN BankCategory="Compras" Y BankSubcategory="Ropa y complementos"

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

---

## 2. Entidad Subcategory (Subcategory Management)

| ID | Requisito | Escenarios |
|----|-----------|------------|
| SC-1 | Subcategory MUST ser AggregateRoot con SubcategoryId (strong-typed), UserId, CategoryId, Name, IsAutoCreated | SC-1 (creación paramétrica) |
| SC-2 | ISubcategoryRepository MUST exponer: GetById, FindByNameAndCategory, GetByUserId, GetByCategoryId, Add | — |
| SC-3 | ResolutionService MUST consultar subcategorías por nombre+CategoryId | Integrado en CR-2d |

#### SC-1: Creación de subcategoría
- GIVEN userId, categoryId, name="Ropa y complementos"
- WHEN new Subcategory(id, userId, categoryId, name, isAutoCreated: false)
- THEN Name="Ropa y complementos" Y IsAutoCreated=false

---

## 3. Integración en Import Handler

| ID | Requisito | Escenarios |
|----|-----------|------------|
| IH-1 | Handler MUST llamar a ResolutionService tras parsear cada row, ANTES de crear la transacción | IH-1 (flujo completo) |
| IH-2 | Constructor de Transaction MUST aceptar bankCategory, bankSubcategory, categorySource, subcategoryId (nullable) | — |
| IH-3 | Si source=RawOnly, CategoryId MUST ser null | Integrado en CR-2c |

#### IH-1: Flujo completo de importación
- GIVEN RawTransactionRow(Category="Compras", SubCategory="Ropa")
- WHEN el handler procesa el row
- THEN llama a ResolutionService → asigna CategoryId+SubcategoryId+source, crea Transaction

---

## 4. Entidad Transaction

| ID | Requisito | Escenarios |
|----|-----------|------------|
| TX-1 | Transaction MUST añadir BankCategory(string?), BankSubcategory(string?), SubcategoryId(SubcategoryId?), CategorySource(CategorySource) | TX-1 (legacy) |
| TX-2 | Categorize() MUST establecer CategorySource=UserOverride | TX-2 (recategorización) |
| TX-3 | CategorySource enum: Legacy, RawOnly, AutoMatched, UserOverride | — |

#### TX-1: Carga de transacción legacy
- GIVEN transacción pre-feature sin bank_category en DB
- WHEN TransactionRow.ToDomain()
- THEN BankCategory=null, BankSubcategory=null, CategorySource=Legacy

#### TX-2: Recategorización manual
- GIVEN transacción con source=RawOnly
- WHEN transaction.Categorize(newCategoryId)
- THEN CategorySource=UserOverride Y CategoryId=newCategoryId

---

## 5. Eliminación de System Defaults

| ID | Requisito | Escenarios |
|----|-----------|------------|
| SD-1 | 24 system defaults MUST eliminarse de DB (migración DELETE) | SD-1 (verificación) |
| SD-2 | SeedSystemDefaultsCommand y Handler MUST eliminarse (código y llamadas) | — |
| SD-3 | CategoryService.GetSystemDefaults() y caché MUST eliminarse | — |
| SD-4 | GetCategoriesQueryHandler MUST eliminar SeedSystemDefaultsCommand call y sort por IsSystemDefault | — |

#### SD-1: Migración DELETE
- GIVEN 24 system defaults con is_system_default=true y user_id IS NULL
- WHEN se ejecuta la migración
- THEN todas se eliminan (verificado: 0 transacciones legacy apuntan)

---

## 6. DTOs y Mappings

| ID | Requisito | Escenarios |
|----|-----------|------------|
| DT-1 | TransactionDto MUST añadir BankCategory, BankSubcategory, SubcategoryId, SubcategoryName, CategorySource | DT-1 |
| DT-2 | TransactionRow MUST añadir bank_category, bank_subcategory, subcategory_id, category_source | — |
| DT-3 | ToDomain/FromDomain MUST mapear todos los nuevos campos | — |

#### DT-1: DTO completo
- GIVEN Transaction con bankCategory="Compras", source=AutoMatched
- WHEN se mapea a TransactionDto
- THEN BankCategory="Compras" Y CategorySource=AutoMatched
