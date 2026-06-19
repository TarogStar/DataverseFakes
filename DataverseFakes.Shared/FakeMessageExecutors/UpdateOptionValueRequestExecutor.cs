using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using DataverseFakes.Extensions;
using System;
using System.Linq;

namespace DataverseFakes.FakeMessageExecutors
{
    /// <summary>
    /// Fake message executor for UpdateOptionValueRequest.
    /// Updates the label of an existing option in a global OptionSet or an entity-attribute picklist.
    /// </summary>
    public class UpdateOptionValueRequestExecutor : IFakeMessageExecutor
    {
        /// <summary>
        /// Determines whether this executor can handle the specified organization request.
        /// </summary>
        /// <param name="request">The organization request to evaluate.</param>
        /// <returns><c>true</c> if the request is an <see cref="UpdateOptionValueRequest"/>; otherwise, <c>false</c>.</returns>
        public bool CanExecute(OrganizationRequest request)
        {
            return request is UpdateOptionValueRequest;
        }

        /// <summary>
        /// Gets the type of organization request that this executor is responsible for handling.
        /// </summary>
        /// <returns>The <see cref="Type"/> of <see cref="UpdateOptionValueRequest"/>.</returns>
        public Type GetResponsibleRequestType()
        {
            return typeof(UpdateOptionValueRequest);
        }

        /// <summary>
        /// Executes the <see cref="UpdateOptionValueRequest"/> and updates the label of an existing option.
        /// </summary>
        /// <param name="request">The organization request to execute. Must be an <see cref="UpdateOptionValueRequest"/>.</param>
        /// <param name="ctx">The faked XRM context containing the metadata cache.</param>
        /// <returns>An <see cref="UpdateOptionValueResponse"/> indicating success.</returns>
        /// <exception cref="System.ServiceModel.FaultException{OrganizationServiceFault}">
        /// Thrown when neither OptionSetName nor EntityLogicalName+AttributeLogicalName are provided,
        /// when the option set key does not exist, or when the option value is not found.
        /// </exception>
        public OrganizationResponse Execute(OrganizationRequest request, XrmFakedContext ctx)
        {
            var req = (UpdateOptionValueRequest)request;

            var hasOptionSetName = !string.IsNullOrEmpty(req.OptionSetName);
            var hasEntityAttr = !string.IsNullOrEmpty(req.EntityLogicalName)
                                && !string.IsNullOrEmpty(req.AttributeLogicalName);

            if (!hasOptionSetName && !hasEntityAttr)
            {
                FakeOrganizationServiceFault.Throw(ErrorCodes.InvalidArgument,
                    "At least OptionSetName or both EntityLogicalName and AttributeLogicalName must be provided.");
            }

            // Determine the key into OptionSetValuesMetadata
            string key = hasOptionSetName
                ? req.OptionSetName
                : $"{req.EntityLogicalName}#{req.AttributeLogicalName}";

            if (!ctx.OptionSetValuesMetadata.ContainsKey(key))
            {
                FakeOrganizationServiceFault.Throw(ErrorCodes.ObjectDoesNotExist,
                    $"OptionSet with key '{key}' does not exist.");
            }

            var optionSetMetadata = ctx.OptionSetValuesMetadata[key];
            var option = optionSetMetadata.Options.FirstOrDefault(o => o.Value == req.Value);
            if (option == null)
            {
                FakeOrganizationServiceFault.Throw(ErrorCodes.ObjectDoesNotExist,
                    $"Option with value '{req.Value}' does not exist in option set '{key}'.");
            }

            option.Label = req.Label;

            // Also update the entity attribute metadata if entity metadata exists
            if (!string.IsNullOrEmpty(req.EntityLogicalName) && !string.IsNullOrEmpty(req.AttributeLogicalName))
            {
                var entityMetadata = ctx.GetEntityMetadataByName(req.EntityLogicalName);
                if (entityMetadata?.Attributes != null)
                {
                    var attribute = entityMetadata.Attributes
                        .FirstOrDefault(a => a.LogicalName == req.AttributeLogicalName);

                    if (attribute is EnumAttributeMetadata enumAttr && enumAttr.OptionSet?.Options != null)
                    {
                        var attrOption = enumAttr.OptionSet.Options.FirstOrDefault(o => o.Value == req.Value);
                        if (attrOption != null)
                        {
                            attrOption.Label = req.Label;
                        }
                        entityMetadata.SetAttribute(enumAttr);
                        ctx.SetEntityMetadata(entityMetadata);
                    }
                }
            }

            return new UpdateOptionValueResponse();
        }
    }
}
