using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Linq;

namespace DataverseFakes.FakeMessageExecutors
{
    /// <summary>
    /// Fake message executor for RetrieveAllEntitiesRequest.
    /// Returns all entity metadata stored in the context as a <see cref="RetrieveAllEntitiesResponse"/>.
    /// </summary>
    public class RetrieveAllEntitiesRequestExecutor : IFakeMessageExecutor
    {
        /// <summary>
        /// Determines whether this executor can handle the specified organization request.
        /// </summary>
        /// <param name="request">The organization request to evaluate.</param>
        /// <returns><c>true</c> if the request is a <see cref="RetrieveAllEntitiesRequest"/>; otherwise, <c>false</c>.</returns>
        public bool CanExecute(OrganizationRequest request)
        {
            return request is RetrieveAllEntitiesRequest;
        }

        /// <summary>
        /// Gets the type of organization request that this executor is responsible for handling.
        /// </summary>
        /// <returns>The <see cref="Type"/> of <see cref="RetrieveAllEntitiesRequest"/>.</returns>
        public Type GetResponsibleRequestType()
        {
            return typeof(RetrieveAllEntitiesRequest);
        }

        /// <summary>
        /// Executes the <see cref="RetrieveAllEntitiesRequest"/> and returns all entity metadata.
        /// </summary>
        /// <param name="request">The organization request to execute. Must be a <see cref="RetrieveAllEntitiesRequest"/>.</param>
        /// <param name="ctx">The faked XRM context containing the entity metadata cache.</param>
        /// <returns>
        /// A <see cref="RetrieveAllEntitiesResponse"/> with Results["EntityMetadata"] = <see cref="EntityMetadata"/>[].
        /// </returns>
        public OrganizationResponse Execute(OrganizationRequest request, XrmFakedContext ctx)
        {
            var allEntities = ctx.CreateMetadataQuery().ToArray();

            var response = new RetrieveAllEntitiesResponse
            {
                Results = new ParameterCollection
                {
                    { "EntityMetadata", allEntities }
                }
            };

            return response;
        }
    }
}
