---
description: "Use when implementing authentication, authorization, serialization, cryptography, platform interop, ASP.NET anti-forgery, and single-file publishing constraints. Includes Microsoft security/platform CA and IL rules."
---

# C# Rules: Security, Platform, Serialization, and IL

Apply these rules for secure coding, interop safety, platform compatibility, and single-file publish constraints.

## Platform & Interop Rules

Mandatory IDs:
- CA1401
- CA1416, CA1417, CA1418, CA1419
- CA1420, CA1421, CA1422

## Critical Platform Rules (with examples)

- CA1416: Guard platform-specific API usage.
```csharp
// ✅
if (OperatingSystem.IsWindows())
{
	UseWindowsOnlyApi();
}

// ❌
UseWindowsOnlyApi();
```

## Security Rules (General)

Mandatory IDs:
- CA2100, CA2101, CA2109, CA2119, CA2153

## Critical Security Rules (with examples)

- CA2100: Never build SQL with string concatenation.
```csharp
// ✅
command.CommandText = "SELECT * FROM Users WHERE Id = @id";
command.Parameters.Add(new NpgsqlParameter("@id", id));

// ❌
command.CommandText = $"SELECT * FROM Users WHERE Id = '{id}'";
```

- CA2153: Do not catch corrupted-state exceptions.
```csharp
// ✅
catch (IOException ex)
{
	_logger.LogError(ex, "I/O error");
}

// ❌
catch (AccessViolationException)
{
}
```

## Insecure Deserialization Rules

Mandatory IDs:
- CA2300, CA2301, CA2302, CA2305
- CA2310, CA2311, CA2312, CA2315
- CA2321, CA2322, CA2326, CA2327, CA2328, CA2329, CA2330
- CA2350, CA2351, CA2352, CA2353, CA2354, CA2355, CA2356
- CA2361, CA2362

## Critical Deserialization Rules (with examples)

- CA2300/CA2301/CA2302: Do not use BinaryFormatter.
```csharp
// ✅
MyDto? dto = JsonSerializer.Deserialize<MyDto>(json);

// ❌
BinaryFormatter formatter = new();
object obj = formatter.Deserialize(stream);
```

- CA2326-CA2330: Keep Json.NET TypeNameHandling safe.
```csharp
// ✅
JsonSerializerSettings settings = new()
{
	TypeNameHandling = TypeNameHandling.None
};

// ❌
JsonSerializerSettings settings = new()
{
	TypeNameHandling = TypeNameHandling.Auto
};
```

## Injection & Validation Rules

Mandatory IDs:
- CA3001, CA3002, CA3003, CA3004, CA3005, CA3006, CA3007, CA3008, CA3009, CA3010, CA3011, CA3012
- CA3061, CA3075, CA3076, CA3077, CA3147

## Critical Injection/Validation Rules (with examples)

- CA3001: Parameterize all SQL inputs.
- CA3002: Encode untrusted content before HTML output.
- CA3147/CA5391: Require anti-forgery tokens on mutating MVC actions.

```csharp
// ✅
[ValidateAntiForgeryToken]
public IActionResult Post(UpdateModel model) => View();

// ❌
public IActionResult Post(UpdateModel model) => View();
```

## Cryptography / Transport / ASP.NET Security Rules

Mandatory IDs:
- CA5350, CA5351, CA5358, CA5359
- CA5360, CA5361, CA5362, CA5363, CA5364, CA5365, CA5366, CA5367, CA5368, CA5369
- CA5370, CA5371, CA5372, CA5373, CA5374, CA5375, CA5376, CA5377, CA5378, CA5379
- CA5380, CA5381, CA5382, CA5383, CA5384, CA5385, CA5386, CA5387, CA5388, CA5389
- CA5390, CA5391, CA5392, CA5393, CA5394, CA5395, CA5396, CA5397, CA5398, CA5399
- CA5400, CA5401, CA5402, CA5403, CA5404, CA5405

## Critical Crypto/Transport Rules (with examples)

- CA5351: Do not use MD5/DES/RC2.
- CA5364/CA5397/CA5398: Do not use deprecated or hard-coded TLS protocol values.
- CA5394: Use RandomNumberGenerator for security values.

```csharp
// ✅
byte[] bytes = RandomNumberGenerator.GetBytes(32);

// ❌
Random random = new();
byte[] bytes = new byte[32];
random.NextBytes(bytes);
```

## IL Rules (Single-file / RequiresAssemblyFiles)

Mandatory IDs:
- IL3000, IL3001, IL3002, IL3003, IL3005

## Critical IL Rules (with examples)

- IL3000/IL3001: Avoid relying on assembly file paths in single-file publish.
- IL3002/IL3003/IL3005: Respect RequiresAssemblyFiles annotations and propagation.

```csharp
// ✅
string? baseDir = AppContext.BaseDirectory;

// ❌ (single-file sensitive)
string location = typeof(Program).Assembly.Location;
```

## Enforcement Notes

- Never relax token/certificate/security validation to pass tests.
- Use XmlReader with secure settings for XML handling.
- Never build SQL/LDAP/XPath/XML commands from untrusted input without parameterization/escaping.
- When publishing single-file, avoid APIs requiring assembly file paths.
