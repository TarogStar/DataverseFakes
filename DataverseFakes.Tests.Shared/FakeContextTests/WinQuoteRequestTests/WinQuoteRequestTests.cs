using FakeXrmEasy.FakeMessageExecutors;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.ServiceModel;
using Xunit;

namespace FakeXrmEasy.Tests.FakeContextTests.WinQuoteRequestTests
{
    /// <summary>
    /// Tests for WinQuoteRequestExecutor.
    /// Addresses upstream PR #510.
    /// </summary>
    public class WinQuoteRequestTests
    {
        [Fact]
        public void When_CanExecute_Is_Called_With_Invalid_Request_Returns_False()
        {
            var executor = new WinQuoteRequestExecutor();
            var anotherRequest = new RetrieveMultipleRequest();
            Assert.False(executor.CanExecute(anotherRequest));
        }

        [Fact]
        public void When_CanExecute_Is_Called_With_Valid_Request_Returns_True()
        {
            var executor = new WinQuoteRequestExecutor();
            var request = new WinQuoteRequest();
            Assert.True(executor.CanExecute(request));
        }

        [Fact]
        public void When_WinQuote_Is_Called_Quote_State_Is_Set_To_Won()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var quote = new Entity
            {
                LogicalName = "quote",
                Id = Guid.NewGuid(),
                Attributes = new AttributeCollection
                {
                    { "statecode", new OptionSetValue(0) },
                    { "statuscode", new OptionSetValue(1) }
                }
            };

            context.Initialize(new[] { quote });

            var request = new WinQuoteRequest
            {
                QuoteClose = new Entity("quoteclose")
                {
                    Attributes = new AttributeCollection
                    {
                        { "quoteid", quote.ToEntityReference() },
                        { "subject", "Quote Won" }
                    }
                },
                Status = new OptionSetValue(4) // Won status
            };

            var response = (WinQuoteResponse)service.Execute(request);

            Assert.NotNull(response);

            // Verify quote state is now Won (2)
            var updatedQuote = service.Retrieve("quote", quote.Id, new ColumnSet(true));
            Assert.Equal(2, updatedQuote.GetAttributeValue<OptionSetValue>("statecode").Value);
            Assert.Equal(4, updatedQuote.GetAttributeValue<OptionSetValue>("statuscode").Value);
        }

        [Fact]
        public void When_WinQuote_Is_Called_QuoteClose_Activity_Is_Created()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var quote = new Entity
            {
                LogicalName = "quote",
                Id = Guid.NewGuid(),
                Attributes = new AttributeCollection
                {
                    { "statecode", new OptionSetValue(0) },
                    { "statuscode", new OptionSetValue(1) }
                }
            };

            context.Initialize(new[] { quote });

            var request = new WinQuoteRequest
            {
                QuoteClose = new Entity("quoteclose")
                {
                    Attributes = new AttributeCollection
                    {
                        { "quoteid", quote.ToEntityReference() },
                        { "subject", "Quote Won" }
                    }
                },
                Status = new OptionSetValue(4)
            };

            service.Execute(request);

            // Verify QuoteClose activity was created
            var quoteCloseQuery = new QueryExpression("quoteclose")
            {
                ColumnSet = new ColumnSet(true)
            };

            var results = service.RetrieveMultiple(quoteCloseQuery);

            Assert.Single(results.Entities);
            var quoteClose = results.Entities[0];
            Assert.Equal(quote.ToEntityReference().Id,
                quoteClose.GetAttributeValue<EntityReference>("regardingobjectid").Id);
        }

        [Fact]
        public void When_WinQuote_Is_Called_Without_Status_Throws_Exception()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var quote = new Entity
            {
                LogicalName = "quote",
                Id = Guid.NewGuid()
            };

            context.Initialize(new[] { quote });

            var request = new WinQuoteRequest
            {
                QuoteClose = new Entity("quoteclose")
                {
                    Attributes = new AttributeCollection
                    {
                        { "quoteid", quote.ToEntityReference() }
                    }
                },
                Status = null
            };

            Assert.Throws<FaultException<OrganizationServiceFault>>(() => service.Execute(request));
        }

        [Fact]
        public void When_WinQuote_Is_Called_Without_QuoteClose_Throws_Exception()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var request = new WinQuoteRequest
            {
                QuoteClose = null,
                Status = new OptionSetValue(4)
            };

            Assert.Throws<FaultException<OrganizationServiceFault>>(() => service.Execute(request));
        }

        [Fact]
        public void When_WinQuote_Is_Called_Without_QuoteId_Throws_Exception()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var request = new WinQuoteRequest
            {
                QuoteClose = new Entity("quoteclose"),
                Status = new OptionSetValue(4)
            };

            Assert.Throws<FaultException<OrganizationServiceFault>>(() => service.Execute(request));
        }

        [Fact]
        public void GetResponsibleRequestType_Returns_Correct_Type()
        {
            var executor = new WinQuoteRequestExecutor();
            Assert.Equal(typeof(WinQuoteRequest), executor.GetResponsibleRequestType());
        }
    }
}
