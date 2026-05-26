namespace SauronSheet.Infrastructure.Tests.PDF.Parsers;

using SauronSheet.Infrastructure.PDF.Parsers;
using Xunit;

/// <summary>
/// Unit tests for <see cref="IngBlockAssembler"/>.
///
/// Covers:
///   IBR-1a — Single-line row: produces exactly one block with the full text.
///   IBR-1b — Multi-line row (continuation without date): both lines become one block.
///   IBR-1c — Two adjacent rows each starting with a date: two independent blocks.
///   IBR-1d — Anchor in middle: description-before-anchor goes to buffer, prepended to new block.
///   IBR-1e — Ambiguous buffer forwarded to next strong anchor.
///   IBR-1f — Incomplete block: backward behavior preserved (regression).
///   EOF    — Non-empty buffer at EOF re-appended to current block.
///
/// Strategy: pure unit tests — no PDF I/O, no mocks.
/// Input is manually constructed <see cref="IngLineData"/> lists;
/// output is the assembled <see cref="IngBlock"/> list.
/// </summary>
[Trait("Category", "Infrastructure")]
public class IngBlockAssemblerTests
{
    // ────────────────────────────────────────────────────────────────────────
    // Shared helpers
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds a minimal <see cref="IngLineData"/> for a text line.
    /// </summary>
    private static IngLineData Line(string text, params PositionedWord[] words)
        => new(text, words);

    // ════════════════════════════════════════════════════════════════════════
    // IBR-1a: Single-line row → exactly one block
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Assemble_SingleDateLine_ReturnsOneBlock()
    {
        // Arrange — IBR-1a fixture: real Jan-2025 DAZN row (single physical line)
        IReadOnlyList<IngLineData> lines =
        [
            Line("15/01/2025 Compras Online DAZN -12,99 1.234,56"),
        ];

        // Act
        IReadOnlyList<IngBlock> blocks = IngBlockAssembler.Assemble(lines);

        // Assert — exactly one block; date captured; full text preserved
        Assert.Single(blocks);
        Assert.Equal("15/01/2025", blocks[0].Date);
        Assert.Contains("DAZN", blocks[0].FullText, StringComparison.Ordinal);
    }

    // ════════════════════════════════════════════════════════════════════════
    // IBR-1b: Continuation line without date → appended to previous block
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Assemble_ContinuationLineWithoutDate_JoinsIntoPreviousBlock()
    {
        // Arrange — IBR-1b: line 0 starts with date; line 1 has no date
        IReadOnlyList<IngLineData> lines =
        [
            Line("15/01/2025 Compras Online DAZN"),
            Line("-12,99 1.234,56"),               // continuation — no date prefix
        ];

        // Act
        IReadOnlyList<IngBlock> blocks = IngBlockAssembler.Assemble(lines);

        // Assert — both lines collapsed into a single logical block
        Assert.Single(blocks);
        Assert.Equal("15/01/2025", blocks[0].Date);
        Assert.Contains("-12,99", blocks[0].FullText, StringComparison.Ordinal);
        Assert.Contains("1.234,56", blocks[0].FullText, StringComparison.Ordinal);
    }

    // ════════════════════════════════════════════════════════════════════════
    // IBR-1c: Two adjacent date-lines → two independent blocks
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Assemble_TwoAdjacentDateLines_ReturnsTwoIndependentBlocks()
    {
        // Arrange — IBR-1c: two rows each starting with a different date
        IReadOnlyList<IngLineData> lines =
        [
            Line("15/01/2025 Compras Online DAZN -12,99 1.234,56"),
            Line("16/01/2025 Ocio Parking Auditorio -3,00 1.231,56"),
        ];

        // Act
        IReadOnlyList<IngBlock> blocks = IngBlockAssembler.Assemble(lines);

        // Assert — exactly two blocks; each has its own date
        Assert.Equal(2, blocks.Count);
        Assert.Equal("15/01/2025", blocks[0].Date);
        Assert.Equal("16/01/2025", blocks[1].Date);
        Assert.Contains("DAZN", blocks[0].FullText, StringComparison.Ordinal);
        Assert.Contains("Parking", blocks[1].FullText, StringComparison.Ordinal);
    }

    [Fact]
    public void Assemble_DateLikeContinuationOutsideFirstColumn_DoesNotOpenNewBlock()
    {
        // Arrange — line 1 starts with a date-like token in description X space,
        // so it is a continuation, not a new transaction row.
        IReadOnlyList<IngLineData> lines =
        [
            Line(
                "15/01/2025 Compras Online DAZN",
                new PositionedWord("15/01/2025", 10.0),
                new PositionedWord("Compras", 178.0),
                new PositionedWord("Online", 278.0),
                new PositionedWord("DAZN", 378.0)),
            Line(
                "16/01/2025 referencia interna",
                new PositionedWord("16/01/2025", 378.0),
                new PositionedWord("referencia", 430.0),
                new PositionedWord("interna", 505.0)),
            Line(
                "17/01/2025 Ocio Parking Auditorio -3,00 1.231,56",
                new PositionedWord("17/01/2025", 10.0),
                new PositionedWord("Ocio", 178.0),
                new PositionedWord("Parking", 278.0),
                new PositionedWord("Auditorio", 378.0),
                new PositionedWord("-3,00", 465.0),
                new PositionedWord("1.231,56", 520.0)),
        ];

        // Act
        IReadOnlyList<IngBlock> blocks = IngBlockAssembler.Assemble(lines);

        // Assert — the middle line stays attached to the first block.
        Assert.Equal(2, blocks.Count);
        Assert.Equal("15/01/2025", blocks[0].Date);
        Assert.Contains("16/01/2025 referencia interna", blocks[0].FullText, StringComparison.Ordinal);
        Assert.Equal("17/01/2025", blocks[1].Date);
    }

    // ════════════════════════════════════════════════════════════════════════
    // Triangulation: empty input → empty result (edge case / boundary)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Assemble_EmptyInput_ReturnsEmptyList()
    {
        // Arrange — no lines at all (e.g., blank page or pre-header section)
        IReadOnlyList<IngLineData> lines = [];

        // Act
        IReadOnlyList<IngBlock> blocks = IngBlockAssembler.Assemble(lines);

        // Assert — empty result; no exception
        Assert.Empty(blocks);
    }

    // ════════════════════════════════════════════════════════════════════════
    // Triangulation: multi-continuation block — several continuation lines
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Assemble_MultiContinuationLines_AllJoinedToSingleBlock()
    {
        // Arrange — one date-line followed by two continuation lines
        IReadOnlyList<IngLineData> lines =
        [
            Line("15/01/2025 Nómina"),
            Line("Empresa S.A."),       // continuation 1
            Line("3.200,00 4.500,00"),  // continuation 2
        ];

        // Act
        IReadOnlyList<IngBlock> blocks = IngBlockAssembler.Assemble(lines);

        // Assert — all three physical lines → one logical block
        Assert.Single(blocks);
        Assert.Equal("15/01/2025", blocks[0].Date);
        Assert.Contains("Nómina", blocks[0].FullText, StringComparison.Ordinal);
        Assert.Contains("Empresa S.A.", blocks[0].FullText, StringComparison.Ordinal);
        Assert.Contains("3.200,00", blocks[0].FullText, StringComparison.Ordinal);
    }

    // ════════════════════════════════════════════════════════════════════════
    // IBR-1d: Anchor in middle — description-before-anchor prepended via buffer
    // RED: fails until IngBlockAssembler gains isComplete + ambiguousBuffer logic
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Assemble_AnchorInMiddle_PrependsToPrecedingDescription_IBR1d()
    {
        // Arrange — IBR-1d:
        //   Line 0: previous complete transaction (strong anchor — has monetary pair)
        //   Line 1: description-only (no date) → after complete block → ambiguousBuffer
        //   Line 2: anchor line (date + monetary pair) → strong anchor → flush prev, prepend buffer
        //   Line 3: description-only (no date) → after complete block → ambiguousBuffer (EOF re-append)
        IReadOnlyList<IngLineData> lines =
        [
            Line("15/01/2025 Compras Online DAZN -12,99 1.234,56"),
            Line("NÓMINA EMPRESA S.L."),
            Line("31/01/2025 Nominas 2.500,00 3.200,00"),
            Line("ENERO 2025"),
        ];

        // Act
        IReadOnlyList<IngBlock> blocks = IngBlockAssembler.Assemble(lines);

        // Assert — 2 blocks; previous DAZN block uncontaminated; nómina block has all 3 nómina lines
        Assert.Equal(2, blocks.Count);

        // Block 0 (DAZN): must NOT contain the nómina description or the after-anchor line
        Assert.Equal("15/01/2025", blocks[0].Date);
        Assert.DoesNotContain("NÓMINA", blocks[0].FullText, StringComparison.Ordinal);
        Assert.DoesNotContain("ENERO 2025", blocks[0].FullText, StringComparison.Ordinal);

        // Block 1 (nómina): must contain the pre-anchor description AND the after-anchor description
        Assert.Equal("31/01/2025", blocks[1].Date);
        Assert.Contains("NÓMINA EMPRESA S.L.", blocks[1].FullText, StringComparison.Ordinal);
        Assert.Contains("ENERO 2025", blocks[1].FullText, StringComparison.Ordinal);
    }

    // ════════════════════════════════════════════════════════════════════════
    // IBR-1e: Ambiguous buffer reassigned forward to the next strong anchor
    // RED: fails until IngBlockAssembler gains isComplete + ambiguousBuffer logic
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Assemble_AmbiguousBufferForwardToNextAnchor_IBR1e()
    {
        // Arrange — IBR-1e:
        //   Block A: complete strong anchor (DAZN)
        //   FRAGMENTO: no date → ambiguousBuffer (block A is complete)
        //   Block B: new strong anchor → FRAGMENTO prepended to block B, not block A
        IReadOnlyList<IngLineData> lines =
        [
            Line("10/01/2025 Compras Online DAZN -12,99 1.234,56"),   // block A (complete)
            Line("FRAGMENTO"),                                           // ambiguousBuffer
            Line("20/01/2025 Nómina Empresa SA 3.200,00 4.500,00"),   // block B (strong anchor)
        ];

        // Act
        IReadOnlyList<IngBlock> blocks = IngBlockAssembler.Assemble(lines);

        // Assert — 2 blocks; FRAGMENTO must be in block B, not block A
        Assert.Equal(2, blocks.Count);
        Assert.DoesNotContain("FRAGMENTO", blocks[0].FullText, StringComparison.Ordinal);
        Assert.Contains("FRAGMENTO", blocks[1].FullText, StringComparison.Ordinal);
        Assert.Equal("20/01/2025", blocks[1].Date);
    }

    // ════════════════════════════════════════════════════════════════════════
    // EOF buffer: non-empty ambiguous buffer at EOF re-appended to current block
    // RED: fails until IngBlockAssembler gains EOF buffer flush logic
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Assemble_EofBufferNonEmpty_ReappendsToCurrentBlock()
    {
        // Arrange — complete block followed by a non-date line (no new anchor arrives)
        //   The ambiguous buffer must be flushed into the same block at EOF.
        IReadOnlyList<IngLineData> lines =
        [
            Line("10/01/2025 Compras Online DAZN -12,99 1.234,56"),   // complete (isComplete=true)
            Line("Texto adicional EOF"),                                 // ambiguousBuffer → EOF flush
        ];

        // Act
        IReadOnlyList<IngBlock> blocks = IngBlockAssembler.Assemble(lines);

        // Assert — single block containing both lines
        Assert.Single(blocks);
        Assert.Equal("10/01/2025", blocks[0].Date);
        Assert.Contains("Texto adicional EOF", blocks[0].FullText, StringComparison.Ordinal);
        Assert.Contains("DAZN", blocks[0].FullText, StringComparison.Ordinal);
    }

    // ════════════════════════════════════════════════════════════════════════
    // IBR-1f: Incomplete block — backward behavior preserved (regression)
    // GREEN even before the fix (existing backward behavior); kept to prevent regression.
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Assemble_IncompleteBlock_BackwardBehaviorPreserved_IBR1f()
    {
        // Arrange — IBR-1f: block opened with a date-line that has NO monetary pair
        //   → isComplete=false → continuation lines must go backward (not to buffer)
        IReadOnlyList<IngLineData> lines =
        [
            Line("15/01/2025 Nómina Empresa SA"),   // incomplete (no monetary pair on this line)
            Line("Continuacion backward"),           // no date → must join the block above
        ];

        // Act
        IReadOnlyList<IngBlock> blocks = IngBlockAssembler.Assemble(lines);

        // Assert — single block; continuation was joined backward
        Assert.Single(blocks);
        Assert.Equal("15/01/2025", blocks[0].Date);
        Assert.Contains("Continuacion backward", blocks[0].FullText, StringComparison.Ordinal);
    }
}
