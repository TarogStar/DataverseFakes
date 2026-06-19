using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using DataverseFakes.Extensions;
using System;
using System.Linq;

namespace DataverseFakes.FakeMessageExecutors
{
    /// <summary>
    /// Fake message executor for UpdateAttributeRequest.
    /// Replaces an existing attribute's metadata on an entity in the faked context.
    /// </summary>
    public class UpdateAttributeRequestExecutor : IFakeMessageExecutor
    {
        /// <summary>
        /// Determines whether this executor can handle the specified organization request.
        /// </summary>
        /// <param name="request">The organization request to evaluate.</param>
        /// <returns><c>true</c> if the request is an <see cref="UpdateAttributeRequest"/>; otherwise, <c>false</c>.</returns>
        public bool CanExecute(OrganizationRequest request)
        {
            return request is UpdateAttributeRequest;
        }

        /// <summary>
        /// Gets the type of organization request that this executor is responsible for handling.
        /// </summary>
        /// <returns>The <see cref="Type"/> of <see cref="UpdateAttributeRequest"/>.</returns>
        public Type GetResponsibleRequestType()
        {
            return typeof(UpdateAttributeRequest);
        }

        /// <summary>
        /// Executes the <see cref="UpdateAttributeRequest"/> and replaces the attribute metadata on the entity.
        /// </summary>
        /// <param name="request">The organization request to execute. Must be an <see cref="UpdateAttributeRequest"/>.</param>
        /// <param name="ctx">The faked XRM context containing the entity metadata cache.</param>
        /// <returns>An <see cref="UpdateAttributeResponse"/> indicating success.</returns>
        /// <exception cref="System.ServiceModel.FaultException{OrganizationServiceFault}">
        /// Thrown when EntityName is missing, Attribute is null, the entity does not exist,
        /// or the attribute does not exist on the entity.
        /// </exception>
        public OrganizationResponse Execute(OrganizationRequest request, XrmFakedContext ctx)
        {
            var req = (UpdateAttributeRequest)request;

            if (string.IsNullOrEmpty(req.EntityName))
            {
                FakeOrganizationServiceFault.Throw(ErrorCodes.InvalidArgument,
                    "EntityName is required for UpdateAttributeRequest.");
            }

            if (req.Attribute == null)
            {
                FakeOrganizationServiceFault.Throw(ErrorCodes.InvalidArgument,
                    "Attribute is required for UpdateAttributeRequest.");
            }

            if (!ctx.EntityMetadata.ContainsKey(req.EntityName))
            {
                FakeOrganizationServiceFault.Throw(ErrorCodes.ObjectDoesNotExist,
                    $"Entity '{req.EntityName}' does not exist in the metadata cache.");
            }

            var entityMetadata = ctx.GetEntityMetadataByName(req.EntityName);

            // Validate that the attribute exists
            if (entityMetadata.Attributes == null
                || !entityMetadata.Attributes.Any(a => a.LogicalName == req.Attribute.LogicalName))
            {
                FakeOrganizationServiceFault.Throw(ErrorCodes.ObjectDoesNotExist,
                    $"Attribute '{req.Attribute.LogicalName}' does not exist on entity '{req.EntityName}'.");
            }

            entityMetadata.SetAttribute(req.Attribute);
            ctx.SetEntityMetadata(entityMetadata);

            return new UpdateAttributeResponse();
        }
    }
}
