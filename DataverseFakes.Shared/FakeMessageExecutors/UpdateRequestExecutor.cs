using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using System;
using System.Collections.Concurrent;
using System.ServiceModel;

namespace FakeXrmEasy.FakeMessageExecutors
{
    /// <summary>
    /// Handles the execution of <see cref="UpdateRequest"/> messages in the faked CRM context.
    /// This executor simulates updating existing entity records in Dynamics 365/CRM.
    /// </summary>
    /// <remarks>
    /// The executor modifies an existing entity record in the in-memory context based on
    /// the attributes provided in the Target entity. Only the attributes included in the
    /// Target are updated; other attributes on the existing record remain unchanged.
    /// Supports optimistic concurrency via ConcurrencyBehavior.IfRowVersionMatches.
    /// </remarks>
    public class UpdateRequestExecutor : IFakeMessageExecutor
    {
        /// <summary>
        /// Determines whether this executor can handle the specified organization request.
        /// </summary>
        /// <param name="request">The <see cref="OrganizationRequest"/> to evaluate.</param>
        /// <returns>
        /// <c>true</c> if the request is an <see cref="UpdateRequest"/>; otherwise, <c>false</c>.
        /// </returns>
        public bool CanExecute(OrganizationRequest request)
        {
            return request is UpdateRequest;
        }

        /// <summary>
        /// Executes the update operation, modifying an existing entity record in the faked CRM context.
        /// </summary>
        /// <param name="request">The <see cref="UpdateRequest"/> containing the entity with updated attributes.</param>
        /// <param name="ctx">The <see cref="XrmFakedContext"/> that provides the in-memory CRM simulation.</param>
        /// <returns>
        /// An <see cref="UpdateResponse"/> indicating successful completion of the update operation.
        /// </returns>
        /// <remarks>
        /// The Target property of the <see cref="UpdateRequest"/> must contain an entity with a valid Id
        /// and the attributes to be updated. The entity must already exist in the context.
        /// When ConcurrencyBehavior is set to IfRowVersionMatches, the RowVersion property must be provided
        /// and must match the current version of the record, or a ConcurrencyVersionMismatch fault is thrown.
        /// </remarks>
        public OrganizationResponse Execute(OrganizationRequest request, XrmFakedContext ctx)
        {
            var updateRequest = (UpdateRequest)request;

            var target = (Entity)request.Parameters["Target"];

            // Check for ConcurrencyBehavior
            if (updateRequest.ConcurrencyBehavior == ConcurrencyBehavior.IfRowVersionMatches)
            {
                ValidateConcurrency(target, ctx);
            }

            var service = ctx.GetOrganizationService();
            service.Update(target);

            return new UpdateResponse();
        }

        /// <summary>
        /// Validates the concurrency by comparing the provided RowVersion with the stored version.
        /// </summary>
        /// <param name="target">The entity being updated.</param>
        /// <param name="ctx">The fake context containing the stored entity.</param>
        /// <exception cref="FaultException{OrganizationServiceFault}">
        /// Thrown when the RowVersion is not provided or when it doesn't match the stored version.
        /// </exception>
        private void ValidateConcurrency(Entity target, XrmFakedContext ctx)
        {
            // Check if RowVersion was provided
            if (!target.Contains("versionnumber") && string.IsNullOrEmpty(target.RowVersion))
            {
                throw new FaultException<OrganizationServiceFault>(
                    new OrganizationServiceFault(),
                    "When ConcurrencyBehavior is set to IfRowVersionMatches, you must provide either the RowVersion property (as a string) or the versionnumber attribute (as a long) on the target entity.");
            }

            // Get stored entity
            ConcurrentDictionary<Guid, Entity> entityDict;
            Entity storedEntity;
            if (!ctx.Data.TryGetValue(target.LogicalName, out entityDict) ||
                !entityDict.TryGetValue(target.Id, out storedEntity))
            {
                return; // Let normal update handle missing entity
            }
            var storedVersion = storedEntity.Contains("versionnumber")
                ? storedEntity.GetAttributeValue<long>("versionnumber")
                : 0L;

            // Get provided version
            long providedVersion = 0L;
            if (!string.IsNullOrEmpty(target.RowVersion))
            {
                long.TryParse(target.RowVersion, out providedVersion);
            }
            else if (target.Contains("versionnumber"))
            {
                providedVersion = target.GetAttributeValue<long>("versionnumber");
            }

            if (storedVersion != providedVersion)
            {
                throw new FaultException<OrganizationServiceFault>(
                    new OrganizationServiceFault(),
                    "The version of the existing record doesn't match the RowVersion property provided.");
            }
        }

        /// <summary>
        /// Gets the type of organization request that this executor handles.
        /// </summary>
        /// <returns>
        /// The <see cref="Type"/> of <see cref="UpdateRequest"/>.
        /// </returns>
        public Type GetResponsibleRequestType()
        {
            return typeof(UpdateRequest);
        }
    }
}
