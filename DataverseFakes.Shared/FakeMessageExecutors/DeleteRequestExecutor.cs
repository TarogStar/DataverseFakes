using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using System;
using System.ServiceModel;

namespace FakeXrmEasy.FakeMessageExecutors
{
    /// <summary>
    /// Handles the execution of <see cref="DeleteRequest"/> messages in the faked CRM context.
    /// This executor simulates deleting entity records from Dynamics 365/CRM.
    /// </summary>
    /// <remarks>
    /// The executor removes an existing entity record from the in-memory context.
    /// If the Target is null, a <see cref="FaultException{OrganizationServiceFault}"/> is thrown.
    /// </remarks>
    public class DeleteRequestExecutor : IFakeMessageExecutor
    {
        /// <summary>
        /// Determines whether this executor can handle the specified organization request.
        /// </summary>
        /// <param name="request">The <see cref="OrganizationRequest"/> to evaluate.</param>
        /// <returns>
        /// <c>true</c> if the request is a <see cref="DeleteRequest"/>; otherwise, <c>false</c>.
        /// </returns>
        public bool CanExecute(OrganizationRequest request)
        {
            return request is DeleteRequest;
        }

        /// <summary>
        /// Executes the delete operation, removing an entity record from the faked CRM context.
        /// </summary>
        /// <param name="request">The <see cref="DeleteRequest"/> containing the target entity reference to delete.</param>
        /// <param name="ctx">The <see cref="XrmFakedContext"/> that provides the in-memory CRM simulation.</param>
        /// <returns>
        /// A <see cref="DeleteResponse"/> indicating successful completion of the delete operation.
        /// </returns>
        /// <exception cref="FaultException{OrganizationServiceFault}">
        /// Thrown when the Target property is null, indicating that a valid entity reference is required.
        /// </exception>
        /// <remarks>
        /// The Target property of the <see cref="DeleteRequest"/> must contain a valid <see cref="EntityReference"/>
        /// with the logical name and unique identifier of the entity to delete.
        /// </remarks>
        public OrganizationResponse Execute(OrganizationRequest request, XrmFakedContext ctx)
        {
            var deleteRequest = (DeleteRequest)request;

            var target = deleteRequest.Target;

            if (target == null)
            {
                throw new FaultException<OrganizationServiceFault>(new OrganizationServiceFault(), "Can not delete without target");
            }

            var targetId = ctx.GetRecordUniqueId(target);

            var service = ctx.GetOrganizationService();
            service.Delete(target.LogicalName, targetId);

            return new DeleteResponse();
        }

        /// <summary>
        /// Gets the type of organization request that this executor handles.
        /// </summary>
        /// <returns>
        /// The <see cref="Type"/> of <see cref="DeleteRequest"/>.
        /// </returns>
        public Type GetResponsibleRequestType()
        {
            return typeof(DeleteRequest);
        }
    }
}
