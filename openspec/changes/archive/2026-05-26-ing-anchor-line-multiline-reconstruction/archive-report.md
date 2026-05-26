# Archive Report: ING Anchor-Line Multiline Reconstruction

**Status**: ✅ ARCHIVED  
**Archive Date**: 2026-05-26 (ISO format)  
**Archive Location**: `openspec/changes/archive/2026-05-26-ing-anchor-line-multiline-reconstruction/`  
**Mode**: Hybrid (Engram + OpenSpec filesystem)

---

## SDD Cycle Summary

| Phase | Status | Date | Artifacts |
|-------|--------|------|-----------|
| Proposal | ✅ Complete | 2026-05-26 07:02:25 | Engram #1100, `openspec/.../proposal.md` |
| Spec | ✅ Complete | 2026-05-26 07:04:53 | Engram #1101, `openspec/.../specs/ing-block-reconstruction/spec.md` |
| Design | ✅ Complete | 2026-05-26 07:07:08 | Engram #1102, `openspec/.../design.md` |
| Tasks | ✅ Complete | 2026-05-26 07:09:12 | Engram #1103, `openspec/.../tasks.md` |
| Apply | ✅ Complete | 2026-05-26 07:26:53 | Engram #1104, `openspec/.../tasks.md` (all tasks marked [x]) |
| Verify | ✅ PASS WITH WARNINGS | 2026-05-26 07:31:33 | Engram #1106 |
| Archive | ✅ Complete | 2026-05-26 | This report |

---

## Specs Synced

| Domain | Action | Details |
|--------|--------|---------|
| `ing-block-reconstruction` | Modified | IBR-1 requirement updated with strong-anchor rule (3 new scenarios: IBR-1d, IBR-1e, IBR-1f) |

### IBR-1 Changes Applied
- **Previous behavior**: All non-date lines unconditionally attached to previous block
- **New behavior**: Introduces state-aware assembly with ambiguous buffer
  - **Incomplete block** (no monetary pair on date line): non-date lines attach backward (preserves repeated-page-header)
  - **Complete block** (strong anchor detected): non-date lines buffer, prepend to next anchor or re-append at EOF
- **Strong anchor definition**: Line with valid date + monetary pair extractable on same line

### Scenarios Added
- **IBR-1d**: Payroll — anchor in middle (multiline reconstruction happy path)
- **IBR-1e**: Ambiguous buffer forward reassignment (fragment moves to next block)
- **IBR-1f**: Regression guard — backward behavior preserved for incomplete blocks

---

## Archive Contents

```
openspec/changes/archive/2026-05-26-ing-anchor-line-multiline-reconstruction/
├── proposal.md                           ✅ (Engram #1100)
├── exploration.md                        ✅
├── design.md                             ✅ (Engram #1102)
├── tasks.md                              ✅ (Engram #1103) [all 22 tasks marked [x]]
├── verify-report.md                      ✅ (Engram #1106, PASS WITH WARNINGS)
├── specs/
│   └── ing-block-reconstruction/
│       └── spec.md                       ✅ Delta spec (Engram #1101)
└── archive-report.md                     ✅ (This file)
```

### Verification Results
- **Tests**: 506/506 passing (0 failures, 0 errors)
- **Build**: 0 warnings, 0 errors
- **Coverage**: Infrastructure.Tests: 132/132; solution-wide: 506/506
- **Verdict**: PASS WITH WARNINGS

#### Warnings (Non-Blocking)
- W-1 (PROCESS): Uncommitted changes in working tree (5 source/test files, untracked openspec/)
- W-2 (COVERAGE): coverlet.collector not installed (cannot measure % coverage; tool limitation only)
- W-3 (DESIGN DEVIATION, documented): `IngBankPdfParser.cs` modified to add `ExtractTaxonomyInput` helper (needed to handle prepended buffer lines correctly)

#### Spec Compliance
All scenarios IBR-1a, 1b, 1c, 1d, 1e, 1f + IBR-2b + IBR-4a/b covered with passing tests.

---

## Source of Truth Updated

### Main Spec
The following spec now reflects the new strong-anchor behavior:
- **Path**: `openspec/specs/ing-block-reconstruction/spec.md`
- **Updated requirement**: IBR-1 (lines 15–61)
- **New scenarios**: IBR-1d (lines 41–47), IBR-1e (lines 49–54), IBR-1f (lines 56–61)
- **Merged by**: sdd-archive (hybrid mode)
- **Date**: 2026-05-26

### Implementation Delivered
**Files Changed** (22/22 tests passing, all tasks complete):
| File | Change |
|------|--------|
| `src/.../PDF/Parsers/IngBlockAssembler.cs` | Added state-aware assembly with `isComplete`, `ambiguousBuffer`, `IsStrongAnchor()` |
| `src/.../PDF/Parsers/IngBankPdfParser.cs` | Added `ExtractTaxonomyInput` helper (deviation from design, necessary for multiline) |
| `tests/.../PDF/Parsers/IngBlockAssemblerTests.cs` | Added 4 new unit tests (IBR-1d, 1e, EOF, 1f regression) |
| `tests/.../PDF/Parsers/IngBankPdfParserBlockTests.cs` | Added 1 integration test (nómina with anchor) |

---

## SDD Cycle Complete

✅ **All phases delivered and verified**:
1. ✅ Proposal — Clear scope, approach, and risk mitigation
2. ✅ Spec — Delta specs with all new scenarios
3. ✅ Design — Technical approach, data flow, testing strategy
4. ✅ Tasks — 22 tasks broken down by RED-GREEN-REFACTOR
5. ✅ Apply — All tasks completed; 22/22 tests passing, 506/506 solution-wide
6. ✅ Verify — PASS WITH WARNINGS (warnings are process/tooling, not blocking)
7. ✅ Archive — Change moved to audit trail, specs synced, traceability recorded

**Next steps**: Ready for the next SDD change.

---

## Engram Observation IDs (Audit Trail)

| Artifact | Engram ID | Created |
|----------|-----------|---------|
| Proposal | #1100 | 2026-05-26 07:02:25 |
| Spec Delta | #1101 | 2026-05-26 07:04:53 |
| Design | #1102 | 2026-05-26 07:07:08 |
| Tasks | #1103 | 2026-05-26 07:09:12 |
| Apply Progress | #1104 | 2026-05-26 07:26:53 |
| Verify Report | #1106 | 2026-05-26 07:31:33 |

**Archive Report stored as**: `sdd/ing-anchor-line-multiline-reconstruction/archive-report` (Engram #XXXX, persistent)
