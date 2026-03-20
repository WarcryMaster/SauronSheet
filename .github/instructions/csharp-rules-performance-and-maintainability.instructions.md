---
description: "Use when optimizing code, reviewing allocations, async hot paths, collections, and maintainability metrics in C#. Includes Microsoft CA performance and maintainability rules."
---

# C# Rules: Performance and Maintainability

Apply these rules for hot paths, memory/allocations, complexity reduction, and maintainability.

## Maintainability Rules

Mandatory IDs:
- CA1501, CA1502, CA1505, CA1506, CA1507, CA1508, CA1509
- CA1510, CA1511, CA1512, CA1513, CA1514, CA1515, CA1516

## Critical Maintainability Rules (with examples)

- CA1507: Use nameof instead of string literals for member names.
```csharp
// ✅
throw new ArgumentNullException(nameof(userId));

// ❌
throw new ArgumentNullException("userId");
```

- CA1510-CA1513: Prefer ThrowIf helpers.
```csharp
// ✅
ArgumentNullException.ThrowIfNull(input);
ArgumentOutOfRangeException.ThrowIfNegative(count);

// ❌
if (input == null) throw new ArgumentNullException(nameof(input));
if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
```

## Performance Rules

Mandatory IDs:
- CA1801, CA1802, CA1805, CA1806
- CA1810, CA1812, CA1813, CA1814, CA1815, CA1816, CA1819
- CA1820, CA1821, CA1822, CA1823, CA1824, CA1825, CA1826, CA1827, CA1828, CA1829
- CA1830, CA1831, CA1832, CA1833, CA1834, CA1835, CA1836, CA1837, CA1838, CA1839
- CA1840, CA1841, CA1842, CA1843, CA1844, CA1845, CA1846, CA1847, CA1848, CA1849
- CA1850, CA1851, CA1852, CA1853, CA1854, CA1855, CA1856, CA1857, CA1858, CA1859
- CA1860, CA1861, CA1862, CA1863, CA1864, CA1865, CA1866, CA1867, CA1868, CA1869
- CA1870, CA1871, CA1872, CA1873, CA1874, CA1875, CA1877

## Critical Performance Rules (with examples)

- CA1827/CA1828: Prefer Any/AnyAsync over Count/CountAsync for existence checks.
```csharp
// ✅
bool exists = items.Any();

// ❌
bool exists = items.Count() > 0;
```

- CA1829/CA1860: Use Count/Length/IsEmpty properties when available.
```csharp
// ✅
if (list.Count > 0) { }

// ❌
if (list.Any()) { }
```

- CA1851: Avoid multiple enumeration of IEnumerable.
```csharp
// ✅
List<int> valuesList = values.ToList();
int count = valuesList.Count;
int max = valuesList.Max();

// ❌
int count = values.Count();
int max = values.Max();
```

- CA1854: Prefer TryGetValue over ContainsKey + indexer.
```csharp
// ✅
if (map.TryGetValue(key, out string? value)) { }

// ❌
if (map.ContainsKey(key))
{
	string value = map[key];
}
```

- CA1846: Prefer AsSpan over Substring in hot paths.
```csharp
// ✅
ReadOnlySpan<char> part = text.AsSpan(start, length);

// ❌
string part = text.Substring(start, length);
```

- CA1848: Prefer LoggerMessage delegates for high-volume logs.
```csharp
// ✅
private static readonly Action<ILogger, string, Exception?> LogUserLoaded =
	LoggerMessage.Define<string>(LogLevel.Debug, new EventId(1, nameof(LogUserLoaded)), "User {UserId} loaded");

// ❌
logger.LogDebug("User {UserId} loaded", userId);
```

## Enforcement Notes

- Favor Any/TryGetValue/TryAdd over Count + Contains patterns.
- Favor Span/Memory APIs over Substring/GetSubArray in perf-sensitive code.
- Prefer async APIs inside async methods.
- Avoid duplicate enumeration of IEnumerable.
