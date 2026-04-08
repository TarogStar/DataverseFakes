using FakeXrmEasy.Extensions;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace FakeXrmEasy.Tests.FakeContextTests.FormattedValuesTests
{
    /// <summary>
    /// Tests for Issue #218: FormattedValues for OptionSet fields not populated
    /// </summary>
    public class OptionSetFormattedValuesTests
    {
        #region OptionSetValue Tests

        [Fact]
        public void FormattedValues_Should_Be_Populated_For_OptionSetValue_With_Metadata()
        {
            // Arrange
            var context = new XrmFakedContext();

            // Create PicklistAttributeMetadata with options
            var picklistAttribute = new PicklistAttributeMetadata()
            {
                LogicalName = "new_status"
            };

            var options = new OptionMetadataCollection
            {
                new OptionMetadata(new Label("Draft", 1033), 1),
                new OptionMetadata(new Label("Pending", 1033), 2),
                new OptionMetadata(new Label("Approved", 1033), 3),
                new OptionMetadata(new Label("Rejected", 1033), 4)
            };
            picklistAttribute.OptionSet = new OptionSetMetadata(options);

            var entityMetadata = new EntityMetadata() { LogicalName = "new_customentity" };
            entityMetadata.SetAttributeCollection(new[] { picklistAttribute });
            context.SetEntityMetadata(entityMetadata);

            var entityId = Guid.NewGuid();
            var entity = new Entity("new_customentity", entityId);
            entity["new_status"] = new OptionSetValue(3); // Approved

            context.Initialize(new List<Entity> { entity });

            var service = context.GetOrganizationService();

            // Act - using RetrieveMultiple
            var query = new QueryExpression("new_customentity");
            query.ColumnSet = new ColumnSet("new_status");
            var results = service.RetrieveMultiple(query);
            var retrievedEntity = results.Entities.FirstOrDefault();

            // Assert
            Assert.NotNull(retrievedEntity);
            Assert.True(retrievedEntity.FormattedValues.ContainsKey("new_status"), "FormattedValues should contain the option set key");
            Assert.Equal("Approved", retrievedEntity.FormattedValues["new_status"]);
        }

        [Fact]
        public void FormattedValues_Should_Fallback_To_Value_When_No_Metadata()
        {
            // Arrange
            var context = new XrmFakedContext();

            var entityId = Guid.NewGuid();
            var entity = new Entity("new_customentity", entityId);
            entity["new_status"] = new OptionSetValue(42);

            context.Initialize(new List<Entity> { entity });

            var service = context.GetOrganizationService();

            // Act
            var query = new QueryExpression("new_customentity");
            query.ColumnSet = new ColumnSet("new_status");
            var results = service.RetrieveMultiple(query);
            var retrievedEntity = results.Entities.FirstOrDefault();

            // Assert
            Assert.NotNull(retrievedEntity);
            Assert.True(retrievedEntity.FormattedValues.ContainsKey("new_status"), "FormattedValues should contain the option set key");
            Assert.Equal("42", retrievedEntity.FormattedValues["new_status"]);
        }

        [Fact]
        public void FormattedValues_Should_Be_Populated_For_OptionSetValue_Using_Retrieve()
        {
            // Arrange
            var context = new XrmFakedContext();

            var picklistAttribute = new PicklistAttributeMetadata()
            {
                LogicalName = "new_priority"
            };

            var options = new OptionMetadataCollection
            {
                new OptionMetadata(new Label("Low", 1033), 100),
                new OptionMetadata(new Label("Medium", 1033), 200),
                new OptionMetadata(new Label("High", 1033), 300),
                new OptionMetadata(new Label("Critical", 1033), 400)
            };
            picklistAttribute.OptionSet = new OptionSetMetadata(options);

            var entityMetadata = new EntityMetadata() { LogicalName = "new_customentity" };
            entityMetadata.SetAttributeCollection(new[] { picklistAttribute });
            context.SetEntityMetadata(entityMetadata);

            var entityId = Guid.NewGuid();
            var entity = new Entity("new_customentity", entityId);
            entity["new_priority"] = new OptionSetValue(300); // High

            context.Initialize(new List<Entity> { entity });

            var service = context.GetOrganizationService();

            // Act - using Retrieve
            var retrievedEntity = service.Retrieve("new_customentity", entityId, new ColumnSet("new_priority"));

            // Assert
            Assert.NotNull(retrievedEntity);
            Assert.True(retrievedEntity.FormattedValues.ContainsKey("new_priority"), "FormattedValues should contain the option set key");
            Assert.Equal("High", retrievedEntity.FormattedValues["new_priority"]);
        }

        #endregion

        #region StateCode Tests

        [Fact]
        public void FormattedValues_Should_Be_Populated_For_StateCode_With_Metadata()
        {
            // Arrange
            var context = new XrmFakedContext();

            var stateAttribute = new StateAttributeMetadata()
            {
                LogicalName = "statecode"
            };

            var options = new OptionMetadataCollection
            {
                new StateOptionMetadata { Value = 0, Label = new Label("Active", 1033) },
                new StateOptionMetadata { Value = 1, Label = new Label("Inactive", 1033) }
            };
            stateAttribute.SetSealedPropertyValue("OptionSet", new OptionSetMetadata(options));

            var entityMetadata = new EntityMetadata() { LogicalName = "account" };
            entityMetadata.SetAttributeCollection(new[] { stateAttribute });
            context.SetEntityMetadata(entityMetadata);

            var entityId = Guid.NewGuid();
            var entity = new Entity("account", entityId);
            entity["statecode"] = new OptionSetValue(0); // Active

            context.Initialize(new List<Entity> { entity });

            var service = context.GetOrganizationService();

            // Act
            var query = new QueryExpression("account");
            query.ColumnSet = new ColumnSet("statecode");
            var results = service.RetrieveMultiple(query);
            var retrievedEntity = results.Entities.FirstOrDefault();

            // Assert
            Assert.NotNull(retrievedEntity);
            Assert.True(retrievedEntity.FormattedValues.ContainsKey("statecode"), "FormattedValues should contain statecode key");
            Assert.Equal("Active", retrievedEntity.FormattedValues["statecode"]);
        }

        #endregion

        #region StatusCode Tests

        [Fact]
        public void FormattedValues_Should_Be_Populated_For_StatusCode_With_Metadata()
        {
            // Arrange
            var context = new XrmFakedContext();

            var statusAttribute = new StatusAttributeMetadata()
            {
                LogicalName = "statuscode"
            };

            var options = new OptionMetadataCollection
            {
                new StatusOptionMetadata { Value = 1, Label = new Label("New", 1033) },
                new StatusOptionMetadata { Value = 2, Label = new Label("In Progress", 1033) },
                new StatusOptionMetadata { Value = 3, Label = new Label("Resolved", 1033) },
                new StatusOptionMetadata { Value = 4, Label = new Label("Closed", 1033) }
            };
            statusAttribute.SetSealedPropertyValue("OptionSet", new OptionSetMetadata(options));

            var entityMetadata = new EntityMetadata() { LogicalName = "incident" };
            entityMetadata.SetAttributeCollection(new[] { statusAttribute });
            context.SetEntityMetadata(entityMetadata);

            var entityId = Guid.NewGuid();
            var entity = new Entity("incident", entityId);
            entity["statuscode"] = new OptionSetValue(2); // In Progress

            context.Initialize(new List<Entity> { entity });

            var service = context.GetOrganizationService();

            // Act
            var query = new QueryExpression("incident");
            query.ColumnSet = new ColumnSet("statuscode");
            var results = service.RetrieveMultiple(query);
            var retrievedEntity = results.Entities.FirstOrDefault();

            // Assert
            Assert.NotNull(retrievedEntity);
            Assert.True(retrievedEntity.FormattedValues.ContainsKey("statuscode"), "FormattedValues should contain statuscode key");
            Assert.Equal("In Progress", retrievedEntity.FormattedValues["statuscode"]);
        }

        #endregion

        #region Boolean Tests

        [Fact]
        public void FormattedValues_Should_Be_Populated_For_Boolean_Without_Metadata()
        {
            // Arrange
            var context = new XrmFakedContext();

            var entityId = Guid.NewGuid();
            var entity = new Entity("contact", entityId);
            entity["donotemail"] = true;

            context.Initialize(new List<Entity> { entity });

            var service = context.GetOrganizationService();

            // Act
            var query = new QueryExpression("contact");
            query.ColumnSet = new ColumnSet("donotemail");
            var results = service.RetrieveMultiple(query);
            var retrievedEntity = results.Entities.FirstOrDefault();

            // Assert
            Assert.NotNull(retrievedEntity);
            Assert.True(retrievedEntity.FormattedValues.ContainsKey("donotemail"), "FormattedValues should contain the boolean key");
            Assert.Equal("Yes", retrievedEntity.FormattedValues["donotemail"]);
        }

        [Fact]
        public void FormattedValues_Should_Return_No_For_False_Boolean()
        {
            // Arrange
            var context = new XrmFakedContext();

            var entityId = Guid.NewGuid();
            var entity = new Entity("contact", entityId);
            entity["donotemail"] = false;

            context.Initialize(new List<Entity> { entity });

            var service = context.GetOrganizationService();

            // Act
            var query = new QueryExpression("contact");
            query.ColumnSet = new ColumnSet("donotemail");
            var results = service.RetrieveMultiple(query);
            var retrievedEntity = results.Entities.FirstOrDefault();

            // Assert
            Assert.NotNull(retrievedEntity);
            Assert.True(retrievedEntity.FormattedValues.ContainsKey("donotemail"), "FormattedValues should contain the boolean key");
            Assert.Equal("No", retrievedEntity.FormattedValues["donotemail"]);
        }

        #endregion

        #region Multiple Attributes Tests

        [Fact]
        public void FormattedValues_Should_Be_Populated_For_Multiple_OptionSetValues()
        {
            // Arrange
            var context = new XrmFakedContext();

            var priorityAttribute = new PicklistAttributeMetadata() { LogicalName = "new_priority" };
            priorityAttribute.OptionSet = new OptionSetMetadata(new OptionMetadataCollection
            {
                new OptionMetadata(new Label("Low", 1033), 1),
                new OptionMetadata(new Label("Medium", 1033), 2),
                new OptionMetadata(new Label("High", 1033), 3)
            });

            var categoryAttribute = new PicklistAttributeMetadata() { LogicalName = "new_category" };
            categoryAttribute.OptionSet = new OptionSetMetadata(new OptionMetadataCollection
            {
                new OptionMetadata(new Label("Sales", 1033), 100),
                new OptionMetadata(new Label("Support", 1033), 200),
                new OptionMetadata(new Label("Marketing", 1033), 300)
            });

            var entityMetadata = new EntityMetadata() { LogicalName = "opportunity" };
            entityMetadata.SetAttributeCollection(new[] { priorityAttribute, categoryAttribute });
            context.SetEntityMetadata(entityMetadata);

            var entityId = Guid.NewGuid();
            var entity = new Entity("opportunity", entityId);
            entity["new_priority"] = new OptionSetValue(3); // High
            entity["new_category"] = new OptionSetValue(200); // Support

            context.Initialize(new List<Entity> { entity });

            var service = context.GetOrganizationService();

            // Act
            var query = new QueryExpression("opportunity");
            query.ColumnSet = new ColumnSet("new_priority", "new_category");
            var results = service.RetrieveMultiple(query);
            var retrievedEntity = results.Entities.FirstOrDefault();

            // Assert
            Assert.NotNull(retrievedEntity);
            Assert.True(retrievedEntity.FormattedValues.ContainsKey("new_priority"));
            Assert.True(retrievedEntity.FormattedValues.ContainsKey("new_category"));
            Assert.Equal("High", retrievedEntity.FormattedValues["new_priority"]);
            Assert.Equal("Support", retrievedEntity.FormattedValues["new_category"]);
        }

        #endregion

        #region Pre-existing FormattedValues Tests

        [Fact]
        public void FormattedValues_Should_Not_Override_PreInjected_Values()
        {
            // Arrange
            var context = new XrmFakedContext();

            var picklistAttribute = new PicklistAttributeMetadata() { LogicalName = "new_status" };
            picklistAttribute.OptionSet = new OptionSetMetadata(new OptionMetadataCollection
            {
                new OptionMetadata(new Label("Draft", 1033), 1),
                new OptionMetadata(new Label("Pending", 1033), 2)
            });

            var entityMetadata = new EntityMetadata() { LogicalName = "new_customentity" };
            entityMetadata.SetAttributeCollection(new[] { picklistAttribute });
            context.SetEntityMetadata(entityMetadata);

            var entityId = Guid.NewGuid();
            var entity = new Entity("new_customentity", entityId);
            entity["new_status"] = new OptionSetValue(1);

            // Pre-inject a custom formatted value
            var formattedValues = new FormattedValueCollection();
            formattedValues.Add("new_status", "Custom Label");
            entity.Inject("FormattedValues", formattedValues);

            context.Initialize(new List<Entity> { entity });

            var service = context.GetOrganizationService();

            // Act
            var query = new QueryExpression("new_customentity");
            query.ColumnSet = new ColumnSet("new_status");
            var results = service.RetrieveMultiple(query);
            var retrievedEntity = results.Entities.FirstOrDefault();

            // Assert - should keep the pre-injected value
            Assert.NotNull(retrievedEntity);
            Assert.True(retrievedEntity.FormattedValues.ContainsKey("new_status"));
            Assert.Equal("Custom Label", retrievedEntity.FormattedValues["new_status"]);
        }

        #endregion

        #region Null Value Tests

        [Fact]
        public void FormattedValues_Should_Not_Contain_Key_For_Null_OptionSetValue()
        {
            // Arrange
            var context = new XrmFakedContext();

            var picklistAttribute = new PicklistAttributeMetadata() { LogicalName = "new_status" };
            picklistAttribute.OptionSet = new OptionSetMetadata(new OptionMetadataCollection
            {
                new OptionMetadata(new Label("Draft", 1033), 1),
                new OptionMetadata(new Label("Pending", 1033), 2)
            });

            var entityMetadata = new EntityMetadata() { LogicalName = "new_customentity" };
            entityMetadata.SetAttributeCollection(new[] { picklistAttribute });
            context.SetEntityMetadata(entityMetadata);

            var entityId = Guid.NewGuid();
            var entity = new Entity("new_customentity", entityId);
            entity["new_status"] = null;

            context.Initialize(new List<Entity> { entity });

            var service = context.GetOrganizationService();

            // Act
            var query = new QueryExpression("new_customentity");
            query.ColumnSet = new ColumnSet("new_status");
            var results = service.RetrieveMultiple(query);
            var retrievedEntity = results.Entities.FirstOrDefault();

            // Assert - null values should not have formatted values
            Assert.NotNull(retrievedEntity);
            Assert.False(retrievedEntity.FormattedValues.ContainsKey("new_status"), "Null OptionSetValue should not have a FormattedValue");
        }

        #endregion
    }
}
