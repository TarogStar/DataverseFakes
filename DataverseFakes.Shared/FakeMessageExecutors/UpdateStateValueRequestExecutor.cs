using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using DataverseFakes.Extensions;
using System;
using System.Linq;

namespace DataverseFakes.FakeMessageExecutors
{
    /// <summary>
    /// Fake message executor for UpdateStateValueRequest.
    /// Updates the label of an existing state (statecode) value on an entity.
    /// </summary>
    public class UpdateStateValueRequestExecutor : IFakeMessageExecutor
    {
        /// <summary>
        /// Determines whether this executor can handle the specified organization request.
        /// </summary>
        /// <param name="request">The organization request to evaluate.</param>
        /// <returns><c>true</c> if the request is an <see cref="UpdateStateValueRequest"/>; otherwise, <c>false</c>.</returns>
        public bool CanExecute(OrganizationRequest request)
        {
            return request is UpdateStateValueRequest;
        }

        /// <summary>
        /// Gets the type of organization request that this executor is responsible for handling.
        /// </summary>
        /// <returns>The <see cref="Type"/> of <see cref="UpdateStateValueRequest"/>.</returns>
        public Type GetResponsibleRequestType()
        {
            return typeof(UpdateStateValueRequest);
        }

        /// <summary>
        /// Executes the <see cref="UpdateStateValueRequest"/> and updates the label of an existing state value.
        /// </summary>
        /// <param name="request">The organization request to execute. Must be an <see cref="UpdateStateValueRequest"/>.</param>
        /// <param name="ctx">The faked XRM context containing the metadata cache.</param>
        /// <returns>An <see cref="UpdateStateValueResponse"/> indicating success.</returns>
        /// <exception cref="System.ServiceModel.FaultException{OrganizationServiceFault}">
        /// Thrown when EntityLogicalName or AttributeLogicalName are missing,
        /// when no state value metadata is found, or when the option value is not found.
        /// </exception>
        public OrganizationResponse Execute(OrganizationRequest request, XrmFakedContext ctx)
        {
            var req = (UpdateStateValueRequest)request;

            if (string.IsNullOrEmpty(req.EntityLogicalName))
            {
                FakeOrganizationServiceFault.Throw(ErrorCodes.InvalidArgument,
                    "EntityLogicalName is required for UpdateStateValueRequest.");
            }

            if (string.IsNullOrEmpty(req.AttributeLogicalName))
            {
                FakeOrganizationServiceFault.Throw(ErrorCodes.InvalidArgument,
                    "AttributeLogicalName is required for UpdateStateValueRequest.");
            }

            var key = $"{req.EntityLogicalName}#{req.AttributeLogicalName}";

            // Update via OptionSetValuesMetadata if present
            if (ctx.OptionSetValuesMetadata.ContainsKey(key))
            {
                var optionSetMetadata = ctx.OptionSetValuesMetadata[key];
                var option = optionSetMetadata.Options.FirstOrDefault(o => o.Value == req.Value);
                if (option == null)
                {
                    FakeOrganizationServiceFault.Throw(ErrorCodes.ObjectDoesNotExist,
                        $"State option with value '{req.Value}' does not exist in option set '{key}'.");
                }
                option.Label = req.Label;
            }
            else
            {
                // Try to find in entity attribute metadata
                var entityMetadataCheck = ctx.GetEntityMetadataByName(req.EntityLogicalName);
                if (entityMetadataCheck?.Attributes == null
                    || !entityMetadataCheck.Attributes.Any(a => a.LogicalName == req.AttributeLogicalName))
                {
                    FakeOrganizationServiceFault.Throw(ErrorCodes.ObjectDoesNotExist,
                        $"No state value metadata found for key '{key}'.");
                }
                // Option set found in entity attribute — will be updated below
            }

            // Also update entity attribute metadata if present
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

            return new UpdateStateValueResponse();
        }
    }
}
