using Microsoft.Xrm.Sdk;
using System;

namespace DataverseFakes.Models
{
    /// <summary>
    /// Captures metadata about a single plugin execution within an <see cref="XrmFakedContext"/>.
    /// Instances are created automatically whenever a plugin executes — both via explicit
    /// <c>ExecutePluginWith*</c> calls and via the auto-trigger pipeline (<see cref="XrmFakedContext.UsePipelineSimulation"/>).
    /// </summary>
    public class PluginExecutionRecord
    {
        /// <summary>Gets the concrete plugin type that was executed.</summary>
        public Type PluginType { get; }

        /// <summary>Gets the SDK message name (e.g. "Create", "Update", "Delete").</summary>
        public string MessageName { get; }

        /// <summary>
        /// Gets the pipeline stage value (10 = PreValidation, 20 = PreOperation, 40 = PostOperation).
        /// </summary>
        public int Stage { get; }

        /// <summary>
        /// Gets the execution mode (0 = Synchronous, 1 = Asynchronous).
        /// </summary>
        public int Mode { get; }

        /// <summary>Gets the logical name of the primary entity (may be null/empty when not available).</summary>
        public string PrimaryEntityName { get; }

        /// <summary>Gets the primary entity id extracted from the Target parameter (null when not available).</summary>
        public Guid? PrimaryEntityId { get; }

        /// <summary>Gets the sdkmessageprocessingstep id when triggered from the pipeline; null for explicit executions.</summary>
        public Guid? StepId { get; }

        /// <summary>
        /// Initializes a new <see cref="PluginExecutionRecord"/>.
        /// </summary>
        public PluginExecutionRecord(
            Type pluginType,
            string messageName,
            int stage,
            int mode,
            string primaryEntityName,
            Guid? primaryEntityId,
            Guid? stepId)
        {
            PluginType = pluginType ?? throw new ArgumentNullException(nameof(pluginType));
            MessageName = messageName;
            Stage = stage;
            Mode = mode;
            PrimaryEntityName = primaryEntityName;
            PrimaryEntityId = primaryEntityId;
            StepId = stepId;
        }

        /// <summary>Returns a human-readable one-line summary of this execution record.</summary>
        public override string ToString()
        {
            var entityInfo = string.IsNullOrEmpty(PrimaryEntityName)
                ? string.Empty
                : $" on {PrimaryEntityName}" + (PrimaryEntityId.HasValue ? $"({PrimaryEntityId.Value})" : string.Empty);

            return $"Stage={Stage} Message={MessageName} Mode={Mode} Plugin={PluginType.Name}{entityInfo}";
        }
    }
}
