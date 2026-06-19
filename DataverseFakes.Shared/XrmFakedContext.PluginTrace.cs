using DataverseFakes.Models;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataverseFakes
{
    /// <summary>
    /// Partial class providing structured plugin execution tracing for <see cref="XrmFakedContext"/>.
    /// Every plugin execution — whether triggered explicitly via <c>ExecutePluginWith*</c> methods
    /// or automatically by the pipeline simulation — is recorded here so tests can assert what ran.
    /// </summary>
    public partial class XrmFakedContext : IXrmContext
    {
        private readonly List<PluginExecutionRecord> _pluginExecutions = new List<PluginExecutionRecord>();

        /// <summary>
        /// Gets the ordered list of every plugin execution that occurred in this context.
        /// </summary>
        public IReadOnlyList<PluginExecutionRecord> PluginExecutions => _pluginExecutions.AsReadOnly();

        /// <summary>Clears all recorded plugin executions. Useful between test actions in the same context.</summary>
        public void ClearPluginExecutions()
        {
            _pluginExecutions.Clear();
        }

        /// <summary>
        /// Returns a human-readable multi-line dump of all recorded plugin executions, ordered by execution order.
        /// Useful for debugging test failures ("print what actually ran").
        /// </summary>
        public string GetPluginStepTrace()
        {
            if (_pluginExecutions.Count == 0)
            {
                return "(no plugin executions recorded)";
            }

            var sb = new StringBuilder();
            sb.AppendLine($"Plugin execution trace ({_pluginExecutions.Count} execution(s)):");
            for (int i = 0; i < _pluginExecutions.Count; i++)
            {
                sb.AppendLine($"  [{i + 1}] {_pluginExecutions[i]}");
            }
            return sb.ToString().TrimEnd();
        }

        /// <summary>
        /// Internal method called by the <c>ExecutePluginWith(XrmFakedPluginExecutionContext, IPlugin)</c>
        /// funnel to record a plugin execution. Extracts entity info from the context's Target parameter.
        /// </summary>
        internal void RecordPluginExecution(XrmFakedPluginExecutionContext ctx, IPlugin instance, Guid? stepId = null)
        {
            if (ctx == null || instance == null)
            {
                return;
            }

            string entityName = ctx.PrimaryEntityName;
            Guid? entityId = ctx.PrimaryEntityId == Guid.Empty ? (Guid?)null : ctx.PrimaryEntityId;

            // Extract from Target parameter when PrimaryEntityName/Id are not explicitly set
            if (ctx.InputParameters != null && ctx.InputParameters.ContainsKey("Target"))
            {
                var target = ctx.InputParameters["Target"];
                if (target is Entity entityTarget)
                {
                    if (string.IsNullOrEmpty(entityName))
                        entityName = entityTarget.LogicalName;
                    if (!entityId.HasValue && entityTarget.Id != Guid.Empty)
                        entityId = entityTarget.Id;
                }
                else if (target is EntityReference refTarget)
                {
                    if (string.IsNullOrEmpty(entityName))
                        entityName = refTarget.LogicalName;
                    if (!entityId.HasValue && refTarget.Id != Guid.Empty)
                        entityId = refTarget.Id;
                }
            }

            var record = new PluginExecutionRecord(
                pluginType: instance.GetType(),
                messageName: ctx.MessageName,
                stage: ctx.Stage,
                mode: ctx.Mode,
                primaryEntityName: entityName,
                primaryEntityId: entityId,
                stepId: stepId);

            _pluginExecutions.Add(record);
        }
    }
}
