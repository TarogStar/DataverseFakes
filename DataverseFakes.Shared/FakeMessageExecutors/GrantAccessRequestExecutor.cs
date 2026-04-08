using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using System;

namespace FakeXrmEasy.FakeMessageExecutors
{
    /// <summary>
    /// Fake message executor that handles <see cref="GrantAccessRequest"/> messages.
    /// This executor simulates the CRM Grant Access operation, which grants a security principal
    /// (user or team) specific access rights to an entity record.
    /// </summary>
    public class GrantAccessRequestExecutor : IFakeMessageExecutor
    {
        /// <summary>
        /// Determines whether this executor can handle the specified organization request.
        /// </summary>
        /// <param name="request">The organization request to evaluate.</param>
        /// <returns>
        /// <c>true</c> if the request is a <see cref="GrantAccessRequest"/>; otherwise, <c>false</c>.
        /// </returns>
        public bool CanExecute(OrganizationRequest request)
        {
            return request is GrantAccessRequest;
        }

        /// <summary>
        /// Executes the <see cref="GrantAccessRequest"/> by granting the specified access rights
        /// to a security principal for a target entity record.
        /// </summary>
        /// <param name="request">The <see cref="GrantAccessRequest"/> containing the target entity reference
        /// and the principal access information (principal reference and access rights to grant).</param>
        /// <param name="ctx">The <see cref="XrmFakedContext"/> that maintains the in-memory CRM state
        /// and provides access to the access rights repository.</param>
        /// <returns>
        /// A <see cref="GrantAccessResponse"/> indicating the operation completed successfully.
        /// </returns>
        public OrganizationResponse Execute(OrganizationRequest request, XrmFakedContext ctx)
        {
            GrantAccessRequest req = (GrantAccessRequest)request;
            ctx.AccessRightsRepository.GrantAccessTo(req.Target, req.PrincipalAccess);
            return new GrantAccessResponse();
        }

        /// <summary>
        /// Gets the type of organization request that this executor is responsible for handling.
        /// </summary>
        /// <returns>
        /// The <see cref="Type"/> of <see cref="GrantAccessRequest"/>.
        /// </returns>
        public Type GetResponsibleRequestType()
        {
            return typeof(GrantAccessRequest);
        }
    }
}
