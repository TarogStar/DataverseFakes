using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Linq;

namespace FakeXrmEasy.FakeMessageExecutors
{
    /// <summary>
    /// Fake message executor that handles <see cref="RetrieveAttributeRequest"/> messages.
    /// Retrieves attribute metadata for a specific entity attribute from the faked CRM context's metadata cache.
    /// </summary>
    public class RetrieveAttributeRequestExecutor : IFakeMessageExecutor
    {
        /// <summary>
        /// Determines whether this executor can handle the specified organization request.
        /// </summary>
        /// <param name="request">The organization request to evaluate.</param>
        /// <returns><c>true</c> if the request is a <see cref="RetrieveAttributeRequest"/>; otherwise, <c>false</c>.</returns>
        public bool CanExecute(OrganizationRequest request)
        {
            return request is RetrieveAttributeRequest;
        }

        /// <summary>
        /// Executes the <see cref="RetrieveAttributeRequest"/> and returns the corresponding attribute metadata.
        /// </summary>
        /// <param name="request">The organization request to execute. Must be a <see cref="RetrieveAttributeRequest"/>.</param>
        /// <param name="ctx">The faked XRM context containing the entity and attribute metadata cache.</param>
        /// <returns>
        /// A <see cref="RetrieveAttributeResponse"/> containing the <see cref="Microsoft.Xrm.Sdk.Metadata.AttributeMetadata"/>
        /// for the requested attribute in the Results collection.
        /// </returns>
        /// <exception cref="Exception">
        /// Thrown when the EntityLogicalName property is not provided,
        /// when the LogicalName property (attribute name) is not provided,
        /// when the entity metadata is not found in the cache,
        /// or when the attribute is not found in the entity's metadata.
        /// </exception>
        public OrganizationResponse Execute(OrganizationRequest request, XrmFakedContext ctx)
        {
            var req = request as RetrieveAttributeRequest;

            if (string.IsNullOrWhiteSpace(req.EntityLogicalName))
            {
                throw new Exception("The EntityLogicalName property must be provided in this request");
            }

            if (string.IsNullOrWhiteSpace(req.LogicalName))
            {
                throw new Exception("The LogicalName property must be provided in this request");
            }

            var entityMetadata = ctx.GetEntityMetadataByName(req.EntityLogicalName);
            if(entityMetadata == null)
            {
                throw new Exception(string.Format("The entity metadata with logical name {0} wasn't initialized. Please use .InitializeMetadata", req.EntityLogicalName));
            }

            if(entityMetadata.Attributes == null)
            {
                throw new Exception(string.Format("The attribute {0} wasn't found in entity metadata with logical name {1}. ", req.LogicalName, req.EntityLogicalName));
            }

            var attributeMetadata = entityMetadata.Attributes
                                    .FirstOrDefault(a => a.LogicalName.Equals(req.LogicalName));

            if (attributeMetadata == null)
            {
                throw new Exception(string.Format("The attribute {0} wasn't found in entity metadata with logical name {1}. ", req.LogicalName, req.EntityLogicalName));
            }

            // For EnumAttributeMetadata types (PicklistAttributeMetadata, StateAttributeMetadata, StatusAttributeMetadata),
            // ensure the OptionSet is populated from OptionSetValuesMetadata if not already set
            if (attributeMetadata is EnumAttributeMetadata enumAttribute)
            {
                PopulateOptionSetFromValuesMetadata(enumAttribute, req.EntityLogicalName, req.LogicalName, ctx);
            }

            var response = new RetrieveAttributeResponse()
            {
                Results = new ParameterCollection
                {
                    { "AttributeMetadata", attributeMetadata }
                }
            };

            return response;
        }

        /// <summary>
        /// Gets the type of organization request that this executor is responsible for handling.
        /// </summary>
        /// <returns>The <see cref="Type"/> of <see cref="RetrieveAttributeRequest"/>.</returns>
        public Type GetResponsibleRequestType()
        {
            return typeof(RetrieveAttributeRequest);
        }

        /// <summary>
        /// Populates the OptionSet property of an EnumAttributeMetadata from the OptionSetValuesMetadata dictionary
        /// if the OptionSet is not already populated with options.
        /// </summary>
        /// <param name="enumAttribute">The enum attribute metadata to populate.</param>
        /// <param name="entityLogicalName">The logical name of the entity.</param>
        /// <param name="attributeLogicalName">The logical name of the attribute.</param>
        /// <param name="ctx">The faked XRM context containing the OptionSetValuesMetadata.</param>
        private void PopulateOptionSetFromValuesMetadata(EnumAttributeMetadata enumAttribute, string entityLogicalName, string attributeLogicalName, XrmFakedContext ctx)
        {
            // Check if OptionSet already has options
            if (enumAttribute.OptionSet?.Options != null && enumAttribute.OptionSet.Options.Count > 0)
            {
                return; // Already populated
            }

            // Try to get options from OptionSetValuesMetadata using the entity#attribute key pattern
            var key = $"{entityLogicalName}#{attributeLogicalName}";
            if (ctx.OptionSetValuesMetadata.ContainsKey(key))
            {
                var optionSetMetadata = ctx.OptionSetValuesMetadata[key];
                if (optionSetMetadata?.Options != null && optionSetMetadata.Options.Count > 0)
                {
                    // Create a new OptionSetMetadata with the options
                    var newOptionSet = new OptionSetMetadata(optionSetMetadata.Options);
                    enumAttribute.OptionSet = newOptionSet;
                }
            }
            // Also check if the attribute references a global option set
            else if (!string.IsNullOrEmpty(enumAttribute.OptionSet?.Name) && ctx.OptionSetValuesMetadata.ContainsKey(enumAttribute.OptionSet.Name))
            {
                var optionSetMetadata = ctx.OptionSetValuesMetadata[enumAttribute.OptionSet.Name];
                if (optionSetMetadata?.Options != null && optionSetMetadata.Options.Count > 0)
                {
                    // Create a new OptionSetMetadata with the options, preserving the name
                    var newOptionSet = new OptionSetMetadata(optionSetMetadata.Options)
                    {
                        Name = enumAttribute.OptionSet.Name
                    };
                    enumAttribute.OptionSet = newOptionSet;
                }
            }
        }
    }
}