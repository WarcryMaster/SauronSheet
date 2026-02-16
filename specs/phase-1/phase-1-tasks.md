# Phase 1 Tasks

**Version**: 1.0.0  
**Created**: 2026-02-15  
**Aligned with**: Constitution v1.1.0, Phase 1 Plan v1.0.0, Phase 1 Spec v1.0.0  
**Duration**: Weeks 3–5  
**Goal**: Multi-user authentication with JWT, tenant-scoped data access, login/register UI

---

## 1. Pre-Implementation Validation

### Tarea 1.0 – Verificar que IUserContext interface existe en Phase 0
- **Path**: `src/SauronSheet.Application/Common/IUserContext.cs`
- **Acción**: Verificar (crear si no existe)
- **Descripción**: El spec de Phase 0 menciona que IUserContext debe ser una interfaz definida en la capa Application. Verificar que existe. Si no existe, crear:
  - Interfaz `IUserContext` en `src/SauronSheet.Application/Common/IUserContext.cs`
  - Dos propiedades: `string UserId { get; }` y `bool IsAuthenticated { get; }`
  - Namespace: `SauronSheet.Application.Common`
- **Dependencias**: Phase 0 completado
- **Validación**: 
  - Archivo `src/SauronSheet.Application/Common/IUserContext.cs` existe
  - Contiene interface `IUserContext` con ambas propiedades
  - Es referenciable desde Application y Infrastructure

### Tarea 1.1 – Validar que Phase 0 está completo
- **Path**: N/A (validación local)
- **Acción**: Validar
- **Descripción**: Ejecutar `dotnet build` y `dotnet test` para asegurar que Phase 0 se compiló correctamente y todos los 13 tests pasan. Si Phase 0 tiene errores, **NO proceder** a Phase 1.
- **Dependencias**: Tarea 1.0
- **Validación**: 
  - `dotnet build` exit code 0, cero warnings
  - `dotnet test` salida contiene "13 passed"
  - `dotnet test --filter Category=Domain` retorna 11 tests
  - `dotnet test --filter Category=Application` retorna 2 tests

### Tarea 1.2 – Verificar Supabase Auth habilitado
- **Path**: N/A (validación external)
- **Acción**: Verificar
- **Descripción**: En el dashboard de Supabase, navegar a Settings → Auth Providers y confirmar que el proveedor "Email" está habilitado. Copiar la URL del proyecto Supabase y el API key (anon key).
- **Dependencias**: Supabase account existente
- **Validación**: 
  - Email provider visible en Supabase dashboard
  - URL del proyecto disponible (formato: https://xxxxx.supabase.co)
  - API key (anon) disponible en Settings → API

### Tarea 1.3 – Obtener JWT Secret de Supabase
- **Path**: N/A (validación external)
- **Acción**: Recuperar
- **Descripción**: En el dashboard de Supabase, navegar a Settings → API → JWT Settings y copiar el JWT Secret. Este valor se usará en appsettings.json en Phase 1E (Frontend layer).
- **Dependencias**: Tarea 1.2
- **Validación**: 
  - JWT Secret copiado y guardado en lugar seguro (password manager o env var local)
  - JWT Secret tiene formato válido (larga cadena de caracteres base64)

---

## 2. Domain Layer Extensions

### Tarea 2.1 – Crear estructura de carpetas Domain para Phase 1
- **Path**: `src/SauronSheet.Domain/ValueObjects/`, `src/SauronSheet.Domain/Services/`
- **Acción**: Crear directorios
- **Descripción**: Crear las carpetas `ValueObjects` (si no existe) y `Services` dentro de `src/SauronSheet.Domain/` para alojar los nuevos value objects y la interfaz de servicio de auth.
- **Dependencias**: Phase 0 Domain layer existe
- **Validación**: 
  - Directorio `src/SauronSheet.Domain/ValueObjects/` existe
  - Directorio `src/SauronSheet.Domain/Services/` existe
  - Ambos están vacíos (sin archivos)

### Tarea 2.2 – Crear archivo UserId.cs (value object)
- **Path**: `src/SauronSheet.Domain/ValueObjects/UserId.cs`
- **Acción**: Crear archivo
- **Descripción**: Crear el value object `UserId` que hereda de `ValueObject` (Phase 0). El record debe contener una propiedad `Value` de tipo `string`, validar que no sea nulo/vacío en el constructor (lanzar `DomainException` si lo es), y tener un método `ToString()` que retorne el valor. Este value object representa el ID único del usuario de forma type-safe.
- **Dependencias**: Tarea 2.1, Phase 0 ValueObject base class
- **Validación**: 
  - Archivo compila sin errores
  - Hereda de `ValueObject`
  - Es un `record` (no una clase)
  - Tiene propiedad `Value` tipo `string` (public get only)
  - Constructor valida que Value no sea null/whitespace

### Tarea 2.3 – Crear archivo AuthResult.cs (value object)
- **Path**: `src/SauronSheet.Domain/ValueObjects/AuthResult.cs`
- **Acción**: Crear archivo
- **Descripción**: Crear el value object `AuthResult` como `record`. Debe contener:
  - `UserId? UserId { get; }`
  - `string? AccessToken { get; }`
  - `string? RefreshToken { get; }`
  - `DateTime? ExpiresAt { get; }`
  - `bool IsSuccess { get; }`
  - `string? ErrorMessage { get; }`
  - Dos factory methods estáticos: `Success(UserId userId, string accessToken, string refreshToken, DateTime expiresAt)` y `Failure(string errorMessage)`
- **Dependencias**: Tarea 2.2 (UserId)
- **Validación**: 
  - Archivo compila
  - Es un `record`
  - Tiene todas las propiedades listadas
  - Factory methods son `static`

### Tarea 2.4 – Crear archivo UserProfile.cs (value object)
- **Path**: `src/SauronSheet.Domain/ValueObjects/UserProfile.cs`
- **Acción**: Crear archivo
- **Descripción**: Crear el value object `UserProfile` como `record`. Debe contener:
  - `UserId Id { get; }`
  - `string Email { get; }`
  - `string? DisplayName { get; }`
  - `DateTime CreatedAt { get; }`
  - Constructor que valide que Email no sea nulo/vacío (lanzar `DomainException` si lo es)
- **Dependencias**: Tarea 2.2 (UserId)
- **Validación**: 
  - Archivo compila
  - Es un `record`
  - Constructor valida Email
  - Todas las propiedades presentes

### Tarea 2.5 – Crear archivo IAuthService.cs (interfaz de dominio)
- **Path**: `src/SauronSheet.Domain/Services/IAuthService.cs`
- **Acción**: Crear archivo
- **Descripción**: Crear la interfaz `IAuthService` en el namespace `SauronSheet.Domain.Services`. La interfaz define el contrato que la capa Infrastructure debe implementar:
  - `Task<AuthResult> RegisterAsync(string email, string password)`
  - `Task<AuthResult> LoginAsync(string email, string password)`
  - `Task LogoutAsync(string accessToken)`
  - `Task<AuthResult> RefreshTokenAsync(string refreshToken)`
  - `Task<UserProfile?> GetUserProfileAsync(string userId)`
- **Dependencias**: Tarea 2.3 (AuthResult), Tarea 2.4 (UserProfile)
- **Validación**: 
  - Archivo compila
  - Es una `interface`
  - Tiene los 5 métodos listados
  - Métodos retornan tipos correctos

---

## 3. Domain Layer Tests (RED Phase)

### Tarea 3.1 – Crear estructura de carpetas Domain.Tests para Phase 1
- **Path**: `tests/SauronSheet.Domain.Tests/ValueObjects/`, `tests/SauronSheet.Domain.Tests/Services/`
- **Acción**: Crear directorios
- **Descripción**: Crear las carpetas `ValueObjects` y `Services` dentro de `tests/SauronSheet.Domain.Tests/` para alojar los nuevos test files de Phase 1.
- **Dependencias**: Phase 0 Domain.Tests project
- **Validación**: 
  - Directorio `tests/SauronSheet.Domain.Tests/ValueObjects/` existe
  - Directorio `tests/SauronSheet.Domain.Tests/Services/` existe

### Tarea 3.2 – Crear archivo UserIdTests.cs (test stubs - RED)
- **Path**: `tests/SauronSheet.Domain.Tests/ValueObjects/UserIdTests.cs`
- **Acción**: Crear archivo
- **Descripción**: Crear clase `UserIdTests` con 6 métodos de test stub (RED phase). Cada método debe tener `[Fact]` y `[Trait("Category", "Domain")]` y contener `Assert.True(false, "Implement ...")` para fallar intencionalmente:
  - `UserId_ValidString_SetsValue()` → Test T-1.01
  - `UserId_NullString_ThrowsDomainException()` → Test T-1.02
  - `UserId_EmptyString_ThrowsDomainException()` → Test T-1.03
  - `UserId_WhitespaceString_ThrowsDomainException()` → Test T-1.04
  - `UserId_Equality_SameValue()` → Test T-1.05
  - `UserId_Inequality_DifferentValue()` → Test T-1.06
- **Dependencias**: Tarea 3.1
- **Validación**: 
  - Archivo compila
  - 6 métodos presentes
  - Todos tienen `[Fact]` y `[Trait("Category", "Domain")]`
  - Todos contienen `Assert.True(false, ...)`

### Tarea 3.3 – Crear archivo AuthResultTests.cs (test stubs - RED)
- **Path**: `tests/SauronSheet.Domain.Tests/ValueObjects/AuthResultTests.cs`
- **Acción**: Crear archivo
- **Descripción**: Crear clase `AuthResultTests` con 2 métodos de test stub (RED phase):
  - `AuthResult_SuccessFactory_SetsProperties()` → Test T-1.07
  - `AuthResult_FailureFactory_SetsError()` → Test T-1.08
  - Ambos con `[Fact]` y `[Trait("Category", "Domain")]` y `Assert.True(false, "Implement ...")`
- **Dependencias**: Tarea 3.1
- **Validación**: 
  - Archivo compila
  - 2 métodos presentes con atributos correctos

### Tarea 3.4 – Ejecutar tests Domain en RED phase
- **Path**: N/A (command line)
- **Acción**: Ejecutar comando
- **Descripción**: Ejecutar `dotnet test --filter Category=Domain --no-build` para verificar que los nuevos 8 tests de Domain fallan (RED phase) y los 11 tests de Phase 0 aún pasan.
- **Dependencias**: Tareas 3.2, 3.3
- **Validación**: 
  - Output contiene "8 failed" (tests nuevos T-1.01 a T-1.08)
  - Output contiene "11 passed" (tests Phase 0)
  - Total: 19 tests discovered

---

## 4. Domain Layer Implementation (GREEN Phase)

### Tarea 4.1 – Implementar UserId.cs (GREEN)
- **Path**: `src/SauronSheet.Domain/ValueObjects/UserId.cs`
- **Acción**: Modificar archivo (actualizar desde tarea 2.2)
- **Descripción**: Implementar la lógica del value object UserId:
  - Constructor que valide que `value` no sea null, empty, o whitespace
  - Si es null/empty/whitespace: lanzar `new DomainException("UserId cannot be null or empty.")`
  - Guardar el valor en la propiedad `Value`
  - Implementar `ToString()` que retorne `Value`
  - El record debe soportar value-based equality automáticamente (C# records)
- **Dependencias**: Tarea 2.2
- **Validación**: 
  - Archivo compila
  - Tests T-1.01 a T-1.06 pasan (`dotnet test --filter "ClassName=SauronSheet.Domain.Tests.ValueObjects.UserIdTests"`)

### Tarea 4.2 – Implementar AuthResult.cs (GREEN)
- **Path**: `src/SauronSheet.Domain/ValueObjects/AuthResult.cs`
- **Acción**: Modificar archivo (actualizar desde tarea 2.3)
- **Descripción**: Implementar AuthResult value object:
  - Constructor privado que acepte todos los 6 parámetros y los asigne a las propiedades
  - Factory method `Success(UserId userId, string accessToken, string refreshToken, DateTime expiresAt)` que retorne `new AuthResult(userId, accessToken, refreshToken, expiresAt, true, null)`
  - Factory method `Failure(string errorMessage)` que retorne `new AuthResult(null, null, null, null, false, errorMessage)`
- **Dependencias**: Tarea 2.3, Tarea 4.1 (UserId)
- **Validación**: 
  - Archivo compila
  - Tests T-1.07 y T-1.08 pasan

### Tarea 4.3 – Completar implementación UserProfile.cs (GREEN)
- **Path**: `src/SauronSheet.Domain/ValueObjects/UserProfile.cs`
- **Acción**: Modificar archivo (actualizar desde tarea 2.4)
- **Descripción**: Asegurar que UserProfile contiene validación en el constructor:
  - Validar que `email` no sea null/empty/whitespace
  - Si es inválido: lanzar `new DomainException("Email cannot be null or empty.")`
- **Dependencias**: Tarea 2.4
- **Validación**: 
  - Archivo compila
  - Constructor valida email

### Tarea 4.4 – Ejecutar tests Domain en GREEN phase
- **Path**: N/A (command line)
- **Acción**: Ejecutar comando
- **Descripción**: Ejecutar `dotnet test --filter Category=Domain --no-build` para verificar que todos los 19 tests de Domain pasan (11 Phase 0 + 8 Phase 1).
- **Dependencias**: Tareas 4.1, 4.2, 4.3
- **Validación**: 
  - `dotnet test --filter Category=Domain --no-build` retorna "19 passed"
  - Exit code 0

---

## 5. Application Layer Tests (RED Phase)

### Tarea 5.1 – Crear estructura de carpetas Application.Tests para Phase 1
- **Path**: `tests/SauronSheet.Application.Tests/Features/Auth/Commands/`, `tests/SauronSheet.Application.Tests/Features/Auth/Queries/`, `tests/SauronSheet.Application.Tests/Common/`
- **Acción**: Crear directorios
- **Descripción**: Crear la estructura de carpetas necesaria en Application.Tests para los nuevos tests de Phase 1:
  - `Features/Auth/Commands/` - Tests de handlers de comandos auth
  - `Features/Auth/Queries/` - Tests de handlers de queries auth
  - `Common/` - Tests de behaviors y DI
- **Dependencias**: Phase 0 Application.Tests project
- **Validación**: 
  - Todos los directorios existen
  - Están vacíos (sin archivos)

### Tarea 5.2 – Crear archivo RegisterUserCommandTests.cs (test stubs - RED)
- **Path**: `tests/SauronSheet.Application.Tests/Features/Auth/Commands/RegisterUserCommandTests.cs`
- **Acción**: Crear archivo
- **Descripción**: Crear clase `RegisterUserCommandTests` con 4 métodos de test stub (RED phase), cada uno con `[Fact]`, `[Trait("Category", "Application")]`, y `Assert.True(false, "Implement ...")`:
  - `RegisterUser_ValidInput_ReturnsRegistrationResult()` → Test T-1.09
  - `RegisterUser_DuplicateEmail_ThrowsDomainException()` → Test T-1.10
  - `RegisterUser_WeakPassword_ThrowsDomainException()` → Test T-1.11
  - `RegisterUser_MismatchedPasswords_ThrowsDomainException()` → Test T-1.12
- **Dependencias**: Tarea 5.1
- **Validación**: 
  - Archivo compila
  - 4 métodos presentes con atributos correctos

### Tarea 5.3 – Crear archivo LoginUserCommandTests.cs (test stubs - RED)
- **Path**: `tests/SauronSheet.Application.Tests/Features/Auth/Commands/LoginUserCommandTests.cs`
- **Acción**: Crear archivo
- **Descripción**: Crear clase `LoginUserCommandTests` con 2 métodos de test stub:
  - `LoginUser_ValidCredentials_ReturnsAuthToken()` → Test T-1.13
  - `LoginUser_InvalidCredentials_ThrowsUnauthorized()` → Test T-1.14
- **Dependencias**: Tarea 5.1
- **Validación**: 
  - Archivo compila
  - 2 métodos presentes

### Tarea 5.4 – Crear archivo LogoutUserCommandTests.cs (test stubs - RED)
- **Path**: `tests/SauronSheet.Application.Tests/Features/Auth/Commands/LogoutUserCommandTests.cs`
- **Acción**: Crear archivo
- **Descripción**: Crear clase `LogoutUserCommandTests` con 1 método de test stub:
  - `LogoutUser_ValidToken_CallsAuthService()` → Test T-1.15
- **Dependencias**: Tarea 5.1
- **Validación**: 
  - Archivo compila
  - 1 método presente

### Tarea 5.5 – Crear archivo RefreshTokenCommandTests.cs (test stubs - RED)
- **Path**: `tests/SauronSheet.Application.Tests/Features/Auth/Commands/RefreshTokenCommandTests.cs`
- **Acción**: Crear archivo
- **Descripción**: Crear clase `RefreshTokenCommandTests` con 2 métodos de test stub:
  - `RefreshToken_ValidRefresh_ReturnsNewTokens()` → Test T-1.16
  - `RefreshToken_InvalidRefresh_ThrowsUnauthorized()` → Test T-1.17
- **Dependencias**: Tarea 5.1
- **Validación**: 
  - Archivo compila
  - 2 métodos presentes

### Tarea 5.6 – Crear archivo GetCurrentUserQueryTests.cs (test stubs - RED)
- **Path**: `tests/SauronSheet.Application.Tests/Features/Auth/Queries/GetCurrentUserQueryTests.cs`
- **Acción**: Crear archivo
- **Descripción**: Crear clase `GetCurrentUserQueryTests` con 2 métodos de test stub:
  - `GetCurrentUser_Authenticated_ReturnsProfile()` → Test T-1.18
  - `GetCurrentUser_Unauthenticated_ThrowsUnauthorized()` → Test T-1.19
- **Dependencias**: Tarea 5.1
- **Validación**: 
  - Archivo compila
  - 2 métodos presentes

### Tarea 5.7 – Crear archivo TenantScopingBehaviorTests.cs (test stubs - RED)
- **Path**: `tests/SauronSheet.Application.Tests/Common/TenantScopingBehaviorTests.cs`
- **Acción**: Crear archivo
- **Descripción**: Crear clase `TenantScopingBehaviorTests` con 3 métodos de test stub:
  - `TenantScoping_Authenticated_Proceeds()` → Test T-1.20
  - `TenantScoping_Unauthenticated_ThrowsUnauthorized()` → Test T-1.21
  - `TenantScoping_AnonymousRequest_SkipsCheck()` → Test T-1.22
- **Dependencias**: Tarea 5.1
- **Validación**: 
  - Archivo compila
  - 3 métodos presentes

### Tarea 5.8 – Ejecutar tests Application en RED phase
- **Path**: N/A (command line)
- **Acción**: Ejecutar comando
- **Descripción**: Ejecutar `dotnet test --filter Category=Application --no-build` para verificar que los nuevos 14 tests de Application fallan (RED phase) y los 2 tests de Phase 0 aún pasan.
- **Dependencias**: Tareas 5.2 a 5.7
- **Validación**: 
  - Output contiene "14 failed" (tests nuevos T-1.09 a T-1.22)
  - Output contiene "2 passed" (tests Phase 0)
  - Total: 16 tests discovered

---

## 6. Application Layer - Commands & Handlers

### Tarea 6.1 – Crear estructura de carpetas Application para Phase 1
- **Path**: `src/SauronSheet.Application/Features/Auth/Commands/`, `src/SauronSheet.Application/Features/Auth/Queries/`, `src/SauronSheet.Application/Features/Auth/DTOs/`
- **Acción**: Crear directorios
- **Descripción**: Crear la estructura de carpetas para Auth feature en Application layer:
  - `Features/Auth/Commands/` - Command records y handlers
  - `Features/Auth/Queries/` - Query records y handlers
  - `Features/Auth/DTOs/` - Data transfer objects
- **Dependencias**: Phase 0 Application project
- **Validación**: 
  - Todos los directorios existen
  - Están vacíos

### Tarea 6.2 – Crear archivo IAnonymousRequest.cs (interfaz marcadora)
- **Path**: `src/SauronSheet.Application/Common/IAnonymousRequest.cs`
- **Acción**: Crear archivo
- **Descripción**: Crear una interfaz marcadora `IAnonymousRequest` sin miembros. Esta interfaz se implementará en comandos que NO requieren autenticación (Register, Login, RefreshToken). El TenantScopingBehavior la usará para saltarse la validación de autenticación.
- **Dependencias**: Tarea 6.1
- **Validación**: 
  - Archivo compila
  - Es una `interface`
  - Sin miembros (marker interface)

### Tarea 6.3 – Crear archivo RegisterUserCommand.cs
- **Path**: `src/SauronSheet.Application/Features/Auth/Commands/RegisterUserCommand.cs`
- **Acción**: Crear archivo
- **Descripción**: Crear record `RegisterUserCommand` que implemente `IRequest<RegistrationResultDto>` e `IAnonymousRequest`. Propiedades:
  - `string Email`
  - `string Password`
  - `string ConfirmPassword`
- **Dependencias**: Tarea 6.2
- **Validación**: 
  - Archivo compila
  - Es un `record`
  - Implementa ambas interfaces

### Tarea 6.4 – Crear archivo LoginUserCommand.cs
- **Path**: `src/SauronSheet.Application/Features/Auth/Commands/LoginUserCommand.cs`
- **Acción**: Crear archivo
- **Descripción**: Crear record `LoginUserCommand` que implemente `IRequest<AuthTokenDto>` e `IAnonymousRequest`. Propiedades:
  - `string Email`
  - `string Password`
- **Dependencias**: Tarea 6.2
- **Validación**: 
  - Archivo compila
  - Es un record
  - Implementa ambas interfaces

### Tarea 6.5 – Crear archivo LogoutUserCommand.cs
- **Path**: `src/SauronSheet.Application/Features/Auth/Commands/LogoutUserCommand.cs`
- **Acción**: Crear archivo
- **Descripción**: Crear record `LogoutUserCommand` que implemente `IRequest<Unit>`. Propiedades:
  - `string AccessToken`
- **Dependencias**: Tarea 6.1
- **Validación**: 
  - Archivo compila
  - No implementa IAnonymousRequest (requiere auth)

### Tarea 6.6 – Crear archivo RefreshTokenCommand.cs
- **Path**: `src/SauronSheet.Application/Features/Auth/Commands/RefreshTokenCommand.cs`
- **Acción**: Crear archivo
- **Descripción**: Crear record `RefreshTokenCommand` que implemente `IRequest<AuthTokenDto>` e `IAnonymousRequest`. Propiedades:
  - `string RefreshToken`
- **Dependencias**: Tarea 6.2
- **Validación**: 
  - Archivo compila
  - Implementa ambas interfaces

### Tarea 6.7 – Crear archivo GetCurrentUserQuery.cs
- **Path**: `src/SauronSheet.Application/Features/Auth/Queries/GetCurrentUserQuery.cs`
- **Acción**: Crear archivo
- **Descripción**: Crear record `GetCurrentUserQuery` que implemente `IRequest<UserProfileDto>`. Sin propiedades (usa IUserContext internamente).
- **Dependencias**: Tarea 6.1
- **Validación**: 
  - Archivo compila
  - Sin propiedades (record vacío)

### Tarea 6.8 – Crear archivo RegistrationResultDto.cs
- **Path**: `src/SauronSheet.Application/Features/Auth/DTOs/RegistrationResultDto.cs`
- **Acción**: Crear archivo
- **Descripción**: Crear record `RegistrationResultDto` con propiedades:
  - `string UserId`
  - `string Email`
- **Dependencias**: Tarea 6.1
- **Validación**: 
  - Archivo compila
  - Es un record
  - Tiene ambas propiedades

### Tarea 6.9 – Crear archivo AuthTokenDto.cs
- **Path**: `src/SauronSheet.Application/Features/Auth/DTOs/AuthTokenDto.cs`
- **Acción**: Crear archivo
- **Descripción**: Crear record `AuthTokenDto` con propiedades:
  - `string AccessToken`
  - `string RefreshToken`
  - `DateTime ExpiresAt`
  - `string UserId`
- **Dependencias**: Tarea 6.1
- **Validación**: 
  - Archivo compila
  - Tiene todas las propiedades

### Tarea 6.10 – Crear archivo UserProfileDto.cs
- **Path**: `src/SauronSheet.Application/Features/Auth/DTOs/UserProfileDto.cs`
- **Acción**: Crear archivo
- **Descripción**: Crear record `UserProfileDto` con propiedades:
  - `string UserId`
  - `string Email`
  - `string? DisplayName`
  - `DateTime CreatedAt`
- **Dependencias**: Tarea 6.1
- **Validación**: 
  - Archivo compila
  - Tiene todas las propiedades

### Tarea 6.11 – Crear archivo RegisterUserCommandHandler.cs (GREEN)
- **Path**: `src/SauronSheet.Application/Features/Auth/Commands/RegisterUserCommandHandler.cs`
- **Acción**: Crear archivo
- **Descripción**: Crear clase `RegisterUserCommandHandler` que implemente `IRequestHandler<RegisterUserCommand, RegistrationResultDto>`. Constructor inyecta `IAuthService`. Método `Handle`:
  - Validar que Password == ConfirmPassword (si no, lanzar DomainException "Passwords do not match.")
  - Validar que Password.Length >= 8 (si no, lanzar DomainException "Password must be at least 8 characters.")
  - Llamar `await _authService.RegisterAsync(request.Email, request.Password)`
  - Si resultado no es success: lanzar DomainException con el error message
  - Si success: retornar `new RegistrationResultDto(result.UserId!.Value, request.Email)`
- **Dependencias**: Tarea 6.3, Domain IAuthService
- **Validación**: 
  - Archivo compila
  - Implementa la interfaz correcta
  - Tests T-1.09 a T-1.12 pasan

### Tarea 6.12 – Crear archivo LoginUserCommandHandler.cs (GREEN)
- **Path**: `src/SauronSheet.Application/Features/Auth/Commands/LoginUserCommandHandler.cs`
- **Acción**: Crear archivo
- **Descripción**: Crear clase `LoginUserCommandHandler` que implemente `IRequestHandler<LoginUserCommand, AuthTokenDto>`. Constructor inyecta `IAuthService`. Método `Handle`:
  - Llamar `await _authService.LoginAsync(request.Email, request.Password)`
  - Si no success: lanzar `new UnauthorizedAccessException("Invalid email or password.")`
  - Si success: retornar `new AuthTokenDto(result.AccessToken!, result.RefreshToken!, result.ExpiresAt!.Value, result.UserId!.Value)`
- **Dependencias**: Tarea 6.4
- **Validación**: 
  - Archivo compila
  - Tests T-1.13 y T-1.14 pasan

### Tarea 6.13 – Crear archivo LogoutUserCommandHandler.cs (GREEN)
- **Path**: `src/SauronSheet.Application/Features/Auth/Commands/LogoutUserCommandHandler.cs`
- **Acción**: Crear archivo
- **Descripción**: Crear clase `LogoutUserCommandHandler` que implemente `IRequestHandler<LogoutUserCommand, Unit>`. Constructor inyecta `IAuthService`. Método `Handle`:
  - Llamar `await _authService.LogoutAsync(request.AccessToken)`
  - Retornar `Unit.Value`
- **Dependencias**: Tarea 6.5
- **Validación**: 
  - Archivo compila
  - Test T-1.15 pasa

### Tarea 6.14 – Crear archivo RefreshTokenCommandHandler.cs (GREEN)
- **Path**: `src/SauronSheet.Application/Features/Auth/Commands/RefreshTokenCommandHandler.cs`
- **Acción**: Crear archivo
- **Descripción**: Crear clase `RefreshTokenCommandHandler` que implemente `IRequestHandler<RefreshTokenCommand, AuthTokenDto>`. Constructor inyecta `IAuthService`. Método `Handle`:
  - Llamar `await _authService.RefreshTokenAsync(request.RefreshToken)`
  - Si no success: lanzar `new UnauthorizedAccessException("Session expired.")`
  - Si success: retornar `new AuthTokenDto(result.AccessToken!, result.RefreshToken!, result.ExpiresAt!.Value, result.UserId!.Value)`
- **Dependencias**: Tarea 6.6
- **Validación**: 
  - Archivo compila
  - Tests T-1.16 y T-1.17 pasan

### Tarea 6.15 – Ejecutar tests Application para Commands (GREEN)
- **Path**: N/A (command line)
- **Acción**: Ejecutar comando
- **Descripción**: Ejecutar `dotnet test tests/SauronSheet.Application.Tests/Features/Auth/Commands/ --no-build` para verificar que los handlers de comandos están correctamente implementados. Alternativamente, ejecutar `dotnet test --filter "Category=Application --no-build"` y verificar que todos los tests de aplicación pasan.
- **Dependencias**: Tareas 6.11 a 6.14
- **Validación**: 
  - Tests T-1.09 a T-1.17 pasan (9 tests)
  - Exit code 0
  - Todos los handlers de Command compilan sin errores

---

## 7. Application Layer - Queries & Behaviors

### Tarea 7.1 – Crear archivo GetCurrentUserQueryHandler.cs (GREEN)
- **Path**: `src/SauronSheet.Application/Features/Auth/Queries/GetCurrentUserQueryHandler.cs`
- **Acción**: Crear archivo
- **Descripción**: Crear clase `GetCurrentUserQueryHandler` que implemente `IRequestHandler<GetCurrentUserQuery, UserProfileDto>`. Constructor inyecta `IAuthService` e `IUserContext`. Método `Handle`:
  - Si `_userContext.IsAuthenticated` es false: lanzar `new UnauthorizedAccessException("User is not authenticated.")`
  - Llamar `await _authService.GetUserProfileAsync(_userContext.UserId)`
  - Si profile es null: lanzar `new EntityNotFoundException("User", _userContext.UserId)`
  - Si profile existe: retornar `new UserProfileDto(profile.Id.Value, profile.Email, profile.DisplayName, profile.CreatedAt)`
- **Dependencias**: Tarea 6.7, Application IUserContext
- **Validación**: 
  - Archivo compila
  - Tests T-1.18 y T-1.19 pasan

### Tarea 7.2 – Crear archivo TenantScopingBehavior.cs (GREEN)
- **Path**: `src/SauronSheet.Application/Common/Behaviors/TenantScopingBehavior.cs`
- **Acción**: Crear archivo
- **Descripción**: Crear clase `TenantScopingBehavior<TRequest, TResponse>` que implemente `IPipelineBehavior<TRequest, TResponse>` donde `TRequest : IRequest<TResponse>`. Constructor inyecta `IUserContext`. Método `Handle`:
  - Si `request is IAnonymousRequest`: retornar `await next()` sin validar autenticación
  - Si `!_userContext.IsAuthenticated`: lanzar `new UnauthorizedAccessException("User is not authenticated.")`
  - Si autenticado y no es anonymous: retornar `await next()`
  - Este behavior se ejecuta ANTES de los handlers para inyectar el contexto de usuario
- **Dependencias**: Tarea 6.2 (IAnonymousRequest)
- **Validación**: 
  - Archivo compila
  - Tests T-1.20, T-1.21, T-1.22 pasan

### Tarea 7.3 – Crear directorio Behaviors en Application.Common
- **Path**: `src/SauronSheet.Application/Common/Behaviors/`
- **Acción**: Crear directorio
- **Descripción**: Si no existe, crear el directorio que alojará los pipeline behaviors (para tarea 7.2).
- **Dependencias**: Phase 0 Application project
- **Validación**: 
  - Directorio `src/SauronSheet.Application/Common/Behaviors/` existe
  - Está vacío

### Tarea 7.4 – Ejecutar tests Application para Queries & Behaviors (GREEN)
- **Path**: N/A (command line)
- **Acción**: Ejecutar comando
- **Descripción**: Ejecutar `dotnet test --filter Category=Application --no-build` para verificar que todos los 16 tests de Application pasan (2 Phase 0 + 14 Phase 1).
- **Dependencias**: Tareas 7.1, 7.2
- **Validación**: 
  - Output contiene "16 passed"
  - Exit code 0
  - `dotnet test --filter Category=Domain --no-build` aún retorna "19 passed"

---

## 8. Application Layer - DI Updates

### Tarea 8.1 – Actualizar DependencyInjection.cs en Application (REEMPLAZO completo)
- **Path**: `src/SauronSheet.Application/DependencyInjection.cs`
- **Acción**: Modificar archivo (reemplazar contenido completo)
- **Descripción**: Reemplazar el contenido completo de `DependencyInjection.cs` para registrar el comportamiento `TenantScopingBehavior`. El archivo debe:
  - Usar `using MediatR;` y `using Common.Behaviors;`
  - Clase estática `DependencyInjection`
  - Método `public static IServiceCollection AddApplicationServices(this IServiceCollection services)`
  - Dentro: `services.AddMediatR(cfg => { cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly); cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(TenantScopingBehavior<,>)); });`
  - Retornar `services`
  - **Nota**: El método `AddMediatR` debe incluir la línea `cfg.AddBehavior(...)` para registrar TenantScopingBehavior como pipeline behavior
  - **IMPORTANTE**: Este método NOT debe referenciar Infrastructure (usar solo Domain + MediatR). DI de Infrastructure se hace EN FRONTEND Program.cs
- **Dependencias**: Tarea 7.2 (TenantScopingBehavior)
- **Validación**: 
  - Archivo compila
  - MediatR se registra correctamente
  - TenantScopingBehavior se registra como behavior
  - NO hay referencias a Infrastructure (validar que solo usa MediatR y Common)

---

## 9. Infrastructure Layer - NuGet Packages

### Tarea 9.1 – Agregar NuGet package Microsoft.AspNetCore.Http
- **Path**: `src/SauronSheet.Infrastructure/SauronSheet.Infrastructure.csproj`
- **Acción**: Agregar dependencia
- **Descripción**: Agregar el NuGet package `Microsoft.AspNetCore.Http` (última versión estable) al proyecto Infrastructure. Este package proporciona acceso a `IHttpContextAccessor`, `CookieOptions`, y tipos de JWT middleware.
- **Dependencias**: Phase 0 Infrastructure project
- **Validación**: 
  - Ejecutar `dotnet build src/SauronSheet.Infrastructure/SauronSheet.Infrastructure.csproj`
  - Build exitoso sin errores

---

## 10. Infrastructure Layer - Auth Services

### Tarea 10.1 – Crear directorio Auth en Infrastructure
- **Path**: `src/SauronSheet.Infrastructure/Auth/`
- **Acción**: Crear directorio
- **Descripción**: Crear el directorio `Auth` dentro de Infrastructure para alojar los servicios de autenticación.
- **Dependencias**: Phase 0 Infrastructure project
- **Validación**: 
  - Directorio `src/SauronSheet.Infrastructure/Auth/` existe
  - Está vacío

### Tarea 10.2 – Crear archivo SupabaseAuthService.cs
- **Path**: `src/SauronSheet.Infrastructure/Auth/SupabaseAuthService.cs`
- **Acción**: Crear archivo
- **Descripción**: Crear clase `SupabaseAuthService` que implemente `IAuthService` (de Domain). Constructor inyecta `HttpClient`, `string supabaseUrl`, `string supabaseKey`. Implementa los 5 métodos de IAuthService:
  - `RegisterAsync(string email, string password)`: POST `/auth/v1/signup` a Supabase Auth, retornar `AuthResult.Success(...)` o `AuthResult.Failure(...)`
  - `LoginAsync(string email, string password)`: POST `/auth/v1/token?grant_type=password`, retornar JWT tokens en AuthResult
  - `LogoutAsync(string accessToken)`: POST `/auth/v1/logout` con Authorization header
  - `RefreshTokenAsync(string refreshToken)`: POST `/auth/v1/token?grant_type=refresh_token`, retornar nuevos tokens
  - `GetUserProfileAsync(string userId)`: GET `/auth/v1/user`, retornar `UserProfile` o null
  - Mapear respuestas Supabase a domain objects (AuthResult, UserProfile)
  - Manejar errores Supabase (email taken, invalid credentials, etc.)
- **Dependencias**: Tarea 10.1, Domain IAuthService, Domain AuthResult, Domain UserProfile
- **Validación**: 
  - Archivo compila
  - Implementa IAuthService correctamente
  - Maneja HTTP calls a Supabase Auth REST API

### Tarea 10.3 – Crear archivo JwtCookieMiddleware.cs
- **Path**: `src/SauronSheet.Infrastructure/Auth/JwtCookieMiddleware.cs`
- **Acción**: Crear archivo
- **Descripción**: Crear clase middleware `JwtCookieMiddleware` que:
  - Constructor inyecta `RequestDelegate next` y `AuthConfiguration config`
  - Método `async Task InvokeAsync(HttpContext context)`:
    - Leer JWT desde cookie: `context.Request.Cookies[_config.AccessTokenCookieName]`
    - Si existe token: usar `JwtSecurityTokenHandler().ReadJwtToken(token)` para parsear (sin validar firma)
    - Extraer claim "sub" (user ID) del token
    - Si existe "sub": crear `ClaimsPrincipal` con claims "sub" y "email", asignar a `context.User`
    - Pasar al siguiente middleware: `await _next(context)`
  - **Importante**: No validar firma del JWT (Supabase ya lo hace)
- **Dependencias**: Tarea 10.1
- **Validación**: 
  - Archivo compila
  - Usa `System.IdentityModel.Tokens.Jwt` para JWT parsing

### Tarea 10.4 – Crear archivo HttpUserContext.cs
- **Path**: `src/SauronSheet.Infrastructure/Auth/HttpUserContext.cs`
- **Acción**: Crear archivo
- **Descripción**: Crear clase `HttpUserContext` que implemente `IUserContext` (interfaz definida en Application.Common desde Phase 0). Constructor inyecta `IHttpContextAccessor`. Implementa las 2 propiedades:
  - `string UserId`: retornar `_httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value` o lanzar `UnauthorizedAccessException` si nulo
  - `bool IsAuthenticated`: retornar `_httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false`
  - **IMPORTANTE**: IUserContext es una interfaz que DEBE estar definida en `src/SauronSheet.Application/Common/IUserContext.cs` desde Phase 0. Si no existe, crearla antes de esta tarea.
- **Dependencias**: Tarea 10.1, Application IUserContext (Phase 0 o crear si no existe)
- **Validación**: 
  - Archivo compila
  - Implementa IUserContext correctamente
  - IUserContext interface existe en Application.Common

### Tarea 10.5 – Crear archivo AuthConfiguration.cs
- **Path**: `src/SauronSheet.Infrastructure/Auth/AuthConfiguration.cs`
- **Acción**: Crear archivo
- **Descripción**: Crear clase `AuthConfiguration` con propiedades públicas:
  - `string AccessTokenCookieName { get; set; } = "sb-access-token"`
  - `string RefreshTokenCookieName { get; set; } = "sb-refresh-token"`
  - `int AccessTokenExpirationMinutes { get; set; } = 60`
  - `int RefreshTokenExpirationDays { get; set; } = 7`
  - `string JwtSecret { get; set; } = string.Empty`
- **Dependencias**: Tarea 10.1
- **Validación**: 
  - Archivo compila

---

## 11. Infrastructure Layer - Database Migration

### Tarea 11.1 – Crear directorio Migrations en Infrastructure
- **Path**: `src/SauronSheet.Infrastructure/Auth/Migrations/`
- **Acción**: Crear directorio
- **Descripción**: Crear el directorio para alojar scripts SQL de migración de auth.
- **Dependencias**: Tarea 10.1
- **Validación**: 
  - Directorio existe
  - Está vacío

### Tarea 11.2 – Crear archivo SQL migration 001_CreateUsersTable.sql
- **Path**: `src/SauronSheet.Infrastructure/Auth/Migrations/001_CreateUsersTable.sql`
- **Acción**: Crear archivo
- **Descripción**: Crear archivo SQL con la migración para Supabase. Debe contener:
  - `CREATE TABLE IF NOT EXISTS public.users (...)`
    - `id UUID PRIMARY KEY REFERENCES auth.users(id) ON DELETE CASCADE`
    - `email TEXT NOT NULL`
    - `display_name TEXT` (nullable)
    - `created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()`
    - `updated_at TIMESTAMPTZ` (nullable)
  - `CREATE INDEX idx_users_email ON public.users(email)`
  - `ALTER TABLE public.users ENABLE ROW LEVEL SECURITY`
  - `CREATE POLICY "Users can view own profile" ON public.users FOR SELECT USING (auth.uid() = id)`
  - `CREATE POLICY "Users can update own profile" ON public.users FOR UPDATE USING (auth.uid() = id) WITH CHECK (auth.uid() = id)`
  - `CREATE POLICY "Users can insert own profile" ON public.users FOR INSERT WITH CHECK (auth.uid() = id)`
- **Dependencias**: Tarea 11.1
- **Validación**: 
  - Archivo contiene SQL válido
  - Puede ser ejecutado manualmente en Supabase SQL Editor

---

## 12. Infrastructure Layer - DI Updates

### Tarea 12.1 – Actualizar DependencyInjection.cs en Infrastructure (REEMPLAZO completo)
- **Path**: `src/SauronSheet.Infrastructure/DependencyInjection.cs`
- **Acción**: Modificar archivo (reemplazar contenido completo)
- **Descripción**: Reemplazar el contenido completo de `DependencyInjection.cs` para registrar servicios de auth. Si hay contenido existente de Phase 0, asegurar que se mantiene la registración de Supabase client (si la hay). El archivo debe:
  - Usar `using Auth;`, `using Domain.Services;`, `using Application.Common;`
  - Clase estática `DependencyInjection`
  - Método `public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)`
  - Leer y validar `configuration["Supabase:Url"]` (lanzar si null)
  - Leer y validar `configuration["Supabase:Key"]` (lanzar si null)
  - Leer y validar `configuration["Supabase:JwtSecret"]` (lanzar si null)
  - Registrar `AuthConfiguration` via `services.Configure<AuthConfiguration>(...)`
  - Registrar `IAuthService` como `HttpClient` client: `services.AddHttpClient<IAuthService, SupabaseAuthService>()`
  - Registrar `IUserContext` como scoped: `services.AddScoped<IUserContext, HttpUserContext>()`
  - Registrar `IHttpContextAccessor`: `services.AddHttpContextAccessor()`
  - Retornar `services`
  - **NOTA**: Si Phase 0 tenía registraciones adicionales, COMBINAR (no descartar)
- **Dependencias**: Tareas 10.2 a 10.5
- **Validación**: 
  - Archivo compila
  - `dotnet build src/SauronSheet.Infrastructure/` exitoso
  - Todas las dependencias (Supabase client, auth services) registradas correctamente

---

## 13. Frontend Layer - Program.cs Updates

### Tarea 13.1 – Crear directorio Auth en Frontend Pages
- **Path**: `src/SauronSheet.Frontend/Pages/Auth/`
- **Acción**: Crear directorio
- **Descripción**: Crear el directorio para alojar las páginas de autenticación (Login, Register).
- **Dependencias**: Phase 0 Frontend project
- **Validación**: 
  - Directorio existe
  - Está vacío

### Tarea 13.2 – Actualizar Program.cs en Frontend
- **Path**: `src/SauronSheet.Frontend/Program.cs`
- **Acción**: Modificar archivo (actualizar sección de registro de servicios)
- **Descripción**: Actualizar el programa para registrar servicios y middleware de auth:
  - Agregar `using SauronSheet.Infrastructure;` (si no está)
  - En la sección de registro de servicios (antes de `builder.Services.AddRazorPages()`):
    - Agregar `builder.Services.AddHttpContextAccessor();` (para IUserContext)
  - En la sección de middleware (después de `app.UseRouting()`, antes de `app.MapRazorPages()`):
    - Agregar `app.UseMiddleware<JwtCookieMiddleware>();` (para extraer JWT del cookie)
    - Agregar `app.UseAuthentication();` (para establecer User context)
    - Agregar `app.UseAuthorization();` (para autorizar acceso)
  - El orden es **crítico**: Routing → JWT Middleware → Authentication → Authorization → MapRazorPages
- **Dependencias**: Tarea 13.1, Infrastructure DI updates
- **Validación**: 
  - Archivo compila
  - `dotnet build src/SauronSheet.Frontend/` exitoso

### Tarea 13.3 – Actualizar _Layout.cshtml (navegación auth-aware)
- **Path**: `src/SauronSheet.Frontend/Pages/Shared/_Layout.cshtml`
- **Acción**: Modificar archivo (actualizar sección de navegación)
- **Descripción**: Actualizar la barra de navegación para mostrar contenido diferente según autenticación:
  - Si `User?.Identity?.IsAuthenticated == true`:
    - Mostrar link a `/Dashboard`
    - Mostrar email del usuario: `@User.FindFirst("email")?.Value`
    - Mostrar botón Logout que POST a `/Auth/Logout`
  - Si no autenticado:
    - Mostrar link a `/Auth/Login` ("Sign In")
    - Mostrar link a `/Auth/Register` ("Sign Up")
  - Usar Tailwind classes para estilo: `flex justify-between h-16`, botones con `px-4 py-2`
- **Dependencias**: Tarea 13.2
- **Validación**: 
  - Archivo compila como Razor page
  - Lógica condicional correcta
  - Formulario de logout usa POST (no GET)

---

## 14. Frontend Layer - Auth Pages

### Tarea 14.1 – Crear archivo Login.cshtml
- **Path**: `src/SauronSheet.Frontend/Pages/Auth/Login.cshtml`
- **Acción**: Crear archivo
- **Descripción**: Crear página Razor para login con:
  - Formulario con campos `Email` (type=email) y `Password` (type=password)
  - Botón "Sign In"
  - Área de error para mostrar `Model.ErrorMessage`
  - Link a Register: "Don't have an account? Sign up"
  - Campo oculto para `ReturnUrl`
  - Tailwind styling: layout centrado, card with shadow, responsive
- **Dependencias**: Tarea 13.1
- **Validación**: 
  - Archivo compila como Razor template
  - Contiene form con method=post

### Tarea 14.2 – Crear archivo Login.cshtml.cs
- **Path**: `src/SauronSheet.Frontend/Pages/Auth/Login.cshtml.cs`
- **Acción**: Crear archivo
- **Descripción**: Crear PageModel `LoginModel` que:
  - Inyecte `IMediator` en constructor
  - Tenga `[BindProperty] LoginInputModel Input` (propiedades Email, Password)
  - Propiedades: `public string? ErrorMessage`, `public string? ReturnUrl`
  - Método `public void OnGet(string? returnUrl = null)`: asignar `ReturnUrl = returnUrl ?? "/Dashboard"`
  - Método `public async Task<IActionResult> OnPostAsync(...)`: 
    - Validar ModelState
    - Enviar `LoginUserCommand(Input.Email, Input.Password)` via mediator
    - Si success: establecer cookies JWT con `Response.Cookies.Append("sb-access-token", ...)`
      - CookieOptions: HttpOnly=true, Secure=true, SameSite=Strict, Path="/", Expires=expiresAt
    - Si success: también establecer refresh token cookie
    - Si success: `return LocalRedirect(ReturnUrl)` (solo same-origin)
    - Si falla (UnauthorizedAccessException): `ErrorMessage = "Invalid email or password."`, retornar Page()
- **Dependencias**: Tarea 14.1
- **Validación**: 
  - Archivo compila
  - PageModel hereda de PageModel
  - Métodos OnGet y OnPostAsync correctos

### Tarea 14.3 – Crear archivo Register.cshtml
- **Path**: `src/SauronSheet.Frontend/Pages/Auth/Register.cshtml`
- **Acción**: Crear archivo
- **Descripción**: Crear página Razor para registro con:
  - Formulario con campos `Email`, `Password` (minlength=8), `ConfirmPassword`
  - Botón "Create Account"
  - Área de error para `Model.ErrorMessage`
  - Link a Login: "Already have an account? Sign in"
  - Tailwind styling: similar a Login.cshtml
- **Dependencias**: Tarea 13.1
- **Validación**: 
  - Archivo compila
  - Contiene validación client-side (minlength)

### Tarea 14.4 – Crear archivo Register.cshtml.cs
- **Path**: `src/SauronSheet.Frontend/Pages/Auth/Register.cshtml.cs`
- **Acción**: Crear archivo
- **Descripción**: Crear PageModel `RegisterModel` que:
  - Inyecte `IMediator`
  - Tenga `[BindProperty] RegisterInputModel Input` (propiedades Email, Password, ConfirmPassword)
  - Propiedad `public string? ErrorMessage`
  - Método `public async Task<IActionResult> OnPostAsync()`:
    - Validar ModelState
    - Validar Password == ConfirmPassword (si no, `ErrorMessage = "Passwords do not match."`, retornar Page())
    - Enviar `RegisterUserCommand(Input.Email, Input.Password, Input.ConfirmPassword)` via mediator
    - Si success: auto-login enviando `LoginUserCommand(Input.Email, Input.Password)`
    - Si login success: establecer JWT cookies (igual que Login.cshtml.cs)
    - Si login success: `return RedirectToPage("/Dashboard")`
    - Si RegisterUserCommand falla (DomainException): `ErrorMessage = ex.Message`, retornar Page()
- **Dependencias**: Tarea 14.3
- **Validación**: 
  - Archivo compila
  - Implementa tanto Register como auto-login

### Tarea 14.5 – Crear archivo Logout.cshtml
- **Path**: `src/SauronSheet.Frontend/Pages/Auth/Logout.cshtml`
- **Acción**: Crear archivo
- **Descripción**: Crear página Razor placeholder (minimal). Nota: Logout es POST-only, esta página no se renderiza normalmente.
- **Dependencias**: Tarea 13.1
- **Validación**: 
  - Archivo existe (aunque sea placeholder)

### Tarea 14.6 – Crear archivo Logout.cshtml.cs
- **Path**: `src/SauronSheet.Frontend/Pages/Auth/Logout.cshtml.cs`
- **Acción**: Crear archivo
- **Descripción**: Crear PageModel `LogoutModel` que:
  - Inyecte `IMediator`
  - **Nota**: Logout es POST-only (no GET)
  - Método `public async Task<IActionResult> OnPostAsync()`:
    - Leer JWT del cookie: `Request.Cookies["sb-access-token"]`
    - Si existe: enviar `LogoutUserCommand(token)` via mediator
    - Catch cualquier excepción y ignorar (logout puede fallar si sesión ya expiró)
    - Limpiar cookies: `Response.Cookies.Delete("sb-access-token")` y `Response.Cookies.Delete("sb-refresh-token")`
    - `return RedirectToPage("/Auth/Login")`
  - **NO implementar OnGet()** (logout es POST-only para CSRF safety)
- **Dependencias**: Tarea 14.5
- **Validación**: 
  - Archivo compila
  - OnPostAsync retorna IActionResult
  - No hay método OnGet (validación manual si es necesario)

### Tarea 14.7 – Crear archivo Dashboard.cshtml
- **Path**: `src/SauronSheet.Frontend/Pages/Dashboard.cshtml`
- **Acción**: Crear archivo
- **Descripción**: Crear página Razor para dashboard (stub para Phase 1). Debe contener:
  - Heading: "Welcome, {email}"
  - Mensaje placeholder: "Your personal expense tracking dashboard"
  - Mensaje info: "Dashboard features will be available in Phase 4 (Analytics & Dashboard)."
  - Usar Tailwind para styling (cards, spacing)
- **Dependencias**: Tarea 13.1
- **Validación**: 
  - Archivo compila
  - Es una página Razor válida

### Tarea 14.8 – Crear archivo Dashboard.cshtml.cs
- **Path**: `src/SauronSheet.Frontend/Pages/Dashboard.cshtml.cs`
- **Acción**: Crear archivo
- **Descripción**: Crear PageModel `DashboardModel` que:
  - Inyecte `IMediator`
  - Propiedad `public UserProfileDto? UserProfile`
  - **IMPORTANTE**: Esta página DEBE ser protegida. Implementar la protección de una de estas formas:
    - **Opción 1 (Recomendado)**: Agregar atributo `[Authorize]` en la clase PageModel
    - **Opción 2 (Manual)**: En OnGetAsync, verificar `User.Identity?.IsAuthenticated` y retornar RedirectToPage("/Auth/Login") si es false
  - Método `public async Task<IActionResult> OnGetAsync()`:
    - Enviar `GetCurrentUserQuery()` via mediator
    - Si success: asignar a `UserProfile`, retornar Page()
    - Si falla (UnauthorizedAccessException): `return RedirectToPage("/Auth/Login")`
  - **Nota**: TenantScopingBehavior en Application layer garantiza que queries solo tengan acceso a datos del usuario autenticado
- **Dependencias**: Tarea 14.7
- **Validación**: 
  - Archivo compila
  - Usa GetCurrentUserQuery
  - Implementa protección de autenticación (Authorize attribute O validación manual)
  - Unauthenticated requests redirigen a /Auth/Login

---

## 15. Frontend Layer - Configuration

### Tarea 15.1 – Actualizar appsettings.json en Frontend
- **Path**: `src/SauronSheet.Frontend/appsettings.json`
- **Acción**: Modificar archivo
- **Descripción**: Actualizar la sección de Supabase para agregar JwtSecret (obtenido en tarea 1.3). El archivo debe contener:
```json
{
    "Supabase": {
      "Url": "https://your-project.supabase.co",
      "Key": "your-anon-key",
      "JwtSecret": "your-jwt-secret"
    },
    "Auth": {
      "AccessTokenCookieName": "sb-access-token",
      "RefreshTokenCookieName": "sb-refresh-token",
      "AccessTokenExpirationMinutes": 60,
      "RefreshTokenExpirationDays": 7
    },
    "Logging": { ... },
    "AllowedHosts": "*"
  }
```
- **Dependencias**: Tarea 1.3 (obtener JWT Secret)
- **Validación**: 
  - Archivo compila como JSON válido
  - Todas las claves Supabase presentes

---

## 16. Integration & Validation

### Tarea 16.1 – Full solution build
- **Path**: N/A (command line)
- **Acción**: Ejecutar comando
- **Descripción**: Ejecutar `dotnet build` desde raíz de solución para compilar todos los proyectos con cambios de Phase 1.
- **Dependencias**: Todas las tareas anteriores
- **Validación**: 
  - Exit code 0
  - Cero errores
  - Cero warnings (TreatWarningsAsErrors=true)

### Tarea 16.2 – Ejecutar todos los tests
- **Path**: N/A (command line)
- **Acción**: Ejecutar comando
- **Descripción**: Ejecutar `dotnet test` para verificar que todos los tests pasan (Phase 0 + Phase 1).
- **Dependencias**: Tarea 16.1
- **Validación**: 
  - Exit code 0
  - Output contiene "35 passed" (11 Phase 0 Domain + 8 Phase 1 Domain + 2 Phase 0 Application + 14 Phase 1 Application)

### Tarea 16.3 – Verificar Domain coverage >= 80%
- **Path**: N/A (coverage report)
- **Acción**: Generar reporte
- **Descripción**: Usar coverlet para generar reporte de cobertura del Domain layer. Este comando se ejecuta DESPUÉS de `dotnet build` (tarea 16.1):
```
dotnet tool install -g coverlet.console
coverlet tests/SauronSheet.Domain.Tests/bin/Debug/net10.0/SauronSheet.Domain.Tests.dll --target "dotnet" --targetargs "test tests/SauronSheet.Domain.Tests/SauronSheet.Domain.Tests.dll --no-build" --format "opencover" --output "./coverage-domain.xml" --include "[SauronSheet.Domain]*" --exclude "[SauronSheet.Domain.Tests]*"
```
Alternativamente, usar ReportGenerator para generar un reporte HTML más legible.
- **Dependencias**: Tarea 16.1 (debe ejecutarse DESPUÉS de build para que existan los .dll)
- **Validación**: 
  - coverage-domain.xml generado en raíz de solución
  - Domain layer coverage >= 80%
  - Archivos reportados incluyen UserId, AuthResult, UserProfile, IAuthService

### Tarea 16.4 – Verificar Application coverage >= 70%
- **Path**: N/A (coverage report)
- **Acción**: Generar reporte
- **Descripción**: Usar coverlet para generar reporte de cobertura del Application layer. Similar a tarea 16.3 pero para Application. Ejecutar DESPUÉS de `dotnet build`:
```
coverlet tests/SauronSheet.Application.Tests/bin/Debug/net10.0/SauronSheet.Application.Tests.dll --target "dotnet" --targetargs "test tests/SauronSheet.Application.Tests/SauronSheet.Application.Tests.dll --no-build" --format "opencover" --output "./coverage-app.xml" --include "[SauronSheet.Application]*" --exclude "[SauronSheet.Application.Tests]*"
```
- **Dependencias**: Tarea 16.1 (debe ejecutarse DESPUÉS de build)
- **Validación**: 
  - coverage-app.xml generado en raíz de solución
  - Application layer coverage >= 70%
  - Handlers, DTOs, y behaviors incluidos en reporte

### Tarea 16.5 – Verificar dependencias (Domain = 0)
- **Path**: `src/SauronSheet.Domain/SauronSheet.Domain.csproj`
- **Acción**: Auditar
- **Descripción**: Verificar que Domain.csproj tiene CERO `<ProjectReference>` y CERO `<PackageReference>`. Comando:
```
grep -E "ProjectReference|PackageReference" src/SauronSheet.Domain/SauronSheet.Domain.csproj
```
  Debe retornar vacío.
- **Dependencias**: Tarea 16.1
- **Validación**: 
  - No hay líneas con ProjectReference
  - No hay líneas con PackageReference

### Tarea 16.6 – Verificar dependencias (Application -> Domain only)
- **Path**: `src/SauronSheet.Application/SauronSheet.Application.csproj`
- **Acción**: Auditar
- **Descripción**: Verificar que Application.csproj referencia SOLO Domain (y MediatR packages). Comando:
```
grep "ProjectReference" src/SauronSheet.Application/SauronSheet.Application.csproj | wc -l
```
  Debe retornar 1 (solo Domain).
- **Dependencias**: Tarea 16.1
- **Validación**: 
  - ProjectReference count == 1
  - La referencia es a Domain

### Tarea 16.7 – Verificar dependencias (Infrastructure -> Domain only)
- **Path**: `src/SauronSheet.Infrastructure/SauronSheet.Infrastructure.csproj`
- **Acción**: Auditar
- **Descripción**: Verificar que Infrastructure.csproj referencia SOLO Domain (no Application ni Frontend).
- **Dependencias**: Tarea 16.1
- **Validación**: 
  - ProjectReference count == 1
  - La referencia es a Domain

### Tarea 16.8 – Verificar dependencias (Frontend -> App + Infra)
- **Path**: `src/SauronSheet.Frontend/SauronSheet.Frontend.csproj`
- **Acción**: Auditar
- **Descripción**: Verificar que Frontend.csproj referencia Application e Infrastructure (exactamente 2 ProjectReferences).
- **Dependencias**: Tarea 16.1
- **Validación**: 
  - ProjectReference count == 2
  - Referencias son a Application e Infrastructure

### Tarea 16.9 – Aplicar migración 001_CreateUsersTable.sql
- **Path**: N/A (Supabase dashboard)
- **Acción**: Ejecutar SQL
- **Descripción**: En el dashboard de Supabase, navegar a SQL Editor, crear una nueva query, copiar el contenido de `src/SauronSheet.Infrastructure/Auth/Migrations/001_CreateUsersTable.sql`, y ejecutarla. Esto crea la tabla `users` en Supabase con políticas de RLS.
- **Dependencias**: Tarea 11.2, Supabase project activo
- **Validación**: 
  - Tabla `users` visible en Supabase dashboard (Tables view)
  - 5 columnas presentes: id, email, display_name, created_at, updated_at
  - RLS habilitado
  - 3 políticas de RLS creadas (view, update, insert)

### Tarea 16.10 – Manual E2E: Registrar nuevo usuario
- **Path**: N/A (manual testing en navegador)
- **Acción**: Probar
- **Descripción**: 
  1. Ejecutar `dotnet run --project src/SauronSheet.Frontend/`
  2. Navegar a http://localhost:5000/Auth/Register
  3. Llenar formulario: email=test@example.com, password=securepass123, confirm=securepass123
  4. Click "Create Account"
  5. Esperado: redirigir a /Dashboard, ver "Welcome, test@example.com"
  6. Verificar en navegador DevTools (F12) que cookie "sb-access-token" existe con flags HttpOnly, Secure, SameSite=Strict
  7. Verificar en Supabase dashboard: tabla `users` contiene nuevo registro con email y user_id
- **Dependencias**: Tarea 16.9
- **Validación**: 
  - Registro exitoso sin errores
  - Cookie JWT establecido
  - Perfil de usuario creado en `users` table

### Tarea 16.11 – Manual E2E: Login con usuario existente
- **Path**: N/A (manual testing en navegador)
- **Acción**: Probar
- **Descripción**: 
  1. Logout del usuario anterior (si está logged in)
  2. Navegar a http://localhost:5000/Auth/Login
  3. Llenar: email=test@example.com, password=securepass123
  4. Click "Sign In"
  5. Esperado: redirigir a /Dashboard, ver "Welcome, test@example.com"
  6. Verificar cookie JWT en DevTools
- **Dependencias**: Tarea 16.10
- **Validación**: 
  - Login exitoso
  - Dashboard visible
  - Cookie JWT presente

### Tarea 16.12 – Manual E2E: Logout
- **Path**: N/A (manual testing en navegador)
- **Acción**: Probar
- **Descripción**: 
  1. Desde dashboard, click en botón "Logout" en navegación
  2. Esperado: redirigir a /Auth/Login
  3. Verificar en DevTools que cookies "sb-access-token" y "sb-refresh-token" están vaciadas/expiradas
  4. Intentar navegar directamente a /Dashboard
  5. Esperado: redirigir a /Auth/Login?returnUrl=/Dashboard
- **Dependencias**: Tarea 16.11
- **Validación**: 
  - Logout exitoso
  - Cookies limpiadas
  - Redirect a /Dashboard requiere login

### Tarea 16.13 – Manual E2E: Validación de formularios
- **Path**: N/A (manual testing en navegador)
- **Acción**: Probar
- **Descripción**: 
  1. Navegar a /Auth/Register
  2. Intentar register con email vacío → error validation
  3. Intentar register con password < 8 caracteres → error "Password must be at least 8 characters"
  4. Intentar register con contraseñas no coincidentes → error "Passwords do not match"
  5. Intentar register con email ya existente → error "Email is already registered"
  6. Navegar a /Auth/Login
  7. Intentar login con credenciales inválidas → error "Invalid email or password" (genérico, sin enumeration)
- **Dependencias**: Tarea 16.10
- **Validación**: 
  - Todos los errores mostrados correctamente
  - Mensajes son seguros (no revelan info sobre usuarios existentes)

### Tarea 16.14 – Manual E2E: Tenant isolation
- **Path**: N/A (manual testing en navegador)
- **Acción**: Probar
- **Descripción**: 
  1. En navegador incógnito, registrar usuario diferente: email=other@example.com, password=otherpass123
  2. En ambas sesiones, navegar a /Dashboard
  3. Verificar que cada usuario ve "Welcome, [su-email]"
  4. Verificar que los datos mostrados son independientes (cada usuario ve solo sus propios datos)
  5. No hay forma de que User A vea data de User B desde la UI
- **Dependencias**: Tarea 16.10
- **Validación**: 
  - Aislamiento de tenant verificado
  - Cada usuario ve sus propios datos

---

## Orden de Implementación

1. Tarea 1.0 – Verificar que IUserContext existe (crear si no existe)
2. Tarea 1.1 – Validar Phase 0 completado
3. Tarea 1.2 – Verificar Supabase Auth habilitado
4. Tarea 1.3 – Obtener JWT Secret
5. Tarea 2.1 – Crear estructura Domain
6. Tarea 2.2 – Crear UserId.cs (schema)
7. Tarea 2.3 – Crear AuthResult.cs (schema)
8. Tarea 2.4 – Crear UserProfile.cs (schema)
9. Tarea 2.5 – Crear IAuthService.cs (schema)
10. Tarea 3.1 – Crear estructura Domain.Tests
11. Tarea 3.2 – Crear UserIdTests.cs (RED)
12. Tarea 3.3 – Crear AuthResultTests.cs (RED)
13. Tarea 3.4 – Ejecutar tests Domain RED
14. Tarea 4.1 – Implementar UserId.cs (GREEN)
15. Tarea 4.2 – Implementar AuthResult.cs (GREEN)
16. Tarea 4.3 – Completar UserProfile.cs (GREEN)
17. Tarea 4.4 – Ejecutar tests Domain GREEN
18. Tarea 5.1 – Crear estructura Application.Tests
19. Tarea 5.2 – Crear RegisterUserCommandTests.cs (RED)
20. Tarea 5.3 – Crear LoginUserCommandTests.cs (RED)
21. Tarea 5.4 – Crear LogoutUserCommandTests.cs (RED)
22. Tarea 5.5 – Crear RefreshTokenCommandTests.cs (RED)
23. Tarea 5.6 – Crear GetCurrentUserQueryTests.cs (RED)
24. Tarea 5.7 – Crear TenantScopingBehaviorTests.cs (RED)
25. Tarea 5.8 – Ejecutar tests Application RED
26. Tarea 6.1 – Crear estructura Application
27. Tarea 6.2 – Crear IAnonymousRequest.cs
28. Tarea 6.3 – Crear RegisterUserCommand.cs
29. Tarea 6.4 – Crear LoginUserCommand.cs
30. Tarea 6.5 – Crear LogoutUserCommand.cs
31. Tarea 6.6 – Crear RefreshTokenCommand.cs
32. Tarea 6.7 – Crear GetCurrentUserQuery.cs
33. Tarea 6.8 – Crear RegistrationResultDto.cs
34. Tarea 6.9 – Crear AuthTokenDto.cs
35. Tarea 6.10 – Crear UserProfileDto.cs
36. Tarea 6.11 – Crear RegisterUserCommandHandler.cs (GREEN)
37. Tarea 6.12 – Crear LoginUserCommandHandler.cs (GREEN)
38. Tarea 6.13 – Crear LogoutUserCommandHandler.cs (GREEN)
39. Tarea 6.14 – Crear RefreshTokenCommandHandler.cs (GREEN)
40. Tarea 6.15 – Ejecutar tests Application Commands GREEN
41. Tarea 7.1 – Crear GetCurrentUserQueryHandler.cs (GREEN)
42. Tarea 7.2 – Crear TenantScopingBehavior.cs (GREEN)
43. Tarea 7.3 – Crear directorio Behaviors
44. Tarea 7.4 – Ejecutar tests Application GREEN
45. Tarea 8.1 – Actualizar DependencyInjection.cs (Application)
46. Tarea 9.1 – Agregar Microsoft.AspNetCore.Http NuGet
47. Tarea 10.1 – Crear directorio Auth (Infrastructure)
48. Tarea 10.2 – Crear SupabaseAuthService.cs
49. Tarea 10.3 – Crear JwtCookieMiddleware.cs
50. Tarea 10.4 – Crear HttpUserContext.cs
51. Tarea 10.5 – Crear AuthConfiguration.cs
52. Tarea 11.1 – Crear directorio Migrations
53. Tarea 11.2 – Crear 001_CreateUsersTable.sql
54. Tarea 12.1 – Actualizar DependencyInjection.cs (Infrastructure)
55. Tarea 13.1 – Crear directorio Auth (Frontend Pages)
56. Tarea 13.2 – Actualizar Program.cs (Frontend)
57. Tarea 13.3 – Actualizar _Layout.cshtml
58. Tarea 14.1 – Crear Login.cshtml
59. Tarea 14.2 – Crear Login.cshtml.cs
60. Tarea 14.3 – Crear Register.cshtml
61. Tarea 14.4 – Crear Register.cshtml.cs
62. Tarea 14.5 – Crear Logout.cshtml
63. Tarea 14.6 – Crear Logout.cshtml.cs
64. Tarea 14.7 – Crear Dashboard.cshtml
65. Tarea 14.8 – Crear Dashboard.cshtml.cs
66. Tarea 15.1 – Actualizar appsettings.json (Frontend)
67. Tarea 16.1 – Full solution build
68. Tarea 16.2 – Ejecutar todos los tests
69. Tarea 16.3 – Verificar Domain coverage >= 80%
70. Tarea 16.4 – Verificar Application coverage >= 70%
71. Tarea 16.5 – Verificar dependencias (Domain = 0)
72. Tarea 16.6 – Verificar dependencias (Application -> Domain)
73. Tarea 16.7 – Verificar dependencias (Infrastructure -> Domain)
74. Tarea 16.8 – Verificar dependencias (Frontend -> App+Infra)
75. Tarea 16.9 – Aplicar migración SQL en Supabase
76. Tarea 16.10 – Manual E2E: Registrar usuario
77. Tarea 16.11 – Manual E2E: Login usuario
78. Tarea 16.12 – Manual E2E: Logout usuario
79. Tarea 16.13 – Manual E2E: Validación formularios
80. Tarea 16.14 – Manual E2E: Tenant isolation

---

**Total Tasks: 80**  
**Estimated Duration: 10 days (Weeks 3–5)**  
**Status**: Ready for implementation ✅
