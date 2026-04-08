using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using FakeXrmEasy.Extensions;
using System;

namespace FakeXrmEasy.FakeMessageExecutors
{
    /// <summary>
    /// Fake message executor that handles <see cref="CreateEntityRequest"/> messages.
    /// Creates a new entity definition in the faked CRM context's metadata cache.
    /// </summary>
    public class CreateEntityRequestExecutor : IFakeMessageExecutor
    {
        /// <summary>
        /// Determines whether this executor can handle the specified organization request.
        /// </summary>
        /// <param name="request">The organization request to evaluate.</param>
        /// <returns><c>true</c> if the request is a <see cref="CreateEntityRequest"/>; otherwise, <c>false</c>.</returns>
        public bool CanExecute(OrganizationRequest request)
        {
            return request is CreateEntityRequest;
        }

        /// <summary>
        /// Executes the <see cref="CreateEntityRequest"/> and creates the entity in the metadata cache.
        /// </summary>
        /// <param name="request">The organization request to execute. Must be a <see cref="CreateEntityRequest"/>.</param>
        /// <param name="ctx">The faked XRM context containing the entity metadata cache.</param>
        /// <returns>
        /// A <see cref="CreateEntityResponse"/> containing the EntityId (MetadataId) of the newly created entity
        /// and an AttributeId for the primary attribute.
        /// </returns>
        /// <remarks>
        /// The EntityMetadata must have a valid LogicalName property. If the LogicalName is null or empty,
        /// or if an entity with the same LogicalName already exists, a FaultException will be thrown.
        /// The created entity metadata is stored in <see cref="XrmFakedContext.EntityMetadata"/>.
        /// </remarks>
        public OrganizationResponse Execute(OrganizationRequest request, XrmFakedContext ctx)
        {
            var createEntityRequest = (CreateEntityRequest)request;

            var entityMetadata = createEntityRequest.Entity;

            if (entityMetadata == null)
            {
                FakeOrganizationServiceFault.Throw(ErrorCodes.InvalidArgument, "Entity is required");
            }

            var logicalName = entityMetadata.LogicalName;

            if (string.IsNullOrEmpty(logicalName))
            {
                FakeOrganizationServiceFault.Throw(ErrorCodes.InvalidArgument, "Entity LogicalName is required");
            }

            if (ctx.EntityMetadata.ContainsKey(logicalName))
            {
                FakeOrganizationServiceFault.Throw(ErrorCodes.DuplicateName, string.Format("An EntityMetadata with the logical name {0} already exists.", logicalName));
            }

            // Assign a new MetadataId if not already set
            var metadataId = entityMetadata.MetadataId ?? Guid.NewGuid();
            entityMetadata.MetadataId = metadataId;

            // Generate an AttributeId for the primary attribute
            var attributeId = Guid.NewGuid();

            // Add the primary attribute to the entity metadata if provided
            var primaryAttribute = createEntityRequest.PrimaryAttribute;
            if (primaryAttribute != null)
            {
                primaryAttribute.MetadataId = attributeId;
                entityMetadata.SetAttribute(primaryAttribute);
            }

            // Store the entity metadata using SetEntityMetadata which handles copying
            ctx.SetEntityMetadata(entityMetadata);

            var response = new CreateEntityResponse()
            {
                Results = new ParameterCollection
                {
                    { "EntityId", metadataId },
                    { "AttributeId", attributeId }
                }
            };

            return response;
        }

        /// <summary>
        /// Gets the type of organization request that this executor is responsible for handling.
        /// </summary>
        /// <returns>The <see cref="Type"/> of <see cref="CreateEntityRequest"/>.</returns>
        public Type GetResponsibleRequestType()
        {
            return typeof(CreateEntityRequest);
        }
    }
}
