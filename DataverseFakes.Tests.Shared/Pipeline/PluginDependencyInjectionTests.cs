using System;
using System.Linq;
using Crm;
using FakeXrmEasy.Tests.PluginsForTesting;
using Microsoft.Xrm.Sdk;
using Xunit;

namespace FakeXrmEasy.Tests.Pipeline
{
    /// <summary>
    /// Tests for plugin dependency injection support in RegisterPluginStep.
    /// Covers GitHub Issue #501.
    /// </summary>
    public class PluginDependencyInjectionTests
    {
        [Fact]
        public void RegisterPluginStep_With_Instance_Should_Use_Provided_Instance()
        {
            // Arrange
            var context = new XrmFakedContext { UsePipelineSimulation = true };
            var mockService = new MockExternalService("Injected Value");
            var pluginInstance = new PluginWithDependency(mockService);

            context.RegisterPluginStep<PluginWithDependency, Account>("Create", pluginInstance, ProcessingStepStage.Preoperation);

            var service = context.GetOrganizationService();

            // Act
            var accountId = service.Create(new Account
            {
                Id = Guid.NewGuid(),
                Name = "Test Account"
            });

            // Assert
            var createdAccount = service.Retrieve(Account.EntityLogicalName, accountId, new Microsoft.Xrm.Sdk.Query.ColumnSet(true));
            Assert.Equal("Injected Value", createdAccount.GetAttributeValue<string>("description"));
        }

        [Fact]
        public void RegisterPluginStep_With_Factory_Should_Call_Factory_Each_Time()
        {
            // Arrange
            var context = new XrmFakedContext { UsePipelineSimulation = true };
            int factoryCallCount = 0;

            // Factory creates a new instance each time and increments the counter
            Func<ExecutionCounterPlugin> factory = () =>
            {
                factoryCallCount++;
                return new ExecutionCounterPlugin();
            };

            context.RegisterPluginStep<ExecutionCounterPlugin, Account>("Create", factory, ProcessingStepStage.Preoperation);

            var service = context.GetOrganizationService();

            // Act - Create 3 accounts
            for (int i = 0; i < 3; i++)
            {
                service.Create(new Account
                {
                    Id = Guid.NewGuid(),
                    Name = $"Test Account {i}"
                });
            }

            // Assert - Factory should have been called 3 times (once per Create)
            Assert.Equal(3, factoryCallCount);

            // Each execution should have count = 1 because it's a fresh instance
            var accounts = context.CreateQuery<Account>().ToList();
            foreach (var account in accounts)
            {
                Assert.Equal(1, account.GetAttributeValue<int>("executioncount"));
            }
        }

        [Fact]
        public void RegisterPluginStep_With_Instance_Same_Instance_Reused()
        {
            // Arrange
            var context = new XrmFakedContext { UsePipelineSimulation = true };
            var pluginInstance = new ExecutionCounterPlugin();

            context.RegisterPluginStep<ExecutionCounterPlugin, Account>("Create", pluginInstance, ProcessingStepStage.Preoperation);

            var service = context.GetOrganizationService();

            // Act - Create 3 accounts
            for (int i = 0; i < 3; i++)
            {
                service.Create(new Account
                {
                    Id = Guid.NewGuid(),
                    Name = $"Test Account {i}"
                });
            }

            // Assert - Same instance should have been used, so execution count should be 3
            Assert.Equal(3, pluginInstance.ExecutionCount);

            // Each execution should show incrementing count because same instance is reused
            var accounts = context.CreateQuery<Account>().OrderBy(a => a.GetAttributeValue<int>("executioncount")).ToList();
            Assert.Equal(1, accounts[0].GetAttributeValue<int>("executioncount"));
            Assert.Equal(2, accounts[1].GetAttributeValue<int>("executioncount"));
            Assert.Equal(3, accounts[2].GetAttributeValue<int>("executioncount"));
        }

        [Fact]
        public void RegisterPluginStep_With_Dependencies_Should_Work()
        {
            // Arrange
            var context = new XrmFakedContext { UsePipelineSimulation = true };

            // Simulate a complex dependency injection scenario
            var mockService = new MockExternalService("DI Works!");
            var pluginWithDependency = new PluginWithDependency(mockService);

            // Register for both Create and Update
            context.RegisterPluginStep<PluginWithDependency, Contact>("Create", pluginWithDependency, ProcessingStepStage.Preoperation);
            context.RegisterPluginStep<PluginWithDependency, Contact>("Update", pluginWithDependency, ProcessingStepStage.Preoperation);

            var service = context.GetOrganizationService();

            // Act - Create a contact
            var contactId = service.Create(new Contact
            {
                Id = Guid.NewGuid(),
                FirstName = "John"
            });

            // Assert - Verify plugin executed with injected dependency
            var createdContact = service.Retrieve(Contact.EntityLogicalName, contactId, new Microsoft.Xrm.Sdk.Query.ColumnSet(true));
            Assert.Equal("DI Works!", createdContact.GetAttributeValue<string>("description"));

            // Act - Update the contact
            service.Update(new Contact
            {
                Id = contactId,
                LastName = "Doe"
            });

            // Assert - Plugin should still work with the injected dependency
            var updatedContact = service.Retrieve(Contact.EntityLogicalName, contactId, new Microsoft.Xrm.Sdk.Query.ColumnSet(true));
            Assert.Equal("DI Works!", updatedContact.GetAttributeValue<string>("description"));
        }

        [Fact]
        public void RegisterPluginStep_With_Null_Instance_Should_Throw()
        {
            // Arrange
            var context = new XrmFakedContext { UsePipelineSimulation = true };

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                context.RegisterPluginStep<PluginWithDependency, Account>("Create", (PluginWithDependency)null));
        }

        [Fact]
        public void RegisterPluginStep_With_Null_Factory_Should_Throw()
        {
            // Arrange
            var context = new XrmFakedContext { UsePipelineSimulation = true };

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                context.RegisterPluginStep<ExecutionCounterPlugin, Account>("Create", (Func<ExecutionCounterPlugin>)null));
        }

        [Fact]
        public void RegisterPluginStep_Without_Entity_Type_With_Instance_Should_Work()
        {
            // Arrange
            var context = new XrmFakedContext { UsePipelineSimulation = true };
            var mockService = new MockExternalService("Global Plugin");
            var pluginInstance = new PluginWithDependency(mockService);

            // Register without entity filter - should apply to all entities
            context.RegisterPluginStep<PluginWithDependency>("Create", pluginInstance, ProcessingStepStage.Preoperation);

            var service = context.GetOrganizationService();

            // Act - Create an account
            var accountId = service.Create(new Account
            {
                Id = Guid.NewGuid(),
                Name = "Test Account"
            });

            // Assert
            var createdAccount = service.Retrieve(Account.EntityLogicalName, accountId, new Microsoft.Xrm.Sdk.Query.ColumnSet(true));
            Assert.Equal("Global Plugin", createdAccount.GetAttributeValue<string>("description"));
        }

        [Fact]
        public void RegisterPluginStep_Without_Entity_Type_With_Factory_Should_Work()
        {
            // Arrange
            var context = new XrmFakedContext { UsePipelineSimulation = true };
            int factoryCallCount = 0;

            Func<ExecutionCounterPlugin> factory = () =>
            {
                factoryCallCount++;
                return new ExecutionCounterPlugin();
            };

            // Register without entity filter
            context.RegisterPluginStep<ExecutionCounterPlugin>("Create", factory, ProcessingStepStage.Preoperation);

            var service = context.GetOrganizationService();

            // Act
            service.Create(new Account { Id = Guid.NewGuid(), Name = "Account" });
            service.Create(new Contact { Id = Guid.NewGuid(), FirstName = "John" });

            // Assert - Factory should have been called twice
            Assert.Equal(2, factoryCallCount);
        }

        [Fact]
        public void Mixed_Registration_Types_Should_Work_Together()
        {
            // Arrange
            var context = new XrmFakedContext { UsePipelineSimulation = true };

            // Register some plugins with instances, some with factories, some with default (type-based)
            var mockService = new MockExternalService("Instance Plugin");
            var instancePlugin = new PluginWithDependency(mockService);

            int factoryCallCount = 0;
            Func<ExecutionCounterPlugin> factory = () =>
            {
                factoryCallCount++;
                return new ExecutionCounterPlugin();
            };

            // Instance for Account Create (pre-op)
            context.RegisterPluginStep<PluginWithDependency, Account>("Create", instancePlugin, ProcessingStepStage.Preoperation);

            // Factory for Account Create (post-op)
            context.RegisterPluginStep<ExecutionCounterPlugin, Account>("Create", factory, ProcessingStepStage.Postoperation);

            // Type-based for Account Update
            context.RegisterPluginStep<ValidatePipelinePlugin, Account>("Update", ProcessingStepStage.Preoperation);

            var service = context.GetOrganizationService();

            // Act
            var accountId = service.Create(new Account
            {
                Id = Guid.NewGuid(),
                Name = "Test Account"
            });

            // Assert
            var account = service.Retrieve(Account.EntityLogicalName, accountId, new Microsoft.Xrm.Sdk.Query.ColumnSet(true));

            // Instance plugin should have set description
            Assert.Equal("Instance Plugin", account.GetAttributeValue<string>("description"));

            // Factory should have been called once
            Assert.Equal(1, factoryCallCount);

            // Factory plugin should have set execution count
            Assert.Equal(1, account.GetAttributeValue<int>("executioncount"));
        }
    }
}
