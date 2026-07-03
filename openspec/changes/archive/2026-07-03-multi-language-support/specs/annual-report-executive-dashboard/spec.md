# Delta para annual-report-executive-dashboard

> Auditoría de `sdd-spec`: esta spec contiene múltiples literales de UI culture-dependent (estados vacíos, mensajes de bloque, Smart Summary) que pasan a depender de la cultura activa conforme a `localization`.

## MODIFIED Requirements

### Requirement: Bloques del dashboard y Smart Summary culture-aware
**ID**: REQ-EXEC-002 (y bloques 003–017 con literales UI)

Los textos de UI del dashboard ejecutivo DEBEN resolverse vía recursos de localización (`.resx`/`IViewLocalizer`/`window.__i18n` según corresponda) según la cultura activa. `Smart Summary` (REQ-EXEC-002, generado por `InsightsService`) DEBE producir texto en la `CultureInfo.CurrentUICulture` con autoría manual de ambas versiones. Quedan prohibidos los literales hardcodeados en español o inglés dentro de `.cshtml`/JS del scope.
(Previously: los literales como "No data", "Single year", "New this year", "No classified", "No anomalies", "No events", "No movements", "No discoveries", "No achievements", "insufficient", "2 years needed", "Need 2+" estaban embebidos en la UI)

#### Scenario: Estado vacío de año en cultura activa
- **Given** año sin movimientos y cultura `es`
- **When** se renderiza el estado vacío
- **Then** `data-testid="annual-empty-state"` muestra el mensaje localizado en español
- **And** con cultura `en` muestra la traducción desde `SharedResources`

#### Scenario: Mensajes por bloque localizados
- **Given** cultura `en` y un bloque sin datos (p.ej. anomalías vacías)
- **When** se renderiza
- **Then** el mensaje proviene de `SharedResources.en.resx` (p.ej. "No anomalies") en lugar de un literal hardcodeado

#### Scenario: Smart Summary bilingüe
- **Given** cultura `es` y datos suficientes
- **When** `InsightsService` genera el resumen
- **Then** produce 2-4 frases en español natural (no traducción literal)
- **And** con cultura `en` produce la versión inglesa autora manualmente

#### Scenario: E2E no depende de texto
- **Given** tests E2E sobre el dashboard
- **When** se aplica localización
- **Then** los selectores usan `data-testid` y no aserciones de texto literal

## ADDED Requirements

### Requirement: Trazas Sentry en inglés
**ID**: REQ-EXEC-LOC-001

Cualquier breadcrumb/mensaje Sentry emitido por `InsightsService` o bloques del dashboard DEBE permanecer en inglés, conforme a `AGENTS.md`, independientemente de la cultura UI activa.

#### Scenario: Sentry ignora cultura UI
- **Given** cultura `es`
- **When** `InsightsService` registra un breadcrumb
- **Then** el mensaje Sentry está en inglés
