using DataverseFakes;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using Xunit;

namespace DataverseFakes.Tests.FakeContextTests.AssertionTests
{
    public class AssertionExtensionsTests
    {
        private static XrmFakedContext ContextWithAccount(Guid id, Action<Entity> configure = null)
        {
            var account = new Entity("account") { Id = id };
            configure?.Invoke(account);
            var context = new XrmFakedContext();
            context.Initialize(new[] { account });
            return context;
        }

        // ---- Existence -----------------------------------------------------------------------

        [Fact]
        public void AssertExists_passes_when_record_present()
        {
            var id = Guid.NewGuid();
            var context = ContextWithAccount(id);
            context.AssertExists("account", id); // no throw
        }

        [Fact]
        public void AssertExists_throws_when_record_missing()
        {
            var context = new XrmFakedContext();
            var ex = Assert.Throws<XrmFakedAssertException>(() => context.AssertExists("account", Guid.NewGuid()));
            Assert.Contains("to exist", ex.Message);
        }

        [Fact]
        public void AssertDoesNotExist_passes_when_record_absent()
        {
            var context = new XrmFakedContext();
            context.AssertDoesNotExist("account", Guid.NewGuid()); // no throw
        }

        [Fact]
        public void AssertDoesNotExist_throws_when_record_present()
        {
            var id = Guid.NewGuid();
            var context = ContextWithAccount(id);
            Assert.Throws<XrmFakedAssertException>(() => context.AssertDoesNotExist("account", id));
        }

        // ---- Attribute value -----------------------------------------------------------------

        [Fact]
        public void AssertAttributeValue_passes_on_string()
        {
            var id = Guid.NewGuid();
            var context = ContextWithAccount(id, a => a["name"] = "Contoso");
            context.AssertAttributeValue("account", id, "name", "Contoso");
        }

        [Fact]
        public void AssertAttributeValue_throws_with_expected_and_actual_in_message()
        {
            var id = Guid.NewGuid();
            var context = ContextWithAccount(id, a => a["name"] = "Fabrikam");
            var ex = Assert.Throws<XrmFakedAssertException>(() => context.AssertAttributeValue("account", id, "name", "Contoso"));
            Assert.Contains("Contoso", ex.Message);
            Assert.Contains("Fabrikam", ex.Message);
        }

        [Fact]
        public void AssertAttributeValue_normalizes_optionsetvalue_to_int()
        {
            var id = Guid.NewGuid();
            var context = ContextWithAccount(id, a => a["statecode"] = new OptionSetValue(0));
            context.AssertAttributeValue("account", id, "statecode", 0);                 // raw int
            context.AssertAttributeValue("account", id, "statecode", new OptionSetValue(0)); // wrapper
        }

        [Fact]
        public void AssertAttributeValue_normalizes_money_to_decimal()
        {
            var id = Guid.NewGuid();
            var context = ContextWithAccount(id, a => a["revenue"] = new Money(1000m));
            context.AssertAttributeValue("account", id, "revenue", 1000m);
            context.AssertAttributeValue("account", id, "revenue", new Money(1000m));
        }

        [Fact]
        public void AssertAttributeValue_compares_entityreference_by_id_and_logicalname()
        {
            var id = Guid.NewGuid();
            var ownerId = Guid.NewGuid();
            var context = ContextWithAccount(id, a => a["ownerid"] = new EntityReference("systemuser", ownerId));
            context.AssertAttributeValue("account", id, "ownerid", ownerId);                                   // Guid
            context.AssertAttributeValue("account", id, "ownerid", new EntityReference("systemuser", ownerId)); // ER

            Assert.Throws<XrmFakedAssertException>(() =>
                context.AssertAttributeValue("account", id, "ownerid", new EntityReference("team", ownerId))); // wrong logical name
        }

        [Fact]
        public void AssertAttributeValue_throws_when_record_missing()
        {
            var context = new XrmFakedContext();
            Assert.Throws<XrmFakedAssertException>(() => context.AssertAttributeValue("account", Guid.NewGuid(), "name", "x"));
        }

        [Fact]
        public void AssertAttributeValue_throws_when_attribute_absent()
        {
            var id = Guid.NewGuid();
            var context = ContextWithAccount(id);
            Assert.Throws<XrmFakedAssertException>(() => context.AssertAttributeValue("account", id, "name", "Contoso"));
        }

        // ---- Has attribute / null ------------------------------------------------------------

        [Fact]
        public void AssertHasAttribute_passes_when_present_and_nonnull()
        {
            var id = Guid.NewGuid();
            var context = ContextWithAccount(id, a => a["name"] = "Contoso");
            context.AssertHasAttribute("account", id, "name");
        }

        [Fact]
        public void AssertHasAttribute_throws_when_absent()
        {
            var id = Guid.NewGuid();
            var context = ContextWithAccount(id);
            Assert.Throws<XrmFakedAssertException>(() => context.AssertHasAttribute("account", id, "name"));
        }

        [Fact]
        public void AssertAttributeNull_passes_when_absent()
        {
            var id = Guid.NewGuid();
            var context = ContextWithAccount(id);
            context.AssertAttributeNull("account", id, "name");
        }

        [Fact]
        public void AssertAttributeNull_throws_when_present()
        {
            var id = Guid.NewGuid();
            var context = ContextWithAccount(id, a => a["name"] = "Contoso");
            Assert.Throws<XrmFakedAssertException>(() => context.AssertAttributeNull("account", id, "name"));
        }

        // ---- Record count --------------------------------------------------------------------

        [Fact]
        public void AssertRecordCount_by_entity_passes_and_throws()
        {
            var context = new XrmFakedContext();
            context.Initialize(new[]
            {
                new Entity("task") { Id = Guid.NewGuid() },
                new Entity("task") { Id = Guid.NewGuid() },
            });
            context.AssertRecordCount("task", 2);
            context.AssertRecordCount("contact", 0); // absent entity counts as 0
            Assert.Throws<XrmFakedAssertException>(() => context.AssertRecordCount("task", 3));
        }

        [Fact]
        public void AssertRecordCount_by_query_passes()
        {
            var context = new XrmFakedContext();
            context.Initialize(new[]
            {
                new Entity("task") { Id = Guid.NewGuid(), ["subject"] = "keep" },
                new Entity("task") { Id = Guid.NewGuid(), ["subject"] = "drop" },
            });
            var query = new QueryExpression("task")
            {
                Criteria = new FilterExpression { Conditions = { new ConditionExpression("subject", ConditionOperator.Equal, "keep") } }
            };
            context.AssertRecordCount(query, 1);
            Assert.Throws<XrmFakedAssertException>(() => context.AssertRecordCount(query, 2));
        }

        // ---- Association (N:N) ---------------------------------------------------------------

        private static XrmFakedContext AssociatedContext(Guid accountId, Guid contactId)
        {
            var context = new XrmFakedContext();
            context.Initialize(new[]
            {
                new Entity("account") { Id = accountId },
                new Entity("contact") { Id = contactId },
            });
            context.AddRelationship("new_account_contact", new XrmFakedRelationship
            {
                IntersectEntity = "new_account_contact",
                Entity1LogicalName = "account",
                Entity1Attribute = "accountid",
                Entity2LogicalName = "contact",
                Entity2Attribute = "contactid",
                RelationshipType = XrmFakedRelationship.enmFakeRelationshipType.ManyToMany
            });
            return context;
        }

        [Fact]
        public void AssertAssociated_passes_in_both_orderings()
        {
            var accountId = Guid.NewGuid();
            var contactId = Guid.NewGuid();
            var context = AssociatedContext(accountId, contactId);

            context.GetOrganizationService().Associate("account", accountId,
                new Relationship("new_account_contact"),
                new EntityReferenceCollection { new EntityReference("contact", contactId) });

            context.AssertAssociated("account", accountId, "contact", contactId, "new_account_contact");
            context.AssertAssociated("contact", contactId, "account", accountId, "new_account_contact"); // reversed order
        }

        [Fact]
        public void AssertAssociated_throws_when_not_associated()
        {
            var accountId = Guid.NewGuid();
            var contactId = Guid.NewGuid();
            var context = AssociatedContext(accountId, contactId);

            Assert.Throws<XrmFakedAssertException>(() =>
                context.AssertAssociated("account", accountId, "contact", contactId, "new_account_contact"));
        }

        [Fact]
        public void AssertNotAssociated_passes_when_not_associated_and_throws_when_associated()
        {
            var accountId = Guid.NewGuid();
            var contactId = Guid.NewGuid();
            var context = AssociatedContext(accountId, contactId);

            context.AssertNotAssociated("account", accountId, "contact", contactId, "new_account_contact"); // not yet associated

            context.GetOrganizationService().Associate("account", accountId,
                new Relationship("new_account_contact"),
                new EntityReferenceCollection { new EntityReference("contact", contactId) });

            Assert.Throws<XrmFakedAssertException>(() =>
                context.AssertNotAssociated("account", accountId, "contact", contactId, "new_account_contact"));
        }

        [Fact]
        public void AssertAssociated_throws_for_unknown_relationship()
        {
            var context = new XrmFakedContext();
            var ex = Assert.Throws<XrmFakedAssertException>(() =>
                context.AssertAssociated("account", Guid.NewGuid(), "contact", Guid.NewGuid(), "no_such_rel"));
            Assert.Contains("no_such_rel", ex.Message);
        }
    }
}
