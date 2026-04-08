using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using Xunit;

namespace DataverseFakes.Tests
{
    /// <summary>
    /// Tests to verify that preImage does NOT get target values merged into it.
    /// Regression tests for state transition detection scenarios.
    /// </summary>
    public class PreImageTargetMergeTests
    {
        /// <summary>
        /// Test plugin that checks if preImage and target have different statecode values
        /// </summary>
        public class StateTransitionPlugin : IPlugin
        {
            public bool PreImageFound { get; private set; }
            public int? PreImageStateCode { get; private set; }
            public int? TargetStateCode { get; private set; }
            public bool TransitionDetected { get; private set; }

            public void Execute(IServiceProvider serviceProvider)
            {
                var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

                // Get target
                if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity target)
                {
                    if (target.Contains("statecode"))
                    {
                        TargetStateCode = target.GetAttributeValue<OptionSetValue>("statecode")?.Value;
                    }
                }

                // Get preImage
                if (context.PreEntityImages.Contains("PreImage"))
                {
                    PreImageFound = true;
                    var preImage = context.PreEntityImages["PreImage"];

                    if (preImage.Contains("statecode"))
                    {
                        PreImageStateCode = preImage.GetAttributeValue<OptionSetValue>("statecode")?.Value;
                    }
                }

                // Check if transition is detected (preImage has different value than target)
                if (PreImageStateCode.HasValue && TargetStateCode.HasValue && PreImageStateCode != TargetStateCode)
                {
                    TransitionDetected = true;
                }
            }
        }

        [Fact]
        public void ExecutePluginWithTarget_With_PreImageColumns_Should_Detect_State_Transition()
        {
            // Arrange
            var context = new XrmFakedContext();
            var accountId = Guid.NewGuid();

            // Create existing account with ACTIVE state (0)
            var existingAccount = new Entity("account")
            {
                Id = accountId,
                ["name"] = "Test Account",
                ["statecode"] = new OptionSetValue(0)  // Active
            };
            context.Initialize(new[] { existingAccount });

            // Create target with INACTIVE state (1) - this is the update
            var target = new Entity("account")
            {
                Id = accountId,
                ["statecode"] = new OptionSetValue(1)  // Inactive
            };

            var plugin = new StateTransitionPlugin();

            // Act - Execute with preImageColumns to auto-populate preImage
            context.ExecutePluginWithTarget(plugin, target,
                messageName: "Update",
                stage: 40,
                preImageColumns: new ColumnSet("statecode"));

            // Assert - preImage should have OLD value (0), target should have NEW value (1)
            Assert.True(plugin.PreImageFound, "PreImage should be found");
            Assert.Equal(0, plugin.PreImageStateCode); // Should be Active (0)
            Assert.Equal(1, plugin.TargetStateCode); // Target should have Inactive (1)
            Assert.True(plugin.TransitionDetected, "State transition should be detected");
        }

        [Fact]
        public void ExecutePluginWith_Manual_PreImage_Should_Detect_State_Transition()
        {
            // Arrange
            var context = new XrmFakedContext();
            var accountId = Guid.NewGuid();

            // Create preImage with ACTIVE state (0)
            var preImage = new Entity("account")
            {
                Id = accountId,
                ["statecode"] = new OptionSetValue(0)  // Active
            };

            // Create target with INACTIVE state (1)
            var target = new Entity("account")
            {
                Id = accountId,
                ["statecode"] = new OptionSetValue(1)  // Inactive
            };

            // Set up plugin context manually
            var pluginContext = context.GetDefaultPluginContext();
            pluginContext.MessageName = "Update";
            pluginContext.Stage = 40;
            pluginContext.InputParameters.Add("Target", target);
            pluginContext.PreEntityImages.Add("PreImage", preImage);

            var plugin = new StateTransitionPlugin();

            // Act
            context.ExecutePluginWith(pluginContext, plugin);

            // Assert - preImage should have OLD value (0), target should have NEW value (1)
            Assert.True(plugin.PreImageFound, "PreImage should be found");
            Assert.Equal(0, plugin.PreImageStateCode); // Should be Active (0)
            Assert.Equal(1, plugin.TargetStateCode); // Target should have Inactive (1)
            Assert.True(plugin.TransitionDetected, "State transition should be detected");
        }
    }
}
