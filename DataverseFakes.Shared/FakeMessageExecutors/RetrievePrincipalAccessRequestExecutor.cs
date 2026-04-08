using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using System;

namespace FakeXrmEasy.FakeMessageExecutors
{
    /// <summary>
    /// Fake message executor that handles <see cref="RetrievePrincipalAccessRequest"/> messages.
    /// This executor simulates the CRM Retrieve Principal Access operation, which retrieves
    /// the access rights that a specific security principal (user or team) has to an entity record.
    /// </summary>
    public class RetrievePrincipalAccessRequestExecutor : IFakeMessageExecutor
    {
        /// <summary>
        /// Determines whether this executor can handle the specified organization request.
        /// </summary>
        /// <param name="request">The organization request to evaluate.</param>
        /// <returns>
        /// <c>true</c> if the request is a <see cref="RetrievePrincipalAccessRequest"/>; otherwise, <c>false</c>.
        /// </returns>
        public bool CanExecute(OrganizationRequest request)
        {
            return request is RetrievePrincipalAccessRequest;
        }

        /// <summary>
        /// Executes the <see cref="RetrievePrincipalAccessRequest"/> by retrieving the access rights
        /// that a specific security principal has to a target entity record.
        /// </summary>
        /// <param name="request">The <see cref="RetrievePrincipalAccessRequest"/> containing the target entity reference
        /// and the principal (user or team) whose access rights should be retrieved.</param>
        /// <param name="ctx">The <see cref="XrmFakedContext"/> that maintains the in-memory CRM state
        /// and provides access to the access rights repository.</param>
        /// <returns>
        /// A <see cref="RetrievePrincipalAccessResponse"/> containing the access rights mask
        /// that the specified principal has to the target record.
        /// </returns>
        public OrganizationResponse Execute(OrganizationRequest request, XrmFakedContext ctx)
        {
            RetrievePrincipalAccessRequest req = (RetrievePrincipalAccessRequest)request;
            return ctx.AccessRightsRepository.RetrievePrincipalAccess(req.Target, req.Principal);
        }

        /// <summary>
        /// Gets the type of organization request that this executor is responsible for handling.
        /// </summary>
        /// <returns>
        /// The <see cref="Type"/> of <see cref="RetrievePrincipalAccessRequest"/>.
        /// </returns>
        public Type GetResponsibleRequestType()
        {
            return typeof(RetrievePrincipalAccessRequest);
        }
    }
}
