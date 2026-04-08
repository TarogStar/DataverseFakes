using Microsoft.Xrm.Sdk;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.Xrm.Sdk.Messages
{
    /// <summary>
    /// UpsertMultiple request for bulk entity upsert (create or update)
    /// This class mirrors the official SDK implementation for compatibility
    /// Note: UpsertMultiple is supported for elastic tables
    /// </summary>
    [DataContract(Namespace = "http://schemas.microsoft.com/xrm/2011/Contracts")]
    public sealed class UpsertMultipleRequest : OrganizationRequest
    {
        /// <summary>
        /// Gets or sets the collection of entities to upsert
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
        /// Initializes a new instance of the UpsertMultipleRequest class
        /// </summary>
        public UpsertMultipleRequest()
        {
            RequestName = "UpsertMultiple";
        }
    }
}
