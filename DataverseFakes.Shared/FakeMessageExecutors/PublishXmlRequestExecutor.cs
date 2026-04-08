using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using System;

namespace FakeXrmEasy.FakeMessageExecutors
{
    /// <summary>
    /// Implements a fake message executor for the CRM PublishXmlRequest message.
    /// This executor simulates publishing customization changes to specific solution components
    /// in the Dynamics 365 / Power Platform environment.
    /// </summary>
    public class PublishXmlRequestExecutor : IFakeMessageExecutor
    {
        /// <summary>
        /// Determines whether this executor can handle the specified organization request.
        /// </summary>
        /// <param name="request">The organization request to evaluate.</param>
        /// <returns><c>true</c> if the request is a <see cref="PublishXmlRequest"/>; otherwise, <c>false</c>.</returns>
        public bool CanExecute(OrganizationRequest request)
        {
            return request is PublishXmlRequest;
        }

        /// <summary>
        /// Executes the PublishXmlRequest against the faked CRM context.
        /// This method validates that the ParameterXml property is provided and returns a successful response.
        /// In a real CRM environment, this would publish specific customization changes defined in the XML.
        /// </summary>
        /// <param name="request">The organization request to execute. Must be a <see cref="PublishXmlRequest"/>.</param>
        /// <param name="ctx">The faked XRM context that simulates the CRM environment.</param>
        /// <returns>A <see cref="PublishXmlResponse"/> indicating successful execution of the publish operation.</returns>
        /// <exception cref="Exception">Thrown when the ParameterXml property is null, empty, or whitespace.</exception>
        public OrganizationResponse Execute(OrganizationRequest request, XrmFakedContext ctx)
        {
            var req = request as PublishXmlRequest;

            if (string.IsNullOrWhiteSpace(req.ParameterXml))
            {
                throw new Exception(string.Format("ParameterXml property must not be blank."));
            }
            return new PublishXmlResponse()
            {
            };
        }

        /// <summary>
        /// Gets the type of organization request that this executor is responsible for handling.
        /// </summary>
        /// <returns>The <see cref="Type"/> of <see cref="PublishXmlRequest"/>.</returns>
        public Type GetResponsibleRequestType()
        {
            return typeof(PublishXmlRequest);
        }
    }
}