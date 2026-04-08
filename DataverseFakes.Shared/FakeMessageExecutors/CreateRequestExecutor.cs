using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using System;

namespace FakeXrmEasy.FakeMessageExecutors
{
    /// <summary>
    /// Handles the execution of <see cref="CreateRequest"/> messages in the faked CRM context.
    /// This executor simulates the creation of entity records in Dynamics 365/CRM.
    /// </summary>
    /// <remarks>
    /// The executor creates a new entity record in the in-memory context and returns
    /// a <see cref="CreateResponse"/> containing the unique identifier (GUID) of the newly created record.
    /// </remarks>
    public class CreateRequestExecutor : IFakeMessageExecutor
    {
        /// <summary>
        /// Determines whether this executor can handle the specified organization request.
        /// </summary>
        /// <param name="request">The <see cref="OrganizationRequest"/> to evaluate.</param>
        /// <returns>
        /// <c>true</c> if the request is a <see cref="CreateRequest"/>; otherwise, <c>false</c>.
        /// </returns>
        public bool CanExecute(OrganizationRequest request)
        {
            return request is CreateRequest;
        }

        /// <summary>
        /// Executes the create operation, adding a new entity record to the faked CRM context.
        /// </summary>
        /// <param name="request">The <see cref="CreateRequest"/> containing the entity to create.</param>
        /// <param name="ctx">The <see cref="XrmFakedContext"/> that provides the in-memory CRM simulation.</param>
        /// <returns>
        /// A <see cref="CreateResponse"/> containing the unique identifier (GUID) of the newly created entity
        /// in the Results collection under the "id" key.
        /// </returns>
        /// <remarks>
        /// The Target property of the <see cref="CreateRequest"/> must contain the entity to be created.
        /// If the entity does not have an Id specified, one will be automatically generated.
        /// </remarks>
        public OrganizationResponse Execute(OrganizationRequest request, XrmFakedContext ctx)
        {
            var createRequest = (CreateRequest)request;

            var service = ctx.GetOrganizationService();

            var guid = service.Create(createRequest.Target);

            return new CreateResponse()
            {
                ResponseName = "Create",
                Results = new ParameterCollection { { "id", guid } }
            };
        }

        /// <summary>
        /// Gets the type of organization request that this executor handles.
        /// </summary>
        /// <returns>
        /// The <see cref="Type"/> of <see cref="CreateRequest"/>.
        /// </returns>
        public Type GetResponsibleRequestType()
        {
            return typeof(CreateRequest);
        }
    }
}
