# Tasks: Fix timezone bug en fechas de transacciones

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | ~200-240 |
| 400-line budget risk | Low |
| Chained PRs recommended | No |
| Suggested split | Single PR |
| Delivery strategy | force-chained |
| Chain strategy | stacked-to-main |

Decision needed before apply: No
Chained PRs recommended: No
Chain strategy: stacked-to-main
400-line budget risk: Low

## Phase 1: Core — SpainDateTime.ToSpainLocal()

- [x] 1.1 [RED] Escribir test `ToSpainLocal_GivenUtc_ConvertsToSpain` en Application.Tests
- [x] 1.2 [RED] Escribir test `ToSpainLocal_GivenUnspecified_ConvertsToSpain`
- [x] 1.3 [RED] Escribir test `ToSpainLocal_GivenLocal_ConvertsToSpain`
- [x] 1.4 [GREEN] Corregir `SpainDateTime.ToSpainLocal()` en `src/.../Helpers/SpainDateTime.cs` para manejar todos los DateTimeKind

## Phase 2: Handler — UTC normalization

- [x] 2.1 [RED] Escribir test para `ImportTransactionsCommandHandler` — verifica que date se normaliza a UTC tras ParseExact
- [x] 2.2 [RED] Escribir test para `CreateTransactionCommandHandler` — verifica que request.Date se normaliza a UTC
- [x] 2.3 [GREEN] Añadir `DateTime.SpecifyKind(date, DateTimeKind.Utc)` en `ImportTransactionsCommandHandler.cs` tras ParseExact
- [x] 2.4 [GREEN] Añadir `DateTime.SpecifyKind(request.Date, DateTimeKind.Utc)` en `CreateTransactionCommandHandler.cs`
- [x] 2.5 [GREEN] Cambiar `DateTime.Today` por `DateTime.UtcNow.Date` en `Add.cshtml.cs`

## Phase 3: Repository — Date Kind enforcement

- [x] 3.1 [RED] Escribir test para `TransactionRow.ToDomain` — verifica Date se interpreta como UTC desde TIMESTAMPTZ
- [x] 3.2 [RED] Escribir test para `TransactionRow.FromDomain`/`FromDomainForInsert` — verifica Date se serializa como UTC
- [x] 3.3 [GREEN] Asegurar `DateTime.SpecifyKind(row.Date, DateTimeKind.Utc)` en `ToDomain()` y `SpecifyKind(t.Date, DateTimeKind.Utc)` en `FromDomain()`/`FromDomainForInsert()` de `SupabaseTransactionRepository.cs`

## Phase 4: Query handlers — ToSpainLocal en DTOs

- [x] 4.1 [RED] Escribir test para `GetTransactionsQueryHandler` — verifica que Date en DTO se convierte a Spain
- [x] 4.2 [RED] Escribir test para `GetRecentTransactionsQueryHandler` — verifica que Date en DTO se convierte a Spain
- [x] 4.3 [GREEN] Aplicar `.ToSpainLocal()` a `t.Date` en mapeo DTO de `GetTransactionsQueryHandler.cs`
- [x] 4.4 [GREEN] Aplicar `.ToSpainLocal()` a `t.Date` en mapeo DTO de `GetRecentTransactionsQueryHandler.cs`

## Phase 5: UI — Display fix

- [x] 5.1 Cambiar a `.ToSpainLocal().ToString("dd/MM/yyyy")` en `Dashboard.cshtml` (línea 210)
- [x] 5.2 Ídem en `Transactions/Index.cshtml` (línea 234)
- [x] 5.3 Ídem en `Transactions/Search.cshtml` (línea 96)

## Phase 6: Data migration — Existing transactions

- [x] 6.1 Crear `supabase/migrations/20260625120000_fix_timezone_transactions.sql` con `UPDATE transactions SET date = date AT TIME ZONE 'Europe/Madrid' WHERE EXTRACT(HOUR FROM date) IN (22, 23);`
- [x] 6.2 Ejecutar `dotnet test` y verificar 0 fallos
