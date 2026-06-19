using DataverseFakes.Extensions;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.ServiceModel;
using Xunit;

namespace DataverseFakes.Tests.FakeContextTests.UpdateOptionValueRequestTests
{
    public class UpdateOptionValueRequestTests
    {
        [Fact]
        public void UpdateGlobalOptionSetLabel_ShouldSucceed()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var optionSet = new OptionSetMetadata { Name = "new_priority" };
            optionSet.Options.Add(new OptionMetadata(new Label("Low", 1033), 1));
            optionSet.Options.Add(new OptionMetadata(new Label("High", 1033), 2));
            context.OptionSetValuesMetadata.Add("new_priority", optionSet);

            var request = new UpdateOptionValueRequest
            {
                OptionSetName = "new_priority",
                Value = 1,
                Label = new Label("Low Priority", 1033)
            };

            var response = service.Execute(request);

            Assert.NotNull(response);
            Assert.IsType<UpdateOptionValueResponse>(response);
            Assert.Equal("Low Priority", context.OptionSetValuesMetadata["new_priority"].Options[0].Label.LocalizedLabels[0].Label);
        }

        [Fact]
        public void UpdateEntityAttributeOptionLabel_ShouldSucceed()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var optionSet = new OptionSetMetadata();
            optionSet.Options.Add(new OptionMetadata(new Label("Active", 1033), 1));
            context.OptionSetValuesMetadata.Add("account#new_status", optionSet);

            var request = new UpdateOptionValueRequest
            {
                EntityLogicalName = "account",
                AttributeLogicalName = "new_status",
                Value = 1,
                Label = new Label("Active Now", 1033)
            };

            service.Execute(request);

            Assert.Equal("Active Now", context.OptionSetValuesMetadata["account#new_status"].Options[0].Label.LocalizedLabels[0].Label);
        }

        [Fact]
        public void UpdateOptionValue_NeitherOptionSetNorEntity_ShouldThrow()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var request = new UpdateOptionValueRequest
            {
                Value = 1,
                Label = new Label("Something", 1033)
            };

            Assert.Throws<FaultException<OrganizationServiceFault>>(() => service.Execute(request));
        }

        [Fact]
        public void UpdateOptionValue_GlobalOptionSetNotFound_ShouldThrow()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var request = new UpdateOptionValueRequest
            {
                OptionSetName = "nonexistent_optionset",
                Value = 1,
                Label = new Label("X", 1033)
            };

            Assert.Throws<FaultException<OrganizationServiceFault>>(() => service.Execute(request));
        }

        [Fact]
        public void UpdateOptionValue_EntityAttrKeyNotFound_ShouldThrow()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var request = new UpdateOptionValueRequest
            {
                EntityLogicalName = "account",
                AttributeLogicalName = "new_status",
                Value = 1,
                Label = new Label("X", 1033)
            };

            Assert.Throws<FaultException<OrganizationServiceFault>>(() => service.Execute(request));
        }

        [Fact]
        public void UpdateOptionValue_OptionValueNotFound_ShouldThrow()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var optionSet = new OptionSetMetadata { Name = "new_type" };
            optionSet.Options.Add(new OptionMetadata(new Label("A", 1033), 10));
            context.OptionSetValuesMetadata.Add("new_type", optionSet);

            var request = new UpdateOptionValueRequest
            {
                OptionSetName = "new_type",
                Value = 99,
                Label = new Label("X", 1033)
            };

            Assert.Throws<FaultException<OrganizationServiceFault>>(() => service.Execute(request));
        }

        [Fact]
        public void UpdateOptionValue_AlsoUpdatesEntityAttributeMetadata_ShouldSucceed()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var optionSet = new OptionSetMetadata();
            optionSet.Options.Add(new OptionMetadata(new Label("Draft", 1033), 1));
            context.OptionSetValuesMetadata.Add("account#new_status", optionSet);

            var picklistAttr = new PicklistAttributeMetadata { LogicalName = "new_status" };
            picklistAttr.OptionSet = new OptionSetMetadata(new OptionMetadataCollection
            {
                new OptionMetadata(new Label("Draft", 1033), 1)
            });
            var entityMetadata = new EntityMetadata { LogicalName = "account" };
            entityMetadata.SetAttributeCollection(new[] { (AttributeMetadata)picklistAttr });
            context.SetEntityMetadata(entityMetadata);

            var request = new UpdateOptionValueRequest
            {
                EntityLogicalName = "account",
                AttributeLogicalName = "new_status",
                Value = 1,
                Label = new Label("Pending", 1033)
            };

            service.Execute(request);

            var updatedMeta = context.GetEntityMetadataByName("account");
            var updatedAttr = updatedMeta.Attributes[0] as PicklistAttributeMetadata;
            Assert.NotNull(updatedAttr);
            Assert.Equal("Pending", updatedAttr.OptionSet.Options[0].Label.LocalizedLabels[0].Label);
        }

        [Fact]
        public void UpdateOptionValue_WithExistingEntityMetadata_OptionNotInAttr_StillUpdatesGlobalKey()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var optionSet = new OptionSetMetadata();
            optionSet.Options.Add(new OptionMetadata(new Label("OldLabel", 1033), 5));
            context.OptionSetValuesMetadata.Add("account#new_category", optionSet);

            // Entity metadata exists but attribute has NO options matching value 5
            var picklistAttr = new PicklistAttributeMetadata { LogicalName = "new_category" };
            picklistAttr.OptionSet = new OptionSetMetadata(new OptionMetadataCollection());
            var entityMetadata = new EntityMetadata { LogicalName = "account" };
            entityMetadata.SetAttributeCollection(new[] { (AttributeMetadata)picklistAttr });
            context.SetEntityMetadata(entityMetadata);

            var request = new UpdateOptionValueRequest
            {
                EntityLogicalName = "account",
                AttributeLogicalName = "new_category",
                Value = 5,
                Label = new Label("NewLabel", 1033)
            };

            service.Execute(request);

            // The global key should be updated
            Assert.Equal("NewLabel", context.OptionSetValuesMetadata["account#new_category"].Options[0].Label.LocalizedLabels[0].Label);
        }
    }
}
