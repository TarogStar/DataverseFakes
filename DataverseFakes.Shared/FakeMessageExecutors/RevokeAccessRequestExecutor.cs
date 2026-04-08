using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using System;

namespace FakeXrmEasy.FakeMessageExecutors
{
    /// <summary>
    /// Fake message executor that handles <see cref="RevokeAccessRequest"/> messages.
    /// This executor simulates the CRM Revoke Access operation, which removes all access rights
    /// that a security principal (user or team) has to an entity record.
    /// </summary>
    public class RevokeAccessRequestExecutor : IFakeMessageExecutor
    {
        /// <summary>
        /// Determines whether this executor can handle the specified organization request.
        /// </summary>
        /// <param name="request">The organization request to evaluate.</param>
        /// <returns>
        /// <c>true</c> if the request is a <see cref="RevokeAccessRequest"/>; otherwise, <c>false</c>.
        /// </returns>
        public bool CanExecute(OrganizationRequest request)
        {
            return request is RevokeAccessRequest;
        }

        /// <summary>
        /// Executes the <see cref="RevokeAccessRequest"/> by revoking all access rights
        /// from a security principal for a target entity record.
        /// </summary>
        /// <param name="request">The <see cref="RevokeAccessRequest"/> containing the target entity reference
        /// and the revokee (the security principal whose access rights are being revoked).</param>
        /// <param name="ctx">The <see cref="XrmFakedContext"/> that maintains the in-memory CRM state
        /// and provides access to the access rights repository.</param>
        /// <returns>
        /// A <see cref="RevokeAccessResponse"/> indicating the operation completed successfully.
        /// </returns>
        public OrganizationResponse Execute(OrganizationRequest request, XrmFakedContext ctx)
        {
            RevokeAccessRequest req = (RevokeAccessRequest)request;
            ctx.AccessRightsRepository.RevokeAccessTo(req.Target, req.Revokee);
            return new RevokeAccessResponse();
        }

        /// <summary>
        /// Gets the type of organization request that this executor is responsible for handling.
        /// </summary>
        /// <returns>
        /// The <see cref="Type"/> of <see cref="RevokeAccessRequest"/>.
        /// </returns>
        public Type GetResponsibleRequestType()
        {
            return typeof(RevokeAccessRequest);
        }
    }
}
