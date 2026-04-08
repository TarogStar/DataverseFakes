using System;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;

namespace FakeXrmEasy.FakeMessageExecutors
{
    /// <summary>
    /// Fake message executor that handles <see cref="SendEmailRequest"/> messages.
    /// Simulates sending an email by updating the email entity's state to Completed and status to Sent.
    /// </summary>
    public class SendEmailRequestExecutor : IFakeMessageExecutor
    {
        /// <summary>
        /// Determines whether this executor can handle the specified organization request.
        /// </summary>
        /// <param name="request">The organization request to evaluate.</param>
        /// <returns><c>true</c> if the request is a <see cref="SendEmailRequest"/>; otherwise, <c>false</c>.</returns>
        public bool CanExecute(OrganizationRequest request)
        {
            return request is SendEmailRequest;
        }

        /// <summary>
        /// Executes the <see cref="SendEmailRequest"/> by updating the email entity status to simulate sending.
        /// </summary>
        /// <param name="request">The organization request to execute. Must be a <see cref="SendEmailRequest"/>.</param>
        /// <param name="ctx">The faked XRM context containing the email entity to update.</param>
        /// <returns>
        /// A <see cref="SendEmailResponse"/> after updating the email entity with:
        /// <list type="bullet">
        /// <item><description>statecode = 1 (Completed)</description></item>
        /// <item><description>statuscode = 3 (Sent)</description></item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// This executor does not actually send any email. It only updates the email entity's
        /// state and status fields to simulate a successful send operation.
        /// </remarks>
        public OrganizationResponse Execute(OrganizationRequest request, XrmFakedContext ctx)
        {
            var req = request as SendEmailRequest;
#if FAKE_XRM_EASY || FAKE_XRM_EASY_2013
            var entity = new Entity("email");
            entity.Id = req.EmailId;
#else
            var entity = new Entity("email", req.EmailId);
#endif
            entity["statecode"] = new OptionSetValue(1); //Completed
            entity["statuscode"] = new OptionSetValue(3); //Sent
            ctx.GetOrganizationService().Update(entity);
            return new SendEmailResponse();
        }

        /// <summary>
        /// Gets the type of organization request that this executor is responsible for handling.
        /// </summary>
        /// <returns>The <see cref="Type"/> of <see cref="SendEmailRequest"/>.</returns>
        public Type GetResponsibleRequestType()
        {
            return typeof(SendEmailRequest);
        }
    }
}
