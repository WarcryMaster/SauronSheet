![SauronSheet](src/SauronSheet.Frontend/wwwroot/img/sauron-sheet-logo.svg)

# SauronSheet — Control Financiero Personal

> **Aplicación web moderna de gestión de finanzas personales con importación de extractos bancarios, análisis avanzado de gastos y presupuestos inteligentes.**

---

## 📋 Tabla de Contenidos

- [Descripción General](#-descripción-general)
- [Stack Tecnológico](#-stack-tecnológico)
- [Funcionalidades Principales](#-funcionalidades-principales)
- [Capturas de Pantalla](#-capturas-de-pantalla)
- [Arquitectura](#-arquitectura)
- [Estructura del Proyecto](#-estructura-del-proyecto)
- [Metodología de Desarrollo](#-metodología-de-desarrollo)
- [Modelo de Datos](#-modelo-de-datos)
- [Instalación y Ejecución](#-instalación-y-ejecución)
- [Despliegue](#-despliegue)
- [Testing](#-testing)
- [Credenciales de Prueba](#-credenciales-de-prueba)
- [Roadmap](#-roadmap)
- [Enlaces](#-enlaces)

---

## 📖 Descripción General

**SauronSheet** es una aplicación web de finanzas personales diseñada para ayudar a usuarios a tomar el control de sus gastos e ingresos de forma sencilla y visual. El nombre evoca la idea de «un ojo que todo lo ve» sobre tus finanzas — una referencia lúdica a Sauron de _El Señor de los Anillos_ combinada con _Sheet_ (hoja de cálculo).

### ¿Qué problema resuelve?

Hacer un seguimiento de los gastos personales es tedioso. La mayoría de la gente no lo hace porque:

- **Requiere introducir datos manualmente** → SauronSheet importa extractos bancarios automáticamente desde ficheros Excel/PDF.
- **No hay visión global** → Dashboard interactivo con gráficos, tendencias y comparativas anuales.
- **Los presupuestos se olvidan** → Presupuestos por categoría con semáforo visual y detección de sobrecoste.
- **Falta contexto** → Análisis anual completo con score de salud financiera, detección de anomalías y predicciones.

### ¿Para quién es?

- Personas que quieren entender **dónde se va su dinero** sin esfuerzo manual.
- Usuarios de banca online que descargan extractos bancarios y quieren visualizarlos.
- Cualquiera que quiera **establecer presupuestos** y recibir alertas visuales de sobrecoste.

### Diferenciadores

- **Importación inteligente**: parsea extractos bancarios (Excel ING) con resolución automática de categorías mediante el banco.
- **Análisis anual completo**: 7 secciones analíticas: resumen ejecutivo, score de salud financiera, ratios, tendencias mensuales, distribución por categorías, comparativa interanual, detección de anomalías y predicciones.
- **Multi-idioma**: interfaz completa en español e inglés con cambio en vivo.
- **Multi-tenant**: cada usuario ve solo sus datos, aislado por Row-Level Security en Supabase.
- **Presupuestos inteligentes**: semáforo verde/amarillo/rojo/sobrecoste con barras de progreso.
- **Trazabilidad de importación**: cada transacción guarda su origen (banco, fichero, fecha de importación) y método de categorización.

---

## 🛠 Stack Tecnológico

### Backend

| Tecnología | Versión | Propósito |
|---|---|---|
| **.NET** | 10 | Framework web y API — minimal APIs, Razor Pages |
| **C#** | 13 | Lenguaje principal |
| **MediatR** | Última | Patrón CQRS — commands y queries |
| **FluentValidation** | — | Validación de comandos/querys |
| **Sentry** | SDK .NET | Observabilidad: trazabilidad, errores, métricas |
| **ExcelDataReader** | — | Parseo de ficheros Excel (.xls/.xlsx) |

### Frontend

| Tecnología | Versión | Propósito |
|---|---|---|
| **Razor Pages** | .NET 10 | Renderizado server-side, PageModel pattern |
| **MDBootstrap 5** | 9.2 | UI Kit Material Design (CDN) |
| **Alpine.js** | 3.14 | Reactividad declarativa en el DOM |
| **HTMX** | 2.0 | AJAX desde atributos HTML (navegación por años, filtros) |
| **Chart.js** | Última | Gráficos interactivos (tendencias, distribución, evolución) |
| **Flatpickr** | Última | Selector de fechas accesible |
| **Font Awesome** | 6.5 | Iconografía funcional |
| **CSS3** | — | Diseño responsivo, variables CSS personalizadas |

### Base de Datos y Autenticación

| Tecnología | Propósito |
|---|---|
| **Supabase** (PostgreSQL) | Base de datos relacional, autenticación, RLS |
| **Supabase Auth** | Registro, login, JWT, refresh tokens |
| **Row-Level Security** | Aislamiento multi-tenant a nivel de base de datos |
| **Supabase CLI** | Migraciones y gestión del proyecto |

### Infraestructura

| Servicio | Propósito |
|---|---|
| **Azure App Service** | Hosting de la aplicación .NET |
| **Supabase Cloud** | Base de datos y autenticación |
| **Sentry** | Observabilidad unificada: tracing, errores, logs, métricas. Pipeline único (sin Console.WriteLine) |
| **GitHub Actions** | CI/CD: tests unitarios, E2E, migraciones Supabase y deploy a Azure |
| **GitHub** | Repositorio y control de versiones |

---

## ✨ Funcionalidades Principales

### 1. 📄 Importación de Extractos Bancarios
- Subida de ficheros Excel (.xls/.xlsx) con extractos bancarios.
- Parseo automático de movimientos (fecha, descripción, importe, saldo).
- Detección de duplicados por saldo y fecha para evitar transacciones repetidas.
- Resolución automática de categorías basada en la subcategoría del banco.
- Progreso en tiempo real de la importación.

**Pantallas involucradas:** `Transactions/Upload`, `Transactions/Index`

### 2. 📊 Dashboard Principal
- KPIs animados: ingresos totales, gastos totales, neto, número de transacciones.
- Gráfico de gastos por categoría (barras apiladas por mes).
- Gráfico de tendencias mensuales (ingresos vs gastos).
- Comparativa año contra año.
- Transacciones recientes.
- Widget de estado de presupuestos con semáforo y barras de progreso.
- Filtro por periodo: Todo, Este mes, Últimos 3 meses, Este año.

**Pantalla:** `Dashboard`

### 3. 📈 Análisis Anual Completo (7 secciones)

#### Sección 1 — Resumen Ejecutivo (REQ-001)
- Ingresos, gastos, neto, ahorro y tasa de ahorro del año.
- Variación porcentual contra el año anterior.
- Ranking del año entre todos los años disponibles.

#### Sección 2 — Salud Financiera (REQ-002)
- Score global de salud financiera (0-100) con anillo visual.
- Desglose por sub-puntuaciones: ahorro, estabilidad de ingresos, estabilidad de gastos, dependencia de categorías, balance, tendencia.
- Clasificación: Excelente, Buena, Aceptable, Necesita atención.

#### Sección 3 — Ratios Financieros
- Tasa de ahorro, ingreso mensual medio, gasto mensual medio, número de transacciones.
- Porcentaje de costes fijos.

#### Sección 4 — Comparativa Interanual (YoY) (REQ-002)
- Comparativa lado a lado del año actual vs año anterior.
- Variación absoluta y porcentual para ingresos, gastos, neto, ahorro y tasa de ahorro.

#### Sección 5 — Tendencia y Distribución Mensual (REQ-003, REQ-004)
- Gráfico de tendencia mensual (ingresos vs gastos).
- Gráfico de distribución fijo/variable.
- Mejor mes en ingresos y gastos.

#### Sección 6 — Categorías y Comparativa (REQ-005, REQ-006, REQ-007)
- Desglose de gastos por categoría con gráfico donut y tabla de tendencias.
- Comparativa de categorías contra el año anterior.

#### Sección 7 — Anomalías y Predicciones (REQ-008, REQ-016)
- Detección de anomalías estadísticas (Z-score > 3) por categoría.
- Predicciones deterministas usando regresión lineal (R² como confianza).
- Proyecciones de ingresos, gastos, ahorro y balance.

**Pantalla:** `Analysis/Annual`

### 4. 💰 Gestión de Presupuestos
- Creación de presupuestos mensuales por categoría con fecha de inicio/fin.
- Edición de límite, periodo y fechas de vigencia.
- Semáforo visual: Verde (≤75%), Amarillo (75-90%), Rojo (90-100%), Sobrecoste (>100%).
- Barras de progreso con importe gastado vs límite.
- Histórico de presupuestos anteriores.
- Métricas por presupuesto: gastado, límite acumulado, porcentaje usado, restante.

**Pantallas:** `Budgets/Index`, `Budgets/Create`, `Budgets/Edit`, `Budgets/Metrics`, `Budgets/History`, `Budgets/Comparison`

### 5. 🏷️ Gestión de Categorías
- Categorías por defecto del sistema (Comida, Transporte, Servicios, Otros).
- Categorías personalizadas por usuario.
- Subcategorías para clasificación detallada.
- Categorización manual de transacciones no clasificadas.
- Resolución automática desde la subcategoría del banco.

**Pantallas:** `Categories/Index`, `Categories/Subcategories`

### 6. 🔍 Búsqueda y Gestión de Transacciones
- Listado paginado de transacciones con filtros por fecha, categoría, importe y origen.
- Búsqueda por palabra clave en la descripción.
- Añadir transacciones manualmente.
- Editar transacciones (descripción, importe, fecha, categoría).
- Eliminación individual o masiva.
- Vista detalle con toda la información de importación.

**Pantallas:** `Transactions/Index`, `Transactions/Add`, `Transactions/Edit`, `Transactions/Search`

### 7. 👤 Autenticación Multi-Usuario
- Registro e inicio de sesión con email y contraseña (Supabase Auth).
- JWT con refresh tokens almacenados en cookies seguras (HttpOnly, SameSite Strict).
- Aislamiento completo de datos por usuario (RLS en Supabase).
- Cierre de sesión.

**Pantallas:** `Auth/Login`, `Auth/Register`, `Auth/Logout`

### 8. 🌐 Internacionalización (i18n)
- Interfaz completa en español (es-ES) e inglés (en-US).
- Selector de idioma en el menú de navegación.
- Persistencia de la elección mediante cookie.
- Localización de Chart.js, Flatpickr, fechas y moneda.
- Sistema de recursos .resx con `SharedResources`.

### 9. 📱 Diseño Responsivo
- Navegación adaptable: menú colapsable en móvil, offcanvas para navegación móvil.
- Cuadrículas adaptativas (1 columna móvil → 4 columnas escritorio).
- Tablas con scroll horizontal en móvil.
- Layouts específicos: ancho completo, formulario estrecho, formulario estrecho compacto, centrado para auth.

### 10. 🎨 Sistema de Diseño Olive
- **Color primario:** Olive Green `#556B2F` — estabilidad y crecimiento.
- **Canvas:** `#f8f9fa` con tarjetas blancas elevadas (`shadow-sm`).
- **Tipografía:** Sistema sans-serif nativo del SO.
- **Semántica:** verde para ingresos, rojo para gastos.
- **Componentes:** barra de navegación blanca, tarjetas sin borde, botones marca, badges de estado, paneles de filtro.
- **Patrones interactivos:** Alpine.js para reactividad, HTMX para AJAX, Chart.js para gráficos.

---

## 🖼️ Capturas de Pantalla

| Sección | Descripción |
|---|---|
| **Login** | Pantalla de inicio de sesión con diseño centrado y tarjeta de 450px |
| **Dashboard** | KPIs animados, gráficos por categoría y tendencias, estado de presupuestos |
| **Transacciones** | Listado paginado con filtros y búsqueda |
| **Importación** | Subida de extracto bancario con progreso en tiempo real |
| **Presupuestos** | Semáforo visual con barras de progreso por categoría |
| **Análisis Anual** | 7 secciones: resumen, salud financiera, ratios, tendencias, categorías, anomalías, predicciones |
| **Categorías** | Gestión de categorías y subcategorías |

> *(Las capturas de pantalla se añadirán tras el despliegue, cuando la URL esté disponible)*

---

## 🏗️ Arquitectura

### Clean Architecture + CQRS

SauronSheet sigue los principios de **Clean Architecture** (Robert C. Martin) combinados con **CQRS** (Command Query Responsibility Segregation) mediante **MediatR**.

```
┌─────────────────────────────────────────────────────────────┐
│                    FRONTEND (Razor Pages)                    │
│  Pages/ · ViewModels · wwwroot (CSS, JS, images)            │
│  Depende de: Application (solo)                             │
├─────────────────────────────────────────────────────────────┤
│                   APPLICATION (CQRS)                         │
│  Commands / Queries / DTOs / Behaviors / Services           │
│  Depende de: Domain (solo)                                  │
├─────────────────────────────────────────────────────────────┤
│                     DOMAIN (Core)                            │
│  Entities / ValueObjects / Services / Specifications        │
│  Depende de: nada (zero external dependencies)              │
├─────────────────────────────────────────────────────────────┤
│                  INFRASTRUCTURE                              │
│  Persistence (Supabase) · Auth · Excel Parsing · Sentry    │
│  Depende de: Domain (implementa interfaces)                 │
└─────────────────────────────────────────────────────────────┘
```

**Reglas de dependencia:**
- `Domain` → No conoce ninguna otra capa.
- `Application` → Solo conoce `Domain`.
- `Infrastructure` → Implementa interfaces definidas en `Domain`.
- `Frontend` → Solo conoce `Application`.
- Las dependencias apuntan **hacia adentro**: nunca hacia afuera.

### Patrón CQRS

**Commands** (operaciones de escritura):
```
CreateTransactionCommand → CreateTransactionCommandHandler → Transaction
ImportTransactionsCommand → ImportTransactionsCommandHandler → Transaction[]
CreateBudgetCommand → CreateBudgetCommandHandler → Budget
DeleteTransactionCommand → DeleteTransactionCommandHandler
UpdateTransactionCommand → UpdateTransactionCommandHandler
```

**Queries** (operaciones de lectura):
```
GetTransactionsQuery → GetTransactionsQueryHandler → TransactionDto[]
GetSpendingByCategoryQuery → GetSpendingByCategoryQueryHandler → CategorySpending[]
GetAnnualDashboardQuery → GetAnnualDashboardQueryHandler → AnnualDashboardResult
GetBudgetMetricsQuery → GetBudgetMetricsQueryHandler → BudgetMetricsDto[]
```

**Pipeline behaviors** (cross-cutting):
- `TenantScopingBehavior` — Inyecta automáticamente el `UserId` en cada request.
- `SentryTracingBehavior` — Traza cada comando/query en Sentry.

### Domain-Driven Design

**Aggregate Roots:**
| Entidad | ID | Propiedades clave |
|---|---|---|
| `Transaction` | `TransactionId(Guid)` | Amount (Money), Date, Description, CategoryId, Balance |
| `Category` | `CategoryId(Guid)` | Name, Type (Income/Expense), Source (System/User), IsAutoCreated |
| `Budget` | `BudgetId(Guid)` | Limit (Money), Period (BudgetPeriod), Status, Effective dates |
| `Subcategory` | `SubcategoryId(Guid)` | Name (SubcategoryName), CategoryId, Color |
| `ImportBatch` | `Guid` | FileName, UploadedAt, TransactionCount, Status |

**Value Objects inmutables:**
| Value Object | Propósito |
|---|---|
| `Money(decimal, Currency)` | Importe con validación y operaciones aritméticas |
| `DateRange(DateTime, DateTime)` | Rango de fechas para filtros/especificaciones |
| `BudgetPeriod(Year, Month)` | Periodo mensual para presupuestos |
| `TransactionId(Guid)` | ID tipado fuerte para transacciones |
| `UserId(string)` | ID tipado fuerte para usuarios |
| `CategoryName(string)` | Nombre de categoría normalizado |
| `BudgetStatusLevel` | Verde/Amarillo/Rojo/Sobrecoste |

**Domain Services:**
| Servicio | Responsabilidad |
|---|---|
| `CategoryService` | Validación de nombres únicos, categorías por defecto del sistema |
| `BudgetCalculationService` | Cálculo de porcentaje usado, nivel de estado de presupuesto |
| `BudgetService` | Reglas de negocio de presupuestos |

**Specifications (patrón Especificación):**
| Specification | Filtro |
|---|---|
| `TransactionByDateRangeSpecification` | Rango de fechas |
| `TransactionByCategorySpecification` | Categoría específica |
| `TransactionByAmountRangeSpecification` | Rango de importes |
| `TransactionByDescriptionKeywordSpecification` | Palabra clave en descripción |
| `TransactionByUserSpecification` | Usuario (tenant) |
| `CompositeSpecification` | Combinación AND/OR de especificaciones |

### Diseño de Interfaz (Olive Design System)

El diseño sigue un sistema de tokens definido en `DESIGN.md`:

```
Colores:   #556B2F (Olive Green) · #f8f9fa (Canvas) · #ffffff (Cards)
Tipografía: system-ui, -apple-system, Segoe UI, Roboto, sans-serif
Elevación: shadow-none (canvas) · shadow-sm (cards) · shadow (navbar) · shadow-lg (modales)
Bordes:    rounded-3 (0.375rem) en inputs, botones y alerts
           rounded-pill en badges y progress bars
           rounded-circle en avatares
Layouts:   full-width (listas) · narrow form (640px) · narrow tight (560px) · auth centered (450px)
```

**Stack interactivo:**
- **Alpine.js v3** — Reactividad declarativa (`x-data`, `x-show`, `x-model`, `x-transition`)
- **HTMX v2** — AJAX desde atributos HTML (`hx-get`, `hx-target`, `hx-swap`, `hx-trigger`)
- **Chart.js** — Gráficos con colores desde variables CSS
- **Flatpickr** — Selector de fechas con localización española

**Patrones prohibidos ❌:**
- `onclick` / `onchange` / `onsubmit` inline → usar `x-on:` / `@event`
- `DOMContentLoaded` para init → usar `x-init` o HTMX `afterSwap`
- `document.getElementById` en Alpine → usar `$refs`
- `innerHTML` → usar `x-html` o HTMX swap
- `data-bs-*` → usar `data-mdb-*`
- `.btn-primary` / `.btn-outline-primary` (azul Bootstrap) → usar `.btn-brand`

---

## 📁 Estructura del Proyecto

```
SauronSheet/
│
├── src/                                    # Código fuente
│   ├── SauronSheet.Domain/                 # Capa de dominio (core)
│   │   ├── Common/                         # AggregateRoot, Entity, ValueObject, IUserContext
│   │   ├── Entities/                       # Transaction, Category, Budget, Subcategory, ImportBatch
│   │   ├── ValueObjects/                   # Money, UserId, TransactionId, CategoryId, BudgetId, etc.
│   │   ├── Services/                       # CategoryService, BudgetService, IAuthService, IStatementParser
│   │   ├── Specifications/                 # Filtros reutilizables: por fecha, categoría, importe...
│   │   ├── Repositories/                   # Interfaces: ITransactionRepository, ICategoryRepository...
│   │   └── Exceptions/                     # DomainException, EntityNotFoundException
│   │
│   ├── SauronSheet.Application/            # Capa de aplicación (CQRS)
│   │   ├── Common/
│   │   │   └── Behaviors/                  # TenantScopingBehavior
│   │   ├── Features/
│   │   │   ├── Auth/                       # Login, Register, Logout (Commands + Queries)
│   │   │   ├── Transactions/               # CRUD + Import + Search (Commands + Queries)
│   │   │   ├── Categories/                 # CRUD categorías (Commands + Queries)
│   │   │   ├── Subcategories/              # CRUD subcategorías
│   │   │   ├── Budgets/                    # CRUD + Metrics + History + Comparison
│   │   │   └── Analytics/                  # Dashboard + Annual Analysis (14 servicios)
│   │   │       ├── Services/               # 14 servicios analíticos
│   │   │       ├── Classification/         # Clasificador de movimientos fijo/variable
│   │   │       ├── Queries/                # Consultas del dashboard y análisis anual
│   │   │       └── DTOs/                   # 26 DTOs de análisis
│   │   ├── Resources/                      # Archivos .resx (i18n: español, inglés)
│   │   └── Services/                       # BankCategoryResolutionService, ImportProgress
│   │
│   ├── SauronSheet.Infrastructure/         # Infraestructura
│   │   ├── Persistence/                    # Implementaciones Supabase de repositorios
│   │   ├── Auth/                           # SupabaseAuthService, JwtCookieMiddleware
│   │   ├── Excel/                          # IngExcelStatementParser (parseo ING)
│   │   ├── Monitoring/                     # SentryTracingBehavior
│   │   ├── Middleware/                     # GlobalExceptionMiddleware
│   │   ├── Mapping/                        # Extensiones de mapeo
│   │   └── Assets/                         # Iconos permitidos
│   │
│   └── SauronSheet.Frontend/              # Interfaz de usuario (Razor Pages)
│       ├── Pages/                          # Páginas Razor
│       │   ├── Auth/                       # Login, Register, Logout
│       │   ├── Transactions/               # Index (listado), Add, Edit, Upload, Search
│       │   ├── Categories/                 # Index, Subcategories
│       │   ├── Budgets/                    # Index, Create, Edit, Metrics, History, Comparison
│       │   ├── Analysis/                   # Annual
│       │   ├── Dashboard.cshtml            # Dashboard principal
│       │   └── Shared/                     # Layout, componentes parciales
│       ├── wwwroot/
│       │   ├── css/                        # site.css, report-print.css
│       │   ├── js/                         # charts.js, alpine-loader.js
│       │   └── img/                        # Logo SVG + PNG (favicons)
│       ├── Helpers/                        # CategoryBadgeDisplay, TransactionCategoryDisplayHelper
│       └── Services/                       # MemoryImportProgressTracker
│
├── tests/                                  # Tests
│   ├── SauronSheet.Domain.Tests/           # Tests unitarios de dominio
│   │   ├── Entities/                       # TransactionTests, BudgetTests, CategoryTests, etc.
│   │   ├── ValueObjects/                   # Tests de Money, etc.
│   │   ├── Services/                       # Tests de servicios de dominio
│   │   ├── Specifications/                 # Tests de especificaciones
│   │   └── Exceptions/                     # Tests de excepciones
│   ├── SauronSheet.Application.Tests/      # Tests de handlers CQRS
│   ├── SauronSheet.Infrastructure.Tests/    # Tests de infraestructura
│   ├── SauronSheet.Integration.Tests/       # Tests de integración
│   └── SauronSheet.Frontend.Tests/          # Tests de frontend (Razor Pages)
│
├── e2e/                                     # Tests end-to-end (Playwright)
│   ├── tests/                               # 10 specs E2E
│   ├── fixtures/                            # Datos de prueba
│   ├── auth.setup.ts                        # Setup de autenticación para E2E
│   └── playwright.config.ts                 # Configuración de Playwright
│
├── supabase/                                # Base de datos Supabase
│   ├── migrations/                          # 20 migraciones SQL
│   └── config.toml                          # Configuración de Supabase
│
├── docs/                                    # Documentación
│   ├── adr/                                 # Architecture Decision Records
│   └── README.md
│
├── scripts/                                 # Scripts de utilidad
├── openspec/                                # Especificaciones SDD (fase de planificación)
├── .github/                                 # Instrucciones AI y configuración
├── AGENTS.md                                # Instrucciones para asistentes AI
├── DESIGN.md                                # Sistema de diseño completo
├── TODO.md                                  # Tareas pendientes
├── DESIGNDiagram.drawio.png                 # Diagrama de diseño (pendiente)
└── SauronSheet.slnx                         # Solución .NET
```

---

## 🧪 Metodología de Desarrollo

### Spec-Driven Development (SDD)

Cada funcionalidad sigue un flujo estructurado en fases:

```
Proposal → Spec → Design → Tasks → Apply → Verify → Archive
```

1. **Exploración** — Investigación del código y requisitos.
2. **Propuesta** — Definición de alcance, enfoque y tradeoffs.
3. **Especificación** — Requisitos detallados y escenarios de aceptación.
4. **Diseño técnico** — Arquitectura, patrones, decisiones técnicas.
5. **Tareas** — Desglose en tareas atómicas implementables.
6. **Implementación** — Codificación siguiendo TDD estricto.
7. **Verificación** — Tests, revisión, validación contra la especificación.
8. **Archivo** — Cierre de la fase y registro de decisiones.

### TDD (Test-Driven Development)

- Las fases de dominio exigen **100% de cobertura** obligatoria.
- Cobertura global mínima: **80% Domain**, **70% Application**.
- Flujo: Test Rojo → Implementación → Test Verde → Refactor.

### 🤖 Asistencia con IA (Gentle AI)

Todo el desarrollo se ha realizado con **Gentle AI**, un asistente de codificación basado en `opencode` con el modelo `deepseek-v4-flash-free`, siguiendo el flujo SDD de forma estructurada. El ecosistema de herramientas incluye:

| Herramienta | Propósito | Ahorro |
|---|---|---|
| **Headroom** | Compresión de contexto en ventana | Reduce el uso de tokens hasta un 90% comprimiendo respuestas largas, logs y resultados de búsqueda antes de razonar sobre ellos |
| **Serena** | Análisis semántico de código con LSP | Navegación precisa de símbolos (clases, métodos, interfaces) sin depender de grep — búsquedas por nombre, declaraciones, implementaciones y referencias |
| **RTK (Rust Token Killer)** | Compresión de comandos de terminal | Reduce 60–99% el consumo de tokens en comandos `git`, `dotnet test`, `dotnet build`, `grep`, `find` y otros, filtrando salida irrelevante sin alterar el comportamiento |
| **Engram** | Memoria persistente entre sesiones | Guarda decisiones, bugs, descubrimientos y configuraciones para que cada sesión retome sin perder contexto |

El flujo de trabajo típico con estas herramientas:

1. **Spec-Driven Development (SDD)** con Gentle AI orquestando las fases.
2. **Exploración** del código mediante Serena (símbolos) y Headroom (contexto comprimido).
3. **Implementación** con comandos prefijados por RTK para minimizar el consumo de tokens.
4. **Verificación** continua con `dotnet test` y Playwright.
5. **Memoria persistente** vía Engram para mantener decisiones entre sesiones.

Este enfoque ha permitido desarrollar **910 tests** (unidad, integración, frontend, infraestructura) y **10 specs E2E** con un consumo eficiente de tokens, manteniendo trazabilidad completa de cada decisión arquitectónica.

### Testing Pyramid

```
        ╱╲
       ╱ E2E ╲           ← Playwright (browser automation)
      ╱────────╲
     ╱ Integration ╲     ← xUnit + Moq + in-memory doubles
    ╱────────────────╲
   ╱   Unit (Domain)   ╲  ← xUnit (entidades, VO, servicios)
  ╱──────────────────────╲
```

### Principios de Desarrollo

| Principio | Aplicación |
|---|---|
| **Clean Architecture** | 4 capas con dependencias hacia adentro |
| **CQRS** | Commands para escritura, Queries para lectura |
| **DDD** | Aggregate Roots, Value Objects, Domain Services |
| **TDD** | Tests antes de implementación |
| **E2E real** | Tests E2E simulan interacción real de usuario (clics, formularios) |
| **Sin var** | Tipos explícitos siempre (nunca `var`) |
| **Sentry first** | Toda observabilidad por Sentry, nunca `Console.WriteLine` |
| **CDN only** | Librerías frontend exclusivamente por CDN |
| **Inglés en código** | Código, identificadores, commits en inglés |
| **Español en especs** | Documentación, especificaciones, planificación en español |

---

## 🗄️ Modelo de Datos

### Esquema en Supabase (PostgreSQL)

```
auth.users                          → Gestión de usuarios (Supabase Auth)
  │
  ├── user_profiles                  → Perfiles de usuario extendidos
  │
  ├── categories                     → Categorías de gastos/ingresos
  │     ├── name, type (Income|Expense), source (System|User)
  │     ├── is_system_default, is_auto_created
  │     └── Restricción: única por nombre normalizado + usuario + tipo
  │
  ├── subcategories                  → Subcategorías (ej: "Supermercado" dentro de "Comida")
  │
  ├── bank_category_translations     → Traducción banco → categoría SauronSheet
  │
  ├── transactions                   → Transacciones financieras
  │     ├── user_id, date, description, amount (signado)
  │     ├── category_id, subcategory_id, category_source
  │     ├── bank_category, bank_subcategory
  │     ├── balance (para detección de duplicados)
  │     └── imported_from, import_batch_id
  │
  ├── budgets                        → Presupuestos mensuales
  │     ├── user_id, category_id, limit_amount
  │     ├── period (YYYY-MM), status (Active/Inactive)
  │     ├── effective_from, effective_until
  │     └── Restricción: único por usuario + categoría + periodo + fechas
  │
  └── import_batches                 → Lotes de importación
        ├── file_name, status, transaction_count
        └── errors (JSON array con errores por fila)
```

### Row-Level Security (RLS)

Todas las tablas tienen RLS habilitado con políticas que filtran por `user_id = auth.uid()`. El cliente Supabase se inicializa por request con el JWT del usuario, garantizando aislamiento total entre inquilinos.

### Migraciones

20 migraciones SQL versionadas en `supabase/migrations/`, aplicadas mediante `supabase db push`.

---

## 🚀 Instalación y Ejecución

### Requisitos Previos

- **.NET 10 SDK** o superior → [Descargar](https://dotnet.microsoft.com/download/dotnet/10.0)
- **Node.js 18+** (opcional, solo para tests E2E) → [Descargar](https://nodejs.org/)
- **Cuenta Supabase** (gratuita) → [Supabase](https://supabase.com)
- **Git** → [Descargar](https://git-scm.com/)

### Instalación Local

#### 1. Clonar el repositorio

```bash
git clone https://github.com/tuusuario/SauronSheet.git
cd SauronSheet
```

#### 2. Configurar Supabase

1. Crear un proyecto en [Supabase](https://supabase.com) (plan gratuito).
2. Copiar la URL del proyecto y las claves (anon key, jwt secret).
3. Aplicar las migraciones:

```bash
supabase link --project-ref <tu-project-ref>
supabase db push --linked
```

4. Configurar autenticación en Supabase Dashboard:
   - `Authentication → Settings → Sites URLs`: Añadir `http://localhost:54100`
   - `Authentication → Providers → Email`: Habilitar email/password

#### 3. Configurar appsettings.json

Crear o modificar `src/SauronSheet.Frontend/appsettings.json`:

```json
{
  "Supabase": {
    "Url": "https://tu-proyecto.supabase.co",
    "Key": "tu-anon-key",
    "JwtSecret": "tu-jwt-secret"
  },
  "Auth": {
    "AccessTokenCookieName": "sb-access-token",
    "RefreshTokenCookieName": "sb-refresh-token",
    "AccessTokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7
  },
  "Sentry": {
    "Dsn": ""  ← Opcional, dejar vacío si no se usa Sentry
  }
}
```

#### 4. Compilar y ejecutar

```bash
# Restaurar paquetes y compilar
dotnet restore
dotnet build

# Ejecutar la aplicación
dotnet run --project src/SauronSheet.Frontend/

# O en modo watch (recarga automática)
dotnet watch run --project src/SauronSheet.Frontend/
```

La aplicación estará disponible en:
- **HTTP:** `http://localhost:54100`
- **HTTPS:** `https://localhost:7000` (si está configurado)

#### 5. Registrar un usuario

1. Navegar a `http://localhost:54100`
2. Ir a `/Auth/Register`
3. Crear una cuenta con email y contraseña
4. Confirmar el email en Supabase Auth dashboard (modo desarrollo) o a través del enlace de confirmación

---

## 🌐 Despliegue

### Producción (Azure App Service)

La aplicación está desplegada en **Azure App Service** (plan gratuito):

**URL de producción:** [https://sauronsheet-akd4gkewdpbtbgea.spaincentral-01.azurewebsites.net](https://sauronsheet-akd4gkewdpbtbgea.spaincentral-01.azurewebsites.net)

El pipeline de CI/CD está configurado con **GitHub Actions** (`.github/workflows/master_sauronsheet.yml`) y se dispara automáticamente al hacer push a ramas con el patrón `RELEASE/Sauron.Sheet/*`. También se puede ejecutar manualmente desde el portal de GitHub.

**Flujo del pipeline:**

1. **Build & Test** — `dotnet restore` → `dotnet build --configuration Release` → `dotnet test` (910 tests)
2. **E2E Tests** — Instala Playwright Chromium → ejecuta 10 specs E2E con credenciales de test
3. **Migraciones Supabase** — `supabase link` → `supabase db push --linked` (aplica migraciones antes del deploy)
4. **Publish** — `dotnet publish` con limpieza de archivos `.map` y `.pdb`
5. **Deploy a Azure** — Login con OIDC → `azure/webapps-deploy` a App Service

**Rollback:** Revertir el commit en la rama `RELEASE/Sauron.Sheet/*` y pushear de nuevo — GitHub Actions redeploya la versión anterior automáticamente.

### Variables de Entorno en Producción

```
Supabase__Url=https://tu-proyecto.supabase.co
Supabase__Key=tu-anon-key
Supabase__JwtSecret=tu-jwt-secret
Sentry__Dsn=tu-sentry-dsn (opcional)
Auth__AccessTokenExpirationMinutes=60
Auth__RefreshTokenExpirationDays=7
```

### Configuración de Supabase para Producción

1. **URL del sitio:** Añadir la URL de Azure en `Authentication → Settings → Site URL`.
2. **Redirect URLs:** Añadir `https://tudominio.azurewebsites.net/**`.
3. **CORS:** Configurar los orígenes permitidos.

---

## 🧪 Testing

### Tests Unitarios (Dominio)

```bash
# Todos los tests
dotnet test

# Tests de dominio (entidades, value objects, servicios)
dotnet test --filter "Category=Domain"

# Tests de aplicación (handlers CQRS)
dotnet test --filter "Category=Application"
```

### Tests E2E (Playwright)

**¿Por qué Playwright?**

| Ventajas | Detalle |
|---|---|
| **Multi-navegador** | Chromium, Firefox, WebKit, Mobile Chrome/Safari con una misma API |
| **Velocidad** | Ejecución paralela, auto-wait inteligente, sin `sleep()` arbitrarios |
| **Selectores robustos** | Soporte nativo para `data-testid` — los tests no se rompen al cambiar idioma (ES/EN) |
| **Debugging** | Trace viewer con timeline, screenshots automáticos en fallo, video en CI |
| **CI/CD nativo** | Integración directa con GitHub Actions, reportes HTML/JUnit |

**Arquitectura de tests:**

```
e2e/
├── auth.setup.ts              # Login global una vez → storageState compartido
├── fixtures/                  # Datos de prueba y helpers de autenticación
│   └── budget-data.fixture.ts
├── helpers.ts                 # Utilidades reutilizables
├── playwright.config.ts       # Configuración: webServer, projects, reporters
└── tests/                     # 9 specs E2E (~60 tests)
    ├── 00-culture.spec.ts
    ├── 01-login.spec.ts
    ├── 02-upload-excel.spec.ts
    ├── 03-budgets.spec.ts
    ├── 03-edit-transaction.spec.ts
    ├── 04-budget-management.spec.ts
    ├── 05-categories-lifecycle.spec.ts
    ├── 07-annual-analysis.spec.ts
    └── 08-import-system-categories-i18n.spec.ts
```

**Estrategia de autenticación:**

El `auth.setup.ts` hace login **una vez** al inicio y guarda las cookies en `.auth/user.json`. Todos los tests reutilizan ese estado vía `storageState`, ahorrando ~3-4s por archivo de test. El test `01-login.spec.ts` limpia cookies explícitamente para probar el flujo de login desde cero.

**Selectores independientes del idioma:**

Todos los elementos interactivos tienen `data-testid` para que los tests funcionen tanto en español como en inglés:

```html
<!-- En la plantilla Razor -->
<input data-testid="login-email" ... />
<button data-testid="login-submit">Sign in</button>

<!-- En el test (inmune al idioma) -->
await page.fill('[data-testid="login-email"]', 'test@example.com');
await page.locator('[data-testid="login-submit"]').click();
```

**Flujos de usuario cubiertos:**

| Spec | Escenarios |
|---|---|
| `00-culture` | Cambio de idioma ES↔EN, persistencia en cookie, `<html lang>` |
| `01-login` | Login válido/inválido, validación de campos, registro, logout |
| `02-upload-excel` | Subida de extracto ING, progreso, detección de duplicados |
| `03-budgets` | Creación de presupuesto, visualización, semáforo |
| `03-edit-transaction` | Edición de descripción, importe, categoría |
| `04-budget-management` | CRUD completo de presupuestos, filtros, histórico |
| `05-categories-lifecycle` | Crear/editar/eliminar categorías, subcategorías |
| `07-annual-analysis` | Navegación por años, 7 secciones del análisis |
| `08-import-system-categories-i18n` | Importación + categorías del sistema + multi-idioma |

**Integración con CI/CD:**

Los tests E2E se ejecutan automáticamente en el pipeline de GitHub Actions **antes del deploy a Azure**. Si fallan, el deploy se bloquea.

```bash
# En CI: auto-arranca la app, instala Playwright, ejecuta tests
npx playwright test --config=e2e/playwright.config.ts --project=chromium
```

**Comandos locales:**

```bash
# Instalar navegadores (primera vez)
npx playwright install chromium

# Ejecutar todos los tests E2E (auto-arranca la app en localhost:54100)
npx playwright test --config=e2e/playwright.config.ts --project=chromium

# Test específico
npx playwright test --config=e2e/playwright.config.ts e2e/tests/03-budgets.spec.ts

# Con navegador visible (debug)
npx playwright test --config=e2e/playwright.config.ts --headed

# Modo debug (inspector paso a paso)
npx playwright test --config=e2e/playwright.config.ts --debug

# Ver reporte HTML de la última ejecución
npx playwright show-report
```

### Credenciales de Test E2E

Los tests E2E usan un usuario semilla en Supabase:

- **Email:** `e2e@saurontest.local`
- **Contraseña:** Configurada en el entorno o en fixtures

También se pueden usar variables de entorno:

```bash
$env:TEST_USER_EMAIL = "tu@email.com"
$env:TEST_USER_PASSWORD = "tu-contraseña"
```

---

## 👤 Credenciales de Prueba

La aplicación tiene un usuario de prueba preconfigurado para evaluar todas las funcionalidades:

| Campo | Valor |
|---|---|
| **Email** | `demo@sauronsheet.app` |
| **Contraseña** | `Demo1234!` |

> **Nota:** Este usuario tiene datos de ejemplo cargados (transacciones importadas, presupuestos activos, análisis anual con datos históricos) para que puedas explorar todas las funcionalidades sin necesidad de importar tus propios datos.

Si prefieres crear tu propio usuario:
1. Ve a `/auth/register`
2. Completa el formulario con tu email y contraseña
3. Confirma el email si es necesario
4. Comienza importando un extracto bancario en `/transactions/upload`

---

## 🗺️ Roadmap

### Fases Completadas ✅

| Fase | Descripción |
|---|---|
| **Fase 0** | Fundación: estructura Clean Architecture, solución .NET, configuración de proyecto |
| **Fase 1** | Autenticación multi-usuario: Supabase Auth, JWT, cookies seguras, registro/login/logout |
| **Fase 2** | Modelo de datos: entidades Transaction, Category, Subcategory, migraciones, repositorios |
| **Fase 3** | Importación de extractos: parseo Excel ING, resolución de categorías, detección de duplicados |
| **Fase 4** | Dashboard: KPIs, gráficos Chart.js, tendencias, transacciones recientes, filtros por periodo |
| **Fase 5** | Presupuestos: CRUD completo, semáforo, barras de progreso, histórico, métricas, comparativas |
| **Fase 6** | Análisis anual completo: 7 secciones, salud financiera, anomalías, predicciones |
| **Fase 7** | UI/UX: diseño responsivo, sistema de diseño Olive, Alpine.js, HTMX, multi-idioma |
| **Fase 8** | Testing: cobertura >80% dominio, >70% aplicación, 10 specs E2E con Playwright |

### Próximos Pasos 🔜

| Funcionalidad | Prioridad |
|---|---|
| Multi-idioma completo (normalizar detección y URLs) | Alta |
| Exportación a PDF/CSV de informes | Media |
| Alertas push de presupuestos (notificaciones) | Media |
| Importación multi-banco (CaixaBank, Santander, BBVA) | Media |
| Modo oscuro | Baja |
| Categorización automática con ML (basada en histórico) | Baja |
| Presupuestos compartidos (gastos de grupo/pareja) | Baja |

---

## 🔗 Enlaces

| Recurso | URL |
|---|---|
| **Repositorio GitHub** | [https://github.com/tuusuario/SauronSheet](https://github.com/tuusuario/SauronSheet) |
| **Despliegue (Azure)** | [https://sauronsheet-akd4gkewdpbtbgea.spaincentral-01.azurewebsites.net](https://sauronsheet-akd4gkewdpbtbgea.spaincentral-01.azurewebsites.net) |
| **Presentación (Slides)** | [URL de Google Slides / PowerPoint / Canva] |
| **Vídeo explicativo** | [URL de YouTube / Google Drive] |
| **Documentación técnica** | `docs/adr/` (Architecture Decision Records) |
| **Sistema de diseño** | `DESIGN.md` |
| **Especificaciones SDD** | `openspec/` |

---

## 📄 Licencia

Este proyecto es parte del Trabajo de Fin de Máster del **Máster de Programación desde Cero** de [MoureDev](https://mouredev.com).

Todos los derechos reservados — Proyecto educativo.

---

## 🙏 Agradecimientos

- **Brais Moure (MoureDev)** — Por el máster y la inspiración.
- **Comunidad MoureDev** — Por el apoyo y feedback durante el desarrollo.
- **Supabase** — Plataforma de base de datos y autenticación gratuita.
- **Azure** — Hosting gratuito para estudiantes.

---

> *"Un ojo que todo lo ve sobre tus finanzas."* — SauronSheet 🧙‍♂️💰
