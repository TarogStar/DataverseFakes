using System;
using System.Linq;
using System.Reflection;
using System.ServiceModel;
using Crm;
using FakeXrmEasy.FakeMessageExecutors;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using Xunit;

namespace FakeXrmEasy.Tests.FakeContextTests.ExecuteAsyncTests
{
    public class ExecuteAsyncRequestTests
    {
        [Fact]
        public void When_can_execute_is_called_with_an_invalid_request_result_is_false()
        {
            var executor = new ExecuteAsyncRequestExecutor();
            var anotherRequest = new RetrieveMultipleRequest();
            Assert.False(executor.CanExecute(anotherRequest));
        }

        [Fact]
        public void When_can_execute_is_called_with_execute_async_request_result_is_true()
        {
            var executor = new ExecuteAsyncRequestExecutor();
            var request = new ExecuteAsyncRequest();
            Assert.True(executor.CanExecute(request));
        }

        [Fact]
        public void When_execute_is_called_with_null_request_exception_is_thrown()
        {
            var context = new XrmFakedContext();
            var executor = new ExecuteAsyncRequestExecutor();
            var request = new ExecuteAsyncRequest
            {
                Request = null
            };
            Assert.Throws<FaultException<OrganizationServiceFault>>(() => executor.Execute(request, context));
        }

        [Fact]
        public async System.Threading.Tasks.Task ExecuteAsync_with_CreateRequest_should_create_record_and_return_job_id()
        {
      // Arrange
      var context = new XrmFakedContext
      {
        ProxyTypesAssembly = Assembly.GetExecutingAssembly()
      };
      var service = context.GetOrganizationService();

            var account = new Account
            {
                Id = Guid.NewGuid(),
                Name = "Test Account"
            };

            var createRequest = new CreateRequest
            {
                Target = account
            };

            var executeAsyncRequest = new ExecuteAsyncRequest
            {
                Request = createRequest
            };

            // Act
            var response = (ExecuteAsyncResponse)await System.Threading.Tasks.Task.Run(() => service.Execute(executeAsyncRequest));

            // Assert
            Assert.NotNull(response);
            Assert.NotEqual(Guid.Empty, response.AsyncJobId);

            // Verify the account was actually created
            var retrievedAccount = service.Retrieve(Account.EntityLogicalName, account.Id, new ColumnSet(true));
            Assert.NotNull(retrievedAccount);
            Assert.Equal("Test Account", retrievedAccount.GetAttributeValue<string>("name"));
        }

        [Fact]
        public async System.Threading.Tasks.Task ExecuteAsync_with_UpdateRequest_should_update_record_and_return_job_id()
        {
      // Arrange
      var context = new XrmFakedContext
      {
        ProxyTypesAssembly = Assembly.GetExecutingAssembly()
      };
      var service = context.GetOrganizationService();

            var accountId = Guid.NewGuid();
            var account = new Account
            {
                Id = accountId,
                Name = "Original Name"
            };

            context.Initialize(new[] { account });

            var updateRequest = new UpdateRequest
            {
                Target = new Account
                {
                    Id = accountId,
                    Name = "Updated Name"
                }
            };

            var executeAsyncRequest = new ExecuteAsyncRequest
            {
                Request = updateRequest
            };

            // Act
            var response = (ExecuteAsyncResponse)await System.Threading.Tasks.Task.Run(() => service.Execute(executeAsyncRequest));

            // Assert
            Assert.NotNull(response);
            Assert.NotEqual(Guid.Empty, response.AsyncJobId);

            // Verify the account was actually updated
            var retrievedAccount = service.Retrieve(Account.EntityLogicalName, accountId, new ColumnSet(true)).ToEntity<Account>();
            Assert.Equal("Updated Name", retrievedAccount.Name);
        }

        [Fact]
        public async System.Threading.Tasks.Task ExecuteAsync_should_create_asyncoperation_record_with_completed_status()
        {
      // Arrange
      var context = new XrmFakedContext
      {
        ProxyTypesAssembly = Assembly.GetExecutingAssembly()
      };
      var service = context.GetOrganizationService();

            var account = new Account
            {
                Id = Guid.NewGuid(),
                Name = "Test Account"
            };

            var createRequest = new CreateRequest
            {
                Target = account
            };

            var executeAsyncRequest = new ExecuteAsyncRequest
            {
                Request = createRequest
            };

            // Act
            var response = (ExecuteAsyncResponse)await System.Threading.Tasks.Task.Run(() => service.Execute(executeAsyncRequest));

            // Assert - Verify asyncoperation record exists with correct state
            var asyncOperations = (from a in context.CreateQuery<AsyncOperation>()
                                   where a.AsyncOperationId == response.AsyncJobId
                                   select a).ToList();

            var asyncOperation = Assert.Single(asyncOperations);
            Assert.Equal(AsyncOperationState.Completed, asyncOperation.StateCode);
        }

        [Fact]
        public async System.Threading.Tasks.Task ExecuteAsync_asyncoperation_should_contain_request_name()
        {
      // Arrange
      var context = new XrmFakedContext
      {
        ProxyTypesAssembly = Assembly.GetExecutingAssembly()
      };
      var service = context.GetOrganizationService();

            var account = new Account
            {
                Id = Guid.NewGuid(),
                Name = "Test Account"
            };

            var createRequest = new CreateRequest
            {
                Target = account
            };

            var executeAsyncRequest = new ExecuteAsyncRequest
            {
                Request = createRequest
            };

            // Act
            var response = (ExecuteAsyncResponse)await System.Threading.Tasks.Task.Run(() => service.Execute(executeAsyncRequest));

            // Assert - Verify asyncoperation record has the request name
            var asyncOperation = service.Retrieve("asyncoperation", response.AsyncJobId, new ColumnSet(true));
            var name = asyncOperation.GetAttributeValue<string>("name");
            Assert.Contains("Create", name);
        }

        [Fact]
        public async System.Threading.Tasks.Task ExecuteAsync_with_DeleteRequest_should_delete_record_and_return_job_id()
        {
      // Arrange
      var context = new XrmFakedContext
      {
        ProxyTypesAssembly = Assembly.GetExecutingAssembly()
      };
      var service = context.GetOrganizationService();

            var accountId = Guid.NewGuid();
            var account = new Account
            {
                Id = accountId,
                Name = "Account to Delete"
            };

            context.Initialize(new[] { account });

            var deleteRequest = new DeleteRequest
            {
                Target = new EntityReference(Account.EntityLogicalName, accountId)
            };

            var executeAsyncRequest = new ExecuteAsyncRequest
            {
                Request = deleteRequest
            };

            // Act
            var response = (ExecuteAsyncResponse)await System.Threading.Tasks.Task.Run(() => service.Execute(executeAsyncRequest));

            // Assert
            Assert.NotNull(response);
            Assert.NotEqual(Guid.Empty, response.AsyncJobId);

            // Verify the account was actually deleted
            Assert.Throws<FaultException<OrganizationServiceFault>>(() =>
                service.Retrieve(Account.EntityLogicalName, accountId, new ColumnSet(true)));
        }

        [Fact]
        public async System.Threading.Tasks.Task ExecuteAsync_multiple_requests_should_create_separate_jobs()
        {
      // Arrange
      var context = new XrmFakedContext
      {
        ProxyTypesAssembly = Assembly.GetExecutingAssembly()
      };
      var service = context.GetOrganizationService();

            var account1 = new Account { Id = Guid.NewGuid(), Name = "Account 1" };
            var account2 = new Account { Id = Guid.NewGuid(), Name = "Account 2" };

            // Act
            var response1 = (ExecuteAsyncResponse)await System.Threading.Tasks.Task.Run(() => service.Execute(new ExecuteAsyncRequest
            {
                Request = new CreateRequest { Target = account1 }
            }));

            var response2 = (ExecuteAsyncResponse)await System.Threading.Tasks.Task.Run(() => service.Execute(new ExecuteAsyncRequest
            {
                Request = new CreateRequest { Target = account2 }
            }));

            // Assert
            Assert.NotEqual(response1.AsyncJobId, response2.AsyncJobId);

            // Both asyncoperation records should exist
            var asyncOperations = context.CreateQuery<AsyncOperation>().ToList();
            Assert.Equal(2, asyncOperations.Count);
            Assert.Contains(asyncOperations, a => a.AsyncOperationId == response1.AsyncJobId);
            Assert.Contains(asyncOperations, a => a.AsyncOperationId == response2.AsyncJobId);
        }
    }
}
