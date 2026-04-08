using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using System;
using System.Linq;

namespace FakeXrmEasy.FakeMessageExecutors
{
    /// <summary>
    /// Fake message executor that handles <see cref="RetrieveRelationshipRequest"/> messages.
    /// Retrieves relationship metadata (one-to-many or many-to-many) from the faked CRM context's relationship cache.
    /// </summary>
    public class RetrieveRelationshipRequestExecutor : IFakeMessageExecutor
    {
        /// <summary>
        /// Determines whether this executor can handle the specified organization request.
        /// </summary>
        /// <param name="request">The organization request to evaluate.</param>
        /// <returns><c>true</c> if the request is a <see cref="RetrieveRelationshipRequest"/>; otherwise, <c>false</c>.</returns>
        public bool CanExecute(OrganizationRequest request)
        {
            return request is RetrieveRelationshipRequest;
        }

        /// <summary>
        /// Executes the <see cref="RetrieveRelationshipRequest"/> and returns the corresponding relationship metadata.
        /// </summary>
        /// <param name="request">The organization request to execute. Must be a <see cref="RetrieveRelationshipRequest"/>.</param>
        /// <param name="ctx">The faked XRM context containing the relationship metadata cache.</param>
        /// <returns>
        /// A <see cref="RetrieveRelationshipResponse"/> containing either a <see cref="Microsoft.Xrm.Sdk.Metadata.ManyToManyRelationshipMetadata"/>
        /// or <see cref="Microsoft.Xrm.Sdk.Metadata.OneToManyRelationshipMetadata"/> depending on the relationship type.
        /// </returns>
        /// <exception cref="Exception">
        /// Thrown when the request is not a <see cref="RetrieveRelationshipRequest"/>
        /// or when the specified relationship is not found in the metadata cache.
        /// </exception>
        public OrganizationResponse Execute(OrganizationRequest request, XrmFakedContext ctx)
        {
            var retrieveRequest = request as RetrieveRelationshipRequest;
            if (retrieveRequest == null)
            {
                throw new Exception("Only RetrieveRelationshipRequest can be processed!");
            }

            var service = ctx.GetOrganizationService();
            var fakeRelationShip = ctx.GetRelationship(retrieveRequest.Name);
            if (fakeRelationShip == null)
            {
                throw new Exception(string.Format("Relationship {0} does not exist in the metadata cache", retrieveRequest.Name));
            }


      var response = new RetrieveRelationshipResponse
      {
        Results = new ParameterCollection()
      };
      response.Results.Add("RelationshipMetadata", GetRelationshipMetadata(fakeRelationShip));
            response.ResponseName = "RetrieveRelationship";

            return response;
        }

        /// <summary>
        /// Converts a <see cref="XrmFakedRelationship"/> to the appropriate CRM SDK relationship metadata type.
        /// </summary>
        /// <param name="fakeRelationShip">The faked relationship to convert.</param>
        /// <returns>
        /// A <see cref="Microsoft.Xrm.Sdk.Metadata.ManyToManyRelationshipMetadata"/> for N:N relationships,
        /// or a <see cref="Microsoft.Xrm.Sdk.Metadata.OneToManyRelationshipMetadata"/> for 1:N relationships.
        /// </returns>
        private static object GetRelationshipMetadata(XrmFakedRelationship fakeRelationShip)
        {
            if (fakeRelationShip.RelationshipType == XrmFakedRelationship.enmFakeRelationshipType.ManyToMany)
            {
        var mtm = new Microsoft.Xrm.Sdk.Metadata.ManyToManyRelationshipMetadata
        {
          Entity1LogicalName = fakeRelationShip.Entity1LogicalName,
          Entity1IntersectAttribute = fakeRelationShip.Entity1Attribute,
          Entity2LogicalName = fakeRelationShip.Entity2LogicalName,
          Entity2IntersectAttribute = fakeRelationShip.Entity2Attribute,
          SchemaName = fakeRelationShip.IntersectEntity,
          IntersectEntityName = fakeRelationShip.IntersectEntity.ToLower()
        };
        return mtm;
            } else {

        var otm = new Microsoft.Xrm.Sdk.Metadata.OneToManyRelationshipMetadata
        {
#if FAKE_XRM_EASY_2016 || FAKE_XRM_EASY_365 || FAKE_XRM_EASY_9
          ReferencedEntityNavigationPropertyName = fakeRelationShip.IntersectEntity,
#endif
          ReferencingAttribute = fakeRelationShip.Entity1Attribute,
          ReferencingEntity = fakeRelationShip.Entity1LogicalName,
          ReferencedAttribute = fakeRelationShip.Entity2Attribute,
          ReferencedEntity = fakeRelationShip.Entity2LogicalName,
          SchemaName = fakeRelationShip.IntersectEntity
        };
        return otm;
            }
        }

        /// <summary>
        /// Gets the type of organization request that this executor is responsible for handling.
        /// </summary>
        /// <returns>The <see cref="Type"/> of <see cref="RetrieveRelationshipRequest"/>.</returns>
        public Type GetResponsibleRequestType()
        {
            return typeof(RetrieveRelationshipRequest);
        }
    }
}