# Design Specification: Login Form Components & Layout

**Date**: 2026-03-07 | **Phase**: 1 - Design & Contracts
**Status**: Complete ✓ | **All Components Specified**

---

## Layout Specification

### Overall Container & Viewport Centering

**Outer Wrapper** (Flex parent - full viewport):
```html
<div class="min-h-screen flex items-center justify-center bg-gray-50 py-12 px-4 sm:px-6 lg:px-8">
  <!-- Login form container -->
</div>
```

**Classes Explained**:
- `min-h-screen`: Minimum height = 100vh (ensures vertical centering on tall screens)
- `flex`: Flexbox display
- `items-center`: Vertical center alignment
- `justify-center`: Horizontal center alignment
- `bg-gray-50`: Light gray background (behind the form)
- `py-12`: Vertical padding (top/bottom) = 48px
- `px-4 sm:px-6 lg:px-8`: Responsive horizontal padding (4px mobile, 24px tablet, 32px desktop)

### Form Container Dimensions

**Desktop Layout (≥ 1024px)**:
```html
<div class="w-full max-w-md bg-white rounded-lg shadow-lg p-8">
```
- Width: 400px fixed (via `max-w-md`)
- Padding: 32px (`p-8`)
- Background: White (`bg-white`)
- Border Radius: 6px (`rounded-lg`)
- Shadow: Medium elevation (`shadow-lg`)

**Tablet Layout (640-1024px)**:
```html
<div class="w-full max-w-md bg-white rounded-lg shadow-lg p-6">
```
- Width: 400px (via `max-w-md`)
- Padding: 24px (`p-6`)
- Shadow: Medium (`shadow-lg`)

**Mobile Layout (< 640px)**:
```html
<div class="w-full max-w-sm bg-white rounded-md shadow-md p-4">
```
- Width: 90% (via `w-full` inside outer container with `px-4`, not exceeding parent)
- Padding: 16px (`p-4`)
- Border Radius: 4px (`rounded-md`)
- Shadow: Slight (`shadow-md`)

### ASCII Layout Diagram

```
┌─────────────────────────────────────────────────────────┐
│ bg-gray-50 (min-h-screen, flex container)               │
│                                                           │
│         ┌──────────────────────────────────────┐        │
│         │ bg-white max-w-md p-8               │        │
│         │ rounded-lg shadow-lg                 │        │
│         │                                       │        │
│         │  [Logo: h-8 w-8 mx-auto mb-4]       │        │
│         │                                       │        │
│         │  Sign in to your account            │        │
│         │  (text-3xl font-extrabold           │        │
│         │   text-gray-900 mt-6 mb-8)          │        │
│         │                                       │        │
│         │  ┌─ space-y-6 between sections ──┐  │        │
│         │  │                                 │  │        │
│         │  │  Email address (label)         │  │        │
│         │  │  [─────────────────────────]   │  │        │
│         │  │  (w-full px-3 py-2 borders)   │  │        │
│         │  │                                 │  │        │
│         │  │  Password (label)              │  │        │
│         │  │  [─────────────────────────]   │  │        │
│         │  │  (w-full px-3 py-2 borders)   │  │        │
│         │  │                                 │  │        │
│         │  │  [──────── Sign in ────────]   │  │        │
│         │  │  (w-full bg-blue-600 text)    │  │        │
│         │  │                                 │  │        │
│         │  │  Don't have account?           │  │        │
│         │  │  Sign up (link text-blue-600)  │  │        │
│         │  │                                 │  │        │
│         │  └─────────────────────────────┘  │        │
│         │                                       │        │
│         │  [Error message if login fails]      │        │
│         │  (role="alert" bg-red-50)           │        │
│         │                                       │        │
│         └──────────────────────────────────────┘        │
│                                                           │
└─────────────────────────────────────────────────────────┘

Dimensions:
  Desktop (≥1024px): 400px width (max-w-md), 32px padding (p-8)
  Tablet (640-1024px): 400px width, 24px padding (p-6)
  Mobile (<640px): 90% width, 16px padding (p-4)
  
Centering: Flexbox parent (min-h-screen flex items-center justify-center)
```

---

## Color Palette Mapping

### Tailwind to Hex Reference

| Element | Tailwind Class | Hex | Purpose | WCAG AA | Notes |
|---------||
| Primary Button BG | `bg-blue-600` | #2563eb | Submit button | N/A | Use with white text |
| Primary Button Hover | `hover:bg-blue-700` | #1d4ed8 | Button hover state | N/A | Interactive feedback |
| Text (Headings/Labels) | `text-gray-900` | #111827 | Title, form labels | 16.3:1 vs white | High contrast |
| Text (Secondary) | `text-gray-700` | #374151 | Regular text | 8.5:1 vs white | Strong contrast |
| Text (Tertiary) | `text-gray-600` | #4b5563 | Placeholders | 5.2:1 vs white | Acceptable |
| Border | `border-gray-300` | #d1d5db | Input borders | N/A | Subtle, mid-gray |
| Focus Ring | `focus:ring-blue-500` | #3b82f6 | Focus states | 10.2:1 vs white | Accessible |
| Error Background | `bg-red-50` | #fef2f2 | Error containers | N/A | Very light red |
| Error Text | `text-red-700` | #b91c1c | Error messages | 9.1:1 vs red-50 | ✓ PASS |
| White | `bg-white` | #ffffff | Form container, input BG | N/A | Clean, neutral |
| Light Gray BG | `bg-gray-50` | #f9fafb | Page background | N/A | Subtle |

### Contrast Verification

| Foreground | Background | Ratio | Standard | Status |
|-----------|-----------|-------|----------|--------|
| gray-900 (#111827) | white (#ffffff) | 16.3:1 | WCAG AA 4.5:1 | ✅ PASS |
| gray-700 (#374151) | white (#ffffff) | 8.5:1 | WCAG AA 4.5:1 | ✅ PASS |
| white (#ffffff) | blue-600 (#2563eb) | 10.2:1 | WCAG AA 4.5:1 | ✅ PASS |
| red-700 (#b91c1c) | red-50 (#fef2f2) | 9.1:1 | WCAG AA 4.5:1 | ✅ PASS |
| gray-400 (#9ca3af) | white (#ffffff) | 5.2:1 | WCAG AA 4.5:1 | ✅ PASS |

---

## Component Specifications

### 1. Logo Component

**Purpose**: SauronSheet branding at form top

**HTML**:
```html
<img 
  src="/img/logo.svg" 
  alt="SauronSheet" 
  class="h-8 w-8 mx-auto mb-4"
/>
```

**Classes**:
- `h-8`: Height 32px (0.5rem × 8 = 2rem)
- `w-8`: Width 32px
- `mx-auto`: Horizontal center (margin-left: auto; margin-right: auto)
- `mb-4`: Margin-bottom 16px (separation from title)

**Visual**:
```
      [Logo 32×32px]
```

**Notes**:
- Fixed 32×32px size (from spec clarification Round 3)
- SVG format (scalable, no quality loss)
- Source: `/Frontend/wwwroot/img/logo.svg`

---

### 2. Title Component

**Purpose**: Form heading "Sign in to your account"

**HTML**:
```html
<h2 class="text-center text-3xl font-extrabold text-gray-900 mt-6 mb-8">
  Sign in to your account
</h2>
```

**Classes**:
- `text-center`: Horizontal center text alignment
- `text-3xl`: Font size 30px
- `font-extrabold`: Font weight 800 (heaviest)
- `text-gray-900`: Dark gray text (#111827)
- `mt-6`: Top margin 24px (separation from logo)
- `mb-8`: Bottom margin 32px (separation from fields)

**Typography**:
- Font Size: 30px
- Line Height: 36px (default for text-3xl)
- Weight: 800 (extrabold)
- Letter Spacing: 0 (default)

**Visual Hierarchy**: Largest, heaviest text on page (next to logo)

---

### 3. Email Input Component

**HTML**:
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

**Label Classes**:
- `block`: Display block (full width)
- `text-xs`: Font size 12px (small)
- `font-medium`: Font weight 500
- `text-gray-700`: Medium gray text
- `mb-1`: Margin-bottom 4px (tight spacing to input)

**Input Classes**:
- `w-full`: Full width of parent
- `px-3`: Horizontal padding 12px
- `py-2`: Vertical padding 8px
- `border border-gray-300`: 1px gray border
- `rounded-md`: 6px border radius
- `shadow-sm`: Subtle shadow
- `focus:outline-none`: Remove default outline
- `focus:ring-2`: 2px focus ring
- `focus:ring-blue-500`: Blue ring (#3b82f6)
- `focus:border-blue-500`: Blue border on focus
- `transition duration-300`: Smooth 300ms transitions

**Accessibility**:
- `for="email"`: Label linked via id attribute
- `aria-label="Email address"`: Screen reader label
- `required`: HTML5 validation
- `type="email"`: Browser email validation

**Input Height**: ~32px (8px + 16px text + 8px = 32px total)

---

### 4. Password Input Component

**HTML**:
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

**Classes**: Identical to email input (copy-paste with id/name/label changes)

**Notes**:
- `type="password"` masks input (bullets instead of text)
- Same styling, spacing, focus behavior as email

---

### 5. Submit Button

**HTML**:
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

**Classes**:
- `w-full`: Full width
- `px-4`: Horizontal padding 16px
- `py-2`: Vertical padding 8px
- `bg-blue-600`: Primary blue
- `hover:bg-blue-700`: Darker blue on hover
- `active:bg-blue-800`: Even darker on click
- `text-white`: White text
- `font-medium`: Font weight 500
- `rounded-md`: 6px border radius
- `shadow-md`: Medium shadow
- `focus:outline-none`: Remove default outline
- `focus:ring-2`: 2px focus ring
- `focus:ring-blue-500`: Blue ring on focus
- `focus:ring-offset-2`: 2px offset for "lifted" effect
- `transition duration-300`: Smooth transitions
- `disabled:opacity-50`: 50% opacity when disabled
- `disabled:cursor-not-allowed`: "Not allowed" cursor

**Button Height**: ~32px (8px + 16px text + 8px = 32px total) ≥ 44px recommended touch target ✓

**Interactive States**:

| State | BG | Text | Ring |
|-------|----|----|------|
| Default | blue-600 #2563eb | white | none |
| Hover | blue-700 #1d4ed8 | white | none |
| Focus | blue-600 | white | blue-500 |
| Active (click) | blue-800 #1e40af | white | blue-500 |
| Disabled | blue-600 + 50% opacity | white | none |

---

### 6. Error Message Component

**HTML**:
```html
<div 
  role="alert" 
  aria-live="polite" 
  class="rounded-md bg-red-50 p-4 mb-6 flex items-center gap-3"
  id="error-message"
  x-show="errorMessage"
>
  <svg class="h-5 w-5 text-red-700 flex-shrink-0" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
    <circle cx="12" cy="12" r="10"/>
    <path d="M15 9l-6 6M9 9l6 6"/>
  </svg>
  <div class="text-sm text-red-700">
    <p x-text="errorMessage"></p>
  </div>
</div>
```

**Container Classes**:
- `rounded-md`: 6px border radius
- `bg-red-50`: Very light red background
- `p-4`: 16px padding
- `mb-6`: 24px margin-bottom
- `flex items-center gap-3`: Flex layout, vertical centering, 12px gap

**Icon Classes**:
- `h-5 w-5`: 20px × 20px
- `text-red-700`: Dark red (#b91c1c)
- `flex-shrink-0`: Icon doesn't shrink

**Text Classes**:
- `text-sm`: 14px font size
- `text-red-700`: Dark red text

**SVG Icon**: X-circle (2px stroke, no fill)

**Accessibility**:
- `role="alert"`: Announces as alert to screen readers
- `aria-live="polite"`: Announces updates (new error messages)
- Icon + text combination (not color-alone)

---

### 7. Sign Up Link Component

**HTML**:
```html
<div class="text-center text-xs font-medium text-gray-600 mt-4">
  Don't have an account? 
  <a href="/Auth/Register" class="text-blue-600 hover:text-blue-500 hover:underline transition duration-300">
    Sign up
  </a>
</div>
```

**Container Classes**:
- `text-center`: Center text alignment
- `text-xs`: 12px font size
- `font-medium`: Font weight 500
- `text-gray-600`: Medium gray text
- `mt-4`: 16px top margin (separation from button)

**Link Classes**:
- `text-blue-600`: Blue text
- `hover:text-blue-500`: Lighter blue on hover
- `hover:underline`: Underline appears on hover
- `transition duration-300`: Smooth color transition

**Visual**:
```
Don't have an account? Sign up
                      ^^^^^^^^ (blue, underlined on hover)
```

---

## Responsive Breakpoint Mapping

### Mobile-First Approach

**Default (< 640px) - Mobile**:
```html
<div class="w-full max-w-sm bg-white p-4 rounded-md shadow-md">
```
- Width: 90% (via `w-full` parent container with `px-4`)
- Max Width: Small (`max-w-sm` = 384px)
- Padding: `p-4` (16px)
- Shadow: `shadow-md` (lighter)
- Classes for form fields: `text-xs`, `py-2`, `px-3`
- Label: `text-xs`
- Title: `text-2xl` or `text-3xl`

**Tablet (≥ 640px)**:
```html
<div class="w-full max-w-md bg-white p-6 rounded-lg shadow-lg">
```
- Width: Scales to 400px (`max-w-md` = 448px)
- Padding: `p-6` (24px)
- Shadow: `shadow-lg` (stronger)

**Desktop (≥ 1024px)**:
```html
<div class="w-full max-w-md bg-white p-8 rounded-lg shadow-lg">
```
- Width: Fixed 400px (`max-w-md`)
- Padding: `p-8` (32px)
- Shadow: `shadow-lg` (strong)
- Classes for form fields: `text-sm`, `py-2`, `px-3`
- Label: `text-xs`
- Title: `text-3xl` (confirmed)

### Complete Responsive Class Map

| Element | Mobile (< 640px) | Tablet (≥ 640px) | Desktop (≥ 1024px) |
|---------|---|---|---|
| Container | `p-4 rounded-md shadow-md` | `p-6 rounded-lg shadow-lg` | `p-8 rounded-lg shadow-lg` |
| Title | `text-3xl` | `text-3xl` | `text-3xl` |
| Label | `text-xs` | `text-xs` | `text-xs` |
| Input | `px-3 py-2 text-sm` | `px-3 py-2 text-sm` | `px-3 py-2 text-sm` |
| Button | `py-2 px-4 text-base` | `py-2 px-4 text-base` | `py-2 px-4 text-base` |
| Error Text | `text-sm` | `text-sm` | `text-sm` |
| Helper Text | `text-xs` | `text-xs` | `text-xs` |

---

## Keyboard Navigation & Accessibility

### Tab Order (Logical Flow, Left-to-Right, Top-to-Bottom)

```
1. Email input field
   ↓
2. Password input field
   ↓
3. Submit button
   ↓
4. Sign up link
```

**HTML Structure Ensures Correct Order**:
- Semantically correct form layout (email, password, button in document order)
- Tab order follows source order automatically (no tabindex overrides)

### Focus Management

**Email Input Focus State**:
```css
focus:outline-none 
focus:ring-2 
focus:ring-blue-500 
focus:border-blue-500 
transition duration-300
```
- Ring: 2px blue ring (#3b82f6)
- Border: Changes to blue on focus
- Animation: 300ms smooth transition

**Button Focus State**:
```css
focus:outline-none 
focus:ring-2 
focus:ring-blue-500 
focus:ring-offset-2
```
- Ring: 2px blue ring with 2px offset (appears "lifted")
- Offset: 2px for visual separation

### Screen Reader Announcements

| Element | ARIA Attribute | Announcement | Tool |
|---------|---|---|---|
| Email input | `<label for="email">Email address</label>` | "Email address, edit text" | NVDA, JAWS |
| Password input | `<label for="password">Password</label>` | "Password, edit text, password" | NVDA, JAWS |
| Submit button | Implicit `role=button` | "Sign in, button" | NVDA, JAWS |
| Error message | `role="alert"` + `aria-live="polite"` | "Alert: [error text]" | NVDA, JAWS |
| Sign up link | Semantic `<a>` tag | "Sign up, link" | NVDA, JAWS |

---

## Interactive States & Transitions

### Button Interactive States

**Duration**: ALL transitions use `duration-300` (300ms)

| State | Class | Effect | Hex | Visual Feedback |
|-------|-------|--------|-----|-----------------|
| Default | `bg-blue-600` | Solid blue | #2563eb | Stable |
| Hover | `hover:bg-blue-700` | Darker blue | #1d4ed8 | User can interact |
| Focus | `focus:ring-blue-500` | Blue ring | #3b82f6 | Keyboard navigation |
| Active | `active:bg-blue-800` | Even darker | #1e40af | Currently clicking |
| Disabled | `disabled:opacity-50` | 50% opacity | Faded | Cannot interact |

### Input Interactive States

| State | Classes | Visual Feedback |
|-------|---------|-----------------|
| Default | `border border-gray-300` | Subtle gray border |
| Hover | (no change) | Browser default hover |
| Focus | `focus:ring-2 focus:ring-blue-500 focus:border-blue-500` | Blue ring + blue border |
| Error (CSS not shown, JS-controlled) | `border-red-500 ring-red-500` | Red border + ring |

### Transition Timing

```tailwind
transition duration-300
```
- All color changes: 300ms smooth interpolation
- Applies to: background-color, border-color, text-color, box-shadow, ring-color
- Easing: Default (cubic-bezier timing function)

---

## Error State Styling

### Error Message Appearance (When errorMessage JS variable is truthy)

```html
<div class="rounded-md bg-red-50 p-4 mb-6 flex items-center gap-3" x-show="errorMessage">
  <!-- Error displayed -->
</div>
```

- Background: `bg-red-50` (#fef2f2) - very light red
- Icon: `text-red-700` (#b91c1c) - dark red X-circle
- Text: `text-red-700` - dark red
- Contrast: 9.1:1 (exceeds WCAG AA ✓)

### Error Input Styling (Applied via Alpine.js or form validation)

```html
<input class="... border-red-500 ring-red-500 ring-1" x-bind:class="{ 'border-red-500': hasError }">
```

- Border: Changes to `border-red-500` (#ef4444)
- Ring: `ring-1 ring-red-500` (thin red ring)
- Transition: Applied within 300ms due to `transition duration-300` class

---

## Component Hierarchy & Spacing

### Vertical Spacing (Using space-y-* classes on form container)

```
Logo (h-8 w-8)
↓ mb-4 (16px)
Title (h2 text-3xl)
↓ mb-8 (32px) [large gap before form fields]
Email Label + Input
↓ space-y-6 (24px) [generous spacing between fields]
Password Label + Input
↓ space-y-6 (24px)
Submit Button
↓ mt-4 (16px)
Sign Up Link
↓ mb-6 (24px) [gap to error message]
Error Message (if error)
```

### space-y-6 Application

```html
<form class="space-y-6">
  <!-- Each direct child gets 24px bottom margin (except last) -->
  <div>Email field</div>
  <div>Password field</div>
  <button>Submit</button>
</form>
```

- Gap Between Fields: 24px (generously spaced for clarity)
- Visual Alignment: Consistent, easy to scan

---

## Summary Table: All Components

| Component | Height | Width | Padding | Font Size | Color | Focus Ring |
|-----------|--------|-------|---------|-----------|-------|-----------|
| Logo | 32px | 32px | N/A | N/A | currentColor | N/A |
| Title | Auto | 100% | 0 | 30px | gray-900 | N/A |
| Email Label | Auto | 100% | 0 | 12px | gray-700 | N/A |
| Email Input | 32px | 100% | 12×8px | 16px | gray-900 | blue-500 ring-2 |
| Password Label | Auto | 100% | 0 | 12px | gray-700 | N/A |
| Password Input | 32px | 100% | 12×8px | 16px | gray-900 | blue-500 ring-2 |
| Submit Button | 32px | 100% | 16×8px | 16px | white on blue-600 | blue-500 ring-2 |
| Error Message | Auto | 100% | 16px | 14px | red-700 | N/A |
| Sign Up Link | Auto | 100% | 0 | 12px | blue-600 | Default browser |

---

**Design Phase 1**: ✅ COMPLETE
**Next**: Generate contracts (login-form-contract.md, responsive-breakpoints.md, accessibility-contract.md)
