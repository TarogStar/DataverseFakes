using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Linq;

namespace FakeXrmEasy.FakeMessageExecutors
{
    /// <summary>
    /// Fake message executor that handles <see cref="DeleteOptionSetRequest"/> messages.
    /// Deletes global OptionSet metadata from the faked CRM context's metadata cache.
    /// </summary>
    /// <remarks>
    /// This executor validates that the OptionSet exists before deletion.
    /// If the OptionSet is referenced by any entity attributes (e.g., PicklistAttributeMetadata),
    /// the deletion will fail with a <see cref="ErrorCodes.CannotDeleteOptionSet"/> error.
    /// Use <see cref="XrmFakedContext.OptionSetValuesMetadata"/> to manage OptionSet metadata.
    /// </remarks>
    public class DeleteOptionSetRequestExecutor : IFakeMessageExecutor
    {
        /// <summary>
        /// Determines whether this executor can handle the specified organization request.
        /// </summary>
        /// <param name="request">The organization request to evaluate.</param>
        /// <returns><c>true</c> if the request is a <see cref="DeleteOptionSetRequest"/>; otherwise, <c>false</c>.</returns>
        public bool CanExecute(OrganizationRequest request)
        {
            return request is DeleteOptionSetRequest;
        }

        /// <summary>
        /// Executes the <see cref="DeleteOptionSetRequest"/> and removes the OptionSet from the context.
        /// </summary>
        /// <param name="request">The organization request to execute. Must be a <see cref="DeleteOptionSetRequest"/>.</param>
        /// <param name="ctx">The faked XRM context containing the OptionSet metadata cache.</param>
        /// <returns>
        /// A <see cref="DeleteOptionSetResponse"/> indicating successful deletion.
        /// </returns>
        /// <exception cref="System.ServiceModel.FaultException{OrganizationServiceFault}">
        /// Thrown when:
        /// <list type="bullet">
        /// <item><description>The Name property is null or empty.</description></item>
        /// <item><description>The specified OptionSet does not exist in the context.</description></item>
        /// <item><description>The OptionSet is referenced by one or more entity attributes.</description></item>
        /// </list>
        /// </exception>
        /// <remarks>
        /// The OptionSet must be identified by Name. Deletion by MetadataId is not currently supported.
        /// Before deletion, this executor checks if the OptionSet is referenced by any entity
        /// attributes in the EntityMetadata cache. If references are found, deletion is blocked.
        /// </remarks>
        public OrganizationResponse Execute(OrganizationRequest request, XrmFakedContext ctx)
        {
            var deleteOptionSetRequest = (DeleteOptionSetRequest)request;

            var name = deleteOptionSetRequest.Name;

            // Validate that the Name parameter is provided
            if (string.IsNullOrEmpty(name))
            {
                FakeOrganizationServiceFault.Throw(ErrorCodes.InvalidArgument, "Name is required to delete an OptionSet.");
            }

            // Validate that the OptionSet exists
            if (!ctx.OptionSetValuesMetadata.ContainsKey(name))
            {
                FakeOrganizationServiceFault.Throw(ErrorCodes.ObjectDoesNotExist, string.Format("An OptionSetMetadata with the name {0} does not exist.", name));
            }

            // Check if the OptionSet is in use by any entity attributes
            if (IsOptionSetInUse(name, ctx))
            {
                FakeOrganizationServiceFault.Throw(ErrorCodes.CannotDeleteOptionSet, string.Format("The OptionSet '{0}' cannot be deleted because it is referenced by one or more entity attributes.", name));
            }

            // Remove the OptionSet from the context
            ctx.OptionSetValuesMetadata.Remove(name);

            return new DeleteOptionSetResponse();
        }

        /// <summary>
        /// Gets the type of organization request that this executor is responsible for handling.
        /// </summary>
        /// <returns>The <see cref="Type"/> of <see cref="DeleteOptionSetRequest"/>.</returns>
        public Type GetResponsibleRequestType()
        {
            return typeof(DeleteOptionSetRequest);
        }

        /// <summary>
        /// Checks if the specified global OptionSet is referenced by any entity attributes in the metadata cache.
        /// Scans through all EntityMetadata and their attributes to find references to the OptionSet.
        /// </summary>
        /// <param name="optionSetName">The name of the global OptionSet to check for references.</param>
        /// <param name="ctx">The faked XRM context containing the EntityMetadata cache.</param>
        /// <returns><c>true</c> if the OptionSet is referenced by any entity attribute; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// This method examines <see cref="EnumAttributeMetadata"/> attributes (PicklistAttributeMetadata,
        /// StateAttributeMetadata, StatusAttributeMetadata, etc.) to determine if they reference the global OptionSet.
        /// An attribute references a global OptionSet when its OptionSet.Name property matches the optionSetName parameter.
        /// </remarks>
        private bool IsOptionSetInUse(string optionSetName, XrmFakedContext ctx)
        {
            // Check the EntityMetadata dictionary for references
            var entityMetadataQuery = ctx.CreateMetadataQuery();

            foreach (var entityMetadata in entityMetadataQuery)
            {
                if (entityMetadata.Attributes == null)
                {
                    continue;
                }

                // Check each attribute that could reference a global option set
                foreach (var attribute in entityMetadata.Attributes)
                {
                    if (attribute is EnumAttributeMetadata enumAttribute)
                    {
                        // Check if the attribute references this global option set
                        if (enumAttribute.OptionSet != null &&
                            !string.IsNullOrEmpty(enumAttribute.OptionSet.Name) &&
                            enumAttribute.OptionSet.Name.Equals(optionSetName, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}
