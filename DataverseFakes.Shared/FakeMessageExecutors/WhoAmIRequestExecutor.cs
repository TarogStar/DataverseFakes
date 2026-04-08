using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using System;
using System.Linq;

namespace FakeXrmEasy.FakeMessageExecutors
{
    /// <summary>
    /// Fake message executor that handles <see cref="WhoAmIRequest"/> messages.
    /// Returns information about the currently authenticated user including UserId, BusinessUnitId, and OrganizationId.
    /// </summary>
    public class WhoAmIRequestExecutor : IFakeMessageExecutor
    {
        /// <summary>
        /// Determines whether this executor can handle the specified organization request.
        /// </summary>
        /// <param name="request">The organization request to evaluate.</param>
        /// <returns><c>true</c> if the request is a <see cref="WhoAmIRequest"/>; otherwise, <c>false</c>.</returns>
        public bool CanExecute(OrganizationRequest request)
        {
            return request is WhoAmIRequest;
        }

        /// <summary>
        /// Executes the <see cref="WhoAmIRequest"/> and returns information about the current user.
        /// </summary>
        /// <param name="request">The organization request to execute. Must be a <see cref="WhoAmIRequest"/>.</param>
        /// <param name="ctx">The faked XRM context containing the caller identity and related user/business unit data.</param>
        /// <returns>
        /// A <see cref="WhoAmIResponse"/> containing:
        /// <list type="bullet">
        /// <item><description>UserId - The GUID of the current user (from <see cref="XrmFakedContext.CallerId"/>)</description></item>
        /// <item><description>BusinessUnitId - The GUID of the user's business unit (if the systemuser entity is initialized)</description></item>
        /// <item><description>OrganizationId - The GUID of the organization (if available from user or business unit data)</description></item>
        /// </list>
        /// </returns>
        public OrganizationResponse Execute(OrganizationRequest request, XrmFakedContext ctx)
        {
            var req = request as WhoAmIRequest;

            var callerId = ctx.CallerId.Id;

            var results = new ParameterCollection {
              { "UserId", callerId }
            };

            var user = ctx.CreateQuery("systemuser")
                          .Where(u => u.Id == callerId)
                          .SingleOrDefault();

            if(user != null) {
              var buId = GetBusinessUnitId(user);
              results.Add("BusinessUnitId", buId);

              var orgId = GetOrganizationId(ctx, user, buId);
              results.Add("OrganizationId", orgId);
            }

            var response = new WhoAmIResponse
            {
                Results = results
            };
            return response;
        }

        /// <summary>
        /// Gets the type of organization request that this executor is responsible for handling.
        /// </summary>
        /// <returns>The <see cref="Type"/> of <see cref="WhoAmIRequest"/>.</returns>
        public Type GetResponsibleRequestType()
        {
            return typeof(WhoAmIRequest);
        }

        /// <summary>
        /// Extracts the business unit ID from the systemuser entity.
        /// </summary>
        /// <param name="user">The systemuser entity to extract the business unit ID from.</param>
        /// <returns>The GUID of the user's business unit, or <see cref="Guid.Empty"/> if not set.</returns>
        private static Guid GetBusinessUnitId(Entity user) {
          var buRef = user.GetAttributeValue<EntityReference>("businessunitid");
          var buId = buRef != null ? buRef.Id : Guid.Empty;
          return buId;
        }

        /// <summary>
        /// Gets the organization ID from the user entity or by looking up the business unit.
        /// </summary>
        /// <param name="ctx">The faked XRM context to query for business unit data.</param>
        /// <param name="user">The systemuser entity to check for organizationid attribute.</param>
        /// <param name="buId">The business unit ID to use for fallback lookup.</param>
        /// <returns>
        /// The organization ID from the user entity if present, otherwise the organization ID
        /// from the business unit, or <see cref="Guid.Empty"/> if not found.
        /// </returns>
        private static Guid GetOrganizationId(XrmFakedContext ctx, Entity user, Guid buId) {
          var orgId = user.GetAttributeValue<Guid?>("organizationid") ?? Guid.Empty;
          if(orgId == Guid.Empty) {
            var bu = ctx.CreateQuery("businessunit")
                        .Where(b => b.Id == buId)
                        .SingleOrDefault();
            var orgRef = bu.GetAttributeValue<EntityReference>("organizationid");
            orgId = orgRef?.Id ?? Guid.Empty;
          }

          return orgId;
        }

    }
}