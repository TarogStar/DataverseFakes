using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;

namespace FakeXrmEasy.FakeMessageExecutors
{
    /// <summary>
    /// Fake message executor for ReviseQuoteRequest
    /// </summary>
    public class ReviseQuoteRequestExecutor : IFakeMessageExecutor
    {
        /// <summary>
        /// Determines whether this executor can execute the given request
        /// </summary>
        /// <param name="request">The organization request</param>
        /// <returns>True if the request is ReviseQuoteRequest</returns>
        public bool CanExecute(OrganizationRequest request)
        {
            return request is ReviseQuoteRequest;
        }

        /// <summary>
        /// Executes the ReviseQuoteRequest
        /// </summary>
        /// <param name="request">The organization request</param>
        /// <param name="ctx">The faked context</param>
        /// <returns>ReviseQuoteResponse</returns>
        public OrganizationResponse Execute(OrganizationRequest request, XrmFakedContext ctx)
        {
            var service = ctx.GetOrganizationService();

            var reviseQuoteRequest = request as ReviseQuoteRequest;

            if (reviseQuoteRequest == null)
            {
                throw new Exception("You did not pass a ReviseQuoteRequest!");
            }

            var oldQuoteId = reviseQuoteRequest.QuoteId;

            if (oldQuoteId == Guid.Empty)
            {
                throw new Exception("QuoteId needs to be set!");
            }

            var oldQuote = service.Retrieve("quote", oldQuoteId, new ColumnSet(true));

            var revisedQuote = new Entity
            {
                LogicalName = "quote",
                Id = Guid.NewGuid()
            };

            var columnSet = reviseQuoteRequest.ColumnSet;
            var quoteBlackList = new List<string> { "quoteid", "statuscode", "statecode", "createdon", "createdby" };

            foreach (var attribute in oldQuote.Attributes)
            {
                if (quoteBlackList.Contains(attribute.Key))
                {
                    continue;
                }

                if (columnSet.AllColumns || columnSet.Columns.Contains(attribute.Key))
                {
                    if(attribute.Key != "statecode")  //Skip statecode on create
                        revisedQuote[attribute.Key] = attribute.Value; 
                }
            }

            service.Create(revisedQuote);

            var quoteLines = service.RetrieveMultiple(new QueryExpression("quotedetail")
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression(LogicalOperator.And)
                {
                    Conditions = { new ConditionExpression("quoteid", ConditionOperator.Equal, oldQuote.ToEntityReference()) }
                }
            }).Entities.ToList();

            foreach (var quoteDetail in quoteLines)
            {
                var revisedDetail = new Entity
                {
                    LogicalName = "quotedetail",
                    Id = Guid.NewGuid(),
                    Attributes = new AttributeCollection
                    {
                        { "quoteid", revisedQuote.ToEntityReference() }
                    }
                };

                var quoteDetailBlackList = new List<string> { "quoteid", "quotedetailid", "createdon", "createdby" };

                foreach (var attribute in quoteDetail.Attributes)
                {
                    if (quoteDetailBlackList.Contains(attribute.Key))
                    {
                        continue;
                    }
                    if (attribute.Key != "statecode")  //Skip statecode on create
                        revisedDetail[attribute.Key] = attribute.Value;
                }

                service.Create(revisedDetail);
            }

            var response = new ReviseQuoteResponse();

            revisedQuote = service.Retrieve(revisedQuote.LogicalName, revisedQuote.Id, new ColumnSet(true));

            response.Results["Entity"] = revisedQuote;

            return response;
        }

        /// <summary>
        /// Gets the type of request this executor is responsible for
        /// </summary>
        /// <returns>The type of ReviseQuoteRequest</returns>
        public Type GetResponsibleRequestType()
        {
            return typeof(ReviseQuoteRequest);
        }
    }
}
