using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Linq;
using Xunit;

namespace FakeXrmEasy.Tests.BulkOperationTests
{
    /// <summary>
    /// Tests for modern bulk operations (v1.0.2):
    /// CreateMultiple, UpdateMultiple, DeleteMultiple, UpsertMultiple
    /// </summary>
    public class BulkOperationTests
    {
        #region CreateMultiple Tests

        [Fact]
        public void When_CreateMultiple_With_Valid_Entities_Should_Create_All_Records()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var accounts = new EntityCollection();
            accounts.Entities.Add(new Entity("account") { ["name"] = "Account 1" });
            accounts.Entities.Add(new Entity("account") { ["name"] = "Account 2" });
            accounts.Entities.Add(new Entity("account") { ["name"] = "Account 3" });

            var request = new CreateMultipleRequest { Targets = accounts };

            // Act
            var response = (CreateMultipleResponse)service.Execute(request);

            // Assert
            Assert.Equal(3, response.Ids.Length);
            Assert.All(response.Ids, id => Assert.NotEqual(Guid.Empty, id));

            // Verify records exist in context
            var createdAccounts = context.CreateQuery("account").ToList();
            Assert.Equal(3, createdAccounts.Count);
            Assert.Contains(createdAccounts, a => a.GetAttributeValue<string>("name") == "Account 1");
            Assert.Contains(createdAccounts, a => a.GetAttributeValue<string>("name") == "Account 2");
            Assert.Contains(createdAccounts, a => a.GetAttributeValue<string>("name") == "Account 3");
        }

        [Fact]
        public void When_CreateMultiple_With_Null_Targets_Should_Throw_Exception()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var request = new CreateMultipleRequest { Targets = null };

            // Act & Assert
            Assert.Throws<System.ServiceModel.FaultException<OrganizationServiceFault>>(() =>
                service.Execute(request));
        }

        [Fact]
        public void When_CreateMultiple_With_Empty_Collection_Should_Throw_Exception()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var request = new CreateMultipleRequest { Targets = new EntityCollection() };

            // Act & Assert
            Assert.Throws<System.ServiceModel.FaultException<OrganizationServiceFault>>(() =>
                service.Execute(request));
        }

        [Fact]
        public void When_CreateMultiple_With_Null_Entity_In_Collection_Should_Throw_Exception()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var accounts = new EntityCollection();
            accounts.Entities.Add(new Entity("account") { ["name"] = "Account 1" });
            accounts.Entities.Add(null);
            accounts.Entities.Add(new Entity("account") { ["name"] = "Account 3" });

            var request = new CreateMultipleRequest { Targets = accounts };

            // Act & Assert
            Assert.Throws<System.ServiceModel.FaultException<OrganizationServiceFault>>(() =>
                service.Execute(request));
        }

        #endregion

        #region UpdateMultiple Tests

        [Fact]
        public void When_UpdateMultiple_With_Valid_Entities_Should_Update_All_Records()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            // Create initial records
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            var id3 = Guid.NewGuid();

            context.Initialize(new[]
            {
                new Entity("account") { Id = id1, ["name"] = "Account 1", ["revenue"] = new Money(10000) },
                new Entity("account") { Id = id2, ["name"] = "Account 2", ["revenue"] = new Money(20000) },
                new Entity("account") { Id = id3, ["name"] = "Account 3", ["revenue"] = new Money(30000) }
            });

            // Prepare updates
            var updates = new EntityCollection();
            updates.Entities.Add(new Entity("account") { Id = id1, ["revenue"] = new Money(15000) });
            updates.Entities.Add(new Entity("account") { Id = id2, ["revenue"] = new Money(25000) });
            updates.Entities.Add(new Entity("account") { Id = id3, ["revenue"] = new Money(35000) });

            var request = new UpdateMultipleRequest { Targets = updates };

            // Act
            var response = (UpdateMultipleResponse)service.Execute(request);

            // Assert
            Assert.NotNull(response);

            // Verify updates
            var account1 = service.Retrieve("account", id1, new ColumnSet(true));
            var account2 = service.Retrieve("account", id2, new ColumnSet(true));
            var account3 = service.Retrieve("account", id3, new ColumnSet(true));

            Assert.Equal(15000, account1.GetAttributeValue<Money>("revenue").Value);
            Assert.Equal(25000, account2.GetAttributeValue<Money>("revenue").Value);
            Assert.Equal(35000, account3.GetAttributeValue<Money>("revenue").Value);
        }

        [Fact]
        public void When_UpdateMultiple_With_Empty_Guid_Should_Throw_Exception()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var updates = new EntityCollection();
            updates.Entities.Add(new Entity("account") { ["name"] = "Invalid" });

            var request = new UpdateMultipleRequest { Targets = updates };

            // Act & Assert
            Assert.Throws<System.ServiceModel.FaultException<OrganizationServiceFault>>(() =>
                service.Execute(request));
        }

        [Fact]
        public void When_UpdateMultiple_With_NonExistent_Record_Should_Throw_Exception()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var updates = new EntityCollection();
            updates.Entities.Add(new Entity("account") { Id = Guid.NewGuid(), ["name"] = "Updated" });

            var request = new UpdateMultipleRequest { Targets = updates };

            // Act & Assert - should throw because record doesn't exist
            Assert.Throws<System.ServiceModel.FaultException<OrganizationServiceFault>>(() =>
                service.Execute(request));
        }

        #endregion

        #region DeleteMultiple Tests

        [Fact]
        public void When_DeleteMultiple_With_Valid_References_Should_Delete_All_Records()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            // Create records
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            var id3 = Guid.NewGuid();

            context.Initialize(new[]
            {
                new Entity("account") { Id = id1, ["name"] = "Account 1" },
                new Entity("account") { Id = id2, ["name"] = "Account 2" },
                new Entity("account") { Id = id3, ["name"] = "Account 3" }
            });

            // Prepare deletes
            var targets = new EntityReferenceCollection();
            targets.Add(new EntityReference("account", id1));
            targets.Add(new EntityReference("account", id2));
            targets.Add(new EntityReference("account", id3));

            var request = new DeleteMultipleRequest { Targets = targets };

            // Act - There is NO DeleteMultipleResponse, just execute
            var response = service.Execute(request);

            // Assert
            Assert.NotNull(response);

            // Verify all records deleted
            var remainingAccounts = context.CreateQuery("account").ToList();
            Assert.Empty(remainingAccounts);
        }

        [Fact]
        public void When_DeleteMultiple_With_Null_Targets_Should_Throw_Exception()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var request = new DeleteMultipleRequest { Targets = null };

            // Act & Assert
            Assert.Throws<System.ServiceModel.FaultException<OrganizationServiceFault>>(() =>
                service.Execute(request));
        }

        [Fact]
        public void When_DeleteMultiple_With_Empty_Guid_Should_Throw_Exception()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var targets = new EntityReferenceCollection();
            targets.Add(new EntityReference("account", Guid.Empty));

            var request = new DeleteMultipleRequest { Targets = targets };

            // Act & Assert
            Assert.Throws<System.ServiceModel.FaultException<OrganizationServiceFault>>(() =>
                service.Execute(request));
        }

        #endregion

        #region UpsertMultiple Tests

        [Fact]
        public void When_UpsertMultiple_With_New_Entities_Should_Create_All()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var targets = new EntityCollection();
            targets.Entities.Add(new Entity("account") { ["name"] = "New Account 1" });
            targets.Entities.Add(new Entity("account") { ["name"] = "New Account 2" });

            var request = new UpsertMultipleRequest { Targets = targets };

            // Act
            var response = (UpsertMultipleResponse)service.Execute(request);

            // Assert
            Assert.Equal(2, response.Results.Length);
            Assert.All(response.Results, result =>
            {
                Assert.NotEqual(Guid.Empty, result.Target.Id);
                Assert.True(result.RecordCreated);
            });

            // Verify records exist
            var createdAccounts = context.CreateQuery("account").ToList();
            Assert.Equal(2, createdAccounts.Count);
        }

        [Fact]
        public void When_UpsertMultiple_With_Existing_Entities_Should_Update_All()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();

            context.Initialize(new[]
            {
                new Entity("account") { Id = id1, ["name"] = "Account 1" },
                new Entity("account") { Id = id2, ["name"] = "Account 2" }
            });

            var targets = new EntityCollection();
            targets.Entities.Add(new Entity("account") { Id = id1, ["name"] = "Updated 1" });
            targets.Entities.Add(new Entity("account") { Id = id2, ["name"] = "Updated 2" });

            var request = new UpsertMultipleRequest { Targets = targets };

            // Act
            var response = (UpsertMultipleResponse)service.Execute(request);

            // Assert
            Assert.Equal(2, response.Results.Length);
            Assert.All(response.Results, result =>
            {
                Assert.NotEqual(Guid.Empty, result.Target.Id);
                Assert.False(result.RecordCreated); // Should be updates
            });

            // Verify updates
            var account1 = service.Retrieve("account", id1, new ColumnSet(true));
            var account2 = service.Retrieve("account", id2, new ColumnSet(true));

            Assert.Equal("Updated 1", account1.GetAttributeValue<string>("name"));
            Assert.Equal("Updated 2", account2.GetAttributeValue<string>("name"));
        }

        [Fact]
        public void When_UpsertMultiple_With_Mixed_New_And_Existing_Should_Create_And_Update()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var existingId = Guid.NewGuid();
            context.Initialize(new[]
            {
                new Entity("account") { Id = existingId, ["name"] = "Existing Account" }
            });

            var targets = new EntityCollection();
            targets.Entities.Add(new Entity("account") { Id = existingId, ["name"] = "Updated Existing" });
            targets.Entities.Add(new Entity("account") { ["name"] = "New Account" });

            var request = new UpsertMultipleRequest { Targets = targets };

            // Act
            var response = (UpsertMultipleResponse)service.Execute(request);

            // Assert
            Assert.Equal(2, response.Results.Length);

            var existingResult = response.Results.First(r => r.Target.Id == existingId);
            var newResult = response.Results.First(r => r.Target.Id != existingId);

            Assert.False(existingResult.RecordCreated); // Updated
            Assert.True(newResult.RecordCreated); // Created

            // Verify both exist
            var accounts = context.CreateQuery("account").ToList();
            Assert.Equal(2, accounts.Count);
        }

        [Fact]
        public void When_UpsertMultiple_With_Null_Targets_Should_Throw_Exception()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var request = new UpsertMultipleRequest { Targets = null };

            // Act & Assert
            Assert.Throws<System.ServiceModel.FaultException<OrganizationServiceFault>>(() =>
                service.Execute(request));
        }

        #endregion

        #region Loosely-Typed Request Tests

        [Fact]
        public void When_CreateMultiple_With_Loosely_Typed_Request_Should_Work()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var accounts = new EntityCollection();
            accounts.Entities.Add(new Entity("account") { ["name"] = "Account 1" });

            var request = new OrganizationRequest("CreateMultiple");
            request.Parameters["Targets"] = accounts;

            // Act
            var response = service.Execute(request);

            // Assert
            Assert.NotNull(response);
            Assert.Equal("CreateMultiple", response.ResponseName);

            var createdAccounts = context.CreateQuery("account").ToList();
            Assert.Single(createdAccounts);
        }

        [Fact]
        public void When_UpdateMultiple_With_Loosely_Typed_Request_Should_Work()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var id = Guid.NewGuid();
            context.Initialize(new[]
            {
                new Entity("account") { Id = id, ["name"] = "Original" }
            });

            var updates = new EntityCollection();
            updates.Entities.Add(new Entity("account") { Id = id, ["name"] = "Updated" });

            var request = new OrganizationRequest("UpdateMultiple");
            request.Parameters["Targets"] = updates;

            // Act
            var response = service.Execute(request);

            // Assert
            Assert.NotNull(response);
            var account = service.Retrieve("account", id, new ColumnSet(true));
            Assert.Equal("Updated", account.GetAttributeValue<string>("name"));
        }

        #endregion
    }
}
