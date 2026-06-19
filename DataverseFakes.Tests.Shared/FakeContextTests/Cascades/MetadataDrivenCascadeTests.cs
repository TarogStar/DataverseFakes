using System;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using DataverseFakes.Extensions;
using Xunit;

namespace DataverseFakes.Tests.FakeContextTests.Cascades
{
    public class MetadataDrivenCascadeTests
    {
        private static OneToManyRelationshipMetadata BuildOneToMany(
            string schema, string referenced, string referencing, string referencingAttr, CascadeType delete)
        {
            var rel = new OneToManyRelationshipMetadata();
            rel.SetSealedPropertyValue("SchemaName", schema);
            rel.SetSealedPropertyValue("ReferencedEntity", referenced);
            rel.SetSealedPropertyValue("ReferencedAttribute", referenced + "id");
            rel.SetSealedPropertyValue("ReferencingEntity", referencing);
            rel.SetSealedPropertyValue("ReferencingAttribute", referencingAttr);
            rel.CascadeConfiguration = new CascadeConfiguration { Delete = delete };
            return rel;
        }

        [Fact]
        public void Cascade_delete_is_driven_by_initialized_entity_metadata()
        {
            var context = new XrmFakedContext();

            var accountMetadata = new EntityMetadata { LogicalName = "account" };
            var rel = BuildOneToMany("account_contacts", "account", "contact", "parentcustomerid", CascadeType.Cascade);
            accountMetadata.SetSealedPropertyValue("OneToManyRelationships",
                new OneToManyRelationshipMetadata[] { rel });

            context.InitializeMetadata(accountMetadata);

            var parentId = Guid.NewGuid();
            var childId = Guid.NewGuid();
            var parent = new Entity("account") { Id = parentId };
            var child = new Entity("contact") { Id = childId };
            child["parentcustomerid"] = new EntityReference("account", parentId);
            context.Initialize(new List<Entity> { parent, child });

            context.GetOrganizationService().Delete("account", parentId);

            Assert.False(context.Data["account"].ContainsKey(parentId));
            Assert.False(context.Data["contact"].ContainsKey(childId));
        }

        [Fact]
        public void Restrict_from_metadata_throws_when_children_exist()
        {
            var context = new XrmFakedContext();
            var accountMetadata = new EntityMetadata { LogicalName = "account" };
            var rel = BuildOneToMany("account_contacts", "account", "contact", "parentcustomerid", CascadeType.Restrict);
            accountMetadata.SetSealedPropertyValue("OneToManyRelationships",
                new OneToManyRelationshipMetadata[] { rel });
            context.InitializeMetadata(accountMetadata);

            var parentId = Guid.NewGuid();
            var childId = Guid.NewGuid();
            var parent = new Entity("account") { Id = parentId };
            var child = new Entity("contact") { Id = childId };
            child["parentcustomerid"] = new EntityReference("account", parentId);
            context.Initialize(new List<Entity> { parent, child });

            Assert.Throws<System.ServiceModel.FaultException<Microsoft.Xrm.Sdk.OrganizationServiceFault>>(
                () => context.GetOrganizationService().Delete("account", parentId));
            Assert.True(context.Data["account"].ContainsKey(parentId));
            Assert.True(context.Data["contact"].ContainsKey(childId));
        }
    }
}
