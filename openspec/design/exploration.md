## Exploration: Timezone Bug in Transaction Dates (Producción vs Local)

### Current State

El flujo de fechas en el sistema es el siguiente:

**Importación (Excel ING):**
1. El banco proporciona fecha valor como string `"01/01/2026"` (solo fecha, sin hora)
2. `IngExcelStatementParser` lo pasa como string raw al handler
3. `ImportTransactionsCommandHandler.ParseExact("dd/MM/yyyy")` → `DateTime(2026, 1, 1, 0, 0, 0, Kind.Unspecified)`
4. `Transaction.Date` = mismo DateTime con `Kind.Unspecified`
5. `TransactionRow.FromDomainForInsert()` → `Date = t.Date` (sigue `Kind.Unspecified`)
6. supabase-csharp (PostgREST client) serializa con Newtonsoft.Json:
   - `Kind.Unspecified` → JSON `"2026-01-01T00:00:00"` (sin Z, sin offset)
   - **PERO**: si el PostgREST client usa `DateTimeZoneHandling.Local` (probable), trata `Unspecified` como `Local` y serializa CON offset: `"2026-01-01T00:00:00+01:00"` (en invierno) o `"2026-01-01T00:00:00+02:00"` (en verano)
7. PostgreSQL (TIMESTAMPTZ) recibe `+01:00` y lo almacena como `2025-12-31 23:00:00 UTC`

**Creación manual (web form):**
1. `Add.cshtml.cs`: `DateTime.Today` → `DateTimeKind.Local` (midnight hora local)
2. Mismo problema: serializa con offset local → almacena como XX:00 UTC del día anterior

**Lectura y visualización:**
1. Supabase (timezone='UTC') devuelve timestamptz como UTC: `"2024-12-31T23:00:00+00:00"`
2. Newtonsoft.Json deserializa `+00:00` como `DateTimeKind.Local` (preserva ticks)
3. `TransactionDto.Date` = `DateTime(2024, 12, 31, 23, 0, 0, Kind.Local)`
4. Razor: `@tx.Date.ToString("dd/MM/yyyy")` → `"31/12/2024"` ✗

**Analíticas (sí funcionan):**
Los handlers de analytics usan `t.Date.GetSpainMonth()` que convierte UTC a Spain local antes de agrupar por mes. Por eso los gráficos de analytics NO tienen el bug.

### Evidencia de Datos

De 388 transacciones en producción:
- **163** a las 00:00 UTC (creación manual, E2E fixtures) — correctas
- **172** a las 22:00 UTC (importación en verano CEST, UTC+2) — muestran el día ANTERIOR
- **53** a las 23:00 UTC (importación en invierno CET, UTC+1) — muestran el día ANTERIOR

**225 transacciones (58%) afectadas.** Las transacciones en límite de mes cambian de mes:
- `2024-12-31 23:00:00+00` → UTC: 31/12/2024, Madrid: 01/01/2025
- `2026-04-30 22:00:00+00` → UTC: 30/04/2026, Madrid: 01/05/2026

### Root Cause Analysis

**Causa raíz**: Las transacciones representan fechas de calendario (date-only, sin hora), pero se almacenan en columna `TIMESTAMPTZ`. Durante la importación, el cliente PostgREST serializa las fechas con el offset horario local del servidor (Madrid), lo que desplaza la medianoche a las 22:00/23:00 UTC. Al leer, la UI renderiza la fecha UTC sin convertir a Europe/Madrid.

**Diferencia prod vs local**: 
- Producción: Supabase Cloud con `timezone='UTC'` → PostgREST devuelve timestamps con offset `+00:00` → Newtonsoft.Json deserializa como `Kind.Local` con ticks de UTC
- Local: El Supabase local (Docker) puede tener `timezone='Europe/Madrid'` → PostgREST devuelve timestamps con offset `+01:00/+02:00` → Newtonsoft.Json deserializa como `Kind.Local` con ticks de Madrid → la fecha "parece" correcta

**Por qué analytics funciona**: `GetSpainMonth()` (en `SpainDateTime.cs`) llama a `ToSpainLocal()` que convierte `DateTimeKind.Utc` a Spain. Pero la visualización normal (`TransactionDto.Date`) no usa esta conversión.

**Cascada de archivos afectados:**
1. `ImportTransactionsCommandHandler.cs` — fecha `Kind.Unspecified` sin normalizar
2. `CreateTransactionCommandHandler.cs` — fecha `request.Date` sin normalizar
3. `TransactionRow.FromDomain/FromDomainForInsert` — pasa `t.Date` sin fijar Kind
4. `TransactionRow.ToDomain()` — pasa `Date` sin convertir
5. `GetTransactionsQueryHandler.cs` — `t.Date` directo al DTO
6. `GetRecentTransactionsQueryHandler.cs` — `t.Date` directo al DTO
7. `TransactionDto.Date` — DateTime sin indicación de timezone
8. 5 archivos `.cshtml` — `@tx.Date.ToString("dd/MM/yyyy")` sin conversión a Spain
9. `SpainDateTime.cs` — `ToSpainLocal()` no cubre `Kind.Local` (necesita convertir a Spain siempre)

### Affected Areas

| Archivo | Rol en el bug |
|---------|---------------|
| `src/.../ImportTransactionsCommandHandler.cs:154-167` | Crea `DateTime` con `Kind.Unspecified` desde `ParseExact` |
| `src/.../CreateTransactionCommandHandler.cs:57` | Pasa `request.Date` sin normalizar |
| `src/.../SupabaseTransactionRepository.cs:83-96,123-141,149-169` | `ToDomain()`/`FromDomain()`/`FromDomainForInsert()` no fijan `DateTimeKind.Utc` |
| `src/.../TransactionDto.cs:13` | `DateTime Date` sin conversión |
| `src/.../GetTransactionsQueryHandler.cs:110` | `t.Date` directo al DTO |
| `src/.../GetRecentTransactionsQueryHandler.cs:66` | `t.Date` directo al DTO |
| `src/.../Analytics/Queries/*.cs` | Usan `GetSpainMonth()` → OK (no afectados) |
| `src/.../SpainDateTime.cs:29-31` | `ToSpainLocal()` no maneja `Kind.Local` correctamente |
| `src/.../Pages/Transactions/Index.cshtml:234` | `@transaction.Date.ToString("dd/MM/yyyy")` |
| `src/.../Pages/Transactions/Search.cshtml:96` | `@tx.Date.ToString("dd/MM/yyyy")` |
| `src/.../Pages/Dashboard.cshtml:210` | `@tx.Date.ToString("dd/MM/yyyy")` |
| `src/.../Pages/Transactions/Index.cshtml.cs:35-38` | `StartDate`/`EndDate` bindings (sin timezone) |
| `src/.../Pages/Transactions/Add.cshtml.cs:112` | `DateTime.Today` → `Kind.Local` |
| `supabase/migrations/20260101000003_...sql:9` | Columna `date TIMESTAMPTZ NOT NULL` |

### Approaches

1. **Normalizar fechas a UTC midnight en el pipeline de importación/creación**
   - Fijar `DateTimeKind.Utc` después de parsear en ambos handlers
   - Hacer lo mismo en `TransactionRow.FromDomain/FromDomainForInsert`
   - Esto hace que las NUEVAS transacciones se almacenen como `00:00 UTC`
   - **No corrige datos existentes** — requieren migración de datos
   - Pros: Solución definitiva para datos nuevos; sin cambios de schema
   - Cons: No corrige las 225 transacciones existentes; requiere migración de datos
   - Esfuerzo: Bajo

2. **Convertir a Spain timezone en la capa de presentación (TransactionDto → View)**
   - Aplicar `Date.ToSpainLocal()` en todos los query handlers al construir TransactionDto
   - Arreglar `SpainDateTime.ToSpainLocal()` para manejar `Kind.Local` correctamente
   - Pros: Corrige TODOS los datos (existentes y nuevos) inmediatamente
   - Cons: Solución parcial — los datos en DB siguen siendo inconsistentes; costo de conversión en cada request
   - Esfuerzo: Bajo-Medio

3. **Enfoque híbrido: normalizar a UTC + convertir a Spain en display**
   - **Fase 1**: Normalizar a UTC midnight en importación/creación (approach 1)
   - **Fase 2**: Convertir a Spain timezone en los query handlers (approach 2)
   - **Fase 3**: Migración de datos existentes: `UPDATE transactions SET date = date AT TIME ZONE 'Europe/Madrid' WHERE EXTRACT(HOUR FROM date) IN (22, 23);`
   - **Fase 4**: Cambiar schema de `TIMESTAMPTZ` a `DATE` o mantener TIMESTAMPTZ con convención UTC midnight
   - Pros: Solución completa y definitiva; datos consistentes en DB y UI
   - Cons: Mayor esfuerzo; requiere migración de datos; riesgo de romper analytics temporalmente
   - Esfuerzo: Medio

4. **Cambiar columna a `DATE` en lugar de `TIMESTAMPTZ`**
   - Migración: `ALTER TABLE transactions ALTER COLUMN date TYPE DATE USING date AT TIME ZONE 'Europe/Madrid'`
   - Elimina toda ambigüedad de timezone
   - Pros: Solución más limpia; tipo de dato correcto para fechas de calendario
   - Cons: Migración de datos delicada; requiere actualizar el modelo TransactionRow; puede romper queries existentes que dependan de TIMESTAMPTZ
   - Esfuerzo: Alto

### Recommendation

**Recomiendo el enfoque 3 (híbrido)**:

**Fase 1 (inmediata, bajo riesgo)**: Normalizar a UTC midnight y convertir a Spain en display simultáneamente:
- En `ImportTransactionsCommandHandler.cs`: después de `ParseExact`, hacer `date = DateTime.SpecifyKind(date, DateTimeKind.Utc)`
- En `Add.cshtml.cs`: `DateTime.SpecifyKind(Date, DateTimeKind.Utc)` 
- En `TransactionRow.FromDomainForInsert()`: `Date = DateTime.SpecifyKind(t.Date, DateTimeKind.Utc)`
- En todos los QueryHandlers de transacciones: `t.Date.ToSpainLocal()` al construir TransactionDto
- Arreglar `SpainDateTime.ToSpainLocal()`: aplicar conversion SIEMPRE, no solo para Kind.Utc

**Fase 2 (posterior)**: Migración de datos existentes:
```sql
UPDATE public.transactions 
SET date = date AT TIME ZONE 'Europe/Madrid' 
WHERE EXTRACT(HOUR FROM date) IN (22, 23);
```

Esto normaliza las 225 transacciones a midnight UTC y elimina la necesidad de la conversión en display (aunque es bueno mantenerla como defensa).

### Risks

- **Riesgo de datos**: La migración SQL de datos existentes podría afectar analytics temporalmente si no se coordina con el cambio de código
- **Riesgo de regresión en analytics**: Los handlers de analytics ya usan `GetSpainMonth()` correctamente, pero si se cambia la hora de los datos existentes, los grupos mensuales podrían cambiar ligeramente. **Verificar**: `GetSpainMonth()` primero convierte a Spain local antes de obtener el mes. Si los datos pasan de 23:00 UTC → 00:00 UTC, `GetSpainMonth()` devolvería el mismo resultado porque `00:00 UTC` sigue siendo `01:00/02:00 Madrid` → mismo mes. Debería ser seguro.
- **Riesgo de duplicados**: `ExistsDuplicateAsync` compara `r.Date.Date == date.Date` — cambiar datos existentes podría afectar detección de duplicados para futuras importaciones
- **Riesgo de timezone del servidor**: Si el servidor C# se despliega con timezone distinto a Europe/Madrid, `DateTimeKind.Local` se comportaría diferente

### Ready for Proposal

**Sí** — el análisis es completo. El orchestrator debe proceder con SDD Proposal.

### Resumen Ejecutivo

225 de 388 transacciones (58%) almacenan su fecha como medianoche Spain time convertida a UTC (22:00/23:00 UTC en lugar de 00:00 UTC). La UI renderiza la fecha UTC sin convertir a Europe/Madrid, mostrando el día anterior. Las analíticas NO tienen el bug porque usan `GetSpainMonth()` que hace la conversión correctamente. 

La solución híbrida (normalizar a UTC + convertir en display) corrige tanto los datos existentes como los nuevos sin cambiar el schema de base de datos.
