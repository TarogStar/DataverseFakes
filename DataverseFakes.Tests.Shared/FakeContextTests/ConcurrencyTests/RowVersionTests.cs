using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Xunit;

namespace FakeXrmEasy.Tests.FakeContextTests.ConcurrencyTests
{
    /// <summary>
    /// Tests for RowVersion / Optimistic Concurrency feature.
    /// Issue #553: https://github.com/DynamicsValue/fake-xrm-easy/issues/553
    /// </summary>
    public class RowVersionTests
    {
        #region Create Tests

        [Fact]
        public void When_entity_is_created_it_should_have_versionnumber()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();
            var entity = new Entity("account") { Id = Guid.NewGuid() };

            // Act
            context.Initialize(new List<Entity> { entity });

            // Assert
            var storedEntity = context.Data["account"][entity.Id];
            Assert.True(storedEntity.Contains("versionnumber"));
            Assert.True(storedEntity.GetAttributeValue<long>("versionnumber") > 0);
        }

        [Fact]
        public void When_entity_is_created_via_service_it_should_have_versionnumber()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            // Act
            var entityId = service.Create(new Entity("account") { ["name"] = "Test Account" });

            // Assert
            var storedEntity = context.Data["account"][entityId];
            Assert.True(storedEntity.Contains("versionnumber"));
            Assert.True(storedEntity.GetAttributeValue<long>("versionnumber") > 0);
        }

        [Fact]
        public void When_multiple_entities_created_each_should_have_unique_versionnumber()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            // Act
            var entity1Id = service.Create(new Entity("account") { ["name"] = "Account 1" });
            var entity2Id = service.Create(new Entity("account") { ["name"] = "Account 2" });
            var entity3Id = service.Create(new Entity("contact") { ["firstname"] = "John" });

            // Assert
            var entity1Version = context.Data["account"][entity1Id].GetAttributeValue<long>("versionnumber");
            var entity2Version = context.Data["account"][entity2Id].GetAttributeValue<long>("versionnumber");
            var entity3Version = context.Data["contact"][entity3Id].GetAttributeValue<long>("versionnumber");

            Assert.True(entity1Version > 0);
            Assert.True(entity2Version > entity1Version);
            Assert.True(entity3Version > entity2Version);

            // All should be unique
            var versions = new[] { entity1Version, entity2Version, entity3Version };
            Assert.Equal(3, versions.Distinct().Count());
        }

        #endregion

        #region Update Tests

        [Fact]
        public void When_entity_is_updated_versionnumber_should_increment()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();
            var entityId = service.Create(new Entity("account") { ["name"] = "Test Account" });

            var initialVersion = context.Data["account"][entityId].GetAttributeValue<long>("versionnumber");

            // Act
            var updateEntity = new Entity("account", entityId) { ["name"] = "Updated Account" };
            service.Update(updateEntity);

            // Assert
            var updatedVersion = context.Data["account"][entityId].GetAttributeValue<long>("versionnumber");
            Assert.True(updatedVersion > initialVersion);
        }

        [Fact]
        public void When_entity_is_updated_multiple_times_versionnumber_should_increment_each_time()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();
            var entityId = service.Create(new Entity("account") { ["name"] = "Test Account" });

            var version1 = context.Data["account"][entityId].GetAttributeValue<long>("versionnumber");

            // Act - First update
            service.Update(new Entity("account", entityId) { ["name"] = "Update 1" });
            var version2 = context.Data["account"][entityId].GetAttributeValue<long>("versionnumber");

            // Act - Second update
            service.Update(new Entity("account", entityId) { ["name"] = "Update 2" });
            var version3 = context.Data["account"][entityId].GetAttributeValue<long>("versionnumber");

            // Assert
            Assert.True(version2 > version1);
            Assert.True(version3 > version2);
        }

        #endregion

        #region ConcurrencyBehavior Tests

        [Fact]
        public void When_update_with_matching_rowversion_it_should_succeed()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();
            var entityId = service.Create(new Entity("account") { ["name"] = "Test Account" });

            var currentVersion = context.Data["account"][entityId].GetAttributeValue<long>("versionnumber");

            // Act
            var updateEntity = new Entity("account", entityId)
            {
                ["name"] = "Updated Account",
                RowVersion = currentVersion.ToString()
            };

            var request = new UpdateRequest
            {
                Target = updateEntity,
                ConcurrencyBehavior = ConcurrencyBehavior.IfRowVersionMatches
            };

            // Act & Assert - should not throw
            var exception = Record.Exception(() => service.Execute(request));
            Assert.Null(exception);

            // Verify update was applied
            Assert.Equal("Updated Account", context.Data["account"][entityId].GetAttributeValue<string>("name"));
        }

        [Fact]
        public void When_update_with_mismatched_rowversion_it_should_throw()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();
            var entityId = service.Create(new Entity("account") { ["name"] = "Test Account" });

            var currentVersion = context.Data["account"][entityId].GetAttributeValue<long>("versionnumber");
            var wrongVersion = currentVersion + 1000; // Wrong version

            var updateEntity = new Entity("account", entityId)
            {
                ["name"] = "Updated Account",
                RowVersion = wrongVersion.ToString()
            };

            var request = new UpdateRequest
            {
                Target = updateEntity,
                ConcurrencyBehavior = ConcurrencyBehavior.IfRowVersionMatches
            };

            // Act & Assert
            var exception = Assert.Throws<FaultException<OrganizationServiceFault>>(() => service.Execute(request));
            Assert.Contains("doesn't match", exception.Message);
        }

        [Fact]
        public void When_update_with_missing_rowversion_and_IfRowVersionMatches_it_should_throw()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();
            var entityId = service.Create(new Entity("account") { ["name"] = "Test Account" });

            var updateEntity = new Entity("account", entityId)
            {
                ["name"] = "Updated Account"
                // Note: No RowVersion set
            };

            var request = new UpdateRequest
            {
                Target = updateEntity,
                ConcurrencyBehavior = ConcurrencyBehavior.IfRowVersionMatches
            };

            // Act & Assert
            var exception = Assert.Throws<FaultException<OrganizationServiceFault>>(() => service.Execute(request));
            // Error message now mentions both RowVersion and versionnumber as valid options
            Assert.Contains("RowVersion", exception.Message);
            Assert.Contains("versionnumber", exception.Message);
        }

        [Fact]
        public void When_update_with_AlwaysOverwrite_it_should_ignore_version_mismatch()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();
            var entityId = service.Create(new Entity("account") { ["name"] = "Test Account" });

            var currentVersion = context.Data["account"][entityId].GetAttributeValue<long>("versionnumber");
            var wrongVersion = currentVersion + 1000; // Wrong version - but should be ignored

            var updateEntity = new Entity("account", entityId)
            {
                ["name"] = "Updated Account",
                RowVersion = wrongVersion.ToString()
            };

            var request = new UpdateRequest
            {
                Target = updateEntity,
                ConcurrencyBehavior = ConcurrencyBehavior.AlwaysOverwrite
            };

            // Act & Assert - should not throw
            var exception = Record.Exception(() => service.Execute(request));
            Assert.Null(exception);

            // Verify update was applied
            Assert.Equal("Updated Account", context.Data["account"][entityId].GetAttributeValue<string>("name"));
        }

        [Fact]
        public void When_update_with_default_concurrency_behavior_it_should_succeed()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();
            var entityId = service.Create(new Entity("account") { ["name"] = "Test Account" });

            var updateEntity = new Entity("account", entityId)
            {
                ["name"] = "Updated Account"
            };

            var request = new UpdateRequest
            {
                Target = updateEntity
                // Default ConcurrencyBehavior (not set)
            };

            // Act & Assert - should not throw
            var exception = Record.Exception(() => service.Execute(request));
            Assert.Null(exception);

            // Verify update was applied
            Assert.Equal("Updated Account", context.Data["account"][entityId].GetAttributeValue<string>("name"));
        }

        [Fact]
        public void When_update_with_versionnumber_attribute_instead_of_RowVersion_it_should_work()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();
            var entityId = service.Create(new Entity("account") { ["name"] = "Test Account" });

            var currentVersion = context.Data["account"][entityId].GetAttributeValue<long>("versionnumber");

            var updateEntity = new Entity("account", entityId)
            {
                ["name"] = "Updated Account",
                ["versionnumber"] = currentVersion  // Using attribute instead of RowVersion property
            };

            var request = new UpdateRequest
            {
                Target = updateEntity,
                ConcurrencyBehavior = ConcurrencyBehavior.IfRowVersionMatches
            };

            // Act & Assert - should not throw
            var exception = Record.Exception(() => service.Execute(request));
            Assert.Null(exception);
        }

        #endregion

        #region Retrieve Tests

        [Fact]
        public void When_retrieving_entity_versionnumber_should_be_available()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();
            var entityId = service.Create(new Entity("account") { ["name"] = "Test Account" });

            // Act
            var retrieved = service.Retrieve("account", entityId, new Microsoft.Xrm.Sdk.Query.ColumnSet(true));

            // Assert
            Assert.True(retrieved.Contains("versionnumber"));
            Assert.True(retrieved.GetAttributeValue<long>("versionnumber") > 0);
        }

        #endregion

        #region Thread Safety Tests

        [Fact]
        public void When_entities_created_in_parallel_versionnumbers_should_be_unique()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();
            var ids = new List<Guid>();

            // Act - Create entities in a loop to simulate parallel creation
            for (int i = 0; i < 100; i++)
            {
                var id = service.Create(new Entity("account") { ["name"] = $"Account {i}" });
                ids.Add(id);
            }

            // Assert - All version numbers should be unique
            var versions = ids.Select(id => context.Data["account"][id].GetAttributeValue<long>("versionnumber")).ToList();
            Assert.Equal(100, versions.Distinct().Count());
        }

        [Fact]
        public void When_update_with_invalid_non_numeric_rowversion_it_should_throw()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();
            var entityId = service.Create(new Entity("account") { ["name"] = "Test Account" });

            var updateEntity = new Entity("account", entityId)
            {
                ["name"] = "Updated Account",
                RowVersion = "invalid"  // Non-numeric RowVersion string
            };

            var request = new UpdateRequest
            {
                Target = updateEntity,
                ConcurrencyBehavior = ConcurrencyBehavior.IfRowVersionMatches
            };

            // Act & Assert
            var exception = Assert.Throws<FaultException<OrganizationServiceFault>>(() => service.Execute(request));
            Assert.Contains("RowVersion", exception.Message);
        }

        [Fact]
        public void When_update_with_alphanumeric_rowversion_it_should_throw()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();
            var entityId = service.Create(new Entity("account") { ["name"] = "Test Account" });

            var updateEntity = new Entity("account", entityId)
            {
                ["name"] = "Updated Account",
                RowVersion = "abc123"  // Alphanumeric RowVersion string
            };

            var request = new UpdateRequest
            {
                Target = updateEntity,
                ConcurrencyBehavior = ConcurrencyBehavior.IfRowVersionMatches
            };

            // Act & Assert
            var exception = Assert.Throws<FaultException<OrganizationServiceFault>>(() => service.Execute(request));
            Assert.Contains("RowVersion", exception.Message);
        }

        #endregion

        #region Concurrency Simulation Tests

        [Fact]
        public void When_simulating_concurrent_updates_stale_version_should_fail()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();
            var entityId = service.Create(new Entity("account") { ["name"] = "Test Account" });

            // User A reads the entity
            var userAVersion = context.Data["account"][entityId].GetAttributeValue<long>("versionnumber");

            // User B updates the entity first
            var userBUpdate = new Entity("account", entityId) { ["name"] = "User B Update" };
            service.Update(userBUpdate);

            // Now User A tries to update with stale version
            var userAUpdate = new Entity("account", entityId)
            {
                ["name"] = "User A Update",
                RowVersion = userAVersion.ToString()  // Stale version
            };

            var request = new UpdateRequest
            {
                Target = userAUpdate,
                ConcurrencyBehavior = ConcurrencyBehavior.IfRowVersionMatches
            };

            // Act & Assert - User A's update should fail
            var exception = Assert.Throws<FaultException<OrganizationServiceFault>>(() => service.Execute(request));
            Assert.Contains("doesn't match", exception.Message);

            // Verify User B's update is still there
            Assert.Equal("User B Update", context.Data["account"][entityId].GetAttributeValue<string>("name"));
        }

        #endregion
    }
}
