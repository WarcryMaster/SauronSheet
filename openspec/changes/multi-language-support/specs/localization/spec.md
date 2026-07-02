# localization — Especificación (Nueva)

## Propósito

Infraestructura de localización ES/EN para SauronSheet: conmutación y persistencia de cultura por usuario, recursos `.resx` para UI estática, diccionario JSON para JavaScript, locale dinámico de Flatpickr/Chart.js y patrón culture-aware para servicios que generan texto. MVP sin cultura en la URL; sin traducción de contenido de usuario; sin tabla `translations` en Supabase.

## Requisitos

### Requirement: Culturas soportadas y fallback
**ID**: REQ-LOC-001

El sistema DEBE soportar exactamente dos culturas UI: `es` (es-ES) y `en` (en-US). `en` DEBE ser el fallback por defecto cuando no exista cookie de cultura ni `Accept-Language` coincidente. El sistema NO DEBE soportar cultura en la URL.

#### Scenario: Sin cookie ni Accept-Language
- **Given** un request sin cookie de cultura y `Accept-Language: zh-CN`
- **When** el pipeline de localización resuelve la cultura
- **Then** la cultura UI resulta `en-US`

#### Scenario: Accept-Language español
- **Given** request sin cookie y `Accept-Language: es-ES,es;q=0.9`
- **When** se resuelve la cultura
- **Then** la cultura UI resulta `es-ES`

### Requirement: Providers de cultura (Cookie + QueryString + Accept-Language)
**ID**: REQ-LOC-010

El sistema DEBE encadenar tres providers en este orden: Cookie (principal), QueryString, Accept-Language (fallback). La cookie de cultura DEBE llamarse `.AspNetCore.Culture` (o equivalente estándar), durar 1 año, y ser legible en cada request.

#### Scenario: Cookie presente gana sobre Accept-Language
- **Given** cookie de cultura=`es` y `Accept-Language: en`
- **When** se resuelve la cultura
- **Then** resulta `es-ES` (cookie vence a Accept-Language)

#### Scenario: QueryString override
- **Given** cookie=`en` y query `?culture=es`
- **When** se resuelve la cultura del request
- **Then** resulta `es-ES` (QueryString vence a cookie)

### Requirement: Language switcher en navbar
**ID**: REQ-LOC-020

El sistema DEBE exponer un switcher en la navbar (icono globo + dropdown ES/EN) que persista la cultura elegida en la cookie por 1 año y recargue la página actual en la nueva cultura.

#### Scenario: Conmutar ES→EN
- **Given** usuario con cultura `es` activa
- **When** selecciona "English" en el switcher
- **Then** se setea cookie de cultura=`en` (1 año) y la página recarga renderizando en inglés

#### Scenario: Persistencia entre sesiones
- **Given** usuario cerró sesión y vuelve al día siguiente con cookie=`es`
- **When** carga cualquier página
- **Then** la UI renderiza en español

### Requirement: Recursos .resx con IStringLocalizer/IViewLocalizer
**ID**: REQ-LOC-030

El sistema DEBE usar `SharedResources` + archivos `.resx` (`SharedResources.es.resx`, `SharedResources.en.resx`) consumidos vía `IViewLocalizer` en `.cshtml` y `IStringLocalizer<T>` en servicios. Toda cadena de UI estática DEBE migrarse a claves de recurso; queda prohibido el texto hardcodeado en `.cshtml` del scope.

#### Scenario: Página traducida (Login)
- **Given** cultura activa `es`
- **When** se renderiza la página Login
- **Then** todos los labels y botones provienen de `SharedResources.es.resx`

#### Scenario: Clave ausente → fallback inglés
- **Given** cultura `es` y una clave sin traducción en `.es.resx`
- **When** se renderiza
- **Then** se muestra el valor de `.en.resx` (fallback)

### Requirement: Diccionario JSON para JavaScript
**ID**: REQ-LOC-040

El sistema DEBE inyectar `window.__i18n` (objeto JSON de claves para la cultura activa) en `_Layout.cshtml`. `charts.js` y cualquier JS del scope DEBEN consumir labels desde `window.__i18n`; queda prohibido texto hardcodeado en JS.

#### Scenario: Chart.js usa i18n
- **Given** cultura `es` y `window.__i18n.legend.income = "Ingresos"`
- **When** `charts.js` renderiza la leyenda
- **Then** la etiqueta muestra "Ingresos"

#### Scenario: Drift de claves
- **Given** una clave usada en JS no existe en `window.__i18n`
- **When** se renderiza
- **Then** se registra Sentry breadcrumb (inglés) y se muestra la clave cruda

### Requirement: Locale dinámico de Flatpickr y Chart.js
**ID**: REQ-LOC-050

El sistema DEBE cargar el locale de Flatpickr (`es`/`en`) según la cultura activa y DEBE configurar Chart.js con labels localizados desde `window.__i18n`.

#### Scenario: Flatpickr en español
- **Given** cultura `es`
- **When** se inicializa Flatpickr
- **Then** el calendario muestra meses/días en español

### Requirement: Servicios culture-aware
**ID**: REQ-LOC-060

`InsightsService` y `AnomalyDetectionService` DEBEN generar texto de salida en la `CultureInfo.CurrentUICulture` activa, con autoría manual de ambas versiones (no auto-traducción). Las trazas de Sentry DEBEN permanecer en inglés.

#### Scenario: Insights en español
- **Given** cultura `es` y datos suficientes
- **When** `InsightsService` genera el resumen
- **Then** produce frases en español natural (no traducción literal)

#### Scenario: Sentry en inglés
- **Given** cultura `es`
- **When** `InsightsService` captura un breadcrumb
- **Then** el mensaje Sentry está en inglés

### Requirement: `<html lang>` dinámico
**ID**: REQ-LOC-070

`_Layout.cshtml` DEBE emitir `<html lang="es|en">` según la cultura activa.

#### Scenario: lang coincide con cultura
- **Given** cultura `en`
- **When** se renderiza `_Layout`
- **Then** el tag es `<html lang="en">`

### Requirement: E2E con data-testid y test de cultura
**ID**: REQ-LOC-080

Los E2E DEBEN usar selectores `data-testid` (no texto) y DEBE existir un test que conmute ES/EN y verifique persistencia.

#### Scenario: Test de conmutación
- **Given** usuario en cultura `en`
- **When** conmuta a `es` y recarga
- **Then** el test verifica `data-testid="lang-switcher"` y persistencia vía cookie
