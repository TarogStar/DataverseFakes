using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Linq;
using System.Collections.Generic;
using Xunit;

namespace FakeXrmEasy.Tests
{
    /// <summary>
    /// Tests for EntityReference.Name population on retrieval (v1.0.3)
    /// Resolves upstream issue #555
    /// </summary>
    public class EntityReferenceNameTests
    {
        [Fact]
        public void When_Retrieve_Single_Entity_With_Lookup_Should_Populate_EntityReference_Name()
        {
            // Arrange
            var context = new XrmFakedContext();

            // Set up metadata
            var accountMetadata = new EntityMetadata
            {
                LogicalName = "account"
            };
            typeof(EntityMetadata)
                .GetProperty("PrimaryNameAttribute")
                .SetValue(accountMetadata, "name", null);

            var contactMetadata = new EntityMetadata
            {
                LogicalName = "contact"
            };
            typeof(EntityMetadata)
                .GetProperty("PrimaryNameAttribute")
                .SetValue(contactMetadata, "fullname", null);

            context.InitializeMetadata(new[] { accountMetadata, contactMetadata });

            var contactId = Guid.NewGuid();
            var accountId = Guid.NewGuid();

            var contact = new Entity("contact")
            {
                Id = contactId,
                ["fullname"] = "John Doe"
            };

            var account = new Entity("account")
            {
                Id = accountId,
                ["name"] = "Contoso",
                ["primarycontactid"] = new EntityReference("contact", contactId)
            };

            context.Initialize(new[] { contact, account });

            var service = context.GetOrganizationService();

            // Act
            var retrieved = service.Retrieve("account", accountId, new ColumnSet(true));

            // Assert
            Assert.NotNull(retrieved);
            Assert.True(retrieved.Contains("primarycontactid"));
            var primaryContact = retrieved.GetAttributeValue<EntityReference>("primarycontactid");
            Assert.NotNull(primaryContact);
            Assert.Equal("John Doe", primaryContact.Name);
        }

        [Fact]
        public void When_RetrieveMultiple_Entities_With_Lookups_Should_Populate_All_EntityReference_Names()
        {
            // Arrange
            var context = new XrmFakedContext();

            var accountMetadata = new EntityMetadata
            {
                LogicalName = "account"
            };
            typeof(EntityMetadata)
                .GetProperty("PrimaryNameAttribute")
                .SetValue(accountMetadata, "name", null);

            var contactMetadata = new EntityMetadata
            {
                LogicalName = "contact"
            };
            typeof(EntityMetadata)
                .GetProperty("PrimaryNameAttribute")
                .SetValue(contactMetadata, "fullname", null);

            context.InitializeMetadata(new[] { accountMetadata, contactMetadata });

            var contact1Id = Guid.NewGuid();
            var contact2Id = Guid.NewGuid();
            var account1Id = Guid.NewGuid();
            var account2Id = Guid.NewGuid();

            var entities = new List<Entity>
            {
                new Entity("contact") { Id = contact1Id, ["fullname"] = "Jane Smith" },
                new Entity("contact") { Id = contact2Id, ["fullname"] = "Bob Johnson" },
                new Entity("account")
                {
                    Id = account1Id,
                    ["name"] = "Account 1",
                    ["primarycontactid"] = new EntityReference("contact", contact1Id)
                },
                new Entity("account")
                {
                    Id = account2Id,
                    ["name"] = "Account 2",
                    ["primarycontactid"] = new EntityReference("contact", contact2Id)
                }
            };

            context.Initialize(entities);
            var service = context.GetOrganizationService();

      // Act
      var query = new QueryExpression("account")
      {
        ColumnSet = new ColumnSet(true)
      };
      var results = service.RetrieveMultiple(query);

            // Assert
            Assert.Equal(2, results.Entities.Count);
            foreach (var account in results.Entities)
            {
                var primaryContact = account.GetAttributeValue<EntityReference>("primarycontactid");
                Assert.NotNull(primaryContact);
                Assert.False(string.IsNullOrEmpty(primaryContact.Name));
            }

            var account1 = results.Entities.Cast<Entity>().Single(e => e.Id == account1Id);
            var contact1 = account1.GetAttributeValue<EntityReference>("primarycontactid");
            Assert.Equal("Jane Smith", contact1.Name);
        }

        [Fact]
        public void When_Retrieve_With_Multiple_Lookup_Fields_Should_Populate_All_Names()
        {
            // Arrange
            var context = new XrmFakedContext();

            var accountMetadata = new EntityMetadata
            {
                LogicalName = "account"
            };
            typeof(EntityMetadata)
                .GetProperty("PrimaryNameAttribute")
                .SetValue(accountMetadata, "name", null);

            var contactMetadata = new EntityMetadata
            {
                LogicalName = "contact"
            };
            typeof(EntityMetadata)
                .GetProperty("PrimaryNameAttribute")
                .SetValue(contactMetadata, "fullname", null);

            var userMetadata = new EntityMetadata
            {
                LogicalName = "systemuser"
            };
            typeof(EntityMetadata)
                .GetProperty("PrimaryNameAttribute")
                .SetValue(userMetadata, "fullname", null);

            context.InitializeMetadata(new[] { accountMetadata, contactMetadata, userMetadata });

            var contactId = Guid.NewGuid();
            var ownerId = Guid.NewGuid();
            var accountId = Guid.NewGuid();

            var entities = new List<Entity>
            {
                new Entity("contact") { Id = contactId, ["fullname"] = "Contact Name" },
                new Entity("systemuser") { Id = ownerId, ["fullname"] = "Owner Name" },
                new Entity("account")
                {
                    Id = accountId,
                    ["name"] = "Test Account",
                    ["primarycontactid"] = new EntityReference("contact", contactId),
                    ["ownerid"] = new EntityReference("systemuser", ownerId)
                }
            };

            context.Initialize(entities);
            var service = context.GetOrganizationService();

            // Act
            var retrieved = service.Retrieve("account", accountId, new ColumnSet(true));

            // Assert
            var primaryContact = retrieved.GetAttributeValue<EntityReference>("primarycontactid");
            Assert.Equal("Contact Name", primaryContact.Name);

            var owner = retrieved.GetAttributeValue<EntityReference>("ownerid");
            Assert.Equal("Owner Name", owner.Name);
        }

        [Fact]
        public void When_Referenced_Entity_Does_Not_Exist_Should_Not_Populate_Name()
        {
            // Arrange
            var context = new XrmFakedContext();

            var accountMetadata = new EntityMetadata
            {
                LogicalName = "account"
            };
            typeof(EntityMetadata)
                .GetProperty("PrimaryNameAttribute")
                .SetValue(accountMetadata, "name", null);

            var contactMetadata = new EntityMetadata
            {
                LogicalName = "contact"
            };
            typeof(EntityMetadata)
                .GetProperty("PrimaryNameAttribute")
                .SetValue(contactMetadata, "fullname", null);

            context.InitializeMetadata(new[] { accountMetadata, contactMetadata });

            var nonExistentContactId = Guid.NewGuid();
            var accountId = Guid.NewGuid();

            var account = new Entity("account")
            {
                Id = accountId,
                ["name"] = "Test Account",
                ["primarycontactid"] = new EntityReference("contact", nonExistentContactId)
            };

            context.Initialize(new[] { account });
            var service = context.GetOrganizationService();

            // Act
            var retrieved = service.Retrieve("account", accountId, new ColumnSet(true));

            // Assert
            var primaryContact = retrieved.GetAttributeValue<EntityReference>("primarycontactid");
            Assert.NotNull(primaryContact);
            Assert.True(string.IsNullOrEmpty(primaryContact.Name)); // Should not be populated
        }

        [Fact]
        public void When_No_Metadata_Exists_Should_Not_Populate_Name()
        {
            // Arrange
            var context = new XrmFakedContext();
            // Intentionally NOT initializing metadata

            var contactId = Guid.NewGuid();
            var accountId = Guid.NewGuid();

            var entities = new List<Entity>
            {
                new Entity("contact") { Id = contactId, ["fullname"] = "John Doe" },
                new Entity("account")
                {
                    Id = accountId,
                    ["name"] = "Test Account",
                    ["primarycontactid"] = new EntityReference("contact", contactId)
                }
            };

            context.Initialize(entities);
            var service = context.GetOrganizationService();

            // Act
            var retrieved = service.Retrieve("account", accountId, new ColumnSet(true));

            // Assert
            var primaryContact = retrieved.GetAttributeValue<EntityReference>("primarycontactid");
            Assert.NotNull(primaryContact);
            // Name should not be populated because metadata is missing
            Assert.True(string.IsNullOrEmpty(primaryContact.Name));
        }

        [Fact]
        public void When_EntityReference_Name_Already_Set_Should_Not_Override()
        {
            // Arrange
            var context = new XrmFakedContext();

            var accountMetadata = new EntityMetadata
            {
                LogicalName = "account"
            };
            typeof(EntityMetadata)
                .GetProperty("PrimaryNameAttribute")
                .SetValue(accountMetadata, "name", null);

            var contactMetadata = new EntityMetadata
            {
                LogicalName = "contact"
            };
            typeof(EntityMetadata)
                .GetProperty("PrimaryNameAttribute")
                .SetValue(contactMetadata, "fullname", null);

            context.InitializeMetadata(new[] { accountMetadata, contactMetadata });

            var contactId = Guid.NewGuid();
            var accountId = Guid.NewGuid();

            var entities = new List<Entity>
            {
                new Entity("contact") { Id = contactId, ["fullname"] = "Actual Name" },
                new Entity("account")
                {
                    Id = accountId,
                    ["name"] = "Test Account",
                    ["primarycontactid"] = new EntityReference("contact", contactId)
                    {
                        Name = "Pre-existing Name" // Already set
                    }
                }
            };

            context.Initialize(entities);
            var service = context.GetOrganizationService();

            // Act
            var retrieved = service.Retrieve("account", accountId, new ColumnSet(true));

            // Assert
            var primaryContact = retrieved.GetAttributeValue<EntityReference>("primarycontactid");
            // Should NOT override the pre-existing name
            Assert.Equal("Pre-existing Name", primaryContact.Name);
        }

        [Fact]
        public void When_Primary_Name_Attribute_Missing_From_Referenced_Entity_Should_Not_Populate()
        {
            // Arrange
            var context = new XrmFakedContext();

            var accountMetadata = new EntityMetadata
            {
                LogicalName = "account"
            };
            typeof(EntityMetadata)
                .GetProperty("PrimaryNameAttribute")
                .SetValue(accountMetadata, "name", null);

            var contactMetadata = new EntityMetadata
            {
                LogicalName = "contact"
            };
            typeof(EntityMetadata)
                .GetProperty("PrimaryNameAttribute")
                .SetValue(contactMetadata, "fullname", null);

            context.InitializeMetadata(new[] { accountMetadata, contactMetadata });

            var contactId = Guid.NewGuid();
            var accountId = Guid.NewGuid();

            var entities = new List<Entity>
            {
                // Contact entity without fullname attribute
                new Entity("contact") { Id = contactId, ["firstname"] = "John", ["lastname"] = "Doe" },
                new Entity("account")
                {
                    Id = accountId,
                    ["name"] = "Test Account",
                    ["primarycontactid"] = new EntityReference("contact", contactId)
                }
            };

            context.Initialize(entities);
            var service = context.GetOrganizationService();

            // Act
            var retrieved = service.Retrieve("account", accountId, new ColumnSet(true));

            // Assert
            var primaryContact = retrieved.GetAttributeValue<EntityReference>("primarycontactid");
            Assert.NotNull(primaryContact);
            // Should not populate because fullname attribute is missing
            Assert.True(string.IsNullOrEmpty(primaryContact.Name));
        }

        [Fact]
        public void When_Using_FetchXml_Should_Also_Populate_EntityReference_Names()
        {
            // Arrange
            var context = new XrmFakedContext();

            var accountMetadata = new EntityMetadata
            {
                LogicalName = "account"
            };
            typeof(EntityMetadata)
                .GetProperty("PrimaryNameAttribute")
                .SetValue(accountMetadata, "name", null);

            var contactMetadata = new EntityMetadata
            {
                LogicalName = "contact"
            };
            typeof(EntityMetadata)
                .GetProperty("PrimaryNameAttribute")
                .SetValue(contactMetadata, "fullname", null);

            context.InitializeMetadata(new[] { accountMetadata, contactMetadata });

            var contactId = Guid.NewGuid();
            var accountId = Guid.NewGuid();

            var entities = new List<Entity>
            {
                new Entity("contact") { Id = contactId, ["fullname"] = "FetchXML Test Contact" },
                new Entity("account")
                {
                    Id = accountId,
                    ["name"] = "FetchXML Test Account",
                    ["primarycontactid"] = new EntityReference("contact", contactId)
                }
            };

            context.Initialize(entities);
            var service = context.GetOrganizationService();

            // Act
            var fetchXml = @"
                <fetch>
                    <entity name='account'>
                        <attribute name='name' />
                        <attribute name='primarycontactid' />
                    </entity>
                </fetch>";
            var results = service.RetrieveMultiple(new FetchExpression(fetchXml));

            // Assert
            Assert.Single(results.Entities);
            var account = Assert.Single(results.Entities);
            var primaryContact = account.GetAttributeValue<EntityReference>("primarycontactid");
            Assert.NotNull(primaryContact);
            Assert.Equal("FetchXML Test Contact", primaryContact.Name);
        }
    }
}
