# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Mpt.Rql is a .NET library implementing [Resource Query Language (RQL)](https://docs.platform.softwareone.com/developer-resources/rest-api/resource-query-language) for filtering, sorting, and projecting data on `IQueryable<T>`. It integrates with Entity Framework and other LINQ providers. Published as NuGet packages `Mpt.Rql` and `Mpt.Rql.Abstractions`.

## Build Commands

```bash
dotnet build                                    # Build entire solution
dotnet test                                     # Run all tests
dotnet test tests/Rql.Tests.Unit                # Run unit tests only
dotnet test tests/Rql.Tests.Integration         # Run integration tests only
dotnet test --filter "FullyQualifiedName~Class" # Run specific test class/method
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover  # With coverage
dotnet pack --configuration Release --output ./nupkgs                  # Create NuGet packages
```

## Architecture

### Core Libraries (src/)

- **Mpt.Rql.Abstractions** — Public interfaces, models (`RqlRequest`, `RqlResponse<T>`, `IRqlNode`), operator definitions (binary/unary/collection), and configuration contracts. This is the stable API surface.
- **Mpt.Rql** — Implementation library containing all services, parser, and DI registration.

### Key Service Layers (src/Mpt.Rql/Services/)

- **Filtering** (`Filtering/`) — Builds LINQ filter expressions from RQL syntax. Separate expression builders for binary (`eq`, `gt`, `lt`...), unary (`empty`, `not`), and collection (`in`, `out`) operators.
- **Ordering** (`Ordering/`) — Converts RQL order strings (e.g. `-age,+name`) into `OrderBy`/`ThenBy` expressions.
- **Projection** (`Projection/`) — Generates `Select` expressions for field subsetting.
- **Mapping** (`Mapping/`) — Maps between storage entities and view DTOs. Supports name-based auto-mapping, explicit static mappings (`MapStatic`), inline expressions (`MapInline`), conditional mappings (`MapIf`), and factory-based mappings (`MapWithFactory`) that resolve via DI. Uses `EntityMapCache` for caching resolved maps.
- **Context** (`Context/`) — `IQueryContext<T>` orchestrates the RQL graph traversal and applies filter/order/projection transformations.

### Parser (src/Mpt.Rql/Parsers/Linear/)

Linear expression parser that tokenizes RQL strings into an `IRqlNode` decision graph.

### Client (src/Mpt.Rql/Client/)

Fluent builder API for programmatically constructing `RqlRequest` objects.

### Entry Point

`ServiceCollectionExtensions.AddRql()` registers all services. Mappers are discovered via `options.ScanForMappers(assembly)`.

## Key Patterns

- **Expression tree composition**: All query operations build `Expression<Func<T, ...>>` trees with parameter replacement (`ReplaceParameterVisitor`) for composability.
- **Graph-based processing**: Parsed RQL produces an `IRqlNode` tree. Services traverse this graph, collecting errors and transformations. Use `response.Graph.Print()` to visualize.
- **Mapping extensibility**: Custom mappings implement `IRqlMapper<TStorage, TView>`. Expression factories (`IRqlMappingExpressionFactory`) enable DI-resolved complex mappings with `ExpressionFactoryHint` for processing strategies (e.g. `TakeFirst` for collections).
- **InternalsVisibleTo**: Configured via `Directory.Build.targets` — by default, internals are visible to `{AssemblyName}.Unit` test projects, enabling internal API testing.

## Test Structure

- **Rql.Tests.Common** — Shared fixtures, test entities, and helpers.
- **Rql.Tests.Unit** — xUnit tests using Moq and FluentAssertions. Covers filtering, ordering, projection, mapping (collection, conditional, inline, factory), parsing, and configuration.
- **Rql.Tests.Integration** — End-to-end tests with database scenarios.

## Tech Stack

- .NET 8.0, C# latest, nullable reference types enabled
- xUnit + Moq + FluentAssertions for testing, Coverlet for coverage
- CI: GitHub Actions (SonarCloud analysis on PR/push, NuGet publish on release)