# Especificación: Chart Legend Ordering

## Propósito

Toda instancia de Chart.js en la aplicación DEBE mostrar la leyenda en el
mismo orden en que aparecen los datasets. Para la gráfica de Spending by
Category, la capa de Application es la fuente única del orden; el frontend
debe confiar en el orden recibido y nunca reordenar por su cuenta.

---

## Requisitos

### Requisito: Handler ordena categorías por total descendente

`GetMonthlySpendingByCategoryQueryHandler` DEBE emitir el resultado con las
categorías ordenadas por total gastado descendente dentro del rango
solicitado.

#### Escenario: Categoría de mayor total aparece primero

- DADO gastos en el rango 2026-Q2: Compras = 800, Ocio = 300, Transporte = 150
- CUANDO se invoca el handler con el rango 2026-04-01 a 2026-06-30
- ENTONCES la primera fila del resultado tiene `CategoryName = "Compras"`

#### Escenario: Cambio de rango reordena las categorías

- DADO "Compras" primero en el rango 2026-Q2 con total 800
- CUANDO se invoca el handler con un rango donde "Ocio" alcanza 1000
- ENTONCES "Ocio" aparece primero

#### Escenario: Empates desempatados de forma estable

- DADO dos categorías con idéntico total
- CUANDO se invoca el handler
- ENTONCES el orden entre ellas es estable (segundo criterio = nombre alfabético ascendente)

### Requisito: Init de Chart.js respeta el orden del payload

`initCategoryStackedChart` (en `wwwroot/js/charts.js`) DEBE construir el array
de `datasets` en el mismo orden en que las categorías aparecen en el payload,
sin reordenar ni desduplicar alterando la secuencia.

#### Escenario: Leyenda coincide con el orden del dataset

- DADO un payload con categorías en orden [Compras, Ocio, Transporte]
- CUANDO se inicializa el Chart.js
- ENTONCES la leyenda muestra "Compras, Ocio, Transporte" en ese mismo orden

#### Escenario: JSDoc documenta el contrato de orden

- DADO `wwwroot/js/charts.js`
- CUANDO se lee la función `initCategoryStackedChart`
- ENTONCES un comentario JSDoc declara explícitamente que el orden de la leyenda depende del orden del array de entrada y que el handler es responsable de producirlo

### Requisito: Regla documentada en instrucciones de frontend

`.github/instructions/razor-frontend.instructions.md` DEBE contener una sección
"Charts" que documente la regla de orden de leyenda para todo Chart.js nuevo
o modificado.

#### Escenario: Sección Charts existe en el archivo de instrucciones

- DADO `.github/instructions/razor-frontend.instructions.md`
- CUANDO se lee el archivo
- ENTONCES existe un encabezado `## Charts` y dentro la regla explícita "legend order MUST match dataset order"

#### Escenario: Cualquier Chart.js nuevo respeta la regla

- DADO un nuevo `<canvas>` que instancia `new Chart(canvas, {...})` en cualquier `.cshtml`
- CUANDO se revisa su configuración
- ENTONCES el orden de los `datasets` del array coincide con el orden esperado de la leyenda

#### Escenario: `Budgets/Comparison.cshtml` cumple la regla

- DADO el inline `new Chart(canvas, {...})` con datasets `Budget` y `Actual` en ese orden
- CUANDO se renderiza la página
- ENTONCES la leyenda muestra "Budget, Actual" en ese mismo orden

---

## Criterios de Aceptación

- [ ] Los tests unitarios de `GetMonthlySpendingByCategoryQuery` cubren el orden por total descendente y los empates
- [ ] `wwwroot/js/charts.js` incluye JSDoc declarando el contrato de orden
- [ ] `initCategoryStackedChart` no reordena ni desduplica alterando la secuencia
- [ ] `.github/instructions/razor-frontend.instructions.md` contiene la sección "Charts" con la regla
- [ ] `Budgets/Comparison.cshtml` cumple la regla
- [ ] `dotnet test` y `dotnet build` pasan

## Fuera de Alcance

- Cambiar el motor de gráficos (sigue siendo Chart.js)
- Añadir paginación o filtros a la leyenda
- Internacionalización de etiquetas de leyenda (siguen siendo nombres de categoría)
