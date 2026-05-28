# Tasks: Configuración de Supabase CLI para Migraciones Automáticas

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | 70-80 |
| 400-line budget risk | Low |
| Chained PRs recommended | Yes |
| Suggested split | PR 1 → PR 2 |
| Delivery strategy | force-chained |
| Chain strategy | feature-branch-chain |

Decision needed before apply: No
Chained PRs recommended: Yes
Chain strategy: feature-branch-chain
400-line budget risk: Low

### Suggested Work Units

| Unit | Goal | Likely PR | Notes |
|------|------|-----------|-------|
| 1 | Configurar CLI y renombrar migraciones | PR 1 | base = feature/supabase-cli-migrations; crea config + renombra 12 archivos |
| 2 | Integrar CI/CD y documentar | PR 2 | base = PR 1 branch; workflow + gitignore + README + limpieza |

## Phase 1: Infraestructura CLI

- [ ] 1.1 Crear `supabase/config.toml` con proyecto ID `zoebndeleapdejmqznif`, puertos y configuración local
- [ ] 1.2 Crear directorio `supabase/migrations/` vacío

## Phase 2: Renombrado de Migraciones

- [ ] 2.1 Renombrar `001_CreateUsersTable.sql` → `20260401120000_create_users_table.sql`
- [ ] 2.2 Renombrar `002_CreateCategoriesTable.sql` → `20260401120001_create_categories_table.sql`
- [ ] 2.3 Renombrar `003_CreateTransactionsTable.sql` → `20260401120002_create_transactions_table.sql`
- [ ] 2.4 Renombrar `004_CreatePdfImportsTable.sql` → `20260401120003_create_pdf_imports_table.sql`
- [ ] 2.5 Renombrar `005_AddCategoryIdToTransactions.sql` → `20260401120004_add_category_id_to_transactions.sql`
- [ ] 2.6 Renombrar `006_AddNormalizedCategoryName.sql` → `20260401120005_add_normalized_category_name.sql`
- [ ] 2.7 Renombrar `007_CreateBudgetsTable.sql` → `20260401120006_create_budgets_table.sql`
- [ ] 2.8 Renombrar `008_AddProfileFieldsToUsers.sql` → `20260401120007_add_profile_fields_to_users.sql`
- [ ] 2.9 Renombrar `009_CreateSubcategoriesTables.sql` → `20260401120008_create_subcategories_tables.sql`
- [ ] 2.10 Renombrar `010_CreateBankCategoryTranslationsTable.sql` → `20260401120009_create_bank_category_translations_table.sql`
- [ ] 2.11 Renombrar `011_AddNormalizedNameToSubcategories.sql` → `20260401120010_add_normalized_name_to_subcategories.sql`
- [ ] 2.12 Renombrar `012_RenamePdfImportsToImportBatches.sql` → `20260401120011_rename_pdf_imports_to_import_batches.sql`

## Phase 3: Integración CI/CD

- [ ] 3.1 Agregar `supabase/.temp/` a `.gitignore`
- [ ] 3.2 Modificar `.github/workflows/master_sauronsheet.yml`: agregar paso `Setup Supabase CLI` y `Run Supabase Migrations` antes del deploy
- [ ] 3.3 Configurar secrets `SUPABASE_ACCESS_TOKEN` y `SUPABASE_DB_PASSWORD` en GitHub (manual, documentar en README)

## Phase 4: Limpieza y Documentación

- [ ] 4.1 Eliminar directorio `src/SauronSheet.Infrastructure/Persistence/Migrations/` tras verificar renombrado completo
- [ ] 4.2 Agregar sección de migraciones en `README.md` con comandos `supabase migration new`, `supabase migration up`, `supabase db push --linked`
- [ ] 4.3 Verificar que `supabase migration list` muestra las 12 migraciones como aplicadas en local
