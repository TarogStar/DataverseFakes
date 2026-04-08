using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System;

namespace FakeXrmEasy
{
    /// <summary>
    /// Provides a fake implementation of <see cref="IWorkflowContext"/> for unit testing Dynamics 365 custom workflow activities (code activities).
    /// This class simulates the workflow execution context that is normally provided by the CRM runtime during workflow execution.
    /// </summary>
    /// <remarks>
    /// Use this class when testing custom workflow activities to provide the necessary context information
    /// without requiring a connection to an actual Dynamics 365 environment.
    /// </remarks>
    public class XrmFakedWorkflowContext : IWorkflowContext
    {
        /// <summary>
        /// Gets or sets the GUID of the business unit in which the user who initiated the workflow activity execution operates.
        /// </summary>
        public Guid BusinessUnitId { get; set; }

        /// <summary>
        /// Gets or sets the GUID used to correlate related operations across multiple transactions.
        /// This is useful for tracking a series of related workflow executions.
        /// </summary>
        public Guid CorrelationId { get; set; }

        /// <summary>
        /// Gets or sets the current depth of execution in the call stack.
        /// The depth increases each time a workflow triggers another workflow or plugin.
        /// CRM enforces a maximum depth limit to prevent infinite loops.
        /// </summary>
        public int Depth { get; set; }

        /// <summary>
        /// Gets or sets the GUID of the user who originally initiated the workflow execution.
        /// This remains constant even if the workflow triggers other workflows or plugins.
        /// </summary>
        public Guid InitiatingUserId { get; set; }

        /// <summary>
        /// Gets or sets the input parameters that were passed to the workflow activity.
        /// Contains the values provided to the workflow's input arguments.
        /// </summary>
        public ParameterCollection InputParameters { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the workflow is executing in offline mode.
        /// </summary>
        public bool IsExecutingOffline { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the workflow is executing within a database transaction.
        /// </summary>
        public bool IsInTransaction { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the workflow is executing as part of offline playback
        /// (syncing data from an offline client to the server).
        /// </summary>
        public bool IsOfflinePlayback { get; set; }

        /// <summary>
        /// Gets or sets the isolation mode in which the workflow activity is executing.
        /// Common values are: 1 = None, 2 = Sandbox.
        /// </summary>
        public int IsolationMode { get; set; }

        /// <summary>
        /// Gets or sets the name of the CRM message (operation) that triggered the workflow.
        /// Examples include "Create", "Update", "Delete", etc.
        /// </summary>
        public string MessageName { get; set; }

        /// <summary>
        /// Gets or sets the execution mode of the workflow.
        /// Common values are: 0 = Synchronous, 1 = Asynchronous.
        /// </summary>
        public int Mode { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the workflow operation was created.
        /// </summary>
        public DateTime OperationCreatedOn { get; set; }

        /// <summary>
        /// Gets or sets the GUID of the system job (async operation) that is executing the workflow.
        /// </summary>
        public Guid OperationId { get; set; }

        /// <summary>
        /// Gets or sets the GUID of the organization in which the workflow is executing.
        /// </summary>
        public Guid OrganizationId { get; set; }

        /// <summary>
        /// Gets or sets the unique name of the organization in which the workflow is executing.
        /// </summary>
        public string OrganizationName { get; set; }

        /// <summary>
        /// Gets or sets the output parameters from the workflow activity.
        /// Contains the values that will be returned from the workflow's output arguments.
        /// </summary>
        public ParameterCollection OutputParameters { get; set; }

        /// <summary>
        /// Gets or sets a reference to the workflow definition entity that owns the currently executing activity.
        /// </summary>
        public EntityReference OwningExtension { get; set; }

        /// <summary>
        /// Gets or sets the parent workflow context if this workflow was triggered by another workflow.
        /// Returns null if this is the top-level workflow execution.
        /// </summary>
        public IWorkflowContext ParentContext { get; set; }

        /// <summary>
        /// Gets or sets the collection of entity images (snapshots) captured after the core operation.
        /// Post-images contain the entity attribute values after the operation completed.
        /// </summary>
        public EntityImageCollection PostEntityImages { get; set; }

        /// <summary>
        /// Gets or sets the collection of entity images (snapshots) captured before the core operation.
        /// Pre-images contain the entity attribute values before the operation was performed.
        /// </summary>
        public EntityImageCollection PreEntityImages { get; set; }

        /// <summary>
        /// Gets or sets the GUID of the primary entity record on which the workflow is operating.
        /// </summary>
        public Guid PrimaryEntityId { get; set; }

        /// <summary>
        /// Gets or sets the logical name of the primary entity type on which the workflow is operating.
        /// </summary>
        public string PrimaryEntityName { get; set; }

        /// <summary>
        /// Gets or sets the GUID of the request that triggered the workflow execution.
        /// This is used for tracking purposes across multiple operations.
        /// </summary>
        public Guid? RequestId { get; set; }

        /// <summary>
        /// Gets or sets the logical name of the secondary entity involved in the operation.
        /// This is typically used in relationship operations such as Associate/Disassociate.
        /// </summary>
        public string SecondaryEntityName { get; set; }

        /// <summary>
        /// Gets or sets the collection of shared variables that can be used to pass data
        /// between workflow activities and plugins within the same execution pipeline.
        /// </summary>
        public ParameterCollection SharedVariables { get; set; }

        /// <summary>
        /// Gets or sets the name of the workflow stage that is currently executing.
        /// </summary>
        public string StageName { get; set; }

        /// <summary>
        /// Gets or sets the GUID of the user under whose context the workflow is executing.
        /// This may differ from InitiatingUserId if the workflow is configured to run as a different user.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the category of the workflow.
        /// Common values are: 0 = Workflow, 1 = Dialog, 2 = Business Rule, 3 = Action, 4 = Business Process Flow.
        /// </summary>
        public int WorkflowCategory { get; set; }

#if FAKE_XRM_EASY_2013 || FAKE_XRM_EASY_2015 || FAKE_XRM_EASY_2016 || FAKE_XRM_EASY_365 || FAKE_XRM_EASY_9
        /// <summary>
        /// Gets or sets the mode in which the workflow is running.
        /// Common values are: 0 = Real-time, 1 = Background.
        /// </summary>
        public int WorkflowMode { get; set; }
#endif
    }
}
