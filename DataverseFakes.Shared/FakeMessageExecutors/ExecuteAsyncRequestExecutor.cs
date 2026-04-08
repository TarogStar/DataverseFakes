using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using System;
using System.ServiceModel;

namespace FakeXrmEasy.FakeMessageExecutors
{
    /// <summary>
    /// Fake message executor that handles ExecuteAsyncRequest messages for asynchronous execution of CRM operations.
    /// In real Dataverse, ExecuteAsyncRequest wraps another OrganizationRequest and executes it asynchronously,
    /// returning an AsyncJobId that references an asyncoperation entity. Since FakeXrmEasy uses an in-memory context,
    /// the wrapped request is executed immediately (synchronously) but the executor still creates an asyncoperation
    /// record to simulate the real behavior and allow tests to verify async job creation.
    /// </summary>
    public class ExecuteAsyncRequestExecutor : IFakeMessageExecutor
    {
        /// <summary>
        /// Determines whether this executor can handle the specified organization request.
        /// </summary>
        /// <param name="request">The organization request to evaluate.</param>
        /// <returns>True if the request is an ExecuteAsyncRequest; otherwise, false.</returns>
        public bool CanExecute(OrganizationRequest request)
        {
            return request is ExecuteAsyncRequest;
        }

        /// <summary>
        /// Executes the ExecuteAsyncRequest by extracting and executing the wrapped request immediately,
        /// then creating an asyncoperation entity to track the job.
        /// </summary>
        /// <param name="request">The ExecuteAsyncRequest containing the wrapped request to execute.</param>
        /// <param name="ctx">The XrmFakedContext providing the in-memory CRM context and organization service.</param>
        /// <returns>An ExecuteAsyncResponse containing the AsyncJobId of the created asyncoperation record.</returns>
        /// <exception cref="FaultException{OrganizationServiceFault}">Thrown when the Request property is null.</exception>
        public OrganizationResponse Execute(OrganizationRequest request, XrmFakedContext ctx)
        {
            var executeAsyncRequest = (ExecuteAsyncRequest)request;

            if (executeAsyncRequest.Request == null)
            {
                throw new FaultException<OrganizationServiceFault>(
                    new OrganizationServiceFault(),
                    "You must provide a value for the 'Request' property in ExecuteAsyncRequest.");
            }

            var service = ctx.GetOrganizationService();

            // Generate a new ID for the async job
            var asyncJobId = Guid.NewGuid();

            // Create the asyncoperation entity to track the job
            // Initially set to Ready state (0)
            var asyncOperation = new Entity("asyncoperation")
            {
                Id = asyncJobId
            };
            asyncOperation["name"] = $"ExecuteAsync: {executeAsyncRequest.Request.RequestName}";
            asyncOperation["operationtype"] = new OptionSetValue(10); // 10 = Workflow (generic async operation)
            asyncOperation["statecode"] = new OptionSetValue(0); // Ready

            service.Create(asyncOperation);

            // Execute the wrapped request immediately (since we're in-memory)
            try
            {
                service.Execute(executeAsyncRequest.Request);

                // Update the asyncoperation to Completed state (3)
                asyncOperation["statecode"] = new OptionSetValue(3); // Completed
                asyncOperation["statuscode"] = new OptionSetValue(30); // Succeeded
                service.Update(asyncOperation);
            }
            catch (Exception ex)
            {
                // Update the asyncoperation to Failed state
                asyncOperation["statecode"] = new OptionSetValue(3); // Completed
                asyncOperation["statuscode"] = new OptionSetValue(31); // Failed
                asyncOperation["message"] = ex.Message;
                service.Update(asyncOperation);

                // Re-throw the exception so the caller knows the operation failed
                throw;
            }

            // Return the response with the async job ID
            var response = new ExecuteAsyncResponse();
            response.Results["AsyncJobId"] = asyncJobId;

            return response;
        }

        /// <summary>
        /// Gets the type of organization request that this executor is responsible for handling.
        /// </summary>
        /// <returns>The Type of ExecuteAsyncRequest.</returns>
        public Type GetResponsibleRequestType()
        {
            return typeof(ExecuteAsyncRequest);
        }
    }
}
