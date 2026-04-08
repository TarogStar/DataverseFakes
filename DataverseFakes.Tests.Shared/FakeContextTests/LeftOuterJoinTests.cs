using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace FakeXrmEasy.Tests
{
    /// <summary>
    /// Tests for left outer join improvements (v1.0.3)
    /// Resolves upstream issue #503
    /// </summary>
    public class LeftOuterJoinTests
    {
        [Fact]
        public void When_Left_Outer_Join_With_No_Match_Should_Return_Parent_Record()
        {
            // Arrange
            var context = new XrmFakedContext();

            var accountId = Guid.NewGuid();
            var contactId = Guid.NewGuid();

            var entities = new List<Entity>
            {
                new Entity("account")
                {
                    Id = accountId,
                    ["name"] = "Account Without Contact"
                },
                new Entity("account")
                {
                    Id = Guid.NewGuid(),
                    ["name"] = "Account With Contact",
                    ["primarycontactid"] = new EntityReference("contact", contactId)
                },
                new Entity("contact")
                {
                    Id = contactId,
                    ["fullname"] = "John Doe"
                }
            };

            context.Initialize(entities);
            var service = context.GetOrganizationService();

      // Act - Left outer join to contact
      var query = new QueryExpression("account")
      {
        ColumnSet = new ColumnSet("name")
      };
      var linkEntity = query.AddLink("contact", "primarycontactid", "contactid", JoinOperator.LeftOuter);
            linkEntity.Columns = new ColumnSet("fullname");
            linkEntity.EntityAlias = "contact";

            var results = service.RetrieveMultiple(query);

            // Assert - Should return both accounts
            Assert.Equal(2, results.Entities.Count);

            // Account without contact should be in results
            Assert.Contains(results.Entities, e => e.GetAttributeValue<string>("name") == "Account Without Contact");
        }

        [Fact]
        public void When_Nested_Left_Outer_Join_Should_Include_All_Levels()
        {
            // Arrange
            var context = new XrmFakedContext();

            var accountId = Guid.NewGuid();
            var contactId = Guid.NewGuid();
            var addressId = Guid.NewGuid();

            var entities = new List<Entity>
            {
                new Entity("account")
                {
                    Id = accountId,
                    ["name"] = "Test Account",
                    ["primarycontactid"] = new EntityReference("contact", contactId)
                },
                new Entity("contact")
                {
                    Id = contactId,
                    ["fullname"] = "John Doe"
                    // No address reference
                },
                new Entity("customeraddress")
                {
                    Id = addressId,
                    ["city"] = "Seattle",
                    ["parentid"] = new EntityReference("contact", Guid.NewGuid()) // Different contact
                }
            };

            context.Initialize(entities);
            var service = context.GetOrganizationService();

      // Act - Account -> Contact (left outer) -> Address (left outer)
      var query = new QueryExpression("account")
      {
        ColumnSet = new ColumnSet("name")
      };

      var contactLink = query.AddLink("contact", "primarycontactid", "contactid", JoinOperator.LeftOuter);
            contactLink.Columns = new ColumnSet("fullname");
            contactLink.EntityAlias = "contact";

            var addressLink = contactLink.AddLink("customeraddress", "contactid", "parentid", JoinOperator.LeftOuter);
            addressLink.Columns = new ColumnSet("city");
            addressLink.EntityAlias = "address";

            var results = service.RetrieveMultiple(query);

            // Assert - Should return the account even though contact has no address
            Assert.Single(results.Entities);
            var account = results.Entities[0];

            Assert.Equal("Test Account", account.GetAttributeValue<string>("name"));
            Assert.True(account.Contains("contact.fullname"));
            Assert.Equal("John Doe", ((AliasedValue)account["contact.fullname"]).Value);
        }

        [Fact]
        public void When_FetchXml_Left_Outer_Join_With_No_Match_Should_Return_Parent()
        {
            // Arrange
            var context = new XrmFakedContext();

            var accountId = Guid.NewGuid();

            var entities = new List<Entity>
            {
                new Entity("account")
                {
                    Id = accountId,
                    ["name"] = "Account Without Contact"
                }
            };

            context.Initialize(entities);
            var service = context.GetOrganizationService();

            // Act - Left outer join via FetchXML
            var fetchXml = @"
                <fetch>
                    <entity name='account'>
                        <attribute name='name' />
                        <link-entity name='contact' from='contactid' to='primarycontactid' link-type='outer' alias='contact'>
                            <attribute name='fullname' />
                        </link-entity>
                    </entity>
                </fetch>";

            var results = service.RetrieveMultiple(new FetchExpression(fetchXml));

            // Assert - Should return the account
            var account = Assert.Single(results.Entities);
            Assert.Equal("Account Without Contact", account.GetAttributeValue<string>("name"));
        }

        [Fact]
        public void When_FetchXml_Nested_Left_Outer_Join_Should_Include_Parent()
        {
            // Arrange
            var context = new XrmFakedContext();

            var accountId = Guid.NewGuid();
            var contactId = Guid.NewGuid();

            var entities = new List<Entity>
            {
                new Entity("account")
                {
                    Id = accountId,
                    ["name"] = "Test Account",
                    ["primarycontactid"] = new EntityReference("contact", contactId)
                },
                new Entity("contact")
                {
                    Id = contactId,
                    ["fullname"] = "John Doe"
                    // No address
                }
            };

            context.Initialize(entities);
            var service = context.GetOrganizationService();

            // Act - Nested left outer join via FetchXML
            var fetchXml = @"
                <fetch>
                    <entity name='account'>
                        <attribute name='name' />
                        <link-entity name='contact' from='contactid' to='primarycontactid' link-type='outer' alias='contact'>
                            <attribute name='fullname' />
                            <link-entity name='customeraddress' from='parentid' to='contactid' link-type='outer' alias='address'>
                                <attribute name='city' />
                            </link-entity>
                        </link-entity>
                    </entity>
                </fetch>";

            var results = service.RetrieveMultiple(new FetchExpression(fetchXml));

            // Assert - Should return the account with contact data
            Assert.Single(results.Entities);
            var account = results.Entities[0];

            Assert.Equal("Test Account", account.GetAttributeValue<string>("name"));
            Assert.True(account.Contains("contact.fullname"));
            Assert.Equal("John Doe", ((AliasedValue)account["contact.fullname"]).Value);
        }

        [Fact]
        public void When_Left_Outer_Join_With_Filter_On_Joined_Entity_Should_Apply_Filter()
        {
            // Arrange
            var context = new XrmFakedContext();

            var accountId1 = Guid.NewGuid();
            var accountId2 = Guid.NewGuid();
            var contactId1 = Guid.NewGuid();
            var contactId2 = Guid.NewGuid();

            var entities = new List<Entity>
            {
                new Entity("account")
                {
                    Id = accountId1,
                    ["name"] = "Account 1",
                    ["primarycontactid"] = new EntityReference("contact", contactId1)
                },
                new Entity("account")
                {
                    Id = accountId2,
                    ["name"] = "Account 2",
                    ["primarycontactid"] = new EntityReference("contact", contactId2)
                },
                new Entity("contact")
                {
                    Id = contactId1,
                    ["fullname"] = "Active Contact",
                    ["statecode"] = new OptionSetValue(0)
                },
                new Entity("contact")
                {
                    Id = contactId2,
                    ["fullname"] = "Inactive Contact",
                    ["statecode"] = new OptionSetValue(1)
                }
            };

            context.Initialize(entities);
            var service = context.GetOrganizationService();

      // Act - Left outer join with filter
      var query = new QueryExpression("account")
      {
        ColumnSet = new ColumnSet("name")
      };

      var linkEntity = query.AddLink("contact", "primarycontactid", "contactid", JoinOperator.LeftOuter);
            linkEntity.Columns = new ColumnSet("fullname");
            linkEntity.EntityAlias = "contact";
            linkEntity.LinkCriteria.AddCondition("statecode", ConditionOperator.Equal, 0);

            var results = service.RetrieveMultiple(query);

            // Assert - Left outer join should return BOTH accounts
            // Account 1 should have contact data (active contact matches filter)
            // Account 2 should have no contact data (inactive contact filtered out, but account still included)
            Assert.Equal(2, results.Entities.Count);

            var account1 = results.Entities.FirstOrDefault(e => e.GetAttributeValue<string>("name") == "Account 1");
            var account2 = results.Entities.FirstOrDefault(e => e.GetAttributeValue<string>("name") == "Account 2");

            Assert.NotNull(account1);
            Assert.NotNull(account2);

            // Account 1 should have contact data because contact matched the filter
            Assert.True(account1.Contains("contact.fullname"));
            Assert.Equal("Active Contact", ((AliasedValue)account1["contact.fullname"]).Value);

            // Account 2 should NOT have contact data because contact didn't match filter
            Assert.False(account2.Contains("contact.fullname"));
        }

        [Fact]
        public void When_Multiple_Left_Outer_Joins_At_Same_Level_Should_Work()
        {
            // Arrange
            var context = new XrmFakedContext();

            var accountId = Guid.NewGuid();
            var contactId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var entities = new List<Entity>
            {
                new Entity("account")
                {
                    Id = accountId,
                    ["name"] = "Test Account",
                    ["primarycontactid"] = new EntityReference("contact", contactId),
                    ["ownerid"] = new EntityReference("systemuser", userId)
                },
                new Entity("contact")
                {
                    Id = contactId,
                    ["fullname"] = "John Doe"
                },
                new Entity("systemuser")
                {
                    Id = userId,
                    ["fullname"] = "Admin User"
                }
            };

            context.Initialize(entities);
            var service = context.GetOrganizationService();

      // Act - Two left outer joins at the same level
      var query = new QueryExpression("account")
      {
        ColumnSet = new ColumnSet("name")
      };

      var contactLink = query.AddLink("contact", "primarycontactid", "contactid", JoinOperator.LeftOuter);
            contactLink.Columns = new ColumnSet("fullname");
            contactLink.EntityAlias = "contact";

            var userLink = query.AddLink("systemuser", "ownerid", "systemuserid", JoinOperator.LeftOuter);
            userLink.Columns = new ColumnSet("fullname");
            userLink.EntityAlias = "owner";

            var results = service.RetrieveMultiple(query);

            // Assert - Should have data from both joins
            Assert.Single(results.Entities);
            var account = results.Entities[0];

            Assert.Equal("Test Account", account.GetAttributeValue<string>("name"));
            Assert.True(account.Contains("contact.fullname"));
            Assert.True(account.Contains("owner.fullname"));
            Assert.Equal("John Doe", ((AliasedValue)account["contact.fullname"]).Value);
            Assert.Equal("Admin User", ((AliasedValue)account["owner.fullname"]).Value);
        }

        [Fact]
        public void When_Mixed_Inner_And_Left_Outer_Joins_Should_Work()
        {
            // Arrange
            var context = new XrmFakedContext();

            var accountId = Guid.NewGuid();
            var contactId = Guid.NewGuid();

            var entities = new List<Entity>
            {
                new Entity("account")
                {
                    Id = accountId,
                    ["name"] = "Test Account",
                    ["primarycontactid"] = new EntityReference("contact", contactId)
                },
                new Entity("contact")
                {
                    Id = contactId,
                    ["fullname"] = "John Doe"
                    // No parent account
                }
            };

            context.Initialize(entities);
            var service = context.GetOrganizationService();

      // Act - Inner join to contact, then left outer to parent account
      var query = new QueryExpression("account")
      {
        ColumnSet = new ColumnSet("name")
      };

      var contactLink = query.AddLink("contact", "primarycontactid", "contactid", JoinOperator.Inner);
            contactLink.Columns = new ColumnSet("fullname");
            contactLink.EntityAlias = "contact";

            var parentLink = contactLink.AddLink("account", "parentcustomerid", "accountid", JoinOperator.LeftOuter);
            parentLink.Columns = new ColumnSet("name");
            parentLink.EntityAlias = "parentaccount";

            var results = service.RetrieveMultiple(query);

            // Assert - Should return the account (inner join matched) without parent account data
            Assert.Single(results.Entities);
            var account = results.Entities[0];

            Assert.Equal("Test Account", account.GetAttributeValue<string>("name"));
            Assert.True(account.Contains("contact.fullname"));
        }

        [Fact]
        public void When_Deep_Nested_Left_Outer_Joins_Should_Work()
        {
            // Arrange
            var context = new XrmFakedContext();

            var level1Id = Guid.NewGuid();
            var level2Id = Guid.NewGuid();
            var level3Id = Guid.NewGuid();

            var entities = new List<Entity>
            {
                new Entity("level1")
                {
                    Id = level1Id,
                    ["name"] = "Level 1",
                    ["level2id"] = new EntityReference("level2", level2Id)
                },
                new Entity("level2")
                {
                    Id = level2Id,
                    ["name"] = "Level 2",
                    ["level3id"] = new EntityReference("level3", level3Id)
                },
                new Entity("level3")
                {
                    Id = level3Id,
                    ["name"] = "Level 3"
                    // No level4
                }
            };

            context.Initialize(entities);
            var service = context.GetOrganizationService();

      // Act - Three levels of left outer joins
      var query = new QueryExpression("level1")
      {
        ColumnSet = new ColumnSet("name")
      };

      var link2 = query.AddLink("level2", "level2id", "level2id", JoinOperator.LeftOuter);
            link2.Columns = new ColumnSet("name");
            link2.EntityAlias = "level2";

            var link3 = link2.AddLink("level3", "level3id", "level3id", JoinOperator.LeftOuter);
            link3.Columns = new ColumnSet("name");
            link3.EntityAlias = "level3";

            var link4 = link3.AddLink("level4", "level4id", "level4id", JoinOperator.LeftOuter);
            link4.Columns = new ColumnSet("name");
            link4.EntityAlias = "level4";

            var results = service.RetrieveMultiple(query);

            // Assert - Should return all three levels even though level4 doesn't exist
            Assert.Single(results.Entities);
            var result = results.Entities[0];

            Assert.Equal("Level 1", result.GetAttributeValue<string>("name"));
            Assert.True(result.Contains("level2.name"));
            Assert.True(result.Contains("level3.name"));
            Assert.Equal("Level 2", ((AliasedValue)result["level2.name"]).Value);
            Assert.Equal("Level 3", ((AliasedValue)result["level3.name"]).Value);
        }
    }
}
