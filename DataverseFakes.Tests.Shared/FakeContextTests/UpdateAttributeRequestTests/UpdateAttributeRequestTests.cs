using DataverseFakes.Extensions;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using System.Linq;
using System.ServiceModel;
using Xunit;

namespace DataverseFakes.Tests.FakeContextTests.UpdateAttributeRequestTests
{
    public class UpdateAttributeRequestTests
    {
        [Fact]
        public void UpdateAttribute_ShouldSucceed()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var entityMetadata = new EntityMetadata { LogicalName = "account" };
            entityMetadata.SetAttributeCollection(new AttributeMetadata[]
            {
                new StringAttributeMetadata { LogicalName = "new_name", MaxLength = 100 }
            });
            context.SetEntityMetadata(entityMetadata);

            var updatedAttr = new StringAttributeMetadata { LogicalName = "new_name", MaxLength = 500 };
            var request = new UpdateAttributeRequest
            {
                EntityName = "account",
                Attribute = updatedAttr
            };

            var response = service.Execute(request);
            Assert.NotNull(response);
            Assert.IsType<UpdateAttributeResponse>(response);
        }

        [Fact]
        public void UpdateAttribute_EntityNotFound_ShouldThrow()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var request = new UpdateAttributeRequest
            {
                EntityName = "nonexistent",
                Attribute = new StringAttributeMetadata { LogicalName = "new_name" }
            };

            Assert.Throws<FaultException<OrganizationServiceFault>>(() => service.Execute(request));
        }

        [Fact]
        public void UpdateAttribute_AttributeNotFound_ShouldThrow()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();
            context.InitializeMetadata(new EntityMetadata { LogicalName = "account" });

            var request = new UpdateAttributeRequest
            {
                EntityName = "account",
                Attribute = new StringAttributeMetadata { LogicalName = "nonexistent_attr" }
            };

            Assert.Throws<FaultException<OrganizationServiceFault>>(() => service.Execute(request));
        }

        [Fact]
        public void UpdateAttribute_NullEntityName_ShouldThrow()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var request = new UpdateAttributeRequest
            {
                EntityName = null,
                Attribute = new StringAttributeMetadata { LogicalName = "new_name" }
            };

            Assert.Throws<FaultException<OrganizationServiceFault>>(() => service.Execute(request));
        }

        [Fact]
        public void UpdateAttribute_NullAttribute_ShouldThrow()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();
            context.InitializeMetadata(new EntityMetadata { LogicalName = "account" });

            var request = new UpdateAttributeRequest
            {
                EntityName = "account",
                Attribute = null
            };

            Assert.Throws<FaultException<OrganizationServiceFault>>(() => service.Execute(request));
        }

        [Fact]
        public void UpdateAttribute_ChangesArePersistedToMetadata()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var entityMetadata = new EntityMetadata { LogicalName = "account" };
            entityMetadata.SetAttributeCollection(new AttributeMetadata[]
            {
                new StringAttributeMetadata { LogicalName = "new_name", MaxLength = 100 }
            });
            context.SetEntityMetadata(entityMetadata);

            var updatedAttr = new StringAttributeMetadata { LogicalName = "new_name", MaxLength = 999 };
            service.Execute(new UpdateAttributeRequest { EntityName = "account", Attribute = updatedAttr });

            var retrieved = context.GetEntityMetadataByName("account");
            var attr = retrieved.Attributes.OfType<StringAttributeMetadata>().First(a => a.LogicalName == "new_name");
            Assert.Equal(999, attr.MaxLength);
        }

        [Fact]
        public void UpdateAttribute_ResponseIsCorrectType()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var entityMetadata = new EntityMetadata { LogicalName = "contact" };
            entityMetadata.SetAttributeCollection(new AttributeMetadata[]
            {
                new StringAttributeMetadata { LogicalName = "firstname" }
            });
            context.SetEntityMetadata(entityMetadata);

            var response = service.Execute(new UpdateAttributeRequest
            {
                EntityName = "contact",
                Attribute = new StringAttributeMetadata { LogicalName = "firstname" }
            });

            Assert.IsType<UpdateAttributeResponse>(response);
        }
    }
}
