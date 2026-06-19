using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using System;
using Xunit;

namespace DataverseFakes.Tests.FakeContextTests.RetrieveAllOptionSetsRequestTests
{
    public class RetrieveAllOptionSetsRequestTests
    {
        [Fact]
        public void RetrieveAllOptionSets_EmptyContext_ReturnsEmpty()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var response = (RetrieveAllOptionSetsResponse)service.Execute(new RetrieveAllOptionSetsRequest());

            Assert.NotNull(response);
            var optionSets = (OptionSetMetadataBase[])response.Results["OptionSetMetadata"];
            Assert.Empty(optionSets);
        }

        [Fact]
        public void RetrieveAllOptionSets_WithOneOptionSet_ReturnsOne()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            context.OptionSetValuesMetadata.Add("new_status", new OptionSetMetadata { Name = "new_status" });

            var response = (RetrieveAllOptionSetsResponse)service.Execute(new RetrieveAllOptionSetsRequest());

            var optionSets = (OptionSetMetadataBase[])response.Results["OptionSetMetadata"];
            Assert.Single(optionSets);
        }

        [Fact]
        public void RetrieveAllOptionSets_WithMultipleOptionSets_ReturnsAll()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            context.OptionSetValuesMetadata.Add("new_a", new OptionSetMetadata { Name = "new_a" });
            context.OptionSetValuesMetadata.Add("new_b", new OptionSetMetadata { Name = "new_b" });
            context.OptionSetValuesMetadata.Add("new_c", new OptionSetMetadata { Name = "new_c" });

            var response = (RetrieveAllOptionSetsResponse)service.Execute(new RetrieveAllOptionSetsRequest());

            var optionSets = (OptionSetMetadataBase[])response.Results["OptionSetMetadata"];
            Assert.Equal(3, optionSets.Length);
        }

        [Fact]
        public void RetrieveAllOptionSets_ResponseHasCorrectType()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var response = service.Execute(new RetrieveAllOptionSetsRequest());

            Assert.IsType<RetrieveAllOptionSetsResponse>(response);
        }

        [Fact]
        public void RetrieveAllOptionSets_OptionsArePreserved()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var optionSet = new OptionSetMetadata { Name = "new_priority" };
            optionSet.Options.Add(new OptionMetadata(new Label("Low", 1033), 1));
            optionSet.Options.Add(new OptionMetadata(new Label("High", 1033), 3));
            context.OptionSetValuesMetadata.Add("new_priority", optionSet);

            var response = (RetrieveAllOptionSetsResponse)service.Execute(new RetrieveAllOptionSetsRequest());

            var optionSets = (OptionSetMetadataBase[])response.Results["OptionSetMetadata"];
            Assert.Single(optionSets);
            var returned = (OptionSetMetadata)optionSets[0];
            Assert.Equal(2, returned.Options.Count);
            Assert.Equal("Low", returned.Options[0].Label.LocalizedLabels[0].Label);
            Assert.Equal("High", returned.Options[1].Label.LocalizedLabels[0].Label);
        }
    }
}
