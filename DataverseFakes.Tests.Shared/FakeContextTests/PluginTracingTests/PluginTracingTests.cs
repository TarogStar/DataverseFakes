using DataverseFakes.Models;
using DataverseFakes.Tests.PluginsForTesting;
using Microsoft.Xrm.Sdk;
using System;
using Xunit;

namespace DataverseFakes.Tests.FakeContextTests.PluginTracingTests
{
    /// <summary>
    /// Tests for the plugin execution tracing + AssertPluginExecuted helpers (#183).
    /// Covers:
    ///   - Trace populated on explicit ExecutePluginWithTarget
    ///   - Trace populated on auto-fired pipeline (UsePipelineSimulation)
    ///   - Each AssertPluginExecuted* helper: happy path + failure path
    ///   - ClearPluginExecutions
    ///   - End-to-end auto-trigger proof (issue #183)
    /// </summary>
    [Collection("CounterPlugin")]
    public class PluginTracingTests
    {
        // ── Explicit-execution tracing ───────────────────────────────────────────────

        [Fact]
        public void Trace_populated_when_plugin_executed_via_ExecutePluginWithTarget()
        {
            var context = new XrmFakedContext();
            var target = new Entity("account") { Id = Guid.NewGuid() };

            context.ExecutePluginWithTarget<CounterPlugin>(target, "Create", 40);

            Assert.Single(context.PluginExecutions);
            var record = context.PluginExecutions[0];
            Assert.Equal(typeof(CounterPlugin), record.PluginType);
            Assert.Equal("Create", record.MessageName);
            Assert.Equal(40, record.Stage);
        }

        [Fact]
        public void Trace_captures_entity_name_and_id_from_target()
        {
            var context = new XrmFakedContext();
            var id = Guid.NewGuid();
            var target = new Entity("contact") { Id = id };

            context.ExecutePluginWithTarget<CounterPlugin>(target, "Update", 20);

            var record = context.PluginExecutions[0];
            Assert.Equal("contact", record.PrimaryEntityName);
            Assert.Equal(id, record.PrimaryEntityId);
        }

        [Fact]
        public void Trace_captures_entity_reference_for_delete_message()
        {
            var context = new XrmFakedContext();
            var id = Guid.NewGuid();
            var targetRef = new EntityReference("account", id);

            context.ExecutePluginWithTargetReference<CounterPlugin>(targetRef, "Delete", 40);

            Assert.Single(context.PluginExecutions);
            var record = context.PluginExecutions[0];
            Assert.Equal(typeof(CounterPlugin), record.PluginType);
            Assert.Equal("Delete", record.MessageName);
            Assert.Equal("account", record.PrimaryEntityName);
            Assert.Equal(id, record.PrimaryEntityId);
        }

        [Fact]
        public void Multiple_explicit_executions_accumulate_in_trace()
        {
            var context = new XrmFakedContext();
            var target = new Entity("account") { Id = Guid.NewGuid() };

            context.ExecutePluginWithTarget<CounterPlugin>(target, "Create", 40);
            context.ExecutePluginWithTarget<CounterPlugin>(target, "Update", 40);

            Assert.Equal(2, context.PluginExecutions.Count);
        }

        // ── ClearPluginExecutions ────────────────────────────────────────────────────

        [Fact]
        public void ClearPluginExecutions_removes_all_recorded_entries()
        {
            var context = new XrmFakedContext();
            var target = new Entity("account") { Id = Guid.NewGuid() };
            context.ExecutePluginWithTarget<CounterPlugin>(target, "Create", 40);

            Assert.NotEmpty(context.PluginExecutions);
            context.ClearPluginExecutions();

            Assert.Empty(context.PluginExecutions);
        }

        // ── GetPluginStepTrace ───────────────────────────────────────────────────────

        [Fact]
        public void GetPluginStepTrace_returns_placeholder_when_empty()
        {
            var context = new XrmFakedContext();
            var trace = context.GetPluginStepTrace();
            Assert.Contains("no plugin executions", trace);
        }

        [Fact]
        public void GetPluginStepTrace_contains_plugin_name_and_message()
        {
            var context = new XrmFakedContext();
            var target = new Entity("account") { Id = Guid.NewGuid() };
            context.ExecutePluginWithTarget<CounterPlugin>(target, "Create", 40);

            var trace = context.GetPluginStepTrace();
            Assert.Contains("CounterPlugin", trace);
            Assert.Contains("Create", trace);
        }

        // ── AssertPluginExecuted<T>() ────────────────────────────────────────────────

        [Fact]
        public void AssertPluginExecuted_passes_when_plugin_ran()
        {
            var context = new XrmFakedContext();
            var target = new Entity("account") { Id = Guid.NewGuid() };
            context.ExecutePluginWithTarget<CounterPlugin>(target, "Create", 40);

            context.AssertPluginExecuted<CounterPlugin>(); // should not throw
        }

        [Fact]
        public void AssertPluginExecuted_throws_when_plugin_did_not_run()
        {
            var context = new XrmFakedContext();

            var ex = Assert.Throws<XrmFakedAssertException>(() => context.AssertPluginExecuted<CounterPlugin>());
            Assert.Contains("CounterPlugin", ex.Message);
        }

        // ── AssertPluginExecuted<T>(messageName) ────────────────────────────────────

        [Fact]
        public void AssertPluginExecuted_with_messageName_passes_when_message_matches()
        {
            var context = new XrmFakedContext();
            var target = new Entity("account") { Id = Guid.NewGuid() };
            context.ExecutePluginWithTarget<CounterPlugin>(target, "Update", 40);

            context.AssertPluginExecuted<CounterPlugin>("Update"); // should not throw
        }

        [Fact]
        public void AssertPluginExecuted_with_messageName_throws_when_message_does_not_match()
        {
            var context = new XrmFakedContext();
            var target = new Entity("account") { Id = Guid.NewGuid() };
            context.ExecutePluginWithTarget<CounterPlugin>(target, "Create", 40);

            var ex = Assert.Throws<XrmFakedAssertException>(() => context.AssertPluginExecuted<CounterPlugin>("Delete"));
            Assert.Contains("Delete", ex.Message);
        }

        // ── AssertPluginExecutedTimes<T> ────────────────────────────────────────────

        [Fact]
        public void AssertPluginExecutedTimes_passes_when_count_matches()
        {
            var context = new XrmFakedContext();
            var target = new Entity("account") { Id = Guid.NewGuid() };
            context.ExecutePluginWithTarget<CounterPlugin>(target, "Create", 40);
            context.ExecutePluginWithTarget<CounterPlugin>(target, "Create", 40);
            context.ExecutePluginWithTarget<CounterPlugin>(target, "Create", 40);

            context.AssertPluginExecutedTimes<CounterPlugin>(3); // should not throw
        }

        [Fact]
        public void AssertPluginExecutedTimes_throws_when_count_is_wrong()
        {
            var context = new XrmFakedContext();
            var target = new Entity("account") { Id = Guid.NewGuid() };
            context.ExecutePluginWithTarget<CounterPlugin>(target, "Create", 40);

            var ex = Assert.Throws<XrmFakedAssertException>(() => context.AssertPluginExecutedTimes<CounterPlugin>(3));
            Assert.Contains("CounterPlugin", ex.Message);
            Assert.Contains("3", ex.Message);
        }

        // ── AssertPluginNotExecuted<T> ───────────────────────────────────────────────

        [Fact]
        public void AssertPluginNotExecuted_passes_when_plugin_never_ran()
        {
            var context = new XrmFakedContext();
            context.AssertPluginNotExecuted<CounterPlugin>(); // should not throw
        }

        [Fact]
        public void AssertPluginNotExecuted_throws_when_plugin_ran()
        {
            var context = new XrmFakedContext();
            var target = new Entity("account") { Id = Guid.NewGuid() };
            context.ExecutePluginWithTarget<CounterPlugin>(target, "Create", 40);

            var ex = Assert.Throws<XrmFakedAssertException>(() => context.AssertPluginNotExecuted<CounterPlugin>());
            Assert.Contains("CounterPlugin", ex.Message);
        }

        // ── Auto-trigger pipeline (issue #183 end-to-end proof) ──────────────────────

        [Fact]
        public void Pipeline_auto_trigger_Create_records_execution_and_AssertPluginExecuted_passes()
        {
            // Arrange
            CounterPlugin.ExecutionCount = 0;
            var context = new XrmFakedContext { UsePipelineSimulation = true };

            // RegisterPluginStep<TPlugin> (no entity type filter) — works for late-bound entities
            context.RegisterPluginStep<CounterPlugin>("Create",
                stage: ProcessingStepStage.Postoperation,
                mode: ProcessingStepMode.Synchronous);

            var service = context.GetOrganizationService();

            // Act — service.Create triggers the pipeline
            var account = new Entity("account") { Id = Guid.NewGuid() };
            service.Create(account);

            // Assert — the plugin fired and the trace recorded it
            Assert.Equal(1, CounterPlugin.ExecutionCount);
            context.AssertPluginExecuted<CounterPlugin>();
            context.AssertPluginExecuted<CounterPlugin>("Create");
            context.AssertPluginExecutedTimes<CounterPlugin>(1);
        }

        [Fact]
        public void Pipeline_auto_trigger_Update_records_execution()
        {
            // Arrange
            CounterPlugin.ExecutionCount = 0;
            var context = new XrmFakedContext { UsePipelineSimulation = true };
            context.RegisterPluginStep<CounterPlugin>("Update",
                stage: ProcessingStepStage.Postoperation);

            var accountId = Guid.NewGuid();
            context.Initialize(new[] { new Entity("account") { Id = accountId } });
            var service = context.GetOrganizationService();

            // Act
            service.Update(new Entity("account") { Id = accountId, ["name"] = "Changed" });

            // Assert
            Assert.Equal(1, CounterPlugin.ExecutionCount);
            context.AssertPluginExecuted<CounterPlugin>("Update");
        }

        [Fact]
        public void Pipeline_auto_trigger_Delete_records_execution()
        {
            // Arrange — Delete pipeline requires ProxyTypesAssembly so the entity type can be reflected
            CounterPlugin.ExecutionCount = 0;
            var context = new XrmFakedContext
            {
                UsePipelineSimulation = true,
                ProxyTypesAssembly = System.Reflection.Assembly.GetExecutingAssembly()
            };
            context.RegisterPluginStep<CounterPlugin>("Delete",
                stage: ProcessingStepStage.Postoperation);

            var accountId = Guid.NewGuid();
            context.Initialize(new[] { new Entity("account") { Id = accountId } });
            var service = context.GetOrganizationService();

            // Act
            service.Delete("account", accountId);

            // Assert
            Assert.Equal(1, CounterPlugin.ExecutionCount);
            context.AssertPluginExecuted<CounterPlugin>("Delete");
        }

        [Fact]
        public void Pipeline_not_triggered_when_UsePipelineSimulation_is_false()
        {
            // Arrange — pipeline off by default
            CounterPlugin.ExecutionCount = 0;
            var context = new XrmFakedContext { UsePipelineSimulation = false };
            context.RegisterPluginStep<CounterPlugin>("Create",
                stage: ProcessingStepStage.Postoperation);

            var service = context.GetOrganizationService();

            // Act
            service.Create(new Entity("account") { Id = Guid.NewGuid() });

            // Assert — no auto-fire, no trace
            Assert.Equal(0, CounterPlugin.ExecutionCount);
            context.AssertPluginNotExecuted<CounterPlugin>();
        }
    }
}
