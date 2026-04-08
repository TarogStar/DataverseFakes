using System;
using System.Collections.Generic;
using System.Linq;
using Crm;
using FakeXrmEasy.Tests.PluginsForTesting;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Xunit;

namespace FakeXrmEasy.Tests
{
    public class PipelineTests
    {
        [Fact]
        public void When_context_is_initialised_pipeline_is_disabled_by_default()
        {
            var context = new XrmFakedContext();
            Assert.False(context.UsePipelineSimulation);
        }

        [Fact]
        public void When_AccountNumberPluginIsRegisteredAsPluginStep_And_OtherPluginCreatesAnAccount_Expect_AccountNumberIsSet()
        {
            var context = new XrmFakedContext() { UsePipelineSimulation = true };

            context.RegisterPluginStep<AccountNumberPlugin>("Create", ProcessingStepStage.Preoperation);

            context.ExecutePluginWith<CreateAccountPlugin>();

            var account = context.CreateQuery<Account>().FirstOrDefault();
            Assert.NotNull(account);
            Assert.True(account.Attributes.ContainsKey("accountnumber"));
            Assert.NotNull(account["accountnumber"]);
        }

        [Fact]
        public void When_PluginIsRegisteredWithEntity_And_OtherPluginCreatesAnAccount_Expect_AccountNumberIsSet()
        {
            var context = new XrmFakedContext() { UsePipelineSimulation = true }; 

            context.RegisterPluginStep<AccountNumberPlugin, Account>("Create");

            context.ExecutePluginWith<CreateAccountPlugin>();

            var account = context.CreateQuery<Account>().FirstOrDefault();
            Assert.NotNull(account);
            Assert.True(account.Attributes.ContainsKey("accountnumber"));
            Assert.NotNull(account["accountnumber"]);
        }

        [Fact]
        public void When_PluginIsRegisteredForOtherEntity_And_OtherPluginCreatesAnAccount_Expect_AccountNumberIsNotSet()
        {
            var context = new XrmFakedContext() { UsePipelineSimulation = true };

            context.RegisterPluginStep<AccountNumberPlugin, Contact>("Create");

            context.ExecutePluginWith<CreateAccountPlugin>();

            var account = context.CreateQuery<Account>().FirstOrDefault();
            Assert.NotNull(account);
            Assert.False(account.Attributes.ContainsKey("accountnumber"));
        }

        [Fact]
        public void When_PluginStepRegisteredAsDeletePreOperationSyncronous_Expect_CorrectValues()
        {
            // Arange
            var context = new XrmFakedContext() { UsePipelineSimulation = true };

            var id = Guid.NewGuid();

            var entities = new List<Entity>
            {
                new Contact
                {
                    Id = id
                }
            };
            context.Initialize(entities);

            // Act
            context.RegisterPluginStep<ValidatePipelinePlugin, Contact>("Delete", ProcessingStepStage.Preoperation, ProcessingStepMode.Synchronous);

            var service = context.GetOrganizationService();
            service.Delete(Contact.EntityLogicalName, id);

            // Assert
            var trace = context.GetFakeTracingService().DumpTrace().Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            Assert.Equal(5, trace.Length);
            Assert.Contains("Message Name: Delete", trace);
            Assert.Contains("Stage: 20", trace);
            Assert.Contains("Mode: 0", trace);
            Assert.Contains($"Entity Reference Logical Name: {Contact.EntityLogicalName}", trace);
            Assert.Contains($"Entity Reference ID: {id}", trace);
        }

        [Fact]
        public void When_PluginStepRegisteredAsDeletePostOperationSyncronous_Expect_CorrectValues()
        {
            // Arange
            var context = new XrmFakedContext() { UsePipelineSimulation = true };

            var id = Guid.NewGuid();

            var entities = new List<Entity>
            {
                new Contact
                {
                    Id = id
                }
            };
            context.Initialize(entities);

            // Act
            context.RegisterPluginStep<ValidatePipelinePlugin, Contact>("Delete", ProcessingStepStage.Postoperation, ProcessingStepMode.Synchronous);

            var service = context.GetOrganizationService();
            service.Delete(Contact.EntityLogicalName, id);

            // Assert
            var trace = context.GetFakeTracingService().DumpTrace().Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            Assert.Equal(5, trace.Length);
            Assert.Contains("Message Name: Delete", trace);
            Assert.Contains("Stage: 40", trace);
            Assert.Contains("Mode: 0", trace);
            Assert.Contains($"Entity Reference Logical Name: {Contact.EntityLogicalName}", trace);
            Assert.Contains($"Entity Reference ID: {id}", trace);
        }

        [Fact]
        public void When_PluginStepRegisteredAsDeletePostOperationAsyncronous_Expect_CorrectValues()
        {
            // Arange
            var context = new XrmFakedContext() { UsePipelineSimulation = true };

            var id = Guid.NewGuid();

            var entities = new List<Entity>
            {
                new Contact
                {
                    Id = id
                }
            };
            context.Initialize(entities);

            // Act
            context.RegisterPluginStep<ValidatePipelinePlugin, Contact>("Delete", ProcessingStepStage.Postoperation, ProcessingStepMode.Asynchronous);

            var service = context.GetOrganizationService();
            service.Delete(Contact.EntityLogicalName, id);

            // Assert
            var trace = context.GetFakeTracingService().DumpTrace().Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            Assert.Equal(5, trace.Length);
            Assert.Contains("Message Name: Delete", trace);
            Assert.Contains("Stage: 40", trace);
            Assert.Contains("Mode: 1", trace);
            Assert.Contains($"Entity Reference Logical Name: {Contact.EntityLogicalName}", trace);
            Assert.Contains($"Entity Reference ID: {id}", trace);
        }

        [Fact]
        public void When_PluginStepRegisteredAsUpdatePreOperationSyncronous_Expect_CorrectValues()
        {
            // Arange
            var context = new XrmFakedContext() { UsePipelineSimulation = true };

            var id = Guid.NewGuid();

            var entities = new List<Entity>
            {
                new Contact
                {
                    Id = id
                }
            };
            context.Initialize(entities);

            // Act
            context.RegisterPluginStep<ValidatePipelinePlugin, Contact>("Update", ProcessingStepStage.Preoperation, ProcessingStepMode.Synchronous);

            var updatedEntity = new Contact
            {
                Id = id
            };

            var service = context.GetOrganizationService();
            service.Update(updatedEntity);

            // Assert
            var trace = context.GetFakeTracingService().DumpTrace().Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            Assert.Equal(5, trace.Length);
            Assert.Contains("Message Name: Update", trace);
            Assert.Contains("Stage: 20", trace);
            Assert.Contains("Mode: 0", trace);
            Assert.Contains($"Entity Logical Name: {Contact.EntityLogicalName}", trace);
            Assert.Contains($"Entity ID: {id}", trace);
        }

        [Fact]
        public void When_PluginStepRegisteredAsUpdatePostOperationSyncronous_Expect_CorrectValues()
        {
            // Arange
            var context = new XrmFakedContext() { UsePipelineSimulation = true };

            var id = Guid.NewGuid();

            var entities = new List<Entity>
            {
                new Contact
                {
                    Id = id
                }
            };
            context.Initialize(entities);

            // Act
            context.RegisterPluginStep<ValidatePipelinePlugin, Contact>("Update", ProcessingStepStage.Postoperation, ProcessingStepMode.Synchronous);

            var updatedEntity = new Contact
            {
                Id = id
            };

            var service = context.GetOrganizationService();
            service.Update(updatedEntity);

            // Assert
            var trace = context.GetFakeTracingService().DumpTrace().Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            Assert.Equal(5, trace.Length);
            Assert.Contains("Message Name: Update", trace);
            Assert.Contains("Stage: 40", trace);
            Assert.Contains("Mode: 0", trace);
            Assert.Contains($"Entity Logical Name: {Contact.EntityLogicalName}", trace);
            Assert.Contains($"Entity ID: {id}", trace);
        }

        [Fact]
        public void When_PluginStepRegisteredAsUpdatePostOperationAsyncronous_Expect_CorrectValues()
        {
            // Arange
            var context = new XrmFakedContext() { UsePipelineSimulation = true };

            var id = Guid.NewGuid();

            var entities = new List<Entity>
            {
                new Contact
                {
                    Id = id
                }
            };
            context.Initialize(entities);

            // Act
            context.RegisterPluginStep<ValidatePipelinePlugin, Contact>("Update", ProcessingStepStage.Postoperation, ProcessingStepMode.Asynchronous);

            var updatedEntity = new Contact
            {
                Id = id
            };

            var service = context.GetOrganizationService();
            service.Update(updatedEntity);

            // Assert
            var trace = context.GetFakeTracingService().DumpTrace().Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            Assert.Equal(5, trace.Length);
            Assert.Contains("Message Name: Update", trace);
            Assert.Contains("Stage: 40", trace);
            Assert.Contains("Mode: 1", trace);
            Assert.Contains($"Entity Logical Name: {Contact.EntityLogicalName}", trace);
            Assert.Contains($"Entity ID: {id}", trace);
        }
        
        [Fact]
        public void When_PluginStepRegisteredAsCreatePreOperationSyncronous_Expect_CorrectValues()
        {
            // Arange
            var context = new XrmFakedContext() { UsePipelineSimulation = true };

            var id = Guid.NewGuid();

            // Act
            context.RegisterPluginStep<ValidatePipelinePlugin, Contact>("Create", ProcessingStepStage.Preoperation, ProcessingStepMode.Synchronous);

            var newEntity = new Contact
            {
                Id = id
            };

            var service = context.GetOrganizationService();
            service.Create(newEntity);

            // Assert
            var trace = context.GetFakeTracingService().DumpTrace().Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            Assert.Equal(5, trace.Length);
            Assert.Contains("Message Name: Create", trace);
            Assert.Contains("Stage: 20", trace);
            Assert.Contains("Mode: 0", trace);
            Assert.Contains($"Entity Logical Name: {Contact.EntityLogicalName}", trace);
            Assert.Contains($"Entity ID: {id}", trace);
        }

        [Fact]
        public void When_PluginStepRegisteredAsCreatePostOperationSyncronous_Expect_CorrectValues()
        {
            // Arange
            var context = new XrmFakedContext() { UsePipelineSimulation = true };

            var id = Guid.NewGuid();

            // Act
            context.RegisterPluginStep<ValidatePipelinePlugin, Contact>("Create", ProcessingStepStage.Postoperation, ProcessingStepMode.Synchronous);

            var newEntity = new Contact
            {
                Id = id
            };

            var service = context.GetOrganizationService();
            service.Create(newEntity);

            // Assert
            var trace = context.GetFakeTracingService().DumpTrace().Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            Assert.Equal(5, trace.Length);
            Assert.Contains("Message Name: Create", trace);
            Assert.Contains("Stage: 40", trace);
            Assert.Contains("Mode: 0", trace);
            Assert.Contains($"Entity Logical Name: {Contact.EntityLogicalName}", trace);
            Assert.Contains($"Entity ID: {id}", trace);
        }

        [Fact]
        public void When_PluginStepRegisteredAsCreatePostOperationAsyncronous_Expect_CorrectValues()
        {
            // Arange
            var context = new XrmFakedContext() { UsePipelineSimulation = true };

            var id = Guid.NewGuid();

            // Act
            context.RegisterPluginStep<ValidatePipelinePlugin, Contact>("Create", ProcessingStepStage.Postoperation, ProcessingStepMode.Asynchronous);

            var newEntity = new Contact
            {
                Id = id
            };

            var service = context.GetOrganizationService();
            service.Create(newEntity);

            // Assert
            var trace = context.GetFakeTracingService().DumpTrace().Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            Assert.Equal(5, trace.Length);
            Assert.Contains("Message Name: Create", trace);
            Assert.Contains("Stage: 40", trace);
            Assert.Contains("Mode: 1", trace);
            Assert.Contains($"Entity Logical Name: {Contact.EntityLogicalName}", trace);
            Assert.Contains($"Entity ID: {id}", trace);
        }

        [Fact]
        public void When_PluginStepRegisteredAsCreatePostOperation_Entity_Available()
        {
            var context = new XrmFakedContext {UsePipelineSimulation = true};

            var target = new Account
            {
                Id = Guid.NewGuid(),
                Name = "Original"
            };

            context.RegisterPluginStep<PostOperationUpdatePlugin>("Create");
            IOrganizationService serivce = context.GetOrganizationService();

            serivce.Create(target);

            var updatedAccount = serivce.Retrieve(Account.EntityLogicalName, target.Id, new ColumnSet(true)).ToEntity<Account>();

            Assert.Equal("Updated", updatedAccount.Name);
        }

        #region PreValidation Stage Tests (GitHub Issue #183)

        [Fact]
        public void When_PluginStep_Registered_For_Create_PreValidation_Should_AutoExecute()
        {
            // Arrange
            var context = new XrmFakedContext { UsePipelineSimulation = true };
            var id = Guid.NewGuid();

            context.RegisterPluginStep<ValidatePipelinePlugin, Contact>("Create", ProcessingStepStage.Prevalidation, ProcessingStepMode.Synchronous);

            var newEntity = new Contact { Id = id };

            // Act
            var service = context.GetOrganizationService();
            service.Create(newEntity);

            // Assert
            var trace = context.GetFakeTracingService().DumpTrace().Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            Assert.Equal(5, trace.Length);
            Assert.Contains("Message Name: Create", trace);
            Assert.Contains("Stage: 10", trace); // PreValidation = 10
            Assert.Contains("Mode: 0", trace);
            Assert.Contains($"Entity Logical Name: {Contact.EntityLogicalName}", trace);
            Assert.Contains($"Entity ID: {id}", trace);
        }

        [Fact]
        public void When_PluginStep_Registered_For_Update_PreValidation_Should_AutoExecute()
        {
            // Arrange
            var context = new XrmFakedContext { UsePipelineSimulation = true };
            var id = Guid.NewGuid();

            var entities = new List<Entity>
            {
                new Contact { Id = id }
            };
            context.Initialize(entities);

            context.RegisterPluginStep<ValidatePipelinePlugin, Contact>("Update", ProcessingStepStage.Prevalidation, ProcessingStepMode.Synchronous);

            var updatedEntity = new Contact { Id = id, FirstName = "Updated" };

            // Act
            var service = context.GetOrganizationService();
            service.Update(updatedEntity);

            // Assert
            var trace = context.GetFakeTracingService().DumpTrace().Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            Assert.Equal(5, trace.Length);
            Assert.Contains("Message Name: Update", trace);
            Assert.Contains("Stage: 10", trace); // PreValidation = 10
            Assert.Contains("Mode: 0", trace);
            Assert.Contains($"Entity Logical Name: {Contact.EntityLogicalName}", trace);
            Assert.Contains($"Entity ID: {id}", trace);
        }

        [Fact]
        public void When_PluginStep_Registered_For_Delete_PreValidation_Should_AutoExecute()
        {
            // Arrange
            var context = new XrmFakedContext { UsePipelineSimulation = true };
            var id = Guid.NewGuid();

            var entities = new List<Entity>
            {
                new Contact { Id = id }
            };
            context.Initialize(entities);

            context.RegisterPluginStep<ValidatePipelinePlugin, Contact>("Delete", ProcessingStepStage.Prevalidation, ProcessingStepMode.Synchronous);

            // Act
            var service = context.GetOrganizationService();
            service.Delete(Contact.EntityLogicalName, id);

            // Assert
            var trace = context.GetFakeTracingService().DumpTrace().Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            Assert.Equal(5, trace.Length);
            Assert.Contains("Message Name: Delete", trace);
            Assert.Contains("Stage: 10", trace); // PreValidation = 10
            Assert.Contains("Mode: 0", trace);
            Assert.Contains($"Entity Reference Logical Name: {Contact.EntityLogicalName}", trace);
            Assert.Contains($"Entity Reference ID: {id}", trace);
        }

        #endregion

        #region UsePipelineSimulation Tests (GitHub Issue #183)

        [Fact]
        public void When_UsePipelineSimulation_False_Plugin_Should_Not_AutoExecute_On_Create()
        {
            // Arrange
            var context = new XrmFakedContext { UsePipelineSimulation = false };
            CounterPlugin.ExecutionCount = 0; // Reset counter

            context.RegisterPluginStep<CounterPlugin, Contact>("Create", ProcessingStepStage.Postoperation, ProcessingStepMode.Synchronous);

            var newEntity = new Contact { Id = Guid.NewGuid() };

            // Act
            var service = context.GetOrganizationService();
            service.Create(newEntity);

            // Assert - plugin should not have executed
            Assert.Equal(0, CounterPlugin.ExecutionCount);
        }

        [Fact]
        public void When_UsePipelineSimulation_False_Plugin_Should_Not_AutoExecute_On_Update()
        {
            // Arrange
            var context = new XrmFakedContext { UsePipelineSimulation = false };
            var id = Guid.NewGuid();
            CounterPlugin.ExecutionCount = 0; // Reset counter

            var entities = new List<Entity>
            {
                new Contact { Id = id }
            };
            context.Initialize(entities);

            context.RegisterPluginStep<CounterPlugin, Contact>("Update", ProcessingStepStage.Postoperation, ProcessingStepMode.Synchronous);

            var updatedEntity = new Contact { Id = id, FirstName = "Updated" };

            // Act
            var service = context.GetOrganizationService();
            service.Update(updatedEntity);

            // Assert - plugin should not have executed
            Assert.Equal(0, CounterPlugin.ExecutionCount);
        }

        [Fact]
        public void When_UsePipelineSimulation_False_Plugin_Should_Not_AutoExecute_On_Delete()
        {
            // Arrange
            var context = new XrmFakedContext { UsePipelineSimulation = false };
            var id = Guid.NewGuid();
            CounterPlugin.ExecutionCount = 0; // Reset counter

            var entities = new List<Entity>
            {
                new Contact { Id = id }
            };
            context.Initialize(entities);

            context.RegisterPluginStep<CounterPlugin, Contact>("Delete", ProcessingStepStage.Postoperation, ProcessingStepMode.Synchronous);

            // Act
            var service = context.GetOrganizationService();
            service.Delete(Contact.EntityLogicalName, id);

            // Assert - plugin should not have executed
            Assert.Equal(0, CounterPlugin.ExecutionCount);
        }

        [Fact]
        public void When_UsePipelineSimulation_True_Plugin_Should_AutoExecute_On_Create()
        {
            // Arrange
            var context = new XrmFakedContext { UsePipelineSimulation = true };
            CounterPlugin.ExecutionCount = 0; // Reset counter

            context.RegisterPluginStep<CounterPlugin, Contact>("Create", ProcessingStepStage.Postoperation, ProcessingStepMode.Synchronous);

            var newEntity = new Contact { Id = Guid.NewGuid() };

            // Act
            var service = context.GetOrganizationService();
            service.Create(newEntity);

            // Assert - plugin should have executed exactly once
            Assert.Equal(1, CounterPlugin.ExecutionCount);
        }

        [Fact]
        public void When_UsePipelineSimulation_True_Plugin_Should_AutoExecute_On_Update()
        {
            // Arrange
            var context = new XrmFakedContext { UsePipelineSimulation = true };
            var id = Guid.NewGuid();
            CounterPlugin.ExecutionCount = 0; // Reset counter

            var entities = new List<Entity>
            {
                new Contact { Id = id }
            };
            context.Initialize(entities);

            context.RegisterPluginStep<CounterPlugin, Contact>("Update", ProcessingStepStage.Postoperation, ProcessingStepMode.Synchronous);

            var updatedEntity = new Contact { Id = id, FirstName = "Updated" };

            // Act
            var service = context.GetOrganizationService();
            service.Update(updatedEntity);

            // Assert - plugin should have executed exactly once
            Assert.Equal(1, CounterPlugin.ExecutionCount);
        }

        [Fact]
        public void When_UsePipelineSimulation_True_Plugin_Should_AutoExecute_On_Delete()
        {
            // Arrange
            var context = new XrmFakedContext { UsePipelineSimulation = true };
            var id = Guid.NewGuid();
            CounterPlugin.ExecutionCount = 0; // Reset counter

            var entities = new List<Entity>
            {
                new Contact { Id = id }
            };
            context.Initialize(entities);

            context.RegisterPluginStep<CounterPlugin, Contact>("Delete", ProcessingStepStage.Postoperation, ProcessingStepMode.Synchronous);

            // Act
            var service = context.GetOrganizationService();
            service.Delete(Contact.EntityLogicalName, id);

            // Assert - plugin should have executed exactly once
            Assert.Equal(1, CounterPlugin.ExecutionCount);
        }

        #endregion

        #region Full Pipeline Execution Order Tests (GitHub Issue #183)

        [Fact]
        public void When_Multiple_Plugin_Steps_Registered_Should_Execute_In_Correct_Order()
        {
            // Arrange
            var context = new XrmFakedContext { UsePipelineSimulation = true };
            var id = Guid.NewGuid();

            // Register plugins for all stages
            context.RegisterPluginStep<ValidatePipelinePlugin, Contact>("Create", ProcessingStepStage.Prevalidation, ProcessingStepMode.Synchronous);
            context.RegisterPluginStep<ValidatePipelinePlugin, Contact>("Create", ProcessingStepStage.Preoperation, ProcessingStepMode.Synchronous);
            context.RegisterPluginStep<ValidatePipelinePlugin, Contact>("Create", ProcessingStepStage.Postoperation, ProcessingStepMode.Synchronous);

            var newEntity = new Contact { Id = id };

            // Act
            var service = context.GetOrganizationService();
            service.Create(newEntity);

            // Assert
            var trace = context.GetFakeTracingService().DumpTrace();
            var traceLines = trace.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            // Should have 15 lines (5 per plugin execution * 3 plugins)
            Assert.Equal(15, traceLines.Length);

            // Verify stages executed in order: 10 (PreValidation), 20 (PreOperation), 40 (PostOperation)
            var stageLines = traceLines.Where(l => l.StartsWith("Stage:")).ToList();
            Assert.Equal(3, stageLines.Count);
            Assert.Equal("Stage: 10", stageLines[0]);
            Assert.Equal("Stage: 20", stageLines[1]);
            Assert.Equal("Stage: 40", stageLines[2]);
        }

        #endregion

        #region Pre/Post Entity Images Tests (GitHub Issue #183)

        [Fact]
        public void When_Create_Operation_PostImage_Should_Be_Available_In_PostOperation()
        {
            // Arrange
            var context = new XrmFakedContext { UsePipelineSimulation = true };
            var id = Guid.NewGuid();
            EntityImageValidationPlugin.Reset();

            context.RegisterPluginStep<EntityImageValidationPlugin, Contact>("Create", ProcessingStepStage.Postoperation, ProcessingStepMode.Synchronous);

            var newEntity = new Contact
            {
                Id = id,
                FirstName = "John",
                LastName = "Doe"
            };

            // Act
            var service = context.GetOrganizationService();
            service.Create(newEntity);

            // Assert - Create should have PostImage but NOT PreImage
            Assert.Equal(1, EntityImageValidationPlugin.ExecutionCount);
            Assert.False(EntityImageValidationPlugin.HasPreImage, "Create operation should NOT have a PreImage");
            Assert.True(EntityImageValidationPlugin.HasPostImage, "Create operation should have a PostImage");
            Assert.NotNull(EntityImageValidationPlugin.LastPostImage);
            Assert.Equal(id, EntityImageValidationPlugin.LastPostImage.Id);
        }

        [Fact]
        public void When_Create_Operation_PreImage_Should_NOT_Be_Available()
        {
            // Arrange
            var context = new XrmFakedContext { UsePipelineSimulation = true };
            var id = Guid.NewGuid();
            EntityImageValidationPlugin.Reset();

            context.RegisterPluginStep<EntityImageValidationPlugin, Contact>("Create", ProcessingStepStage.Postoperation, ProcessingStepMode.Synchronous);

            var newEntity = new Contact { Id = id };

            // Act
            var service = context.GetOrganizationService();
            service.Create(newEntity);

            // Assert - Create should NOT have PreImage (entity didn't exist before)
            Assert.False(EntityImageValidationPlugin.HasPreImage, "Create operation should NOT have a PreImage");
        }

        [Fact]
        public void When_Update_Operation_Both_PreImage_And_PostImage_Should_Be_Available()
        {
            // Arrange
            var context = new XrmFakedContext { UsePipelineSimulation = true };
            var id = Guid.NewGuid();
            EntityImageValidationPlugin.Reset();

            // Initialize with existing entity
            var existingEntity = new Contact
            {
                Id = id,
                FirstName = "Original",
                LastName = "Name"
            };
            context.Initialize(new List<Entity> { existingEntity });

            context.RegisterPluginStep<EntityImageValidationPlugin, Contact>("Update", ProcessingStepStage.Postoperation, ProcessingStepMode.Synchronous);

            var updatedEntity = new Contact
            {
                Id = id,
                FirstName = "Updated"
            };

            // Act
            var service = context.GetOrganizationService();
            service.Update(updatedEntity);

            // Assert - Update should have both PreImage and PostImage
            Assert.Equal(1, EntityImageValidationPlugin.ExecutionCount);
            Assert.True(EntityImageValidationPlugin.HasPreImage, "Update operation should have a PreImage");
            Assert.True(EntityImageValidationPlugin.HasPostImage, "Update operation should have a PostImage");
            Assert.NotNull(EntityImageValidationPlugin.LastPreImage);
            Assert.NotNull(EntityImageValidationPlugin.LastPostImage);
        }

        [Fact]
        public void When_Update_Operation_PreImage_Should_Contain_Original_Values()
        {
            // Arrange
            var context = new XrmFakedContext { UsePipelineSimulation = true };
            var id = Guid.NewGuid();
            EntityImageValidationPlugin.Reset();

            // Initialize with existing entity
            var existingEntity = new Contact
            {
                Id = id,
                FirstName = "Original"
            };
            context.Initialize(new List<Entity> { existingEntity });

            context.RegisterPluginStep<EntityImageValidationPlugin, Contact>("Update", ProcessingStepStage.Postoperation, ProcessingStepMode.Synchronous);

            var updatedEntity = new Contact
            {
                Id = id,
                FirstName = "Updated"
            };

            // Act
            var service = context.GetOrganizationService();
            service.Update(updatedEntity);

            // Assert - PreImage should contain the original value
            Assert.NotNull(EntityImageValidationPlugin.LastPreImage);
            Assert.Equal("Original", EntityImageValidationPlugin.LastPreImage.GetAttributeValue<string>("firstname"));
        }

        [Fact]
        public void When_Update_Operation_PostImage_Should_Contain_Updated_Values()
        {
            // Arrange
            var context = new XrmFakedContext { UsePipelineSimulation = true };
            var id = Guid.NewGuid();
            EntityImageValidationPlugin.Reset();

            // Initialize with existing entity
            var existingEntity = new Contact
            {
                Id = id,
                FirstName = "Original"
            };
            context.Initialize(new List<Entity> { existingEntity });

            context.RegisterPluginStep<EntityImageValidationPlugin, Contact>("Update", ProcessingStepStage.Postoperation, ProcessingStepMode.Synchronous);

            var updatedEntity = new Contact
            {
                Id = id,
                FirstName = "Updated"
            };

            // Act
            var service = context.GetOrganizationService();
            service.Update(updatedEntity);

            // Assert - PostImage should contain the updated value
            Assert.NotNull(EntityImageValidationPlugin.LastPostImage);
            Assert.Equal("Updated", EntityImageValidationPlugin.LastPostImage.GetAttributeValue<string>("firstname"));
        }

        [Fact]
        public void When_Delete_Operation_PreImage_Should_Be_Available()
        {
            // Arrange
            var context = new XrmFakedContext { UsePipelineSimulation = true };
            var id = Guid.NewGuid();
            EntityImageValidationPlugin.Reset();

            // Initialize with existing entity
            var existingEntity = new Contact
            {
                Id = id,
                FirstName = "ToBeDeleted"
            };
            context.Initialize(new List<Entity> { existingEntity });

            context.RegisterPluginStep<EntityImageValidationPlugin, Contact>("Delete", ProcessingStepStage.Postoperation, ProcessingStepMode.Synchronous);

            // Act
            var service = context.GetOrganizationService();
            service.Delete(Contact.EntityLogicalName, id);

            // Assert - Delete should have PreImage but NOT PostImage
            Assert.Equal(1, EntityImageValidationPlugin.ExecutionCount);
            Assert.True(EntityImageValidationPlugin.HasPreImage, "Delete operation should have a PreImage");
            Assert.False(EntityImageValidationPlugin.HasPostImage, "Delete operation should NOT have a PostImage");
            Assert.NotNull(EntityImageValidationPlugin.LastPreImage);
        }

        [Fact]
        public void When_Delete_Operation_PostImage_Should_NOT_Be_Available()
        {
            // Arrange
            var context = new XrmFakedContext { UsePipelineSimulation = true };
            var id = Guid.NewGuid();
            EntityImageValidationPlugin.Reset();

            // Initialize with existing entity
            var existingEntity = new Contact { Id = id };
            context.Initialize(new List<Entity> { existingEntity });

            context.RegisterPluginStep<EntityImageValidationPlugin, Contact>("Delete", ProcessingStepStage.Postoperation, ProcessingStepMode.Synchronous);

            // Act
            var service = context.GetOrganizationService();
            service.Delete(Contact.EntityLogicalName, id);

            // Assert - Delete should NOT have PostImage (entity no longer exists)
            Assert.False(EntityImageValidationPlugin.HasPostImage, "Delete operation should NOT have a PostImage");
        }

        [Fact]
        public void When_Delete_Operation_PreImage_Should_Contain_Entity_State_Before_Deletion()
        {
            // Arrange
            var context = new XrmFakedContext { UsePipelineSimulation = true };
            var id = Guid.NewGuid();
            EntityImageValidationPlugin.Reset();

            // Initialize with existing entity with attributes
            var existingEntity = new Contact
            {
                Id = id,
                FirstName = "John",
                LastName = "Doe"
            };
            context.Initialize(new List<Entity> { existingEntity });

            context.RegisterPluginStep<EntityImageValidationPlugin, Contact>("Delete", ProcessingStepStage.Postoperation, ProcessingStepMode.Synchronous);

            // Act
            var service = context.GetOrganizationService();
            service.Delete(Contact.EntityLogicalName, id);

            // Assert - PreImage should contain the entity values before deletion
            Assert.NotNull(EntityImageValidationPlugin.LastPreImage);
            Assert.Equal("John", EntityImageValidationPlugin.LastPreImage.GetAttributeValue<string>("firstname"));
            Assert.Equal("Doe", EntityImageValidationPlugin.LastPreImage.GetAttributeValue<string>("lastname"));
        }

        [Fact]
        public void When_Update_PreOperation_Should_Have_PreImage_But_Not_PostImage()
        {
            // Arrange
            var context = new XrmFakedContext { UsePipelineSimulation = true };
            var id = Guid.NewGuid();
            EntityImageValidationPlugin.Reset();

            // Initialize with existing entity
            var existingEntity = new Contact
            {
                Id = id,
                FirstName = "Original"
            };
            context.Initialize(new List<Entity> { existingEntity });

            // Register for pre-operation stage
            context.RegisterPluginStep<EntityImageValidationPlugin, Contact>("Update", ProcessingStepStage.Preoperation, ProcessingStepMode.Synchronous);

            var updatedEntity = new Contact
            {
                Id = id,
                FirstName = "Updated"
            };

            // Act
            var service = context.GetOrganizationService();
            service.Update(updatedEntity);

            // Assert - Pre-operation should have PreImage but NOT PostImage (update hasn't happened yet)
            Assert.True(EntityImageValidationPlugin.HasPreImage, "Update pre-operation should have a PreImage");
            Assert.False(EntityImageValidationPlugin.HasPostImage, "Update pre-operation should NOT have a PostImage");
        }

        [Fact]
        public void When_Entity_Images_Should_Contain_All_Attributes()
        {
            // Arrange
            var context = new XrmFakedContext { UsePipelineSimulation = true };
            var id = Guid.NewGuid();
            EntityImageValidationPlugin.Reset();

            // Initialize with existing entity with multiple attributes
            var existingEntity = new Contact
            {
                Id = id,
                FirstName = "John",
                LastName = "Doe",
                EMailAddress1 = "john.doe@example.com"
            };
            context.Initialize(new List<Entity> { existingEntity });

            context.RegisterPluginStep<EntityImageValidationPlugin, Contact>("Update", ProcessingStepStage.Postoperation, ProcessingStepMode.Synchronous);

            var updatedEntity = new Contact
            {
                Id = id,
                FirstName = "Jane"
            };

            // Act
            var service = context.GetOrganizationService();
            service.Update(updatedEntity);

            // Assert - PreImage should contain ALL attributes from the original entity
            Assert.NotNull(EntityImageValidationPlugin.LastPreImage);
            Assert.True(EntityImageValidationPlugin.LastPreImage.Contains("firstname"), "PreImage should contain firstname");
            Assert.True(EntityImageValidationPlugin.LastPreImage.Contains("lastname"), "PreImage should contain lastname");
            Assert.True(EntityImageValidationPlugin.LastPreImage.Contains("emailaddress1"), "PreImage should contain emailaddress1");
            Assert.Equal("John", EntityImageValidationPlugin.LastPreImage.GetAttributeValue<string>("firstname"));
            Assert.Equal("Doe", EntityImageValidationPlugin.LastPreImage.GetAttributeValue<string>("lastname"));
            Assert.Equal("john.doe@example.com", EntityImageValidationPlugin.LastPreImage.GetAttributeValue<string>("emailaddress1"));
        }

        #endregion

    }
}
