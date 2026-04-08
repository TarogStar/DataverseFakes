#if FAKE_XRM_EASY_2013 || FAKE_XRM_EASY_2015 || FAKE_XRM_EASY_2016 || FAKE_XRM_EASY_365 || FAKE_XRM_EASY_9

using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Linq;
using System.ServiceModel;

namespace FakeXrmEasy.FakeMessageExecutors
{
    /// <summary>
    /// Implements a fake message executor for the CRM PickFromQueueRequest message.
    /// This executor simulates picking (claiming) a queue item and assigning it to a worker (system user)
    /// in the Dynamics 365 / Power Platform environment.
    /// </summary>
    public class PickFromQueueRequestExecutor : IFakeMessageExecutor
    {
        /// <summary>
        /// Determines whether this executor can handle the specified organization request.
        /// </summary>
        /// <param name="request">The organization request to evaluate.</param>
        /// <returns><c>true</c> if the request is a <see cref="PickFromQueueRequest"/>; otherwise, <c>false</c>.</returns>
        public bool CanExecute(OrganizationRequest request)
        {
            return request is PickFromQueueRequest;
        }

        /// <summary>
        /// Executes the PickFromQueueRequest against the faked CRM context.
        /// This method assigns the specified queue item to a worker (system user). If the RemoveQueueItem
        /// property is set to true, the queue item is deleted instead of being assigned.
        /// </summary>
        /// <param name="request">The organization request to execute. Must be a <see cref="PickFromQueueRequest"/>.</param>
        /// <param name="ctx">The faked XRM context that simulates the CRM environment.</param>
        /// <returns>A <see cref="PickFromQueueResponse"/> indicating successful execution of the pick operation.</returns>
        /// <exception cref="FaultException{OrganizationServiceFault}">
        /// Thrown when the QueueItemId or WorkerId is an empty GUID, when the specified worker does not exist,
        /// or when the specified queue item does not exist.
        /// </exception>
        public OrganizationResponse Execute(OrganizationRequest request, XrmFakedContext ctx)
        {
            var pickFromQueueRequest = (PickFromQueueRequest)request;

            var queueItemId = pickFromQueueRequest.QueueItemId;
            var workerid = pickFromQueueRequest.WorkerId;

            if ((queueItemId == Guid.Empty) || (workerid == Guid.Empty))
            {
                throw new FaultException<OrganizationServiceFault>(new OrganizationServiceFault(), "Expected non-empty Guid.");
            }

            var service = ctx.GetOrganizationService();

            var query = new QueryByAttribute("systemuser");
            query.Attributes.Add("systemuserid");
            query.Values.Add(workerid);

            var worker = service.RetrieveMultiple(query).Entities.FirstOrDefault();
            if (worker == null)
            {
                throw new FaultException<OrganizationServiceFault>(new OrganizationServiceFault(), string.Format("Invalid workerid: {0} of type 8", workerid));
            }

            query = new QueryByAttribute("queueitem");
            query.Attributes.Add("queueitemid");
            query.Values.Add(queueItemId);

            var queueItem = service.RetrieveMultiple(query).Entities.FirstOrDefault();
            if (queueItem == null)
            {
                throw new FaultException<OrganizationServiceFault>(new OrganizationServiceFault(), string.Format("queueitem With Id = {0} Does Not Exist", queueItemId));
            }

            if (pickFromQueueRequest.RemoveQueueItem)
            {
                service.Delete("queueitem", queueItemId);
            }
            else
            {
                var pickUpdateEntity = new Entity
                {
                    LogicalName = "queueitem",
                    Id = queueItem.Id,
                    Attributes = new AttributeCollection
                    {
                        { "workerid", worker.ToEntityReference() },
                        { "workeridmodifiedon", DateTime.Now },
                    }
                };

                service.Update(pickUpdateEntity);
            }

            return new PickFromQueueResponse();
        }

        /// <summary>
        /// Gets the type of organization request that this executor is responsible for handling.
        /// </summary>
        /// <returns>The <see cref="Type"/> of <see cref="PickFromQueueRequest"/>.</returns>
        public Type GetResponsibleRequestType()
        {
            return typeof(PickFromQueueRequest);
        }
    }
}

#endif