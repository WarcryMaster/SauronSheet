---
description: "Use when writing, reviewing, or refactoring Razor Pages (.cshtml, .cshtml.cs) or frontend JavaScript. Covers MDBootstrap usage, PageModel patterns, form validation, and frontend-specific rules for SauronSheet."
applyTo: "**/*.cshtml"
---

# Razor Pages & Frontend Rules

Rules for all `.cshtml` and PageModel files in SauronSheet.

---

## PageModel Pattern

```csharp
// ✅ Correct PageModel
public class DashboardModel(IMediator mediator) : PageModel
{
    public SpendingByCategoryDto SpendingData { get; set; } = default!;

    public async Task OnGetAsync()
    {
        string? userId = User.FindFirst("sub")?.Value;
        SpendingData = await mediator.Send(new GetSpendingByCategoryQuery(userId!));
    }
}
```

- Use **primary constructors** for injecting services.
- Never inject services via `[FromServices]` attribute in handlers.
- All state-changing operations go through `OnPostAsync` with `[ValidateAntiForgeryToken]` on the **class**, not on individual handlers.
- Read `userId` from `User.FindFirst("sub")?.Value` — never from form inputs.

---

## MDBootstrap v9.2.0 (CRITICAL)

SauronSheet uses **MDBootstrap (mdb-ui-kit) v9.2.0 via Cloudflare CDN**, NOT standard Bootstrap.

| ❌ Wrong (Bootstrap) | ✅ Correct (MDBootstrap) |
|---|---|
| `new bootstrap.Modal(el)` | `new mdb.Modal(el)` |
| `bootstrap.Modal.getInstance(el)` | `mdb.Modal.getInstance(el)` |
| `bootstrap.Tooltip(el)` | `new mdb.Tooltip(el)` |

**CDN (always use these exact URLs):**
```html
<link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/mdb-ui-kit/9.2.0/mdb.min.css" />
<script src="https://cdnjs.cloudflare.com/ajax/libs/mdb-ui-kit/9.2.0/mdb.umd.min.js"></script>
```

**Submit buttons:**
```html
<!-- ✅ Safe submit button -->
<button type="submit" data-mdb-ripple-init class="btn btn-primary">Save</button>

<!-- ❌ NEVER use data-mdb-button-init on submit — breaks form submission -->
<button type="submit" data-mdb-button-init>Save</button>
```

**`new mdb.Input()` — ONLY for `.form-outline` wrappers:**
```html
<!-- ✅ Correct: MDB floating input inside .form-outline -->
<div class="form-outline">
  <input type="text" id="myInput" class="form-control" />
  <label class="form-label" for="myInput">Label</label>
</div>
<script>
  // Safe: element is .form-outline, MDB finds <input> inside
  new mdb.Input(document.getElementById('myInput'));
</script>

<!-- ❌ WRONG: calling mdb.Input on a raw <input> without .form-outline wrapper -->
<input type="email" id="loginEmail" class="form-control" />
<script>
  // CRASH: this.input is null → "Cannot read properties of null (reading 'classList')"
  new mdb.Input(document.getElementById('loginEmail'));
</script>
```
**Rule:** Only call `new mdb.Input(el)` when the `<input>` is inside a `.form-outline` div with a sibling `<label>`. Plain `<label for="...">` + `<input class="form-control">` does NOT need `mdb.Input()` — it works out of the box.

**Flatpickr — replaces `<input type="date">`:**
- All date inputs use `type="text"` + `x-init="flatpickr($el, {...})"` — never `type="date"`.
- The actual value lives in a **hidden `<input type="hidden">`** managed by Flatpickr.
- **E2E test impact:** Playwright `page.fill('#Date', value)` fails on hidden inputs. Use `page.evaluate()` to set the value via the Flatpickr API:
  ```typescript
  // ❌ Fails: #Date is hidden (Flatpickr manages it)
  await page.fill('#Date', '2026-06-01');

  // ✅ Correct: set value via Flatpickr API
  await page.evaluate(() => {
    const el = document.getElementById('Date') as HTMLInputElement;
    const fp = (el as any)._flatpickr;
    fp.setDate('2026-06-01', true);
  });
  ```

**`<template x-for>` inside `<select>` — BROKEN in browsers:**

Browsers move `<template>` elements out of `<select>` during DOM parsing, so Alpine.js `x-for` never renders `<option>` elements inside a `<select>`. The `<select>` remains empty (only the placeholder `<option>`).

- ❌ `<template x-for>` inside `<select>` — silently fails, zero console errors, options never render.
- ✅ Use a `rebuildOptions()` method that manipulates the DOM directly (`document.createElement("option")` + `sel.appendChild()`), called from `x-effect` when the filter data changes. See `Budgets/Create.cshtml` for the working pattern.

```html
<!-- ❌ WRONG: template x-for inside select — browsers break this -->
<select x-model="categoryId">
  <option value="">-- Select --</option>
  <template x-for="cat in filteredCategories" :key="cat.id">
    <option :value="cat.id" x-text="cat.name"></option>
  </template>
</select>

<!-- ✅ CORRECT: rebuildOptions() + x-effect -->
<form x-data="{ budgetType: '', categories: [...], get filteredCategories() { ... } }"
      x-effect="rebuildOptions()">
  <select x-model="categoryId" id="CategoryId">
    <option value="">-- Select --</option>
  </select>
</form>
<script>
// Inside x-data: rebuildOptions() { const sel = document.getElementById('CategoryId'); while (sel.options.length > 1) sel.remove(1); this.filteredCategories.forEach(c => { const o = document.createElement('option'); o.value = c.id; o.textContent = c.name; sel.appendChild(o); }); }
</script>
```

---

## Form & Antiforgery

- `[ValidateAntiForgeryToken]` goes on the **PageModel class**, not on individual `OnPost*` handlers (MVC1001 warning).
- Always include `@Html.AntiForgeryToken()` or use `<form asp-page="...">` tag helpers (auto-included).
- Never put `_ViewImports.cshtml` in `Shared/`; only in `Pages/` — adding it to Shared breaks Tag Helpers.

---

## JavaScript Quality (Frontend Scripts)

- **No `var`** — use `const` or `let`.
- **Strict null checks** before accessing DOM elements.
- **Event listeners** preferred over `onclick` attributes.
- **Fetch API** for AJAX calls; always handle errors.
- Never trust client-side data for security decisions; re-validate server-side.

```javascript
// ✅
const modal = new mdb.Modal(document.getElementById('confirmModal'));
document.getElementById('deleteBtn')?.addEventListener('click', () => modal.show());

// ❌
var modal = new bootstrap.Modal($('#confirmModal'));
```

---

## Charts

All Chart.js instances in SauronSheet are managed by `wwwroot/js/charts.js`. When adding or modifying charts, follow these rules:

### Legend Ordering Contract

**Legend order MUST match dataset order.** The backend handler is the single source of truth for dataset ordering. The frontend MUST NOT reorder, sort, or deduplicate datasets — it MUST preserve the array order received from the server.

- Chart.js renders the legend in dataset insertion order.
- Handlers MUST sort categories by total amount descending (ties broken by name ascending) before returning data.
- `initCategoryStackedChart` and `initMonthlyTrendsChart` iterate categories in the exact order they appear in the payload — no frontend sort.

### Extending `charts.js`

When adding a new chart type:

1. Add the init function to `charts.js` (not inline in `.cshtml`).
2. Document the data contract with JSDoc, including any PRECONDITION on sort order.
3. Use `destroyAllCharts()` on `htmx:beforeSwap` to prevent memory leaks (already wired in `_Layout.cshtml`).
4. Resolve colors from CSS custom properties via `cssVar()` — see `DESIGN.md` for the design token palette.
5. Follow the existing pattern: guard against null canvas, destroy existing instance before recreating.

### Color Tokens

All chart colors come from CSS custom properties defined in `DESIGN.md`. The `tokens` object in `charts.js` maps semantic names (`brand`, `success`, `danger`, `warning`, `info`) to computed CSS values. Never hardcode hex colors in chart config — use `tokens.*` or `designColors[]` for category palettes.

### JSDoc Contract Reference

Every init function in `charts.js` MUST include:
- A `@param` tag describing the expected data shape.
- A `PRECONDITION` comment when the function depends on server-side sort order.
- A note that the function MUST NOT reorder datasets.

See `initCategoryStackedChart` and `initMonthlyTrendsChart` for reference implementations.

---

## _ViewImports.cshtml

Only one `_ViewImports.cshtml` is allowed, located in `Pages/`. It must include:

```cshtml
@using SauronSheet.Frontend
@namespace SauronSheet.Frontend.Pages
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
```

**Never** add a second `_ViewImports.cshtml` in `Pages/Shared/` — this silently breaks Tag Helpers and causes form POST submissions to fail.

---

## Alpine.js Mandatory Patterns

### FORBIDDEN — NEVER USE:
- ❌ `onclick`, `onchange`, `onsubmit` inline attributes → use Alpine.js `x-on:` / `@@event` directives
- ❌ `DOMContentLoaded` for initialization → use Alpine.js `x-init` or HTMX lifecycle events (`htmx:afterSwap`)
- ❌ `<script>` blocks with vanilla JS for page interactivity → use Alpine.js `x-data` components
- ❌ `document.getElementById()` for state reading → use Alpine.js `x-data` properties and `$refs`
- ❌ `addEventListener()` for UI events → use `@@click`, `@@change`, `@@submit`, `@@input`, `@@keydown`
- ❌ `classList.add/remove('d-none')` for visibility → use `x-show` + `x-transition`
- ❌ `.textContent = '...'` for dynamic text → use `x-text` or `x-html`
- ❌ `.disabled = true/false` for buttons → use `:disabled` + reactive state
- ❌ `.style.backgroundColor = '...'` → use `:style` binding
- ❌ Separate `.js` files for page-specific logic → embed in Alpine.js `x-data`
- ❌ `d-flex`, `d-block`, `d-grid` (MDB classes with `!important`) on elements with `x-show` → wrap in inner div
- ❌ `@Json.Serialize()` inside double-quoted HTML attributes → use single-quoted attributes (`x-data='...'`) and double-quoted JS strings
- ❌ `@(decimal)` without invariant culture inside JavaScript → use `.ToString(System.Globalization.CultureInfo.InvariantCulture)`
- ❌ `<input type="date">` — use Flatpickr via Alpine.js `x-init` instead
- ❌ `<template x-for>` inside `<select>` — browsers move `<template>` out of `<select>` in the DOM, breaking Alpine.js. Use `rebuildOptions()` + `x-effect` to rebuild `<option>` elements via DOM manipulation instead (see Budgets/Create.cshtml for the pattern)

### REQUIRED — ALWAYS USE:
- ✅ `x-data` on every interactive component (forms, filters, modals, toolbars)
- ✅ `x-show` + `x-transition` for any conditional visibility
- ✅ `x-model` for all form inputs (text, select, checkbox, radio, color)
- ✅ `:disabled` + spinner on ALL submit buttons with `x-data="{ loading: false }"`
- ✅ `@@submit="loading = true"` on all forms
- ✅ `x-text` for dynamic text content
- ✅ `:class` for conditional CSS classes
- ✅ `$refs` for DOM element references when unavoidable
- ✅ MDB modals via `new mdb.Modal(el).show()` from Alpine.js methods (DO NOT use `data-mdb-toggle` on buttons, call `show()` explicitly)
- ✅ `htmx:beforeSwap` → `destroyAllCharts()` for any page with Chart.js + HTMX
- ✅ Flatpickr via `x-data x-init="flatpickr($el, { dateFormat: 'Y-m-d', altInput: true, altFormat: 'd/m/Y', allowInput: true })"` for ALL date inputs. Never use `type="date"`.
- ✅ `<template x-for>` inside `<select>` — browsers move `<template>` out of `<select>` in the DOM, breaking Alpine.js. Use `rebuildOptions()` + `x-effect` to rebuild `<option>` elements via DOM manipulation instead (see Budgets/Create.cshtml for the pattern)

---

## Cross-Attribute Compatibility

- **CRITICAL: MDB uses `data-mdb-*` attributes, NOT `data-bs-*` (Bootstrap).**
  - `data-mdb-toggle="dropdown"` (not `data-bs-toggle`)
  - `data-mdb-target="#modal"` (not `data-bs-target`)
  - `data-mdb-dismiss="modal"` (not `data-bs-dismiss`)
  - `data-mdb-auto-close="outside"` (not `data-bs-auto-close`)
  - `data-mdb-ripple-init` (not `data-bs-ripple`)
  - Alpine.js uses its own attributes: `x-data`, `x-show`, `x-on`, `x-model`, `x-bind` — these do NOT conflict with `data-mdb-*`
  - HTMX uses its own attributes: `hx-get`, `hx-target`, `hx-swap` — these do NOT conflict with `data-mdb-*`
- **DESIGN.md is the visual source of truth for all UI work.** Read `DESIGN.md` BEFORE changing any `.cshtml`, CSS, component layout, spacing, or interaction pattern.
- When delegating frontend/UI work to any sub-agent, ALWAYS include `DESIGN.md` and the Frontend Rules section of this file in the prompt/context.
- Keep JavaScript modern: `const` / `let`, event listeners, null checks, and server-side revalidation.
- Utilizar la metodología https://github.com/voltagent/awesome-design-md para la generación de interfaces. Todas las interfaces frontend deben crearse siguiendo esta metodología.
