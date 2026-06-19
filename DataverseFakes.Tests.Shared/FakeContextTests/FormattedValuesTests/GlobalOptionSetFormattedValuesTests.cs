using DataverseFakes.Extensions;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace DataverseFakes.Tests.FakeContextTests.FormattedValuesTests
{
    /// <summary>
    /// Tests for Issue #218: FormattedValues fallback to global OptionSetValuesMetadata
    /// when entity-attribute metadata is absent.
    /// </summary>
    public class GlobalOptionSetFormattedValuesTests
    {
        [Fact]
        public void FormattedValues_Via_RetrieveMultiple_Should_Use_GlobalOptionSetFallback_When_NoEntityMetadata()
        {
            var context = new XrmFakedContext();

            // Register global option set using entity#attribute key pattern — no entity metadata
            var optionSet = new OptionSetMetadata();
            optionSet.Options.Add(new OptionMetadata(new Label("Active", 1033), 1));
            optionSet.Options.Add(new OptionMetadata(new Label("Inactive", 1033), 2));
            context.OptionSetValuesMetadata.Add("account#new_status", optionSet);

            var entityId = Guid.NewGuid();
            var entity = new Entity("account", entityId);
            entity["new_status"] = new OptionSetValue(1);
            context.Initialize(new List<Entity> { entity });

            var service = context.GetOrganizationService();

            var query = new QueryExpression("account") { ColumnSet = new ColumnSet("new_status") };
            var results = service.RetrieveMultiple(query);
            var retrieved = results.Entities.FirstOrDefault();

            Assert.NotNull(retrieved);
            Assert.True(retrieved.FormattedValues.ContainsKey("new_status"),
                "FormattedValues should contain new_status key using global fallback");
            Assert.Equal("Active", retrieved.FormattedValues["new_status"]);
        }

        [Fact]
        public void FormattedValues_Via_Retrieve_Should_Use_GlobalOptionSetFallback_When_NoEntityMetadata()
        {
            var context = new XrmFakedContext();

            var optionSet = new OptionSetMetadata();
            optionSet.Options.Add(new OptionMetadata(new Label("High", 1033), 100));
            optionSet.Options.Add(new OptionMetadata(new Label("Low", 1033), 200));
            context.OptionSetValuesMetadata.Add("contact#new_priority", optionSet);

            var entityId = Guid.NewGuid();
            var entity = new Entity("contact", entityId);
            entity["new_priority"] = new OptionSetValue(100);
            context.Initialize(new List<Entity> { entity });

            var service = context.GetOrganizationService();

            var retrieved = service.Retrieve("contact", entityId, new ColumnSet("new_priority"));

            Assert.NotNull(retrieved);
            Assert.True(retrieved.FormattedValues.ContainsKey("new_priority"),
                "FormattedValues should contain new_priority key using global fallback via Retrieve");
            Assert.Equal("High", retrieved.FormattedValues["new_priority"]);
        }

        [Fact]
        public void FormattedValues_EntityMetadataStillWins_Over_GlobalFallback()
        {
            var context = new XrmFakedContext();

            // Both entity metadata and global optionset key exist — entity metadata should win
            var globalOptionSet = new OptionSetMetadata();
            globalOptionSet.Options.Add(new OptionMetadata(new Label("GlobalLabel", 1033), 1));
            context.OptionSetValuesMetadata.Add("account#new_type", globalOptionSet);

            var picklistAttr = new PicklistAttributeMetadata { LogicalName = "new_type" };
            picklistAttr.OptionSet = new OptionSetMetadata(new OptionMetadataCollection
            {
                new OptionMetadata(new Label("EntityLabel", 1033), 1)
            });
            var entityMetadata = new EntityMetadata { LogicalName = "account" };
            entityMetadata.SetAttributeCollection(new[] { (AttributeMetadata)picklistAttr });
            context.SetEntityMetadata(entityMetadata);

            var entityId = Guid.NewGuid();
            var entity = new Entity("account", entityId);
            entity["new_type"] = new OptionSetValue(1);
            context.Initialize(new List<Entity> { entity });

            var service = context.GetOrganizationService();

            var query = new QueryExpression("account") { ColumnSet = new ColumnSet("new_type") };
            var results = service.RetrieveMultiple(query);
            var retrieved = results.Entities.FirstOrDefault();

            Assert.NotNull(retrieved);
            Assert.True(retrieved.FormattedValues.ContainsKey("new_type"));
            // Entity attribute metadata wins
            Assert.Equal("EntityLabel", retrieved.FormattedValues["new_type"]);
        }

        [Fact]
        public void FormattedValues_GlobalFallback_Falls_Through_To_Numeric_When_No_Key_Matches()
        {
            var context = new XrmFakedContext();

            // No entity metadata, no matching optionset key — should return numeric string
            var entityId = Guid.NewGuid();
            var entity = new Entity("account", entityId);
            entity["new_unmapped"] = new OptionSetValue(42);
            context.Initialize(new List<Entity> { entity });

            var service = context.GetOrganizationService();

            var query = new QueryExpression("account") { ColumnSet = new ColumnSet("new_unmapped") };
            var results = service.RetrieveMultiple(query);
            var retrieved = results.Entities.FirstOrDefault();

            Assert.NotNull(retrieved);
            Assert.True(retrieved.FormattedValues.ContainsKey("new_unmapped"));
            Assert.Equal("42", retrieved.FormattedValues["new_unmapped"]);
        }
    }
}
