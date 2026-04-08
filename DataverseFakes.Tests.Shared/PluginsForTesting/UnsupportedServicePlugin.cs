using Microsoft.Xrm.Sdk;
using System;

namespace FakeXrmEasy.Tests.PluginsForTesting
{
    /// <summary>
    /// A test plugin that requests an unsupported service type to verify
    /// that the exception message includes the type name.
    /// </summary>
    public class UnsupportedServicePlugin : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            // Request an unsupported service type - IDisposable is not a supported service
            var unsupportedService = serviceProvider.GetService(typeof(IDisposable));
        }
    }
}
