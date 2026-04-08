using Crm;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace FakeXrmEasy.Tests.FakeContextTests.FetchXml
{
    /// <summary>
    /// Tests for FetchXML Any/All/NotAny/NotAll link-type filter operators
    /// These operators support EXISTS/NOT EXISTS style queries in Dataverse
    ///
    /// link-type="any"    - EXISTS: Returns parent if ANY related record matches
    /// link-type="not any"- NOT EXISTS: Returns parent if NO related records match
    /// link-type="all"    - ALL: Returns parent if ALL related records match (or no related records exist)
    /// link-type="not all"- NOT ALL: Returns parent if at least one related record does NOT match
    /// </summary>
    public class AnyAllFilterTests
    {
        #region Test Data Setup Helpers

        private (XrmFakedContext context, List<Guid> accountIds, List<Guid> contactIds) SetupAccountsWithContacts()
        {
            var context = new XrmFakedContext();

            // Account 1: Has 2 contacts - John and Jane
            var account1Id = Guid.NewGuid();
            var account1 = new Entity("account")
            {
                Id = account1Id,
                ["name"] = "Account With Multiple Contacts"
            };

            var contact1 = new Entity("contact")
            {
                Id = Guid.NewGuid(),
                ["firstname"] = "John",
                ["lastname"] = "Doe",
                ["parentcustomerid"] = new EntityReference("account", account1Id)
            };

            var contact2 = new Entity("contact")
            {
                Id = Guid.NewGuid(),
                ["firstname"] = "Jane",
                ["lastname"] = "Doe",
                ["parentcustomerid"] = new EntityReference("account", account1Id)
            };

            // Account 2: Has 1 contact - John
            var account2Id = Guid.NewGuid();
            var account2 = new Entity("account")
            {
                Id = account2Id,
                ["name"] = "Account With One John Contact"
            };

            var contact3 = new Entity("contact")
            {
                Id = Guid.NewGuid(),
                ["firstname"] = "John",
                ["lastname"] = "Smith",
                ["parentcustomerid"] = new EntityReference("account", account2Id)
            };

            // Account 3: Has no contacts
            var account3Id = Guid.NewGuid();
            var account3 = new Entity("account")
            {
                Id = account3Id,
                ["name"] = "Account With No Contacts"
            };

            // Account 4: Has 2 contacts - both named John
            var account4Id = Guid.NewGuid();
            var account4 = new Entity("account")
            {
                Id = account4Id,
                ["name"] = "Account With All Johns"
            };

            var contact4 = new Entity("contact")
            {
                Id = Guid.NewGuid(),
                ["firstname"] = "John",
                ["lastname"] = "Adams",
                ["parentcustomerid"] = new EntityReference("account", account4Id)
            };

            var contact5 = new Entity("contact")
            {
                Id = Guid.NewGuid(),
                ["firstname"] = "John",
                ["lastname"] = "Kennedy",
                ["parentcustomerid"] = new EntityReference("account", account4Id)
            };

            context.Initialize(new List<Entity>
            {
                account1, account2, account3, account4,
                contact1, contact2, contact3, contact4, contact5
            });

            return (context,
                    new List<Guid> { account1Id, account2Id, account3Id, account4Id },
                    new List<Guid> { contact1.Id, contact2.Id, contact3.Id, contact4.Id, contact5.Id });
        }

        #endregion

        #region Any (EXISTS) Tests

        [Fact]
        public void FetchXml_LinkType_Any_Returns_Accounts_With_At_Least_One_Contact()
        {
            // Arrange
            var (context, accountIds, _) = SetupAccountsWithContacts();
            var service = context.GetOrganizationService();

            // Act - Find accounts that have at least one contact
            var fetchXml = @"
                <fetch>
                    <entity name='account'>
                        <attribute name='name' />
                        <link-entity name='contact' from='parentcustomerid' to='accountid' link-type='any'>
                        </link-entity>
                    </entity>
                </fetch>";

            var results = service.RetrieveMultiple(new FetchExpression(fetchXml));

            // Assert - Should return accounts 1, 2, and 4 (accounts with contacts)
            Assert.Equal(3, results.Entities.Count);
            var resultIds = results.Entities.Select(e => e.Id).ToList();
            Assert.Contains(accountIds[0], resultIds); // Account with multiple contacts
            Assert.Contains(accountIds[1], resultIds); // Account with one John contact
            Assert.Contains(accountIds[3], resultIds); // Account with all Johns
            Assert.DoesNotContain(accountIds[2], resultIds); // Account with no contacts
        }

        [Fact]
        public void FetchXml_LinkType_Any_With_Filter_Returns_Accounts_With_Matching_Contact()
        {
            // Arrange
            var (context, accountIds, _) = SetupAccountsWithContacts();
            var service = context.GetOrganizationService();

            // Act - Find accounts that have at least one contact named "John"
            var fetchXml = @"
                <fetch>
                    <entity name='account'>
                        <attribute name='name' />
                        <link-entity name='contact' from='parentcustomerid' to='accountid' link-type='any'>
                            <filter>
                                <condition attribute='firstname' operator='eq' value='John' />
                            </filter>
                        </link-entity>
                    </entity>
                </fetch>";

            var results = service.RetrieveMultiple(new FetchExpression(fetchXml));

            // Assert - Should return accounts 1, 2, and 4 (all have at least one John)
            Assert.Equal(3, results.Entities.Count);
            var resultIds = results.Entities.Select(e => e.Id).ToList();
            Assert.Contains(accountIds[0], resultIds); // Has John and Jane
            Assert.Contains(accountIds[1], resultIds); // Has John Smith
            Assert.Contains(accountIds[3], resultIds); // Has two Johns
        }

        [Fact]
        public void FetchXml_LinkType_Any_With_Non_Matching_Filter_Returns_Empty()
        {
            // Arrange
            var (context, _, _) = SetupAccountsWithContacts();
            var service = context.GetOrganizationService();

            // Act - Find accounts that have at least one contact named "NonExistent"
            var fetchXml = @"
                <fetch>
                    <entity name='account'>
                        <attribute name='name' />
                        <link-entity name='contact' from='parentcustomerid' to='accountid' link-type='any'>
                            <filter>
                                <condition attribute='firstname' operator='eq' value='NonExistent' />
                            </filter>
                        </link-entity>
                    </entity>
                </fetch>";

            var results = service.RetrieveMultiple(new FetchExpression(fetchXml));

            // Assert - Should return no accounts
            Assert.Empty(results.Entities);
        }

        [Fact]
        public void FetchXml_LinkType_Any_With_Multiple_Filter_Conditions()
        {
            // Arrange
            var (context, accountIds, _) = SetupAccountsWithContacts();
            var service = context.GetOrganizationService();

            // Act - Find accounts that have at least one contact named John Doe
            var fetchXml = @"
                <fetch>
                    <entity name='account'>
                        <attribute name='name' />
                        <link-entity name='contact' from='parentcustomerid' to='accountid' link-type='any'>
                            <filter type='and'>
                                <condition attribute='firstname' operator='eq' value='John' />
                                <condition attribute='lastname' operator='eq' value='Doe' />
                            </filter>
                        </link-entity>
                    </entity>
                </fetch>";

            var results = service.RetrieveMultiple(new FetchExpression(fetchXml));

            // Assert - Should only return account 1 (the only one with John Doe)
            Assert.Single(results.Entities);
            Assert.Equal(accountIds[0], results.Entities[0].Id);
        }

        #endregion

        #region NotAny (NOT EXISTS) Tests

        [Fact]
        public void FetchXml_LinkType_NotAny_Returns_Accounts_Without_Contacts()
        {
            // Arrange
            var (context, accountIds, _) = SetupAccountsWithContacts();
            var service = context.GetOrganizationService();

            // Act - Find accounts that have NO contacts
            var fetchXml = @"
                <fetch>
                    <entity name='account'>
                        <attribute name='name' />
                        <link-entity name='contact' from='parentcustomerid' to='accountid' link-type='not any'>
                        </link-entity>
                    </entity>
                </fetch>";

            var results = service.RetrieveMultiple(new FetchExpression(fetchXml));

            // Assert - Should only return account 3 (no contacts)
            Assert.Single(results.Entities);
            Assert.Equal(accountIds[2], results.Entities[0].Id);
            Assert.Equal("Account With No Contacts", results.Entities[0].GetAttributeValue<string>("name"));
        }

        [Fact]
        public void FetchXml_LinkType_NotAny_With_Filter_Returns_Accounts_Without_Matching_Contacts()
        {
            // Arrange
            var (context, accountIds, _) = SetupAccountsWithContacts();
            var service = context.GetOrganizationService();

            // Act - Find accounts that have NO contacts named "Jane"
            var fetchXml = @"
                <fetch>
                    <entity name='account'>
                        <attribute name='name' />
                        <link-entity name='contact' from='parentcustomerid' to='accountid' link-type='not any'>
                            <filter>
                                <condition attribute='firstname' operator='eq' value='Jane' />
                            </filter>
                        </link-entity>
                    </entity>
                </fetch>";

            var results = service.RetrieveMultiple(new FetchExpression(fetchXml));

            // Assert - Should return accounts 2, 3, and 4 (don't have Jane)
            Assert.Equal(3, results.Entities.Count);
            var resultIds = results.Entities.Select(e => e.Id).ToList();
            Assert.Contains(accountIds[1], resultIds); // Has John Smith only
            Assert.Contains(accountIds[2], resultIds); // No contacts
            Assert.Contains(accountIds[3], resultIds); // Has two Johns
            Assert.DoesNotContain(accountIds[0], resultIds); // Has Jane
        }

        [Fact]
        public void FetchXml_LinkType_NotAny_Excludes_Accounts_With_Matching_Contacts()
        {
            // Arrange
            var (context, accountIds, _) = SetupAccountsWithContacts();
            var service = context.GetOrganizationService();

            // Act - Find accounts that have NO contacts named "John"
            var fetchXml = @"
                <fetch>
                    <entity name='account'>
                        <attribute name='name' />
                        <link-entity name='contact' from='parentcustomerid' to='accountid' link-type='not any'>
                            <filter>
                                <condition attribute='firstname' operator='eq' value='John' />
                            </filter>
                        </link-entity>
                    </entity>
                </fetch>";

            var results = service.RetrieveMultiple(new FetchExpression(fetchXml));

            // Assert - Should only return account 3 (no contacts at all, hence no Johns)
            Assert.Single(results.Entities);
            Assert.Equal(accountIds[2], results.Entities[0].Id);
        }

        #endregion

        #region All Tests

        [Fact]
        public void FetchXml_LinkType_All_Returns_Accounts_Where_All_Contacts_Match()
        {
            // Arrange
            var (context, accountIds, _) = SetupAccountsWithContacts();
            var service = context.GetOrganizationService();

            // Act - Find accounts where ALL contacts are named "John"
            var fetchXml = @"
                <fetch>
                    <entity name='account'>
                        <attribute name='name' />
                        <link-entity name='contact' from='parentcustomerid' to='accountid' link-type='all'>
                            <filter>
                                <condition attribute='firstname' operator='eq' value='John' />
                            </filter>
                        </link-entity>
                    </entity>
                </fetch>";

            var results = service.RetrieveMultiple(new FetchExpression(fetchXml));

            // Assert - Should return:
            // - Account 2 (has one John - all match)
            // - Account 3 (no contacts - vacuously true)
            // - Account 4 (has two Johns - all match)
            // Should NOT return Account 1 (has Jane who doesn't match)
            Assert.Equal(3, results.Entities.Count);
            var resultIds = results.Entities.Select(e => e.Id).ToList();
            Assert.Contains(accountIds[1], resultIds); // One John
            Assert.Contains(accountIds[2], resultIds); // No contacts (vacuously true)
            Assert.Contains(accountIds[3], resultIds); // All Johns
            Assert.DoesNotContain(accountIds[0], resultIds); // Has Jane
        }

        [Fact]
        public void FetchXml_LinkType_All_With_No_Related_Records_Returns_Parent_Vacuously_True()
        {
            // Arrange
            var (context, accountIds, _) = SetupAccountsWithContacts();
            var service = context.GetOrganizationService();

            // Act - Find accounts where ALL contacts have lastname "DoesNotExist"
            var fetchXml = @"
                <fetch>
                    <entity name='account'>
                        <attribute name='name' />
                        <link-entity name='contact' from='parentcustomerid' to='accountid' link-type='all'>
                            <filter>
                                <condition attribute='lastname' operator='eq' value='DoesNotExist' />
                            </filter>
                        </link-entity>
                    </entity>
                </fetch>";

            var results = service.RetrieveMultiple(new FetchExpression(fetchXml));

            // Assert - Should only return account 3 (no contacts - vacuously true)
            Assert.Single(results.Entities);
            Assert.Equal(accountIds[2], results.Entities[0].Id);
        }

        [Fact]
        public void FetchXml_LinkType_All_Excludes_Accounts_Where_Some_Contacts_Dont_Match()
        {
            // Arrange
            var (context, accountIds, _) = SetupAccountsWithContacts();
            var service = context.GetOrganizationService();

            // Act - Find accounts where ALL contacts have lastname "Doe"
            var fetchXml = @"
                <fetch>
                    <entity name='account'>
                        <attribute name='name' />
                        <link-entity name='contact' from='parentcustomerid' to='accountid' link-type='all'>
                            <filter>
                                <condition attribute='lastname' operator='eq' value='Doe' />
                            </filter>
                        </link-entity>
                    </entity>
                </fetch>";

            var results = service.RetrieveMultiple(new FetchExpression(fetchXml));

            // Assert - Should return:
            // - Account 1 (both John Doe and Jane Doe - all are Doe)
            // - Account 3 (no contacts - vacuously true)
            // Should NOT return:
            // - Account 2 (John Smith - not a Doe)
            // - Account 4 (John Adams and John Kennedy - not Does)
            Assert.Equal(2, results.Entities.Count);
            var resultIds = results.Entities.Select(e => e.Id).ToList();
            Assert.Contains(accountIds[0], resultIds); // All Does
            Assert.Contains(accountIds[2], resultIds); // No contacts
        }

        #endregion

        #region NotAll Tests

        [Fact]
        public void FetchXml_LinkType_NotAll_Returns_Accounts_Where_Some_Contacts_Dont_Match()
        {
            // Arrange
            var (context, accountIds, _) = SetupAccountsWithContacts();
            var service = context.GetOrganizationService();

            // Act - Find accounts where NOT ALL contacts are named "John"
            var fetchXml = @"
                <fetch>
                    <entity name='account'>
                        <attribute name='name' />
                        <link-entity name='contact' from='parentcustomerid' to='accountid' link-type='not all'>
                            <filter>
                                <condition attribute='firstname' operator='eq' value='John' />
                            </filter>
                        </link-entity>
                    </entity>
                </fetch>";

            var results = service.RetrieveMultiple(new FetchExpression(fetchXml));

            // Assert - Should only return Account 1 (has Jane who is not John)
            // Account 2, 3, 4 all have all Johns or no contacts
            Assert.Single(results.Entities);
            Assert.Equal(accountIds[0], results.Entities[0].Id);
        }

        [Fact]
        public void FetchXml_LinkType_NotAll_Excludes_Accounts_Where_All_Contacts_Match()
        {
            // Arrange
            var (context, accountIds, _) = SetupAccountsWithContacts();
            var service = context.GetOrganizationService();

            // Act - Find accounts where NOT ALL contacts have lastname "Doe"
            var fetchXml = @"
                <fetch>
                    <entity name='account'>
                        <attribute name='name' />
                        <link-entity name='contact' from='parentcustomerid' to='accountid' link-type='not all'>
                            <filter>
                                <condition attribute='lastname' operator='eq' value='Doe' />
                            </filter>
                        </link-entity>
                    </entity>
                </fetch>";

            var results = service.RetrieveMultiple(new FetchExpression(fetchXml));

            // Assert - Should return:
            // - Account 2 (John Smith - not a Doe)
            // - Account 4 (Adams and Kennedy - not Does)
            // Should NOT return:
            // - Account 1 (all Does)
            // - Account 3 (no contacts - vacuously all match)
            Assert.Equal(2, results.Entities.Count);
            var resultIds = results.Entities.Select(e => e.Id).ToList();
            Assert.Contains(accountIds[1], resultIds); // Has Smith
            Assert.Contains(accountIds[3], resultIds); // Has Adams and Kennedy
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void FetchXml_LinkType_Any_With_Empty_Related_Collection_Returns_Nothing()
        {
            // Arrange
            var context = new XrmFakedContext();
            var accountId = Guid.NewGuid();
            var account = new Entity("account")
            {
                Id = accountId,
                ["name"] = "Lonely Account"
            };

            context.Initialize(new List<Entity> { account });
            var service = context.GetOrganizationService();

            // Act
            var fetchXml = @"
                <fetch>
                    <entity name='account'>
                        <attribute name='name' />
                        <link-entity name='contact' from='parentcustomerid' to='accountid' link-type='any'>
                        </link-entity>
                    </entity>
                </fetch>";

            var results = service.RetrieveMultiple(new FetchExpression(fetchXml));

            // Assert
            Assert.Empty(results.Entities);
        }

        [Fact]
        public void FetchXml_LinkType_Any_With_Single_Related_Entity()
        {
            // Arrange
            var context = new XrmFakedContext();
            var accountId = Guid.NewGuid();
            var account = new Entity("account")
            {
                Id = accountId,
                ["name"] = "Account With Single Contact"
            };

            var contact = new Entity("contact")
            {
                Id = Guid.NewGuid(),
                ["firstname"] = "Solo",
                ["parentcustomerid"] = new EntityReference("account", accountId)
            };

            context.Initialize(new List<Entity> { account, contact });
            var service = context.GetOrganizationService();

            // Act
            var fetchXml = @"
                <fetch>
                    <entity name='account'>
                        <attribute name='name' />
                        <link-entity name='contact' from='parentcustomerid' to='accountid' link-type='any'>
                            <filter>
                                <condition attribute='firstname' operator='eq' value='Solo' />
                            </filter>
                        </link-entity>
                    </entity>
                </fetch>";

            var results = service.RetrieveMultiple(new FetchExpression(fetchXml));

            // Assert
            Assert.Single(results.Entities);
            Assert.Equal(accountId, results.Entities[0].Id);
        }

        [Fact]
        public void FetchXml_LinkType_Any_With_Multiple_Related_Entities_Mixed_Matches()
        {
            // Arrange
            var context = new XrmFakedContext();
            var accountId = Guid.NewGuid();
            var account = new Entity("account")
            {
                Id = accountId,
                ["name"] = "Mixed Account"
            };

            // 5 contacts: 2 match filter, 3 don't
            var contacts = new List<Entity>();
            for (int i = 0; i < 5; i++)
            {
                contacts.Add(new Entity("contact")
                {
                    Id = Guid.NewGuid(),
                    ["firstname"] = i < 2 ? "Match" : "NoMatch",
                    ["parentcustomerid"] = new EntityReference("account", accountId)
                });
            }

            var entities = new List<Entity> { account };
            entities.AddRange(contacts);
            context.Initialize(entities);
            var service = context.GetOrganizationService();

            // Act
            var fetchXml = @"
                <fetch>
                    <entity name='account'>
                        <attribute name='name' />
                        <link-entity name='contact' from='parentcustomerid' to='accountid' link-type='any'>
                            <filter>
                                <condition attribute='firstname' operator='eq' value='Match' />
                            </filter>
                        </link-entity>
                    </entity>
                </fetch>";

            var results = service.RetrieveMultiple(new FetchExpression(fetchXml));

            // Assert - Should return because at least one matches
            Assert.Single(results.Entities);
            Assert.Equal(accountId, results.Entities[0].Id);
        }

        [Fact]
        public void FetchXml_LinkType_All_With_Single_Non_Matching_Contact_Excludes_Parent()
        {
            // Arrange
            var context = new XrmFakedContext();
            var accountId = Guid.NewGuid();
            var account = new Entity("account")
            {
                Id = accountId,
                ["name"] = "Account With Non-Matching Contact"
            };

            // 10 matching contacts, 1 non-matching
            var contacts = new List<Entity>();
            for (int i = 0; i < 10; i++)
            {
                contacts.Add(new Entity("contact")
                {
                    Id = Guid.NewGuid(),
                    ["firstname"] = "Match",
                    ["parentcustomerid"] = new EntityReference("account", accountId)
                });
            }
            contacts.Add(new Entity("contact")
            {
                Id = Guid.NewGuid(),
                ["firstname"] = "NoMatch",
                ["parentcustomerid"] = new EntityReference("account", accountId)
            });

            var entities = new List<Entity> { account };
            entities.AddRange(contacts);
            context.Initialize(entities);
            var service = context.GetOrganizationService();

            // Act
            var fetchXml = @"
                <fetch>
                    <entity name='account'>
                        <attribute name='name' />
                        <link-entity name='contact' from='parentcustomerid' to='accountid' link-type='all'>
                            <filter>
                                <condition attribute='firstname' operator='eq' value='Match' />
                            </filter>
                        </link-entity>
                    </entity>
                </fetch>";

            var results = service.RetrieveMultiple(new FetchExpression(fetchXml));

            // Assert - Should NOT return because one contact doesn't match
            Assert.Empty(results.Entities);
        }

        #endregion

        #region Validation and Translation Tests

        [Fact]
        public void FetchXml_LinkType_Any_Translation_Sets_Correct_JoinOperator()
        {
            // Arrange
            var context = new XrmFakedContext();

            var fetchXml = @"
                <fetch>
                    <entity name='account'>
                        <attribute name='name' />
                        <link-entity name='contact' from='parentcustomerid' to='accountid' link-type='any'>
                            <filter>
                                <condition attribute='firstname' operator='eq' value='John' />
                            </filter>
                        </link-entity>
                    </entity>
                </fetch>";

            // Act
            var query = XrmFakedContext.TranslateFetchXmlToQueryExpression(context, fetchXml);

            // Assert
            Assert.Single(query.LinkEntities);
            Assert.Equal(JoinOperator.Any, query.LinkEntities[0].JoinOperator);
        }

        [Fact]
        public void FetchXml_LinkType_NotAny_Translation_Sets_Correct_JoinOperator()
        {
            // Arrange
            var context = new XrmFakedContext();

            var fetchXml = @"
                <fetch>
                    <entity name='account'>
                        <attribute name='name' />
                        <link-entity name='contact' from='parentcustomerid' to='accountid' link-type='not any'>
                        </link-entity>
                    </entity>
                </fetch>";

            // Act
            var query = XrmFakedContext.TranslateFetchXmlToQueryExpression(context, fetchXml);

            // Assert
            Assert.Single(query.LinkEntities);
            Assert.Equal(JoinOperator.NotAny, query.LinkEntities[0].JoinOperator);
        }

        [Fact]
        public void FetchXml_LinkType_All_Translation_Sets_Correct_JoinOperator()
        {
            // Arrange
            var context = new XrmFakedContext();

            var fetchXml = @"
                <fetch>
                    <entity name='account'>
                        <attribute name='name' />
                        <link-entity name='contact' from='parentcustomerid' to='accountid' link-type='all'>
                            <filter>
                                <condition attribute='firstname' operator='eq' value='John' />
                            </filter>
                        </link-entity>
                    </entity>
                </fetch>";

            // Act
            var query = XrmFakedContext.TranslateFetchXmlToQueryExpression(context, fetchXml);

            // Assert
            Assert.Single(query.LinkEntities);
            Assert.Equal(JoinOperator.All, query.LinkEntities[0].JoinOperator);
        }

        [Fact]
        public void FetchXml_LinkType_NotAll_Translation_Sets_Correct_JoinOperator()
        {
            // Arrange
            var context = new XrmFakedContext();

            var fetchXml = @"
                <fetch>
                    <entity name='account'>
                        <attribute name='name' />
                        <link-entity name='contact' from='parentcustomerid' to='accountid' link-type='not all'>
                            <filter>
                                <condition attribute='firstname' operator='eq' value='John' />
                            </filter>
                        </link-entity>
                    </entity>
                </fetch>";

            // Act
            var query = XrmFakedContext.TranslateFetchXmlToQueryExpression(context, fetchXml);

            // Assert
            Assert.Single(query.LinkEntities);
            Assert.Equal(JoinOperator.NotAll, query.LinkEntities[0].JoinOperator);
        }

        [Fact]
        public void FetchXml_LinkType_Any_With_Columns_Should_Be_Handled()
        {
            // Note: In real Dataverse, Any/All link-entities typically don't return columns
            // from the linked entity. This test verifies the behavior doesn't throw.

            // Arrange
            var (context, accountIds, _) = SetupAccountsWithContacts();
            var service = context.GetOrganizationService();

            // Act - Any link-entity with attribute (columns) specified
            var fetchXml = @"
                <fetch>
                    <entity name='account'>
                        <attribute name='name' />
                        <link-entity name='contact' from='parentcustomerid' to='accountid' link-type='any' alias='c'>
                            <attribute name='firstname' />
                            <filter>
                                <condition attribute='firstname' operator='eq' value='John' />
                            </filter>
                        </link-entity>
                    </entity>
                </fetch>";

            // This should not throw
            var results = service.RetrieveMultiple(new FetchExpression(fetchXml));

            // Assert - Should still return the accounts with matching contacts
            Assert.Equal(3, results.Entities.Count);
        }

        #endregion

        #region Complex Scenarios

        [Fact]
        public void FetchXml_LinkType_Any_Combined_With_Regular_Filter()
        {
            // Arrange
            var (context, accountIds, _) = SetupAccountsWithContacts();

            // Add a state to accounts for filtering
            var service = context.GetOrganizationService();

            // Update account 1 to have a different state
            var account1Update = new Entity("account", accountIds[0]);
            account1Update["statecode"] = new OptionSetValue(1); // Inactive
            service.Update(account1Update);

            // Act - Find active accounts that have at least one contact named "John"
            var fetchXml = @"
                <fetch>
                    <entity name='account'>
                        <attribute name='name' />
                        <filter>
                            <condition attribute='statecode' operator='eq' value='0' />
                        </filter>
                        <link-entity name='contact' from='parentcustomerid' to='accountid' link-type='any'>
                            <filter>
                                <condition attribute='firstname' operator='eq' value='John' />
                            </filter>
                        </link-entity>
                    </entity>
                </fetch>";

            var results = service.RetrieveMultiple(new FetchExpression(fetchXml));

            // Assert - Should return accounts 2 and 4 only (account 1 is inactive)
            Assert.Equal(2, results.Entities.Count);
            var resultIds = results.Entities.Select(e => e.Id).ToList();
            Assert.Contains(accountIds[1], resultIds);
            Assert.Contains(accountIds[3], resultIds);
            Assert.DoesNotContain(accountIds[0], resultIds); // Inactive
        }

        [Fact]
        public void FetchXml_LinkType_Any_With_Or_Filter_In_LinkEntity()
        {
            // Arrange
            var (context, accountIds, _) = SetupAccountsWithContacts();
            var service = context.GetOrganizationService();

            // Act - Find accounts with contacts named John OR Jane
            var fetchXml = @"
                <fetch>
                    <entity name='account'>
                        <attribute name='name' />
                        <link-entity name='contact' from='parentcustomerid' to='accountid' link-type='any'>
                            <filter type='or'>
                                <condition attribute='firstname' operator='eq' value='John' />
                                <condition attribute='firstname' operator='eq' value='Jane' />
                            </filter>
                        </link-entity>
                    </entity>
                </fetch>";

            var results = service.RetrieveMultiple(new FetchExpression(fetchXml));

            // Assert - Should return accounts 1, 2, and 4
            Assert.Equal(3, results.Entities.Count);
            var resultIds = results.Entities.Select(e => e.Id).ToList();
            Assert.Contains(accountIds[0], resultIds); // Has John and Jane
            Assert.Contains(accountIds[1], resultIds); // Has John
            Assert.Contains(accountIds[3], resultIds); // Has two Johns
        }

        [Fact]
        public void FetchXml_Multiple_Any_LinkEntities()
        {
            // Arrange
            var context = new XrmFakedContext();

            var accountId = Guid.NewGuid();
            var account = new Entity("account")
            {
                Id = accountId,
                ["name"] = "Test Account"
            };

            var contact = new Entity("contact")
            {
                Id = Guid.NewGuid(),
                ["firstname"] = "John",
                ["parentcustomerid"] = new EntityReference("account", accountId)
            };

            var opportunity = new Entity("opportunity")
            {
                Id = Guid.NewGuid(),
                ["name"] = "Big Deal",
                ["parentaccountid"] = new EntityReference("account", accountId)
            };

            context.Initialize(new List<Entity> { account, contact, opportunity });
            var service = context.GetOrganizationService();

            // Act - Find accounts with both contacts and opportunities
            var fetchXml = @"
                <fetch>
                    <entity name='account'>
                        <attribute name='name' />
                        <link-entity name='contact' from='parentcustomerid' to='accountid' link-type='any'>
                        </link-entity>
                        <link-entity name='opportunity' from='parentaccountid' to='accountid' link-type='any'>
                        </link-entity>
                    </entity>
                </fetch>";

            var results = service.RetrieveMultiple(new FetchExpression(fetchXml));

            // Assert
            Assert.Single(results.Entities);
            Assert.Equal(accountId, results.Entities[0].Id);
        }

        [Fact]
        public void FetchXml_LinkType_NotAny_And_Any_Combined()
        {
            // Arrange
            var context = new XrmFakedContext();

            // Account 1: Has contacts but no opportunities
            var account1Id = Guid.NewGuid();
            var account1 = new Entity("account")
            {
                Id = account1Id,
                ["name"] = "Account with Contacts Only"
            };

            var contact1 = new Entity("contact")
            {
                Id = Guid.NewGuid(),
                ["firstname"] = "John",
                ["parentcustomerid"] = new EntityReference("account", account1Id)
            };

            // Account 2: Has both contacts and opportunities
            var account2Id = Guid.NewGuid();
            var account2 = new Entity("account")
            {
                Id = account2Id,
                ["name"] = "Account with Both"
            };

            var contact2 = new Entity("contact")
            {
                Id = Guid.NewGuid(),
                ["firstname"] = "Jane",
                ["parentcustomerid"] = new EntityReference("account", account2Id)
            };

            var opportunity = new Entity("opportunity")
            {
                Id = Guid.NewGuid(),
                ["name"] = "Deal",
                ["parentaccountid"] = new EntityReference("account", account2Id)
            };

            context.Initialize(new List<Entity> { account1, account2, contact1, contact2, opportunity });
            var service = context.GetOrganizationService();

            // Act - Find accounts that have contacts but NO opportunities
            var fetchXml = @"
                <fetch>
                    <entity name='account'>
                        <attribute name='name' />
                        <link-entity name='contact' from='parentcustomerid' to='accountid' link-type='any'>
                        </link-entity>
                        <link-entity name='opportunity' from='parentaccountid' to='accountid' link-type='not any'>
                        </link-entity>
                    </entity>
                </fetch>";

            var results = service.RetrieveMultiple(new FetchExpression(fetchXml));

            // Assert - Should only return account 1
            Assert.Single(results.Entities);
            Assert.Equal(account1Id, results.Entities[0].Id);
        }

        #endregion

        #region QueryExpression Equivalence Tests

        [Fact]
        public void FetchXml_LinkType_Any_Produces_Same_Results_As_QueryExpression()
        {
            // Arrange
            var (context, accountIds, _) = SetupAccountsWithContacts();
            var service = context.GetOrganizationService();

            // FetchXML
            var fetchXml = @"
                <fetch>
                    <entity name='account'>
                        <attribute name='name' />
                        <link-entity name='contact' from='parentcustomerid' to='accountid' link-type='any'>
                            <filter>
                                <condition attribute='firstname' operator='eq' value='John' />
                            </filter>
                        </link-entity>
                    </entity>
                </fetch>";

      // Equivalent QueryExpression
      var query = new QueryExpression("account")
      {
        ColumnSet = new ColumnSet("name")
      };
      var linkEntity = query.AddLink("contact", "accountid", "parentcustomerid", JoinOperator.Any);
            linkEntity.LinkCriteria.AddCondition("firstname", ConditionOperator.Equal, "John");

            // Act
            var fetchResults = service.RetrieveMultiple(new FetchExpression(fetchXml));
            var queryResults = service.RetrieveMultiple(query);

            // Assert
            Assert.Equal(queryResults.Entities.Count, fetchResults.Entities.Count);
            var fetchIds = fetchResults.Entities.Select(e => e.Id).OrderBy(x => x).ToList();
            var queryIds = queryResults.Entities.Select(e => e.Id).OrderBy(x => x).ToList();
            Assert.Equal(queryIds, fetchIds);
        }

        #endregion
    }
}
