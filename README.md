# AIProjectTest

> An experimental library management system built entirely using AI agentic coding

## Overview

AIProjectTest is an experimental project that demonstrates the capabilities of AI-driven software development. This project implements a generic library storage system using modern software engineering practices and design patterns, with a particular focus on **Type Descriptors** and maintaining **minimum 80% code coverage**.

The entire codebase was generated through AI agentic coding techniques, showcasing how AI can architect, implement, and test a complete application following clean architecture principles.

##  Architecture

The project follows **Clean Architecture** principles with clear separation of concerns:

```
AIProjectTest/
├── Library.Domain/              # Core business logic and entities
│   ├── Resources/              
│   └── TypeDescriptors/         # Type descriptor implementations
├── Library.Application/         # Application business rules
│   ├── Resources/              
│   └── TypeDescriptors/         # Application-level type descriptors
├── Library. Infrastructure/      # External services and data access
├── Library.WebApi/              # API presentation layer
│   ├── Controllers/            
│   ├── Errors/                  # Error handling
│   ├── Middleware/              # Custom middleware
│   └── Program.cs               # Application entry point
├── Library.Tests. Unit/          # Unit tests
├── Library.Tests.Integration/   # Integration tests
└── Library.Tests.Api/           # API tests
```

##  Key Features

### Type Descriptor Pattern
The system implements a generic Type Descriptor pattern that allows for:
- Dynamic type information and metadata management
- Flexible resource handling across different entity types
- Runtime type inspection and manipulation

### High Code Coverage
- **Minimum 80% code coverage** requirement
- Comprehensive unit tests
- Integration testing suite
- API endpoint testing
- Coverage configuration via `coverage.runsettings`

### Clean Architecture
- **Domain Layer**: Pure business logic with no external dependencies
- **Application Layer**: Use cases and application-specific business rules
- **Infrastructure Layer**: Database access, external services, and cross-cutting concerns
- **WebApi Layer**: RESTful API endpoints with proper error handling and middleware

### Database
- SQLite database (`library.db`) for lightweight, file-based storage
- Entity Framework Core for data access
- Repository pattern implementation

##  Getting Started

### Prerequisites
- .NET 8.0 or later
- Visual Studio 2022 or VS Code

### Running the Application

```bash
# Clone the repository
git clone https://github.com/RamiObaid92/AIProjectTest.git

# Navigate to the WebApi project
cd AIProjectTest/Library.WebApi

# Run the application
dotnet run
```

The API will be available at `https://localhost:5001` (or as configured in `appsettings.json`)

### Running Tests

```bash
# Run all tests
dotnet test

# Run with code coverage
dotnet test --settings coverage.runsettings /p:CollectCoverage=true
```

##  Testing Strategy

The project maintains high quality through three testing layers:

1. **Unit Tests** (`Library.Tests.Unit`): Test individual components in isolation
2. **Integration Tests** (`Library.Tests.Integration`): Test component interactions and database operations
3. **API Tests** (`Library.Tests.Api`): End-to-end API endpoint testing

##  AI Agentic Coding

This project serves as a proof-of-concept for AI-driven development, demonstrating: 

-  Complete application architecture designed by AI
-  Implementation of complex design patterns (Type Descriptors, Repository, CQRS)
-  Comprehensive test coverage generation
-  Clean code organization and separation of concerns
-  Production-ready error handling and middleware
-  Database integration and migrations

##  API Documentation

API documentation is available through Swagger UI when running the application in Development mode:

```
https://localhost:5001/swagger
```

Sample requests can be found in `Library.WebApi. http` for testing with HTTP clients.

##  Technologies

- **Framework**: .NET 8.0
- **Database**: SQLite with Entity Framework Core
- **API**:  ASP.NET Core Web API
- **Testing**: xUnit
- **Architecture**: Clean Architecture
- **Patterns**: Type Descriptors, Repository, Dependency Injection

##  Project Goals

1.  Implement a generic library storage system
2.  Demonstrate Type Descriptor pattern usage
3.  Achieve minimum 80% code coverage
4.  Build entire system using AI agentic coding
5.  Follow clean architecture principles

##  Contributing

This is an experimental project. Contributions, issues, and feature requests are welcome! 

##  License

This project is for experimental and educational purposes. 

##  Author

**RamiObaid92**

- GitHub: [@RamiObaid92](https://github.com/RamiObaid92)

---

*Built with AI Agentic Coding*
