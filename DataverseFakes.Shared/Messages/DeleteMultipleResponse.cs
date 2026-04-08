using Microsoft.Xrm.Sdk;
using System.Runtime.Serialization;

namespace Microsoft.Xrm.Sdk.Messages
{
    /// <summary>
    /// DeleteMultiple response
    /// This class mirrors the official SDK implementation for compatibility
    /// </summary>
    [DataContract(Namespace = "http://schemas.microsoft.com/xrm/2011/Contracts")]
    public sealed class DeleteMultipleResponse : OrganizationResponse
    {
        /// <summary>
        /// Initializes a new instance of the DeleteMultipleResponse class
        /// </summary>
        public DeleteMultipleResponse()
        {
            ResponseName = "DeleteMultiple";
            Results = new ParameterCollection();
        }
    }
}
