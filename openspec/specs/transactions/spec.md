# Especificación: Transaction Source Filter

Filtro por `ImportedFrom` (archivo de origen) en el listado de /transactions, implementado con searchable combobox HTML5 vía `<datalist>`.

---

## Resumen

Añade un filtro buscador por `ImportedFrom` en el formulario de /transactions. El usuario puede seleccionar (o teclear para filtrar) un archivo de origen de una lista de valores distintos extraídos de sus transacciones. Al seleccionar uno, la tabla se filtra solo a transacciones de ese origen.

---

## Requisitos Funcionales

### RF-1: Especificación de dominio

| ID | Requisito | Escenarios |
|----|-----------|------------|
| RF-1 | TransactionByImportedFromSpecification MUST existir en Domain/Specifications, filtrando `t.ImportedFrom == value` (case-insensitive), siguiendo el patrón de `TransactionByCategorySpecification` | RF-1a (match), RF-1b (null) |

#### RF-1a: Match exacto (case-insensitive)
- GIVEN transacciones con ImportedFrom="nomina.pdf" e ImportedFrom="NOMINA.pdf"
- WHEN se crea la spec con value="nomina.pdf"
- THEN la spec empareja ambas (case-insensitive)

#### RF-1b: ImportedFrom null en algunas transacciones
- GIVEN transacciones con ImportedFrom=null e ImportedFrom="facturas.pdf"
- WHEN se filtra por "facturas.pdf"
- THEN solo se incluyen las que tienen ImportedFrom="facturas.pdf" (null no matchea)

---

### RF-2: Query record

| ID | Requisito | Escenarios |
|----|-----------|------------|
| RF-2 | GetTransactionsQuery MUST añadir `string? ImportedFrom = null` como parámetro opcional | RF-2a (default null) |

#### RF-2a: Parámetro opcional
- GIVEN GetTransactionsQuery sin ImportedFrom
- WHEN new GetTransactionsQuery(...)
- THEN ImportedFrom es null (no se aplica filtro)

---

### RF-3: Composición condicional en handler

| ID | Requisito | Escenarios |
|----|-----------|------------|
| RF-3 | Handler MUST componer TransactionByImportedFromSpecification condicionalmente si `ImportedFrom` tiene valor | RF-3a (filtro activo), RF-3b (sin filtro) |

#### RF-3a: Filtro activo compone spec
- GIVEN query con ImportedFrom="nomina.pdf"
- WHEN handler ejecuta
- THEN spec compuesta incluye TransactionByImportedFromSpecification("nomina.pdf") via AND

#### RF-3b: Sin filtro omite spec
- GIVEN query con ImportedFrom=null
- WHEN handler ejecuta
- THEN no se añade TransactionByImportedFromSpecification a la spec compuesta

---

### RF-6: Query separada para fuentes disponibles

| ID | Requisito | Escenarios |
|----|-----------|------------|
| RF-6 | MUST existir una query separada `GetDistinctImportedSourcesQuery` que retorna `List<string>` con valores `ImportedFrom` no-null/empty distintos del usuario, ordenados alfabéticamente. El handler usa `FindBySpecificationAsync` con `TransactionByUserSpecification` y extrae distinct en memoria | RF-6a (sources poblados), RF-6b (sin transacciones) |

#### RF-6a: Fuentes pobladas
- GIVEN usuario con transacciones de 3 archivos: "facturas.pdf", "nomina.pdf", "recibos.pdf"
- WHEN se ejecuta GetDistinctImportedSourcesQuery
- THEN retorna ["facturas.pdf", "nomina.pdf", "recibos.pdf"] ordenados

#### RF-6b: Sin transacciones
- GIVEN usuario sin transacciones
- WHEN se ejecuta GetDistinctImportedSourcesQuery
- THEN retorna lista vacía

---

### RF-4: PageModel con binding y fuentes

| ID | Requisito | Escenarios |
|----|-----------|------------|
| RF-4 | IndexModel MUST añadir `ImportedFrom` ([BindProperty(SupportsGet = true)]) y `AvailableSources` (List<string?>). MUST cargar AvailableSources vía `GetDistinctImportedSourcesQuery`, pasar ImportedFrom al `GetTransactionsQuery`, y exponer AvailableSources a la vista | RF-4a (carga con fuentes), RF-4b (filtro preservado en GET) |

#### RF-4a: AvailableSources en model
- GIVEN IndexModel.OnGetAsync()
- WHEN handler retorna GetTransactionsResult con AvailableSources
- THEN Model.AvailableSources contiene los valores extraídos

#### RF-4b: ImportedFrom bindeado desde query string
- GIVEN GET /transactions?ImportedFrom=nomina.pdf
- WHEN OnGetAsync()
- THEN query.ImportedFrom = "nomina.pdf"

---

### RF-5: Searchable combobox en UI

| ID | Requisito | Escenarios |
|----|-----------|------------|
| RF-5 | UI MUST renderizar `<input list="importedFromList">` + `<datalist id="importedFromList">` entre el date range y el botón Apply. Option vacía MUST existir para limpiar filtro. MUST usar `form-control form-control-sm` (consistente MDBootstrap) | RF-5a (selección filtra), RF-5b (clear restaura), RF-5c (type-to-filter), RF-5d (sin sources) |

#### RF-5a: Selección filtra transacciones
- GIVEN página /transactions con filtros
- WHEN usuario selecciona "nomina.pdf" del datalist y hace click en Apply
- THEN la tabla muestra solo transacciones con ImportedFrom="nomina.pdf"

#### RF-5b: Clear restaura listado completo
- GIVEN página filtrada por "nomina.pdf"
- WHEN usuario selecciona la opción vacía (o navega a /transactions sin ImportedFrom)
- THEN la tabla muestra todas las transacciones del usuario

#### RF-5c: Type-to-filter funciona
- GIVEN datalist con opciones ["facturas.pdf", "nomina.pdf", "recibos.pdf"]
- WHEN usuario teclea "nom" en el input
- THEN el navegador filtra las opciones a "nomina.pdf" (comportamiento nativo HTML5)

#### RF-5d: Sin AvailableSources (primer uso)
- GIVEN usuario sin transacciones
- WHEN carga /transactions
- THEN el input datalist está vacío, no impide usar otros filtros

---

## Criterios de Aceptación

| # | Criterio | Verificación |
|---|----------|--------------|
| CA-1 | El datalist muestra valores `ImportedFrom` distintos del usuario, ordenados alfabéticamente | Test unitario de GetDistinctImportedSourcesQuery |
| CA-2 | Seleccionar un origen filtra la tabla solo a transacciones de ese archivo | Test unitario del handler spec composition |
| CA-3 | La opción vacía (ningún origen) restaura el listado completo | Test handler sin ImportedFrom |
| CA-4 | El input `ImportedFrom` reacciona al tipeo para filtrar opciones del datalist | Comportamiento nativo HTML5, verificación manual |
| CA-5 | No se rompe la paginación actual ni otros filtros al usar el nuevo filtro | Test de integración combinando con fecha/categoría |
| CA-6 | Menos de 50 líneas C# nuevas y menos de 20 líneas Razor/JS nuevas | Revisión de diff |
