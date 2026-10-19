# Enterprise .NET Architecture & Coding Standards

> Source: `Global_Enterprise_NET_Coding_Standards.docx` (provided by the project owner). **These rules are binding** for every
> change in this repository, by humans and AI tools alike. Where this repo's interpretation matters it is noted under
> "How VocalEye applies this". Changes to the rules themselves need the owner's approval.

## 1. Architectural layering and design foundations
Clean Architecture and domain-driven isolation. Dependencies flow inward toward the domain layer. Infrastructure, databases and frameworks are external details.

- **Core / Domain layer:** domain objects, tracking entities, aggregate roots, core specifications, value models, internal domain interfaces, global enterprise options.
- **Application layer:** implements abstract interfaces, orchestrates transactions, houses application services, translates entities via DTOs, dictates command workflows.
- **Infrastructure / Data-access layer:** database systems, EF contexts, object storage, messaging queues, third-party integrations.
- **Presentation layer (API / Host):** execution entry point (Web API, GraphQL, worker nodes, minimal APIs). Dependency configuration, global request lifecycle, middleware.

## 2. Framework and language
- Target framework: **.NET 10.0** everywhere.
- **File-scoped namespaces** always.
- **Implicit usings** enabled in every project (`<ImplicitUsings>enable</ImplicitUsings>`).
- **Nullable reference types** enabled (`<Nullable>enable</Nullable>`); null handling must be explicit.

## 3. Naming
| Element | Casing | Prefix / suffix | Example |
|---|---|---|---|
| Classes & enums | PascalCase | none | `UserProfile`, `OrderStatus` |
| Interfaces | PascalCase | prefix `I` | `IConsultationRepository` |
| DTOs | PascalCase | suffix `Dto` | `BookingPageDto` |
| Methods | PascalCase | none | `GetRecordById`, `SaveChangesAsync` |
| Private fields | camelCase | prefix `_` | `_context`, `_logger` |
| Parameters & locals | camelCase | none | `customerRecord`, `itemId` |
| Constants | PascalCase | none | `MaximumPageSizeLimit` |

## 4. Formatting and structure
- Indentation: exactly **4 spaces** (no tabs).
- **Allman** braces everywhere (classes, statements, loops): braces on their own lines.
- **One public class per file** (single responsibility).
- `using` directives clustered at the very top, **outside** the file-scoped namespace.

```csharp
namespace Enterprise.Application.Dtos.Identity;

public class CustomerProfileDto
{
    public Guid Identifier { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? OptionalCountryRegion { get; set; }
}
```

## 5. Async and memory
- **End-to-end async (TAP)** from the data layer to the edge.
- In **infrastructure assemblies and low-level data-layer code**, add `.ConfigureAwait(false)` to awaited tasks.
- In **presentation hosts, API pipelines and endpoint methods**, do **not** use `.ConfigureAwait(false)`.

```csharp
var trackingRecord = await _context.Set<DomainEntity>()
    .Where(entity => entity.Id == trackingId)
    .AsNoTracking()
    .FirstOrDefaultAsync()
    .ConfigureAwait(false);
```

## 6. Data layer (EF Core)
- **Repository encapsulation:** focused query logic only behind the transactional/domain layer; raw LINQ must not bypass layer borders.
- **Projections:** never return or filter heavy entity graphs when a thin slice is needed; project directly into DTOs.
- **Read-only:** use `.AsNoTracking()` for lookup/reporting queries.

## 7. Quality control, validation, exceptions
- **Environment safety:** when mandatory configuration/parameters are missing, stop at startup/injection by throwing a clear `InvalidOperationException`.
- **Global exception engine:** one unified catch-all exception middleware. Domain classes bubble raw faults up without local try/catch bloat.
- **Tests:** xUnit runner, **Moq** for mocks, **FluentAssertions** for assertions.

```csharp
validationResponse.Should().NotBeNull();
validationResponse.ProcessingDuration.Should().BeLessThanOrEqualTo(250);
validationResponse.StatusResult.Should().Be(OperationStatus.Completed);
```

---

## How VocalEye applies this
Interpretations made when the rules were adopted (flag to the owner if any is wrong):

1. **Layer mapping.** Domain = `VocalEye.Domain`; Application = `VocalEye.Application` (+ feature modules `Agents`, `Tools`, `Rag`); Infrastructure = `VocalEye.Infrastructure`; Presentation = `VocalEye.Api`, `VocalEye.Worker`. Ports (interfaces the application depends on) live in `Application/Abstractions`. Repository interfaces for persisted entities live in **Domain** (the document lists "internal domain interfaces" there) and are implemented in Infrastructure. This applies from the Postgres slice onward.
2. **DTOs** are the public contract types in `VocalEye.Contracts`, written as classes with `get; set;` and defaults, suffix `Dto`. Internal immutable messages between layers (`AgentRequest`, `AgentReply`, `ModelSelection`...) are `sealed record`s without the suffix because they are not DTOs.
3. **"One public class per file"** is applied to every public type kind (class, record, interface, enum).
4. **Constructors** are explicit with `_camelCase` fields. No primary constructors.
5. **Control statements always use braces**, even for one line.
6. **ConfigureAwait(false)** is applied in `VocalEye.Infrastructure`. It is not used in Api, Worker, Application or Agents.
7. **Exceptions.** Faults bubble to `GlobalExceptionMiddleware`. Three narrow, documented exceptions exist (ADR-0008): the tool boundary (tool failure becomes text for the model), vendor-exception translation in the LLM adapter, and mid-stream errors on SSE responses where headers are already sent.
8. **FluentAssertions version.** v8 and later use a commercial licence for commercial use; v7.x remains Apache-2.0. This repo pins 7.x. Revisit if the project becomes commercial.
