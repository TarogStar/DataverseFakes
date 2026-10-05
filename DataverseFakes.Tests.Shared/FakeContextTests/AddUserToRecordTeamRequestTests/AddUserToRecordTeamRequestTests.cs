#if FAKE_XRM_EASY_2013 || FAKE_XRM_EASY_2015 || FAKE_XRM_EASY_2016 || FAKE_XRM_EASY_365 || FAKE_XRM_EASY_9
using DataverseFakes.FakeMessageExecutors;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using System;
using System.Linq;
using Crm;
using Xunit;

namespace DataverseFakes.Tests.FakeContextTests.AddUserToRecordTeamRequestTests
{
    public class AddUserToRecordTeamRequestTests
    {
        [Fact]
        public void When_can_execute_is_called_with_an_invalid_request_result_is_false()
        {
            var executor = new AddUserToRecordTeamRequestExecutor();
            var anotherRequest = new AddToQueueRequest();
            Assert.False(executor.CanExecute(anotherRequest));
        }

        [Fact]
        public void When_a_request_is_called_User_Is_Added_To_Record_Team()
        {
            var context = new XrmFakedContext();

            var teamTemplate = new TeamTemplate
            {
                Id = Guid.NewGuid(),
                DefaultAccessRightsMask = (int)AccessRights.ReadAccess
            };

            var user = new SystemUser
            {
                Id = Guid.NewGuid()
            };

            var account = new Account
            {
                Id = Guid.NewGuid()
            };

            context.Initialize(new Entity[]
            {
                teamTemplate, user, account
            });

            var executor = new AddUserToRecordTeamRequestExecutor();

            var req = new AddUserToRecordTeamRequest
            {
                Record = account.ToEntityReference(),
                SystemUserId = user.Id,
                TeamTemplateId = teamTemplate.Id
            };

            executor.Execute(req, context);

            var team = context.CreateQuery<Team>().FirstOrDefault(p => p.TeamTemplateId.Id == teamTemplate.Id);
            Assert.NotNull(team);

            var teamMembership = context.CreateQuery<TeamMembership>().FirstOrDefault(p => p.SystemUserId == user.Id && p.TeamId == team.Id);
            Assert.NotNull(teamMembership);

            var poa = context.CreateQuery("principalobjectaccess").FirstOrDefault(p => (Guid)p["objectid"] == account.Id && 
                                                                                       (Guid)p["principalid"] == team.Id);
            Assert.NotNull(poa);

            var response = context.AccessRightsRepository.RetrievePrincipalAccess(account.ToEntityReference(),
                user.ToEntityReference());
            Assert.Equal((AccessRights)teamTemplate.DefaultAccessRightsMask, response.AccessRights);

        }

        [Fact]
        public void When_two_records_use_the_same_template_each_gets_its_own_access_team()
        {
            var context = new XrmFakedContext();
            var teamTemplate = new TeamTemplate { Id = Guid.NewGuid() };
            var user = new SystemUser { Id = Guid.NewGuid() };
            var accountA = new Account { Id = Guid.NewGuid() };
            var accountB = new Account { Id = Guid.NewGuid() };
            // A team without a template must not break the lookup.
            var unrelatedTeam = new Team { Id = Guid.NewGuid() };
            context.Initialize(new Entity[] { teamTemplate, user, accountA, accountB, unrelatedTeam });
            var service = context.GetOrganizationService();

            foreach (var account in new[] { accountA, accountB })
            {
                service.Execute(new AddUserToRecordTeamRequest
                {
                    Record = account.ToEntityReference(),
                    SystemUserId = user.Id,
                    TeamTemplateId = teamTemplate.Id
                });
            }

            var teams = context.CreateQuery<Team>().Where(t => t.TeamTemplateId != null).ToList();
            Assert.Equal(2, teams.Count);
            Assert.Contains(teams, t => t.RegardingObjectId.Id == accountA.Id);
            Assert.Contains(teams, t => t.RegardingObjectId.Id == accountB.Id);
            Assert.All(teams, t => Assert.Equal(1, t.GetAttributeValue<OptionSetValue>("teamtype").Value));

            // Removing the user from record A leaves their membership on record B's team.
            service.Execute(new RemoveUserFromRecordTeamRequest
            {
                Record = accountA.ToEntityReference(),
                SystemUserId = user.Id,
                TeamTemplateId = teamTemplate.Id
            });

            var teamB = teams.Single(t => t.RegardingObjectId.Id == accountB.Id);
            var memberships = context.CreateQuery<TeamMembership>().ToList();
            Assert.Single(memberships);
            Assert.Equal(teamB.Id, memberships[0].TeamId);
        }

        [Fact]
        public void When_the_same_user_is_added_twice_no_duplicate_membership_or_share_is_created()
        {
            var context = new XrmFakedContext();
            var teamTemplate = new TeamTemplate { Id = Guid.NewGuid(), DefaultAccessRightsMask = (int)AccessRights.ReadAccess };
            var user = new SystemUser { Id = Guid.NewGuid() };
            var otherUser = new SystemUser { Id = Guid.NewGuid() };
            var account = new Account { Id = Guid.NewGuid() };
            context.Initialize(new Entity[] { teamTemplate, user, otherUser, account });
            var service = context.GetOrganizationService();

            foreach (var userId in new[] { user.Id, user.Id, otherUser.Id })
            {
                service.Execute(new AddUserToRecordTeamRequest
                {
                    Record = account.ToEntityReference(),
                    SystemUserId = userId,
                    TeamTemplateId = teamTemplate.Id
                });
            }

            Assert.Single(context.CreateQuery<Team>().ToList());
            Assert.Equal(2, context.CreateQuery<TeamMembership>().Count());
            Assert.Single(context.CreateQuery("principalobjectaccess").ToList());
            Assert.Equal(AccessRights.ReadAccess,
                context.AccessRightsRepository.RetrievePrincipalAccess(account.ToEntityReference(), user.ToEntityReference()).AccessRights);
        }
    }
}
#endif