# ADR 00001: PDF Parser Amount Normalization — Dual-Format Support

## Date
2026-03-13

## Status
Accepted

## Context
Bank PDFs from Spanish institutions (e.g., ING) use mixed decimal/thousands separators:
- European format: `1.246,74` (point=thousands, comma=decimal)
- Anglo format: `1,246.74` (comma=thousands, point=decimal)

Parsers must handle both formats transparently without raising errors during amount conversion
to `decimal` type.

## Decision
Implement static `NormalizeAmount(string?)` method in each PDF parser (IngBankPdfParser,
GenericBankPdfParser) that automatically detects and normalizes all numeric inputs to standard
format: **decimal point, no thousands separator**.

### Algorithm
1. If both separators present → rightmost is decimal separator
2. If only comma → European decimal separator (replace with point)
3. If only point → already normalized (return as-is)
4. If no separator → numeric value (return as-is)

### Example Transformations
| Input | Output | Logic |
|-------|--------|-------|
| `1,246.74` | `1246.74` | comma=thousands (left) |
| `1.246,74` | `1246.74` | comma=decimal (right) |
| `0,82` | `0.82` | single comma=decimal |
| `0.82` | `0.82` | already normalized |
| `-1.246,74` | `-1246.74` | negative European |

## Consequences
- ✅ Supports all bank PDF formats without code changes
- ✅ Thread-safe (static methods, immutable input)
- ✅ 22 unit tests validate all combinations
- ✅ No breaking changes to existing code
- ⚠️ Assumes rightmost separator is always decimal in mixed-format strings (acceptable for banking context)

## Related
- Constitution: PDF Parser Amount Normalization rule
- Implementation: Infrastructure/PDF/Parsers/{IngBankPdfParser.cs, GenericBankPdfParser.cs}
- Tests: SauronSheet.Infrastructure.Tests/PDF/Parsers/AmountNormalizationTests.cs
