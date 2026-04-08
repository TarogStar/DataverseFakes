using Microsoft.Xrm.Sdk;
using System.Runtime.Serialization;

namespace Microsoft.Xrm.Sdk.Messages
{
    /// <summary>
    /// UpdateMultiple response
    /// This class mirrors the official SDK implementation for compatibility
    /// </summary>
    [DataContract(Namespace = "http://schemas.microsoft.com/xrm/2011/Contracts")]
    public sealed class UpdateMultipleResponse : OrganizationResponse
    {
        /// <summary>
        /// Initializes a new instance of the UpdateMultipleResponse class
        /// </summary>
        public UpdateMultipleResponse()
        {
            ResponseName = "UpdateMultiple";
            Results = new ParameterCollection();
        }
    }
}
