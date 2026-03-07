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

4. **Color & Icon Flexibility** — Colors are hex strings (#RGB format) stored in database. Icons are names of Bootstrap icons (e.g., "bootstrap-icon-name") defined in frontend. This allows UI to render icons from any icon library without backend changes.

5. **No Partial Deletes** — If a category has even one transaction, it cannot be deleted. This prevents orphaned transaction records and data integrity issues. Users must reassign or hide transactions first.

6. **Name Uniqueness Per User** — Within a user's account, category names (across all types) must be unique. So no duplicates in either Income or Expense categories.

---

## User Scenarios & Testing

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

**Validation ValueObjects**

```
CategoryName (ValueObject)
  - Value: string
  - Constraints: 1-50 chars, not empty/whitespace, trimmed

CategoryColor (ValueObject)
  - Value: string (hex format)
  - Constraints: Must match regex #[0-9A-Fa-f]{6}

CategoryIcon (ValueObject)
  - Value: string
  - Constraints: Must exist in AllowedIcons list (e.g., "shopping-cart", "home", etc.)
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
- **A-003**: Bootstrap icon library is integrated in Frontend (via CDN or bundled) and available for selection.
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
- Category tagging or soft deletes
- Subcategories or hierarchies
- Real-time sync across devices
- Batch operations (update/delete multiple at once)

---

_Last Updated: March 7, 2026 | Phase: 002-category-management | Status: Ready for Clarification Review_
