using FakeXrmEasy.Tests.PluginsForTesting;
using Microsoft.Xrm.Sdk;
using System;
using Xunit;

namespace FakeXrmEasy.Tests.FakeContextTests
{
    /// <summary>
    /// Tests for service provider exception messages.
    /// </summary>
    public class ServiceProviderExceptionTests
    {
        [Fact]
        public void When_requesting_unsupported_service_exception_message_should_contain_type_name()
        {
            // Arrange
            var context = new XrmFakedContext();
            var target = new Entity("account") { Id = Guid.NewGuid() };

            // Act & Assert
            var exception = Assert.Throws<PullRequestException>(() =>
                context.ExecutePluginWithTarget<UnsupportedServicePlugin>(target, "Create", 40));

            // Verify the exception message contains the type name
            Assert.Contains("System.IDisposable", exception.Message);
        }

        [Fact]
        public void When_requesting_unsupported_service_exception_message_should_not_be_generic()
        {
            // Arrange
            var context = new XrmFakedContext();
            var target = new Entity("account") { Id = Guid.NewGuid() };

            // Act & Assert
            var exception = Assert.Throws<PullRequestException>(() =>
                context.ExecutePluginWithTarget<UnsupportedServicePlugin>(target, "Create", 40));

            // Verify the exception message is not the old generic message
            Assert.DoesNotContain("The specified service type is not supported", exception.Message);
        }
    }
}
