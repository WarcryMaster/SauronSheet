# Tasks: Fase 6 - Pulido UI, Rendimiento y Despliegue Produccion

**Input**: Documentos de diseno en `specs/phase-6/`
**Prerequisitos**: `phase-6-plan.md` (requerido), `phase-6-spec.md` (requerido)

## Formato: `[ID] [P?] [Story] Descripcion`

- `[P]`: tarea paralelizable (archivos distintos, sin dependencia directa)
- `[Story]`: historia de usuario (`[US1]` a `[US7]`)
- Todas las tareas incluyen ruta de archivo explicita

## Phase 1: Setup (Infraestructura Compartida)

**Objetivo**: Preparar base tecnica de Fase 6 y bloqueo de riesgos de plataforma.

- [ ] T001 Registrar decision de plataforma (Vercel/Railway/Render) en `specs/phase-6/phase-6-plan.md`
- [ ] T002 Crear configuracion Tailwind en `src/SauronSheet.Frontend/tailwind.config.js`
- [ ] T003 [P] Crear archivo de entrada Tailwind en `src/SauronSheet.Frontend/tailwind-input.css`
- [ ] T004 [P] Crear carpeta de salida CSS en `src/SauronSheet.Frontend/wwwroot/css/site.css`
- [ ] T005 Actualizar layout para usar CSS compilado y remover CDN en `src/SauronSheet.Frontend/Pages/Shared/_Layout.cshtml`
- [ ] T006 [P] Crear script de build CSS para desarrollo/produccion en `scripts/phase-6/build-tailwind.ps1`
- [ ] T007 [P] Crear script de auditoria Lighthouse base en `scripts/phase-6/run-lighthouse.ps1`

---

## Phase 2: Foundational (Prerequisitos Bloqueantes)

**Objetivo**: Dejar listas las piezas tecnicas compartidas antes de historias de usuario.

**CRITICO**: Ninguna historia inicia sin completar esta fase.

- [ ] T008 Crear componente base de toast en `src/SauronSheet.Frontend/Pages/Shared/Components/_Toast.cshtml`
- [ ] T009 [P] Crear componente de accesibilidad skip link en `src/SauronSheet.Frontend/Pages/Shared/Components/_SkipToContent.cshtml`
- [ ] T010 [P] Crear middleware de cabeceras de seguridad en `src/SauronSheet.Infrastructure/Middleware/SecurityHeadersMiddleware.cs`
- [ ] T011 [P] Crear configuracion Sentry central en `src/SauronSheet.Infrastructure/Monitoring/SentryConfiguration.cs`
- [ ] T012 Actualizar pipeline base de frontend (compression, static files, middleware order) en `src/SauronSheet.Frontend/Program.cs`
- [ ] T013 Crear configuracion productiva inicial en `src/SauronSheet.Frontend/appsettings.Production.json`

**Checkpoint**: Base lista para implementar historias en paralelo.

---

## Phase 3: User Story 1 - UI Pulida y Responsiva (Prioridad: P1) 🎯 MVP

**Goal**: Unificar diseno, responsive y consistencia visual global.

**Independent Test**: T-6.01, T-6.02, T-6.03, T-6.04 en `specs/phase-6/phase-6-spec.md` ejecutados y validados sin depender de otras historias.

### Tests US1

- [ ] T014 [P] [US1] Implementar validacion automatizada de tamano CSS (T-6.14 soporte US1) en `scripts/phase-6/validate-css-size.ps1`
- [ ] T015 [P] [US1] Implementar checklist responsive 320/768/1024 (T-6.02 a T-6.04) en `scripts/phase-6/validate-responsive.ps1`

### Implementacion US1

- [ ] T016 [US1] Definir capas de componentes (`.btn-*`, `.input-*`, `.card`) en `src/SauronSheet.Frontend/tailwind-input.css`
- [ ] T017 [P] [US1] Aplicar clases de botones e inputs en `src/SauronSheet.Frontend/Pages/Transactions/Add.cshtml`
- [ ] T018 [P] [US1] Aplicar clases consistentes en `src/SauronSheet.Frontend/Pages/Categories/Index.cshtml`
- [ ] T019 [P] [US1] Aplicar clases consistentes en `src/SauronSheet.Frontend/Pages/Budgets/Index.cshtml`
- [ ] T020 [P] [US1] Ajustar layout responsive y navegacion en `src/SauronSheet.Frontend/Pages/Shared/_Layout.cshtml`
- [ ] T021 [US1] Ajustar dashboard responsive y consistencia de tarjetas en `src/SauronSheet.Frontend/Pages/Dashboard.cshtml`

**Checkpoint**: US1 funcional y validable de forma independiente.

---

## Phase 4: User Story 2 - Estados de Carga, Error y Vacio (Prioridad: P2)

**Goal**: Feedback claro para carga, fallos y ausencia de datos.

**Independent Test**: T-6.05, T-6.06, T-6.07, T-6.08 validados en paginas de Dashboard, Transactions, Budgets y Categories.

### Tests US2

- [ ] T022 [P] [US2] Crear script de validacion de estados de carga y skeletons (T-6.05) en `scripts/phase-6/validate-loading-states.ps1`
- [ ] T023 [P] [US2] Crear script/checklist de errores y mensajes amigables (T-6.06) en `scripts/phase-6/validate-error-states.ps1`

### Implementacion US2

- [ ] T024 [US2] Integrar componente toast en layout compartido en `src/SauronSheet.Frontend/Pages/Shared/_Layout.cshtml`
- [ ] T025 [P] [US2] Implementar estado vacio para transacciones en `src/SauronSheet.Frontend/Pages/Transactions/Index.cshtml`
- [ ] T026 [P] [US2] Implementar estado vacio para dashboard en `src/SauronSheet.Frontend/Pages/Dashboard.cshtml`
- [ ] T027 [P] [US2] Implementar estado vacio para presupuestos en `src/SauronSheet.Frontend/Pages/Budgets/Index.cshtml`
- [ ] T028 [P] [US2] Implementar estado vacio para categorias en `src/SauronSheet.Frontend/Pages/Categories/Index.cshtml`
- [ ] T029 [US2] Integrar manejo global de errores amigables en `src/SauronSheet.Frontend/Pages/Error.cshtml`

**Checkpoint**: US2 completa sin dependencia funcional de US3-US7.

---

## Phase 5: User Story 3 - Accesibilidad WCAG 2.1 AA (Prioridad: P3)

**Goal**: Cumplimiento base de accesibilidad en navegacion, contraste y semantica.

**Independent Test**: T-6.09 a T-6.13 en Lighthouse/axe-core y verificacion manual de teclado.

### Tests US3

- [ ] T030 [P] [US3] Crear script de auditoria axe-core (T-6.09, T-6.11) en `scripts/phase-6/run-axe-audit.ps1`
- [ ] T031 [P] [US3] Crear script de auditoria Lighthouse accesibilidad (T-6.10, T-6.13) en `scripts/phase-6/run-a11y-lighthouse.ps1`

### Implementacion US3

- [ ] T032 [US3] Integrar skip-to-content y landmarks semanticos en `src/SauronSheet.Frontend/Pages/Shared/_Layout.cshtml`
- [ ] T033 [P] [US3] Corregir labels/aria en formulario de alta de transaccion en `src/SauronSheet.Frontend/Pages/Transactions/Add.cshtml`
- [ ] T034 [P] [US3] Corregir labels/aria en filtros de busqueda en `src/SauronSheet.Frontend/Pages/Transactions/Search.cshtml`
- [ ] T035 [P] [US3] Mejorar alternativas textuales para graficos en `src/SauronSheet.Frontend/Pages/Dashboard.cshtml`
- [ ] T036 [P] [US3] Asegurar foco visible y navegacion teclado en componentes compartidos en `src/SauronSheet.Frontend/tailwind-input.css`
- [ ] T037 [US3] Validar contraste WCAG y estados no basados solo en color en `src/SauronSheet.Frontend/Pages/Shared/_BudgetStatusBadge.cshtml`

**Checkpoint**: US3 aprobada por score de accesibilidad y checks manuales.

---

## Phase 6: User Story 4 - Recuperacion de Contrasena (Prioridad: P4)

**Goal**: Flujo forgot-password completo con Supabase Auth y anti-enumeracion.

**Independent Test**: T-6.25, T-6.26, T-6.27 green con pruebas de handler + validacion UI.

### Tests US4

- [ ] T038 [P] [US4] Crear pruebas de comando RequestPasswordReset (T-6.25, T-6.26) en `tests/SauronSheet.Application.Tests/Features/Auth/Commands/RequestPasswordResetCommandTests.cs`
- [ ] T039 [P] [US4] Crear prueba de validacion de formulario forgot-password (T-6.27) en `tests/SauronSheet.Application.Tests/Features/Auth/Commands/RequestPasswordResetValidationTests.cs`

### Implementacion US4

- [ ] T040 [US4] Agregar comando de recuperacion en `src/SauronSheet.Application/Features/Auth/Commands/RequestPasswordResetCommand.cs`
- [ ] T041 [US4] Implementar handler de recuperacion en `src/SauronSheet.Application/Features/Auth/Commands/RequestPasswordResetCommandHandler.cs`
- [ ] T042 [US4] Extender contrato de auth con password reset en `src/SauronSheet.Domain/Services/IAuthService.cs`
- [ ] T043 [US4] Implementar password reset en Supabase auth service en `src/SauronSheet.Infrastructure/Auth/SupabaseAuthService.cs`
- [ ] T044 [US4] Crear pagina forgot password en `src/SauronSheet.Frontend/Pages/Auth/ForgotPassword.cshtml`
- [ ] T045 [US4] Crear PageModel forgot password y enlace en login en `src/SauronSheet.Frontend/Pages/Auth/ForgotPassword.cshtml.cs`

**Checkpoint**: US4 operativa end-to-end sin afectar flujos de login existentes.

---

## Phase 7: User Story 5 - Optimizacion de Rendimiento (Prioridad: P5)

**Goal**: Cumplir objetivos de TTI, compresion y cacheo.

**Independent Test**: T-6.14, T-6.15, T-6.16, T-6.17 verificados en entorno local/staging.

### Tests US5

- [ ] T046 [P] [US5] Crear script para medir TTI/FCP en perfil Slow 4G (T-6.15) en `scripts/phase-6/measure-tti.ps1`
- [ ] T047 [P] [US5] Crear script para validar compresion y cache headers (T-6.16, T-6.17) en `scripts/phase-6/validate-performance-headers.ps1`

### Implementacion US5

- [ ] T048 [US5] Configurar compresion Brotli/Gzip en `src/SauronSheet.Frontend/Program.cs`
- [ ] T049 [US5] Configurar cacheo de estaticos y no-cache para dinamicos en `src/SauronSheet.Frontend/Program.cs`
- [ ] T050 [P] [US5] Implementar lazy loading de Chart.js en `src/SauronSheet.Frontend/wwwroot/js/charts.js`
- [ ] T051 [P] [US5] Ajustar carga diferida de scripts en layout en `src/SauronSheet.Frontend/Pages/Shared/_Layout.cshtml`
- [ ] T052 [US5] Verificar salida minificada final de CSS en `src/SauronSheet.Frontend/wwwroot/css/site.css`

**Checkpoint**: US5 cumple metricas de rendimiento definidas en SC-6.6 y SC-6.7.

---

## Phase 8: User Story 6 - Despliegue a Produccion (Prioridad: P6)

**Goal**: Publicar app en Vercel (o fallback) con CI/CD y health checks.

**Independent Test**: T-6.20 a T-6.24 ejecutados en URL publica.

### Tests US6

- [ ] T053 [P] [US6] Crear script de smoke test despliegue (health, https, env vars) en `scripts/phase-6/smoke-deploy.ps1`
- [ ] T054 [P] [US6] Crear script de validacion Docker build/run (T-6.21) en `scripts/phase-6/validate-docker.ps1`

### Implementacion US6

- [ ] T055 [US6] Crear Dockerfile multi-stage para frontend en `src/SauronSheet.Frontend/Dockerfile`
- [ ] T056 [US6] Crear configuracion Vercel en `src/SauronSheet.Frontend/vercel.json`
- [ ] T057 [P] [US6] Crear archivo de exclusiones de despliegue en `src/SauronSheet.Frontend/.vercelignore`
- [ ] T058 [US6] Implementar endpoint de salud `/health` en `src/SauronSheet.Frontend/Program.cs`
- [ ] T059 [US6] Configurar variables de entorno de produccion en `src/SauronSheet.Frontend/appsettings.Production.json`
- [ ] T060 [US6] Configurar CORS para dominio de despliegue en `src/SauronSheet.Frontend/Program.cs`

**Checkpoint**: US6 desplegada y observable en entorno productivo.

---

## Phase 9: User Story 7 - Endurecimiento de Seguridad y Privacidad (Prioridad: P7)

**Goal**: Aplicar cabeceras, CSP estricta y monitoreo sin PII financiera.

**Independent Test**: T-6.18 y T-6.19 green; evidencia de filtros Sentry sin PII.

### Tests US7

- [ ] T061 [P] [US7] Crear script de validacion de security headers (T-6.18) en `scripts/phase-6/validate-security-headers.ps1`
- [ ] T062 [P] [US7] Crear script de validacion CSP e intento de inyeccion (T-6.19) en `scripts/phase-6/validate-csp.ps1`

### Implementacion US7

- [ ] T063 [US7] Registrar middleware de seguridad en pipeline HTTP en `src/SauronSheet.Frontend/Program.cs`
- [ ] T064 [US7] Configurar politicas CSP estrictas en `src/SauronSheet.Infrastructure/Middleware/SecurityHeadersMiddleware.cs`
- [ ] T065 [US7] Configurar exclusion de `/health` en tracing Sentry en `src/SauronSheet.Frontend/Program.cs`
- [ ] T066 [US7] Implementar filtrado de PII financiera en Sentry en `src/SauronSheet.Infrastructure/Monitoring/SentryConfiguration.cs`
- [ ] T067 [US7] Remover fuga de cabeceras sensibles (`X-Powered-By`) en `src/SauronSheet.Frontend/Program.cs`

**Checkpoint**: US7 cumple baseline de seguridad y privacidad de Fase 6.

---

## Phase 10: Polish & Cross-Cutting Concerns

**Objetivo**: Cierre integral, regresion y release readiness.

- [ ] T068 [P] Actualizar metadatos, favicon y OG tags en `src/SauronSheet.Frontend/Pages/Shared/_Layout.cshtml`
- [ ] T069 [P] Implementar pagina 404 estilizada en `src/SauronSheet.Frontend/Pages/NotFound.cshtml`
- [ ] T070 [P] Implementar code-behind para NotFound en `src/SauronSheet.Frontend/Pages/NotFound.cshtml.cs`
- [ ] T071 Ejecutar regresion completa de solucion en `SauronSheet.slnx`
- [ ] T072 Ejecutar bateria de scripts de validacion de Fase 6 en `scripts/phase-6/`
- [ ] T073 Registrar resultado final de gates y release readiness en `specs/phase-6/phase-6-plan.md`

---

## Dependencias y Orden de Ejecucion

### Dependencias por Fase

- Setup (Phase 1): inicia inmediatamente
- Foundational (Phase 2): depende de Setup y bloquea todas las historias
- Historias US1-US7 (Phase 3-9): dependen de Foundational; se ejecutan por prioridad P1→P7 o en paralelo por capacidad
- Polish final (Phase 10): depende de US1-US7 completadas

### Dependencias entre Historias

- US1 (P1): base visual para US2 y US3
- US2 (P2): independiente funcionalmente, pero reutiliza layout de US1
- US3 (P3): puede correr en paralelo con US2 tras US1
- US4 (P4): independiente de UI avanzada; requiere base de auth existente
- US5 (P5): depende de setup de assets y cambios en Program.cs
- US6 (P6): depende de US5 y de decision de plataforma T001
- US7 (P7): depende de middleware/sentry base de Phase 2 y Program.cs de US5/US6

### Orden Interno por Historia

- Tests primero (si aplica)
- Componentes/contratos antes de integracion
- Integracion antes de validacion final de historia

---

## Oportunidades de Paralelizacion por Historia

### US1

- En paralelo: T017, T018, T019, T020
- Luego: T021

### US2

- En paralelo: T025, T026, T027, T028
- Luego: T024, T029

### US3

- En paralelo: T033, T034, T035, T036
- Luego: T032, T037

### US4

- En paralelo: T038, T039
- Luego: T040, T041, T042, T043, T044, T045

### US5

- En paralelo: T050, T051
- Luego: T048, T049, T052

### US6

- En paralelo: T053, T054, T057
- Luego: T055, T056, T058, T059, T060

### US7

- En paralelo: T061, T062
- Luego: T063, T064, T065, T066, T067

---

## Estrategia de Implementacion

### MVP Primero (US1)

1. Completar Phase 1 y Phase 2
2. Completar US1 (Phase 3)
3. Validar T-6.01 a T-6.04
4. Congelar baseline visual para el resto de historias

### Entrega Incremental

1. Base tecnica: Setup + Foundational
2. UX base: US1 + US2 + US3
3. Funcionalidad auth: US4
4. Rendimiento y despliegue: US5 + US6
5. Seguridad final: US7
6. Cierre release: Phase 10

### Alcance MVP Sugerido

- MVP recomendado para primera entrega de Fase 6: **US1 (UI pulida y responsiva)**
- Siguiente incremento recomendado: **US2 + US3**
- Release productivo completo: **US1-US7 + Phase 10**

---

## Notas

- Todas las tareas respetan formato checklist requerido.
- Las tareas [P] usan archivos distintos para minimizar conflictos.
- Esta especificacion asume que Phase 0-5 permanece estable y sin regresiones.