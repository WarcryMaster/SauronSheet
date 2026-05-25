# Delta: category-resolution

Cambio: `pdf-driven-category-import`.
El import handler evoluciona de lookup-only a get-or-add PDF-driven; el display helper prioriza el literal del PDF para transacciones importadas salvo override manual.

---

## MODIFIED Requirements

### Integración en Import Handler — IH-1

| ID | Requisito | Escenarios |
|----|-----------|------------|
| IH-1 | Handler MUST llamar a `IPdfCategoryResolverService.ResolveOrCreateAsync` (get-or-add) tras parsear cada row, ANTES de crear la transacción. MUST NOT llamar a `ICategoryResolutionService` en el flujo de import PDF. `ICategoryResolutionService` se reserva exclusivamente para el path de recategorización manual. | IH-1 (flujo pdf-driven) |

(Previously: el handler invocaba `ICategoryResolutionService` — lookup-only, sin capacidad de crear categorías)

#### IH-1: Flujo de importación PDF-driven
- GIVEN RawTransactionRow(Category="Compras", SubCategory="Ropa")
- WHEN el handler procesa el row
- THEN llama a `IPdfCategoryResolverService.ResolveOrCreateAsync`
- AND CategoryId y SubcategoryId quedan resueltos o recién creados
- AND Transaction se crea con BankCategory="Compras", BankSubcategory="Ropa", source=AutoMatched

---

## ADDED Requirements

### Display Helper — DH-1

| ID | Requisito | Escenarios |
|----|-----------|------------|
| DH-1 | `TransactionCategoryDisplayHelper` MUST retornar el literal `BankCategory` como nombre de display para transacciones donde `CategorySource != UserOverride` y `BankCategory != null`. Para `CategorySource = UserOverride`, MUST retornar `CategoryName` (nombre de la categoría asignada manualmente). Para `Legacy` sin `BankCategory`, MUST retornar `CategoryName`. | DH-1a (AutoMatched), DH-1b (RawOnly), DH-1c (UserOverride), DH-1d (Legacy) |

#### DH-1a: AutoMatched → muestra BankCategory (literal PDF)
- GIVEN Transaction(BankCategory="Compras", CategoryName="Compras", source=AutoMatched)
- WHEN `GetDisplayCategory` es invocado
- THEN retorna "Compras" (el literal bruto del PDF, no el nombre resuelto)

#### DH-1b: RawOnly → muestra BankCategory aunque no haya CategoryId
- GIVEN Transaction(BankCategory="ING Direct", CategoryName=null, source=RawOnly)
- WHEN `GetDisplayCategory` es invocado
- THEN retorna "ING Direct"

#### DH-1c: UserOverride → muestra CategoryName resuelto (respeta override manual)
- GIVEN Transaction(BankCategory="Compras", CategoryName="Ropa", source=UserOverride)
- WHEN `GetDisplayCategory` es invocado
- THEN retorna "Ropa" (nombre de la categoría asignada por el usuario)

#### DH-1d: Legacy sin BankCategory → muestra CategoryName
- GIVEN Transaction(BankCategory=null, CategoryName="Alimentación", source=Legacy)
- WHEN `GetDisplayCategory` es invocado
- THEN retorna "Alimentación"
