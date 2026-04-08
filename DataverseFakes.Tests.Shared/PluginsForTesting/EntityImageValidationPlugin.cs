using System;
using Microsoft.Xrm.Sdk;

namespace FakeXrmEasy.Tests.PluginsForTesting
{
    /// <summary>
    /// Plugin that validates and traces entity image information for testing purposes.
    /// </summary>
    public class EntityImageValidationPlugin : IPlugin
    {
        /// <summary>
        /// Static storage for test verification - stores the last captured images.
        /// </summary>
        public static Entity LastPreImage { get; set; }
        public static Entity LastPostImage { get; set; }
        public static bool HasPreImage { get; set; }
        public static bool HasPostImage { get; set; }
        public static int ExecutionCount { get; set; }

        /// <summary>
        /// Resets the static state for test isolation.
        /// </summary>
        public static void Reset()
        {
            LastPreImage = null;
            LastPostImage = null;
            HasPreImage = false;
            HasPostImage = false;
            ExecutionCount = 0;
        }

        public void Execute(IServiceProvider serviceProvider)
        {
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            var tracing = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            ExecutionCount++;

            tracing.Trace($"EntityImageValidationPlugin executing - Message: {context.MessageName}, Stage: {context.Stage}");

            // Check for PreImage
            if (context.PreEntityImages != null && context.PreEntityImages.ContainsKey("PreImage"))
            {
                HasPreImage = true;
                LastPreImage = context.PreEntityImages["PreImage"];
                tracing.Trace($"PreImage found with {LastPreImage.Attributes.Count} attributes");
                foreach (var attr in LastPreImage.Attributes)
                {
                    tracing.Trace($"  PreImage.{attr.Key} = {attr.Value}");
                }
            }
            else
            {
                HasPreImage = false;
                LastPreImage = null;
                tracing.Trace("No PreImage found");
            }

            // Check for PostImage
            if (context.PostEntityImages != null && context.PostEntityImages.ContainsKey("PostImage"))
            {
                HasPostImage = true;
                LastPostImage = context.PostEntityImages["PostImage"];
                tracing.Trace($"PostImage found with {LastPostImage.Attributes.Count} attributes");
                foreach (var attr in LastPostImage.Attributes)
                {
                    tracing.Trace($"  PostImage.{attr.Key} = {attr.Value}");
                }
            }
            else
            {
                HasPostImage = false;
                LastPostImage = null;
                tracing.Trace("No PostImage found");
            }
        }
    }
}
