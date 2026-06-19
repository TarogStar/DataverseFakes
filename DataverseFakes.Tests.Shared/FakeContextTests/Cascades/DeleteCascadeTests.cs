using System;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using Xunit;

namespace DataverseFakes.Tests.FakeContextTests.Cascades
{
    public class DeleteCascadeTests
    {
        private static XrmFakedContext NewContext() => new XrmFakedContext();

        private static (XrmFakedContext ctx, Guid parentId, Guid childId) SeedParentChild(CascadeType behavior)
        {
            var ctx = NewContext();
            ctx.AddCascadeDeleteRelationship(
                "account_contacts", "account", "contact", "parentcustomerid", behavior);

            var parentId = Guid.NewGuid();
            var childId = Guid.NewGuid();
            var parent = new Entity("account") { Id = parentId };
            var child = new Entity("contact") { Id = childId };
            child["parentcustomerid"] = new EntityReference("account", parentId);
            ctx.Initialize(new List<Entity> { parent, child });
            return (ctx, parentId, childId);
        }

        [Fact]
        public void AddCascadeDeleteRelationship_registers_a_rule_that_is_inert_for_unrelated_parents()
        {
            var context = NewContext();
            context.AddCascadeDeleteRelationship(
                "account_contacts", "account", "contact", "parentcustomerid", CascadeType.Cascade);

            // No exception, no data changes: registering a rule alone does nothing.
            Assert.NotNull(context.GetOrganizationService());
        }

        [Fact]
        public void Cascade_deletes_child_when_parent_deleted()
        {
            var (ctx, parentId, childId) = SeedParentChild(CascadeType.Cascade);
            var service = ctx.GetOrganizationService();

            service.Delete("account", parentId);

            Assert.False(ctx.Data["account"].ContainsKey(parentId));
            Assert.False(ctx.Data["contact"].ContainsKey(childId));
        }

        [Fact]
        public void Cascade_deletes_all_children()
        {
            var ctx = NewContext();
            ctx.AddCascadeDeleteRelationship(
                "account_contacts", "account", "contact", "parentcustomerid", CascadeType.Cascade);

            var parentId = Guid.NewGuid();
            var parent = new Entity("account") { Id = parentId };
            var seed = new List<Entity> { parent };
            var childIds = new List<Guid>();
            for (int i = 0; i < 3; i++)
            {
                var childId = Guid.NewGuid();
                childIds.Add(childId);
                var child = new Entity("contact") { Id = childId };
                child["parentcustomerid"] = new EntityReference("account", parentId);
                seed.Add(child);
            }
            ctx.Initialize(seed);

            ctx.GetOrganizationService().Delete("account", parentId);

            Assert.Empty(ctx.Data["contact"]);
        }

        [Fact]
        public void RemoveLink_nulls_child_lookup_and_keeps_child()
        {
            var (ctx, parentId, childId) = SeedParentChild(CascadeType.RemoveLink);
            var service = ctx.GetOrganizationService();

            service.Delete("account", parentId);

            Assert.False(ctx.Data["account"].ContainsKey(parentId));
            Assert.True(ctx.Data["contact"].ContainsKey(childId));
            var child = ctx.Data["contact"][childId];
            Assert.Null(child.GetAttributeValue<EntityReference>("parentcustomerid"));
        }

        [Fact]
        public void RemoveLink_leaves_unrelated_child_untouched()
        {
            var ctx = NewContext();
            ctx.AddCascadeDeleteRelationship(
                "account_contacts", "account", "contact", "parentcustomerid", CascadeType.RemoveLink);

            var parentId = Guid.NewGuid();
            var otherParentId = Guid.NewGuid();
            var relatedChildId = Guid.NewGuid();
            var unrelatedChildId = Guid.NewGuid();

            var parent = new Entity("account") { Id = parentId };
            var relatedChild = new Entity("contact") { Id = relatedChildId };
            relatedChild["parentcustomerid"] = new EntityReference("account", parentId);
            var unrelatedChild = new Entity("contact") { Id = unrelatedChildId };
            unrelatedChild["parentcustomerid"] = new EntityReference("account", otherParentId);

            ctx.Initialize(new List<Entity> { parent, relatedChild, unrelatedChild });

            ctx.GetOrganizationService().Delete("account", parentId);

            Assert.Null(ctx.Data["contact"][relatedChildId].GetAttributeValue<EntityReference>("parentcustomerid"));
            Assert.Equal(otherParentId,
                ctx.Data["contact"][unrelatedChildId].GetAttributeValue<EntityReference>("parentcustomerid").Id);
        }

        [Fact]
        public void Restrict_throws_and_leaves_parent_and_child_intact()
        {
            var (ctx, parentId, childId) = SeedParentChild(CascadeType.Restrict);
            var service = ctx.GetOrganizationService();

            Assert.Throws<System.ServiceModel.FaultException<Microsoft.Xrm.Sdk.OrganizationServiceFault>>(
                () => service.Delete("account", parentId));

            Assert.True(ctx.Data["account"].ContainsKey(parentId));
            Assert.True(ctx.Data["contact"].ContainsKey(childId));
        }

        [Fact]
        public void Restrict_allows_delete_when_no_children_exist()
        {
            var ctx = NewContext();
            ctx.AddCascadeDeleteRelationship(
                "account_contacts", "account", "contact", "parentcustomerid", CascadeType.Restrict);
            var parentId = Guid.NewGuid();
            ctx.Initialize(new List<Entity> { new Entity("account") { Id = parentId } });

            ctx.GetOrganizationService().Delete("account", parentId);

            Assert.False(ctx.Data["account"].ContainsKey(parentId));
        }

        [Fact]
        public void NoCascade_leaves_children_orphaned()
        {
            var (ctx, parentId, childId) = SeedParentChild(CascadeType.NoCascade);
            var service = ctx.GetOrganizationService();

            service.Delete("account", parentId);

            Assert.False(ctx.Data["account"].ContainsKey(parentId));
            Assert.True(ctx.Data["contact"].ContainsKey(childId));
            // Lookup still points at the now-deleted parent (orphaned).
            Assert.Equal(parentId,
                ctx.Data["contact"][childId].GetAttributeValue<EntityReference>("parentcustomerid").Id);
        }

        [Fact]
        public void Delete_with_no_cascade_rule_registered_behaves_normally()
        {
            var ctx = NewContext();
            var parentId = Guid.NewGuid();
            var childId = Guid.NewGuid();
            var parent = new Entity("account") { Id = parentId };
            var child = new Entity("contact") { Id = childId };
            child["parentcustomerid"] = new EntityReference("account", parentId);
            ctx.Initialize(new List<Entity> { parent, child });

            ctx.GetOrganizationService().Delete("account", parentId);

            Assert.False(ctx.Data["account"].ContainsKey(parentId));
            Assert.True(ctx.Data["contact"].ContainsKey(childId)); // child untouched
        }

        [Fact]
        public void Cascade_deletes_grandchildren_recursively()
        {
            var ctx = NewContext();
            ctx.AddCascadeDeleteRelationship(
                "account_contacts", "account", "contact", "parentcustomerid", CascadeType.Cascade);
            ctx.AddCascadeDeleteRelationship(
                "contact_tasks", "contact", "task", "regardingobjectid", CascadeType.Cascade);

            var accountId = Guid.NewGuid();
            var contactId = Guid.NewGuid();
            var taskId = Guid.NewGuid();

            var account = new Entity("account") { Id = accountId };
            var contact = new Entity("contact") { Id = contactId };
            contact["parentcustomerid"] = new EntityReference("account", accountId);
            var task = new Entity("task") { Id = taskId };
            task["regardingobjectid"] = new EntityReference("contact", contactId);

            ctx.Initialize(new List<Entity> { account, contact, task });

            ctx.GetOrganizationService().Delete("account", accountId);

            Assert.False(ctx.Data["account"].ContainsKey(accountId));
            Assert.False(ctx.Data["contact"].ContainsKey(contactId));
            Assert.False(ctx.Data["task"].ContainsKey(taskId));
        }

        [Fact]
        public void Self_referential_cascade_terminates_without_infinite_loop()
        {
            var ctx = NewContext();
            // account.parentaccountid -> account (self-referential 1:N)
            ctx.AddCascadeDeleteRelationship(
                "account_parent_account", "account", "account", "parentaccountid", CascadeType.Cascade);

            var rootId = Guid.NewGuid();
            var childId = Guid.NewGuid();
            var grandChildId = Guid.NewGuid();

            var root = new Entity("account") { Id = rootId };
            var child = new Entity("account") { Id = childId };
            child["parentaccountid"] = new EntityReference("account", rootId);
            var grandChild = new Entity("account") { Id = grandChildId };
            grandChild["parentaccountid"] = new EntityReference("account", childId);

            ctx.Initialize(new List<Entity> { root, child, grandChild });

            ctx.GetOrganizationService().Delete("account", rootId);

            Assert.Empty(ctx.Data["account"]);
        }

        [Fact]
        public void Cyclic_self_reference_does_not_loop_forever()
        {
            var ctx = NewContext();
            ctx.AddCascadeDeleteRelationship(
                "account_parent_account", "account", "account", "parentaccountid", CascadeType.Cascade);

            var aId = Guid.NewGuid();
            var bId = Guid.NewGuid();
            var a = new Entity("account") { Id = aId };
            var b = new Entity("account") { Id = bId };
            // A references B and B references A (a cycle).
            a["parentaccountid"] = new EntityReference("account", bId);
            b["parentaccountid"] = new EntityReference("account", aId);

            ctx.Initialize(new List<Entity> { a, b });

            ctx.GetOrganizationService().Delete("account", aId);

            // Both eventually removed; importantly, the call returns (no stack overflow / hang).
            Assert.Empty(ctx.Data["account"]);
        }
    }
}
