# Phase 0 Tasks

## 1. Setup de solución

### Tarea 1.1 – Crear archivo de solución SauronSheet.sln
- **Path**: `SauronSheet.sln`
- **Acción**: Crear archivo
- **Descripción**: Crear solución .NET 10 vacía que actuará como contenedor de los 6 proyectos. Ejecutar `dotnet new sln --name SauronSheet`.
- **Dependencias**: Ninguna (primera tarea)
- **Validación**: Archivo existe, contiene `<Solution>` root element, sin proyectos aún listados

### Tarea 1.2 – Crear directorios src y tests
- **Path**: `src/`, `tests/`
- **Acción**: Crear directorios
- **Descripción**: Crear estructura de carpetas para organizar los 6 proyectos. `src/` contiene 4 proyectos (Domain, Application, Infrastructure, Frontend); `tests/` contiene 2 proyectos (Domain.Tests, Application.Tests).
- **Dependencias**: Tarea 1.1
- **Validación**: Ambos directorios existen y están vacíos

### Tarea 1.3 – Crear archivo global.json
- **Path**: `global.json`
- **Acción**: Crear archivo
- **Descripción**: Configurar SDK de .NET pinning version a 10.0.100 con rollForward latestMinor. Contenido: `{"sdk": {"version": "10.0.100", "rollForward": "latestMinor"}}`.
- **Dependencias**: Tarea 1.2
- **Validación**: Archivo existe, contiene estructura JSON válida, version y rollForward presentes

### Tarea 1.4 – Crear archivo Directory.Build.props
- **Path**: `Directory.Build.props`
- **Acción**: Crear archivo
- **Descripción**: Configurar propiedades de compilación a nivel de solución: `<TargetFramework>net10.0</TargetFramework>`, `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`, `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`, `<LangVersion>latest</LangVersion>`.
- **Dependencias**: Tarea 1.2
- **Validación**: Archivo existe, todas las PropertyGroup presentes, parse como XML válido

### Tarea 1.5 – Crear archivo .gitignore
- **Path**: `.gitignore`
- **Acción**: Crear archivo
- **Descripción**: Agregar ignores estándar de .NET: `bin/`, `obj/`, `.vs/`, `.vscode/`, `*.nupkg`, `TestResults/`, `*.trx`, `*.coverage`, `*.user`, `*.suo`, `.DS_Store`.
- **Dependencias**: Tarea 1.2
- **Validación**: Archivo existe, contiene patrones de ignore para .NET

---

## 2. Domain Layer – Projects & Base Abstractions

### Tarea 2.1 – Crear proyecto SauronSheet.Domain
- **Path**: `src/SauronSheet.Domain/SauronSheet.Domain.csproj`
- **Acción**: Crear proyecto
- **Descripción**: Crear proyecto class library .NET 10 para capa de dominio. Ejecutar `dotnet new classlib --name SauronSheet.Domain --framework net10.0` en `src/`, luego `dotnet sln add src/SauronSheet.Domain/SauronSheet.Domain.csproj`. Eliminar `Class1.cs` si se crea automáticamente.
- **Dependencias**: Tarea 1.4
- **Validación**: Archivo .csproj existe, contiene `<TargetFramework>net10.0</TargetFramework>`, se puede agregar a solución sin errores, `dotnet build src/SauronSheet.Domain/SauronSheet.Domain.csproj` pasa

### Tarea 2.2 – Verificar que Domain.csproj tiene cero dependencias
- **Path**: `src/SauronSheet.Domain/SauronSheet.Domain.csproj`
- **Acción**: Validar
- **Descripción**: Auditar que Domain.csproj **NO contiene ningún `<ProjectReference>` ni `<PackageReference>`**. Esto es mandatorio por Constitución (Clean Architecture: Domain MUST have zero external dependencies).
- **Dependencias**: Tarea 2.1
- **Validación**: `grep -E "ProjectReference|PackageReference" src/SauronSheet.Domain/SauronSheet.Domain.csproj` retorna empty o sin coincidencias. Comando de validación manual (no grep en sistema): inspeccionar .csproj manualmente, confirmar que NO hay `<ItemGroup>` con dependencias

### Tarea 2.3 – Crear estructura de carpetas Domain
- **Path**: `src/SauronSheet.Domain/`
- **Acción**: Crear directorios
- **Descripción**: Crear estructura de carpetas dentro de Domain: `Common/`, `Exceptions/`, `Repositories/`.
- **Dependencias**: Tarea 2.1
- **Validación**: Todos los directorios existen y están vacíos

### Tarea 2.4 – Crear clase base Entity<TId>
- **Path**: `src/SauronSheet.Domain/Common/Entity.cs`
- **Acción**: Crear archivo
- **Descripción**: Crear clase abstracta genérica `Entity<TId>` que implemente:
  - Propiedad `TId Id` (set protected)
  - Propiedad `DateTime CreatedAt` (set protected, inicializar en constructor con UtcNow)
  - Propiedad `DateTime? UpdatedAt` (set protected, inicializar null)
  - Constraint: `where TId : notnull`
  - Proteger ID nulo/vacío en constructor (lanzar ArgumentException si default(TId))
  - Métodos de igualdad: `Equals(object)`, `GetHashCode()`, operadores `==` y `!=`
  - Igualdad basada en tipo + Id
- **Dependencias**: Tarea 2.3
- **Validación**: Archivo compila, no hay errores de sintaxis, genéricos se resuelven correctamente

### Tarea 2.5 – Crear clase base AggregateRoot<TId>
- **Path**: `src/SauronSheet.Domain/Common/AggregateRoot.cs`
- **Acción**: Crear archivo
- **Descripción**: Crear clase abstracta `AggregateRoot<TId>` que herede de `Entity<TId>`. Incluir comentario TODO: `// TODO: Add domain events collection in future phase`. Constructor protegido que llama `base(id)`.
- **Dependencias**: Tarea 2.4
- **Validación**: Archivo compila, hereda correctamente, TODO comment presente

### Tarea 2.6 – Crear clase base ValueObject
- **Path**: `src/SauronSheet.Domain/Common/ValueObject.cs`
- **Acción**: Crear archivo
- **Descripción**: Crear `abstract record ValueObject`. C# records proporcionan igualdad basada en valores automáticamente. Sin implementación adicional requerida en Phase 0.
- **Dependencias**: Tarea 2.3
- **Validación**: Archivo compila, es un record abstracto

### Tarea 2.7 – Crear excepción base DomainException
- **Path**: `src/SauronSheet.Domain/Exceptions/DomainException.cs`
- **Acción**: Crear archivo
- **Descripción**: Crear clase `DomainException : Exception`. Dos constructores: `(string message)` y `(string message, Exception innerException)`.
- **Dependencias**: Tarea 2.3
- **Validación**: Archivo compila, ambos constructores presentes, hereda de Exception

### Tarea 2.8 – Crear excepción EntityNotFoundException
- **Path**: `src/SauronSheet.Domain/Exceptions/EntityNotFoundException.cs`
- **Acción**: Crear archivo
- **Descripción**: Crear clase `EntityNotFoundException : DomainException`. Constructor: `(string entityName, object entityId)`. Propiedades públicas: `EntityName`, `EntityId`. Mensaje formateado: `$"Entity '{entityName}' with id '{entityId}' was not found."`.
- **Dependencias**: Tarea 2.7
- **Validación**: Archivo compila, hereda de DomainException, formato de mensaje correcto

### Tarea 2.9 – Crear interfaz ISpecification<T>
- **Path**: `src/SauronSheet.Domain/Repositories/ISpecification.cs`
- **Acción**: Crear archivo
- **Descripción**: Crear interfaz genérica `ISpecification<T>` con:
  - Propiedad: `Expression<Func<T, bool>> Criteria { get; }`
  - Propiedad con default implementation: `int MaxResults => 1000;`
  - Propiedad: `List<Expression<Func<T, object>>> Includes { get; }`
  - Propiedad: `List<string> IncludeStrings { get; }`
- **Dependencias**: Tarea 2.3
- **Validación**: Archivo compila, default interface implementation válida, MaxResults retorna 1000

---

## 3. Application Layer – Project & DI Setup

### Tarea 3.1 – Crear proyecto SauronSheet.Application
- **Path**: `src/SauronSheet.Application/SauronSheet.Application.csproj`
- **Acción**: Crear proyecto
- **Descripción**: Crear proyecto class library .NET 10. Ejecutar `dotnet new classlib --name SauronSheet.Application --framework net10.0` en `src/`, luego `dotnet sln add`. Agregar referencia a Domain: `dotnet add src/SauronSheet.Application/SauronSheet.Application.csproj reference src/SauronSheet.Domain/SauronSheet.Domain.csproj`.
- **Dependencias**: Tarea 2.2 (Domain completo)
- **Validación**: Proyecto existe, compila, referencia a Domain presente en .csproj, `dotnet build` pasa

### Tarea 3.2 – Agregar NuGet package MediatR a Application
- **Path**: `src/SauronSheet.Application/SauronSheet.Application.csproj`
- **Acción**: Agregar dependencia
- **Descripción**: Agregar paquetes NuGet a Application: `MediatR` (12+) y `MediatR.Extensions.Microsoft.DependencyInjection`. Usar comando: `dotnet add src/SauronSheet.Application/SauronSheet.Application.csproj package MediatR` y `dotnet add src/SauronSheet.Application/SauronSheet.Application.csproj package MediatR.Extensions.Microsoft.DependencyInjection`.
- **Dependencias**: Tarea 3.1
- **Validación**: Ambos paquetes listados en .csproj, `dotnet build` pasa, MediatR se puede resolver en Program.cs

### Tarea 3.3 – Crear interfaz IUserContext en Application.Common
- **Path**: `src/SauronSheet.Application/Common/IUserContext.cs`
- **Acción**: Crear archivo
- **Descripción**: Crear directorio `src/SauronSheet.Application/Common/` si no existe. Crear interfaz `IUserContext` con dos propiedades públicas:
  - `string UserId { get; }`
  - `bool IsAuthenticated { get; }`
- **Dependencias**: Tarea 3.1
- **Validación**: Archivo compila, interfaz tiene dos propiedades

### Tarea 3.4 – Crear clase DependencyInjection.cs en Application
- **Path**: `src/SauronSheet.Application/DependencyInjection.cs`
- **Acción**: Crear archivo
- **Descripción**: Crear clase estática `DependencyInjection` con método de extensión `public static IServiceCollection AddApplicationServices(this IServiceCollection services)`. Dentro: llamar `services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));` y retornar `services`.
- **Dependencias**: Tarea 3.2
- **Validación**: Archivo compila, método es estático, se puede invocar como extensión, MediatR se registra

---

## 4. Infrastructure Layer – Project & Config Setup

### Tarea 4.1 – Crear proyecto SauronSheet.Infrastructure
- **Path**: `src/SauronSheet.Infrastructure/SauronSheet.Infrastructure.csproj`
- **Acción**: Crear proyecto
- **Descripción**: Crear proyecto class library .NET 10. Ejecutar `dotnet new classlib --name SauronSheet.Infrastructure --framework net10.0` en `src/`, luego `dotnet sln add`. Agregar referencia a Domain: `dotnet add src/SauronSheet.Infrastructure/SauronSheet.Infrastructure.csproj reference src/SauronSheet.Domain/SauronSheet.Domain.csproj`.
- **Dependencias**: Tarea 2.2 (Domain completo)
- **Validación**: Proyecto existe, compila, referencia a Domain presente, solo Domain (no Application)

### Tarea 4.2 – Agregar NuGet package supabase-csharp a Infrastructure
- **Path**: `src/SauronSheet.Infrastructure/SauronSheet.Infrastructure.csproj`
- **Acción**: Agregar dependencia
- **Descripción**: Agregar paquete NuGet `supabase-csharp` (última versión estable 1.0.0 o posterior). Comando: `dotnet add src/SauronSheet.Infrastructure/SauronSheet.Infrastructure.csproj package supabase-csharp`.
- **Dependencias**: Tarea 4.1
- **Validación**: Paquete listado en .csproj, `dotnet build` pasa

### Tarea 4.3 – Crear clase DependencyInjection.cs en Infrastructure
- **Path**: `src/SauronSheet.Infrastructure/DependencyInjection.cs`
- **Acción**: Crear archivo
- **Descripción**: Crear clase estática `DependencyInjection` con método `public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)`. Dentro:
  - Leer `configuration["Supabase:Url"]` → si null/empty, lanzar `InvalidOperationException("Configuration key 'Supabase:Url' is not set.")`
  - Leer `configuration["Supabase:Key"]` → si null/empty, lanzar `InvalidOperationException("Configuration key 'Supabase:Key' is not set.")`
  - Validación ocurre en tiempo de registro de DI (no deferred)
  - Retornar `services`
  - NO registrar cliente Supabase aún (comentar con TODO)
- **Dependencias**: Tarea 4.1
- **Validación**: Archivo compila, método valida config on startup, excepciones descriptivas

---

## 5. Frontend Layer – Project & Startup

### Tarea 5.1 – Crear proyecto SauronSheet.Frontend
- **Path**: `src/SauronSheet.Frontend/SauronSheet.Frontend.csproj`
- **Acción**: Crear proyecto
- **Descripción**: Crear proyecto Razor Pages .NET 10 (ASP.NET Core web app). Ejecutar `dotnet new webapp --name SauronSheet.Frontend --framework net10.0` en `src/`, luego `dotnet sln add`. Agregar referencias: `dotnet add src/SauronSheet.Frontend/SauronSheet.Frontend.csproj reference src/SauronSheet.Application/SauronSheet.Application.csproj` y `dotnet add src/SauronSheet.Frontend/SauronSheet.Frontend.csproj reference src/SauronSheet.Infrastructure/SauronSheet.Infrastructure.csproj`.
- **Dependencias**: Tarea 3.4 (Application completo), Tarea 4.1 (Infrastructure completo)
- **Validación**: Proyecto existe, compila, referencias a Application y Infrastructure presentes, no referencia Domain directamente

### Tarea 5.2 – Actualizar Program.cs en Frontend
- **Path**: `src/SauronSheet.Frontend/Program.cs`
- **Acción**: Modificar archivo
- **Descripción**: Reemplazar contenido existente. Agregar `using SauronSheet.Application; using SauronSheet.Infrastructure;`. En startup:
  - `builder.Services.AddApplicationServices();`
  - `builder.Services.AddInfrastructureServices(builder.Configuration);`
  - `builder.Services.AddRazorPages();`
  - Mantener middleware estándar (UseHttpsRedirection, UseStaticFiles, UseRouting, MapRazorPages, Run)
  - Auth middleware comentado (preparado para Phase 1)
- **Dependencias**: Tarea 5.1
- **Validación**: Archivo compila, DI registrations presentes, middleware order correcto

### Tarea 5.3 – Crear archivo appsettings.json en Frontend
- **Path**: `src/SauronSheet.Frontend/appsettings.json`
- **Acción**: Crear archivo
- **Descripción**: Crear configuración JSON con placeholders Supabase:
```json
{
    "Supabase": {
      "Url": "https://your-project.supabase.co",
      "Key": "your-anon-key"
    },
    "Logging": {
      "LogLevel": {
        "Default": "Information",
        "Microsoft.AspNetCore": "Warning"
      }
    },
    "AllowedHosts": "*"
  }
```
- **Dependencias**: Tarea 5.1
- **Validación**: Archivo existe, JSON válido, claves Supabase presentes como placeholders

### Tarea 5.4 – Crear archivo appsettings.Development.json en Frontend
- **Path**: `src/SauronSheet.Frontend/appsettings.Development.json`
- **Acción**: Crear archivo
- **Descripción**: Crear configuración de desarrollo con LogLevel más verboso (Debug).
- **Dependencias**: Tarea 5.3
- **Validación**: Archivo existe, JSON válido

### Tarea 5.5 – Actualizar _Layout.cshtml en Frontend
- **Path**: `src/SauronSheet.Frontend/Pages/Shared/_Layout.cshtml`
- **Acción**: Modificar archivo
- **Descripción**: Actualizar layout existente:
  - Agregar `<script src="https://cdn.tailwindcss.com"></script>` en `<head>`
  - Crear navbar con clase `bg-white shadow` conteniendo logo "SauronSheet" (h1 text-2xl font-bold text-blue-600)
  - Agregar placeholder nav links (Home)
  - Mantener `@RenderBody()` en main
  - Agregar footer con texto copyright (placeholder version)
  - Aplicar Tailwind classes para responsive design (max-w-7xl mx-auto px-4)
- **Dependencias**: Tarea 5.1
- **Validación**: Archivo compila como Razor, Tailwind CDN script presente, layout structure correcto

### Tarea 5.6 – Actualizar Index.cshtml en Frontend
- **Path**: `src/SauronSheet.Frontend/Pages/Index.cshtml`
- **Acción**: Modificar archivo
- **Descripción**: Reemplazar contenido de página de inicio:
  - Crear layout centrado (flex, min-h-screen)
  - Mostrar heading "SauronSheet" (h1 text-4xl font-bold)
  - Mostrar badge verde: "System OK" con texto "Foundation Phase 0 is running successfully"
  - Mostrar caja de información con fecha/hora actual (DateTime.UtcNow format yyyy-MM-dd HH:mm:ss UTC)
  - Aplicar Tailwind classes para estilo (bg-green-100, border-l-4, border-green-500, text-green-700, etc.)
- **Dependencias**: Tarea 5.5
- **Validación**: Página compila, muestra contenido esperado, Tailwind clases aplicadas

### Tarea 5.7 – Actualizar Index.cshtml.cs en Frontend
- **Path**: `src/SauronSheet.Frontend/Pages/Index.cshtml.cs`
- **Acción**: Modificar archivo
- **Descripción**: Reemplazar contenido. Crear clase `IndexModel : PageModel` con método `public void OnGet()` vacío. Sin lógica MediatR en Phase 0 (solo health check).
- **Dependencias**: Tarea 5.6
- **Validación**: Archivo compila, PageModel estructura correcta

---

## 6. Testing Layer – Projects & Base Setup

### Tarea 6.1 – Crear proyecto SauronSheet.Domain.Tests
- **Path**: `tests/SauronSheet.Domain.Tests/SauronSheet.Domain.Tests.csproj`
- **Acción**: Crear proyecto
- **Descripción**: Crear proyecto xUnit .NET 10. Ejecutar `dotnet new xunit --name SauronSheet.Domain.Tests --framework net10.0` en `tests/`, luego `dotnet sln add`. Agregar referencia a Domain: `dotnet add tests/SauronSheet.Domain.Tests/SauronSheet.Domain.Tests.csproj reference src/SauronSheet.Domain/SauronSheet.Domain.csproj`.
- **Dependencias**: Tarea 2.2 (Domain completo)
- **Validación**: Proyecto existe, contiene xunit, Moq (si está en template), compila

### Tarea 6.2 – Crear proyecto SauronSheet.Application.Tests
- **Path**: `tests/SauronSheet.Application.Tests/SauronSheet.Application.Tests.csproj`
- **Acción**: Crear proyecto
- **Descripción**: Crear proyecto xUnit .NET 10. Ejecutar `dotnet new xunit --name SauronSheet.Application.Tests --framework net10.0` en `tests/`, luego `dotnet sln add`. Agregar referencias: `dotnet add tests/SauronSheet.Application.Tests/SauronSheet.Application.Tests.csproj reference src/SauronSheet.Application/SauronSheet.Application.csproj` y `dotnet add tests/SauronSheet.Application.Tests/SauronSheet.Application.Tests.csproj reference src/SauronSheet.Domain/SauronSheet.Domain.csproj`.
- **Dependencias**: Tarea 3.4 (Application completo), Tarea 2.2 (Domain completo)
- **Validación**: Proyecto existe, referencias correctas, compila

### Tarea 6.3 – Crear estructura de carpetas en Domain.Tests
- **Path**: `tests/SauronSheet.Domain.Tests/`
- **Acción**: Crear directorios
- **Descripción**: Crear carpetas: `Common/`, `Exceptions/`, `Repositories/`.
- **Dependencias**: Tarea 6.1
- **Validación**: Todos los directorios existen, vacíos

### Tarea 6.4 – Crear estructura de carpetas en Application.Tests
- **Path**: `tests/SauronSheet.Application.Tests/`
- **Acción**: Crear directorios
- **Descripción**: Crear carpeta: `Common/`.
- **Dependencias**: Tarea 6.2
- **Validación**: Directorio existe, vacío

---

## 7. Domain Tests – RED Phase (Test Stubs)

### Tarea 7.1 – Crear test stub para Entity<TId>
- **Path**: `tests/SauronSheet.Domain.Tests/Common/EntityBaseTests.cs`
- **Acción**: Crear archivo
- **Descripción**: Crear clase `EntityBaseTests` con 4 métodos de prueba (todos con `Assert.True(false, "message")` - RED phase):
  - `Test_Entity_SetsCreatedAtOnConstruction()`
  - `Test_Entity_EqualityByIdAndType()`
  - `Test_Entity_InequalityByDifferentId()`
  - `Test_Entity_InequalityByDifferentType()`
  - Cada método tiene atributo `[Fact]` y `[Trait("Category", "Domain")]`
- **Dependencias**: Tarea 6.3
- **Validación**: Archivo compila, 4 tests descubiertos por xUnit, todos fallan

### Tarea 7.2 – Crear test stub para ValueObject
- **Path**: `tests/SauronSheet.Domain.Tests/Common/ValueObjectBaseTests.cs`
- **Acción**: Crear archivo
- **Descripción**: Crear clase `ValueObjectBaseTests` con 2 métodos (RED phase):
  - `Test_ValueObject_EqualityByProperties()`
  - `Test_ValueObject_InequalityByDifferentProperties()`
  - Cada método tiene `[Fact]` y `[Trait("Category", "Domain")]`
- **Dependencias**: Tarea 6.3
- **Validación**: Archivo compila, 2 tests descubiertos, todos fallan

### Tarea 7.3 – Crear test stub para DomainException
- **Path**: `tests/SauronSheet.Domain.Tests/Exceptions/DomainExceptionTests.cs`
- **Acción**: Crear archivo
- **Descripción**: Crear clase `DomainExceptionTests` con 2 métodos (RED phase):
  - `Test_DomainException_CarriesMessage()`
  - `Test_DomainException_CarriesInnerException()`
  - Cada método tiene `[Fact]` y `[Trait("Category", "Domain")]`
- **Dependencias**: Tarea 6.3
- **Validación**: Archivo compila, 2 tests descubiertos, todos fallan

### Tarea 7.4 – Crear test stub para EntityNotFoundException
- **Path**: `tests/SauronSheet.Domain.Tests/Exceptions/EntityNotFoundExceptionTests.cs`
- **Acción**: Crear archivo
- **Descripción**: Crear clase `EntityNotFoundExceptionTests` con 2 métodos (RED phase):
  - `Test_EntityNotFoundException_FormatsMessage()`
  - `Test_EntityNotFoundException_StoresProperties()`
  - Cada método tiene `[Fact]` y `[Trait("Category", "Domain")]`
- **Dependencias**: Tarea 6.3
- **Validación**: Archivo compila, 2 tests descubiertos, todos fallan

### Tarea 7.5 – Crear test stub para ISpecification<T>
- **Path**: `tests/SauronSheet.Domain.Tests/Repositories/SpecificationBaseTests.cs`
- **Acción**: Crear archivo
- **Descripción**: Crear clase `SpecificationBaseTests` con 1 método (RED phase):
  - `Test_Specification_DefaultMaxResultsIs1000()`
  - Método tiene `[Fact]` y `[Trait("Category", "Domain")]`
- **Dependencias**: Tarea 6.3
- **Validación**: Archivo compila, 1 test descubierto, falla

### Tarea 7.6 – Crear test stub para MediatR Registration
- **Path**: `tests/SauronSheet.Application.Tests/Common/MediatRRegistrationTests.cs`
- **Acción**: Crear archivo
- **Descripción**: Crear clase `MediatRRegistrationTests` con 2 métodos (RED phase):
  - `Test_MediatR_ResolvesFromServiceProvider()`
  - `Test_AddApplicationServices_RegistersWithoutException()`
  - Cada método tiene `[Fact]` y `[Trait("Category", "Application")]`
- **Dependencias**: Tarea 6.4
- **Validación**: Archivo compila, 2 tests descubiertos, fallan

### Tarea 7.7 – Ejecutar tests en fase RED
- **Path**: N/A (command line)
- **Acción**: Ejecutar comando
- **Descripción**: Ejecutar `dotnet test --filter Category=Domain --no-build` y `dotnet test --filter Category=Application --no-build` para verificar que todos los tests fallan (RED phase).
- **Dependencias**: Tarea 7.1 a 7.6 completas
- **Validación**: `dotnet test --filter Category=Domain` retorna "11 failed" (4+2+2+2+1); `dotnet test --filter Category=Application` retorna "2 failed"

---

## 8. Domain Implementation – GREEN Phase

### Tarea 8.1 – Implementar Entity<TId> para pasar tests
- **Path**: `src/SauronSheet.Domain/Common/Entity.cs`
- **Acción**: Modificar archivo
- **Descripción**: Implementar lógica suficiente para pasar los 4 tests de EntityBaseTests:
  - Guardar `CreatedAt = DateTime.UtcNow` en constructor
  - Implementar `Equals()` comparando tipo + Id
  - Implementar `GetHashCode()` basado en Id
  - Implementar operadores `==` y `!=`
- **Dependencias**: Tarea 2.4 (Entity schema creado), Tarea 7.1 (tests listos)
- **Validación**: `dotnet test --filter ClassName=EntityBaseTests` pasa todos 4 tests

### Tarea 8.2 – Implementar ValueObject para pasar tests
- **Path**: `src/SauronSheet.Domain/Common/ValueObject.cs`
- **Acción**: Modificar archivo
- **Descripción**: Implementar ValueObject como record abstract (C# records manejan igualdad automáticamente). Validar que dos records con mismos valores son iguales, diferentes valores son distintos.
- **Dependencias**: Tarea 2.6 (ValueObject schema creado), Tarea 7.2 (tests listos)
- **Validación**: `dotnet test --filter ClassName=ValueObjectBaseTests` pasa ambos tests

### Tarea 8.3 – Implementar DomainException para pasar tests
- **Path**: `src/SauronSheet.Domain/Exceptions/DomainException.cs`
- **Acción**: Modificar archivo
- **Descripción**: Implementar lógica de constructores para pasar tests: guardar message en property, guardar InnerException.
- **Dependencias**: Tarea 2.7 (schema creado), Tarea 7.3 (tests listos)
- **Validación**: `dotnet test --filter ClassName=DomainExceptionTests` pasa ambos tests

### Tarea 8.4 – Implementar EntityNotFoundException para pasar tests
- **Path**: `src/SauronSheet.Domain/Exceptions/EntityNotFoundException.cs`
- **Acción**: Modificar archivo
- **Descripción**: Implementar mensaje formateado correcto, guardar EntityName y EntityId como properties públicas.
- **Dependencias**: Tarea 2.8 (schema creado), Tarea 7.4 (tests listos)
- **Validación**: `dotnet test --filter ClassName=EntityNotFoundExceptionTests` pasa ambos tests

### Tarea 8.5 – Implementar ISpecification<T> para pasar tests
- **Path**: `src/SauronSheet.Domain/Repositories/ISpecification.cs`
- **Acción**: Verificar/Completar archivo
- **Descripción**: Asegurar que interfaz implementa default `MaxResults => 1000` correctamente.
- **Dependencias**: Tarea 2.9 (schema creado), Tarea 7.5 (tests listos)
- **Validación**: `dotnet test --filter ClassName=SpecificationBaseTests` pasa test

### Tarea 8.6 – Implementar AggregateRoot<TId> para usar Entity
- **Path**: `src/SauronSheet.Domain/Common/AggregateRoot.cs`
- **Acción**: Verificar/Completar archivo
- **Descripción**: Asegurar que AggregateRoot hereda correctamente de Entity<TId> y se puede instanciar (subclaseado) para tests futuros.
- **Dependencias**: Tarea 2.5 (schema creado), Tarea 8.1 (Entity implementado)
- **Validación**: Archivo compila, puede usarse como base para entidades

---

## 9. Application Implementation – GREEN Phase

### Tarea 9.1 – Implementar MediatR DI para pasar tests
- **Path**: `src/SauronSheet.Application/DependencyInjection.cs`
- **Acción**: Modificar archivo (si es necesario)
- **Descripción**: Verificar que `AddApplicationServices()` registra MediatR correctamente permitiendo que IMediator se resuelva desde ServiceProvider.
- **Dependencias**: Tarea 3.4 (schema creado), Tarea 7.6 (tests listos), Tarea 3.2 (MediatR package agregado)
- **Validación**: `dotnet test --filter ClassName=MediatRRegistrationTests` pasa ambos tests (2 tests en Application)

---

## 10. Validación – Build & Test

### Tarea 10.1 – Full solution build
- **Path**: N/A (command line)
- **Acción**: Ejecutar comando
- **Descripción**: Ejecutar `dotnet build` desde raíz de solución para compilar todos 6 proyectos.
- **Dependencias**: Todas las tareas 1-9 completas
- **Validación**: Build exitoso, exit code 0, cero errores, cero warnings (TreatWarningsAsErrors=true)

### Tarea 10.2 – Ejecutar todos los tests
- **Path**: N/A (command line)
- **Acción**: Ejecutar comando
- **Descripción**: Ejecutar `dotnet test` desde raíz. Esperar 13+ tests passing (11 Domain + 2 Application).
- **Dependencias**: Tarea 10.1 (build exitoso)
- **Validación**: `dotnet test` retorna exit code 0, salida muestra "13 passed", sin errores

### Tarea 10.3 – Ejecutar tests con filtro Domain
- **Path**: N/A (command line)
- **Acción**: Ejecutar comando
- **Descripción**: `dotnet test --filter Category=Domain --no-build` verificar que todos 11 Domain tests pasan.
- **Dependencias**: Tarea 10.1
- **Validación**: 11 tests passed, exit code 0

### Tarea 10.4 – Ejecutar tests con filtro Application
- **Path**: N/A (command line)
- **Acción**: Ejecutar comando
- **Descripción**: `dotnet test --filter Category=Application --no-build` verificar que todos 2 Application tests pasan.
- **Dependencias**: Tarea 10.1
- **Validación**: 2 tests passed, exit code 0

### Tarea 10.5 – Verificar dependencias Domain (cero referencias)
- **Path**: N/A (validation)
- **Acción**: Auditar
- **Descripción**: Ejecutar grep o inspección manual: `grep -E "ProjectReference|PackageReference" src/SauronSheet.Domain/SauronSheet.Domain.csproj` debe retornar vacío (Domain tiene cero dependencias).
- **Dependencias**: Tarea 2.1
- **Validación**: No hay líneas con ProjectReference o PackageReference en Domain.csproj

### Tarea 10.6 – Verificar dependencias Application (Domain only)
- **Path**: N/A (validation)
- **Acción**: Auditar
- **Descripción**: Application.csproj debe tener EXACTAMENTE 1 ProjectReference a Domain, sin referencias a Infrastructure, Frontend, o NuGet packages que violen Clean Architecture (solo MediatR permitido).
- **Dependencias**: Tarea 3.1
- **Validación**: `grep "ProjectReference" src/SauronSheet.Application/SauronSheet.Application.csproj | wc -l` retorna 1; `grep "PackageReference" src/SauronSheet.Application/SauronSheet.Application.csproj | grep -E "MediatR"` retorna MediatR references

### Tarea 10.7 – Verificar dependencias Infrastructure (Domain only)
- **Path**: N/A (validation)
- **Acción**: Auditar
- **Descripción**: Infrastructure.csproj debe tener EXACTAMENTE 1 ProjectReference a Domain, sin referencias a Application o Frontend.
- **Dependencias**: Tarea 4.1
- **Validación**: `grep "ProjectReference" src/SauronSheet.Infrastructure/SauronSheet.Infrastructure.csproj | wc -l` retorna 1

### Tarea 10.8 – Verificar dependencias Frontend (Application + Infrastructure)
- **Path**: N/A (validation)
- **Acción**: Auditar
- **Descripción**: Frontend.csproj debe tener EXACTAMENTE 2 ProjectReferences: Application e Infrastructure. No debe referenciar Domain directamente.
- **Dependencias**: Tarea 5.1
- **Validación**: `grep "ProjectReference" src/SauronSheet.Frontend/SauronSheet.Frontend.csproj | wc -l` retorna 2

### Tarea 10.9 – Verificar que Frontend inicia sin errores
- **Path**: N/A (manual testing)
- **Acción**: Ejecutar aplicación
- **Descripción**: Ejecutar `dotnet run --project src/SauronSheet.Frontend/SauronSheet.Frontend.csproj` desde raíz. La aplicación debe iniciar sin excepciones (salvo falta de config Supabase, que es esperado). Acceder a http://localhost:5000/ (o puerto asignado).
- **Dependencias**: Tarea 10.1
- **Validación**: App inicia, no hay stack traces en consola, página de salud visible (contiene "System OK")

### Tarea 10.10 – Verificar que página de salud renderiza
- **Path**: N/A (manual testing)
- **Acción**: Visual inspection
- **Descripción**: En navegador, navegar a http://localhost:5000/. Verificar que se muestra:
  - Heading "SauronSheet"
  - Badge verde "System OK"
  - Mensaje "Foundation Phase 0 is running successfully"
  - Fecha/hora actual en formato UTC
  - Layout con navbar (SauronSheet logo, nav links)
  - Footer con versión
- **Dependencias**: Tarea 10.9
- **Validación**: Página carga, todos los elementos HTML visibles, estilos Tailwind aplicados (colores, espaciado)

### Tarea 10.11 – Validar coverage >= 80% para Domain (opcional)
- **Path**: N/A (command line / report)
- **Acción**: Generar reporte
- **Descripción**: Ejecutar coverlet para medir cobertura del Domain layer:
```
dotnet tool install -g coverlet.console
  coverlet tests/SauronSheet.Domain.Tests/bin/Debug/net10.0/SauronSheet.Domain.Tests.dll \
    --target "dotnet" \
    --targetargs "test tests/SauronSheet.Domain.Tests/ --no-build --configuration Debug" \
    --format "opencover" \
    --output "./coverage.xml" \
    --include "[SauronSheet.Domain]*" \
    --exclude "[SauronSheet.Domain.Tests]*"
```
- **Dependencias**: Tarea 10.1
- **Validación**: coverage.xml generado, Domain layer >= 80% covered

---

## 11. Documentación & Cleanup

### Tarea 11.1 – Crear archivo README.md (si no existe)
- **Path**: `README.md`
- **Acción**: Crear archivo
- **Descripción**: Crear README con:
  - Título "SauronSheet"
  - Descripción breve: "Multi-user expense tracking web application"
  - Stack: ".NET 10, Razor Pages, Supabase PostgreSQL, Tailwind CSS"
  - Getting Started: build instructions (dotnet build, dotnet run)
  - Architecture: referencia a Clean Architecture (Phases)
  - Status: "Phase 0 – Foundation (COMPLETE)"
- **Dependencias**: Todas las tareas completas
- **Validación**: README existe, contiene info básica del proyecto

### Tarea 11.2 – Verificar .gitignore actualizado
- **Path**: `.gitignore`
- **Acción**: Auditar
- **Descripción**: Confirmar que .gitignore contiene patrones para: bin/, obj/, .vs/, .vscode/, *.nupkg, TestResults/, coverage.xml, etc.
- **Dependencias**: Tarea 1.5
- **Validación**: Archivo existe, contiene patrones de .NET

### Tarea 11.3 – Cleanup: Eliminar archivos de template no necesarios
- **Path**: N/A (varios archivos)
- **Acción**: Eliminar archivos
- **Descripción**: Si existen, eliminar archivos autogenerados no necesarios:
  - `src/SauronSheet.Domain/Class1.cs`
  - `src/SauronSheet.Application/Class1.cs`
  - `src/SauronSheet.Infrastructure/Class1.cs`
  - `tests/SauronSheet.Domain.Tests/UnitTest1.cs`
  - `tests/SauronSheet.Application.Tests/UnitTest1.cs`
  - Archivos de template de Razor Pages que no se usen (ej: Privacy.cshtml)
- **Dependencias**: Todas las tareas 1-10 completas
- **Validación**: Archivos no existen, proyectos aún compilan

---

## Orden de implementación

Ejecutar las tareas en el siguiente orden secuencial:

1. **Tarea 1.1** – Crear SauronSheet.sln
2. **Tarea 1.2** – Crear directorios src/, tests/
3. **Tarea 1.3** – Crear global.json
4. **Tarea 1.4** – Crear Directory.Build.props
5. **Tarea 1.5** – Crear .gitignore
6. **Tarea 2.1** – Crear proyecto Domain
7. **Tarea 2.2** – Verificar Domain tiene cero dependencias
8. **Tarea 2.3** – Crear estructura carpetas Domain (Common/, Exceptions/, Repositories/)
9. **Tarea 2.4** – Crear Entity<TId> (schema)
10. **Tarea 2.5** – Crear AggregateRoot<TId> (schema)
11. **Tarea 2.6** – Crear ValueObject (schema)
12. **Tarea 2.7** – Crear DomainException (schema)
13. **Tarea 2.8** – Crear EntityNotFoundException (schema)
14. **Tarea 2.9** – Crear ISpecification<T> (schema)
15. **Tarea 3.1** – Crear proyecto Application
16. **Tarea 3.2** – Agregar MediatR NuGet packages
17. **Tarea 3.3** – Crear IUserContext
18. **Tarea 3.4** – Crear DependencyInjection.cs (Application)
19. **Tarea 4.1** – Crear proyecto Infrastructure
20. **Tarea 4.2** – Agregar supabase-csharp NuGet
21. **Tarea 4.3** – Crear DependencyInjection.cs (Infrastructure)
22. **Tarea 5.1** – Crear proyecto Frontend
23. **Tarea 5.2** – Actualizar Program.cs (Frontend)
24. **Tarea 5.3** – Crear appsettings.json
25. **Tarea 5.4** – Crear appsettings.Development.json
26. **Tarea 5.5** – Actualizar _Layout.cshtml
27. **Tarea 5.6** – Actualizar Index.cshtml
28. **Tarea 5.7** – Actualizar Index.cshtml.cs
29. **Tarea 6.1** – Crear proyecto Domain.Tests
30. **Tarea 6.2** – Crear proyecto Application.Tests
31. **Tarea 6.3** – Crear estructura carpetas Domain.Tests (Common/, Exceptions/, Repositories/)
32. **Tarea 6.4** – Crear estructura carpetas Application.Tests (Common/)
33. **Tarea 7.1** – Crear EntityBaseTests.cs (RED)
34. **Tarea 7.2** – Crear ValueObjectBaseTests.cs (RED)
35. **Tarea 7.3** – Crear DomainExceptionTests.cs (RED)
36. **Tarea 7.4** – Crear EntityNotFoundExceptionTests.cs (RED)
37. **Tarea 7.5** – Crear SpecificationBaseTests.cs (RED)
38. **Tarea 7.6** – Crear MediatRRegistrationTests.cs (RED)
39. **Tarea 7.7** – Ejecutar tests en fase RED (validar que fallan)
40. **Tarea 8.1** – Implementar Entity<TId> (GREEN)
41. **Tarea 8.2** – Implementar ValueObject (GREEN)
42. **Tarea 8.3** – Implementar DomainException (GREEN)
43. **Tarea 8.4** – Implementar EntityNotFoundException (GREEN)
44. **Tarea 8.5** – Implementar ISpecification<T> (GREEN)
45. **Tarea 8.6** – Verificar AggregateRoot<TId> (GREEN)
46. **Tarea 9.1** – Implementar MediatR DI (GREEN)
47. **Tarea 10.1** – Full solution build
48. **Tarea 10.2** – Ejecutar todos los tests
49. **Tarea 10.3** – Tests Domain filter
50. **Tarea 10.4** – Tests Application filter
51. **Tarea 10.5** – Verificar Domain cero dependencias
52. **Tarea 10.6** – Verificar Application (Domain only)
53. **Tarea 10.7** – Verificar Infrastructure (Domain only)
54. **Tarea 10.8** – Verificar Frontend (App + Infra)
55. **Tarea 10.9** – Verificar Frontend inicia
56. **Tarea 10.10** – Verificar página salud renderiza
57. **Tarea 10.11** – Validar coverage >= 80% Domain (opcional)
58. **Tarea 11.1** – Crear README.md
59. **Tarea 11.2** – Verificar .gitignore
60. **Tarea 11.3** – Cleanup archivos template

---

# Fin del documento
