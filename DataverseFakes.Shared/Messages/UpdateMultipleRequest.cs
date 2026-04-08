using Microsoft.Xrm.Sdk;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.Xrm.Sdk.Messages
{
    /// <summary>
    /// UpdateMultiple request for bulk entity updates
    /// This class mirrors the official SDK implementation for compatibility
    /// </summary>
    [DataContract(Namespace = "http://schemas.microsoft.com/xrm/2011/Contracts")]
    public sealed class UpdateMultipleRequest : OrganizationRequest
    {
        /// <summary>
        /// Gets or sets the collection of entities to update
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
        /// Initializes a new instance of the UpdateMultipleRequest class
        /// </summary>
        public UpdateMultipleRequest()
        {
            RequestName = "UpdateMultiple";
        }
    }
}
