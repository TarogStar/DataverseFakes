using Microsoft.Xrm.Sdk;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.Xrm.Sdk.Messages
{
    /// <summary>
    /// CreateMultiple request for bulk entity creation
    /// This class mirrors the official SDK implementation for compatibility
    /// </summary>
    [DataContract(Namespace = "http://schemas.microsoft.com/xrm/2011/Contracts")]
    public sealed class CreateMultipleRequest : OrganizationRequest
    {
        /// <summary>
        /// Gets or sets the collection of entities to create
        /// </summary>
        public EntityCollection Targets
        {
            get
            {
                if (Parameters.Contains("Targets"))
                    return (EntityCollection)Parameters["Targets"];
                return null;
            }
            set
            {
                Parameters["Targets"] = value;
            }
        }

        /// <summary>
        /// Initializes a new instance of the CreateMultipleRequest class
        /// </summary>
        public CreateMultipleRequest()
        {
            RequestName = "CreateMultiple";
        }
    }
}
