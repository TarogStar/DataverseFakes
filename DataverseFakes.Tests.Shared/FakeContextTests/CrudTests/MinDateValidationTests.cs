using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.ServiceModel;
using Xunit;

namespace FakeXrmEasy.Tests.CrudTests
{
    /// <summary>
    /// Tests for minimum date validation in CRUD operations.
    /// CRM/Dataverse (SQL Server) doesn't support dates before 01/01/1753.
    /// See: https://github.com/DynamicsValue/fake-xrm-easy/issues/562
    /// </summary>
    public class MinDateValidationTests
    {
        #region Create Tests

        [Fact]
        public void When_creating_entity_with_datetime_min_value_an_exception_is_thrown()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var entity = new Entity("account")
            {
                Id = Guid.NewGuid(),
                ["name"] = "Test Account",
                ["createdon"] = DateTime.MinValue
            };

            var ex = Assert.Throws<FaultException<OrganizationServiceFault>>(() => service.Create(entity));
            Assert.Contains("Date is less than the minimum value supported by CrmDateTime", ex.Message);
            Assert.Contains("Minimum value supported: 01/01/1753 00:00:00", ex.Message);
        }

        [Fact]
        public void When_creating_entity_with_date_before_1753_an_exception_is_thrown()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var entity = new Entity("account")
            {
                Id = Guid.NewGuid(),
                ["name"] = "Test Account",
                ["somedate"] = new DateTime(1752, 12, 31)
            };

            var ex = Assert.Throws<FaultException<OrganizationServiceFault>>(() => service.Create(entity));
            Assert.Contains("Date is less than the minimum value supported by CrmDateTime", ex.Message);
        }

        [Fact]
        public void When_creating_entity_with_date_on_1753_boundary_it_should_succeed()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var minSupportedDate = new DateTime(1753, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var entity = new Entity("account")
            {
                Id = Guid.NewGuid(),
                ["name"] = "Test Account",
                ["somedate"] = minSupportedDate
            };

            var id = service.Create(entity);

            Assert.NotEqual(Guid.Empty, id);
            var retrieved = service.Retrieve("account", id, new ColumnSet(true));
            Assert.Equal(minSupportedDate, retrieved.GetAttributeValue<DateTime>("somedate"));
        }

        [Fact]
        public void When_creating_entity_with_date_after_1753_it_should_succeed()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var testDate = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);
            var entity = new Entity("account")
            {
                Id = Guid.NewGuid(),
                ["name"] = "Test Account",
                ["somedate"] = testDate
            };

            var id = service.Create(entity);

            Assert.NotEqual(Guid.Empty, id);
            var retrieved = service.Retrieve("account", id, new ColumnSet(true));
            Assert.Equal(testDate, retrieved.GetAttributeValue<DateTime>("somedate"));
        }

        #endregion

        #region Update Tests

        [Fact]
        public void When_updating_entity_with_datetime_min_value_an_exception_is_thrown()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            // First create a valid entity
            var entity = new Entity("account")
            {
                Id = Guid.NewGuid(),
                ["name"] = "Test Account",
                ["somedate"] = DateTime.UtcNow
            };
            var id = service.Create(entity);

            // Now try to update with invalid date
            var updateEntity = new Entity("account")
            {
                Id = id,
                ["somedate"] = DateTime.MinValue
            };

            var ex = Assert.Throws<FaultException<OrganizationServiceFault>>(() => service.Update(updateEntity));
            Assert.Contains("Date is less than the minimum value supported by CrmDateTime", ex.Message);
            Assert.Contains("Minimum value supported: 01/01/1753 00:00:00", ex.Message);
        }

        [Fact]
        public void When_updating_entity_with_date_before_1753_an_exception_is_thrown()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            // First create a valid entity
            var entity = new Entity("account")
            {
                Id = Guid.NewGuid(),
                ["name"] = "Test Account"
            };
            var id = service.Create(entity);

            // Now try to update with invalid date
            var updateEntity = new Entity("account")
            {
                Id = id,
                ["somedate"] = new DateTime(1700, 1, 1)
            };

            var ex = Assert.Throws<FaultException<OrganizationServiceFault>>(() => service.Update(updateEntity));
            Assert.Contains("Date is less than the minimum value supported by CrmDateTime", ex.Message);
        }

        [Fact]
        public void When_updating_entity_with_date_on_1753_boundary_it_should_succeed()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            // First create a valid entity
            var entity = new Entity("account")
            {
                Id = Guid.NewGuid(),
                ["name"] = "Test Account"
            };
            var id = service.Create(entity);

            // Now update with boundary date
            var minSupportedDate = new DateTime(1753, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var updateEntity = new Entity("account")
            {
                Id = id,
                ["somedate"] = minSupportedDate
            };

            service.Update(updateEntity);

            var retrieved = service.Retrieve("account", id, new ColumnSet(true));
            Assert.Equal(minSupportedDate, retrieved.GetAttributeValue<DateTime>("somedate"));
        }

        [Fact]
        public void When_updating_entity_with_date_after_1753_it_should_succeed()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            // First create a valid entity
            var entity = new Entity("account")
            {
                Id = Guid.NewGuid(),
                ["name"] = "Test Account"
            };
            var id = service.Create(entity);

            // Now update with valid date
            var testDate = new DateTime(2024, 6, 15, 14, 30, 0, DateTimeKind.Utc);
            var updateEntity = new Entity("account")
            {
                Id = id,
                ["somedate"] = testDate
            };

            service.Update(updateEntity);

            var retrieved = service.Retrieve("account", id, new ColumnSet(true));
            Assert.Equal(testDate, retrieved.GetAttributeValue<DateTime>("somedate"));
        }

        #endregion

        #region Initialize Tests

        [Fact]
        public void When_initializing_context_with_datetime_min_value_an_exception_is_thrown()
        {
            var context = new XrmFakedContext();

            var entity = new Entity("account")
            {
                Id = Guid.NewGuid(),
                ["name"] = "Test Account",
                ["somedate"] = DateTime.MinValue
            };

            var ex = Assert.Throws<FaultException<OrganizationServiceFault>>(() => context.Initialize(entity));
            Assert.Contains("Date is less than the minimum value supported by CrmDateTime", ex.Message);
        }

        [Fact]
        public void When_initializing_context_with_date_before_1753_an_exception_is_thrown()
        {
            var context = new XrmFakedContext();

            var entity = new Entity("account")
            {
                Id = Guid.NewGuid(),
                ["name"] = "Test Account",
                ["somedate"] = new DateTime(1000, 5, 15)
            };

            var ex = Assert.Throws<FaultException<OrganizationServiceFault>>(() => context.Initialize(entity));
            Assert.Contains("Date is less than the minimum value supported by CrmDateTime", ex.Message);
        }

        [Fact]
        public void When_initializing_context_with_valid_date_it_should_succeed()
        {
            var context = new XrmFakedContext();

            var testDate = new DateTime(1753, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var entity = new Entity("account")
            {
                Id = Guid.NewGuid(),
                ["name"] = "Test Account",
                ["somedate"] = testDate
            };

            context.Initialize(entity);

            var service = context.GetOrganizationService();
            var retrieved = service.Retrieve("account", entity.Id, new ColumnSet(true));
            Assert.Equal(testDate, retrieved.GetAttributeValue<DateTime>("somedate"));
        }

        #endregion
    }
}
