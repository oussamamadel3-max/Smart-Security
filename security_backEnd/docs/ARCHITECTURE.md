# Architecture Blueprint

## Layers

| Project | Responsibility |
|---------|----------------|
| Domain | Entities, enums, domain exceptions, markers (`IAuditableEntity`) |
| Application | Use-cases/services, DTOs, repository/service interfaces, Mapster profiles |
| Infrastructure | `AppDbContext`, EF configs/migrations, repositories, Identity, DI |
| API | Controllers, middleware, HTTP extensions, unified `ApiResponse` |

## Rules

1. Domain never references Application/Infrastructure/API.
2. Application never injects `AppDbContext` — only interfaces.
3. Controllers never contain business rules or talk to repositories directly.
4. Prefer exception-driven error flow + central middleware.
5. Register new services/repos as `Scoped` in `InfrastructureServiceRegistration`.

## Response shape

Success:

```json
{ "success": true, "message": "...", "data": {}, "meta": {} }
```

Error:

```json
{ "success": false, "error": { "code": "NOT_FOUND", "message": "...", "meta": {} } }
```
