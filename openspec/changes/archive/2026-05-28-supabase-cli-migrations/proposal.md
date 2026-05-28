# Proposal: Configuración de Supabase CLI para Migraciones Automáticas

## Intent

Las migraciones de base de datos se ejecutan manualmente vía dashboard de Supabase. Esto causó el error **PGRST205** en producción cuando la tabla `import_batches` existía en el esquema esperado por la API pero no había sido creada. Migrar a Supabase CLI garantiza que las migraciones se apliquen automáticamente en local, CI/CD y despliegues.

## Scope

### In Scope
- Crear directorio `supabase/` con `config.toml` apuntando al proyecto `zoebndeleapdejmqznif`
- Renombrar 12 migraciones existentes al formato CLI (`YYYYMMDDHHMMSS_name.sql`)
- Agregar paso de migración en GitHub Actions (antes del deploy)
- Actualizar `.gitignore` para excluir `supabase/.temp/`
- Documentar flujo de desarrollo local con `supabase migration up`

### Out of Scope
- Migraciones nuevas de funcionalidad
- Cambios en la arquitectura de la aplicación
- Refactor de los parsers de PDF o la lógica de dominio

## Capabilities

### New Capabilities
- `database-migrations`: Orquestación de migraciones SQL vía Supabase CLI, incluyendo naming convention, CI/CD integration y flujo local

### Modified Capabilities
- None

## Approach

1. **Estructura**: Crear `supabase/config.toml` con proyecto ID y configuración local
2. **Renombrado**: Convertir las 12 migraciones de `NNN_Name.sql` a `YYYYMMDDHHMMSS_name.sql` con timestamps secuenciales
3. **CI/CD**: Agregar step `supabase db push --linked` antes del deploy en `master_sauronsheet.yml`, requiriendo secrets `SUPABASE_ACCESS_TOKEN` y `SUPABASE_DB_PASSWORD`
4. **Local**: Documentar `supabase start` + `supabase migration up` en README

### Migraciones a Renombrar

| Actual | Nuevo |
|--------|-------|
| `001_CreateUsersTable.sql` | `20260401120000_create_users_table.sql` |
| `002_CreateCategoriesTable.sql` | `20260401120001_create_categories_table.sql` |
| `003_CreateTransactionsTable.sql` | `20260401120002_create_transactions_table.sql` |
| `004_CreatePdfImportsTable.sql` | `20260401120003_create_pdf_imports_table.sql` |
| `005_CreateUserProfileTrigger.sql` | `20260401120004_create_user_profile_trigger.sql` |
| `006_CreateBudgetsTable.sql` | `20260401120005_create_budgets_table.sql` |
| `007_SystemCategoriesGlobalScope.sql` | `20260401120006_system_categories_global_scope.sql` |
| `008_RevertSystemCategoriesGlobalScope.sql` | `20260401120007_revert_system_categories_global_scope.sql` |
| `009_SyncExistingAuthUsers.sql` | `20260401120008_sync_existing_auth_users.sql` |
| `010_EnableRLSOnSubcategoriesAndBankTranslations.sql` | `20260401120009_enable_rls_on_subcategories_and_bank_translations.sql` |
| `011_AddNormalizedNameColumns.sql` | `20260401120010_add_normalized_name_columns.sql` |
| `012_RenamePdfImportsToImportBatches.sql` | `20260401120011_rename_pdf_imports_to_import_batches.sql` |

### Convención para Futuras Migraciones

Formato: `YYYYMMDDHHMMSS_descripcion_en_snake_case.sql`
Generar timestamp: `date -u +"%Y%m%d%H%M%S"` (UTC)

## Affected Areas

| Area | Impact | Descripción |
|------|--------|-------------|
| `src/SauronSheet.Infrastructure/Persistence/Migrations/` | Removed | Directorio eliminado tras migrar archivos |
| `supabase/config.toml` | New | Configuración del proyecto Supabase CLI |
| `supabase/migrations/` | New | 12 archivos SQL renombrados |
| `.github/workflows/master_sauronsheet.yml` | Modified | Nuevo step de migración antes del deploy |
| `.gitignore` | Modified | Agregar `supabase/.temp/` |

## Risks

| Riesgo | Probabilidad | Mitigación |
|--------|-------------|------------|
| Migraciones existentes ya aplicadas en producción — CLI intenta re-aplicarlas | Alta | Usar `supabase db push --linked` que marca como aplicadas; o crear archivo `supabase/migrations/.supabase` vacío como baseline |
| CI/CD falla por credenciales no configuradas | Media | Documentar secrets requeridos; probar en rama feature antes de merge |
| Conflictos si hay migraciones manuales recientes en producción | Media | Verificar estado actual en dashboard antes de migrar; usar `supabase migration list` |

## Rollback Plan

1. Eliminar directorio `supabase/`
2. Restaurar archivos SQL en `src/SauronSheet.Infrastructure/Persistence/Migrations/` desde el commit anterior
3. Revertir cambios en `.github/workflows/master_sauronsheet.yml`
4. Revertir cambios en `.gitignore`
5. Las migraciones en producción no se ven afectadas (solo cambia el mecanismo de ejecución)

## Dependencies

- Supabase CLI instalado localmente (`brew install supabase/tap/supabase`)
- Secrets de GitHub Actions: `SUPABASE_ACCESS_TOKEN`, `SUPABASE_DB_PASSWORD`
- Proyecto Supabase existente: `zoebndeleapdejmqznif`

## Success Criteria

- [ ] `supabase migration list` muestra las 12 migraciones como aplicadas
- [ ] `supabase migration up` ejecuta sin errores en local
- [ ] GitHub Actions ejecuta migraciones automáticamente antes del deploy
- [ ] Error PGRST205 no se reproduce al crear tablas nuevas
- [ ] Flujo de desarrollo documentado: crear migración → probar local → push → deploy automático
