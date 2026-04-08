using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xrm.Sdk;
using System.Configuration;
using System.IO;

using System.Xml.Linq;
using System.Linq;

using System.IO.Compression;
using System.Runtime.Serialization;

#if FAKE_XRM_EASY_2016 || FAKE_XRM_EASY_365 || FAKE_XRM_EASY_9
using Microsoft.Xrm.Tooling.Connector;
#else

using Microsoft.Xrm.Client;
using Microsoft.Xrm.Client.Services;

#endif

namespace FakeXrmEasy
{
    /// <summary>
    /// Reuse unit test syntax to test against a real CRM organisation.
    /// It uses a real CRM organisation service instance.
    /// </summary>
    public class XrmRealContext : XrmFakedContext, IXrmContext
    {
        /// <summary>
        /// Gets or sets the name of the connection string to use for connecting to CRM.
        /// Can be either a connection string name from app.config or the actual connection string itself.
        /// Defaults to "fakexrmeasy-connection".
        /// </summary>
        public string ConnectionStringName { get; set; } = "fakexrmeasy-connection";

        /// <summary>
        /// Initializes a new instance of the XrmRealContext class using the default connection string name.
        /// </summary>
        public XrmRealContext()
        {
            //Don't setup fakes in this case.
        }

        /// <summary>
        /// Initializes a new instance of the XrmRealContext class with the specified connection string name.
        /// </summary>
        /// <param name="connectionStringName">The name of the connection string or the connection string itself.</param>
        public XrmRealContext(string connectionStringName)
        {
            ConnectionStringName = connectionStringName;
            //Don't setup fakes in this case.
        }

        /// <summary>
        /// Initializes a new instance of the XrmRealContext class with an existing organization service.
        /// </summary>
        /// <param name="organizationService">The organization service instance to use.</param>
        public XrmRealContext(IOrganizationService organizationService)
        {
            Service = organizationService;
            //Don't setup fakes in this case.
        }

        /// <summary>
        /// Gets the organization service instance for the real CRM connection.
        /// Creates a new connection if one does not already exist.
        /// </summary>
        /// <returns>An IOrganizationService instance connected to the real CRM.</returns>
        public override IOrganizationService GetOrganizationService()
        {
            if (Service != null)
                return Service;

            Service = GetOrgService();
            return Service;
        }

        /// <summary>
        /// Does nothing in XrmRealContext to prevent creating records in a real organization database.
        /// Override of the base Initialize method that ignores the entity collection.
        /// </summary>
        /// <param name="entities">The entities to initialize (ignored in XrmRealContext).</param>
        public override void Initialize(IEnumerable<Entity> entities)
        {
            //Does nothing...  otherwise it would create records in a real org db
        }

        /// <summary>
        /// Creates a new organization service connection using the configured connection string.
        /// </summary>
        /// <returns>An IOrganizationService instance connected to the CRM.</returns>
        protected IOrganizationService GetOrgService()
        {
            var connection = ConfigurationManager.ConnectionStrings[ConnectionStringName];

            // In case of missing connection string in configuration,
            // use ConnectionStringName as an explicit connection string
            var connectionString = connection == null ? ConnectionStringName : connection.ConnectionString;

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new Exception("The ConnectionStringName property must be either a connection string or a connection string name");
            }

#if FAKE_XRM_EASY_2016 || FAKE_XRM_EASY_365 || FAKE_XRM_EASY_9

            // Connect to the CRM web service using a connection string.
            CrmServiceClient client = new Microsoft.Xrm.Tooling.Connector.CrmServiceClient(connectionString);
            return client;

#else
            CrmConnection crmConnection = CrmConnection.Parse(connectionString);
            OrganizationService service = new OrganizationService(crmConnection);
            return service;
#endif
        }

        /// <summary>
        /// Deserializes a plugin execution context from a compressed, base64-encoded profile string.
        /// Useful for replaying plugin executions captured from the Plugin Registration Tool.
        /// </summary>
        /// <param name="sCompressedProfile">The base64-encoded, compressed plugin context profile.</param>
        /// <returns>A XrmFakedPluginExecutionContext instance deserialized from the profile.</returns>
        public XrmFakedPluginExecutionContext GetContextFromSerialisedCompressedProfile(string sCompressedProfile)
        {
            byte[] data = Convert.FromBase64String(sCompressedProfile);

            using (var memStream = new MemoryStream(data))
            {
                using (var decompressedStream = new DeflateStream(memStream, CompressionMode.Decompress, false))
                {
                    byte[] buffer = new byte[0x1000];

                    using (var tempStream = new MemoryStream())
                    {
                        int numBytesRead = decompressedStream.Read(buffer, 0, buffer.Length);
                        while (numBytesRead > 0)
                        {
                            tempStream.Write(buffer, 0, numBytesRead);
                            numBytesRead = decompressedStream.Read(buffer, 0, buffer.Length);
                        }

                        //tempStream has the decompressed plugin context now
                        var decompressedString = Encoding.UTF8.GetString(tempStream.ToArray());
                        var xlDoc = XDocument.Parse(decompressedString);

                        var contextElement = xlDoc.Descendants().Elements()
                            .Where(x => x.Name.LocalName.Equals("Context"))
                            .FirstOrDefault();

                        var pluginContextString = contextElement.Value;

                        XrmFakedPluginExecutionContext context = null;
                        using (var reader = new MemoryStream(Encoding.UTF8.GetBytes(pluginContextString)))
                        {
                            var dcSerializer = new DataContractSerializer(typeof(XrmFakedPluginExecutionContext));
                            context = (XrmFakedPluginExecutionContext)dcSerializer.ReadObject(reader);
                        }

                        return context;
                    }
                }
            }
        }
    }
}
