using Crm;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using System;
using System.Linq;
using Xunit;

namespace FakeXrmEasy.Tests.FakeContextTests
{
    public class FakeContextTestCalculateRollupField
    {
        [Fact]
        public void When_Executing_CalculateRollupField_Request_Should_Not_Throw_Exception()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var accountId = Guid.NewGuid();
            var account = new Account
            {
                Id = accountId,
                Name = "Test Account"
            };

            context.Initialize(new[] { account });

            // Act
            var calculateRollupRequest = new CalculateRollupFieldRequest
            {
                Target = new EntityReference("account", accountId),
                FieldName = "revenue"
            };

            // Should not throw an exception
            var response = service.Execute(calculateRollupRequest) as CalculateRollupFieldResponse;

            // Assert
            Assert.NotNull(response);
            Assert.NotNull(response.Entity);
        }

        [Fact]
        public void When_Executing_CalculateRollupField_Request_With_Existing_Value_Should_Preserve_Value()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var accountId = Guid.NewGuid();
            var expectedRevenue = new Money(50000m);
            var account = new Account
            {
                Id = accountId,
                Name = "Test Account",
                Revenue = expectedRevenue
            };

            context.Initialize(new[] { account });

            // Act
            var calculateRollupRequest = new CalculateRollupFieldRequest
            {
                Target = new EntityReference("account", accountId),
                FieldName = "revenue"
            };

            var response = service.Execute(calculateRollupRequest) as CalculateRollupFieldResponse;

            // Assert
            Assert.NotNull(response);
            Assert.NotNull(response.Entity);
            var resultEntity = response.Entity as Entity;
            Assert.True(resultEntity.Contains("revenue"));
            Assert.Equal(expectedRevenue.Value, ((Money)resultEntity["revenue"]).Value);
        }

        [Fact]
        public void When_Executing_CalculateRollupField_Request_Without_Target_Should_Throw_Exception()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            // Act & Assert
            var calculateRollupRequest = new CalculateRollupFieldRequest
            {
                Target = null,
                FieldName = "revenue"
            };

            Assert.Throws<ArgumentNullException>(() => service.Execute(calculateRollupRequest));
        }

        [Fact]
        public void When_Executing_CalculateRollupField_Request_Without_FieldName_Should_Throw_Exception()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var accountId = Guid.NewGuid();
            var account = new Account
            {
                Id = accountId,
                Name = "Test Account"
            };

            context.Initialize(new[] { account });

            // Act & Assert
            var calculateRollupRequest = new CalculateRollupFieldRequest
            {
                Target = new EntityReference("account", accountId),
                FieldName = null
            };

            Assert.Throws<ArgumentNullException>(() => service.Execute(calculateRollupRequest));
        }

        [Fact]
        public void When_Executing_CalculateRollupField_Request_With_Nonexistent_Entity_Should_Throw_Exception()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var accountId = Guid.NewGuid();

            // Act & Assert
            var calculateRollupRequest = new CalculateRollupFieldRequest
            {
                Target = new EntityReference("account", accountId),
                FieldName = "revenue"
            };

            Assert.Throws<InvalidOperationException>(() => service.Execute(calculateRollupRequest));
        }

        [Fact]
        public void When_Executing_CalculateRollupField_Request_With_Late_Bound_Entity_Should_Work()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var accountId = Guid.NewGuid();
            var account = new Entity("account")
            {
                Id = accountId,
                ["name"] = "Test Account",
                ["revenue"] = new Money(75000m)
            };

            context.Initialize(new[] { account });

            // Act
            var calculateRollupRequest = new CalculateRollupFieldRequest
            {
                Target = new EntityReference("account", accountId),
                FieldName = "revenue"
            };

            var response = service.Execute(calculateRollupRequest) as CalculateRollupFieldResponse;

            // Assert
            Assert.NotNull(response);
            Assert.NotNull(response.Entity);
            var resultEntity = response.Entity as Entity;
            Assert.True(resultEntity.Contains("revenue"));
            Assert.Equal(75000m, ((Money)resultEntity["revenue"]).Value);
        }

        [Fact]
        public void When_Executing_CalculateRollupField_Request_Multiple_Times_Should_Succeed()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var accountId = Guid.NewGuid();
            var account = new Account
            {
                Id = accountId,
                Name = "Test Account",
                Revenue = new Money(10000m)
            };

            context.Initialize(new[] { account });

            // Act - Execute multiple times
            var calculateRollupRequest = new CalculateRollupFieldRequest
            {
                Target = new EntityReference("account", accountId),
                FieldName = "revenue"
            };

            var response1 = service.Execute(calculateRollupRequest) as CalculateRollupFieldResponse;
            var response2 = service.Execute(calculateRollupRequest) as CalculateRollupFieldResponse;
            var response3 = service.Execute(calculateRollupRequest) as CalculateRollupFieldResponse;

            // Assert - All executions should succeed
            Assert.NotNull(response1);
            Assert.NotNull(response2);
            Assert.NotNull(response3);
        }
    }
}
