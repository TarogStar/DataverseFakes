using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using System;

namespace FakeXrmEasy.FakeMessageExecutors
{
    /// <summary>
    /// Fake message executor that handles SetStateRequest messages for changing the state and status of CRM entities.
    /// Translates the SetStateRequest into an update operation that modifies the statecode and statuscode attributes.
    /// </summary>
    public class SetStateRequestExecutor : IFakeMessageExecutor
    {
        /// <summary>
        /// Determines whether this executor can handle the specified organization request.
        /// </summary>
        /// <param name="request">The organization request to evaluate.</param>
        /// <returns>True if the request is a SetStateRequest; otherwise, false.</returns>
        public bool CanExecute(OrganizationRequest request)
        {
            return request is SetStateRequest;
        }

        /// <summary>
        /// Executes the SetStateRequest to change the state and status of a CRM entity.
        /// Internally translates this to an update operation on the statecode and statuscode attributes.
        /// </summary>
        /// <param name="request">The SetStateRequest containing the entity reference (EntityMoniker), State, and Status values.</param>
        /// <param name="ctx">The XrmFakedContext providing the in-memory CRM context and organization service.</param>
        /// <returns>A SetStateResponse indicating successful completion of the state change.</returns>
        public OrganizationResponse Execute(OrganizationRequest request, XrmFakedContext ctx)
        {
            var req = request as SetStateRequest;

            //We are going to translate a SetStateRequest into an update message basically

            var entityName = req.EntityMoniker.LogicalName;
            var guid = req.EntityMoniker.Id;

            var entityToUpdate = new Entity(entityName) { Id = guid };
            entityToUpdate["statecode"] = req.State;
            entityToUpdate["statuscode"] = req.Status;

            // Dataverse maintains system-managed fields for certain entities.
            // For systemuser, isdisabled is kept in sync with the active/inactive state.
            if (string.Equals(entityName, "systemuser", StringComparison.OrdinalIgnoreCase) && req.State != null)
            {
                entityToUpdate["isdisabled"] = req.State.Value != 0;
            }

            var fakedService = ctx.GetOrganizationService();
            fakedService.Update(entityToUpdate);

            return new SetStateResponse();
        }

        /// <summary>
        /// Gets the type of organization request that this executor is responsible for handling.
        /// </summary>
        /// <returns>The Type of SetStateRequest.</returns>
        public Type GetResponsibleRequestType()
        {
            return typeof(SetStateRequest);
        }
    }
}