using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using Microsoft.Xrm.Sdk.Query;

namespace FakeXrmEasy.FakeMessageExecutors
{
    /// <summary>
    /// Fake message executor for RetrieveExchangeRateRequest
    /// </summary>
    public class RetrieveExchangeRateRequestExecutor : IFakeMessageExecutor
    {
        /// <summary>
        /// Determines whether this executor can execute the given request
        /// </summary>
        /// <param name="request">The organization request</param>
        /// <returns>True if the request is RetrieveExchangeRateRequest</returns>
        public bool CanExecute(OrganizationRequest request)
        {
            return request is RetrieveExchangeRateRequest;
        }

        /// <summary>
        /// Executes the RetrieveExchangeRateRequest
        /// </summary>
        /// <param name="request">The organization request</param>
        /// <param name="ctx">The faked context</param>
        /// <returns>RetrieveExchangeRateResponse</returns>
        public OrganizationResponse Execute(OrganizationRequest request, XrmFakedContext ctx)
        {
            var retrieveExchangeRateRequest = (RetrieveExchangeRateRequest)request;

            var currencyId = retrieveExchangeRateRequest.TransactionCurrencyId;

            if (currencyId == null)
            {
                throw new FaultException<OrganizationServiceFault>(new OrganizationServiceFault(), "Can not retrieve Exchange Rate without Transaction Currency Guid");
            }

            var service = ctx.GetOrganizationService();

            var result = service.RetrieveMultiple(new QueryExpression("transactioncurrency")
            {
                ColumnSet = new ColumnSet("exchangerate"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("transactioncurrencyid", ConditionOperator.Equal, currencyId)
                    }
                }
            }).Entities;

            if (!result.Any())
            {
                throw new FaultException<OrganizationServiceFault>(new OrganizationServiceFault(), "Transaction Currency not found");
            }

            var exchangeRate = result.First().GetAttributeValue<decimal>("exchangerate");

            return new RetrieveExchangeRateResponse
            {
                Results = new ParameterCollection
                {
                    {"ExchangeRate", exchangeRate}
                }
            };
        }

        /// <summary>
        /// Gets the type of request this executor is responsible for
        /// </summary>
        /// <returns>The type of RetrieveExchangeRateRequest</returns>
        public Type GetResponsibleRequestType()
        {
            return typeof(RetrieveExchangeRateRequest);
        }
    }
}