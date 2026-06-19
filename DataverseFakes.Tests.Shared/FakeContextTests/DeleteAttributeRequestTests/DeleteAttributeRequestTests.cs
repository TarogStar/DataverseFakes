using DataverseFakes.Extensions;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using System.Linq;
using System.ServiceModel;
using Xunit;

namespace DataverseFakes.Tests.FakeContextTests.DeleteAttributeRequestTests
{
    public class DeleteAttributeRequestTests
    {
        [Fact]
        public void DeleteAttribute_ShouldSucceed()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var entityMetadata = new EntityMetadata { LogicalName = "account" };
            entityMetadata.SetAttributeCollection(new AttributeMetadata[]
            {
                new StringAttributeMetadata { LogicalName = "new_name" }
            });
            context.SetEntityMetadata(entityMetadata);

            var request = new DeleteAttributeRequest
            {
                EntityLogicalName = "account",
                LogicalName = "new_name"
            };

            var response = service.Execute(request);
            Assert.NotNull(response);
            Assert.IsType<DeleteAttributeResponse>(response);
        }

        [Fact]
        public void DeleteAttribute_EntityNotFound_ShouldThrow()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var request = new DeleteAttributeRequest
            {
                EntityLogicalName = "nonexistent",
                LogicalName = "new_name"
            };

            Assert.Throws<FaultException<OrganizationServiceFault>>(() => service.Execute(request));
        }

        [Fact]
        public void DeleteAttribute_AttributeNotFound_ShouldThrow()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();
            context.InitializeMetadata(new EntityMetadata { LogicalName = "account" });

            var request = new DeleteAttributeRequest
            {
                EntityLogicalName = "account",
                LogicalName = "nonexistent_attr"
            };

            Assert.Throws<FaultException<OrganizationServiceFault>>(() => service.Execute(request));
        }

        [Fact]
        public void DeleteAttribute_NullEntityLogicalName_ShouldThrow()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var request = new DeleteAttributeRequest
            {
                EntityLogicalName = null,
                LogicalName = "new_name"
            };

            Assert.Throws<FaultException<OrganizationServiceFault>>(() => service.Execute(request));
        }

        [Fact]
        public void DeleteAttribute_NullAttributeLogicalName_ShouldThrow()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();
            context.InitializeMetadata(new EntityMetadata { LogicalName = "account" });

            var request = new DeleteAttributeRequest
            {
                EntityLogicalName = "account",
                LogicalName = null
            };

            Assert.Throws<FaultException<OrganizationServiceFault>>(() => service.Execute(request));
        }

        [Fact]
        public void DeleteAttribute_RemovedAttributeNotInMetadata()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var entityMetadata = new EntityMetadata { LogicalName = "account" };
            entityMetadata.SetAttributeCollection(new AttributeMetadata[]
            {
                new StringAttributeMetadata { LogicalName = "new_field" }
            });
            context.SetEntityMetadata(entityMetadata);

            service.Execute(new DeleteAttributeRequest
            {
                EntityLogicalName = "account",
                LogicalName = "new_field"
            });

            var retrieved = context.GetEntityMetadataByName("account");
            Assert.False(retrieved.Attributes?.Any(a => a.LogicalName == "new_field") ?? false);
        }

        [Fact]
        public void DeleteAttribute_OtherAttributesPreserved()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var entityMetadata = new EntityMetadata { LogicalName = "account" };
            entityMetadata.SetAttributeCollection(new AttributeMetadata[]
            {
                new StringAttributeMetadata { LogicalName = "new_field1" },
                new StringAttributeMetadata { LogicalName = "new_field2" },
                new StringAttributeMetadata { LogicalName = "new_field3" }
            });
            context.SetEntityMetadata(entityMetadata);

            service.Execute(new DeleteAttributeRequest
            {
                EntityLogicalName = "account",
                LogicalName = "new_field2"
            });

            var retrieved = context.GetEntityMetadataByName("account");
            Assert.Equal(2, retrieved.Attributes.Length);
            Assert.Contains(retrieved.Attributes, a => a.LogicalName == "new_field1");
            Assert.Contains(retrieved.Attributes, a => a.LogicalName == "new_field3");
            Assert.DoesNotContain(retrieved.Attributes, a => a.LogicalName == "new_field2");
        }
    }
}
