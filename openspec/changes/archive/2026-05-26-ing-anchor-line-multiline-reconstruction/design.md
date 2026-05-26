# Design: ING Anchor-Line Multiline Reconstruction

## Technical Approach

Introduce a **state-aware assembler** in `IngBlockAssembler.Assemble()` that distinguishes *complete* blocks (strong anchor: date + monetary pair on the same line) from *incomplete* blocks. Non-date lines after a complete block accumulate in an **ambiguous buffer** that is reassigned forward when a new anchor arrives, or re-appended to the current block at EOF. The existing `IngMonetaryExtractor.ExtractRightToLeft()` serves as the completeness probe — no new dependencies.

## Architecture Decisions

| Decision | Alternatives considered | Rationale |
|----------|------------------------|-----------|
| Completeness via `ExtractRightToLeft` on anchor line text | New dedicated regex; X-position heuristic | Already available, tested, and encapsulates the exact monetary isolation logic. Avoids coupling to geometry. |
| Buffer lives inside `Assemble()` as local state | Separate `AmbiguousBuffer` class; post-processing pass | Minimal allocation, single-pass, no public surface change. Keeps the method pure and static. |
| `Assemble` signature unchanged (`IReadOnlyList<IngLineData> → IReadOnlyList<IngBlock>`) | Add `AssembleAnchorAware` overload | Backward-compatible; `ProcessBlocks` callers need zero change. Swap is atomic. |
| Prepend buffer to new block (not append to previous) | Append to previous on flush | Matches spec IBR-1d: description fragments before the anchor belong to the anchor's transaction. |

## Data Flow

```
foreach line:
  ┌─ TryGetBlockStartDate? ──YES──┐
  │                                ▼
  │                    IsStrongAnchor(line)?
  │                     │            │
  │                    YES          NO
  │                     │            │
  │   ┌─────────────────┘            └──────────────┐
  │   │ FlushBlock(current)                          │ FlushBlock(current)
  │   │ PrependBuffer → new block prefix             │ ReappendBuffer → current
  │   │ Start new block (complete)                   │ Start new block (incomplete)
  │   └──────────────────────────────────────────────┘
  │
  └─ NO ──┐
           │
           ▼
    currentBlock is COMPLETE?
     │               │
    YES             NO
     │               │
     ▼               ▼
  ambiguousBuffer   currentLines.Add(line)
    .Add(line)
```

At EOF: if buffer is non-empty → re-append to current block, then flush.

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `src/.../PDF/Parsers/IngBlockAssembler.cs` | Modify | Add `isComplete` flag, `ambiguousBuffer` list, `IsStrongAnchor()` private helper, adjust loop + `FlushBlock`. |
| `tests/.../PDF/Parsers/IngBlockAssemblerTests.cs` | Modify | Add tests for IBR-1d (anchor in middle), IBR-1e (buffer forward), IBR-1f (incomplete → backward regression). |
| `tests/.../PDF/Parsers/IngBankPdfParserBlockTests.cs` | Modify | Integration regression: repeated-page-header still works after assembler change. |

No changes to `IngBankPdfParser.cs`, `IngMonetaryExtractor.cs`, `IngBlock.cs`, or `IngColumnThresholds.cs`.

## Interfaces / Contracts

```csharp
// New private helper inside IngBlockAssembler (no public API change)
private static bool IsStrongAnchor(IngLineData lineData)
{
    // Strong anchor = date detected + monetary pair isolatable on that line alone
    if (!TryGetBlockStartDate(lineData, out _))
        return false;

    string lineText = lineData.Text.Trim();
    return IngMonetaryExtractor.ExtractRightToLeft(lineText) is not null;
}
```

The `IngBlock` record struct and `Assemble()` public signature remain unchanged.

## Testing Strategy

| Layer | What to test | Approach |
|-------|-------------|----------|
| Unit | IBR-1d: anchor in middle produces correct single block with prepended description | `IngBlockAssemblerTests` — manual `IngLineData` lists |
| Unit | IBR-1e: buffer reassigned forward on new anchor | Same test class, two-block scenario |
| Unit | IBR-1f: incomplete block → backward behavior preserved | Same class, regression fixture |
| Unit | EOF with non-empty buffer → re-appended | Edge case fixture |
| Integration | `ProcessBlocks` with nómina-style input produces correct `RawTransactionRow` | `IngBankPdfParserBlockTests` via reflection |
| Regression | Repeated-page-header test stays green | Existing test + explicit assertion in new test |

## Migration / Rollout

No migration required. Pure logic change inside a static internal class. `git revert` of the PR is the rollback.

## Open Questions

None — all design decisions are unblocked by the proposal and spec.
