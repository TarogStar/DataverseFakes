using DataverseFakes.Extensions;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Linq;
using System.ServiceModel;
using Xunit;

namespace DataverseFakes.Tests.FakeContextTests.CreateAttributeRequestTests
{
    public class CreateAttributeRequestTests
    {
        [Fact]
        public void CreateAttribute_ShouldSucceed_AndReturnAttributeId()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();
            context.InitializeMetadata(new EntityMetadata { LogicalName = "account" });

            var request = new CreateAttributeRequest
            {
                EntityName = "account",
                Attribute = new StringAttributeMetadata { LogicalName = "new_name", SchemaName = "new_Name" }
            };

            var response = (CreateAttributeResponse)service.Execute(request);

            Assert.NotNull(response);
            var attributeId = (Guid)response.Results["AttributeId"];
            Assert.NotEqual(Guid.Empty, attributeId);
        }

        [Fact]
        public void CreateAttribute_EntityNotFound_ShouldThrow()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var request = new CreateAttributeRequest
            {
                EntityName = "nonexistent",
                Attribute = new StringAttributeMetadata { LogicalName = "new_name" }
            };

            Assert.Throws<FaultException<OrganizationServiceFault>>(() => service.Execute(request));
        }

        [Fact]
        public void CreateAttribute_NullEntityName_ShouldThrow()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var request = new CreateAttributeRequest
            {
                EntityName = null,
                Attribute = new StringAttributeMetadata { LogicalName = "new_name" }
            };

            Assert.Throws<FaultException<OrganizationServiceFault>>(() => service.Execute(request));
        }

        [Fact]
        public void CreateAttribute_NullAttribute_ShouldThrow()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();
            context.InitializeMetadata(new EntityMetadata { LogicalName = "account" });

            var request = new CreateAttributeRequest
            {
                EntityName = "account",
                Attribute = null
            };

            Assert.Throws<FaultException<OrganizationServiceFault>>(() => service.Execute(request));
        }

        [Fact]
        public void CreateAttribute_DuplicateLogicalName_ShouldThrow()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var entityMetadata = new EntityMetadata { LogicalName = "account" };
            entityMetadata.SetAttributeCollection(new AttributeMetadata[]
            {
                new StringAttributeMetadata { LogicalName = "new_name" }
            });
            context.SetEntityMetadata(entityMetadata);

            var request = new CreateAttributeRequest
            {
                EntityName = "account",
                Attribute = new StringAttributeMetadata { LogicalName = "new_name" }
            };

            Assert.Throws<FaultException<OrganizationServiceFault>>(() => service.Execute(request));
        }

        [Fact]
        public void CreateAttribute_AttributeCanBeRetrievedAfterCreation()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();
            context.InitializeMetadata(new EntityMetadata { LogicalName = "account" });

            var request = new CreateAttributeRequest
            {
                EntityName = "account",
                Attribute = new StringAttributeMetadata { LogicalName = "new_description", MaxLength = 500 }
            };

            service.Execute(request);

            var entityMeta = context.GetEntityMetadataByName("account");
            Assert.NotNull(entityMeta.Attributes);
            var attr = entityMeta.Attributes.FirstOrDefault(a => a.LogicalName == "new_description");
            Assert.NotNull(attr);
        }

        [Fact]
        public void CreateAttribute_MetadataIdIsAssigned()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();
            context.InitializeMetadata(new EntityMetadata { LogicalName = "account" });

            var attribute = new StringAttributeMetadata { LogicalName = "new_code" };
            Assert.False(attribute.MetadataId.HasValue);

            var request = new CreateAttributeRequest
            {
                EntityName = "account",
                Attribute = attribute
            };

            var response = (CreateAttributeResponse)service.Execute(request);

            var attributeId = (Guid)response.Results["AttributeId"];
            Assert.NotEqual(Guid.Empty, attributeId);
        }
    }
}
