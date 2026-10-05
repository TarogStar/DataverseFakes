#if FAKE_XRM_EASY_2013 || FAKE_XRM_EASY_2015 || FAKE_XRM_EASY_2016 || FAKE_XRM_EASY_365 || FAKE_XRM_EASY_9
using DataverseFakes.FakeMessageExecutors;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using System;
using System.Linq;
using Crm;
using Xunit;

namespace DataverseFakes.Tests.FakeContextTests.RemoveUserFromRecordTeamRequestTests
{
    public class RemoveUserFromRecordTeamRequestTests
    {
        [Fact]
        public void When_can_execute_is_called_with_an_invalid_request_result_is_false()
        {
            var executor = new RemoveUserFromRecordTeamRequestExecutor();
            var anotherRequest = new AddToQueueRequest();
            Assert.False(executor.CanExecute(anotherRequest));
        }

        [Fact]
        public void When_a_request_is_called_User_Is_Removed_From_Record_Team()
        {
            var context = new XrmFakedContext();

            var teamTemplate = new TeamTemplate
            {
                Id = Guid.NewGuid(),
                DefaultAccessRightsMask = (int)AccessRights.ReadAccess
            };

            var team = new Team
            {
                Id = Guid.NewGuid(),
                TeamTemplateId = teamTemplate.ToEntityReference()
            };

            var user = new SystemUser
            {
                Id = Guid.NewGuid()
            };

            var teamMembership = new TeamMembership
            {
                Id = Guid.NewGuid(),
                ["systemuserid"] = user.Id,
                ["teamid"] = team.Id
            };

            var account = new Account
            {
                Id = Guid.NewGuid()
            };

            context.Initialize(new Entity[]
            {
                teamTemplate, team, teamMembership, user, account
            });

            var executor = new RemoveUserFromRecordTeamRequestExecutor();

            var req = new RemoveUserFromRecordTeamRequest
            {
                Record = account.ToEntityReference(),
                SystemUserId = user.Id,
                TeamTemplateId = teamTemplate.Id
            };

            context.AccessRightsRepository.GrantAccessTo(account.ToEntityReference(), new PrincipalAccess
            {
                Principal = user.ToEntityReference(),
                AccessMask = AccessRights.ReadAccess
            });

            executor.Execute(req, context);

            var retrievedTeamMembership = context.CreateQuery<TeamMembership>().FirstOrDefault(p => p.SystemUserId == user.Id && p.TeamId == team.Id);
            Assert.Null(retrievedTeamMembership);

            var response = context.AccessRightsRepository.RetrievePrincipalAccess(account.ToEntityReference(),
                user.ToEntityReference());
            Assert.Equal(AccessRights.None, response.AccessRights);

        }

        [Fact]
        public void When_a_request_is_executed_through_the_service_User_Is_Removed_From_Record_Team()
        {
            // Goes through IOrganizationService.Execute so the executor's registration
            // (GetResponsibleRequestType) is exercised, not just Execute().
            var context = new XrmFakedContext();

            var teamTemplate = new TeamTemplate { Id = Guid.NewGuid() };
            var team = new Team { Id = Guid.NewGuid(), TeamTemplateId = teamTemplate.ToEntityReference() };
            var user = new SystemUser { Id = Guid.NewGuid() };
            var teamMembership = new TeamMembership
            {
                Id = Guid.NewGuid(),
                ["systemuserid"] = user.Id,
                ["teamid"] = team.Id
            };
            var account = new Account { Id = Guid.NewGuid() };

            context.Initialize(new Entity[] { teamTemplate, team, teamMembership, user, account });

            var service = context.GetOrganizationService();
            var response = service.Execute(new RemoveUserFromRecordTeamRequest
            {
                Record = account.ToEntityReference(),
                SystemUserId = user.Id,
                TeamTemplateId = teamTemplate.Id
            });

            Assert.IsType<RemoveUserFromRecordTeamResponse>(response);
            Assert.Null(context.CreateQuery<TeamMembership>().FirstOrDefault(p => p.SystemUserId == user.Id && p.TeamId == team.Id));
        }

        [Fact]
        public void When_removing_a_user_another_users_membership_on_the_same_team_is_kept()
        {
            var context = new XrmFakedContext();

            var teamTemplate = new TeamTemplate { Id = Guid.NewGuid() };
            var team = new Team { Id = Guid.NewGuid(), TeamTemplateId = teamTemplate.ToEntityReference() };
            var keptUser = new SystemUser { Id = Guid.NewGuid() };
            // removedUser has no membership, so the only one on the team belongs to keptUser:
            // a lookup by team alone would delete it.
            var removedUser = new SystemUser { Id = Guid.NewGuid() };
            var keptMembership = new TeamMembership { Id = Guid.NewGuid(), ["systemuserid"] = keptUser.Id, ["teamid"] = team.Id };
            var account = new Account { Id = Guid.NewGuid() };

            context.Initialize(new Entity[] { teamTemplate, team, keptUser, removedUser, keptMembership, account });

            context.GetOrganizationService().Execute(new RemoveUserFromRecordTeamRequest
            {
                Record = account.ToEntityReference(),
                SystemUserId = removedUser.Id,
                TeamTemplateId = teamTemplate.Id
            });

            var remaining = context.CreateQuery<TeamMembership>().ToList();
            Assert.Single(remaining);
            Assert.Equal(keptUser.Id, remaining[0].SystemUserId);
        }
    }
}
#endif