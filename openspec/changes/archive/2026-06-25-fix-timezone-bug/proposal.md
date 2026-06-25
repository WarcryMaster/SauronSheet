# Propuesta: Fix timezone bug en fechas de transacciones

## Intención

225 de 388 transacciones (58%) muestran la fecha incorrecta en la UI porque `DateTimeKind.Unspecified` se serializa con offset local (Europe/Madrid) al almacenarse en TIMESTAMPTZ. La UI renderiza el valor UTC sin convertir, mostrando el día anterior. Las analíticas **no** están afectadas porque usan `GetSpainMonth()` correctamente.

## Alcance

### Incluye
- Normalizar fechas a UTC midnight en pipelines de importación y creación manual
- Convertir a Europe/Madrid en query handlers (capa de presentación)
- Corregir `SpainDateTime.ToSpainLocal()` para manejar todos los `DateTimeKind`
- Migración SQL para las 225 transacciones existentes

### Excluye
- Cambio de schema (TIMESTAMPTZ → DATE) — se mantiene TIMESTAMPTZ por consistencia
- Refactor de analíticas — no tienen el bug
- Cambios en contratos de repositorio o entidades de dominio

## Capacidades

### Nuevas Capacidades
None — es una corrección de bug, no introduce nuevas capacidades.

### Capacidades Modificadas
None — el comportamiento externo (fecha mostrada al usuario) cambia pero ninguna spec existente prescribe el comportamiento horario actual.

## Enfoque

**Fase 1 (código):** Normalizar a UTC en importación y creación (`DateTime.SpecifyKind(date, DateTimeKind.Utc)`). Convertir a Spain en todos los query handlers (`t.Date.ToSpainLocal()`). Corregir `ToSpainLocal()` para aplicar la conversión siempre, no solo para `Kind.Utc`.

**Fase 2 (datos):** Migración SQL: `UPDATE transactions SET date = date AT TIME ZONE 'Europe/Madrid' WHERE EXTRACT(HOUR FROM date) IN (22, 23);`

## Áreas Afectadas

| Área | Impacto | Descripción |
|------|---------|-------------|
| `ImportTransactionsCommandHandler.cs` | Modificado | Normalizar a UTC tras ParseExact |
| `CreateTransactionCommandHandler.cs` | Modificado | Normalizar a UTC desde request.Date |
| `SupabaseTransactionRepository.cs` | Modificado | Fix en FromDomain/FromDomainForInsert/ToDomain |
| `TransactionDto.cs` | Modificado | Sin cambios de schema, solo uso |
| `GetTransactionsQueryHandler.cs` | Modificado | Aplicar ToSpainLocal() |
| `GetRecentTransactionsQueryHandler.cs` | Modificado | Aplicar ToSpainLocal() |
| `SpainDateTime.cs` | Modificado | ToSpainLocal() para todos los Kind |
| 5 vistas `.cshtml` | Modificado | ToString con fecha Spain |
| `Pages/Transactions/Add.cshtml.cs` | Modificado | DateTime.Today → Utc |
| `supabase/migrations/` | Nuevo | Migración de datos existentes |

## Riesgos

| Riesgo | Probabilidad | Mitigación |
|--------|-------------|------------|
| Analytics cambian agrupación mensual | Baja | `GetSpainMonth()` convierte a Spain antes del mes; datos a 00:00 UTC dan mismo resultado |
| Duplicados en futuras importaciones | Baja | `ExistsDuplicateAsync` compara `.Date == date.Date`; datos normalizados a 00:00 UTC son consistentes |
| Timezone del servidor C# distinto a Madrid | Baja | `ToSpainLocal()` usa `TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid")` — explícito, no depende del servidor |

## Rollback

`git revert` del PR único. La migración SQL tiene `ROLLBACK` en la misma transacción. Sin cambio de schema irreversible.

## Dependencias

- Ninguna externa. El schema actual (`TIMESTAMPTZ`) no cambia.

## Criterios de Éxito

- [ ] Transacciones nuevas se almacenan como 00:00 UTC
- [ ] UI muestra fecha correcta (Europe/Madrid) para todas las transacciones
- [ ] `dotnet test` pasa sin fallos
- [ ] Las 225 transacciones existentes corregidas tras migración
