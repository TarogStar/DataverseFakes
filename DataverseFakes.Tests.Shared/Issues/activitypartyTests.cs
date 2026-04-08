using Crm;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.ServiceModel;
using Xunit;

namespace FakeXrmEasy.Tests.Issues
{
    public class ActivityPartyTests
    {
        [Fact]
        public void Retrieve_of_activityparty_is_not_supported()
        {
            var context = new XrmFakedContext();

            var activityParty = new Entity("activityparty")
            {
                Id = Guid.NewGuid()
            };

            context.Initialize(new List<Entity> { activityParty });

            var service = context.GetOrganizationService();

            var ex = Assert.Throws<FaultException<OrganizationServiceFault>>(() =>
                service.Retrieve("activityparty", activityParty.Id, new ColumnSet(true)));

            Assert.Contains("does not support entities of type 'activityparty'", ex.Message);
        }

        [Fact]
        public void ActivityParty_should_be_accessible_via_activity_entity_collection_attributes()
        {
            var context = new XrmFakedContext();
            context.ProxyTypesAssembly = typeof(PhoneCall).Assembly;

            var contact = new Contact
            {
                Id = Guid.NewGuid(),
                FirstName = "Test",
                LastName = "Contact"
            };

            // Use the activity party list format: EntityCollection of activityparty entities
            var party = new Entity("activityparty")
            {
                Id = Guid.NewGuid(),
                ["partyid"] = contact.ToEntityReference()
            };

            var phoneCall = new PhoneCall
            {
                Id = Guid.NewGuid(),
                Subject = "Test phone call"
            };

            phoneCall["to"] = new EntityCollection(new List<Entity> { party });

            context.Initialize(new List<Entity> { contact, phoneCall });

            var service = context.GetOrganizationService();
            var retrievedPhoneCall = service.Retrieve(PhoneCall.EntityLogicalName, phoneCall.Id, new ColumnSet(true));

            var toParties = retrievedPhoneCall.GetAttributeValue<EntityCollection>("to");
            Assert.NotNull(toParties);

            var firstParty = Assert.Single(toParties.Entities);
            var partyId = firstParty.GetAttributeValue<EntityReference>("partyid");
            Assert.NotNull(partyId);
            Assert.Equal(contact.Id, partyId.Id);
        }
    }
}
