# Delta para annual-analysis-dashboard

## MODIFIED Requirements

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
