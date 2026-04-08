using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using System;
using System.Collections.Generic;
using System.ServiceModel;

namespace FakeXrmEasy.FakeMessageExecutors
{
    /// <summary>
    /// Executor for DeleteMultiple requests - optimized bulk delete operation
    /// Note: DeleteMultiple is currently in preview and primarily supported for elastic tables
    /// </summary>
    public class DeleteMultipleRequestExecutor : IFakeMessageExecutor
    {
        /// <summary>
        /// Determines whether this executor can execute the given request
        /// </summary>
        /// <param name="request">The organization request</param>
        /// <returns>True if the request is DeleteMultipleRequest</returns>
        public bool CanExecute(OrganizationRequest request)
        {
            return request is DeleteMultipleRequest ||
                   (request != null && request.RequestName == "DeleteMultiple");
        }

        /// <summary>
        /// Executes the DeleteMultipleRequest
        /// </summary>
        /// <param name="request">The organization request</param>
        /// <param name="ctx">The faked context</param>
        /// <returns>OrganizationResponse</returns>
        public OrganizationResponse Execute(OrganizationRequest request, XrmFakedContext ctx)
        {
            var deleteMultipleRequest = request as DeleteMultipleRequest;
            EntityReferenceCollection targets = null;

            // Handle both strongly-typed and loosely-typed requests
            if (deleteMultipleRequest != null)
            {
                targets = deleteMultipleRequest.Targets;
            }
            else if (request.Parameters.Contains("Targets"))
            {
                targets = (EntityReferenceCollection)request.Parameters["Targets"];
            }

            if (targets == null)
            {
                throw new FaultException<OrganizationServiceFault>(
                    new OrganizationServiceFault(),
                    "DeleteMultipleRequest must contain a 'Targets' parameter with an EntityReferenceCollection.");
            }

            if (targets.Count == 0)
            {
                throw new FaultException<OrganizationServiceFault>(
                    new OrganizationServiceFault(),
                    "DeleteMultipleRequest 'Targets' collection cannot be empty.");
            }

            var service = ctx.GetOrganizationService();

            // DeleteMultiple is transactional - all succeed or all fail
            try
            {
                foreach (var entityRef in targets)
                {
                    if (entityRef == null)
                    {
                        throw new FaultException<OrganizationServiceFault>(
                            new OrganizationServiceFault(),
                            "DeleteMultipleRequest 'Targets' collection cannot contain null entity references.");
                    }

                    if (entityRef.Id == Guid.Empty)
                    {
                        throw new FaultException<OrganizationServiceFault>(
                            new OrganizationServiceFault(),
                            "DeleteMultipleRequest requires all entity references to have a valid Id.");
                    }

                    service.Delete(entityRef.LogicalName, entityRef.Id);
                }

                // All deletes succeeded
                // Note: DeleteMultiple does not have a specific response type, returns base OrganizationResponse
                return new OrganizationResponse();
            }
            catch (Exception ex)
            {
                // In a real transactional scenario, this would rollback
                // For now, we'll just throw the exception
                throw new FaultException<OrganizationServiceFault>(
                    new OrganizationServiceFault { Message = $"DeleteMultiple operation failed: {ex.Message}" },
                    ex.Message);
            }
        }

        /// <summary>
        /// Gets the type of request this executor is responsible for
        /// </summary>
        /// <returns>The type of DeleteMultipleRequest</returns>
        public Type GetResponsibleRequestType()
        {
            return typeof(DeleteMultipleRequest);
        }
    }
}
