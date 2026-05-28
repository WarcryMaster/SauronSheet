# Design: Configuración de Supabase CLI para Migraciones Automáticas

## Technical Approach

Migrar el sistema de migraciones de base de datos de ejecución manual vía dashboard a Supabase CLI. Esto implica crear directorio `supabase/` con configuración, renombrar 12 migraciones existentes al formato CLI, integrar en CI/CD, y documentar flujo local. El enfoque es migración completa para consistencia a largo plazo.

## Architecture Decisions

### Decision: Ubicación del Directorio supabase/

| Opción | Tradeoff | Decisión |
|--------|----------|----------|
| Raíz del proyecto (`supabase/`) | Ubicación estándar CLI, fácil de encontrar | **Elegida** |
| Dentro de `src/SauronSheet.Infrastructure/` | Consistencia con migraciones actuales | Rechazada — rompe convención CLI |
| Subdirectorio `tools/supabase/` | Separación de herramientas | Rechazada — no es estándar |

**Rationale**: Supabase CLI espera `supabase/` en la raíz del repositorio. Ubicarlo en otro lugar requiere flags adicionales y rompe la convención.

### Decision: Manejo de Migraciones Pre-existentes

| Opción | Tradeoff | Decisión |
|--------|----------|----------|
| `supabase db push --linked` con tracking automático | Detecta y marca como aplicadas | **Elegida** |
| Crear archivo `.supabase` vacío como baseline | Control manual | Rechazada — propenso a errores |
| Re-ejecutar con `IF NOT EXISTS` | Duplica lógica | Rechazada — no es limpio |

**Rationale**: `supabase db push --linked` sincroniza el estado de migraciones aplicadas en producción sin re-ejecutarlas. Es el mecanismo oficial y seguro.

### Decision: Formato de Nombres de Migración

| Opción | Tradeoff | Decisión |
|--------|----------|----------|
| `YYYYMMDDHHMMSS_snake_case.sql` (estándar CLI) | Requiere renombrar 12 archivos | **Elegida** |
| Mantener `NNN_Name.sql` con config custom | Inconsistencia con CLI | Rechazada |
| Usar hash o UUID | No legible | Rechazada |

**Rationale**: El formato estándar CLI garantiza orden cronológico y compatibilidad con todas las herramientas de Supabase.

## Data Flow

```
Desarrollador (local)
    │
    ├── supabase migration new nombre
    │       └── Crea archivo en supabase/migrations/
    │
    ├── supabase migration up
    │       └── Aplica a BD local
    │
    └── git push
            │
            └── GitHub Actions
                    │
                    ├── supabase db push --linked
                    │       └── Aplica migraciones pendientes
                    │
                    └── Deploy Azure
```

## File Changes

| Archivo | Acción | Descripción |
|---------|--------|-------------|
| `supabase/config.toml` | Crear | Configuración del proyecto CLI |
| `supabase/migrations/` | Crear | Directorio para migraciones |
| `supabase/migrations/20260401120000_create_users_table.sql` | Crear | Renombrado de 001 |
| `supabase/migrations/20260401120001_create_categories_table.sql` | Crear | Renombrado de 002 |
| `supabase/migrations/20260401120002_create_transactions_table.sql` | Crear | Renombrado de 003 |
| `supabase/migrations/20260401120003_create_pdf_imports_table.sql` | Crear | Renombrado de 004 |
| `supabase/migrations/20260401120004_create_user_profile_trigger.sql` | Crear | Renombrado de 005 |
| `supabase/migrations/20260401120005_create_budgets_table.sql` | Crear | Renombrado de 006 |
| `supabase/migrations/20260401120006_system_categories_global_scope.sql` | Crear | Renombrado de 007 |
| `supabase/migrations/20260401120007_revert_system_categories_global_scope.sql` | Crear | Renombrado de 008 |
| `supabase/migrations/20260401120008_sync_existing_auth_users.sql` | Crear | Renombrado de 009 |
| `supabase/migrations/20260401120009_enable_rls_on_subcategories_and_bank_translations.sql` | Crear | Renombrado de 010 |
| `supabase/migrations/20260401120010_add_normalized_name_columns.sql` | Crear | Renombrado de 011 |
| `supabase/migrations/20260401120011_rename_pdf_imports_to_import_batches.sql` | Crear | Renombrado de 012 |
| `src/SauronSheet.Infrastructure/Persistence/Migrations/` | Eliminar | Directorio completo tras migrar |
| `.github/workflows/master_sauronsheet.yml` | Modificar | Agregar paso de migración |
| `.gitignore` | Modificar | Agregar `supabase/.temp/` |
| `README.md` | Modificar | Agregar sección de migraciones |

## Interfaces / Contracts

### Supabase CLI Configuration

```toml
# supabase/config.toml
[project]
id = "zoebndeleapdejmqznif"

[db]
port = 54322
shadow_port = 54320
major_version = 15

[studio]
port = 54323

[auth]
site_url = "http://localhost:3000"
additional_redirect_urls = ["https://localhost:3000"]
jwt_expiry = 3600
```

### GitHub Actions Migration Step

```yaml
- name: Setup Supabase CLI
  uses: supabase/setup-cli@v1
  with:
    version: latest

- name: Run Supabase Migrations
  env:
    SUPABASE_ACCESS_TOKEN: ${{ secrets.SUPABASE_ACCESS_TOKEN }}
    SUPABASE_DB_PASSWORD: ${{ secrets.SUPABASE_DB_PASSWORD }}
  run: |
    supabase link --project-ref zoebndeleapdejmqznif
    supabase db push --linked
```

## Testing Strategy

| Capa | Qué Probar | Enfoque |
|------|------------|---------|
| Unit | Contenido de migraciones SQL | Verificar que cada archivo SQL es válido |
| Integration | Ejecución de migraciones | `supabase migration up` en local |
| E2E | Flujo completo CI/CD | Push a rama feature, verificar GitHub Actions |

## Migration / Rollout

### Fase 1: Preparación
1. Crear rama feature `feature/supabase-cli-migrations`
2. Crear directorio `supabase/` con `config.toml`
3. Renombrar las 12 migraciones al formato CLI
4. Actualizar `.gitignore`
5. Probar localmente con `supabase migration up`

### Fase 2: CI/CD
1. Agregar secrets en GitHub: `SUPABASE_ACCESS_TOKEN`, `SUPABASE_DB_PASSWORD`
2. Modificar workflow de GitHub Actions
3. Probar en rama feature

### Fase 3: Deploy
1. Merge a rama principal
2. Verificar que `supabase db push --linked` marca migraciones como aplicadas
3. Eliminar directorio `src/SauronSheet.Infrastructure/Persistence/Migrations/`
4. Actualizar README

### Rollback
1. Eliminar directorio `supabase/`
2. Restaurar migraciones originales desde git
3. Revertir cambios en `.github/workflows/` y `.gitignore`
4. Las migraciones en producción NO se ven afectadas

## Open Questions

- [ ] ¿Ya existen secrets `SUPABASE_ACCESS_TOKEN` y `SUPABASE_DB_PASSWORD` en GitHub Actions?
- [ ] ¿Hay migraciones manuales recientes en producción que no estén en el repositorio?
- [ ] ¿Se debe crear un paso de verificación después de las migraciones en CI/CD?
