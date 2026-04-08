using Microsoft.Xrm.Sdk;
using System;

namespace FakeXrmEasy.Tests.PluginsForTesting
{
    /// <summary>
    /// Test plugin that accesses pre-entity image
    /// </summary>
    public class PreImageAccessPlugin : IPlugin
    {
        public bool PreImageFound { get; private set; }
        public string PreImageName { get; private set; }
        public string PreImageAccountNumber { get; private set; }
        public bool PreImageHasTelephone { get; private set; }

        public void Execute(IServiceProvider serviceProvider)
        {
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            if (context.PreEntityImages.Contains("PreImage"))
            {
                PreImageFound = true;
                var preImage = context.PreEntityImages["PreImage"];

                if (preImage.Contains("name"))
                {
                    PreImageName = preImage.GetAttributeValue<string>("name");
                }

                if (preImage.Contains("accountnumber"))
                {
                    PreImageAccountNumber = preImage.GetAttributeValue<string>("accountnumber");
                }

                PreImageHasTelephone = preImage.Contains("telephone1");
            }
        }
    }
}
