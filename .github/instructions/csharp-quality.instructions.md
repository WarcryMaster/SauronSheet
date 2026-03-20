---
description: "Use when writing, reviewing, or refactoring C# code. Covers Microsoft code quality rules: naming, design, performance, reliability, usage, and maintainability. Applies to all C# source files."
applyTo: "**/*.cs"
---

# C# Code Quality Rules (Microsoft CA Rules)

Rules derived from Microsoft's .NET code analysis guidelines. Enforced for all C# files in SauronSheet.

---

## 🔒 Typing & Modern C# (Project-Specific)

- **Never use `var`** — always declare the type explicitly.
- **Prefer primary constructors** (C# 12+) for records and simple classes.
- **Use `new()` target-typed** only when the type is explicit in the declaration.
- **Use collection expressions `[]`** or `new[] { }` for arrays and lists.

```csharp
// ✅
List<string> items = new();
int[] ids = new[] { 1, 2, 3 };
string name = user.GetName();

// ❌
var items = new List<string>();
var name = user.GetName();
```

---

## 📐 Design Rules (CA1000–CA1069)

- **CA1000** — Do not declare static members on generic types; call via the concrete type.
- **CA1002** — Do not expose `List<T>` in public APIs; use `IReadOnlyList<T>` or `IEnumerable<T>`.
- **CA1003** — Use generic `EventHandler<T>` instead of custom delegate types for events.
- **CA1008** — Enums should have a zero value named `None` or `Unknown`.
- **CA1010** — Collections should implement `IEnumerable<T>` (generic interface).
- **CA1012** — Abstract types should not have public constructors; use `protected`.
- **CA1016** — Mark assemblies with `AssemblyVersionAttribute`.
- **CA1019** — Define accessors for attribute arguments (use properties, not just constructor args).
- **CA1021** — Avoid `out` parameters; prefer returning a tuple or result object.
- **CA1024** — Use properties for simple state access instead of `Get…()` methods with no side effects.
- **CA1027** — Mark flag enums with `[Flags]` attribute.
- **CA1028** — Enum storage should be `int` unless there's a specific reason otherwise.
- **CA1031** — Do not catch general `Exception` without re-throwing or logging the specific type.
- **CA1032** — Implement standard exception constructors (message, message+inner, serialization).
- **CA1033** — Interface methods should be callable by child types; avoid explicit-only implementations.
- **CA1036** — Override comparison operators (`<`, `>`, etc.) when implementing `IComparable`.
- **CA1040** — Avoid empty interfaces; use attributes instead.
- **CA1041** — Provide `ObsoleteAttribute.Message` when marking members `[Obsolete]`.
- **CA1043** — Use integral or string arguments for indexers.
- **CA1044** — Properties should not be write-only; add a getter or convert to a method.
- **CA1045** — Do not pass types by reference (`ref`) in public APIs.
- **CA1046** — Do not overload `==` on reference types unless they are value-semantic.
- **CA1047** — Do not declare `protected` members in sealed classes.
- **CA1050** — Declare types in namespaces; avoid global namespace types.
- **CA1051** — Do not declare visible instance fields; use properties instead.
- **CA1052** — Static holder types should be `static` classes.
- **CA1054** — URI parameters should not be strings; use `Uri` type.
- **CA1055** — URI return values should not be strings; return `Uri`.
- **CA1056** — URI properties should not be strings; use `Uri` type.
- **CA1058** — Types should not extend certain base types (`ApplicationException`, `XmlDocument`, etc.).
- **CA1060** — Move P/Invokes to a `NativeMethods` class.
- **CA1061** — Do not hide base class methods with non-covariant overloads.
- **CA1062** — Validate arguments of public methods are not null before use. Prefer nullable reference types.
- **CA1063** — Implement `IDisposable` correctly (virtual `Dispose(bool)` pattern).
- **CA1064** — Exceptions should be public.
- **CA1065** — Do not raise exceptions from unexpected places (property getters, `ToString`, `GetHashCode`, `Equals`).
- **CA1068** — `CancellationToken` parameters must come last in method signatures.
- **CA1069** — Enum values should not be duplicated.

---

## 🌐 Globalization Rules (CA1300–CA1311)

- **CA1303** — Do not pass literals as localized parameters; use resource strings.
- **CA1304** — Specify `CultureInfo` when calling `ToLower`, `ToUpper`, etc.
- **CA1305** — Specify `IFormatProvider` for `string.Format`, `ToString`, and parse methods.
- **CA1307** — Specify `StringComparison` for `string.Equals` and `string.Compare`.
- **CA1308** — Normalize to uppercase, not lowercase (`ToUpperInvariant` over `ToLowerInvariant`).
- **CA1309** — Use `StringComparison.Ordinal` or `OrdinalIgnoreCase` for non-linguistic comparisons.
- **CA1310** — Specify `StringComparison` for correctness in `string.StartsWith`, `string.EndsWith`.
- **CA1311** — Specify a culture or use `InvariantCulture` for `ToUpper`/`ToLower`.

---

## 🔤 Naming Rules (CA1700–CA1727)

- **CA1700** — Do not name enum values `Reserved`.
- **CA1707** — Identifiers should not contain underscores (except `_` prefix for private fields).
- **CA1708** — Identifiers should differ by more than case.
- **CA1710** — Identifiers should have correct suffix (`Collection`, `Dictionary`, `EventArgs`, `Exception`, `EventHandler`).
- **CA1711** — Identifiers should not have incorrect suffix (avoid `Enum`, `Flag`, `Flags`, `Impl`).
- **CA1712** — Do not prefix enum values with the type name.
- **CA1713** — Events should not have `Before`/`After` prefix; use present/past tense.
- **CA1714** — Flag enums should have plural names.
- **CA1715** — Prefixes: interfaces start with `I`, generic type parameters start with `T`.
- **CA1716** — Identifiers should not match keywords.
- **CA1717** — Only flag enums should have plural names.
- **CA1720** — Identifiers should not contain type names (`int`, `string`, `object`, etc.).
- **CA1721** — Property names should not match `Get` methods.
- **CA1724** — Type names should not match namespaces.
- **CA1725** — Parameter names should match base declaration.
- **CA1727** — Use `PascalCase` for named placeholders in log messages.

---

## ⚡ Performance Rules (CA1800–CA1900)

- **CA1802** — Use literals where appropriate instead of `static readonly` for compile-time constants.
- **CA1805** — Do not initialize fields to their default value (`= null`, `= 0`, `= false` are redundant).
- **CA1806** — Do not ignore method results; check return values (especially LINQ, `TryParse`, etc.).
- **CA1810** — Initialize static fields inline, not in a static constructor (avoids `beforefieldinit` penalty).
- **CA1812** — Avoid uninstantiated internal classes; remove dead code.
- **CA1813** — Avoid unsealed attributes; sealed attributes are faster to reflect.
- **CA1814** — Prefer jagged arrays over multidimensional arrays for sparse data.
- **CA1815** — Override `Equals` and `==` on value types.
- **CA1816** — Use `GC.SuppressFinalize` correctly (call only in `Dispose`, not conditionally).
- **CA1819** — Properties should not return arrays; return `IReadOnlyList<T>` or `ReadOnlySpan<T>`.
- **CA1820** — Test for empty strings using `string.Length == 0` or `string.IsNullOrEmpty`, not `== ""`.
- **CA1821** — Remove empty finalizers.
- **CA1822** — Mark methods that don't access instance state as `static`.
- **CA1823** — Avoid unused private fields.
- **CA1825** — Avoid zero-length array allocations; use `Array.Empty<T>()`.
- **CA1826** — Use property instead of `Enumerable` method (e.g., `.Count` over `.Count()` on `ICollection`).
- **CA1827** — Do not use `Count()`/`LongCount()` when `Any()` can be used.
- **CA1828** — Do not use `CountAsync()` when `AnyAsync()` can be used.
- **CA1829** — Use `Length`/`Count` property instead of `Enumerable.Count()`.
- **CA1830** — Prefer strongly-typed `StringBuilder.Append` overloads.
- **CA1831** — Use `AsSpan` instead of range-based indexers to avoid array copies.
- **CA1832** — Use `AsSpan`/`AsMemory` over range-based indexers for `ReadOnlySpan`/`ReadOnlyMemory`.
- **CA1833** — Use `AsSpan`/`AsMemory` over range-based indexers for `Span`/`Memory`.
- **CA1834** — Use `StringBuilder.Append(char)` for single-character strings.
- **CA1835** — Prefer `Memory`-based overloads for `ReadAsync`/`WriteAsync`.
- **CA1836** — Prefer `IsEmpty` over `Count == 0` for collections that expose it.
- **CA1837** — Use `Environment.ProcessId` instead of `Process.GetCurrentProcess().Id`.
- **CA1838** — Avoid `StringBuilder` parameters for P/Invokes; use `char[]` instead.
- **CA1845** — Use `string.AsSpan` rather than `string.Substring` when passing to `Span`-accepting APIs.
- **CA1846** — Prefer `AsSpan` over `Substring` when `Span`-based overloads are available.
- **CA1847** — Use `char` literal for single-character searches (`IndexOf('x')` not `IndexOf("x")`).
- **CA1848** — Use `LoggerMessage` delegates for high-performance logging.
- **CA1849** — Call async methods when in an async method (don't mix `.Result`/`.Wait()` with `await`).
- **CA1851** — Avoid multiple enumerations of `IEnumerable<T>`; materialize with `.ToList()` first.
- **CA1852** — Seal internal types that are not inherited; improves JIT devirtualization.
- **CA1853** — Unnecessary `Dictionary.ContainsKey` call before `Dictionary[key]`; use `TryGetValue`.
- **CA1854** — Prefer `Dictionary.TryGetValue` over `Dictionary.ContainsKey` + indexer.
- **CA1855** — Prefer `Clear()` + `AddRange()` over `new List<T>(collection)` for reuse scenarios.
- **CA1856** — Incorrect usage of `ConstantExpected` attribute.
- **CA1857** — Parameter expects a constant for optimal performance (e.g., spans, compiled regex).
- **CA1858** — Use `StartsWith` instead of `IndexOf` when checking prefix.
- **CA1859** — Use concrete types for better performance when possible (avoid unnecessary interface casting).
- **CA1860** — Avoid using `Enumerable.Any()` extension on arrays and lists; use `.Length > 0` / `.Count > 0`.
- **CA1861** — Avoid constant arrays as arguments; use `static readonly` fields.
- **CA1862** — Use `StringComparison.OrdinalIgnoreCase` overloads instead of `ToLower()`/`ToUpper()` comparisons.
- **CA1863** — Use `CompositeFormat` for frequently formatted strings.
- **CA1864** — Prefer `Dictionary.GetOrAdd` over conditional `ContainsKey`/`Add`.
- **CA1865–CA1867** — Use `string.IndexOf(char)` overloads when searching for a single character.

---

## 🔒 Reliability Rules (CA2000–CA2019)

- **CA2000** — Dispose objects before losing scope; use `using` statements or `using` declarations.
- **CA2002** — Do not lock on objects with weak identity (`string`, `Type`, boxed value types).
- **CA2007** — Consider using `ConfigureAwait(false)` on awaited tasks in library code.
- **CA2008** — Do not create tasks without passing a `TaskScheduler` (use `Task.Run` correctly).
- **CA2009** — Do not call `ToImmutableCollection` on an already-immutable collection.
- **CA2011** — Do not assign property within its own setter (avoid stack overflow).
- **CA2012** — Use `ValueTask` correctly; do not await more than once or use after completion.
- **CA2013** — Do not use `ReferenceEquals` with value types; it always returns false.
- **CA2014** — Do not use `stackalloc` in loops; it accumulates stack space.
- **CA2015** — Do not define finalizers for types derived from `MemoryManager<T>`.
- **CA2016** — Forward `CancellationToken` parameter to methods that take one.
- **CA2017** — Log format string parameter count mismatch.
- **CA2018** — `Buffer.BlockCopy` count argument should specify bytes, not elements.
- **CA2019** — `ThreadStatic` fields should not use instance-level initialization.

---

## 🛡️ Security Rules (CA2100–CA3147)

- **CA2100** — Review SQL queries for security vulnerabilities; never concatenate user input into SQL.
- **CA2101** — Specify marshaling for P/Invoke string arguments.
- **CA2109** — Review visible event handlers; do not make security-sensitive event handlers public.
- **CA2119** — Seal methods that satisfy private interface contracts.
- **CA2153** — Avoid catching `CorruptedStateException` (access violations, stack overflows).
- **CA2300–CA2302** — Do not use insecure deserializers (`BinaryFormatter`, `SoapFormatter`).
- **CA2326–CA2327** — Use safe `TypeNameHandling` settings with `JsonSerializer`.
- **CA3001–CA3012** — Review code for injection vulnerabilities (SQL, XSS, path traversal, LDAP, XML, command, XPath, regex).
- **CA5350–CA5360** — Do not use weak cryptographic algorithms (`DES`, `RC2`, `MD5`, `SHA1` for security).
- **CA5363** — Do not disable request validation.
- **CA5364** — Do not use deprecated security protocols (`SSL3`, `TLS 1.0/1.1`).
- **CA5369** — Use `XmlReader` for `XmlSerializer.Deserialize`.
- **CA5370** — Use `XmlReader` for `XmlTextReader`.
- **CA5371** — Use `XmlReader` for `XmlSchema.Read`.
- **CA5379** — Do not use weak key derivation algorithm (use `PBKDF2`, `Argon2`).
- **CA5394** — Do not use insecure randomness (`System.Random`) for security purposes; use `RandomNumberGenerator`.
- **CA5397** — Do not use deprecated `SslProtocols` values.

---

## ✅ Usage Rules (CA2200–CA2259)

- **CA2200** — Rethrow exceptions to preserve stack details using `throw;` not `throw ex;`.
- **CA2201** — Do not raise reserved exception types (`NullReferenceException`, `IndexOutOfRangeException`, etc.).
- **CA2207** — Initialize value type static fields inline.
- **CA2208** — Instantiate argument exceptions correctly (pass parameter name to constructor).
- **CA2211** — Non-constant fields should not be visible as `public static`.
- **CA2213** — Disposable fields should be disposed in `Dispose()`.
- **CA2214** — Do not call overridable methods in constructors.
- **CA2215** — Dispose methods should call base class `Dispose`.
- **CA2216** — Disposable types should declare finalizer (if holding unmanaged resources).
- **CA2217** — Do not mark enums with `[Flags]` unless all combinations are valid.
- **CA2218** — Override `GetHashCode` when overriding `Equals`.
- **CA2219** — Do not raise exceptions in exception clauses (`finally`, `catch`, `fault`).
- **CA2224** — Override `Equals` when overloading `operator ==`.
- **CA2225** — Operator overloads should have named alternates (`Add`, `Subtract`, etc.).
- **CA2226** — Operators should have symmetrical overloads (if `==` then `!=`).
- **CA2227** — Collection properties should be read-only; setter causes unnecessary exposure.
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
