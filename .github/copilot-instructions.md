# GitHub Copilot Instructions

These are repository-level instructions for GitHub Copilot.

The goal of this project is to build a **generic library system** with a clean architecture, strong test coverage, and a flexible data model based on **Type Descriptors**.

---

## 1. Project Overview

- Build a **generic resource library**, not a book-only app.
- Users can store arbitrary objects (books, articles, users, notes, etc.) as **Resources**.
- Each resource:
  - Has common fields (id, type, timestamps, owner, metadata).
  - Stores type-specific data in a **JSON payload**.
  - Is validated and interpreted by a **Type Descriptor** definition.

Main requirements:

- **ASP.NET Core Web API** as the main interface.
- **SQLite** as the database via **EF Core**.
- **Type Descriptor** pattern to define and validate resource types.
- **xUnit** for unit and integration tests.
- **Playwright** for API-level tests.
- Overall test coverage target: **≥ 80%** (unit tests), plus meaningful integration & API tests.

---

## 2. Tech Stack & Standards

### Backend

- Runtime: **.NET 9** (if version not specified, assume latest STS).
- Web framework: **ASP.NET Core Web API**.
- Data access: **Entity Framework Core** with **SQLite**.
- Testing:
  - **xUnit** for unit and integration tests.
  - **Playwright** for API tests (using `APIRequestContext` rather than UI).

### Solution layout

When generating code, assume a structure similar to:

- `Library.WebApi` – ASP.NET Core Web API (controllers, DI setup, startup)
- `Library.Domain` – domain models, core abstractions, business rules
- `Library.Application` – application services, use cases, validation, Type Descriptor logic
- `Library.Infrastructure` – EF Core, SQLite configuration, repositories, migrations
- `Library.Tests.Unit` – unit tests
- `Library.Tests.Integration` – integration tests (SQLite + EF)
- `Library.Tests.Api` – Playwright API tests

If these projects or namespaces don’t exist yet, propose and generate them consistently.

---

## 3. Core Domain Concepts

### 3.1 Resource (generic entity)

Generate a **single generic Resource entity**, not one per type (no `BookEntity`, `ArticleEntity`, etc.).

A `Resource` should conceptually have:

- `Id` – primary key (Guid or int, be consistent).
- `Type` – string key like `"book"`, `"article"`, `"userProfile"`.
- `CreatedAt` / `UpdatedAt`.
- `OwnerId` – ID or identifier of the owner.
- `Metadata` – JSON or a structure that can be serialized (tags, labels, etc.).
- `Payload` – JSON representing type-specific data.

In EF Core with SQLite:

- Map `Payload` (and optionally `Metadata`) to TEXT columns containing JSON.
- Configure and use migrations normally (`Add-Migration`, `dotnet ef migrations add`, etc.).

### 3.2 Type Descriptors

Implement a **Type Descriptor** system (this is custom to this project):

- A **Type Descriptor** describes how a particular `Type` behaves and is validated.
- For each type (e.g. `"book"`, `"article"`, `"userNote"`), define:
  - **Identity**: `typeKey`, `displayName`.
  - **Schema**: fields with names, types (`string`, `int`, `bool`, `date`, etc.), `required`, `maxLength`, etc.
  - **Indexing metadata**: which fields are allowed as filters and sort keys.
  - **Policy**: which roles can create/read/update/delete.
  - **UI hints** (even if no UI yet): title field, list columns.
  - **Schema version**: `schemaVersion`.

Storage for Type Descriptors:

- Initial implementation may load them from configuration (e.g. `appsettings.json`).
- A **Descriptor Registry** should:
  - Load descriptors at startup.
  - Provide a `GetDescriptor(typeKey)` method.
  - Throw or return a clear error for unknown types.

### 3.3 Validation Engine

Implement a validation engine that:

- Uses the Type Descriptor to validate the `Payload` for a given resource.
- Checks:
  - Required fields.
  - Basic type correctness.
  - Length/range/pattern constraints when defined.
- Returns a structured result (success or a list of validation errors).
- Is used on create and update operations.

---

## 4. API Design

### General rules

- Build a **RESTful** Web API.
- Prefer clear, resource-based URLs and HTTP verbs:
  - `POST /resources`
  - `GET /resources/{id}`
  - `PUT /resources/{id}`
  - `DELETE /resources/{id}`
  - `GET /resources` with filters, sorting, and paging.
- Use **DTOs** for request/response models; don’t expose EF entities directly.
- Use asynchronous methods (`async`/`await`) for I/O and EF Core operations.
- When in doubt, generate complete examples including:
  - DTOs
  - Controller methods
  - Application services
  - Repository methods
  - Dependency injection configuration

### Querying

For `GET /resources`:

- Support:
  - `type` filter.
  - `ownerId` filter.
  - Optional filters on payload fields that are allowed by the Type Descriptor.
  - Sorting by supported fields (descriptor-driven).
  - Paging with `page` and `pageSize`.
- When payload-based filtering is too complex for now, start with:
  - Filtering on normal columns (Type, OwnerId, etc.).
  - Clearly comment where payload-based filtering could be extended later.

---

## 5. Testing Strategy

### General

- For new code (services, controllers, utilities), always generate corresponding tests.
- Aim for overall coverage **≥ 80%** on unit tests.
- Prefer smaller, focused tests rather than one huge test per class.

### Unit tests (xUnit)

- Use **xUnit** for unit tests.
- Place tests in `Library.Tests.Unit`:
  - Validation engine tests.
  - Descriptor registry tests.
  - Resource application service tests.
  - Query/filter/sort mapping tests.
- Use fakes/mocks where external dependencies (DB, HTTP, etc.) are involved.

### Integration tests (xUnit + SQLite)

- Use **SQLite** for integration tests, preferably:
  - In-memory (`UseSqlite("DataSource=:memory:")`) with proper migrations, or
  - A separate test database file.
- Place integration tests in `Library.Tests.Integration`.
- Test:
  - EF migrations (schema creation).
  - Resource CRUD flows.
  - Simple querying (filter, paging, sort).

### API tests (Playwright)

- Use **Playwright** in `Library.Tests.Api`.
- Use `APIRequestContext` to:
  - Call the running Web API (or a TestServer).
  - Verify end-to-end flows:
    - `POST /resources` with valid/invalid payloads.
    - `GET /resources/{id}`.
    - `GET /resources` with queries.
    - `PUT` and `DELETE`.
- Focus on status codes, response shapes, and main behaviors.

When generating tests, include:

- Arrange–Act–Assert structure.
- Clear naming for test methods (e.g. `MethodName_Condition_ExpectedResult`).

---

## 6. Coding Style & Conventions

- Language: **C#**, comments in **English**.
- Use **async/await** and cancellation tokens where appropriate.
- Prefer clean, readable code over overly clever solutions.
- Keep methods focused and relatively small.
- Use dependency injection rather than static/global services.
- Don’t rely on magic strings for type keys when enums or constants would improve clarity.
- Use `nullable` reference types if possible and handle null safely.
- When adding new features, update:
  - Interfaces in Domain/Application.
  - Implementations in Infrastructure.
  - Controllers in WebApi.
  - Relevant tests.

---

## 7. How Copilot Should Behave

When asked to implement something in this repo:

1. **Respect this architecture**:
   - Keep Web, Application, Domain, Infrastructure layers separated.
   - Avoid putting business logic directly in controllers.

2. **Be explicit and complete**:
   - Generate full class implementations, including namespaces and `using` statements.
   - Update DI registrations when new services or repositories are added.
   - Add or update tests for new logic.

3. **Prefer patterns that fit this project**:
   - Generic `Resource` model + Type Descriptor pattern.
   - Descriptor-driven validation and querying.
   - DTOs at API boundary.

4. **If something is ambiguous**:
   - Choose reasonable defaults and add a `TODO` comment explaining assumptions.
   - Try to remain consistent with previous patterns in this repository.

5. **Avoid**:
   - Creating separate entities per type (e.g. `BookEntity`, `ArticleEntity`) unless explicitly requested.
   - Introducing other databases or frameworks unless explicitly requested.
   - Mixing concerns across layers (e.g. EF logic inside controllers).
   - Replacing or updating already existing packages.

---

## 8. Example flows Copilot should support

When asked, Copilot should be able to:

- Scaffold a new feature end-to-end:
  - DTOs → Application service → Repository → Controller → Tests.
- Add a new resource type:
  - Update Type Descriptor configuration.
  - Add or adjust tests for validation and behavior.
- Enhance querying logic:
  - Expand filter/sort handling based on descriptors.
  - Add tests to ensure descriptors are respected.

---

Use these instructions as the default context for all code and test generation in this repository.
