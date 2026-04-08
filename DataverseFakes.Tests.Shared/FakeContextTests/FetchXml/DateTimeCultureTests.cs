using Crm;
using FakeXrmEasy.FakeMessageExecutors;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Threading;
using System.Xml.Linq;
using Xunit;

namespace FakeXrmEasy.Tests.FakeContextTests.FetchXml
{
    /// <summary>
    /// Tests for GitHub issue #439 - DateTime culture formatting in ExecuteFetchRequestExecutor
    ///
    /// The issue has TWO parts:
    /// 1. The ISO 8601 timestamp INSIDE the element must use InvariantCulture
    ///    (to prevent Finnish culture turning 13:12:43 into 13.12.43)
    /// 2. The date/time ATTRIBUTES should use the user's culture to match real D365 behavior
    ///
    /// Real D365 example (Finnish culture):
    /// &lt;createdon date="26.11.2018" time="14.41"&gt;2018-11-26T14:41:49+02:00&lt;/createdon&gt;
    /// </summary>
    public class DateTimeCultureTests
    {
        /// <summary>
        /// Tests that the ISO 8601 timestamp inside the element uses InvariantCulture,
        /// regardless of the current thread culture. This prevents issues where cultures
        /// like Finnish use periods instead of colons in time formatting.
        /// Uses AttributeValueToFetchResult directly to isolate the formatting logic.
        /// </summary>
        [Fact]
        public void AttributeValueToFetchResult_DateTime_IsoTimestamp_Uses_InvariantCulture()
        {
            // Arrange - Save current culture and set to Finnish (uses periods in time)
            var originalCulture = Thread.CurrentThread.CurrentCulture;
            var originalUICulture = Thread.CurrentThread.CurrentUICulture;

            try
            {
                // Finnish culture uses periods in time (14.30 instead of 14:30)
                var finnishCulture = new CultureInfo("fi-FI");
                Thread.CurrentThread.CurrentCulture = finnishCulture;
                Thread.CurrentThread.CurrentUICulture = finnishCulture;

                var executor = new ExecuteFetchRequestExecutor();
                var testDate = new DateTime(2018, 11, 26, 14, 41, 49, DateTimeKind.Local);

                // Act - Call the method directly
                var element = executor.AttributeValueToFetchResult(
                    new KeyValuePair<string, object>("createdon", testDate), null, null);

                // Assert - The ISO timestamp inside the element MUST use colons (not periods)
                Assert.NotNull(element);
                var isoTimestamp = element.Value;

                // The ISO 8601 format requires colons in the time portion
                // e.g., "2018-11-26T14:41:49+02:00" NOT "2018-11-26T14.41.49+02:00"
                Assert.Contains(":", isoTimestamp);
                Assert.Contains("T", isoTimestamp);

                // Verify the time portion uses colons (14:41:49, not 14.41.49)
                Assert.Matches(@"\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}", isoTimestamp);
            }
            finally
            {
                // Restore original culture
                Thread.CurrentThread.CurrentCulture = originalCulture;
                Thread.CurrentThread.CurrentUICulture = originalUICulture;
            }
        }

        /// <summary>
        /// Tests that date/time ATTRIBUTES use the current culture,
        /// matching real D365 behavior where these are formatted for the user.
        /// </summary>
        [Fact]
        public void AttributeValueToFetchResult_DateTime_Attributes_Use_CurrentCulture()
        {
            // Arrange - Save current culture and set to Finnish
            var originalCulture = Thread.CurrentThread.CurrentCulture;
            var originalUICulture = Thread.CurrentThread.CurrentUICulture;

            try
            {
                // Finnish culture uses dd.MM.yyyy for dates and HH.mm for times
                var finnishCulture = new CultureInfo("fi-FI");
                Thread.CurrentThread.CurrentCulture = finnishCulture;
                Thread.CurrentThread.CurrentUICulture = finnishCulture;

                var executor = new ExecuteFetchRequestExecutor();
                var testDate = new DateTime(2018, 11, 26, 14, 41, 49, DateTimeKind.Local);

                // Act - Call the method directly
                var element = executor.AttributeValueToFetchResult(
                    new KeyValuePair<string, object>("createdon", testDate), null, null);

                // Assert - The date and time ATTRIBUTES should use Finnish culture
                Assert.NotNull(element);
                var dateAttr = element.Attribute("date")?.Value;
                var timeAttr = element.Attribute("time")?.Value;

                Assert.NotNull(dateAttr);
                Assert.NotNull(timeAttr);

                // Finnish date format is dd.MM.yyyy or d.M.yyyy (short date)
                var expectedDate = testDate.ToString("d", finnishCulture);
                Assert.Equal(expectedDate, dateAttr);

                // Finnish time format uses periods or colons depending on OS settings
                var expectedTime = testDate.ToString("t", finnishCulture);
                Assert.Equal(expectedTime, timeAttr);
            }
            finally
            {
                // Restore original culture
                Thread.CurrentThread.CurrentCulture = originalCulture;
                Thread.CurrentThread.CurrentUICulture = originalUICulture;
            }
        }

        /// <summary>
        /// Tests that FetchXML condition values with ISO dates are parsed correctly
        /// regardless of current culture. FetchXML always uses ISO format for dates.
        /// </summary>
        [Fact]
        public void FetchXml_DateCondition_Uses_InvariantCulture_For_Parsing()
        {
            // Arrange - Save current culture and set to Finnish
            var originalCulture = Thread.CurrentThread.CurrentCulture;
            var originalUICulture = Thread.CurrentThread.CurrentUICulture;

            try
            {
                // Finnish culture would parse "11-26" as November 26th differently
                var finnishCulture = new CultureInfo("fi-FI");
                Thread.CurrentThread.CurrentCulture = finnishCulture;
                Thread.CurrentThread.CurrentUICulture = finnishCulture;

                var ctx = new XrmFakedContext();
                ctx.ProxyTypesAssembly = Assembly.GetAssembly(typeof(Contact));

                // ISO format date in FetchXML - should always parse correctly
                var fetchXml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                  <entity name='contact'>
                                        <attribute name='fullname' />
                                        <filter type='and'>
                                            <condition attribute='birthdate' operator='on' value='2018-11-26' />
                                        </filter>
                                  </entity>
                            </fetch>";

                // Act - This should not throw regardless of culture
                var query = XrmFakedContext.TranslateFetchXmlToQueryExpression(ctx, fetchXml);

                // Assert
                Assert.NotNull(query);
                Assert.NotNull(query.Criteria);
                Assert.Single(query.Criteria.Conditions);

                var conditionValue = query.Criteria.Conditions[0].Values[0];
                Assert.IsType<DateTime>(conditionValue);

                var parsedDate = (DateTime)conditionValue;
                Assert.Equal(2018, parsedDate.Year);
                Assert.Equal(11, parsedDate.Month);
                Assert.Equal(26, parsedDate.Day);
            }
            finally
            {
                // Restore original culture
                Thread.CurrentThread.CurrentCulture = originalCulture;
                Thread.CurrentThread.CurrentUICulture = originalUICulture;
            }
        }

        /// <summary>
        /// Tests that FetchXML with ISO datetime including time is parsed correctly
        /// regardless of current culture.
        /// </summary>
        [Fact]
        public void FetchXml_DateTimeCondition_With_Time_Uses_InvariantCulture_For_Parsing()
        {
            // Arrange - Save current culture and set to German (uses periods in dates)
            var originalCulture = Thread.CurrentThread.CurrentCulture;
            var originalUICulture = Thread.CurrentThread.CurrentUICulture;

            try
            {
                var germanCulture = new CultureInfo("de-DE");
                Thread.CurrentThread.CurrentCulture = germanCulture;
                Thread.CurrentThread.CurrentUICulture = germanCulture;

                var ctx = new XrmFakedContext();
                ctx.ProxyTypesAssembly = Assembly.GetAssembly(typeof(Contact));

                // ISO format datetime in FetchXML
                var fetchXml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                  <entity name='contact'>
                                        <attribute name='fullname' />
                                        <filter type='and'>
                                            <condition attribute='birthdate' operator='on' value='2018-11-26T14:30:00' />
                                        </filter>
                                  </entity>
                            </fetch>";

                // Act - This should not throw regardless of culture
                var query = XrmFakedContext.TranslateFetchXmlToQueryExpression(ctx, fetchXml);

                // Assert
                Assert.NotNull(query);
                var conditionValue = query.Criteria.Conditions[0].Values[0];
                Assert.IsType<DateTime>(conditionValue);

                var parsedDate = (DateTime)conditionValue;
                Assert.Equal(2018, parsedDate.Year);
                Assert.Equal(11, parsedDate.Month);
                Assert.Equal(26, parsedDate.Day);
                Assert.Equal(14, parsedDate.Hour);
                Assert.Equal(30, parsedDate.Minute);
            }
            finally
            {
                // Restore original culture
                Thread.CurrentThread.CurrentCulture = originalCulture;
                Thread.CurrentThread.CurrentUICulture = originalUICulture;
            }
        }
    }
}
