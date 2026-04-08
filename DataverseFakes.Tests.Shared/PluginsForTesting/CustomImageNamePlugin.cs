using Microsoft.Xrm.Sdk;
using System;

namespace FakeXrmEasy.Tests.PluginsForTesting
{
    /// <summary>
    /// Test plugin that accesses custom-named entity image
    /// </summary>
    public class CustomImageNamePlugin : IPlugin
    {
        public bool CustomPreImageFound { get; private set; }

        public void Execute(IServiceProvider serviceProvider)
        {
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            if (context.PreEntityImages.Contains("CustomPreImage"))
            {
                CustomPreImageFound = true;
            }
        }
    }
}
