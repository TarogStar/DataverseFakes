#if FAKE_XRM_EASY_2013 || FAKE_XRM_EASY_2015 || FAKE_XRM_EASY_2016 || FAKE_XRM_EASY_365 || FAKE_XRM_EASY_9
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using System;
using System.Linq;
using Crm;
using Xunit;

namespace DataverseFakes.Tests.FakeContextTests.AccessTeamLifecycleTests
{
    /// <summary>
    /// Access team lifecycle through AddUserToRecordTeam / RemoveUserFromRecordTeam, matching behavior
    /// observed against a live Dataverse org (template-based access teams on an opportunity).
    /// </summary>
    public class AccessTeamLifecycleTests
    {
        private readonly XrmFakedContext _context = new XrmFakedContext();
        private readonly IOrganizationService _service;
        private readonly TeamTemplate _template = new TeamTemplate { Id = Guid.NewGuid(), DefaultAccessRightsMask = 23 };
        private readonly Opportunity _record = new Opportunity { Id = Guid.NewGuid() };
        private readonly SystemUser _userA = new SystemUser { Id = Guid.NewGuid() };
        private readonly SystemUser _userB = new SystemUser { Id = Guid.NewGuid() };

        public AccessTeamLifecycleTests()
        {
            _context.Initialize(new Entity[] { _template, _record, _userA, _userB });
            _service = _context.GetOrganizationService();
        }

        private Guid Add(SystemUser user) =>
            ((AddUserToRecordTeamResponse)_service.Execute(new AddUserToRecordTeamRequest
            {
                Record = _record.ToEntityReference(), SystemUserId = user.Id, TeamTemplateId = _template.Id
            })).AccessTeamId;

        private Guid Remove(SystemUser user) =>
            ((RemoveUserFromRecordTeamResponse)_service.Execute(new RemoveUserFromRecordTeamRequest
            {
                Record = _record.ToEntityReference(), SystemUserId = user.Id, TeamTemplateId = _template.Id
            })).AccessTeamId;

        private AccessRights AccessOf(SystemUser user) =>
            _context.AccessRightsRepository.RetrievePrincipalAccess(_record.ToEntityReference(), user.ToEntityReference()).AccessRights;

        [Fact]
        public void Add_creates_a_system_managed_access_team_and_shares_the_record_with_it()
        {
            var teamId = Add(_userA);

            var team = _context.CreateQuery<Team>().Single();
            Assert.Equal(teamId, team.Id);
            Assert.Equal($"opportunity {_record.Id}+{_template.Id}", team.Name);
            Assert.Equal(_record.Id, team.RegardingObjectId.Id);
            Assert.Equal(_template.Id, team.TeamTemplateId.Id);
            Assert.Equal(1, team.GetAttributeValue<OptionSetValue>("teamtype").Value);
            Assert.True(team.SystemManaged);

            var share = _context.CreateQuery<PrincipalObjectAccess>().Single();
            Assert.Equal(_record.Id, share.ObjectId);
            Assert.Equal("opportunity", share.ObjectTypeCode);
            Assert.Equal(team.Id, share.PrincipalId);
            Assert.Equal("team", share.PrincipalTypeCode);
            Assert.Equal(23, share.AccessRightsMask);
            Assert.Equal(0, share.InheritedAccessRightsMask);

            Assert.Equal((AccessRights)23, AccessOf(_userA));
        }

        [Fact]
        public void Adding_the_same_user_twice_returns_the_same_team()
        {
            var teamId = Add(_userA);
            Assert.NotEqual(Guid.Empty, teamId);
            Assert.Equal(teamId, Add(_userA));
            Assert.Single(_context.CreateQuery<TeamMembership>().ToList());
        }

        [Fact]
        public void Remove_without_an_access_team_succeeds_with_an_empty_team_id()
        {
            Assert.Equal(Guid.Empty, Remove(_userA));
        }

        [Fact]
        public void Remove_of_a_non_member_returns_the_existing_team_and_changes_nothing()
        {
            var teamId = Add(_userA);
            Assert.NotEqual(Guid.Empty, teamId);

            Assert.Equal(teamId, Remove(_userB));
            Assert.Single(_context.CreateQuery<TeamMembership>().ToList());
            Assert.Single(_context.CreateQuery<PrincipalObjectAccess>().ToList());
        }

        [Fact]
        public void Removing_one_of_two_members_keeps_the_team_and_its_share()
        {
            var teamId = Add(_userA);
            Assert.NotEqual(Guid.Empty, teamId);
            Add(_userB);

            Assert.Equal(teamId, Remove(_userA));

            Assert.Single(_context.CreateQuery<Team>().ToList());
            Assert.Single(_context.CreateQuery<PrincipalObjectAccess>().ToList());
            Assert.Equal(AccessRights.None, AccessOf(_userA));
            Assert.Equal((AccessRights)23, AccessOf(_userB));
        }

        [Fact]
        public void Removing_the_last_member_deletes_the_team_and_its_share()
        {
            var teamId = Add(_userA);

            Assert.Equal(teamId, Remove(_userA));

            Assert.Empty(_context.CreateQuery<Team>().ToList());
            Assert.Empty(_context.CreateQuery<PrincipalObjectAccess>().ToList());
            Assert.Equal(AccessRights.None, AccessOf(_userA));

            // A later Add starts a fresh team.
            Assert.NotEqual(teamId, Add(_userA));
        }

        [Fact]
        public void A_direct_share_survives_removal_from_the_access_team()
        {
            _context.AccessRightsRepository.GrantAccessTo(_record.ToEntityReference(), new PrincipalAccess
            {
                Principal = _userA.ToEntityReference(),
                AccessMask = AccessRights.ReadAccess
            });
            Add(_userA);

            Remove(_userA);

            Assert.Equal(AccessRights.ReadAccess, AccessOf(_userA));
        }
    }
}
#endif
