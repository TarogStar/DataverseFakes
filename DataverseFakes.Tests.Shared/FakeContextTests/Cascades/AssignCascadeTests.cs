using System;
using System.Collections.Generic;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using DataverseFakes.Extensions;
using Xunit;

namespace DataverseFakes.Tests.FakeContextTests.Cascades
{
    public class AssignCascadeTests
    {
        [Fact]
        public void Assign_cascade_sets_child_ownerid()
        {
            var context = new XrmFakedContext();

            // Register a 1:N rule with Assign = Cascade via metadata so AssignBehavior is captured.
            var accountMetadata = new EntityMetadata { LogicalName = "account" };
            var rel = new OneToManyRelationshipMetadata();
            rel.SetSealedPropertyValue("SchemaName", "account_contacts");
            rel.SetSealedPropertyValue("ReferencedEntity", "account");
            rel.SetSealedPropertyValue("ReferencedAttribute", "accountid");
            rel.SetSealedPropertyValue("ReferencingEntity", "contact");
            rel.SetSealedPropertyValue("ReferencingAttribute", "parentcustomerid");
            rel.CascadeConfiguration = new CascadeConfiguration { Assign = CascadeType.Cascade };
            accountMetadata.SetSealedPropertyValue("OneToManyRelationships",
                new OneToManyRelationshipMetadata[] { rel });
            context.InitializeMetadata(accountMetadata);

            var oldOwner = new EntityReference("systemuser", Guid.NewGuid());
            var newOwner = new EntityReference("systemuser", Guid.NewGuid());

            var parentId = Guid.NewGuid();
            var childId = Guid.NewGuid();
            var parent = new Entity("account") { Id = parentId };
            parent["ownerid"] = oldOwner;
            var child = new Entity("contact") { Id = childId };
            child["parentcustomerid"] = new EntityReference("account", parentId);
            child["ownerid"] = oldOwner;
            context.Initialize(new List<Entity> { parent, child });

            var service = context.GetOrganizationService();
            service.Execute(new AssignRequest
            {
                Target = new EntityReference("account", parentId),
                Assignee = newOwner
            });

            Assert.Equal(newOwner.Id, context.Data["account"][parentId].GetAttributeValue<EntityReference>("ownerid").Id);
            Assert.Equal(newOwner.Id, context.Data["contact"][childId].GetAttributeValue<EntityReference>("ownerid").Id);
        }
    }
}
