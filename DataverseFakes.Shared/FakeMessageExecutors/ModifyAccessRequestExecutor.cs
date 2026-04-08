using System;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;

namespace FakeXrmEasy.FakeMessageExecutors
{
    /// <summary>
    /// Fake message executor that handles <see cref="ModifyAccessRequest"/> messages.
    /// This executor simulates the CRM Modify Access operation, which modifies the access rights
    /// that a security principal (user or team) has to an entity record.
    /// </summary>
    public class ModifyAccessRequestExecutor : IFakeMessageExecutor
    {
        /// <summary>
        /// Determines whether this executor can handle the specified organization request.
        /// </summary>
        /// <param name="request">The organization request to evaluate.</param>
        /// <returns>
        /// <c>true</c> if the request is a <see cref="ModifyAccessRequest"/>; otherwise, <c>false</c>.
        /// </returns>
        public bool CanExecute(OrganizationRequest request)
        {
            return request is ModifyAccessRequest;
        }

        /// <summary>
        /// Executes the <see cref="ModifyAccessRequest"/> by modifying the access rights
        /// that a security principal has to a target entity record.
        /// </summary>
        /// <param name="request">The <see cref="ModifyAccessRequest"/> containing the target entity reference
        /// and the principal access information (principal reference and updated access rights).</param>
        /// <param name="ctx">The <see cref="XrmFakedContext"/> that maintains the in-memory CRM state
        /// and provides access to the access rights repository.</param>
        /// <returns>
        /// A <see cref="ModifyAccessResponse"/> indicating the operation completed successfully.
        /// </returns>
        public OrganizationResponse Execute(OrganizationRequest request, XrmFakedContext ctx)
        {
            ModifyAccessRequest req = (ModifyAccessRequest)request;
            ctx.AccessRightsRepository.ModifyAccessOn(req.Target, req.PrincipalAccess);
            return new ModifyAccessResponse();
        }

        /// <summary>
        /// Gets the type of organization request that this executor is responsible for handling.
        /// </summary>
        /// <returns>
        /// The <see cref="Type"/> of <see cref="ModifyAccessRequest"/>.
        /// </returns>
        public Type GetResponsibleRequestType()
        {
            return typeof(ModifyAccessRequest);
        }
    }
}
