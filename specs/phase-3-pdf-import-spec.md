# Phase 3: Transaction Import & PDF Parsing

**Version**: 1.0.0  
**Status**: Ready for speckit.tasks generation  
**Duration**: 3-4 weeks (HIGH RISK)  
**Depends On**: Phase 2 Complete

---

## Executive Summary

**Objective**: Enable users to import bank transactions from PDF bank statements

**What We Build**:
- PDF parsing library integration (iText or PdfSharp)
- PDF validator + parser for common bank formats
- ImportTransactionsFromPdf command
- Duplicate detection + reconciliation
- Bulk transaction creation with validation
- Import history + audit trail

**Risk Level**: HIGH - Third-party PDF parsing complexity, bank format variations

---

## PDF Import Workflow

```
User uploads PDF
    ↓
System identifies bank (by statement format)
    ↓
Parser extracts transactions (date, amount, description)
    ↓
Validate + deduplicate against existing
    ↓
Map to Transaction entities
    ↓
Create in bulk
    ↓
Return import summary (count, duplicates, errors)
```

---

## Deliverables (17 items)

**Domain Layer** (3):
- [ ] ImportTransaction value object (represents parsed transaction)
- [ ] ImportedTransactionsDomainEvent
- [ ] PDFParsingException

**Application Layer** (8):
- [ ] ImportTransactionsFromPdfCommand (takes file + returns summary)
- [ ] ImportTransactionsFromPdfHandler
- [ ] IPdfParser interface
- [ ] ImportedTransactionDto
- [ ] ImportSummaryDto (count, duplicates, errors)
- [ ] GetImportHistoryQuery + handler
- [ ] DuplicateDetectionService
- [ ] AccountReconciliationService

**Infrastructure Layer** (6):
- [ ] PdfParser implementation (iText/PdfSharp)
- [ ] BankStatementFormats factory (identify + parse by bank)
- [ ] ImportHistory entity + repository
- [ ] Migration: 003_CreateImportHistoryTable.sql
- [ ] DependencyInjection.cs (register PDF services)
- [ ] OCR service (fallback for scanned PDFs)

**Frontend Layer**:
- [ ] Upload page + UploadPageModel
- [ ] Progress indicator (async upload)
- [ ] Import summary display

---

## Test Specifications (12 tests)

Domain (2):
- T03-001: ImportTransaction validates required fields
- T03-002: ImportedTransactionsDomainEvent publishes

Application (8):
- T03-003: ImportTransactionsFromPdf parses valid PDF
- T03-004: Duplicate detection works (date + amount + description hash)
- T03-005: ImportSummaryDto counts correctly
- T03-006: GetImportHistoryQuery returns all imports
- T03-007: Transactions created in bulk (transaction management)
- T03-008: Cross-tenant imports blocked
- T03-009: Invalid PDF rejected + error reported
- T03-010: OCR fallback for scanned PDFs

Infrastructure (2):
- T03-011: Multiple bank formats supported (Chase, Bank of America, etc.)
- T03-012: ImportHistory table persists + auditable

---

## Supported Bank Formats

- Chase Bank (CSV + PDF)
- Bank of America (PDF)
- Wells Fargo (PDF)
- Generic CSV (fallback)

---

## Risk Mitigations

| Risk | Mitigation |
|------|-----------|
| PDF parsing library complexity | Use well-tested library (iText); start with simple format |
| Bank format variations | Create abstract parser + concrete implementations per bank |
| Large file uploads | Implement chunked upload + progress tracking |
| Duplicate detection performance | Use hash-based matching (date + amount + description) |
| OCR for scanned PDFs | Use Tesseract or cloud OCR API (post-Phase 3) |

---

## Success Criteria

✅ Parse transactions from PDF statements (3+ banks)  
✅ Duplicate detection accurate  
✅ 12/12 tests passing  
✅ Bulk import performance acceptable (<10s for 1000 transactions)  
✅ User can track import history  

---

## Next Phase

Phase 4: Analytics Dashboard (3-4 weeks) - MVP RELEASE
- Create dashboard page with charts
- Spending by category
- Monthly trends
- Budget alerts
