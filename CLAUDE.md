# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**DataverseFakes.Community** is a modern, open-source testing framework for Dynamics 365 / Power Platform that enables unit testing of plugins, code activities, and applications by mocking the `IOrganizationService` with an in-memory context.

This is the community edition v1.0+ that focuses exclusively on **Dynamics 365 v9.x and later** (Power Platform / Common Data Service / Dataverse).

## Architecture

### Simplified Structure

The solution uses **Shared Projects** to organize code efficiently:

- **DataverseFakes**: Main library project (targets .NET Framework 4.6.2)
  - References DataverseFakes.Shared for all implementation code
  - Links to D365 v9.x SDK assemblies
- **DataverseFakes.Shared**: Contains all core implementation code
- **DataverseFakes.Tests**: Test project
  - References DataverseFakes.Tests.Shared for all test code
- **DataverseFakes.Tests.Shared**: Contains all test code

This structure eliminates code duplication while maintaining clear separation of concerns.

### Core Components

#### XrmFakedContext
The central class that simulates the CRM context (`DataverseFakes.Shared\XrmFakedContext*.cs`):
- **XrmFakedContext.cs**: Main context class with in-memory data storage (`Data` dictionary)
- **XrmFakedContext.Crud.cs**: CRUD operations (Create, Retrieve, Update, Delete)
- **XrmFakedContext.Queries.cs**: Query translation (QueryExpression, FetchXML, LINQ)
- **XrmFakedContext.Plugins.cs**: Plugin execution support
- **XrmFakedContext.CodeActivities.cs**: Workflow activity execution support
- **XrmFakedContext.Metadata.cs**: Metadata management
- **XrmFakedContext.Aggregations.cs**: Aggregate query support
- **XrmFakedContext.Pipeline.cs**: Plugin pipeline simulation

#### FakeMessageExecutors
Located in `DataverseFakes.Shared\FakeMessageExecutors\`, these handle specific CRM request types:
- Each executor implements `IFakeMessageExecutor`
- Examples: CreateRequestExecutor, UpdateRequestExecutor, RetrieveMultipleRequestExecutor, AssociateRequestExecutor, ExecuteMultipleRequestExecutor
- Can be extended with custom executors using `context.AddFakeMessageExecutor()`

#### Entity Initializer Services
Located in `DataverseFakes.Shared\Services\EntityInitializer\`, these initialize entities with default values when using `InitializeFromRequest`:
- DefaultEntityInitializerService
- InvoiceInitializerService
- InvoiceDetailInitializerService

## Building and Testing

### Prerequisites

- .NET Framework 4.6.2 or higher
- Visual Studio 2017+ or MSBuild tools
- NuGet CLI or dotnet CLI

### Build Commands

**Full build with tests** (default):
```bash
build.bat
```

**Individual commands**:
```bash
build.bat clean      # Clean build artifacts
build.bat restore    # Restore NuGet packages
build.bat build      # Build solution
build.bat test       # Run tests
build.bat pack       # Create NuGet package
```

### Running Tests

Tests use **xUnit2** and are located in `DataverseFakes.Tests.Shared\FakeContextTests\`.

**Run all tests**:
```bash
build.bat test
```

**Run tests directly with dotnet**:
```bash
dotnet test DataverseFakes.Tests\DataverseFakes.Tests.csproj --configuration Release
```

**Run a specific test class** (use Visual Studio Test Explorer or specify the filter):
```bash
dotnet test --filter "FullyQualifiedName~FakeContextTestPlugins"
```

## Testing Patterns

### Creating Tests

```csharp
var context = new XrmFakedContext();
context.Initialize(new List<Entity> { /* seed data */ });

// For early-bound entities
context.ProxyTypesAssembly = Assembly.GetExecutingAssembly();

// Set caller context
context.CallerId = new EntityReference("systemuser", Guid.NewGuid());

var service = context.GetOrganizationService();
```

### Testing Plugins

```csharp
var context = new XrmFakedContext();
var target = new Entity("account") { Id = Guid.NewGuid() };

// Execute plugin with target
context.ExecutePluginWithTarget<MyPlugin>(target);

// Or with full context control
var pluginContext = context.GetDefaultPluginContext();
pluginContext.MessageName = "Create";
pluginContext.Stage = 40; // Post-operation
context.ExecutePluginWith<MyPlugin>(pluginContext);
```

### Testing Code Activities (Workflows)

```csharp
var context = new XrmFakedContext();
var inputs = new Dictionary<string, object>
{
    { "InputParameter", someValue }
};

var result = context.ExecuteCodeActivity<MyActivity>(inputs);
var outputValue = result["OutputParameter"];
```

### Using FetchXML

```csharp
var fetchXml = @"<fetch><entity name='account'>
    <attribute name='name' />
    <filter><condition attribute='statecode' operator='eq' value='0' /></filter>
</entity></fetch>";

var collection = service.RetrieveMultiple(new FetchExpression(fetchXml));
```

## Key Conventions

1. **Metadata Inference**: The framework can infer metadata from early-bound types when `ProxyTypesAssembly` is set
2. **Relationships**: Define N:N relationships using `context.AddRelationship()` before associating entities
3. **Custom Message Executors**: Register with `context.AddFakeMessageExecutor<TRequest>(new MyCustomExecutor())`
4. **Generic Message Executors**: For custom actions, use `context.AddGenericFakeMessageExecutor("custom_Action", new MyExecutor())`
5. **Access Rights**: Use `context.AccessRightsRepository` to test security-related operations

## Development Guidelines

### Adding New Features

1. Add implementation to the appropriate file in `DataverseFakes.Shared\`
2. Add tests to `DataverseFakes.Tests.Shared\FakeContextTests\`
3. Run `build.bat test` to ensure all tests pass
4. Update documentation in README.md if adding public APIs

### Adding New Message Executors

1. Create a new class in `DataverseFakes.Shared\FakeMessageExecutors\`
2. Implement `IFakeMessageExecutor`
3. The executor will be automatically discovered and registered
4. Add comprehensive tests in `DataverseFakes.Tests.Shared\`

### Code Organization

- Keep partial class files focused (e.g., XrmFakedContext.Plugins.cs only contains plugin-related methods)
- Place extension methods in appropriate files under `DataverseFakes.Shared\Extensions\`
- Group related tests in subdirectories under `DataverseFakes.Tests.Shared\FakeContextTests\`

## NuGet Package

To create a NuGet package:

```bash
build.bat pack
```

The package will be created in the `nuget\` directory.

**Package details**:
- ID: DataverseFakes.Community
- Version: 1.0.1
- Target Framework: .NET Framework 4.6.2
- Dependencies: D365 v9.x SDK assemblies, FakeItEasy 6.0.0

## Target Platform

This community edition **only** supports:
- Dynamics 365 v9.x and later
- Power Platform / Common Data Service
- Dataverse

**Note**: Support for legacy CRM versions (2011, 2013, 2015, 2016, 365) has been removed to simplify maintenance and focus on modern platform features.

## Contributing

This is a truly open-source, community-driven project. Contributions are welcome!

When submitting PRs:
- Include unit tests for all changes
- Ensure `build.bat` runs successfully
- Update documentation as needed
- Follow existing code conventions and structure
