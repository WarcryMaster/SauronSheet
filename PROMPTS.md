/prompt:'prompts/speckit.plan.prompt.md' 

Actúa como un arquitecto de software senior experto en Spec Driven Development (SDD) usando Speckit.

Necesito que generes un archivo `speckit.plan` COMPLETO y detallado para la fase 3 #file:'C:\Projects\SauronSheet\specs\phase-3\phase-3-spec.md' 
---

## CONTEXTO PARA EL speckit.plan
Que siga el documento el mismo formato que las fases previas:
#file:'C:\Projects\SauronSheet\.specify\memory\constitution.md' 
#file:'C:\Projects\SauronSheet\specs\phase-0\phase-0-plan.md' 
#file:'C:\Projects\SauronSheet\specs\phase-1\phase-1-plan.md' 
#file:'C:\Projects\SauronSheet\specs\phase-2\phase-2-plan.md' 
#file:'C:\Projects\SauronSheet\specs\spec.md' 

-------------------------------------------------------------

/prompt:'prompts/speckit.clarify.prompt.md' 

verifica que no haya en los archivos inconsistencias o casos no contemplados o ambiguos
#file:'C:\Projects\SauronSheet\specs\phase-1\phase-1-tasks.md'  
#file:'C:\Projects\SauronSheet\.specify\memory\constitution.md'  
#file:'C:\Projects\SauronSheet\specs\phase-1\phase-1-plan.md' 
#file:'C:\Projects\SauronSheet\specs\phase-1\phase-1-spec.md' 

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

/prompt:'prompts/speckit.implement.prompt.md' 

Implementa la fase 3 del proyecto SpecKit siguiendo los documentos de referencia: #file:'C:\Projects\SauronSheet\specs\phase-3\phase-3-tasks.md' 

Entradas:
- #file:'C:\Projects\SauronSheet\.specify\memory\constitution.md' 
- #file:'C:\Projects\SauronSheet\specs\phase-3\phase-3-spec.md' 
- #file:'C:\Projects\SauronSheet\specs\phase-3\phase-3-plan.md' 
- #file:'C:\Projects\SauronSheet\specs\phase-2\phase-2-tasks.md' 
- #file:'C:\Projects\SauronSheet\specs\spec.md' 

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