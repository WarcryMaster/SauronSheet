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
