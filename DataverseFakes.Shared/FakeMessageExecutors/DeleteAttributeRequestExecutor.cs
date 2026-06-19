using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using DataverseFakes.Extensions;
using System;
using System.Linq;

namespace DataverseFakes.FakeMessageExecutors
{
    /// <summary>
    /// Fake message executor for DeleteAttributeRequest.
    /// Removes an attribute from an entity's metadata in the faked context.
    /// </summary>
    public class DeleteAttributeRequestExecutor : IFakeMessageExecutor
    {
        /// <summary>
        /// Determines whether this executor can handle the specified organization request.
        /// </summary>
        /// <param name="request">The organization request to evaluate.</param>
        /// <returns><c>true</c> if the request is a <see cref="DeleteAttributeRequest"/>; otherwise, <c>false</c>.</returns>
        public bool CanExecute(OrganizationRequest request)
        {
            return request is DeleteAttributeRequest;
        }

        /// <summary>
        /// Gets the type of organization request that this executor is responsible for handling.
        /// </summary>
        /// <returns>The <see cref="Type"/> of <see cref="DeleteAttributeRequest"/>.</returns>
        public Type GetResponsibleRequestType()
        {
            return typeof(DeleteAttributeRequest);
        }

        /// <summary>
        /// Executes the <see cref="DeleteAttributeRequest"/> and removes the attribute from the entity metadata.
        /// </summary>
        /// <param name="request">The organization request to execute. Must be a <see cref="DeleteAttributeRequest"/>.</param>
        /// <param name="ctx">The faked XRM context containing the entity metadata cache.</param>
        /// <returns>A <see cref="DeleteAttributeResponse"/> indicating success.</returns>
        /// <exception cref="System.ServiceModel.FaultException{OrganizationServiceFault}">
        /// Thrown when EntityLogicalName or LogicalName are missing, when the entity does not exist,
        /// or when the attribute does not exist on the entity.
        /// </exception>
        public OrganizationResponse Execute(OrganizationRequest request, XrmFakedContext ctx)
        {
            var req = (DeleteAttributeRequest)request;

            if (string.IsNullOrEmpty(req.EntityLogicalName))
            {
                FakeOrganizationServiceFault.Throw(ErrorCodes.InvalidArgument,
                    "EntityLogicalName is required for DeleteAttributeRequest.");
            }

            if (string.IsNullOrEmpty(req.LogicalName))
            {
                FakeOrganizationServiceFault.Throw(ErrorCodes.InvalidArgument,
                    "LogicalName is required for DeleteAttributeRequest.");
            }

            if (!ctx.EntityMetadata.ContainsKey(req.EntityLogicalName))
            {
                FakeOrganizationServiceFault.Throw(ErrorCodes.ObjectDoesNotExist,
                    $"Entity '{req.EntityLogicalName}' does not exist in the metadata cache.");
            }

            var entityMetadata = ctx.GetEntityMetadataByName(req.EntityLogicalName);

            if (entityMetadata.Attributes == null
                || !entityMetadata.Attributes.Any(a => a.LogicalName == req.LogicalName))
            {
                FakeOrganizationServiceFault.Throw(ErrorCodes.ObjectDoesNotExist,
                    $"Attribute '{req.LogicalName}' does not exist on entity '{req.EntityLogicalName}'.");
            }

            var remainingAttributes = entityMetadata.Attributes
                .Where(a => a.LogicalName != req.LogicalName)
                .ToArray();

            entityMetadata.SetAttributeCollection(remainingAttributes);
            ctx.SetEntityMetadata(entityMetadata);

            return new DeleteAttributeResponse();
        }
    }
}
