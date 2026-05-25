# Tasks: ING Single-Line Category Extraction Fix

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | 550–650 (additions + deletions) |
| 400-line budget risk | Low (budget 800 — ~70% used) |
| Chained PRs recommended | No |
| Suggested split | Single PR |
| Delivery strategy | single-pr |
| Chain strategy | size-exception not required |

Decision needed before apply: No
Chained PRs recommended: No
Chain strategy: single-pr
400-line budget risk: Low

### Suggested Work Units

| Unit | Goal | Likely PR | Notes |
|------|------|-----------|-------|
| 1 | All 5 files in one focused PR | PR 1 | Parser-only scope; tests/docs included |

---

## Phase 1: RED — IngColumnThresholds (TDD)

- [x] 1.1 Create `tests/.../PDF/Parsers/IngColumnThresholdsTests.cs` — test `FromHeaderWords` happy path with real ING Jan-2025 header X positions (`CATEGORÍA ~175`, `SUBCATEGORÍA ~275`, `CONCEPTO ~375`); assert `CategoryStart`, `SubCategoryStart`, `DescriptionStart` not null.
- [x] 1.2 Add test `FromHeaderWords_MissingHeaderWords_ReturnsNull` — no header words present → method returns null.
- [x] 1.3 Add test `SplitWords_ThreeBuckets_ReturnsCategorySubCategoryDescription` — PCE-SLa fixture: words spanning all three X zones → all three strings populated.
- [x] 1.4 Add test `SplitWords_AllWordsInDescriptionZone_ReturnsNull` — PCE-SLb fallback: all words in description X range → method returns null.
- [x] 1.5 Add test `SplitWords_CategoryOnlyNoSubCategory_ReturnsPartialResult` — PCE-SLc: words only in category + description zones → `Category != null`, `SubCategory == null`.
- [x] 1.6 Confirm `dotnet test` shows exactly 5 new failing tests (IngColumnThresholdsTests) — no other tests broken.

## Phase 2: GREEN — Implement IngColumnThresholds

- [x] 2.1 Create `src/.../PDF/Parsers/IngColumnThresholds.cs` — `internal sealed class` with `CategoryStart`, `SubCategoryStart`, `DescriptionStart` properties.
- [x] 2.2 Implement `FromHeaderWords(PositionedWord[] headerWords)` — locates words "CATEGORÍA", "SUBCATEGORÍA", "CONCEPTO" (case-insensitive); returns `null` if any threshold is undetectable.
- [x] 2.3 Implement `SplitWords(PositionedWord[] words)` — assigns each word to a bucket by comparing `word.Left` to thresholds; returns `null` (fallback) when category bucket is empty.
- [x] 2.4 Run `dotnet test --filter IngColumnThresholdsTests` — all 5 tests green.

## Phase 3: RED — IngBankPdfParser single-line (TDD, replace existing tests)

- [x] 3.1 Replace `tests/.../PDF/Parsers/IngBankPdfParserSingleLineTests.cs` — remove reflection-based `ParseTextColumns` tests that document the old null-category behavior (D5/W3 now revoked).
- [x] 3.2 Add test `ParseIngTransactionLine_Jan2025PositionedWords_ExtractsCategory` — PCE-SLa: construct `IngLineData` with Jan-2025 fixture positioned words; call internal method via reflection; assert `Category == "Compras"`, `SubCategory == "Ropa y complementos"`.
- [x] 3.3 Add test `ParseIngTransactionLine_InsufficientXSignal_FallsBackToDescription` — PCE-SLb: all words clustered in description X zone; assert `Category == null`, `Description` contains full text.
- [x] 3.4 Add test `ParseIngTransactionLine_NullOrWhitespace_ReturnsAllNull` — boundary guard (keeps parity with removed test).
- [x] 3.5 Confirm new tests fail with `ParseTextColumns` still ignoring positions (expected RED state).

## Phase 4: GREEN — Refactor IngBankPdfParser

- [x] 4.1 Add `internal readonly record struct PositionedWord(string Text, double Left)` and `internal readonly record struct IngLineData(string Text, PositionedWord[] Words)` at top of `IngBankPdfParser.cs`.
- [x] 4.2 Refactor `ReconstructLinesFromWords(List<Word> words)` → returns `List<IngLineData>`; each `IngLineData` carries ordered `PositionedWord[]` (from `word.BoundingBox.Left`) alongside the joined text string.
- [x] 4.3 Capture header thresholds per page: when a reconstructed line contains "F. VALOR" and "CATEGORÍA", extract `IngColumnThresholds.FromHeaderWords(lineData.Words)` and store in a page-scoped variable.
- [x] 4.4 Refactor `ParseTextColumns(string? text)` → `ParseTextColumns(string? text, PositionedWord[]? words = null, IngColumnThresholds? thresholds = null)` — when both are non-null, attempt `thresholds.SplitWords(words)`; on null result apply existing fallback; on success return split buckets.
- [x] 4.5 Update `ParseIngTransactionLine` to accept `IngLineData` (instead of `string`) and forward `lineData.Words` + captured `thresholds` to `ParseTextColumns`.
- [x] 4.6 Update `FlushRowBuffer` single-line path to pass `IngLineData` instead of raw string to `ParseIngTransactionLine`.
- [x] 4.7 Multi-line path: use `lineData.Text` only (no positions forwarded) — `IngTransactionLineParser.ExtractFromMultiLine` unchanged — PCE-SLd guard.
- [x] 4.8 Add Sentry breadcrumb on fallback activation: `"ParseTextColumns: X-signal insufficient — fallback to full-text description"`.
- [x] 4.9 Run `dotnet test --filter IngBankPdfParserSingleLineTests` — all 3 new tests green.

## Phase 5: Integration Test + Wiring Verification

- [x] 5.1 RED: Add `ImportTransactionsFromPdfCommand_SingleLineRowWithCategory_DoesNotProduceRawOnly` to `tests/.../Commands/ImportTransactionsFromPdfCommandTests.cs` — mock parser returns `RawTransactionRow` with `Category = "Compras"`, `SubCategory = "Ropa y complementos"`; assert resolved `CategorySource != RawOnly`.
- [x] 5.2 GREEN: Verify test passes with no prod-code changes (parser refactor already propagates correctly).

## Phase 6: No-Regression + Cleanup

- [x] 6.1 VERIFY: Run `dotnet test --filter IngTransactionLineParserTests` — all 8 multi-line tests green (PCE-SLd).
- [x] 6.2 VERIFY: Run full `dotnet test` — zero regressions.
- [x] 6.3 Remove stale XML `<summary>` from `ParseTextColumns` that references the old single-line limitation (D5/W3); update doc comment to reflect the new split-or-fallback contract.
- [x] 6.4 Verify `IngColumnThresholds.cs` has no public API leaking to Application or Domain layer (all types `internal`).
