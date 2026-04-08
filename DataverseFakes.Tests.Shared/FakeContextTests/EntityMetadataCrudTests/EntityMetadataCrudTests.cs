using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.ServiceModel;
using Xunit;

namespace FakeXrmEasy.Tests.FakeContextTests.EntityMetadataCrudTests
{
    /// <summary>
    /// Comprehensive tests for Entity Metadata CRUD operations (CreateEntityRequest,
    /// UpdateEntityRequest, DeleteEntityRequest).
    /// </summary>
    public class EntityMetadataCrudTests
    {
        #region CreateEntityRequest Tests

        [Fact]
        public void CreateEntity_ShouldSucceed()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var entityMetadata = new EntityMetadata
            {
                LogicalName = "new_testentity",
                DisplayName = new Label("Test Entity", 1033),
                Description = new Label("A test entity for unit testing", 1033),
                SchemaName = "new_TestEntity"
            };

            var request = new CreateEntityRequest
            {
                Entity = entityMetadata,
                PrimaryAttribute = new StringAttributeMetadata
                {
                    LogicalName = "new_name",
                    SchemaName = "new_Name",
                    DisplayName = new Label("Name", 1033),
                    RequiredLevel = new AttributeRequiredLevelManagedProperty(AttributeRequiredLevel.ApplicationRequired),
                    MaxLength = 100
                }
            };

            // Act
            var response = (CreateEntityResponse)service.Execute(request);

            // Assert
            Assert.NotNull(response);
            var metadataId = (Guid)response.Results["EntityId"];
            Assert.NotEqual(Guid.Empty, metadataId);

            // Verify entity was added to metadata cache
            var retrievedMetadata = context.GetEntityMetadataByName("new_testentity");
            Assert.NotNull(retrievedMetadata);
            Assert.Equal("new_testentity", retrievedMetadata.LogicalName);
        }

        [Fact]
        public void CreateEntityWithAttributes_ShouldStoreAttributes()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var entityMetadata = new EntityMetadata
            {
                LogicalName = "new_customentity",
                DisplayName = new Label("Custom Entity", 1033)
            };

            // Set up attributes using reflection (EntityMetadata.Attributes is internal set)
            var primaryAttribute = new StringAttributeMetadata
            {
                LogicalName = "new_name",
                SchemaName = "new_Name",
                DisplayName = new Label("Name", 1033),
                MaxLength = 200
            };

            var request = new CreateEntityRequest
            {
                Entity = entityMetadata,
                PrimaryAttribute = primaryAttribute
            };

            // Act
            var response = (CreateEntityResponse)service.Execute(request);

            // Assert
            Assert.NotNull(response);
            var retrievedMetadata = context.GetEntityMetadataByName("new_customentity");
            Assert.NotNull(retrievedMetadata);
            Assert.Equal("new_customentity", retrievedMetadata.LogicalName);
            Assert.Equal("Custom Entity", retrievedMetadata.DisplayName.LocalizedLabels[0].Label);

            // Verify primary attribute was stored
            Assert.NotNull(retrievedMetadata.Attributes);
            Assert.Contains(retrievedMetadata.Attributes, a => a.LogicalName == "new_name");
        }

        [Fact]
        public void CreateDuplicateEntity_ShouldThrowFault()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            // Pre-populate with existing entity
            var existingMetadata = new EntityMetadata { LogicalName = "new_existingentity" };
            context.InitializeMetadata(existingMetadata);

            var request = new CreateEntityRequest
            {
                Entity = new EntityMetadata { LogicalName = "new_existingentity" },
                PrimaryAttribute = new StringAttributeMetadata { LogicalName = "new_name" }
            };

            // Act & Assert
            Assert.Throws<FaultException<OrganizationServiceFault>>(() => service.Execute(request));
        }

        [Fact]
        public void CreateEntityWithNullLogicalName_ShouldThrowFault()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var request = new CreateEntityRequest
            {
                Entity = new EntityMetadata { LogicalName = null },
                PrimaryAttribute = new StringAttributeMetadata { LogicalName = "new_name" }
            };

            // Act & Assert
            Assert.Throws<FaultException<OrganizationServiceFault>>(() => service.Execute(request));
        }

        [Fact]
        public void CreateEntityWithEmptyLogicalName_ShouldThrowFault()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var request = new CreateEntityRequest
            {
                Entity = new EntityMetadata { LogicalName = "" },
                PrimaryAttribute = new StringAttributeMetadata { LogicalName = "new_name" }
            };

            // Act & Assert
            Assert.Throws<FaultException<OrganizationServiceFault>>(() => service.Execute(request));
        }

        [Fact]
        public void CreateEntityWithNullEntity_ShouldThrowFault()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var request = new CreateEntityRequest
            {
                Entity = null,
                PrimaryAttribute = new StringAttributeMetadata { LogicalName = "new_name" }
            };

            // Act & Assert
            Assert.Throws<FaultException<OrganizationServiceFault>>(() => service.Execute(request));
        }

        [Theory]
        [InlineData("new_simpleentity")]
        [InlineData("prefix_entityname")]
        [InlineData("new_entity_with_underscores")]
        public void CreateEntityWithVariousNames_ShouldSucceed(string entityLogicalName)
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var request = new CreateEntityRequest
            {
                Entity = new EntityMetadata { LogicalName = entityLogicalName },
                PrimaryAttribute = new StringAttributeMetadata { LogicalName = "new_name" }
            };

            // Act
            var response = (CreateEntityResponse)service.Execute(request);

            // Assert
            Assert.NotNull(response);
            var retrievedMetadata = context.GetEntityMetadataByName(entityLogicalName);
            Assert.NotNull(retrievedMetadata);
            Assert.Equal(entityLogicalName, retrievedMetadata.LogicalName);
        }

        #endregion

        #region UpdateEntityRequest Tests

        [Fact]
        public void UpdateEntityDisplayName_ShouldSucceed()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var existingEntityMetadata = new EntityMetadata
            {
                LogicalName = "new_updateentity",
                DisplayName = new Label("Old Display Name", 1033)
            };
            context.InitializeMetadata(existingEntityMetadata);

            var updatedEntityMetadata = new EntityMetadata
            {
                LogicalName = "new_updateentity",
                DisplayName = new Label("New Display Name", 1033)
            };

            var request = new UpdateEntityRequest
            {
                Entity = updatedEntityMetadata
            };

            // Act
            var response = (UpdateEntityResponse)service.Execute(request);

            // Assert
            Assert.NotNull(response);
            var retrievedMetadata = context.GetEntityMetadataByName("new_updateentity");
            Assert.Equal("New Display Name", retrievedMetadata.DisplayName.LocalizedLabels[0].Label);
        }

        [Fact]
        public void UpdateNonExistentEntity_ShouldThrowFault()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var request = new UpdateEntityRequest
            {
                Entity = new EntityMetadata
                {
                    LogicalName = "nonexistent_entity",
                    DisplayName = new Label("Some Name", 1033)
                }
            };

            // Act & Assert
            Assert.Throws<FaultException<OrganizationServiceFault>>(() => service.Execute(request));
        }

        [Fact]
        public void UpdateEntityDescription_ShouldSucceed()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var existingEntityMetadata = new EntityMetadata
            {
                LogicalName = "new_descriptionentity",
                Description = new Label("Old Description", 1033)
            };
            context.InitializeMetadata(existingEntityMetadata);

            var updatedEntityMetadata = new EntityMetadata
            {
                LogicalName = "new_descriptionentity",
                Description = new Label("New Description", 1033)
            };

            var request = new UpdateEntityRequest
            {
                Entity = updatedEntityMetadata
            };

            // Act
            var response = (UpdateEntityResponse)service.Execute(request);

            // Assert
            Assert.NotNull(response);
            var retrievedMetadata = context.GetEntityMetadataByName("new_descriptionentity");
            Assert.Equal("New Description", retrievedMetadata.Description.LocalizedLabels[0].Label);
        }

        [Fact]
        public void UpdateEntityWithNullEntity_ShouldThrowFault()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var request = new UpdateEntityRequest
            {
                Entity = null
            };

            // Act & Assert
            Assert.Throws<FaultException<OrganizationServiceFault>>(() => service.Execute(request));
        }

        [Fact]
        public void UpdateEntityWithEmptyLogicalName_ShouldThrowFault()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var request = new UpdateEntityRequest
            {
                Entity = new EntityMetadata { LogicalName = "" }
            };

            // Act & Assert
            Assert.Throws<FaultException<OrganizationServiceFault>>(() => service.Execute(request));
        }

        [Fact]
        public void UpdateEntityPreservesUnchangedProperties()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var existingEntityMetadata = new EntityMetadata
            {
                LogicalName = "new_preserveprops",
                DisplayName = new Label("Original Display Name", 1033),
                Description = new Label("Original Description", 1033)
            };
            context.InitializeMetadata(existingEntityMetadata);

            // Only update DisplayName, leave Description unchanged
            var updatedEntityMetadata = new EntityMetadata
            {
                LogicalName = "new_preserveprops",
                DisplayName = new Label("Updated Display Name", 1033)
            };

            var request = new UpdateEntityRequest
            {
                Entity = updatedEntityMetadata
            };

            // Act
            service.Execute(request);

            // Assert
            var retrievedMetadata = context.GetEntityMetadataByName("new_preserveprops");
            Assert.Equal("Updated Display Name", retrievedMetadata.DisplayName.LocalizedLabels[0].Label);
            Assert.Equal("Original Description", retrievedMetadata.Description.LocalizedLabels[0].Label);
        }

        [Fact]
        public void UpdateEntityIsAuditEnabled_ShouldSucceed()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var existingEntityMetadata = new EntityMetadata
            {
                LogicalName = "new_auditable",
                IsAuditEnabled = new BooleanManagedProperty(false)
            };
            context.InitializeMetadata(existingEntityMetadata);

            var updatedEntityMetadata = new EntityMetadata
            {
                LogicalName = "new_auditable",
                IsAuditEnabled = new BooleanManagedProperty(true)
            };

            var request = new UpdateEntityRequest
            {
                Entity = updatedEntityMetadata
            };

            // Act
            var response = (UpdateEntityResponse)service.Execute(request);

            // Assert
            Assert.NotNull(response);
            var retrievedMetadata = context.GetEntityMetadataByName("new_auditable");
            Assert.True(retrievedMetadata.IsAuditEnabled.Value);
        }

        #endregion

        #region DeleteEntityRequest Tests

        [Fact]
        public void DeleteEntity_ShouldSucceed()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var entityMetadata = new EntityMetadata { LogicalName = "new_tobedeleted" };
            context.InitializeMetadata(entityMetadata);

            var request = new DeleteEntityRequest
            {
                LogicalName = "new_tobedeleted"
            };

            // Act
            var response = (DeleteEntityResponse)service.Execute(request);

            // Assert
            Assert.NotNull(response);
            var retrievedMetadata = context.GetEntityMetadataByName("new_tobedeleted");
            Assert.Null(retrievedMetadata);
        }

        [Fact]
        public void DeleteNonExistentEntity_ShouldThrowFault()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var request = new DeleteEntityRequest
            {
                LogicalName = "nonexistent_entity"
            };

            // Act & Assert
            Assert.Throws<FaultException<OrganizationServiceFault>>(() => service.Execute(request));
        }

        [Fact]
        public void DeleteEntityAlsoRemovesData()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            // Initialize entity metadata
            var entityMetadata = new EntityMetadata { LogicalName = "new_dataentity" };
            context.InitializeMetadata(entityMetadata);

            // Initialize some test data for this entity
            var entityId = Guid.NewGuid();
            context.Initialize(new List<Entity>
            {
                new Entity("new_dataentity", entityId) { ["new_name"] = "Test Record 1" },
                new Entity("new_dataentity", Guid.NewGuid()) { ["new_name"] = "Test Record 2" }
            });

            // Verify data exists
            var query = new QueryExpression("new_dataentity");
            var results = service.RetrieveMultiple(query);
            Assert.Equal(2, results.Entities.Count);

            var request = new DeleteEntityRequest
            {
                LogicalName = "new_dataentity"
            };

            // Act
            var response = (DeleteEntityResponse)service.Execute(request);

            // Assert
            Assert.NotNull(response);

            // Verify metadata was deleted
            var retrievedMetadata = context.GetEntityMetadataByName("new_dataentity");
            Assert.Null(retrievedMetadata);

            // Verify data was also deleted
            var query2 = new QueryExpression("new_dataentity");
            var results2 = service.RetrieveMultiple(query2);
            Assert.Empty(results2.Entities);
        }

        [Fact]
        public void DeleteEntityWithNullLogicalName_ShouldThrowFault()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var request = new DeleteEntityRequest
            {
                LogicalName = null
            };

            // Act & Assert
            Assert.Throws<FaultException<OrganizationServiceFault>>(() => service.Execute(request));
        }

        [Fact]
        public void DeleteEntityWithEmptyLogicalName_ShouldThrowFault()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var request = new DeleteEntityRequest
            {
                LogicalName = ""
            };

            // Act & Assert
            Assert.Throws<FaultException<OrganizationServiceFault>>(() => service.Execute(request));
        }

        [Fact]
        public void DeleteEntityDoesNotAffectOtherEntities()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var entity1 = new EntityMetadata { LogicalName = "new_entity1" };
            var entity2 = new EntityMetadata { LogicalName = "new_entity2" };
            var entity3 = new EntityMetadata { LogicalName = "new_entity3" };

            context.InitializeMetadata(new List<EntityMetadata> { entity1, entity2, entity3 });

            // Act - Delete entity2
            var request = new DeleteEntityRequest { LogicalName = "new_entity2" };
            service.Execute(request);

            // Assert - Other entities should still exist
            var metadata1 = context.GetEntityMetadataByName("new_entity1");
            var metadata2 = context.GetEntityMetadataByName("new_entity2");
            var metadata3 = context.GetEntityMetadataByName("new_entity3");

            Assert.NotNull(metadata1);
            Assert.Null(metadata2);
            Assert.NotNull(metadata3);
        }

        [Fact]
        public void VerifyDeletedEntityNotRetrievable()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var entityMetadata = new EntityMetadata { LogicalName = "new_deletedentity" };
            context.InitializeMetadata(entityMetadata);

            // First, delete the entity
            var deleteRequest = new DeleteEntityRequest
            {
                LogicalName = "new_deletedentity"
            };
            service.Execute(deleteRequest);

            // Now, try to retrieve it using RetrieveEntityRequest
            var retrieveRequest = new RetrieveEntityRequest
            {
                EntityFilters = EntityFilters.Entity,
                LogicalName = "new_deletedentity"
            };

            // Act & Assert - Should throw because the entity no longer exists
            Assert.Throws<Exception>(() => service.Execute(retrieveRequest));
        }

        #endregion

        #region Integration Tests

        [Fact]
        public void CreateThenUpdateThenDeleteEntity_ShouldSucceed()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            // Create
            var createRequest = new CreateEntityRequest
            {
                Entity = new EntityMetadata
                {
                    LogicalName = "new_lifecycle",
                    DisplayName = new Label("Initial Name", 1033),
                    Description = new Label("Initial Description", 1033)
                },
                PrimaryAttribute = new StringAttributeMetadata
                {
                    LogicalName = "new_name",
                    DisplayName = new Label("Name", 1033)
                }
            };
            var createResponse = (CreateEntityResponse)service.Execute(createRequest);
            Assert.NotNull(createResponse);
            Assert.NotNull(context.GetEntityMetadataByName("new_lifecycle"));

            // Update
            var updateRequest = new UpdateEntityRequest
            {
                Entity = new EntityMetadata
                {
                    LogicalName = "new_lifecycle",
                    DisplayName = new Label("Updated Name", 1033)
                }
            };
            var updateResponse = (UpdateEntityResponse)service.Execute(updateRequest);
            Assert.NotNull(updateResponse);
            var updatedMetadata = context.GetEntityMetadataByName("new_lifecycle");
            Assert.Equal("Updated Name", updatedMetadata.DisplayName.LocalizedLabels[0].Label);

            // Delete
            var deleteRequest = new DeleteEntityRequest
            {
                LogicalName = "new_lifecycle"
            };
            var deleteResponse = (DeleteEntityResponse)service.Execute(deleteRequest);
            Assert.NotNull(deleteResponse);
            Assert.Null(context.GetEntityMetadataByName("new_lifecycle"));
        }

        [Fact]
        public void CreateAndRetrieveEntity_ShouldReturnSameData()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var entityMetadata = new EntityMetadata
            {
                LogicalName = "new_retrievable",
                DisplayName = new Label("Retrievable Entity", 1033),
                Description = new Label("This entity can be retrieved", 1033),
                IsCustomizable = new BooleanManagedProperty(true)
            };

            var createRequest = new CreateEntityRequest
            {
                Entity = entityMetadata,
                PrimaryAttribute = new StringAttributeMetadata
                {
                    LogicalName = "new_name",
                    DisplayName = new Label("Name", 1033)
                }
            };
            service.Execute(createRequest);

            // Act
            var retrieveRequest = new RetrieveEntityRequest
            {
                EntityFilters = EntityFilters.Entity,
                LogicalName = "new_retrievable"
            };
            var retrieveResponse = (RetrieveEntityResponse)service.Execute(retrieveRequest);

            // Assert
            var retrievedEntityMetadata = retrieveResponse.EntityMetadata;
            Assert.Equal("new_retrievable", retrievedEntityMetadata.LogicalName);
            Assert.Equal("Retrievable Entity", retrievedEntityMetadata.DisplayName.LocalizedLabels[0].Label);
            Assert.Equal("This entity can be retrieved", retrievedEntityMetadata.Description.LocalizedLabels[0].Label);
            Assert.True(retrievedEntityMetadata.IsCustomizable.Value);
        }

        [Fact]
        public void CreateEntityThenCreateRecords_ShouldSucceed()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            // Create entity metadata first
            var createEntityRequest = new CreateEntityRequest
            {
                Entity = new EntityMetadata
                {
                    LogicalName = "new_recordstest",
                    DisplayName = new Label("Records Test Entity", 1033)
                },
                PrimaryAttribute = new StringAttributeMetadata
                {
                    LogicalName = "new_name",
                    DisplayName = new Label("Name", 1033)
                }
            };
            service.Execute(createEntityRequest);

            // Act - Create some records
            var record1 = new Entity("new_recordstest")
            {
                ["new_name"] = "Record 1"
            };
            var record2 = new Entity("new_recordstest")
            {
                ["new_name"] = "Record 2"
            };

            var id1 = service.Create(record1);
            var id2 = service.Create(record2);

            // Assert
            Assert.NotEqual(Guid.Empty, id1);
            Assert.NotEqual(Guid.Empty, id2);

            var retrievedRecord1 = service.Retrieve("new_recordstest", id1, new ColumnSet(true));
            var retrievedRecord2 = service.Retrieve("new_recordstest", id2, new ColumnSet(true));

            Assert.Equal("Record 1", retrievedRecord1["new_name"]);
            Assert.Equal("Record 2", retrievedRecord2["new_name"]);
        }

        #endregion
    }
}
