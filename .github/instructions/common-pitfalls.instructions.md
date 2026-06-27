---
description: "Use when debugging, fixing code, or working with Supabase/Postgrest, PDF parsing, or common SauronSheet anti-patterns."
---

# Common Pitfalls & Lessons Learned

## Architecture & Code

- ❌ Application referencing Infrastructure directly (use Domain interfaces).
- ❌ Domain logic in MediatR handlers (handlers are thin orchestrators).
- ❌ Mixing query/command logic (separate concerns strictly).
- ❌ Supabase client leaking into Application layer.
- ❌ Raw Guid or string for entity IDs (MUST use strong-typed value objects).
- ❌ Public setters on domain entities (use parameterized constructors).
- ❌ Never put `_ViewImports.cshtml` in `Shared/` — only in `Pages/` (breaks Tag Helpers).
- ❌ Never use `data-mdb-button-init` on `<button type="submit">` — breaks form submission in MDBootstrap v9+.
- ❌ Never call `new mdb.Input(el)` on a raw `<input>` without `.form-outline` wrapper — causes `Cannot read properties of null (reading 'classList')`. Only use `mdb.Input()` inside `.form-outline` divs with sibling `<label>`.
- ❌ Never use `<input type="date">` — use Flatpickr (`type="text"` + `x-init`). Flatpickr hides the original input and uses a hidden `<input type="hidden">` for the value. E2E tests MUST use `page.evaluate()` + Flatpickr API to set date values, not `page.fill()`.
- ❌ Never reference local CSS, JS, or image assets with hardcoded `/css/...`, `/js/...`, or `/img/...` paths in Razor. Use `~/...` + `asp-append-version="true"` to prevent stale-cache drift between local and production.
- ❌ Never embed Razor data in JS via `@Html.Raw(Json.Serialize(model))` (in `on*` attributes or inside `<script>` blocks) or via `@Html.Encode(value)` inside an HTML attribute. Both produce double-encoding / XSS / attribute-breakage bugs. Use `data-*` + a delegated listener for per-item data, or `<script type="application/json">` + `JSON.parse` for larger payloads. Full rationale and patterns in [`docs/adr/0002-safe-json-data-passing.md`](docs/adr/0002-safe-json-data-passing.md).

---

## Supabase/Postgrest C# Client

### PGRST205: Table Not Found in Schema Cache

If you see `PGRST205: Could not find the table 'public.XXX' in the schema cache`, the table does not exist or the migration was not applied.
**Solución**: Check `supabase/migrations/` for the correct migration file. Apply it with `supabase db push --linked` (production) or `supabase migration up` (local). Do NOT create tables manually via SQL Editor — always use migrations.

### OR Conditions NOT Supported

El cliente Postgrest C# (supabase-csharp 0.16.2) no soporta OR dentro de `.Where()`.
**Solución**: dos consultas separadas y combinar en memoria.

### Method Calls in .Where() Lambda NOT Supported (CRITICAL)

```csharp
// ❌ INCORRECTO — method call inside lambda
await _client.From<TransactionRow>()
    .Where(x => x.Id == id.Value.ToString())
    .Delete();

// ✅ CORRECTO — convert outside
var idString = id.Value.ToString();
await _client.From<TransactionRow>()
    .Where(x => x.Id == idString)
    .Delete();
```

---

## PDF Parser: Dual-Format Number Normalization

Los PDFs bancarios usan formato europeo (coma decimal) o anglo (punto decimal).
Ver `Infrastructure/PDF/Parsers/` para la lógica de normalización.
