using Microsoft.Xrm.Sdk;
using System;

namespace DataverseFakes.Tests.PluginsForTesting
{
    /// <summary>
    /// Simple plugin that counts executions - useful for testing filtering attributes
    /// </summary>
    public class CounterPlugin : IPlugin
    {
        public static int ExecutionCount { get; set; }

        public void Execute(IServiceProvider serviceProvider)
        {
            ExecutionCount++;
        }
    }
}
