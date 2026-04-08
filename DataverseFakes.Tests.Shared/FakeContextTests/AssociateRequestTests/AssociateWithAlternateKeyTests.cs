using Crm;
using FakeXrmEasy.Extensions;
using FakeXrmEasy.FakeMessageExecutors;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace FakeXrmEasy.Tests.FakeContextTests.AssociateRequestTests
{
#if !FAKE_XRM_EASY && !FAKE_XRM_EASY_2013 && !FAKE_XRM_EASY_2015
    /// <summary>
    /// Tests for Associate request with alternate keys - Issue #508.
    /// Verifies that Associate and Disassociate requests work correctly
    /// when using alternate keys instead of GUIDs.
    /// </summary>
    public class AssociateWithAlternateKeyTests
    {
        [Fact]
        public void When_Associate_Uses_AlternateKey_Should_Resolve_And_Associate()
        {
      // Arrange
      var context = new XrmFakedContext
      {
        ProxyTypesAssembly = Assembly.GetExecutingAssembly()
      };
      context.InitializeMetadata(Assembly.GetExecutingAssembly());

            // Set up alternate key metadata for systemuser entity
            var userMetadata = context.GetEntityMetadataByName("systemuser");
            userMetadata.SetFieldValue("Keys", new EntityKeyMetadata[]
            {
                new EntityKeyMetadata()
                {
                    KeyAttributes = new string[] { "domainname" }
                }
            });
            context.SetEntityMetadata(userMetadata);

            // Set up alternate key metadata for team entity
            var teamMetadata = context.GetEntityMetadataByName("team");
            teamMetadata.SetFieldValue("Keys", new EntityKeyMetadata[]
            {
                new EntityKeyMetadata()
                {
                    KeyAttributes = new string[] { "name" }
                }
            });
            context.SetEntityMetadata(teamMetadata);

            // Initialize entities with alternate key values
            var userId = Guid.NewGuid();
            var teamId = Guid.NewGuid();

            context.Initialize(new List<Entity>
            {
                new SystemUser
                {
                    Id = userId,
                    DomainName = "user@example.com"
                },
                new Team
                {
                    Id = teamId,
                    Name = "Test Team"
                }
            });

            // Set up the relationship (using teammembership which is a known proxy type)
            context.AddRelationship("teammembership",
                new XrmFakedRelationship()
                {
                    RelationshipType = XrmFakedRelationship.enmFakeRelationshipType.ManyToMany,
                    IntersectEntity = "teammembership",
                    Entity1LogicalName = "systemuser",
                    Entity1Attribute = "systemuserid",
                    Entity2LogicalName = "team",
                    Entity2Attribute = "teamid"
                });

            var service = context.GetOrganizationService();

            // Create entity references using alternate keys (no GUIDs)
            var userRef = new EntityReference("systemuser");
            userRef.KeyAttributes["domainname"] = "user@example.com";

            var teamRef = new EntityReference("team");
            teamRef.KeyAttributes["name"] = "Test Team";

            // Act - Associate using alternate keys
            var request = new AssociateRequest()
            {
                Target = userRef,
                RelatedEntities = new EntityReferenceCollection() { teamRef },
                Relationship = new Relationship("teammembership")
            };

            service.Execute(request);

            // Assert - Verify the association was created
            using (var ctx = new XrmServiceContext(service))
            {
                var association = (from tu in ctx.TeamMembershipSet
                                   where tu.TeamId == teamId
                                   && tu.SystemUserId == userId
                                   select tu).FirstOrDefault();
                Assert.NotNull(association);
            }
        }

        [Fact]
        public void When_Associate_Uses_AlternateKey_For_Target_Only_Should_Resolve_And_Associate()
        {
      // Arrange
      var context = new XrmFakedContext
      {
        ProxyTypesAssembly = Assembly.GetExecutingAssembly()
      };
      context.InitializeMetadata(Assembly.GetExecutingAssembly());

            // Set up alternate key metadata for systemuser entity only
            var userMetadata = context.GetEntityMetadataByName("systemuser");
            userMetadata.SetFieldValue("Keys", new EntityKeyMetadata[]
            {
                new EntityKeyMetadata()
                {
                    KeyAttributes = new string[] { "domainname" }
                }
            });
            context.SetEntityMetadata(userMetadata);

            // Initialize entities
            var userId = Guid.NewGuid();
            var teamId = Guid.NewGuid();

            context.Initialize(new List<Entity>
            {
                new SystemUser
                {
                    Id = userId,
                    DomainName = "target@example.com"
                },
                new Team
                {
                    Id = teamId
                }
            });

            // Set up the relationship
            context.AddRelationship("teammembership",
                new XrmFakedRelationship()
                {
                    RelationshipType = XrmFakedRelationship.enmFakeRelationshipType.ManyToMany,
                    IntersectEntity = "teammembership",
                    Entity1LogicalName = "systemuser",
                    Entity1Attribute = "systemuserid",
                    Entity2LogicalName = "team",
                    Entity2Attribute = "teamid"
                });

            var service = context.GetOrganizationService();

            // Create target with alternate key, related entity with GUID
            var userRef = new EntityReference("systemuser");
            userRef.KeyAttributes["domainname"] = "target@example.com";

            var teamRef = new EntityReference("team", teamId);

            // Act - Associate
            var request = new AssociateRequest()
            {
                Target = userRef,
                RelatedEntities = new EntityReferenceCollection() { teamRef },
                Relationship = new Relationship("teammembership")
            };

            service.Execute(request);

            // Assert - Verify the association was created
            using (var ctx = new XrmServiceContext(service))
            {
                var association = (from tu in ctx.TeamMembershipSet
                                   where tu.TeamId == teamId
                                   && tu.SystemUserId == userId
                                   select tu).FirstOrDefault();
                Assert.NotNull(association);
            }
        }

        [Fact]
        public void When_Associate_Uses_AlternateKey_For_Related_Only_Should_Resolve_And_Associate()
        {
      // Arrange
      var context = new XrmFakedContext
      {
        ProxyTypesAssembly = Assembly.GetExecutingAssembly()
      };
      context.InitializeMetadata(Assembly.GetExecutingAssembly());

            // Set up alternate key metadata for team entity only
            var teamMetadata = context.GetEntityMetadataByName("team");
            teamMetadata.SetFieldValue("Keys", new EntityKeyMetadata[]
            {
                new EntityKeyMetadata()
                {
                    KeyAttributes = new string[] { "name" }
                }
            });
            context.SetEntityMetadata(teamMetadata);

            // Initialize entities
            var userId = Guid.NewGuid();
            var teamId = Guid.NewGuid();

            context.Initialize(new List<Entity>
            {
                new SystemUser
                {
                    Id = userId
                },
                new Team
                {
                    Id = teamId,
                    Name = "Related Team"
                }
            });

            // Set up the relationship
            context.AddRelationship("teammembership",
                new XrmFakedRelationship()
                {
                    RelationshipType = XrmFakedRelationship.enmFakeRelationshipType.ManyToMany,
                    IntersectEntity = "teammembership",
                    Entity1LogicalName = "systemuser",
                    Entity1Attribute = "systemuserid",
                    Entity2LogicalName = "team",
                    Entity2Attribute = "teamid"
                });

            var service = context.GetOrganizationService();

            // Create target with GUID, related entity with alternate key
            var userRef = new EntityReference("systemuser", userId);

            var teamRef = new EntityReference("team");
            teamRef.KeyAttributes["name"] = "Related Team";

            // Act - Associate
            var request = new AssociateRequest()
            {
                Target = userRef,
                RelatedEntities = new EntityReferenceCollection() { teamRef },
                Relationship = new Relationship("teammembership")
            };

            service.Execute(request);

            // Assert - Verify the association was created
            using (var ctx = new XrmServiceContext(service))
            {
                var association = (from tu in ctx.TeamMembershipSet
                                   where tu.TeamId == teamId
                                   && tu.SystemUserId == userId
                                   select tu).FirstOrDefault();
                Assert.NotNull(association);
            }
        }

        [Fact]
        public void When_Associate_Uses_Multiple_Related_Entities_With_AlternateKeys_Should_Resolve_All()
        {
      // Arrange
      var context = new XrmFakedContext
      {
        ProxyTypesAssembly = Assembly.GetExecutingAssembly()
      };
      context.InitializeMetadata(Assembly.GetExecutingAssembly());

            // Set up alternate key metadata
            var userMetadata = context.GetEntityMetadataByName("systemuser");
            userMetadata.SetFieldValue("Keys", new EntityKeyMetadata[]
            {
                new EntityKeyMetadata()
                {
                    KeyAttributes = new string[] { "domainname" }
                }
            });
            context.SetEntityMetadata(userMetadata);

            var teamMetadata = context.GetEntityMetadataByName("team");
            teamMetadata.SetFieldValue("Keys", new EntityKeyMetadata[]
            {
                new EntityKeyMetadata()
                {
                    KeyAttributes = new string[] { "name" }
                }
            });
            context.SetEntityMetadata(teamMetadata);

            // Initialize entities
            var userId = Guid.NewGuid();
            var team1Id = Guid.NewGuid();
            var team2Id = Guid.NewGuid();

            context.Initialize(new List<Entity>
            {
                new SystemUser
                {
                    Id = userId,
                    DomainName = "multiuser@example.com"
                },
                new Team
                {
                    Id = team1Id,
                    Name = "Team One"
                },
                new Team
                {
                    Id = team2Id,
                    Name = "Team Two"
                }
            });

            // Set up the relationship
            context.AddRelationship("teammembership",
                new XrmFakedRelationship()
                {
                    RelationshipType = XrmFakedRelationship.enmFakeRelationshipType.ManyToMany,
                    IntersectEntity = "teammembership",
                    Entity1LogicalName = "systemuser",
                    Entity1Attribute = "systemuserid",
                    Entity2LogicalName = "team",
                    Entity2Attribute = "teamid"
                });

            var service = context.GetOrganizationService();

            // Create entity references using alternate keys
            var userRef = new EntityReference("systemuser");
            userRef.KeyAttributes["domainname"] = "multiuser@example.com";

            var teamRef1 = new EntityReference("team");
            teamRef1.KeyAttributes["name"] = "Team One";

            var teamRef2 = new EntityReference("team");
            teamRef2.KeyAttributes["name"] = "Team Two";

            // Act - Associate multiple related entities
            var request = new AssociateRequest()
            {
                Target = userRef,
                RelatedEntities = new EntityReferenceCollection() { teamRef1, teamRef2 },
                Relationship = new Relationship("teammembership")
            };

            service.Execute(request);

            // Assert - Verify both associations were created
            using (var ctx = new XrmServiceContext(service))
            {
                var associations = (from tu in ctx.TeamMembershipSet
                                    where tu.SystemUserId == userId
                                    select tu).ToList();
                Assert.Equal(2, associations.Count);
                Assert.Contains(associations, a => a.TeamId == team1Id);
                Assert.Contains(associations, a => a.TeamId == team2Id);
            }
        }
    }
#endif
}
