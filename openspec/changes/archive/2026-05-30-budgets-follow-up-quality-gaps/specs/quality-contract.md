# Contrato de Calidad: budgets-follow-up-quality-gaps

## Declaración de no cambio

`openspec/specs/monthly-budgets/spec.md` **no se modifica** en este change.
Todos sus requisitos y escenarios permanecen vigentes sin alteración.
Este documento captura únicamente los requisitos de verificabilidad propios de este follow-up.

---

## Requisitos

### Requisito: Cobertura de líneas Domain ≥ 80 %

El módulo `SauronSheet.Domain` DEBE alcanzar una cobertura de líneas medida por
Coverlet igual o superior al **80 %** al ejecutar `SauronSheet.Domain.Tests`.

| Clase bajo test                                    | Líneas base | Objetivo |
|----------------------------------------------------|-------------|----------|
| `DuplicateEntityException`                         | 0 / 9       | 100 %    |
| `BankCategoryTranslation`                          | 0 / 5       | 100 %    |
| Respaldo: `TransactionByMultipleImportedFromsSpec` | 0 / 5       | 100 %    |

#### Escenario: Umbral superado tras los nuevos tests unitarios

- DADO el baseline de 432/553 líneas (78,11 %)
- CUANDO se añaden tests para `DuplicateEntityException` y `BankCategoryTranslation`
- ENTONCES `dotnet test --collect:"XPlat Code Coverage"` reporta ≥ 446/553 líneas (> 80 %)

---

### Requisito: Suite E2E de presupuestos sin skips activos

`e2e/tests/03-budgets.spec.ts` NO DEBE contener ningún `test.skip` activo
al ejecutarse contra datos deterministas provisionados por el fixture.

#### Escenario: Fixture provisiona datos antes de la suite

- DADO que el usuario E2E (`TEST_USER_EMAIL`) está autenticado
- CUANDO el fixture idempotente en `e2e/fixtures/` ha provisionado:
  - 2 categorías personales deterministas
  - 1 transacción negativa del mes actual ligada a la segunda categoría
  - 0 budgets sobre esa categoría (garantiza escenario "Sin presupuesto")
- ENTONCES `npx playwright test --project=chromium e2e/tests/03-budgets.spec.ts`
  completa sin ningún test marcado como skipped

#### Escenario: Idempotencia ante ejecuciones repetidas

- DADO que el fixture ya ejecutó al menos una vez para el mismo usuario
- CUANDO se ejecuta de nuevo sin limpiar datos manualmente
- ENTONCES el fixture detecta los datos existentes y NO crea duplicados;
  la suite E2E completa sin errores ni skips

---

### Requisito: Build limpio tras los cambios

`dotnet build` DEBE completar sin advertencias nuevas tras la adición de
los archivos de test (Slice 1) y los cambios en `03-budgets.spec.ts` (Slice 2).

#### Escenario: Sin regresión de build

- DADO el estado actual del repositorio sin advertencias de build
- CUANDO se añaden los archivos de las dos slices
- ENTONCES `dotnet build` finaliza con cero advertencias nuevas

---

## Restricciones de implementación

| Restricción | Razón |
|-------------|-------|
| No usar migración de BD ni `supabase/seed.sql` para datos E2E | Evita contaminar el proyecto Supabase alojado |
| El fixture DEBE provisionar vía flujos UI existentes | Garantiza que los paths de la app son ejercitados, no solo datos en BD |
| No modificar código de producción ni modelos de dominio | Este change es solo tests; el comportamiento del producto no cambia |

---

## Fuera del alcance de este spec

Requisitos de comportamiento de `monthly-budgets`, nuevas páginas o endpoints,
configuración CI adicional, migración de base de datos.
