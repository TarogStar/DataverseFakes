using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;

namespace FakeXrmEasy.FakeMessageExecutors
{
    /// <summary>
    /// Executor for UpsertMultiple requests - optimized bulk upsert (create or update) operation
    /// </summary>
    public class UpsertMultipleRequestExecutor : IFakeMessageExecutor
    {
        /// <summary>
        /// Determines whether this executor can execute the given request
        /// </summary>
        /// <param name="request">The organization request</param>
        /// <returns>True if the request is UpsertMultipleRequest</returns>
        public bool CanExecute(OrganizationRequest request)
        {
            return request is UpsertMultipleRequest ||
                   (request != null && request.RequestName == "UpsertMultiple");
        }

        /// <summary>
        /// Executes the UpsertMultipleRequest
        /// </summary>
        /// <param name="request">The organization request</param>
        /// <param name="ctx">The faked context</param>
        /// <returns>UpsertMultipleResponse</returns>
        public OrganizationResponse Execute(OrganizationRequest request, XrmFakedContext ctx)
        {
            var upsertMultipleRequest = request as UpsertMultipleRequest;
            EntityCollection targets = null;

            // Handle both strongly-typed and loosely-typed requests
            if (upsertMultipleRequest != null)
            {
                targets = upsertMultipleRequest.Targets;
            }
            else if (request.Parameters.Contains("Targets"))
            {
                targets = (EntityCollection)request.Parameters["Targets"];
            }

            if (targets == null)
            {
                throw new FaultException<OrganizationServiceFault>(
                    new OrganizationServiceFault(),
                    "UpsertMultipleRequest must contain a 'Targets' parameter with an EntityCollection.");
            }

            if (targets.Entities == null || targets.Entities.Count == 0)
            {
                throw new FaultException<OrganizationServiceFault>(
                    new OrganizationServiceFault(),
                    "UpsertMultipleRequest 'Targets' collection cannot be empty.");
            }

            var service = ctx.GetOrganizationService();
            var results = new List<UpsertResponse>();

            // UpsertMultiple is transactional - all succeed or all fail
            try
            {
                foreach (var entity in targets.Entities)
                {
                    if (entity == null)
                    {
                        throw new FaultException<OrganizationServiceFault>(
                            new OrganizationServiceFault(),
                            "UpsertMultipleRequest 'Targets' collection cannot contain null entities.");
                    }

                    bool recordCreated = false;
                    Guid id = entity.Id;

                    // Check if entity exists - thread-safe access
                    bool exists = false;
                    if (id != Guid.Empty)
                    {
                        ConcurrentDictionary<Guid, Entity> entityDict;
                        exists = ctx.Data.TryGetValue(entity.LogicalName, out entityDict) &&
                                entityDict.ContainsKey(id);
                    }

                    if (exists)
                    {
                        // Update existing record
                        service.Update(entity);
                        recordCreated = false;
                    }
                    else
                    {
                        // Create new record
                        id = service.Create(entity);
                        recordCreated = true;
                    }

                    var upsertResult = new UpsertResponse();
                    upsertResult.Results.Add("RecordCreated", recordCreated);
                    upsertResult.Results.Add("Target", new EntityReference(entity.LogicalName, id));
                    results.Add(upsertResult);
                }

                // All upserts succeeded
                var response = new UpsertMultipleResponse();
                response["Results"] = results.ToArray();

                return response;
            }
            catch (Exception ex)
            {
                // In a real transactional scenario, this would rollback
                // For now, we'll just throw the exception
                throw new FaultException<OrganizationServiceFault>(
                    new OrganizationServiceFault { Message = $"UpsertMultiple operation failed: {ex.Message}" },
                    ex.Message);
            }
        }

        /// <summary>
        /// Gets the type of request this executor is responsible for
        /// </summary>
        /// <returns>The type of UpsertMultipleRequest</returns>
        public Type GetResponsibleRequestType()
        {
            return typeof(UpsertMultipleRequest);
        }
    }
}
