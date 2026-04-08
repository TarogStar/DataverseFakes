using Microsoft.Xrm.Sdk;
using System;
using System.Runtime.Serialization;

namespace FakeXrmEasy
{
    /// <summary>
    /// Provides a fake implementation of <see cref="IPluginExecutionContext4"/> for unit testing Dynamics 365 plugins.
    /// This class simulates the plugin execution context that is normally provided by the CRM runtime during plugin execution.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class holds custom properties of IPluginExecutionContext extracted from
    /// https://msdn.microsoft.com/es-es/library/microsoft.xrm.sdk.ipluginexecutioncontext_properties.aspx
    /// </para>
    /// <para>
    /// Implements IPluginExecutionContext4 for full D365 v9+ compatibility, which includes support for
    /// Azure Active Directory authentication, Portals integration, and transaction integration messages.
    /// </para>
    /// </remarks>
    [DataContract(Name = "PluginExecutionContext", Namespace = "")]
    public class XrmFakedPluginExecutionContext : IPluginExecutionContext4
    {
        /// <summary>
        /// Gets or sets the GUID of the business unit in which the user who initiated the plugin execution operates.
        /// </summary>
        [DataMember(Order = 1)]
        public Guid BusinessUnitId { get; set; }

        /// <summary>
        /// Gets or sets the GUID used to correlate related operations across multiple transactions.
        /// This is useful for tracking a series of related plugin executions.
        /// </summary>
        [DataMember(Order = 2)]
        public Guid CorrelationId { get; set; }

        /// <summary>
        /// Gets or sets the current depth of execution in the call stack.
        /// The depth increases each time a plugin triggers another plugin or workflow.
        /// CRM enforces a maximum depth limit (default 8) to prevent infinite loops.
        /// </summary>
        [DataMember(Order = 3)]
        public int Depth { get; set; }

        /// <summary>
        /// Gets or sets the GUID of the user who originally initiated the plugin execution.
        /// This remains constant even if the plugin triggers other plugins or workflows.
        /// </summary>
        [DataMember(Order = 4)]
        public Guid InitiatingUserId { get; set; }

        /// <summary>
        /// Gets or sets the input parameters that were passed to the plugin.
        /// For most operations, this contains a "Target" key with the entity or entity reference being processed.
        /// </summary>
        [DataMember(Order = 5)]
        public ParameterCollection InputParameters { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the plugin is executing in offline mode.
        /// </summary>
        [DataMember(Order = 6)]
        public bool IsExecutingOffline { get; set; }

        /// <summary>
        /// Gets a value indicating whether the plugin is executing within a database transaction.
        /// This is automatically calculated based on the Stage and Mode properties:
        /// true for Pre-operation stage or Post-operation stage with Synchronous mode.
        /// </summary>
        /// <remarks>
        /// The setter is provided only to support serialization/deserialization scenarios
        /// and does not actually modify the calculated value.
        /// </remarks>
        [DataMember(Order = 7)]
        public bool IsInTransaction
        {
            get
            {
                return Stage == (int)ProcessingStepStage.Preoperation || Stage == (int)ProcessingStepStage.Postoperation && Mode == (int)ProcessingStepMode.Synchronous;
            }
            set {  /* This property is writable only to correctly support serialization/deserialization */ }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the plugin is executing as part of offline playback
        /// (syncing data from an offline client to the server).
        /// </summary>
        [DataMember(Order = 8)]
        public bool IsOfflinePlayback { get; set; }

        /// <summary>
        /// Gets or sets the isolation mode in which the plugin is executing.
        /// Common values are: 1 = None, 2 = Sandbox.
        /// </summary>
        [DataMember(Order = 9)]
        public int IsolationMode { get; set; }

        /// <summary>
        /// Gets or sets the name of the CRM message (operation) that triggered the plugin.
        /// Examples include "Create", "Update", "Delete", "Retrieve", "RetrieveMultiple", etc.
        /// </summary>
        [DataMember(Order = 10)]
        public string MessageName { get; set; }

        /// <summary>
        /// Gets or sets the execution mode of the plugin.
        /// Common values are: 0 = Synchronous, 1 = Asynchronous.
        /// </summary>
        /// <seealso cref="ProcessingStepMode"/>
        [DataMember(Order = 11)]
        public int Mode { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the plugin operation was created.
        /// </summary>
        [DataMember(Order = 12)]
        public DateTime OperationCreatedOn { get; set; }

        /// <summary>
        /// Gets or sets the GUID of the system job (async operation) that is executing the plugin.
        /// This is only populated for asynchronous plugin executions.
        /// </summary>
        [DataMember(Order = 13)]
        public Guid OperationId { get; set; }

        /// <summary>
        /// Gets or sets the GUID of the organization in which the plugin is executing.
        /// </summary>
        [DataMember(Order = 14)]
        public Guid OrganizationId { get; set; }

        /// <summary>
        /// Gets or sets the unique name of the organization in which the plugin is executing.
        /// </summary>
        [DataMember(Order = 15)]
        public string OrganizationName { get; set; }

        /// <summary>
        /// Gets or sets the output parameters from the plugin execution.
        /// For Create operations, this contains the "id" key with the GUID of the created record.
        /// For Retrieve operations, this contains the "BusinessEntity" key with the retrieved entity.
        /// </summary>
        [DataMember(Order = 16)]
        public ParameterCollection OutputParameters { get; set; }

        /// <summary>
        /// Gets or sets a reference to the plugin step registration (sdkmessageprocessingstep) that triggered this execution.
        /// </summary>
        [DataMember(Order = 17)]
        public EntityReference OwningExtension { get; set; }

        /// <summary>
        /// Gets or sets the collection of entity images (snapshots) captured after the core operation.
        /// Post-images contain the entity attribute values after the operation completed.
        /// Only available in Post-operation stage.
        /// </summary>
        [DataMember(Order = 18)]
        public EntityImageCollection PostEntityImages { get; set; }

        /// <summary>
        /// Gets or sets the collection of entity images (snapshots) captured before the core operation.
        /// Pre-images contain the entity attribute values before the operation was performed.
        /// </summary>
        [DataMember(Order = 19)]
        public EntityImageCollection PreEntityImages { get; set; }

        /// <summary>
        /// Gets or sets the GUID of the primary entity record on which the plugin is operating.
        /// </summary>
        [DataMember(Order = 20)]
        public Guid PrimaryEntityId { get; set; }

        /// <summary>
        /// Gets or sets the logical name of the primary entity type on which the plugin is operating.
        /// </summary>
        [DataMember(Order = 21)]
        public string PrimaryEntityName { get; set; }

        /// <summary>
        /// Gets or sets the GUID of the request that triggered the plugin execution.
        /// This is used for tracking purposes across multiple operations.
        /// </summary>
        [DataMember(Order = 22)]
        public Guid? RequestId { get; set; }

        /// <summary>
        /// Gets or sets the logical name of the secondary entity involved in the operation.
        /// This is typically used in relationship operations such as Associate/Disassociate.
        /// </summary>
        [DataMember(Order = 23)]
        public string SecondaryEntityName { get; set; }

        /// <summary>
        /// Gets or sets the collection of shared variables that can be used to pass data
        /// between plugins within the same execution pipeline.
        /// </summary>
        [DataMember(Order = 24)]
        public ParameterCollection SharedVariables { get; set; }

        /// <summary>
        /// Gets or sets the GUID of the user under whose context the plugin is executing.
        /// This may differ from InitiatingUserId if the plugin step is configured to run as a different user.
        /// </summary>
        [DataMember(Order = 25)]
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the parent plugin execution context if this plugin was triggered by another plugin.
        /// Returns null if this is the top-level plugin execution.
        /// </summary>
        [DataMember(Order = 26)]
        public IPluginExecutionContext ParentContext { get; set; }

        /// <summary>
        /// Gets or sets the stage in the plugin execution pipeline.
        /// Common values are: 10 = Pre-validation, 20 = Pre-operation, 40 = Post-operation.
        /// </summary>
        /// <seealso cref="ProcessingStepStage"/>
        [DataMember(Order = 27)]
        public int Stage { get; set; }

        // IPluginExecutionContext2 properties

        /// <summary>
        /// Gets or sets the Azure Active Directory Object Id for the user who initiated the plugin execution.
        /// This property is part of the IPluginExecutionContext2 interface for Azure AD integration.
        /// </summary>
        [DataMember(Order = 28)]
        public Guid InitiatingUserAzureActiveDirectoryObjectId { get; set; }

        /// <summary>
        /// Gets or sets the Azure Active Directory Object Id for the user under whose context the plugin is executing.
        /// This property is part of the IPluginExecutionContext2 interface for Azure AD integration.
        /// </summary>
        [DataMember(Order = 29)]
        public Guid UserAzureActiveDirectoryObjectId { get; set; }

        /// <summary>
        /// Gets or sets the application ID of the Azure AD application that initiated the plugin execution.
        /// This is used when the plugin is triggered by an application rather than a user.
        /// This property is part of the IPluginExecutionContext2 interface.
        /// </summary>
        [DataMember(Order = 30)]
        public Guid InitiatingUserApplicationId { get; set; }

        /// <summary>
        /// Gets or sets the contact ID associated with a Power Apps Portals user.
        /// This is populated when the plugin is triggered by a Portals request.
        /// This property is part of the IPluginExecutionContext2 interface.
        /// </summary>
        [DataMember(Order = 31)]
        public Guid PortalsContactId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the plugin was triggered by a Power Apps Portals client call.
        /// This property is part of the IPluginExecutionContext2 interface.
        /// </summary>
        [DataMember(Order = 32)]
        public bool IsPortalsClientCall { get; set; }

        // IPluginExecutionContext3 properties

        /// <summary>
        /// Gets or sets the GUID of the authenticated user who made the request that triggered the plugin.
        /// This property is part of the IPluginExecutionContext3 interface.
        /// </summary>
        [DataMember(Order = 33)]
        public Guid AuthenticatedUserId { get; set; }

        /// <summary>
        /// Gets or sets a custom DataCollection of properties from the parent plugin context.
        /// This allows passing custom data through nested plugin executions.
        /// This property is part of the IPluginExecutionContext3 interface.
        /// </summary>
        [DataMember(Order = 34)]
        public ParameterCollection ParentContextProperties { get; set; }

        // IPluginExecutionContext4 properties

        /// <summary>
        /// Gets or sets a value indicating whether the execution is part of a transaction integration message.
        /// This property is part of the IPluginExecutionContext4 interface.
        /// </summary>
        [DataMember(Order = 35)]
        public bool IsTransactionIntegrationMessage { get; set; }

        /// <summary>
        /// Gets or sets the collection of pre-entity images for batch operations.
        /// Each element in the array corresponds to an entity in a batch request.
        /// This property is part of the IPluginExecutionContext4 interface.
        /// </summary>
        [DataMember(Order = 36)]
        public EntityImageCollection[] PreEntityImagesCollection { get; set; }

        /// <summary>
        /// Gets or sets the collection of post-entity images for batch operations.
        /// Each element in the array corresponds to an entity in a batch request.
        /// This property is part of the IPluginExecutionContext4 interface.
        /// </summary>
        [DataMember(Order = 37)]
        public EntityImageCollection[] PostEntityImagesCollection { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="XrmFakedPluginExecutionContext"/> class with default values.
        /// </summary>
        /// <remarks>
        /// Default values are:
        /// <list type="bullet">
        /// <item><description>Depth = 1 (first level of execution)</description></item>
        /// <item><description>IsExecutingOffline = false</description></item>
        /// <item><description>MessageName = "Create"</description></item>
        /// <item><description>IsolationMode = 1 (None)</description></item>
        /// <item><description>All Azure AD and Portals properties are set to empty/false</description></item>
        /// <item><description>Entity image collections are initialized to empty arrays</description></item>
        /// </list>
        /// </remarks>
        public XrmFakedPluginExecutionContext()
        {
            Depth = 1;
            IsExecutingOffline = false;
            MessageName = "Create"; //Default value,
            IsolationMode = 1;

            // IPluginExecutionContext2
            InitiatingUserAzureActiveDirectoryObjectId = Guid.Empty;
            UserAzureActiveDirectoryObjectId = Guid.Empty;
            InitiatingUserApplicationId = Guid.Empty;
            PortalsContactId = Guid.Empty;
            IsPortalsClientCall = false;

            // IPluginExecutionContext3
            AuthenticatedUserId = Guid.Empty;
            ParentContextProperties = new ParameterCollection();

            // IPluginExecutionContext4
            IsTransactionIntegrationMessage = false;
            PreEntityImagesCollection = new EntityImageCollection[0];
            PostEntityImagesCollection = new EntityImageCollection[0];
        }
    }
}
