#if FAKE_XRM_EASY_9
using FakeXrmEasy.Extensions;
using FakeXrmEasy.Tests.PluginsForTesting;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using Xunit;

namespace FakeXrmEasy.Tests.Features
{
    public class RetrieveMultipleDataProviderTesting
    {
        [Fact]
        public void IServiceProvider_should_has_IEntityDataSourceRetrieverService_in_v9()
        {
            var context = new XrmFakedContext
            {
                EntityDataSourceRetriever = new Entity("abc_customdatasource")
                {
                    ["abc_crmurl"] = "https://...",
                    ["abc_username"] = "abcd",
                    ["abc_password"] = "1234"
                }
            };
            var pluginContext = context.GetDefaultPluginContext();
            var entity = new Entity();
            var query = new QueryExpression();
            pluginContext.InputParameters["Query"] = query;

            context.ExecutePluginWithConfigurations<RetrieveMultipleDataProvider>(pluginContext, null, null);

            var outputParameters = pluginContext.OutputParameters["BusinessEntityCollection"] as EntityCollection;
            Assert.Equal(2, outputParameters.Entities.Count);
            Assert.Equal("abc_dataprovider", outputParameters.EntityName);
        }

        /// <summary>
        /// GitHub issue #579: Backward compatibility test - manual EntityDataSourceRetriever still works.
        /// When EntityDataSourceRetriever is explicitly set, it should be used regardless of metadata.
        /// </summary>
        [Fact]
        public void EntityDataSourceRetriever_ManualSetting_ShouldTakePrecedenceOverMetadata()
        {
            // Arrange
            var dataSourceId = Guid.NewGuid();
            var manualDataSource = new Entity("entitydatasource")
            {
                Id = Guid.NewGuid(), // Different ID
                ["abc_crmurl"] = "https://manual...",
                ["abc_username"] = "manual_user",
                ["abc_password"] = "manual_pass"
            };

            var metadataDataSource = new Entity("entitydatasource")
            {
                Id = dataSourceId,
                ["abc_crmurl"] = "https://metadata...",
                ["abc_username"] = "metadata_user",
                ["abc_password"] = "metadata_pass"
            };

            var context = new XrmFakedContext
            {
                // Manually set EntityDataSourceRetriever
                EntityDataSourceRetriever = manualDataSource
            };

            // Initialize the entitydatasource entity in Data (for metadata lookup)
            context.Initialize(new List<Entity> { metadataDataSource });

            // Set up EntityMetadata with DataSourceId pointing to metadataDataSource
            var entityMetadata = new EntityMetadata { LogicalName = "abc_virtualentity" };
            entityMetadata.SetFieldValue("DataSourceId", dataSourceId);
            context.InitializeMetadata(entityMetadata);

            // Set the current virtual entity name
            context.CurrentVirtualEntityLogicalName = "abc_virtualentity";

            // Act
            var service = context.GetFakedEntityDataSourceRetrieverService();
            var result = service.RetrieveEntityDataSource();

            // Assert - should return the manually set EntityDataSourceRetriever, not the one from metadata
            Assert.NotNull(result);
            Assert.Equal("https://manual...", result.GetAttributeValue<string>("abc_crmurl"));
            Assert.Equal("manual_user", result.GetAttributeValue<string>("abc_username"));
        }

        /// <summary>
        /// GitHub issue #579: Auto-lookup test - when EntityDataSourceRetriever is null,
        /// the entity data source should be automatically looked up from metadata's DataSourceId.
        /// </summary>
        [Fact]
        public void EntityDataSourceRetriever_WhenNull_ShouldAutoLookupFromMetadataDataSourceId()
        {
            // Arrange
            var dataSourceId = Guid.NewGuid();
            var entityDataSource = new Entity("entitydatasource")
            {
                Id = dataSourceId,
                ["abc_crmurl"] = "https://autolookup...",
                ["abc_username"] = "autolookup_user",
                ["abc_password"] = "autolookup_pass"
            };

            var context = new XrmFakedContext();
            // Do NOT set EntityDataSourceRetriever - it should be null

            // Initialize the entitydatasource entity in Data
            context.Initialize(new List<Entity> { entityDataSource });

            // Set up EntityMetadata with DataSourceId
            var entityMetadata = new EntityMetadata { LogicalName = "abc_virtualentity" };
            entityMetadata.SetFieldValue("DataSourceId", dataSourceId);
            context.InitializeMetadata(entityMetadata);

            // Set the current virtual entity name
            context.CurrentVirtualEntityLogicalName = "abc_virtualentity";

            // Act
            var service = context.GetFakedEntityDataSourceRetrieverService();
            var result = service.RetrieveEntityDataSource();

            // Assert - should return the entity data source from metadata lookup
            Assert.NotNull(result);
            Assert.Equal(dataSourceId, result.Id);
            Assert.Equal("https://autolookup...", result.GetAttributeValue<string>("abc_crmurl"));
            Assert.Equal("autolookup_user", result.GetAttributeValue<string>("abc_username"));
            Assert.Equal("autolookup_pass", result.GetAttributeValue<string>("abc_password"));
        }

        /// <summary>
        /// GitHub issue #579: Auto-lookup returns null when no metadata is configured.
        /// </summary>
        [Fact]
        public void EntityDataSourceRetriever_WhenNullAndNoMetadata_ShouldReturnNull()
        {
            // Arrange
            var context = new XrmFakedContext();
            // Do NOT set EntityDataSourceRetriever or any metadata

            context.CurrentVirtualEntityLogicalName = "abc_virtualentity";

            // Act
            var service = context.GetFakedEntityDataSourceRetrieverService();
            var result = service.RetrieveEntityDataSource();

            // Assert
            Assert.Null(result);
        }

        /// <summary>
        /// GitHub issue #579: Auto-lookup returns null when DataSourceId is not set in metadata.
        /// </summary>
        [Fact]
        public void EntityDataSourceRetriever_WhenNullAndNoDataSourceIdInMetadata_ShouldReturnNull()
        {
            // Arrange
            var context = new XrmFakedContext();

            // Set up EntityMetadata WITHOUT DataSourceId
            var entityMetadata = new EntityMetadata { LogicalName = "abc_virtualentity" };
            context.InitializeMetadata(entityMetadata);

            context.CurrentVirtualEntityLogicalName = "abc_virtualentity";

            // Act
            var service = context.GetFakedEntityDataSourceRetrieverService();
            var result = service.RetrieveEntityDataSource();

            // Assert
            Assert.Null(result);
        }

        /// <summary>
        /// GitHub issue #579: Auto-lookup returns null when entitydatasource entity doesn't exist.
        /// </summary>
        [Fact]
        public void EntityDataSourceRetriever_WhenNullAndEntityDataSourceNotInData_ShouldReturnNull()
        {
            // Arrange
            var dataSourceId = Guid.NewGuid();
            var context = new XrmFakedContext();

            // Set up EntityMetadata with DataSourceId, but don't initialize the entitydatasource entity
            var entityMetadata = new EntityMetadata { LogicalName = "abc_virtualentity" };
            entityMetadata.SetFieldValue("DataSourceId", dataSourceId);
            context.InitializeMetadata(entityMetadata);

            context.CurrentVirtualEntityLogicalName = "abc_virtualentity";

            // Act
            var service = context.GetFakedEntityDataSourceRetrieverService();
            var result = service.RetrieveEntityDataSource();

            // Assert
            Assert.Null(result);
        }

        /// <summary>
        /// GitHub issue #579: Auto-lookup should work when virtual entity name is obtained from plugin context.
        /// </summary>
        [Fact]
        public void EntityDataSourceRetriever_AutoLookupFromPluginContext_ShouldWork()
        {
            // Arrange
            var dataSourceId = Guid.NewGuid();
            var entityDataSource = new Entity("entitydatasource")
            {
                Id = dataSourceId,
                ["abc_crmurl"] = "https://fromplugincontext...",
                ["abc_username"] = "plugin_user",
                ["abc_password"] = "plugin_pass"
            };

            var context = new XrmFakedContext();

            // Initialize the entitydatasource entity in Data
            context.Initialize(new List<Entity> { entityDataSource });

            // Set up EntityMetadata with DataSourceId for the virtual entity
            var entityMetadata = new EntityMetadata { LogicalName = "abc_virtualentity" };
            entityMetadata.SetFieldValue("DataSourceId", dataSourceId);
            context.InitializeMetadata(entityMetadata);

            // Create plugin context with PrimaryEntityName set
            var pluginContext = context.GetDefaultPluginContext();
            pluginContext.PrimaryEntityName = "abc_virtualentity";

            // Get faked service provider (this should set CurrentVirtualEntityLogicalName from plugin context)
            var fakedServiceProvider = typeof(XrmFakedContext)
                .GetMethod("GetFakedServiceProvider", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(context, new object[] { pluginContext }) as IServiceProvider;

            // Act
            var retrieverService = fakedServiceProvider.GetService(typeof(IEntityDataSourceRetrieverService)) as IEntityDataSourceRetrieverService;
            var result = retrieverService.RetrieveEntityDataSource();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(dataSourceId, result.Id);
            Assert.Equal("https://fromplugincontext...", result.GetAttributeValue<string>("abc_crmurl"));
        }

        /// <summary>
        /// GitHub issue #579: Auto-lookup should work when virtual entity name is obtained from Query in plugin context.
        /// </summary>
        [Fact]
        public void EntityDataSourceRetriever_AutoLookupFromQueryExpression_ShouldWork()
        {
            // Arrange
            var dataSourceId = Guid.NewGuid();
            var entityDataSource = new Entity("entitydatasource")
            {
                Id = dataSourceId,
                ["abc_crmurl"] = "https://fromquery...",
                ["abc_username"] = "query_user",
                ["abc_password"] = "query_pass"
            };

            var context = new XrmFakedContext();

            // Initialize the entitydatasource entity in Data
            context.Initialize(new List<Entity> { entityDataSource });

            // Set up EntityMetadata with DataSourceId for the virtual entity
            var entityMetadata = new EntityMetadata { LogicalName = "abc_queryentity" };
            entityMetadata.SetFieldValue("DataSourceId", dataSourceId);
            context.InitializeMetadata(entityMetadata);

            // Create plugin context with Query in InputParameters (no PrimaryEntityName)
            var pluginContext = context.GetDefaultPluginContext();
            pluginContext.PrimaryEntityName = null;
            var query = new QueryExpression("abc_queryentity");
            pluginContext.InputParameters["Query"] = query;

            // Get faked service provider (this should set CurrentVirtualEntityLogicalName from query)
            var fakedServiceProvider = typeof(XrmFakedContext)
                .GetMethod("GetFakedServiceProvider", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(context, new object[] { pluginContext }) as IServiceProvider;

            // Act
            var retrieverService = fakedServiceProvider.GetService(typeof(IEntityDataSourceRetrieverService)) as IEntityDataSourceRetrieverService;
            var result = retrieverService.RetrieveEntityDataSource();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(dataSourceId, result.Id);
            Assert.Equal("https://fromquery...", result.GetAttributeValue<string>("abc_crmurl"));
        }
    }
}
#endif
