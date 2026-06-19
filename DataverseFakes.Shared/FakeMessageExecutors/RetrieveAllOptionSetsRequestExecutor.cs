using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Linq;

namespace DataverseFakes.FakeMessageExecutors
{
    /// <summary>
    /// Fake message executor for RetrieveAllOptionSetsRequest.
    /// Returns all global option sets stored in the context as a <see cref="RetrieveAllOptionSetsResponse"/>.
    /// </summary>
    public class RetrieveAllOptionSetsRequestExecutor : IFakeMessageExecutor
    {
        /// <summary>
        /// Determines whether this executor can handle the specified organization request.
        /// </summary>
        /// <param name="request">The organization request to evaluate.</param>
        /// <returns><c>true</c> if the request is a <see cref="RetrieveAllOptionSetsRequest"/>; otherwise, <c>false</c>.</returns>
        public bool CanExecute(OrganizationRequest request)
        {
            return request is RetrieveAllOptionSetsRequest;
        }

        /// <summary>
        /// Gets the type of organization request that this executor is responsible for handling.
        /// </summary>
        /// <returns>The <see cref="Type"/> of <see cref="RetrieveAllOptionSetsRequest"/>.</returns>
        public Type GetResponsibleRequestType()
        {
            return typeof(RetrieveAllOptionSetsRequest);
        }

        /// <summary>
        /// Executes the <see cref="RetrieveAllOptionSetsRequest"/> and returns all global option sets.
        /// </summary>
        /// <param name="request">The organization request to execute. Must be a <see cref="RetrieveAllOptionSetsRequest"/>.</param>
        /// <param name="ctx">The faked XRM context containing the OptionSet metadata cache.</param>
        /// <returns>
        /// A <see cref="RetrieveAllOptionSetsResponse"/> with Results["OptionSetMetadata"] = <see cref="OptionSetMetadataBase"/>[].
        /// </returns>
        public OrganizationResponse Execute(OrganizationRequest request, XrmFakedContext ctx)
        {
            var allOptionSets = ctx.OptionSetValuesMetadata.Values
                .Cast<OptionSetMetadataBase>()
                .ToArray();

            var response = new RetrieveAllOptionSetsResponse
            {
                Results = new ParameterCollection
                {
                    { "OptionSetMetadata", allOptionSets }
                }
            };

            return response;
        }
    }
}
