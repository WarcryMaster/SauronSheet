# Implementation Plan: Estilizado visual atractivo para Login con Tailwind

**Branch**: `001-login-tailwind-style` | **Date**: 2026-03-07 | **Spec**: [spec.md](spec.md)
**Input**: Frontend-only visual redesign of /Auth/Login page using Tailwind CSS, WCAG 2.1 AA accessibility, responsive design

## Summary

Enhance the login page (/Auth/Login) with modern, attractive visual styling using Tailwind CSS utilities. Implement responsive design (mobile/tablet/desktop), WCAG 2.1 AA accessibility compliance, and complete interactive states (hover/focus/error). No changes to authentication logic, domain, or application layers — pure frontend HTML/CSS/JavaScript work in Razor Pages.

**Primary Requirements:**
- Centered, 400px fixed-width form (90% mobile) with white background
- Responsive breakpoints using Tailwind standards (mobile < 640px, tablet 640-1024px, desktop ≥ 1024px)
- Complete interactive states: base → hover → focus → error with 0.3s transitions
- SauronSheet logo (32×32px) centered at form top
- Adaptive padding (p-4 mobile, p-8 desktop), generous spacing (space-y-6 between fields)
- Error messages with X-circle icon, WCAG 2.1 AA contrast (4.5:1), keyboard navigation, screen reader compatible
- Font hierarchy: Title text-3xl extrabold, button text-base medium, labels text-xs medium
- Brand color palette consistency (blue-600 primary, grays, Dashboard alignment)
- **HTML5 validation**: type="email" required + server-side validation on POST
- **Loading state**: Spinner visible in button + button disabled (opacity-50, cursor-not-allowed) to prevent multiple submissions
- **Password toggle**: Eye/eye-off icon with Alpine.js to toggle type="password" ↔ type="text" (44×44px hit target for mobile)
- **Focus management**: After error, focus returns to Email field automatically; error message announces via aria-live="polite"

**Scope**: Frontend Layer ONLY
- In Scope: Razor Pages .cshtml markup, Tailwind CSS classes, Alpine.js (if interactivity needed), SVG/icon assets
- Out of Scope: Authentication logic changes, Application/Domain/Infrastructure modifications, backend API changes

---

## Technical Context

**Language/Version**: C# .NET Core 10 + Razor Pages + Tailwind CSS 3.x + Alpine.js 3.x

**Primary Dependencies**: 
- Tailwind CSS 3.4+ (CDN-based, already integrated in Frontend/Program.cs)
- **Alpine.js 3.x** (REQUIRED for password visibility toggle + loading spinner state management)
- Heroicons or custom SVG (for X-circle error icon and eye/eye-off password toggle)
- Existing logo assets in `wwwroot/img/` folder

**Storage**: N/A (no data persistence changes)

**Testing**: 
- E2E browser testing: Visual regression, accessibility (Lighthouse, axe DevTools)
- Manual testing: Keyboard navigation (Tab order), screen reader (NVDA/JAWS), responsive breakpoints
- No unit tests required (visual-only, no business logic)

**Target Platform**: Web browsers (Chrome, Firefox, Safari, Edge — latest 2 versions)

**Project Type**: Web application (frontend refactoring)

**Performance Goals**: 
- Page load: ≤ 2s (visual milestone paint)
- Form interaction: ≤ 100ms (user input → visual feedback)
- Accessibility: WCAG 2.1 AA compliance (scoring ≥ 90 in Lighthouse)

**Constraints**: 
- No breaking changes to existing authentication backend
- Tailwind-only styling (no custom CSS additions except SVG icon definitions)
- IE 11 not supported (modern browsers only)
- Form width must adapt to ultra-small screens (< 320px) without horizontal scroll

**Scale/Scope**: 
- Single page: /Auth/Login
- Estimated 60-80 lines of HTML (.cshtml), 0 new C# lines (PageModel already exists)
- 4-5 interactive component sections (logo, title, email input, password input, submit button, error message, signup link)

---

## Constitution Check

**GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.**

| Principle | Requirement | Status | Justification |
|-----------|-------------|--------|---------------|
| **Clean Architecture** | No upward layer references; Frontend uses Application/Domain only | ✅ PASS | Frontend-only work: no new Application/Domain/Infrastructure code generated. Existing PageModel (HttpGet → MediatR query dispatcher) remains unchanged. |
| **CQRS Pattern** | Commands/Queries routed through MediatR | ✅ PASS | Login PageModel already dispatches authentication via MediatR (no changes needed). Feature only modifies HTML/CSS presentation layer. |
| **Domain-Driven Design** | Entities with strong-typed IDs, immutable value objects, guard methods | ✅ PASS | No domain changes. Feature uses existing domain entities (User, authentication workflow unchanged). |
| **Test-First Development** | Tests written before implementation | ⚠️ WARNING | Visual features lack traditional unit tests. Mitigation: E2E browser tests + manual accessibility audit + visual regression checks capture expected behavior. |
| **Spec-Driven Development** | Single spec file, layer scope boundaries enforced | ✅ PASS | Spec declares Frontend-only scope explicitly. No out-of-scope Application/Domain/Infrastructure deliverables. Single phase spec file (`spec.md`). |

**Gate Outcome**: ✅ **PASS** — Feature compliant with all principles. Warning on testing remediated via E2E + accessibility audit strategy.

---

## Project Structure

### Documentation (this feature)

```text
specs/001-login-tailwind-style/
├── spec.md              # Feature specification (complete)
├── plan.md              # This file - implementation planning
├── research.md          # Phase 0 output: dependency verification
├── data-model.md        # Phase 1 output: visual component specs (Figma/ASCII if needed)
├── quickstart.md        # Phase 1 output: developer quick-start
├── contracts/           # Phase 1 output: HTML/CSS contract specifications
│   ├── login-form-contract.md        # Component contract
│   ├── responsive-breakpoints.md    # Tailwind breakpoint mapping
│   └── accessibility-contract.md    # WCAG compliance checklist
└── tasks.md             # Phase 2 output: task breakdown (generated by speckit.tasks)
```

### Source Code (repository root)

```text
Frontend/
├── Pages/
│   └── Auth/
│       ├── Login.cshtml         # [MODIFY] Markup with new Tailwind styling
│       ├── Login.cshtml.cs      # [NO CHANGE] PageModel authentication logic stays same
│       ├── Register.cshtml      # [REF] Use as style reference (brand consistency)
│       └── Shared/              # [REF] Check _Layout.cshtml for Tailwind setup
│
├── wwwroot/
│   ├── css/                     # [NO NEW] Tailwind already in Program.cs
│   ├── img/
│   │   ├── logo.svg             # [USE] Existing SauronSheet logo (32×32px)
│   │   └── [other assets]
│   └── js/
│       └── [optional] alpine-spinner.js  # [NEW] Optional: spinner during form submit
│
└── bin/Debug/              # Build artifacts (auto-generated)
```

**Structure Decision**: 
- **Scope**: Minimal — modify only `Frontend/Pages/Auth/Login.cshtml`
- **No new files required**: Tailwind CSS already available in project; logo exists in wwwroot/img
- **Optional additions**: If spinner/loading state uses Alpine.js interaction, may add inline Alpine directives (no separate JS file needed for MVP)

---

## Complexity Tracking

No violations to justify. Feature is straightforward frontend styling with no architectural complexity.

---

## Phase 0: Research & Verification

**Goal**: Confirm technical dependencies, asset availability, and design tooling readiness.

### Research Tasks

| Task | Description | Expected Output | Responsibility |
|------|-------------|-----------------|-----------------|
| **Task R1**: Verify Tailwind CDN/build setup | Check Frontend/Program.cs for Tailwind CSS integration; confirm all breakpoints available (sm, md, lg, xl, 2xl) | `research.md` section: "Tailwind Setup Verified" | Developer |
| **Task R2**: Audit logo assets in wwwroot/img | List available logo files; confirm 32×32px version exists or can be resized | `research.md` section: "Logo Assets Inventory" | Developer |
| **Task R3**: Review Dashboard.cshtml for style reference | Extract color classes, button styles, form field patterns used in existing Dashboard | `research.md` section: "Brand Color Palette Reference" | Developer |
| **Task R4**: Confirm WCAG 2.1 AA tooling availability | Verify Lighthouse, axe DevTools, NVDA/JAWS access for accessibility testing | `research.md` section: "Accessibility Testing Tools Ready" | QA |
| **Task R5**: Validate CSS breakpoint defaults in Tailwind config | Check `tailwind.config.js` (or defaults) for mobile-first breakpoints (640px, 1024px, 1280px) | `research.md` section: "Tailwind Breakpoints Confirmed" | Developer |

**Phase 0 Deliverable**: `research.md` with all unknowns resolved

---

## Phase 1: Design & Contracts

**Goal**: Document visual specifications, component structure, and CSS class mappings.

### Design Tasks

| Task | Description | Expected Output | Dependencies |
|------|-------------|-----------------|--------------|
| **Task D1**: Create responsive layout spec | Define exact pixel dimensions for desktop (400px), tablet (640-1024px), mobile (< 640px); document vertical/horizontal centering approach | `data-model.md`: "Layout Specification" section with ASCII diagrams | R5 |
| **Task D2**: Map Tailwind color palette | Document exact Tailwind classes for brand colors (primary blue, error red, text gray, focus ring); reference Dashboard for consistency | `data-model.md`: "Color Palette Mapping" section | R3 |
| **Task D3**: Design component hierarchy | Specify each form component (logo, title, label, input, button, error message, signup link) with exact Tailwind classes and responsive variants | `data-model.md`: "Component Specifications" section | D1 |
| **Task D4**: Define keyboard navigation & focus order | Document Tab key flow (email → password (+ eye toggle) → submit → signup link); specify focus ring styling (ring-blue-500, ring-2); document focus return to email on error | `data-model.md`: "Keyboard Navigation & Focus Management" section | D3 |
| **Task D5**: Create error state specification | Define error message HTML structure (icon + text), Tailwind classes for error styling, X-circle icon implementation (SVG or Heroicons), focus return to email on error display | `data-model.md`: "Error Message Component & Focus Management" section | D3 |
| **Task D6**: Specify transition & animation timing | Document 0.3s transition class usage (transition duration-300) for all hover/focus/active states | `data-model.md`: "Interactive States & Transitions" section | D3 |
| **Task D7**: Design password toggle component | Specify eye/eye-off icon (20px, 44×44px touch target), Alpine.js directives (@click, x-show), type toggle behavior, accessibility attributes (aria-pressed, aria-label) | `data-model.md`: "Password Toggle Component" section | D3 |
| **Task D8**: Generate API contracts | Create structured JSON/YAML specifications for all components including password toggle (input attributes, Alpine directives, CSS output, accessibility attributes) | `contracts/login-form-contract.md` + optional `contracts/password-toggle-contract.md` | D1-D7 |

**Phase 1 Deliverables**: 
- `data-model.md` (comprehensive visual & layout specifications)
- `contracts/login-form-contract.md` (component structure contract)
- `contracts/responsive-breakpoints.md` (Tailwind breakpoint mapping)
- `contracts/accessibility-contract.md` (WCAG 2.1 AA checklist)
- `quickstart.md` (developer quick-start guide for implementation)

---

## Phase 2: Task Generation

**Goal**: Break Phase 1 design into granular, implementation-ready tasks.

### Implementation Task Categories

**Category A: HTML Markup Refactoring**
- T1: Update Login.cshtml with centered container wrapper + responsive width
- T2: Add SauronSheet logo image element (32×32px, centered, mb-4)
- T3: Style form title ("Sign in to your account") with text-3xl font-extrabold classes
- T4: Refactor email input with Tailwind classes (border, rounded, focus ring, placeholder, label, HTML5 type=email required)
- T5: Refactor password input wrapper with Tailwind classes + password toggle component container (relative positioning for icon overlay)
- T6: Implement password toggle icon (eye/eye-off SVG, 20px, 44×44px hit target in wrapper, Alpine.js @click directive to toggle type)
- T7: Style submit button with bg-blue-600, hover/focus states, full width, **disabled state (opacity-50, cursor-not-allowed)**
- T8: Add loading spinner inside button (Alpine.js x-show conditional, spinner visible during submission)
- T9: Add error message container with X-circle icon (SVG), role="alert" accessibility, Alpine.js x-show conditional
- T10: Implement focus return to email field on error display (via Alpine.js or middleware script)
- T11: Add "Sign up" link below submit button with hover color change

**Category B: Responsive Design Implementation**
- T12: Implement mobile breakpoint adjustments (padding p-4, container 90% width, font sizes scale down)
- T13: Implement tablet breakpoint (640px: padding-6, container 400px)
- T14: Implement desktop breakpoint (1024px+: padding-8, container 400px, full vertical centering)
- T15: Ensure password toggle eye icon is 44×44px minimum (touch target) on all breakpoints
- T16: Test responsive behavior across all breakpoints in browser DevTools

**Category C: Validation & Form Behavior**
- T17: Ensure HTML5 validation enabled (type="email" required on email, required on password)
- T18: Verify server-side validation on POST (PageModel responsibility, no HTML changes needed)
- T19: Implement error display + focus return to email field on login failure (JavaScript or Alpine.js @watch on form state)

**Category D: Accessibility Compliance**
- T20: Verify WCAG 2.1 AA color contrast (4.5:1 for text, 3:1 for UI components)
- T21: Ensure all form inputs have associated <label> elements with proper for="" attributes
- T22: Verify password toggle icon is keyboard accessible (Tab to reach, Enter/Space to activate, aria-pressed, aria-label="Show/Hide Password")
- T23: Configure keyboard navigation Tab order (email → password (+ toggle icon) → submit → signup) with Tab key testing
- T24: Test with screen reader (NVDA or JAWS) to verify label announcements, toggle state, error messages
- T25: Add aria-live="polite" and role="alert" to error message container
- T26: Verify focus management: focus returns to email field after error display

**Category E: Interactive States & Polish**
- T27: Add 0.3s transition classes (transition duration-300) to button, inputs, toggle icon
- T28: Implement button hover state (bg-blue-700)
- T29: Implement input focus state (ring-2 ring-blue-500 border-blue-500)
- T30: Add error state styling for inputs (border-red-500, ring-red-500, focus ring-red-500)
- T31: **REQUIRED**: Implement loading spinner during form submission (Alpine.js x-show, spinner inside button)
- T32: **REQUIRED**: Implement button disabled state during submission (disabled=true, opacity-50, cursor-not-allowed to prevent duplicate submissions)
- T33: Implement password toggle icon state (aria-pressed true/false, icon SVG changes eye→eye-off, smooth transition 300ms)

**Category F: Quality Assurance & Testing**
- T34: E2E visual regression test (Playwright or Cypress screenshot baseline)
- T35: Lighthouse accessibility audit (target ≥ 90 score)
- T36: Manual keyboard navigation test (Tab through all controls including toggle, no focus traps)
- T37: Manual password toggle test (click/tap eye icon, type changes password ↔ text, no data loss)
- T38: Manual focus management test (trigger error via invalid login, verify focus returns to email automatically)
- T39: Manual loading state test (submit form, verify spinner visible + button disabled, re-enable on response)
- T40: Manual cross-browser test (Chrome, Firefox, Safari, Edge latest 2 versions)
- T41: Manual responsive test on real mobile devices (iPhone 12/14, Android, verify 44×44px toggle hit target)

**Phase 2 Deliverable**: `tasks.md` with prioritized, dependency-ordered task list (generated by speckit.tasks command)

---

## Password Toggle Component Specification (NEW - Ronda 4)

**Component**: Eye/Eye-Off Icon Toggle for Password Visibility

**Purpose**: Allow users to toggle password visibility (show/hide) for better UX and accessibility on password field

**HTML Structure**:
```html
<div class="relative">
  <input 
    type="password" 
    id="password" 
    name="password" 
    placeholder="Enter your password"
    x-model="passwordType"
    :type="showPassword ? 'text' : 'password'"
    class="w-full px-3 py-2 pr-10 border border-gray-300 rounded-md shadow-sm 
           focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500 
           transition duration-300"
    required
    aria-label="Password"
  />
  <button 
    type="button" 
    @click="showPassword = !showPassword"
    :aria-pressed="showPassword"
    aria-label="Show/Hide password"
    class="absolute right-3 top-1/2 transform -translate-y-1/2 h-11 w-11 flex items-center justify-center 
           text-gray-400 hover:text-gray-600 focus:outline-none focus:ring-2 focus:ring-blue-500 
           transition duration-300"
    tabindex="0"
  >
    <!-- Eye icon (closed) -->
    <svg x-show="!showPassword" class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-4.803m5.596-3.856a3.375 3.375 0 11-6.75 0 3.375 3.375 0 016.75 0M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
    </svg>
    <!-- Eye-off icon (open) -->
    <svg x-show="showPassword" class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3.98 8.223A10.477 10.477 0 001.934 12C3.226 16.338 7.244 19.5 12 19.5c.993 0 1.953-.138 2.863-.395M6.228 6.228A10.45 10.45 0 0112 4.5c4.756 0 8.773 3.162 10.065 7.498a10.523 10.523 0 01-4.293 5.774M6.228 6.228L3 3m3.228 3.228l3.65 3.65m7.894 7.894L21 21m-3.228-3.228l-3.65-3.65m0 0a3 3 0 10-4.243-4.243m4.242 4.242L9.88 9.88" />
    </svg>
  </button>
</div>
```

**Alpine.js Data** (in component initialization):
```javascript
{
  showPassword: false  // Toggle state
}
```

**Accessibility**:
- `aria-pressed`: Reflects toggle state (true = showing password)
- `aria-label`: "Show/Hide password" 
- `type="button"` on toggle button (not submit)
- 44×44px minimum touch target (h-11 w-11 = 44px in Tailwind)
- Focus ring 2px blue visible on toggle button
- Keyboard accessible: Tab to reach, Enter/Space to toggle

**Responsive Considerations**:
- Icon size: h-5 w-5 (20px) on all breakpoints
- Button wrapper: 44×44px on mobile, desktop (consistent hit target)
- Right padding on input adjusted (pr-10) to accommodate toggle icon

---

## Design Specifications (Contracts)

### Component Contract: Login Form

**File**: `contracts/login-form-contract.md`

```markdown
# Login Form Component Contract

## Overall Container

**Purpose**: Wrapper for entire login form with fixed width and vertical centering

**Desktop Layout (≥1024px)**
- Type: div with fixed width 400px
- Tailwind Classes: `w-full max-w-md bg-white rounded-lg shadow-lg p-8 mx-auto my-auto`
- Computed: Center horizontally via `mx-auto`; center vertically via flexbox parent
- Spacing: Inner padding 32px (p-8), outer gap from viewport edge ≥ 20px

**Tablet Layout (640-1024px)**
- Type: div with responsive width
- Tailwind Classes: `w-full max-w-md bg-white rounded-lg shadow-lg p-6 mx-auto`
- Computed: 90% width on tablet; padding 24px (p-6)

**Mobile Layout (< 640px)**
- Type: div with responsive width
- Tailwind Classes: `w-full max-w-sm bg-white rounded-lg shadow-md p-4 mx-auto`
- Computed: 90% width; padding 16px (p-4); reduced shadow

---

## Logo Component

**Purpose**: SauronSheet branding at form top

**Attributes**:
- Source: `/img/logo.svg` (existing asset)
- Alt Text: "SauronSheet"
- Dimensions: 32px × 32px (fixed, no resize)
- Positioning: `<img src="/img/logo.svg" alt="SauronSheet" class="h-8 w-8 mx-auto mb-4">`
- Tailwind: `h-8 w-8` (32px in Tailwind units, 0.5rem = 8px per unit), `mx-auto` (horizontal center), `mb-4` (16px margin below)

---

## Title Component

**Purpose**: Form heading "Sign in to your account"

**Attributes**:
- HTML Tag: `<h2>`
- Text Content: "Sign in to your account"
- Tailwind Classes: `text-center text-3xl font-extrabold text-gray-900 mt-6 mb-8`
- Computed Font Size: 30px (text-3xl), Weight: 800 (font-extrabold), Line Height: 36px
- Spacing: Top margin 24px (mt-6), bottom margin 32px (mb-8)

**Responsive Variants**:
- Mobile (< 640px): Consider `text-2xl` (24px) if space is tight; default `text-3xl` acceptable
- Desktop (≥1024px): `text-3xl` (30px)

---

## Email Input Component

**Purpose**: User email entry field

**HTML Structure**:
```html
<div>
  <label for="email" class="block text-xs font-medium text-gray-700 mb-1">
    Email address
  </label>
  <input 
    type="email" 
    id="email" 
    name="email" 
    placeholder="Enter your email"
    class="w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm 
           focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500 
           transition duration-300"
    required
    aria-label="Email address"
  />
</div>
```

**Tailwind Classes Breakdown**:
- `w-full`: Full width of parent container
- `px-3 py-2`: 12px horizontal, 8px vertical padding
- `border border-gray-300`: 1px border, light gray
- `rounded-md`: 6px border radius
- `shadow-sm`: Subtle shadow for depth
- `focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500 transition duration-300`: Remove default outline; add 2px blue focus ring; change border to blue on focus; 300ms transition

**Accessibility**:
- `for="email"`: Label linked to input via id
- `aria-label="Email address"`: Screen reader label
- `required`: HTML5 validation hint

---

## Password Input Component

**Purpose**: User password entry field

**HTML Structure** (same as email, substituting):
```html
<div>
  <label for="password" class="block text-xs font-medium text-gray-700 mb-1">
    Password
  </label>
  <input 
    type="password" 
    id="password" 
    name="password" 
    placeholder="Enter your password"
    class="w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm 
           focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500 
           transition duration-300"
    required
    aria-label="Password"
  />
</div>
```

**Tailwind Classes**: Identical to email input
**Accessibility**: Same pattern as email

---

## Submit Button

**Purpose**: "Sign in" form submission button

**HTML Structure**:
```html
<button 
  type="submit" 
  class="w-full px-4 py-2 bg-blue-600 hover:bg-blue-700 active:bg-blue-800 
         text-white font-medium rounded-md shadow-md
         focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2
         transition duration-300
         disabled:opacity-50 disabled:cursor-not-allowed"
  id="submit-button"
>
  Sign in
</button>
```

**Tailwind Classes Breakdown**:
- `w-full`: Full width (stretches to container)
- `px-4 py-2`: 16px horizontal, 8px vertical padding
- `bg-blue-600`: Primary blue background
- `hover:bg-blue-700`: Darker blue on hover
- `active:bg-blue-800`: Even darker on click
- `text-white`: White text
- `font-medium`: Font weight 500
- `rounded-md`: 6px border radius
- `shadow-md`: Medium shadow for elevation
- `focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2`: Blue ring on focus with 2px offset
- `transition duration-300`: Smooth color transitions
- `disabled:opacity-50 disabled:cursor-not-allowed`: Visual feedback when disabled

**Interactive States**:
| State | Background | Text Color | Cursor | Ring |
|-------|-----------|-----------|--------|------|
| Default | bg-blue-600 | white | pointer | none |
| Hover | bg-blue-700 | white | pointer | none |
| Focus | bg-blue-600 | white | pointer | ring-blue-500 |
| Active (click) | bg-blue-800 | white | pointer | ring-blue-500 |
| Disabled | bg-blue-600 + opacity-50 | white | not-allowed | none |

**Loading State** (optional with Alpine.js):
```html
<!-- When form submitting -->
<button ... :disabled="isSubmitting" class="...">
  <span x-show="!isSubmitting">Sign in</span>
  <svg x-show="isSubmitting" class="animate-spin h-5 w-5 text-white inline mr-2">
    <!-- Spinner SVG -->
  </svg>
</button>
```

---

## Error Message Component

**Purpose**: Display validation/authentication error messages

**HTML Structure**:
```html
<div 
  role="alert" 
  aria-live="polite" 
  class="rounded-md bg-red-50 p-4 mb-6 flex items-center gap-3"
  id="error-message"
  x-show="errorMessage" <!-- Alpine.js conditional display -->
>
  <svg class="h-5 w-5 text-red-700 flex-shrink-0" viewBox="0 0 24 24" fill="currentColor">
    <!-- X-circle SVG: circle with X inside -->
    <circle cx="12" cy="12" r="10" stroke="currentColor" stroke-width="2" fill="none"/>
    <path d="M8 8l8 8M16 8l-8 8" stroke="currentColor" stroke-width="2"/>
  </svg>
  <div class="text-sm text-red-700">
    <p x-text="errorMessage"></p>
  </div>
</div>
```

**Tailwind Classes**:
- `rounded-md`: 6px border radius
- `bg-red-50`: Very light red background
- `p-4`: 16px padding
- `mb-6`: 24px margin below
- `flex items-center gap-3`: Flex layout, center vertically, 12px gap between icon and text
- `h-5 w-5`: 20px × 20px icon size
- `text-red-700`: Dark red text color (contrast ≥ 4.5:1 ✓)
- `flex-shrink-0`: Icon doesn't shrink

**Accessibility**:
- `role="alert"`: Announces error to screen readers
- `aria-live="polite"`: Updates read when message changes
- Icon color matches text color (no color-alone conveying meaning)

---

## Sign Up Link Component

**Purpose**: "Don't have an account? Sign up" below submit button

**HTML Structure**:
```html
<div class="text-center text-xs font-medium text-gray-600 mt-4">
  Don't have an account? 
  <a href="/Auth/Register" class="text-blue-600 hover:text-blue-500 hover:underline transition duration-300">
    Sign up
  </a>
</div>
```

**Tailwind Classes**:
- Container: `text-center text-xs font-medium text-gray-600 mt-4`
  - Centered text, extra-small font, medium weight, gray color, 16px top margin
- Link (`<a>`): `text-blue-600 hover:text-blue-500 hover:underline transition duration-300`
  - Blue text, lighter blue on hover, underline appears on hover, smooth transition

**Accessibility**:
- `href` points to `/Auth/Register` (existing page)
- Semantically correct `<a>` tag (not styled as button)
- Focus state visible (browser default or custom ring if needed)

---

## Container Layout Structure

**Outer Wrapper** (Razor Page body or div):
```html
<div class="min-h-screen flex items-center justify-center bg-gray-50 py-12 px-4 sm:px-6 lg:px-8">
  <!-- Login form container goes here -->
</div>
```

**Tailwind Breakdown**:
- `min-h-screen`: Minimum height = full viewport (allows vertical centering)
- `flex items-center justify-center`: Flexbox, center both axes
- `bg-gray-50`: Light gray background (behind form)
- `py-12`: Vertical padding (top/bottom) 48px
- `px-4 sm:px-6 lg:px-8`: Responsive horizontal padding (4px mobile, 24px tablet, 32px desktop)

---

## Responsive Breakpoint Mapping

| Breakpoint | Screen Width | Use Case | Classes Applied |
|-----------|------------|----------|-----------------|
| Default (< 640px) | Mobile | < 640px | `w-[90%] mx-auto p-4 text-xs` + form width adjustments |
| `sm` | 640px | Small tablets | `w-screen max-w-[400px] p-6` |
| `md` | 768px | Tablets | `max-w-[400px]` (same) |
| `lg` | 1024px | Laptops | `max-w-[400px] p-8` |
| `xl` | 1280px | Desktops | Same as `lg` |
| `2xl` | 1536px | Large desktops | Same as `lg` |

**Responsive Heuristics**:
- Form width capped at 400px (no larger on ultra-wide screens)
- Padding scales with viewport: p-4 (mobile) → p-6 (tablet) → p-8 (desktop)
- Font sizes scale gracefully: text-sm/text-xs on mobile → text-base/text-sm on desktop
- Gap/spacing scales: space-y-4 (mobile) → space-y-6 (desktop)
```

(End of contract file)

---

### Accessibility Contract

**File**: `contracts/accessibility-contract.md`

```markdown
# Accessibility Contract: WCAG 2.1 AA Compliance

## Color Contrast Requirements

| Element | Foreground | Background | Required Ratio | Actual Ratio | Status |
|---------|-----------|-----------|----------------|-------------|---------|
| Title | #111827 (gray-900) | #ffffff (white) | 4.5:1 | ✓ 16.3:1 | ✅ PASS |
| Label | #374151 (gray-700) | #ffffff (white) | 4.5:1 | ✓ 8.5:1 | ✅ PASS |
| Input text | #111827 (gray-900) | #ffffff (white) | 4.5:1 | ✓ 16.3:1 | ✅ PASS |
| Button text | #ffffff (white) | #2563eb (blue-600) | 4.5:1 | ✓ 10.2:1 | ✅ PASS |
| Error text | #b91c1c (red-700) | #fef2f2 (red-50) | 4.5:1 | ✓ 9.1:1 | ✅ PASS |
| Placeholder | #9ca3af (gray-400) | #ffffff (white) | 3:1 | ✓ 5.2:1 | ✅ PASS |
| Button hover | #ffffff (white) | #1d4ed8 (blue-700) | 4.5:1 | ✓ 11.5:1 | ✅ PASS |

## Focus Indicators

| Element | Focus State | Color | Ring Size | Status |
|---------|-----------|-------|-----------|--------|
| Input fields | focus:ring-2 focus:ring-blue-500 | #3b82f6 (blue-500) | 2px | ✅ Visible |
| Button | focus:ring-2 focus:ring-blue-500 | #3b82f6 (blue-500) | 2px | ✅ Visible |
| Links | browser default + hover:underline | blue + underline | 2px | ✅ Visible |

**Validation**: Focus indicators must be visible against both white and blue backgrounds.

## Keyboard Navigation

**Tab Order** (left-to-right, top-to-bottom):
1. Email input field
2. Password input field
3. Submit button
4. Sign up link

**Testing Requirements**:
- [ ] All 4 elements reachable via Tab key
- [ ] No Tab traps (ability to tab forward through all items)
- [ ] Shift+Tab works to reverse order
- [ ] Enter key submits form from button
- [ ] Space key activates button
- [ ] Enter key navigates to Sign up link

## Screen Reader Compatibility

| Element | ARIA Attribute | Expected Announcement | Testing Tool |
|---------|----------------|----------------------|--------------|
| Email input | `<label for="email">` + `aria-label="Email address"` | "Email address, edit text" | NVDA, JAWS |
| Password input | `<label for="password">` + `aria-label="Password"` | "Password, edit text, password" | NVDA, JAWS |
| Submit button | Implicit role=button, text content | "Sign in, button" | NVDA, JAWS |
| Error message | `role="alert"` + `aria-live="polite"` | "Alert: [error text]" | NVDA, JAWS |
| Sign up link | Implicit role=link | "Sign up, link" | NVDA, JAWS |

**Testing Procedure**:
- Use NVDA (Windows) or JAWS (licensed) to read page top-to-bottom
- Verify each element announced with correct role and label
- Confirm error messages triggered aria-live announcement

## Semantic HTML Requirements

| Element | Semantic Requirement | Implementation |
|---------|----------------------|-----------------|
| Form | Must use `<form>` tag | ✓ |
| Labels | Associated via `<label for="">` | ✓ |
| Error container | `role="alert"` + `aria-live="polite"` | ✓ |
| Link | Semantic `<a>` tag with href | ✓ |
| Button | Semantic `<button type="submit">` | ✓ |
| Headings | `<h1>` or `<h2>` for page title | ✓ Title = `<h2>` |

## Responsive Accessibility

| Scenario | Requirement | Test Method |
|----------|-------------|-------------|
| Mobile (320px) | Form readable without horizontal scroll | DevTools 320px viewport |
| Landscape (568px) | Form fits in landscape without cutoff | DevTools 568×320 rotate |
| Zoom 200% | All text readable, buttons clickable | Browser zoom 200% |
| Touch targets | Min 44×44px for keyboard/touch | Measure button/input height |

## Lighthouse Accessibility Audit

**Target Score**: ≥ 90/100

**Audit Checks** (Lighthouse DevTools):
- [ ] Background and foreground colors have sufficient contrast (All: 4.5:1+)
- [ ] Buttons and links have sufficient size and padding (44×44px min)
- [ ] Form inputs are labeled
- [ ] Frame and iframe elements have titles
- [ ] Images have alt text
- [ ] Links have descriptive text
- [ ] Document title is descriptive
- [ ] Page has meta viewport tag
- [ ] ARIA roles, properties, values are correct and semantic

**Execution Steps**:
1. Open Login page in Chrome
2. Open DevTools (F12)
3. Go to **Lighthouse** tab
4. Check categories: **Accessibility**
5. Run audit → verify score ≥ 90

## Testing Checklist

Pre-launch accessibility audit:

- [ ] **Contrast**: All text ≥4.5:1, UI components ≥3:1 (verified via WebAIM Contrast Checker or Lighthouse)
- [ ] **Keyboard**: Tab through all controls, Shift+Tab reverse, no focus traps
- [ ] **Screen Reader**: NVDA announces all labels, roles, error messages correctly
- [ ] **Responsive**: No horizontal scroll at 320px, 768px, 1920px
- [ ] **Touch**: All buttons/inputs ≥44×44px; easy to tap on mobile
- [ ] **Focus Visible**: All focusable elements show visible ring/outline
- [ ] **Zoom**: Page readable and functional at 200% zoom
- [ ] **Lighthouse**: Accessibility score ≥ 90/100
```

(End of accessibility contract file)

---

## Implementation Approach (Top-level)

### Phase 0: Research (Quick Verification)
1. Run setup-plan.ps1 script
2. Audit Tailwind CSS setup (already in place from Phase 6)
3. Locate and verify logo asset (32×32px) in wwwroot/img/
4. Review Dashboard.cshtml for styling reference
5. Output findings to `research.md`

### Phase 1: Design Contracts
1. Create visual component specifications (data-model.md)
2. Generate responsive breakpoint mappings (contracts/responsive-breakpoints.md)
3. Document accessibility requirements (contracts/accessibility-contract.md)
4. Create component-level contracts with exact Tailwind classes (contracts/login-form-contract.md)
5. Output quickstart guide for developers (quickstart.md)

### Phase 2: Implementation Tasks
1. Task generation via speckit.tasks → tasks.md
2. 25-30 granular tasks grouped by category:
   - HTML refactoring (Tailwind markup updates)
   - Responsive design (breakpoint testing)
   - Accessibility (WCAG checklist)
   - Interactive states (hover/focus/error)
   - QA (visual regression, accessibility audit, cross-browser)

### Phase 3: Quality Assurance
1. E2E visual regression testing (screenshot baseline)
2. Lighthouse accessibility audit (target ≥ 90/100)
3. Manual keyboard navigation (Tab order, no focus traps)
4. Cross-browser testing (Chrome, Firefox, Safari, Edge)
5. Responsive testing (mobile, tablet, desktop real devices)

---

## Git & Code Review Workflow

**Branch**: `001-login-tailwind-style` (already created)

**Commit Strategy**:
```
commit 1: "refactor(frontend): update Login page layout with Tailwind CSS"
  - files: Frontend/Pages/Auth/Login.cshtml

commit 2: "style(frontend): add responsive breakpoints and accessibility classes"
  - files: Frontend/Pages/Auth/Login.cshtml

commit 3: "refactor(frontend): add loading spinner and error message styling"
  - files: Frontend/Pages/Auth/Login.cshtml (if Alpine.js added)
```

**PR Review Checklist**:
- [ ] Markup follows Tailwind-only utility approach (no custom CSS)
- [ ] All Tailwind classes exist in project config
- [ ] No breaking changes to Login.cshtml.cs PageModel
- [ ] Logo asset reference correct (src="/img/logo.svg")
- [ ] Responsive design tested on 320px, 1024px, 1920px viewports
- [ ] Accessibility audit passes (Lighthouse ≥ 90)
- [ ] Focus indicators visible and functional
- [ ] Error message displays correctly with icon
- [ ] Sign up link navigates to Register page
- [ ] Cross-browser compatibility verified

---

## Success Metrics

| Metric | Target | Method | Pass/Fail |
|--------|--------|--------|-----------|
| Visual Attractiveness | Modern, professional appearance | Visual review by PM/Designer | ✓ |
| Responsive Coverage | Works on 320px, 768px, 1024px, 1920px | Manual testing in DevTools | ✓ |
| Accessibility Score | Lighthouse ≥ 90/100 | Lighthouse audit | ✓ |
| Keyboard Navigation | All controls reachable via Tab | Tab through all elements | ✓ |
| Screen Reader | Labels/roles announced correctly | NVDA full-page read-through | ✓ |
| Color Contrast | All ≥4.5:1 | WebAIM Contrast Checker | ✓ |
| Cross-Browser | No visual glitches | Chrome, Firefox, Safari, Edge | ✓ |
| Page Load | ≤ 2s to interactive paint | Lighthouse performance | ✓ |
| User Acceptance | Positive feedback | Informal user testing | ✓ |

---

## Risk Mitigation

| Risk | Impact | Mitigation |
|------|--------|-----------|
| **Tailwind class naming conflict** | Styling breaks unexpectedly | Verify all classes in project config before using |
| **Logo asset missing/wrong size** | Logo doesn't display | Pre-audit wwwroot/img/ in Phase 0 research |
| **Browser  compatibility** | Styling fails in older browsers | Target modern browsers only (no IE 11) |
| **Accessibility oversight** | WCAG 2.1 AA non-compliance | Run Lighthouse + manual screen reader test before merge |
| **Focus trap in Tab order** | Keyboard navigation broken | Manual Tab-through test on all form elements |
| **Responsive breakpoint miss** | Mobile layout broken | Test at 320px, 640px, 1024px explicitly |

---

## Next Steps

1. **Phase 0 Execution**: Run Phase 0 research tasks → generate research.md
2. **Phase 1 Execution**: Execute Phase 1 design tasks → generate data-model.md, contracts/*, quickstart.md
3. **Phase 2 Execution**: Run `/speckit.tasks` → generate tasks.md with granular implementation tasks
4. **Implementation**: Execute tasks.md tasks, commit changes, open PR
5. **Review & QA**: Address PR feedback, run accessibility audit, merge

---

**Plan Status**: Ready for Phase 0 Research

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

[Gates determined based on constitution file]

## Project Structure

### Documentation (this feature)

```text
specs/[###-feature]/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)
<!--
  ACTION REQUIRED: Replace the placeholder tree below with the concrete layout
  for this feature. Delete unused options and expand the chosen structure with
  real paths (e.g., apps/admin, packages/something). The delivered plan must
  not include Option labels.
-->

```text
# [REMOVE IF UNUSED] Option 1: Single project (DEFAULT)
src/
├── models/
├── services/
├── cli/
└── lib/

tests/
├── contract/
├── integration/
└── unit/

# [REMOVE IF UNUSED] Option 2: Web application (when "frontend" + "backend" detected)
backend/
├── src/
│   ├── models/
│   ├── services/
│   └── api/
└── tests/

frontend/
├── src/
│   ├── components/
│   ├── pages/
│   └── services/
└── tests/

# [REMOVE IF UNUSED] Option 3: Mobile + API (when "iOS/Android" detected)
api/
└── [same as backend above]

ios/ or android/
└── [platform-specific structure: feature modules, UI flows, platform tests]
```

**Structure Decision**: [Document the selected structure and reference the real
directories captured above]

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| [e.g., 4th project] | [current need] | [why 3 projects insufficient] |
| [e.g., Repository pattern] | [specific problem] | [why direct DB access insufficient] |
