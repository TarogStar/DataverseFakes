using FakeXrmEasy.FakeMessageExecutors;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Metadata.Query;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Linq;
using Xunit;

namespace FakeXrmEasy.Tests.FakeContextTests.RetrieveMetadataChangesRequestTests
{
    /// <summary>
    /// Tests for RetrieveMetadataChangesRequestExecutor.
    /// Addresses upstream PR #538 from MarkMpn.
    /// </summary>
    public class RetrieveMetadataChangesRequestTests
    {
        [Fact]
        public void When_CanExecute_Is_Called_With_Invalid_Request_Returns_False()
        {
            var executor = new RetrieveMetadataChangesRequestExecutor();
            var anotherRequest = new RetrieveMultipleRequest();
            Assert.False(executor.CanExecute(anotherRequest));
        }

        [Fact]
        public void When_CanExecute_Is_Called_With_Valid_Request_Returns_True()
        {
            var executor = new RetrieveMetadataChangesRequestExecutor();
            var request = new RetrieveMetadataChangesRequest();
            Assert.True(executor.CanExecute(request));
        }

        [Fact]
        public void When_No_Query_Returns_All_Metadata()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            // Initialize some metadata
            var accountMetadata = new EntityMetadata { LogicalName = "account" };
            var contactMetadata = new EntityMetadata { LogicalName = "contact" };

            context.InitializeMetadata(new[] { accountMetadata, contactMetadata });

            var request = new RetrieveMetadataChangesRequest
            {
                Query = null
            };

            var response = (RetrieveMetadataChangesResponse)service.Execute(request);

            Assert.NotNull(response);
            Assert.NotNull(response.EntityMetadata);
            Assert.True(response.EntityMetadata.Count >= 2);
        }

        [Fact]
        public void When_Filter_By_LogicalName_Returns_Matching_Entities()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var accountMetadata = new EntityMetadata { LogicalName = "account" };
            var contactMetadata = new EntityMetadata { LogicalName = "contact" };

            context.InitializeMetadata(new[] { accountMetadata, contactMetadata });

            var request = new RetrieveMetadataChangesRequest
            {
                Query = new EntityQueryExpression
                {
                    Criteria = new MetadataFilterExpression
                    {
                        FilterOperator = LogicalOperator.And,
                        Conditions =
                        {
                            new MetadataConditionExpression("LogicalName",
                                MetadataConditionOperator.Equals, "account")
                        }
                    }
                }
            };

            var response = (RetrieveMetadataChangesResponse)service.Execute(request);

            Assert.NotNull(response.EntityMetadata);
            Assert.Single(response.EntityMetadata);
            Assert.Equal("account", response.EntityMetadata[0].LogicalName);
        }

        [Fact]
        public void When_Filter_With_In_Operator_Returns_Matching_Entities()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var accountMetadata = new EntityMetadata { LogicalName = "account" };
            var contactMetadata = new EntityMetadata { LogicalName = "contact" };
            var leadMetadata = new EntityMetadata { LogicalName = "lead" };

            context.InitializeMetadata(new[] { accountMetadata, contactMetadata, leadMetadata });

            var request = new RetrieveMetadataChangesRequest
            {
                Query = new EntityQueryExpression
                {
                    Criteria = new MetadataFilterExpression
                    {
                        FilterOperator = LogicalOperator.And,
                        Conditions =
                        {
                            new MetadataConditionExpression("LogicalName",
                                MetadataConditionOperator.In, new[] { "account", "contact" })
                        }
                    }
                }
            };

            var response = (RetrieveMetadataChangesResponse)service.Execute(request);

            Assert.NotNull(response.EntityMetadata);
            Assert.Equal(2, response.EntityMetadata.Count);
            Assert.Contains(response.EntityMetadata, e => e.LogicalName == "account");
            Assert.Contains(response.EntityMetadata, e => e.LogicalName == "contact");
            Assert.DoesNotContain(response.EntityMetadata, e => e.LogicalName == "lead");
        }

        [Fact]
        public void When_Filter_With_NotEquals_Operator_Returns_Non_Matching_Entities()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var accountMetadata = new EntityMetadata { LogicalName = "account" };
            var contactMetadata = new EntityMetadata { LogicalName = "contact" };

            context.InitializeMetadata(new[] { accountMetadata, contactMetadata });

            var request = new RetrieveMetadataChangesRequest
            {
                Query = new EntityQueryExpression
                {
                    Criteria = new MetadataFilterExpression
                    {
                        FilterOperator = LogicalOperator.And,
                        Conditions =
                        {
                            new MetadataConditionExpression("LogicalName",
                                MetadataConditionOperator.NotEquals, "account")
                        }
                    }
                }
            };

            var response = (RetrieveMetadataChangesResponse)service.Execute(request);

            Assert.NotNull(response.EntityMetadata);
            Assert.DoesNotContain(response.EntityMetadata, e => e.LogicalName == "account");
            Assert.Contains(response.EntityMetadata, e => e.LogicalName == "contact");
        }

        [Fact]
        public void When_Or_Filter_Returns_Any_Matching_Entities()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var accountMetadata = new EntityMetadata { LogicalName = "account" };
            var contactMetadata = new EntityMetadata { LogicalName = "contact" };
            var leadMetadata = new EntityMetadata { LogicalName = "lead" };

            context.InitializeMetadata(new[] { accountMetadata, contactMetadata, leadMetadata });

            var request = new RetrieveMetadataChangesRequest
            {
                Query = new EntityQueryExpression
                {
                    Criteria = new MetadataFilterExpression
                    {
                        FilterOperator = LogicalOperator.Or,
                        Conditions =
                        {
                            new MetadataConditionExpression("LogicalName",
                                MetadataConditionOperator.Equals, "account"),
                            new MetadataConditionExpression("LogicalName",
                                MetadataConditionOperator.Equals, "lead")
                        }
                    }
                }
            };

            var response = (RetrieveMetadataChangesResponse)service.Execute(request);

            Assert.NotNull(response.EntityMetadata);
            Assert.Equal(2, response.EntityMetadata.Count);
            Assert.Contains(response.EntityMetadata, e => e.LogicalName == "account");
            Assert.Contains(response.EntityMetadata, e => e.LogicalName == "lead");
        }

        [Fact]
        public void GetResponsibleRequestType_Returns_Correct_Type()
        {
            var executor = new RetrieveMetadataChangesRequestExecutor();
            Assert.Equal(typeof(RetrieveMetadataChangesRequest), executor.GetResponsibleRequestType());
        }
    }
}
