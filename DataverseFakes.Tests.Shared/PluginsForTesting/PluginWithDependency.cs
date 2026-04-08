using System;
using Microsoft.Xrm.Sdk;

namespace FakeXrmEasy.Tests.PluginsForTesting
{
    /// <summary>
    /// Interface for a mock external service used to test dependency injection in plugins.
    /// </summary>
    public interface IExternalService
    {
        /// <summary>
        /// Gets a value that will be set on entities processed by the plugin.
        /// </summary>
        string GetValue();
    }

    /// <summary>
    /// A simple implementation of IExternalService for testing.
    /// </summary>
    public class MockExternalService : IExternalService
    {
        private readonly string _value;

        public MockExternalService(string value)
        {
            _value = value;
        }

        public string GetValue() => _value;
    }

    /// <summary>
    /// A plugin that requires an external service dependency.
    /// This plugin cannot be instantiated with a parameterless constructor,
    /// so it requires dependency injection support.
    /// </summary>
    public class PluginWithDependency : IPlugin
    {
        private readonly IExternalService _externalService;

        /// <summary>
        /// Creates a new instance of the plugin with the required dependency.
        /// </summary>
        /// <param name="externalService">The external service dependency.</param>
        public PluginWithDependency(IExternalService externalService)
        {
            _externalService = externalService ?? throw new ArgumentNullException(nameof(externalService));
        }

        public void Execute(IServiceProvider serviceProvider)
        {
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            if (context.InputParameters.TryGetValue("Target", out var targetObj) && targetObj is Entity target)
            {
                // Set the external service's value on the target entity
                target["description"] = _externalService.GetValue();
            }
        }
    }

    /// <summary>
    /// A plugin that counts how many times it has been executed.
    /// Used to verify factory vs instance behavior.
    /// </summary>
    public class ExecutionCounterPlugin : IPlugin
    {
        private int _executionCount;

        /// <summary>
        /// Gets the number of times this plugin instance has executed.
        /// </summary>
        public int ExecutionCount => _executionCount;

        public void Execute(IServiceProvider serviceProvider)
        {
            _executionCount++;

            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            var tracing = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            tracing.Trace($"Execution count: {_executionCount}");

            if (context.InputParameters.TryGetValue("Target", out var targetObj) && targetObj is Entity target)
            {
                // Store the execution count on the entity for verification
                target["executioncount"] = _executionCount;
            }
        }
    }
}
