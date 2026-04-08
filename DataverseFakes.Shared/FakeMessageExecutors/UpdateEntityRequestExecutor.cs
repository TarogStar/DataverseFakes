using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using System;

namespace FakeXrmEasy.FakeMessageExecutors
{
    /// <summary>
    /// Fake message executor that handles <see cref="UpdateEntityRequest"/> messages.
    /// Updates existing entity metadata in the faked CRM context's metadata cache.
    /// </summary>
    /// <remarks>
    /// This executor validates that the entity exists before updating, and merges
    /// the updated properties (DisplayName, Description, etc.) with the existing metadata.
    /// Use <see cref="XrmFakedContext.InitializeMetadata(EntityMetadata)"/> to initialize entity metadata
    /// before executing this request.
    /// </remarks>
    public class UpdateEntityRequestExecutor : IFakeMessageExecutor
    {
        /// <summary>
        /// Determines whether this executor can handle the specified organization request.
        /// </summary>
        /// <param name="request">The organization request to evaluate.</param>
        /// <returns><c>true</c> if the request is an <see cref="UpdateEntityRequest"/>; otherwise, <c>false</c>.</returns>
        public bool CanExecute(OrganizationRequest request)
        {
            return request is UpdateEntityRequest;
        }

        /// <summary>
        /// Executes the <see cref="UpdateEntityRequest"/> and updates the corresponding entity metadata.
        /// </summary>
        /// <param name="request">The organization request to execute. Must be an <see cref="UpdateEntityRequest"/>.</param>
        /// <param name="ctx">The faked XRM context containing the entity metadata cache.</param>
        /// <returns>
        /// An <see cref="UpdateEntityResponse"/> indicating successful update of the entity metadata.
        /// </returns>
        /// <exception cref="System.ServiceModel.FaultException{OrganizationServiceFault}">
        /// Thrown when the entity specified in the request does not exist in the metadata cache,
        /// or when the Entity property is null.
        /// </exception>
        /// <remarks>
        /// The executor merges the following properties from the request's Entity into the existing metadata:
        /// <list type="bullet">
        ///   <item><description>DisplayName - if provided in the request</description></item>
        ///   <item><description>DisplayCollectionName - if provided in the request</description></item>
        ///   <item><description>Description - if provided in the request</description></item>
        ///   <item><description>IsAuditEnabled - if provided in the request</description></item>
        ///   <item><description>IsValidForQueue - if provided in the request</description></item>
        ///   <item><description>IsConnectionsEnabled - if provided in the request</description></item>
        ///   <item><description>IsActivityParty - if provided in the request</description></item>
        ///   <item><description>IsMailMergeEnabled - if provided in the request</description></item>
        ///   <item><description>IsVisibleInMobile - if provided in the request</description></item>
        ///   <item><description>IsVisibleInMobileClient - if provided in the request</description></item>
        ///   <item><description>IconLargeName - if provided in the request</description></item>
        ///   <item><description>IconMediumName - if provided in the request</description></item>
        ///   <item><description>IconSmallName - if provided in the request</description></item>
        /// </list>
        /// The entity is identified by its LogicalName property.
        /// </remarks>
        public OrganizationResponse Execute(OrganizationRequest request, XrmFakedContext ctx)
        {
            var updateEntityRequest = (UpdateEntityRequest)request;

            if (updateEntityRequest.Entity == null)
            {
                FakeOrganizationServiceFault.Throw(ErrorCodes.InvalidArgument, "Entity property is required for UpdateEntityRequest.");
            }

            var entityToUpdate = updateEntityRequest.Entity;
            var logicalName = entityToUpdate.LogicalName;

            if (string.IsNullOrEmpty(logicalName))
            {
                FakeOrganizationServiceFault.Throw(ErrorCodes.InvalidArgument, "Entity LogicalName is required for UpdateEntityRequest.");
            }

            if (!ctx.EntityMetadata.ContainsKey(logicalName))
            {
                FakeOrganizationServiceFault.Throw(ErrorCodes.ObjectDoesNotExist, string.Format("An EntityMetadata with the name '{0}' does not exist.", logicalName));
            }

            var existingEntity = ctx.EntityMetadata[logicalName];

            // Merge updated properties
            if (entityToUpdate.DisplayName != null)
            {
                existingEntity.DisplayName = entityToUpdate.DisplayName;
            }

            if (entityToUpdate.DisplayCollectionName != null)
            {
                existingEntity.DisplayCollectionName = entityToUpdate.DisplayCollectionName;
            }

            if (entityToUpdate.Description != null)
            {
                existingEntity.Description = entityToUpdate.Description;
            }

            // Merge boolean managed properties if provided
            if (entityToUpdate.IsAuditEnabled != null)
            {
                existingEntity.IsAuditEnabled = entityToUpdate.IsAuditEnabled;
            }

            if (entityToUpdate.IsValidForQueue != null)
            {
                existingEntity.IsValidForQueue = entityToUpdate.IsValidForQueue;
            }

            if (entityToUpdate.IsConnectionsEnabled != null)
            {
                existingEntity.IsConnectionsEnabled = entityToUpdate.IsConnectionsEnabled;
            }

            if (entityToUpdate.IsActivityParty != null)
            {
                existingEntity.IsActivityParty = entityToUpdate.IsActivityParty;
            }

            if (entityToUpdate.IsMailMergeEnabled != null)
            {
                existingEntity.IsMailMergeEnabled = entityToUpdate.IsMailMergeEnabled;
            }

            if (entityToUpdate.IsVisibleInMobile != null)
            {
                existingEntity.IsVisibleInMobile = entityToUpdate.IsVisibleInMobile;
            }

            if (entityToUpdate.IsVisibleInMobileClient != null)
            {
                existingEntity.IsVisibleInMobileClient = entityToUpdate.IsVisibleInMobileClient;
            }

            // Merge icon names if provided
            if (!string.IsNullOrEmpty(entityToUpdate.IconLargeName))
            {
                existingEntity.IconLargeName = entityToUpdate.IconLargeName;
            }

            if (!string.IsNullOrEmpty(entityToUpdate.IconMediumName))
            {
                existingEntity.IconMediumName = entityToUpdate.IconMediumName;
            }

            if (!string.IsNullOrEmpty(entityToUpdate.IconSmallName))
            {
                existingEntity.IconSmallName = entityToUpdate.IconSmallName;
            }

            return new UpdateEntityResponse();
        }

        /// <summary>
        /// Gets the type of organization request that this executor is responsible for handling.
        /// </summary>
        /// <returns>The <see cref="Type"/> of <see cref="UpdateEntityRequest"/>.</returns>
        public Type GetResponsibleRequestType()
        {
            return typeof(UpdateEntityRequest);
        }
    }
}
