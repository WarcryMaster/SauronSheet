# Login Form Component Contract

**Date**: 2026-03-07 | **Version**: 1.0
**Purpose**: Comprehensive specification for Login form HTML structure and Tailwind CSS classes

---

## Contract Overview

This document defines the exact HTML structure and CSS classes for the Login form, including all interactive states, responsive variants, and accessibility requirements.

**Compliance Scope**: Frontend-only (no backend changes); Tailwind CSS 3.x; no custom CSS.

---

## Outer Container (Flexbox Wrapper)

**Purpose**: Provides full-viewport centering for the form

**HTML**:
```html
<div class="min-h-screen flex items-center justify-center bg-gray-50 py-12 px-4 sm:px-6 lg:px-8">
  <!-- Form container goes here -->
</div>
```

**Classes**:
| Class | Effect | Value |
|-------|--------|-------|
| `min-h-screen` | Minimum height | 100vh (full viewport height) |
| `flex` | Display mode | Flexbox |
| `items-center` | Vertical alignment | center |
| `justify-center` | Horizontal alignment | center |
| `bg-gray-50` | Background color | #f9fafb (light gray) |
| `py-12` | Padding top/bottom | 48px |
| `px-4 sm:px-6 lg:px-8` | Padding left/right (responsive) | 4px (mobile), 24px (tablet), 32px (desktop) |

**Computed Layout**:
- Full viewport height available for centering
- Form appears in center of screen (both axes)
- Light gray background (provides visual separation from white form)

---

## Form Container

**Purpose**: White container holding all form fields

### Desktop/Tablet Layout (≥ 640px)

```html
<div class="w-full max-w-md bg-white rounded-lg shadow-lg p-8">
  <!-- Form content -->
</div>
```

**Classes**:
| Class | Effect | Desktop Value | Tablet Value |
|-------|--------|--------------|-------------|
| `w-full` | Width | 100% of parent | 100% of parent |
| `max-w-md` | Maximum width | 448px (400px effective) | 448px |
| `bg-white` | Background | #ffffff | #ffffff |
| `rounded-lg` | Border radius | 8px | 8px |
| `shadow-lg` | Box shadow | Large shadow (elevation) | Large shadow |
| `p-8` | Padding (desktop) | 32px | - |
| `p-6` | Padding (tablet) | - | 24px |

### Mobile Layout (< 640px)

```html
<div class="w-full max-w-sm bg-white rounded-md shadow-md p-4">
  <!-- Form content -->
</div>
```

**Classes**:
| Class | Effect | Mobile Value |
|-------|--------|------------|
| `w-full` | Width | 100% of parent (~90% via outer container px-4) |
| `max-w-sm` | Maximum width | 384px (constrained) |
| `bg-white` | Background | #ffffff |
| `rounded-md` | Border radius | 6px (smaller than desktop) |
| `shadow-md` | Box shadow | Medium shadow (less elevation) |
| `p-4` | Padding | 16px |

**Computed Dimensions**:
- Desktop: 400px fixed width, 32px padding
- Tablet: 400px fixed width, 24px padding
- Mobile: 90% width (within outer px-4), 16px padding

---

## Logo Component

```html
<img 
  src="/img/logo.svg" 
  alt="SauronSheet" 
  class="h-8 w-8 mx-auto mb-4"
/>
```

**Classes**:
| Class | Effect | Value |
|-------|--------|-------|
| `h-8` | Height | 32px (0.5rem × 8 = 2rem) |
| `w-8` | Width | 32px |
| `mx-auto` | Margin left/right | auto (horizontal center) |
| `mb-4` | Margin bottom | 16px (separation from title) |

**Attributes**:
| Attribute | Value | Purpose |
|-----------|-------|---------|
| `src` | `/img/logo.svg` | Asset path |
| `alt` | `SauronSheet` | Screen reader text |

**Visual Result**:
- 32×32px image, centered horizontally
- 16px gap below (to title)

---

## Title Component

```html
<h2 class="text-center text-3xl font-extrabold text-gray-900 mt-6 mb-8">
  Sign in to your account
</h2>
```

**Classes**:
| Class | Effect | Value |
|-------|--------|-------|
| `text-center` | Alignment | center |
| `text-3xl` | Font size | 30px |
| `font-extrabold` | Font weight | 800 |
| `text-gray-900` | Text color | #111827 (dark gray) |
| `mt-6` | Margin top | 24px |
| `mb-8` | Margin bottom | 32px |

**Typography**:
- Size: 30px
- Weight: 800 (heaviest)
- Line Height: 36px (default)
- Color: Dark gray (#111827)

**Spacing**:
- Above (from logo): 24px
- Below (to fields): 32px

---

## Form Element: Email Input

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
| Class | Effect | Value |
|-------|--------|-------|
| `block` | Display | block (full width) |
| `text-xs` | Font size | 12px |
| `font-medium` | Font weight | 500 |
| `text-gray-700` | Text color | #374151 (medium gray) |
| `mb-1` | Margin bottom | 4px |

**Input Classes**:
| Class | Effect | Value |
|-------|--------|--------|
| `w-full` | Width | 100% of parent |
| `px-3` | Padding left/right | 12px |
| `py-2` | Padding top/bottom | 8px (input height ~32px) |
| `border border-gray-300` | Border | 1px gray (#d1d5db) |
| `rounded-md` | Border radius | 6px |
| `shadow-sm` | Box shadow | Subtle shadow |
| **Focus State** |
| `focus:outline-none` | Default outline | removed |
| `focus:ring-2` | Focus ring width | 2px |
| `focus:ring-blue-500` | Focus ring color | #3b82f6 (blue) |
| `focus:border-blue-500` | Focus border color | #3b82f6 (blue) |
| `transition duration-300` | Animation | 300ms smooth |

**Input Attributes**:
| Attribute | Value | Purpose |
|-----------|-------|---------|
| `type` | `email` | Native email validation |
| `id` | `email` | Label linkage + form access |
| `name` | `email` | Form submission |
| `placeholder` | `Enter your email` | Visual hint |
| `required` | Present | HTML5 validation |
| `aria-label` | `Email address` | Screen reader label |

---

## Form Element: Password Input

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

**Classes**: Identical to email input (copy-paste with id/name changes)

**Input Attributes**:
| Attribute | Value | Purpose |
|-----------|-------|---------|
| `type` | `password` | Masks input as bullets |
| `id` | `password` | Label linkage + form access |
| `name` | `password` | Form submission |
| `placeholder` | `Enter your password` | Visual hint |
| `required` | Present | HTML5 validation |
| `aria-label` | `Password` | Screen reader label |

---

## Form Element: Submit Button

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

**Classes - Layout**:
| Class | Effect | Value |
|-------|--------|-------|
| `w-full` | Width | 100% of parent |
| `px-4` | Padding left/right | 16px |
| `py-2` | Padding top/bottom | 8px (button height ~32px) |

**Classes - Colors**:
| Class | State | Effect | Value |
|-------|-------|--------|-------|
| `bg-blue-600` | Default | Background | #2563eb (blue) |
| `hover:bg-blue-700` | Hover | Background | #1d4ed8 (darker blue) |
| `active:bg-blue-800` | Click | Background | #1e40af (even darker) |
| `text-white` | All | Text color | #ffffff |
| `font-medium` | All | Font weight | 500 |

**Classes - Shape & Shadow**:
| Class | Effect | Value |
|-------|--------|-------|
| `rounded-md` | Border radius | 6px |
| `shadow-md` | Box shadow | Medium elevation |

**Classes - Focus & Transitions**:
| Class | Effect | Value |
|-------|--------|-------|
| `focus:outline-none` | Focus | Remove browser outline |
| `focus:ring-2` | Focus ring | 2px width |
| `focus:ring-blue-500` | Focus ring | #3b82f6 (blue) |
| `focus:ring-offset-2` | Focus ring offset | 2px gap (lifted effect) |
| `transition duration-300` | All state changes | 300ms smooth |

**Classes - Disabled State**:
| Class | Effect | Value |
|-------|--------|-------|
| `disabled:opacity-50` | When disabled | 50% opacity (faded) |
| `disabled:cursor-not-allowed` | When disabled | "Not allowed" cursor |

**Button Attributes**:
| Attribute | Value | Purpose |
|-----------|-------|---------|
| `type` | `submit` | Form submission trigger |
| `id` | `submit-button` | JavaScript access |

**Interactive States Table**:
| State | BG Color | Ring | Cursor |
|-------|----------|------|--------|
| Default | blue-600 | none | pointer |
| Hover | blue-700 | none | pointer |
| Focus | blue-600 | blue-500 (2px) | pointer |
| Active (click) | blue-800 | blue-500 (2px) | pointer |
| Disabled | blue-600 (50% opacity) | none | not-allowed |

---

## Error Message Component

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
| Class | Effect | Value |
|-------|--------|-------|
| `rounded-md` | Border radius | 6px |
| `bg-red-50` | Background | #fef2f2 (very light red) |
| `p-4` | Padding | 16px (all sides) |
| `mb-6` | Margin bottom | 24px |
| `flex` | Display | flexbox |
| `items-center` | Vertical align | center |
| `gap-3` | Gap between children | 12px |

**Icon Classes**:
| Class | Effect | Value |
|-------|--------|-------|
| `h-5` | Height | 20px |
| `w-5` | Width | 20px |
| `text-red-700` | Color | #b91c1c (dark red) |
| `flex-shrink-0` | Flex behavior | doesn't shrink |

**SVG**: X-circle icon (circle + cross lines)
- viewBox: `0 0 24 24`
- Stroke: 2px
- Fill: none (outline only)
- Color: currentColor (inherits `text-red-700`)

**Text Classes**:
| Class | Effect | Value |
|-------|--------|-------|
| `text-sm` | Font size | 14px |
| `text-red-700` | Text color | #b91c1c (dark red) |

**ARIA Attributes**:
| Attribute | Value | Purpose |
|-----------|-------|---------|
| `role` | `alert` | Screen reader role (announces immediately) |
| `aria-live` | `polite` | Updates announced when text changes |

**Alpine.js Directive**:
| Directive | Binding | Purpose |
|-----------|---------|---------|
| `x-show="errorMessage"` | Boolean | Shows/hides element when errorMessage is truthy |
| `x-text="errorMessage"` | Text content | Dynamically sets error message text |

---

## Sign Up Link Component

```html
<div class="text-center text-xs font-medium text-gray-600 mt-4">
  Don't have an account? 
  <a href="/Auth/Register" class="text-blue-600 hover:text-blue-500 hover:underline transition duration-300">
    Sign up
  </a>
</div>
```

**Container Classes**:
| Class | Effect | Value |
|-------|--------|-------|
| `text-center` | Alignment | center |
| `text-xs` | Font size | 12px |
| `font-medium` | Font weight | 500 |
| `text-gray-600` | Text color | #4b5563 (medium-dark gray) |
| `mt-4` | Margin top | 16px |

**Link Classes**:
| Class | Effect | Value |
|-------|--------|-------|
| `text-blue-600` | Text color (default) | #2563eb (blue) |
| `hover:text-blue-500` | Text color (hover) | #3b82f6 (lighter blue) |
| `hover:underline` | Decoration (hover) | underline appears |
| `transition duration-300` | Animation | 300ms smooth color + decoration transition |

**Link Attributes**:
| Attribute | Value | Purpose |
|-----------|-------|---------|
| `href` | `/Auth/Register` | Navigation URL |

---

## Complete Form HTML Structure (Reference)

```html
<!-- Outer Centering Container -->
<div class="min-h-screen flex items-center justify-center bg-gray-50 py-12 px-4 sm:px-6 lg:px-8">

  <!-- Form Container (w-full max-w-md + responsive padding) -->
  <div class="w-full max-w-md bg-white rounded-lg shadow-lg p-8 sm:p-6 lg:p-8">
    
    <!-- Logo -->
    <img src="/img/logo.svg" alt="SauronSheet" class="h-8 w-8 mx-auto mb-4" />
    
    <!-- Title -->
    <h2 class="text-center text-3xl font-extrabold text-gray-900 mt-6 mb-8">
      Sign in to your account
    </h2>
    
    <!-- Form -->
    <form class="space-y-6">
      
      <!-- Email Field -->
      <div>
        <label for="email" class="block text-xs font-medium text-gray-700 mb-1">
          Email address
        </label>
        <input 
          type="email" 
          id="email" 
          name="email" 
          placeholder="Enter your email"
          class="w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500 transition duration-300"
          required
          aria-label="Email address"
        />
      </div>
      
      <!-- Password Field -->
      <div>
        <label for="password" class="block text-xs font-medium text-gray-700 mb-1">
          Password
        </label>
        <input 
          type="password" 
          id="password" 
          name="password" 
          placeholder="Enter your password"
          class="w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500 transition duration-300"
          required
          aria-label="Password"
        />
      </div>
      
      <!-- Submit Button -->
      <button 
        type="submit" 
        class="w-full px-4 py-2 bg-blue-600 hover:bg-blue-700 active:bg-blue-800 text-white font-medium rounded-md shadow-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 transition duration-300 disabled:opacity-50 disabled:cursor-not-allowed"
        id="submit-button"
      >
        Sign in
      </button>
      
    </form>
    
    <!-- Error Message (Alpine.js conditional) -->
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
    
    <!-- Sign Up Link -->
    <div class="text-center text-xs font-medium text-gray-600 mt-4">
      Don't have an account? 
      <a href="/Auth/Register" class="text-blue-600 hover:text-blue-500 hover:underline transition duration-300">
        Sign up
      </a>
    </div>
    
  </div>
  
</div>
```

---

**Document Status**: Ready for Implementation
**Next**: Execute tasks in `tasks.md` to implement form HTML
