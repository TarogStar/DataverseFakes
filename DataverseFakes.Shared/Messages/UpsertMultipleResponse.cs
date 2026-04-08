using Microsoft.Xrm.Sdk;
using System;
using System.Runtime.Serialization;

namespace Microsoft.Xrm.Sdk.Messages
{
    /// <summary>
    /// Result of an upsert operation indicating whether record was created or updated
    /// </summary>
    [DataContract(Namespace = "http://schemas.microsoft.com/xrm/2011/Contracts")]
    public sealed class UpsertMultipleResult
    {
        /// <summary>
        /// Gets or sets the ID of the upserted record
        /// </summary>
        [DataMember]
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets whether the record was created (true) or updated (false)
        /// </summary>
        [DataMember]
        public bool RecordCreated { get; set; }
    }

    /// <summary>
    /// UpsertMultiple response containing results for each upserted entity
    /// This class mirrors the official SDK implementation for compatibility
    /// </summary>
    [DataContract(Namespace = "http://schemas.microsoft.com/xrm/2011/Contracts")]
    public sealed class UpsertMultipleResponse : OrganizationResponse
    {
        /// <summary>
        /// Gets the collection of upsert results
        /// </summary>
        public UpsertMultipleResult[] Results
        {
            get
            {
                if (base.Results.Contains("Results"))
                    return (UpsertMultipleResult[])base.Results["Results"];
                return Array.Empty<UpsertMultipleResult>();
            }
        }

        /// <summary>
        /// Initializes a new instance of the UpsertMultipleResponse class
        /// </summary>
        public UpsertMultipleResponse()
        {
            ResponseName = "UpsertMultiple";
            base.Results = new ParameterCollection();
        }
    }
}
