using Crm;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.ServiceModel;
using Xunit;

namespace FakeXrmEasy.Tests
{
    public class FakeContextTestCreate
    {
        [Fact]
        public void When_a_null_entity_is_created_an_exception_is_thrown()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var ex = Assert.Throws<InvalidOperationException>(() => service.Create(null));
            Assert.Equal("The entity must not be null", ex.Message);
        }

        [Fact]
        public void When_an_entity_is_created_with_an_empty_logical_name_an_exception_is_thrown()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var e = new Entity("") { Id = Guid.Empty };

            var ex = Assert.Throws<InvalidOperationException>(() => service.Create(e));
            Assert.Equal("The LogicalName property must not be empty", ex.Message);
        }

        [Fact]
        public void When_adding_an_entity_the_returned_guid_must_not_be_empty_and_the_context_should_have_it()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var e = new Entity("account") { Id = Guid.Empty };
            var guid = service.Create(e);

            Assert.True(guid != Guid.Empty);
            Assert.True(context.Data.Count == 1);
            Assert.True(context.Data["account"].Count == 1);
        }

        [Fact]
        public void When_Creating_Without_Id_It_should_Be_set_Automatically()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var account = new Account
            {
                Name = "TestAcc"
            };

            account.Id = service.Create(account);

            Assert.NotEqual(Guid.Empty, account.Id);
        }

        [Fact]
        public void When_Creating_With_Id_It_should_Be_set()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();
            var accId = Guid.NewGuid();

            var account = new Account
            {
                Name = "TestAcc",
                Id = accId
            };

            var createdId = service.Create(account);

            Assert.Equal(accId, createdId);
        }

        [Fact]
        public void When_Creating_With_Already_Existing_Id_Exception_Should_Be_Thrown()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();
            var accId = Guid.NewGuid();

            var account = new Account
            {
                Name = "TestAcc",
                Id = accId
            };
            service.Create(account);

            Assert.Throws<InvalidOperationException>(() => service.Create(account));
        }

        [Fact]
        public void When_Creating_With_Active_StateCode_Property_It_Should_Be_Preserved()
        {
            // Dataverse allows creating with Active state (0)
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var account = new Entity("account")
            {
                Id = Guid.NewGuid(),
                ["name"] = "Test Account",
                ["statecode"] = new OptionSetValue(0) // Active
            };

            var id = service.Create(account);
            var retrieved = service.Retrieve("account", id, new ColumnSet(true));

            Assert.Equal(0, retrieved.GetAttributeValue<OptionSetValue>("statecode").Value);
        }

        [Fact]
        public void When_Creating_With_Inactive_StateCode_Exception_Is_Thrown()
        {
            // Dataverse does NOT allow creating inactive records directly
            // You must create Active, then update to Inactive
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var account = new Entity("account")
            {
                Id = Guid.NewGuid(),
                ["name"] = "Test Account",
                ["statecode"] = new OptionSetValue(1) // Inactive - not allowed on Create
            };

            var ex = Assert.Throws<FaultException<OrganizationServiceFault>>(() => service.Create(account));
            Assert.Contains("Cannot create entity", ex.Message);
            Assert.Contains("statecode 1", ex.Message);
        }

        [Fact]
        public void When_Creating_With_Active_StateCode_And_StatusCode_Both_Should_Be_Preserved()
        {
            // Dataverse allows setting statuscode with valid Active state
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var account = new Entity("account")
            {
                Id = Guid.NewGuid(),
                ["name"] = "Test Account",
                ["statecode"] = new OptionSetValue(0),  // Active
                ["statuscode"] = new OptionSetValue(1)  // Active status reason
            };

            var id = service.Create(account);
            var retrieved = service.Retrieve("account", id, new ColumnSet(true));

            Assert.Equal(0, retrieved.GetAttributeValue<OptionSetValue>("statecode").Value);
            Assert.Equal(1, retrieved.GetAttributeValue<OptionSetValue>("statuscode").Value);
        }

        [Fact]
        public void When_Creating_Without_StateCode_It_Defaults_To_Active()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var account = new Entity("account")
            {
                Id = Guid.NewGuid(),
                ["name"] = "Test Account"
            };

            var id = service.Create(account);
            var retrieved = service.Retrieve("account", id, new ColumnSet(true));

            // Statecode should default to Active (0)
            Assert.Equal(0, retrieved.GetAttributeValue<OptionSetValue>("statecode").Value);
        }

        [Fact]
        public void When_Creating_With_StateCode_Set_To_Null_It_Defaults_To_Active()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var account = new Entity("account")
            {
                Id = Guid.NewGuid(),
                ["name"] = "Test Account",
                ["statecode"] = null
            };

            var id = service.Create(account);
            var retrieved = service.Retrieve("account", id, new ColumnSet(true));

            Assert.Equal(0, retrieved.GetAttributeValue<OptionSetValue>("statecode").Value);
        }

        [Fact]
        public void When_Creating_Using_ExecuteMultiple_With_StateCode_Set_To_Null_All_Records_Are_Created_And_Response_Is_Not_Faulted()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var account1 = new Entity("account")
            {
                Id = Guid.NewGuid(),
                ["name"] = "Acc1",
                ["statecode"] = null
            };

            var account2 = new Entity("account")
            {
                Id = Guid.NewGuid(),
                ["name"] = "Acc2",
                ["statecode"] = null
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

            var response = (ExecuteMultipleResponse)service.Execute(executeMultipleRequest);

            Assert.False(response.IsFaulted);
            Assert.Equal(2, response.Responses.Count);
            Assert.All(response.Responses, r => Assert.Null(r.Fault));

            var created1 = service.Retrieve("account", account1.Id, new ColumnSet(true));
            var created2 = service.Retrieve("account", account2.Id, new ColumnSet(true));

            Assert.Equal(0, created1.GetAttributeValue<OptionSetValue>("statecode").Value);
            Assert.Equal(0, created2.GetAttributeValue<OptionSetValue>("statecode").Value);
        }

        [Fact]
        public void When_Creating_Using_Organization_Context_Record_Should_Be_Created()
        {
      var context = new XrmFakedContext
      {
        ProxyTypesAssembly = Assembly.GetAssembly(typeof(Account))
      };

      var account = new Account() { Id = Guid.NewGuid(), Name = "Super Great Customer", AccountNumber = "69" };

            var service = context.GetOrganizationService();

            using (var ctx = new OrganizationServiceContext(service))
            {
                ctx.AddObject(account);
                ctx.SaveChanges();
            }

            Assert.NotNull(service.Retrieve(Account.EntityLogicalName, account.Id, new ColumnSet(true)));
        }

        [Fact]
        public void When_Creating_Using_Organization_Context_Without_Saving_Changes_Record_Should_Not_Be_Created()
        {
      var context = new XrmFakedContext
      {
        ProxyTypesAssembly = Assembly.GetAssembly(typeof(Account))
      };

      var account = new Account() { Id = Guid.NewGuid(), Name = "Super Great Customer", AccountNumber = "69" };

            var service = context.GetOrganizationService();

            using (var ctx = new OrganizationServiceContext(service))
            {
                ctx.AddObject(account);

                var retrievedAccount = ctx.CreateQuery<Account>().SingleOrDefault(acc => acc.Id == account.Id);
                Assert.Null(retrievedAccount);
            }
        }

        [Fact]
        public void When_creating_a_record_using_early_bound_entities_primary_key_should_be_populated()
        {
            var context = new XrmFakedContext();

            var c = new Contact();

            IOrganizationService service = context.GetOrganizationService();
            var id = service.Create(c);

            //Retrieve the record created
            var contact = (from con in context.CreateQuery<Contact>()
                           select con).FirstOrDefault();

            Assert.True(contact.Attributes.ContainsKey("contactid"));
            Assert.Equal(id, contact["contactid"]);
        }

        [Fact]
        public void When_creating_a_record_using_dynamic_entities_primary_key_should_be_populated()
        {
            var context = new XrmFakedContext();

            Entity e = new Entity("new_myentity");

            IOrganizationService service = context.GetOrganizationService();
            var id = service.Create(e);

            //Retrieve the record created
            var record = (from r in context.CreateQuery("new_myentity")
                          select r).FirstOrDefault();

            Assert.True(record.Attributes.ContainsKey("new_myentityid"));
            Assert.Equal(id, record["new_myentityid"]);
        }

        [Fact]
        public void When_creating_a_record_using_early_bound_entities_and_proxytypes_primary_key_should_be_populated()
        {
      var context = new XrmFakedContext
      {
        ProxyTypesAssembly = Assembly.GetAssembly(typeof(Contact))
      };
      var c = new Contact
      {
        Id = Guid.NewGuid()
      };

      IOrganizationService service = context.GetOrganizationService();

            context.Initialize(new List<Entity>() { c });

            //Retrieve the record created
            var contact = (from con in context.CreateQuery<Contact>()
                           select con).FirstOrDefault();

            Assert.True(contact.Attributes.ContainsKey("contactid"));
            Assert.Equal(c.Id, contact["contactid"]);
        }

        [Fact]
        public void When_related_entities_are_used_without_relationship_info_exception_is_raised()
        {
            var ctx = new XrmFakedContext();
            var service = ctx.GetOrganizationService();

            var order = new SalesOrder();

            var orderItems = new EntityCollection(new List<Entity>()
            {
                new SalesOrderDetail(),
                new SalesOrderDetail()
            });

            // Add related order items so it can be created in one request
            order.RelatedEntities.Add(new Relationship
            {
                PrimaryEntityRole = EntityRole.Referenced,
                SchemaName = "order_details"
            }, orderItems);

            var request = new CreateRequest
            {
                Target = order
            };

            var exception = Record.Exception(() => service.Execute(request));

            Assert.IsType<Exception>(exception);
            Assert.Equal("Relationship order_details does not exist in the metadata cache", exception.Message);
        }

        [Fact]
        public void When_related_entities_and_relationship_are_used_child_entities_are_created()
        {
            var ctx = new XrmFakedContext();
            var service = ctx.GetOrganizationService();

            ctx.AddRelationship("order_details",
                new XrmFakedRelationship()
                {
                    Entity1LogicalName = SalesOrder.EntityLogicalName,  //Referenced
                    Entity1Attribute = "salesorderid",              //Pk
                    Entity2LogicalName = SalesOrderDetail.EntityLogicalName,
                    Entity2Attribute = "salesorderid",              //Lookup attribute
                    RelationshipType = XrmFakedRelationship.enmFakeRelationshipType.OneToMany
                });

            var order = new SalesOrder();

            var orderItems = new EntityCollection(new List<Entity>()
            {
                new SalesOrderDetail(),
                new SalesOrderDetail()
            });

            // Add related order items so it can be created in one request
            order.RelatedEntities.Add(new Relationship
            {
                PrimaryEntityRole = EntityRole.Referenced,
                SchemaName = "order_details"
            }, orderItems);

            var request = new CreateRequest
            {
                Target = order
            };

            var id = (service.Execute(request) as CreateResponse).id;
            var createdOrderDetails = ctx.CreateQuery<SalesOrderDetail>().ToList();

            Assert.Equal(2, createdOrderDetails.Count);
            Assert.Equal(createdOrderDetails[0].SalesOrderId.Id, id);
            Assert.Equal(createdOrderDetails[1].SalesOrderId.Id, id);
        }

        [Fact]
        public void Shouldnt_store_references_to_variables_but_actual_clones()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            //create an account and then retrieve it with no changes
            Entity newAccount = new Entity("account");
            newAccount["name"] = "New Account";

            newAccount.Id = service.Create(newAccount);

            Entity retrievedAccount = service.Retrieve("account", newAccount.Id, new Microsoft.Xrm.Sdk.Query.ColumnSet(true));
            Assert.True(retrievedAccount.Attributes.Contains("name"));

            //do the same as above, but this time clear the attributes - see that when retrieved, the retrieved entity does not contain the name attribute
            Entity newAccount1 = new Entity("account");
            newAccount1["name"] = "New Account1";

            newAccount1.Id = service.Create(newAccount1);
            newAccount1.Attributes.Clear();

            Entity retrievedAccount1 = service.Retrieve("account", newAccount1.Id, new Microsoft.Xrm.Sdk.Query.ColumnSet(true));
            Assert.True(retrievedAccount1.Attributes.Contains("name"));

            //third time around, change the name to something new, the retrieved entity should not reflect this change
            Entity newAccount2 = new Entity("account");
            newAccount2["name"] = "New Account2";

            newAccount2.Id = service.Create(newAccount2);
            newAccount2["name"] = "Changed name";

            Entity retrievedAccount2 = service.Retrieve("account", newAccount2.Id, new Microsoft.Xrm.Sdk.Query.ColumnSet(true));
            Assert.True(retrievedAccount2["name"].ToString() == "New Account2", $"'{retrievedAccount2["name"]}' was not the expected result");
        }

        [Fact]
        public void Shouldnt_modify_objects_passed_to_the_service() // *PLEASE_READ* This test is correct?
        {
      var context = new XrmFakedContext
      {
        ProxyTypesAssembly = Assembly.GetAssembly(typeof(Contact))
      };
      var account = new Account { Id = Guid.NewGuid(), Name = "Test account" };

            IOrganizationService service = context.GetOrganizationService();

            context.Initialize(new List<Entity>() { account });

            //Retrieve the record created
            Contact c = new Contact
            {
                ParentCustomerId = account.ToEntityReference(),
                LastName = "Duck",
            };
            foreach (var name in new[] { "Huey", "Dewey", "Louie"})
            {
                c.FirstName = name;
                service.Create(c);
            }

            var createdContacts = context.CreateQuery<Contact>().ToList();

            Assert.Equal(Guid.Empty, c.Id);
            Assert.Null(c.ContactId);
            Assert.Null(c.CreatedOn);

            Assert.Equal(3, createdContacts.Count);
        }

        [Fact]
        public void When_Creating_Without_Default_Attributes_They_Should_Be_Set_By_Default()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var account = new Account
            {
                Name = "test"
            };

            service.Create(account);
            var createdAccount = context.CreateQuery<Account>().FirstOrDefault();

            Assert.True(createdAccount.Attributes.ContainsKey("createdon"));
            Assert.True(createdAccount.Attributes.ContainsKey("createdby"));
            Assert.True(createdAccount.Attributes.ContainsKey("modifiedon"));
            Assert.True(createdAccount.Attributes.ContainsKey("modifiedby"));
            Assert.True(createdAccount.Attributes.ContainsKey("statecode"));
        }

        [Fact]
        public void When_Creating_Without_Default_Attributes_They_Should_Be_Set_By_Default_With_Early_Bound()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();
            context.ProxyTypesAssembly = Assembly.GetAssembly(typeof(Account));

            var account = new Account
            {
                Name = "test"
            };

            service.Create(account);
            var createdAccount = context.CreateQuery<Account>().FirstOrDefault();

            Assert.True(createdAccount.Attributes.ContainsKey("createdon"));
            Assert.True(createdAccount.Attributes.ContainsKey("createdby"));
            Assert.True(createdAccount.Attributes.ContainsKey("modifiedon"));
            Assert.True(createdAccount.Attributes.ContainsKey("modifiedby"));
            Assert.True(createdAccount.Attributes.ContainsKey("statecode"));
        }

        [Fact]
        public void When_creating_a_record_overridencreatedon_should_override_created_on()
        {
            var ctx = new XrmFakedContext();
            var service = ctx.GetOrganizationService();

            // Use UTC - Dataverse stores all dates as UTC
            var now = DateTime.UtcNow.Date;

            var account = new Account()
            {
                OverriddenCreatedOn = now,
                ["createdon"] = now.AddDays(-1)
            };

            service.Create(account);

            var createdAccount = ctx.CreateQuery<Account>().FirstOrDefault();
            Assert.Equal(now, createdAccount.CreatedOn);
        }
    }
}