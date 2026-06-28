---
description: "Use when writing, reviewing, or fixing tests. Covers testing pyramid, coverage requirements, E2E policy, and test commands for SauronSheet."
---

# Testing Strategy

## Testing Pyramid

| Level | Scope | Tools | When |
|---|---|---|---|
| Unit Tests | Domain entities, VOs, domain services | xUnit + Moq | Every phase with domain changes |
| Integration | Application handlers (mocked repos) | xUnit + Moq + in-memory doubles | App layer scope phases |
| End-to-End | Playwright browser tests | Playwright | UI/UX scope phases |

## Coverage Requirements

| Scope | Minimum Coverage |
|---|---|
| Domain Layer | 80% |
| Application Layer | 70% |

## Mandatory Rules

- Tests are mandatory BEFORE implementation in every feature (Red-Green-Refactor).
- Domain service tests MUST mock repository interfaces (not real databases).
- Tests serve as executable specification and regression prevention.
- **E2E test coupling**: every code change that affects UI behavior (Razor Pages, forms, modals, navigation, JS interactions, Alpine.js components) MUST be accompanied by a corresponding review and update of the affected E2E tests. Never modify frontend code without ensuring the E2E tests still match the new behavior.

## Test Commands

```bash
# Run all .NET tests
dotnet test

# Run Playwright E2E tests (starts app automatically)
npx playwright test --config=e2e/playwright.config.ts --project=chromium
```

---

## E2E Element Selection: `data-testid` Required

**All E2E tests MUST use dedicated `data-testid` attributes for element selection.** CSS classes, ARIA roles, text content, or DOM position must NOT be used as primary selectors.

**Pattern:**
- Attribute: `data-testid` with kebab-case value
- Naming: `{page-or-section}-{element-purpose}` (e.g. `upload-progressbar`, `upload-success-alert`, `annual-yoy-no-data`)
- Selector in tests: `page.locator('[data-testid="upload-progressbar"]')`
- Test files MUST NOT contain CSS class names, generic roles, or text content matches

**Rationale:**
- CSS classes change frequently with design updates (MDBootstrap, theme changes)
- ARIA roles are generic and collide (e.g. multiple `[role="status"]` on the same page)
- Text content changes with i18n/l10n or copy updates
- DOM position is brittle — adding or removing elements shifts indexes
- `data-testid` creates an explicit contract between test and markup that survives refactors

**Implementation:** When writing new tests or modifying existing ones, add `data-testid` attributes to the corresponding `.cshtml` markup if they don't exist yet. The test selector must reference the `data-testid`, not CSS classes or text.

## E2E Testing Policy: Real User Interaction Required

**Fundamental rule:** E2E interface tests **MUST** act as a real user navigating and interacting with the web. If they don't simulate real interaction, **they are worthless**.

**DO:**
- Use `page.click()`, `page.fill()`, `page.selectOption()` to interact with elements.
- Wait for elements to be visible before interacting (`waitFor`, `toBeVisible`).
- Handle native browser dialogs (`page.on('dialog')`).
- Navigate the app as a real user would (clicks on buttons, links, forms).
- Verify observable results in the UI (visible text, redirects, success/error messages).

**DO NOT:**
- ❌ Use `page.evaluate()` to execute direct JavaScript in the DOM.
- ❌ Use `fetch()` inside `page.evaluate()` to call APIs directly.
- ❌ Manipulate the DOM directly (`document.querySelector`, `input.type = 'text'`).
- ❌ Skip UI flows (modals, confirmations, form validations).
- ❌ Verify database state directly instead of the UI.

**Exception:** Post-execution cleanup helpers (`test.afterAll`) **MAY** use `fetch()` directly, as they are maintenance operations, not tests themselves.

**Reason:** An E2E test that doesn't simulate real interaction doesn't test real use cases. It only verifies the API works, which integration tests already cover. E2E tests must validate that **the user can complete their task** through the interface.
