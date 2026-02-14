# Phase 3: Transaction Import Pipeline (PDF Parsing)

**Quick Start**: Read [SPEC.md](./SPEC.md)

## Phase 3 at a Glance

| Item | Value |
|------|-------|
| Duration | 3-4 weeks |
| Depends on | Phase 0 + Phase 1 + Phase 2 |
| Goal | PDF import + bulk transaction loading |
| Tests | 12 integration tests (T03-001 to T03-012) |
| Risk | HIGH (PDF parsing complexity) |
| Milestone | **Partial MVP Launch** (Week 14) |

## Key Features

- PDF statement upload
- Automatic transaction extraction
- Duplicate detection
- Category auto-matching
- CSV export
- Import history

## Start Here

1. Read [SPEC.md](./SPEC.md)
2. **Spike Week 1**: Select PDF parser (iTextSharp recommended)
3. Create ImportRecord entity + ImportTransactionsFromPdfCommand
4. Write 12 tests
5. Build upload page + import history page

## Critical Spike

**Week 1, first task**: Evaluate iTextSharp vs PdfSharp for parsing bank statement tables. Create POC that extracts 5 transactions from sample PDF.

## Exit Criteria

```bash
✅ dotnet test         # 12/12 Phase 3 tests pass
✅ Phase 0-2 tests still pass  # 39 tests
✅ Total: 51 tests passing
✅ PDF upload working
✅ Category matching active
✅ CSV export functional
```
