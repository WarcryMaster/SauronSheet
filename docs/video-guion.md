# Guión del Vídeo — SauronSheet TFM

**Duración estimada:** 7-8 minutos
**Formato:** Screencast (grabación de pantalla) + voz en off
**Idioma:** Español neutro
**URL de la app:** https://sauronsheet-akd4gkewdpbtbgea.spaincentral-01.azurewebsites.net
**Credenciales demo:** `demo@sauronsheet.app` / `Demo1234!`

---

## Estructura del vídeo

| Bloque | Duración | Tiempo acumulado |
|--------|----------|------------------|
| 1. Introducción | 1 min | 0:00 – 1:00 |
| 2. El problema | 1 min | 1:00 – 2:00 |
| 3. Arquitectura y stack | 1 min 30 s | 2:00 – 3:30 |
| 4. Demo de funcionalidades | 3 min | 3:30 – 6:30 |
| 5. Metodología y testing | 1 min | 6:30 – 7:30 |
| 6. Cierre | 30 s | 7:30 – 8:00 |

---

## Guión técnico

### BLOQUE 1 — Introducción (0:00 – 1:00)

| Visual | Audio (voz en off) |
|--------|-------------------|
| **Slide de título** del PowerPoint (SauronSheet — logo, nombre, subtítulo "Control Financiero Personal"). Fundido a negro. | "Hola a todos. Soy [TU NOMBRE] y este es el vídeo de presentación de SauronSheet, mi Trabajo de Fin de Máster del Máster de Programación desde Cero de MoureDev." |
| Transición a **slide de agenda** del PowerPoint. | "SauronSheet es una aplicación web de gestión de finanzas personales que permite importar extractos bancarios, insertary gestionar datos manualmente, categorizar gastos, visualizar gastos con gráficos interactivos, establecer presupuestos inteligentes y obtener un análisis anual completo de tu salud financiera." |
| Fundido a **pantalla de la app desplegada** (página de login, sin estar logueado). La cámara hace un leve zoom al logo. | "En este vídeo os voy a contar qué problema resuelve, cómo está construido técnicamente, y os haré una demo de las funcionalidades principales." |

---

### BLOQUE 2 — El problema (1:00 – 2:00)

| Visual | Audio (voz en off) |
|--------|-------------------|
| **Slide "El Problema"** del PowerPoint la primera mitad. | "Hacer un seguimiento de los gastos personales es tedioso. La mayoría de la gente no lo hace por cuatro razones principales." |
| Siguen apareciendo bullets en la slide uno por uno. | "Primero, requiere introducir datos manualmente, lo que es aburido y propenso a errores. Segundo, no hay una visión global de ingresos versus gastos. Tercero, los presupuestos se olvidan o no se respetan porque no hay alertas visuales. Y cuarto, falta contexto histórico: no hay análisis de tendencias ni predicciones." |
| **Slide "La Solución"** del PowerPoint. | "SauronSheet resuelve estos problemas. Importa automáticamente extractos bancarios desde ficheros Excel, muestra un dashboard interactivo con gráficos y tendencias, permite crear presupuestos con semáforo visual, y ofrece un análisis anual completo con siete secciones que incluyen scoring de salud financiera, detección de anomalías y predicciones basadas en regresión lineal." |
| Subtítulo en la slide: "Un ojo que todo lo ve sobre tus finanzas". Pausa breve. | "El nombre SauronSheet es un juego de palabras entre Sauron, el ojo que todo lo ve del Señor de los Anillos, y Sheet, hoja de cálculo." |

---

### BLOQUE 3 — Arquitectura y stack (2:00 – 3:30)

| Visual | Audio (voz en off) |
|--------|-------------------|
| **Slide "Stack tecnológico"** del PowerPoint (Backend). | "Técnicamente, SauronSheet está construido con .NET 10 y C# 13 en el backend, usando el patrón CQRS con MediatR para separar commands de queries. Como base de datos usa Supabase, que es PostgreSQL gestionado con autenticación integrada y Row-Level Security para aislar los datos de cada usuario." |
| **Slide "Stack Frontend"** del PowerPoint. | "En el frontend, usa Razor Pages con renderizado server-side, MDBootstrap para la interfaz, Alpine.js para reactividad, HTMX para peticiones AJAX, y Chart.js para los gráficos interactivos." |
| **Slide "Clean Architecture"** del PowerPoint (diagrama de capas). | "La arquitectura sigue los principios de Clean Architecture de Robert C. Martin, con cuatro capas cuyas dependencias apuntan siempre hacia adentro. El Dominio no conoce ninguna otra capa. La Aplicación solo conoce el Dominio. La Infraestructura implementa interfaces del Dominio. Y el Frontend solo conoce la Aplicación." |
| **Slide "Domain-Driven Design"** del PowerPoint. | "Dentro del Dominio se aplican los patrones de Domain-Driven Design: aggregate roots como Transaction, Budget y Category, value objects inmutables como Money y UserId, domain services como BudgetCalculationService, y el patrón Specification para filtros reutilizables." |
| Transición a la pantalla de la app. | "Todo esto se despliega automáticamente en Azure App Service mediante un pipeline de CI/CD con GitHub Actions." |

---

### BLOQUE 4 — Demo de funcionalidades (3:30 – 6:30)

| Visual | Audio (voz en off) |
|--------|-------------------|
| **Pantalla: página de login** de la app desplegada en Azure. Escribir email `demo@sauronsheet.app` y contraseña `Demo1234!`. Click en "Iniciar sesión". | "Vamos a ver la aplicación en funcionamiento. Inicio sesión con el usuario de demostración, que ya tiene datos cargados de los últimos dos años y medio." |
| **Pantalla: Dashboard**. Esperar a que carguen los KPIs animados y los gráficos. Recorrer visualmente de arriba a abajo. | "Lo primero que vemos es el Dashboard. Arriba, cuatro KPIs animados: ingresos totales, gastos totales, neto y número de transacciones. Estos datos corresponden a todo el histórico del usuario demo, que va desde enero de 2024 hasta julio de 2026." |
| **Pantalla: Dashboard — gráfico de gastos por categoría** (barras apiladas). | "El primer gráfico muestra los gastos por categoría agrupados por mes en barras apiladas. Podemos ver cómo se distribuyen los gastos en alimentación, transporte, ocio, compras, salud y servicios." |
| **Pantalla: Dashboard — gráfico de tendencias mensuales**. | "El segundo gráfico muestra la tendencia mensual de ingresos versus gastos. Se ve claramente el patrón: ingresos estables de la nómina cada mes, con picos de gasto en verano por las vacaciones y en diciembre por Navidad." |
| **Pantalla: Dashboard — widget de presupuestos**. Señalar las barras de progreso. | "Abajo del todo está el widget de presupuestos. Muestra siete presupuestos activos con un semáforo visual: verde si vas bien, amarillo si te acercas al límite, rojo si lo alcanzas. Cada barra de progreso se rellena según el porcentaje consumido del presupuesto mensual." |
| **Navegar a Transactions** (menú lateral o superior). Listado de transacciones. | "Si vamos a Transacciones, vemos el listado completo, paginado, con filtros por fecha, categoría, importe y origen. Cada transacción tiene su descripción, importe, categoría con badge de color, y fecha." |
| **Navegar a Transactions/Upload**. Mostrar el formulario de subida. | "La funcionalidad estrella es la importación de extractos bancarios. Desde aquí se sube un fichero Excel del banco ING. El sistema lo parsea automáticamente, detecta duplicados por saldo y fecha, y resuelve las categorías basándose en la subcategoría que el banco ya asigna." |
| **Navegar a Budgets**. Mostrar la lista de presupuestos con semáforo. | "En la sección de Presupuestos podemos crear presupuestos mensuales por categoría, definir el límite y las fechas de vigencia. El semáforo cambia de color automáticamente según el porcentaje consumido." |
| **Navegar a Budgets/Metrics**. Mostrar las métricas detalladas. | "Dentro de cada presupuesto hay métricas detalladas: gastado, límite acumulado, porcentaje usado, restante y nivel de estado." |
| **Navegar a Analysis/Annual**. Scroll por las 7 secciones. | "Por último, el análisis anual completo. Tiene siete secciones: un resumen ejecutivo con ingresos, gastos y ahorro del año; un score de salud financiera con un anillo visual de cero a cien; ratios financieros; comparativa interanual; tendencias mensuales; distribución por categorías con gráfico donut; y detección de anomalías estadísticas con predicciones basadas en regresión lineal." |
| **Cambiar idioma a inglés** (selector en el menú de navegación). La interfaz cambia en vivo. | "Y como detalle final, SauronSheet es multi-idioma. Con un click en el selector del menú, toda la interfaz cambia entre español e inglés en tiempo real, incluyendo los gráficos y el selector de fechas." |

---

### BLOQUE 5 — Metodología y testing (6:30 – 7:30)

| Visual | Audio (voz en off) |
|--------|-------------------|
| **Slide "SDD"** del PowerPoint (flujo de fases). | "El desarrollo se ha hecho con Spec-Driven Development o SDD, una metodología donde cada funcionalidad sigue un flujo estructurado en siete fases: propuesta, especificación, diseño, tareas, implementación, verificación y archivo." |
| **Slide "TDD y Testing Pyramid"** del PowerPoint. | "Se ha aplicado TDD estricto en el dominio, con una pirámide de testing que incluye 910 tests unitarios y de integración con xUnit, y 9 especificaciones end-to-end con Playwright que simulan la interacción real de un usuario en el navegador." |
| **Slide "E2E con Playwright"** del PowerPoint. | "Los tests E2E cubren login, importación de extractos, presupuestos, categorías, análisis anual y cambio de idioma. Usan selectores data-testid para ser independientes del idioma, y se ejecutan automáticamente en el pipeline de CI/CD antes de cada deploy." |
| **Slide "Asistencia con IA"** del PowerPoint. | "Todo el desarrollo se ha realizado con asistencia de IA usando Gentle AI sobre opencode, con un ecosistema de herramientas de optimización de tokens: Headroom para compresión de contexto, Serena para análisis semántico del código, RTK para compresión de comandos, y Engram para memoria persistente entre sesiones." |

---

### BLOQUE 6 — Cierre (7:30 – 8:00)

| Visual | Audio (voz en off) |
|--------|-------------------|
| **Slide "Conclusiones"** del PowerPoint. | "En resumen, SauronSheet es una aplicación completa, desplegada en producción, con Clean Architecture, CQRS y Domain-Driven Design, 910 tests automatizados, CI/CD con GitHub Actions, multi-idioma, y un análisis financiero avanzado que incluye anomalías y predicciones." |
| **Slide "¡Gracias!"** del PowerPoint. | "Como trabajo futuro, queda pendiente la exportación a PDF, alertas push de presupuestos, importación de otros bancos como CaixaBank o Santander, y categorización automática con machine learning." |
| Fundido a negro. Aparece texto: "SauronSheet — Un ojo que todo lo ve sobre tus finanzas 🧙‍♂️💰". Debajo: URL de la demo, URL del repositorio y URL de las slides. | "Muchas gracias por vuestra atención. Podéis probar la aplicación en la URL que aparece en pantalla con las credenciales de demostración. Cualquier pregunta será bienvenida." |

---

## Notas de grabación

### Antes de grabar
1. **Preparar la app**: Iniciar sesión con `demo@sauronsheet.app` antes de grabar para que las cookies estén cacheadas y el login sea instantáneo.
2. **Navegador**: Chrome o Edge a pantalla completa (F11), sin barra de favoritos visible. Limpiar history para que no aparezca autocompletado.
3. **Resolución**: Grabar a 1920×1080 para que el texto sea legible.
4. **Idioma**: Grabar la demo en español. Si quieres, puedes mostrar el cambio a inglés pero mantener la narración en español.

### Durante la grabación
- **Ritmo**: Hablar despacio y pausado. Mejor grabar segmentos cortos yEditar.
- **Pausas**: Dejar 2-3 segundos de silencio entre bloques para facilitar el montaje.
- **Cursor**: Mover el cursor lentamente al señalar elementos. No hacer clics rápidos.
- **Carga**: Esperar a que carguen los gráficos antes de narrar lo que muestran.

### Tender after grabación
1. **Intro y cierre**: Usar las slides del PowerPoint como fondo.
2. **Demo**: Screencast de la app. Recortar tiempos de carga si son lentos.
3. **Texto**: Añadir subtítulos para accesibilidad (opcional pero recomendado para TFM).
4. **Música**: Música de fondo suave en intro y cierre (volumen muy bajo, que no compita con la voz).
5. **Transiciones**: Usar cortes secos o fundidos suaves. Evitar transiciones espectaculares.

### Software recomendado
- **Grabación de pantalla**: OBS Studio (gratis) o Camtasia.
- **Edición**: DaVinci Resolve (gratis) o Camtasia.
- **Voz en off**: Grabar por separado con Audacity (gratis) y sincronizar en edición.