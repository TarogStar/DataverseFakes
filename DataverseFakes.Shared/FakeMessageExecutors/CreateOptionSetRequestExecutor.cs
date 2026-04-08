using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using System;

namespace FakeXrmEasy.FakeMessageExecutors
{
    /// <summary>
    /// Fake message executor that handles <see cref="CreateOptionSetRequest"/> messages.
    /// Creates a new global OptionSet in the faked CRM context's metadata cache.
    /// </summary>
    public class CreateOptionSetRequestExecutor : IFakeMessageExecutor
    {
        /// <summary>
        /// Determines whether this executor can handle the specified organization request.
        /// </summary>
        /// <param name="request">The organization request to evaluate.</param>
        /// <returns><c>true</c> if the request is a <see cref="CreateOptionSetRequest"/>; otherwise, <c>false</c>.</returns>
        public bool CanExecute(OrganizationRequest request)
        {
            return request is CreateOptionSetRequest;
        }

        /// <summary>
        /// Executes the <see cref="CreateOptionSetRequest"/> and creates the OptionSet in the metadata cache.
        /// </summary>
        /// <param name="request">The organization request to execute. Must be a <see cref="CreateOptionSetRequest"/>.</param>
        /// <param name="ctx">The faked XRM context containing the OptionSet metadata cache.</param>
        /// <returns>
        /// A <see cref="CreateOptionSetResponse"/> containing the MetadataId of the newly created OptionSet.
        /// </returns>
        /// <remarks>
        /// The OptionSet must have a valid Name property. If the Name is null or empty, or if an OptionSet
        /// with the same Name already exists, a FaultException will be thrown.
        /// The created OptionSet is stored in <see cref="XrmFakedContext.OptionSetValuesMetadata"/>.
        /// </remarks>
        public OrganizationResponse Execute(OrganizationRequest request, XrmFakedContext ctx)
        {
            var createOptionSetRequest = (CreateOptionSetRequest)request;

            var optionSetBase = createOptionSetRequest.OptionSet;

            if (optionSetBase == null)
            {
                FakeOrganizationServiceFault.Throw(ErrorCodes.InvalidArgument, "OptionSet is required");
            }

            var optionSet = optionSetBase as OptionSetMetadata;
            if (optionSet == null)
            {
                FakeOrganizationServiceFault.Throw(ErrorCodes.InvalidArgument, "OptionSet must be of type OptionSetMetadata");
            }

            var name = optionSet.Name;

            if (string.IsNullOrEmpty(name))
            {
                FakeOrganizationServiceFault.Throw(ErrorCodes.InvalidArgument, "OptionSet Name is required");
            }

            if (ctx.OptionSetValuesMetadata.ContainsKey(name))
            {
                FakeOrganizationServiceFault.Throw(ErrorCodes.DuplicateName, string.Format("An OptionSetMetadata with the name {0} already exists.", name));
            }

            var metadataId = Guid.NewGuid();
            optionSet.MetadataId = metadataId;

            ctx.OptionSetValuesMetadata.Add(name, optionSet);

            var response = new CreateOptionSetResponse()
            {
                Results = new ParameterCollection
                {
                    { "MetadataId", metadataId }
                }
            };

            return response;
        }

        /// <summary>
        /// Gets the type of organization request that this executor is responsible for handling.
        /// </summary>
        /// <returns>The <see cref="Type"/> of <see cref="CreateOptionSetRequest"/>.</returns>
        public Type GetResponsibleRequestType()
        {
            return typeof(CreateOptionSetRequest);
        }
    }
}
