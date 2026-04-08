using Crm;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Linq;
using System.ServiceModel;
using Xunit;

namespace FakeXrmEasy.Tests.FakeContextTests.ExecuteMultipleRequestTests
{
    public class ExecuteMultipleRequestTests
    {
        [Fact]
        public static void Should_Execute_Subsequent_Requests()
        {
            var context = new XrmFakedContext();

            var service = context.GetOrganizationService();

            var account1 = new Account
            {
                Id = Guid.NewGuid(),
                Name = "Acc1"
            };

            var account2 = new Account
            {
                Id = Guid.NewGuid(),
                Name = "Acc2"
            };

            var executeMultipleRequest = new ExecuteMultipleRequest
            {
                Settings = new ExecuteMultipleSettings
                {
                    ReturnResponses = true
                },
                Requests = new OrganizationRequestCollection
                {
                    new CreateRequest
                    {
                        Target = account1
                    },

                    new CreateRequest
                    {
                        Target = account2
                    }
                }
            };

            var response = service.Execute(executeMultipleRequest) as ExecuteMultipleResponse;

            Assert.False(response.IsFaulted);
            Assert.NotEmpty(response.Responses);

            Assert.NotNull(service.Retrieve(Account.EntityLogicalName, account1.Id, new ColumnSet(true)));
            Assert.NotNull(service.Retrieve(Account.EntityLogicalName, account2.Id, new ColumnSet(true)));
        }

        [Fact]
        public static void Should_Execute_Subsequent_Requests_In_Order()
        {
            var context = new XrmFakedContext();

            var service = context.GetOrganizationService();

            var account1 = new Account
            {
                Id = Guid.NewGuid(),
                Name = "Acc1"
            };

            var account2 = new Account
            {
                Id = Guid.NewGuid(),
                Name = "Acc2"
            };

            var executeMultipleRequest = new ExecuteMultipleRequest
            {
                Settings = new ExecuteMultipleSettings
                {
                    ReturnResponses = true
                },
                Requests = new OrganizationRequestCollection
                {
                    new CreateRequest
                    {
                        Target = account1
                    },

                    new CreateRequest
                    {
                        Target = account2
                    },

                    new UpdateRequest
                    {
                        Target = new Account
                        {
                            Id = account1.Id,
                            Name = "Acc1 - Updated"
                        }
                    },

                    new UpdateRequest
                    {
                        Target = new Account
                        {
                            Id = account2.Id,
                            Name = "Acc2 - Updated"
                        }
                    }
                }
            };

            var response = service.Execute(executeMultipleRequest) as ExecuteMultipleResponse;

            Assert.False(response.IsFaulted);
            Assert.NotEmpty(response.Responses);

            var acc1 = service.Retrieve(Account.EntityLogicalName, account1.Id, new ColumnSet(true)).ToEntity<Account>();
            Assert.NotNull(acc1);
            var acc2 = (service.Retrieve(Account.EntityLogicalName, account2.Id, new ColumnSet(true))).ToEntity<Account>();
            Assert.NotNull(acc2);

            Assert.Equal("Acc1 - Updated", acc1.Name);
            Assert.Equal("Acc2 - Updated", acc2.Name);
        }

        [Fact]
        public static void Should_Not_Return_Responses_If_Not_Told_To_Do_So_And_No_Faults_Occur()
        {
            var context = new XrmFakedContext();

            var service = context.GetOrganizationService();

            var account1 = new Account
            {
                Id = Guid.NewGuid(),
                Name = "Acc1"
            };

            var account2 = new Account
            {
                Id = Guid.NewGuid(),
                Name = "Acc2"
            };

            var executeMultipleRequest = new ExecuteMultipleRequest
            {
                Settings = new ExecuteMultipleSettings
                {
                    ReturnResponses = false
                },
                Requests = new OrganizationRequestCollection
                {
                    new CreateRequest
                    {
                        Target = account1
                    },

                    new CreateRequest
                    {
                        Target = account2
                    }
                }
            };

            var response = service.Execute(executeMultipleRequest) as ExecuteMultipleResponse;

            Assert.False(response.IsFaulted);
            Assert.Empty(response.Responses);

            Assert.NotNull(service.Retrieve(Account.EntityLogicalName, account1.Id, new ColumnSet(true)));
            Assert.NotNull(service.Retrieve(Account.EntityLogicalName, account2.Id, new ColumnSet(true)));
        }

        [Fact]
        public static void Should_Return_Error_Responses_Only_If_Faults_Occur_And_Return_Is_False()
        {
            var context = new XrmFakedContext();

            var service = context.GetOrganizationService();

            var account1 = new Account
            {
                Id = Guid.NewGuid(),
                Name = "Acc1"
            };

            var account2 = new Account
            {
                Id = account1.Id,
                Name = "Acc2"
            };

            var executeMultipleRequest = new ExecuteMultipleRequest
            {
                Settings = new ExecuteMultipleSettings
                {
                    ReturnResponses = false
                },
                Requests = new OrganizationRequestCollection
                {
                    new CreateRequest
                    {
                        Target = account1
                    },

                    new CreateRequest
                    {
                        Target = account2
                    }
                }
            };

            var response = service.Execute(executeMultipleRequest) as ExecuteMultipleResponse;

            Assert.True(response.IsFaulted);
            Assert.NotEmpty(response.Responses);
            Assert.Single(response.Responses);

            Assert.NotNull(service.Retrieve(Account.EntityLogicalName, account1.Id, new ColumnSet(true)));
        }

        [Fact]
        public static void Should_Return_All_Responses_If_Told_To_Do_So()
        {
            var context = new XrmFakedContext();

            var service = context.GetOrganizationService();

            var account1 = new Account
            {
                Id = Guid.NewGuid(),
                Name = "Acc1"
            };

            var account2 = new Account
            {
                Id = Guid.NewGuid(),
                Name = "Acc2"
            };

            var executeMultipleRequest = new ExecuteMultipleRequest
            {
                Settings = new ExecuteMultipleSettings
                {
                    ReturnResponses = true
                },
                Requests = new OrganizationRequestCollection
                {
                    new CreateRequest
                    {
                        Target = account1
                    },

                    new CreateRequest
                    {
                        Target = account2
                    }
                }
            };

            var response = service.Execute(executeMultipleRequest) as ExecuteMultipleResponse;

            Assert.False(response.IsFaulted);
            Assert.NotEmpty(response.Responses);
            Assert.Equal(2, response.Responses.Count);
            Assert.True(response.Responses[0].Response is CreateResponse);
            Assert.True(response.Responses[1].Response is CreateResponse);

            Assert.NotNull(service.Retrieve(Account.EntityLogicalName, account1.Id, new ColumnSet(true)));
            Assert.NotNull(service.Retrieve(Account.EntityLogicalName, account2.Id, new ColumnSet(true)));
        }

        [Fact]
        public static void Should_Continue_On_Error_If_Told_To_Do_So()
        {
            var context = new XrmFakedContext();

            var service = context.GetOrganizationService();

            var account1 = new Account
            {
                Id = Guid.NewGuid(),
                Name = "Acc1"
            };

            var account2 = new Account
            {
                Id = account1.Id,
                Name = "Acc2 - Same ID as Acc 1"
            };

            var account3 = new Account
            {
                Id = Guid.NewGuid(),
                Name = "Acc3"
            };

            var executeMultipleRequest = new ExecuteMultipleRequest
            {
                Settings = new ExecuteMultipleSettings
                {
                    ReturnResponses = true,
                    ContinueOnError = true
                },
                Requests = new OrganizationRequestCollection
                {
                    new CreateRequest
                    {
                        Target = account1
                    },

                    new CreateRequest
                    {
                        Target = account2
                    },

                    new CreateRequest
                    {
                        Target = account3
                    }
                }
            };

            var response = service.Execute(executeMultipleRequest) as ExecuteMultipleResponse;

            Assert.True(response.IsFaulted);
            Assert.NotEmpty(response.Responses);

            Assert.Contains(response.Responses, resp => resp.Fault != null);

            Assert.NotNull(service.Retrieve(Account.EntityLogicalName, account1.Id, new ColumnSet(true)));
            Assert.NotNull(service.Retrieve(Account.EntityLogicalName, account3.Id, new ColumnSet(true)));
        }

        [Fact]
        public static void Should_Not_Continue_On_Error_If_Not_Told_To_Do_So()
        {
            var context = new XrmFakedContext();

            var service = context.GetOrganizationService();

            var account1 = new Account
            {
                Id = Guid.NewGuid(),
                Name = "Acc1"
            };

            var account2 = new Account
            {
                Id = account1.Id,
                Name = "Acc2 - Same ID as Acc 1"
            };

            var account3 = new Account
            {
                Id = Guid.NewGuid(),
                Name = "Acc3"
            };

            var executeMultipleRequest = new ExecuteMultipleRequest
            {
                Settings = new ExecuteMultipleSettings
                {
                    ReturnResponses = true,
                    ContinueOnError = false
                },
                Requests = new OrganizationRequestCollection
                {
                    new CreateRequest
                    {
                        Target = account1
                    },

                    new CreateRequest
                    {
                        Target = account2
                    },

                    new CreateRequest
                    {
                        Target = account3
                    }
                }
            };

            var response = service.Execute(executeMultipleRequest) as ExecuteMultipleResponse;

            Assert.True(response.IsFaulted);
            Assert.NotEmpty(response.Responses);

            Assert.Contains(response.Responses, resp => resp.Fault != null);

            Assert.NotNull(service.Retrieve(Account.EntityLogicalName, account1.Id, new ColumnSet(true)));
            Assert.Throws<FaultException<OrganizationServiceFault>>(() => service.Retrieve(Account.EntityLogicalName, account3.Id, new ColumnSet(true)));
        }

        [Fact]
        public static void Should_Set_Correct_RequestIndex_On_Fault()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var account1 = new Account
            {
                Id = Guid.NewGuid(),
                Name = "Acc1"
            };

            var account2 = new Account
            {
                Id = account1.Id,  // Duplicate ID will cause failure
                Name = "Acc2"
            };

            var executeMultipleRequest = new ExecuteMultipleRequest
            {
                Settings = new ExecuteMultipleSettings
                {
                    ReturnResponses = true,
                    ContinueOnError = false
                },
                Requests = new OrganizationRequestCollection
                {
                    new CreateRequest { Target = account1 },
                    new CreateRequest { Target = account2 }
                }
            };

            var response = service.Execute(executeMultipleRequest) as ExecuteMultipleResponse;

            Assert.True(response.IsFaulted);
            var faultResponse = response.Responses.FirstOrDefault(r => r.Fault != null);
            Assert.NotNull(faultResponse);
            Assert.Equal(1, faultResponse.RequestIndex);  // Second request (index 1) should have failed
        }

        [Fact]
        public static void Should_Populate_Fault_Message()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var account1 = new Account
            {
                Id = Guid.NewGuid(),
                Name = "Acc1"
            };

            var account2 = new Account
            {
                Id = account1.Id,  // Duplicate ID
                Name = "Acc2"
            };

            var executeMultipleRequest = new ExecuteMultipleRequest
            {
                Settings = new ExecuteMultipleSettings
                {
                    ReturnResponses = true,
                    ContinueOnError = false
                },
                Requests = new OrganizationRequestCollection
                {
                    new CreateRequest { Target = account1 },
                    new CreateRequest { Target = account2 }
                }
            };

            var response = service.Execute(executeMultipleRequest) as ExecuteMultipleResponse;

            Assert.True(response.IsFaulted);
            var faultResponse = response.Responses.FirstOrDefault(r => r.Fault != null);
            Assert.NotNull(faultResponse);
            Assert.NotNull(faultResponse.Fault);
            Assert.False(string.IsNullOrEmpty(faultResponse.Fault.Message));
        }

        [Fact]
        public static void ContinueOnError_True_Should_Collect_Multiple_Faults()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var account1 = new Account
            {
                Id = Guid.NewGuid(),
                Name = "Acc1"
            };

            // Both account2 and account3 have the same ID as account1, causing 2 failures
            var account2 = new Account
            {
                Id = account1.Id,
                Name = "Acc2 - Duplicate"
            };

            var account3 = new Account
            {
                Id = account1.Id,
                Name = "Acc3 - Also Duplicate"
            };

            var account4 = new Account
            {
                Id = Guid.NewGuid(),
                Name = "Acc4"
            };

            var executeMultipleRequest = new ExecuteMultipleRequest
            {
                Settings = new ExecuteMultipleSettings
                {
                    ReturnResponses = true,
                    ContinueOnError = true
                },
                Requests = new OrganizationRequestCollection
                {
                    new CreateRequest { Target = account1 },
                    new CreateRequest { Target = account2 },  // Will fail
                    new CreateRequest { Target = account3 },  // Will also fail
                    new CreateRequest { Target = account4 }
                }
            };

            var response = service.Execute(executeMultipleRequest) as ExecuteMultipleResponse;

            Assert.True(response.IsFaulted);

            // Should have 4 responses (2 successful, 2 faulted)
            Assert.Equal(4, response.Responses.Count);

            // Count faults
            var faults = response.Responses.Where(r => r.Fault != null).ToList();
            Assert.Equal(2, faults.Count);

            // Verify fault indexes
            Assert.Contains(faults, f => f.RequestIndex == 1);
            Assert.Contains(faults, f => f.RequestIndex == 2);

            // Verify successful creates
            Assert.NotNull(service.Retrieve(Account.EntityLogicalName, account1.Id, new ColumnSet(true)));
            Assert.NotNull(service.Retrieve(Account.EntityLogicalName, account4.Id, new ColumnSet(true)));
        }

        [Fact]
        public static void ContinueOnError_True_ReturnResponses_False_Should_Only_Return_Faults()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var account1 = new Account
            {
                Id = Guid.NewGuid(),
                Name = "Acc1"
            };

            var account2 = new Account
            {
                Id = account1.Id,  // Will fail
                Name = "Acc2 - Duplicate"
            };

            var account3 = new Account
            {
                Id = Guid.NewGuid(),
                Name = "Acc3"
            };

            var executeMultipleRequest = new ExecuteMultipleRequest
            {
                Settings = new ExecuteMultipleSettings
                {
                    ReturnResponses = false,
                    ContinueOnError = true
                },
                Requests = new OrganizationRequestCollection
                {
                    new CreateRequest { Target = account1 },
                    new CreateRequest { Target = account2 },  // Will fail
                    new CreateRequest { Target = account3 }
                }
            };

            var response = service.Execute(executeMultipleRequest) as ExecuteMultipleResponse;

            Assert.True(response.IsFaulted);

            // Should only have 1 response (the fault) because ReturnResponses=false
            Assert.Single(response.Responses);

            var faultResponse = response.Responses[0];
            Assert.NotNull(faultResponse.Fault);
            Assert.Equal(1, faultResponse.RequestIndex);

            // Both successful creates should have been executed
            Assert.NotNull(service.Retrieve(Account.EntityLogicalName, account1.Id, new ColumnSet(true)));
            Assert.NotNull(service.Retrieve(Account.EntityLogicalName, account3.Id, new ColumnSet(true)));
        }

        [Fact]
        public static void Should_Return_Correct_Response_Types()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var account = new Account
            {
                Id = Guid.NewGuid(),
                Name = "Test Account"
            };

            context.Initialize(new[] { account });

            var executeMultipleRequest = new ExecuteMultipleRequest
            {
                Settings = new ExecuteMultipleSettings
                {
                    ReturnResponses = true,
                    ContinueOnError = true
                },
                Requests = new OrganizationRequestCollection
                {
                    new UpdateRequest
                    {
                        Target = new Account { Id = account.Id, Name = "Updated Name" }
                    },
                    new RetrieveRequest
                    {
                        Target = new EntityReference(Account.EntityLogicalName, account.Id),
                        ColumnSet = new ColumnSet(true)
                    }
                }
            };

            var response = service.Execute(executeMultipleRequest) as ExecuteMultipleResponse;

            Assert.False(response.IsFaulted);
            Assert.Equal(2, response.Responses.Count);

            Assert.True(response.Responses[0].Response is UpdateResponse);
            Assert.True(response.Responses[1].Response is RetrieveResponse);

            var retrieveResponse = (RetrieveResponse)response.Responses[1].Response;
            var retrievedAccount = retrieveResponse.Entity.ToEntity<Account>();
            Assert.Equal("Updated Name", retrievedAccount.Name);
        }

        [Fact]
        public static void ContinueOnError_False_Should_Stop_At_First_Failure()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var account1 = new Account
            {
                Id = Guid.NewGuid(),
                Name = "Acc1"
            };

            // This will fail because account1 already exists
            var account2 = new Account
            {
                Id = account1.Id,
                Name = "Acc2"
            };

            var account3 = new Account
            {
                Id = Guid.NewGuid(),
                Name = "Acc3"
            };

            var account4 = new Account
            {
                Id = Guid.NewGuid(),
                Name = "Acc4"
            };

            var executeMultipleRequest = new ExecuteMultipleRequest
            {
                Settings = new ExecuteMultipleSettings
                {
                    ReturnResponses = true,
                    ContinueOnError = false
                },
                Requests = new OrganizationRequestCollection
                {
                    new CreateRequest { Target = account1 },
                    new CreateRequest { Target = account2 },  // Will fail - should stop here
                    new CreateRequest { Target = account3 },  // Should NOT execute
                    new CreateRequest { Target = account4 }   // Should NOT execute
                }
            };

            var response = service.Execute(executeMultipleRequest) as ExecuteMultipleResponse;

            Assert.True(response.IsFaulted);

            // Should have 2 responses (1 successful, 1 faulted) - stopped after fault
            Assert.Equal(2, response.Responses.Count);

            // First request succeeded
            Assert.NotNull(response.Responses[0].Response);
            Assert.Null(response.Responses[0].Fault);

            // Second request faulted
            Assert.Null(response.Responses[1].Response);
            Assert.NotNull(response.Responses[1].Fault);

            // account1 was created, but account3 and account4 were not
            Assert.NotNull(service.Retrieve(Account.EntityLogicalName, account1.Id, new ColumnSet(true)));
            Assert.Throws<FaultException<OrganizationServiceFault>>(() =>
                service.Retrieve(Account.EntityLogicalName, account3.Id, new ColumnSet(true)));
            Assert.Throws<FaultException<OrganizationServiceFault>>(() =>
                service.Retrieve(Account.EntityLogicalName, account4.Id, new ColumnSet(true)));
        }

        [Fact]
        public static void Should_Throw_When_Settings_Is_Null()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var executeMultipleRequest = new ExecuteMultipleRequest
            {
                Settings = null,
                Requests = new OrganizationRequestCollection()
            };

            Assert.Throws<FaultException<OrganizationServiceFault>>(() =>
                service.Execute(executeMultipleRequest));
        }

        [Fact]
        public static void Should_Throw_When_Requests_Is_Null()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var executeMultipleRequest = new ExecuteMultipleRequest
            {
                Settings = new ExecuteMultipleSettings { ReturnResponses = true },
                Requests = null
            };

            Assert.Throws<FaultException<OrganizationServiceFault>>(() =>
                service.Execute(executeMultipleRequest));
        }
    }
}