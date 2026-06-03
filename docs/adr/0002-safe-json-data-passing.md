# ADR 0002 — Pass JSON and per-item data from Razor to JS without `Html.Raw` or `Html.Encode`

## Status

Active

## Context

We had two related defects in the frontend that both originated from Razor embedding
data into JS in unsafe ways:

1. **Double encoding in `Categories/Index.cshtml` (lines 44 and 82).** A delete button used
   `onclick="showDeleteConfirm('@category.Id', '@Html.Encode(category.Name)')"`. Razor
   already HTML-encodes attribute values, and `@Html.Encode` did it a second time. A
   category named `Alimentación` rendered in the modal as the literal text
   `Alimentaci&#xF3;n`.

2. **Fragile / XSS-prone `Html.Raw(Json.Serialize(...))` patterns.** Two flavors existed:

   - Inside an `onclick` attribute:
     `onclick='openEditModal(@Html.Raw(Json.Serialize(category)))'`. If a category name
     contained `'`, the attribute broke. If the JSON happened to contain `</script>` (or
     the encoder changed), the same shape was an XSS vector.
   - Inside a regular `<script>` block:
     `var chartData = @Html.Raw(Json.Serialize(Model.Comparison));`. Razor does not
     encode values inside `<script>` blocks at all, so any data the user could influence
     flowed out raw. This worked today only because `Json.Serialize` uses the JavaScript
     encoder by default, but it is not the safest, most explicit pattern.

Both flavors are easy to write, hard to spot in review, and break in surprising ways
once data contains non-ASCII characters or quote-like characters.

## Decision

**Do not embed user-influenced or model-derived data into JS through `Html.Raw`,
`Html.Encode`, or inline `on*` event attributes.** Use one of the two patterns below,
matched to the shape of the data.

### Pattern A — Per-item data on a button or element (e.g. one record per row)

Pass each field as a `data-*` attribute on the element, then read it from a single
delegated event handler in JS. Never embed a JSON-serialized object in an attribute.

```cshtml
@* Razor *@
<button type="button"
        class="js-delete-category"
        data-category-id="@category.Id"
        data-category-name="@category.Name"
        aria-label="Delete @category.Name category">
    Delete
</button>
```

```js
// JS — registered once, usually inside setupEventListeners() on DOMContentLoaded
document.addEventListener('click', function (event) {
    const btn = event.target.closest('.js-delete-category');
    if (!btn) return;
    showDeleteConfirm(btn.dataset.categoryId, btn.dataset.categoryName);
});
```

Razor encodes the `data-*` value once, the browser decodes it once when it parses the
HTML, and `dataset` exposes the real character. Apostrophes, quotes, non-ASCII, and
HTML-special characters all round-trip correctly.

### Pattern B — Larger JSON payload from a model to a script block

Serialize the JSON into a `<script type="application/json">` block placed **before** the
script that consumes it, and parse it with `JSON.parse(el.textContent)`.

```cshtml
@* Razor — placed before the consuming <script> *@
<script type="application/json" id="dashboard-category-data">
    @Json.Serialize(Model.SpendingByCategory)
</script>

<script>
    document.addEventListener('DOMContentLoaded', function () {
        const data = JSON.parse(
            document.getElementById('dashboard-category-data').textContent
        );
        // ...
    });
</script>
```

The browser does not execute `<script type="application/json">` content as JavaScript,
so even if a value happened to contain `</script>` the block would not break out of
context. The content is also not interpreted as HTML, so embedding it in a `.cshtml`
file does not require HTML encoding.

### When to use which

| Case | Pattern |
|---|---|
| A handful of fields on a single element (button, link, row) | A — `data-*` + delegated listener |
| An object or array of model data consumed by a script | B — `application/json` script + `JSON.parse` |
| A handful of fields on a single element, but the value must be valid JSON itself | Combine: `data-payload='@Json.Serialize(model)'` is acceptable because `Json.Serialize` HTML-encodes the outer single quotes. If you need to read it, prefer a `data-payload` plus `JSON.parse(btn.dataset.payload)` to stay consistent. |

### Forbidden

- `@Html.Raw(Json.Serialize(model))` inside an `on*` attribute or any HTML attribute.
- `@Html.Raw(Json.Serialize(model))` inside a `<script>` block (use Pattern B instead).
- `@Html.Encode(value)` inside any HTML attribute (Razor already encodes attributes; the
  second pass is double encoding).
- Inline `onclick="func('@someRazorValue')"` for any value that includes user input.

## Consequences

### Positive

- No more double-encoding bugs in modals, popups, or tooltips.
- No more broken attributes when data contains `'`, `"`, `<`, `>`, or `&`.
- Reduced XSS surface: the `application/json` script is inert and the `data-*` pattern
  is Razor-encoded exactly once.
- The data flow is explicit and reviewable: data lives in the DOM as a typed
  attribute or a JSON block, not as a string embedded in JS source.
- Tests that click buttons by visible text or by element ID (e.g.
  `e2e/tests/05-categories-lifecycle.spec.ts`) keep working because the buttons and
  modal IDs do not change.

### Tradeoff

- One extra element (`<script type="application/json">`) per payload, or one extra
  block of `data-*` attributes per element.
- JS that consumes the data needs an extra `JSON.parse` or `dataset.*` read.
- For very small scripts that only need a single value (e.g. an analytics year passed
  to `initYearlyComparisonChart`), the pattern is still the same; the value just lives
  on the chart's `<canvas>` as `data-year="@Model.AnalyticsYear"`.

## Verification path

When adding or reviewing a Razor view that hands data to JS:

1. Run `rg -n --include='*.cshtml' 'Html\.Raw\(Json\.Serialize' src/SauronSheet.Frontend/`.
   Every hit must be inside a `<script type="application/json">` block, or it must be
   refactored to Pattern A or Pattern B.
2. Run `rg -n --include='*.cshtml' 'Html\.Encode|Html\.AttributeEncode' src/SauronSheet.Frontend/`.
   Hits should be zero in attribute contexts.
3. Run `rg -n --include='*.cshtml' 'on\w+="[^"]*@' src/SauronSheet.Frontend/`. Each hit
   should be either a hardcoded constant or a value with no risk of user influence;
   anything else should be Pattern A.
4. Manually create a category whose name contains `ó`, `'`, and `&`. Open the Edit and
   Delete modals and confirm the name appears correctly in both.
5. Run the E2E suite:
   `npx playwright test --config=e2e/playwright.config.ts --project=chromium`. The
   `05-categories-lifecycle` spec exercises the Delete modal end-to-end and must still
   pass.
