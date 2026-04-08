using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.Xrm.Sdk.Messages
{
    /// <summary>
    /// CreateMultiple response containing created entity IDs
    /// This class mirrors the official SDK implementation for compatibility
    /// </summary>
    [DataContract(Namespace = "http://schemas.microsoft.com/xrm/2011/Contracts")]
    public sealed class CreateMultipleResponse : OrganizationResponse
    {
        /// <summary>
        /// Gets the collection of created entity IDs
        /// </summary>
        public Guid[] Ids
        {
            get
            {
                if (Results.Contains("Ids"))
                    return (Guid[])Results["Ids"];
                return Array.Empty<Guid>();
            }
        }

        /// <summary>
        /// Initializes a new instance of the CreateMultipleResponse class
        /// </summary>
        public CreateMultipleResponse()
        {
            ResponseName = "CreateMultiple";
            Results = new ParameterCollection();
        }
    }
}
