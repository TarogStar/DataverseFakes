using System;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Crm.Sdk.Messages;

namespace FakeXrmEasy.FakeMessageExecutors
{
    /// <summary>
    /// Implements a fake message executor for the CRM QualifyLeadRequest message.
    /// This executor simulates the lead qualification process in Dynamics 365 / Power Platform,
    /// which converts a lead into account, contact, and/or opportunity records based on the request parameters.
    /// </summary>
    public class QualifyLeadRequestExecutor : IFakeMessageExecutor
    {
        /// <summary>
        /// Determines whether this executor can handle the specified organization request.
        /// </summary>
        /// <param name="request">The organization request to evaluate.</param>
        /// <returns><c>true</c> if the request is a <see cref="QualifyLeadRequest"/>; otherwise, <c>false</c>.</returns>
        public bool CanExecute(OrganizationRequest request)
        {
            return request is QualifyLeadRequest;
        }

        /// <summary>
        /// Executes the QualifyLeadRequest against the faked CRM context.
        /// This method qualifies a lead by optionally creating account, contact, and/or opportunity records
        /// based on the request properties. The lead's status is updated to reflect the qualification.
        /// Created entities are linked to the original lead via the originatingleadid attribute.
        /// </summary>
        /// <param name="request">The organization request to execute. Must be a <see cref="QualifyLeadRequest"/>.</param>
        /// <param name="ctx">The faked XRM context that simulates the CRM environment.</param>
        /// <returns>
        /// A <see cref="QualifyLeadResponse"/> containing the CreatedEntities collection with references
        /// to any account, contact, and/or opportunity records that were created during qualification.
        /// </returns>
        /// <exception cref="Exception">
        /// Thrown when the LeadId is null, when no lead exists with the specified ID, when multiple leads
        /// exist with the same ID, or when the OpportunityCustomerId references an entity type other than
        /// account or contact.
        /// </exception>
        public OrganizationResponse Execute(OrganizationRequest request, XrmFakedContext ctx)
        {
            var req = request as QualifyLeadRequest;

            var orgService = ctx.GetOrganizationService();

            if (req.LeadId == null) throw new Exception("Lead Id must be set in request.");

            var leads = (from l in ctx.CreateQuery("lead")
                         where l.Id == req.LeadId.Id
                         select l);

            var leadsCount = leads.Count();

            if (leadsCount != 1) throw new Exception(string.Format("Number of Leads by given LeadId should be 1. Instead it is {0}.", leadsCount));

            // Made here to get access to CreatedEntities collection
            var response = new QualifyLeadResponse();
            response["CreatedEntities"] = new EntityReferenceCollection();

            // Create Account
            if (req.CreateAccount) // ParentAccount
            {
                var account = new Entity("account")
                {
                    Id = Guid.NewGuid()
                };
                account.Attributes["originatingleadid"] = req.LeadId;
                orgService.Create(account);
                response.CreatedEntities.Add(account.ToEntityReference());
            }

            // Create Contact
            if (req.CreateContact)
            {
                var contact = new Entity("contact")
                {
                    Id = Guid.NewGuid()
                };
                contact.Attributes["originatingleadid"] = req.LeadId;
                orgService.Create(contact);
                response.CreatedEntities.Add(contact.ToEntityReference());
            }

            // Create Opportunity
            if (req.CreateOpportunity)
            {
                var opportunity = new Entity("opportunity")
                {
                    Id = Guid.NewGuid()
                };

                // Set OpportunityCurrencyId if given
                // MSDN link:
                // https://msdn.microsoft.com/en-us/library/microsoft.crm.sdk.messages.qualifyleadrequest.opportunitycurrencyid.aspx
                if (req.OpportunityCurrencyId != null)
                {
                    opportunity.Attributes["transactioncurrencyid"] = req.OpportunityCurrencyId;
                }

                // Associate Account or Contact with Opportunity
                // MSDN link:
                // https://msdn.microsoft.com/en-us/library/microsoft.crm.sdk.messages.qualifyleadrequest.opportunitycustomerid.aspx
                if (req.OpportunityCustomerId != null)
                {
                    var logicalName = req.OpportunityCustomerId.LogicalName;

                    // Associate Account or Contact
                    if (logicalName.Equals("account") || logicalName.Equals("contact"))
                    {
                        opportunity.Attributes["customerid"] = req.OpportunityCustomerId;
                    }
                    // Wrong Entity was given as parameter
                    else
                    {
                        throw new Exception(string.Format("Opportunity Customer Id should be connected with Account or Contact. Instead OpportunityCustomerId was given with Entity.LogicalName = {0}", logicalName));
                    }
                }

                opportunity.Attributes["originatingleadid"] = req.LeadId;
                orgService.Create(opportunity);
                response.CreatedEntities.Add(opportunity.ToEntityReference());
            }

            // Actual Lead
            var lead = leads.First();
            lead.Attributes["statuscode"] = new OptionSetValue(req.Status.Value);
            orgService.Update(lead);

            return response;
        }

        /// <summary>
        /// Gets the type of organization request that this executor is responsible for handling.
        /// </summary>
        /// <returns>The <see cref="Type"/> of <see cref="QualifyLeadRequest"/>.</returns>
        public Type GetResponsibleRequestType()
        {
            return typeof(QualifyLeadRequest);
        }
    }
}