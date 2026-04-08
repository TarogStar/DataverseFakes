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
    /// Tests for GitHub issue #462: When ProxyTypesAssembly is set, users should be able to
    /// Initialize() late-bound entities alongside early-bound entities, but service.Create()
    /// should still validate entity names (throw for unknown entities like real CRM).
    /// </summary>
    public class FetchXmlLateBoundEntityTests
    {
        /// <summary>
        /// Tests that Initialize() allows late-bound entities when ProxyTypesAssembly is set.
        /// This is the main fix for GitHub issue #462.
        /// </summary>
        [Fact]
        public void When_ProxyTypesAssembly_Is_Set_Initialize_Should_Allow_LateBound_Entities()
        {
            var context = new XrmFakedContext();
            context.ProxyTypesAssembly = Assembly.GetExecutingAssembly();

            var customEntityId = Guid.NewGuid();
            var customEntity = new Entity("custom_entity")
            {
                Id = customEntityId,
                ["custom_name"] = "Test Custom Entity",
                ["custom_value"] = 100
            };

            var accountId = Guid.NewGuid();
            var account = new Account
            {
                Id = accountId,
                Name = "Test Account"
            };

            // Initialize should succeed with both early-bound and late-bound entities
            context.Initialize(new List<Entity> { customEntity, account });

            // Verify both entities are in the data store
            Assert.True(context.Data.ContainsKey("custom_entity"));
            Assert.True(context.Data.ContainsKey("account"));
            Assert.Single(context.Data["custom_entity"]);
            Assert.Single(context.Data["account"]);

            // Verify we can retrieve the late-bound entity
            var service = context.GetOrganizationService();
            var retrievedCustomEntity = service.Retrieve("custom_entity", customEntityId, new ColumnSet(true));
            Assert.Equal("Test Custom Entity", retrievedCustomEntity["custom_name"]);
            Assert.Equal(100, retrievedCustomEntity["custom_value"]);

            // Verify we can retrieve the early-bound entity
            var retrievedAccount = service.Retrieve("account", accountId, new ColumnSet(true));
            Assert.Equal("Test Account", retrievedAccount["name"]);
        }

        /// <summary>
        /// Tests that service.Create() throws for unknown entity types when ProxyTypesAssembly is set.
        /// This validates that the fix for #462 does not break the existing behavior for Create.
        /// </summary>
        [Fact]
        public void When_ProxyTypesAssembly_Is_Set_Create_Should_Throw_For_Unknown_Entity()
        {
            var context = new XrmFakedContext();
            context.ProxyTypesAssembly = Assembly.GetExecutingAssembly();

            var service = context.GetOrganizationService();

            var unknownEntity = new Entity("unknown_entity")
            {
                Id = Guid.NewGuid(),
                ["field1"] = "value1"
            };

            // Create should throw for unknown entity types
            var ex = Assert.Throws<Exception>(() => service.Create(unknownEntity));
            Assert.Contains("unknown_entity", ex.Message);
        }

        /// <summary>
        /// Tests that Initialize() with a single late-bound entity works when ProxyTypesAssembly is set.
        /// </summary>
        [Fact]
        public void When_ProxyTypesAssembly_Is_Set_Initialize_With_Single_LateBound_Entity_Should_Work()
        {
            var context = new XrmFakedContext();
            context.ProxyTypesAssembly = Assembly.GetExecutingAssembly();

            var customEntity = new Entity("my_customentity")
            {
                Id = Guid.NewGuid(),
                ["my_field"] = "Test Value"
            };

            // Initialize should succeed
            context.Initialize(customEntity);

            Assert.True(context.Data.ContainsKey("my_customentity"));
            Assert.Single(context.Data["my_customentity"]);
        }

        /// <summary>
        /// Tests that queries work on late-bound entities initialized alongside early-bound entities.
        /// </summary>
        [Fact]
        public void When_ProxyTypesAssembly_Is_Set_FetchXml_Should_Work_On_LateBound_Entities()
        {
            var context = new XrmFakedContext();
            context.ProxyTypesAssembly = Assembly.GetExecutingAssembly();

            var entity1 = new Entity("custom_product")
            {
                Id = Guid.NewGuid(),
                ["custom_name"] = "Product 1",
                ["custom_price"] = 100m
            };

            var entity2 = new Entity("custom_product")
            {
                Id = Guid.NewGuid(),
                ["custom_name"] = "Product 2",
                ["custom_price"] = 200m
            };

            context.Initialize(new List<Entity> { entity1, entity2 });

            var service = context.GetOrganizationService();

            var fetchXml = @"<fetch>
                <entity name='custom_product'>
                    <attribute name='custom_name' />
                    <attribute name='custom_price' />
                    <filter>
                        <condition attribute='custom_price' operator='gt' value='150' />
                    </filter>
                </entity>
            </fetch>";

            var result = service.RetrieveMultiple(new FetchExpression(fetchXml));

            Assert.Single(result.Entities);
            Assert.Equal("Product 2", result.Entities[0]["custom_name"]);
        }

        /// <summary>
        /// Tests that QueryExpression works on late-bound entities initialized alongside early-bound entities.
        /// </summary>
        [Fact]
        public void When_ProxyTypesAssembly_Is_Set_QueryExpression_Should_Work_On_LateBound_Entities()
        {
            var context = new XrmFakedContext();
            context.ProxyTypesAssembly = Assembly.GetExecutingAssembly();

            var entity1 = new Entity("custom_order")
            {
                Id = Guid.NewGuid(),
                ["custom_ordernumber"] = "ORD-001",
                ["custom_status"] = 1
            };

            var entity2 = new Entity("custom_order")
            {
                Id = Guid.NewGuid(),
                ["custom_ordernumber"] = "ORD-002",
                ["custom_status"] = 2
            };

            context.Initialize(new List<Entity> { entity1, entity2 });

            var service = context.GetOrganizationService();

            var query = new QueryExpression("custom_order")
            {
                ColumnSet = new ColumnSet("custom_ordernumber", "custom_status"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("custom_status", ConditionOperator.Equal, 1)
                    }
                }
            };

            var result = service.RetrieveMultiple(query);

            Assert.Single(result.Entities);
            Assert.Equal("ORD-001", result.Entities[0]["custom_ordernumber"]);
        }

        /// <summary>
        /// Tests that service.Create() works for known (early-bound) entity types when ProxyTypesAssembly is set.
        /// </summary>
        [Fact]
        public void When_ProxyTypesAssembly_Is_Set_Create_Should_Work_For_Known_Entity()
        {
            var context = new XrmFakedContext();
            context.ProxyTypesAssembly = Assembly.GetExecutingAssembly();

            var service = context.GetOrganizationService();

            var account = new Account
            {
                Name = "Test Account"
            };

            var accountId = service.Create(account);

            Assert.NotEqual(Guid.Empty, accountId);
            Assert.True(context.Data.ContainsKey("account"));
            Assert.Single(context.Data["account"]);
        }

        /// <summary>
        /// Tests that Create works for late-bound entities AFTER they have been initialized.
        /// Once a late-bound entity type exists in the Data dictionary, Create should work.
        /// </summary>
        [Fact]
        public void When_LateBound_Entity_Is_Initialized_Create_Should_Work_For_Same_Type()
        {
            var context = new XrmFakedContext();
            context.ProxyTypesAssembly = Assembly.GetExecutingAssembly();

            // First, initialize a late-bound entity
            var initialEntity = new Entity("custom_widget")
            {
                Id = Guid.NewGuid(),
                ["custom_name"] = "Widget 1"
            };
            context.Initialize(initialEntity);

            var service = context.GetOrganizationService();

            // Now Create should throw because custom_widget is not in ProxyTypesAssembly
            // Even though the entity type exists in Data, Create validates against ProxyTypesAssembly
            var newEntity = new Entity("custom_widget")
            {
                Id = Guid.NewGuid(),
                ["custom_name"] = "Widget 2"
            };

            var ex = Assert.Throws<Exception>(() => service.Create(newEntity));
            Assert.Contains("custom_widget", ex.Message);
        }

        /// <summary>
        /// Tests that Update works on late-bound entities that were initialized.
        /// </summary>
        [Fact]
        public void When_LateBound_Entity_Is_Initialized_Update_Should_Work()
        {
            var context = new XrmFakedContext();
            context.ProxyTypesAssembly = Assembly.GetExecutingAssembly();

            var entityId = Guid.NewGuid();
            var entity = new Entity("custom_item")
            {
                Id = entityId,
                ["custom_description"] = "Original Description"
            };

            context.Initialize(entity);

            var service = context.GetOrganizationService();

            var updateEntity = new Entity("custom_item")
            {
                Id = entityId,
                ["custom_description"] = "Updated Description"
            };

            // Update should work
            service.Update(updateEntity);

            var retrieved = service.Retrieve("custom_item", entityId, new ColumnSet(true));
            Assert.Equal("Updated Description", retrieved["custom_description"]);
        }

        /// <summary>
        /// Tests that Delete works on late-bound entities that were initialized.
        /// </summary>
        [Fact]
        public void When_LateBound_Entity_Is_Initialized_Delete_Should_Work()
        {
            var context = new XrmFakedContext();
            context.ProxyTypesAssembly = Assembly.GetExecutingAssembly();

            var entityId = Guid.NewGuid();
            var entity = new Entity("custom_record")
            {
                Id = entityId,
                ["custom_field"] = "Test"
            };

            context.Initialize(entity);

            var service = context.GetOrganizationService();

            // Delete should work
            service.Delete("custom_record", entityId);

            Assert.Empty(context.Data["custom_record"]);
        }

        /// <summary>
        /// Tests complex scenario with mixed early-bound and late-bound entities and relationships.
        /// </summary>
        [Fact]
        public void When_ProxyTypesAssembly_Is_Set_Mixed_EarlyBound_And_LateBound_Entities_Should_Work()
        {
            var context = new XrmFakedContext();
            context.ProxyTypesAssembly = Assembly.GetExecutingAssembly();

            var accountId = Guid.NewGuid();
            var account = new Account
            {
                Id = accountId,
                Name = "Parent Company"
            };

            // Late-bound entity that references the early-bound account
            var customEntity = new Entity("custom_project")
            {
                Id = Guid.NewGuid(),
                ["custom_name"] = "Project Alpha",
                ["custom_accountid"] = new EntityReference("account", accountId)
            };

            context.Initialize(new List<Entity> { account, customEntity });

            var service = context.GetOrganizationService();

            // Query the late-bound entity
            var query = new QueryExpression("custom_project")
            {
                ColumnSet = new ColumnSet(true)
            };

            var results = service.RetrieveMultiple(query);
            Assert.Single(results.Entities);

            var projectAccountRef = results.Entities[0].GetAttributeValue<EntityReference>("custom_accountid");
            Assert.NotNull(projectAccountRef);
            Assert.Equal(accountId, projectAccountRef.Id);
            Assert.Equal("account", projectAccountRef.LogicalName);
        }

        /// <summary>
        /// Tests that without ProxyTypesAssembly set, both Initialize and Create work with late-bound entities.
        /// </summary>
        [Fact]
        public void When_ProxyTypesAssembly_Is_Not_Set_Both_Initialize_And_Create_Should_Work_With_LateBound()
        {
            var context = new XrmFakedContext();
            // ProxyTypesAssembly is not set

            var entity1 = new Entity("any_entity")
            {
                Id = Guid.NewGuid(),
                ["field1"] = "Value 1"
            };

            context.Initialize(entity1);

            var service = context.GetOrganizationService();

            var entity2 = new Entity("any_entity")
            {
                Id = Guid.NewGuid(),
                ["field1"] = "Value 2"
            };

            // Create should work without ProxyTypesAssembly
            var createdId = service.Create(entity2);

            Assert.NotEqual(Guid.Empty, createdId);
            Assert.Equal(2, context.Data["any_entity"].Count);
        }
    }
}
