/prompt:'prompts/speckit.plan.prompt.md

Actúa como un arquitecto de software senior experto en Spec Driven Development (SDD) usando Speckit.

Necesito que generes un archivo `speckit.plan` COMPLETO y detallado para el siguiente proyecto:

## 📌 CONTEXTO DEL PROYECTO
- **Nombre del proyecto:** [NOMBRE]
- **Descripción:** [DESCRIPCIÓN BREVE]
- **Tipo de aplicación:** [web app / API / CLI / librería / microservicio / etc.]
- **Stack tecnológico:** [lenguajes, frameworks, bases de datos, etc.]
- **Usuarios objetivo:** [quiénes van a usar esto]
- **Problema que resuelve:** [problema principal]

## 📐 REQUISITOS CLAVE
1. [Requisito funcional 1]
2. [Requisito funcional 2]
3. [Requisito funcional 3]
...

## 🚫 RESTRICCIONES / LIMITACIONES
- [Restricción 1]
- [Restricción 2]

## 🔗 INTEGRACIONES EXTERNAS
- [API / servicio externo 1]
- [API / servicio externo 2]

---

## INSTRUCCIONES PARA EL speckit.plan

El `speckit.plan` que generes DEBE incluir las siguientes secciones completas:

### 1. **project_overview**
   - Nombre, descripción, objetivo principal
   - Alcance (qué SÍ incluye y qué NO incluye)
   - Supuestos y dependencias externas

### 2. **architecture**
   - Patrón arquitectónico elegido y justificación
   - Diagrama de componentes (en texto/mermaid)
   - Flujo de datos principal
   - Decisiones arquitectónicas clave (ADRs resumidos)

### 3. **modules**
   - Lista de cada módulo/componente del sistema
   - Para CADA módulo:
     - Nombre y descripción
     - Responsabilidades (Single Responsibility)
     - Inputs / Outputs
     - Dependencias internas y externas
     - Archivos/specs asociados que se generarán

### 4. **data_model**
   - Entidades principales
   - Atributos y tipos de cada entidad
   - Relaciones entre entidades
   - Reglas de validación críticas

### 5. **api_surface** (si aplica)
   - Endpoints / interfaces públicas
   - Métodos, parámetros, respuestas esperadas
   - Códigos de error y manejo de errores
   - Autenticación/autorización

### 6. **specs_breakdown**
   - Lista ordenada de TODOS los archivos `.spec` que se deben crear
   - Cada spec debe tener:
     - Nombre del archivo
     - Propósito
     - Dependencias con otros specs
     - Criterios de aceptación resumidos
   - Orden de implementación sugerido (dependencias primero)

### 7. **phases**
   - Fases de desarrollo en orden
   - Qué specs/módulos se implementan en cada fase
   - Entregables por fase
   - Criterios de "done" por fase

### 8. **testing_strategy**
   - Tipos de tests (unit, integration, e2e)
   - Cobertura mínima esperada
   - Casos edge críticos a cubrir
   - Herramientas de testing

### 9. **conventions**
   - Estructura de carpetas del proyecto
   - Naming conventions
   - Patrones de código a seguir
   - Manejo de errores estándar
   - Formato de logs

### 10. **context_for_ai**
   - Resumen ejecutivo del proyecto para que cualquier LLM entienda el contexto
   - Glosario de términos del dominio
   - Reglas de negocio críticas que NO se deben violar
   - Ejemplos de uso típicos (happy path)
   - Anti-patrones a evitar en este proyecto

---

## FORMATO DE SALIDA

- Usa formato YAML o TOML (el nativo de speckit)
- Sé ESPECÍFICO, no genérico — cada campo debe tener contenido real basado en mi proyecto
- Incluye comentarios inline explicando decisiones importantes
- Asegúrate de que los specs tengan un DAG (grafo acíclico dirigido) claro de dependencias
- Cada spec debe ser lo suficientemente granular para implementarse en UNA sesión de trabajo

## CRITERIOS DE CALIDAD

El plan debe ser:
✅ Auto-contenido (cualquier dev nuevo entiende el proyecto leyendo solo el plan)
✅ Implementable (no hay ambigüedades)
✅ Ordenado por dependencias (nada se implementa antes que sus dependencias)
✅ Trazable (cada requisito está mapeado a al menos un spec)
✅ Consistente (no hay contradicciones entre secciones)

-------------------------------------------------------------

/prompt:'prompts/speckit.clarify.prompt.md' 

verifica que no haya en los archivos 
#file:'C:\Projects\SauronSheet\specs\phase-1\phase-1-tasks.md'  
#file:'C:\Projects\SauronSheet\.specify\memory\constitution.md'  
#file:'C:\Projects\SauronSheet\specs\phase-1\phase-1-plan.md' 
#file:'C:\Projects\SauronSheet\specs\phase-1\phase-1-spec.md' 

inconsistencias o casos no contemplados o ambiguos

-------------------------------------------------------------

/prompt:'prompts/speckit.tasks.prompt.md' 
Genera el archivo phase-1-tasks.md para la fase actual del proyecto SpecKit.

Entradas obligatorias:
- #file:'C:\Projects\SauronSheet\.specify\memory\constitution.md' 
- #file:'C:\Projects\SauronSheet\specs\phase-0\phase-0-plan.md' 
- #file:'C:\Projects\SauronSheet\specs\phase-0\phase-0-spec.md' 
- #file:'C:\Projects\SauronSheet\specs\phase-0\phase-0-tasks.md' 
- #file:'C:\Projects\SauronSheet\specs\phase-1\phase-1-plan.md' 
#file:'C:\Projects\SauronSheet\specs\phase-1\phase-1-spec.md' 
- #file:'C:\Projects\SauronSheet\specs\spec.md' 

Objetivo:
Transformar el plan de implementación en una lista de tareas ejecutables paso a paso.

REQUISITOS ESTRICTOS:

1. Las tareas deben ser:
- Atómicas
- Secuenciales
- Con dependencias explícitas
- Listas para implementación
- Sin ambigüedad
- Orientadas a TDD cuando aplique

2. Cada tarea debe incluir:
- Path exacto del archivo
- Acción (crear, modificar, eliminar)
- Descripción clara del objetivo de implementación
- Dependencias (si existen)
- Criterios de validación

3. No escribas código.
Solo describe tareas.

4. Respeta estrictamente:
- Las reglas de constitution.md
- La estructura definida en plan.md
- Los requisitos de phase-X-spec.md
- Los límites de Clean Architecture

5. No implementes fases futuras.
Solo genera tareas necesarias para la fase actual.

6. Agrupa las tareas por capa:
- Setup de solución
- Domain
- Application
- Infrastructure
- Frontend
- Testing
- Validación

7. Las tareas deben ser lo suficientemente detalladas para que:
- Un desarrollador junior pueda ejecutarlas.
- Un agente AI pueda implementarlas sin interpretación adicional.
- Cada tarea afecte solo una unidad lógica.

Usa el siguiente formato:

# Tasks

## 1. Setup de solución

### Tarea 1.1 – Crear estructura de solución
- Path:
- Acción:
- Descripción:
- Dependencias:
- Validación:

## 2. Domain

### Tarea 2.1 – Crear clase base Entity
- Path: src/Project.Domain/Common/Entity.cs
- Acción: Crear archivo
- Descripción:
- Dependencias:
- Validación:

...

## Orden de implementación

Incluye al final una lista ordenada de ejecución.

No expliques nada fuera del documento.
No resumas.
Produce únicamente el contenido completo de tasks.md.

--------------------------------------------------------------

/prompt:'prompts/speckit.implement.prompt.md

Implementa la fase actual del proyecto SpecKit siguiendo los documentos de referencia.

Entradas:
- constitution.md (autoridad arquitectónica)
- plan.md (plan de implementación)
- tasks.md (lista de tareas de la fase actual)
- phase-X-spec.md (spec de la fase actual)
- Otras phase specs (solo para contexto)

Objetivo:
Generar la implementación de todas las tareas de tasks.md en código real y completo, lista para compilar y ejecutar, respetando Clean Architecture y las reglas de constitution.md.

REQUISITOS ESTRICTOS:

1. Implementa exactamente las tareas definidas en tasks.md.
   - No agregar tareas no listadas.
   - No implementar tareas de fases futuras.

2. Código:
   - Correcto, compilable y funcional.
   - Con namespaces correctos.
   - Con convenciones de código definidas en constitution.md.
   - Cada clase, archivo, test y configuración debe coincidir con plan.md.
   - Mantener separación por capas (Domain, Application, Infrastructure, Frontend, Tests).

3. Tests:
   - Implementar los tests definidos en plan.md / phase-X-spec.md.
   - Seguir naming convention y patrón Arrange-Act-Assert.
   - Deben ser ejecutables con `dotnet test`.

4. Documentación y comentarios:
   - Incluir comentarios claros donde sea necesario.
   - Comentar “TODO” si algún elemento depende de fases futuras.
   - No omitir ninguna parte requerida del plan.

5. Archivos de configuración:
   - global.json, Directory.Build.props, appsettings.json
   - Deben ser generados según plan.md.
   - Validar consistencia con la fase actual.

6. Estructura de solución:
   - Crear o modificar archivos exactamente en las rutas definidas.
   - Mantener dependencia entre proyectos según plan.md.

7. No mezclar fases:
   - Implementar solo lo definido para la fase actual.
   - Si hay elementos marcados como futuros o deferred, solo colocar comentarios TODO.

Formato de salida:

- Entregar como archivos de código estructurados por carpeta.
- Cada archivo con su path correspondiente.
- Para tests, mostrar el contenido completo.
- No resumir.
- No explicar fuera de los archivos.