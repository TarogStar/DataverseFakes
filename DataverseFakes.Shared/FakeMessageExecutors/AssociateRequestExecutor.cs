using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using System;
using System.Linq;

namespace FakeXrmEasy.FakeMessageExecutors
{
    /// <summary>
    /// Fake message executor that handles AssociateRequest messages for creating relationships between CRM entities.
    /// Supports both Many-to-Many (N:N) relationships by creating intersect entity records and
    /// One-to-Many (1:N) relationships by updating the lookup field on the related entity.
    /// </summary>
    public class AssociateRequestExecutor : IFakeMessageExecutor
    {
        /// <summary>
        /// Determines whether this executor can handle the specified organization request.
        /// </summary>
        /// <param name="request">The organization request to evaluate.</param>
        /// <returns>True if the request is an AssociateRequest; otherwise, false.</returns>
        public bool CanExecute(OrganizationRequest request)
        {
            return request is AssociateRequest;
        }

        /// <summary>
        /// Executes the AssociateRequest to create an association between CRM entities.
        /// For Many-to-Many relationships, creates a record in the intersect entity.
        /// For One-to-Many relationships, updates the lookup field on the related entity.
        /// </summary>
        /// <param name="request">The AssociateRequest containing the target entity, related entities, and relationship information.</param>
        /// <param name="ctx">The XrmFakedContext providing the in-memory CRM context and organization service.</param>
        /// <returns>An AssociateResponse indicating successful completion of the association.</returns>
        /// <exception cref="Exception">Thrown when the request is not an AssociateRequest, the relationship does not exist in the metadata cache, or the target/related entities do not exist.</exception>
        public OrganizationResponse Execute(OrganizationRequest request, XrmFakedContext ctx)
        {
            var associateRequest = request as AssociateRequest;
            var service = ctx.GetOrganizationService();

            if (associateRequest == null)
            {
                throw new Exception("Only associate request can be processed!");
            }

            var associateRelationship = associateRequest.Relationship;
            var relationShipName = associateRelationship.SchemaName;
            var fakeRelationShip = ctx.GetRelationship(relationShipName);

            if (fakeRelationShip == null)
            {
                throw new Exception(string.Format("Relationship {0} does not exist in the metadata cache", relationShipName));
            }

            if (associateRequest.Target == null)
            {
                throw new Exception("Association without target is invalid!");
            }

            foreach (var relatedEntityReference in associateRequest.RelatedEntities)
            {
                // Try to resolve alternate keys, fallback to .Id if not found
                var targetId = associateRequest.Target.Id;
                if (associateRequest.Target.KeyAttributes != null && associateRequest.Target.KeyAttributes.Count > 0)
                {
                    var resolvedId = ctx.GetRecordUniqueId(associateRequest.Target, validate: false);
                    if (resolvedId != Guid.Empty)
                    {
                        targetId = resolvedId;
                    }
                }

                var relatedEntityId = relatedEntityReference.Id;
                if (relatedEntityReference.KeyAttributes != null && relatedEntityReference.KeyAttributes.Count > 0)
                {
                    var resolvedId = ctx.GetRecordUniqueId(relatedEntityReference, validate: false);
                    if (resolvedId != Guid.Empty)
                    {
                        relatedEntityId = resolvedId;
                    }
                }

                if (fakeRelationShip.RelationshipType == XrmFakedRelationship.enmFakeRelationshipType.ManyToMany)
                {
                    var isFrom1to2 = associateRequest.Target.LogicalName == fakeRelationShip.Entity1LogicalName
                                         || relatedEntityReference.LogicalName != fakeRelationShip.Entity1LogicalName
                                         || String.IsNullOrWhiteSpace(associateRequest.Target.LogicalName);
                    var fromAttribute = isFrom1to2 ? fakeRelationShip.Entity1Attribute : fakeRelationShip.Entity2Attribute;
                    var toAttribute = isFrom1to2 ? fakeRelationShip.Entity2Attribute : fakeRelationShip.Entity1Attribute;
                    var fromEntityName = isFrom1to2 ? fakeRelationShip.Entity1LogicalName : fakeRelationShip.Entity2LogicalName;
                    var toEntityName = isFrom1to2 ? fakeRelationShip.Entity2LogicalName : fakeRelationShip.Entity1LogicalName;

                    //Check records exist
                    var targetExists = ctx.CreateQuery(fromEntityName)
                                                .Where(e => e.Id == targetId)
                                                .FirstOrDefault() != null;

                    if (!targetExists)
                    {
                        throw new Exception(string.Format("{0} with Id {1} doesn't exist", fromEntityName, targetId.ToString()));
                    }

                    var relatedExists = ctx.CreateQuery(toEntityName)
                                                .Where(e => e.Id == relatedEntityId)
                                                .FirstOrDefault() != null;

                    if (!relatedExists)
                    {
                        throw new Exception(string.Format("{0} with Id {1} doesn't exist", toEntityName, relatedEntityId.ToString()));
                    }

                    var association = new Entity(fakeRelationShip.IntersectEntity)
                    {
                        Attributes = new AttributeCollection
                        {
                            { fromAttribute, targetId },
                            { toAttribute, relatedEntityId }
                        }
                    };

                    service.Create(association);
                }
                else
                {
                    //One to many
                    //Get entity to update
                    var entityToUpdate = new Entity(relatedEntityReference.LogicalName)
                    {
                        Id = relatedEntityId
                    };

                    entityToUpdate[fakeRelationShip.Entity2Attribute] = associateRequest.Target;
                    service.Update(entityToUpdate);
                }
            }

            return new AssociateResponse();
        }

        /// <summary>
        /// Gets the type of organization request that this executor is responsible for handling.
        /// </summary>
        /// <returns>The Type of AssociateRequest.</returns>
        public Type GetResponsibleRequestType()
        {
            return typeof(AssociateRequest);
        }
    }
}