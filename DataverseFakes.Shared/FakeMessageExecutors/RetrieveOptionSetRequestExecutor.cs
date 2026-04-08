using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using System;

namespace FakeXrmEasy.FakeMessageExecutors
{
    /// <summary>
    /// Fake message executor that handles <see cref="RetrieveOptionSetRequest"/> messages.
    /// Retrieves global OptionSet metadata from the faked CRM context's metadata cache.
    /// </summary>
    public class RetrieveOptionSetRequestExecutor : IFakeMessageExecutor
    {
        /// <summary>
        /// Determines whether this executor can handle the specified organization request.
        /// </summary>
        /// <param name="request">The organization request to evaluate.</param>
        /// <returns><c>true</c> if the request is a <see cref="RetrieveOptionSetRequest"/>; otherwise, <c>false</c>.</returns>
        public bool CanExecute(OrganizationRequest request)
        {
            return request is RetrieveOptionSetRequest;
        }

        /// <summary>
        /// Executes the <see cref="RetrieveOptionSetRequest"/> and returns the corresponding OptionSet metadata.
        /// </summary>
        /// <param name="request">The organization request to execute. Must be a <see cref="RetrieveOptionSetRequest"/>.</param>
        /// <param name="ctx">The faked XRM context containing the OptionSet metadata cache.</param>
        /// <returns>
        /// A <see cref="RetrieveOptionSetResponse"/> containing the <see cref="Microsoft.Xrm.Sdk.Metadata.OptionSetMetadata"/>
        /// for the requested global OptionSet in the Results collection.
        /// </returns>
        /// <remarks>
        /// The OptionSet can be retrieved by Name. Retrieval by MetadataId is not currently supported.
        /// Use <see cref="XrmFakedContext.OptionSetValuesMetadata"/> to initialize OptionSet metadata before executing this request.
        /// </remarks>
        public OrganizationResponse Execute(OrganizationRequest request, XrmFakedContext ctx)
        {
            var retrieveOptionSetRequest = (RetrieveOptionSetRequest)request;

            if (retrieveOptionSetRequest.MetadataId != Guid.Empty) //ToDo: Implement retrieving option sets by Id
            {
                FakeOrganizationServiceFault.Throw(ErrorCodes.ObjectDoesNotExist, $"Could not find optionset with optionset id: {retrieveOptionSetRequest.MetadataId}");
            }

            var name = retrieveOptionSetRequest.Name;

            if (string.IsNullOrEmpty(name))
            {
                FakeOrganizationServiceFault.Throw(ErrorCodes.InvalidArgument, "Name is required when optionSet id is not specified");
            }

            if (!ctx.OptionSetValuesMetadata.ContainsKey(name))
            {
                FakeOrganizationServiceFault.Throw(ErrorCodes.ObjectDoesNotExist, string.Format("An OptionSetMetadata with the name {0} does not exist.", name));
            }

            var optionSetMetadata = ctx.OptionSetValuesMetadata[name];

            var response = new RetrieveOptionSetResponse()
            {
                Results = new ParameterCollection
                        {
                            { "OptionSetMetadata", optionSetMetadata }
                        }
            };

            return response;
        }

        /// <summary>
        /// Gets the type of organization request that this executor is responsible for handling.
        /// </summary>
        /// <returns>The <see cref="Type"/> of <see cref="RetrieveOptionSetRequest"/>.</returns>
        public Type GetResponsibleRequestType()
        {
            return typeof(RetrieveOptionSetRequest);
        }
    }
}