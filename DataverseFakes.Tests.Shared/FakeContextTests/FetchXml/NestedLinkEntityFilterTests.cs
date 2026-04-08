using Crm;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace FakeXrmEasy.Tests.FakeContextTests.FetchXml
{
    /// <summary>
    /// Tests for GitHub Issue #560 - FindReflectedAttributeType throws NRE when filtering on linked entity attributes.
    /// These tests verify that filters on nested LinkEntity attributes work correctly,
    /// including when using entityname without explicit alias and deeply nested structures.
    /// </summary>
    public class NestedLinkEntityFilterTests
    {
        /// <summary>
        /// Test that filtering on a deeply nested LinkEntity attribute works correctly.
        /// This reproduces the scenario where GetEntityNameFromAlias only searched top-level LinkEntities.
        /// </summary>
        [Fact]
        public void When_filtering_on_deeply_nested_link_entity_attribute_correct_results_are_returned()
        {
      var context = new XrmFakedContext
      {
        ProxyTypesAssembly = Assembly.GetAssembly(typeof(Account))
      };

      // Create test data: Account -> Contact -> Lead (nested relationship)
      var lead1 = new Lead
            {
                Id = Guid.NewGuid(),
                Subject = "Hot Lead",
                FirstName = "John"
            };

            var lead2 = new Lead
            {
                Id = Guid.NewGuid(),
                Subject = "Cold Lead",
                FirstName = "Jane"
            };

            var contact1 = new Contact
            {
                Id = Guid.NewGuid(),
                FirstName = "Contact1",
                OriginatingLeadId = lead1.ToEntityReference()
            };

            var contact2 = new Contact
            {
                Id = Guid.NewGuid(),
                FirstName = "Contact2",
                OriginatingLeadId = lead2.ToEntityReference()
            };

            var account1 = new Account
            {
                Id = Guid.NewGuid(),
                Name = "Account1",
                PrimaryContactId = contact1.ToEntityReference()
            };

            var account2 = new Account
            {
                Id = Guid.NewGuid(),
                Name = "Account2",
                PrimaryContactId = contact2.ToEntityReference()
            };

            context.Initialize(new List<Entity> { lead1, lead2, contact1, contact2, account1, account2 });

            // FetchXML with nested link-entity and filter on the innermost entity
            var fetchXml = @"
                <fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                    <entity name='account'>
                        <attribute name='name' />
                        <link-entity name='contact' from='contactid' to='primarycontactid' alias='c'>
                            <attribute name='firstname' />
                            <link-entity name='lead' from='leadid' to='originatingleadid' alias='l'>
                                <attribute name='subject' />
                                <filter type='and'>
                                    <condition attribute='subject' operator='eq' value='Hot Lead' />
                                </filter>
                            </link-entity>
                        </link-entity>
                    </entity>
                </fetch>";

            var service = context.GetOrganizationService();
            var result = service.RetrieveMultiple(new FetchExpression(fetchXml));

            Assert.Single(result.Entities);
            Assert.Equal("Account1", result.Entities[0]["name"]);
        }

        /// <summary>
        /// Test that filtering works when using entityname attribute without explicit alias.
        /// FetchXML allows specifying entityname in conditions without requiring an explicit alias.
        /// </summary>
        [Fact]
        public void When_filtering_using_entityname_without_explicit_alias_correct_results_are_returned()
        {
      var context = new XrmFakedContext
      {
        ProxyTypesAssembly = Assembly.GetAssembly(typeof(Account))
      };

      var contact1 = new Contact
            {
                Id = Guid.NewGuid(),
                FirstName = "John",
                LastName = "Doe"
            };

            var contact2 = new Contact
            {
                Id = Guid.NewGuid(),
                FirstName = "Jane",
                LastName = "Smith"
            };

            var account1 = new Account
            {
                Id = Guid.NewGuid(),
                Name = "Account1",
                PrimaryContactId = contact1.ToEntityReference()
            };

            var account2 = new Account
            {
                Id = Guid.NewGuid(),
                Name = "Account2",
                PrimaryContactId = contact2.ToEntityReference()
            };

            context.Initialize(new List<Entity> { contact1, contact2, account1, account2 });

            // FetchXML with link-entity without explicit alias, filter references entityname
            var fetchXml = @"
                <fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                    <entity name='account'>
                        <attribute name='name' />
                        <link-entity name='contact' from='contactid' to='primarycontactid'>
                            <attribute name='firstname' />
                            <filter type='and'>
                                <condition attribute='firstname' operator='eq' value='John' />
                            </filter>
                        </link-entity>
                    </entity>
                </fetch>";

            var service = context.GetOrganizationService();
            var result = service.RetrieveMultiple(new FetchExpression(fetchXml));

            Assert.Single(result.Entities);
            Assert.Equal("Account1", result.Entities[0]["name"]);
        }

        /// <summary>
        /// Test that multiple levels of nesting work correctly with mixed explicit and implicit aliases.
        /// </summary>
        [Fact]
        public void When_multiple_levels_of_nesting_with_filters_correct_results_are_returned()
        {
      var context = new XrmFakedContext
      {
        ProxyTypesAssembly = Assembly.GetAssembly(typeof(Account))
      };

      // Create a 3-level nested structure
      var systemUser = new SystemUser
            {
                Id = Guid.NewGuid(),
                FirstName = "Admin",
                LastName = "User"
            };

            var contact1 = new Contact
            {
                Id = Guid.NewGuid(),
                FirstName = "John",
                OwnerId = systemUser.ToEntityReference()
            };

            var contact2 = new Contact
            {
                Id = Guid.NewGuid(),
                FirstName = "Jane",
                OwnerId = systemUser.ToEntityReference()
            };

            var account1 = new Account
            {
                Id = Guid.NewGuid(),
                Name = "Account1",
                PrimaryContactId = contact1.ToEntityReference()
            };

            var account2 = new Account
            {
                Id = Guid.NewGuid(),
                Name = "Account2",
                PrimaryContactId = contact2.ToEntityReference()
            };

            context.Initialize(new List<Entity> { systemUser, contact1, contact2, account1, account2 });

            // FetchXML with multiple nested link-entities with filters at different levels
            var fetchXml = @"
                <fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                    <entity name='account'>
                        <attribute name='name' />
                        <link-entity name='contact' from='contactid' to='primarycontactid' alias='c'>
                            <attribute name='firstname' />
                            <filter type='and'>
                                <condition attribute='firstname' operator='eq' value='John' />
                            </filter>
                            <link-entity name='systemuser' from='systemuserid' to='ownerid' alias='u'>
                                <attribute name='firstname' />
                                <filter type='and'>
                                    <condition attribute='firstname' operator='eq' value='Admin' />
                                </filter>
                            </link-entity>
                        </link-entity>
                    </entity>
                </fetch>";

            var service = context.GetOrganizationService();
            var result = service.RetrieveMultiple(new FetchExpression(fetchXml));

            Assert.Single(result.Entities);
            Assert.Equal("Account1", result.Entities[0]["name"]);
        }

        /// <summary>
        /// Test that the GetEntityNameFromAlias extension method correctly resolves nested aliases.
        /// This is a unit test for the fix itself.
        /// </summary>
        [Fact]
        public void GetEntityNameFromAlias_should_find_nested_link_entity_by_alias()
        {
            var qe = new QueryExpression("account");

      // First level link
      var contactLink = new LinkEntity("account", "contact", "primarycontactid", "contactid", JoinOperator.Inner)
      {
        EntityAlias = "c"
      };

      // Nested link
      var leadLink = new LinkEntity("contact", "lead", "originatingleadid", "leadid", JoinOperator.Inner)
      {
        EntityAlias = "l"
      };

      contactLink.LinkEntities.Add(leadLink);
            qe.LinkEntities.Add(contactLink);

            // Test that we can find the nested alias
            var result = FakeXrmEasy.Extensions.QueryExpressionExtensions.GetEntityNameFromAlias(qe, "l");
            Assert.Equal("lead", result);

            // Test that we can still find the top-level alias
            var result2 = FakeXrmEasy.Extensions.QueryExpressionExtensions.GetEntityNameFromAlias(qe, "c");
            Assert.Equal("contact", result2);
        }

        /// <summary>
        /// Test that the GetEntityNameFromAlias extension method correctly resolves by entity name when no alias is set.
        /// </summary>
        [Fact]
        public void GetEntityNameFromAlias_should_find_link_entity_by_logical_name_when_no_alias()
        {
            var qe = new QueryExpression("account");

            // Link without explicit alias
            var contactLink = new LinkEntity("account", "contact", "primarycontactid", "contactid", JoinOperator.Inner);
            // No EntityAlias set

            qe.LinkEntities.Add(contactLink);

            // Test that we can find by entity logical name
            var result = FakeXrmEasy.Extensions.QueryExpressionExtensions.GetEntityNameFromAlias(qe, "contact");
            Assert.Equal("contact", result);
        }

        /// <summary>
        /// Test that GetEntityNameFromAlias handles the dot notation (e.g., "contact.fullname").
        /// </summary>
        [Fact]
        public void GetEntityNameFromAlias_should_handle_dot_notation_in_alias()
        {
            var qe = new QueryExpression("account");

      var contactLink = new LinkEntity("account", "contact", "primarycontactid", "contactid", JoinOperator.Inner)
      {
        EntityAlias = "c"
      };

      qe.LinkEntities.Add(contactLink);

            // Test with dot notation (extracting entity portion)
            var result = FakeXrmEasy.Extensions.QueryExpressionExtensions.GetEntityNameFromAlias(qe, "c.fullname");
            Assert.Equal("contact", result);
        }

        /// <summary>
        /// Test QueryExpression with deeply nested LinkEntities and filters using proxy types.
        /// This ensures the fix works in a real-world scenario with early-bound entities.
        /// </summary>
        [Fact]
        public void When_using_queryexpression_with_deeply_nested_link_entities_and_filters_correct_results_are_returned()
        {
      var context = new XrmFakedContext
      {
        ProxyTypesAssembly = Assembly.GetAssembly(typeof(Account))
      };

      var lead1 = new Lead
            {
                Id = Guid.NewGuid(),
                Subject = "Target Lead"
            };

            var lead2 = new Lead
            {
                Id = Guid.NewGuid(),
                Subject = "Other Lead"
            };

            var contact1 = new Contact
            {
                Id = Guid.NewGuid(),
                FirstName = "Contact1",
                OriginatingLeadId = lead1.ToEntityReference()
            };

            var contact2 = new Contact
            {
                Id = Guid.NewGuid(),
                FirstName = "Contact2",
                OriginatingLeadId = lead2.ToEntityReference()
            };

            var account1 = new Account
            {
                Id = Guid.NewGuid(),
                Name = "TargetAccount",
                PrimaryContactId = contact1.ToEntityReference()
            };

            var account2 = new Account
            {
                Id = Guid.NewGuid(),
                Name = "OtherAccount",
                PrimaryContactId = contact2.ToEntityReference()
            };

            context.Initialize(new List<Entity> { lead1, lead2, contact1, contact2, account1, account2 });

      // Create QueryExpression with nested LinkEntities
      var qe = new QueryExpression("account")
      {
        ColumnSet = new ColumnSet("name")
      };

      var contactLink = qe.AddLink("contact", "primarycontactid", "contactid", JoinOperator.Inner);
            contactLink.EntityAlias = "c";
            contactLink.Columns = new ColumnSet("firstname");

            var leadLink = contactLink.AddLink("lead", "originatingleadid", "leadid", JoinOperator.Inner);
            leadLink.EntityAlias = "l";
            leadLink.Columns = new ColumnSet("subject");
            leadLink.LinkCriteria.AddCondition("subject", ConditionOperator.Equal, "Target Lead");

            var service = context.GetOrganizationService();
            var result = service.RetrieveMultiple(qe);

            Assert.Single(result.Entities);
            Assert.Equal("TargetAccount", result.Entities[0]["name"]);
        }
    }
}
