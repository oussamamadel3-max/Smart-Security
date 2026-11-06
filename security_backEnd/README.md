# ESP32 Smart Security — Backend Skeleton

Clean Architecture skeleton copied from the militaryhealth-api layout.

## Solution layout

```
Backend.sln
├── API/                 Controllers, Middleware, Extensions, ApiResponse
├── Application/         Services, DTOs, Interfaces, Mappings
├── Domain/              Entities, Enums, Exceptions, IAuditableEntity
├── Infrastructure/      EF Core, Repositories, DI registration
├── Application.Tests/
└── API.IntegrationTests/
```

## Dependency flow

```
API → Application → Domain
API → Infrastructure → Application / Domain
```

Request path: `Controller → Application Service → Repository / IUnitOfWork → Database`

## Stack

- .NET 10 / ASP.NET Core
- EF Core 10 + PostgreSQL (Npgsql)
- ASP.NET Identity
- Mapster
- Swashbuckle
- xUnit + Moq

## Run

1. Update `API/appsettings.json` connection string.
2. From repo root:

```bash
dotnet restore Backend.sln
dotnet build Backend.sln
dotnet run --project API
```

3. Open Swagger: `https://localhost:7150/swagger`

## Adding a module (vertical slice)

1. Entity in `Domain/Entities/<Area>/`
2. EF config in `Infrastructure/Persistence/Configurations/<Area>/`
3. Repository interface in `Application/Interfaces/Repositories/`
4. Repository impl in `Infrastructure/Repositories/`
5. DTOs in `Application/Dtos/<Module>/`
6. Mapster profile in `Application/Mappings/`
7. Service interface + impl under `Application/`
8. Controller in `API/Controllers/`
9. Register in `Infrastructure/InfrastructureServiceRegistration.cs`

## Contracts

- JSON success: `ApiResponse.Success` → `{ success, message, data, meta }`
- JSON errors: `ErrorHandlingMiddleware` → `{ success:false, error:{ code, message, meta } }`
- Controllers stay thin; throw domain/business exceptions from services.
