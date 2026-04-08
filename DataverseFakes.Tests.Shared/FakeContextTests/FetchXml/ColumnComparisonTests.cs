using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace FakeXrmEasy.Tests.FakeContextTests.FetchXml
{
    /// <summary>
    /// Tests for issue #514 - FetchXML valueof attribute for column-to-column comparison.
    /// This feature allows comparing one column's value against another column's value
    /// in the same entity row.
    /// </summary>
    public class ColumnComparisonTests
    {
        [Fact]
        public void When_Valueof_Eq_Should_Return_Matching_Records()
        {
            // Arrange - Find contacts where firstname equals lastname
            var context = new XrmFakedContext();

            var contact1 = new Entity("contact") { Id = Guid.NewGuid() };
            contact1["firstname"] = "John";
            contact1["lastname"] = "John"; // Match - should be returned

            var contact2 = new Entity("contact") { Id = Guid.NewGuid() };
            contact2["firstname"] = "Jane";
            contact2["lastname"] = "Doe"; // No match

            var contact3 = new Entity("contact") { Id = Guid.NewGuid() };
            contact3["firstname"] = "Bob";
            contact3["lastname"] = "Bob"; // Match - should be returned

            context.Initialize(new[] { contact1, contact2, contact3 });

            var service = context.GetOrganizationService();

            var fetchXml = @"
                <fetch>
                    <entity name='contact'>
                        <attribute name='firstname' />
                        <attribute name='lastname' />
                        <filter>
                            <condition attribute='firstname' operator='eq' valueof='lastname' />
                        </filter>
                    </entity>
                </fetch>";

            // Act
            var results = service.RetrieveMultiple(new FetchExpression(fetchXml));

            // Assert
            Assert.Equal(2, results.Entities.Count);
            Assert.Contains(results.Entities, e => e.GetAttributeValue<string>("firstname") == "John");
            Assert.Contains(results.Entities, e => e.GetAttributeValue<string>("firstname") == "Bob");
        }

        [Fact]
        public void When_Valueof_Ne_Should_Return_Non_Matching_Records()
        {
            // Arrange - Find contacts where firstname does NOT equal lastname
            var context = new XrmFakedContext();

            var contact1 = new Entity("contact") { Id = Guid.NewGuid() };
            contact1["firstname"] = "John";
            contact1["lastname"] = "John"; // Match - should NOT be returned

            var contact2 = new Entity("contact") { Id = Guid.NewGuid() };
            contact2["firstname"] = "Jane";
            contact2["lastname"] = "Doe"; // No match - should be returned

            context.Initialize(new[] { contact1, contact2 });

            var service = context.GetOrganizationService();

            var fetchXml = @"
                <fetch>
                    <entity name='contact'>
                        <attribute name='firstname' />
                        <filter>
                            <condition attribute='firstname' operator='ne' valueof='lastname' />
                        </filter>
                    </entity>
                </fetch>";

            // Act
            var results = service.RetrieveMultiple(new FetchExpression(fetchXml));

            // Assert
            Assert.Single(results.Entities);
            Assert.Equal("Jane", results.Entities[0].GetAttributeValue<string>("firstname"));
        }

        [Fact]
        public void When_Valueof_Gt_Should_Compare_Numeric_Columns()
        {
            // Arrange - Find opportunities where actualvalue > estimatedvalue
            var context = new XrmFakedContext();

            var opp1 = new Entity("opportunity") { Id = Guid.NewGuid() };
            opp1["name"] = "Opp1";
            opp1["estimatedvalue"] = new Money(1000m);
            opp1["actualvalue"] = new Money(1500m); // Greater - should be returned

            var opp2 = new Entity("opportunity") { Id = Guid.NewGuid() };
            opp2["name"] = "Opp2";
            opp2["estimatedvalue"] = new Money(2000m);
            opp2["actualvalue"] = new Money(1800m); // Less - should NOT be returned

            var opp3 = new Entity("opportunity") { Id = Guid.NewGuid() };
            opp3["name"] = "Opp3";
            opp3["estimatedvalue"] = new Money(500m);
            opp3["actualvalue"] = new Money(500m); // Equal - should NOT be returned

            context.Initialize(new[] { opp1, opp2, opp3 });

            var service = context.GetOrganizationService();

            var fetchXml = @"
                <fetch>
                    <entity name='opportunity'>
                        <attribute name='name' />
                        <filter>
                            <condition attribute='actualvalue' operator='gt' valueof='estimatedvalue' />
                        </filter>
                    </entity>
                </fetch>";

            // Act
            var results = service.RetrieveMultiple(new FetchExpression(fetchXml));

            // Assert
            Assert.Single(results.Entities);
            Assert.Equal("Opp1", results.Entities[0].GetAttributeValue<string>("name"));
        }

        [Fact]
        public void When_Valueof_Lt_Should_Compare_Date_Columns()
        {
            // Arrange - Find tasks where actualend < scheduledend (completed early)
            var context = new XrmFakedContext();

            var task1 = new Entity("task") { Id = Guid.NewGuid() };
            task1["subject"] = "Early Task";
            task1["scheduledend"] = new DateTime(2025, 6, 15);
            task1["actualend"] = new DateTime(2025, 6, 10); // Earlier - should be returned

            var task2 = new Entity("task") { Id = Guid.NewGuid() };
            task2["subject"] = "Late Task";
            task2["scheduledend"] = new DateTime(2025, 6, 15);
            task2["actualend"] = new DateTime(2025, 6, 20); // Later - should NOT be returned

            context.Initialize(new[] { task1, task2 });

            var service = context.GetOrganizationService();

            var fetchXml = @"
                <fetch>
                    <entity name='task'>
                        <attribute name='subject' />
                        <filter>
                            <condition attribute='actualend' operator='lt' valueof='scheduledend' />
                        </filter>
                    </entity>
                </fetch>";

            // Act
            var results = service.RetrieveMultiple(new FetchExpression(fetchXml));

            // Assert
            Assert.Single(results.Entities);
            Assert.Equal("Early Task", results.Entities[0].GetAttributeValue<string>("subject"));
        }

        [Fact]
        public void When_Valueof_With_Null_Values_Should_Handle_Correctly()
        {
            // Arrange - When one column is null, comparison should not match
            var context = new XrmFakedContext();

            var contact1 = new Entity("contact") { Id = Guid.NewGuid() };
            contact1["firstname"] = "John";
            contact1["lastname"] = null; // Null - should NOT match eq

            var contact2 = new Entity("contact") { Id = Guid.NewGuid() };
            contact2["firstname"] = null;
            contact2["lastname"] = null; // Both null - debatable, but typically doesn't match eq

            var contact3 = new Entity("contact") { Id = Guid.NewGuid() };
            contact3["firstname"] = "Bob";
            contact3["lastname"] = "Bob"; // Match - should be returned

            context.Initialize(new[] { contact1, contact2, contact3 });

            var service = context.GetOrganizationService();

            var fetchXml = @"
                <fetch>
                    <entity name='contact'>
                        <attribute name='firstname' />
                        <filter>
                            <condition attribute='firstname' operator='eq' valueof='lastname' />
                        </filter>
                    </entity>
                </fetch>";

            // Act
            var results = service.RetrieveMultiple(new FetchExpression(fetchXml));

            // Assert - Only Bob should be returned (null comparisons don't match)
            Assert.Single(results.Entities);
            Assert.Equal("Bob", results.Entities[0].GetAttributeValue<string>("firstname"));
        }

        [Fact]
        public void When_Valueof_Ge_Should_Include_Equal_Values()
        {
            // Arrange - Find where quantity >= reorderpoint
            var context = new XrmFakedContext();

            var product1 = new Entity("product") { Id = Guid.NewGuid() };
            product1["name"] = "Product1";
            product1["quantityonhand"] = 100;
            product1["reorderpoint"] = 50; // Greater - should be returned

            var product2 = new Entity("product") { Id = Guid.NewGuid() };
            product2["name"] = "Product2";
            product2["quantityonhand"] = 50;
            product2["reorderpoint"] = 50; // Equal - should be returned

            var product3 = new Entity("product") { Id = Guid.NewGuid() };
            product3["name"] = "Product3";
            product3["quantityonhand"] = 30;
            product3["reorderpoint"] = 50; // Less - should NOT be returned

            context.Initialize(new[] { product1, product2, product3 });

            var service = context.GetOrganizationService();

            var fetchXml = @"
                <fetch>
                    <entity name='product'>
                        <attribute name='name' />
                        <filter>
                            <condition attribute='quantityonhand' operator='ge' valueof='reorderpoint' />
                        </filter>
                    </entity>
                </fetch>";

            // Act
            var results = service.RetrieveMultiple(new FetchExpression(fetchXml));

            // Assert
            Assert.Equal(2, results.Entities.Count);
            Assert.Contains(results.Entities, e => e.GetAttributeValue<string>("name") == "Product1");
            Assert.Contains(results.Entities, e => e.GetAttributeValue<string>("name") == "Product2");
        }

        [Fact]
        public void When_Valueof_Le_Should_Include_Equal_Values()
        {
            // Arrange - Find where actualcost <= budgetedcost
            var context = new XrmFakedContext();

            var campaign1 = new Entity("campaign") { Id = Guid.NewGuid() };
            campaign1["name"] = "Under Budget";
            campaign1["actualcost"] = new Money(800m);
            campaign1["budgetedcost"] = new Money(1000m); // Less - should be returned

            var campaign2 = new Entity("campaign") { Id = Guid.NewGuid() };
            campaign2["name"] = "On Budget";
            campaign2["actualcost"] = new Money(1000m);
            campaign2["budgetedcost"] = new Money(1000m); // Equal - should be returned

            var campaign3 = new Entity("campaign") { Id = Guid.NewGuid() };
            campaign3["name"] = "Over Budget";
            campaign3["actualcost"] = new Money(1200m);
            campaign3["budgetedcost"] = new Money(1000m); // Greater - should NOT be returned

            context.Initialize(new[] { campaign1, campaign2, campaign3 });

            var service = context.GetOrganizationService();

            var fetchXml = @"
                <fetch>
                    <entity name='campaign'>
                        <attribute name='name' />
                        <filter>
                            <condition attribute='actualcost' operator='le' valueof='budgetedcost' />
                        </filter>
                    </entity>
                </fetch>";

            // Act
            var results = service.RetrieveMultiple(new FetchExpression(fetchXml));

            // Assert
            Assert.Equal(2, results.Entities.Count);
            Assert.Contains(results.Entities, e => e.GetAttributeValue<string>("name") == "Under Budget");
            Assert.Contains(results.Entities, e => e.GetAttributeValue<string>("name") == "On Budget");
        }

        [Fact]
        public void When_Valueof_With_EntityReference_Should_Compare_Ids()
        {
            // Arrange - Find where createdby equals modifiedby (same person created and modified)
            var context = new XrmFakedContext();

            var user1 = new Entity("systemuser") { Id = Guid.NewGuid() };
            var user2 = new Entity("systemuser") { Id = Guid.NewGuid() };

            var account1 = new Entity("account") { Id = Guid.NewGuid() };
            account1["name"] = "Same User";
            account1["createdby"] = user1.ToEntityReference();
            account1["modifiedby"] = user1.ToEntityReference(); // Same - should be returned

            var account2 = new Entity("account") { Id = Guid.NewGuid() };
            account2["name"] = "Different Users";
            account2["createdby"] = user1.ToEntityReference();
            account2["modifiedby"] = user2.ToEntityReference(); // Different - should NOT be returned

            context.Initialize(new[] { user1, user2, account1, account2 });

            var service = context.GetOrganizationService();

            var fetchXml = @"
                <fetch>
                    <entity name='account'>
                        <attribute name='name' />
                        <filter>
                            <condition attribute='createdby' operator='eq' valueof='modifiedby' />
                        </filter>
                    </entity>
                </fetch>";

            // Act
            var results = service.RetrieveMultiple(new FetchExpression(fetchXml));

            // Assert
            Assert.Single(results.Entities);
            Assert.Equal("Same User", results.Entities[0].GetAttributeValue<string>("name"));
        }

        #region SDK QueryExpression CompareColumns Tests

        [Fact]
        public void When_QueryExpression_CompareColumns_Eq_Should_Return_Matching_Records()
        {
            // Arrange - Find contacts where firstname equals lastname using SDK QueryExpression
            var context = new XrmFakedContext();

            var contact1 = new Entity("contact") { Id = Guid.NewGuid() };
            contact1["firstname"] = "John";
            contact1["lastname"] = "John"; // Match - should be returned

            var contact2 = new Entity("contact") { Id = Guid.NewGuid() };
            contact2["firstname"] = "Jane";
            contact2["lastname"] = "Doe"; // No match

            var contact3 = new Entity("contact") { Id = Guid.NewGuid() };
            contact3["firstname"] = "Bob";
            contact3["lastname"] = "Bob"; // Match - should be returned

            context.Initialize(new[] { contact1, contact2, contact3 });

            var service = context.GetOrganizationService();

            // Using SDK QueryExpression with CompareColumns property
            var query = new QueryExpression("contact")
            {
                ColumnSet = new ColumnSet("firstname", "lastname"),
                Criteria = new FilterExpression(LogicalOperator.And)
                {
                    Conditions = {
                        new ConditionExpression(
                            attributeName: "firstname",
                            conditionOperator: ConditionOperator.Equal,
                            compareColumns: true,
                            value: "lastname")
                    }
                }
            };

            // Act
            var results = service.RetrieveMultiple(query);

            // Assert
            Assert.Equal(2, results.Entities.Count);
            Assert.Contains(results.Entities, e => e.GetAttributeValue<string>("firstname") == "John");
            Assert.Contains(results.Entities, e => e.GetAttributeValue<string>("firstname") == "Bob");
        }

        [Fact]
        public void When_QueryExpression_CompareColumns_Ne_Should_Return_Non_Matching_Records()
        {
            // Arrange - Find contacts where firstname NOT equals lastname using SDK QueryExpression
            var context = new XrmFakedContext();

            var contact1 = new Entity("contact") { Id = Guid.NewGuid() };
            contact1["firstname"] = "John";
            contact1["lastname"] = "John"; // Match - should NOT be returned

            var contact2 = new Entity("contact") { Id = Guid.NewGuid() };
            contact2["firstname"] = "Jane";
            contact2["lastname"] = "Doe"; // No match - should be returned

            context.Initialize(new[] { contact1, contact2 });

            var service = context.GetOrganizationService();

            var query = new QueryExpression("contact")
            {
                ColumnSet = new ColumnSet("firstname"),
                Criteria = new FilterExpression(LogicalOperator.And)
                {
                    Conditions = {
                        new ConditionExpression(
                            attributeName: "firstname",
                            conditionOperator: ConditionOperator.NotEqual,
                            compareColumns: true,
                            value: "lastname")
                    }
                }
            };

            // Act
            var results = service.RetrieveMultiple(query);

            // Assert
            Assert.Single(results.Entities);
            Assert.Equal("Jane", results.Entities[0].GetAttributeValue<string>("firstname"));
        }

        [Fact]
        public void When_QueryExpression_CompareColumns_Gt_Should_Compare_Values()
        {
            // Arrange - Find products where quantityonhand > reorderpoint
            var context = new XrmFakedContext();

            var product1 = new Entity("product") { Id = Guid.NewGuid() };
            product1["name"] = "Product1";
            product1["quantityonhand"] = 100;
            product1["reorderpoint"] = 50; // Greater - should be returned

            var product2 = new Entity("product") { Id = Guid.NewGuid() };
            product2["name"] = "Product2";
            product2["quantityonhand"] = 30;
            product2["reorderpoint"] = 50; // Less - should NOT be returned

            context.Initialize(new[] { product1, product2 });

            var service = context.GetOrganizationService();

            var query = new QueryExpression("product")
            {
                ColumnSet = new ColumnSet("name"),
                Criteria = new FilterExpression(LogicalOperator.And)
                {
                    Conditions = {
                        new ConditionExpression(
                            attributeName: "quantityonhand",
                            conditionOperator: ConditionOperator.GreaterThan,
                            compareColumns: true,
                            value: "reorderpoint")
                    }
                }
            };

            // Act
            var results = service.RetrieveMultiple(query);

            // Assert
            Assert.Single(results.Entities);
            Assert.Equal("Product1", results.Entities[0].GetAttributeValue<string>("name"));
        }

        #endregion
    }
}
