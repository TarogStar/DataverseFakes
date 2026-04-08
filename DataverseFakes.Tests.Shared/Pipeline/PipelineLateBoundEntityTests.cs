using System;
using FakeXrmEasy.Extensions;
using FakeXrmEasy.Tests.PluginsForTesting;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using Xunit;

namespace FakeXrmEasy.Tests.Pipeline
{
    /// <summary>
    /// Tests for plugin pipeline functionality with late-bound Entity class.
    /// These tests verify that the pipeline works correctly when using generic Entity
    /// instead of early-bound entity classes with EntityTypeCode fields.
    /// </summary>
    public class PipelineLateBoundEntityTests
    {
        private const int AccountObjectTypeCode = 1;
        private const int ContactObjectTypeCode = 2;

        /// <summary>
        /// When a late-bound entity is used with ObjectTypeCode in EntityMetadata,
        /// the pipeline should correctly match registered plugins.
        /// </summary>
        [Fact]
        public void When_LateBoundEntityWithMetadata_Pipeline_ShouldExecuteMatchingPlugin()
        {
            // Arrange
            var context = new XrmFakedContext { UsePipelineSimulation = true };

            // Initialize metadata with ObjectTypeCode
            var accountMetadata = new EntityMetadata { LogicalName = "account" };
            accountMetadata.SetFieldValue("ObjectTypeCode", AccountObjectTypeCode);
            context.InitializeMetadata(accountMetadata);

            var id = Guid.NewGuid();

            // Register plugin step with explicit entity type code
            context.RegisterPluginStep<ValidatePipelinePlugin>(
                "Create",
                ProcessingStepStage.Preoperation,
                ProcessingStepMode.Synchronous,
                rank: 1,
                filteringAttributes: null,
                primaryEntityTypeCode: AccountObjectTypeCode);

            // Act - Create a late-bound entity
            var lateBoundEntity = new Entity("account") { Id = id };
            var service = context.GetOrganizationService();
            service.Create(lateBoundEntity);

            // Assert
            var trace = context.GetFakeTracingService().DumpTrace().Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            Assert.Equal(5, trace.Length);
            Assert.Contains("Message Name: Create", trace);
            Assert.Contains("Stage: 20", trace);
            Assert.Contains("Mode: 0", trace);
            Assert.Contains("Entity Logical Name: account", trace);
            Assert.Contains($"Entity ID: {id}", trace);
        }

        /// <summary>
        /// When a late-bound entity is used and metadata provides ObjectTypeCode,
        /// the pipeline should not execute plugins registered for different entity types.
        /// </summary>
        [Fact]
        public void When_LateBoundEntityWithMetadata_AndPluginForDifferentEntity_ShouldNotExecutePlugin()
        {
            // Arrange
            var context = new XrmFakedContext { UsePipelineSimulation = true };

            // Initialize metadata with ObjectTypeCode for both entities
            var accountMetadata = new EntityMetadata { LogicalName = "account" };
            accountMetadata.SetFieldValue("ObjectTypeCode", AccountObjectTypeCode);

            var contactMetadata = new EntityMetadata { LogicalName = "contact" };
            contactMetadata.SetFieldValue("ObjectTypeCode", ContactObjectTypeCode);

            context.InitializeMetadata(new[] { accountMetadata, contactMetadata });

            var id = Guid.NewGuid();

            // Register plugin step for contact (different entity type)
            context.RegisterPluginStep<ValidatePipelinePlugin>(
                "Create",
                ProcessingStepStage.Preoperation,
                ProcessingStepMode.Synchronous,
                rank: 1,
                filteringAttributes: null,
                primaryEntityTypeCode: ContactObjectTypeCode);

            // Act - Create a late-bound account entity
            var lateBoundEntity = new Entity("account") { Id = id };
            var service = context.GetOrganizationService();
            service.Create(lateBoundEntity);

            // Assert - Plugin should NOT have executed (registered for contact, not account)
            var trace = context.GetFakeTracingService().DumpTrace();
            Assert.True(string.IsNullOrEmpty(trace), "Plugin should not have executed for account when registered for contact");
        }

        /// <summary>
        /// When a plugin is registered without entity filtering (primaryEntityTypeCode is null),
        /// it should execute for all entities including late-bound entities.
        /// </summary>
        [Fact]
        public void When_PluginRegisteredWithoutEntityFilter_ShouldExecuteForLateBoundEntity()
        {
            // Arrange
            var context = new XrmFakedContext { UsePipelineSimulation = true };

            var id = Guid.NewGuid();

            // Register plugin step without entity filtering
            context.RegisterPluginStep<ValidatePipelinePlugin>(
                "Create",
                ProcessingStepStage.Preoperation,
                ProcessingStepMode.Synchronous,
                rank: 1,
                filteringAttributes: null,
                primaryEntityTypeCode: null);

            // Act - Create a late-bound entity (no metadata needed since no entity filtering)
            var lateBoundEntity = new Entity("customentity") { Id = id };
            var service = context.GetOrganizationService();
            service.Create(lateBoundEntity);

            // Assert
            var trace = context.GetFakeTracingService().DumpTrace().Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            Assert.Equal(5, trace.Length);
            Assert.Contains("Message Name: Create", trace);
            Assert.Contains("Entity Logical Name: customentity", trace);
        }

        /// <summary>
        /// When a late-bound entity has no metadata and plugin is registered with entity type code,
        /// the plugin should not execute (entity type code cannot be determined).
        /// </summary>
        [Fact]
        public void When_LateBoundEntityWithoutMetadata_AndPluginWithEntityFilter_ShouldNotExecute()
        {
            // Arrange
            var context = new XrmFakedContext { UsePipelineSimulation = true };

            var id = Guid.NewGuid();

            // Register plugin step with entity type code
            context.RegisterPluginStep<ValidatePipelinePlugin>(
                "Create",
                ProcessingStepStage.Preoperation,
                ProcessingStepMode.Synchronous,
                rank: 1,
                filteringAttributes: null,
                primaryEntityTypeCode: AccountObjectTypeCode);

            // Act - Create a late-bound entity WITHOUT metadata
            var lateBoundEntity = new Entity("account") { Id = id };
            var service = context.GetOrganizationService();
            service.Create(lateBoundEntity);

            // Assert - Plugin should NOT have executed (entity type code cannot be determined)
            var trace = context.GetFakeTracingService().DumpTrace();
            Assert.True(string.IsNullOrEmpty(trace),
                "Plugin should not have executed when entity type code cannot be determined from late-bound entity without metadata");
        }

        /// <summary>
        /// When attempting to register a plugin step using RegisterPluginStep&lt;TPlugin, TEntity&gt;
        /// with a generic Entity type (which has no EntityTypeCode), an appropriate exception should be thrown.
        /// </summary>
        [Fact]
        public void When_RegisterPluginStepWithGenericEntityType_ShouldThrowHelpfulException()
        {
            // Arrange
            var context = new XrmFakedContext { UsePipelineSimulation = true };

            // Act & Assert - Trying to register with Entity type should throw
            var exception = Assert.Throws<InvalidOperationException>(() =>
            {
                context.RegisterPluginStep<ValidatePipelinePlugin, Entity>("Create");
            });

            // Verify the error message is helpful
            Assert.Contains("late-bound", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("RegisterPluginStep", exception.Message);
        }
    }
}
