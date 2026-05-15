---
version: 1.0
name: SauronSheet-Olive-Design
description: A practical, clean, and accessible financial dashboard design built on top of MDBootstrap 5. The base canvas is **light gray** (`bg-light`), with main content areas and cards using pure white (`bg-white`) to create contrast and elevation. The single brand primary color is **Olive Green** (`#556B2F`), offering a calm, organic, and financial-friendly aesthetic. Typography relies on the robust default system sans-serif stack provided by Bootstrap, ensuring maximum readability and performance. The design embraces standard border-radiuses (`rounded-sm`, `rounded-3`) and drop shadows (`shadow-sm`, `shadow`) to establish hierarchy and depth. 

colors:
  primary: "#556B2F"
  primary-active: "#435425"
  primary-hover: "#435425"
  primary-light: "#f4f7ee"
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
    border: "1px solid {colors.hairline}"
    rounded: "{rounded.md}"
    shadow: "shadow-sm"
---

## Overview

SauronSheet uses a clean, utility-first design approach based on MDBootstrap 5. The interface is optimized for financial data entry and visualization, prioritizing clarity, contrast, and ease of use over dramatic styling. The primary brand color is **Olive Green** (`#556B2F`), which conveys stability and growth, while semantic colors (success, danger) are used for income and expenses respectively.

**Key Characteristics:**
- Single brand accent: `{colors.primary}` (Olive Green #556B2F) for primary actions, buttons, and brand highlights.
- Light canvas (`#f8f9fa`) with white elevated cards (`#ffffff`).
- System sans-serif typography for optimal readability across all OS environments.
- Sensible border radiuses (typically `0.375rem` / `rounded-3`) to soften the interface.
- Use of MDBootstrap shadows (`shadow-sm`, `shadow`) to create physical depth and elevation hierarchy.

## Colors

### Brand & Accent
- **Olive Green** (`{colors.primary}` — #556B2F): Main brand color used for primary buttons, active states, and brand text.
- **Olive Dark** (`{colors.primary-active}` — #435425): Used for hover and active states on primary buttons.
- **Olive Light** (`{colors.primary-light}` — #f4f7ee): Used for subtle brand background highlights.

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
- **Warning** (`{colors.semantic-warning}` — #e4a11b): Cautions, limits approaching.
- **Info** (`{colors.semantic-info}` — #3b71ca): Informational alerts.

## Typography

SauronSheet relies on the system font stack to ensure fast loading times and a native feel on any operating system (`system-ui, -apple-system, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif`). 

Headings use standard Bootstrap scaling (`h1` through `h6`), with `fw-bold` or `fw-semibold` utility classes used to establish hierarchy where necessary.

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
Standard content containers use `bg-white`, `shadow-sm`, and are often wrapped with a subtle border (`border`) to define their edges clearly against the light gray canvas.

### Buttons
- **Primary**: Uses the `.btn-brand` class (Olive Green background, white text).
- **Secondary**: Uses `.btn-outline-secondary` or `.btn-light` for less prominent actions.
- **Semantic**: Uses `.btn-danger`, `.btn-success` standard MDBootstrap classes.

### Forms
Inputs and form controls follow standard MDBootstrap styling with blue focus rings and standard border radiuses.

## Do's and Don'ts

### Do
- Use `.btn-brand` for the primary call to action on a page.
- Wrap content in `.card` with `.shadow-sm` on top of a `.bg-light` layout.
- Use MDBootstrap utility classes (`mb-3`, `p-4`, `d-flex`) for layout and spacing.
- Rely on semantic text colors (`text-success`, `text-danger`) for financial amounts.

### Don't
- Don't use overly sharp corners (`rounded-0`) unless explicitly necessary for flush elements.
- Don't use dark themes or inverted canvases for main content areas; keep the app light and readable.
- Don't hardcode hex colors in HTML; use utility classes or CSS variables.
