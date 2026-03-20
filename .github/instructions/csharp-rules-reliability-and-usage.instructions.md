---
description: "Use when writing exception handling, resource lifecycle, async flow, cancellation, Task/ValueTask patterns, and API usage correctness in C#. Includes Microsoft CA reliability and usage rules."
---

# C# Rules: Reliability and Usage

Apply these rules for disposal, exceptions, task/cancellation behavior, and API usage correctness.

## Reliability Rules

Mandatory IDs:
- CA2000, CA2002, CA2007, CA2008, CA2009
- CA2011, CA2012, CA2013, CA2014, CA2015, CA2016, CA2017, CA2018, CA2019
- CA2020, CA2021, CA2022, CA2023, CA2024, CA2025, CA2026

## Critical Reliability Rules (with examples)

- CA2000: Dispose objects before losing scope.
```csharp
// ✅
using FileStream stream = File.OpenRead(path);

// ❌
FileStream stream = File.OpenRead(path);
```

- CA2007: In library/infrastructure code, use ConfigureAwait(false) where context capture is unnecessary.
```csharp
// ✅
await task.ConfigureAwait(false);

// ❌
await task;
```

- CA2016: Forward CancellationToken to downstream APIs.
```csharp
// ✅
await repository.GetAsync(id, cancellationToken);

// ❌
await repository.GetAsync(id, CancellationToken.None);
```

- CA2022: Validate Stream.Read return count; it may read fewer bytes than requested.
```csharp
// ✅
int bytesRead = await stream.ReadAsync(buffer, cancellationToken);
if (bytesRead == 0) { return; }

// ❌
await stream.ReadAsync(buffer, cancellationToken);
ProcessBuffer(buffer); // assumes full read
```

- CA2025: Do not pass IDisposable instances into fire-and-forget tasks.
```csharp
// ✅
await Task.Run(() => UseResource(resource), cancellationToken);

// ❌
_ = Task.Run(() => UseResource(resource));
resource.Dispose();
```

## Usage Rules

Mandatory IDs:
- CA2200, CA2201, CA2207, CA2208
- CA2211, CA2213, CA2214, CA2215, CA2216, CA2217, CA2218, CA2219
- CA2224, CA2225, CA2226, CA2227, CA2229
- CA2231, CA2234, CA2235, CA2237
- CA2241, CA2242, CA2243, CA2244, CA2245, CA2246, CA2247, CA2248, CA2249
- CA2250, CA2251, CA2252, CA2253, CA2254, CA2255, CA2256, CA2257, CA2258, CA2259
- CA2260, CA2261, CA2262, CA2263, CA2264, CA2265

## Critical Usage Rules (with examples)

- CA2200: Preserve stack trace when rethrowing.
```csharp
// ✅
catch (Exception)
{
	throw;
}

// ❌
catch (Exception ex)
{
	throw ex;
}
```

- CA2208: Use correct argument-exception constructors.
```csharp
// ✅
throw new ArgumentException("Value is invalid.", nameof(value));

// ❌
throw new ArgumentException(nameof(value));
```

- CA2213: Dispose disposable fields in Dispose.
```csharp
// ✅
public void Dispose()
{
	_connection.Dispose();
}

// ❌
public void Dispose()
{
}
```

- CA2249: Prefer string.Contains when checking presence.
```csharp
// ✅
if (text.Contains("abc", StringComparison.Ordinal)) { }

// ❌
if (text.IndexOf("abc", StringComparison.Ordinal) >= 0) { }
```

- CA2250: Use ThrowIfCancellationRequested.
```csharp
// ✅
cancellationToken.ThrowIfCancellationRequested();

// ❌
if (cancellationToken.IsCancellationRequested)
{
	throw new OperationCanceledException(cancellationToken);
}
```

## Enforcement Notes

- Always preserve stack traces on rethrow.
- Always forward CancellationToken when downstream APIs accept it.
- Use ThrowIf... helpers for guards.
- Never leave IDisposable unmanaged beyond scope.
