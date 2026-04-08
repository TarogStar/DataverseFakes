using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;

namespace FakeXrmEasy.FakeMessageExecutors
{
    /// <summary>
    /// Executor for CreateMultiple requests - optimized bulk create operation
    /// </summary>
    public class CreateMultipleRequestExecutor : IFakeMessageExecutor
    {
        /// <summary>
        /// Determines whether this executor can execute the given request
        /// </summary>
        /// <param name="request">The organization request</param>
        /// <returns>True if the request is CreateMultipleRequest</returns>
        public bool CanExecute(OrganizationRequest request)
        {
            return request is CreateMultipleRequest ||
                   (request != null && request.RequestName == "CreateMultiple");
        }

        /// <summary>
        /// Executes the CreateMultipleRequest
        /// </summary>
        /// <param name="request">The organization request</param>
        /// <param name="ctx">The faked context</param>
        /// <returns>CreateMultipleResponse</returns>
        public OrganizationResponse Execute(OrganizationRequest request, XrmFakedContext ctx)
        {
            var createMultipleRequest = request as CreateMultipleRequest;
            EntityCollection targets = null;

            // Handle both strongly-typed and loosely-typed requests
            if (createMultipleRequest != null)
            {
                targets = createMultipleRequest.Targets;
            }
            else if (request.Parameters.Contains("Targets"))
            {
                targets = (EntityCollection)request.Parameters["Targets"];
            }

            if (targets == null)
            {
                throw new FaultException<OrganizationServiceFault>(
                    new OrganizationServiceFault(),
                    "CreateMultipleRequest must contain a 'Targets' parameter with an EntityCollection.");
            }

            if (targets.Entities == null || targets.Entities.Count == 0)
            {
                throw new FaultException<OrganizationServiceFault>(
                    new OrganizationServiceFault(),
                    "CreateMultipleRequest 'Targets' collection cannot be empty.");
            }

            var service = ctx.GetOrganizationService();
            var createdIds = new List<Guid>();

            // CreateMultiple is transactional - all succeed or all fail
            // We'll simulate this by collecting all creates and only committing if all succeed
            try
            {
                foreach (var entity in targets.Entities)
                {
                    if (entity == null)
                    {
                        throw new FaultException<OrganizationServiceFault>(
                            new OrganizationServiceFault(),
                            "CreateMultipleRequest 'Targets' collection cannot contain null entities.");
                    }

                    var id = service.Create(entity);
                    createdIds.Add(id);
                }

                // All creates succeeded
                var response = new CreateMultipleResponse();
                response.Results["Ids"] = createdIds.ToArray();
                response.ResponseName = "CreateMultiple";

                return response;
            }
            catch (Exception ex)
            {
                // In a real transactional scenario, this would rollback
                // For now, we'll just throw the exception
                // Note: FakeXrmEasy doesn't support full transaction rollback by default
                throw new FaultException<OrganizationServiceFault>(
                    new OrganizationServiceFault { Message = $"CreateMultiple operation failed: {ex.Message}" },
                    ex.Message);
            }
        }

        /// <summary>
        /// Gets the type of request this executor is responsible for
        /// </summary>
        /// <returns>The type of CreateMultipleRequest</returns>
        public Type GetResponsibleRequestType()
        {
            return typeof(CreateMultipleRequest);
        }
    }
}
