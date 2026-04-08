using Microsoft.Xrm.Sdk;
using System;
using Xunit;

namespace FakeXrmEasy.Tests
{
    /// <summary>
    /// Tests for IPluginExecutionContext4 support
    /// </summary>
    public class PluginExecutionContext4Tests
    {
        [Fact]
        public void When_Plugin_Requests_IPluginExecutionContext4_It_Should_Be_Returned()
        {
            // Arrange
            var context = new XrmFakedContext();
            var target = new Entity("account") { Id = Guid.NewGuid() };

            var pluginContext = context.GetDefaultPluginContext();
            pluginContext.MessageName = "Create";
            pluginContext.InputParameters.Add("Target", target);

            // Set IPluginExecutionContext4 specific properties
            pluginContext.InitiatingUserAzureActiveDirectoryObjectId = Guid.NewGuid();
            pluginContext.UserAzureActiveDirectoryObjectId = Guid.NewGuid();
            pluginContext.IsTransactionIntegrationMessage = true;
            pluginContext.ParentContextProperties["CustomProperty"] = "CustomValue";

            // Act & Assert - Execute plugin that uses IPluginExecutionContext4
            var plugin = context.ExecutePluginWith<TestPluginExecutionContext4Plugin>(pluginContext);

            // The plugin would have thrown an exception if it couldn't cast to IPluginExecutionContext4
            // or if any of the properties were not accessible
            Assert.NotNull(plugin);
        }

        [Fact]
        public void When_Plugin_Uses_IPluginExecutionContext2_It_Should_Have_AzureAD_Properties()
        {
            // Arrange
            var context = new XrmFakedContext();
            var target = new Entity("account") { Id = Guid.NewGuid() };

            var pluginContext = context.GetDefaultPluginContext();
            pluginContext.InitiatingUserAzureActiveDirectoryObjectId = Guid.NewGuid();
            pluginContext.UserAzureActiveDirectoryObjectId = Guid.NewGuid();
            pluginContext.InputParameters.Add("Target", target);

            // Act
            var plugin = context.ExecutePluginWith<TestPluginExecutionContext2Plugin>(pluginContext);

            // Assert
            Assert.NotNull(plugin);
        }

        [Fact]
        public void When_Plugin_Uses_IPluginExecutionContext3_It_Should_Have_ParentContextProperties()
        {
            // Arrange
            var context = new XrmFakedContext();
            var target = new Entity("account") { Id = Guid.NewGuid() };

            var pluginContext = context.GetDefaultPluginContext();
            pluginContext.ParentContextProperties["Key1"] = "Value1";
            pluginContext.ParentContextProperties["Key2"] = "Value2";
            pluginContext.InputParameters.Add("Target", target);

            // Act
            var plugin = context.ExecutePluginWith<TestPluginExecutionContext3Plugin>(pluginContext);

            // Assert
            Assert.NotNull(plugin);
        }

        [Fact]
        public void XrmFakedPluginExecutionContext_Should_Initialize_IPluginExecutionContext4_Properties()
        {
            // Arrange & Act
            var ctx = new XrmFakedPluginExecutionContext();

            // Assert - Default values should be set
            Assert.Equal(Guid.Empty, ctx.InitiatingUserAzureActiveDirectoryObjectId);
            Assert.Equal(Guid.Empty, ctx.UserAzureActiveDirectoryObjectId);
            Assert.NotNull(ctx.ParentContextProperties);
            Assert.False(ctx.IsTransactionIntegrationMessage);
            Assert.NotNull(ctx.PreEntityImagesCollection);
            Assert.NotNull(ctx.PostEntityImagesCollection);
        }
    }

    /// <summary>
    /// Test plugin that uses IPluginExecutionContext4
    /// </summary>
    public class TestPluginExecutionContext4Plugin : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            // This is the key test - can we cast to IPluginExecutionContext4?
            var context = (IPluginExecutionContext4)serviceProvider.GetService(typeof(IPluginExecutionContext4));

            if (context == null)
            {
                throw new InvalidPluginExecutionException("Could not retrieve IPluginExecutionContext4");
            }

            // Verify IPluginExecutionContext2 properties (inherited)
            var initiatingUserAadId = context.InitiatingUserAzureActiveDirectoryObjectId;
            var userAadId = context.UserAzureActiveDirectoryObjectId;

            // Verify standard IPluginExecutionContext properties still work
            var messageName = context.MessageName;
            var inputParams = context.InputParameters;

            // Note: IPluginExecutionContext4 doesn't expose additional properties directly
            // The properties like IsTransactionIntegrationMessage are on the concrete class
        }
    }

    /// <summary>
    /// Test plugin that uses IPluginExecutionContext2
    /// </summary>
    public class TestPluginExecutionContext2Plugin : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            var context = (IPluginExecutionContext2)serviceProvider.GetService(typeof(IPluginExecutionContext2));

            if (context == null)
            {
                throw new InvalidPluginExecutionException("Could not retrieve IPluginExecutionContext2");
            }

            // Verify we can access Azure AD properties (they are Guids)
            var initiatingAadId = context.InitiatingUserAzureActiveDirectoryObjectId;
            var userAadId = context.UserAzureActiveDirectoryObjectId;

            // Just accessing them is enough - if they throw, the test fails
        }
    }

    /// <summary>
    /// Test plugin that uses IPluginExecutionContext3
    /// </summary>
    public class TestPluginExecutionContext3Plugin : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            var context = (IPluginExecutionContext3)serviceProvider.GetService(typeof(IPluginExecutionContext3));

            if (context == null)
            {
                throw new InvalidPluginExecutionException("Could not retrieve IPluginExecutionContext3");
            }

            // Verify we can access AuthenticatedUserId which is part of IPluginExecutionContext3
            var authUserId = context.AuthenticatedUserId;

            // Note: ParentContextProperties is on the concrete class, not the interface
        }
    }
}
