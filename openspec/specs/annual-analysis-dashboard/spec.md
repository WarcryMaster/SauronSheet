# annual-analysis-dashboard — Especificación

## Propósito

Dashboard de Análisis Anual con KPIs, gráficos de tendencia y distribución, comparativa interanual y tablas de detalle colapsables. Reemplaza la vista actual basada únicamente en tablas de 15 columnas. Los datos provienen de los DTOs existentes; no se modifican consultas ni motor de clasificación.

## Requisitos

### Requirement: Tarjetas KPI con variación interanual
**ID**: REQ-ANNUAL-001

El sistema DEBE mostrar 4 tarjetas KPI en una fila superior: Ingresos Total, Gastos Total, Neto (ingresos − gastos) y % Coste Fijo (gastos fijos / gastos totales). Cada tarjeta DEBE incluir un badge YoY con flecha (↑/↓) y porcentaje de variación respecto al año anterior. Cada tarjeta DEBE animar el contador numérico al cargarse.

| Tarjeta | Valor | Badge YoY |
|---------|-------|-----------|
| Ingresos Total | `Summary.IncomeTotal` | `Variation.IncomeTotalPct` |
| Gastos Total | `Summary.ExpenseTotal` | `Variation.ExpenseTotalPct` |
| Neto | `Summary.Net` | `Variation.NetPct` |
| % Coste Fijo | `ExpenseFixed / ExpenseTotal * 100` | Sin badge |

#### Scenario: KPIs con datos del año anterior (CRITICAL)
- **Given** el usuario selecciona un año con datos y el año anterior también tiene datos
- **When** se carga la página
- **Then** cada tarjeta muestra el valor animado y el badge YoY con flecha direccional y porcentaje
- **And** los badges de ingreso/neto usan verde para ↑ y rojo para ↓; los de gasto usan rojo para ↑ y verde para ↓

#### Scenario: KPIs sin datos del año anterior (NORMAL)
- **Given** el año seleccionado tiene datos pero el año anterior no
- **When** se carga la página
- **Then** las tarjetas muestran el valor pero los badges YoY permanecen ocultos

### Requirement: Gráfico de tendencia mensual
**ID**: REQ-ANNUAL-010

El sistema DEBE renderizar un gráfico de líneas (Chart.js) con dos series: ingresos mensuales (verde) y gastos mensuales (rojo). Eje X: meses (Ene–Dic). Eje Y: importe en €. El gráfico DEBE ser responsive y ajustarse al contenedor.

#### Scenario: Tendencia con 12 meses de datos (CRITICAL)
- **Given** el año seleccionado tiene movimientos distribuidos en varios meses
- **When** se carga la página
- **Then** el gráfico muestra dos líneas con los totales mensuales agregados de todos los movimientos
- **And** la leyenda identifica cada serie con color y etiqueta textual

#### Scenario: Tendencia con meses vacíos (NORMAL)
- **Given** algunos meses no tienen movimientos
- **When** se renderiza el gráfico
- **Then** esos meses muestran valor 0 en ambas líneas

### Requirement: Distribución Fijo/Variable
**ID**: REQ-ANNUAL-020

El sistema DEBE mostrar un gráfico (donut o barras apiladas) que desglose ingresos y gastos en fijo vs variable, con etiquetas y porcentajes visibles.

#### Scenario: Distribución con datos mixtos (CRITICAL)
- **Given** existen movimientos clasificados como fijos y variables
- **When** se carga la página
- **Then** el gráfico muestra los segmentos fijo/variable con porcentaje para ingresos y gastos

#### Scenario: Distribución con un solo tipo (NORMAL)
- **Given** todos los movimientos son de un solo tipo (todo fijo o todo variable)
- **When** se carga la página
- **Then** el gráfico muestra un único segmento al 100%

### Requirement: Comparativa interanual (YoY)
**ID**: REQ-ANNUAL-030

El sistema DEBE incluir una sección de comparativa YoY con indicadores visuales (flechas, colores) para la dirección del cambio en métricas clave. Los textos de estado DEBEN resolverse vía recursos de localización (`.resx`/`IViewLocalizer`) según la cultura activa; queda prohibido texto hardcodeado.
(Previously: el mensaje "Sin datos del año anterior" estaba hardcodeado en español)

#### Scenario: Comparativa con año anterior (CRITICAL)
- **Given** existen datos del año anterior
- **When** se carga la página
- **Then** la sección muestra variaciones porcentuales con flechas ↑/↓ y colores direccionales acompañados de texto localizado

#### Scenario: Comparativa sin año anterior (NORMAL)
- **Given** no existen datos del año anterior y cultura activa `es`
- **When** se carga la página
- **Then** la sección muestra el mensaje localizado equivalente a "Sin datos del año anterior"
- **And** con cultura `en` muestra la traducción correspondiente desde `SharedResources`

### Requirement: Tablas de detalle colapsables
**ID**: REQ-ANNUAL-040

El sistema DEBE mostrar tablas de detalle (una para ingresos, otra para gastos) ocultas por defecto, accesibles mediante botón toggle. Cada fila DEBE mostrar: nombre del movimiento, badge de tipo, total anual, media mensual y % YoY si está disponible. Los 12 meses se acceden mediante expansión de fila. El texto del botón toggle DEBE resolverse vía localización; queda prohibido el literal "Ver detalle" hardcodeado.
(Previously: el botón toggle mostraba "Ver detalle" hardcodeado en español)

#### Scenario: Tabla oculta por defecto (CRITICAL)
- **Given** la página se carga con datos y cultura `es`
- **When** el usuario ve la sección de tablas
- **Then** las tablas están colapsadas y solo se muestra el botón con texto localizado (ej. "Ver detalle" en `es`)

#### Scenario: Tabla oculta en inglés
- **Given** cultura activa `en`
- **When** se renderiza el botón toggle
- **Then** el texto proviene de `SharedResources.en.resx` (no del literal español)

#### Scenario: Expansión de fila con datos mensuales (CRITICAL)
- **Given** el usuario expande una fila de la tabla
- **When** hace clic en el toggle de la fila
- **Then** se muestran las 12 columnas mensuales para ese movimiento

#### Scenario: Preservar data-testid para E2E (CRITICAL)
- **Given** los tests E2E existentes usan `data-testid="annual-income-table"` y `data-testid="annual-expense-table"`
- **When** se aplica localización
- **Then** esos atributos se conservan y los E2E no dependen del texto

### Requirement: Selector de año
**ID**: REQ-ANNUAL-060

El sistema DEBE mantener el comportamiento actual del selector de año (form GET submit) y mostrar un estado de carga mientras se cargan los datos.

#### Scenario: Cambio de año (CRITICAL)
- **Given** el usuario está en la página de análisis anual
- **When** selecciona un año diferente
- **Then** el formulario se envía por GET y la página recarga con los datos del nuevo año

### Requirement: Estado vacío
**ID**: REQ-ANNUAL-070

El sistema DEBE preservar el estado vacío cuando no existen datos para el año seleccionado, manteniendo `data-testid="annual-empty-state"`. El mensaje de estado vacío DEBE ser culture-dependent vía localización; queda prohibido el literal "Sin datos para este año" hardcodeado.
(Previously: el texto "Sin datos para este año" estaba hardcodeado en español)

#### Scenario: Año sin datos en español (CRITICAL)
- **Given** el usuario selecciona un año sin movimientos y cultura `es`
- **When** se carga la página
- **Then** se muestra el estado vacío con `data-testid="annual-empty-state"` y el mensaje localizado en español

#### Scenario: Año sin datos en inglés
- **Given** cultura activa `en`
- **When** se carga el estado vacío
- **Then** el mensaje proviene de `SharedResources.en.resx` y los E2E lo verifican vía `data-testid` (no vía texto)

### Requirement: Layout responsive
**ID**: REQ-ANNUAL-080

| Elemento | Desktop (≥992px) | Tablet (≥576px) | Mobile (<576px) |
|----------|-------------------|------------------|------------------|
| KPIs | 4 columnas | 2 columnas | 1 columna |
| Gráficos | Ancho completo | Ancho completo, apilados | Ancho completo, apilados |
| Tablas | Ancho completo | Scroll horizontal | Scroll horizontal |

#### Scenario: Desktop (NORMAL)
- **Given** viewport ≥ 992px
- **When** se carga la página
- **Then** los 4 KPIs se muestran en una fila de 4 columnas

#### Scenario: Mobile (NORMAL)
- **Given** viewport < 576px
- **When** se carga la página
- **Then** los KPIs se apilan en 1 columna y las tablas permiten scroll horizontal

### Requirement: Accesibilidad
**ID**: REQ-ANNUAL-090

El sistema DEBE garantizar que los gráficos tengan `aria-label` descriptivo o tabla fallback. El color NO DEBE ser el único indicador de dirección — flechas y texto acompañan siempre al color. Los botones toggle DEBEN ser navegables por teclado.

#### Scenario: Navegación por teclado (CRITICAL)
- **Given** el usuario navega con teclado (Tab)
- **When** alcanza un botón toggle de tabla
- **Then** puede expandir/colapsar con Enter o Space

#### Scenario: Fallback de gráfico (NORMAL)
- **Given** el usuario usa un lector de pantalla
- **When** llega a un gráfico
- **Then** el gráfico tiene `aria-label` descriptivo o existe una tabla fallback accesible

## Requisitos eliminados

### Requirement: Tablas de 15 columnas como vista principal
(Razón: Reemplazadas por dashboard de KPIs + gráficos como vista principal; las tablas de 15 columnas se mueven a detalle colapsable)
(Migración: Los datos se conservan en tablas colapsables con los mismos data-testid)

### Requirement: Tarjeta Neto en posición independiente centrada
(Razón: Neto se integra en la fila de KPIs superiores para lectura unificada)
(Migración: El valor y badge YoY se conservan en REQ-ANNUAL-001)
