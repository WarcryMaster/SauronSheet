# Guión del Vídeo — SauronSheet TFM

**Autor:** Gonzalo Cantarero Galvez
**Máster:** Desarrollo con IA
**Duración estimada:** 9-10 minutos
**Formato:** Screencast (grabación de pantalla) + voz en off
**Idioma:** Español neutro
**URL de la app:** https://sauronsheet-akd4gkewdpbtbgea.spaincentral-01.azurewebsites.net
**Credenciales demo:** `demo@sauronsheet.app` / `Demo1234!`

---

## Estructura del vídeo

| Bloque | Duración | Tiempo acumulado |
|--------|----------|------------------|
| 1. Introducción | 45 s | 0:00 – 0:45 |
| 2. El problema y la solución | 1 min 15 s | 0:45 – 2:00 |
| 3. Arquitectura y stack | 1 min 30 s | 2:00 – 3:30 |
| 4. Demo de funcionalidades (pantalla por pantalla) | 4 min 30 s | 3:30 – 8:00 |
| 5. Metodología, testing y CI/CD | 1 min 15 s | 8:00 – 9:15 |
| 6. Cierre | 45 s | 9:15 – 10:00 |

> La demo es el corazón del vídeo. Si necesitas recortar tiempo, reduce los bloques 3 y 5, nunca el 4.

---

## Guión técnico

### BLOQUE 1 — Introducción (0:00 – 0:45)

| Visual | Audio (voz en off) |
|--------|-------------------|
| **Slide de portada** de la presentación (logo de SauronSheet, título "Control Financiero Personal", nombre "Gonzalo Cantarero Galvez", "Máster de Desarrollo con IA"). | "Hola. Soy Gonzalo Cantarero Galvez y este es el vídeo de presentación de SauronSheet, mi Trabajo de Fin de Máster del Máster de Desarrollo con IA." |
| Transición a la **slide de índice** de la presentación. | "SauronSheet es una aplicación web de finanzas personales que importa extractos bancarios desde Excel, permite gestionar transacciones a mano, categorizar gastos, crear presupuestos con semáforo visual y obtener un análisis anual avanzado de la salud financiera." |
| Fundido a la **pantalla de login** de la app desplegada. Leve zoom al logo. | "En estos minutos os cuento qué problema resuelve, cómo está construido, y os enseño la aplicación en funcionamiento, pantalla por pantalla." |

---

### BLOQUE 2 — El problema y la solución (0:45 – 2:00)

| Visual | Audio (voz en off) |
|--------|-------------------|
| **Slide "El Problema"** de la presentación. Los bullets aparecen uno a uno. | "Hacer seguimiento de los gastos personales es tedioso, y por eso la mayoría de la gente no lo hace. Por cuatro motivos: primero, meter los datos a mano es aburrido y propenso a errores. Segundo, falta una visión global de ingresos frente a gastos. Tercero, los presupuestos se olvidan porque no hay alertas visuales. Y cuarto, no hay contexto histórico: ni tendencias ni predicciones." |
| **Slide "La Solución"** de la presentación. | "SauronSheet ataca esos cuatro problemas. Importa automáticamente los extractos del banco desde ficheros Excel, muestra un dashboard interactivo con gráficos, permite crear presupuestos con un semáforo de colores, y ofrece un análisis anual con score de salud financiera, detección de anomalías y predicciones." |
| Subtítulo en la slide: "Un ojo que todo lo ve sobre tus finanzas". Pausa breve. | "El nombre es un juego de palabras: Sauron, el ojo que todo lo ve, y Sheet, hoja de cálculo. Un ojo que todo lo ve sobre tus finanzas." |

---

### BLOQUE 3 — Arquitectura y stack (2:00 – 3:30)

| Visual | Audio (voz en off) |
|--------|-------------------|
| **Slide "Stack tecnológico — Backend"**. | "Técnicamente, el backend está construido con .NET 10 y C# 13, aplicando CQRS con MediatR para separar los comandos de las consultas. La persistencia es Supabase, es decir PostgreSQL gestionado, con autenticación integrada y Row-Level Security para aislar los datos de cada usuario a nivel de base de datos." |
| **Slide "Stack tecnológico — Frontend"**. | "El frontend usa Razor Pages con renderizado en servidor, MDBootstrap para la interfaz, Alpine.js para la reactividad, HTMX para las peticiones AJAX sin recargar la página, Chart.js para los gráficos y Flatpickr para los selectores de fecha." |
| **Slide "Clean Architecture"** (diagrama de capas). | "La arquitectura sigue Clean Architecture, con cuatro capas y las dependencias apuntando siempre hacia adentro. El Dominio no conoce a nadie. La Aplicación solo conoce el Dominio. La Infraestructura implementa las interfaces del Dominio. Y el Frontend solo habla con la Aplicación." |
| **Slide "Domain-Driven Design"**. | "Dentro del Dominio se aplica Domain-Driven Design: aggregate roots como Transaction, Budget y Category; value objects inmutables como Money; domain services para la lógica que cruza entidades; y el patrón Specification para los filtros reutilizables." |
| Transición a la app. | "Y todo esto se despliega solo en Azure App Service mediante un pipeline de integración y despliegue continuos con GitHub Actions." |

---

### BLOQUE 4 — Demo de funcionalidades (3:30 – 8:00)

> Grabar toda la demo con el usuario `demo@sauronsheet.app`, que ya tiene 30 meses de datos cargados (enero 2024 – julio 2026).

#### 4.1 — Login (3:30 – 3:45)

| Visual | Audio (voz en off) |
|--------|-------------------|
| **Pantalla de login**. Escribir `demo@sauronsheet.app` y `Demo1234!`. Click en "Iniciar sesión". | "Entramos con el usuario de demostración. La autenticación la gestiona Supabase Auth con tokens JWT; cada usuario solo ve sus propios datos gracias a las políticas de seguridad a nivel de fila." |

#### 4.2 — Dashboard (3:45 – 4:30)

| Visual | Audio (voz en off) |
|--------|-------------------|
| **Dashboard**. Esperar a que se animen los KPIs y carguen los gráficos. | "Esto es el Dashboard. Arriba, cuatro indicadores animados: ingresos totales, gastos totales, neto y número de transacciones." |
| Señalar el **selector de periodo** (píldoras: Todo / Este mes / Últimos 3 meses / Este año). Hacer click en "Este año". | "Con estas píldoras filtramos el periodo sin recargar la página, usando HTMX: todo el histórico, este mes, los últimos tres meses o este año. Los KPIs y los gráficos se recalculan al vuelo." |
| Recorrer los **tres gráficos**: barras apiladas por categoría, tendencia mensual y comparativa interanual. | "Debajo hay tres gráficos: los gastos por categoría en barras apiladas por mes; la tendencia mensual de ingresos frente a gastos; y una comparativa año contra año. Se ve el patrón claramente: nómina estable e ingresos y gastos que suben en verano y en Navidad." |
| Scroll a la **tabla de transacciones recientes** y al **widget de presupuestos** con barras de progreso y chips de estado. | "Más abajo, las últimas transacciones, y el widget de presupuestos: cada presupuesto con su barra de progreso y un semáforo — verde si vas bien, amarillo si te acercas, rojo si lo superas — más el porcentaje total consumido." |

#### 4.3 — Transacciones: listado, filtros y borrado (4:30 – 5:00)

| Visual | Audio (voz en off) |
|--------|-------------------|
| Menú lateral → **Transacciones**. Mostrar la tabla paginada. | "En Transacciones tenemos el listado completo, paginado. Cada fila muestra fecha, descripción, importe con su divisa, y la categoría con un badge de color." |
| Usar los **filtros**: rango de fechas con Flatpickr y el selector de **origen** (chips). Aplicar. | "Podemos filtrar por rango de fechas con un calendario, y por origen del movimiento con este selector de etiquetas, para aislar por ejemplo solo lo importado de un banco concreto." |
| Marcar dos o tres **checkboxes** y pulsar "Eliminar seleccionadas". Aparece el **modal con cuenta atrás de 2 segundos**. Cancelar. | "Permite selección múltiple y borrado en lote, con un modal de confirmación que obliga a esperar dos segundos antes de confirmar, para evitar borrados accidentales. Cancelo." |

#### 4.4 — Alta manual y edición (5:00 – 5:15)

| Visual | Audio (voz en off) |
|--------|-------------------|
| Click en **"Añadir transacción"**. Rellenar el formulario (fecha, descripción, importe, categoría). Guardar. | "Además de importar, podemos dar de alta transacciones a mano: fecha, descripción, importe y categoría. El formulario valida los datos antes de enviarlos." |
| Volver al listado y abrir **editar** en una fila. | "Y cualquier transacción se puede editar después desde el propio listado." |

#### 4.5 — Búsqueda avanzada (5:15 – 5:30)

| Visual | Audio (voz en off) |
|--------|-------------------|
| Menú → **Buscar**. Escribir un término y aplicar filtros. | "Hay también una búsqueda avanzada, para localizar transacciones por texto y combinaciones de filtros cuando el volumen de datos crece." |

#### 4.6 — Importación de extractos (5:30 – 6:00)

| Visual | Audio (voz en off) |
|--------|-------------------|
| Menú → **Importar**. Mostrar la **guía de formato** (hoja "Movimientos", columnas F. VALOR, CATEGORÍA, SUBCATEGORÍA, DESCRIPCIÓN, IMPORTE, SALDO). | "Esta es la funcionalidad estrella. La app espera un Excel del banco con la hoja Movimientos y estas columnas. Aquí mismo se muestra la guía de formato." |
| **Arrastrar un fichero Excel** a la zona de drop (o seleccionarlo). Pulsar importar. Mostrar la **barra de progreso en tiempo real**. | "Soltamos el fichero en la zona de arrastre — admite varios a la vez — y al importar vemos el progreso en tiempo real, que se actualiza por polling con HTMX cada segundo." |
| Mostrar el **resultado**: importadas, omitidas (duplicados) y errores por fila. | "Al terminar, un resumen: cuántas se importaron, cuántas se omitieron por ser duplicados — que detecta por saldo y fecha — y el detalle de errores fila por fila si los hubiera. Las categorías se resuelven automáticamente a partir de la subcategoría que el banco ya asigna." |

#### 4.7 — Categorías (6:00 – 6:20)

| Visual | Audio (voz en off) |
|--------|-------------------|
| Menú → **Categorías**. Mostrar el listado (categorías del sistema + propias). | "En Categorías tenemos las categorías por defecto del sistema, que están traducidas, y las que crea el usuario." |
| Abrir el **modal de crear**: nombre, tipo (ingreso/gasto), icono y selector de color. | "Al crear una categoría elegimos nombre, tipo — ingreso o gasto —, un icono y un color, con vista previa en vivo." |
| Intentar borrar una **categoría del sistema** para mostrar que está protegida. | "Las categorías del sistema están protegidas: no se pueden borrar, porque son la base sobre la que se clasifican las importaciones. Es una regla de negocio del dominio." |

#### 4.8 — Presupuestos (6:20 – 6:50)

| Visual | Audio (voz en off) |
|--------|-------------------|
| Menú → **Presupuestos**. Mostrar la tabla (categoría, granularidad, límite, vigencia, estado) y los **filtros** (solo activos, por categoría). | "En Presupuestos definimos cuánto queremos gastar por categoría. La tabla muestra la granularidad, el límite, el periodo de vigencia y si está activo o no. Podemos filtrar por categoría o ver solo los activos." |
| Entrar en **crear presupuesto** (categoría, límite, fechas). Guardar. | "Al crear uno indicamos categoría, importe límite y fechas de vigencia." |
| Navegar a **Métricas** de presupuestos: gastado, límite acumulado, porcentaje usado, restante y estado. | "Y en las métricas vemos el detalle: gastado, límite acumulado, porcentaje consumido, restante y el nivel de estado con su semáforo. También hay un histórico y una comparativa de presupuestado frente a real." |

#### 4.9 — Análisis Anual (6:50 – 7:45)

| Visual | Audio (voz en off) |
|--------|-------------------|
| Menú → **Análisis Anual**. Mostrar arriba el **navegador de año** (◀ ▶ y desplegable). | "Y llegamos a la joya de la corona: el análisis anual. Arriba navegamos entre años con las flechas o el desplegable, otra vez con HTMX." |
| Señalar el **anillo de Health Score** (0-100) y su **desglose en 6 sub-scores** (ahorro, estabilidad de ingresos, estabilidad de gastos, dependencia de categorías, equilibrio, tendencia). | "Lo primero es un score de salud financiera de cero a cien, con un anillo que cambia de color. Y no es una nota mágica: se desglosa en seis sub-scores — ahorro, estabilidad de ingresos, estabilidad de gastos, dependencia de categorías, equilibrio y tendencia." |
| Recorrer los **KPIs ejecutivos** (ingresos, gastos, neto, % coste fijo, tasa de ahorro) y los **ratios**. | "Debajo, los KPIs ejecutivos del año: ingresos, gastos, neto, porcentaje de coste fijo y tasa de ahorro, cada uno con su variación respecto al año anterior; y una fila de ratios financieros." |
| Mostrar los **gráficos**: tendencia, distribución (donut), evolución mensual y comparativa multi-año. | "Luego, varios gráficos: la tendencia mensual, la distribución por tipo en donut, la evolución mes a mes con el mejor mes de ingreso y de gasto, y una comparativa de varios años." |
| Mostrar la **tabla de categorías** con flechas de tendencia (▲▼) y badge de "nueva", y la **comparativa interanual** y la **detección de anomalías**. | "La tabla de categorías marca con flechas si cada categoría sube o baja respecto al año pasado, y etiqueta las nuevas. Y cierra con la comparativa año contra año y la detección de anomalías estadísticas, con predicciones basadas en regresión lineal." |

#### 4.10 — Multi-idioma (7:45 – 8:00)

| Visual | Audio (voz en off) |
|--------|-------------------|
| Abrir el **selector de idioma** del menú y cambiar a inglés. La interfaz cambia en vivo. | "Como último detalle, la aplicación es multi-idioma. Con un clic cambiamos toda la interfaz entre español e inglés en tiempo real, incluyendo los gráficos y los formatos de fecha y moneda." |

---

### BLOQUE 5 — Metodología, testing y CI/CD (8:00 – 9:15)

| Visual | Audio (voz en off) |
|--------|-------------------|
| **Slide "Spec-Driven Development"** (flujo de fases). | "El desarrollo se ha hecho con Spec-Driven Development: cada funcionalidad recorre un flujo estructurado — propuesta, especificación, diseño, tareas, implementación, verificación y archivo — antes de darse por terminada." |
| **Slide "TDD y pirámide de testing"**. | "En el dominio se ha aplicado TDD estricto, con una pirámide de testing: tests unitarios y de integración con xUnit, y tests end-to-end con Playwright que simulan a un usuario real en el navegador." |
| **Slide "E2E con Playwright"**. | "Los tests end-to-end cubren login, importación, presupuestos, categorías, análisis anual y cambio de idioma. Usan selectores data-testid, así que son independientes del idioma, y se ejecutan en el pipeline antes de cada despliegue." |
| **Slide "CI/CD con GitHub Actions"**. | "El pipeline de GitHub Actions compila, pasa todos los tests y, solo si todo está en verde, despliega automáticamente en Azure." |
| **Slide "Asistencia con IA"**. | "Y todo el proyecto se ha desarrollado con asistencia de inteligencia artificial, con un ecosistema de herramientas: compresión de contexto, análisis semántico del código y memoria persistente entre sesiones, dirigiendo yo la arquitectura y validando cada decisión." |

---

### BLOQUE 6 — Cierre (9:15 – 10:00)

| Visual | Audio (voz en off) |
|--------|-------------------|
| **Slide "Conclusiones"**. | "En resumen, SauronSheet es una aplicación completa y desplegada en producción: Clean Architecture, CQRS y DDD, importación automática de extractos, análisis financiero avanzado con anomalías y predicciones, multi-idioma, tests automatizados y CI/CD." |
| **Slide de trabajo futuro**. | "Como trabajo futuro quedan la exportación a PDF, las alertas de presupuesto, el soporte de más bancos y la categorización automática con machine learning." |
| Fundido a la **slide de cierre** con el logo, el texto "Un ojo que todo lo ve sobre tus finanzas" y las URLs (demo, repositorio y slides). | "Gracias por vuestra atención. Podéis probar la aplicación en la URL en pantalla con las credenciales de demostración. Cualquier pregunta será bienvenida." |

---

## Notas de grabación

### Antes de grabar
1. **Preparar la app**: Iniciar sesión con `demo@sauronsheet.app` antes de grabar para que las cookies estén cacheadas y el login sea instantáneo. Ten un Excel de ejemplo listo para la demo de importación.
2. **Navegador**: Chrome o Edge a pantalla completa (F11), sin barra de favoritos visible. Limpiar el historial para que no aparezca autocompletado con datos personales.
3. **Resolución**: Grabar a 1920×1080 para que el texto sea legible.
4. **Idioma**: Grabar la demo en español. Mostrar el cambio a inglés al final, pero mantener la narración en español.

### Durante la grabación
- **Ritmo**: Hablar despacio y pausado. Mejor grabar segmentos cortos y editar después.
- **Pausas**: Dejar 2-3 segundos de silencio entre bloques para facilitar el montaje.
- **Cursor**: Mover el cursor lentamente al señalar elementos. Evitar clics rápidos.
- **Carga**: Esperar a que carguen los gráficos antes de narrar lo que muestran.

### Tras la grabación
1. **Intro y cierre**: Usar las slides de la presentación como fondo.
2. **Demo**: Screencast de la app. Recortar tiempos de carga si son lentos.
3. **Texto**: Añadir subtítulos para accesibilidad (recomendado para un TFM).
4. **Música**: Música de fondo suave en intro y cierre, a volumen muy bajo, que no compita con la voz.
5. **Transiciones**: Cortes secos o fundidos suaves. Evitar transiciones espectaculares.

### Software recomendado
- **Grabación de pantalla**: OBS Studio (gratis) o Camtasia.
- **Edición**: DaVinci Resolve (gratis) o Camtasia.
- **Voz en off**: Grabar por separado con Audacity (gratis) y sincronizar en la edición.
