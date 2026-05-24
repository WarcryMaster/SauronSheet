# Delta para: Transaction Add Form (category-selector-inline-create)

## MODIFIED Requirements

### RF-4: PageModel con binding y fuentes (AddTransaction)

**ID**: RF-4 (transactions spec) — Modificado

| ID | Requisito | Escenarios |
|----|-----------|------------|
| RF-4 | AddModel MUST aceptar `CategoryName` (string?) en lugar de `CategoryId` (Guid?) en el InputModel. MUST cargar lista de nombres de categorías vía `GetCategoriesQuery`. MUST resolver CategoryName→CategoryId en OnPostAsync | RF-4a (match → ID), RF-4b (nuevo → crear), RF-4c (vacío → null) |

(Anteriormente: AddModel aceptaba `CategoryId` (Guid?) vinculado a un `<select>`)

#### RF-4a: Match con categoría existente
- GIVEN Model.Categories con [CategoryDto("Comida"), CategoryDto("Transporte")]
- WHEN Input.CategoryName = "Comida"
- THEN se usa CategoryId de "Comida" en CreateTransactionCommand

#### RF-4b: Nuevo nombre → crear categoría
- GIVEN Model.Categories sin "Nómina"
- WHEN Input.CategoryName = "Nómina"
- THEN se envía CreateCategoryCommand("Nómina", type) y se usa el resultado como CategoryId

#### RF-4c: Vacío o null → Uncategorized
- GIVEN Input.CategoryName = "" o null
- THEN CategoryId = null en CreateTransactionCommand (sin error)

### RF-5: Searchable combobox en UI (Add Transaction)

**ID**: RF-5 (transactions spec) — Modificado

| ID | Requisito | Escenarios |
|----|-----------|------------|
| RF-5 | UI MUST renderizar `<input list="categories">` + `<datalist id="categories">` en lugar de `<select>`. Option vacía para Uncategorized. MUST usar `form-control` consistente con otros campos. MUST NO referenciar `IsSystemDefault` | RF-5a (selección existente), RF-5b (nuevo nombre), RF-5c (type-to-filter), RF-5d (sin categorías) |

(Anteriormente: renderizaba `<select>` con `<option value="@category.Id">`, mostraba `IsSystemDefault` lock icon)

#### RF-5a: Seleccionar categoría existente
- GIVEN datalist con categorías del usuario
- WHEN usuario selecciona una del datalist y envía
- THEN Input.CategoryName contiene el nombre seleccionado

#### RF-5b: Escribir nuevo nombre
- GIVEN ningún match en el datalist
- WHEN usuario escribe "Nómina" y envía
- THEN Input.CategoryName = "Nómina"

#### RF-5c: Type-to-filter funciona
- GIVEN datalist con opciones
- WHEN usuario teclea parcialmente
- THEN el navegador filtra las opciones (comportamiento nativo HTML5)

#### RF-5d: Sin categorías disponibles
- GIVEN usuario sin categorías creadas
- WHEN carga /transactions/add
- THEN el input es funcional, permite escribir nombre nuevo

## REMOVED Requirements

### RF: Select de categorías con CategoryId

Se elimina el `<select>` con `Guid? CategoryId` del formulario de Add Transaction. Ya no se renderizan opciones como `<option value="@category.Id">`. El dato de entrada cambia de ID a nombre.

(Reason: Reemplazado por datalist searchable con resolución nombre→ID en el PageModel, siguiendo el patrón ImportedFrom.)
