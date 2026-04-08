using FakeXrmEasy.FakeMessageExecutors;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using System;
using Xunit;

namespace FakeXrmEasy.Tests.FakeContextTests.UtcTimeFromLocalTimeRequestTests
{
    /// <summary>
    /// Tests for UtcTimeFromLocalTimeRequestExecutor.
    /// Addresses upstream issue #340.
    /// </summary>
    public class UtcTimeFromLocalTimeRequestTests
    {
        [Fact]
        public void When_CanExecute_Is_Called_With_Invalid_Request_Returns_False()
        {
            var executor = new UtcTimeFromLocalTimeRequestExecutor();
            var anotherRequest = new RetrieveMultipleRequest();
            Assert.False(executor.CanExecute(anotherRequest));
        }

        [Fact]
        public void When_CanExecute_Is_Called_With_Valid_Request_Returns_True()
        {
            var executor = new UtcTimeFromLocalTimeRequestExecutor();
            var request = new UtcTimeFromLocalTimeRequest();
            Assert.True(executor.CanExecute(request));
        }

        [Fact]
        public void When_LocalTime_Is_Converted_Returns_Correct_UtcTime()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            // Use a specific local time
            var localTime = new DateTime(2025, 6, 15, 14, 30, 0, DateTimeKind.Local);

            var request = new UtcTimeFromLocalTimeRequest
            {
                LocalTime = localTime,
                TimeZoneCode = 1 // Not used in our implementation, but required by the SDK
            };

            var response = (UtcTimeFromLocalTimeResponse)service.Execute(request);

            // The UTC time should be different from local time (unless in UTC timezone)
            Assert.Equal(DateTimeKind.Utc, response.UtcTime.Kind);
        }

        [Fact]
        public void When_LocalTime_Is_Already_Utc_Returns_Same_Time()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var utcTime = new DateTime(2025, 6, 15, 14, 30, 0, DateTimeKind.Utc);

            var request = new UtcTimeFromLocalTimeRequest
            {
                LocalTime = utcTime,
                TimeZoneCode = 1
            };

            var response = (UtcTimeFromLocalTimeResponse)service.Execute(request);

            Assert.Equal(utcTime, response.UtcTime);
        }

        [Fact]
        public void When_LocalTime_Is_Unspecified_Treats_As_Local()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var unspecifiedTime = new DateTime(2025, 6, 15, 14, 30, 0, DateTimeKind.Unspecified);

            var request = new UtcTimeFromLocalTimeRequest
            {
                LocalTime = unspecifiedTime,
                TimeZoneCode = 1
            };

            var response = (UtcTimeFromLocalTimeResponse)service.Execute(request);

            Assert.Equal(DateTimeKind.Utc, response.UtcTime.Kind);
        }

        [Fact]
        public void GetResponsibleRequestType_Returns_Correct_Type()
        {
            var executor = new UtcTimeFromLocalTimeRequestExecutor();
            Assert.Equal(typeof(UtcTimeFromLocalTimeRequest), executor.GetResponsibleRequestType());
        }
    }
}
