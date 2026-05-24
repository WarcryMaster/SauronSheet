# Especificación: Inline Category Creation

Creación inline de categorías durante el alta de transacciones. El usuario escribe un nombre nuevo y el sistema lo crea al enviar el formulario.

---

## Requisitos

### RF-1: Resolución nombre → ID en el PageModel

| ID | Requisito | Escenarios |
|----|-----------|------------|
| RF-1 | El PageModel MUST resolver `CategoryName` (string) a `Guid?` antes de enviar el command. Reglas: null/vacío→null; match case-insensitive→ID existente; sin match→crear vía `CreateCategoryCommand` y usar nuevo ID | RF-1a (vacío), RF-1b (match existente), RF-1c (nuevo nombre), RF-1d (límite 500) |

#### RF-1a: Sin categoría (Uncategorized)
- GIVEN Input.CategoryName es null o vacío
- WHEN OnPostAsync resuelve la categoría
- THEN CategoryId = null (sin error, transacción Uncategorized)

#### RF-1b: Match con categoría existente (case-insensitive)
- GIVEN categorías: ["Comida", "Transporte"]
- WHEN Input.CategoryName = "comida"
- THEN CategoryId = Id de "Comida" (case-insensitive match)

#### RF-1c: Nombre nuevo → crear categoría
- GIVEN categorías sin "Nómina"
- WHEN Input.CategoryName = "Nómina"
- THEN CreateCategoryCommand("Nómina", Expense) se envía y CategoryId = nuevo Id

#### RF-1d: Nombre muy largo truncado
- GIVEN CategoryName con más de 500 caracteres
- WHEN se envía el command
- THEN el modelo rechaza con error de validación (StringLength)

### RF-2: Datalist searchable en UI

| ID | Requisito | Escenarios |
|----|-----------|------------|
| RF-2 | UI MUST mostrar `<input list="categories">` + `<datalist id="categories">` con nombres de categorías. MUST usar `form-control` consistente con los demás campos | RF-2a (type-to-filter), RF-2b (sin categorías) |

#### RF-2a: Type-to-filter nativo
- GIVEN datalist con ["Comida", "Transporte", "Ocio"]
- WHEN usuario teclea "tra"
- THEN el navegador filtra a "Transporte" (comportamiento HTML5 nativo)

#### RF-2b: Sin categorías (primer uso)
- GIVEN usuario sin categorías creadas
- WHEN carga /transactions/add
- THEN el datalist está vacío, no impide escribir un nombre nuevo

### RF-3: Limpieza IsSystemDefault

| ID | Requisito | Escenarios |
|----|-----------|------------|
| RF-3 | CategoryDto MUST eliminar `IsSystemDefault`. Vistas y handlers MUST dejar de referenciarlo | RF-3a (sin roturas) |

#### RF-3a: Sin referencias rotas
- GIVEN CategoryDto sin IsSystemDefault
- WHEN se compila el proyecto
- THEN no hay errores de compilación en vistas ni handlers

## Criterios de Aceptación

| # | Criterio | Verificación |
|---|----------|--------------|
| CA-1 | Escribir nombre existente (case-insensitive) asigna la categoría correcta | Test unitario del PageModel |
| CA-2 | Escribir nombre nuevo crea la categoría y asigna el nuevo ID | Test de integración handler+PageModel |
| CA-3 | Dejar vacío asigna null (Uncategorized) | Test unitario |
| CA-4 | El input reacciona al tipeo para filtrar opciones (nativo HTML5) | Verificación manual |
| CA-5 | No hay referencias a IsSystemDefault en CategoryDto ni vistas | Build sin errores + grep |
