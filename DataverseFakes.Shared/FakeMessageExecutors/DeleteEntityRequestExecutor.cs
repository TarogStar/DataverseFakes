using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using System;

namespace FakeXrmEasy.FakeMessageExecutors
{
    /// <summary>
    /// Fake message executor that handles <see cref="DeleteEntityRequest"/> messages.
    /// Deletes entity metadata from the faked CRM context's metadata cache and optionally
    /// removes any associated entity data from the in-memory data store.
    /// </summary>
    /// <remarks>
    /// This executor validates that the entity exists in the metadata cache before deletion.
    /// When an entity is deleted, any records of that entity type stored in the context's
    /// Data dictionary are also removed.
    /// Use <see cref="XrmFakedContext.InitializeMetadata(Microsoft.Xrm.Sdk.Metadata.EntityMetadata)"/>
    /// to add entity metadata before testing deletion scenarios.
    /// </remarks>
    public class DeleteEntityRequestExecutor : IFakeMessageExecutor
    {
        /// <summary>
        /// Determines whether this executor can handle the specified organization request.
        /// </summary>
        /// <param name="request">The organization request to evaluate.</param>
        /// <returns><c>true</c> if the request is a <see cref="DeleteEntityRequest"/>; otherwise, <c>false</c>.</returns>
        public bool CanExecute(OrganizationRequest request)
        {
            return request is DeleteEntityRequest;
        }

        /// <summary>
        /// Executes the <see cref="DeleteEntityRequest"/> and removes the entity from the context.
        /// </summary>
        /// <param name="request">The organization request to execute. Must be a <see cref="DeleteEntityRequest"/>.</param>
        /// <param name="ctx">The faked XRM context containing the entity metadata cache.</param>
        /// <returns>
        /// A <see cref="DeleteEntityResponse"/> indicating successful deletion.
        /// </returns>
        /// <exception cref="System.ServiceModel.FaultException{OrganizationServiceFault}">
        /// Thrown when:
        /// <list type="bullet">
        /// <item><description>The LogicalName property is null or empty.</description></item>
        /// <item><description>The specified entity does not exist in the metadata cache.</description></item>
        /// </list>
        /// </exception>
        /// <remarks>
        /// The entity must be identified by LogicalName. When the entity is deleted:
        /// <list type="bullet">
        /// <item><description>The entity metadata is removed from the EntityMetadata dictionary.</description></item>
        /// <item><description>Any entity records of this type are removed from the Data dictionary.</description></item>
        /// </list>
        /// </remarks>
        public OrganizationResponse Execute(OrganizationRequest request, XrmFakedContext ctx)
        {
            var deleteEntityRequest = (DeleteEntityRequest)request;

            var logicalName = deleteEntityRequest.LogicalName;

            // Validate that the LogicalName parameter is provided
            if (string.IsNullOrEmpty(logicalName))
            {
                FakeOrganizationServiceFault.Throw(ErrorCodes.InvalidArgument, "LogicalName is required to delete an entity.");
            }

            // Validate that the entity exists in the metadata cache
            if (!ctx.EntityMetadata.ContainsKey(logicalName))
            {
                FakeOrganizationServiceFault.Throw(ErrorCodes.ObjectDoesNotExist, string.Format("An EntityMetadata with the logical name '{0}' does not exist.", logicalName));
            }

            // Remove the entity metadata from the context
            ctx.EntityMetadata.Remove(logicalName);

            // Also remove any entity data for this entity type from the Data dictionary - thread-safe removal
            ctx.Data.TryRemove(logicalName, out _);

            return new DeleteEntityResponse();
        }

        /// <summary>
        /// Gets the type of organization request that this executor is responsible for handling.
        /// </summary>
        /// <returns>The <see cref="Type"/> of <see cref="DeleteEntityRequest"/>.</returns>
        public Type GetResponsibleRequestType()
        {
            return typeof(DeleteEntityRequest);
        }
    }
}
