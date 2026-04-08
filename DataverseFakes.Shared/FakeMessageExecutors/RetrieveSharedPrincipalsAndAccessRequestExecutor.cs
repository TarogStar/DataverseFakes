using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using System;

namespace FakeXrmEasy.FakeMessageExecutors
{
    /// <summary>
    /// Fake message executor that handles <see cref="RetrieveSharedPrincipalsAndAccessRequest"/> messages.
    /// This executor simulates the CRM Retrieve Shared Principals And Access operation, which retrieves
    /// all security principals (users and teams) that have been granted access to a specific entity record,
    /// along with their respective access rights.
    /// </summary>
    public class RetrieveSharedPrincipalsAndAccessRequestExecutor : IFakeMessageExecutor
    {
        /// <summary>
        /// Determines whether this executor can handle the specified organization request.
        /// </summary>
        /// <param name="request">The organization request to evaluate.</param>
        /// <returns>
        /// <c>true</c> if the request is a <see cref="RetrieveSharedPrincipalsAndAccessRequest"/>; otherwise, <c>false</c>.
        /// </returns>
        public bool CanExecute(OrganizationRequest request)
        {
            return request is RetrieveSharedPrincipalsAndAccessRequest;
        }

        /// <summary>
        /// Executes the <see cref="RetrieveSharedPrincipalsAndAccessRequest"/> by retrieving all
        /// security principals that have been shared access to a target entity record.
        /// </summary>
        /// <param name="request">The <see cref="RetrieveSharedPrincipalsAndAccessRequest"/> containing
        /// the target entity reference for which shared principals and their access rights should be retrieved.</param>
        /// <param name="ctx">The <see cref="XrmFakedContext"/> that maintains the in-memory CRM state
        /// and provides access to the access rights repository.</param>
        /// <returns>
        /// A <see cref="RetrieveSharedPrincipalsAndAccessResponse"/> containing a collection of
        /// <see cref="PrincipalAccess"/> objects, each representing a principal and their access rights
        /// to the target record.
        /// </returns>
        public OrganizationResponse Execute(OrganizationRequest request, XrmFakedContext ctx)
        {
            RetrieveSharedPrincipalsAndAccessRequest req = (RetrieveSharedPrincipalsAndAccessRequest)request;
            return ctx.AccessRightsRepository.RetrieveSharedPrincipalsAndAccess(req.Target);
        }

        /// <summary>
        /// Gets the type of organization request that this executor is responsible for handling.
        /// </summary>
        /// <returns>
        /// The <see cref="Type"/> of <see cref="RetrieveSharedPrincipalsAndAccessRequest"/>.
        /// </returns>
        public Type GetResponsibleRequestType()
        {
            return typeof(RetrieveSharedPrincipalsAndAccessRequest);
        }
    }
}
