using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using System;
using System.Linq;

namespace FakeXrmEasy.FakeMessageExecutors
{
    /// <summary>
    /// Fake message executor that handles <see cref="WinOpportunityRequest"/> messages.
    /// This executor simulates the CRM Win Opportunity operation, which closes an opportunity
    /// as won and updates its status code to reflect the successful outcome.
    /// </summary>
    public class WinOpportunityRequestExecutor : IFakeMessageExecutor
    {
        /// <summary>
        /// Determines whether this executor can handle the specified organization request.
        /// </summary>
        /// <param name="request">The organization request to evaluate.</param>
        /// <returns>
        /// <c>true</c> if the request is a <see cref="WinOpportunityRequest"/>; otherwise, <c>false</c>.
        /// </returns>
        public bool CanExecute(OrganizationRequest request)
        {
            return request is WinOpportunityRequest;
        }

        /// <summary>
        /// Executes the <see cref="WinOpportunityRequest"/> by closing the specified opportunity as won.
        /// The method retrieves the opportunity from the in-memory context using the opportunity ID
        /// from the OpportunityClose entity, then updates the opportunity's status code to the specified value.
        /// </summary>
        /// <param name="request">The <see cref="WinOpportunityRequest"/> containing:
        /// <list type="bullet">
        /// <item><description>OpportunityClose - An entity containing the opportunityid attribute referencing the opportunity to close.</description></item>
        /// <item><description>Status - An OptionSetValue representing the status reason for winning the opportunity.</description></item>
        /// </list>
        /// </param>
        /// <param name="ctx">The <see cref="XrmFakedContext"/> that maintains the in-memory CRM state
        /// containing the opportunity record to be updated.</param>
        /// <returns>
        /// A <see cref="WinOpportunityResponse"/> indicating the operation completed successfully.
        /// </returns>
        /// <exception cref="Exception">
        /// Thrown when:
        /// <list type="bullet">
        /// <item><description>OpportunityClose or Status is null.</description></item>
        /// <item><description>No opportunity is found with the specified ID.</description></item>
        /// <item><description>More than one opportunity is found with the specified ID (data integrity error).</description></item>
        /// </list>
        /// </exception>
        public OrganizationResponse Execute(OrganizationRequest request, XrmFakedContext ctx)
        {
            var req = request as WinOpportunityRequest;

            // Check if OpportunityClose and Status were passed to request
            if (req.OpportunityClose != null &&
                req.Status != null)
            {
                // WinOpportunityRequest.OpportunityClose.OpportunityId
                var opportunityReference = req.OpportunityClose.GetAttributeValue<EntityReference>("opportunityid");
                var opportunityId = opportunityReference.Id;

                // Get Opportunities (in good scenario, should return 1 record)
                var opportunities = (from op in ctx.CreateQuery("opportunity")
                                   where op.Id == opportunityId
                                   select op);

                // More than one if to check and give better feedback to user
                if (opportunities.Count() < 1) throw new Exception(string.Format("No Opportunity found with Id = {0}", opportunityId));
                else if (opportunities.Count() > 1) throw new Exception(string.Format("More than one Opportunity found with Id = {0}", opportunityId));
                else
                {
                    var opportunity = opportunities.FirstOrDefault();
                    opportunity.Attributes["statuscode"] = req.Status;

                    ctx.GetOrganizationService().Update(opportunity);

                    return new WinOpportunityResponse();
                }
            }
            else
            {
                throw new Exception("OpportunityClose or Status was not passed to request.");
            }
        }

        /// <summary>
        /// Gets the type of organization request that this executor is responsible for handling.
        /// </summary>
        /// <returns>
        /// The <see cref="Type"/> of <see cref="WinOpportunityRequest"/>.
        /// </returns>
        public Type GetResponsibleRequestType()
        {
            return typeof(WinOpportunityRequest);
        }
    }
}
