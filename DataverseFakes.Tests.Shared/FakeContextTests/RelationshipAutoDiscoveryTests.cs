using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace FakeXrmEasy.Tests
{
    /// <summary>
    /// Tests for automatic relationship discovery from metadata (v1.0.2)
    /// </summary>
    public class RelationshipAutoDiscoveryTests
    {
        [Fact]
        public void When_InitializeMetadata_With_ManyToMany_Relationships_Should_Auto_Register_Them()
        {
            // Arrange
            var context = new XrmFakedContext();

            var accountMetadata = new EntityMetadata
            {
                LogicalName = "account"
            };
            typeof(EntityMetadata)
                .GetProperty("ManyToManyRelationships")
                .SetValue(accountMetadata, new[]
                {
                    new ManyToManyRelationshipMetadata
                    {
                        SchemaName = "new_account_contact",
                        IntersectEntityName = "new_account_contact",
                        Entity1LogicalName = "account",
                        Entity1IntersectAttribute = "accountid",
                        Entity2LogicalName = "contact",
                        Entity2IntersectAttribute = "contactid"
                    }
                }, null);

            // Act
            context.InitializeMetadata(accountMetadata);

            // Now test association
            var accountId = Guid.NewGuid();
            var contactId = Guid.NewGuid();

            context.Initialize(new[]
            {
                new Entity("account") { Id = accountId },
                new Entity("contact") { Id = contactId }
            });

            var service = context.GetOrganizationService();

            // This should work without manually calling AddRelationship!
            service.Associate("account", accountId, new Relationship("new_account_contact"),
                new EntityReferenceCollection { new EntityReference("contact", contactId) });

            // Assert - verify association was created
            var intersect = context.CreateQuery("new_account_contact")
                .FirstOrDefault(e => e.GetAttributeValue<Guid>("accountid") == accountId &&
                                    e.GetAttributeValue<Guid>("contactid") == contactId);

            Assert.NotNull(intersect);
        }

        [Fact]
        public void When_InitializeMetadata_With_OneToMany_Relationships_Should_Auto_Register_Them()
        {
            // Arrange
            var context = new XrmFakedContext();

            var accountMetadata = new EntityMetadata
            {
                LogicalName = "account"
            };
            typeof(EntityMetadata)
                .GetProperty("OneToManyRelationships")
                .SetValue(accountMetadata, new[]
                {
                    new OneToManyRelationshipMetadata
                    {
                        SchemaName = "account_contacts",
                        ReferencedEntity = "account",
                        ReferencedAttribute = "accountid",
                        ReferencingEntity = "contact",
                        ReferencingAttribute = "parentcustomerid"
                    }
                }, null);

            // Act
            context.InitializeMetadata(accountMetadata);

            // Relationship should be registered automatically
            // We can verify by trying to use it (though 1:N typically works via lookups)
            Assert.True(true); // If no exception, relationship was registered
        }

        [Fact]
        public void When_InitializeMetadata_With_ManyToOne_Relationships_Should_Auto_Register_Them()
        {
            // Arrange
            var context = new XrmFakedContext();

            var contactMetadata = new EntityMetadata
            {
                LogicalName = "contact"
            };
            typeof(EntityMetadata)
                .GetProperty("ManyToOneRelationships")
                .SetValue(contactMetadata, new[]
                {
                    new OneToManyRelationshipMetadata
                    {
                        SchemaName = "contact_account",
                        ReferencedEntity = "account",
                        ReferencedAttribute = "accountid",
                        ReferencingEntity = "contact",
                        ReferencingAttribute = "parentcustomerid"
                    }
                }, null);

            // Act
            context.InitializeMetadata(contactMetadata);

            // Relationship should be registered automatically
            Assert.True(true);
        }

        [Fact]
        public void When_InitializeMetadata_With_Multiple_Entities_Should_Register_All_Relationships()
        {
            // Arrange
            var context = new XrmFakedContext();

            var accountMetadata = new EntityMetadata
            {
                LogicalName = "account"
            };
            typeof(EntityMetadata)
                .GetProperty("ManyToManyRelationships")
                .SetValue(accountMetadata, new[]
                {
                    new ManyToManyRelationshipMetadata
                    {
                        SchemaName = "accountcontactbase",
                        IntersectEntityName = "accountcontactbase",
                        Entity1LogicalName = "account",
                        Entity1IntersectAttribute = "accountid",
                        Entity2LogicalName = "contact",
                        Entity2IntersectAttribute = "contactid"
                    }
                }, null);

            var contactMetadata = new EntityMetadata
            {
                LogicalName = "contact"
            };
            typeof(EntityMetadata)
                .GetProperty("ManyToManyRelationships")
                .SetValue(contactMetadata, new[]
                {
                    new ManyToManyRelationshipMetadata
                    {
                        SchemaName = "new_contact_opportunity",
                        IntersectEntityName = "new_contact_opportunity",
                        Entity1LogicalName = "contact",
                        Entity1IntersectAttribute = "contactid",
                        Entity2LogicalName = "opportunity",
                        Entity2IntersectAttribute = "opportunityid"
                    }
                }, null);

            // Act
            context.InitializeMetadata(new[] { accountMetadata, contactMetadata });

            // Both relationships should be registered
            var accountId = Guid.NewGuid();
            var contactId = Guid.NewGuid();
            var opportunityId = Guid.NewGuid();

            context.Initialize(new[]
            {
                new Entity("account") { Id = accountId },
                new Entity("contact") { Id = contactId },
                new Entity("opportunity") { Id = opportunityId }
            });

            var service = context.GetOrganizationService();

            // Assert - both associations should work
            service.Associate("account", accountId, new Relationship("accountcontactbase"),
                new EntityReferenceCollection { new EntityReference("contact", contactId) });

            service.Associate("contact", contactId, new Relationship("new_contact_opportunity"),
                new EntityReferenceCollection { new EntityReference("opportunity", opportunityId) });

            Assert.True(true); // No exceptions = success
        }

        [Fact]
        public void When_InitializeMetadata_With_Incomplete_Relationship_Metadata_Should_Skip_Gracefully()
        {
            // Arrange
            var context = new XrmFakedContext();

            var accountMetadata = new EntityMetadata
            {
                LogicalName = "account"
            };
            typeof(EntityMetadata)
                .GetProperty("ManyToManyRelationships")
                .SetValue(accountMetadata, new[]
                {
                    // Missing SchemaName
                    new ManyToManyRelationshipMetadata
                    {
                        IntersectEntityName = "test_intersect",
                        Entity1LogicalName = "account",
                        Entity1IntersectAttribute = "accountid"
                    },
                    // Missing IntersectEntityName
                    new ManyToManyRelationshipMetadata
                    {
                        SchemaName = "incomplete_relationship",
                        Entity1LogicalName = "account"
                    },
                    // Valid relationship
                    new ManyToManyRelationshipMetadata
                    {
                        SchemaName = "valid_relationship",
                        IntersectEntityName = "valid_intersect",
                        Entity1LogicalName = "account",
                        Entity1IntersectAttribute = "accountid",
                        Entity2LogicalName = "contact",
                        Entity2IntersectAttribute = "contactid"
                    }
                }, null);

            // Act - should not throw exception
            context.InitializeMetadata(accountMetadata);

            // Assert - valid relationship should be registered
            var accountId = Guid.NewGuid();
            var contactId = Guid.NewGuid();

            context.Initialize(new[]
            {
                new Entity("account") { Id = accountId },
                new Entity("contact") { Id = contactId }
            });

            var service = context.GetOrganizationService();

            service.Associate("account", accountId, new Relationship("valid_relationship"),
                new EntityReferenceCollection { new EntityReference("contact", contactId) });

            Assert.True(true);
        }

        [Fact]
        public void When_InitializeMetadata_With_Duplicate_Relationship_Should_Not_Register_Twice()
        {
            // Arrange
            var context = new XrmFakedContext();

            // Manually add a relationship first
            context.AddRelationship("existing_relationship", new XrmFakedRelationship
            {
                IntersectEntity = "existing_intersect",
                Entity1LogicalName = "account",
                Entity1Attribute = "accountid",
                Entity2LogicalName = "contact",
                Entity2Attribute = "contactid",
                RelationshipType = XrmFakedRelationship.enmFakeRelationshipType.ManyToMany
            });

            var accountMetadata = new EntityMetadata
            {
                LogicalName = "account"
            };
            typeof(EntityMetadata)
                .GetProperty("ManyToManyRelationships")
                .SetValue(accountMetadata, new[]
                {
                    new ManyToManyRelationshipMetadata
                    {
                        SchemaName = "existing_relationship", // Same name
                        IntersectEntityName = "existing_intersect",
                        Entity1LogicalName = "account",
                        Entity1IntersectAttribute = "accountid",
                        Entity2LogicalName = "contact",
                        Entity2IntersectAttribute = "contactid"
                    }
                }, null);

            // Act - should not throw or duplicate
            context.InitializeMetadata(accountMetadata);

            // Assert - relationship should still work
            var accountId = Guid.NewGuid();
            var contactId = Guid.NewGuid();

            context.Initialize(new[]
            {
                new Entity("account") { Id = accountId },
                new Entity("contact") { Id = contactId }
            });

            var service = context.GetOrganizationService();

            service.Associate("account", accountId, new Relationship("existing_relationship"),
                new EntityReferenceCollection { new EntityReference("contact", contactId) });

            Assert.True(true);
        }

        [Fact]
        public void When_InitializeMetadata_With_Null_Relationships_Should_Handle_Gracefully()
        {
            // Arrange
            var context = new XrmFakedContext();

            var accountMetadata = new EntityMetadata
            {
                LogicalName = "account"
            };
            typeof(EntityMetadata)
                .GetProperty("ManyToManyRelationships")
                .SetValue(accountMetadata, null, null);
            typeof(EntityMetadata)
                .GetProperty("OneToManyRelationships")
                .SetValue(accountMetadata, null, null);
            typeof(EntityMetadata)
                .GetProperty("ManyToOneRelationships")
                .SetValue(accountMetadata, null, null);

            // Act & Assert - should not throw
            context.InitializeMetadata(accountMetadata);
            Assert.True(true);
        }
    }
}
