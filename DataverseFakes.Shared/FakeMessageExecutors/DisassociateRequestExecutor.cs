using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Linq;

namespace FakeXrmEasy.FakeMessageExecutors
{
    /// <summary>
    /// Fake message executor that handles DisassociateRequest messages for removing relationships between CRM entities.
    /// Removes Many-to-Many (N:N) relationship records by deleting the corresponding intersect entity records.
    /// </summary>
    public class DisassociateRequestExecutor : IFakeMessageExecutor
    {
        /// <summary>
        /// Determines whether this executor can handle the specified organization request.
        /// </summary>
        /// <param name="request">The organization request to evaluate.</param>
        /// <returns>True if the request is a DisassociateRequest; otherwise, false.</returns>
        public bool CanExecute(OrganizationRequest request)
        {
            return request is DisassociateRequest;
        }

        /// <summary>
        /// Executes the DisassociateRequest to remove an association between CRM entities.
        /// Queries the intersect entity to find matching relationship records and deletes them.
        /// </summary>
        /// <param name="request">The DisassociateRequest containing the target entity, related entities, and relationship information.</param>
        /// <param name="ctx">The XrmFakedContext providing the in-memory CRM context and organization service.</param>
        /// <returns>A DisassociateResponse indicating successful completion of the disassociation.</returns>
        /// <exception cref="Exception">Thrown when the request is not a DisassociateRequest, the relationship does not exist in the metadata cache, or the target is null.</exception>
        public OrganizationResponse Execute(OrganizationRequest request, XrmFakedContext ctx)
        {
            var disassociateRequest = request as DisassociateRequest;
            var service = ctx.GetOrganizationService();

            if (disassociateRequest == null)
            {
                throw new Exception("Only disassociate request can be processed!");
            }

            var relationShipName = disassociateRequest.Relationship.SchemaName;
            var relationShip = ctx.GetRelationship(relationShipName);

            if (relationShip == null)
            {
                throw new Exception(string.Format("Relationship {0} does not exist in the metadata cache", relationShipName));
            }

            if (disassociateRequest.Target == null)
            {
                throw new Exception("Disassociation without target is invalid!");
            }

            foreach (var relatedEntity in disassociateRequest.RelatedEntities)
            {
                // Try to resolve alternate keys, fallback to .Id if not found
                var targetId = disassociateRequest.Target.Id;
                if (disassociateRequest.Target.KeyAttributes != null && disassociateRequest.Target.KeyAttributes.Count > 0)
                {
                    var resolvedId = ctx.GetRecordUniqueId(disassociateRequest.Target, validate: false);
                    if (resolvedId != Guid.Empty)
                    {
                        targetId = resolvedId;
                    }
                }

                var relatedEntityId = relatedEntity.Id;
                if (relatedEntity.KeyAttributes != null && relatedEntity.KeyAttributes.Count > 0)
                {
                    var resolvedId = ctx.GetRecordUniqueId(relatedEntity, validate: false);
                    if (resolvedId != Guid.Empty)
                    {
                        relatedEntityId = resolvedId;
                    }
                }

                var isFrom1to2 = disassociateRequest.Target.LogicalName == relationShip.Entity1LogicalName
                                      || relatedEntity.LogicalName != relationShip.Entity1LogicalName
                                      || String.IsNullOrWhiteSpace(disassociateRequest.Target.LogicalName);
                var fromAttribute = isFrom1to2 ? relationShip.Entity1Attribute : relationShip.Entity2Attribute;
                var toAttribute = isFrom1to2 ? relationShip.Entity2Attribute : relationShip.Entity1Attribute;

                var query = new QueryExpression(relationShip.IntersectEntity)
                {
                    ColumnSet = new ColumnSet(true),
                    Criteria = new FilterExpression(LogicalOperator.And)
                };

                query.Criteria.AddCondition(new ConditionExpression(fromAttribute,
                    ConditionOperator.Equal, targetId));
                query.Criteria.AddCondition(new ConditionExpression(toAttribute,
                    ConditionOperator.Equal, relatedEntityId));

                var results = service.RetrieveMultiple(query);

                if (results.Entities.Count == 1)
                {
                    service.Delete(relationShip.IntersectEntity, results.Entities.First().Id);
                }
            }

            return new DisassociateResponse();
        }

        /// <summary>
        /// Gets the type of organization request that this executor is responsible for handling.
        /// </summary>
        /// <returns>The Type of DisassociateRequest.</returns>
        public Type GetResponsibleRequestType()
        {
            return typeof(DisassociateRequest);
        }
    }
}