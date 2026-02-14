# SauronSheet Phase 3: Transaction Import Pipeline (PDF Parsing)

**Version**: 1.0.0  
**Duration**: 3-4 weeks  
**Status**: ⏳ Blocked by Phase 2  
**Depends**: Phase 0, Phase 1, Phase 2

---

## Goal

Build PDF import pipeline: upload bank statement → parse transactions → validate against domain rules → persist to database. This enables users to bulk-import transactions instead of manual entry.

**Risk Level**: HIGH (PDF parsing library selection + error handling complexity)

---

## Requirements

### Functional Requirements

| ID | Requirement | Acceptance Criteria |
|----|-------------|-------------------|
| **FR-001** | PDF upload endpoint | User selects PDF, sends to /api/transactions/import |
| **FR-002** | PDF parsing library selection | Spike: evaluate iTextSharp, PdfSharp, Aspose |
| **FR-003** | Transaction extraction from table | Parse bank statement date, amount, description |
| **FR-004** | Category auto-matching | Suggest category based on description (ML optional Phase 5+) |
| **FR-005** | Validation before import | Check for duplicates, invalid amounts, future dates |
| **FR-006** | Batch import with rollback | Atomic: all pass or all fail |
| **FR-007** | Import status reporting | User sees count of imported, errors, warnings |

### Non-Functional Requirements
- NF-001: 12 integration tests covering import workflow + error scenarios
- NF-002: PDF parsing fails gracefully with user-friendly error message
- NF-003: Maximum PDF size: 10 MB
- NF-004: Import timeout: 60 seconds
- NF-005: Duplicate detection: exact date + amount match

---

## Architecture

### Import Pipeline Flow

```
1. User uploads PDF
2. File validation (size, format)
3. PDF parsing → list of raw transactions
4. Domain validation (amount > 0, category exists, no future dates)
5. Duplicate detection (check if date + amount + description exists)
6. Database transaction begins
7. Transactions + CSV import record persisted
8. On success: return import ID + count
9. On error: rollback, return error details
```

### New Components
- `ImportTransactionsFromPdfCommand` (Application)
- `PdfParserService` (Infrastructure) - abstracted behind interface
- `ImportRecord` entity (Domain) - tracks import metadata
- CSV export query (Application) - export transactions post-import

---

## Deliverables

### Domain Layer
- [ ] `Domain/Entities/ImportRecord.cs` - PDF import metadata (date, count, status)
- [ ] `Domain/ValueObjects/FileReference.cs` - S3/Supabase URL reference
- [ ] `Domain/Repositories/IImportRecordRepository.cs`

### Application Layer
- [ ] `Application/Features/Imports/ImportTransactionsFromPdfCommand.cs` + handler
- [ ] `Application/Features/Imports/GetImportHistoryQuery.cs` + handler
- [ ] `Application/Features/Exports/ExportTransactionsToCsvQuery.cs` + handler
- [ ] `Application/Services/PdfParserService.cs` (interface only)
- [ ] `Application/Services/CategoryMatchingService.cs` (rule-based matching)
- [ ] `Application/Tests/Features/Imports/ImportPdfTests.cs` (8 tests)
- [ ] `Application/Tests/Features/Imports/ValidationTests.cs` (4 tests)

### Infrastructure Layer
- [ ] `Infrastructure/Services/PdfParsers/ITextSharpPdfParser.cs` (spike choice implementation)
- [ ] `Infrastructure/Persistence/Migrations/006_CreateImportRecordsTable.sql`
- [ ] `Infrastructure/FileStorage/SupabaseStorageClient.cs` (optional: store PDFs)

### Frontend Layer
- [ ] `Frontend/Pages/Imports/Upload.cshtml` + `Upload.cshtml.cs` (file upload form)
- [ ] `Frontend/Pages/Imports/History.cshtml` + `History.cshtml.cs` (import history list)
- [ ] `Frontend/Pages/Transactions/Export.cshtml` (CSV download)
- [ ] JavaScript: file drag-and-drop, progress indicator

---

## Test Specifications

### Import Tests (8 tests)

- **T03-001**: Valid PDF with 5 transactions imports all 5
- **T03-002**: Duplicate transaction rejected (same date + amount + description)
- **T03-003**: Negative amount in CSV rejected
- **T03-004**: Future transaction date rejected
- **T03-005**: Transaction with invalid category ID rejected
- **T03-006**: Partial import failure: 3 valid + 2 invalid = import 3 (atomic per transaction)
- **T03-007**: ImportRecord created with count, status, timestamp
- **T03-008**: GetImportHistoryQuery returns only current user's imports

### Validation Tests (4 tests)

- **T03-009**: PDF file size > 10 MB rejected
- **T03-010**: Non-PDF file rejected (e.g., .txt)
- **T03-011**: Malformed CSV in PDF rejected (parse error handled)
- **T03-012**: Empty PDF (no transactions) handled gracefully

---

## Success Criteria

✅ Phase 3 is complete when:

1. `dotnet test` shows **12/12 Phase 3 tests passing**
2. PDF parser library selected + integrated
3. ImportTransactionsFromPdfCommand functional
4. CategoryMatchingService suggests categories
5. Duplicate detection working
6. CSV export working
7. Import history page shows previous imports
8. All Phase 0 + 1 + 2 tests still passing (11 + 8 + 20 = 39)

Total passing tests: 39 (Phase 0-2) + 12 (Phase 3) = **51 tests**

**Phase 3 also marks partial MVP launch** (users can now import transactions + view list)

---

## Spike: PDF Parser Selection

### Candidates
1. **iTextSharp** - Industry standard, good for forms, table extraction requires custom logic
2. **PdfSharp** - Simpler API, open-source, less feature-rich
3. **Aspose** - Most robust, expensive, not needed for MVP

**Recommendation**: Select iTextSharp week 1 of Phase 3, create proof-of-concept for parsing bank statement table.

---

## Database Schema

```sql
-- Import Records
CREATE TABLE import_records (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id),
    file_name VARCHAR(255) NOT NULL,
    transaction_count INT NOT NULL,
    status VARCHAR(20) DEFAULT 'SUCCESS', -- SUCCESS, PARTIAL, FAILED
    error_message TEXT,
    imported_at TIMESTAMP DEFAULT NOW()
);

-- Link import record to transactions
ALTER TABLE transactions ADD COLUMN import_record_id UUID REFERENCES import_records(id);
```

---

## Timeline

- **Week 1**: PDF parser spike + PdfParserService interface
- **Week 2**: ImportTransactionsFromPdfCommand + validation + 12 tests
- **Week 3**: CSV export + category matching + UI pages
- **Week 4**: Error handling + refinement

Target: 12 tests green + PDF upload page functional

---

**Specification Version**: 1.0.0  
**Last Updated**: 2026-02-14
