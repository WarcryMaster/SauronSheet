---
description: "Use when writing, reviewing, or refactoring C# code. Covers Microsoft code quality rules: naming, design, performance, reliability, usage, and maintainability. Applies to all C# source files."
applyTo: "**/*.cs"
---

# C# Baseline Rules (Always On)

This file stays lightweight and is auto-loaded for every C# file.

## Typing and Style Baseline

- Always use explicit type declarations. Do not use var.
- Prefer primary constructors where possible.
- Use target-typed new only when the left-hand type is explicit.
- Use [] or new[] for list/array initialization when appropriate.

## Core Safety Baseline

- Validate external inputs and nulls.
- Keep CancellationToken as the last parameter.
- Forward CancellationToken to downstream APIs.
- Dispose disposable resources deterministically.
- Use throw; when rethrowing exceptions.
- Keep logging/tracing via Sentry APIs.

## Full Rule Catalog (Split by Context)

For the complete Microsoft rule set requested in this repository, consult:

- .github/instructions/csharp-rules-design-and-naming.instructions.md
- .github/instructions/csharp-rules-performance-and-maintainability.instructions.md
- .github/instructions/csharp-rules-reliability-and-usage.instructions.md
- .github/instructions/csharp-rules-security-platform-and-il.instructions.md
- **CA2229** — Implement serialization constructors for `ISerializable` types.
- **CA2231** — Overload `operator ==` when overriding `Equals` on value types.
- **CA2234** — Pass `System.Uri` objects instead of strings where overloads exist.
- **CA2235** — Mark all non-serializable fields in serializable types with `[NonSerialized]`.
- **CA2237** — Mark `ISerializable` types with `[Serializable]`.
- **CA2241** — Provide correct format arguments to `string.Format` (match argument count).
- **CA2242** — Test for `NaN` with `float.IsNaN()` not `== float.NaN` (always false).
- **CA2243** — Attribute string literals should parse correctly (valid URLs, GUIDs, etc.).
- **CA2244** — Do not duplicate indexed element initializations.
- **CA2245** — Do not self-assign a property.
- **CA2246** — Assigning a symbol and its member in the same statement is error-prone; split it.
- **CA2248** — Provide correct `enum` argument to `Enum.HasFlag`.
- **CA2249** — Consider using `string.Contains` instead of `string.IndexOf >= 0`.
- **CA2250** — Use `ThrowIfNull` instead of `if (x == null) throw new ArgumentNullException`.
- **CA2251** — Use `string.Equals` instead of `==` when semantic equality is required.
- **CA2253** — Named placeholders should not be numeric values in log messages.
- **CA2254** — Template should be a static expression, not dynamically constructed.
- **CA2255** — The `ModuleInitializer` attribute should not be used in libraries.
- **CA2256** — All members defined in parent interfaces must have an implementation in `DynamicInterfaceCastableImplementation`-attributed interface member.
- **CA2257** — Members defined on interfaces with `DynamicInterfaceCastableImplementation` should be `static`.
- **CA2258** — Providing `DynamicInterfaceCastableImplementation` in Visual Basic is unsupported.
- **CA2259** — `ThreadStatic` only affects static fields; has no effect on instance fields.

---

## 🔧 Maintainability Rules (CA1500–CA1513)

- **CA1501** — Avoid excessive inheritance (max 5 levels recommended).
- **CA1502** — Avoid excessive complexity (cyclomatic complexity > 25 is a warning).
- **CA1505** — Avoid unmaintainable code (maintainability index < 10).
- **CA1506** — Avoid excessive class coupling (> 95 unique types referenced).
- **CA1507** — Use `nameof` instead of string literals when referencing member names.
- **CA1508** — Avoid dead conditional code (conditions always true/false).
- **CA1509** — Invalid entry in code metrics configuration file.
- **CA1510** — Use `ArgumentNullException.ThrowIfNull` instead of `if/throw` pattern.
- **CA1511** — Use `ArgumentException.ThrowIfNullOrEmpty` instead of `if/throw` pattern.
- **CA1512** — Use `ObjectDisposedException.ThrowIf` instead of `if/throw` pattern.
- **CA1513** — Use `ArgumentOutOfRangeException.ThrowIfOutOfRange` instead of `if/throw` pattern.

```csharp
// ✅ Modern throw helpers (CA1510–CA1513)
ArgumentNullException.ThrowIfNull(userId);
ArgumentException.ThrowIfNullOrEmpty(description);
ObjectDisposedException.ThrowIf(_disposed, this);
ArgumentOutOfRangeException.ThrowIfNegative(amount);

// ❌ Old pattern
if (userId == null) throw new ArgumentNullException(nameof(userId));
if (string.IsNullOrEmpty(description)) throw new ArgumentException("...", nameof(description));
```

---

## 📦 Single File Rules (CA2260)

- **CA2260** — Use correct type parameter when implementing generic interfaces.

---

## Summary: Top Rules for This Project

Given SauronSheet's Clean Architecture + DDD + async patterns, prioritize:

1. **CA1068** — `CancellationToken` always last
2. **CA2016** — Forward `CancellationToken`
3. **CA2007** — `ConfigureAwait(false)` in infrastructure/library code
4. **CA1851** — Don't enumerate `IEnumerable<T>` multiple times
5. **CA1510–CA1513** — Use modern throw helpers
6. **CA2213** — Dispose injected disposables
7. **CA1822** — Mark stateless methods `static`
8. **CA1825** — Use `Array.Empty<T>()`
9. **CA1854** — Use `TryGetValue` over `ContainsKey` + indexer
10. **CA2100** — Never concatenate user input into SQL
