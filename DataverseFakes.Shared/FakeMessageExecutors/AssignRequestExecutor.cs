using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ServiceModel;

namespace FakeXrmEasy.FakeMessageExecutors
{
    /// <summary>
    /// Fake message executor that handles AssignRequest messages for changing the owner of CRM entities.
    /// Updates the ownerid attribute along with either owninguser or owningteam depending on the assignee type.
    /// </summary>
    public class AssignRequestExecutor : IFakeMessageExecutor
    {
        /// <summary>
        /// Determines whether this executor can handle the specified organization request.
        /// </summary>
        /// <param name="request">The organization request to evaluate.</param>
        /// <returns>True if the request is an AssignRequest; otherwise, false.</returns>
        public bool CanExecute(OrganizationRequest request)
        {
            return request is AssignRequest;
        }

        /// <summary>
        /// Executes the AssignRequest to change the owner of a CRM entity.
        /// Sets the ownerid attribute to the new assignee and updates owninguser or owningteam
        /// based on whether the assignee is a systemuser or team. Also updates owningbusinessunit
        /// to match the new owner's business unit.
        /// </summary>
        /// <param name="request">The AssignRequest containing the Target entity reference and the Assignee entity reference.</param>
        /// <param name="ctx">The XrmFakedContext providing the in-memory CRM context and organization service.</param>
        /// <returns>An AssignResponse indicating successful completion of the assignment.</returns>
        /// <exception cref="FaultException{OrganizationServiceFault}">Thrown when the target or assignee is null.</exception>
        public OrganizationResponse Execute(OrganizationRequest request, XrmFakedContext ctx)
        {
            var assignRequest = (AssignRequest)request;

            var target = assignRequest.Target;
            var assignee = assignRequest.Assignee;

            if (target == null)
            {
                throw new FaultException<OrganizationServiceFault>(new OrganizationServiceFault(), "Can not assign without target");
            }

            if (assignee == null)
            {
                throw new FaultException<OrganizationServiceFault>(new OrganizationServiceFault(), "Can not assign without assignee");
            }

            var service = ctx.GetOrganizationService();

            KeyValuePair<string, object> owningX = new KeyValuePair<string, object>();
            if (assignee.LogicalName == "systemuser")
                owningX = new KeyValuePair<string, object>("owninguser", assignee);
            else if (assignee.LogicalName == "team")
                owningX = new KeyValuePair<string, object>("owningteam", assignee);

            // Get the assignee's business unit to update owningbusinessunit
            EntityReference owningBusinessUnit = null;
            ConcurrentDictionary<Guid, Entity> assigneeEntityDict;
            Entity assigneeEntity;
            if (ctx.Data.TryGetValue(assignee.LogicalName, out assigneeEntityDict) &&
                assigneeEntityDict.TryGetValue(assignee.Id, out assigneeEntity))
            {
                owningBusinessUnit = assigneeEntity.GetAttributeValue<EntityReference>("businessunitid");
            }

            var assignment = new Entity
            {
                LogicalName = target.LogicalName,
                Id = target.Id,
                Attributes = new AttributeCollection
                {
                    { "ownerid", assignee },
                    owningX
                }
            };

            // Set owningbusinessunit if we found the assignee's business unit.
            // Note: owningBusinessUnit may be null if the assignee entity doesn't have a businessunitid
            // attribute set (e.g., the user or team entity wasn't fully initialized in the test data).
            if (owningBusinessUnit != null)
            {
                assignment["owningbusinessunit"] = owningBusinessUnit;
            }

            service.Update(assignment);

            return new AssignResponse();
        }

        /// <summary>
        /// Gets the type of organization request that this executor is responsible for handling.
        /// </summary>
        /// <returns>The Type of AssignRequest.</returns>
        public Type GetResponsibleRequestType()
        {
            return typeof(AssignRequest);
        }
    }
}