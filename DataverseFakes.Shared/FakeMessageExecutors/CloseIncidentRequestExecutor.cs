using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.ServiceModel;

namespace FakeXrmEasy.FakeMessageExecutors
{
    /// <summary>
    /// Implements a fake message executor for the CloseIncidentRequest CRM message.
    /// This executor handles closing incident (case) records in the faked CRM context
    /// by creating an incident resolution entity and setting the incident state to resolved.
    /// </summary>
    public class CloseIncidentRequestExecutor : IFakeMessageExecutor
    {
        private const string AttributeIncidentId = "incidentid";
        private const string AttributeSubject = "subject";
        private const string IncidentLogicalName = "incident";
        private const string IncidentResolutionLogicalName = "incidentresolution";
        private const int StateResolved = 1;

        /// <summary>
        /// Determines whether this executor can handle the specified organization request.
        /// </summary>
        /// <param name="request">The organization request to evaluate.</param>
        /// <returns>True if the request is a CloseIncidentRequest; otherwise, false.</returns>
        public bool CanExecute(OrganizationRequest request)
        {
            return request is CloseIncidentRequest;
        }

        /// <summary>
        /// Executes the CloseIncidentRequest, closing an incident record in the faked CRM context.
        /// This method validates the incident resolution and status, creates an incident resolution
        /// activity record, and sets the incident state to resolved.
        /// </summary>
        /// <param name="request">The CloseIncidentRequest containing the incident resolution and status information.</param>
        /// <param name="ctx">The faked XRM context containing the in-memory CRM data.</param>
        /// <returns>A CloseIncidentResponse indicating successful completion of the close incident operation.</returns>
        /// <exception cref="FaultException{OrganizationServiceFault}">
        /// Thrown when the incident resolution is null, the status is null, or the incident cannot be found.
        /// </exception>
        public OrganizationResponse Execute(OrganizationRequest request, XrmFakedContext ctx)
        {
            var service = ctx.GetOrganizationService();
            var closeIncidentRequest = (CloseIncidentRequest)request;

            var incidentResolution = closeIncidentRequest.IncidentResolution;
            if (incidentResolution == null)
            {
                throw new FaultException<OrganizationServiceFault>(new OrganizationServiceFault(), "Cannot close incident without incident resolution.");
            }

            var status = closeIncidentRequest.Status;
            if (status == null)
            {
                throw new FaultException<OrganizationServiceFault>(new OrganizationServiceFault(), "Cannot close incident without status.");
            }

            var incidentId = (EntityReference)incidentResolution[AttributeIncidentId];
            ConcurrentDictionary<Guid, Entity> incidentDict;
            if (ctx.Data.TryGetValue(IncidentLogicalName, out incidentDict) &&
                incidentDict.Values.SingleOrDefault(p => p.Id == incidentId.Id) == null)
            {
                throw new FaultException<OrganizationServiceFault>(new OrganizationServiceFault(),
                    string.Format("Incident with id {0} not found.", incidentId.Id));
            }

            var newIncidentResolution = new Entity
            {
                LogicalName = IncidentResolutionLogicalName,
                Attributes = new AttributeCollection
                {
                    { "description", incidentResolution[AttributeSubject] },
                    { AttributeSubject, incidentResolution[AttributeSubject] },
                    { AttributeIncidentId, incidentId }
                }
            };
            service.Create(newIncidentResolution);

            var setState = new SetStateRequest
            {
                EntityMoniker = incidentId,
                Status = status,
                State = new OptionSetValue(StateResolved)
            };

            service.Execute(setState);

            return new CloseIncidentResponse();
        }

        /// <summary>
        /// Gets the type of CRM request that this executor is responsible for handling.
        /// </summary>
        /// <returns>The Type of CloseIncidentResponse, indicating this executor handles CloseIncidentRequest messages.</returns>
        public Type GetResponsibleRequestType()
        {
            return typeof(CloseIncidentRequest);
        }
    }
}