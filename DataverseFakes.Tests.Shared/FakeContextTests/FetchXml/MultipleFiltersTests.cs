using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace FakeXrmEasy.Tests.FetchXml
{
    /// <summary>
    /// Tests for FetchXML multiple filter elements (v1.0.3)
    /// Resolves upstream issue #507
    /// </summary>
    public class MultipleFiltersTests
    {
        [Fact]
        public void When_FetchXml_Has_Single_Filter_Should_Work_As_Before()
        {
            // Arrange
            var context = new XrmFakedContext();

            var entities = new List<Entity>
            {
                new Entity("account") { Id = Guid.NewGuid(), ["name"] = "Account 1", ["statecode"] = new OptionSetValue(0) },
                new Entity("account") { Id = Guid.NewGuid(), ["name"] = "Account 2", ["statecode"] = new OptionSetValue(1) }
            };

            context.Initialize(entities);
            var service = context.GetOrganizationService();

            // Act - Single filter with multiple conditions
            var fetchXml = @"
                <fetch>
                    <entity name='account'>
                        <attribute name='name' />
                        <filter type='and'>
                            <condition attribute='statecode' operator='eq' value='0' />
                        </filter>
                    </entity>
                </fetch>";

            var results = service.RetrieveMultiple(new FetchExpression(fetchXml));

            // Assert
            Assert.Single(results.Entities);
            Assert.Equal("Account 1", results.Entities[0].GetAttributeValue<string>("name"));
        }

        [Fact]
        public void When_FetchXml_Has_Two_Filters_Should_Combine_With_AND()
        {
            // Arrange
            var context = new XrmFakedContext();

            var guid1 = Guid.NewGuid();
            var guid2 = Guid.NewGuid();
            var guid3 = Guid.NewGuid();

            var entities = new List<Entity>
            {
                new Entity("account") { Id = guid1, ["name"] = "Contoso", ["statecode"] = new OptionSetValue(0) },
                new Entity("account") { Id = guid2, ["name"] = "Fabrikam", ["statecode"] = new OptionSetValue(0) },
                new Entity("account") { Id = guid3, ["name"] = "Contoso Inactive", ["statecode"] = new OptionSetValue(1) }
            };

            context.Initialize(entities);
            var service = context.GetOrganizationService();

            // Act - Two separate filter elements (should be AND'ed together)
            var fetchXml = @"
                <fetch>
                    <entity name='account'>
                        <attribute name='name' />
                        <filter type='and'>
                            <condition attribute='statecode' operator='eq' value='0' />
                        </filter>
                        <filter type='and'>
                            <condition attribute='name' operator='like' value='%Contoso%' />
                        </filter>
                    </entity>
                </fetch>";

            var results = service.RetrieveMultiple(new FetchExpression(fetchXml));

            // Assert - Should only return "Contoso" with statecode=0
            Assert.Single(results.Entities);
            Assert.Equal(guid1, results.Entities[0].Id);
            Assert.Equal("Contoso", results.Entities[0].GetAttributeValue<string>("name"));
        }

        [Fact]
        public void When_FetchXml_Has_Three_Filters_Should_Combine_All_With_AND()
        {
            // Arrange
            var context = new XrmFakedContext();

            var guid1 = Guid.NewGuid();
            var guid2 = Guid.NewGuid();
            var guid3 = Guid.NewGuid();
            var guid4 = Guid.NewGuid();

            var entities = new List<Entity>
            {
                new Entity("account")
                {
                    Id = guid1,
                    ["name"] = "Contoso Corp",
                    ["statecode"] = new OptionSetValue(0),
                    ["revenue"] = new Money(100000m)
                },
                new Entity("account")
                {
                    Id = guid2,
                    ["name"] = "Contoso Ltd",
                    ["statecode"] = new OptionSetValue(0),
                    ["revenue"] = new Money(50000m)
                },
                new Entity("account")
                {
                    Id = guid3,
                    ["name"] = "Fabrikam Corp",
                    ["statecode"] = new OptionSetValue(0),
                    ["revenue"] = new Money(100000m)
                },
                new Entity("account")
                {
                    Id = guid4,
                    ["name"] = "Contoso Inactive",
                    ["statecode"] = new OptionSetValue(1),
                    ["revenue"] = new Money(100000m)
                }
            };

            context.Initialize(entities);
            var service = context.GetOrganizationService();

            // Act - Three separate filter elements
            var fetchXml = @"
                <fetch>
                    <entity name='account'>
                        <attribute name='name' />
                        <filter>
                            <condition attribute='statecode' operator='eq' value='0' />
                        </filter>
                        <filter>
                            <condition attribute='name' operator='like' value='%Contoso%' />
                        </filter>
                        <filter>
                            <condition attribute='revenue' operator='ge' value='100000' />
                        </filter>
                    </entity>
                </fetch>";

            var results = service.RetrieveMultiple(new FetchExpression(fetchXml));

            // Assert - Should only return "Contoso Corp" (all three filters match)
            Assert.Single(results.Entities);
            Assert.Equal(guid1, results.Entities[0].Id);
            Assert.Equal("Contoso Corp", results.Entities[0].GetAttributeValue<string>("name"));
        }

        [Fact]
        public void When_FetchXml_Has_Multiple_Filters_With_OR_Type_Should_Respect_Filter_Types()
        {
            // Arrange
            var context = new XrmFakedContext();

            var guid1 = Guid.NewGuid();
            var guid2 = Guid.NewGuid();
            var guid3 = Guid.NewGuid();

            var entities = new List<Entity>
            {
                new Entity("account") { Id = guid1, ["name"] = "Contoso", ["city"] = "Seattle" },
                new Entity("account") { Id = guid2, ["name"] = "Fabrikam", ["city"] = "Redmond" },
                new Entity("account") { Id = guid3, ["name"] = "AdventureWorks", ["city"] = "Bellevue" }
            };

            context.Initialize(entities);
            var service = context.GetOrganizationService();

            // Act - First filter is OR (name contains Contoso OR Fabrikam)
            //       Second filter is AND (city must be Redmond)
            var fetchXml = @"
                <fetch>
                    <entity name='account'>
                        <attribute name='name' />
                        <filter type='or'>
                            <condition attribute='name' operator='eq' value='Contoso' />
                            <condition attribute='name' operator='eq' value='Fabrikam' />
                        </filter>
                        <filter type='and'>
                            <condition attribute='city' operator='eq' value='Redmond' />
                        </filter>
                    </entity>
                </fetch>";

            var results = service.RetrieveMultiple(new FetchExpression(fetchXml));

            // Assert - Should only return Fabrikam (matches OR filter AND city filter)
            Assert.Single(results.Entities);
            Assert.Equal(guid2, results.Entities[0].Id);
            Assert.Equal("Fabrikam", results.Entities[0].GetAttributeValue<string>("name"));
        }

        [Fact]
        public void When_LinkEntity_Has_Multiple_Filters_Should_Combine_With_AND()
        {
            // Arrange
            var context = new XrmFakedContext();

            var contactId1 = Guid.NewGuid();
            var contactId2 = Guid.NewGuid();
            var accountId1 = Guid.NewGuid();
            var accountId2 = Guid.NewGuid();

            var entities = new List<Entity>
            {
                new Entity("contact")
                {
                    Id = contactId1,
                    ["fullname"] = "John Doe",
                    ["statecode"] = new OptionSetValue(0),
                    ["city"] = "Seattle"
                },
                new Entity("contact")
                {
                    Id = contactId2,
                    ["fullname"] = "Jane Smith",
                    ["statecode"] = new OptionSetValue(0),
                    ["city"] = "Redmond"
                },
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
                }
            };

            context.Initialize(entities);
            var service = context.GetOrganizationService();

            // Act - Link-entity with multiple filters
            var fetchXml = @"
                <fetch>
                    <entity name='account'>
                        <attribute name='name' />
                        <link-entity name='contact' from='contactid' to='primarycontactid'>
                            <filter>
                                <condition attribute='statecode' operator='eq' value='0' />
                            </filter>
                            <filter>
                                <condition attribute='city' operator='eq' value='Seattle' />
                            </filter>
                        </link-entity>
                    </entity>
                </fetch>";

            var results = service.RetrieveMultiple(new FetchExpression(fetchXml));

            // Assert - Should only return Account 1 (contact is active AND in Seattle)
            Assert.Single(results.Entities);
            Assert.Equal(accountId1, results.Entities[0].Id);
        }

        [Fact]
        public void When_FetchXml_Has_Empty_Filters_Should_Return_All_Records()
        {
            // Arrange
            var context = new XrmFakedContext();

            var entities = new List<Entity>
            {
                new Entity("account") { Id = Guid.NewGuid(), ["name"] = "Account 1" },
                new Entity("account") { Id = Guid.NewGuid(), ["name"] = "Account 2" }
            };

            context.Initialize(entities);
            var service = context.GetOrganizationService();

            // Act - No filter elements
            var fetchXml = @"
                <fetch>
                    <entity name='account'>
                        <attribute name='name' />
                    </entity>
                </fetch>";

            var results = service.RetrieveMultiple(new FetchExpression(fetchXml));

            // Assert - Should return all records
            Assert.Equal(2, results.Entities.Count);
        }

        [Fact]
        public void When_FetchXml_Has_Nested_Filters_Within_Multiple_Top_Filters_Should_Work()
        {
            // Arrange
            var context = new XrmFakedContext();

            var guid1 = Guid.NewGuid();
            var guid2 = Guid.NewGuid();
            var guid3 = Guid.NewGuid();

            var entities = new List<Entity>
            {
                new Entity("account")
                {
                    Id = guid1,
                    ["name"] = "Contoso",
                    ["city"] = "Seattle",
                    ["statecode"] = new OptionSetValue(0)
                },
                new Entity("account")
                {
                    Id = guid2,
                    ["name"] = "Fabrikam",
                    ["city"] = "Seattle",
                    ["statecode"] = new OptionSetValue(0)
                },
                new Entity("account")
                {
                    Id = guid3,
                    ["name"] = "Contoso",
                    ["city"] = "Redmond",
                    ["statecode"] = new OptionSetValue(0)
                }
            };

            context.Initialize(entities);
            var service = context.GetOrganizationService();

            // Act - Multiple top-level filters with nested filters
            var fetchXml = @"
                <fetch>
                    <entity name='account'>
                        <attribute name='name' />
                        <attribute name='city' />
                        <filter>
                            <filter type='or'>
                                <condition attribute='name' operator='eq' value='Contoso' />
                                <condition attribute='name' operator='eq' value='Fabrikam' />
                            </filter>
                        </filter>
                        <filter>
                            <condition attribute='city' operator='eq' value='Seattle' />
                        </filter>
                        <filter>
                            <condition attribute='statecode' operator='eq' value='0' />
                        </filter>
                    </entity>
                </fetch>";

            var results = service.RetrieveMultiple(new FetchExpression(fetchXml));

            // Assert - Should return both Seattle accounts
            Assert.Equal(2, results.Entities.Count);
            Assert.All(results.Entities, e =>
                Assert.Equal("Seattle", e.GetAttributeValue<string>("city")));
        }

        [Fact]
        public void When_FetchXml_Multiple_Filters_With_QueryExpression_Comparison()
        {
            // Arrange
            var context = new XrmFakedContext();

            var guid1 = Guid.NewGuid();

            var entities = new List<Entity>
            {
                new Entity("account")
                {
                    Id = guid1,
                    ["name"] = "Test Account",
                    ["statecode"] = new OptionSetValue(0),
                    ["revenue"] = new Money(50000m)
                },
                new Entity("account")
                {
                    Id = Guid.NewGuid(),
                    ["name"] = "Other Account",
                    ["statecode"] = new OptionSetValue(1),
                    ["revenue"] = new Money(50000m)
                }
            };

            context.Initialize(entities);
            var service = context.GetOrganizationService();

            // Act with FetchXML - Multiple filters
            var fetchXml = @"
                <fetch>
                    <entity name='account'>
                        <attribute name='name' />
                        <filter>
                            <condition attribute='statecode' operator='eq' value='0' />
                        </filter>
                        <filter>
                            <condition attribute='name' operator='like' value='%Test%' />
                        </filter>
                    </entity>
                </fetch>";

            var fetchResults = service.RetrieveMultiple(new FetchExpression(fetchXml));

      // Act with QueryExpression - Equivalent query
      var query = new QueryExpression("account")
      {
        ColumnSet = new ColumnSet("name")
      };
      query.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0);
            query.Criteria.AddCondition("name", ConditionOperator.Like, "%Test%");

            var queryResults = service.RetrieveMultiple(query);

            // Assert - Both should return the same result
            Assert.Equal(queryResults.Entities.Count, fetchResults.Entities.Count);
            Assert.Single(fetchResults.Entities);
            Assert.Equal(guid1, fetchResults.Entities[0].Id);
        }
    }
}
