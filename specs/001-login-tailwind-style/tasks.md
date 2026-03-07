# Tareas de Implementación: Estilizado Visual Atractivo para Login con Tailwind

**Feature**: `001-login-tailwind-style` | **Rama**: `001-login-tailwind-style` | **Fecha**: 2026-03-07
**Total de Tareas**: 45 | **Plazo Estimado**: 4-6 horas | **Esfuerzo**: Bajo-Medio

---

## Estrategia de Implementación

**MVP Scope**: Tareas T1-T36 (markup, responsividad, validación, accesibilidad, estados interactivos)
**QA Scope**: Tareas T37-T45 (testing exhaustivo)
**Parallelización**: Tareas marcadas con `[P]` pueden ejecutarse en paralelo; tareas sin marcar tienen dependencias secuenciales
**Orden Recomendado**: Completar por Fase (1-7). Dentro de cada fase, ejecutar tareas `[P]` en paralelo.

---

## Phase 1: Setup & Prerequisites

- [ ] T001 Prepare development environment (branch 001-login-tailwind-style created, Tailwind verified, Alpine.js available)
- [ ] T002 [P] Verify logo asset exists at wwwroot/img/logo.svg (32×32px, correct format)
- [ ] T003 [P] Audit Dashboard.cshtml for brand color reference (extract exact classes: bg-blue-600, text-gray-900, etc.)

---

## Phase 2: Foundational - HTML Markup Refactoring

**Goal**: Implement core form structure with Tailwind styling (Frontend/Pages/Auth/Login.cshtml)
**Independent Test**: Form displays centered, logo visible, all fields present
**Dependency**: Phase 1 complete

- [ ] T004 [P] Add outer wrapper with min-h-screen flexbox centering (min-h-screen flex items-center justify-center bg-gray-50) in Login.cshtml
- [ ] T005 [P] Add form container with responsive width (w-full max-w-md, 90% on mobile, fixed 400px desktop, bg-white rounded-lg shadow-lg)
- [ ] T006 [P] Add SauronSheet logo image element (h-8 w-8, mx-auto, mb-4, src="/img/logo.svg", alt="SauronSheet")
- [ ] T007 [P] Add form title "Sign in to your account" (h2, text-center text-3xl font-extrabold text-gray-900, mt-6 mb-8)
- [ ] T008 [P] Add email input with label (type="email", required, full width, border border-gray-300, rounded-md, px-3 py-2, placeholder, aria-label)
- [ ] T009 [P] Add password input wrapper with relative positioning for toggle icon overlay (div class="relative", password input pr-10 to accommodate icon)
- [ ] T010 [P] Implement password toggle eye/eye-off icon (SVG icons, 20px, positioned absolute right-3 top-1/2, Alpine.js @click to toggle type="password" ↔ type="text")
- [ ] T011 [P] Add submit button (type="submit", w-full, bg-blue-600, text white, font-medium, rounded-md, text-base, py-2 px-4)
- [ ] T012 [P] Add error message container placeholder (div role="alert" aria-live="polite", bg-red-50, p-4, mb-6, with SVG X-circle icon, text-red-700, initially x-show="false")
- [ ] T013 [P] Add "Sign up" link (text-center text-xs font-medium text-gray-600, mt-4, link class="text-blue-600 hover:text-blue-500", href="/Auth/Register")
- [ ] T014 Add Alpine.js data object for form state (showPassword: false, errorMessage: null, isSubmitting: false) to handle interactivity

**Acceptance Criteria**:
- [ ] Form renders centered on screen with white background
- [ ] Logo displays 32×32px at top center
- [ ] Email and password inputs visible with labels
- [ ] Submit button full-width, blue background
- [ ] Sign up link displays below button
- [ ] No Tailwind class errors in browser DevTools

---

## Phase 3: User Story 1 - Responsive Design Implementation

**Goal**: Implement mobile-first responsive design across all breakpoints
**Independent Test**: Form displays correctly on 320px (mobile), 768px (tablet), 1920px (desktop)
**Dependency**: Phase 2 complete

- [ ] T015 [P] [US1] Implement mobile breakpoint padding (p-4 on inputs/form, space-y-6 between fields, font scales: text-xs labels, text-sm button)
- [ ] T016 [P] [US1] Implement tablet breakpoint adjustments (640px sm: prefix, padding p-6, container maintains 400px max-w-md)
- [ ] T017 [P] [US1] Implement desktop breakpoint styling (1024px lg: prefix, padding p-8, container 400px centered, full vertical alignment)
- [ ] T018 [P] [US1] Ensure password toggle eye icon is 44×44px minimum (hit target h-11 w-11 on all breakpoints, easy tap on mobile)
- [ ] T019 [P] [US1] Test responsive behavior DevTools (320px, 768px, 1024px, 1920px - verify no horizontal scroll, legibility maintained)

**Acceptance Criteria**:
- [ ] Mobile (< 640px): Form 90% width, padding p-4, container readable
- [ ] Tablet (640-1024px): Form 400px width, padding p-6, centered
- [ ] Desktop (≥ 1024px): Form 400px width, padding p-8, full page centering
- [ ] Toggle icon always ≥ 44×44px
- [ ] No horizontal scroll at any breakpoint
- [ ] Fonts scale appropriately (text-3xl title readable, text-xs labels visible on mobile)

---

## Phase 4: User Story 2 - Validation & Form Behavior

**Goal**: Implement HTML5 validation, auto-focus, Enter key behavior, and error handling
**Independent Test**: Form validates email on submit, focuses email on load, Enter on password submits
**Dependency**: Phase 2 complete (can parallel with Phase 3)

- [ ] T020 [P] [US2] Ensure HTML5 validation enabled (type="email" required on email input, required on password input) for browser-native feedback
- [ ] T021 [P] [US2] Set focus to email field on page load (use HTML5 autofocus="true" on email input OR Alpine.js @init directive)
- [ ] T022 [P] [US2] Implement Enter key behavior on password field (pressing Enter in password → submit form via form onsubmit or Alpine.js @keydown.enter)
- [ ] T023 [P] [US2] Connect form submit to PageModel (ensure form POST to existing Login PageModel endpoint, error messages populate from ModelState.ErrorValues)
- [ ] T024 [P] [US2] Implement error display + focus return to email on login failure (Alpine.js watch on error message from server, auto-focus email field, aria-live="polite" announces error)

**Acceptance Criteria**:
- [ ] Email field focused automatically on page load
- [ ] Pressing Enter on password field submits form
- [ ] HTML5 validation prevents invalid emails from submitting
- [ ] Error messages display with X-circle icon, bg-red-50, text-red-700
- [ ] Focus returns to email field when error occurs
- [ ] Screen readers announce error via aria-live

---

## Phase 5: User Story 3 - Accessibility Compliance (WCAG 2.1 AA)

**Goal**: Achieve WCAG 2.1 AA compliance - keyboard navigation, screen reader support, color contrast
**Independent Test**: All controls reachable via Tab, screen reader announces all labels/buttons, contrast ≥ 4.5:1
**Dependency**: Phase 2 complete (can parallel with Phases 3-4)

- [ ] T025 [P] [US3] Verify WCAG 2.1 AA color contrast (4.5:1 minimum: title 16.3:1 ✓, labels 8.5:1 ✓, button text 10.2:1 ✓, error text 9.1:1 ✓)
- [ ] T026 [P] [US3] Ensure all form inputs have associated labels (label for="email/password", displayed visually)
- [ ] T027 [P] [US3] Verify password toggle icon keyboard accessibility (Tab to reach, Enter/Space to activate, aria-pressed reflects state true/false, aria-label="Show/Hide password")
- [ ] T028 [P] [US3] Configure keyboard navigation Tab order (Tab sequence: email → password (+ toggle icon) → submit button → signup link; no Tab traps)
- [ ] T029 [P] [US3] Add aria-live="polite" and role="alert" to error message container (announces error to screen readers on display)
- [ ] T030 [P] [US3] Add focus ring styling to all interactive elements (focus:ring-2 focus:ring-blue-500 on inputs and buttons, 2px blue visible)
- [ ] T031 [P] [US3] Test with screen reader NVDA/JAWS (verify: email announced "Email address, edit text, required", password announced "Password, edit text, password, required", button "Sign in, button", error "Alert: [message]")

**Acceptance Criteria**:
- [ ] All text ≥ 4.5:1 contrast ratio
- [ ] Tab navigation works, no focus traps
- [ ] Focus ring visible (2px blue) on all interactive elements
- [ ] Screen reader announces all labels and button purposes
- [ ] Error messages announced as alerts via aria-live
- [ ] Lighthouse accessibility score ≥ 90/100

---

## Phase 6: Polish - Interactive States & Loading Behavior

**Goal**: Implement smooth transitions, loading spinner, disabled states, and button interactions
**Independent Test**: Transitions smooth (0.3s), spinner visible during submit, inputs/button disabled, form state preserved during loading
**Dependency**: Phase 2 complete (can parallel with other phases)

- [ ] T032 [P] Add 0.3s transition classes to all interactive elements (transition duration-300 on button, inputs, toggle icon for smooth color/state changes)
- [ ] T033 [P] Implement button hover state (bg-blue-700 on hover, text white, smooth transition, cursor pointer)
- [ ] T034 [P] Implement input focus state (ring-2 ring-blue-500, border-blue-500 focus, shadow-sm, transition 300ms)
- [ ] T035 [P] Add error state styling for form inputs (border-red-500, focus:ring-red-500 when error triggered, text-red-700)
- [ ] T036 Implement loading spinner during form submission (Alpine.js x-show conditional, spinner SVG inline in button, rotate animation, visible when isSubmitting=true)
- [ ] T037 **REQUIRED**: Implement button disabled state during submission (disabled="true" + opacity-50 + cursor-not-allowed, prevents accidental re-clicks, screen readers announce "button disabled")
- [ ] T038 **REQUIRED**: Implement inputs disabled state during submission (email & password inputs both disabled="true" + opacity-50, toggle icon disabled, prevents edits during POST)
- [ ] T039 Implement password toggle icon state transitions (aria-pressed toggles true/false, icon SVG switches eye ↔ eye-off, smooth transition 300ms)

**Acceptance Criteria**:
- [ ] All interactive elements have smooth 0.3s transitions
- [ ] Button changes to darker blue on hover (bg-blue-700)
- [ ] Inputs show blue ring on focus
- [ ] Error state shows red border + background
- [ ] Loading spinner visible inside button when submitting
- [ ] Button disabled during loading (opacity-50, cursor-not-allowed)
- [ ] Both inputs disabled during loading (cannot type/edit)
- [ ] Password toggle icon state reflects visibility toggle
- [ ] Form re-enables after server response (success or error)

---

## Phase 7: Quality Assurance & Testing

**Goal**: Comprehensive testing - visual regression, accessibility audit, keyboard/screen reader, cross-browser, responsive
**Independent Test**: All tests pass; Lighthouse ≥ 90; no visual glitches across browsers/devices
**Dependency**: All implementation phases (T001-T039) complete

- [ ] T040 [P] E2E visual regression baseline (Playwright/Cypress: take screenshot of /Auth/Login at 1920×1080, baseline for future regressions)
- [ ] T041 [P] Lighthouse accessibility audit (open DevTools, run Lighthouse audit, target score ≥ 90/100, record baseline)
- [ ] T042 [P] Manual keyboard navigation test (Tab through all controls: email → password → password toggle → submit → signup, Shift+Tab reverse, Enter submits, Space activates button, no Tab traps)
- [ ] T043 [P] Manual page load test (navigate to /Auth/Login, verify email field is auto-focused immediately, cursor in email input, no other focus issues)
- [ ] T044 [P] Manual password toggle test (click eye icon, type changes from password ↔ text, click again hides, no data loss, icon rotates smoothly)
- [ ] T045 [P] Manual focus management test (submit form with invalid email, trigger error, verify focus automatically moves to email field, error message announced)
- [ ] T046 [P] Manual loading state test (submit valid form, observe spinner inside button + button disabled, inputs disabled (cannot type), Enter key blocked, wait for response, form re-enables)
- [ ] T047 [P] Manual cross-browser test (Chrome latest, Firefox latest, Safari latest, Edge latest: verify no visual glitches, styles consistent, functionality works)
- [ ] T048 [P] Manual responsive test on real devices (iPhone 12/14, Android phone: verify 44×44px toggle hit target, landscape orientation works, text readable, no scroll issues, form centered)

**Acceptance Criteria**:
- [ ] E2E screenshot baseline created (no regression)
- [ ] Lighthouse accessibility ≥ 90/100
- [ ] Tab navigation flawless, no focus traps
- [ ] Email focused on page load
- [ ] Password toggle works: click to show/hide
- [ ] Error triggers focus return + announcement
- [ ] Loading state: spinner visible, inputs disabled, re-enabled on response
- [ ] Cross-browser: Chrome, Firefox, Safari, Edge all pass
- [ ] Responsive: mobile (< 640px), tablet (640-1024px), desktop (≥ 1024px) all pass
- [ ] Mobile: 44×44px touch targets, landscape works

---

## Dependency Graph

```
Phase 1 (Setup)
    ↓
Phase 2 (Markup - T004-T014)
    ↓
├─ Phase 3 (Responsive - T015-T019) [parallel]
├─ Phase 4 (Validation - T020-T024) [parallel]
└─ Phase 5 (Accessibility - T025-T031) [parallel]
    ↓
Phase 6 (Interactive States - T032-T039) [waits for Phase 2]
    ↓
Phase 7 (QA Testing - T040-T048) [waits for Phase 6]
```

**Parallelization Opportunities**:
- **Phases 3, 4, 5**: All can run in parallel after Phase 2 (different concerns: responsive, validation, accessibility)
- **Within Phase 3**: T015-T019 all parallelizable (responsive adjustments, independent)
- **Within Phase 4**: T020-T024 mostly parallelizable (validation, focus, error handling overlap slightly but no blockers)
- **Within Phase 5**: T025-T031 all parallelizable (accessibility verification, independent checks)
- **Within Phase 6**: T032-T035 parallelizable (styles), T036-T039 sequential (states depend on Alpine data)
- **Within Phase 7**: T040-T048 all parallelizable (different test types/tools)

---

## Estimated Effort Per Phase

| Phase | Nombre | Tareas | Tiempo Estimado | Prioridad |
|-------|--------|--------|-----------------|-----------|
| 1 | Setup | 3 | 15 min | CRITICAL |
| 2 | Markup | 11 | 60-75 min | CRITICAL |
| 3 | Responsive | 5 | 30-45 min | HIGH |
| 4 | Validation | 5 | 30-45 min | HIGH |
| 5 | Accessibility | 7 | 45-60 min | HIGH |
| 6 | Interactive States | 8 | 45-60 min | MEDIUM |
| 7 | QA Testing | 9 | 60-90 min | HIGH (pre-merge) |
| **TOTAL** | | **48 tareas** | **4-6 horas** | |

**Timeline**: 1-2 jornadas de desarrollo (si se paraleliza Phases 3-5)

---

## Success Criteria (Global)

- [ ] All 48 tasks completed and checked-off
- [ ] No Tailwind class errors in browser console
- [ ] Form displays centered, white background, no layout issues
- [ ] Responsive on 320px, 768px, 1920px: verified in DevTools
- [ ] Keyboard navigation works (Tab, Shift+Tab, Enter, Space)
- [ ] Screen reader announces all elements correctly (NVDA/JAWS)
- [ ] Lighthouse accessibility ≥ 90/100
- [ ] Spinner visible during form submission, inputs disabled
- [ ] Error message displays with icon, focus returns to email
- [ ] Password toggle works (show/hide, icon changes, accessible by keyboard)
- [ ] Cross-browser: Chrome, Firefox, Safari, Edge latest 2 versions
- [ ] Mobile real devices: 44×44px toggle hit target, landscape orientation, readable
- [ ] No regression vs. baseline screenshot
- [ ] PR review approved: no style conflicts, clean Tailwind utilities, accessible markup

---

## Notes for Developer

**Frontend File to Modify**: `src/SauronSheet.Frontend/Pages/Auth/Login.cshtml`

**PageModel** (`Login.cshtml.cs`): **NO CHANGES** - existing authentication logic stays intact

**Architecture Compliance**:
- ✅ Frontend-only work (no Application/Domain/Infrastructure changes)
- ✅ Razor Pages + Tailwind CSS only (no custom CSS beyond utility classes)
- ✅ Alpine.js for interactivity (minimal, contained in view)
- ✅ Constitution: Clean Architecture maintained (no upward layer refs)

**Key Dependencies**:
- Tailwind CSS 3.4+ (already integrated)
- Alpine.js 3.x (already available)
- Logo asset: `/img/logo.svg` (verify 32×32px)

**Reference Files**:
- Dashboard.cshtml: Brand color/style reference
- contracts/login-form-contract.md: Exact HTML/Tailwind mappings
- accessibility-contract.md: WCAG 2.1 AA requirements
- quickstart.md: Developer quick-start guide

**Testing Tools**:
- Browser DevTools: responsive testing, console errors
- Lighthouse (Chrome): accessibility audit (target ≥ 90)
- NVDA (Windows) / JAWS (licensed): screen reader testing
- Playwright/Cypress: visual regression baseline

---

## Changelog

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2026-03-07 | Initial task generation: 48 tasks organized across 7 phases with parallelization opportunities |
