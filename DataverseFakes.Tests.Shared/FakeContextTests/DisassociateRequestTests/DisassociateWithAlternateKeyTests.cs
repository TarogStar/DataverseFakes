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

namespace FakeXrmEasy.Tests.FakeContextTests.DisassociateRequestTests
{
#if !FAKE_XRM_EASY && !FAKE_XRM_EASY_2013 && !FAKE_XRM_EASY_2015
    /// <summary>
    /// Tests for Disassociate request with alternate keys - Issue #508.
    /// Verifies that Disassociate requests work correctly when using
    /// alternate keys instead of GUIDs.
    /// </summary>
    public class DisassociateWithAlternateKeyTests
    {
        [Fact]
        public void When_Disassociate_Uses_AlternateKey_Should_Resolve_And_Disassociate()
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
                    DomainName = "disassoc@example.com"
                },
                new Team
                {
                    Id = teamId,
                    Name = "Disassoc Team"
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

            // First, create the association using GUIDs
            service.Associate("systemuser", userId, new Relationship("teammembership"),
                new EntityReferenceCollection(new List<EntityReference> { new EntityReference("team", teamId) }));

            // Verify association exists before disassociation
            using (var ctx = new XrmServiceContext(service))
            {
                var beforeAssoc = (from tu in ctx.TeamMembershipSet
                                   where tu.TeamId == teamId && tu.SystemUserId == userId
                                   select tu).FirstOrDefault();
                Assert.NotNull(beforeAssoc);
            }

            // Create entity references using alternate keys (no GUIDs)
            var userRef = new EntityReference("systemuser");
            userRef.KeyAttributes["domainname"] = "disassoc@example.com";

            var teamRef = new EntityReference("team");
            teamRef.KeyAttributes["name"] = "Disassoc Team";

            // Act - Disassociate using alternate keys
            var request = new DisassociateRequest()
            {
                Target = userRef,
                RelatedEntities = new EntityReferenceCollection() { teamRef },
                Relationship = new Relationship("teammembership")
            };

            service.Execute(request);

            // Assert - Verify the association was removed
            using (var ctx = new XrmServiceContext(service))
            {
                var afterAssoc = (from tu in ctx.TeamMembershipSet
                                  where tu.TeamId == teamId && tu.SystemUserId == userId
                                  select tu).FirstOrDefault();
                Assert.Null(afterAssoc);
            }
        }

        [Fact]
        public void When_Disassociate_Uses_AlternateKey_For_Target_Only_Should_Resolve_And_Disassociate()
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

            var userId = Guid.NewGuid();
            var teamId = Guid.NewGuid();

            context.Initialize(new List<Entity>
            {
                new SystemUser
                {
                    Id = userId,
                    DomainName = "targetonly@example.com"
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

            // First, create the association using GUIDs
            service.Associate("systemuser", userId, new Relationship("teammembership"),
                new EntityReferenceCollection(new List<EntityReference> { new EntityReference("team", teamId) }));

            // Create target with alternate key, related entity with GUID
            var userRef = new EntityReference("systemuser");
            userRef.KeyAttributes["domainname"] = "targetonly@example.com";

            var teamRef = new EntityReference("team", teamId);

            // Act - Disassociate
            var request = new DisassociateRequest()
            {
                Target = userRef,
                RelatedEntities = new EntityReferenceCollection() { teamRef },
                Relationship = new Relationship("teammembership")
            };

            service.Execute(request);

            // Assert - Verify the association was removed
            using (var ctx = new XrmServiceContext(service))
            {
                var afterAssoc = (from tu in ctx.TeamMembershipSet
                                  where tu.TeamId == teamId && tu.SystemUserId == userId
                                  select tu).FirstOrDefault();
                Assert.Null(afterAssoc);
            }
        }

        [Fact]
        public void When_Disassociate_Uses_AlternateKey_For_Related_Only_Should_Resolve_And_Disassociate()
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
                    Name = "RelatedOnly Team"
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

            // First, create the association using GUIDs
            service.Associate("systemuser", userId, new Relationship("teammembership"),
                new EntityReferenceCollection(new List<EntityReference> { new EntityReference("team", teamId) }));

            // Create target with GUID, related entity with alternate key
            var userRef = new EntityReference("systemuser", userId);

            var teamRef = new EntityReference("team");
            teamRef.KeyAttributes["name"] = "RelatedOnly Team";

            // Act - Disassociate
            var request = new DisassociateRequest()
            {
                Target = userRef,
                RelatedEntities = new EntityReferenceCollection() { teamRef },
                Relationship = new Relationship("teammembership")
            };

            service.Execute(request);

            // Assert - Verify the association was removed
            using (var ctx = new XrmServiceContext(service))
            {
                var afterAssoc = (from tu in ctx.TeamMembershipSet
                                  where tu.TeamId == teamId && tu.SystemUserId == userId
                                  select tu).FirstOrDefault();
                Assert.Null(afterAssoc);
            }
        }

        [Fact]
        public void When_Disassociate_Uses_Multiple_Related_Entities_With_AlternateKeys_Should_Resolve_All()
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

            var userId = Guid.NewGuid();
            var team1Id = Guid.NewGuid();
            var team2Id = Guid.NewGuid();
            var team3Id = Guid.NewGuid();

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
                    Name = "Multi Team 1"
                },
                new Team
                {
                    Id = team2Id,
                    Name = "Multi Team 2"
                },
                new Team
                {
                    Id = team3Id,
                    Name = "Multi Team 3"
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

            // First, create three associations using GUIDs
            service.Associate("systemuser", userId, new Relationship("teammembership"),
                new EntityReferenceCollection(new List<EntityReference>
                {
                    new EntityReference("team", team1Id),
                    new EntityReference("team", team2Id),
                    new EntityReference("team", team3Id)
                }));

            // Verify 3 associations exist before disassociation
            using (var ctx = new XrmServiceContext(service))
            {
                var beforeAssocs = (from tu in ctx.TeamMembershipSet
                                    where tu.SystemUserId == userId
                                    select tu).ToList();
                Assert.Equal(3, beforeAssocs.Count);
            }

            // Create entity references using alternate keys
            var userRef = new EntityReference("systemuser");
            userRef.KeyAttributes["domainname"] = "multiuser@example.com";

            var teamRef1 = new EntityReference("team");
            teamRef1.KeyAttributes["name"] = "Multi Team 1";

            var teamRef2 = new EntityReference("team");
            teamRef2.KeyAttributes["name"] = "Multi Team 2";
            // Note: Not disassociating team3

            // Act - Disassociate two of three related entities
            var request = new DisassociateRequest()
            {
                Target = userRef,
                RelatedEntities = new EntityReferenceCollection() { teamRef1, teamRef2 },
                Relationship = new Relationship("teammembership")
            };

            service.Execute(request);

            // Assert - Verify only one association remains (team3)
            using (var ctx = new XrmServiceContext(service))
            {
                var afterAssocs = (from tu in ctx.TeamMembershipSet
                                   where tu.SystemUserId == userId
                                   select tu).ToList();
                Assert.Single(afterAssocs);
                Assert.Equal(team3Id, afterAssocs[0].TeamId);
            }
        }
    }
#endif
}
