# Design: ING PDF Field Reconstruction

## Technical Approach

Replace the fragile dual-path (single-line/multi-line) logic in `IngBankPdfParser` with a unified **block-first pipeline**: assemble → extract R→L → extract L→R taxonomy → emit `RawTransactionRow`. No changes to Domain or Application layers. Detection moves to header-only, decoupled from parse success.

## Architecture Decisions

| Decision | Alternatives | Rationale |
|----------|-------------|-----------|
| Unified block pipeline | Patch existing dual-path | Eliminates root cause (position-by-line-index assumption); single extraction path regardless of line count |
| R→L monetary extraction on full block text | `ExtractTrailingNumbers` over partial text | Avoids false matches in description text; operates on known contract (last two tokens = balance, amount) |
| `IngControlledTaxonomy` as ordered dictionary | Open regex, KnownCategories list | Spec IBR-3 requires L→R match of known ING categories; ordered dictionary enables longest-prefix-first matching |
| Header-based ING detection | Parse-then-count detection | Current approach parses entire PDF twice; header scan is O(1 pages) and decouples detection from parser strictness |
| Keep `IngColumnThresholds` as optional geometric fallback | Delete it | Provides secondary signal for edge cases where taxonomy fails; avoids breaking existing single-line geometric tests |

## Data Flow

```
PDF pages ──→ ReconstructLinesFromWords (existing, unchanged)
         ──→ IngBlockAssembler.Assemble(lines)
                 │ groups by date-starting lines
                 ▼
         List<IngBlock> (date + joined text per block)
                 │
                 ├──→ IngMonetaryExtractor.ExtractRightToLeft(block.Text)
                 │        → (amount, balance, cleanText)
                 │
                 └──→ IngControlledTaxonomy.ExtractLeftToRight(cleanText)
                          → (category?, subCategory?, description)
                          
         ──→ RawTransactionRow (existing record, unchanged)
```

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `Infrastructure/PDF/Parsers/IngBlock.cs` | Create | `internal readonly record struct IngBlock(string Date, string FullText, IngLineData[] Lines)` — assembled logical block |
| `Infrastructure/PDF/Parsers/IngBlockAssembler.cs` | Create | Pure static helper: takes `List<IngLineData>`, returns `List<IngBlock>` using date-pattern as delimiter |
| `Infrastructure/PDF/Parsers/IngMonetaryExtractor.cs` | Create | Static R→L extractor: returns `(string? amount, string? balance, string cleanText)` or null on failure |
| `Infrastructure/PDF/Parsers/IngControlledTaxonomy.cs` | Create | Ordered category/subcategory dictionary; `ExtractLeftToRight(text)` → `(cat?, subCat?, description)` |
| `Infrastructure/PDF/Parsers/IngBankPdfParser.cs` | Modify | Replace `FlushRowBuffer`/`ParseMultiLineTransaction`/`ParseIngTransactionLine` with pipeline calls; keep `ReconstructLinesFromWords` and `NormalizeAmount` intact |
| `Infrastructure/PDF/Parsers/AdaptivePdfParser.cs` | Modify | `IsIngFormatAsync` → header-scan only (no full parse); add `HasIngHeader(Stream)` method |
| `Infrastructure/PDF/Parsers/IngTransactionLineParser.cs` | Delete | Superseded by `IngControlledTaxonomy` L→R extraction |
| `Infrastructure/PDF/Parsers/IngColumnThresholds.cs` | Keep | Degraded to optional fallback (not called from main pipeline unless taxonomy returns null and geometric signal exists) |
| `tests/.../PDF/Parsers/IngBlockAssemblerTests.cs` | Create | Unit tests IBR-1a/1b/1c |
| `tests/.../PDF/Parsers/IngMonetaryExtractorTests.cs` | Create | Unit tests IBR-2a/2b |
| `tests/.../PDF/Parsers/IngControlledTaxonomyTests.cs` | Create | Unit tests IBR-3a/3b/3c |
| `tests/.../PDF/Parsers/IngBankPdfParserBlockTests.cs` | Create | Integration tests with text fixtures (DAZN, parking, nómina) |
| `tests/.../PDF/IngTransactionLineParserTests.cs` | Delete | Covered by new taxonomy tests |
| `openspec/specs/pdf-category-extraction/spec.md` | Modify | Delta PCE-1 for ING controlled taxonomy path |

## Testing Strategy

| Layer | What | Approach |
|-------|------|----------|
| Unit | `IngBlockAssembler` | Pure text input → assert block boundaries (IBR-1) |
| Unit | `IngMonetaryExtractor` | Text with amounts → assert R→L isolation (IBR-2) |
| Unit | `IngControlledTaxonomy` | Known/unknown combos → assert L→R + fallback (IBR-3) |
| Unit | Fallback null | Malformed blocks → null result (IBR-4) |
| Integration | `IngBankPdfParser` pipeline | Text fixtures from Jan-2025 real data (DAZN, parking, nómina, multi-line) |
| Integration | `AdaptivePdfParser` detection | Header-present/absent streams (IBR-5) |

## Migration / Rollout

No migration required. All changes are in Infrastructure. Chained PRs:
- **PR 1** (~250 LOC): `IngBlock` + `IngBlockAssembler` + `IngMonetaryExtractor` + their unit tests
- **PR 2** (~350 LOC): Wire pipeline in `IngBankPdfParser` + `AdaptivePdfParser` header detection + fallback tests
- **PR 3** (~300 LOC): `IngControlledTaxonomy` + regression fixtures + delta OpenSpec

## Open Questions

- [ ] Complete list of ING categories/subcategories — user confirmed contract but not the full enum; will extract from Jan-2025 fixture and extend incrementally
