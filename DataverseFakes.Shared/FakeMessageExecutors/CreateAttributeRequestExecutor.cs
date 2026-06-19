using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using DataverseFakes.Extensions;
using System;
using System.Linq;

namespace DataverseFakes.FakeMessageExecutors
{
    /// <summary>
    /// Fake message executor for CreateAttributeRequest.
    /// Adds an <see cref="AttributeMetadata"/> to an existing entity's metadata in the faked context.
    /// </summary>
    public class CreateAttributeRequestExecutor : IFakeMessageExecutor
    {
        /// <summary>
        /// Determines whether this executor can handle the specified organization request.
        /// </summary>
        /// <param name="request">The organization request to evaluate.</param>
        /// <returns><c>true</c> if the request is a <see cref="CreateAttributeRequest"/>; otherwise, <c>false</c>.</returns>
        public bool CanExecute(OrganizationRequest request)
        {
            return request is CreateAttributeRequest;
        }

        /// <summary>
        /// Gets the type of organization request that this executor is responsible for handling.
        /// </summary>
        /// <returns>The <see cref="Type"/> of <see cref="CreateAttributeRequest"/>.</returns>
        public Type GetResponsibleRequestType()
        {
            return typeof(CreateAttributeRequest);
        }

        /// <summary>
        /// Executes the <see cref="CreateAttributeRequest"/> and adds the attribute to the entity metadata.
        /// </summary>
        /// <param name="request">The organization request to execute. Must be a <see cref="CreateAttributeRequest"/>.</param>
        /// <param name="ctx">The faked XRM context containing the entity metadata cache.</param>
        /// <returns>
        /// A <see cref="CreateAttributeResponse"/> with Results["AttributeId"] set to the new attribute's MetadataId.
        /// </returns>
        /// <exception cref="System.ServiceModel.FaultException{OrganizationServiceFault}">
        /// Thrown when EntityName is missing, Attribute is null, the entity does not exist,
        /// or an attribute with the same LogicalName already exists on the entity.
        /// </exception>
        public OrganizationResponse Execute(OrganizationRequest request, XrmFakedContext ctx)
        {
            var req = (CreateAttributeRequest)request;

            if (string.IsNullOrEmpty(req.EntityName))
            {
                FakeOrganizationServiceFault.Throw(ErrorCodes.InvalidArgument,
                    "EntityName is required for CreateAttributeRequest.");
            }

            if (req.Attribute == null)
            {
                FakeOrganizationServiceFault.Throw(ErrorCodes.InvalidArgument,
                    "Attribute is required for CreateAttributeRequest.");
            }

            if (!ctx.EntityMetadata.ContainsKey(req.EntityName))
            {
                FakeOrganizationServiceFault.Throw(ErrorCodes.ObjectDoesNotExist,
                    $"Entity '{req.EntityName}' does not exist in the metadata cache.");
            }

            var entityMetadata = ctx.GetEntityMetadataByName(req.EntityName);

            // Check for duplicate attribute
            if (entityMetadata.Attributes != null
                && entityMetadata.Attributes.Any(a => a.LogicalName == req.Attribute.LogicalName))
            {
                FakeOrganizationServiceFault.Throw(ErrorCodes.DuplicateName,
                    $"An attribute with logical name '{req.Attribute.LogicalName}' already exists on entity '{req.EntityName}'.");
            }

            // Assign a MetadataId if not set
            if (!req.Attribute.MetadataId.HasValue || req.Attribute.MetadataId == Guid.Empty)
            {
                req.Attribute.MetadataId = Guid.NewGuid();
            }

            var attributeId = req.Attribute.MetadataId.Value;

            entityMetadata.SetAttribute(req.Attribute);
            ctx.SetEntityMetadata(entityMetadata);

            var response = new CreateAttributeResponse
            {
                Results = new ParameterCollection
                {
                    { "AttributeId", attributeId }
                }
            };

            return response;
        }
    }
}
