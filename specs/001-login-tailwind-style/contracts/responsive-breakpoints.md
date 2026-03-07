# Responsive Breakpoints Contract

**Date**: 2026-03-07 | **Version**: 1.0
**Purpose**: Mapping of Tailwind breakpoints to form layouts and visual adjustments

---

## Tailwind Breakpoint Configuration

**Mobile-First Design Approach**: Styles declared without breakpoint prefix apply to all screens; breakpoint prefixes (`sm:`, `md:`, `lg:`, etc.) apply from that breakpoint width upward.

| Breakpoint Prefix | Minimum Width | Use Case | Device Examples |
|-------------------|---------------|----------|-----------------|
| None (default) | 0px | Mobile | iPhone 12/13, small Android |
| `sm:` | 640px | Tablet | iPad (horizontal), larger tablets |
| `md:` | 768px | Medium tablet | iPad Pro |
| `lg:` | 1024px | Desktop laptop | MacBook, typical desktop monitor |
| `xl:` | 1280px | Large desktop | 1440p monitor |
| `2xl:` | 1536px | Ultra-wide | Cinema/ultrawide display |

---

## Form Layout Responsive Strategy

### Principle: Progressive Scaling
- **Mobile (< 640px)**: Compact, minimal padding, smaller text, optimized for touch
- **Tablet (640-1024px)**: Balanced, medium padding, normal text
- **Desktop (≥ 1024px)**: Spacious, generous padding, larger proportions

### Container Width Strategy

| Width Range | Tailwind Class | Effective Width | Padding | Centering |
|------------|---|---|---|---|
| Mobile (< 640px) | `w-full max-w-sm px-4` | 90% (up to 384px) | `p-4` (16px) | auto margin |
| Tablet (640px-1024px) | `w-full max-w-md` | 448px (capped) | `p-6` (24px) | `mx-auto` |
| Desktop (≥ 1024px) | `w-full max-w-md` | 448px (capped) | `p-8` (32px) | `mx-auto` |

**CSS Equivalent**:
```css
/* Mobile */
@media (max-width: 639px) {
  .form-container { max-width: calc(100% - 32px); padding: 16px; }
}

/* Tablet */
@media (min-width: 640px) {
  .form-container { max-width: 448px; padding: 24px; }
}

/* Desktop */
@media (min-width: 1024px) {
  .form-container { max-width: 448px; padding: 32px; }
}
```

---

## Component-Level Responsive Classes

### Logo Component

| Breakpoint | Classes |
|-----------|---------|
| Mobile | `h-8 w-8 mx-auto mb-4` |
| Tablet | `h-8 w-8 mx-auto mb-4` (no change) |
| Desktop | `h-8 w-8 mx-auto mb-4` (no change) |

**Notes**: Fixed 32×32px size across all breakpoints (from spec clarification)

---

### Title Component

| Breakpoint | Size | Weight | Margin Top | Margin Bottom |
|-----------|------|--------|-----------|-------------|
| Mobile | `text-3xl` | `font-extrabold` | `mt-6` (24px) | `mb-8` (32px) |
| Tablet | `text-3xl` | `font-extrabold` | `mt-6` | `mb-8` |
| Desktop | `text-3xl` | `font-extrabold` | `mt-6` | `mb-8` |

**Notes**: 
- Title maintains text-3xl (30px) across all breakpoints
- Generous top/bottom margins for visual hierarchy
- No responsive variant needed (title scales appropriately on mobile)

---

### Label Component

| Breakpoint | Font Size | Font Weight | Margin Bottom |
|-----------|-----------|------------|-------------|
| Mobile | `text-xs` (12px) | `font-medium` (500) | `mb-1` (4px) |
| Tablet | `text-xs` | `font-medium` | `mb-1` |
| Desktop | `text-xs` | `font-medium` | `mb-1` |

**Notes**: Consistent across breakpoints (text-xs is appropriately small for all screens)

---

### Input Field Component

| Breakpoint | Padding | Border Radius | Shadow | Font Size |
|-----------|---------|---------------|--------|-----------|
| Mobile | `px-3 py-2` | `rounded-md` | `shadow-sm` | `text-sm` |
| Tablet | `px-3 py-2` | `rounded-md` | `shadow-sm` | `text-sm` |
| Desktop | `px-3 py-2` | `rounded-md` | `shadow-sm` | `text-sm` |

**Notes**: 
- Input padding consistent (12×8px) across all breakpoints
- Touch target height: ~32px at all breakpoints (still accessible)
- Consider increasing to `py-3` on mobile if touch target ≥ 44px desired (optional enhancement)

---

### Submit Button Component

| Breakpoint | Padding | Border Radius | Font Size | Width |
|-----------|---------|---------------|-----------|-------|
| Mobile | `px-4 py-2` | `rounded-md` | `text-base` | `w-full` |
| Tablet | `px-4 py-2` | `rounded-md` | `text-base` | `w-full` |
| Desktop | `px-4 py-2` | `rounded-md` | `text-base` | `w-full` |

**Important**: 
- Button height: ~32px (8px + 16px text + 8px)
- iOS recommendation: ≥ 44px touch target (consider `py-3` for future enhancement)
- Current 32px is acceptable with generous spacing between elements

---

### Error Message Component

| Breakpoint | Padding | Font Size | Margin Bottom |
|-----------|---------|-----------|-------------|
| Mobile | `p-4` (16px) | `text-sm` (14px) | `mb-6` (24px) |
| Tablet | `p-4` | `text-sm` | `mb-6` |
| Desktop | `p-4` | `text-sm` | `mb-6` |

**Notes**: Consistent across breakpoints (compact, readable at all sizes)

---

### Sign Up Link Component

| Breakpoint | Font Size | Font Weight | Margin Top |
|-----------|-----------|-------------|-----------|
| Mobile | `text-xs` (12px) | `font-medium` (500) | `mt-4` (16px) |
| Tablet | `text-xs` | `font-medium` | `mt-4` |
| Desktop | `text-xs` | `font-medium` | `mt-4` |

**Notes**: Consistent across breakpoints; text-xs is appropriately small for all screens

---

## Spacing & Vertical Rhythm (space-y-* classes)

### Form Field Spacing

```html
<form class="space-y-6">
  <!-- Each child gets 24px bottom margin (except last) -->
</form>
```

| Breakpoint | Spacing Between Fields | Total Height |
|-----------|----------------------|------------|
| Mobile | `space-y-6` (24px) | ~220px (logo + title + 2 fields + button + link) |
| Tablet | `space-y-6` (24px) | Same |
| Desktop | `space-y-6` (24px) | Same |

**CSS Equivalent**:
```css
.form-container > * + * {
  margin-top: 24px; /* space-y-6 = 24px (1.5rem) */
}
```

---

## Outer Container Responsive Padding

```html
<div class="py-12 px-4 sm:px-6 lg:px-8">
```

| Breakpoint | Vertical Padding | Horizontal Padding | Purpose |
|-----------|-----------------|-------------------|---------|
| Mobile (< 640px) | `py-12` (48px) | `px-4` (16px) | Compress horizontally, maintain vertical breathing |
| Tablet (≥ 640px) | `py-12` (48px) | `sm:px-6` (24px) | Balanced padding |
| Desktop (≥ 1024px) | `py-12` (48px) | `lg:px-8` (32px) | Spacious, generous margins |

---

## Border Radius Responsive Variants

| Breakpoint | Form Container | Inputs | Button |
|-----------|---|---|---|
| Mobile | `rounded-md` (6px) | `rounded-md` (6px) | `rounded-md` (6px) |
| Tablet | `rounded-lg` (8px) | `rounded-md` (6px) | `rounded-md` (6px) |
| Desktop | `rounded-lg` (8px) | `rounded-md` (6px) | `rounded-md` (6px) |

**Notes**: 
- Container border-radius increases slightly on tablet/desktop for modern appearance
- Input/button radius stays consistent for cohesion

---

## Shadow Responsive Variants

| Breakpoint | Form Container | Other Elements |
|-----------|---|---|
| Mobile | `shadow-md` (medium) | `shadow-sm` (subtle) |
| Tablet | `shadow-lg` (large) | `shadow-sm` |
| Desktop | `shadow-lg` (large) | `shadow-sm` |

**Notes**: 
- Container shadow is lighter on mobile (visual hierarchy, less "weight" on compact screens)
- Increases on tablet/desktop for modern appearance and depth

---

## Font Size Responsive Variants

| Element | Mobile | Tablet | Desktop | Notes |
|---------|--------|--------|---------|-------|
| Logo | 32px | 32px | 32px | Fixed |
| Title | 30px (text-3xl) | 30px | 30px | Fixed |
| Labels | 12px (text-xs) | 12px | 12px | Fixed |
| Input Text | 16px (text-sm) | 16px | 16px | Fixed |
| Button Text | 16px (text-base) | 16px | 16px | Fixed |
| Error Text | 14px (text-sm) | 14px | 14px | Fixed |
| Link Text | 12px (text-xs) | 12px | 12px | Fixed |

**Notes**: All font sizes are responsive by default (Tailwind uses rem units). No explicit breakpoint variants needed for typography in this design.

---

## Testing Checklist for Responsive Design

### Mobile Breakpoint (< 640px)

- [ ] Form renders at 90% width
- [ ] No horizontal scrollbar
- [ ] Padding p-4 (16px) applied
- [ ] Logo 32×32px visible and centered
- [ ] Title (text-3xl) readable without breaking into 2+ lines
- [ ] Input fields full width (`w-full`)
- [ ] Button full width, easily tappable (≥ 32px height)
- [ ] Error message compact and readable
- [ ] Link text small but readable (text-xs)
- [ ] space-y-6 (24px) spacing between fields appears generous

**Test Devices/Viewports**:
- iPhone 12/13 (390 × 844px)
- Samsung Galaxy S21 (360 × 800px)
- Chrome DevTools: 375px, 320px

### Tablet Breakpoint (640-1024px)

- [ ] Form container becomes max-w-md (448px width capped)
- [ ] Padding p-6 (24px) applied
- [ ] Border-radius rounds to rounded-lg (8px)
- [ ] Shadow increases to shadow-lg
- [ ] All text remains readable
- [ ] Spacing proportional (not too cramped, not too spacious)

**Test Devices/Viewports**:
- iPad 7th Gen (1024 × 768px)
- Chrome DevTools: 768px

### Desktop Breakpoint (≥ 1024px)

- [ ] Form container max-w-md (448px width capped at 400px effective)
- [ ] Padding p-8 (32px) applied
- [ ] Generous spacing between fields and elements
- [ ] Button has clear hover state (blue-700)
- [ ] Focus rings visible (blue-500, 2px)
- [ ] Form appears centered on screen (flexbox centering)
- [ ] No excessive white space around form

**Test Devices/Viewports**:
- MacBook Pro 16" (1728 × 1117px at 1x, or 3456 × 2234px at 2x)
- Desktop monitors (1920 × 1080px)
- Chrome DevTools: 1920px

### Ultra-Wide Breakpoint (≥ 1536px)

- [ ] Form does not exceed 400px width (max-w-md cap holds)
- [ ] Form remains centered and proportional
- [ ] No stretching or distortion

**Test Viewports**:
- Chrome DevTools: 2560px (4K monitor)

---

## Responsive Edge Cases

### Extremely Small Screens (< 320px)

**Issue**: Some Android devices have < 320px width

**Testing**:
- [ ] Form renders without horizontal scroll at 280px viewport
- [ ] Text does not break awkwardly
- [ ] Buttons and inputs remain clickable

**Mitigation**: Tailwind defaults (px-4 outer padding) provide 32px buffer; form content should fit comfortably.

### Landscape Orientation (Mobile)

**Issue**: Landscape mode compresses height but expands width

**Device Example**: iPhone in landscape = ~812px × 375px

**Testing**:
- [ ] Form renders correctly at 812px width
- [ ] No excessive vertical scrolling needed
- [ ] Button remains within viewport (no need to scroll down to submit)

**Current Design**: Logo (32px) + Title (30px + padding) + 2 inputs (32px each + labels) + button (32px) + link (small) = ~220px total height → fits in 375px landscape viewport ✓

---

## Responsive Design Summary

| Aspect | Strategy |
|--------|----------|
| Width | Progressive capping: 90% mobile → 400px tablet/desktop |
| Padding | Scales: p-4 (16px) → p-6 (24px) → p-8 (32px) |
| Typography | Fixed sizes across breakpoints (responsive via rem units) |
| Spacing | Consistent (space-y-6: 24px) across all breakpoints |
| Shadow | Scales: shadow-md (mobile) → shadow-lg (tablet/desktop) |
| Border Radius | Scales: rounded-md (mobile) → rounded-lg (tablet/desktop) |
| Centering | Flexbox parent ensures alignment at all breakpoints |

---

**Document Status**: Ready for Implementation
**Verification**: Manual testing required across device categories listed above
