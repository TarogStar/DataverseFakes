using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using System;

namespace FakeXrmEasy.FakeMessageExecutors
{
    /// <summary>
    /// Fake message executor that handles <see cref="UpdateOptionSetRequest"/> messages.
    /// Updates existing global OptionSet metadata in the faked CRM context's metadata cache.
    /// </summary>
    /// <remarks>
    /// This executor validates that the OptionSet exists before updating, and merges
    /// the updated properties (DisplayName, Description, Options) with the existing metadata.
    /// Use <see cref="XrmFakedContext.OptionSetValuesMetadata"/> to initialize OptionSet metadata
    /// before executing this request.
    /// </remarks>
    public class UpdateOptionSetRequestExecutor : IFakeMessageExecutor
    {
        /// <summary>
        /// Determines whether this executor can handle the specified organization request.
        /// </summary>
        /// <param name="request">The organization request to evaluate.</param>
        /// <returns><c>true</c> if the request is an <see cref="UpdateOptionSetRequest"/>; otherwise, <c>false</c>.</returns>
        public bool CanExecute(OrganizationRequest request)
        {
            return request is UpdateOptionSetRequest;
        }

        /// <summary>
        /// Executes the <see cref="UpdateOptionSetRequest"/> and updates the corresponding OptionSet metadata.
        /// </summary>
        /// <param name="request">The organization request to execute. Must be an <see cref="UpdateOptionSetRequest"/>.</param>
        /// <param name="ctx">The faked XRM context containing the OptionSet metadata cache.</param>
        /// <returns>
        /// An <see cref="UpdateOptionSetResponse"/> indicating successful update of the OptionSet metadata.
        /// </returns>
        /// <exception cref="System.ServiceModel.FaultException{OrganizationServiceFault}">
        /// Thrown when the OptionSet specified in the request does not exist in the metadata cache,
        /// or when the OptionSet property is null.
        /// </exception>
        /// <remarks>
        /// The executor merges the following properties from the request's OptionSet into the existing metadata:
        /// <list type="bullet">
        ///   <item><description>DisplayName - if provided in the request</description></item>
        ///   <item><description>Description - if provided in the request</description></item>
        ///   <item><description>Options - replaces existing options if provided in the request</description></item>
        /// </list>
        /// The OptionSet is identified by its Name property. Retrieval by MetadataId is not currently supported.
        /// </remarks>
        public OrganizationResponse Execute(OrganizationRequest request, XrmFakedContext ctx)
        {
            var updateOptionSetRequest = (UpdateOptionSetRequest)request;

            if (updateOptionSetRequest.OptionSet == null)
            {
                FakeOrganizationServiceFault.Throw(ErrorCodes.InvalidArgument, "OptionSet property is required for UpdateOptionSetRequest.");
            }

            var optionSetToUpdate = updateOptionSetRequest.OptionSet;
            var name = optionSetToUpdate.Name;

            if (string.IsNullOrEmpty(name))
            {
                FakeOrganizationServiceFault.Throw(ErrorCodes.InvalidArgument, "OptionSet Name is required for UpdateOptionSetRequest.");
            }

            if (!ctx.OptionSetValuesMetadata.ContainsKey(name))
            {
                FakeOrganizationServiceFault.Throw(ErrorCodes.ObjectDoesNotExist, string.Format("An OptionSetMetadata with the name {0} does not exist.", name));
            }

            var existingOptionSet = ctx.OptionSetValuesMetadata[name];

            // Merge updated properties
            if (optionSetToUpdate.DisplayName != null)
            {
                existingOptionSet.DisplayName = optionSetToUpdate.DisplayName;
            }

            if (optionSetToUpdate.Description != null)
            {
                existingOptionSet.Description = optionSetToUpdate.Description;
            }

            // If the request contains options, replace the existing options
            if (optionSetToUpdate is OptionSetMetadata requestOptionSetMetadata &&
                existingOptionSet is OptionSetMetadata existingOptionSetMetadata)
            {
                if (requestOptionSetMetadata.Options != null && requestOptionSetMetadata.Options.Count > 0)
                {
                    existingOptionSetMetadata.Options.Clear();
                    foreach (var option in requestOptionSetMetadata.Options)
                    {
                        existingOptionSetMetadata.Options.Add(option);
                    }
                }
            }

            return new UpdateOptionSetResponse();
        }

        /// <summary>
        /// Gets the type of organization request that this executor is responsible for handling.
        /// </summary>
        /// <returns>The <see cref="Type"/> of <see cref="UpdateOptionSetRequest"/>.</returns>
        public Type GetResponsibleRequestType()
        {
            return typeof(UpdateOptionSetRequest);
        }
    }
}
