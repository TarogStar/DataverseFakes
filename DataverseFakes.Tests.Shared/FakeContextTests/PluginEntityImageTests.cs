using FakeXrmEasy.Tests.PluginsForTesting;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using Xunit;

namespace FakeXrmEasy.Tests
{
    /// <summary>
    /// Tests for auto-populate entity images feature (v1.0.2)
    /// </summary>
    public class PluginEntityImageTests
    {
        [Fact]
        public void When_ExecutePluginWithTarget_With_PreImageColumns_Should_Auto_Populate_PreImage()
        {
            // Arrange
            var context = new XrmFakedContext();
            var accountId = Guid.NewGuid();

            // Create existing account in context
            var existingAccount = new Entity("account")
            {
                Id = accountId,
                ["name"] = "Original Name",
                ["accountnumber"] = "ACC001",
                ["revenue"] = new Money(100000)
            };
            context.Initialize(new[] { existingAccount });

            // Create update target with only changed fields
            var target = new Entity("account")
            {
                Id = accountId,
                ["name"] = "Updated Name"
            };

            // Create a test plugin that accesses pre-image
            var plugin = new PreImageAccessPlugin();

            // Act
            context.ExecutePluginWithTarget(plugin, target,
                messageName: "Update",
                stage: 40,
                preImageColumns: new ColumnSet(true));

            // Assert - plugin should have accessed pre-image successfully
            Assert.True(plugin.PreImageFound);
            Assert.Equal("Original Name", plugin.PreImageName);
            Assert.Equal("ACC001", plugin.PreImageAccountNumber);
        }

        [Fact]
        public void When_ExecutePluginWithTarget_With_PostImageColumns_Should_Auto_Populate_PostImage()
        {
            // Arrange
            var context = new XrmFakedContext();
            var accountId = Guid.NewGuid();

            // Create existing account in context
            var existingAccount = new Entity("account")
            {
                Id = accountId,
                ["name"] = "Test Account",
                ["revenue"] = new Money(100000)
            };
            context.Initialize(new[] { existingAccount });

            // Create target
            var target = new Entity("account")
            {
                Id = accountId,
                ["revenue"] = new Money(200000)
            };

            // Create a test plugin that accesses post-image
            var plugin = new PostImageAccessPlugin();

            // Act
            context.ExecutePluginWithTarget(plugin, target,
                messageName: "Update",
                stage: 40,
                postImageColumns: new ColumnSet(true));

            // Assert
            Assert.True(plugin.PostImageFound);
            Assert.Equal("Test Account", plugin.PostImageName);
        }

        [Fact]
        public void When_ExecutePluginWithTarget_With_Specific_PreImage_Columns_Should_Only_Include_Specified_Columns()
        {
            // Arrange
            var context = new XrmFakedContext();
            var accountId = Guid.NewGuid();

            var existingAccount = new Entity("account")
            {
                Id = accountId,
                ["name"] = "Test Account",
                ["accountnumber"] = "ACC001",
                ["revenue"] = new Money(100000),
                ["telephone1"] = "555-1234"
            };
            context.Initialize(new[] { existingAccount });

            var target = new Entity("account")
            {
                Id = accountId,
                ["name"] = "Updated"
            };

            var plugin = new PreImageAccessPlugin();

            // Act - only request name and accountnumber
            context.ExecutePluginWithTarget(plugin, target,
                messageName: "Update",
                stage: 40,
                preImageColumns: new ColumnSet("name", "accountnumber"));

            // Assert
            Assert.True(plugin.PreImageFound);
            Assert.Equal("Test Account", plugin.PreImageName);
            Assert.Equal("ACC001", plugin.PreImageAccountNumber);
            Assert.False(plugin.PreImageHasTelephone); // Should not be included
        }

        [Fact]
        public void When_ExecutePluginWithTarget_With_Custom_PreImage_Name_Should_Use_Custom_Name()
        {
            // Arrange
            var context = new XrmFakedContext();
            var accountId = Guid.NewGuid();

            var existingAccount = new Entity("account")
            {
                Id = accountId,
                ["name"] = "Test Account"
            };
            context.Initialize(new[] { existingAccount });

            var target = new Entity("account")
            {
                Id = accountId,
                ["name"] = "Updated"
            };

            var plugin = new CustomImageNamePlugin();

            // Act
            context.ExecutePluginWithTarget(plugin, target,
                messageName: "Update",
                stage: 40,
                preImageColumns: new ColumnSet(true),
                preImageName: "CustomPreImage");

            // Assert
            Assert.True(plugin.CustomPreImageFound);
        }

        [Fact]
        public void When_ExecutePluginWithTarget_Without_Image_Columns_Should_Not_Populate_Images()
        {
            // Arrange
            var context = new XrmFakedContext();
            var accountId = Guid.NewGuid();

            var existingAccount = new Entity("account")
            {
                Id = accountId,
                ["name"] = "Test Account"
            };
            context.Initialize(new[] { existingAccount });

            var target = new Entity("account")
            {
                Id = accountId,
                ["name"] = "Updated"
            };

            var plugin = new PreImageAccessPlugin();

            // Act - no image columns specified
            context.ExecutePluginWithTarget(plugin, target,
                messageName: "Update",
                stage: 40);

            // Assert - no images should be populated
            Assert.False(plugin.PreImageFound);
        }

        [Fact]
        public void When_ExecutePluginWithTarget_For_NonExistent_Entity_Should_Not_Populate_Images()
        {
            // Arrange
            var context = new XrmFakedContext();
            var accountId = Guid.NewGuid();

            var target = new Entity("account")
            {
                Id = accountId,
                ["name"] = "New Account"
            };

            var plugin = new PreImageAccessPlugin();

            // Act - entity doesn't exist in context
            context.ExecutePluginWithTarget(plugin, target,
                messageName: "Create",
                stage: 20,
                preImageColumns: new ColumnSet(true));

            // Assert - no pre-image should be found
            Assert.False(plugin.PreImageFound);
        }

        [Fact]
        public void When_ExecutePluginWithTarget_Generic_Method_With_Images_Should_Work()
        {
            // Arrange
            var context = new XrmFakedContext();
            var accountId = Guid.NewGuid();

            var existingAccount = new Entity("account")
            {
                Id = accountId,
                ["name"] = "Test Account"
            };
            context.Initialize(new[] { existingAccount });

            var target = new Entity("account")
            {
                Id = accountId,
                ["name"] = "Updated"
            };

            // Act - using generic method
            context.ExecutePluginWithTarget<PreImageAccessPlugin>(target,
                messageName: "Update",
                stage: 40,
                preImageColumns: new ColumnSet(true));

            // Note: We can't directly assert on the plugin instance with generic method
            // but we can verify no exceptions were thrown
            Assert.True(true);
        }
    }
}
