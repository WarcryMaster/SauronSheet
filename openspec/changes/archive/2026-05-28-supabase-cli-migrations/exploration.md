# Exploración: Configuración de Supabase CLI para Migraciones Automáticas

## Estado Actual

### Migraciones Existentes
El proyecto tiene **12 archivos SQL de migración** en `src/SauronSheet.Infrastructure/Persistence/Migrations/`:

| Archivo | Propósito |
|---------|-----------|
| `001_CreateUsersTable.sql` | Tabla de perfiles de usuario |
| `002_CreateCategoriesTable.sql` | Tabla de categorías |
| `003_CreateTransactionsTable.sql` | Tabla de transacciones |
| `004_CreatePdfImportsTable.sql` | Tabla de importaciones PDF |
| `005_CreateUserProfileTrigger.sql` | Trigger para perfil de usuario |
| `006_CreateBudgetsTable.sql` | Tabla de presupuestos |
| `007_SystemCategoriesGlobalScope.sql` | Categorías del sistema globales |
| `008_RevertSystemCategoriesGlobalScope.sql` | Revertir scope global |
| `009_SyncExistingAuthUsers.sql` | Sincronizar usuarios existentes |
| `010_EnableRLSOnSubcategoriesAndBankTranslations.sql` | Habilitar RLS |
| `011_AddNormalizedNameColumns.sql` | Agregar columnas normalizadas |
| `012_RenamePdfImportsToImportBatches.sql` | Renombrar tabla |

**Convención de nombres actual**: `NNN_Descripcion.sql` (ej: `001_CreateUsersTable.sql`)

**Formato Supabase CLI requerido**: `YYYYMMDDHHMMSS_migration_name.sql` (ej: `20260528120000_create_users_table.sql`)

### Configuración del Proyecto
- **No existe directorio `supabase/`** en la raíz del proyecto
- **No existe `config.toml`** para Supabase CLI
- **No existen archivos `.env`** con credenciales de Supabase
- **Referencia del proyecto**: `zoebndeleapdejmqznif` (extraído de `appsettings.json`)

### Proceso de Despliegue Actual
El workflow de GitHub Actions (`master_sauronsheet.yml`) maneja:
- Build y test de .NET
- Tests E2E con Playwright
- Despliegue a Azure Web App
- **NO incluye pasos de migración de base de datos**

### Proceso de Migración Actual
Las migraciones se aplican **manualmente** vía:
- Editor SQL del dashboard de Supabase
- Sin automatización en CI/CD
- Sin integración con herramientas de migración

## Áreas Afectadas

| Ruta | Razón |
|------|-------|
| `src/SauronSheet.Infrastructure/Persistence/Migrations/` | Migraciones existentes deben renombrarse o moverse |
| `.github/workflows/master_sauronsheet.yml` | Necesita paso de migración automática |
| Raíz del proyecto | Necesita directorio `supabase/` con `config.toml` |
| `.gitignore` | Debe incluir `supabase/.temp/` |
| `README.md` | Documentar proceso de migración |

## Enfoques

### Enfoque 1: Migración Completa a Supabase CLI (Recomendado)
Reestructurar el proyecto para usar completamente el Supabase CLI.

**Pros:**
- ✅ Consistencia con el ecosistema Supabase
- ✅ Soporte para migraciones locales con `supabase db reset`
- ✅ Integración nativa con CI/CD
- ✅ Tracking automático de migraciones aplicadas
- ✅ Capacidad de rollback automatizado

**Contras:**
- ❌ Requiere renombrar los 12 archivos de migración existentes
- ❌ Impacto en documentación existente
- ❌ Riesgo de romper referencias en specs y planes

**Esfuerzo**: Medio

### Enfoque 2: Integración Parcial con Supabase CLI
Mantener la estructura actual pero agregar soporte para `supabase migration up`.

**Pros:**
- ✅ Cambio mínimo al código existente
- ✅ No afecta documentación actual
- ✅ Implementación rápida

**Contras:**
- ❌ Inconsistencia entre formatos de nombres
- ❌ Mantenimiento dual de procesos
- ❌ No aprovecha todas las ventajas del CLI

**Esfuerzo**: Bajo

## Recomendación

**Enfoque 1: Migración Completa** por las siguientes razones:

1. **Consistencia a largo plazo**: El ecosistema Supabase está creciendo y usar su CLI nativo asegura compatibilidad futura.

2. **Desarrollo local mejorado**: `supabase db reset` permite recrear la base de datos localmente desde cero, invaluable para desarrollo y testing.

3. **CI/CD robusto**: Integración nativa con GitHub Actions vía `supabase/cli` action.

4. **Tracking automático**: El CLI mantiene un registro de qué migraciones se han aplicado, evitando duplicados.

## Riesgos

1. **Riesgo de datos en producción**: Las migraciones existentes ya están aplicadas en Supabase. El CLI las marcará como "nuevas" si no se configura correctamente.
   - **Mitigación**: Usar `supabase migration repair` para marcar las existentes como aplicadas.

2. **Riesgo de ruptura en CI/CD**: El paso de migración puede fallar si las credenciales no están configuradas correctamente.
   - **Mitigación**: Configurar secrets en GitHub Actions y probar en un branch primero.

3. **Riesgo de naming conflict**: Si se renombran los archivos, las referencias en specs y planes quedarán obsoletas.
   - **Mitigación**: Actualizar documentación como parte del cambio.

## Listo para Propuesta

**Sí** — La exploración está completa. Se puede proceder con una propuesta que defina:
1. Estructura del directorio `supabase/`
2. Estrategia para las 12 migraciones existentes
3. Integración con CI/CD
4. Configuración para desarrollo local
5. Plan de rollout para producción

## Qué Necesito del Usuario

### Decisiones Requeridas
1. **¿Proceder con el Enfoque 1 (completo) o Enfoque 2 (parcial)?**
   - Recomiendo Enfoque 1, pero depende de tu preferencia.

2. **¿Las migraciones existentes deben renombrarse o mantenerse como están?**
   - Renombrar: Consistencia total, pero más trabajo
   - Mantener: Menos cambio, pero inconsistencia

3. **¿Dónde se ejecutarán las migraciones?**
   - Solo CI/CD (producción)
   - También desarrollo local
   - Ambos

### Lo que Ya Puedo Inferir del Código
- El proyecto usa Supabase para autenticación y base de datos
- Las migraciones se han aplicado manualmente hasta ahora
- No hay automatización de base de datos en el pipeline actual
- El proyecto tiene una estructura Clean Architecture bien definida
- El workflow de GitHub Actions está configurado para Azure Web App
