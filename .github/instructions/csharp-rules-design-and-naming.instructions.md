---
description: "Use when designing public APIs, DTOs, entities, interfaces, events, naming, and globalization behavior in C#. Includes Microsoft CA design/naming/globalization rules."
---

# C# Rules: Design, API, Naming, Globalization

Apply these rules when creating or changing public contracts, object models, naming conventions, and string/culture behavior.

## Design & API Rules

Mandatory IDs:
- CA1000, CA1001, CA1002, CA1003, CA1005, CA1008, CA1010, CA1012, CA1014, CA1016, CA1017, CA1018, CA1019
- CA1021, CA1024, CA1027, CA1028
- CA1030, CA1031, CA1032, CA1033, CA1034, CA1036
- CA1040, CA1041, CA1043, CA1044, CA1045, CA1046, CA1047
- CA1050, CA1051, CA1052, CA1053, CA1054, CA1055, CA1056, CA1058
- CA1060, CA1061, CA1062, CA1063, CA1064, CA1065, CA1066, CA1067, CA1068, CA1069, CA1070
- CA1200

## Critical Design/API Rules (with examples)

- CA1002: Do not expose List<T> in public APIs.
```csharp
// ✅
public IReadOnlyList<string> Categories { get; }

// ❌
public List<string> Categories { get; }
```

- CA1031: Avoid catching general Exception.
```csharp
// ✅
catch (InvalidOperationException ex)
{
	SentrySdk.Logger?.LogError(ex, "Operation failed");
}

// ❌
catch (Exception)
{
}
```

- CA1054/CA1055/CA1056: Use Uri instead of string for URI inputs/outputs/properties.
```csharp
// ✅
public Uri CallbackUri { get; init; } = default!;

// ❌
public string CallbackUri { get; init; } = string.Empty;
```

- CA1062: Validate public method arguments.
```csharp
// ✅
public void Process(User user)
{
	ArgumentNullException.ThrowIfNull(user);
}

// ❌
public void Process(User user)
{
	_ = user.Name;
}
```

- CA1068: CancellationToken must be last.
```csharp
// ✅
public Task SaveAsync(string id, CancellationToken cancellationToken)

// ❌
public Task SaveAsync(CancellationToken cancellationToken, string id)
```

## Globalization Rules

Mandatory IDs:
- CA1303, CA1304, CA1305, CA1307, CA1308, CA1309, CA1310, CA1311

## Critical Globalization Rules (with examples)

- CA1305: Specify IFormatProvider for formatting/parsing.
```csharp
// ✅
string text = amount.ToString(CultureInfo.InvariantCulture);

// ❌
string text = amount.ToString();
```

- CA1309/CA1310: Use explicit StringComparison.
```csharp
// ✅
if (name.Equals("admin", StringComparison.OrdinalIgnoreCase)) { }

// ❌
if (name.Equals("admin")) { }
```

## Naming Rules

Mandatory IDs:
- CA1700, CA1707, CA1708
- CA1710, CA1711, CA1712, CA1713, CA1714, CA1715, CA1716, CA1717
- CA1720, CA1721, CA1724, CA1725, CA1727

## Critical Naming Rules (with examples)

- CA1715: Interfaces start with I; generic type parameters start with T.
```csharp
// ✅
public interface IUserService {}
public class Repository<TItem> {}

// ❌
public interface UserService {}
public class Repository<ItemType> {}
```

- CA1727: Use PascalCase for named logging placeholders.
```csharp
// ✅
logger.LogInformation("User {UserId} logged in", userId);

// ❌
logger.LogInformation("User {user_id} logged in", userId);
```

## Enforcement Notes

- Prefer explicit types (no var) in project code.
- Prefer primary constructors when applicable.
- Use target-typed new only with explicit left-side type.
- Use [] or new[] for collection/array initialization when appropriate.
