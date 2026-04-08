using Microsoft.Xrm.Sdk;
using System;

namespace DataverseFakes.Tests.PluginsForTesting
{
    public class TestPropertiesPlugin : IPlugin
    {
        public string Property { get; set; }

        public void Execute(IServiceProvider serviceProvider)
        {
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            Property = "Property Updated";
        }
    }
}