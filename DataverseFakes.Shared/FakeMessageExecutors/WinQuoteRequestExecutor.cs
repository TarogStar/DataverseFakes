using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using System;
using System.ServiceModel;

namespace FakeXrmEasy.FakeMessageExecutors
{
    /// <summary>
    /// Fake message executor for WinQuoteRequest.
    /// Sets the quote state to Won and creates a QuoteClose activity.
    /// Addresses upstream PR #510.
    /// </summary>
    public class WinQuoteRequestExecutor : IFakeMessageExecutor
    {
        /// <summary>
        /// Determines whether this executor can execute the given request
        /// </summary>
        /// <param name="request">The organization request</param>
        /// <returns>True if the request is WinQuoteRequest</returns>
        public bool CanExecute(OrganizationRequest request)
        {
            return request is WinQuoteRequest;
        }

        /// <summary>
        /// Executes the WinQuoteRequest by setting quote state to Won and creating QuoteClose activity
        /// </summary>
        /// <param name="request">The organization request</param>
        /// <param name="ctx">The faked context</param>
        /// <returns>WinQuoteResponse</returns>
        public OrganizationResponse Execute(OrganizationRequest request, XrmFakedContext ctx)
        {
            var winQuoteRequest = request as WinQuoteRequest;

            if (winQuoteRequest == null)
            {
                throw new FaultException<OrganizationServiceFault>(
                    new OrganizationServiceFault(),
                    $"{nameof(WinQuoteRequest)} must not be null");
            }

            if (winQuoteRequest.Status == null)
            {
                throw new FaultException<OrganizationServiceFault>(
                    new OrganizationServiceFault(),
                    $"{nameof(WinQuoteRequest.Status)} must not be null");
            }

            if (winQuoteRequest.QuoteClose == null)
            {
                throw new FaultException<OrganizationServiceFault>(
                    new OrganizationServiceFault(),
                    $"{nameof(WinQuoteRequest.QuoteClose)} must not be null");
            }

            var quote = winQuoteRequest.QuoteClose.GetAttributeValue<EntityReference>("quoteid");

            if (quote == null)
            {
                throw new FaultException<OrganizationServiceFault>(
                    new OrganizationServiceFault(),
                    "QuoteClose must have a 'quoteid' EntityReference");
            }

            if (!quote.LogicalName.Equals("quote", StringComparison.InvariantCultureIgnoreCase))
            {
                throw new FaultException<OrganizationServiceFault>(
                    new OrganizationServiceFault(),
                    $"'quoteid' must refer to 'quote'; got '{quote.LogicalName}'");
            }

            // Update the quote to Won state (statecode = 2)
            var quoteUpdate = new Entity
            {
                Id = quote.Id,
                LogicalName = quote.LogicalName,
                Attributes = new AttributeCollection
                {
                    { "statecode", new OptionSetValue(2) },      // Won
                    { "statuscode", winQuoteRequest.Status }
                }
            };

            // Set up the QuoteClose activity
            winQuoteRequest.QuoteClose["regardingobjectid"] = quote;
            winQuoteRequest.QuoteClose["activitytypecode"] = new OptionSetValue(4211);  // QuoteClose

            var service = ctx.GetOrganizationService();

            // Update the Quote state and status
            service.Update(quoteUpdate);

            // Create the QuoteClose activity
            Guid quoteCloseId = service.Create(winQuoteRequest.QuoteClose);

            // Mark the QuoteClose activity as Completed
            service.Update(new Entity
            {
                Id = quoteCloseId,
                LogicalName = winQuoteRequest.QuoteClose.LogicalName,
                Attributes = new AttributeCollection
                {
                    { "statecode", new OptionSetValue(1) },      // Completed
                    { "statuscode", new OptionSetValue(2) }      // Completed
                }
            });

            return new WinQuoteResponse();
        }

        /// <summary>
        /// Gets the type of request this executor is responsible for
        /// </summary>
        /// <returns>The type of WinQuoteRequest</returns>
        public Type GetResponsibleRequestType()
        {
            return typeof(WinQuoteRequest);
        }
    }
}
