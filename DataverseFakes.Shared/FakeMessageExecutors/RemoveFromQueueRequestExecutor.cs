#if FAKE_XRM_EASY_2013 || FAKE_XRM_EASY_2015 || FAKE_XRM_EASY_2016 || FAKE_XRM_EASY_365 || FAKE_XRM_EASY_9

using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using System;
using System.ServiceModel;

namespace FakeXrmEasy.FakeMessageExecutors
{
    /// <summary>
    /// Implements a fake message executor for the CRM RemoveFromQueueRequest message.
    /// This executor simulates removing an item from a queue by deleting the queue item record
    /// in the Dynamics 365 / Power Platform environment.
    /// </summary>
    public class RemoveFromQueueRequestExecutor : IFakeMessageExecutor
    {
        /// <summary>
        /// Determines whether this executor can handle the specified organization request.
        /// </summary>
        /// <param name="request">The organization request to evaluate.</param>
        /// <returns><c>true</c> if the request is a <see cref="RemoveFromQueueRequest"/>; otherwise, <c>false</c>.</returns>
        public bool CanExecute(OrganizationRequest request)
        {
            return request is RemoveFromQueueRequest;
        }

        /// <summary>
        /// Executes the RemoveFromQueueRequest against the faked CRM context.
        /// This method deletes the specified queue item from the system.
        /// </summary>
        /// <param name="request">The organization request to execute. Must be a <see cref="RemoveFromQueueRequest"/>.</param>
        /// <param name="ctx">The faked XRM context that simulates the CRM environment.</param>
        /// <returns>A <see cref="RemoveFromQueueResponse"/> indicating successful removal of the queue item.</returns>
        /// <exception cref="FaultException{OrganizationServiceFault}">Thrown when the QueueItemId is an empty GUID.</exception>
        public OrganizationResponse Execute(OrganizationRequest request, XrmFakedContext ctx)
        {
            var removeFromQueueRequest = (RemoveFromQueueRequest)request;

            var queueItemId = removeFromQueueRequest.QueueItemId;
            if (queueItemId == Guid.Empty)
            {
                throw new FaultException<OrganizationServiceFault>(new OrganizationServiceFault(), "Cannot remove without queue item.");
            }

            var service = ctx.GetOrganizationService();
            service.Delete("queueitem", queueItemId);

            return new RemoveFromQueueResponse();
        }

        /// <summary>
        /// Gets the type of organization request that this executor is responsible for handling.
        /// </summary>
        /// <returns>The <see cref="Type"/> of <see cref="RemoveFromQueueRequest"/>.</returns>
        public Type GetResponsibleRequestType()
        {
            return typeof(RemoveFromQueueRequest);
        }
    }
}
#endif