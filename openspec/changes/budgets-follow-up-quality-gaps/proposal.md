# Propuesta: budgets-follow-up-quality-gaps

## Intención

Cerrar las dos advertencias de calidad pendientes tras el archive de `monthly-budgets`:
(1) cobertura del módulo Domain por debajo del umbral del 80 % (línea de base actual: **432/553 líneas = 78,11 %**)
y (2) suite E2E de presupuestos con `skip` sistemáticos por falta de datos del usuario de prueba.
El comportamiento del producto **no cambia**; solo mejora la verificabilidad.

---

## Alcance

### En Alcance
- Tests unitarios mínimos para clases Domain con 0 % de cobertura hasta superar el 80 % verificado con Coverlet
- Helper/fixture Playwright que provisione datos deterministas para el usuario E2E autenticado antes del suite de presupuestos
- Eliminación de los `skip` condicionales de `03-budgets.spec.ts` una vez garantizados esos datos

### Fuera de Alcance
- Cambios en lógica de negocio, modelos de dominio o handlers de Application/Infrastructure
- Migración de base de datos o `supabase/seed.sql` para sembrar datos de prueba
- Nuevas páginas, flujos de UI o endpoints
- Configuración de entornos CI adicionales

---

## Capacidades

### Capacidades Nuevas
Ninguna.

### Capacidades Modificadas
Ninguna — los requisitos y escenarios de `monthly-budgets` no cambian. Este change mejora la cobertura de tests de una capacidad ya especificada, sin alterar su comportamiento observable.

---

## Enfoque

**Slice 1 — Cobertura Domain** (PR 1):
- `DuplicateEntityException` (0/9 líneas) — tests de constructores, mensaje e inner exception
- `BankCategoryTranslation` (0/5 líneas) — test de construcción/equality del record
- Respaldo si el mapeo Coverlet no rinde: `TransactionByMultipleImportedFromsSpecification` (0/5)
- Resultado esperado: ≥ 446/553 líneas (> 80 %) frente al baseline 432/553 (78,11 %)

**Slice 2 — Datos E2E** (PR 2, apila sobre PR 1):
Fixture idempotente en `e2e/fixtures/` que provisione vía flujos UI existentes:
- 2 categorías personales deterministas para el usuario autenticado
- 1 transacción negativa del mes actual ligada a la segunda categoría
- 0 budgets previos sobre esa categoría (para que TC-B02 valide "Sin presupuesto")

> **No se usa migración ni seed.sql**: el frontend de desarrollo apunta al proyecto Supabase alojado y `public.users` depende de `auth.users`; mezclar test data en migraciones contaminaría el entorno real.

---

## Áreas Afectadas

| Área | Impacto | Descripción |
|------|---------|-------------|
| `tests/SauronSheet.Domain.Tests/` | Nuevos archivos | Tests para clases Domain a 0 % |
| `src/SauronSheet.Domain/Exceptions/DuplicateEntityException.cs` | Solo lectura | Clase bajo test, sin modificar |
| `src/SauronSheet.Domain/Repositories/IBankCategoryTranslationRepository.cs` | Solo lectura | Clase bajo test, sin modificar |
| `e2e/fixtures/` | Nuevo archivo | Helper de provisión de datos deterministas |
| `e2e/tests/03-budgets.spec.ts` | Modificado | Eliminar `skip` condicionales; usar el fixture |

---

## Riesgos

| Riesgo | Probabilidad | Mitigación |
|--------|-------------|------------|
| Baseline Coverlet varía si otra clase Domain cambia antes de la slice 1 | Media | Reejecutar cobertura antes de cerrar; ajustar candidatos de respaldo |
| Helper E2E apoya en selectores inestables | Media | Usar exclusivamente rutas y selectores ya presentes en la suite |
| Datos acumulados entre runs provocan duplicados | Baja | El fixture verifica existencia antes de crear; diseño idempotente |

---

## Plan de Rollback

- **Slice 1**: eliminar los ficheros de test añadidos. Sin cambio en código de producción ni en BD.
- **Slice 2**: eliminar `e2e/fixtures/<helper>` y revertir cambios en `03-budgets.spec.ts`. Sin migración que revertir.

---

## Dependencias

El usuario E2E (`TEST_USER_EMAIL` / `TEST_USER_PASSWORD`) debe existir en el proyecto Supabase alojado. No hay dependencias externas adicionales.

---

## Criterios de Éxito

- [ ] `dotnet test tests/SauronSheet.Domain.Tests --collect:"XPlat Code Coverage"` reporta cobertura Domain **≥ 80 %**
- [ ] `npx playwright test --project=chromium e2e/tests/03-budgets.spec.ts` completa **sin ningún `test.skip` activo**
- [ ] `dotnet build` no introduce advertencias nuevas
