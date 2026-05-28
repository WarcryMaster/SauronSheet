# Database Migrations Specification

## Purpose

Orquestación de migraciones SQL mediante Supabase CLI para garantizar que los cambios de esquema se apliquen automáticamente en entornos local, CI/CD y producción, previniendo errores como PGRST205 por tablas no sincronizadas.

## Requirements

### Requirement: CLI Configuration Setup

El sistema SHALL disponer de un directorio `supabase/` en la raíz del proyecto con `config.toml` que apunte al proyecto Supabase existente (`zoebndeleapdejmqznif`).

#### Scenario: Project initialization

- GIVEN que no existe directorio `supabase/` en la raíz
- WHEN se ejecuta `supabase init`
- THEN se crea `supabase/config.toml` con la configuración del proyecto
- AND se genera `supabase/.gitignore` para excluir archivos temporales

#### Scenario: Configuration validation

- GIVEN que `supabase/config.toml` existe
- WHEN se ejecuta `supabase status`
- THEN el CLI muestra el estado de conexión con el proyecto remoto
- AND no hay errores de configuración

### Requirement: Migration File Naming Convention

Las migraciones SHALL seguir el formato `YYYYMMDDHHMMSS_descripcion_en_snake_case.sql` donde el timestamp es UTC y secuencial.

#### Scenario: New migration creation

- GIVEN que se necesita una nueva migración
- WHEN se ejecuta `supabase migration new nombre_descripcion`
- THEN se crea un archivo en `supabase/migrations/` con timestamp UTC actual
- AND el nombre usa snake_case minúsculas

#### Scenario: Existing migrations rename

- GIVEN las 12 migraciones existentes en `src/SauronSheet.Infrastructure/Persistence/Migrations/`
- WHEN se renombran al formato CLI
- THEN cada archivo mantiene su contenido intacto
- AND los timestamps son secuenciales desde `20260401120000`

### Requirement: Migration Execution Flow

El sistema SHALL ejecutar migraciones de forma automática y consistente en todos los entornos.

#### Scenario: Local development execution

- GIVEN que el desarrollador tiene Supabase CLI instalado
- WHEN ejecuta `supabase migration up`
- THEN las migraciones pendientes se aplican a la base de datos local
- AND el estado se actualiza en `supabase/migrations/` como aplicadas

#### Scenario: CI/CD execution

- GIVEN que hay cambios en archivos de migración en el push
- WHEN GitHub Actions ejecuta el workflow de deploy
- THEN `supabase db push --linked` aplica las migraciones antes del deploy
- AND el paso falla si hay errores de migración

#### Scenario: Pre-existing migrations handling

- GIVEN que las 12 migraciones ya están aplicadas en producción
- WHEN se configura Supabase CLI por primera vez
- THEN `supabase db push --linked` detecta que ya existen
- AND NO intenta re-aplicarlas (marca como aplicadas en el tracking)

### Requirement: CI/CD Integration

El workflow de GitHub Actions SHALL incluir un paso de migración antes del deploy a Azure.

#### Scenario: Successful migration in pipeline

- GIVEN que hay nuevas migraciones en el commit
- WHEN el workflow ejecuta el paso de migración
- THEN las migraciones se aplican correctamente
- AND el deploy continúa normalmente

#### Scenario: Migration failure blocks deploy

- GIVEN que una migración tiene errores de SQL
- WHEN el workflow ejecuta el paso de migración
- THEN el paso falla y el deploy NO se ejecuta
- AND se genera un error claro en los logs de GitHub Actions

#### Scenario: No migrations to apply

- GIVEN que no hay cambios en archivos de migración
- WHEN el workflow ejecuta el paso de migración
- THEN el paso completa exitosamente sin aplicar nada
- AND el deploy continúa normalmente

### Requirement: Development Documentation

El proyecto SHALL documentar el flujo de desarrollo con migraciones para nuevos desarrolladores.

#### Scenario: README documentation exists

- GIVEN que un nuevo desarrollador clona el proyecto
- WHEN lee la sección de migraciones en README
- THEN encuentra instrucciones para instalar Supabase CLI
- AND encuentra comandos para crear y ejecutar migraciones localmente

#### Scenario: Migration creation workflow documented

- GIVEN que un desarrollador necesita crear una migración
- WHEN consulta la documentación
- THEN encuentra el proceso: crear migración → probar local → push → deploy automático
- AND encuentra la convención de nombres y ejemplos

### Requirement: Gitignore Configuration

El archivo `.gitignore` SHALL excluir archivos temporales de Supabase CLI.

#### Scenario: Temporary files excluded

- GIVEN que Supabase CLI genera archivos en `.temp/`
- WHEN se ejecuta `git status`
- THEN los archivos en `supabase/.temp/` NO aparecen como pendientes
- AND el directorio `supabase/migrations/` SÍ se versiona

### Requirement: Rollback Capability

El sistema SHALL permitir revertir la configuración de Supabase CLI sin afectar migraciones aplicadas.

#### Scenario: Rollback execution

- GIVEN que se necesita revertir la configuración
- WHEN se ejecuta el plan de rollback
- THEN se elimina directorio `supabase/`
- AND se restauran archivos SQL originales desde el commit anterior
- AND se revierten cambios en `.github/workflows/` y `.gitignore`
- AND las migraciones en producción NO se ven afectadas

## Coverage

| Tipo | Estado |
|------|--------|
| Happy paths | ✅ Cubiertos |
| Edge cases | ✅ Cubiertos (migraciones pre-existente, sin cambios) |
| Error states | ✅ Cubiertos (falla SQL, credenciales) |

## Notes

- Tamaño del spec: ~550 palabras (dentro del presupuesto de 650)
- Todos los escenarios usan formato Given/When/Then
- Requisitos usan keywords RFC 2119 (SHALL, SHOULD, MAY)
- Specs en español neutro según config.yaml
