using Xunit;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using FakeXrmEasy.Extensions;
using System;
using System.Linq;

namespace FakeXrmEasy.Tests.FakeContextTests.RetrieveAttributeRequestTests
{
    public class RetrieveAttributeTests
    {
        [Fact]
        public static void When_retrieve_attribute_request_is_called_correctly_attribute_is_returned()
        {
            var ctx = new XrmFakedContext();
            var service = ctx.GetOrganizationService();

            var entityMetadata = new EntityMetadata()
            {
                LogicalName = "account"
            };
            var nameAttribute = new StringAttributeMetadata()
            {
                LogicalName = "name",
                RequiredLevel = new AttributeRequiredLevelManagedProperty(AttributeRequiredLevel.ApplicationRequired)
            };
            entityMetadata.SetAttributeCollection(new[] { nameAttribute });

            ctx.InitializeMetadata(entityMetadata);

            RetrieveAttributeRequest req = new RetrieveAttributeRequest()
            {
                EntityLogicalName = "account",
                LogicalName = "name"
            };

            var response = service.Execute(req) as RetrieveAttributeResponse;
            Assert.NotNull(response.AttributeMetadata);
            Assert.Equal(AttributeRequiredLevel.ApplicationRequired, response.AttributeMetadata.RequiredLevel.Value);
            Assert.Equal("name", response.AttributeMetadata.LogicalName);
        }

        [Fact]
        public static void When_retrieve_attribute_request_is_without_entity_logical_name_exception_is_raised()
        {
            var ctx = new XrmFakedContext();
            var service = ctx.GetOrganizationService();

            RetrieveAttributeRequest req = new RetrieveAttributeRequest()
            {
                EntityLogicalName = null,
                LogicalName = "name"
            };

            Assert.Throws<Exception>(() => service.Execute(req));
        }

        [Fact]
        public static void When_retrieve_attribute_request_is_without_logical_name_exception_is_raised()
        {
            var ctx = new XrmFakedContext();
            var service = ctx.GetOrganizationService();

            var entityMetadata = new EntityMetadata()
            {
                LogicalName = "account"
            };
            ctx.InitializeMetadata(entityMetadata);

            RetrieveAttributeRequest req = new RetrieveAttributeRequest()
            {
                EntityLogicalName = "account",
                LogicalName = null
            };

            Assert.Throws<Exception>(() => service.Execute(req));
        }

        [Fact]
        public static void When_retrieve_attribute_request_is_without_being_initialised_exception_is_raised()
        {
            var ctx = new XrmFakedContext();
            var service = ctx.GetOrganizationService();

            RetrieveAttributeRequest req = new RetrieveAttributeRequest()
            {
                EntityLogicalName = "account",
                LogicalName = "name"
            };

            Assert.Throws<Exception>(() => service.Execute(req));
        }

        [Fact]
        public static void When_retrieve_attribute_request_is_initialised_but_attribute_doesnt_exists_exception_is_raised()
        {
            var ctx = new XrmFakedContext();
            var service = ctx.GetOrganizationService();

            var entityMetadata = new EntityMetadata()
            {
                LogicalName = "account"
            };
            ctx.InitializeMetadata(entityMetadata);

            RetrieveAttributeRequest req = new RetrieveAttributeRequest()
            {
                EntityLogicalName = "account",
                LogicalName = "name"
            };

            Assert.Throws<Exception>(() => service.Execute(req));
        }

        [Fact]
        public static void When_retrieve_picklist_attribute_with_optionset_initialized_directly_options_are_returned()
        {
            var ctx = new XrmFakedContext();
            var service = ctx.GetOrganizationService();

            // Create PicklistAttributeMetadata with options initialized directly
            var picklistAttribute = new PicklistAttributeMetadata()
            {
                LogicalName = "prioritycode"
            };

            var options = new OptionMetadataCollection
            {
                new OptionMetadata(new Label("Low", 1033), 1),
                new OptionMetadata(new Label("Normal", 1033), 2),
                new OptionMetadata(new Label("High", 1033), 3)
            };
            picklistAttribute.OptionSet = new OptionSetMetadata(options);

            var entityMetadata = new EntityMetadata()
            {
                LogicalName = "incident"
            };
            entityMetadata.SetAttributeCollection(new[] { picklistAttribute });
            ctx.InitializeMetadata(entityMetadata);

            RetrieveAttributeRequest req = new RetrieveAttributeRequest()
            {
                EntityLogicalName = "incident",
                LogicalName = "prioritycode"
            };

            var response = service.Execute(req) as RetrieveAttributeResponse;

            Assert.NotNull(response.AttributeMetadata);
            Assert.IsType<PicklistAttributeMetadata>(response.AttributeMetadata);

            var retrievedPicklist = (PicklistAttributeMetadata)response.AttributeMetadata;
            Assert.NotNull(retrievedPicklist.OptionSet);
            Assert.NotNull(retrievedPicklist.OptionSet.Options);
            Assert.Equal(3, retrievedPicklist.OptionSet.Options.Count);
            Assert.Contains(retrievedPicklist.OptionSet.Options, o => o.Value == 1);
            Assert.Contains(retrievedPicklist.OptionSet.Options, o => o.Value == 2);
            Assert.Contains(retrievedPicklist.OptionSet.Options, o => o.Value == 3);
        }

        [Fact]
        public static void When_retrieve_picklist_attribute_options_populated_via_insert_option_request_options_are_returned()
        {
            var ctx = new XrmFakedContext();
            var service = ctx.GetOrganizationService();

            // Create PicklistAttributeMetadata without options
            var picklistAttribute = new PicklistAttributeMetadata()
            {
                LogicalName = "new_customfield"
            };

            var entityMetadata = new EntityMetadata()
            {
                LogicalName = "account"
            };
            entityMetadata.SetAttributeCollection(new[] { picklistAttribute });
            ctx.InitializeMetadata(entityMetadata);

            // Add options via InsertOptionValueRequest
            service.Execute(new InsertOptionValueRequest
            {
                EntityLogicalName = "account",
                AttributeLogicalName = "new_customfield",
                Label = new Label("Option A", 1033),
                Value = 100
            });

            service.Execute(new InsertOptionValueRequest
            {
                EntityLogicalName = "account",
                AttributeLogicalName = "new_customfield",
                Label = new Label("Option B", 1033),
                Value = 200
            });

            RetrieveAttributeRequest req = new RetrieveAttributeRequest()
            {
                EntityLogicalName = "account",
                LogicalName = "new_customfield"
            };

            var response = service.Execute(req) as RetrieveAttributeResponse;

            Assert.NotNull(response.AttributeMetadata);
            Assert.IsType<PicklistAttributeMetadata>(response.AttributeMetadata);

            var retrievedPicklist = (PicklistAttributeMetadata)response.AttributeMetadata;
            Assert.NotNull(retrievedPicklist.OptionSet);
            Assert.NotNull(retrievedPicklist.OptionSet.Options);
            Assert.Equal(2, retrievedPicklist.OptionSet.Options.Count);
            Assert.Contains(retrievedPicklist.OptionSet.Options, o => o.Value == 100);
            Assert.Contains(retrievedPicklist.OptionSet.Options, o => o.Value == 200);
        }

        [Fact]
        public static void When_retrieve_state_attribute_with_optionset_initialized_directly_options_are_returned()
        {
            var ctx = new XrmFakedContext();
            var service = ctx.GetOrganizationService();

            // Create StateAttributeMetadata with options initialized directly
            var stateAttribute = new StateAttributeMetadata()
            {
                LogicalName = "statecode"
            };

            var options = new OptionMetadataCollection
            {
                new OptionMetadata(new Label("Active", 1033), 0),
                new OptionMetadata(new Label("Inactive", 1033), 1)
            };
            stateAttribute.OptionSet = new OptionSetMetadata(options);

            var entityMetadata = new EntityMetadata()
            {
                LogicalName = "account"
            };
            entityMetadata.SetAttributeCollection(new[] { stateAttribute });
            ctx.InitializeMetadata(entityMetadata);

            RetrieveAttributeRequest req = new RetrieveAttributeRequest()
            {
                EntityLogicalName = "account",
                LogicalName = "statecode"
            };

            var response = service.Execute(req) as RetrieveAttributeResponse;

            Assert.NotNull(response.AttributeMetadata);
            Assert.IsType<StateAttributeMetadata>(response.AttributeMetadata);

            var retrievedState = (StateAttributeMetadata)response.AttributeMetadata;
            Assert.NotNull(retrievedState.OptionSet);
            Assert.NotNull(retrievedState.OptionSet.Options);
            Assert.Equal(2, retrievedState.OptionSet.Options.Count);
            Assert.Contains(retrievedState.OptionSet.Options, o => o.Value == 0);
            Assert.Contains(retrievedState.OptionSet.Options, o => o.Value == 1);
        }

        [Fact]
        public static void When_retrieve_status_attribute_with_optionset_initialized_directly_options_are_returned()
        {
            var ctx = new XrmFakedContext();
            var service = ctx.GetOrganizationService();

            // Create StatusAttributeMetadata with options initialized directly
            var statusAttribute = new StatusAttributeMetadata()
            {
                LogicalName = "statuscode"
            };

            var options = new OptionMetadataCollection
            {
                new OptionMetadata(new Label("Active", 1033), 1),
                new OptionMetadata(new Label("Inactive", 1033), 2)
            };
            statusAttribute.OptionSet = new OptionSetMetadata(options);

            var entityMetadata = new EntityMetadata()
            {
                LogicalName = "contact"
            };
            entityMetadata.SetAttributeCollection(new[] { statusAttribute });
            ctx.InitializeMetadata(entityMetadata);

            RetrieveAttributeRequest req = new RetrieveAttributeRequest()
            {
                EntityLogicalName = "contact",
                LogicalName = "statuscode"
            };

            var response = service.Execute(req) as RetrieveAttributeResponse;

            Assert.NotNull(response.AttributeMetadata);
            Assert.IsType<StatusAttributeMetadata>(response.AttributeMetadata);

            var retrievedStatus = (StatusAttributeMetadata)response.AttributeMetadata;
            Assert.NotNull(retrievedStatus.OptionSet);
            Assert.NotNull(retrievedStatus.OptionSet.Options);
            Assert.Equal(2, retrievedStatus.OptionSet.Options.Count);
            Assert.Contains(retrievedStatus.OptionSet.Options, o => o.Value == 1);
            Assert.Contains(retrievedStatus.OptionSet.Options, o => o.Value == 2);
        }

        [Fact]
        public static void When_retrieve_picklist_attribute_options_have_correct_labels()
        {
            var ctx = new XrmFakedContext();
            var service = ctx.GetOrganizationService();

            // Create PicklistAttributeMetadata with labeled options
            var picklistAttribute = new PicklistAttributeMetadata()
            {
                LogicalName = "categorycode"
            };

            var options = new OptionMetadataCollection
            {
                new OptionMetadata(new Label("Category One", 1033), 1),
                new OptionMetadata(new Label("Category Two", 1033), 2)
            };
            picklistAttribute.OptionSet = new OptionSetMetadata(options);

            var entityMetadata = new EntityMetadata()
            {
                LogicalName = "opportunity"
            };
            entityMetadata.SetAttributeCollection(new[] { picklistAttribute });
            ctx.InitializeMetadata(entityMetadata);

            RetrieveAttributeRequest req = new RetrieveAttributeRequest()
            {
                EntityLogicalName = "opportunity",
                LogicalName = "categorycode"
            };

            var response = service.Execute(req) as RetrieveAttributeResponse;
            var retrievedPicklist = (PicklistAttributeMetadata)response.AttributeMetadata;

            var optionOne = retrievedPicklist.OptionSet.Options.First(o => o.Value == 1);
            var optionTwo = retrievedPicklist.OptionSet.Options.First(o => o.Value == 2);

            Assert.Equal("Category One", optionOne.Label.LocalizedLabels[0].Label);
            Assert.Equal("Category Two", optionTwo.Label.LocalizedLabels[0].Label);
        }

        [Fact]
        public static void When_retrieve_picklist_with_global_optionset_options_are_populated_from_metadata()
        {
            var ctx = new XrmFakedContext();
            var service = ctx.GetOrganizationService();

            var globalOptionSetName = "my_globaloptionset";

            // Add global option set to OptionSetValuesMetadata
            var globalOptions = new OptionSetMetadata();
            globalOptions.Options.Add(new OptionMetadata(new Label("Global Option 1", 1033), 10));
            globalOptions.Options.Add(new OptionMetadata(new Label("Global Option 2", 1033), 20));
            ctx.OptionSetValuesMetadata.Add(globalOptionSetName, globalOptions);

      // Create PicklistAttributeMetadata referencing the global option set
      var picklistAttribute = new PicklistAttributeMetadata
      {
        LogicalName = "new_globalpicklist",
        OptionSet = new OptionSetMetadata() { Name = globalOptionSetName }
      };

      var entityMetadata = new EntityMetadata()
            {
                LogicalName = "account"
            };
            entityMetadata.SetAttributeCollection(new[] { picklistAttribute });
            ctx.InitializeMetadata(entityMetadata);

            RetrieveAttributeRequest req = new RetrieveAttributeRequest()
            {
                EntityLogicalName = "account",
                LogicalName = "new_globalpicklist"
            };

            var response = service.Execute(req) as RetrieveAttributeResponse;

            Assert.NotNull(response.AttributeMetadata);
            Assert.IsType<PicklistAttributeMetadata>(response.AttributeMetadata);

            var retrievedPicklist = (PicklistAttributeMetadata)response.AttributeMetadata;
            Assert.NotNull(retrievedPicklist.OptionSet);
            Assert.NotNull(retrievedPicklist.OptionSet.Options);
            Assert.Equal(2, retrievedPicklist.OptionSet.Options.Count);
            Assert.Equal(globalOptionSetName, retrievedPicklist.OptionSet.Name);
            Assert.Contains(retrievedPicklist.OptionSet.Options, o => o.Value == 10);
            Assert.Contains(retrievedPicklist.OptionSet.Options, o => o.Value == 20);
        }

    }
}
