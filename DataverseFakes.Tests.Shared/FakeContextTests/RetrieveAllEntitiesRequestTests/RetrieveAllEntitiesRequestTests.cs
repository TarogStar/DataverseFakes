using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using System.Linq;
using Xunit;

namespace DataverseFakes.Tests.FakeContextTests.RetrieveAllEntitiesRequestTests
{
    public class RetrieveAllEntitiesRequestTests
    {
        [Fact]
        public void RetrieveAllEntities_EmptyContext_ReturnsEmpty()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var response = (RetrieveAllEntitiesResponse)service.Execute(new RetrieveAllEntitiesRequest());

            Assert.NotNull(response);
            var entities = (EntityMetadata[])response.Results["EntityMetadata"];
            Assert.Empty(entities);
        }

        [Fact]
        public void RetrieveAllEntities_WithOneEntity_ReturnsOne()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();
            context.InitializeMetadata(new EntityMetadata { LogicalName = "account" });

            var response = (RetrieveAllEntitiesResponse)service.Execute(new RetrieveAllEntitiesRequest());

            var entities = (EntityMetadata[])response.Results["EntityMetadata"];
            Assert.Single(entities);
        }

        [Fact]
        public void RetrieveAllEntities_WithMultipleEntities_ReturnsAll()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();
            context.InitializeMetadata(new[] {
                new EntityMetadata { LogicalName = "account" },
                new EntityMetadata { LogicalName = "contact" },
                new EntityMetadata { LogicalName = "opportunity" }
            });

            var response = (RetrieveAllEntitiesResponse)service.Execute(new RetrieveAllEntitiesRequest());

            var entities = (EntityMetadata[])response.Results["EntityMetadata"];
            Assert.Equal(3, entities.Length);
        }

        [Fact]
        public void RetrieveAllEntities_ResponseHasCorrectType()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var response = service.Execute(new RetrieveAllEntitiesRequest());

            Assert.IsType<RetrieveAllEntitiesResponse>(response);
        }

        [Fact]
        public void RetrieveAllEntities_ReturnsEntityLogicalNames()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();
            context.InitializeMetadata(new[] {
                new EntityMetadata { LogicalName = "account" },
                new EntityMetadata { LogicalName = "contact" }
            });

            var response = (RetrieveAllEntitiesResponse)service.Execute(new RetrieveAllEntitiesRequest());

            var entities = (EntityMetadata[])response.Results["EntityMetadata"];
            var names = entities.Select(e => e.LogicalName).ToArray();
            Assert.Contains("account", names);
            Assert.Contains("contact", names);
        }

        [Fact]
        public void RetrieveAllEntities_WithEntityFiltersEntity_ReturnsAll()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();
            context.InitializeMetadata(new[] {
                new EntityMetadata { LogicalName = "account" },
                new EntityMetadata { LogicalName = "contact" }
            });

            var request = new RetrieveAllEntitiesRequest
            {
                EntityFilters = EntityFilters.Entity
            };
            var response = (RetrieveAllEntitiesResponse)service.Execute(request);

            var entities = (EntityMetadata[])response.Results["EntityMetadata"];
            Assert.Equal(2, entities.Length);
        }
    }
}
