using Microsoft.Xrm.Sdk;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.Xrm.Sdk.Messages
{
    /// <summary>
    /// DeleteMultiple request for bulk entity deletion
    /// This class mirrors the official SDK implementation for compatibility
    /// Note: DeleteMultiple is currently in preview and only supported for elastic tables
    /// </summary>
    [DataContract(Namespace = "http://schemas.microsoft.com/xrm/2011/Contracts")]
    public sealed class DeleteMultipleRequest : OrganizationRequest
    {
        /// <summary>
        /// Gets or sets the collection of entity references to delete
        /// </summary>
        public EntityReferenceCollection Targets
        {
            get
            {
                if (Parameters.Contains("Targets"))
                    return (EntityReferenceCollection)Parameters["Targets"];
                return null;
            }
            set
            {
                Parameters["Targets"] = value;
            }
        }

        /// <summary>
        /// Initializes a new instance of the DeleteMultipleRequest class
        /// </summary>
        public DeleteMultipleRequest()
        {
            RequestName = "DeleteMultiple";
        }
    }
}
