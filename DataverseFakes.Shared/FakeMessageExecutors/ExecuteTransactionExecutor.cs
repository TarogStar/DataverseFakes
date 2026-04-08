#if FAKE_XRM_EASY_2016 || FAKE_XRM_EASY_365 || FAKE_XRM_EASY_9

using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using System;

namespace FakeXrmEasy.FakeMessageExecutors
{
    /// <summary>
    /// Fake message executor that handles ExecuteTransactionRequest messages for transactional execution of multiple CRM requests.
    /// Executes all requests in the collection sequentially and optionally returns responses based on the ReturnResponses setting.
    /// Available only for Dynamics 365 v2016 and later (FAKE_XRM_EASY_2016, FAKE_XRM_EASY_365, FAKE_XRM_EASY_9).
    /// </summary>
    public class ExecuteTransactionExecutor : IFakeMessageExecutor
    {
        /// <summary>
        /// Determines whether this executor can handle the specified organization request.
        /// </summary>
        /// <param name="request">The organization request to evaluate.</param>
        /// <returns>True if the request is an ExecuteTransactionRequest; otherwise, false.</returns>
        public bool CanExecute(OrganizationRequest request)
        {
            return request is ExecuteTransactionRequest;
        }

        /// <summary>
        /// Executes the ExecuteTransactionRequest to process multiple CRM requests as a single transactional unit.
        /// Iterates through each request in the collection, executing them sequentially.
        /// If ReturnResponses is set to true, includes individual responses in the response collection.
        /// </summary>
        /// <param name="request">The ExecuteTransactionRequest containing a collection of requests and the ReturnResponses option.</param>
        /// <param name="ctx">The XrmFakedContext providing the in-memory CRM context and organization service.</param>
        /// <returns>An ExecuteTransactionResponse containing the responses collection if ReturnResponses is enabled.</returns>
        public OrganizationResponse Execute(OrganizationRequest request, XrmFakedContext ctx)
        {
            var executeTransactionRequest = (ExecuteTransactionRequest)request;
            var response = new ExecuteTransactionResponse { ["Responses"] = new OrganizationResponseCollection() };

            var service = ctx.GetOrganizationService();

            foreach (var r in executeTransactionRequest.Requests)
            {
                var result = service.Execute(r);

                if (executeTransactionRequest.ReturnResponses.HasValue && executeTransactionRequest.ReturnResponses.Value)
                {
                    response.Responses.Add(result);
                }
            }
            return response;
        }

        /// <summary>
        /// Gets the type of organization request that this executor is responsible for handling.
        /// </summary>
        /// <returns>The Type of ExecuteTransactionRequest.</returns>
        public Type GetResponsibleRequestType()
        {
            return typeof(ExecuteTransactionRequest);
        }
    }
}
#endif