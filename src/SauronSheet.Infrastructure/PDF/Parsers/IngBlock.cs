namespace SauronSheet.Infrastructure.PDF.Parsers;

/// <summary>
/// Represents a logical transaction block assembled from one or more physical
/// PDF lines belonging to a single ING Bank statement row.
///
/// A block starts at the physical line that carries the value date
/// (<c>dd/mm/yyyy</c> prefix) and accumulates all subsequent physical lines
/// that have no date prefix (continuations).
///
/// <see cref="FullText"/> is the whitespace-joined text of all lines and is
/// the input surface for downstream extractors (<see cref="IngMonetaryExtractor"/>).
/// <see cref="Lines"/> preserves the original <see cref="IngLineData"/> list for
/// cases where X-position geometry is still needed (e.g. geometric fallback).
/// </summary>
internal readonly record struct IngBlock(
    /// <summary>Value date extracted from the first line, format <c>dd/mm/yyyy</c>.</summary>
    string Date,

    /// <summary>
    /// Whitespace-joined text of all physical lines in this logical block,
    /// including the date prefix of the first line.
    /// </summary>
    string FullText,

    /// <summary>
    /// Original positioned-word lines that form this block (ordered, first line first).
    /// </summary>
    IngLineData[] Lines);
