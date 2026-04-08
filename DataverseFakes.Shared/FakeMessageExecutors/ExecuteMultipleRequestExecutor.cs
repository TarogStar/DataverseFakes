using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using System;
using System.Linq;
using System.ServiceModel;

namespace FakeXrmEasy.FakeMessageExecutors
{
    /// <summary>
    /// Fake message executor that handles ExecuteMultipleRequest messages for batch execution of multiple CRM requests.
    /// Supports configurable behavior through ExecuteMultipleSettings including ContinueOnError and ReturnResponses options.
    /// Implements response behavior as documented at https://msdn.microsoft.com/en-us/library/jj863631.aspx.
    /// </summary>
    public class ExecuteMultipleRequestExecutor : IFakeMessageExecutor
    {
        /// <summary>
        /// Determines whether this executor can handle the specified organization request.
        /// </summary>
        /// <param name="request">The organization request to evaluate.</param>
        /// <returns>True if the request is an ExecuteMultipleRequest; otherwise, false.</returns>
        public bool CanExecute(OrganizationRequest request)
        {
            return request is ExecuteMultipleRequest;
        }

        /// <summary>
        /// Executes the ExecuteMultipleRequest to process multiple CRM requests in batch.
        /// Iterates through each request in the collection, executing them sequentially.
        /// Handles errors based on ContinueOnError setting and returns responses based on ReturnResponses setting.
        /// </summary>
        /// <param name="request">The ExecuteMultipleRequest containing a collection of requests and execution settings.</param>
        /// <param name="ctx">The XrmFakedContext providing the in-memory CRM context and organization service.</param>
        /// <returns>An ExecuteMultipleResponse containing the responses collection and IsFaulted indicator if any request failed.</returns>
        /// <exception cref="FaultException{OrganizationServiceFault}">Thrown when Settings or Requests properties are null.</exception>
        public OrganizationResponse Execute(OrganizationRequest request, XrmFakedContext ctx)
        {
            var executeMultipleRequest = (ExecuteMultipleRequest)request;

            if (executeMultipleRequest.Settings == null)
            {
                throw new FaultException<OrganizationServiceFault>(new OrganizationServiceFault(), "You need to pass a value for 'Settings' in execute multiple request");
            }

            if (executeMultipleRequest.Requests == null)
            {
                throw new FaultException<OrganizationServiceFault>(new OrganizationServiceFault(), "You need to pass a value for 'Requests' in execute multiple request");
            }

            var service = ctx.GetOrganizationService();

            var response = new ExecuteMultipleResponse();
            response.Results["Responses"] = new ExecuteMultipleResponseItemCollection();

            for (var i = 0; i < executeMultipleRequest.Requests.Count; i++)
            {
                var executeRequest = executeMultipleRequest.Requests[i];

                try
                {
                    OrganizationResponse resp = service.Execute(executeRequest);

                    if (executeMultipleRequest.Settings.ReturnResponses)
                    {
                        response.Responses.Add(new ExecuteMultipleResponseItem
                        {
                            RequestIndex = i,
                            Response = resp
                        });
                    }
                }
                catch (Exception ex)
                {
                    if (!response.IsFaulted)
                    {
                        response.Results["IsFaulted"] = true;
                    }

                    // Extract fault details from the exception
                    OrganizationServiceFault fault;
                    var faultException = ex as FaultException<OrganizationServiceFault>;
                    if (faultException != null && faultException.Detail != null)
                    {
                        // Preserve the original fault information
                        fault = new OrganizationServiceFault
                        {
                            Message = faultException.Detail.Message ?? ex.Message,
                            ErrorCode = faultException.Detail.ErrorCode,
                            TraceText = faultException.Detail.TraceText
                        };
                    }
                    else
                    {
                        fault = new OrganizationServiceFault { Message = ex.Message };
                    }

                    response.Responses.Add(new ExecuteMultipleResponseItem
                    {
                        Fault = fault,
                        RequestIndex = i
                    });

                    if (!executeMultipleRequest.Settings.ContinueOnError)
                    {
                        break;
                    }
                }
            }

            // When ReturnResponses is false and no faults occurred, clear the responses
            // When ReturnResponses is false and faults occurred, only keep the fault responses
            // The Responses collection was already populated correctly during execution
            if (!executeMultipleRequest.Settings.ReturnResponses)
            {
                if (response.Responses.Any(resp => resp.Fault != null))
                {
                    // Keep only fault responses
                    var failures = new ExecuteMultipleResponseItemCollection();
                    failures.AddRange(response.Responses.Where(resp => resp.Fault != null));
                    response.Results["Responses"] = failures;
                }
                else
                {
                    // Clear responses when ReturnResponses is false and no faults
                    response.Results["Responses"] = new ExecuteMultipleResponseItemCollection();
                }
            }

            return response;
        }

        /// <summary>
        /// Gets the type of organization request that this executor is responsible for handling.
        /// </summary>
        /// <returns>The Type of ExecuteMultipleRequest.</returns>
        public Type GetResponsibleRequestType()
        {
            return typeof(ExecuteMultipleRequest);
        }
    }
}