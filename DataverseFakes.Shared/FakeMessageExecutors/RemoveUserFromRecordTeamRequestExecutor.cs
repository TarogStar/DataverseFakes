#if FAKE_XRM_EASY_2013 || FAKE_XRM_EASY_2015 || FAKE_XRM_EASY_2016 || FAKE_XRM_EASY_365 || FAKE_XRM_EASY_9
using System;
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

            var response = new RemoveUserFromRecordTeamResponse
            {
                ResponseName = "RemoveUserFromRecordTeam"
            };

            // No access team for this record yet: Dataverse succeeds and returns an empty AccessTeamId.
            Entity team = AddUserToRecordTeamRequestExecutor.FindRecordTeam(ctx, target, teamTemplateId);
            response.Results["AccessTeamId"] = team?.Id ?? Guid.Empty;
            if (team == null)
            {
                return response;
            }

            // The user's access came from the team, so removing the membership is enough; any direct
            // share the user holds is a separate principalobjectaccess row and is left alone.
            var memberships = AddUserToRecordTeamRequestExecutor.FindMemberships(ctx, team.Id, systemuserId);
            foreach (var tm in memberships)
            {
                service.Delete(tm.LogicalName, tm.Id);
            }

            // When the last member leaves a system-managed access team, Dataverse deletes the team and its
            // share of the record. Removing a non-member changes nothing, and teams that weren't created by
            // AddUserToRecordTeam (not systemmanaged, e.g. seeded by a test) are never auto-deleted.
            var lastMemberLeft = memberships.Count > 0 &&
                !ctx.CreateQuery("teammembership").AsEnumerable().Any(m => m.GetAttributeValue<Guid>("teamid") == team.Id);
            if (lastMemberLeft && team.GetAttributeValue<bool>("systemmanaged"))
            {
                var teamShares = ctx.CreateQuery("principalobjectaccess").AsEnumerable()
                    .Where(p => p.GetAttributeValue<Guid>("objectid") == target.Id &&
                                p.GetAttributeValue<Guid>("principalid") == team.Id)
                    .ToList();
                foreach (var poa in teamShares)
                {
                    service.Delete(poa.LogicalName, poa.Id);
                }

                ctx.AccessRightsRepository.RevokeAccessTo(target, team.ToEntityReference());
                service.Delete(team.LogicalName, team.Id);
            }

            return response;
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
