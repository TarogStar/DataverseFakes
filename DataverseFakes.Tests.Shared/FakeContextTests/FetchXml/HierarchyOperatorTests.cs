using Crm;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace FakeXrmEasy.Tests.FakeContextTests.FetchXml
{
    /// <summary>
    /// Tests for hierarchy operators (Above, AboveOrEqual/eq-or-above, Under, UnderOrEqual/eq-or-under, NotUnder).
    /// These operators work on entities with self-referential parent-child relationships.
    ///
    /// Issue #287: AboveOrEqual hierarchy operator not implemented
    /// </summary>
    public class HierarchyOperatorTests
    {
        /// <summary>
        /// Creates a test hierarchy of accounts:
        ///
        /// RootAccount (no parent)
        ///   ├── Level1Account (parent: Root)
        ///   │     ├── Level2AccountA (parent: Level1)
        ///   │     └── Level2AccountB (parent: Level1)
        ///   │           └── Level3Account (parent: Level2B)
        ///   └── Level1AccountB (parent: Root)
        ///
        /// UnrelatedAccount (no parent, not in the hierarchy)
        /// </summary>
        private (XrmFakedContext context, Dictionary<string, Guid> accountIds) CreateTestHierarchy()
        {
      var context = new XrmFakedContext
      {
        ProxyTypesAssembly = Assembly.GetExecutingAssembly()
      };

      // Define the hierarchical relationship for account entity
      context.HierarchicalRelationships["account"] = "parentaccountid";

            var accountIds = new Dictionary<string, Guid>
            {
                { "Root", Guid.NewGuid() },
                { "Level1A", Guid.NewGuid() },
                { "Level1B", Guid.NewGuid() },
                { "Level2A", Guid.NewGuid() },
                { "Level2B", Guid.NewGuid() },
                { "Level3", Guid.NewGuid() },
                { "Unrelated", Guid.NewGuid() }
            };

            var accounts = new List<Entity>
            {
                new Account { Id = accountIds["Root"], Name = "Root Account" },
                new Account { Id = accountIds["Level1A"], Name = "Level 1 Account A", ParentAccountId = new EntityReference(Account.EntityLogicalName, accountIds["Root"]) },
                new Account { Id = accountIds["Level1B"], Name = "Level 1 Account B", ParentAccountId = new EntityReference(Account.EntityLogicalName, accountIds["Root"]) },
                new Account { Id = accountIds["Level2A"], Name = "Level 2 Account A", ParentAccountId = new EntityReference(Account.EntityLogicalName, accountIds["Level1A"]) },
                new Account { Id = accountIds["Level2B"], Name = "Level 2 Account B", ParentAccountId = new EntityReference(Account.EntityLogicalName, accountIds["Level1A"]) },
                new Account { Id = accountIds["Level3"], Name = "Level 3 Account", ParentAccountId = new EntityReference(Account.EntityLogicalName, accountIds["Level2B"]) },
                new Account { Id = accountIds["Unrelated"], Name = "Unrelated Account" }
            };

            context.Initialize(accounts);

            return (context, accountIds);
        }

        #region AboveOrEqual Tests (eq-or-above) - Issue #287

        [Fact]
        public void FetchXml_AboveOrEqual_ReturnsRecordAndAllAncestors()
        {
            // Arrange
            var (context, accountIds) = CreateTestHierarchy();
            var service = context.GetOrganizationService();

            // Act - Query for accounts that are above or equal to Level2B
            // Should return: Level2B (itself), Level1A (parent), Root (grandparent)
            var fetchXml = $@"
                <fetch>
                    <entity name='account'>
                        <attribute name='name' />
                        <filter>
                            <condition attribute='accountid' operator='eq-or-above' value='{accountIds["Level2B"]}' />
                        </filter>
                    </entity>
                </fetch>";

            var result = service.RetrieveMultiple(new FetchExpression(fetchXml));

            // Assert
            Assert.Equal(3, result.Entities.Count);
            var resultIds = new HashSet<Guid>(result.Entities.Select(e => e.Id));
            Assert.Contains(accountIds["Level2B"], resultIds); // The record itself
            Assert.Contains(accountIds["Level1A"], resultIds); // Parent
            Assert.Contains(accountIds["Root"], resultIds);    // Grandparent
        }

        [Fact]
        public void FetchXml_AboveOrEqual_FromRootReturnsOnlyRoot()
        {
            // Arrange
            var (context, accountIds) = CreateTestHierarchy();
            var service = context.GetOrganizationService();

            // Act - Query for accounts that are above or equal to Root
            // Should return only Root (no ancestors)
            var fetchXml = $@"
                <fetch>
                    <entity name='account'>
                        <attribute name='name' />
                        <filter>
                            <condition attribute='accountid' operator='eq-or-above' value='{accountIds["Root"]}' />
                        </filter>
                    </entity>
                </fetch>";

            var result = service.RetrieveMultiple(new FetchExpression(fetchXml));

            // Assert
            Assert.Single(result.Entities);
            Assert.Equal(accountIds["Root"], result.Entities[0].Id);
        }

        [Fact]
        public void QueryExpression_AboveOrEqual_ReturnsRecordAndAllAncestors()
        {
            // Arrange
            var (context, accountIds) = CreateTestHierarchy();
            var service = context.GetOrganizationService();

      // Act - Query using QueryExpression
      var query = new QueryExpression(Account.EntityLogicalName)
      {
        ColumnSet = new ColumnSet("name")
      };
      query.Criteria.AddCondition("accountid", ConditionOperator.AboveOrEqual, accountIds["Level3"]);

            var result = service.RetrieveMultiple(query);

            // Assert - Should return Level3, Level2B, Level1A, Root
            Assert.Equal(4, result.Entities.Count);
            var resultIds = new HashSet<Guid>(result.Entities.Select(e => e.Id));
            Assert.Contains(accountIds["Level3"], resultIds);
            Assert.Contains(accountIds["Level2B"], resultIds);
            Assert.Contains(accountIds["Level1A"], resultIds);
            Assert.Contains(accountIds["Root"], resultIds);
        }

        #endregion

        #region Above Tests

        [Fact]
        public void FetchXml_Above_ReturnsOnlyAncestors()
        {
            // Arrange
            var (context, accountIds) = CreateTestHierarchy();
            var service = context.GetOrganizationService();

            // Act - Query for accounts that are above Level2B (excludes Level2B itself)
            var fetchXml = $@"
                <fetch>
                    <entity name='account'>
                        <attribute name='name' />
                        <filter>
                            <condition attribute='accountid' operator='above' value='{accountIds["Level2B"]}' />
                        </filter>
                    </entity>
                </fetch>";

            var result = service.RetrieveMultiple(new FetchExpression(fetchXml));

            // Assert - Should NOT include Level2B itself
            Assert.Equal(2, result.Entities.Count);
            var resultIds = new HashSet<Guid>(result.Entities.Select(e => e.Id));
            Assert.DoesNotContain(accountIds["Level2B"], resultIds);
            Assert.Contains(accountIds["Level1A"], resultIds);
            Assert.Contains(accountIds["Root"], resultIds);
        }

        [Fact]
        public void FetchXml_Above_FromRootReturnsEmpty()
        {
            // Arrange
            var (context, accountIds) = CreateTestHierarchy();
            var service = context.GetOrganizationService();

            // Act - Query for accounts that are above Root (no ancestors)
            var fetchXml = $@"
                <fetch>
                    <entity name='account'>
                        <attribute name='name' />
                        <filter>
                            <condition attribute='accountid' operator='above' value='{accountIds["Root"]}' />
                        </filter>
                    </entity>
                </fetch>";

            var result = service.RetrieveMultiple(new FetchExpression(fetchXml));

            // Assert
            Assert.Empty(result.Entities);
        }

        #endregion

        #region UnderOrEqual Tests (eq-or-under)

        [Fact]
        public void FetchXml_UnderOrEqual_ReturnsRecordAndAllDescendants()
        {
            // Arrange
            var (context, accountIds) = CreateTestHierarchy();
            var service = context.GetOrganizationService();

            // Act - Query for accounts that are under or equal to Level1A
            // Should return: Level1A, Level2A, Level2B, Level3
            var fetchXml = $@"
                <fetch>
                    <entity name='account'>
                        <attribute name='name' />
                        <filter>
                            <condition attribute='accountid' operator='eq-or-under' value='{accountIds["Level1A"]}' />
                        </filter>
                    </entity>
                </fetch>";

            var result = service.RetrieveMultiple(new FetchExpression(fetchXml));

            // Assert
            Assert.Equal(4, result.Entities.Count);
            var resultIds = new HashSet<Guid>(result.Entities.Select(e => e.Id));
            Assert.Contains(accountIds["Level1A"], resultIds);
            Assert.Contains(accountIds["Level2A"], resultIds);
            Assert.Contains(accountIds["Level2B"], resultIds);
            Assert.Contains(accountIds["Level3"], resultIds);
        }

        [Fact]
        public void FetchXml_UnderOrEqual_FromLeafReturnsOnlyLeaf()
        {
            // Arrange
            var (context, accountIds) = CreateTestHierarchy();
            var service = context.GetOrganizationService();

            // Act - Query for accounts that are under or equal to Level3 (a leaf node)
            var fetchXml = $@"
                <fetch>
                    <entity name='account'>
                        <attribute name='name' />
                        <filter>
                            <condition attribute='accountid' operator='eq-or-under' value='{accountIds["Level3"]}' />
                        </filter>
                    </entity>
                </fetch>";

            var result = service.RetrieveMultiple(new FetchExpression(fetchXml));

            // Assert
            Assert.Single(result.Entities);
            Assert.Equal(accountIds["Level3"], result.Entities[0].Id);
        }

        #endregion

        #region Under Tests

        [Fact]
        public void FetchXml_Under_ReturnsOnlyDescendants()
        {
            // Arrange
            var (context, accountIds) = CreateTestHierarchy();
            var service = context.GetOrganizationService();

            // Act - Query for accounts that are under Root
            var fetchXml = $@"
                <fetch>
                    <entity name='account'>
                        <attribute name='name' />
                        <filter>
                            <condition attribute='accountid' operator='under' value='{accountIds["Root"]}' />
                        </filter>
                    </entity>
                </fetch>";

            var result = service.RetrieveMultiple(new FetchExpression(fetchXml));

            // Assert - Should NOT include Root itself, but include all 5 descendants
            Assert.Equal(5, result.Entities.Count);
            var resultIds = new HashSet<Guid>(result.Entities.Select(e => e.Id));
            Assert.DoesNotContain(accountIds["Root"], resultIds);
            Assert.DoesNotContain(accountIds["Unrelated"], resultIds);
            Assert.Contains(accountIds["Level1A"], resultIds);
            Assert.Contains(accountIds["Level1B"], resultIds);
            Assert.Contains(accountIds["Level2A"], resultIds);
            Assert.Contains(accountIds["Level2B"], resultIds);
            Assert.Contains(accountIds["Level3"], resultIds);
        }

        [Fact]
        public void FetchXml_Under_FromLeafReturnsEmpty()
        {
            // Arrange
            var (context, accountIds) = CreateTestHierarchy();
            var service = context.GetOrganizationService();

            // Act - Query for accounts that are under Level3 (a leaf node)
            var fetchXml = $@"
                <fetch>
                    <entity name='account'>
                        <attribute name='name' />
                        <filter>
                            <condition attribute='accountid' operator='under' value='{accountIds["Level3"]}' />
                        </filter>
                    </entity>
                </fetch>";

            var result = service.RetrieveMultiple(new FetchExpression(fetchXml));

            // Assert
            Assert.Empty(result.Entities);
        }

        #endregion

        #region NotUnder Tests

        [Fact]
        public void FetchXml_NotUnder_ReturnsRecordsNotInDescendantTree()
        {
            // Arrange
            var (context, accountIds) = CreateTestHierarchy();
            var service = context.GetOrganizationService();

            // Act - Query for accounts that are NOT under Level1A
            // Should include: Root, Level1B, Unrelated
            // Should exclude: Level1A's descendants (Level2A, Level2B, Level3)
            var fetchXml = $@"
                <fetch>
                    <entity name='account'>
                        <attribute name='name' />
                        <filter>
                            <condition attribute='accountid' operator='not-under' value='{accountIds["Level1A"]}' />
                        </filter>
                    </entity>
                </fetch>";

            var result = service.RetrieveMultiple(new FetchExpression(fetchXml));

            // Assert
            var resultIds = new HashSet<Guid>(result.Entities.Select(e => e.Id));
            Assert.Contains(accountIds["Root"], resultIds);
            Assert.Contains(accountIds["Level1A"], resultIds); // The reference record itself is NOT under itself
            Assert.Contains(accountIds["Level1B"], resultIds);
            Assert.Contains(accountIds["Unrelated"], resultIds);
            Assert.DoesNotContain(accountIds["Level2A"], resultIds);
            Assert.DoesNotContain(accountIds["Level2B"], resultIds);
            Assert.DoesNotContain(accountIds["Level3"], resultIds);
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public void HierarchyOperator_ThrowsWhenNoHierarchyDefined()
        {
      // Arrange
      var context = new XrmFakedContext
      {
        ProxyTypesAssembly = Assembly.GetExecutingAssembly()
      };
      // Note: NOT defining HierarchicalRelationships

      var account = new Account { Id = Guid.NewGuid(), Name = "Test" };
            context.Initialize(new List<Entity> { account });

            var service = context.GetOrganizationService();

            // Act & Assert
            var fetchXml = $@"
                <fetch>
                    <entity name='account'>
                        <attribute name='name' />
                        <filter>
                            <condition attribute='accountid' operator='eq-or-above' value='{account.Id}' />
                        </filter>
                    </entity>
                </fetch>";

            var exception = Assert.Throws<Exception>(() => service.RetrieveMultiple(new FetchExpression(fetchXml)));
            Assert.Contains("hierarchical relationship", exception.Message.ToLower());
        }

        [Fact]
        public void HierarchyOperator_NonExistentRecordReturnsEmpty()
        {
            // Arrange
            var (context, accountIds) = CreateTestHierarchy();
            var service = context.GetOrganizationService();

            var nonExistentId = Guid.NewGuid();

            // Act - Query with a non-existent record ID
            var fetchXml = $@"
                <fetch>
                    <entity name='account'>
                        <attribute name='name' />
                        <filter>
                            <condition attribute='accountid' operator='eq-or-above' value='{nonExistentId}' />
                        </filter>
                    </entity>
                </fetch>";

            var result = service.RetrieveMultiple(new FetchExpression(fetchXml));

            // Assert - Should return empty since the reference record doesn't exist
            Assert.Empty(result.Entities);
        }

        #endregion

        #region FetchXML Translation Tests

        [Fact]
        public void FetchXml_AboveOrEqual_TranslatesCorrectly()
        {
            // Arrange
            var context = new XrmFakedContext();
            var referenceId = Guid.NewGuid();
            var fetchXml = $@"
                <fetch>
                    <entity name='account'>
                        <filter>
                            <condition attribute='accountid' operator='eq-or-above' value='{referenceId}' />
                        </filter>
                    </entity>
                </fetch>";

            // Act
            var query = XrmFakedContext.TranslateFetchXmlToQueryExpression(context, fetchXml);

            // Assert
            Assert.Single(query.Criteria.Conditions);
            Assert.Equal(ConditionOperator.AboveOrEqual, query.Criteria.Conditions[0].Operator);
        }

        [Fact]
        public void FetchXml_Above_TranslatesCorrectly()
        {
            // Arrange
            var context = new XrmFakedContext();
            var referenceId = Guid.NewGuid();
            var fetchXml = $@"
                <fetch>
                    <entity name='account'>
                        <filter>
                            <condition attribute='accountid' operator='above' value='{referenceId}' />
                        </filter>
                    </entity>
                </fetch>";

            // Act
            var query = XrmFakedContext.TranslateFetchXmlToQueryExpression(context, fetchXml);

            // Assert
            Assert.Single(query.Criteria.Conditions);
            Assert.Equal(ConditionOperator.Above, query.Criteria.Conditions[0].Operator);
        }

        [Fact]
        public void FetchXml_Under_TranslatesCorrectly()
        {
            // Arrange
            var context = new XrmFakedContext();
            var referenceId = Guid.NewGuid();
            var fetchXml = $@"
                <fetch>
                    <entity name='account'>
                        <filter>
                            <condition attribute='accountid' operator='under' value='{referenceId}' />
                        </filter>
                    </entity>
                </fetch>";

            // Act
            var query = XrmFakedContext.TranslateFetchXmlToQueryExpression(context, fetchXml);

            // Assert
            Assert.Single(query.Criteria.Conditions);
            Assert.Equal(ConditionOperator.Under, query.Criteria.Conditions[0].Operator);
        }

        [Fact]
        public void FetchXml_UnderOrEqual_TranslatesCorrectly()
        {
            // Arrange
            var context = new XrmFakedContext();
            var referenceId = Guid.NewGuid();
            var fetchXml = $@"
                <fetch>
                    <entity name='account'>
                        <filter>
                            <condition attribute='accountid' operator='eq-or-under' value='{referenceId}' />
                        </filter>
                    </entity>
                </fetch>";

            // Act
            var query = XrmFakedContext.TranslateFetchXmlToQueryExpression(context, fetchXml);

            // Assert
            Assert.Single(query.Criteria.Conditions);
            Assert.Equal(ConditionOperator.UnderOrEqual, query.Criteria.Conditions[0].Operator);
        }

        [Fact]
        public void FetchXml_NotUnder_TranslatesCorrectly()
        {
            // Arrange
            var context = new XrmFakedContext();
            var referenceId = Guid.NewGuid();
            var fetchXml = $@"
                <fetch>
                    <entity name='account'>
                        <filter>
                            <condition attribute='accountid' operator='not-under' value='{referenceId}' />
                        </filter>
                    </entity>
                </fetch>";

            // Act
            var query = XrmFakedContext.TranslateFetchXmlToQueryExpression(context, fetchXml);

            // Assert
            Assert.Single(query.Criteria.Conditions);
            Assert.Equal(ConditionOperator.NotUnder, query.Criteria.Conditions[0].Operator);
        }

        #endregion
    }
}
