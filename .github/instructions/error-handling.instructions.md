---
description: "Use when writing exception handling, catch blocks, Sentry logging, or any error-returning code. Covers leak prevention, exception hierarchy, and user-safe error messages."
applyTo: "**/*.cs"
---

# Error Handling and Leak Prevention

- **Never expose internal exception details to users.** Show generic messages only.
- `catch (Exception ex)` blocks must NEVER include `ex.Message` in user-facing output.
- `catch (DomainException ex)` may show `ex.Message` since domain exceptions are user-safe by design (business rule violations).
- `catch (HttpRequestException ex)` must always show a generic network error message.
- **Every catch block must capture to Sentry** via `SentrySdk.CaptureException` with appropriate scope tags and level.
- Infrastructure layer must never embed raw exception messages in return values (`AuthResult.Failure`, etc.). Log to Sentry, return generic.
- Application layer command handlers must not propagate infrastructure error messages as DomainException messages. Use fixed/translated messages.
- Validation: before adding a new catch block or error path, verify the message cannot contain sensitive infrastructure details (hostnames, IPs, connection strings, file paths, stack traces).
- Exception type hierarchy: catch specific types (HttpRequestException, DomainException) before the generic `Exception` fallback.
