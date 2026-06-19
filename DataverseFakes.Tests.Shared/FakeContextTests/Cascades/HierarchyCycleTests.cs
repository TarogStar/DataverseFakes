using System;
using System.Collections.Generic;
using System.ServiceModel;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using DataverseFakes.Extensions;
using Xunit;

namespace DataverseFakes.Tests.FakeContextTests.Cascades
{
    /// <summary>
    /// Tests that the hierarchy cycle guard correctly rejects self-referential parent/child loops
    /// (direct and transitive) while being inert when no hierarchy lookup is registered.
    /// Validated against real Dataverse behavior: HTTP 400 with the message
    /// "Creating this parental association would create a loop in {entity} hierarchy."
    /// </summary>
    public class HierarchyCycleTests
    {
        // -------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------

        private static XrmFakedContext NewContext() => new XrmFakedContext();

        /// <summary>Creates a self-referential OneToManyRelationshipMetadata for the given entity.</summary>
        private static OneToManyRelationshipMetadata BuildSelfRefRelationship(
            string entity, string attribute, string schemaName = null)
        {
            var rel = new OneToManyRelationshipMetadata();
            rel.SetSealedPropertyValue("SchemaName", schemaName ?? $"{entity}_parent_{entity}");
            rel.SetSealedPropertyValue("ReferencedEntity", entity);
            rel.SetSealedPropertyValue("ReferencedAttribute", $"{entity}id");
            rel.SetSealedPropertyValue("ReferencingEntity", entity);
            rel.SetSealedPropertyValue("ReferencingAttribute", attribute);
            // No cascade configuration needed for hierarchy-only test.
            return rel;
        }

        // -------------------------------------------------------------------------
        // 1. Guard is INERT when no hierarchy is registered
        // -------------------------------------------------------------------------

        [Fact]
        public void Create_allows_any_parent_lookup_when_no_hierarchy_registered()
        {
            var ctx = NewContext();
            var service = ctx.GetOrganizationService();

            var parentId = Guid.NewGuid();
            var parent = new Entity("account") { Id = parentId };
            ctx.Initialize(new List<Entity> { parent });

            // Point account at itself — no hierarchy registered, so this must succeed.
            var record = new Entity("account") { Id = parentId };
            record["parentaccountid"] = new EntityReference("account", parentId);

            // No exception expected.
            var id = service.Create(new Entity("account") { Id = Guid.NewGuid() });
            Assert.NotEqual(Guid.Empty, id);
        }

        [Fact]
        public void Update_allows_self_parent_when_no_hierarchy_registered()
        {
            var ctx = NewContext();
            var id = Guid.NewGuid();
            ctx.Initialize(new List<Entity> { new Entity("account") { Id = id } });
            var service = ctx.GetOrganizationService();

            // Self-reference on update — inert because hierarchy not registered.
            var upd = new Entity("account") { Id = id };
            upd["parentaccountid"] = new EntityReference("account", id);
            service.Update(upd); // Must not throw.

            Assert.True(ctx.Data["account"].ContainsKey(id));
        }

        // -------------------------------------------------------------------------
        // 2. Registration via AddSelfReferentialHierarchy helper
        // -------------------------------------------------------------------------

        [Fact]
        public void Direct_self_reference_on_Create_is_rejected_after_helper_registration()
        {
            var ctx = NewContext();
            ctx.AddSelfReferentialHierarchy("account", "parentaccountid");
            var service = ctx.GetOrganizationService();

            var id = Guid.NewGuid();
            // Seed the record first (Initialize bypasses CRUD validation).
            ctx.Initialize(new List<Entity> { new Entity("account") { Id = id } });

            var record = new Entity("account") { Id = Guid.NewGuid() };
            record["parentaccountid"] = new EntityReference("account", record.Id);

            var ex = Assert.Throws<FaultException>(
                () => service.Create(record));
            Assert.Contains("loop", ex.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("account", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Direct_self_reference_on_Update_is_rejected_after_helper_registration()
        {
            var ctx = NewContext();
            ctx.AddSelfReferentialHierarchy("account", "parentaccountid");

            var id = Guid.NewGuid();
            ctx.Initialize(new List<Entity> { new Entity("account") { Id = id } });
            var service = ctx.GetOrganizationService();

            var upd = new Entity("account") { Id = id };
            upd["parentaccountid"] = new EntityReference("account", id);

            var ex = Assert.Throws<FaultException>(
                () => service.Update(upd));
            Assert.Contains("loop", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        // -------------------------------------------------------------------------
        // 3. Transitive loops
        // -------------------------------------------------------------------------

        [Fact]
        public void Transitive_2hop_loop_on_Update_is_rejected()
        {
            // A.parent = B, then B.parent = A  => rejected.
            var ctx = NewContext();
            ctx.AddSelfReferentialHierarchy("account", "parentaccountid");

            var idA = Guid.NewGuid();
            var idB = Guid.NewGuid();
            var entityA = new Entity("account") { Id = idA };
            var entityB = new Entity("account") { Id = idB };
            entityB["parentaccountid"] = new EntityReference("account", idA); // B -> A
            ctx.Initialize(new List<Entity> { entityA, entityB });

            var service = ctx.GetOrganizationService();

            // Now try to set A.parent = B (would create A -> B -> A loop).
            var upd = new Entity("account") { Id = idA };
            upd["parentaccountid"] = new EntityReference("account", idB);

            var ex = Assert.Throws<FaultException>(
                () => service.Update(upd));
            Assert.Contains("loop", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Transitive_3hop_loop_on_Update_is_rejected()
        {
            // Chain: B -> C -> D (stored). Then D.parent = B => loop B -> C -> D -> B.
            var ctx = NewContext();
            ctx.AddSelfReferentialHierarchy("account", "parentaccountid");

            var idB = Guid.NewGuid();
            var idC = Guid.NewGuid();
            var idD = Guid.NewGuid();

            var entityB = new Entity("account") { Id = idB };
            var entityC = new Entity("account") { Id = idC };
            entityC["parentaccountid"] = new EntityReference("account", idB); // C -> B
            var entityD = new Entity("account") { Id = idD };
            entityD["parentaccountid"] = new EntityReference("account", idC); // D -> C
            ctx.Initialize(new List<Entity> { entityB, entityC, entityD });

            var service = ctx.GetOrganizationService();

            // Set B.parent = D => B -> D -> C -> B (3-hop loop).
            var upd = new Entity("account") { Id = idB };
            upd["parentaccountid"] = new EntityReference("account", idD);

            var ex = Assert.Throws<FaultException>(
                () => service.Update(upd));
            Assert.Contains("loop", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        // -------------------------------------------------------------------------
        // 4. Valid chains and re-parenting are allowed
        // -------------------------------------------------------------------------

        [Fact]
        public void Valid_deep_chain_is_allowed()
        {
            // A -> B -> C -> D: no loop; all allowed.
            var ctx = NewContext();
            ctx.AddSelfReferentialHierarchy("account", "parentaccountid");

            var idA = Guid.NewGuid();
            var idB = Guid.NewGuid();
            var idC = Guid.NewGuid();
            var idD = Guid.NewGuid();

            var entityA = new Entity("account") { Id = idA };
            var entityB = new Entity("account") { Id = idB };
            entityB["parentaccountid"] = new EntityReference("account", idA);
            var entityC = new Entity("account") { Id = idC };
            entityC["parentaccountid"] = new EntityReference("account", idB);
            var entityD = new Entity("account") { Id = idD };
            ctx.Initialize(new List<Entity> { entityA, entityB, entityC, entityD });

            var service = ctx.GetOrganizationService();

            // Attach D to C — valid (D -> C -> B -> A).
            var upd = new Entity("account") { Id = idD };
            upd["parentaccountid"] = new EntityReference("account", idC);
            service.Update(upd); // Must NOT throw.

            Assert.Equal(idC, ctx.Data["account"][idD].GetAttributeValue<EntityReference>("parentaccountid").Id);
        }

        [Fact]
        public void Reparenting_to_non_ancestor_is_allowed()
        {
            // A -> B (stored). Reparent A to C (unrelated root). No loop.
            var ctx = NewContext();
            ctx.AddSelfReferentialHierarchy("account", "parentaccountid");

            var idA = Guid.NewGuid();
            var idB = Guid.NewGuid();
            var idC = Guid.NewGuid();

            var entityA = new Entity("account") { Id = idA };
            entityA["parentaccountid"] = new EntityReference("account", idB);
            var entityB = new Entity("account") { Id = idB };
            var entityC = new Entity("account") { Id = idC };
            ctx.Initialize(new List<Entity> { entityA, entityB, entityC });

            var service = ctx.GetOrganizationService();

            // Move A under C — valid.
            var upd = new Entity("account") { Id = idA };
            upd["parentaccountid"] = new EntityReference("account", idC);
            service.Update(upd); // Must NOT throw.

            Assert.Equal(idC, ctx.Data["account"][idA].GetAttributeValue<EntityReference>("parentaccountid").Id);
        }

        // -------------------------------------------------------------------------
        // 5. Registration via EntityMetadata (OneToManyRelationships)
        // -------------------------------------------------------------------------

        [Fact]
        public void Direct_self_reference_rejected_when_registered_via_entity_metadata()
        {
            var ctx = NewContext();

            var accountMeta = new EntityMetadata { LogicalName = "account" };
            var rel = BuildSelfRefRelationship("account", "parentaccountid");
            accountMeta.SetSealedPropertyValue("OneToManyRelationships",
                new OneToManyRelationshipMetadata[] { rel });
            ctx.InitializeMetadata(accountMeta);

            var id = Guid.NewGuid();
            ctx.Initialize(new List<Entity> { new Entity("account") { Id = id } });
            var service = ctx.GetOrganizationService();

            var upd = new Entity("account") { Id = id };
            upd["parentaccountid"] = new EntityReference("account", id);

            var ex = Assert.Throws<FaultException>(
                () => service.Update(upd));
            Assert.Contains("loop", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Transitive_loop_rejected_when_registered_via_entity_metadata()
        {
            var ctx = NewContext();

            var accountMeta = new EntityMetadata { LogicalName = "account" };
            var rel = BuildSelfRefRelationship("account", "parentaccountid");
            accountMeta.SetSealedPropertyValue("OneToManyRelationships",
                new OneToManyRelationshipMetadata[] { rel });
            ctx.InitializeMetadata(accountMeta);

            var idX = Guid.NewGuid();
            var idY = Guid.NewGuid();
            var entityX = new Entity("account") { Id = idX };
            var entityY = new Entity("account") { Id = idY };
            entityY["parentaccountid"] = new EntityReference("account", idX); // Y -> X
            ctx.Initialize(new List<Entity> { entityX, entityY });

            var service = ctx.GetOrganizationService();

            // Set X.parent = Y => loop X -> Y -> X.
            var upd = new Entity("account") { Id = idX };
            upd["parentaccountid"] = new EntityReference("account", idY);

            var ex = Assert.Throws<FaultException>(
                () => service.Update(upd));
            Assert.Contains("loop", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        // -------------------------------------------------------------------------
        // 6. Null / cleared parent is allowed (no loop)
        // -------------------------------------------------------------------------

        [Fact]
        public void Null_parent_update_is_allowed()
        {
            var ctx = NewContext();
            ctx.AddSelfReferentialHierarchy("account", "parentaccountid");

            var idA = Guid.NewGuid();
            var idB = Guid.NewGuid();
            var entityA = new Entity("account") { Id = idA };
            entityA["parentaccountid"] = new EntityReference("account", idB);
            var entityB = new Entity("account") { Id = idB };
            ctx.Initialize(new List<Entity> { entityA, entityB });

            var service = ctx.GetOrganizationService();

            // Clear the parent — always valid.
            var upd = new Entity("account") { Id = idA };
            upd["parentaccountid"] = null;
            service.Update(upd); // Must NOT throw.
        }
    }
}
