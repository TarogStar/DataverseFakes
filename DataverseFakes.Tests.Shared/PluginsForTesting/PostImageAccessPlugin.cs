using Microsoft.Xrm.Sdk;
using System;

namespace FakeXrmEasy.Tests.PluginsForTesting
{
    /// <summary>
    /// Test plugin that accesses post-entity image
    /// </summary>
    public class PostImageAccessPlugin : IPlugin
    {
        public bool PostImageFound { get; private set; }
        public string PostImageName { get; private set; }

        public void Execute(IServiceProvider serviceProvider)
        {
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            if (context.PostEntityImages.Contains("PostImage"))
            {
                PostImageFound = true;
                var postImage = context.PostEntityImages["PostImage"];

                if (postImage.Contains("name"))
                {
                    PostImageName = postImage.GetAttributeValue<string>("name");
                }
            }
        }
    }
}
