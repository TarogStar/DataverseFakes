#if FAKE_XRM_EASY_2013 || FAKE_XRM_EASY_2015 || FAKE_XRM_EASY_2016 || FAKE_XRM_EASY_365 || FAKE_XRM_EASY_9
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Sdk;
using System.ServiceModel;
using Microsoft.Crm.Sdk.Messages;

namespace DataverseFakes.FakeMessageExecutors
{
    /// <summary>
    /// Fake message executor for RemoveUserFromRecordTeamRequest
    /// </summary>
    public class RemoveUserFromRecordTeamRequestExecutor : IFakeMessageExecutor
    {
        /// <summary>
        /// Determines whether this executor can execute the given request
        /// </summary>
        /// <param name="request">The organization request</param>
        /// <returns>True if the request is RemoveUserFromRecordTeamRequest</returns>
        public bool CanExecute(OrganizationRequest request)
        {
            return request is RemoveUserFromRecordTeamRequest;
        }

        /// <summary>
        /// Executes the RemoveUserFromRecordTeamRequest
        /// </summary>
        /// <param name="request">The organization request</param>
        /// <param name="ctx">The faked context</param>
        /// <returns>RemoveUserFromRecordTeamResponse</returns>
        public OrganizationResponse Execute(OrganizationRequest request, XrmFakedContext ctx)
        {
            RemoveUserFromRecordTeamRequest remReq = (RemoveUserFromRecordTeamRequest)request;

            EntityReference target = remReq.Record;
            Guid systemuserId = remReq.SystemUserId;
            Guid teamTemplateId = remReq.TeamTemplateId;

            if (target == null)
            {
                throw new FaultException<OrganizationServiceFault>(new OrganizationServiceFault(), "Can not remove from team without target");
            }

            if (systemuserId == Guid.Empty)
            {
                throw new FaultException<OrganizationServiceFault>(new OrganizationServiceFault(), "Can not remove from team without user");
            }

            if (teamTemplateId == Guid.Empty)
            {
                throw new FaultException<OrganizationServiceFault>(new OrganizationServiceFault(), "Can not remove from team without team");
            }

            Entity teamTemplate = ctx.CreateQuery("teamtemplate").FirstOrDefault(p => p.Id == teamTemplateId);
            if (teamTemplate == null)
            {
                throw new FaultException<OrganizationServiceFault>(new OrganizationServiceFault(), "Team template with id=" + teamTemplateId + " does not exist");
            }

            Entity user = ctx.CreateQuery("systemuser").FirstOrDefault(p => p.Id == systemuserId);
            if (user == null)
            {
                throw new FaultException<OrganizationServiceFault>(new OrganizationServiceFault(), "User with id=" + systemuserId + " does not exist");
            }

            IOrganizationService service = ctx.GetOrganizationService();

            Entity team = AddUserToRecordTeamRequestExecutor.FindRecordTeam(ctx, target, teamTemplateId);
            var memberships = team == null
                ? new List<Entity>()
                : AddUserToRecordTeamRequestExecutor.FindMemberships(ctx, team.Id, systemuserId);

            foreach (var tm in memberships)
            {
                service.Delete(tm.LogicalName, tm.Id);
            }

            // Only revoke access that came from this team: a user who wasn't on it may still hold
            // a direct share of the record. The team's own share (principalobjectaccess) stays, since
            // the team and its other members keep their access to the record.
            // Limitation: AccessRightsRepository keeps one entry per principal, so a user who was BOTH
            // directly shared and on the team loses the direct share here too.
            if (memberships.Count > 0)
            {
                ctx.AccessRightsRepository.RevokeAccessTo(target, user.ToEntityReference());
            }

            return new RemoveUserFromRecordTeamResponse
            {
                ResponseName = "RemoveUserFromRecordTeam"
            };
        }

        /// <summary>
        /// Gets the type of request this executor is responsible for
        /// </summary>
        /// <returns>The type of RemoveUserFromRecordTeamRequest</returns>
        public Type GetResponsibleRequestType()
        {
            return typeof(RemoveUserFromRecordTeamRequest);
        }
    }
}
#endif
