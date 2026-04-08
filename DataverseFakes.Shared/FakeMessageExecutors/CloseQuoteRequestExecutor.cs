using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using System;

namespace FakeXrmEasy.FakeMessageExecutors
{
    /// <summary>
    /// Fake message executor for CloseQuoteRequest
    /// </summary>
    public class CloseQuoteRequestExecutor : IFakeMessageExecutor
    {
        /// <summary>
        /// Determines whether this executor can execute the given request
        /// </summary>
        /// <param name="request">The organization request</param>
        /// <returns>True if the request is CloseQuoteRequest</returns>
        public bool CanExecute(OrganizationRequest request)
        {
            return request is CloseQuoteRequest;
        }

        /// <summary>
        /// Executes the CloseQuoteRequest
        /// </summary>
        /// <param name="request">The organization request</param>
        /// <param name="ctx">The faked context</param>
        /// <returns>CloseQuoteResponse</returns>
        public OrganizationResponse Execute(OrganizationRequest request, XrmFakedContext ctx)
        {
            var closeRequest = request as CloseQuoteRequest;

            if (closeRequest == null)
            {
                throw new Exception("You did not pass a CloseQuoteRequest");
            }

            var quoteClose = closeRequest.QuoteClose;

            if (quoteClose == null)
            {
                throw new Exception("QuoteClose is mandatory");
            }

            var quoteId = quoteClose.GetAttributeValue<EntityReference>("quoteid");

            if (quoteId == null)
            {
                throw new Exception("Quote ID is not set on QuoteClose, but is required");
            }

            var update = new Entity
            {
                Id = quoteId.Id,
                LogicalName = "quote",
                Attributes = new AttributeCollection
                {
                    { "statuscode", closeRequest.Status }
                }
            };

            var service = ctx.GetOrganizationService();

            service.Update(update);

            return new CloseQuoteResponse();
        }

        /// <summary>
        /// Gets the type of request this executor is responsible for
        /// </summary>
        /// <returns>The type of CloseQuoteRequest</returns>
        public Type GetResponsibleRequestType()
        {
            return typeof(CloseQuoteRequest);
        }
    }
}