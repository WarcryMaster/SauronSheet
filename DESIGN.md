---
version: 1.1
name: SauronSheet-Olive-Design
description: A practical, clean, and accessible financial dashboard design built on top of MDBootstrap 5. The base canvas is **light gray** (`bg-light`), with main content areas and cards using pure white (`bg-white`) to create contrast and elevation. The single brand primary color is **Olive Green** (`#556B2F`), offering a calm, organic, and financial-friendly aesthetic. Typography relies on the robust default system sans-serif stack provided by Bootstrap, ensuring maximum readability and performance. The design embraces standard border-radiuses (`rounded-sm`, `rounded-3`) and drop shadows (`shadow-sm`, `shadow`) to establish hierarchy and depth.
colors:
  primary: "#556B2F"
  primary-active: "#435425"
  primary-hover: "#435425"
  primary-light: "#f4f7ee"
  primary-hover-light: "#fafbf6"
  primary-ring: "rgba(85, 107, 47, 0.25)"
  primary-ring-strong: "rgba(85, 107, 47, 0.35)"
  ink: "#212529"
  body: "#4f4f4f"
  muted: "#6c757d"
  canvas: "#f8f9fa"
  surface-card: "#ffffff"
  hairline: "#dee2e6"
  on-primary: "#ffffff"
  semantic-info: "#3b71ca"
  semantic-success: "#14a44d"
  semantic-warning: "#e4a11b"
  semantic-danger: "#dc4c64"
  semantic-danger-light: "#fef5f6"
  semantic-danger-hover-light: "#fef8f9"
  semantic-warning-badge-bg: "#fff6e6"
  semantic-warning-badge-border: "#f1d08a"
  semantic-warning-badge-text: "#9c6b00"
typography:
  fontFamily: "system-ui, -apple-system, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif"
  display-lg:
    fontSize: 2.5rem
    fontWeight: 500
  title-md:
    fontSize: 1.25rem
    fontWeight: 500
  body-md:
    fontSize: 1rem
    fontWeight: 400
  button:
    fontSize: 0.875rem
    fontWeight: 500
    textTransform: uppercase
rounded:
  sm: 0.25rem
  md: 0.375rem
  lg: 0.5rem
  pill: 50rem
  circle: 50%
spacing:
  form-narrow: "640px"
  form-narrow-tight: "560px"
  auth-card: "450px"
  auth-min-height: "70vh"
  progress-bar-default: "10px"
  chart-default: "300px"
components:
  top-nav:
    backgroundColor: "{colors.surface-card}"
    textColor: "rgba(0, 0, 0, 0.65)"
    shadow: "shadow"
  button-primary:
    backgroundColor: "{colors.primary}"
    textColor: "{colors.on-primary}"
    rounded: "{rounded.md}"
  card:
    backgroundColor: "{colors.surface-card}"
    border: "none"
    rounded: "{rounded.md}"
    shadow: "shadow-sm"
  empty-state:
    container: "card shadow-sm p-5 text-center"
    emojiSize: "fs-1"
    heading: "h5 fw-semibold mb-2"
    description: "text-muted mb-4"
  summary-card:
    container: "card shadow-sm p-3 text-center h-100"
    label: "small fw-semibold text-muted text-uppercase mb-1"
    value: "h4 fw-bold text-brand mb-0"
    grid: "row row-cols-1 row-cols-sm-2 row-cols-lg-4 g-3"
  page-header:
    container: "d-flex justify-content-between align-items-center"
    title: "h3 fw-bold mb-0"
  back-link-title:
    container: "d-flex align-items-center gap-3"
    link: "btn btn-link btn-sm text-muted text-decoration-none"
    title: "h3 fw-bold mb-0"
  filter-panel:
    container: "card shadow-sm p-4"
    applyButton: "btn btn-brand btn-sm"
    clearButton: "btn btn-link btn-sm text-muted text-decoration-none"
  table:
    container: "table table-hover align-middle mb-0"
    header: "thead table-light"
    headerCell: "text-uppercase small fw-semibold text-muted"
  alert:
    container: "alert alert-{type} rounded-3"
    role: "alert"
  badge-status:
    active: "badge bg-success"
    inactive: "badge bg-secondary"
---

## Overview

SauronSheet uses a clean, utility-first design approach based on MDBootstrap 5. The interface is optimized for financial data entry and visualization, prioritizing clarity, contrast, and ease of use over dramatic styling. The primary brand color is **Olive Green** (`#556B2F`), which conveys stability and growth, while semantic colors (success, danger) are used for income and expenses respectively.

**Key Characteristics:**
- Single brand accent: `{colors.primary}` (Olive Green #556B2F) for primary actions, buttons, and brand highlights.
- Light canvas (`#f8f9fa`) with white elevated cards (`#ffffff`).
- System sans-serif typography for optimal readability across all OS environments.
- Sensible border radiuses (typically `0.375rem` / `rounded-3`) to soften the interface.
- Use of MDBootstrap shadows (`shadow-sm`, `shadow`) to create physical depth and elevation hierarchy.
- Cards use **borderless elevation** — `border: 0` plus `shadow-sm` (do not add a visible border).

## Colors

### Brand & Accent
- **Olive Green** (`{colors.primary}` — #556B2F): Main brand color used for primary buttons, active states, and brand text.
- **Olive Dark** (`{colors.primary-active}` — #435425): Used for hover and active states on primary buttons.
- **Olive Light** (`{colors.primary-light}` — #f4f7ee): Used for subtle brand background highlights.
- **Olive Hover Light** (`{colors.primary-hover-light}` — #fafbf6): Used for hover state on light brand surfaces (e.g. toggle cards).
- **Olive Ring** (`{colors.primary-ring}` — rgba 0.25): Used for focus rings and selected states on brand surfaces.
- **Olive Ring Strong** (`{colors.primary-ring-strong}` — rgba 0.35): Used for `focus-visible` keyboard focus on brand surfaces.

### Surface
- **Canvas** (`{colors.canvas}` — #f8f9fa / `bg-light`): The base background color for the application layout.
- **Surface Card** (`{colors.surface-card}` — #ffffff / `bg-white`): Used for cards, modals, and the top navbar.

### Text
- **Ink** (`{colors.ink}` — #212529): Primary text color for headings and strong emphasis.
- **Body** (`{colors.body}` — #4f4f4f): Standard body text color.
- **Muted** (`{colors.muted}` — #6c757d): Secondary text, timestamps, and subtle hints.

### Semantic
- **Success** (`{colors.semantic-success}` — #14a44d): Positive actions, income, budget surpluses.
- **Danger** (`{colors.semantic-danger}` — #dc4c64): Destructive actions, expenses, budget overruns.
- **Danger Light** (`{colors.semantic-danger-light}` — #fef5f6): Tinted background for selected/active danger states.
- **Danger Hover Light** (`{colors.semantic-danger-hover-light}` — #fef8f9): Tinted background for hover on danger-tinted surfaces.
- **Warning** (`{colors.semantic-warning}` — #e4a11b): Cautions, limits approaching.
- **Warning Badge** (bg/border/text): Pre-built palette for amber badges (`#fff6e6` / `#f1d08a` / `#9c6b00`).
- **Info** (`{colors.semantic-info}` — #3b71ca): Informational alerts.

## Typography

SauronSheet relies on the system font stack to ensure fast loading times and a native feel on any operating system (`system-ui, -apple-system, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif`).

Headings use standard Bootstrap scaling (`h1` through `h6`), with `fw-bold` or `fw-semibold` utility classes used to establish hierarchy where necessary.

- Page titles inside authenticated pages use `<h3>` (`h3 fw-bold mb-0`).
- Auth and error pages may use `<h1>` for stronger hero-level emphasis.
- Do not mix `<h1>` and `<h3>` for the same role on a single page.

## Elevation & Depth

The design utilizes standard MDBootstrap drop shadows to separate overlapping surfaces and draw attention to interactive elements.

| Level | Class | Use |
|---|---|---|
| Flat | `shadow-none` | Default body canvas |
| Low | `shadow-sm` | Standard cards, subtle containers |
| Medium | `shadow` | Top navbar, dropdown menus |
| High | `shadow-lg` | Modals, offcanvas drawers |

## Shapes & Radiuses

Unlike sharp-edged designs, this system embraces rounded corners to create an approachable, modern SaaS feel.

- **Standard Elements** (Cards, Inputs, Buttons): Use Bootstrap's default rounding (approx `0.375rem`).
- **Badges**: Use `rounded-pill` for status indicators and small counters.
- **Avatars/Icons**: Use `rounded-circle`.

## Components

### Top Navigation
The navbar is a bright, white surface (`bg-white`) elevated with a medium shadow (`shadow`). Nav links are dark gray (`rgba(0,0,0,0.65)`) and hover to the Olive Green brand color.

### Cards
Standard content containers use `bg-white`, `shadow-sm`, and `border-0` to define their edges through elevation rather than a visible border. This is the **de-facto convention used across all pages** — `site.css` enforces `border: 0` on `.card`. Do not add a visible border to cards unless a specific feature requires it (e.g. dashed file upload zone).

### Buttons
- **Primary**: Uses the `.btn-brand` class (Olive Green background, white text).
- **Secondary**: Uses `.btn-outline-secondary` or `.btn-light` for less prominent actions.
- **Semantic**: Uses `.btn-danger`, `.btn-success` standard MDBootstrap classes.
- **Forbidden**: `.btn-primary` / `.btn-outline-primary` (Bootstrap blue) — they bypass the brand and create visual drift.

### Page Layouts

Four layout patterns cover the whole app. Pick the one that matches the page's role.

| Pattern | Container | Width | Use |
|---|---|---|---|
| **Full-width** | `d-flex flex-column gap-4` | `100%` | Authenticated list/grid pages (Index, Dashboard, Metrics, History, Comparison) |
| **Narrow form** | `mx-auto d-flex flex-column gap-4` | `max-width: 640px` | Create/Edit forms (Add Transaction, Upload, Budgets Create) |
| **Narrow form (tight)** | same | `max-width: 560px` | Edit forms with denser fields (Budgets Edit) |
| **Auth centered** | `d-flex align-items-center justify-content-center` | `max-width: 450px`, `min-height: 70vh` | Login, Register |

### Page Header
Every list/grid page starts with a page header. Use `d-flex justify-content-between align-items-center` with the title on the left (`h3 fw-bold mb-0`) and primary action(s) on the right.

```
[d-flex justify-content-between align-items-center]
  ├─ h3.fw-bold.mb-0     ← Page Title
  └─ div                ← Action buttons (e.g. "+ Create Budget")
```

### Back Link + Title
Detail pages (Create, Edit, Add, Upload) replace the page header with a back-link + title row.

```
[d-flex align-items-center gap-3]
  ├─ a.btn.btn-link.btn-sm.text-muted.text-decoration-none ← "← Back"
  └─ h3.fw-bold.mb-0                                         ← Title
```

### Filter Panel
Filterable list pages wrap their filter form in a `card shadow-sm p-4`. Apply uses `.btn-brand.btn-sm`; Clear uses `.btn-link.btn-sm.text-muted.text-decoration-none`.

### Tables
Data tables use `table table-hover align-middle mb-0`. Headers use `thead class="table-light"` with `th class="text-uppercase small fw-semibold text-muted"`. Numeric/right-aligned columns append `text-end`. Status columns use the `Status Badges` pattern.

### Empty State
When a list is empty, show a centered card with an emoji, a short heading, a muted description, and an optional CTA button group. Add `role="status" aria-live="polite"` for screen readers.

```html
<div class="card shadow-sm p-5 text-center" role="status" aria-live="polite">
    <div class="fs-1 mb-3">EMOJI</div>
    <p class="h5 fw-semibold mb-2">No budgets found.</p>
    <p class="text-muted mb-4">Create one to start tracking your spending.</p>
    <div class="d-flex justify-content-center gap-3">
        <a class="btn btn-brand">+ Create Budget</a>
    </div>
</div>
```

### Summary / Metric Cards
Dashboard-style summary cards live in a responsive grid (`row row-cols-1 row-cols-sm-2 row-cols-lg-4 g-3`). Each card uses `card shadow-sm p-3 text-center h-100`, with a `small fw-semibold text-muted text-uppercase mb-1` label and a `h4 fw-bold text-brand mb-0` value.

### Forms
Inputs and form controls follow standard MDBootstrap styling with **brand-tinted** focus rings (use `var(--brand-ring)` or `var(--brand-ring-strong)`) and standard border radiuses.

**Two label variants** — pick by context:
- **Standard form** (Create/Edit/Add/Upload): `class="form-label fw-semibold"`
- **Compact/filter** (filter panels, inline fields): `class="form-label small fw-semibold text-muted text-uppercase mb-1"`

Use `.form-text` for hints and context under fields.

### Status Badges
- Active: `class="badge bg-success"`
- Inactive: `class="badge bg-secondary"`
- For the budget traffic light (Green/Yellow/Red/Overage) reuse the `_BudgetStatusBadge` partial.
- For category dots/colors reuse the `_CategoryBadge` partial or `.category-badge-*` CSS classes.

### Alerts
All alerts use the unified `alert alert-{type} rounded-3` pattern with `role="alert"`. Do not omit `rounded-3`. Enhanced variants add a leading icon span (`d-flex align-items-start gap-2`) for context.

### Toggle Cards / Segmented Controls
When two or more mutually exclusive choices share a single field (e.g. Income vs Expense), wrap the radios in a `.d-flex.gap-3` with one `<label class="flex-fill">` per option. Each label hides its radio visually and styles the inner content (`p-3 rounded-3 border`) as a selectable card. Selected state uses `border-color: var(--brand)` and `box-shadow: 0 0 0 2px var(--brand-ring)`; danger-tinted options swap those for the danger tokens.

### Iconography
- **Emoji** is the default visual accent in nav links and empty states.
- **Font Awesome** (via CDN in the layout) is used for functional icons inside forms and buttons.
- **Bootstrap Icons** (`bi bi-*`) are **not loaded** and must not be used — they render as missing glyphs.

## Interactive Layer (Alpine.js + HTMX + Chart.js)

SauronSheet uses a lightweight reactive stack on top of MDBootstrap:
- **Alpine.js v3** (<15 KB, CDN): Declarative reactivity (`x-data`, `x-show`, `x-transition`, `x-on`)
- **HTMX v2** (<14 KB, CDN): Ajax from HTML attributes (`hx-get`, `hx-target`, `hx-swap`)
- **Chart.js** (latest, CDN): Interactive charts with CSS custom property tokens

### Alpine.js Patterns

| Pattern | Directive | Use |
|---|---|---|
| Toggle visibility | `x-show` + `x-transition` | Show/hide elements reactively (custom date range, empty state vs data, modals) |
| Form binding | `x-model` | Two-way bind inputs and selects without manual JS |
| Event handling | `@@click`, `@@change`, `@@submit` | Replace inline `onclick` / `onchange` attributes |
| Component state | `x-data="{ key: value }"` | Scoped reactive state for a DOM subtree |
| Lifecycle | `x-init` | Run code when Alpine component mounts (replaces `DOMContentLoaded`) |
| Conditional rendering | `x-if` / `template` | Lazily create/destroy DOM (vs `x-show` which toggles visibility) |
| Class binding | `:class="condition ? 'class-a' : 'class-b'"` | Dynamic CSS classes reactively |
| Outside click | `@@click.outside` | Close dropdowns/modals when clicking outside |

### HTMX Patterns

| Pattern | Attributes | Use |
|---|---|---|
| Ajax GET | `hx-get="/endpoint"` | Load HTML fragment from server |
| Target swap | `hx-target="#element"` + `hx-swap="outerHTML"` | Replace specific DOM region |
| Select from response | `hx-select="#content"` | Extract only the target from a full-page response |
| Loading indicator | `hx-indicator="#spinner"` | Show element during request |
| Push URL | `hx-push-url="true"` | Update browser URL for bookmarkable state |
| Trigger | `hx-trigger="click"` | Explicit trigger (default depends on element) |

### Chart.js Configuration

| Setting | Value | Reason |
|---|---|---|
| `responsive` | `true` | Charts fill their container card |
| `maintainAspectRatio` | `false` | Let CSS height control the chart area |
| `interaction.mode` | `'index'` | Show all datasets at hover point |
| `interaction.intersect` | `false` | Trigger tooltip without exact hover |
| Colors | `cssVar('--brand')` | Resolve design tokens at runtime, fallback to hex |

### HTMX + Chart.js Lifecycle

Chart instances must be destroyed before HTMX replaces the DOM:

```js
// In x-init or global listener
document.addEventListener('htmx:beforeSwap', (e) => {
    if (e.detail.target.id === 'dashboard-content') {
        destroyAllCharts(); // Chart.getChart(canvas)?.destroy()
    }
});
document.addEventListener('htmx:afterSwap', (e) => {
    if (e.detail.target.id === 'dashboard-content') {
        initCharts(); // Re-read JSON blocks, recreate charts
    }
});
```

### Forbidden Interactive Patterns

- ❌ `onclick` / `onchange` / `onsubmit` inline — use `x-on:` / `@@event`
- ❌ `DOMContentLoaded` for chart init — use `x-init` or HTMX `afterSwap`
- ❌ `document.getElementById` in Alpine components — use `$refs`
- ❌ `innerHTML` manipulation — use `x-html` or HTMX swap
- ❌ Hardcoded chart colors — use `cssVar('--token', fallback)`

### Script Loading Order

1. MDB CSS (render-critical)
2. Alpine.js (defer — non-blocking)
3. HTMX (immediate — must register before DOM)
4. Chart.js (loaded first in _Layout head, executes before charts.js)
5. charts.js (executes last, depends on Chart.js global)

## Do's and Don'ts

### Do
- Use `.btn-brand` for the primary call to action on a page.
- Wrap content in `.card` with `.shadow-sm` on top of a `.bg-light` layout.
- Use MDBootstrap utility classes (`mb-3`, `p-4`, `d-flex`) for layout and spacing.
- Rely on semantic text colors (`text-success`, `text-danger`) for financial amounts.
- Match the existing component patterns (page header, back-link title, filter panel, empty state, summary card).
- Use MDBootstrap attributes (`data-mdb-*`) for dismiss/toggle/ripple — never `data-bs-*`.
- Reference design tokens (`var(--brand)`, `var(--brand-ring)`, etc.) instead of hardcoding hex values.

### Don't
- Don't use `.btn-primary` or `.btn-outline-primary` (Bootstrap blue) — they bypass the brand.
- Don't use `data-bs-dismiss`, `data-bs-toggle`, or `data-bs-target` — use `data-mdb-*` variants.
- Don't use `bi bi-*` icons — they're not loaded and will render as empty squares.
- Don't use overly sharp corners (`rounded-0`) unless explicitly necessary for flush elements.
- Don't use dark themes or inverted canvases for main content areas; keep the app light and readable.
- Don't hardcode hex colors in inline `style=""`; use utility classes or CSS variables.
- Don't omit `rounded-3` on alerts — it is part of the standard alert pattern.
- Don't mix heading levels (h1 vs h3) for the same role on a single page.
