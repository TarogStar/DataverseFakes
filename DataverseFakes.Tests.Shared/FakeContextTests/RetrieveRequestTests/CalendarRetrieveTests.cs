using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using Xunit;

namespace FakeXrmEasy.Tests.FakeContextTests
{
    public class CalendarRetrieveTests
    {
        [Fact]
        public void When_retrieving_a_calendar_calendarrules_are_returned_even_if_not_requested()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var calendarId = Guid.NewGuid();

            var rule = new Entity("calendarrule") { Id = Guid.NewGuid() };
            rule["name"] = "Rule";

            var rules = new EntityCollection();
            rules.Entities.Add(rule);

            var calendar = new Entity("calendar") { Id = calendarId };
            calendar["calendarrules"] = rules;

            context.Initialize(new[] { calendar });

            var retrieved = service.Retrieve("calendar", calendarId, new ColumnSet("calendarid"));

            Assert.True(retrieved.Attributes.ContainsKey("calendarrules"));
            var returnedRules = retrieved.GetAttributeValue<EntityCollection>("calendarrules");
            Assert.NotNull(returnedRules);
            Assert.Single(returnedRules.Entities);
        }

        [Fact]
        public void When_retrieving_a_calendar_with_calendarrules_in_columnset_it_does_not_throw()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var calendarId = Guid.NewGuid();

            var rules = new EntityCollection();
            rules.Entities.Add(new Entity("calendarrule") { Id = Guid.NewGuid() });

            var calendar = new Entity("calendar") { Id = calendarId };
            calendar["calendarrules"] = rules;

            context.Initialize(new[] { calendar });

            var retrieved = service.Retrieve("calendar", calendarId, new ColumnSet("calendarid", "calendarrules"));

            Assert.True(retrieved.Attributes.ContainsKey("calendarrules"));
        }
    }
}
