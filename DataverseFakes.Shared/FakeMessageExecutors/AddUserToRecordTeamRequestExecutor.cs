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
    /// Fake message executor for AddUserToRecordTeamRequest
    /// </summary>
    public class AddUserToRecordTeamRequestExecutor : IFakeMessageExecutor
    {
        /// <summary>
        /// Determines whether this executor can execute the given request
        /// </summary>
        /// <param name="request">The organization request</param>
        /// <returns>True if the request is AddUserToRecordTeamRequest</returns>
        public bool CanExecute(OrganizationRequest request)
        {
            return request is AddUserToRecordTeamRequest;
        }

        /// <summary>
        /// Executes the AddUserToRecordTeamRequest
        /// </summary>
        /// <param name="request">The organization request</param>
        /// <param name="ctx">The faked context</param>
        /// <returns>AddUserToRecordTeamResponse</returns>
        public OrganizationResponse Execute(OrganizationRequest request, XrmFakedContext ctx)
        {
            AddUserToRecordTeamRequest addReq = (AddUserToRecordTeamRequest)request;

            EntityReference target = addReq.Record;
            Guid systemuserId = addReq.SystemUserId;
            Guid teamTemplateId = addReq.TeamTemplateId;

            if (target == null)
            {
                throw new FaultException<OrganizationServiceFault>(new OrganizationServiceFault(), "Can not add to team without target");
            }

            if (systemuserId == Guid.Empty)
            {
                throw new FaultException<OrganizationServiceFault>(new OrganizationServiceFault(), "Can not add to team without user");
            }

            if (teamTemplateId == Guid.Empty)
            {
                throw new FaultException<OrganizationServiceFault>(new OrganizationServiceFault(), "Can not add to team without team");
            }

            IOrganizationService service = ctx.GetOrganizationService();

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


            Entity team = FindRecordTeam(ctx, target, teamTemplateId);
            if (team == null)
            {
                team = new Entity("team")
                {
                    ["teamtemplateid"] = new EntityReference("teamtemplate", teamTemplateId),
                    ["regardingobjectid"] = target,
                    ["teamtype"] = new OptionSetValue(AccessTeamType)
                };
                team.Id = service.Create(team);
            }

            // Adding a user who is already on the record's team is a no-op, as in Dataverse.
            if (!FindMemberships(ctx, team.Id, systemuserId).Any())
            {
                service.Create(new Entity("teammembership")
                {
                    ["systemuserid"] = systemuserId,
                    ["teamid"] = team.Id
                });
            }

            var accessRightsMask = teamTemplate.Contains("defaultaccessrightsmask") ? teamTemplate["defaultaccessrightsmask"] : 0;

            // One share (principalobjectaccess) per record and team, however many members the team has.
            var hasShare = ctx.CreateQuery("principalobjectaccess").AsEnumerable().Any(p =>
                IdOrEmpty(p, "objectid") == target.Id &&
                IdOrEmpty(p, "principalid") == team.Id);
            if (!hasShare)
            {
                service.Create(new Entity("principalobjectaccess")
                {
                    ["objectid"] = target.Id,
                    ["principalid"] = team.Id,
                    ["accessrightsmask"] = accessRightsMask
                });
            }

            ctx.AccessRightsRepository.GrantAccessTo(target, new PrincipalAccess
            {
                Principal = user.ToEntityReference(),
                AccessMask = (AccessRights)accessRightsMask
            });
            
            return new AddUserToRecordTeamResponse
            {
                ResponseName = "AddUserToRecordTeam"
            };
        }

        /// <summary>
        /// Gets the type of request this executor is responsible for
        /// </summary>
        /// <returns>The type of AddUserToRecordTeamRequest</returns>
        public Type GetResponsibleRequestType()
        {
            return typeof(AddUserToRecordTeamRequest);
        }

        /// <summary>
        /// team.teamtype value for access teams.
        /// </summary>
        internal const int AccessTeamType = 1;

        /// <summary>
        /// Finds the access team for a record and team template. Access teams are per record, so a team
        /// whose regardingobjectid is the record wins; a team seeded without regardingobjectid matches any
        /// record, for backwards compatibility.
        /// </summary>
        internal static Entity FindRecordTeam(XrmFakedContext ctx, EntityReference record, Guid teamTemplateId)
        {
            var candidates = ctx.CreateQuery("team")
                .AsEnumerable()
                .Where(t => LookupOrNull(t, "teamtemplateid")?.Id == teamTemplateId)
                .ToList();

            return candidates.FirstOrDefault(t => LookupOrNull(t, "regardingobjectid")?.Id == record.Id)
                ?? candidates.FirstOrDefault(t => !t.Contains("regardingobjectid") || t["regardingobjectid"] == null);
        }

        /// <summary>
        /// Finds a user's memberships of a team.
        /// </summary>
        internal static List<Entity> FindMemberships(XrmFakedContext ctx, Guid teamId, Guid systemUserId)
        {
            return ctx.CreateQuery("teammembership")
                .AsEnumerable()
                .Where(m => IdOrEmpty(m, "teamid") == teamId &&
                            IdOrEmpty(m, "systemuserid") == systemUserId)
                .ToList();
        }

        // These tolerate seeded values of the wrong type instead of throwing InvalidCastException.
        private static EntityReference LookupOrNull(Entity e, string attribute)
        {
            return e.Contains(attribute) ? e[attribute] as EntityReference : null;
        }

        // Id stored either as a Guid or as an EntityReference.
        private static Guid IdOrEmpty(Entity e, string attribute)
        {
            var value = e.Contains(attribute) ? e[attribute] : null;
            return value is Guid id ? id : (value as EntityReference)?.Id ?? Guid.Empty;
        }
    }
}
#endif