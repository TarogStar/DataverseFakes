using FakeXrmEasy.Tests.PluginsForTesting;
using Microsoft.Xrm.Sdk;
using System;
using Xunit;

namespace FakeXrmEasy.Tests
{
    /// <summary>
    /// Tests for filtering attributes validation in pipeline simulation (v1.0.2)
    /// </summary>
    public class FilteringAttributesTests
    {
        [Fact]
        public void When_Plugin_Registered_With_Filtering_Attributes_Should_Only_Execute_When_Attributes_Present()
        {
      // Arrange
      var context = new XrmFakedContext
      {
        UsePipelineSimulation = true
      };

      // Register plugin with filtering attributes
      context.RegisterPluginStep<CounterPlugin>("Update",
                stage: ProcessingStepStage.Postoperation,
                filteringAttributes: new[] { "name", "accountnumber" });

            var service = context.GetOrganizationService();

            var accountId = Guid.NewGuid();
            context.Initialize(new[]
            {
                new Entity("account")
                {
                    Id = accountId,
                    ["name"] = "Test Account",
                    ["accountnumber"] = "ACC001",
                    ["revenue"] = new Money(100000)
                }
            });

            // Reset counter
            CounterPlugin.ExecutionCount = 0;

            // Act 1 - Update with filtered attribute (name)
            var update1 = new Entity("account")
            {
                Id = accountId,
                ["name"] = "Updated Name"
            };
            service.Update(update1);

            // Assert 1 - Plugin should have executed
            Assert.Equal(1, CounterPlugin.ExecutionCount);

            // Act 2 - Update with filtered attribute (accountnumber)
            var update2 = new Entity("account")
            {
                Id = accountId,
                ["accountnumber"] = "ACC002"
            };
            service.Update(update2);

            // Assert 2 - Plugin should have executed again
            Assert.Equal(2, CounterPlugin.ExecutionCount);

            // Act 3 - Update with non-filtered attribute (revenue)
            var update3 = new Entity("account")
            {
                Id = accountId,
                ["revenue"] = new Money(200000)
            };
            service.Update(update3);

            // Assert 3 - Plugin should NOT have executed
            Assert.Equal(2, CounterPlugin.ExecutionCount); // Still 2, not 3
        }

        [Fact]
        public void When_Plugin_Registered_Without_Filtering_Attributes_Should_Always_Execute()
        {
      // Arrange
      var context = new XrmFakedContext
      {
        UsePipelineSimulation = true
      };

      // Register plugin without filtering attributes
      context.RegisterPluginStep<CounterPlugin>("Update",
                stage: ProcessingStepStage.Postoperation);

            var service = context.GetOrganizationService();

            var accountId = Guid.NewGuid();
            context.Initialize(new[]
            {
                new Entity("account")
                {
                    Id = accountId,
                    ["name"] = "Test Account",
                    ["revenue"] = new Money(100000)
                }
            });

            CounterPlugin.ExecutionCount = 0;

            // Act - Update different attributes
            service.Update(new Entity("account") { Id = accountId, ["name"] = "Updated" });
            service.Update(new Entity("account") { Id = accountId, ["revenue"] = new Money(200000) });
            service.Update(new Entity("account") { Id = accountId, ["telephone1"] = "555-1234" });

            // Assert - Plugin should execute for all updates
            Assert.Equal(3, CounterPlugin.ExecutionCount);
        }

        [Fact]
        public void When_Plugin_Registered_With_Multiple_Filtering_Attributes_Should_Execute_If_Any_Present()
        {
      // Arrange
      var context = new XrmFakedContext
      {
        UsePipelineSimulation = true
      };

      context.RegisterPluginStep<CounterPlugin>("Update",
                stage: ProcessingStepStage.Postoperation,
                filteringAttributes: new[] { "name", "accountnumber", "telephone1" });

            var service = context.GetOrganizationService();

            var accountId = Guid.NewGuid();
            context.Initialize(new[]
            {
                new Entity("account") { Id = accountId, ["name"] = "Test" }
            });

            CounterPlugin.ExecutionCount = 0;

            // Act - Update with one of three filtering attributes
            service.Update(new Entity("account") { Id = accountId, ["telephone1"] = "555-1234" });

            // Assert - Plugin should execute (one filtering attribute is present)
            Assert.Equal(1, CounterPlugin.ExecutionCount);

            // Act - Update with multiple filtering attributes
            var updateWithMultiple = new Entity("account")
            {
                Id = accountId,
                ["name"] = "Updated",
                ["accountnumber"] = "ACC001"
            };
            service.Update(updateWithMultiple);

            // Assert - Plugin should execute again
            Assert.Equal(2, CounterPlugin.ExecutionCount);
        }

        [Fact]
        public void When_Target_Contains_Multiple_Attributes_But_None_Match_Filter_Should_Not_Execute()
        {
      // Arrange
      var context = new XrmFakedContext
      {
        UsePipelineSimulation = true
      };

      context.RegisterPluginStep<CounterPlugin>("Update",
                stage: ProcessingStepStage.Postoperation,
                filteringAttributes: new[] { "name" }); // Only name

            var service = context.GetOrganizationService();

            var accountId = Guid.NewGuid();
            context.Initialize(new[]
            {
                new Entity("account")
                {
                    Id = accountId,
                    ["name"] = "Test",
                    ["revenue"] = new Money(100000),
                    ["telephone1"] = "555-0000"
                }
            });

            CounterPlugin.ExecutionCount = 0;

            // Act - Update multiple attributes but not 'name'
            var update = new Entity("account")
            {
                Id = accountId,
                ["revenue"] = new Money(200000),
                ["telephone1"] = "555-1234",
                ["emailaddress1"] = "test@test.com"
            };
            service.Update(update);

            // Assert - Plugin should NOT execute
            Assert.Equal(0, CounterPlugin.ExecutionCount);
        }

        [Fact]
        public void When_Filtering_Attributes_Have_Whitespace_Should_Trim_And_Compare_Correctly()
        {
      // Arrange
      var context = new XrmFakedContext
      {
        UsePipelineSimulation = true
      };

      // Register with whitespace in filtering attributes (simulating string from config)
      context.RegisterPluginStep<CounterPlugin>("Update",
                stage: ProcessingStepStage.Postoperation,
                filteringAttributes: new[] { " name ", "accountnumber ", " telephone1" });

            var service = context.GetOrganizationService();

            var accountId = Guid.NewGuid();
            context.Initialize(new[]
            {
                new Entity("account") { Id = accountId, ["name"] = "Test" }
            });

            CounterPlugin.ExecutionCount = 0;

            // Act - Update with 'name' (no whitespace in actual attribute)
            service.Update(new Entity("account") { Id = accountId, ["name"] = "Updated" });

            // Assert - Plugin should execute (whitespace trimmed)
            Assert.Equal(1, CounterPlugin.ExecutionCount);
        }
    }
}
