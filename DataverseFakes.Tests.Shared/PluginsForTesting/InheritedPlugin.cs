using Microsoft.Xrm.Sdk;
using System;

namespace DataverseFakes.Tests.PluginsForTesting
{
    public abstract class PluginBase : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
        }
    }

    public class MyPlugin : PluginBase
    {
    }
}