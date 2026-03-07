# Feature Specification: Category Management

**Feature Branch**: `002-category-management`  
**Created**: March 7, 2026  
**Status**: Draft  

## Quick Reference

- **Scope Layers**: Full-Stack (Domain + Application + Frontend + Infrastructure)
- **MVP Completeness**: Category CRUD + system defaults + UI with validation
- **Default Categories**: 6 groups with 24 predefined categories (immutable)
- **User Permissions**: View all categories; Edit/Delete only custom categories
- **Constraint**: No deletion if category has associated transactions

---

## Clarifications Session (March 7–12, 2026)

### Resolved Design Decisions

| Question | Decision | Rationale |
|----------|----------|-----------|
| **Validation Architecture** | D - Hybrid Pattern | Domain ValueObjects (CategoryName, ColorHex) + Domain Service (CategoryService) + Application Handlers. DDD-compliant. |
| **CQRS Handlers** | B - 5 Handlers | GetAllCategoriesQuery, SearchCategoriesQuery, CreateCategoryCommand, UpdateCategoryCommand, DeleteCategoryCommand. Covers all stories. |
| **Delete Guard Logic** | B+C Hybrid | Application handler executes EXISTS query (efficient), passes boolean to Domain Service CanDeleteCategory(). Maintains invariants in Domain. |
| **Seeding 24 Categories** | A - SQL Migration | File-based SQL migration with 24 INSERT statements. Supabase migration in source control per Constitution. |
| **Color Hex Validation** | C - Defense-in-Depth | Frontend HTML5 color picker (UX) + Domain ValueObject ColorHex regex validation (invariant enforcement). Both layers validate. |
| **IconName Validation** | B - Application Handler | IconName remains simple string in Domain entity. Validation happens in Application handler against hardcoded AllowedBootstrapIcons constant. Decouples Domain from Bootstrap semantics. |
| **Concurrency Strategy** | E - Name Uniqueness + LWW | Database UNIQUE(UserId, Name) constraint prevents concurrent name conflicts. Update handler uses Last-Write-Wins for other properties (color, icon). No optimistic locking for MVP; revisit in Phase 3 if needed. |
| **Accessibility Scope** | A - WCAG 2.1 AA Compliance | Full accessibility required: ARIA labels, semantic HTML, keyboard navigation, screen reader support, 4.5:1 color contrast. Phase 2 includes A11y acceptance tests + Lighthouse audits. |
| **Bootstrap Icons Version** | A - Icons 5.x from CDN | Bootstrap Icons 5.x (~120 icons) loaded from CDN (jsDelivr). AllowedBootstrapIcons constant mirrors stable 5.x icon list. Accept that 6.x upgrade would require spec update. |
| **Delete Strategy** | A - Hard Delete | DELETE operation immediately removes category row from database. No soft delete or audit log. Simple MVP approach; defer soft delete/audit trail to Phase 3+ if compliance required. |

---

## Executive Summary (In Scope / Deferred)

### In Scope

**MVP Category Management System** delivers a complete category lifecycle for expense tracking:

1. **System Categories** (6 groups, 24 predefined categories) - immutable, seeded database records
   - 💰 Income (5 categories)
   - 🏠 Fixed Expenses (5 categories)
   - 🛒 Variable Expenses (5 categories)
   - 🎭 Lifestyle & Leisure (5 categories)
   - 📉 Finance & Other (4 categories)

2. **CRUD Operations** for custom categories
   - Create new personal expense categories
   - Edit names, colors, icons of custom categories
   - Delete custom categories (with transaction guard)

3. **Category Management UI** 
   - Categorized list view (grouped by Income/Expenses)
   - Visual distinction (system categories read-only badge, custom categories with edit/delete actions)
   - Color picker + icon selector for custom categories
   - Validation feedback (errors for duplicate names, required fields, etc.)

4. **Business Rules & Validations**
   - Category names must be unique per user per type (Income/Expense)
   - Cannot modify or delete system default categories
   - Cannot delete category with active transactions
   - Category colors stored as hex format (e.g., #FF5733)
   - Icons referenced by icon library name (e.g., "credit-card", "home", "shopping-cart")

5. **Web Accessibility (WCAG 2.1 AA)**
   - Full keyboard navigation for all UI interactivity (no mouse required)
   - ARIA labels for form inputs, buttons, icons, dynamic content
   - Semantic HTML (proper heading hierarchy, form structure)
   - Screen reader support for category lists, forms, error messages
   - Color contrast ≥4.5:1 for all text; icons paired with labels
   - Tested via Lighthouse audits + axe accessibility scanner

### Deferred to Future Phases

- Category hierarchies (parent-child relationships)
- Category budgets & spending limits
- Category tagging or multi-type assignment
- Bulk category import/export
- Category sharing between users
- AI-powered category suggestions
- Category analytics & spending trends

---

## Critical Decisions

1. **System Categories Are Immutable** — All 24 system defaults cannot be modified or deleted by users. This ensures consistent financial reporting and prevents accidental loss of data context. User can only delete custom categories.

2. **Strong-Typed ID Pattern** — Categories use `CategoryId` value object (wraps Guid) to prevent mixing IDs at compile time. Follows Clean Architecture & Domain-Driven Design principles.

3. **User Isolation (Tenant)** — Every category is scoped to a UserId. User A cannot view or modify User B's custom categories. System categories are global but filtered per user in UI.

4. **Color & Icon with Validation** — Colors are hex strings (#RRGGBB format) validated by Domain ValueObject `ColorHex` (regex `#[0-9A-F]{6}`). Frontend HTML5 color picker provides UX. Icons are Bootstrap icon names (e.g., "credit-card", "home"). Defense-in-depth: Frontend picker + Domain validation.

5. **Delete Guard: Domain Service Pattern** — Deletion guarded by Domain Service `CategoryService.CanDeleteCategory(category, hasTransactions)`. Application handler queries transaction count (EXISTS query), passes boolean to service. Prevents orphaned records.

6. **Name Uniqueness Enforced in Domain** — Within a user's account, category names must be unique per user (no duplicates in Income or Expense). Validated by Domain Service `CategoryService.ValidateUniqueName(userId, name)` to enforce cross-entity invariant.

7. **Hybrid Validation Architecture** — Format/simple validations in Domain ValueObjects (CategoryName.Create(name), CategoryNameMaxLength). Cross-entity validations in Domain Service (ValidateUniqueName, CanDelete). Application handlers orchestrate. Frontend provides immediate UX feedback.

---

## Architecture & Implementation Details

### Domain Layer (Clean Architecture)

**Entities (Aggregate Roots):**
- `Category` — Entity with CategoryId, UserId, Name, Type (Income/Expense), Color, IconName, IsSystemDefault, CreatedAt, UpdatedAt
- Parameterized constructor enforcing invariants
- Methods: `CanDelete(bool hasTransactions): bool` — Guard method returning true if category can be deleted

**Value Objects:**
- `CategoryId(Guid value)` — Strong-typed ID preventing ID mixing at compile time; required per Constitution
- `CategoryName(string value)` — Validates 1-50 chars, non-empty after trim, encapsulates name validation logic
- `ColorHex(string value)` — Validates regex `#[0-9A-F]{6}`, immutable color representation
- `UserId` — Ensures user isolation

**Domain Service (Cross-Entity Logic):**
- `CategoryService.ValidateUniqueName(UserId userId, string name)` — Queries repository; throws DomainException if duplicate exists
- `CategoryService.CanDeleteCategory(Category category, bool hasTransactions): bool` — Returns false if IsSystemDefault=true or hasTransactions=true; true otherwise
- `CategoryService.GetSystemDefaults(): IReadOnlyList<Category>` — Returns immutable list of 24 system categories
- Depends only on `ICategoryRepository` interface (Domain layer)

**Repository Interface (Domain):**
- `ICategoryRepository` — Contracts for CRUD; implementation in Infrastructure: `SupabaseCategoryRepository`
- Methods: `GetByIdAsync(CategoryId)`, `GetByUserIdAsync(UserId)`, `FindByNameAndUserAsync(UserId, string name)`, `AddAsync(Category)`, `UpdateAsync(Category)`, `DeleteAsync(CategoryId)`, `GetCountAsync(CategoryId)` — for transaction count check

**Exceptions:**
- `DomainException` — Thrown on invariant violation (name exists, cannot delete if IsSystemDefault)
- `EntityNotFoundException` — Thrown if category not found on update/delete

### Application Layer (CQRS via MediatR)

**Commands (State-Changing):**
- `CreateCategoryCommand(string Name, string Type, string Color, string IconName): IRequest<CategoryId>`
  - Handler: `CreateCategoryCommandHandler` — Validates Name via CategoryService, validates IconName against AllowedBootstrapIcons constant, creates entity, persists
- `UpdateCategoryCommand(CategoryId Id, string Name, string Color, string IconName): IRequest<Unit>`
  - Handler: `UpdateCategoryCommandHandler` — Prevents Type/IsSystemDefault changes; validates Name uniqueness via CategoryService; validates IconName against AllowedBootstrapIcons constant
- `DeleteCategoryCommand(CategoryId Id): IRequest<Unit>`
  - Handler: `DeleteCategoryCommandHandler` — Queries transaction count, calls CategoryService.CanDeleteCategory(), deletes or throws

**Queries (Read-Only):**
- `GetAllCategoriesQuery(UserId userId): IRequest<List<CategoryDto>>`
  - Handler: `GetAllCategoriesQueryHandler` — Returns system + user-custom categories grouped by Type
- `SearchCategoriesQuery(UserId userId, string searchTerm): IRequest<List<CategoryDto>>`
  - Handler: `SearchCategoriesQueryHandler` — Filters categories by name containing searchTerm (case-insensitive)

**DTOs (Data Transfer Objects):**
- `CategoryDto` — For API/Frontend: Id, Name, Type, Color, IconName, IsSystemDefault, CreatedAt, UpdatedAt
- Used in query responses; never expose raw entities to Frontend

**Icon Validation (Application Layer Constant):**
- `AllowedBootstrapIcons` — Hardcoded constant in Application/Common/ with list of ~120 valid Bootstrap Icons 5.x names (e.g., "shopping-cart", "home", "credit-card", "coffee", etc.)
- All Create/Update handlers validate IconName against this list; throw ValidationException if not found
- Defense-in-depth: Frontend icon picker (prevents invalid selections) + Application validation (enforces at boundary)
- Bootstrap Icons 5.x CDN reference: `https://cdn.jsdelivr.net/npm/bootstrap-icons@5.3.0/font/bootstrap-icons.css` (pinned to 5.3.0 for stability)
- Contains examples: shopping-cart, home, credit-card, building-dollar, trending-up, gift, star, lightbulb, shield, repeat, book, scissors, wrench, paw, utensils, shopping-bags, plane, heart, piggy-bank, hands-helping, exclamation-circle, coffee, question-circle (and ~90 more per Bootstrap Icons 5.x library)
- **Version Strategy** (per Clarification #9): Pinned to 5.x stable release; AllowedBootstrapIcons constant must match 5.x icon names exactly. Upgrade to 6.x (if/when released) requires spec + constant update; no rolling updates to avoid breaking changes.

**Pipeline Behaviors:**
- Tenant scoping already handles UserId extraction from JWT
- New validation behavior (if needed) for common Command validation rules

### Infrastructure Layer (Supabase/PostgreSQL)

**Persistence:**
- `SupabaseCategoryRepository : ICategoryRepository` — Implements all repository methods using Postgrest client
- Maps `CategoryRow` (Postgrest DTO) ↔ `Category` (Domain entity) via `ToDomain()` / `FromDomainForInsert()`
- Uses `FromDomainForInsert()` to exclude CreatedAt/UpdatedAt (database triggers manage timestamps)

**Database Migration:**
- File: `Infrastructure/Persistence/Migrations/xyz_SeedSystemDefaultCategories.sql`
- Contains: CREATE TABLE categories (if not exists) + 24 INSERT statements for system defaults
- Executed once on deployment; idempotent

**Seeding:**
- SQL migration creates 24 rows with IsSystemDefault=true for each system category
- No Application-level seed command needed; database handles initialization

### Frontend Layer (Razor Pages)

**Pages:**
- Route: `/Categories`
- Handler (`CategoriesModel.cshtml.cs`):
  - `OnGetAsync()` — Calls `GetAllCategoriesQuery` via MediatR; passes list to view
  - `OnPostCreateAsync()` — Binds form, validates, sends `CreateCategoryCommand`; handles DomainException errors
  - `OnPostUpdateAsync()` — Sends `UpdateCategoryCommand`; validates edit form
  - `OnPostDeleteAsync()` — Sends `DeleteCategoryCommand`; handles "has transactions" error gracefully

**UI Components:**
- Color picker (HTML5 `<input type="color">`) — Frontend validation; sends hex to backend
- Icon selector (Bootstrap Icons 5.x dropdown or search; loads ~120 icon names from AllowedBootstrapIcons; icon glyphs rendered via Bootstrap Icons CDN CSS class)
- Form validation feedback (error messages aligned with DomainException messages)
- System vs. Custom badges using `IsSystemDefault` flag
- Disabled Edit/Delete buttons for system categories

**External Dependencies (CDN):**
- **Bootstrap Icons 5.x CSS** — Declared in `_Layout.cshtml` as: `<link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@5.3.0/font/bootstrap-icons.css" />`
- Icon glyphs rendered via CSS classes (e.g., `<i class="bi bi-shopping-cart"></i>` for shopping-cart icon)
- CDN ensures consistent icon appearance across all environments (dev/staging/prod)
- Per Clarification #9: Pinned to 5.3.0; future upgrades to 6.x require spec + AllowedBootstrapIcons constant update

**Validation (Frontend):**
- Required field indicators for Name, Type, Color
- Max 50 char limit on name input
- Color picker automatically provides valid hex format
- Icon dropdown prevents invalid icon selection

**Error Handling:**
- "Name already exists" → Form error message
- "Cannot delete category with [N] transactions" → Modal alert
- "System categories cannot be modified" → Disable buttons
- Network errors → Graceful error message

**Accessibility (WCAG 2.1 AA):**
- **Keyboard Navigation**: All UI interactions accessible via Tab, Enter, Escape keys; modals focusable with clear focus management
- **ARIA Labels**: Form inputs labeled with `<label>` elements or `aria-label` attributes; buttons describe action (e.g., "Edit Coffee Subscriptions category"); icons paired with screen-reader text
- **Semantic HTML**: Proper heading hierarchy (`<h1>`, `<h2>`, etc.), `<form>` elements for forms, `<button>` for buttons, `<fieldset>` for grouped form controls
- **Screen Reader Support**: Category list announced as table with columns (Name, Type, Color, Actions); edit/delete buttons marked with `aria-label="Edit [CategoryName] category"`; error messages associated with form fields via `aria-describedby`
- **Color Contrast**: Text ≥4.5:1 against background; category color badges paired with text label (not color-only indicators)
- **Testing**: Lighthouse A11y audit ≥90 score; axe-core automated tests in CI; manual keyboard/screen reader validation

### User Story 1 - View All Categories (Priority: P1)

**Narrative**:

Emma logs into SauronSheet and navigates to the Categories management page. She needs to see all available categories organized for reference. The page displays her personal categories mixed with system defaults. She can quickly identify which are built-in (marked as read-only) and which are her custom ones.

**Why this priority**: 

P1 because it's the foundation for all other category operations. Users must see categories before they can edit or delete them. It establishes visual hierarchy and system/custom distinction critical for understanding data structure.

**Independent Test**: 

Can be fully tested by: opening Categories page → verify 24 system categories display + any custom user categories display in correct groupings (Income/Expenses) with visual badges indicating system vs. custom status → delivers immediate value of category awareness.

**Acceptance Scenarios**:

1. **Given** Emma is logged in and has never created custom categories, **When** she opens the Categories page, **Then** she sees exactly 24 system categories organized into 6 groups (Income, Fixed Expenses, Variable Expenses, Lifestyle, Finance & Other), each with name, color chip, and icon displayed.

2. **Given** Emma has created 3 custom categories (e.g., "Side Hustle", "Freelance", "Bonus"), **When** she views the Categories page, **Then** she sees:
   - All 24 system categories with a "System" or lock badge
   - Her 3 custom categories marked as "Custom" or "Personal"
   - Categories grouped logically (Income together, Expenses together)
   - Each category shows: Name, Color chip, Icon, Type (Income/Expense), Edit/Delete buttons (on custom only)

3. **Given** Emma scrolls through the category list, **When** she examines a system category (e.g., "Salary" under Income), **Then** the Edit and Delete buttons are disabled/hidden and the category has a "System" badge to prevent accidental modification.

4. **Given** Emma scrolls through the category list, **When** she examines one of her custom categories (e.g., "Side Hustle"), **Then** the Edit and Delete buttons are visible and enabled, allowing her to modify or remove it (unless it has transactions).

---

### User Story 2 - Create Custom Category (Priority: P1)

**Narrative**:

Emma realizes SauronSheet's 24 default categories don't perfectly match her spending patterns. She wants to create a custom category called "Coffee Subscriptions" to track her daily coffee habit separately. She clicks "Add Category" button, fills in a form with name, selects a color (warm brown), chooses an icon (coffee cup), marks it as Expense type, and clicks Save.

**Why this priority**: 

P1 because personalization is core to SauronSheet's value. Users must be able to create categories matching their unique financial life. Without this, system is generic and doesn't adapt to user needs.

**Independent Test**: 

Can be fully tested by: clicking "Add Category" → entering valid form data → submitting → verifying new category appears in list with correct properties and can be used for tagging transactions → user gains ability to track custom spending patterns.

**Acceptance Scenarios**:

1. **Given** Emma is on the Categories page, **When** she clicks "Add New Category" button, **Then** a form modal opens with fields:
   - Category Name (text input, required, max 50 chars)
   - Type (dropdown: Income or Expense, required)
   - Color (color picker showing hex value, required, default to #3498db)
   - Icon (icon selector dropdown showing available icons, required, default to "question-circle")
   - Buttons: Cancel, Save

2. **Given** Emma fills in:
   - Name: "Coffee Subscriptions"
   - Type: Expense
   - Color: #8B6F47 (coffee brown)
   - Icon: "coffee"
   
   **When** she clicks Save, **Then**:
   - Modal closes
   - New category "Coffee Subscriptions" appears in her custom categories section
   - Category is immediately available for tagging transactions
   - No page refresh required (smooth UX)
   - Success message displays: "Category 'Coffee Subscriptions' created"

3. **Given** Emma tries to create a category with name "Salary" but she already has an Income category named "Salary", **When** she clicks Save, **Then** the form displays error: "A category with name 'Salary' already exists" and the Save button is disabled until she changes the name.

4. **Given** Emma clicks Save on the form without filling Name field, **When** the form validates, **Then** name field shows red border with error text: "Category name is required" and Save button stays disabled.

5. **Given** Emma enters a category name with 51 characters, **When** she tries to save, **Then** the form shows error: "Category name must not exceed 50 characters" and prevents submission.

---

### User Story 3 - Edit Custom Category (Priority: P2)

**Narrative**:

Three months later, Emma wants to rename her "Coffee Subscriptions" category to "Daily Coffee Budget" because she realized it now includes tea too. She clicks Edit on that category, modifies the name in the form, and saves. The change applies immediately.

**Why this priority**: 

P2 because while important for long-term UX, it can be done via delete + recreate as workaround. However, edit is more efficient and preserves transaction history with the category.

**Independent Test**: 

Can be fully tested by: opening existing custom category → clicking Edit → modifying one or more fields → saving → verifying updates immediately appear in list and are reflected in transaction records that reference this category.

**Acceptance Scenarios**:

1. **Given** Emma's "Coffee Subscriptions" category exists with color #8B6F47 and icon "coffee", **When** she clicks the Edit button on this category, **Then** a form modal opens with:
   - Name field pre-filled with "Coffee Subscriptions"
   - Type field showing "Expense" (read-only, cannot change type)
   - Color field showing #8B6F47
   - Icon field showing "coffee"
   - Buttons: Cancel, Save

2. **Given** the Edit form is open with current data, **When** Emma changes:
   - Name to "Daily Coffee Budget"
   - Color to #A0826D
   - Icon to empty (keeps existing coffee icon)
   
   **And** clicks Save, **Then**:
   - Modal closes
   - Category list updates with new name "Daily Coffee Budget"
   - All transactions previously tagged "Coffee Subscriptions" now show "Daily Coffee Budget"
   - Success message: "Category 'Daily Coffee Budget' updated"

3. **Given** Emma is editing the category, **When** she tries to change the name to "Salary" (which already exists as an Income category), **Then** the form shows error: "A category with name 'Salary' already exists" and Save is disabled.

4. **Given** Emma tries to edit a system category (e.g., "Salary"), **When** she clicks on it, **Then** the Edit button is not clickable or the form opens in read-only mode with a message: "System categories cannot be modified" and only a Close button is available.

5. **Given** Emma is editing a category and clicks Cancel, **When** the modal closes, **Then** any unsaved changes are discarded and the category list shows the category's original values.

---

### User Story 4 - Delete Custom Category (Priority: P2)

**Narrative**:

After tracking expenses for a year, Emma wants to clean up her custom categories. She created "Experimental Category" as a test and never used it. She clicks Delete on that category, a confirmation dialog appears, she confirms, and the category is immediately removed from the list.

**Why this priority**: 

P2 because it enables category maintenance and cleanup. Users need ability to remove unused or incorrectly created categories. However, the delete is guarded to prevent data loss.

**Independent Test**: 

Can be fully tested by: selecting unused custom category → clicking Delete → confirming deletion → verifying category removed from list and no longer available for new transactions → verifies safe cleanup capability.

**Acceptance Scenarios**:

1. **Given** Emma has a custom category "Experimental Category" with zero transactions, **When** she clicks the Delete button, **Then** a confirmation dialog appears showing:
   - Message: "Are you sure you want to delete 'Experimental Category'? This action cannot be undone."
   - 2 buttons: Cancel, Delete

2. **Given** the confirmation dialog is open, **When** Emma clicks Delete, **Then**:
   - Dialog closes
   - Category disappears from the category list
   - Success message displays: "Category 'Experimental Category' deleted"
   - Category is no longer available for new transactions

3. **Given** Emma's "Coffee Subscriptions" category has 47 associated transactions (past expenses tagged with this category), **When** she clicks Delete on this category, **Then** instead of allowing deletion, the system shows error message dialog:
   - Title: "Cannot Delete Category"
   - Message: "This category has 47 transactions. To delete it, reassign or delete those transactions first."
   - Button: OK
   - The category remains in the list

4. **Given** Emma has a delete confirmation dialog open, **When** she clicks Cancel, **Then** the dialog closes and the category remains unchanged.

5. **Given** Emma tries to delete a system category by any means (even if somehow exposed in UI), **When** deletion is attempted, **Then** the system rejects with error: "System categories cannot be deleted" and the category remains.

---

### User Story 5 - Category Search & Filter (Priority: P3)

**Narrative**:

As Emma's custom category list grows, she wants to quickly find a specific category. She types "coffee" in a search box and only categories matching that name appear, allowing faster navigation.

**Why this priority**: 

P3 because with 24 system + user's custom categories, list could become unwieldy. However, for MVP, a simpler flat list is acceptable. Search becomes important as category count grows.

**Independent Test**: 

Can be fully tested by: entering search term → filtering results → verifying only matching categories display with highlighting → enables faster category management.

**Acceptance Scenarios**:

1. **Given** Emma views the Categories page with 30+ categories, **When** she enters "coffee" in the search field, **Then** only categories containing "coffee" in their name display (e.g., "Coffee Subscriptions", "Coffee & Tea", "Iced Coffee Orders").

2. **Given** Emma has filtered results by searching "xyz", **When** no categories match, **Then** the message "No categories found" displays and the list is empty.

3. **Given** search results are filtered, **When** Emma clears the search field, **Then** all categories display again.

---

### Edge Cases

- **What happens if user tries to create a category with only whitespace (e.g., "   ")?** System should trim input and treat as empty, triggering validation error: "Category name is required."

- **What happens if user creates a category, then immediately deletes it, then tries to create a new one with the same name?** System should allow it because the old category no longer exists.

- **What happens if a user creates category "Salary" (Income), then somehow a second user also creates "Salary" (Income)?** Each user's categories are isolated by UserId, so no conflict; both users have their own "Salary" category.

- **What happens if category name contains special characters like "Café" or "Rent & Utilities"?** System should accept and store as-is; no restrictions on special characters beyond max length.

- **What happens if user clicks Edit on a category, and another tab deletes that category while edit form is open?** If form is submitted after deletion, validation should fail with: "This category no longer exists." If form is cancelled and list refreshed, category should be gone.

- **What happens if a transaction is deleted that was the only transaction tied to a category?** Category itself is NOT deleted; category remains in list and can be used for future transactions. Only when user explicitly clicks Delete does category get removed.

- **What happens if user tries to upload an invalid image as a category icon?** System should reject and use a default icon; error message: "Invalid icon format. Using default icon."

- **What happens if a category color is stored as invalid hex (e.g., "#GGGGGG")?** This should not happen if using color picker, but in data migration scenarios, system should fall back to default color #3498db and log warning.

- **What happens if two users view the same page and one deletes a shared system category?** System categories are global (not deleted per user), so this scenario doesn't apply. System categories are either deleted globally (which should never happen in normal operation) or not deleted at all.

- **What happens if custom categories count reaches 500+?** No hard limit is enforced in MVP; system should handle gracefully with pagination or virtualization to avoid performance issues.

---

## Requirements

### Functional Requirements

**Category Entity Model:**

- **FR-001**: System MUST maintain Category entity with properties: `CategoryId` (Guid), `UserId` (string or UserId value object), `Name` (string, max 50 chars), `Type` (Enum: Income | Expense), `Color` (hex string #RRGGBB), `IconName` (string, e.g., "credit-card"), `IsSystemDefault` (bool), `CreatedDate` (DateTime), `UpdatedDate` (DateTime)

**System Default Categories:**

- **FR-002**: System MUST seed exactly 24 immutable system categories across 6 groups on application startup (via database migration):
  
  **💰 Income (5 categories):**
  - Salary: Sueldo principal, bonos y comisiones | Icon: building-dollar | Color: #27AE60
  - Sales: Dinero por vender artículos usados | Icon: shopping-bag | Color: #27AE60
  - Investments: Dividendos, intereses, ganancias cripto | Icon: trending-up | Color: #27AE60
  - Gifts: Dinero recibido por cumpleaños o eventos | Icon: gift | Color: #27AE60
  - Other Income: Devoluciones de impuestos, premios | Icon: star | Color: #27AE60
  
  **🏠 Fixed Expenses (5 categories):**
  - Housing: Alquiler, hipoteca, comunidad, IBI | Icon: home | Color: #E74C3C
  - Utilities: Electricidad, agua, gas, internet, móvil | Icon: lightbulb | Color: #E74C3C
  - Insurance: Salud, vida, coche, hogar | Icon: shield | Color: #E74C3C
  - Subscriptions: Netflix, Spotify, gimnasio, nube | Icon: repeat | Color: #E74C3C
  - Education: Colegio, universidad, cursos online | Icon: book | Color: #E74C3C
  
  **🛒 Variable Expenses (5 categories):**
  - Groceries: Supermercado, compras básicas despensa | Icon: shopping-cart | Color: #F39C12
  - Transportation: Gasolina, transporte público, parkings, peajes | Icon: car | Color: #F39C12
  - Personal Care: Peluquería, cosméticos, farmacia | Icon: scissors | Color: #F39C12
  - Home: Productos limpieza, reparaciones pequeñas, decoración | Icon: wrench | Color: #F39C12
  - Pets: Comida, veterinario, accesorios | Icon: paw | Color: #F39C12
  
  **🎭 Lifestyle & Leisure (5 categories):**
  - Restaurants: Comidas fuera, café diario, delivery | Icon: utensils | Color: #9B59B6
  - Entertainment: Cine, conciertos, videojuegos, libros | Icon: star | Color: #9B59B6
  - Shopping: Ropa, calzado, gadgets tecnológicos | Icon: shopping-bags | Color: #9B59B6
  - Travel: Vuelos, hoteles, actividades vacaciones | Icon: plane | Color: #9B59B6
  - Health & Wellness: Dentista, terapia, suplementos deportivos | Icon: heart | Color: #9B59B6
  
  **📉 Finance & Other (4 categories):**
  - Debt Payments: Pago tarjetas crédito, préstamos personales | Icon: credit-card | Color: #34495E
  - Savings & Investment: Transferencias fondos emergencia, bolsa | Icon: piggy-bank | Color: #34495E
  - Donations: ONGs, ayuda a familiares | Icon: hands-helping | Color: #34495E
  - Unexpected Expenses: Multas, reparaciones emergencia, médicas | Icon: exclamation-circle | Color: #34495E

- **FR-003**: System MUST mark all seeded categories with `IsSystemDefault = true` and prevent any modification or deletion of these categories via API/UI.

**Custom Category Operations:**

- **FR-004**: Users MUST be able to create custom categories within their own account by providing: Name (required, max 50 chars), Type (required: Income or Expense), Color (required, hex format), IconName (required, must be valid Bootstrap icon).

- **FR-005**: System MUST validate that custom category names are unique per user (no two categories with same name for same user, across all types).

- **FR-006**: System MUST enforce that custom category names cannot duplicate system category names.

- **FR-007**: Users MUST be able to edit custom category properties (Name, Color, IconName) but NOT Type or IsSystemDefault flag.

- **FR-008**: Users MUST be able to delete custom categories they created, UNLESS the category has one or more associated transactions.

- **FR-009**: System MUST prevent deletion if category has active transactions, displaying error: "This category has [N] transactions. To delete it, reassign or delete those transactions first."

**Category Retrieval & Display:**

- **FR-010**: System MUST retrieve all categories (system + custom) for logged-in user ordered: System categories (alphabetically), then Custom categories (alphabetically within each type).

- **FR-011**: System MUST return category color as hex string (#RRGGBB format) and validate hex format on input.

- **FR-012**: System MUST allow filtering categories by Type (Income | Expense) on API and UI level.

**User Isolation:**

- **FR-013**: System MUST ensure users cannot view, edit, or delete other users' custom categories.

- **FR-014**: System MUST automatically assign CreatedDate and UpdatedDate (UTC timezone) on category creation and updates.

**Validation & Error Handling:**

- **FR-015**: System MUST reject empty or whitespace-only category names and display: "Category name is required."

- **FR-016**: System MUST reject category names exceeding 50 characters with error: "Category name must not exceed 50 characters."

- **FR-017**: System MUST reject invalid color hex format with error: "Color must be valid hex code (e.g., #FF5733)."

- **FR-018**: System MUST reject invalid icon names (if icon doesn't exist in available library) with error: "Icon '[name]' is not available. Choose from available icons."

**Accessibility (WCAG 2.1 AA):**

- **FR-019**: UI MUST be fully keyboard-accessible: all form fields, buttons, modals, dropdowns navigable via Tab/Shift+Tab, passable via Enter/Space, closeable via Escape. Focus management preserved through modal open/close cycles.

- **FR-020**: UI MUST include semantic HTML, ARIA labels for interactive elements, and descriptive button text. Form errors associated with fields via `aria-describedby`. Category lists announced as structured data with column headers.

- **FR-021**: UI MUST maintain ≥4.5:1 color contrast for all text. Category color badges paired with text labels (not color-only). Tested via Lighthouse A11y audit (target ≥90) and axe-core automated scans.

---

### Business Rules & Constraints

| Rule | Description | Enforcement Level |
|------|-------------|-------------------|
| R-001 | System categories are immutable | Domain: throw DomainException if modification attempted |
| R-002 | Category names unique per user | Application: validation in handler before persistence |
| R-003 | No duplicate name with system defaults | Application: check against seeded list |
| R-004 | Cannot delete category with transactions | Application: query transaction count before delete |
| R-005 | Name max 50 chars, no empty/whitespace | Domain: ValueObject validation on construction |
| R-006 | Color must be valid hex (#RRGGBB) | Domain: ValueObject validation |
| R-007 | Icon must be from allowed library | Application: validate against allowed icon list |
| R-008 | Type cannot be changed after creation | Application: omit Type from update handler |
| R-009 | Categories scoped to UserId | Infrastructure: all queries filtered by UserId |
| R-010 | CreatedDate/UpdatedDate auto-managed | Infrastructure: set by repository on create/update |
| R-011 | Concurrent updates use Last-Write-Wins (LWW) | Infrastructure: UNIQUE(UserId, Name) prevents name conflicts; color/icon updates resolve via DB row timestamp |
| R-012 | WCAG 2.1 AA Compliance Required | Frontend: keyboard nav, semantic HTML, ARIA labels, ≥4.5:1 color contrast, Lighthouse ≥90 |

---

### Key Entities

**Category (Aggregate Root)**

```
CategoryId (ValueObject)
  - Value: Guid
  - Constraints: Cannot be Guid.Empty

Category (Entity)
  - categoryId: CategoryId
  - userId: UserId (or string if using Supabase user IDs)
  - name: string (1-50 chars, trimmed)
  - type: CategoryType enum (Income | Expense)
  - color: string (hex format #RRGGBB, e.g., "#F39C12")
  - iconName: string (Bootstrap icon name, e.g., "shopping-cart")
  - isSystemDefault: bool (true for seeded, false for custom)
  - createdDate: DateTime (UTC, set on creation)
  - updatedDate: DateTime (UTC, set on creation/update)

Relationships:
  - One-to-Many: Category → Transactions (through TransactionId foreign key)
```

**CategoryType (Enum)**

```
Income   (0) - Revenue sources
Expense  (1) - Money outflows
```

**Validation ValueObjects** (Per Clarification #5: Defense-in-Depth Color Validation; Clarification #6: IconName Application Validation)

```
CategoryName (ValueObject)
  - Value: string
  - Constraints: 1-50 chars, not empty/whitespace, trimmed
  - Validation: Applied in Domain layer; prevents invalid names at construction

ColorHex (ValueObject)
  - Value: string (hex format)
  - Constraints: Must match regex #[0-9A-F]{6} (uppercase hex, 6 digits)
  - Validation: Domain layer regex enforcement + Frontend HTML5 color picker (defense-in-depth per Clarification #5)
  - Examples: #F39C12, #E74C3C, #27AE60

IconName (String, validated in Application layer)
  - Value: string (Bootstrap icon identifier, e.g., "shopping-cart", "home", "credit-card")
  - Constraints: Must exist in AllowedBootstrapIcons constant (~100+ valid Bootstrap icon names)
  - Validation: Application handler validates against AllowedBootstrapIcons at command time (per Clarification #6)
  - Note: NOT a ValueObject; kept as simple string in Domain to decouple Domain from Bootstrap library semantics
  - Defense-in-depth: Frontend icon picker dropdown (prevents invalid selections) + Application handler validation (enforces at entry point)
```

---

## Success Criteria

### Measurable Outcomes

**Functional Completeness:**

- **SC-001**: All 24 system default categories display correctly in the UI within 500ms of page load, grouped by Income/Expenses with proper icons, colors, and system badge.

- **SC-002**: Users can create a new custom category (name, type, color, icon) in under 1 minute through the UI and it appears in the list immediately without page reload.

- **SC-003**: Users can edit a custom category's name/color/icon and changes are reflected in the UI and persist in database correctly within 1 second of save.

- **SC-004**: Users can delete an unused custom category with one click (confirm dialog) and it removed from UI within 500ms.

- **SC-005**: System prevents deletion of categories with transactions, displaying clear error message with transaction count within 300ms.

**Data Integrity & Validation:**

- **SC-006**: 100% of system categories are correctly seeded in database on first application run with accurate names, colors, icons, and isSystemDefault=true flag.

- **SC-007**: All custom category names are validated for uniqueness per user before persistence (duplicate names rejected 100% of the time).

- **SC-008**: All category operations (create/update/delete) correctly route through MediatR handlers with proper error handling and logging.

- **SC-009**: Category color compliance: 100% of stored colors are valid hex format (#RRGGBB).

- **SC-010**: User isolation: Users cannot view or modify other users' custom categories (100% role enforcement in queries/commands).

**User Experience:**

- **SC-011**: First-time users can understand the category system within 2 minutes by observing system category groupings and visual distinction (system vs. custom).

- **SC-012**: Category search/filter (if implemented) finds matching categories with <500ms response time.

- **SC-013**: Form validation provides clear, actionable error messages (not generic "error occurred") for all 8+ validation rules.

- **SC-014**: Icon picker displays available icons with visual preview and allows selection in under 10 clicks.

**Accessibility (WCAG 2.1 AA):**

- **SC-021**: All UI elements (forms, buttons, dropdowns, modals) are fully keyboard-navigable without mouse; Tab/Shift+Tab moves focus correctly; Enter/Space activates buttons; Escape closes modals.

- **SC-022**: All form inputs have associated labels or `aria-label` attributes; edit/delete buttons labeled with category names (e.g., "Edit Coffee Subscriptions category").

- **SC-023**: Category list displayed as semantic HTML `<table>` with `<th>` headers (Name, Type, Color, Actions); screen reader announces structure correctly.

- **SC-024**: Form error messages associated with fields via `aria-describedby`; screen reader announces which field has error.

- **SC-025**: All text has ≥4.5:1 color contrast ratio against background; category color badges paired with text (not color-only).

- **SC-026**: Lighthouse A11y audit score ≥90; axe-core automated tests pass with zero violations; manual screen reader testing (NVDA/JAWS) confirms usability.

**Test Coverage:**

- **SC-015**: Domain layer (Category entity, ValueObjects) has 100% unit test coverage.

- **SC-016**: Application layer (command/query handlers) has ≥80% test coverage.

- **SC-017**: Integration tests verify end-to-end category CRUD operations (create → retrieve → update → delete) work correctly with database.

**Performance:**

- **SC-018**: List all categories query completes in <100ms for user with up to 100 custom categories.

- **SC-019**: Create/Update/Delete operations complete in <500ms including database persistence.

**Phase Completion:**

- **SC-020**: Feature meets full-stack scope: Domain entities + Application handlers + Frontend UI + Infrastructure (Supabase) persistence fully integrated.

---

## Assumptions

- **A-001**: System categories are global and not user-specific (all users see the same 24 default categories). Custom categories are user-scoped.
- **A-002**: Color hex values stored as uppercase strings (e.g., #F39C12, not #f39c12) for consistency.
- **A-003**: Bootstrap Icons 5.x library is integrated in Frontend via CDN (jsDelivr) at pinned version 5.3.0: `https://cdn.jsdelivr.net/npm/bootstrap-icons@5.3.0/font/bootstrap-icons.css`. AllowedBootstrapIcons constant mirrors 5.x icon list (~120 icons). Upgrade to 6.x requires spec + constant update.
- **A-004**: Database connection to Supabase is already configured in Infrastructure layer.
- **A-005**: User authentication is already implemented (Phase 1) and UserId/token available in PageModel.
- **A-006**: MediatR pipeline for commands/queries is already configured in Application layer.
- **A-007**: Category is scoped to single user (no multi-user categories or teams in MVP).
- **A-008**: CRUD operations follow Clean Architecture: Domain entities → Application handlers (CQRS) → Infrastructure persistence.
- **A-009**: No duplicate system category names exist in seed data.
- **A-010**: Error messages are user-friendly, non-technical language.

---

## Out of Scope (Phase 2 Boundary)

- Category budgets or spending limits
- Recurring category assignments
- Category analytics/reporting
- Export/import categories
- Multi-language support (descriptions in other languages)
- **Soft deletes & audit trails** — Phase 2 uses hard delete (immediate permanent removal). Soft delete, audit logs, and deletion recovery deferred to Phase 3+ when compliance/audit requirements are clarified (per Clarification #10).
- Subcategories or hierarchies
- Real-time sync across devices
- Batch operations (update/delete multiple at once)

---

_Last Updated: March 12, 2026 | Phase: 002-category-management | Status: Ready for Clarification Review_
