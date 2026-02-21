# MyResourcesApi — Project Context

## Stack

- ASP.NET Core 8 Controller API
- MediatR (CQRS)
- EF Core + PostgreSQL
- FluentValidation
- xUnit + Testcontainers

## Conventions

- Endpoints are controller actions in `Controllers/`
- Keep request/response contracts and MediatR handlers in `Features/` (vertical slice)
- Return `IActionResult` from controller actions with explicit status mapping
- Records for DTOs, primary constructors preferred
- Manual mapping via extension methods — no AutoMapper
- `CancellationToken` on every async method
- Nullable reference types enabled
- Validation failures are returned as `400 ValidationProblem` (via global exception handling)

## SDD Workflow

This project follows Spec-Driven Development. For any new feature:

1. Run the `sdd-requirements` agent first
2. Then `sdd-design`
3. Then `sdd-implement`
4. Finally `sdd-review`

Spec artifacts live in `docs/specs/{feature-name}/`.
Never write implementation code before a design doc is approved.
