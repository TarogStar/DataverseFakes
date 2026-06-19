using DataverseFakes.Extensions;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.ServiceModel;
using Xunit;

namespace DataverseFakes.Tests.FakeContextTests.UpdateStateValueRequestTests
{
    public class UpdateStateValueRequestTests
    {
        [Fact]
        public void UpdateStateValue_ViaOptionSetValuesMetadata_ShouldSucceed()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var optionSet = new OptionSetMetadata();
            optionSet.Options.Add(new OptionMetadata(new Label("Active", 1033), 0));
            optionSet.Options.Add(new OptionMetadata(new Label("Inactive", 1033), 1));
            context.OptionSetValuesMetadata.Add("account#statecode", optionSet);

            var request = new UpdateStateValueRequest
            {
                EntityLogicalName = "account",
                AttributeLogicalName = "statecode",
                Value = 0,
                Label = new Label("Active State", 1033)
            };

            service.Execute(request);

            Assert.Equal("Active State", context.OptionSetValuesMetadata["account#statecode"].Options[0].Label.LocalizedLabels[0].Label);
        }

        [Fact]
        public void UpdateStateValue_AlsoUpdatesEntityAttributeMetadata_ShouldSucceed()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var optionSet = new OptionSetMetadata();
            optionSet.Options.Add(new OptionMetadata(new Label("Active", 1033), 0));
            context.OptionSetValuesMetadata.Add("account#statecode", optionSet);

            var stateAttr = new StateAttributeMetadata { LogicalName = "statecode" };
            stateAttr.SetSealedPropertyValue("OptionSet", new OptionSetMetadata(new OptionMetadataCollection
            {
                new StateOptionMetadata { Value = 0, Label = new Label("Active", 1033) }
            }));
            var entityMetadata = new EntityMetadata { LogicalName = "account" };
            entityMetadata.SetAttributeCollection(new[] { (AttributeMetadata)stateAttr });
            context.SetEntityMetadata(entityMetadata);

            var request = new UpdateStateValueRequest
            {
                EntityLogicalName = "account",
                AttributeLogicalName = "statecode",
                Value = 0,
                Label = new Label("Open", 1033)
            };

            service.Execute(request);

            var updatedMeta = context.GetEntityMetadataByName("account");
            var updatedAttr = updatedMeta.Attributes[0] as StateAttributeMetadata;
            Assert.NotNull(updatedAttr);
            Assert.Equal("Open", updatedAttr.OptionSet.Options[0].Label.LocalizedLabels[0].Label);
        }

        [Fact]
        public void UpdateStateValue_KeyNotFound_ShouldThrow()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var request = new UpdateStateValueRequest
            {
                EntityLogicalName = "account",
                AttributeLogicalName = "statecode",
                Value = 0,
                Label = new Label("Active", 1033)
            };

            Assert.Throws<FaultException<OrganizationServiceFault>>(() => service.Execute(request));
        }

        [Fact]
        public void UpdateStateValue_ValueNotFound_ShouldThrow()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var optionSet = new OptionSetMetadata();
            optionSet.Options.Add(new OptionMetadata(new Label("Active", 1033), 0));
            context.OptionSetValuesMetadata.Add("account#statecode", optionSet);

            var request = new UpdateStateValueRequest
            {
                EntityLogicalName = "account",
                AttributeLogicalName = "statecode",
                Value = 99,
                Label = new Label("Unknown", 1033)
            };

            Assert.Throws<FaultException<OrganizationServiceFault>>(() => service.Execute(request));
        }

        [Fact]
        public void UpdateStateValue_NullEntityLogicalName_ShouldThrow()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var request = new UpdateStateValueRequest
            {
                EntityLogicalName = null,
                AttributeLogicalName = "statecode",
                Value = 0,
                Label = new Label("Active", 1033)
            };

            Assert.Throws<FaultException<OrganizationServiceFault>>(() => service.Execute(request));
        }

        [Fact]
        public void UpdateStateValue_NullAttributeLogicalName_ShouldThrow()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var request = new UpdateStateValueRequest
            {
                EntityLogicalName = "account",
                AttributeLogicalName = null,
                Value = 0,
                Label = new Label("Active", 1033)
            };

            Assert.Throws<FaultException<OrganizationServiceFault>>(() => service.Execute(request));
        }

        [Fact]
        public void UpdateStateValue_Response_IsCorrectType()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var optionSet = new OptionSetMetadata();
            optionSet.Options.Add(new OptionMetadata(new Label("Active", 1033), 0));
            context.OptionSetValuesMetadata.Add("contact#statecode", optionSet);

            var request = new UpdateStateValueRequest
            {
                EntityLogicalName = "contact",
                AttributeLogicalName = "statecode",
                Value = 0,
                Label = new Label("Open", 1033)
            };

            var response = service.Execute(request);
            Assert.IsType<UpdateStateValueResponse>(response);
        }
    }
}
