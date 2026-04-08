using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace FakeXrmEasy.Tests.FakeContextTests.NestedQueryIssuesTests
{
    /// <summary>
    /// Tests for issue #547 - ConditionOperator.Null broken in nested filters.
    /// The issue is that null conditions in nested LinkEntity filters
    /// return no results when they should match.
    /// </summary>
    public class Issue547_NullInNestedFiltersTests
    {
        [Fact]
        public void When_Null_Condition_In_Nested_Filter_Should_Return_Matching_Records()
        {
            // Arrange - Reproduces the exact scenario from issue #547
            // Person entity with Employment linked entity
            // Query for persons with employment where EndDate is null (current employment)

            var context = new XrmFakedContext();

            var person1 = new Entity("person") { Id = Guid.NewGuid() };
            person1["name"] = "John Doe";

            var person2 = new Entity("person") { Id = Guid.NewGuid() };
            person2["name"] = "Jane Smith";

            // Employment for person1 - EndDate is null (current job)
            var employment1 = new Entity("employment") { Id = Guid.NewGuid() };
            employment1["personid"] = person1.ToEntityReference();
            employment1["startdate"] = new DateTime(2020, 1, 1);
            employment1["enddate"] = null; // Current employment - should match

            // Employment for person2 - EndDate is set (past job)
            var employment2 = new Entity("employment") { Id = Guid.NewGuid() };
            employment2["personid"] = person2.ToEntityReference();
            employment2["startdate"] = new DateTime(2018, 1, 1);
            employment2["enddate"] = new DateTime(2019, 12, 31); // Past employment - should NOT match

            context.Initialize(new[] { person1, person2, employment1, employment2 });

            var service = context.GetOrganizationService();

            // Act - Query with nested filter containing Null condition
            var query = new QueryExpression("person")
            {
                ColumnSet = new ColumnSet("name"),
                LinkEntities =
                {
                    new LinkEntity
                    {
                        LinkFromEntityName = "person",
                        LinkFromAttributeName = "personid",
                        LinkToEntityName = "employment",
                        LinkToAttributeName = "personid",
                        JoinOperator = JoinOperator.Inner,
                        // Nested filter with Null condition - this is the problematic case
                        LinkCriteria = new FilterExpression
                        {
                            Filters =
                            {
                                new FilterExpression(LogicalOperator.And)
                                {
                                    Conditions =
                                    {
                                        new ConditionExpression("enddate", ConditionOperator.Null)
                                    }
                                }
                            }
                        }
                    }
                }
            };

            var results = service.RetrieveMultiple(query);

            // Assert - Should return person1 (John Doe) with current employment
            Assert.Single(results.Entities);
            Assert.Equal("John Doe", results.Entities[0].GetAttributeValue<string>("name"));
        }

        [Fact]
        public void When_Null_Condition_Direct_In_LinkCriteria_Should_Work()
        {
            // This tests direct conditions (not nested) - should work as baseline
            var context = new XrmFakedContext();

            var person1 = new Entity("person") { Id = Guid.NewGuid() };
            person1["name"] = "John Doe";

            var employment1 = new Entity("employment") { Id = Guid.NewGuid() };
            employment1["personid"] = person1.ToEntityReference();
            employment1["enddate"] = null;

            context.Initialize(new[] { person1, employment1 });

            var service = context.GetOrganizationService();

            // Direct condition in LinkCriteria (not nested in Filters)
            var query = new QueryExpression("person")
            {
                ColumnSet = new ColumnSet("name"),
                LinkEntities =
                {
                    new LinkEntity
                    {
                        LinkFromEntityName = "person",
                        LinkFromAttributeName = "personid",
                        LinkToEntityName = "employment",
                        LinkToAttributeName = "personid",
                        JoinOperator = JoinOperator.Inner,
                        LinkCriteria = new FilterExpression
                        {
                            Conditions =
                            {
                                new ConditionExpression("enddate", ConditionOperator.Null)
                            }
                        }
                    }
                }
            };

            var results = service.RetrieveMultiple(query);

            Assert.Single(results.Entities);
            Assert.Equal("John Doe", results.Entities[0].GetAttributeValue<string>("name"));
        }

        [Fact]
        public void When_NotNull_Condition_In_Nested_Filter_Should_Return_Matching_Records()
        {
            // Arrange
            var context = new XrmFakedContext();

            var person1 = new Entity("person") { Id = Guid.NewGuid() };
            person1["name"] = "John Doe";

            var person2 = new Entity("person") { Id = Guid.NewGuid() };
            person2["name"] = "Jane Smith";

            var employment1 = new Entity("employment") { Id = Guid.NewGuid() };
            employment1["personid"] = person1.ToEntityReference();
            employment1["enddate"] = null; // Should NOT match NotNull

            var employment2 = new Entity("employment") { Id = Guid.NewGuid() };
            employment2["personid"] = person2.ToEntityReference();
            employment2["enddate"] = new DateTime(2019, 12, 31); // Should match NotNull

            context.Initialize(new[] { person1, person2, employment1, employment2 });

            var service = context.GetOrganizationService();

            var query = new QueryExpression("person")
            {
                ColumnSet = new ColumnSet("name"),
                LinkEntities =
                {
                    new LinkEntity
                    {
                        LinkFromEntityName = "person",
                        LinkFromAttributeName = "personid",
                        LinkToEntityName = "employment",
                        LinkToAttributeName = "personid",
                        JoinOperator = JoinOperator.Inner,
                        LinkCriteria = new FilterExpression
                        {
                            Filters =
                            {
                                new FilterExpression(LogicalOperator.And)
                                {
                                    Conditions =
                                    {
                                        new ConditionExpression("enddate", ConditionOperator.NotNull)
                                    }
                                }
                            }
                        }
                    }
                }
            };

            var results = service.RetrieveMultiple(query);

            // Should return Jane Smith with past employment
            Assert.Single(results.Entities);
            Assert.Equal("Jane Smith", results.Entities[0].GetAttributeValue<string>("name"));
        }

        [Fact]
        public void When_Deeply_Nested_Filter_With_Null_Should_Work()
        {
            // Test deeply nested filters (Filters within Filters)
            var context = new XrmFakedContext();

            var person1 = new Entity("person") { Id = Guid.NewGuid() };
            person1["name"] = "John Doe";

            var employment1 = new Entity("employment") { Id = Guid.NewGuid() };
            employment1["personid"] = person1.ToEntityReference();
            employment1["enddate"] = null;
            employment1["status"] = new OptionSetValue(1); // Active

            context.Initialize(new[] { person1, employment1 });

            var service = context.GetOrganizationService();

            var query = new QueryExpression("person")
            {
                ColumnSet = new ColumnSet("name"),
                LinkEntities =
                {
                    new LinkEntity
                    {
                        LinkFromEntityName = "person",
                        LinkFromAttributeName = "personid",
                        LinkToEntityName = "employment",
                        LinkToAttributeName = "personid",
                        JoinOperator = JoinOperator.Inner,
                        LinkCriteria = new FilterExpression
                        {
                            Filters =
                            {
                                new FilterExpression(LogicalOperator.And)
                                {
                                    Filters =
                                    {
                                        new FilterExpression(LogicalOperator.And)
                                        {
                                            Conditions =
                                            {
                                                new ConditionExpression("enddate", ConditionOperator.Null)
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };

            var results = service.RetrieveMultiple(query);

            Assert.Single(results.Entities);
            Assert.Equal("John Doe", results.Entities[0].GetAttributeValue<string>("name"));
        }
    }
}
