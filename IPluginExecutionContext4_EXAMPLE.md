# IPluginExecutionContext4 Support

DataverseFakes now fully supports `IPluginExecutionContext4` and all its predecessor interfaces (`IPluginExecutionContext`, `IPluginExecutionContext2`, `IPluginExecutionContext3`).

## What's New

### IPluginExecutionContext2 (Added properties)
- `InitiatingUserAzureActiveDirectoryObjectId` - The Azure AD Object ID of the user who initiated the operation
- `UserAzureActiveDirectoryObjectId` - The Azure AD Object ID of the user on whose behalf the code is running

### IPluginExecutionContext3 (Added properties)
- `ParentContextProperties` - A collection of custom properties from the parent context

### IPluginExecutionContext4 (Added properties)
- `IsTransactionIntegrationMessage` - Indicates whether the message is part of a transaction integration

## Usage Example

### Basic Plugin Using IPluginExecutionContext4

```csharp
public class MyModernPlugin : IPlugin
{
    public void Execute(IServiceProvider serviceProvider)
    {
        // Now you can request IPluginExecutionContext4 directly
        var context = (IPluginExecutionContext4)serviceProvider.GetService(typeof(IPluginExecutionContext4));

        // Access IPluginExecutionContext4 properties
        var isTransactionMsg = context.IsTransactionIntegrationMessage;

        // Access IPluginExecutionContext2 properties (inherited)
        var initiatingUserAadId = context.InitiatingUserAzureActiveDirectoryObjectId;
        var userAadId = context.UserAzureActiveDirectoryObjectId;

        // Access IPluginExecutionContext3 properties (inherited)
        var customProps = context.ParentContextProperties;

        // All standard IPluginExecutionContext properties still available
        var target = context.InputParameters["Target"] as Entity;
        var userId = context.UserId;
    }
}
```

### Unit Testing with IPluginExecutionContext4

```csharp
[Fact]
public void Test_Plugin_With_IPluginExecutionContext4()
{
    // Arrange
    var context = new XrmFakedContext();
    var target = new Entity("account")
    {
        Id = Guid.NewGuid(),
        ["name"] = "Test Account"
    };

    var pluginContext = context.GetDefaultPluginContext();
    pluginContext.MessageName = "Create";
    pluginContext.InputParameters.Add("Target", target);

    // Set IPluginExecutionContext4-specific properties
    pluginContext.InitiatingUserAzureActiveDirectoryObjectId = "aad-guid-initiating";
    pluginContext.UserAzureActiveDirectoryObjectId = "aad-guid-user";
    pluginContext.IsTransactionIntegrationMessage = true;
    pluginContext.ParentContextProperties["CustomKey"] = "CustomValue";

    // Act
    var plugin = context.ExecutePluginWith<MyModernPlugin>(pluginContext);

    // Assert
    Assert.NotNull(plugin);
}
```

### Testing with IPluginExecutionContext2

```csharp
[Fact]
public void Test_Plugin_With_Azure_AD_Properties()
{
    // Arrange
    var context = new XrmFakedContext();
    var pluginContext = context.GetDefaultPluginContext();

    // Set Azure AD Object IDs
    pluginContext.InitiatingUserAzureActiveDirectoryObjectId = "12345678-1234-1234-1234-123456789abc";
    pluginContext.UserAzureActiveDirectoryObjectId = "87654321-4321-4321-4321-cba987654321";

    pluginContext.InputParameters.Add("Target", new Entity("contact"));

    // Act
    context.ExecutePluginWith<PluginThatUsesAzureAD>(pluginContext);

    // Assert - Plugin executed without errors
}
```

### Testing with IPluginExecutionContext3

```csharp
[Fact]
public void Test_Plugin_With_Parent_Context_Properties()
{
    // Arrange
    var context = new XrmFakedContext();
    var pluginContext = context.GetDefaultPluginContext();

    // Add custom properties that would come from parent context
    pluginContext.ParentContextProperties["WorkflowId"] = "workflow-guid";
    pluginContext.ParentContextProperties["CustomSetting"] = "enabled";

    pluginContext.InputParameters.Add("Target", new Entity("account"));

    // Act
    context.ExecutePluginWith<PluginThatUsesParentProperties>(pluginContext);

    // Assert - Plugin executed without errors
}
```

## Backward Compatibility

All existing code continues to work. You can still request `IPluginExecutionContext` and it will work perfectly:

```csharp
// This still works exactly as before
var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
```

## Implementation Details

- `XrmFakedPluginExecutionContext` now implements `IPluginExecutionContext4`
- The service provider (`GetService`) returns the same context instance for all interface versions
- All properties are initialized with sensible defaults in the constructor
- Properties can be set individually on the `XrmFakedPluginExecutionContext` before executing the plugin

## Default Values

When you create a new `XrmFakedPluginExecutionContext`, the new properties are initialized as follows:

```csharp
InitiatingUserAzureActiveDirectoryObjectId = string.Empty
UserAzureActiveDirectoryObjectId = string.Empty
ParentContextProperties = new DataCollection<string, string>()
IsTransactionIntegrationMessage = false
```

## Complete Example

See [PluginExecutionContext4Tests.cs](DataverseFakes.Tests.Shared/FakeContextTests/PluginExecutionContext4Tests.cs) for comprehensive test examples.
